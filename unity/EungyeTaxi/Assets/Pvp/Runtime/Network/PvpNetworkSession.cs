using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;
using Pvp.Map;
using Pvp.Driving;
using Pvp.Passengers;
namespace Pvp.Network {
[Serializable] public sealed class PvpWelcome {public string Session,Ticket;public int Epoch;public float Grace;public bool Resumed;public PvpDriveSnapshot State;}
[Serializable] public struct PvpControl {public int Epoch,Index;}
// Direct-IP trusted-LAN trial. Tickets stay in process memory; no host migration or account authentication.
public sealed class PvpNetworkSession:MonoBehaviour {
 sealed class Peer {
  public PvpNetworkMotor Motor;public PvpInputBuffer Inputs=new PvpInputBuffer();public int Ack,Epoch,PassengerIndex;public Vector3 Start;
  public Peer(PvpMapRuntime map,int slot){Motor=new PvpNetworkMotor(map,slot,true);Start=Motor.Capture().State.Position;}
  public int ThrottleFrames,SteerFrames,BrakeFrames;
  public void StopMotor(){var state=Motor.Capture();state.State.Speed=0;state.State.Steering=0;Motor.Restore(state);}
  public void Freeze(){var state=Motor.Capture();state.State.Speed=0;state.State.Steering=0;Motor.Restore(state);Inputs=new PvpInputBuffer();}
 }
 sealed class Remote {public GameObject Object;public PvpVehicleState Current,Target;}
 NetworkManager manager;GameObject networkRoot;PvpPracticeSession practice;PvpMapRuntime map;PvpPrediction prediction;PvpReconnectRegistry registry;
 Pvp.Competition.PvpTaxiBot localBot;bool offlineBot,previousBackground;
 // Only the explicitly installed headless probe uses this input seam.
 public Func<PvpDriveSnapshot,PvpVehicleInput> OfflineTestInput;
 public bool IsOfflineBot {get{return offlineBot;}}
 public PvpMapRuntime Arena {get{return map;}}public PvpCompetitionBridge Competition {get{return competition;}}
 public string BotStatus {get{return localBot==null?"":localBot.Status;}}
 readonly Dictionary<ulong,Peer> peers=new Dictionary<ulong,Peer>();readonly Dictionary<ulong,Remote> visuals=new Dictionary<ulong,Remote>();
 PvpCompetitionBridge competition;Material remoteMaterial;bool host,ready,reverse,stopping,terminal,needsRetry,leaving,welcomeReady,dropPerformed,previewSent;
 int tick,samples,epoch,reconnects;float started,attemptStarted,driveStart=-1,lastSnapshot,accumulator,retryAt,retryDeadline,leaveDeadline;
 string address,credential,sessionId;ushort port;
 PvpDriveSnapshot latestAuthority;Vector3 ownStart,dropPosition;bool gotOwnStart,contentRestored;int dropPreview;float ownDistance,restoreError,resumedAt,postResumeDistance;Vector3 resumedPosition;
 public bool ProbeDriving,ProbeDriveSwap,ProbeDriveViolation;public int FixtureWrites {get;private set;}public bool ProbeCompetition;public bool Probe,ProbeReconnect,ProbeTamperTicket,ProbeLeave,ProbeExternalDrop;public float ProbeSeconds=14,ProbeRetryDelay=1,ReconnectGraceSeconds=30;public string ExpectedRejection;
 public bool IsHost {get{return host;}}public bool GuestConnected {get{return offlineBot||registry!=null&&registry.Connected;}}public bool InputReady {get{return ready&&!stopping&&(offlineBot||manager!=null);}}public int AuthorityTick {get{return tick;}}
 public PvpDriveSnapshot DriverPose {get{if(host)return AuthorityPose(0);if(prediction==null)return latestAuthority;var pose=prediction.State;pose.Tick=latestAuthority.Tick+prediction.PendingCount;return pose;}}
 public void ObserveTraffic(Pvp.Traffic.PvpTrafficSnapshot state){if(host){foreach(var peer in peers.Values)peer.Motor.ObserveTraffic(state);}else if(prediction!=null)prediction.ObserveTraffic(state);}
 public PvpDrivingAudit[] DrivingAudits(){var rows=new List<PvpDrivingAudit>();foreach(var pair in peers)rows.Add(new PvpDrivingAudit{Player=(int)pair.Key,DistanceMm=Mathf.RoundToInt(pair.Value.Motor.TravelDistance*1000),BoundaryStops=pair.Value.Motor.BoundaryStops,Throttle=pair.Value.ThrottleFrames,Steering=pair.Value.SteerFrames,Brake=pair.Value.BrakeFrames,FixtureWrites=FixtureWrites});rows.Sort((a,b)=>a.Player.CompareTo(b.Player));return rows.ToArray();}
 public PvpDriveSnapshot DisplayPose {get{return host?AuthorityPose(0):latestAuthority;}}
 internal PvpDriveSnapshot AuthorityPose(int slot){return peers.ContainsKey((ulong)slot)?Snapshot((ulong)slot,peers[(ulong)slot]):default(PvpDriveSnapshot);}
 internal void ResetAuthoritySpawn(int slot){int spawn=ProbeDriving&&ProbeDriveSwap?1-slot:slot;var peer=peers[(ulong)slot];peer.Motor.Restore(new PvpNetworkMotor(map,spawn,true).Capture());peer.Motor.ResetAudit();peer.ThrottleFrames=peer.SteerFrames=peer.BrakeFrames=0;}
 internal void SetFixtureStop(int slot,PvpDriveSnapshot pose){if(ProbeDriving||offlineBot)throw new InvalidOperationException("Position fixture forbidden during input driving or local bot match");if(!host||!ProbeCompetition||slot==1&&!GuestConnected)return;FixtureWrites++;peers[(ulong)slot].Motor.Restore(pose);}
 public void IssueCompetitionCommand(Pvp.Competition.PvpPassengerCommand command){if(!InputReady)return;if(host)competition.Queue(0,command);else {command.Epoch=epoch;Send("competition",0,command);}}
 internal void BroadcastWorld(PvpSharedWorld state){if(manager!=null)foreach(ulong id in manager.ConnectedClientsIds)if(id!=0)Send("world",id,state);}
 void ReceiveWorld(ulong sender,FastBufferReader reader){if(host||sender!=0||!welcomeReady)return;try{competition.Accept(Read<PvpSharedWorld>(reader,32768));}catch(Exception e){Terminal(e.Message);}}
 void ReceiveCompetition(ulong sender,FastBufferReader reader){if(!host)return;try{var command=Read<Pvp.Competition.PvpPassengerCommand>(reader,1024);if(registry.CanDrive(sender,command.Epoch))competition.Queue(1,command);}catch{manager.DisconnectClient(sender,"invalid competition request");}}
 public string Status {get;private set;}public float RemoteDistance {get;private set;}public int Samples {get{return samples;}}
 public void BeginOfflineBot(int seconds,int finishGrace){
  new PvpSessionConfig{MatchDurationSeconds=seconds,FinishGraceSeconds=finishGrace,PlayerCount=2,RulesVersion="solo-bot-v1"}.Validate();
  checked{int finalTick=150+seconds*50+finishGrace*50;if(finalTick<=150)throw new ArgumentException("경기 시간이 너무 큽니다");}
  if(NetworkManager.Singleton!=null)throw new InvalidOperationException("Network manager already active");
  offlineBot=true;host=true;previousBackground=Application.runInBackground;Application.runInBackground=true;started=Time.realtimeSinceStartup;
  var bootstrap=GetComponent<PvpBootstrap>();bootstrap.StartPractice();practice=GetComponent<PvpPracticeSession>();if(practice==null)throw new InvalidOperationException(bootstrap.StartupError);practice.SetScripted(true);map=practice.Map;
  remoteMaterial=new Material(Shader.Find("Standard")){color=new Color(.2f,.6f,1)};
  peers[0]=new Peer(map,0);peers[1]=new Peer(map,1);localBot=new Pvp.Competition.PvpTaxiBot(map,1);
  competition=gameObject.AddComponent<PvpCompetitionBridge>();competition.Begin(this,map,practice);ready=true;welcomeReady=true;
  competition.Queue(1,new Pvp.Competition.PvpPassengerCommand{Kind=0});competition.StartRound(seconds,finishGrace);
  Status="1인 경기 / 나와 로컬 봇 · 오프라인";Debug.Log("PVP_SOLO_BOT_STARTED transport=none players=human+bot seconds="+seconds+" grace="+finishGrace);
 }
 public void Begin(bool asHost,string ip,ushort listenPort,bool badMap=false){
  IPAddress parsed;if(!IPAddress.TryParse(ip,out parsed)||parsed.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork||listenPort==0)throw new ArgumentException("IPv4 address and port required");
  if(NetworkManager.Singleton!=null)throw new InvalidOperationException("Network manager already active");
  host=asHost;address=ip;port=listenPort;Application.runInBackground=true;started=Time.realtimeSinceStartup;Status="접속 중";
  var bootstrap=GetComponent<PvpBootstrap>();bootstrap.StartPractice();practice=GetComponent<PvpPracticeSession>();if(practice==null)throw new InvalidOperationException(bootstrap.StartupError);practice.SetScripted(true);map=practice.Map;
  remoteMaterial=new Material(Shader.Find("Standard")){color=new Color(.2f,.6f,1)};
  if(host){registry=new PvpReconnectRegistry(ReconnectGraceSeconds);sessionId=Guid.NewGuid().ToString("N");}
  competition=gameObject.AddComponent<PvpCompetitionBridge>();competition.Begin(this,map,practice);
  OpenTransport(badMap);
 }
 void OpenTransport(bool badMap=false){
  attemptStarted=Time.realtimeSinceStartup;terminal=false;welcomeReady=host;
  networkRoot=new GameObject("PVP direct IP transport");manager=networkRoot.AddComponent<NetworkManager>();var transport=networkRoot.AddComponent<UnityTransport>();
  transport.SetConnectionData(address,port,host?address:null);transport.DisconnectTimeoutMS=3000;transport.ConnectTimeoutMS=1000;transport.MaxConnectAttempts=3;
  manager.NetworkConfig=new NetworkConfig{NetworkTransport=transport,EnableSceneManagement=false,ConnectionApproval=true,TickRate=50,ForceSamePrefabs=false};
  var hello=PvpAdmission.Hello(badMap?"wrong-map":map.MapHash);hello.Ticket=credential;
  manager.NetworkConfig.ConnectionData=Encoding.UTF8.GetBytes(JsonUtility.ToJson(hello));
  manager.ConnectionApprovalCallback=Approve;manager.OnClientConnectedCallback+=Connected;manager.OnClientDisconnectCallback+=Disconnected;
  if(!(host?manager.StartHost():manager.StartClient()))throw new InvalidOperationException("Transport could not start");
  var messages=manager.CustomMessagingManager;messages.RegisterNamedMessageHandler("world",ReceiveWorld);messages.RegisterNamedMessageHandler("competition",ReceiveCompetition);messages.RegisterNamedMessageHandler("drive",ReceiveInput);messages.RegisterNamedMessageHandler("state",ReceiveState);messages.RegisterNamedMessageHandler("welcome",ReceiveWelcome);messages.RegisterNamedMessageHandler("preview",ReceivePreview);messages.RegisterNamedMessageHandler("leave",ReceiveLeave);
  if(host&&!peers.ContainsKey(0))Connected(0);
  Debug.Log("PVP_NETWORK_STARTED role="+(host?"host":"client")+" SDK=NGO1.12.0 protocol="+PvpAdmission.Protocol+" map="+map.MapHash);
 }
 void Approve(NetworkManager.ConnectionApprovalRequest req,NetworkManager.ConnectionApprovalResponse res){
  string reason=null;PvpJoinHello hello=null;bool resumed;
  try{if(req.Payload==null||req.Payload.Length>1024)reason="invalid handshake";else {hello=JsonUtility.FromJson<PvpJoinHello>(Encoding.UTF8.GetString(req.Payload));reason=PvpAdmission.Reject(hello,map.MapHash);}
   if(reason==null&&req.ClientNetworkId!=0)registry.TryReserve(req.ClientNetworkId,hello.Ticket,tick*PvpNetworkMotor.Delta,out resumed,out reason);
  }catch{reason="invalid handshake";}
  res.Approved=reason==null;res.CreatePlayerObject=false;res.Pending=false;res.Reason=reason??"";
  if(reason!=null)Debug.Log("PVP_ADMISSION_REJECT "+reason);
 }
 void Connected(ulong id){
  if(host){
   if(id==0){if(!peers.ContainsKey(0))peers[0]=new Peer(map,0);ready=true;Status="호스트 / 직접 IP 주행";return;}
   if(!registry.Confirm(id,tick*PvpNetworkMotor.Delta)){manager.DisconnectClient(id,"reconnect expired");return;}
   competition.GuestConnected();bool resumed=peers.ContainsKey(1);if(!resumed)peers[1]=new Peer(map,1);var peer=peers[1];peer.Inputs=new PvpInputBuffer();peer.Ack=0;peer.Epoch=registry.Epoch;
   if(driveStart<0)driveStart=Time.realtimeSinceStartup;
   Send("welcome",id,new PvpWelcome{Session=sessionId,Ticket=registry.TicketFor(id),Epoch=registry.Epoch,Grace=ReconnectGraceSeconds,Resumed=resumed,State=Snapshot(1,peer)});
   Status=resumed?"상대 재접속 · 기존 상태 복원":"상대 접속";Debug.Log(resumed?"PVP_PEER_RESUMED":"PVP_PEER_JOINED");
  }else if(manager.IsConnectedClient)Status="연결됨 / 서버 복원 상태 수신 대기";
 }
 void ReceiveWelcome(ulong sender,FastBufferReader reader){
  if(host||sender!=0)return;
  try{var welcome=Read<PvpWelcome>(reader,4096);if(welcome==null||string.IsNullOrEmpty(welcome.Session)||welcome.Ticket==null||welcome.Ticket.Length!=44||welcome.Epoch<1||float.IsNaN(welcome.Grace)||welcome.Grace<=0||welcome.Grace>120||welcome.State.Owner!=1||welcome.State.Epoch!=welcome.Epoch||welcome.State.Ack!=0)throw new ArgumentException("invalid welcome");
   if(sessionId!=null&&sessionId!=welcome.Session)throw new ArgumentException("host session changed");
   sessionId=welcome.Session;credential=welcome.Ticket;epoch=welcome.Epoch;ReconnectGraceSeconds=welcome.Grace;prediction=new PvpPrediction(map,1,true);welcomeReady=true;retryDeadline=0;needsRetry=false;
   if(welcome.Resumed){reconnects++;resumedAt=Time.realtimeSinceStartup;resumedPosition=welcome.State.State.Position;restoreError=Vector3.Distance(dropPosition,welcome.State.State.Position);contentRestored=welcome.State.PassengerIndex==dropPreview;Debug.Log("PVP_RECONNECT_RESTORED epoch="+epoch+" positionError="+restoreError+" preview="+welcome.State.PassengerIndex);}
   ApplyOwn(welcome.State);if(welcome.Resumed)competition.Reconnected();if(driveStart<0)driveStart=Time.realtimeSinceStartup;
  }catch(Exception e){Terminal(e.Message);}
 }
 void Disconnected(ulong id){
  if(stopping)return;
  if(host){if(registry!=null&&registry.Disconnect(id,tick*PvpNetworkMotor.Delta)&&peers.TryGetValue(1,out var peer)){peer.Freeze();competition.GuestDisconnected();Status="상대 연결 끊김 / 복귀 대기";PresentRemote(Snapshot(1,peer));}return;}
  ready=false;string reason=manager==null?"connection lost":manager.DisconnectReason??"";Status="연결 종료: "+reason;Debug.Log("PVP_NETWORK_DISCONNECTED "+reason);
  if(Probe&&!string.IsNullOrEmpty(ExpectedRejection)&&reason.Contains(ExpectedRejection)){Finish(true,"expected rejection "+reason);return;}
  if(leaving){if(Probe)Finish(reason.Contains("left session"),"explicit leave "+reason);else Exit();return;}
  if(Probe&&!ProbeReconnect){Finish(false,"admission/disconnect "+reason);return;}
  if(string.IsNullOrEmpty(credential)||PvpReconnectPolicy.IsTerminalRejection(reason)){Terminal(reason);return;}
  RequestReconnect(1);
 }
 void RequestReconnect(float delay){
  if(host||stopping||leaving)return;if(string.IsNullOrEmpty(credential)){Terminal("no reconnect credential");return;}
  if(retryDeadline<=0)retryDeadline=Time.realtimeSinceStartup+ReconnectGraceSeconds;
  competition.BeginReconnect();ready=false;welcomeReady=false;prediction=null;terminal=false;needsRetry=true;retryAt=Time.realtimeSinceStartup+delay;CloseTransport();Status="연결 복구 중 · 서버 차량 상태를 기다립니다";
 }
 static T Read<T>(FastBufferReader reader,int limit){if(reader.Length>limit)throw new ArgumentException("oversized message");reader.ReadValueSafe(out string json);return JsonUtility.FromJson<T>(json);}
 void ReceiveInput(ulong sender,FastBufferReader reader){
  if(!host||sender==0||!peers.TryGetValue(1,out var peer))return;
  try{var cmd=Read<PvpDriveCommand>(reader,1024);if(!registry.CanDrive(sender,cmd.Epoch))return;peer.Inputs.Enqueue(cmd);}catch{manager.DisconnectClient(sender,"invalid drive input");}
 }
 void ReceivePreview(ulong sender,FastBufferReader reader){
  if(!host)return;try{var choice=Read<PvpControl>(reader,512);if(!registry.CanDrive(sender,choice.Epoch)||choice.Index<0||choice.Index>=PvpPassengerCatalog.Count)return;peers[1].PassengerIndex=choice.Index;}catch{manager.DisconnectClient(sender,"invalid preview choice");}
 }
 void ReceiveLeave(ulong sender,FastBufferReader reader){
  if(!host)return;try{var request=Read<PvpControl>(reader,512);if(!registry.CanDrive(sender,request.Epoch))return;if(registry.Leave(sender)){competition.GuestLeft();peers[1].Freeze();PresentRemote(Snapshot(1,peers[1]));manager.DisconnectClient(sender,"left session");Status="상대가 나갔습니다 · 새 세션에서 참가 가능";}}catch{manager.DisconnectClient(sender,"invalid leave request");}
 }
 void ReceiveState(ulong sender,FastBufferReader reader){
  if(host||sender!=0||!welcomeReady)return;
  try{var state=Read<PvpDriveSnapshot>(reader,2048);if(state.Owner==1){if(state.Epoch==epoch)ApplyOwn(state);}else if(state.Owner==0)PresentRemote(state);}catch(Exception e){Terminal(e.Message);}
 }
 void ApplyOwn(PvpDriveSnapshot state){
  if(state.PassengerIndex<0||state.PassengerIndex>=PvpPassengerCatalog.Count)throw new ArgumentException("invalid passenger content index");
  prediction.Reconcile(state);latestAuthority=state;if(reconnects>0)postResumeDistance=Mathf.Max(postResumeDistance,Vector3.Distance(resumedPosition,state.State.Position));if(!gotOwnStart){ownStart=state.State.Position;gotOwnStart=true;}ownDistance=Mathf.Max(ownDistance,Vector3.Distance(ownStart,state.State.Position));ready=true;lastSnapshot=Time.realtimeSinceStartup;samples++;practice.PresentNetworkState(prediction.State.State);Status=reconnects>0?"재접속 완료 / 서버 상태 복원":"클라이언트 / 입력 예측·권한 보정";
 }
 void Send<T>(string name,ulong target,T value){using(var writer=new FastBufferWriter(32768,Allocator.Temp)){writer.WriteValueSafe(JsonUtility.ToJson(value));manager.CustomMessagingManager.SendNamedMessage(name,target,writer,NetworkDelivery.ReliableFragmentedSequenced);}}
 PvpDriveSnapshot Snapshot(ulong owner,Peer peer){var state=peer.Motor.Capture();state.Owner=owner;state.Ack=peer.Ack;state.Tick=tick;state.Epoch=peer.Epoch;state.PassengerIndex=peer.PassengerIndex;state.Connected=owner==0||GuestConnected;return state;}
 void PresentRemote(PvpDriveSnapshot state){
  if(!visuals.TryGetValue(state.Owner,out var remote)){var obj=new GameObject("PVP remote authority taxi");obj.transform.SetParent(transform,false);PvpTaxiVisual.Build(obj.transform,true,new Color(.2f,.6f,1));remote=new Remote{Object=obj,Current=state.State};visuals[state.Owner]=remote;}
  if(!remote.Object.activeSelf)remote.Current=state.State;remote.Target=state.State;remote.Object.SetActive(state.Connected);
 }
 void LateUpdate(){foreach(var remote in visuals.Values){if(!remote.Object.activeSelf)continue;remote.Current=PvpRemotePresentation.Step(remote.Current,remote.Target,Time.unscaledDeltaTime);remote.Object.transform.SetPositionAndRotation(remote.Current.Position,Quaternion.Euler(0,remote.Current.YawDegrees,0));}}
 PvpVehicleInput InputNow(){if(offlineBot&&Probe&&OfflineTestInput!=null)return OfflineTestInput(DriverPose);if(Probe){if(ProbeCompetition)return ProbeDriving?competition.DrivingInput(DriverPose):new PvpVehicleInput(0,1,0);if(!host&&reconnects>0){bool accelerating=Time.realtimeSinceStartup-resumedAt<1;return new PvpVehicleInput(accelerating?1:0,accelerating?0:1,0);}float age=driveStart<0?0:Time.realtimeSinceStartup-driveStart;return new PvpVehicleInput(age>0&&age<2?1:0,age>=2?1:0,0);}return new PvpVehicleInput((Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow))?1:0,Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.Space)?1:0,(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),reverse,Application.isFocused);}
 void Update(){
  if(stopping)return;float now=Time.realtimeSinceStartup;
  if(!Probe&&Input.GetKeyDown(KeyCode.Escape)){Stop();return;}
  if(leaving){if(now>=leaveDeadline){if(Probe)Finish(false,"leave acknowledgement timeout");else Exit();}return;}
  if(terminal)return;
  try{
   if(needsRetry){if(now>=retryDeadline){Terminal("reconnect timeout");return;}if(now>=retryAt&&NetworkManager.Singleton==null){needsRetry=false;try{OpenTransport();}catch{RequestReconnect(1);}}return;}
   if(manager==null&&!offlineBot)return;
   if(!host&&retryDeadline>0&&now>=retryDeadline){Terminal("reconnect timeout");return;}
   if(!host&&ready&&PvpReconnectPolicy.ShouldRecover(prediction.PendingCount,now-lastSnapshot)){if(Probe&&!ProbeReconnect)Terminal("authority timeout");else RequestReconnect(1);return;}
   if(!ready&&now-attemptStarted>8){if(string.IsNullOrEmpty(credential))Terminal("connection timeout");else RequestReconnect(1);return;}
   if(!Probe&&ready&&Input.GetKeyDown(KeyCode.Q)&&Mathf.Abs(practice.Capture().Speed)<.35f)reverse=!reverse;
   if(Probe&&!host&&ready&&!previewSent){SelectPreview(10);previewSent=true;}
   if(Probe&&!host&&ready&&!dropPerformed&&now-started>5&&(ProbeReconnect||ProbeLeave)){
    dropPerformed=true;dropPosition=latestAuthority.State.Position;dropPreview=latestAuthority.PassengerIndex;
    if(ProbeLeave){Stop();return;}if(ProbeTamperTicket)credential="invalid-ticket";if(!ProbeExternalDrop){RequestReconnect(ProbeRetryDelay);return;}
   }
   if(host&&!offlineBot)registry.Expire(tick*PvpNetworkMotor.Delta);
   accumulator+=Time.unscaledDeltaTime;int steps=0;while(accumulator>=PvpNetworkMotor.Delta&&steps++<5){accumulator-=PvpNetworkMotor.Delta;Advance();}if(steps>5)accumulator=0;
   if(Probe&&!offlineBot&&now-started>ProbeSeconds){
    float positionError=prediction==null?0:Vector3.Distance(prediction.State.State.Position,latestAuthority.State.Position);float yawError=prediction==null?0:Mathf.Abs(Mathf.DeltaAngle(prediction.State.State.YawDegrees,latestAuthority.State.YawDegrees));float speedError=prediction==null?0:Mathf.Abs(prediction.State.State.Speed-latestAuthority.State.Speed);
    bool ok=string.IsNullOrEmpty(ExpectedRejection)&&!ProbeLeave&&(host?RemoteDistance>10:samples>20&&prediction!=null&&prediction.PendingCount<30&&ownDistance>10&&positionError<.05f&&yawError<.1f&&speedError<.05f&&Mathf.Abs(latestAuthority.State.Speed)<.05f&&latestAuthority.PassengerIndex==10&&(!ProbeReconnect||reconnects>0&&restoreError<.001f&&contentRestored&&postResumeDistance>2));
    if(ProbeCompetition){ok=competition.ProbeSucceeded&&(!ProbeReconnect||host||reconnects>0&&competition.TripRestored);Debug.Log("PVP_MATCH_RESULT "+competition.ResultSignature);if(ProbeDriving)Debug.Log("PVP_DRIVING_PROOF "+competition.DrivingProof);}
    Finish(ok,"samples="+samples+" pending="+(prediction==null?0:prediction.PendingCount)+" remoteDistance="+RemoteDistance+" ownDistance="+ownDistance+" positionError="+positionError+" yawError="+yawError+" speedError="+speedError+" maxCorrection="+(prediction==null?0:prediction.MaxCorrection)+" postResumeDistance="+postResumeDistance+" reconnects="+reconnects+" preview="+latestAuthority.PassengerIndex);
   }
  }catch(Exception e){Terminal(e.Message);}
 }
 void Advance(){
  if(!ready)return;tick++;if(host)competition.BeforeTick(tick);
  if(host){foreach(var pair in peers){var peer=pair.Value;var before=Snapshot(pair.Key,peer);PvpVehicleInput input;if(pair.Key==0)input=InputNow();else if(offlineBot){var decision=localBot.StepWithServices(competition.CurrentMatch,competition.CurrentTraffic,before);input=decision.Input;if(decision.Command.HasValue)competition.Queue(1,decision.Command.Value);}else input=GuestConnected?peer.Inputs.InputForTick(out peer.Ack):new PvpVehicleInput(0,1,0);if(competition.CanDrive((int)pair.Key)&&(pair.Key==0||GuestConnected)){if(input.Throttle>0)peer.ThrottleFrames++;if(Mathf.Abs(input.Steering)>.1f)peer.SteerFrames++;if(input.Brake>0)peer.BrakeFrames++;peer.Motor.Step(input,tick);}else peer.StopMotor();var state=Snapshot(pair.Key,peer);var motion=peer.Motor.LastMotion;if(motion.Tick!=tick)motion=new Pvp.Rules.PvpMotionTrace{Tick=tick};competition.ObserveDriving((int)pair.Key,before,state,motion,tick);if(pair.Key==0)practice.PresentNetworkState(state.State);else{RemoteDistance=Mathf.Max(RemoteDistance,Vector3.Distance(peer.Start,state.State.Position));PresentRemote(state);}if(tick%5==0&&manager!=null)foreach(ulong id in manager.ConnectedClientsIds)if(id!=0)Send("state",id,state);}competition.AfterTick(tick);}
  else if(prediction!=null){var cmd=prediction.Predict(competition.DrivingAllowed?InputNow():new PvpVehicleInput(0,1,0));cmd.Epoch=epoch;Send("drive",0,cmd);practice.PresentNetworkState(prediction.State.State);}
 }
 void SelectPreview(int index){if(!ready)return;if(host)peers[0].PassengerIndex=index;else Send("preview",0,new PvpControl{Epoch=epoch,Index=index});}
 void Terminal(string reason){if(competition!=null)competition.ConnectionLost();Status="접속 중단: "+reason;ready=false;terminal=true;needsRetry=false;CloseTransport();if(Probe)Finish(false,reason);}
 void Finish(bool ok,string detail){if(stopping)return;Debug.Log((ok?"PVP_NETWORK_PLAYER_OK ":"PVP_NETWORK_PLAYER_FAIL ")+(host?"host ":"client ")+detail);stopping=true;CloseTransport();Application.Quit(ok?0:1);}
 public void Stop(){if(stopping||leaving)return;if(!host&&manager!=null&&manager.IsConnectedClient&&welcomeReady){leaving=true;ready=false;leaveDeadline=Time.realtimeSinceStartup+2;Status="나가기 확인 중";Send("leave",0,new PvpControl{Epoch=epoch});}else Exit();}
 void Exit(){if(stopping)return;stopping=true;credential=null;Cleanup();if(practice!=null)practice.ExitPractice();Destroy(this);}
 void CloseTransport(){if(manager!=null){manager.OnClientConnectedCallback-=Connected;manager.OnClientDisconnectCallback-=Disconnected;manager.Shutdown();manager=null;}if(networkRoot!=null){Destroy(networkRoot);networkRoot=null;}}
 void OnDestroy(){Cleanup();}
 void Cleanup(){CloseTransport();if(competition!=null)Destroy(competition);credential=null;OfflineTestInput=null;foreach(var v in visuals.Values)if(v.Object!=null)Destroy(v.Object);visuals.Clear();if(remoteMaterial!=null)Destroy(remoteMaterial);if(offlineBot)Application.runInBackground=previousBackground;}
 void OnGUI(){
  if(GetComponent<Pvp.UI.PvpRaceHud>()!=null)return;
  GUILayout.BeginArea(new Rect(16,16,Mathf.Min(650,Screen.width*.52f),285),GUI.skin.box);GUILayout.Label(offlineBot?"PVP 혼자 경기 · 봇 상대":competition!=null&&competition.HasRound?"PVP 직접 IP 경기 시험":"PVP 직접 IP 주행 시험 / 경기 시작 전");GUILayout.Label(Status??"준비 중");GUILayout.Label("W 가속 · S/Space 제동 · A/D 조향 · 정차 후 Q 전후진 · Esc 나가기");GUILayout.Label(offlineBot?"인터넷 없이 한 창 / 차량A / 택시 간 무접촉":"서버50Hz / 차량A / 무접촉 / 재접속 유예 "+ReconnectGraceSeconds+"초 (시험값)");if(offlineBot)GUILayout.Label("봇: "+BotStatus);
  if(competition==null||!competition.HasRound){int index=host&&peers.ContainsKey(0)?peers[0].PassengerIndex:latestAuthority.PassengerIndex;var card=PvpPassengerCatalog.At(index);GUILayout.Space(8);GUILayout.Label("승객 콘텐츠 미리보기 · 게임 효과 미적용");GUILayout.Label(card.TypeLabel+" / "+card.VariantLabel);GUILayout.Label(card.Dialogue);
  GUI.enabled=ready;if(GUILayout.Button("다음 승객 변주"))SelectPreview((index+1)%PvpPassengerCatalog.Count);GUI.enabled=true;}
  if(!host&&!ready&&!string.IsNullOrEmpty(credential)&&!leaving&&GUILayout.Button("재접속"))RequestReconnect(1);
  if(offlineBot)GUILayout.Label("보행자 앞을 막았다면 정차 후 Q로 후진을 선택해 공간을 양보하세요.");if(GUILayout.Button(offlineBot?"초기 화면으로":"나가기 · 재접속 자격 종료"))Stop();GUILayout.EndArea();
 }
}
}
