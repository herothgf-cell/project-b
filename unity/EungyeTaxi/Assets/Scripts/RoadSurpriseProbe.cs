using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Road surprises, first batch, player acceptance (Eungye single, 2026-11-24). Real physics in the built player;
// the probe drives the taxi by setting its velocity.
// A) parked car, door: gap, then the door swings at the trigger distance; the taxi brakes (required braking is
//    recognised), waits, passes after the door closes -> "avoided", a card, no accident or ledger change.
// B) cut-in: an ordinary car one lane over signals, then glides onto the taxi's lane as a surprise vehicle;
//    the taxi eases off -> "gave space", a card, no ledger change.
// C) parked car, door: the taxi keeps going into the open door -> a vehicle accident at shared fault.
public sealed class RoadSurpriseProbe : MonoBehaviour {
 const string Flag="-surpriseCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Road surprise acceptance").AddComponent<RoadSurpriseProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("ROADSURPRISE_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Info(string line){report.AppendLine("INFO "+line);Debug.Log("ROADSURPRISE_PLAYER INFO "+line);}
 void Pose(Vector3 p,Vector3 heading){var t=s.taxi;t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(heading);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();}
 string Money(){var l=s.business.ledger;return l.Account.Expense(l.Day,"accident")+"|"+l.Account.Expense(l.Day,"repair")+"|"+l.Account.Expense(l.Day,"tow")+"|"+l.Account.Cash+"|"+s.law.ledger.Count+"|"+s.law.ledger.TotalScore+"|"+l.Care.Damage.Durability;}
 void Drive(Vector3 dir,float speed){var v=dir*speed;v.y=s.taxi.body.velocity.y;s.taxi.body.velocity=v;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.rotation=Quaternion.LookRotation(dir);}
 bool Feed(string text){var n=s.GetComponent<DriveNotifications>();return n&&n.Feed.Any(line=>line.Contains(text));}
 static bool Ordinary(HdEdge e){return e.change==0&&e.turn==0&&!e.TraversalDisabled&&!e.id.StartsWith("join:");}
 // A lane pose from which a door spot is found ahead; returns the lane point beside the spot.
 bool DoorScene(int skip,out Vector3 start,out Vector3 spot,out Vector3 dir){
  var g=s.jobs.nav.hd;start=spot=dir=Vector3.zero;int found=0;
  for(int i=0;i<g.edges.Length;i++){if(!Ordinary(g.edges[i])||g.paths[i].length<90)continue;
   var p=g.paths[i].Position(8);var d=g.paths[i].Tangent(8);d.y=0;d.Normalize();Pose(p,d);
   Vector3 q,qd;if(!s.surprises.FindDoorSpot(40,70,out q,out qd))continue;
   var lane=q-TrafficSystem.Right(qd)*2.35f;var toward=lane-p;toward.y=0;
   // Straight from the pose to the spot, so a straight drive stays in the lane.
   if(Vector3.Angle(toward,d)>3||Vector3.Angle(qd,d)>4)continue;
   if(found++<skip)continue;
   start=p;spot=q;dir=toward.normalized;Pose(start,dir);
   Info("door scene on edge "+g.edges[i].id+" ("+g.edges[i].roadName+") spot "+q.ToString("F1"));return true;}
  return false;
 }
 // Two parallel same-direction lanes on a long straight, far from junctions.
 bool CutScene(out int lane,out int other,out float at,out float otherAt){
  var g=s.jobs.nav.hd;lane=other=-1;at=otherAt=0;
  int tried=0;
  for(int i=0;i<g.edges.Length;i++){if(!Ordinary(g.edges[i])||g.paths[i].length<90)continue;
   float a=25;var p=g.paths[i].Position(a);var d=g.paths[i].Tangent(a);d.y=0;d.Normalize();
   if(Vector3.Angle(g.paths[i].Tangent(10),g.paths[i].Tangent(75))>5)continue;
   bool near=false;foreach(var j in s.world.map.targets)if(Vector3.Distance(p,j.V())<35||Vector3.Distance(p+d*45,j.V())<35)near=true;if(near)continue;
   tried++;
   foreach(int side in new[]{-1,1}){var q=p+TrafficSystem.Right(d)*3.4f*side;float qa;int k=g.Nearest(q,d,out qa);if(k<0||k==i||!Ordinary(g.edges[k]))continue;
    float err;g.paths[k].Project(q,out err);if(err>1f||g.paths[k].length-qa<60)continue;
    lane=i;other=k;at=a;otherAt=qa;Info("cut-in scene: taxi on "+g.edges[i].id+" ("+g.edges[i].roadName+"), car on "+g.edges[k].id+" side "+side+" after "+tried+" straights");return true;}
  }
  Info("cut-in scene: "+tried+" straights tried");
  return false;
 }
 IEnumerator Start(){
  float limitTime=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limitTime){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-surpriseOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/surprise");Directory.CreateDirectory(output);
  yield return null;string error=null;var b=s.business;var sp=s.surprises;var wait=new WaitForFixedUpdate();
  Vector3 start=Vector3.zero,spot=Vector3.zero,dir=Vector3.forward;TrafficCar car=null;string before=null;
  try{
   b.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();s.jobs.nav.Clear();
   Check(sp,"the road surprise component is attached to the Eungye single session");
   Check(s.damage.FaultRules,"fault rules are on");
   Check(sp.BlockedNow(SurpriseRules.Door)!="","no automatic surprise right after startup ("+sp.BlockedNow(SurpriseRules.Door)+")");
   car=FindObjectsOfType<TrafficCar>().FirstOrDefault(c=>c.isActiveAndEnabled&&c.Visible&&c.district!=null);Check(car,"an ordinary car is available");
   foreach(var rest in FindObjectsOfType<TrafficCar>())if(rest!=car)rest.gameObject.SetActive(false);
   car.Recycle();car.respawn=false;
   Check(DoorScene(0,out start,out spot,out dir),"a kerb-side spot for a parked car is found on the map");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}

  // A) Door: approach at 9 m/s, brake when it opens, wait for it to close, pass.
  sp.SpawnDoor(spot,dir);yield return new WaitForSeconds(.5f);before=Money();int avoided=sp.Avoided;
  float openedAlong=-1;bool ajarSeen=false,required=false;
  {float until=Time.time+12;while(Time.time<until&&!sp.DoorOpen){Drive(dir,9);var hinge=sp.DoorCar.transform.Find("운전석 문 경첩");ajarSeen|=hinge&&Mathf.DeltaAngle(0,hinge.localEulerAngles.y)>5;yield return wait;}
   openedAlong=Vector3.Dot(spot-s.taxi.transform.position,dir);}
  try{
   Check(sp.DoorOpen,"the door opens as the taxi approaches");
   Check(ajarSeen,"the door gap shows before it swings");
   Check(openedAlong>=16&&openedAlong<=24,"it swings about 18 m ahead (centre distance "+openedAlong.ToString("0.0")+" m)");
   Check(sp.DoorActor&&sp.DoorActor.startedAt<=Time.time,"the door is marked as a surprise from the moment it swings");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"door-open.png"));
  try{var hinge=sp.DoorCar.transform.Find("운전석 문 경첩");var tip=hinge.TransformPoint(new Vector3(-.03f,.85f,-1.1f));var right=TrafficSystem.Right(dir);
   float intrude=Vector3.Dot(tip-(spot-right*2.35f),right);Info("open door tip "+intrude.ToString("0.00")+" m right of the lane centre");
   Check(intrude<.9f&&intrude>-.2f,"the open door reaches into the taxi's path (taxi body edge at 0.9 m)");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  {var t=s.taxi.transform;var hits=Physics.SphereCastAll(t.position+Vector3.up*.65f,.8f,t.forward,Mathf.Max(5,s.taxi.body.velocity.magnitude*2+4),(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);
   Info("brake sensor from "+t.position.ToString("F2")+" fwd "+t.forward.ToString("F2")+" gap "+Vector3.Dot(spot-t.position,dir).ToString("0.0")+" hits: "+string.Join(", ",hits.Select(h=>h.collider.name+"@"+h.distance.ToString("0.0")+" layer "+h.collider.gameObject.layer)));
   var panel=sp.DoorCar.GetComponentsInChildren<Collider>().Select(c=>c.name+" "+c.bounds.center.ToString("F2")+" "+c.bounds.size.ToString("F2")+" layer "+c.gameObject.layer+" enabled "+c.enabled);Info("door colliders: "+string.Join(" | ",panel));}
  {float v=9;while(v>0){v=Mathf.Max(0,v-6*Time.fixedDeltaTime);Drive(dir,v);required|=s.manners.NecessaryBraking();yield return wait;}}
  float stoppedAlong=Vector3.Dot(spot-s.taxi.transform.position,dir);
  yield return new WaitForSeconds(SurpriseRules.DoorOpenSeconds+1.5f);
  {float until=Time.time+10;while(Time.time<until&&sp.Avoided==avoided){Drive(dir,6);yield return wait;}Drive(dir,0);}
  yield return new WaitForSeconds(.5f);
  try{
   Check(required,"braking for the open door counts as required braking");
   Info("stopped "+stoppedAlong.ToString("0.0")+" m behind the parked car centre");
   Check(sp.Avoided==avoided+1&&sp.LastOutcome=="피함","passing after it closed is an avoided surprise ("+sp.LastOutcome+")");
   Check(Feed("문 열림을 피했어요"),"a card tells the driver");
   Check(!s.accident.Locked&&Money()==before,"no accident, fee, repair, law record, score or durability change ("+before+" -> "+Money()+")");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(3.5f);

  // B) Cut-in: the car starts 14 m ahead one lane over; the taxi holds 10 m/s, then eases to 5 m/s.
  int lane=-1,other=-1;float at=0,otherAt=0;
  try{Check(CutScene(out lane,out other,out at,out otherAt),"a straight with two lanes the same way is found");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  var g=s.jobs.nav.hd;var laneDir=g.paths[lane].Tangent(at);laneDir.y=0;laneDir.Normalize();Pose(g.paths[lane].Position(at),laneDir);
  TrafficRoute route=null;foreach(var stop in s.world.map.stops){route=car.district.CreateRoute(other,otherAt,stop.laneEdge,stop.laneAt);if(route!=null&&route.length>80&&!route.stops.Any(x=>x.distance<60))break;route=null;}
  try{Check(route!=null,"the car gets a route along the next lane");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  {float until=Time.time+25;while(sp.Busy&&Time.time<until)yield return null;}
  car.route=route;car.Place(14);car.speed=8;yield return wait;
  before=Money();avoided=sp.Avoided;bool started=sp.TryCutIn(true);int side=0;bool signalled=false,marked=false;float signalSince=Time.time;float closest=99;
  try{Check(started&&sp.CutInCar==car,"the car is chosen for the cut-in ("+(started?"":sp.CutInReject)+")");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  side=car.CurrentTurn;
  while(!sp.CutInMoving&&Time.time-signalSince<3){signalled|=car.CurrentTurn!=0;Drive(laneDir,10);yield return wait;}
  float signalSeconds=Time.time-signalSince;bool captured=false;
  {float until=Time.time+SurpriseRules.CutInMergeSeconds+.6f;while(Time.time<until&&sp.CutInMoving){marked|=car.GetComponent<FaultActor>();Drive(laneDir,Mathf.MoveTowards(s.taxi.body.velocity.magnitude,5,4*Time.fixedDeltaTime));if(!captured&&car.CutInRemaining<2){captured=true;ScreenCapture.CaptureScreenshot(Path.Combine(output,"cut-in.png"));}yield return wait;}}
  float err;g.paths[lane].Project(car.transform.position,out err);closest=err;
  {float until=Time.time+5;while(Time.time<until&&sp.Avoided==avoided){Drive(laneDir,5);yield return wait;}Drive(laneDir,0);}
  yield return new WaitForSeconds(.5f);
  try{
   Check(signalled&&side!=0,"the indicator blinks before the move (side "+side+")");
   Check(signalSeconds>=SurpriseRules.CutInSignalSeconds-.05f&&signalSeconds<SurpriseRules.CutInSignalSeconds+.3f,"it moves "+signalSeconds.ToString("0.00")+" s after the indicator");
   Check(sp.CutInMoving||marked,"the car is a surprise vehicle while it moves across");
   Check(closest<.6f,"it ends on the taxi's lane centre ("+closest.ToString("0.00")+" m off)");
   Check(sp.Avoided==avoided+1&&sp.LastOutcome=="간격 줌","easing off is counted as giving space ("+sp.LastOutcome+")");
   Check(Feed("끼어든 차에 간격을 줬어요"),"a card tells the driver");
   Check(!s.accident.Locked&&Money()==before,"no accident or ledger change ("+before+" -> "+Money()+")");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(3.5f);
  try{Check(!car.GetComponent<FaultActor>()&&car.CurrentTurn==0||!car.GetComponent<FaultActor>(),"the surprise mark is removed afterwards");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  car.Recycle();

  // C) Door again, the taxi does not react.
  try{Check(DoorScene(1,out start,out spot,out dir)||DoorScene(0,out start,out spot,out dir),"a second door scene");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  sp.SpawnDoor(spot,dir);yield return new WaitForSeconds(.5f);int touched=sp.Touched;
  {float until=Time.time+12;while(Time.time<until&&!s.accident.Locked&&sp.Touched==touched){Drive(dir,9);yield return wait;}}
  float settle=Time.realtimeSinceStartup+8;while(s.accident.State!=null&&s.accident.State.phase==AccidentPhase.Stopping&&Time.realtimeSinceStartup<settle)yield return null;
  yield return new WaitForSecondsRealtime(.5f); // the accident screen may pause game time
  try{
   Check(sp.Touched==touched+1&&sp.LastOutcome=="접촉","driving into the open door is recorded as a contact");
   Info("fault reason: "+s.damage.LastFaultReason);
   Check(s.damage.LastFaultReason.StartsWith("돌발"),"the contact was judged as a surprise ("+s.damage.LastFaultReason+")");
   Check(s.accident.Locked&&s.accident.State.Kind==AccidentKind.Vehicle,"a vehicle accident opens (door open well before the touch)");
   Check(s.accident.State.Shared,"at shared fault: the door opened more than the reaction time before");
  }catch(Exception e){error=e.ToString();}
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"door-contact.png"));yield return null;
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("ROADSURPRISE_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
