using UnityEngine;
[System.Serializable] public sealed class MannerCrossingSnapshot {public string junction;public Vector3 direction;public int axis,sign;public bool through,left,right,uturnSigned,uturnAllowed,penalized,yieldPenalized,uncertain,simpleRightStop,stopped,entered;}
[System.Serializable] public sealed class DrivingMannersSnapshot {public int score,events,redEvents,collisionEvents,accelEvents,brakeEvents,boundaryEvents,necessaryBrakeEvents,motionDebt,permanentLoss,stableRecovered;public bool comfortAccepted,recoveryCaptured;public string[] reasons;public StableDrivingSnapshot stable;public DrivingLawSnapshot lawObservation;public MannerCrossingSnapshot crossing;}
public class DrivingManners : MonoBehaviour {
 public DriveSession session;public readonly MannerLedger ledger=new MannerLedger();readonly StableDrivingProgress stable=new StableDrivingProgress();public string lastEvent="안전하게 운전해 주세요";public float eventUntil;public string collisionDiagnostic="none";public int events,redEvents,collisionEvents,accelEvents,brakeEvents,boundaryEvents;
 class Crossing {public TrafficJunction junction;public Vector3 direction;public int axis,sign;public bool through,left,right,uturnSigned,uturnAllowed,penalized,yieldPenalized,uncertain,simpleRightStop,stopped,entered;}
 readonly System.Collections.Generic.Dictionary<string,float> stops=new System.Collections.Generic.Dictionary<string,float>();
 Crossing crossing;
 readonly System.Collections.Generic.List<string> rideReasons=new System.Collections.Generic.List<string>();
 public string RideReasons {get{return rideReasons.Count==0?"감점 없음":string.Join(" / ",rideReasons.ToArray());}}
 public bool ComfortAccepted;public int necessaryBrakeEvents;
 public DrivingMannersSnapshot CaptureLive(){return new DrivingMannersSnapshot{crossing=crossing==null?null:new MannerCrossingSnapshot{junction=crossing.junction.name,direction=crossing.direction,axis=crossing.axis,sign=crossing.sign,stopped=crossing.stopped,entered=crossing.entered,through=crossing.through,left=crossing.left,right=crossing.right,uturnSigned=crossing.uturnSigned,uturnAllowed=crossing.uturnAllowed,penalized=crossing.penalized,yieldPenalized=crossing.yieldPenalized,uncertain=crossing.uncertain,simpleRightStop=crossing.simpleRightStop},lawObservation=session&&session.law?session.law.CaptureObservation():null,score=ledger.Score,events=events,redEvents=redEvents,collisionEvents=collisionEvents,accelEvents=accelEvents,brakeEvents=brakeEvents,boundaryEvents=boundaryEvents,necessaryBrakeEvents=necessaryBrakeEvents,motionDebt=ledger.MotionDebt,permanentLoss=ledger.PermanentLoss,stableRecovered=ledger.StableRecovered,recoveryCaptured=true,stable=stable.Capture(),comfortAccepted=ComfortAccepted,reasons=rideReasons.ToArray()};}
 public void RestoreLive(DrivingMannersSnapshot s){if(s.recoveryCaptured){ledger.RestoreRecovery(s.score,s.motionDebt,s.permanentLoss,s.stableRecovered);stable.Restore(s.stable);}else{ledger.RestoreScore(s.score);stable.Clear();}events=s.events;redEvents=s.redEvents;collisionEvents=s.collisionEvents;accelEvents=s.accelEvents;brakeEvents=s.brakeEvents;boundaryEvents=s.boundaryEvents;necessaryBrakeEvents=s.necessaryBrakeEvents;ComfortAccepted=s.comfortAccepted;rideReasons.Clear();if(s.reasons!=null)rideReasons.AddRange(s.reasons);Suppress(.8f);if(session&&session.law)session.law.RestoreObservation(s.lawObservation);var c=s.crossing;if(c!=null&&session&&session.traffic){var j=session.traffic.junctions.Find(x=>x.name==c.junction);if(j!=null&&c.axis>=0&&c.axis<2&&(c.sign==1||c.sign==-1))crossing=new Crossing{junction=j,direction=c.direction,axis=c.axis,sign=c.sign,stopped=c.stopped,entered=c.entered,through=c.through,left=c.left,right=c.right,uturnSigned=c.uturnSigned,uturnAllowed=c.uturnAllowed,penalized=c.penalized,yieldPenalized=c.yieldPenalized,uncertain=c.uncertain,simpleRightStop=c.simpleRightStop};}}
 Vector3 previousVelocity,previousFront;float suppressed,filtered,harshTime,offroadTime;bool harshLatched,offroadLatched,necessaryLatched;int sample;
 public void BeginRide(){ComfortAccepted=false;ledger.Reset();stable.Clear();rideReasons.Clear();lastEvent="승객 탑승 · 운전 만족도 M 100점으로 시작";eventUntil=Time.time+4;Suppress(.8f);}
 public void Suppress(float seconds){crossing=null;stops.Clear();suppressed=Time.time+seconds;previousVelocity=GetComponent<Rigidbody>().velocity;previousFront=transform.position+transform.forward*2;stable.Rebase(transform.position.x,transform.position.z);filtered=0;harshTime=0;harshLatched=false;offroadTime=0;offroadLatched=false;}
 void Start(){Suppress(1);}
 public bool Deduct(string key,int amount,string reason,float cooldown=3){var notices=session&&session.jobs&&session.law&&session.requests?session.GetComponent<DriveNotifications>():null;if(notices)notices.RefreshScores();if(!ledger.Penalize(key,amount,Time.time,cooldown))return false;stable.BreakSegment();events++;if(key.StartsWith("red"))redEvents++;if(key=="collision")collisionEvents++;if(key=="accel")accelEvents++;if(key=="brake")brakeEvents++;if(key=="sidewalk")boundaryEvents++;lastEvent=reason+" -"+amount+"점";rideReasons.Add(lastEvent);eventUntil=Time.time+4;if(notices)notices.RefreshScores();return true;}
 public void ObserveStable(float x,float z,float speed,float seconds,bool gentle){if(!session||!session.jobs||session.jobs.trip.State!=TripState.OnTrip||session.jobs.trip.ArrivalLocked)return;if(!stable.Observe(x,z,speed,seconds,gentle))return;int gain=ledger.RecoverStable(2);int comfort=session.jobs.RecoverComfort(gain);if(gain<=0&&comfort<=0)return;lastEvent=gain>0?"안정적인 주행 +"+gain+"점":"안정 주행 안심 +"+comfort+"점";rideReasons.Add(lastEvent);eventUntil=Time.time+4;var notices=session.GetComponent<DriveNotifications>();if(notices)notices.RefreshScores();if(session.business)session.business.SaveCheckpoint();}
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
   ObserveStable(transform.position.x,transform.position.z,velocity.magnitude,Time.fixedDeltaTime,!harsh);
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
   var q=after-crossing.junction.center;float along=Vector3.Dot(q,crossing.direction),side=Vector3.Dot(q,TrafficSystem.Right(crossing.direction));
   if(!crossing.entered){
    var observed=session.traffic.Observe(crossing.junction,crossing.axis,crossing.sign,crossing.direction,gameObject);string laneId=session.traffic.EditorLaneId(transform.position,transform.forward);var leftSignal=crossing.junction.editorProgram!=null?crossing.junction.editorProgram.Movement(crossing.axis,crossing.sign,-1,session.traffic.Clock,laneId):session.traffic.LeftSignal(crossing.junction,crossing.axis,crossing.sign);
    crossing.through=observed.forward==SignalColor.Green;crossing.left=leftSignal==SignalColor.Green;crossing.right=observed.RightSignalPermits(crossing.stopped);crossing.uncertain=observed.forward==SignalColor.Yellow||leftSignal==SignalColor.Yellow;crossing.uturnAllowed=crossing.uturnSigned&&observed.PedestrianClear&&session.traffic.UturnPermitted(crossing.junction,crossing.axis,crossing.sign,laneId);crossing.simpleRightStop=observed.known&&!observed.dedicated&&observed.forward==SignalColor.Red&&observed.PedestrianClear&&!crossing.stopped;
    crossing.entered=along>= -crossing.junction.halfWidth[1-crossing.axis];
   }
   // A driver may wait inside a permissive junction; evaluate yielding when actually turning.
   float angle=Vector3.SignedAngle(crossing.direction,transform.forward,Vector3.up);
   int turning=angle< -110&&crossing.uturnSigned?UturnMovement.Turn:angle< -15?-1:angle>15?1:0;
   if(turning!=0&&!crossing.yieldPenalized&&session.taxi.body.velocity.magnitude>.3f&&!session.traffic.YieldClear(crossing.junction,crossing.direction,turning,gameObject)){
    Deduct("yield"+crossing.junction.name,8,turning==UturnMovement.Turn?"유턴 양보 위반":turning<0?"비보호 좌회전 양보 위반":"우회전 양보 위반",5);crossing.yieldPenalized=true;
   }
   float edge=crossing.junction.halfWidth[crossing.axis]+4;bool left=side< -edge,right=side>edge,through=along>crossing.junction.StopDistance(crossing.axis)+3;
   bool reversed=Vector3.Dot(transform.forward,crossing.direction)<-.7f;bool uturn=reversed&&(left||along< -crossing.junction.StopDistance(crossing.axis)-3);bool holdLeft=crossing.uturnSigned&&left&&!uturn&&side> -edge-5;
   if(uturn||left&&!holdLeft||right||through){bool allowed=uturn?crossing.uturnAllowed:left?crossing.left:right?crossing.right:crossing.through;if(!crossing.uncertain&&!crossing.penalized&&!allowed)Deduct("redmovement"+crossing.junction.name,right&&crossing.simpleRightStop?4:12,uturn?"유턴 조건 위반":left?"좌회전 신호 위반":right?crossing.simpleRightStop?"우회전 전 일시정지 위반":"우회전 신호 위반":"진행 방향 신호 위반",5);crossing=null;}
  }
  foreach(var j in session.traffic.junctions)for(int axis=0;axis<2;axis++)foreach(int sign in new[]{-1,1}){
   var zones=session.GetComponent<ServiceLawZones>();if(zones&&zones.Owns(j))continue;
   if(!j.signalized&&!j.scenarioSignal)continue;
   var dir=session.traffic.AxisDirection(j,axis)*sign;if(Vector3.Dot(transform.forward,dir)<.7f)continue;var stop=session.traffic.StopPosition(j,axis,sign);var planeNormal=session.traffic.StopNormal(j,axis,sign);float a=Vector3.Dot(before-stop,planeNormal),b=Vector3.Dot(after-stop,planeNormal);float lane=Vector3.Dot(after-j.center,TrafficSystem.Right(dir));bool validLane=lane>.15f&&lane<j.halfWidth[axis]||session.traffic.DirectedApproach(j,axis,sign,transform.position,transform.forward);string key=j.name+axis+sign;
   if(b>=-3&&b<=TrafficJunction.StopLineEntryTolerance&&validLane&&session.taxi.body.velocity.magnitude<.05f)stops[key]=Time.time;
   if(a<TrafficJunction.StopLineEntryTolerance&&b>=TrafficJunction.StopLineEntryTolerance&&validLane){
    float last;bool stopped=stops.TryGetValue(key,out last)&&Time.time-last<8;stops.Remove(key);
    string laneId=session.traffic.EditorLaneId(transform.position,transform.forward);var forward=j.editorProgram!=null?j.editorProgram.Movement(axis,sign,0,session.traffic.Clock,laneId):session.traffic.Signal(j,axis,sign);var leftSignal=j.editorProgram!=null?j.editorProgram.Movement(axis,sign,-1,session.traffic.Clock,laneId):session.traffic.LeftSignal(j,axis,sign);bool uncertain=forward==SignalColor.Yellow||leftSignal==SignalColor.Yellow;bool through=forward==SignalColor.Green,allowedLeft=leftSignal==SignalColor.Green;
    var observed=session.traffic.Observe(j,axis,sign,dir,gameObject);bool right=observed.RightSignalPermits(stopped);bool uturnSigned=session.traffic.UturnCondition(laneId)!=null;bool allowedUturn=uturnSigned&&observed.PedestrianClear&&session.traffic.UturnPermitted(j,axis,sign,laneId);bool red=!uncertain&&!through&&!allowedLeft&&!right&&!allowedUturn;
    // Crossing the line alone cannot distinguish a red straight violation from a right turn.
    crossing=new Crossing{junction=j,direction=dir,axis=axis,sign=sign,stopped=stopped,through=through,left=allowedLeft,right=right,uturnSigned=uturnSigned,uturnAllowed=allowedUturn,penalized=false,uncertain=uncertain,simpleRightStop=observed.known&&!observed.dedicated&&observed.forward==SignalColor.Red&&observed.PedestrianClear&&!stopped};
   }
  }
 }
 void OnCollisionEnter(Collision collision){collisionDiagnostic=collision.collider.name+" prior="+previousVelocity.magnitude+" relative="+collision.relativeVelocity.magnitude+" normal="+(collision.contactCount>0?collision.GetContact(0).normal.ToString():"none")+" grace="+(suppressed-Time.time);if(Time.time<suppressed||previousVelocity.magnitude<1.5f||collision.relativeVelocity.magnitude<1.5f)return;
  if(collision.contactCount>0&&collision.GetContact(0).normal.y>.65f)return;
  Deduct("collision",10,"충돌",3);suppressed=Time.time+.8f;filtered=0;
 }
}
