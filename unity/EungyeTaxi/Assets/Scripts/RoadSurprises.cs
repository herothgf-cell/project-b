using System.Collections.Generic;
using UnityEngine;

// Road surprises, first batch (design 08 ch.5.6 item 1.2, Eungye single, 2026-11-24): a parked car whose driver's
// door opens into the taxi's lane, and an ordinary car that cuts in from the next lane. Every surprise is announced
// (hazard lamps, cabin light and a door gap; an indicator before the cut-in) and a contact is judged by
// AccidentFault (FaultActor): within the reaction time and not over the limit = the other side's fault.
public static class SurpriseRules {
 public const int None=0,Door=1,CutIn=2;
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 static int I(string id,int fallback){return ContentTuning.Int(id,fallback);}
 public static bool Enabled {get{return I("surprise.enabled",1)!=0;}}
 public static int PerShiftMax {get{return Mathf.Max(0,I("surprise.perShiftMax",2));}}
 public static bool FirstDay {get{return I("surprise.firstDay",0)!=0;}}
 public static float MinGapSeconds {get{return Mathf.Max(10,F("surprise.minGapSeconds",150));}}
 public static bool DoorEnabled {get{return I("surprise.door.enabled",1)!=0;}}
 public static float DoorTriggerMetres {get{return Mathf.Clamp(F("surprise.door.triggerMetres",18),6,60);}}
 public static float DoorOpenSeconds {get{return Mathf.Clamp(F("surprise.door.openSeconds",4),1,20);}}
 public const float DoorTellSeconds=.8f;
 public static bool CutInEnabled {get{return I("surprise.cutIn.enabled",1)!=0;}}
 public static float CutInSignalSeconds {get{return Mathf.Clamp(F("surprise.cutIn.signalSeconds",.6),.2f,3);}}
 public static float CutInMergeSeconds {get{return Mathf.Clamp(F("surprise.cutIn.mergeSeconds",1.8),.8f,5);}}
 // Shift windows in ledger seconds: none in the first 3 minutes or the last 2.
 public const double QuietStart=180,QuietEnd=120;
 public static string Label(int kind){return kind==Door?"문 여는 정차 차량":kind==CutIn?"끼어드는 차":"";}

 // Why no surprise may start now; "" = allowed. Pure, checked by the editor rules.
 public static string Blocked(bool running,bool evening,bool accident,int day,double elapsed,double secondsLeft,int done,int kindDone,int mission,bool carrying){
  if(!Enabled)return "꺼짐";
  if(!running)return "영업 중 아님";
  if(evening)return "퇴근 후 드라이브";
  if(accident)return "사고 처리 중";
  if(day<=1&&!FirstDay)return "첫날";
  if(elapsed<QuietStart)return "영업 시작 직후";
  if(secondsLeft<QuietEnd)return "마감 직전";
  if(done>=PerShiftMax)return "하루 한도";
  if(kindDone>0)return "같은 종류 한도";
  // A hard stop would fail these missions through no fault of the driver.
  if(carrying&&(mission==MissionRules.Express||mission==MissionRules.Coffee))return "급행·커피 미션 중";
  return "";
 }
 // Door: the gap shows DoorTellSeconds before the door swings, which starts at the trigger distance.
 public static float DoorTellDistance(float taxiSpeed){return DoorTriggerMetres+Mathf.Max(0,taxiSpeed)*DoorTellSeconds;}
 // Cut-in candidate relative to the taxi: ahead 10..22 m (centres), one lane over, same direction,
 // taxi at 25..55 km/h, the other car not faster than the taxi by more than 1 m/s.
 public static bool CutInCandidate(float along,float lateral,float headingDot,float taxiKmh,float carSpeed,float taxiSpeed){
  return taxiKmh>=25&&taxiKmh<=55&&along>=10&&along<=22&&Mathf.Abs(lateral)>=2.6f&&Mathf.Abs(lateral)<=4.6f&&headingDot>.96f&&carSpeed>3&&carSpeed<=taxiSpeed+1;
 }
}

// Records a touch by the player's taxi on a surprise body (kinematic rigidbody root).
public sealed class SurpriseContact : MonoBehaviour {
 public Rigidbody taxi;public bool touched;
 void OnCollisionEnter(Collision c){if(taxi&&c.rigidbody==taxi)touched=true;}
 void OnCollisionStay(Collision c){if(taxi&&c.rigidbody==taxi)touched=true;}
}

