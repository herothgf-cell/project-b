using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Actual WheelCollider validation. Automated inputs use the same manual demand path as W/S/A/D.
public class ManualHandlingSmokeTest : MonoBehaviour {
 public DriveSession session;
 readonly StringBuilder report=new StringBuilder();bool ok=true;
 readonly Vector3 padCenter=new Vector3(-5000,0,-5000);
 void Check(bool value,string name){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+name);Debug.Log("MANUAL_HANDLING "+name+"="+value);}
 void Place(Vector3 p,Vector3 forward){var t=session.taxi;t.autoThrottle=t.autoSteer=t.autoServiceBrake=0;t.autoBrake=false;t.SetSpawn(p+Vector3.up*.65f,Quaternion.LookRotation(forward));t.ResetTaxi();}
 IEnumerator Settle(){yield return new WaitForSeconds(1);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);
  var t=session.taxi;var m=session.manners;t.automated=true;
  session.assisted.SetActive(false);session.assisted.allowAutomated=false;
  session.business.enabledCampaign=false;session.business.enabled=false;session.paused=false;Time.timeScale=1;
  foreach(var c in session.traffic.cars){c.Recycle();c.enabled=false;}
  var probe=t.gameObject.AddComponent<ManualHandlingContactProbe>();probe.manners=m;
  var pad=GameObject.CreatePrimitive(PrimitiveType.Cube);pad.name="Manual handling isolated pad";pad.transform.position=padCenter+Vector3.down*.5f;pad.transform.localScale=new Vector3(800,1,800);
  Place(padCenter,Vector3.forward);yield return Settle();m.BeginRide();yield return new WaitForSeconds(.9f);
  int accelBefore=m.accelEvents,brakeBefore=m.brakeEvents;t.autoThrottle=1;probe.Capture("ordinary-W");float startTime=Time.fixedTime;
  float elapsed=0,peakAcceleration=0,previousSpeed=t.Speed;
  while(elapsed<10&&t.Speed<30/3.6f){yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;peakAcceleration=Mathf.Max(peakAcceleration,(t.Speed-previousSpeed)/Time.fixedDeltaTime);previousSpeed=t.Speed;}
  Check(t.Speed>=30/3.6f&&elapsed>=3.6f&&elapsed<=8,"manual 0-30kph seconds="+elapsed+" actualFixedSeconds="+(Time.fixedTime-startTime)+" sampledPeakMps2="+peakAcceleration);
  Check(probe.maxWindowAcceleration<3.5f,"ordinary full W 300ms acceleration remains comfortable "+probe.Summary());
  Check(m.accelEvents==accelBefore,"ordinary full W has no harsh acceleration deduction before="+accelBefore+" "+probe.Summary());
  probe.Capture("ordinary-S");
  var brakingStart=t.transform.position;t.autoThrottle=-1;float brakeElapsed=0;
  while(brakeElapsed<5&&t.Speed>.4f){yield return new WaitForFixedUpdate();brakeElapsed+=Time.fixedDeltaTime;}
  float brakingDistance=Vector3.Distance(brakingStart,t.transform.position);t.autoThrottle=0;t.autoBrake=true;
  Check(brakeElapsed<5&&brakingDistance>=3&&brakingDistance<12&&m.brakeEvents==brakeBefore,"ordinary S stop distance="+brakingDistance+" seconds="+brakeElapsed+" no harsh deduction "+probe.Summary());probe.Capture("");
  foreach(int side in new[]{-1,1}){
   Place(padCenter,Vector3.forward);yield return Settle();m.Suppress(20);
   Vector3 start=t.transform.position;float time=0,angle=0,maxTilt=0;
   t.autoSteer=side;
   while(time<10&&angle<90){t.autoThrottle=t.Speed<2.2f?.75f:0;yield return new WaitForFixedUpdate();time+=Time.fixedDeltaTime;angle=Mathf.Abs(Vector3.SignedAngle(Vector3.forward,t.transform.forward,Vector3.up));maxTilt=Mathf.Max(maxTilt,Vector3.Angle(t.transform.up,Vector3.up));}
   Vector3 delta=t.transform.position-start;float radius=(Mathf.Abs(delta.x)+Mathf.Abs(delta.z))*.5f;t.autoThrottle=0;t.autoBrake=true;
   Check(angle>=90&&radius<=3.6f&&maxTilt<15,"physical low-speed 90deg side="+side+" radius="+radius+" delta="+delta+" seconds="+time+" tilt="+maxTilt);
  }
  foreach(int side in new[]{-1,1}){
   Place(padCenter,Vector3.forward);yield return Settle();m.Suppress(20);t.body.velocity=Vector3.forward*25;t.autoSteer=side;
   float maxTilt=0;for(float time=0;time<4;time+=Time.fixedDeltaTime){yield return new WaitForFixedUpdate();maxTilt=Mathf.Max(maxTilt,Vector3.Angle(t.transform.up,Vector3.up));}
   Check(maxTilt<25,"90kph full steering remains upright side="+side+" maxTilt="+maxTilt);
  }
  // Sustained abuse still deducts, while the ordinary W/S cases above remain comfortable.
  Place(padCenter,Vector3.forward);yield return Settle();m.BeginRide();yield return new WaitForSeconds(.9f);accelBefore=m.accelEvents;probe.Capture("harsh-acceleration");
  // Releasing the idle hold is essential: with no W it physically cancels the
  // external force. Apply the sustained test force on every physics tick.
  t.autoThrottle=1;probe.injectAcceleration=9;yield return new WaitForSeconds(1.3f);probe.injectAcceleration=0;
  Check(m.accelEvents==accelBefore+1&&m.RideReasons.Contains("급가속 -2점"),"sustained harsh acceleration remains one two-point event before="+accelBefore+" "+probe.Summary());probe.Capture("");
  Place(padCenter,Vector3.forward);yield return Settle();t.body.velocity=Vector3.forward*20;m.BeginRide();yield return new WaitForSeconds(.9f);brakeBefore=m.brakeEvents;t.autoBrake=true;probe.Capture("harsh-braking");
  yield return new WaitForSeconds(2);
  Check(m.brakeEvents==brakeBefore+1&&m.RideReasons.Contains("급제동 -2점"),"sustained harsh emergency brake remains one two-point event before="+brakeBefore+" "+probe.Summary());probe.Capture("");
  Place(padCenter,Vector3.forward);yield return Settle();t.body.velocity=Vector3.forward*20;m.BeginRide();
  var hazard=GameObject.CreatePrimitive(PrimitiveType.Cube);hazard.name="Necessary braking hazard";hazard.transform.position=padCenter+Vector3.forward*40+Vector3.up;hazard.transform.localScale=new Vector3(8,2,1);hazard.layer=9;
  yield return new WaitForSeconds(.9f);brakeBefore=m.brakeEvents;t.autoBrake=true;probe.Capture("necessary-braking");yield return new WaitForSeconds(2);
  Check(m.brakeEvents==brakeBefore,"necessary emergency braking remains exempt "+probe.Summary());probe.Capture("");Destroy(hazard);
  // The temporary service parking uses actual map geometry and colliders. A small path
  // follower generates manual steering only; H stays disabled for the entire physical run.
  var parking=session.jobs.stops[session.services.indices[2]];Place(parking.Kerb,parking.Direction);yield return Settle();m.Suppress(30);
  Check(!session.world.map.hybrid||session.world.map.mapId!="eungye"||session.services.indices[2]==0,"Eungye startup parking uses the broad temporary service stop index="+session.services.indices[2]);
  var graph=session.jobs.nav.hd;HdRoadGraph.RoutePath route=null;
  if(graph!=null)foreach(var stop in session.jobs.stops){if(stop==parking)continue;var candidate=graph.Route(parking.laneEdge,parking.laneAt,stop.laneEdge,stop.laneAt);if(candidate!=null&&candidate.path.length>60){route=candidate;break;}}
  Check(route!=null,"startup parking departure route exists width="+parking.width+" edge="+parking.laneEdge+" position="+parking.Kerb);
  if(route!=null){
   int contacts=probe.sideContacts;float time=0,at=0,maxError=0;bool grounded=true;
   while(time<24&&at<45){float error;at=route.path.ProjectRange(t.transform.position,Mathf.Max(0,at-2),Mathf.Min(route.path.length,at+8),out error);maxError=Mathf.Max(maxError,error);
    var target=route.path.Position(at+2.5f);var local=t.transform.InverseTransformPoint(target+Vector3.up*t.transform.position.y);
    float angle=Mathf.Atan2(2*2.6f*local.x,Mathf.Max(1,local.x*local.x+local.z*local.z))*Mathf.Rad2Deg;
    t.autoSteer=Mathf.Clamp(angle/DriveMath.SteerAngle(t.Speed*3.6f),-1,1);t.autoThrottle=t.Speed<2.5f?.8f:0;
    yield return new WaitForFixedUpdate();time+=Time.fixedDeltaTime;grounded&=t.transform.position.y<2;
   }
   t.autoThrottle=0;t.autoBrake=true;
   Check(at>=45&&probe.sideContacts==contacts&&maxError<2&&grounded,"startup parking manual-demand departure 45m at="+at+" seconds="+time+" pathError="+maxError+" sideContacts="+(probe.sideContacts-contacts)+" last="+probe.last);
  }
  Check(!session.assisted.Active,"H remained disabled throughout manual physical validation");
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"manual-handling-smoke.txt"),"success="+ok+"\n"+report);File.WriteAllText(Path.Combine(folder,"manual-handling-physics.csv"),probe.trace.ToString());Debug.Log(report.ToString());Application.Quit(ok?0:1);
 }
}
public class ManualHandlingContactProbe : MonoBehaviour {
 public int sideContacts;public string last="none";
 public DrivingManners manners;public float maxWindowAcceleration,maxRawAcceleration,maxFiltered,minFiltered,longestHarsh,injectAcceleration;int necessaryTicks,ticks;string phase="";float previousAt,previousSpeed;
 struct MotionSample {public float time,speed;}
 readonly System.Collections.Generic.List<MotionSample> samples=new System.Collections.Generic.List<MotionSample>();
 public readonly StringBuilder trace=new StringBuilder("phase,fixedTime,dt,speed,rawAcceleration,windowAcceleration,filtered,harshTime,suppressionRemaining,accelEvents,brakeEvents,necessaryBraking,mannerEnabled,reasons\n");
 readonly System.Reflection.FieldInfo filteredField=typeof(DrivingManners).GetField("filtered",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic),harshField=typeof(DrivingManners).GetField("harshTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic),suppressedField=typeof(DrivingManners).GetField("suppressed",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
 public void Capture(string name){phase=name;maxWindowAcceleration=maxRawAcceleration=maxFiltered=minFiltered=longestHarsh=0;necessaryTicks=ticks=0;samples.Clear();previousAt=Time.fixedTime;previousSpeed=GetComponent<TaxiController>().Speed;}
 public string Summary(){return "speed="+GetComponent<TaxiController>().Speed+" rawPeak="+maxRawAcceleration+" window300msPeak="+maxWindowAcceleration+" filtered="+minFiltered+".."+maxFiltered+" longestHarsh="+longestHarsh+" events="+manners.accelEvents+","+manners.brakeEvents+" necessaryTicks="+necessaryTicks+"/"+ticks+" enabled="+manners.enabled+" reasons="+manners.RideReasons;}
 void FixedUpdate(){if(injectAcceleration!=0)GetComponent<Rigidbody>().AddForce(transform.forward*injectAcceleration,ForceMode.Acceleration);if(phase==""||!manners)return;float now=Time.fixedTime,speed=GetComponent<TaxiController>().Speed,dt=now-previousAt;float raw=dt>0?(speed-previousSpeed)/dt:0;previousAt=now;previousSpeed=speed;maxRawAcceleration=Mathf.Max(maxRawAcceleration,raw);
  samples.Add(new MotionSample{time=now,speed=speed});while(samples.Count>1&&now-samples[1].time>=.3f)samples.RemoveAt(0);float average=samples.Count>1&&now-samples[0].time>=.28f?(speed-samples[0].speed)/(now-samples[0].time):0;maxWindowAcceleration=Mathf.Max(maxWindowAcceleration,average);
  float filtered=(float)filteredField.GetValue(manners),harsh=(float)harshField.GetValue(manners),suppression=(float)suppressedField.GetValue(manners)-Time.time;maxFiltered=Mathf.Max(maxFiltered,filtered);minFiltered=Mathf.Min(minFiltered,filtered);longestHarsh=Mathf.Max(longestHarsh,harsh);bool necessary=manners.NecessaryBraking();ticks++;if(necessary)necessaryTicks++;
  trace.AppendLine(phase+","+now+","+dt+","+speed+","+raw+","+average+","+filtered+","+harsh+","+suppression+","+manners.accelEvents+","+manners.brakeEvents+","+necessary+","+manners.enabled+","+manners.RideReasons.Replace(",",";"));
 }
 void OnCollisionEnter(Collision collision){if(collision.contactCount>0&&Mathf.Abs(collision.GetContact(0).normal.y)>.65f)return;sideContacts++;last=collision.collider.name;}
}
