using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Real Rigidbody/steering inputs, real fare formula and 480-second shifts.
// Ambient cars are disabled to isolate campaign/navigation from traffic congestion.
public class CampaignDriveSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true,arrived;bool lawful;string observedEntry="";readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string text){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+text);Debug.Log(text+" = "+value);Flush();}
 void Flush(){File.WriteAllText(Path.GetFullPath(lawful?"artifacts/campaign-lawful-drive-test.txt":"artifacts/campaign-drive-test.txt"),"success="+ok+"\n"+report);}
 void Place(MapStop stop){var t=session.taxi;t.body.velocity=t.body.angularVelocity=Vector3.zero;t.body.position=stop.Kerb+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(stop.Direction);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();session.law.ResetObservation();session.manners.Suppress(1);}
 IEnumerator Drive(){
  var taxi=session.taxi;var nav=session.jobs.nav;session.assisted.SetActive(true);arrived=false;float start=Time.time,maxError=0;
  while(Time.time-start<600&&session.jobs.trip.State==TripState.OnTrip){
   var observed=typeof(DrivingLaw).GetField("entry",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(session.law);
   if(observed!=null){var type=observed.GetType();string id=(string)type.GetField("id").GetValue(observed);if(id!=observedEntry){observedEntry=id;var direction=(Vector3)type.GetField("direction").GetValue(observed);var junction=(TrafficJunction)type.GetField("junction").GetValue(observed);var state=(RoadObservation)type.GetField("observed").GetValue(observed);report.AppendLine("ENTRY "+junction.name+" signal="+state.forward+" alignment="+Vector3.Dot(taxi.transform.forward,direction).ToString("F3")+" speed="+taxi.Speed+" time="+session.business.ledger.Elapsed);Flush();}}
   var route=nav.hdRoute;if(route==null)break;float error;float at=route.path.Project(taxi.transform.position,out error);maxError=Mathf.Max(error,maxError);
   float remaining=route.path.length-at;var edge=nav.hd.edges[route.EdgeAt(at)];float bend=Vector3.Angle(route.path.Tangent(at),route.path.Tangent(Mathf.Min(route.path.length,at+14)));
   float speed=remaining<10?1.2f:bend>12||edge.turn!=0||edge.status=="provisional-portal"?3.5f:9;
   if(lawful)foreach(var j in session.traffic.junctions){if(!j.signalized)continue;for(int axis=0;axis<2;axis++)foreach(int sign in new[]{-1,1}){var direction=session.traffic.AxisDirection(j,axis)*sign;if(Vector3.Dot(taxi.transform.forward,direction)<.8f)continue;var front=taxi.transform.position+taxi.transform.forward*2;float lateral=Vector3.Dot(front-j.center,TrafficSystem.Right(direction));float gap=Vector3.Dot(j.center-direction*j.StopDistance(axis)-front,direction);if(lateral<=.15f||lateral>=j.halfWidth[axis]||gap<-.3f||gap>45)continue;if(session.traffic.Signal(j,axis,sign)!=SignalColor.Green)speed=Mathf.Min(speed,Mathf.Sqrt(4*Mathf.Max(0,gap-1.5f)));}}
   taxi.autoThrottle=taxi.Speed<speed?(lawful?.22f:.4f):0;taxi.autoBrake=taxi.Speed>speed+.3f;
   if(remaining<1.8f){taxi.autoThrottle=0;taxi.autoBrake=true;if(taxi.body.velocity.magnitude<.56f){arrived=true;break;}}
   if(!session.assisted.Active&&remaining>2.5f)break;
   yield return null;
  }
  taxi.autoThrottle=0;taxi.autoBrake=true;session.assisted.SetActive(false);
  Check(arrived&&maxError<1.8f,"physical arrival seconds="+(Time.time-start).ToString("F1")+" maxError="+maxError.ToString("F2")+" remaining="+session.jobs.trip.Remaining.ToString("F1"));
 }
 IEnumerator Start(){
  yield return new WaitForSeconds(1);lawful=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-lawfulDrive")>=0;session.jobs.nav.PreferFastest=!lawful;Time.timeScale=8;var b=session.business;var jobs=session.jobs;session.taxi.automated=true;session.taxi.autoBrake=true;session.assisted.allowAutomated=true;session.roadConditions.enabled=false;
  foreach(var c in session.traffic.cars){c.Recycle();c.enabled=false;}
  Check(b.ledger.Account!=null&&b.ledger.Settings.days==3&&b.ledger.Settings.daySeconds==480,"unmodified campaign settings");
  for(int day=1;day<=3;day++){
   Place(jobs.stops[0]);yield return new WaitForSeconds(.6f);
   for(int ride=0;ride<2;ride++){
    Check(jobs.Interact()&&jobs.trip.State==TripState.OnTrip,"roadside boarding day="+day+" ride="+ride);
    if(jobs.trip.State!=TripState.OnTrip)break;
    session.requests.request.DeferSafety();int cash=b.ledger.Account.Cash;yield return Drive();
    if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-lawfulProbe")>=0){report.AppendLine("PROBE DONE");Flush();Application.Quit(0);yield break;}
    Check(arrived&&jobs.Interact()&&b.ledger.Account.Cash>cash,"normal fare credited day="+day+" ride="+ride);
    if(!arrived)break;
    float resultEnd=Time.time+4.2f;while(Time.time<resultEnd&&!b.ShowingSummary&&!b.taxPrompt)yield return null;
    if(ride==0&&b.ledger.CanAccept){for(int fill=0;fill<2&&b.ledger.Account.Fuel<99;fill++){session.services.Toggle();bool bought=session.services.Buy(0);Check(bought,"visit fuel station day="+day+" purchase="+fill);if(!bought)break;Time.timeScale=8;}}
    if(!b.ledger.CanAccept)break;
   }
   if(day==2)Check(b.ledger.Account.Renew(session.law.ids.Next("insurance"),day),"manual insurance renewal before third day");
   if(day==3)Check(b.ledger.Account.PayTax(session.law.ids.Next("tax"),day),"manual tax payment on due day");
   if(session.services.open)session.services.Toggle();
   // Let the actual game clock reach closing; no Advance() or injected fare.
   float timeout=Time.realtimeSinceStartup+120;
   while(!b.ShowingSummary&&Time.realtimeSinceStartup<timeout){if(b.taxPrompt)b.ConfirmTaxClose(false);yield return new WaitForSecondsRealtime(.1f);}
   Check(b.ShowingSummary&&b.ledger.Elapsed>=480,"automatic 480-second settlement day="+day);
   Check(b.SaveCheckpoint(),"checkpoint day="+day);
   report.AppendLine("DAY "+day+" income="+b.ledger.Income+" net="+b.ledger.TotalNet+" fuel="+b.ledger.Account.Fuel+" rides="+b.ledger.Rides+" failures="+b.ledger.FailedTrips+" law="+session.law.ledger.Score(day));Flush();
   if(day<3){if(!b.NextDay()){Check(false,"next day "+day);break;}Time.timeScale=8;}
  }
  Check(b.ledger.Day==3&&b.ledger.ShowingSummary,"three-day campaign reached final result");
  Check(b.ledger.State==(b.ledger.Account.GoalMet?BusinessState.Won:BusinessState.Lost),"final result matches actual net and payment conditions");
  if(lawful)Check(b.ledger.State==BusinessState.Won,"signal-aware campaign reaches goal");
  report.AppendLine(lawful?"DONE: automated physics with signal braking, no ambient traffic. Inspect law/manner records before interpreting compliance; not manual play.":"DONE: automated physics, no ambient traffic, no signal-aware braking. Not a lawful/manual campaign claim.");Flush();Application.Quit(ok?0:1);
 }
}