public sealed class RoadSurprises : MonoBehaviour {
 public DriveSession session;
 bool automatic;int day=-1;readonly int[] kindDone=new int[3];int done;float nextTry;
 public int Started,Avoided,Touched,Missed;public string CutInReject="";public string LastOutcome="",LastBlocked="";public int LastKind;
 // Active door surprise.
 sealed class DoorEvent {public GameObject car;public Transform hinge;public FaultActor actor;public SurpriseContact contact;public Vector3 position,direction;public float spawned,ajarAt=-1,openAt=-1,endAt=-1;public bool counted;public Material hazardOn,hazardOff;public Renderer[] hazards;}
 sealed class CutInEvent {public TrafficCar car;public FaultActor actor;public SurpriseContact contact;public int side;public float signalAt,moveAt=-1,endAt=-1;}
 DoorEvent door;CutInEvent cut;
 public bool Busy {get{return door!=null||cut!=null;}}
 public bool DoorOpen {get{return door!=null&&door.openAt>=0;}}
 public FaultActor DoorActor {get{return door==null?null:door.actor;}}
 public GameObject DoorCar {get{return door==null?null:door.car;}}
 public TrafficCar CutInCar {get{return cut==null?null:cut.car;}}
 public bool CutInMoving {get{return cut!=null&&cut.moveAt>=0;}}

 public void Initialize(DriveSession owner){
  session=owner;automatic=true;
  // Acceptance runs place their own scenes; a probe calls the Try methods itself.
  foreach(var arg in System.Environment.GetCommandLineArgs())if(arg.EndsWith("Test")||arg.EndsWith("Check")||arg=="-assistedTrial"||arg=="-roadRulesTrial")automatic=false;
  if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-surpriseAuto")>=0)automatic=true;
  nextTry=Time.time+30;
 }
 bool Single {get{return session&&session.traffic&&session.traffic.EungyeSingle&&!session.simulationMatch&&!session.IsSimulationBot&&!PvpNativeLan.IsReplica(session)&&session.world.map.hybrid&&!PracticeSession.Enabled&&!RoadMapRuntime.Trial;}}
 bool Carrying {get{return session.jobs&&session.jobs.trip.State==TripState.OnTrip&&!session.jobs.trip.ArrivalLocked;}}
 public string BlockedNow(int kind){
  if(!Single)return "은계 싱글 아님";
  var b=session.business;var l=b?b.ledger:null;if(l==null)return "영업 정보 없음";
  if(l.Day!=day){day=l.Day;done=0;System.Array.Clear(kindDone,0,kindDone.Length);}
  if(kind==SurpriseRules.Door&&!SurpriseRules.DoorEnabled||kind==SurpriseRules.CutIn&&!SurpriseRules.CutInEnabled)return "종류 꺼짐";
  int mission=session.jobs&&session.jobs.current!=null?session.jobs.current.mission:0;
  return SurpriseRules.Blocked(b.DrowsinessRunning,b.EveningDrive,session.accident&&session.accident.Locked,l.Day,l.Elapsed,l.SecondsLeft,done,kindDone[kind],mission,Carrying);
 }
 void Update(){
  if(!session||!session.taxi)return;
  if(door!=null)TickDoor();
  if(cut!=null)TickCutIn();
  if(!automatic||Busy||Time.time<nextTry)return;
  nextTry=Time.time+1;
  int first=Random.value<.5f?SurpriseRules.Door:SurpriseRules.CutIn;
  for(int n=0;n<2;n++){int kind=n==0?first:3-first;string why=BlockedNow(kind);LastBlocked=why;if(why!="")continue;
   if(kind==SurpriseRules.Door?TryDoor():TryCutIn()){nextTry=Time.time+SurpriseRules.MinGapSeconds*Random.Range(1f,1.6f);return;}}
 }
 void Count(int kind){done++;kindDone[kind]++;Started++;LastKind=kind;}
 void Card(string text){var notices=session.GetComponent<DriveNotifications>();if(notices)notices.Card(text);}
 void Finish(string outcome,bool avoided){LastOutcome=outcome;if(avoided)Avoided++;else Touched++;Debug.Log("ROAD_SURPRISE kind="+LastKind+" outcome="+outcome);}

