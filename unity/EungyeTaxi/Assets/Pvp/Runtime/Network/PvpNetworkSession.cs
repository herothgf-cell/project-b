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
namespace Pvp.Network {
// Account-free direct-IP experiment. No relay, match, score, reconnect identity or host migration.
public sealed class PvpNetworkSession:MonoBehaviour {
 sealed class Peer {public PvpNetworkMotor Motor;public PvpInputBuffer Inputs=new PvpInputBuffer();public int Ack;public Vector3 Start;public Peer(PvpMapRuntime map,int slot){Motor=new PvpNetworkMotor(map,slot);Start=Motor.Capture().State.Position;}}
 NetworkManager manager;GameObject networkRoot;PvpPracticeSession practice;PvpMapRuntime map;PvpPrediction prediction;
 readonly Dictionary<ulong,Peer> peers=new Dictionary<ulong,Peer>();readonly Dictionary<ulong,GameObject> visuals=new Dictionary<ulong,GameObject>();
 Material remoteMaterial;bool host,ready,reverse,stopping;int tick,samples;float started,connectedAt=-1,lastSnapshot,accumulator;
 PvpDriveSnapshot latestAuthority;Vector3 ownStart;bool gotOwnStart;float ownDistance;
 public bool Probe; public float ProbeSeconds=14;public string ExpectedRejection;
 public string Status {get;private set;} public float RemoteDistance {get;private set;}public int Samples {get{return samples;}}
 public void Begin(bool asHost,string address,ushort port,bool badMap=false){
  IPAddress parsed;if(!IPAddress.TryParse(address,out parsed)||parsed.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork||port==0)throw new ArgumentException("IPv4 address and port required");
  if(NetworkManager.Singleton!=null)throw new InvalidOperationException("Network manager already active");
  host=asHost;Application.runInBackground=true;started=Time.realtimeSinceStartup;Status="접속 중";
  var bootstrap=GetComponent<PvpBootstrap>();bootstrap.StartPractice();practice=GetComponent<PvpPracticeSession>();if(practice==null)throw new InvalidOperationException(bootstrap.StartupError);practice.SetScripted(true);map=practice.Map;
  networkRoot=new GameObject("PVP direct IP transport");manager=networkRoot.AddComponent<NetworkManager>();var transport=networkRoot.AddComponent<UnityTransport>();
  transport.SetConnectionData(address,port,asHost?address:null);
  manager.NetworkConfig=new NetworkConfig{NetworkTransport=transport,EnableSceneManagement=false,ConnectionApproval=true,TickRate=50,ForceSamePrefabs=false};
  manager.NetworkConfig.ConnectionData=Encoding.UTF8.GetBytes(JsonUtility.ToJson(PvpAdmission.Hello(badMap?"wrong-map":map.MapHash)));
  manager.ConnectionApprovalCallback=Approve;manager.OnClientConnectedCallback+=Connected;manager.OnClientDisconnectCallback+=Disconnected;
  bool result=host?manager.StartHost():manager.StartClient();if(!result)throw new InvalidOperationException("Transport could not start");
  manager.CustomMessagingManager.RegisterNamedMessageHandler("drive",ReceiveInput);manager.CustomMessagingManager.RegisterNamedMessageHandler("state",ReceiveState);
  if(host&&!peers.ContainsKey(0))Connected(0);
  remoteMaterial=new Material(Shader.Find("Standard")){color=new Color(.2f,.6f,1)};
  Debug.Log("PVP_NETWORK_STARTED role="+(host?"host":"client")+" SDK=NGO1.12.0 protocol="+PvpAdmission.Protocol+" map="+map.MapHash);
 }
 void Approve(NetworkManager.ConnectionApprovalRequest req,NetworkManager.ConnectionApprovalResponse res){
  string reason=null;try{if(req.Payload==null||req.Payload.Length>1024)reason="invalid handshake";else reason=PvpAdmission.Reject(JsonUtility.FromJson<PvpJoinHello>(Encoding.UTF8.GetString(req.Payload)),map.MapHash);}catch{reason="invalid handshake";}
  if(reason==null&&manager.ConnectedClientsIds.Count>=2)reason="trial full";res.Approved=reason==null;res.CreatePlayerObject=false;res.Pending=false;res.Reason=reason??"";
  if(reason!=null)Debug.Log("PVP_ADMISSION_REJECT "+reason);
 }
 void Connected(ulong id){
  if(host){if(!peers.ContainsKey(id))peers[id]=new Peer(map,id==0?0:1);if(id!=0){connectedAt=Time.realtimeSinceStartup;Debug.Log("PVP_PEER_JOINED");}ready=true;Status="호스트 / 직접 IP 주행";}
  else if(manager.IsConnectedClient){Status="연결됨 / 권한 상태 수신 대기";connectedAt=Time.realtimeSinceStartup;}
 }
 void Disconnected(ulong id){
  if(stopping)return;
  if(host){peers.Remove(id);if(visuals.TryGetValue(id,out var visual)){Destroy(visual);visuals.Remove(id);}Status="상대 연결 종료";}
  else {ready=false;Status="연결 종료: "+manager.DisconnectReason;Debug.Log("PVP_NETWORK_DISCONNECTED "+manager.DisconnectReason);if(Probe){bool expected=!string.IsNullOrEmpty(ExpectedRejection)&&manager.DisconnectReason.Contains(ExpectedRejection);Finish(expected,"admission/disconnect "+manager.DisconnectReason);}}
 }
 void ReceiveInput(ulong sender,FastBufferReader reader){
  if(!host||sender==0||!peers.TryGetValue(sender,out var peer))return;
  try{if(reader.Length>1024)throw new ArgumentException("oversized input");reader.ReadValueSafe(out string json);var cmd=JsonUtility.FromJson<PvpDriveCommand>(json);peer.Inputs.Enqueue(cmd);}
  catch{manager.DisconnectClient(sender,"invalid drive input");}
 }
 void ReceiveState(ulong sender,FastBufferReader reader){
  if(host||sender!=NetworkManager.ServerClientId)return;
  try{if(reader.Length>2048)throw new ArgumentException("oversized state");reader.ReadValueSafe(out string json);var state=JsonUtility.FromJson<PvpDriveSnapshot>(json);
   if(state.Owner==manager.LocalClientId){if(prediction==null)prediction=new PvpPrediction(map,1);prediction.Reconcile(state);latestAuthority=state;if(!gotOwnStart){ownStart=state.State.Position;gotOwnStart=true;}ownDistance=Mathf.Max(ownDistance,Vector3.Distance(ownStart,state.State.Position));ready=true;lastSnapshot=Time.realtimeSinceStartup;samples++;practice.PresentNetworkState(prediction.State.State);Status="클라이언트 / 입력 예측·권한 보정";}
   else PresentRemote(state);
  }catch(Exception e){Fail(e.Message);}
 }
 void Send<T>(string name,ulong target,T value){string json=JsonUtility.ToJson(value);using(var writer=new FastBufferWriter(4096,Allocator.Temp)){writer.WriteValueSafe(json);manager.CustomMessagingManager.SendNamedMessage(name,target,writer,NetworkDelivery.ReliableSequenced);}}
 void PresentRemote(PvpDriveSnapshot s){
  if(!visuals.TryGetValue(s.Owner,out var visual)){visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.name="PVP remote authority vehicle";visual.transform.SetParent(transform,false);visual.transform.localScale=new Vector3(1.8f,1.3f,3.6f);visual.GetComponent<Renderer>().sharedMaterial=remoteMaterial;Destroy(visual.GetComponent<Collider>());visuals[s.Owner]=visual;}
  visual.transform.SetPositionAndRotation(s.State.Position+Vector3.up*.75f,Quaternion.Euler(0,s.State.YawDegrees,0));
 }
 PvpVehicleInput InputNow(){
  if(Probe){float age=connectedAt<0?0:Time.realtimeSinceStartup-connectedAt;return new PvpVehicleInput(age>0&&age<2?1:0,age>=2?1:0,0);}
  return new PvpVehicleInput(Input.GetKey(KeyCode.W)?1:0,Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.Space)?1:0,(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),reverse,Application.isFocused);
 }
 void Update(){
  if(manager==null||stopping)return;
  if(!Probe&&Input.GetKeyDown(KeyCode.Escape)){Stop();return;}
  if(!Probe&&ready&&Input.GetKeyDown(KeyCode.Q)&&Mathf.Abs(practice.Capture().Speed)<.35f)reverse=!reverse;
  try{
   accumulator+=Time.unscaledDeltaTime;int steps=0;while(accumulator>=PvpNetworkMotor.Delta&&steps++<5){accumulator-=PvpNetworkMotor.Delta;Advance();}if(steps>5)accumulator=0;
   float now=Time.realtimeSinceStartup;
   if(!host&&ready&&now-lastSnapshot>3)throw new InvalidOperationException("authority timeout");
   if(!ready&&now-started>12)throw new InvalidOperationException("connection timeout");
   if(Probe&&now-started>ProbeSeconds){float positionError=prediction==null?0:Vector3.Distance(prediction.State.State.Position,latestAuthority.State.Position);
    float yawError=prediction==null?0:Mathf.Abs(Mathf.DeltaAngle(prediction.State.State.YawDegrees,latestAuthority.State.YawDegrees));
    float speedError=prediction==null?0:Mathf.Abs(prediction.State.State.Speed-latestAuthority.State.Speed);
    bool ok=string.IsNullOrEmpty(ExpectedRejection)&&(host?RemoteDistance>10:samples>20&&prediction!=null&&prediction.PendingCount<30&&ownDistance>10&&positionError<.05f&&yawError<.1f&&speedError<.05f&&Mathf.Abs(latestAuthority.State.Speed)<.05f);
    Finish(ok,"samples="+samples+" pending="+(prediction==null?0:prediction.PendingCount)+" remoteDistance="+RemoteDistance+" ownDistance="+ownDistance+" positionError="+positionError+" yawError="+yawError+" speedError="+speedError+" maxCorrection="+(prediction==null?0:prediction.MaxCorrection));}
  }catch(Exception e){Fail(e.Message);}
 }
 void Advance(){
  if(!ready)return;tick++;
  if(host){
   foreach(var pair in peers){var peer=pair.Value;PvpVehicleInput input;if(pair.Key==0)input=InputNow();else input=peer.Inputs.InputForTick(out peer.Ack);peer.Motor.Step(input);
    var state=peer.Motor.Capture();state.Owner=pair.Key;state.Ack=peer.Ack;state.Tick=tick;
    if(pair.Key==0)practice.PresentNetworkState(state.State);else{RemoteDistance=Mathf.Max(RemoteDistance,Vector3.Distance(peer.Start,state.State.Position));PresentRemote(state);}
    if(tick%5==0)foreach(ulong id in manager.ConnectedClientsIds)if(id!=0)Send("state",id,state);
   }
  }else if(prediction!=null){var cmd=prediction.Predict(InputNow());Send("drive",NetworkManager.ServerClientId,cmd);practice.PresentNetworkState(prediction.State.State);}
 }
 void Fail(string cause){Status="주행 중단: "+cause;Debug.LogError("PVP_NETWORK_FAIL "+cause);if(Probe)Finish(false,cause);else Stop();}
 void Finish(bool ok,string detail){if(stopping)return;Debug.Log((ok?"PVP_NETWORK_PLAYER_OK ":"PVP_NETWORK_PLAYER_FAIL ")+(host?"host ":"client ")+detail);stopping=true;if(manager!=null)manager.Shutdown();Application.Quit(ok?0:1);}
 public void Stop(){if(stopping)return;stopping=true;Cleanup();if(practice!=null)practice.ExitPractice();Destroy(this);}
 void OnDestroy(){Cleanup();}
 void Cleanup(){if(manager!=null){manager.OnClientConnectedCallback-=Connected;manager.OnClientDisconnectCallback-=Disconnected;manager.Shutdown();}if(networkRoot!=null)Destroy(networkRoot);foreach(var v in visuals.Values)if(v!=null)Destroy(v);visuals.Clear();if(remoteMaterial!=null)Destroy(remoteMaterial);}
 void OnGUI(){GUILayout.BeginArea(new Rect(16,16,620,155),GUI.skin.box);GUILayout.Label("PVP 직접 IP 주행 시험 / 경기·승객·정산 없음");GUILayout.Label(Status??"준비 중");GUILayout.Label("W 가속 · S/Space 제동 · A/D 조향 · 정차 후 Q 전후진 · Esc 종료");GUILayout.Label("서버50Hz / 차량A / 택시 간 충돌 없음 / 재접속·복구 미구현");if(GUILayout.Button("접속 종료"))Stop();GUILayout.EndArea();}
}
}