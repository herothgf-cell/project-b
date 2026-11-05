using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;

[Serializable] public sealed class PvpNativeHello {public string protocol,map,ticket,nickname;}
[Serializable] public sealed class PvpNativeRoom {public int slot,seconds;public string ticket,session,contentProfileJson;public bool started,racing;public bool[] humans;public string[] names;}
[Serializable] public sealed class PvpNativeDrive {public int sequence;public float throttle,steer;public bool brake,blocked;}
[Serializable] public sealed class PvpNativeCommand {public int sequence,value;public string action,token,trip,request;}

// Trusted LAN, host-authoritative native WheelCollider simulation. No client pose/fare authority.
[DefaultExecutionOrder(-110)]
public sealed class PvpNativeLan:MonoBehaviour {
 public const string Protocol="native-city-lan-v5";
 public static PvpNativeLan Instance {get;private set;}
 public static bool ApplyingReplica;
 public PvpSimulationMatch Match {get;private set;}
 public bool IsHost {get;private set;}public bool InMatch {get{return Match;}}
 public int LocalSlot {get;private set;}public int Seconds {get;private set;}=1200;
 public string Status {get;private set;}="";public string Address {get;private set;}public ushort Port {get;private set;}
 public bool Started {get;private set;}public bool Racing {get;private set;}public bool MenuOpen {get;private set;}
 public static bool ProbeMode {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeLanCheck")>=0;}}
 public Func<PvpNativeDrive> ProbeInput;
 public PvpNativePrediction Prediction {get;internal set;}
 public bool ProbeHoldInputs,ProbeHoldFrames;public int ProbeInputCount {get;private set;}
 public int ProcessedInput(int slot){return IsHost&&slot>=0&&slot<4&&seats[slot]!=null&&seats[slot].appliedThisTick?seats[slot].controls.Applied:0;}
 public int ProbeRejectedTripCommands {get;private set;}public int ProbeRejectedClaimCommands {get;private set;}
 public bool HasOwnFrame {get{return receivedOwn;}}
 public int ConnectedGuests {get{int n=0;foreach(var p in peers)if(p.Value.connected)n++;return n;}}
 public void ProbeSendCommand(PvpNativeCommand cmd){if(!ProbeMode||IsHost||!manager)return;cmd.sequence=++commandSeq;Send("cmd",0,cmd);}
 public static string LastNotice="";static string savedAddress,savedTicket;static ushort savedPort;
 static string fingerprint;
 NetworkManager manager;GameObject networkRoot;Pvp.PvpBootstrap bootstrap;bool stopping,disconnectPending,previousBackground;
 int inputSeq,commandSeq;bool receivedOwn;float nextFrame,nextFull,worldDeadline,lastFrameAt;string session,roomContent;string[] names=new string[4];bool[] humans=new bool[4];
 sealed class Seat {public int slot,lastInput,lastCommand,commandCount;public ulong id;public string ticket,nickname;public bool connected,ready,blocked,appliedThisTick;public float lastAt,graceUntil,rateAt;public PvpNativeDrive input;public readonly PvpNativeInputQueue controls=new PvpNativeInputQueue();}
 readonly Dictionary<ulong,Seat> peers=new Dictionary<ulong,Seat>();readonly Seat[] seats=new Seat[4];
 public static bool RunningOnline {get{return Instance&&Instance.InMatch&&!Instance.stopping;}}
 public static bool WaitingForWorld {get{return Instance&&!Instance.stopping&&Instance.Started&&!Instance.Racing;}}
 public static bool IsReplica(DriveSession s){return s&&Instance&&!Instance.IsHost&&!Instance.stopping&&s.business&&s.business.SimulationSession;}
 public static bool IsRemoteHuman(DriveSession s){if(!s||!Instance||!Instance.IsHost||!Instance.Match)return false;int slot=Instance.Match.DriverIndex(s);return slot>0&&Instance.seats[slot]!=null&&Instance.seats[slot].connected&&Instance.seats[slot].ready;}
 public static bool UiBlocked(DriveSession s){if(s&&s.accident&&s.accident.Locked)return true;if(!s||!Instance||s.simulationMatch!=Instance.Match)return false;if(WaitingForWorld)return true;if(s==Instance.Match.Human)return Instance.MenuOpen||s.RideResultOpen||s.FullMapOpen||s.services.open||s.law.HistoryOpen;if(Instance.IsHost){int slot=Instance.Match.DriverIndex(s);return slot>=0&&Instance.seats[slot]!=null&&Instance.seats[slot].connected&&Instance.seats[slot].blocked;}return false;}
 public static bool HandleEscape(DriveSession s){if(!RunningOnline||s!=Instance.Match.Human||!(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.JoystickButton7)))return false;Instance.ToggleMenu();return true;}
 public static bool Route(DriveSession s,string action,int value=0,string token=""){
  if(ApplyingReplica||!IsReplica(s))return false;
  if(Instance.Match&&s==Instance.Match.Human&&Instance.manager&&Instance.manager.IsConnectedClient&&Instance.Racing){var cmd=new PvpNativeCommand{sequence=++Instance.commandSeq,action=action,value=value,token=token??"",trip=s.law.tripId??"",request=s.requests.request.Id??""};Instance.Send("cmd",0,cmd);}
  return true;
 }
 public void ToggleMenu(){MenuOpen=!MenuOpen;}
 public int ServerToLocal(int server){if(IsHost)return server;if(server==LocalSlot)return 0;int at=1;for(int i=0;i<4;i++)if(i!=LocalSlot){if(i==server)return at;at++;}return -1;}
 public int LocalToServer(int local){if(IsHost)return local;if(local==0)return LocalSlot;int at=1;for(int i=0;i<4;i++)if(i!=LocalSlot){if(at==local)return i;at++;}return -1;}
 public string Label(int local){int slot=LocalToServer(local);if(slot>=0&&humans[slot]){string name=names[slot];return string.IsNullOrEmpty(name)?(local==0?"나":"플레이어 "+(slot+1)):name+(local==0?" (나)":"");}return "봇 "+(slot+1);}
 public static string MapFingerprint {get{if(fingerprint==null){var map=PvpCityMap.Create();using(var sha=SHA256.Create())fingerprint=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(map)))).Replace("-","").ToLowerInvariant();}return fingerprint;}}
 public static string LocalAddress(){try{foreach(var a in Dns.GetHostAddresses(Dns.GetHostName()))if(a.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a))return a.ToString();}catch{}return "127.0.0.1";}
 public void Begin(Pvp.PvpBootstrap owner,bool host,string address,ushort port,int seconds){
  if(Instance||NetworkManager.Singleton)throw new InvalidOperationException("이미 접속 중입니다");
  IPAddress parsed;if(!IPAddress.TryParse(address,out parsed)||parsed.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork||port==0||seconds<30||seconds>7200)throw new ArgumentException("IPv4 주소·포트·경기 시간을 확인하세요");
  Instance=this;bootstrap=owner;IsHost=host;Address=address;Port=port;Seconds=seconds;LocalSlot=host?0:-1;session=Guid.NewGuid().ToString("N");previousBackground=Application.runInBackground;Application.runInBackground=true;
  if(host){ContentTuningRuntime.LoadActive();roomContent=ContentTuningRuntime.Snapshot();ContentTuningRuntime.ValidateHostSnapshot(roomContent,null);seats[0]=new Seat{slot=0,id=0,ticket="",connected=true};humans[0]=true;names[0]=DriveFlowPolicy.LocalName;seats[0].nickname=names[0];}
  networkRoot=new GameObject("Native LAN transport");manager=networkRoot.AddComponent<NetworkManager>();var transport=networkRoot.AddComponent<UnityTransport>();transport.SetConnectionData(address,port,host?"0.0.0.0":null);transport.DisconnectTimeoutMS=120000;transport.ConnectTimeoutMS=1000;transport.MaxConnectAttempts=3;
  transport.MaxPayloadSize=1024*1024;transport.MaxSendQueueSize=4*1024*1024;
  manager.NetworkConfig=new NetworkConfig{NetworkTransport=transport,EnableSceneManagement=false,ConnectionApproval=true,TickRate=30,ForceSamePrefabs=false};manager.NetworkConfig.ConnectionData=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new PvpNativeHello{nickname=DriveFlowPolicy.LocalName,protocol=Protocol,map=MapFingerprint,ticket=!host&&savedAddress==address&&savedPort==port?savedTicket:""}));
  manager.ConnectionApprovalCallback=Approve;manager.OnClientConnectedCallback+=Connected;manager.OnClientDisconnectCallback+=Disconnected;
  if(!(host?manager.StartHost():manager.StartClient()))throw new InvalidOperationException("접속을 시작하지 못했습니다");
  foreach(var name in new[]{"room","ready","input","cmd","frame","closed"})manager.CustomMessagingManager.RegisterNamedMessageHandler("native-"+name,(sender,reader)=>Receive(name,sender,reader));
  Status=host?"방 생성 완료 · 참가자를 기다리거나 봇과 시작하세요":"호스트에 접속 중";
 }

 void Approve(NetworkManager.ConnectionApprovalRequest req,NetworkManager.ConnectionApprovalResponse res){
  string reason=null;Seat seat=null;
  try{if(req.ClientNetworkId==0){res.Approved=true;res.CreatePlayerObject=false;res.Pending=false;return;}
   if(req.Payload==null||req.Payload.Length>1024)reason="접속 정보 오류";
   else{var hello=JsonUtility.FromJson<PvpNativeHello>(Encoding.UTF8.GetString(req.Payload));if(hello==null||hello.protocol!=Protocol)reason="실행본 버전이 다릅니다";else if(hello.map!=MapFingerprint)reason="PVP 지도가 다릅니다";else{
    for(int i=1;i<4;i++)if(seats[i]!=null&&!seats[i].connected&&seats[i].ticket==hello.ticket&&seats[i].graceUntil>=Time.realtimeSinceStartup){seat=seats[i];break;}
    if(seat==null&&!Started)for(int i=1;i<4;i++)if(seats[i]==null||!seats[i].connected&&seats[i].graceUntil<Time.realtimeSinceStartup){seat=new Seat{slot=i,ticket=Guid.NewGuid().ToString("N")};seats[i]=seat;break;}
    if(seat!=null){string nick=DriveFlowPolicy.Nickname(hello.nickname);if(nick.Length>0)seat.nickname=nick;names[seat.slot]=seat.nickname;}
    if(seat==null)reason=Started?"시작한 방에는 재접속만 가능합니다":"방이 가득 찼습니다";
   }}
   if(reason==null){seat.id=req.ClientNetworkId;seat.connected=true;seat.ready=false;seat.lastAt=Time.realtimeSinceStartup;seat.lastCommand=seat.lastInput=0;seat.input=null;seat.controls.Clear();seat.appliedThisTick=false;seat.blocked=true;peers[req.ClientNetworkId]=seat;humans[seat.slot]=true;}
  }catch{reason="접속 정보 오류";}
  res.Approved=reason==null;res.CreatePlayerObject=false;res.Pending=false;res.Reason=reason??"";
 }
 void Connected(ulong id){if(!IsHost){Status="연결 완료 · 방 정보를 기다리는 중";return;}if(id==0)return;Seat seat;if(!peers.TryGetValue(id,out seat))return;SendRoom();}
 void Disconnected(ulong id){if(stopping)return;if(!IsHost){LastNotice=manager==null?"접속 종료":string.IsNullOrEmpty(manager.DisconnectReason)?"호스트 연결이 종료되었습니다":manager.DisconnectReason;disconnectPending=true;return;}Seat seat;if(peers.TryGetValue(id,out seat)){Debug.Log("PVP_NATIVE_DISCONNECTED slot="+seat.slot);seat.connected=false;seat.ready=false;seat.blocked=false;seat.graceUntil=Time.realtimeSinceStartup+30;humans[seat.slot]=false;peers.Remove(id);if(Match)ControlSeat(seat.slot,false);SendRoom();}}
 void SendRoom(){if(!IsHost||!manager)return;foreach(var pair in peers)if(pair.Value.connected)Send("room",pair.Key,new PvpNativeRoom{slot=pair.Value.slot,seconds=Seconds,ticket=pair.Value.ticket,session=session,contentProfileJson=roomContent,started=Started,racing=Racing,humans=humans,names=names});}
 public void StartMatch(){if(!IsHost||Started)return;Started=true;Racing=false;worldDeadline=Time.realtimeSinceStartup+120;Status="모든 참가자의 지도를 불러오는 중";SendRoom();bootstrap.StartSimulation(true,Seconds);}
 public void Bind(PvpSimulationMatch match){if(Match==match)return;Match=match;MenuOpen=false;foreach(var d in Match.Drivers){d.roadConditions=Match.Human.roadConditions;d.jobs.nav.conditions=d.roadConditions;foreach(var behaviour in d.GetComponents<MonoBehaviour>())if(behaviour is RightTurnSituation||behaviour is ExitQueueSituation||behaviour is PracticeSession||behaviour is PracticeCoach||behaviour is ConfusionPractice||behaviour is ScenarioChoiceSituation)behaviour.enabled=false;}if(IsHost){seats[0].ready=true;for(int i=1;i<4;i++)ControlSeat(i,seats[i]!=null&&seats[i].connected&&seats[i].ready);}else{lastFrameAt=Time.realtimeSinceStartup;PvpNativeReplica.BindClient(this);Send("ready",0,new PvpNativeCommand{token=session});}Debug.Log("PVP_NATIVE_BOUND role="+(IsHost?"host":"client")+" slot="+LocalSlot);}
 void ControlSeat(int slot,bool human){if(!Match||slot<=0||slot>=4)return;var d=Match.Drivers[slot];var pilot=d.GetComponent<PvpSimulationBot>();if(pilot)pilot.enabled=!human;d.services.enabled=human;d.assisted.SetActive(false);d.taxi.AutomatedHumanInput=human;d.taxi.ResetPedalIntent();d.taxi.autoThrottle=0;d.taxi.autoSteer=0;d.taxi.autoBrake=true;}
 static T Read<T>(FastBufferReader reader,int bytes){if(reader.Length>bytes)throw new ArgumentException("oversized message");reader.ReadValueSafe(out string json);return JsonUtility.FromJson<T>(json);}
 void Receive(string kind,ulong sender,FastBufferReader reader){if(stopping)return;try{
  if(IsHost){Seat seat;if(!peers.TryGetValue(sender,out seat)||!seat.connected)return;
   if(kind=="ready"){var ready=Read<PvpNativeCommand>(reader,1024);if(ready.token!=session)return;seat.ready=true;seat.lastAt=Time.realtimeSinceStartup;Debug.Log("PVP_NATIVE_READY slot="+seat.slot);if(Match)ControlSeat(seat.slot,true);}
   else if(kind=="input"){var input=Read<PvpNativeDrive>(reader,1024);if(!seat.ready||!seat.controls.Enqueue(input))return;seat.lastInput=input.sequence;seat.lastAt=Time.realtimeSinceStartup;}
   else if(kind=="cmd"){var cmd=Read<PvpNativeCommand>(reader,4096);if(!Racing||!Match||Match.Finished||!seat.ready||cmd.sequence<=seat.lastCommand)return;seat.lastCommand=cmd.sequence;if(Time.realtimeSinceStartup-seat.rateAt>1){seat.rateAt=Time.realtimeSinceStartup;seat.commandCount=0;}if(++seat.commandCount<=40)Execute(seat,cmd);}
  }else if(sender==0){
   if(kind=="room"){var room=Read<PvpNativeRoom>(reader,1024*1024);if(room==null||room.slot<1||room.slot>3||room.seconds<30||room.seconds>7200||room.humans==null||room.humans.Length!=4)throw new ArgumentException("invalid room");ContentTuningRuntime.ValidateHostSnapshot(room.contentProfileJson,roomContent);if(roomContent==null){ContentTuningRuntime.Restore(room.contentProfileJson);roomContent=ContentTuningRuntime.Snapshot();}LocalSlot=room.slot;Seconds=room.seconds;session=room.session;humans=room.humans;if(room.names!=null&&room.names.Length==4){for(int n=0;n<4;n++)names[n]=DriveFlowPolicy.Nickname(room.names[n]);}Started=room.started;Racing=room.racing;savedAddress=Address;savedPort=Port;savedTicket=room.ticket;Status=Started?"경기 준비/진행 중":"호스트의 시작을 기다리는 중";if(Started&&!Match)bootstrap.StartSimulation(true,Seconds);}
   else if(kind=="frame"){if(ProbeMode&&ProbeHoldFrames)return;long perf=PvpNativePerf.Start();var frame=Read<PvpNativeFrame>(reader,1024*1024);if(Match&&frame.recipient==LocalSlot){PvpNativeReplica.Apply(this,frame);lastFrameAt=Time.realtimeSinceStartup;receivedOwn|=frame.full;}PvpNativePerf.End("receive",perf);}
   else if(kind=="closed"){LastNotice="호스트가 방을 종료했습니다";disconnectPending=true;}
  }
 }catch(Exception e){Debug.LogWarning("PVP_NATIVE_MESSAGE_REJECT "+kind+" "+e.Message);if(!IsHost){LastNotice="동기화 정보/콘텐츠 설정 오류 · 양쪽 실행본을 갱신하세요: "+e.Message;disconnectPending=true;}}}
 void Execute(Seat seat,PvpNativeCommand c){var d=Match.Drivers[seat.slot];if(c==null||string.IsNullOrEmpty(c.action)||c.action.Length>40)return;
  bool tripAction=c.action=="interact"||c.action=="behavior"||c.action=="reply"||c.action=="response"||c.action=="eta"||c.action=="alternative"||c.action=="situation";
  if(tripAction&&(c.trip??"")!=(d.law.tripId??"")){ProbeRejectedTripCommands++;return;}
  if((c.action=="reply"||c.action=="response"||c.action=="eta"||c.action=="alternative")&&(c.request??"")!=(d.requests.request.Id??""))return;
  switch(c.action){
   case "accept":bool matched=false;for(int i=0;i<d.jobs.offers.Length;i++)if(d.jobs.offers[i]!=null&&d.jobs.offers[i].candidateSeed==c.token){matched=true;if(!d.jobs.Accept(i))ProbeRejectedClaimCommands++;break;}if(!matched)ProbeRejectedClaimCommands++;break;
   case "roadside":d.jobs.AcceptRoadside(c.value);break;case "interact":d.jobs.Interact();break;case "behavior":d.jobs.RespondBehavior(c.value);break;case "situation":if(d.jobs.SituationPending&&d.jobs.Situation.Pending.ToString()==c.token)d.jobs.RespondSituation(c.value);break;
   case "reply":if(Enum.IsDefined(typeof(PassengerReply),c.value))d.requests.Choose((PassengerReply)c.value);break;case "response":d.requests.Respond(c.value==1);break;case "eta":if(c.value>=-1&&c.value<=1)d.requests.RespondEta(c.value);break;case "alternative":d.requests.RespondAlternative();break;
   case "toggle-service":d.services.Toggle();break;case "close-service":d.services.Close(c.value==1);break;case "service-request":if(c.value>=0&&c.value<=5)d.services.RequestAction(c.value);break;case "service-confirm":d.services.ConfirmAction(c.value==1);break;case "service-buy":if(c.value>=0&&c.value<=5)d.services.Buy(c.value);break;case "service-meal":d.services.SelectMeal(c.value);break;
   case "guide":d.services.Guide(c.value);break;case "guide-fuel":d.services.GuideFuel(c.value);break;case "guide-after-trip":d.services.GuideAfterTrip(c.value);break;case "guide-facility":d.services.GuideFacilityById(c.value,c.token);break;
   case "signal":if(c.value>=-1&&c.value<=1)d.signals.Toggle(c.value);break;case "hazards":d.signals.ToggleHazards();break;case "assist":d.assisted.SetActive(c.value==1);break;case "assist-lane":d.assisted.RequestLaneChange(c.value);break;case "assist-turn":d.assisted.RequestTurn(c.value);break;
   case "nav-preference":d.jobs.nav.TogglePreference();break;case "nav-passenger":d.jobs.nav.SetPassengerPreference((c.value&1)!=0,(c.value&2)!=0);break;
   case "tow":d.damage.Tow();break;case "repair":d.damage.Repair();break;case "reset":d.taxi.RequestReset();break;case "emergency-rest":d.business.RequestEmergencyHome();break;
  }
 }

 void Send<T>(string kind,ulong target,T data){if(!manager||!manager.IsListening)return;string json=JsonUtility.ToJson(data);int length=8+json.Length*2;if(length>1024*1024)throw new InvalidOperationException("snapshot limit");using(var writer=new FastBufferWriter(Math.Max(128,length),Allocator.Temp)){writer.WriteValueSafe(json);manager.CustomMessagingManager.SendNamedMessage("native-"+kind,target,writer,NetworkDelivery.ReliableFragmentedSequenced);}}
 void Update(){
  if(stopping)return;if(disconnectPending){disconnectPending=false;LeaveToMenu();return;}
  if(IsHost&&Started&&Match&&!Racing){if(Time.realtimeSinceStartup>worldDeadline){var stale=new List<ulong>();foreach(var pair in peers)if(pair.Value.connected&&!pair.Value.ready)stale.Add(pair.Key);foreach(var id in stale)manager.DisconnectClient(id);worldDeadline=Time.realtimeSinceStartup+120;}bool all=seats[0].ready;foreach(var pair in peers)if(pair.Value.connected)all&=pair.Value.ready;if(all){Racing=true;foreach(var pair in peers)pair.Value.lastAt=Time.realtimeSinceStartup;Status="경기 중";SendRoom();}}
  if(!Match)return;
  if(Racing)PvpNativePerf.Frame();
  if(!IsHost&&Racing&&Time.realtimeSinceStartup-lastFrameAt>6){LastNotice="호스트 응답이 없어 연결을 종료합니다. 30초 내 같은 방으로 재참가할 수 있습니다.";disconnectPending=true;return;}
  if(IsHost&&Racing){var expired=new List<ulong>();foreach(var pair in peers)if(pair.Value.connected&&pair.Value.ready&&Time.realtimeSinceStartup-pair.Value.lastAt>4)expired.Add(pair.Key);foreach(var id in expired)manager.DisconnectClient(id);}
  if(IsHost){if(Time.unscaledTime>=nextFrame){nextFrame=Mathf.Max(nextFrame+.1f,Time.unscaledTime);bool full=Time.unscaledTime>=nextFull;if(full)nextFull=Time.unscaledTime+.5f;foreach(var pair in peers)if(pair.Value.connected&&pair.Value.ready)Send("frame",pair.Key,PvpNativeReplica.Capture(this,pair.Value.slot,full));}}
  else{PvpNativeReplica.Tick(this);if(Racing&&receivedOwn&&!UiBlocked(Match.Human)){float lateral=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);float pad=Input.GetAxisRaw("TaxiSteer");Match.Human.assisted.ReadInput(Mathf.Abs(pad)>.15f?pad:lateral);}}
 }
 PvpNativeDrive ReadDrive(){
  var own=Match.Human;bool blocked=!receivedOwn||UiBlocked(own)||!Application.isFocused;float throttle=0,steer=0;
  if(!blocked){throttle=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.JoystickButton0)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.JoystickButton1)?1:0);steer=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);float pad=Input.GetAxisRaw("TaxiSteer");if(Mathf.Abs(pad)>.15f)steer=pad;}
  var packet=new PvpNativeDrive{throttle=throttle,steer=steer,brake=blocked||Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.JoystickButton2),blocked=blocked};
  if(ProbeMode&&ProbeInput!=null&&receivedOwn)packet=ProbeInput();return packet;
 }
 void FixedUpdate(){
  if(!Match||!Racing||Match.Finished)return;
  if(!IsHost){if(!manager||!manager.IsConnectedClient||!receivedOwn||Prediction==null)return;var packet=ReadDrive();packet.sequence=++inputSeq;Prediction.Step(packet,Time.fixedDeltaTime);if(!(ProbeMode&&ProbeHoldInputs)){Send("input",0,packet);ProbeInputCount++;}return;}
  foreach(var pair in peers){var seat=pair.Value;if(!seat.connected||!seat.ready)continue;var next=seat.controls.Take();seat.appliedThisTick=next!=null;if(next!=null){seat.input=next;seat.blocked=next.blocked;}bool safe=seat.input==null||Time.realtimeSinceStartup-seat.lastAt>.5f||seat.blocked;PvpNativeInputQueue.Apply(Match.Drivers[seat.slot].taxi,seat.input,safe,Time.fixedDeltaTime);}
 }
 void LateUpdate(){if(Match&&!Match.Finished&&Time.timeScale!=1)Time.timeScale=1;}
 public void LeaveToMenu(){if(stopping)return;StopTransport();var unified=FindObjectOfType<UnifiedModeBootstrap>();if(unified)unified.ReturnToMenu();else{PvpSimulationMode.Reset();UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);}}
 public void StopTransport(){if(stopping)return;stopping=true;if(IsHost&&manager&&manager.IsListening)foreach(var pair in peers)if(pair.Value.connected)try{Send("closed",pair.Key,new PvpNativeCommand());}catch{}if(manager){manager.OnClientConnectedCallback-=Connected;manager.OnClientDisconnectCallback-=Disconnected;manager.Shutdown();}if(networkRoot)Destroy(networkRoot);networkRoot=null;manager=null;Application.runInBackground=previousBackground;if(Instance==this)Instance=null;}
 void OnDestroy(){StopTransport();}
 void OnGUI(){if(!Match)return;GUI.skin.font=KoreanText.Font;
  if(!Racing&&!Match.Finished)GUI.Box(new Rect(Screen.width/2-200,110,400,70),"다른 참가자의 지도 준비를 기다리는 중");
  if(MenuOpen){GUILayout.BeginArea(new Rect(Screen.width/2-170,Screen.height/2-110,340,220),GUI.skin.box);GUILayout.Label("LAN PVP · 경기는 계속 진행됩니다");if(GUILayout.Button("운전으로 돌아가기"))MenuOpen=false;if(GUILayout.Button(IsHost?"방 종료 · 모드 선택":"나가기 · 빈자리는 봇으로"))LeaveToMenu();GUILayout.EndArea();}
 }
}
