using System.Collections.Generic;
using UnityEngine;

// Road surprises, second batch (design 08 ch.5.6 item 1.2, decided 2026-11-24, Eungye single):
// - kick scooter: on a "번길" street a rider in a bright vest rings a bell and crosses from the kerb ahead.
//   Touching the rider is a person accident; it is relieved (fee and penalty halved, no major grade) only when
//   the rider came out within the reaction time, the taxi was not over the limit and was braking.
// - ambulance: while the taxi approaches a junction on green, an ambulance crosses from the side with its siren
//   on. Ordinary cars hold at that junction; entering it before the ambulance has passed is a game-rule
//   "긴급차 양보 위반" (passenger satisfaction -8, driving score -5). No exception for touching an ambulance.
public static partial class SurpriseRules {
 static float F2(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 public static bool ScooterEnabled {get{return ContentTuning.Int("surprise.scooter.enabled",1)!=0;}}
 public static float ScooterTriggerMetres {get{return Mathf.Clamp(F2("surprise.scooter.triggerMetres",22),8,60);}}
 public static float ScooterSpeed {get{return Mathf.Clamp(F2("surprise.scooter.speedMetres",4),1.5f,8);}}
 public const float ScooterTellSeconds=1f;
 public static int ReliefPercent {get{return Mathf.Clamp(ContentTuning.Int("surprise.scooter.reliefPercent",50),0,100);}}
 public static bool AmbulanceEnabled {get{return ContentTuning.Int("surprise.ambulance.enabled",1)!=0;}}
 public static float SirenSeconds {get{return Mathf.Clamp(F2("surprise.ambulance.sirenSeconds",5),2,15);}}
 public static float AmbulanceSpeed {get{return Mathf.Clamp(F2("surprise.ambulance.speedMetres",12),5,25);}}
 public static int YieldScorePenalty {get{return Mathf.Clamp(ContentTuning.Int("surprise.ambulance.scorePenalty",5),0,50);}}
 public static int YieldSatisfactionPenalty {get{return Mathf.Clamp(ContentTuning.Int("surprise.ambulance.satisfactionPenalty",8),0,50);}}
 // Relief for a person accident with the scooter: all three conditions.
 public static bool Relieved(float sinceStart,bool aboveLimit,bool braking){return sinceStart<=AccidentFault.ReactionSeconds&&!aboveLimit&&braking;}
 // Ambulance trigger: a signalised junction 45..90 m ahead on the taxi's road, taxi's own signal green, 15+ km/h.
 public static bool AmbulanceCandidate(float ahead,float lateral,bool green,float taxiKmh){return green&&taxiKmh>=15&&ahead>=45&&ahead<=90&&Mathf.Abs(lateral)<=10;}
}

public sealed partial class RoadSurprises {
 // ---- kick scooter ----
 sealed class ScooterEvent {public GameObject rider;public Vector3 from,to,crossing,direction;public float spawned,startAt=-1,doneAt=-1;public bool touched,counted;public AudioSource bell;}
 ScooterEvent scooter;
 public GameObject ScooterRider {get{return scooter==null?null:scooter.rider;}}
 public bool ScooterMoving {get{return scooter!=null&&scooter.startAt>=0;}}
 public float ScooterStartedAt {get{return scooter==null?-1:scooter.startAt;}}
 float speedSample,speedSampleAt;float lastDecel;
 // Braking: the taxi lost 1.5 m/s or more over the last half second.
 bool TaxiBraking {get{return lastDecel>=1.5f;}}
 void SampleBraking(){if(Time.time-speedSampleAt>=.25f){float v=session.taxi.Speed;lastDecel=(speedSample-v)/Mathf.Max(.01f,Time.time-speedSampleAt)*.5f;speedSample=v;speedSampleAt=Time.time;}}
 static bool Alley(string road){return !string.IsNullOrEmpty(road)&&road.EndsWith("번길");}
 // A crossing point 30..60 m ahead on a 번길 street, clear of junctions and stops; the rider starts on the
 // right-hand kerb beyond the paved road and crosses to the far side.
 public bool FindScooterSpot(float minAhead,float maxAhead,out Vector3 from,out Vector3 to,out Vector3 crossing){
  from=to=crossing=Vector3.zero;float at;var route=LaneAhead(out at);if(route==null)return false;var graph=session.jobs.nav.hd;var map=session.world.map;
  for(float ahead=minAhead;ahead<=maxAhead;ahead+=5){float d=at+ahead;if(d>route.path.length-5)break;
   int edge=route.EdgeAt(d);var e=graph.edges[edge];if(!OrdinaryEdge(e)||!Alley(e.roadName))continue;
   var lane=route.path.Position(d);var dir=route.path.Tangent(d);dir.y=0;if(dir.sqrMagnitude<.5f)continue;dir.Normalize();var right=TrafficSystem.Right(dir);
   bool clear=true;foreach(var j in map.targets)if(Vector3.Distance(lane,j.V())<25){clear=false;break;}
   if(clear)foreach(var s in map.stops)if(Vector3.Distance(lane,s.Kerb)<15){clear=false;break;}
   if(clear&&session.parkedAlley)foreach(var c in session.parkedAlley.cars)if(Vector3.Distance(lane,c.position)<8){clear=false;break;}
   if(!clear)continue;
   float r=0;while(r<12&&session.roadEnvironment.OnRoad(lane+right*(r+.1f),0))r+=.1f;float l=0;while(l<14&&session.roadEnvironment.OnRoad(lane-right*(l+.1f),0))l+=.1f;
   if(r>=12||l>=14)continue;
   from=lane+right*(r+1.2f);to=lane-right*(l+1.2f);crossing=lane;from.y=to.y=lane.y;
   if(Physics.CheckBox(from+Vector3.up*.9f,new Vector3(.5f,.6f,.5f),Quaternion.identity,(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore))continue;
   return true;
  }
  return false;
 }
 public bool TryScooter(bool force=false){
  if(Busy||!session.world.map.hybrid)return false;if(!force&&BlockedNow(SurpriseRules.Scooter)!="")return false;
  float kmh=session.taxi.Speed*3.6f;if(!force&&(kmh<15||kmh>50))return false;
  Vector3 from,to,crossing;if(!FindScooterSpot(force?30:40,force?80:70,out from,out to,out crossing))return false;
  SpawnScooter(from,to,crossing);return true;
 }
 public void SpawnScooter(Vector3 from,Vector3 to,Vector3 crossing){
  var e=new ScooterEvent{from=from,to=to,crossing=crossing,spawned=Time.time};var dir=to-from;dir.y=0;e.direction=dir.normalized;
  var rider=new GameObject("돌발 · 킥보드");rider.transform.SetParent(transform);rider.transform.SetPositionAndRotation(from,Quaternion.LookRotation(e.direction));
  var vest=WorldBuilder.Mat("돌발 형광 조끼",new Color(.75f,1,.1f));vest.EnableKeyword("_EMISSION");vest.SetColor("_EmissionColor",new Color(.5f,.8f,.05f));
  var dark=WorldBuilder.Mat("돌발 킥보드",new Color(.12f,.13f,.15f));var skin=WorldBuilder.Mat("돌발 얼굴",new Color(.85f,.68f,.55f));
  WorldBuilder.Box("발판",new Vector3(0,.12f,0),new Vector3(.18f,.06f,1f),dark,rider.transform,false);
  WorldBuilder.Box("손잡이 대",new Vector3(0,.62f,.45f),new Vector3(.05f,1f,.05f),dark,rider.transform,false);
  WorldBuilder.Box("다리",new Vector3(0,.6f,-.05f),new Vector3(.32f,.85f,.25f),dark,rider.transform,false);
  WorldBuilder.Box("조끼",new Vector3(0,1.3f,0),new Vector3(.45f,.6f,.3f),vest,rider.transform,false);
  WorldBuilder.Box("머리",new Vector3(0,1.78f,0),new Vector3(.24f,.28f,.24f),skin,rider.transform,false);
  if(session.traffic)session.traffic.RegisterNightReadable(vest,new Color(.8f,1,.15f));
  e.bell=rider.AddComponent<AudioSource>();e.bell.spatialBlend=1;e.bell.minDistance=6;e.bell.maxDistance=80;e.bell.playOnAwake=false;e.bell.clip=BellClip();
  foreach(var child in rider.GetComponentsInChildren<Transform>())child.gameObject.layer=9;
  e.rider=rider;scooter=e;rider.SetActive(false);
  Debug.Log("ROAD_SURPRISE scooter spawned at "+from.ToString("F1"));
 }
 static AudioClip bellClip;
 static AudioClip BellClip(){if(bellClip)return bellClip;const int rate=44100;int length=(int)(rate*.9f);var data=new float[length];
  foreach(float start in new[]{0f,.22f}){int s=(int)(rate*start);for(int i=s;i<length;i++){float t=(i-s)/(float)rate;float env=Mathf.Exp(-t*9)*Mathf.Clamp01(t*500);data[i]+=.35f*env*(Mathf.Sin(2*Mathf.PI*2400*t)+.5f*Mathf.Sin(2*Mathf.PI*3600*t));}}
  bellClip=AudioClip.Create("킥보드 벨",length,1,rate,false);bellClip.SetData(data,0);return bellClip;}
 void TickScooter(){
  var e=scooter;if(!e.rider){scooter=null;return;}
  var taxi=session.taxi.transform.position;var rel=e.crossing-taxi;rel.y=0;var heading=session.taxi.transform.forward;heading.y=0;heading.Normalize();
  float along=Vector3.Dot(rel,heading),speed=session.taxi.Speed;
  SampleBraking();
  if(e.startAt<0){
   float tell=SurpriseRules.ScooterTriggerMetres+speed*SurpriseRules.ScooterTellSeconds;
   // Visible (and ringing) 1 s of travel before it rolls out.
   if(!e.rider.activeSelf&&along>0&&along<=tell+2){e.rider.SetActive(true);e.bell.Play();}
   if(e.rider.activeSelf&&along>0&&along<=SurpriseRules.ScooterTriggerMetres+2){e.startAt=Time.time;e.rider.AddComponent<FaultActor>().Begin();if(!e.counted){e.counted=true;Count(SurpriseRules.Scooter);}Debug.Log("ROAD_SURPRISE scooter rolls out along="+along.ToString("0.0")+" kmh="+(speed*3.6f).ToString("0"));}
   if(Time.time-e.spawned>40||along<-5||along>140){Missed++;LastOutcome="지나치지 않음";EndScooter();nextTry=Time.time+20;}
   return;
  }
  float travel=(Time.time-e.startAt)*SurpriseRules.ScooterSpeed;float total=Vector3.Distance(e.from,e.to);
  // The rider stops short if the taxi is right in front, like a person would.
  var next=Vector3.Lerp(e.from,e.to,Mathf.Clamp01(travel/Mathf.Max(.1f,total)));
  e.rider.transform.position=next;
  if(e.doneAt<0){
   var chest=next+Vector3.up*.9f;
   if(session.accident&&session.accident.TouchesPerson(chest)){
    e.touched=true;e.doneAt=Time.time;float since=Time.time-e.startAt;bool above=session.law&&RoadSpeedLimits.AboveLimit(speed*3.6f,session.law.CurrentLimit);bool braking=TaxiBraking;
    bool begun=session.accident.ReportContact("scooter:"+Started);
    bool relief=begun&&SurpriseRules.Relieved(since,above,braking)&&session.accident.State.ReliefPerson(SurpriseRules.ReliefPercent);
    if(relief)session.business.SaveCheckpoint();
    LastScooterRelief=relief;Finish(relief?"접촉 · 감경":"접촉",false);
    Debug.Log("ROAD_SURPRISE scooter contact since="+since.ToString("0.00")+" above="+above+" braking="+braking+" relief="+relief);
   }else if(travel>=total){e.doneAt=Time.time;Finish("먼저 보냄",true);Card("돌발 · 킥보드를 먼저 보냈어요"+(Carrying?" · “어휴, 저렇게 튀어나오면 큰일 나요.”":""));}
  }
  if(e.doneAt>=0&&Time.time-e.doneAt>2)EndScooter();
 }
 public bool LastScooterRelief;public string AmbulanceReject="";
 void EndScooter(){if(scooter!=null&&scooter.rider){scooter.rider.SetActive(false);Destroy(scooter.rider);}scooter=null;}

 // ---- ambulance ----
 sealed class AmbulanceEvent {public GameObject body;public HdRoadGraph.RoutePath route;public float distance,centreAt,spawned,passedAt=-1;public TrafficJunction junction;public bool violated,counted;public AudioSource siren;public Renderer[] lamps;public Material red,blue,off;public int side;}
 AmbulanceEvent ambulance;
 public GameObject AmbulanceBody {get{return ambulance==null?null:ambulance.body;}}
 public TrafficJunction AmbulanceJunction {get{return ambulance==null?null:ambulance.junction;}}
 public bool AmbulanceViolation {get{return ambulance!=null&&ambulance.violated;}}
 public int AmbulanceSide {get{return ambulance==null?0:ambulance.side;}}
 // The junction ahead that the taxi approaches on green, if any.
 public TrafficJunction JunctionAhead(out int axis,out int sign,out float ahead){
  axis=sign=0;ahead=0;var t=session.taxi.transform;var fwd=t.forward;fwd.y=0;fwd.Normalize();TrafficJunction best=null;float bestAhead=float.MaxValue;
  foreach(var j in session.traffic.junctions){if(!j.signalized||!j.SameLevel(t.position))continue;var rel=j.center-t.position;rel.y=0;float a=Vector3.Dot(rel,fwd),lat=Vector3.Dot(rel,TrafficSystem.Right(fwd));
   if(a<0||a>100||Mathf.Abs(lat)>10||a>=bestAhead)continue;best=j;bestAhead=a;}
  if(best==null)return null;ahead=bestAhead;axis=Mathf.Abs(Vector3.Dot(fwd,session.traffic.AxisDirection(best,0)))>.7f?0:1;sign=session.traffic.Sign(fwd,axis,best);return best;
 }
 public bool TryAmbulance(bool force=false){
  if(Busy||session.traffic==null||session.jobs.nav.hd==null){AmbulanceReject="다른 돌발 진행 중";return false;}if(!force&&BlockedNow(SurpriseRules.Ambulance)!=""){AmbulanceReject=BlockedNow(SurpriseRules.Ambulance);return false;}
  int axis,sign;float ahead;var j=JunctionAhead(out axis,out sign,out ahead);if(j==null){AmbulanceReject="앞 교차로 없음";return false;}
  bool green=session.traffic.Signal(j,axis,sign)==SignalColor.Green;
  var fwd=session.taxi.transform.forward;fwd.y=0;fwd.Normalize();var right=TrafficSystem.Right(fwd);
  float lat=Vector3.Dot(j.center-session.taxi.transform.position,right);
  if(!force&&!SurpriseRules.AmbulanceCandidate(ahead,lat,green,session.taxi.Speed*3.6f)){AmbulanceReject="조건 밖";return false;}AmbulanceReject="";
  // Cross direction: from the left or the right, whichever approach lane is empty.
  var graph=session.jobs.nav.hd;var cross=session.traffic.AxisDirection(j,1-axis);
  foreach(int s in new[]{1,-1}){var heading=cross*s;var approach=j.center-heading*(SurpriseRules.AmbulanceSpeed*SurpriseRules.SirenSeconds);
   // Its lane is on the right of its own heading.
   float at;int edge=graph.Nearest(approach+TrafficSystem.Right(heading)*1.75f,heading,out at);if(edge<0||graph.edges[edge].change!=0){AmbulanceReject+=" "+s+":차로 없음";continue;}
   var route=graph.Forward(edge,Mathf.Max(0,at-5));if(route==null)continue;
   float centreErr;float centreAt=route.path.Project(j.center,out centreErr);if(centreErr>8||centreAt<30||route.path.length-centreAt<25){AmbulanceReject+=" "+s+":경로 err="+centreErr.ToString("0")+" at="+centreAt.ToString("0")+" len="+route.path.length.ToString("0");continue;}
   bool occupied=false;foreach(var c in session.traffic.cars)if(c&&c.Visible&&c.gameObject.activeInHierarchy){float err;float cAt=route.path.Project(c.transform.position,out err);if(err<1.6f&&cAt<centreAt)occupied=true;}
   if(occupied){AmbulanceReject+=" "+s+":접근 차로에 차";continue;}
   SpawnAmbulance(j,route,centreAt,Vector3.Dot(heading,right)>0?-1:1);return true;
  }
  return false;
 }
 void SpawnAmbulance(TrafficJunction j,HdRoadGraph.RoutePath route,float centreAt,int side){
  var e=new AmbulanceEvent{junction=j,route=route,distance=0,centreAt=centreAt,spawned=Time.time,side=side};
  var body=new GameObject("돌발 · 구급차");body.transform.SetParent(transform);
  var rb=body.AddComponent<Rigidbody>();rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;
  var box=body.AddComponent<BoxCollider>();box.center=new Vector3(0,1.1f,0);box.size=new Vector3(2f,2f,5.2f);body.AddComponent<StagedVehicle>();
  var white=WorldBuilder.Mat("구급차 흰색",new Color(.95f,.95f,.93f));var stripe=WorldBuilder.Mat("구급차 띠",new Color(.85f,.1f,.08f));var glass=WorldBuilder.Mat("구급차 유리",new Color(.08f,.15f,.2f));var tyre=WorldBuilder.Mat("AI tyres",new Color(.035f,.04f,.05f));
  WorldBuilder.Box("차체",new Vector3(0,1.15f,-.3f),new Vector3(2f,1.8f,4.4f),white,body.transform,false);
  WorldBuilder.Box("앞부분",new Vector3(0,.75f,2.2f),new Vector3(1.95f,1f,.9f),white,body.transform,false);
  WorldBuilder.Box("앞유리",new Vector3(0,1.55f,1.9f),new Vector3(1.8f,.55f,.05f),glass,body.transform,false);
  foreach(int sd in new[]{-1,1})WorldBuilder.Box("띠",new Vector3(sd*1.01f,1.1f,-.3f),new Vector3(.02f,.3f,4.4f),stripe,body.transform,false);
  e.red=WorldBuilder.Mat("경광등 빨강",new Color(1,.1f,.05f));e.red.EnableKeyword("_EMISSION");e.red.SetColor("_EmissionColor",new Color(1,.05f,0)*3);
  e.blue=WorldBuilder.Mat("경광등 파랑",new Color(.1f,.3f,1));e.blue.EnableKeyword("_EMISSION");e.blue.SetColor("_EmissionColor",new Color(0,.2f,1)*3);
  e.off=WorldBuilder.Mat("경광등 꺼짐",new Color(.3f,.3f,.32f));
  var lamps=new List<Renderer>();foreach(int sd in new[]{-1,1})lamps.Add(WorldBuilder.Box("경광등",new Vector3(sd*.55f,2.12f,1.4f),new Vector3(.5f,.15f,.3f),e.off,body.transform,false).GetComponent<Renderer>());e.lamps=lamps.ToArray();
  for(int i=0;i<4;i++){var w=GameObject.CreatePrimitive(PrimitiveType.Cylinder);w.transform.SetParent(body.transform,false);w.transform.localPosition=new Vector3(i%2==0?-.92f:.92f,.38f,i<2?1.7f:-1.6f);w.transform.localRotation=Quaternion.Euler(0,0,90);w.transform.localScale=new Vector3(.74f,.14f,.74f);w.GetComponent<Renderer>().sharedMaterial=tyre;Destroy(w.GetComponent<Collider>());}
  foreach(var child in body.GetComponentsInChildren<Transform>())child.gameObject.layer=9;
  e.siren=body.AddComponent<AudioSource>();e.siren.spatialBlend=1;e.siren.minDistance=12;e.siren.maxDistance=260;e.siren.loop=true;e.siren.clip=SirenClip();e.siren.Play();
  e.body=body;ambulance=e;Place(e);
  session.traffic.emergencyHold=j;session.traffic.emergencyHoldUntil=Time.time+60;
  Count(SurpriseRules.Ambulance);
  Card("돌발 · 구급차 접근("+(side<0?"왼쪽":"오른쪽")+") · 교차로 앞에서 멈춰 주세요");
  Debug.Log("ROAD_SURPRISE ambulance at junction "+j.name+" side="+side+" centreAt="+centreAt.ToString("0"));
 }
 static AudioClip sirenClip;
 static AudioClip SirenClip(){if(sirenClip)return sirenClip;const int rate=22050;int length=rate*2;var data=new float[length];float phase=0;
  for(int i=0;i<length;i++){float t=i/(float)rate;float f=t<1?950:650;phase+=2*Mathf.PI*f/rate;data[i]=.28f*(Mathf.Sin(phase)+.3f*Mathf.Sin(2*phase));}
  sirenClip=AudioClip.Create("구급차 사이렌",length,1,rate,false);sirenClip.SetData(data,0);return sirenClip;}
 void Place(AmbulanceEvent e){var p=e.route.path.Position(e.distance);var d=e.route.path.Tangent(e.distance);d.y=0;var rb=e.body.GetComponent<Rigidbody>();if(d.sqrMagnitude<.01f)d=e.body.transform.forward;rb.MovePosition(p);rb.MoveRotation(Quaternion.LookRotation(d.normalized));}
 void TickAmbulance(){
  var e=ambulance;if(!e.body){EndAmbulance();return;}
  bool lit=Mathf.Repeat(Time.time,.5f)<.25f;e.lamps[0].sharedMaterial=lit?e.red:e.off;e.lamps[1].sharedMaterial=lit?e.off:e.blue;
  // Drive along its lane at its speed, stopping short of anything in front (a car still inside the junction, the taxi).
  float step=SurpriseRules.AmbulanceSpeed*Time.deltaTime;var ahead=e.route.path.Position(Mathf.Min(e.route.path.length,e.distance+5));var dir=e.route.path.Tangent(e.distance);
  bool blocked=false;foreach(var h in Physics.OverlapBox(ahead+Vector3.up*1f,new Vector3(1,.7f,3),Quaternion.LookRotation(dir),(1<<8)|(1<<9),QueryTriggerInteraction.Ignore))if(h.attachedRigidbody!=e.body.GetComponent<Rigidbody>()){blocked=true;break;}
  if(!blocked){e.distance=Mathf.Min(e.route.path.length,e.distance+step);Place(e);}
  // The taxi entering the junction before the ambulance has gone through it.
  var taxi=session.taxi.transform.position;float half=Mathf.Max(e.junction.halfWidth[0],e.junction.halfWidth[1]);var rel=taxi-e.junction.center;rel.y=0;
  bool passed=e.distance>e.centreAt+half+6;
  if(!e.violated&&!passed&&rel.magnitude<half&&session.taxi.Speed>.5f){e.violated=true;Violation();}
  if(passed&&e.passedAt<0){e.passedAt=Time.time;session.traffic.emergencyHoldUntil=Time.time+1;
   if(!e.violated){Finish("양보",true);Card("돌발 · 구급차에 길을 비켜 줬어요"+(Carrying?" · “잘하셨어요. 누가 다쳤나 봐요.”":""));}else Finish("양보 위반",false);}
  if(e.passedAt>=0&&Time.time-e.passedAt>6||Time.time-e.spawned>45)EndAmbulance();
 }
 void Violation(){
  var law=session.law;
  var ev=new LawEvent{recoveryPolicyVersion=1,id=law.ids.Next("emergency-yield"),day=session.business.ledger.Day,tripId=law.tripId,family="긴급차",action="긴급차 양보 위반",reason="사이렌이 울린 뒤 구급차보다 먼저 교차로에 진입(게임 규칙)",speed=session.taxi.Speed,seconds=(float)session.business.ledger.Elapsed,occupied=Carrying};
  ev.Add("emergency-yield",LawVerdict.Violation,SurpriseRules.YieldScorePenalty);law.ledger.Record(ev);
  if(Carrying&&session.manners)session.manners.Deduct("emergency:"+ev.id,SurpriseRules.YieldSatisfactionPenalty,"긴급차 양보 위반",0);
  Card("돌발 · 긴급차 양보 위반 · 운전 점수 −"+SurpriseRules.YieldScorePenalty);
  session.business.SaveCheckpoint();Debug.Log("ROAD_SURPRISE ambulance yield violation");
 }
 void EndAmbulance(){if(ambulance!=null){if(ambulance.body){ambulance.body.SetActive(false);Destroy(ambulance.body);}if(session&&session.traffic&&session.traffic.emergencyHold==ambulance.junction)session.traffic.emergencyHold=null;}ambulance=null;}
 void OnGUI(){
  if(ambulance==null||ambulance.passedAt>=0||!session||session.IsSimulationBot)return;
  // Screen-edge cue on the side the ambulance comes from.
  if(Mathf.Repeat(Time.time,.5f)>=.3f)return;var old=GUI.color;GUI.color=new Color(1,.1f,.05f,.55f);float w=Mathf.Max(18,Screen.width*.018f);
  GUI.DrawTexture(new Rect(ambulance.side<0?0:Screen.width-w,Screen.height*.15f,w,Screen.height*.6f),Texture2D.whiteTexture);GUI.color=old;
 }
}
