using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Normal business scene and real passenger APIs; controlled poses isolate observation boundaries.
public class ServiceLawZonesSmokeTest : MonoBehaviour {
 string ReportPath {get{return System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-physicalService")>=0?"artifacts/service-law-physical-test.txt":"artifacts/service-law-zones-test.txt";}}
 public DriveSession session;bool ok=true;StringBuilder report=new StringBuilder();
 void Check(bool b,string n){ok&=b;report.AppendLine((b?"PASS ":"FAIL ")+n);File.WriteAllText(ReportPath,"success="+ok+"\n"+report);}
 void Pose(Vector3 p,Vector3 dir,float speed=0){var t=session.taxi;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(dir);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);t.body.velocity=dir*speed;Physics.SyncTransforms();}
 IEnumerator At(AssistedRoadNetwork.Path path,float at,float speed=3){var heading=path.Tangent(at);Pose(path.Position(at)-heading*2,heading,speed);yield return new WaitForFixedUpdate();yield return null;}
 IEnumerator Capture(string name){if(Application.isBatchMode)yield break;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/"+name+".png"));yield return new WaitForSecondsRealtime(.2f);}
 IEnumerator ViaBoundaries(ServiceLawZones zones){
  var loop=new AssistedRoadNetwork.Path(new[]{new Vector3(0,0,0),new Vector3(20,0,0),new Vector3(20,0,20),new Vector3(0,0,20),new Vector3(0,0,1),new Vector3(20,0,1)});float loopError;
  Check(loop.Project(new Vector3(10,0,.8f),out loopError)>70&&loop.ProjectRange(new Vector3(10,0,.8f),5,18,out loopError)<20,"route progress stays on first visit at overlapping junction");
  var z=zones.zones[0];var nav=session.jobs.nav;var incoming=nav.hd.paths[z.incoming];
  Pose(incoming.Position(incoming.length-60),incoming.Tangent(incoming.length-60));nav.SetHdDestination(z.outgoing,25);nav.ObserveEdge(z.connector);
  var path=nav.hdRoute.path;float end=nav.hdRoute.ends[nav.hdRoute.edges.IndexOf(z.connector)];
  Pose(path.Position(end-3),path.Tangent(end-3),20);yield return new WaitForFixedUpdate();yield return null;
  for(int i=0;i<15&&nav.ObservedEdge>=0;i++){float at=end-2.5f+i*.5f;Pose(path.Position(at),path.Tangent(at),20);yield return new WaitForFixedUpdate();yield return null;}
  Check(nav.PassedObservedEdge==z.connector,"72 km/h continuous crossing releases waypoint");
  Pose(incoming.Position(incoming.length-60),incoming.Tangent(incoming.length-60));nav.SetHdDestination(z.outgoing,25);nav.ObserveEdge(z.connector);path=nav.hdRoute.path;end=nav.hdRoute.ends[nav.hdRoute.edges.IndexOf(z.connector)];
  Pose(path.Position(end-5),path.Tangent(end-5));yield return new WaitForFixedUpdate();yield return null;
  Pose(path.Position(end+15),path.Tangent(end+15));yield return new WaitForFixedUpdate();yield return null;
  Check(nav.ObservedEdge==z.connector,"teleport across connector does not satisfy waypoint");nav.Clear();
 }
 IEnumerator ExtraCase(bool deferred){
  var zones=GetComponent<ServiceLawZones>();var jobs=session.jobs;jobs.ClearForDay();int offer=-1;
  var fixture=new JobOffer{fare=5000,seconds=2000,requiredScore=60};if(zones.ConfigureScenario(fixture,zones.scenarios.Find(x=>x.variant==8))){jobs.offers[0]=fixture;offer=0;}
  Check(offer>=0,"additional encounter offer");if(offer<0)yield break;
  Check(jobs.Accept(offer),"additional encounter accepted");var selected=jobs.current;var pickup=jobs.stops[selected.pickup];Pose(pickup.Kerb,pickup.Direction);Check(jobs.Interact(),"additional passenger boarded");string trip=session.law.tripId;int reds=session.manners.redEvents;
  var incoming=jobs.nav.hd.paths[zones.Assigned.incoming];float start=Mathf.Max(0,incoming.length-70);Pose(incoming.Position(start),incoming.Tangent(start));jobs.nav.ObserveEdge(zones.Assigned.connector);jobs.nav.Refresh();
  zones.enabled=!deferred;for(int i=0;i<5;i++)yield return null;
  var path=deferred?jobs.nav.hdRoute.path:zones.Live.Route.path;float end=deferred?jobs.nav.hdRoute.ends[jobs.nav.hdRoute.edges.IndexOf(zones.Assigned.connector)]:path.length;
  for(float at=0;at<end+4;at+=.5f)yield return At(path,Mathf.Min(path.length,at));
  zones.enabled=true;yield return null;
  var e=System.Array.Find(session.law.ledger.Events,x=>x.tripId==trip&&x.family=="confusion");
  Check(zones.Completed&&e!=null&&e.occupied&&e.rules[0].verdict==(deferred?LawVerdict.Deferred:LawVerdict.Violation),deferred?"unavailable observation persists as deferred":"red flashing violation persists during actual passenger ride");
  Check(e!=null&&e.Deduction==(deferred?0:15)&&(deferred||session.manners.redEvents==reds),"law judgment does not duplicate passenger red penalty");
  var drop=jobs.stops[selected.drop];Pose(drop.Kerb,drop.Direction);int before=jobs.balance;Check(jobs.Interact()&&jobs.balance-before==MannerLedger.Payment(selected.fare,session.manners.ledger.Score,selected.requiredScore),"law verdict does not alter ordinary manner fare formula");
 }
 IEnumerator Physical(){
  var jobs=session.jobs;var zones=GetComponent<ServiceLawZones>();JobOffer best=null;int wanted=0;foreach(var arg in System.Environment.GetCommandLineArgs())if(arg.StartsWith("-serviceVariant="))int.TryParse(arg.Substring(16),out wanted);
  for(int round=0;round<84;round++){jobs.Generate();foreach(var offer in jobs.offers){var z=zones.ZoneFor(offer);if(z!=null&&(wanted==0||z.variant==wanted)&&(best==null||offer.distance<best.distance))best=offer;}}
  Check(best!=null,"physical reachable offer");if(best!=null)report.AppendLine("SELECTED variant="+zones.ZoneFor(best).variant+" distance="+best.distance+" pickup="+best.pickup+" drop="+best.drop);if(best==null){Application.Quit(1);yield break;}
  jobs.offers[0]=best;var taxi=session.taxi;taxi.automated=true;session.assisted.allowAutomated=true;
  var pickup=jobs.stops[best.pickup];taxi.autoBrake=true;float safeUntil=Time.time+60;bool clear=false;
  while(Time.time<safeUntil){clear=true;foreach(var c in Physics.OverlapBox(pickup.Kerb+Vector3.up*.8f,new Vector3(1.2f,.7f,5),Quaternion.LookRotation(pickup.Direction),1<<9,QueryTriggerInteraction.Ignore))clear=false;if(clear)break;yield return new WaitForFixedUpdate();}
  Check(clear,"pickup road clear before physical test start");Pose(pickup.Kerb,pickup.Direction);yield return new WaitForSeconds(.8f);
  bool grounded=false,onCar=false;foreach(var wheel in taxi.wheels){WheelHit hit;if(wheel.GetGroundHit(out hit)){grounded=true;onCar|=hit.collider.gameObject.layer==9;}}
  Check(grounded&&!onCar,"taxi wheels on road, not another vehicle roof");Check(jobs.Accept(0)&&jobs.Interact(),"physical ordinary passenger boarding");
  float travel=0;foreach(var car in session.traffic.cars)travel+=car.totalTravel;
  Check(session.traffic.cars.Count>=24,"24 ambient vehicles enabled");int cash=session.business.ledger.Account.Cash;string trip=session.law.tripId;
  var driver=gameObject.AddComponent<LiveTrafficValidationTest>();driver.enabled=false;driver.session=session;driver.cruise=9;
  Time.timeScale=4;yield return driver.Drive("normal passenger via law zone");
  Check(driver.Arrived&&zones.Completed,"physical arrival after situation");Check(driver.Passed,"physical collision, signal, law and braking checks");
  Check(jobs.trip.State==TripState.OnTrip&&jobs.Interact()&&session.business.ledger.Account.Cash>cash,"physical passenger fare credited");
  float after=0;foreach(var car in session.traffic.cars)after+=car.totalTravel;Check(after>travel+100,"ambient vehicles traveled during passenger ride");
  var e=System.Array.Find(session.law.ledger.Events,x=>x.tripId==trip&&x.family=="confusion");Check(e!=null&&e.occupied,"physical encounter in passenger history");
  yield return Capture("service-law-physical");Application.Quit(ok?0:1);
 }
 void Update(){if(Time.realtimeSinceStartup>360){Check(false,"watchdog expired");Application.Quit(1);}}
 IEnumerator Start(){
  Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=120;
  yield return new WaitForSecondsRealtime(1.5f);var zones=GetComponent<ServiceLawZones>();var jobs=session.jobs;var taxi=session.taxi;session.business.Depart();if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-physicalService")>=0){yield return Physical();yield break;}taxi.automated=true;taxi.enabled=false;taxi.body.useGravity=false;taxi.body.detectCollisions=false;
  Check(!PracticeSession.Enabled&&session.world.map.mapId=="eungye"&&zones.Ready,"normal Eungye business scene, no practice process");Check(zones.zones.Count==7,"seven distinct Eungye places registered");
  var unique=new System.Collections.Generic.HashSet<string>();foreach(var z in zones.zones)unique.Add(z.junction.name);Check(unique.Count==zones.zones.Count,"separate junction spaces");foreach(var z in zones.zones)report.AppendLine("ZONE "+z.variant+" "+z.junction.name+" "+z.junction.center+" connector="+z.connector);
  int map=session.world.GetInstanceID(),vehicle=taxi.GetInstanceID();var account=session.business.ledger.Account;int cash=account.Cash;
  // Keep ambient cars allocated but isolate these controlled boundary fixtures. Physical ambient drives are separate.
  foreach(var car in session.traffic.cars){car.Recycle();car.enabled=false;}
  yield return ViaBoundaries(zones);
  for(int variant=8;variant<=14;variant++){
   jobs.ClearForDay();int offer=-1;
   var course=zones.scenarios.Find(x=>x.variant==variant);var fixture=new JobOffer{fare=5000,seconds=2000,requiredScore=60};if(zones.ConfigureScenario(fixture,course)){jobs.offers[0]=fixture;offer=0;}
   Check(offer>=0,"reachable passenger offer for condition "+variant);if(offer<0)continue;
   Check(jobs.Accept(offer),"accept normal call "+variant);var selected=jobs.current;var pickup=jobs.stops[selected.pickup];Pose(pickup.Kerb,pickup.Direction);var before=taxi.transform.position;Check(jobs.Interact()&&jobs.trip.State==TripState.OnTrip,"board passenger "+variant);
   Check(Vector3.Distance(before,taxi.transform.position)<.01f&&map==session.world.GetInstanceID()&&vehicle==taxi.GetInstanceID(),"boarding does not teleport or replace map/taxi "+variant);
   string trip=session.law.tripId;Check(zones.Assigned!=null&&jobs.nav.RequiredVia==-1&&jobs.nav.ObservedEdge==zones.Assigned.connector&&jobs.nav.hdRoute.edges.Contains(zones.Assigned.connector),"navigation crosses assigned situation "+variant);
   int connector=zones.Assigned.connector;jobs.nav.TogglePreference();Check(jobs.nav.hdRoute!=null&&jobs.nav.RequiredVia==-1,"route preference has no forced situation "+variant);
   var drop=jobs.stops[selected.drop];Pose(pickup.Kerb,pickup.Direction);
   var incoming=jobs.nav.hd.paths[zones.Assigned.incoming];float approachAt=Mathf.Max(0,incoming.length-70);Pose(incoming.Position(approachAt),incoming.Tangent(approachAt));jobs.nav.Refresh();
   for(int tick=0;tick<10&&!zones.Live.Active;tick++)yield return null;
   Check(zones.Live.Active,"situation activates in existing scene "+variant);if(!zones.Live.Active)continue;
   var f=zones.Live;var state=f.State;var path=f.Route.path;float error;float line=path.Project(f.Junction.center+f.Direction*f.StopAlong,out error);Time.timeScale=3;
   // Prove a distant approach does not consume the encounter before the player arrives.
   Check((variant!=10||state.Blocked)&&(variant!=13||state.Oncoming),"hazard visible before decision point "+variant);
   if(variant==8||variant==10||variant==12||variant==13){yield return At(path,line-1,0);float until=Time.time+(variant==10?7:variant==12?5:variant==13?14:.2f);while(Time.time<until)yield return At(path,line-1,0);float wait=Time.time+40;while((state.Blocked||state.Oncoming)&&Time.time<wait)yield return At(path,line-1,0);}
   if(variant==10)yield return Capture("service-zone-occupied-taxi");
   for(float at=line-.5f;at<path.length&&!zones.Completed;at+=.5f)yield return At(path,at);
   yield return null;
   Check(zones.Completed&&state.Finished&&state.CorrectRoute&&zones.LastFeedback!=null,"transport encounter completes with feedback "+variant);
   Check(!session.paused&&Time.timeScale>0&&jobs.trip.State==TripState.OnTrip&&session.manners.enabled,"transport/time/manner remain active after encounter "+variant);
   var e=System.Array.Find(session.law.ledger.Events,x=>x.tripId==trip&&x.family=="confusion");Check(e!=null&&e.occupied&&e.Deduction==0,"independent law history linked to passenger ride "+variant);
   int balance=jobs.balance;Pose(drop.Kerb,drop.Direction);Check(jobs.Interact()&&jobs.balance>balance,"drop and receive normal fare "+variant);int paid=jobs.balance;Check(!jobs.Interact()&&jobs.balance==paid,"no duplicate fare "+variant);
   if(variant==14){zones.ToggleDetails();yield return Capture("service-law-result");zones.ToggleDetails();}
  }
  yield return ExtraCase(false);yield return ExtraCase(true);
  Check(account.Cash>cash,"fares actually reach campaign cash account");Check(!GetComponent<PracticeSession>().Busy&&map==session.world.GetInstanceID(),"no isolated practice launched across transport tests");
  report.AppendLine("Controlled poses for 7 normal passenger trips. Ambient traffic isolated in these boundary cases; not manual or full-route wheel driving.");File.WriteAllText(ReportPath,"success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
