using UnityEngine;
public class DrivingManners : MonoBehaviour {
 public DriveSession session;public readonly MannerLedger ledger=new MannerLedger();public string lastEvent="안전하게 운전해 주세요";public float eventUntil;public string collisionDiagnostic="none";public int events,redEvents,collisionEvents,accelEvents,brakeEvents,boundaryEvents;
 class Crossing {public TrafficJunction junction;public Vector3 direction;public int axis;public bool through,left,right,penalized,yieldPenalized;}
 readonly System.Collections.Generic.Dictionary<string,float> stops=new System.Collections.Generic.Dictionary<string,float>();
 Crossing crossing;
 Vector3 previousVelocity,previousFront;float suppressed,filtered,harshTime,offroadTime;bool harshLatched,offroadLatched;int sample;
 public void BeginRide(){ledger.Reset();lastEvent="승객 탑승 · 매너 100점으로 시작";eventUntil=Time.time+4;Suppress(.8f);}
 public void Suppress(float seconds){crossing=null;stops.Clear();suppressed=Time.time+seconds;previousVelocity=GetComponent<Rigidbody>().velocity;previousFront=transform.position+transform.forward*2;filtered=0;harshTime=0;harshLatched=false;offroadTime=0;offroadLatched=false;}
 void Start(){Suppress(1);}
 public bool Deduct(string key,int amount,string reason,float cooldown=3){if(!ledger.Penalize(key,amount,Time.time,cooldown))return false;events++;if(key.StartsWith("red"))redEvents++;if(key=="collision")collisionEvents++;if(key=="accel")accelEvents++;if(key=="brake")brakeEvents++;if(key=="sidewalk")boundaryEvents++;lastEvent=reason+" -"+amount+"점";eventUntil=Time.time+4;return true;}
 void FixedUpdate(){if(!session)return;var body=session.taxi.body;var velocity=body.velocity;var front=transform.position+transform.forward*2;
  if(Time.time>=suppressed){
   CheckSignal(previousFront,front);
   float accel=DriveMath.TravelAcceleration(Vector3.Dot((velocity-previousVelocity)/Time.fixedDeltaTime,transform.forward),session.taxi.Speed);filtered=Mathf.Lerp(filtered,accel,1-Mathf.Exp(-Time.fixedDeltaTime/.25f));
   bool harsh=(filtered>3.3f||filtered<-5.5f)&&Mathf.Abs(session.taxi.Speed)>2;
   if(harsh){harshTime+=Time.fixedDeltaTime;if(harshTime>.25f&&!harshLatched){Deduct(filtered>0?"accel":"brake",filtered>0?3:4,filtered>0?"급가속":"급제동",4);harshLatched=true;}}else{harshTime=0;harshLatched=false;}
   if(++sample%5==0){int outside=0;foreach(float side in new[]{-.8f,.8f})foreach(float end in new[]{-1.25f,1.25f})if(!session.roadEnvironment.OnRoad(transform.TransformPoint(new Vector3(side,0,end)),.1f))outside++;
    if(outside>=2&&velocity.magnitude>1){offroadTime+=Time.fixedDeltaTime*5;if(offroadTime>.7f&&!offroadLatched){Deduct("sidewalk",5,"도로 경계·인도 침범",4);offroadLatched=true;}}else if(outside==0){offroadTime=0;offroadLatched=false;}
   }
  }
  previousVelocity=velocity;previousFront=front;
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
   if(left||right||through){if(!crossing.penalized&&!(left?crossing.left:right?crossing.right:crossing.through))Deduct("redmovement"+crossing.junction.name,12,left?"좌회전 신호 위반":right?"우회전 전 일시정지 위반":"진행 방향 신호 위반",5);crossing=null;}
  }
  foreach(var j in session.traffic.junctions)for(int axis=0;axis<2;axis++)foreach(int sign in new[]{-1,1}){
   if(!j.signalized)continue;
   var dir=session.traffic.AxisDirection(j,axis)*sign;var stop=j.center-dir*j.StopDistance(axis);float a=Vector3.Dot(before-stop,dir),b=Vector3.Dot(after-stop,dir);float lane=Vector3.Dot(after-j.center,TrafficSystem.Right(dir));string key=j.name+axis+sign;
   if(b>=-3&&b<=0&&lane>.15f&&lane<j.halfWidth[axis]&&session.taxi.body.velocity.magnitude<.05f)stops[key]=Time.time;
   if(a<0&&b>=0&&lane>.15f&&lane<j.halfWidth[axis]){
    float last;bool stopped=stops.TryGetValue(key,out last)&&Time.time-last<8;stops.Remove(key);
    bool through=session.traffic.Signal(j,axis,sign)==SignalColor.Green,allowedLeft=session.traffic.LeftSignal(j,axis,sign)==SignalColor.Green;
    bool right=KoreanSignalRules.MayEnter(session.traffic.Clock+j.offset,j.leftTurnMode,axis,sign,1,stopped,true,true);bool red=!through&&!allowedLeft&&!right;
    if(red)Deduct("red"+j.name+axis+sign,12,"적색 신호 통과",5);
    crossing=new Crossing{junction=j,direction=dir,axis=axis,through=through,left=allowedLeft,right=right,penalized=red};
   }
  }
 }
 void OnCollisionEnter(Collision collision){collisionDiagnostic=collision.collider.name+" prior="+previousVelocity.magnitude+" relative="+collision.relativeVelocity.magnitude+" normal="+(collision.contactCount>0?collision.GetContact(0).normal.ToString():"none")+" grace="+(suppressed-Time.time);if(Time.time<suppressed||previousVelocity.magnitude<1.5f||collision.relativeVelocity.magnitude<1.5f)return;
  if(collision.contactCount>0&&collision.GetContact(0).normal.y>.65f)return;
  Deduct("collision",10,"충돌",3);suppressed=Time.time+.8f;filtered=0;
 }
}
