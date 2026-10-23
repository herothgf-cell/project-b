using UnityEngine;
[System.Serializable] public sealed class DrivingMannersSnapshot {public int score,events,redEvents,collisionEvents,accelEvents,brakeEvents,boundaryEvents,necessaryBrakeEvents;public bool comfortAccepted;public string[] reasons;}
public class DrivingManners : MonoBehaviour {
 public DriveSession session;public readonly MannerLedger ledger=new MannerLedger();public string lastEvent="안전하게 운전해 주세요";public float eventUntil;public string collisionDiagnostic="none";public int events,redEvents,collisionEvents,accelEvents,brakeEvents,boundaryEvents;
 class Crossing {public TrafficJunction junction;public Vector3 direction;public int axis;public bool through,left,right,penalized,yieldPenalized,uncertain;}
 readonly System.Collections.Generic.Dictionary<string,float> stops=new System.Collections.Generic.Dictionary<string,float>();
 Crossing crossing;
 readonly System.Collections.Generic.List<string> rideReasons=new System.Collections.Generic.List<string>();
 public string RideReasons {get{return rideReasons.Count==0?"감점 없음":string.Join(" / ",rideReasons.ToArray());}}
 public bool ComfortAccepted;public int necessaryBrakeEvents;
 public DrivingMannersSnapshot CaptureLive(){return new DrivingMannersSnapshot{score=ledger.Score,events=events,redEvents=redEvents,collisionEvents=collisionEvents,accelEvents=accelEvents,brakeEvents=brakeEvents,boundaryEvents=boundaryEvents,necessaryBrakeEvents=necessaryBrakeEvents,comfortAccepted=ComfortAccepted,reasons=rideReasons.ToArray()};}
 public void RestoreLive(DrivingMannersSnapshot s){ledger.RestoreScore(s.score);events=s.events;redEvents=s.redEvents;collisionEvents=s.collisionEvents;accelEvents=s.accelEvents;brakeEvents=s.brakeEvents;boundaryEvents=s.boundaryEvents;necessaryBrakeEvents=s.necessaryBrakeEvents;ComfortAccepted=s.comfortAccepted;rideReasons.Clear();if(s.reasons!=null)rideReasons.AddRange(s.reasons);Suppress(.8f);}
 Vector3 previousVelocity,previousFront;float suppressed,filtered,harshTime,offroadTime;bool harshLatched,offroadLatched,necessaryLatched;int sample;
 public void BeginRide(){ComfortAccepted=false;ledger.Reset();rideReasons.Clear();lastEvent="승객 탑승 · 운전 만족도 M 100점으로 시작";eventUntil=Time.time+4;Suppress(.8f);}
 public void Suppress(float seconds){crossing=null;stops.Clear();suppressed=Time.time+seconds;previousVelocity=GetComponent<Rigidbody>().velocity;previousFront=transform.position+transform.forward*2;filtered=0;harshTime=0;harshLatched=false;offroadTime=0;offroadLatched=false;}
 void Start(){Suppress(1);}
 public bool Deduct(string key,int amount,string reason,float cooldown=3){if(!ledger.Penalize(key,amount,Time.time,cooldown))return false;events++;if(key.StartsWith("red"))redEvents++;if(key=="collision")collisionEvents++;if(key=="accel")accelEvents++;if(key=="brake")brakeEvents++;if(key=="sidewalk")boundaryEvents++;lastEvent=reason+" -"+amount+"점";rideReasons.Add(lastEvent);eventUntil=Time.time+4;return true;}
 void FixedUpdate(){if(!session)return;var body=session.taxi.body;var velocity=body.velocity;var front=transform.position+transform.forward*2;
  if(Time.time>=suppressed){
   CheckSignal(previousFront,front);
   float accel=DriveMath.TravelAcceleration(Vector3.Dot((velocity-previousVelocity)/Time.fixedDeltaTime,transform.forward),session.taxi.Speed);filtered=Mathf.Lerp(filtered,accel,1-Mathf.Exp(-Time.fixedDeltaTime/.25f));
   bool required=filtered<0&&NecessaryBraking();bool excluded=required&&DriveMath.HarshPassengerMotion(filtered,session.taxi.Speed,false);if(excluded&&!necessaryLatched)necessaryBrakeEvents++;necessaryLatched=excluded;
   bool harsh=DriveMath.HarshPassengerMotion(filtered,session.taxi.Speed,required);
   if(harsh){harshTime+=Time.fixedDeltaTime;if(harshTime>.45f&&!harshLatched){Deduct(filtered>0?"accel":"brake",MannerLedger.MotionPenalty(ComfortAccepted),(ComfortAccepted?"차멀미 배려 요청 중 ":"")+(filtered>0?"급가속":"급제동"),6);harshLatched=true;}}else{harshTime=0;harshLatched=false;}
   if(++sample%5==0){int outside=0;foreach(float side in new[]{-.8f,.8f})foreach(float end in new[]{-1.25f,1.25f})if(!session.roadEnvironment.OnRoad(transform.TransformPoint(new Vector3(side,0,end)),.1f))outside++;
    if(outside>=2&&velocity.magnitude>1){offroadTime+=Time.fixedDeltaTime*5;if(offroadTime>.7f&&!offroadLatched){Deduct("sidewalk",5,"도로 경계·인도 침범",4);offroadLatched=true;}}else if(outside==0){offroadTime=0;offroadLatched=false;}
   }
  }
  previousVelocity=velocity;previousFront=front;
 }
 public bool NecessaryBraking(){
  var zones=session.GetComponent<ServiceLawZones>();if(zones&&zones.RequiredBraking())return true;
  // Observation only: assistance never takes over accelerator/brake input.
  float distance=Mathf.Max(5,session.taxi.body.velocity.magnitude*2+4);
  foreach(var hit in Physics.SphereCastAll(transform.position+Vector3.up*.65f,.8f,transform.forward,distance,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore))if(hit.rigidbody!=session.taxi.body)return true;
  foreach(var j in session.traffic.junctions)for(int axis=0;axis<2;axis++)foreach(int sign in new[]{-1,1}){
   var dir=session.traffic.AxisDirection(j,axis)*sign;
   // The right-side pedestrian crossing is beyond the entry stop line. Keep
   // recognizing required braking there after the vehicle turns into the exit.
   var outgoing=TrafficSystem.Right(dir);var front=transform.position+transform.forward*2;var offset=front-j.center;
   float toCrossing=j.CrossingNearEdge(axis)-Vector3.Dot(offset,outgoing);
   if(Vector3.Dot(transform.forward,outgoing)>.7f&&toCrossing>=-1&&toCrossing<=distance&&Mathf.Abs(Vector3.Dot(offset,dir))<=j.halfWidth[1-axis]+2){
    var crossingObservation=session.traffic.Observe(j,axis,sign,dir,gameObject);
    if(crossingObservation.pedestrian==PedestrianState.Intending||crossingObservation.pedestrian==PedestrianState.Crossing)return true;
   }
   if(Vector3.Dot(dir,transform.forward)<.85f)continue;
   var delta=j.center-dir*j.StopDistance(axis)-(transform.position+transform.forward*2);float ahead=Vector3.Dot(delta,dir),side=Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(dir)));
   if(ahead< -2||ahead>distance||side>j.halfWidth[axis])continue;
   var o=session.traffic.Observe(j,axis,sign,dir,gameObject);if(o.forward!=SignalColor.Green||!o.PedestrianClear||o.dedicated&&o.right!=SignalColor.Green)return true;
  }
  return false;
 }
 public void CheckSignal(Vector3 before,Vector3 after){if(Vector3.Distance(before,after)>10)return;
  if(crossing!=null){
   // A driver may wait inside a permissive junction; evaluate yielding when actually turning.
   float angle=Vector3.SignedAngle(crossing.direction,transform.forward,Vector3.up);
   int turning=angle< -15?-1:angle>15?1:0;
   if(turning!=0&&!crossing.yieldPenalized&&session.taxi.body.velocity.magnitude>.3f&&!session.traffic.YieldClear(crossing.junction,crossing.direction,turning,gameObject)){
    Deduct("yield"+crossing.junction.name,8,turning<0?"비보호 좌회전 양보 위반":"우회전 양보 위반",5);crossing.yieldPenalized=true;
   }
   var q=after-crossing.junction.center;float along=Vector3.Dot(q,crossing.direction),side=Vector3.Dot(q,TrafficSystem.Right(crossing.direction));float edge=crossing.junction.halfWidth[crossing.axis]+4;bool left=side< -edge,right=side>edge,through=along>crossing.junction.StopDistance(crossing.axis)+3;
   if(left||right||through){if(!crossing.uncertain&&!crossing.penalized&&!(left?crossing.left:right?crossing.right:crossing.through))Deduct("redmovement"+crossing.junction.name,12,left?"좌회전 신호 위반":right?"우회전 전 일시정지 위반":"진행 방향 신호 위반",5);crossing=null;}
  }
  foreach(var j in session.traffic.junctions)for(int axis=0;axis<2;axis++)foreach(int sign in new[]{-1,1}){
   var zones=session.GetComponent<ServiceLawZones>();if(zones&&zones.Owns(j))continue;
   if(!j.signalized&&!j.scenarioSignal)continue;
   var dir=session.traffic.AxisDirection(j,axis)*sign;if(Vector3.Dot(transform.forward,dir)<.7f)continue;var stop=session.traffic.StopPosition(j,axis,sign);var planeNormal=session.traffic.StopNormal(j,axis,sign);float a=Vector3.Dot(before-stop,planeNormal),b=Vector3.Dot(after-stop,planeNormal);float lane=Vector3.Dot(after-j.center,TrafficSystem.Right(dir));string key=j.name+axis+sign;
   if(b>=-3&&b<=TrafficJunction.StopLineEntryTolerance&&lane>.15f&&lane<j.halfWidth[axis]&&session.taxi.body.velocity.magnitude<.05f)stops[key]=Time.time;
   if(a<TrafficJunction.StopLineEntryTolerance&&b>=TrafficJunction.StopLineEntryTolerance&&lane>.15f&&lane<j.halfWidth[axis]){
    float last;bool stopped=stops.TryGetValue(key,out last)&&Time.time-last<8;stops.Remove(key);
    string laneId=session.traffic.EditorLaneId(transform.position,transform.forward);var forward=j.editorProgram!=null?j.editorProgram.Movement(axis,sign,0,session.traffic.Clock,laneId):session.traffic.Signal(j,axis,sign);var leftSignal=j.editorProgram!=null?j.editorProgram.Movement(axis,sign,-1,session.traffic.Clock,laneId):session.traffic.LeftSignal(j,axis,sign);bool uncertain=forward==SignalColor.Yellow||leftSignal==SignalColor.Yellow;bool through=forward==SignalColor.Green,allowedLeft=leftSignal==SignalColor.Green;
    bool right=session.traffic.Observe(j,axis,sign,dir,gameObject).RightSignalPermits(stopped);bool red=!uncertain&&!through&&!allowedLeft&&!right;
    if(red)Deduct("red"+j.name+axis+sign,12,"적색 신호 통과",5);
    crossing=new Crossing{junction=j,direction=dir,axis=axis,through=through,left=allowedLeft,right=right,penalized=red,uncertain=uncertain};
   }
  }
 }
 void OnCollisionEnter(Collision collision){collisionDiagnostic=collision.collider.name+" prior="+previousVelocity.magnitude+" relative="+collision.relativeVelocity.magnitude+" normal="+(collision.contactCount>0?collision.GetContact(0).normal.ToString():"none")+" grace="+(suppressed-Time.time);if(Time.time<suppressed||previousVelocity.magnitude<1.5f||collision.relativeVelocity.magnitude<1.5f)return;
  if(collision.contactCount>0&&collision.GetContact(0).normal.y>.65f)return;
  Deduct("collision",10,"충돌",3);suppressed=Time.time+.8f;filtered=0;
 }
}