 // ---- Door ----
 // The taxi's lane ahead: the guided route when the taxi is on it, otherwise the straight-ahead chain.
 HdRoadGraph.RoutePath LaneAhead(out float at){
  var nav=session.jobs.nav;var graph=nav.hd;at=0;if(graph==null)return null;
  if(nav.hdRoute!=null){float error;float a=nav.ProjectVehicle(out error);if(error<2.5f){at=a;return nav.hdRoute;}}
  float edgeAt;int edge=graph.Nearest(session.taxi.transform.position,session.taxi.transform.forward,out edgeAt);if(edge<0)return null;
  var forward=graph.Forward(edge,edgeAt);at=0;return forward;
 }
 bool OrdinaryEdge(HdEdge e){return e.change==0&&e.turn==0&&!e.TraversalDisabled&&!e.id.StartsWith("join:")&&e.id.IndexOf("parking",System.StringComparison.OrdinalIgnoreCase)<0&&e.id.IndexOf("service",System.StringComparison.OrdinalIgnoreCase)<0;}
 // A kerb-side spot where a parked car fits on the paved road outside the lane, on a straight stretch away from
 // junctions and stops, with no other lane to the right and nothing standing there.
 public bool FindDoorSpot(float minAhead,float maxAhead,out Vector3 position,out Vector3 direction){
  position=direction=Vector3.zero;float at;var route=LaneAhead(out at);if(route==null)return false;var graph=session.jobs.nav.hd;var map=session.world.map;
  for(float ahead=minAhead;ahead<=maxAhead;ahead+=5){float d=at+ahead;if(d>route.path.length-10)break;
   int edge=route.EdgeAt(d);if(!OrdinaryEdge(graph.edges[edge]))continue;
   var lane=route.path.Position(d);var dir=route.path.Tangent(d);dir.y=0;if(dir.sqrMagnitude<.5f)continue;dir.Normalize();
   if(Vector3.Angle(route.path.Tangent(Mathf.Max(0,d-10)),route.path.Tangent(Mathf.Min(route.path.length,d+10)))>6)continue;
   var right=TrafficSystem.Right(dir);var p=lane+right*2.35f;p.y=lane.y;
   bool clear=true;
   foreach(var j in map.targets)if(Vector3.Distance(p,j.V())<35){clear=false;break;}
   if(clear&&map.junctionRules!=null)foreach(var r in map.junctionRules)if(Vector3.Distance(p,r.V())<35){clear=false;break;}
   if(clear)foreach(var s in map.stops)if(Vector3.Distance(p,s.Kerb)<20){clear=false;break;}
   if(!clear)continue;
   foreach(float x in new[]{-.95f,.95f})foreach(float z in new[]{-2.2f,0,2.2f})if(!session.roadEnvironment.OnRoad(p+right*x+dir*z,-.1f))clear=false;
   if(!clear)continue;
   // Another lane centre on this side means the car would stand in traffic, not at the kerb.
   float other;int nearest=graph.Nearest(p,dir,out other);if(nearest>=0&&nearest!=edge){float err;graph.paths[nearest].Project(p,out err);if(err<2.2f)continue;}
   if(Physics.CheckBox(p+Vector3.up*.9f,new Vector3(1.6f,.6f,3.2f),Quaternion.LookRotation(dir),(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore))continue;
   position=p;direction=dir;return true;
  }
  return false;
 }
 public bool TryDoor(bool force=false){
  if(Busy||!session.world.map.hybrid)return false;if(!force&&BlockedNow(SurpriseRules.Door)!="")return false;
  float kmh=session.taxi.Speed*3.6f;if(!force&&(kmh<20||kmh>60))return false;
  Vector3 p,dir;if(!FindDoorSpot(force?30:55,force?90:95,out p,out dir))return false;
  SpawnDoor(p,dir);return true;
 }
 public void SpawnDoor(Vector3 p,Vector3 dir){
  var e=new DoorEvent{position=p,direction=dir,spawned=Time.time};
  var car=new GameObject("돌발 · 정차 차량");car.transform.SetParent(transform);car.transform.SetPositionAndRotation(p,Quaternion.LookRotation(dir));
  var body=car.AddComponent<Rigidbody>();body.isKinematic=true;
  var box=car.AddComponent<BoxCollider>();box.center=new Vector3(0,.8f,0);box.size=new Vector3(1.8f,1.25f,4.2f);
  car.AddComponent<StagedVehicle>();
  var paint=WorldBuilder.Mat("돌발 정차 차량",new Color(.82f,.82f,.8f));var glass=WorldBuilder.Mat("돌발 정차 유리",new Color(.08f,.16f,.2f));var tyre=WorldBuilder.Mat("AI tyres",new Color(.035f,.04f,.05f));
  WorldBuilder.Box("차체",new Vector3(0,.65f,0),new Vector3(1.8f,.65f,4.2f),paint,car.transform,false);
  WorldBuilder.Box("창문",new Vector3(0,1.2f,-.2f),new Vector3(1.5f,.62f,2),glass,car.transform,false);
  WorldBuilder.Box("지붕",new Vector3(0,1.55f,-.2f),new Vector3(1.55f,.1f,2.1f),paint,car.transform,false);
  for(int i=0;i<4;i++){var w=GameObject.CreatePrimitive(PrimitiveType.Cylinder);w.transform.SetParent(car.transform,false);w.transform.localPosition=new Vector3(i%2==0?-.87f:.87f,.34f,i<2?1.3f:-1.3f);w.transform.localRotation=Quaternion.Euler(0,0,90);w.transform.localScale=new Vector3(.68f,.12f,.68f);w.GetComponent<Renderer>().sharedMaterial=tyre;Destroy(w.GetComponent<Collider>());}
  // Cabin light: someone is inside and about to get out.
  var cabin=WorldBuilder.Mat("돌발 실내등",new Color(1,.88f,.55f));cabin.EnableKeyword("_EMISSION");cabin.SetColor("_EmissionColor",new Color(1,.85f,.5f)*1.6f);
  WorldBuilder.Box("실내등",new Vector3(0,1.47f,.2f),new Vector3(.5f,.05f,.3f),cabin,car.transform,false);
  if(session.traffic)session.traffic.RegisterNightReadable(cabin,new Color(1,.9f,.6f));
  e.hazardOn=WorldBuilder.Mat("돌발 비상등 켜짐",new Color(1,.55f,.02f));e.hazardOn.EnableKeyword("_EMISSION");e.hazardOn.SetColor("_EmissionColor",new Color(1,.45f,0)*2.2f);e.hazardOff=WorldBuilder.Mat("돌발 비상등 꺼짐",new Color(.28f,.12f,0));
  if(session.traffic)session.traffic.RegisterNightReadable(e.hazardOn,new Color(1,.6f,.08f));
  var lamps=new List<Renderer>();foreach(int side in new[]{-1,1})foreach(float z in new[]{2.12f,-2.12f})lamps.Add(WorldBuilder.Box("비상등",new Vector3(side*.78f,.8f,z),new Vector3(.18f,.15f,.05f),e.hazardOff,car.transform,false).GetComponent<Renderer>());
  e.hazards=lamps.ToArray();
  // Driver's door on the left (traffic) side, hinged at its front edge; closed it lies along the body.
  var hinge=new GameObject("운전석 문 경첩").transform;hinge.SetParent(car.transform,false);hinge.localPosition=new Vector3(-.92f,0,.75f);
  // Solid: the primitive keeps its box collider (part of the parked car's kinematic body).
  WorldBuilder.Box("운전석 문",new Vector3(-.03f,.85f,-.55f),new Vector3(.07f,.95f,1.1f),paint,hinge,true);
  var doorGlass=WorldBuilder.Box("문 유리",new Vector3(-.03f,1.38f,-.6f),new Vector3(.06f,.4f,.9f),glass,hinge,false);var dg=doorGlass.GetComponent<Collider>();if(dg)Destroy(dg);
  e.actor=hinge.gameObject.AddComponent<FaultActor>();e.actor.startedAt=float.MaxValue;
  foreach(var child in car.GetComponentsInChildren<Transform>())child.gameObject.layer=9;
  e.contact=car.AddComponent<SurpriseContact>();e.contact.taxi=session.taxi.body;
  e.car=car;e.hinge=hinge;door=e;Physics.SyncTransforms();
  Debug.Log("ROAD_SURPRISE door spawned at "+p.ToString("F1"));
 }
 void TickDoor(){
  var e=door;if(!e.car){door=null;return;}
  bool lit=Mathf.Repeat(Time.time,.8f)<.4f;foreach(var r in e.hazards)if(r)r.sharedMaterial=lit?e.hazardOn:e.hazardOff;
  var taxi=session.taxi.transform.position;var rel=e.position-taxi;rel.y=0;float along=Vector3.Dot(rel,e.direction);float lateral=Mathf.Abs(Vector3.Dot(rel,TrafficSystem.Right(e.direction)));
  float speed=session.taxi.Speed;bool approaching=lateral<6&&Vector3.Dot(session.taxi.transform.forward,e.direction)>.7f;
  float angle=0;
  if(e.openAt<0){
   bool near=approaching&&along>0;
   // The gap shows first; the door swings DoorTellSeconds later, or at the trigger distance after at least .3 s.
   if(near&&e.ajarAt<0&&along<=SurpriseRules.DoorTellDistance(speed)+2)e.ajarAt=Time.time;
   float ajar=e.ajarAt<0?-1:Time.time-e.ajarAt;
   if(near&&ajar>=0&&(ajar>=SurpriseRules.DoorTellSeconds||ajar>=.3f&&along<=SurpriseRules.DoorTriggerMetres+2)){
    e.openAt=Time.time;e.actor.Begin();if(!e.counted){e.counted=true;Count(SurpriseRules.Door);}
    Debug.Log("ROAD_SURPRISE door opens along="+along.ToString("0.0")+" kmh="+(speed*3.6f).ToString("0"));}
   if(e.ajarAt>=0)angle=12;
  }
  if(e.openAt>=0){float t=Time.time-e.openAt,hold=SurpriseRules.DoorOpenSeconds;
   angle=t<.45f?Mathf.Lerp(12,70,t/.45f):t<.45f+hold?70:Mathf.Lerp(70,0,(t-.45f-hold)/.8f);
   if(e.endAt<0){
    if(e.contact.touched){e.endAt=Time.time;Finish("접촉",false);}
    else if(along<-3.5f){e.endAt=Time.time;Finish("피함",true);Card("돌발 · 문 열림을 피했어요"+(Carrying?" · “어머, 깜짝이야. 잘 피하셨어요.”":""));}
    else if(t>hold+12){e.endAt=Time.time;Finish("멈춰 기다림",true);}
   }
  }
  e.hinge.localRotation=Quaternion.Euler(0,angle,0); // positive yaw swings the rear edge out to the left (traffic side)
  // Nobody came: the taxi turned off or stopped far away.
  if(e.openAt<0&&(Time.time-e.spawned>45||along<-3||along>140||lateral>12)){Missed++;LastOutcome="지나치지 않음";Remove();nextTry=Time.time+20;return;}
  if(e.endAt>=0&&Time.time-e.endAt>3&&(along<-20||along>60||Time.time-e.endAt>20))Remove();
 }
 void Remove(){if(door!=null&&door.car){door.car.SetActive(false);Destroy(door.car);}door=null;}

 // ---- Cut-in ----
 public bool TryCutIn(bool force=false){
  if(Busy||session.traffic==null){CutInReject="다른 돌발 진행 중";return false;}if(!force&&BlockedNow(SurpriseRules.CutIn)!=""){CutInReject=BlockedNow(SurpriseRules.CutIn);return false;}
  var taxi=session.taxi;var graph=session.jobs.nav.hd;if(graph==null)return false;
  float taxiAt;int taxiEdge=graph.Nearest(taxi.transform.position,taxi.transform.forward,out taxiAt);if(taxiEdge<0||!OrdinaryEdge(graph.edges[taxiEdge])){CutInReject="택시가 일반 차로에 있지 않음";return false;}
  var fwd=taxi.transform.forward;fwd.y=0;fwd.Normalize();var right=TrafficSystem.Right(fwd);float taxiSpeed=taxi.Speed;
  TrafficCar best=null;float bestScore=float.MaxValue;CutInReject="후보 없음";
  foreach(var c in session.traffic.cars){
   if(!c||!c.Visible||!c.enabled||c.district==null||c.CuttingIn||c.GetComponent<FaultActor>())continue;
   var rel=c.transform.position-taxi.transform.position;rel.y=0;float along=Vector3.Dot(rel,fwd),lateral=Vector3.Dot(rel,right);
   if(!SurpriseRules.CutInCandidate(along,lateral,Vector3.Dot(c.transform.forward,fwd),force?Mathf.Clamp(taxiSpeed*3.6f,25,55):taxiSpeed*3.6f,c.speed,force?Mathf.Max(taxiSpeed,c.speed):taxiSpeed)){if(Mathf.Abs(along)<30&&Mathf.Abs(lateral)<8)CutInReject="위치·속도 along="+along.ToString("0.0")+" lateral="+lateral.ToString("0.0")+" speed="+c.speed.ToString("0.0");continue;}
   if(c.laneYielding||c.waitingForJunction||c.waitingForSignal){CutInReject="교차로·차로 변경 대기 중";continue;}
   if(c.route.graphPath==null||!OrdinaryEdge(graph.edges[c.route.graphPath.EdgeAt(c.distance)])){CutInReject="회전·차로 변경 구간";continue;}
   bool junction=false;foreach(var stop in c.route.stops)if(c.distance<=stop.exitDistance&&stop.distance-c.distance<40)junction=true;if(junction){CutInReject="앞 40m 안 교차로";continue;}
   float score=Mathf.Abs(along-14);if(score<bestScore){bestScore=score;best=c;}
  }
  if(!best)return false;
  var rel2=best.transform.position-taxi.transform.position;int side=Vector3.Dot(rel2,right)>0?-1:1; // moves toward the taxi's lane
  cut=new CutInEvent{car=best,side=side,signalAt=Time.time};best.scriptedTurn=side;
  Debug.Log("ROAD_SURPRISE cut-in signal car="+best.name);
  return true;
 }
 // The car's new route: from beside it on the taxi's lane, toward its own goal.
 TrafficRoute CutInRoute(TrafficCar c){
  var graph=session.jobs.nav.hd;var taxi=session.taxi;float taxiAt;int lane=graph.Nearest(taxi.transform.position,taxi.transform.forward,out taxiAt);if(lane<0||!OrdinaryEdge(graph.edges[lane]))return null;
  float error;float at=graph.paths[lane].Project(c.transform.position,out error);if(error<2.4f||error>4.8f||at<taxiAt+6||at>graph.paths[lane].length-20)return null;
  var route=c.district.CreateRoute(lane,at,c.route.goalEdge,c.route.goalAt);
  if(route==null)return null;foreach(var stop in route.stops)if(stop.distance<30)return null;
  // Nothing else on the stretch it moves into.
  var p=route.Position(Mathf.Min(route.length,6));var dir=route.Tangent(6);
  foreach(var h in Physics.OverlapBox(p+Vector3.up*.85f,new Vector3(1.1f,.55f,9),Quaternion.LookRotation(dir),(1<<8)|(1<<9),QueryTriggerInteraction.Ignore)){if(h.attachedRigidbody==taxi.body||h.GetComponentInParent<TrafficCar>()==c)continue;return null;}
  return route;
 }
 void TickCutIn(){
  var e=cut;var c=e.car;if(!c||!c.Visible){EndCut(false);return;}
  if(e.moveAt<0){
   if(Time.time-e.signalAt<SurpriseRules.CutInSignalSeconds)return;
   var route=CutInRoute(c);
   if(route==null){Debug.Log("ROAD_SURPRISE cut-in aborted (no room)");c.scriptedTurn=0;cut=null;Missed++;LastOutcome="끼어들 자리 없음";nextTry=Time.time+15;return;}
   c.CutInto(route,SurpriseRules.CutInMergeSeconds);e.moveAt=Time.time;
   e.actor=c.gameObject.AddComponent<FaultActor>();e.actor.Begin();e.contact=c.gameObject.AddComponent<SurpriseContact>();e.contact.taxi=session.taxi.body;
   Count(SurpriseRules.CutIn);Debug.Log("ROAD_SURPRISE cut-in moves car="+c.name);return;
  }
  float since=Time.time-e.moveAt;
  if(since>SurpriseRules.CutInMergeSeconds+.4f)c.scriptedTurn=0;
  if(e.endAt<0){
   if(e.contact.touched){e.endAt=Time.time;Finish("접촉",false);}
   else if(since>SurpriseRules.CutInMergeSeconds+3){e.endAt=Time.time;Finish("간격 줌",true);Card("돌발 · 끼어든 차에 간격을 줬어요"+(Carrying?" · “저 차 너무하네요. 침착하시네요.”":""));}
  }
  if(e.endAt>=0&&Time.time-e.endAt>3)EndCut(true);
 }
 void EndCut(bool keep){var e=cut;cut=null;if(e==null)return;if(e.car){e.car.scriptedTurn=0;if(e.actor)Destroy(e.actor);if(e.contact)Destroy(e.contact);}}

 public void ResetForDay(){Remove();EndCut(false);day=-1;nextTry=Time.time+30;}
 void OnDestroy(){Remove();}
}
