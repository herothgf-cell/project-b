using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Continuous physical day: no position changes, account injection, or disabled ambient traffic.
public sealed class OneDayTrafficDriveTest:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-oneDayTrafficTest")>=0)new GameObject("Continuous day traffic validation").AddComponent<OneDayTrafficDriveTest>();}
 DriveSession s;LiveTrafficValidationTest driver;bool ok=true,halt,finished,dayReady;float startReal,nextSample;readonly StringBuilder report=new StringBuilder();int meals,sleeps,fills,rides;float npcStart;
 bool firstFare, historicRoute;
 bool measuringGaragePath, crossedGarageBoundary, previousInside;
 Vector3 previousGaragePosition, garageDoor;
 float garageMetres, largestGarageStep, boundaryDoorDistance, nextGarageSample;
 AssistedRoadNetwork.Path garageTracePath;
 void Log(string text){report.AppendLine(text);Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/one-day-traffic-drive.txt",report.ToString());}
 void Check(bool pass,string text){ok&=pass;Log((pass?"PASS ":"FAIL ")+text);}
 float NpcDistance(){float d=0;foreach(var c in s.traffic.cars)d+=c.totalTravel;return d;}
 void Update(){if(!s||!s.StartupReady||finished)return;if(s.business.ShowingSummary&&!dayReady){Check(false,"day settled before completing transport and all facilities");Finish();return;}var choice=s.GetComponent<ScenarioChoiceSituation>();if(choice&&choice.Open){Log("CHOICE "+choice.Course.id+" wait/observe");choice.Select(0);Time.timeScale=4;}if(!firstFare&&!historicRoute&&s.requests&&!s.requests.request.Resolved)s.requests.request.DeferSafety();
  if(Time.realtimeSinceStartup>=nextSample){nextSample=Time.realtimeSinceStartup+10;var l=s.business.ledger;Log("SAMPLE elapsed="+l.Elapsed.ToString("F1")+" metres="+l.Metres.ToString("F1")+" rides="+rides+" cash="+l.Account.Cash+" net="+l.ProjectedNet+" fuel="+l.Account.Fuel.ToString("F1")+" satiety="+l.Driver.satiety.ToString("F1")+" fatigue="+l.Driver.fatigue.ToString("F1")+" npc="+NpcDistance().ToString("F0")+" pos="+s.taxi.transform.position+" trip="+s.jobs.trip.State);}
  if(Time.realtimeSinceStartup-startReal>1200){Check(false,"real-time watchdog");Finish();}}
 IEnumerator Leg(string name,float distanceLimit=0){
  Log("LEG "+name+" at="+s.business.ledger.Elapsed.ToString("F1"));
  yield return driver.Drive(name,distanceLimit);
  // A scored driving mistake still permits a driver to return home and settle the fare.
  // Keep the independent driving audit in live-traffic-validation.txt and report its result.
  if(firstFare)Log("DRIVING_AUDIT leg="+name+" passed="+driver.Passed+" redEvents="+s.manners.redEvents);
  else Check(driver.Passed,"physical safety checks "+name);
  if(!driver.Arrived){Check(false,"blocked leg "+name);halt=true;yield break;}
  Log("ARRIVED "+name+" at="+s.business.ledger.Elapsed.ToString("F1"));
  if(!firstFare&&!driver.Passed)halt=true;
 }
 // Navigation ends in the travel lane. A separate pedal/steering manoeuvre
 // follows the fictional pocket entrance, just as the player releases H.
 IEnumerator Pocket(MapStop stop,bool leave){
  var lane=s.jobs.nav.hd.paths[stop.laneEdge];var points=new List<Vector3>();Vector3 a=s.taxi.transform.position;a.y=stop.Kerb.y;
  Vector3 d=leave?lane.Position(stop.laneAt+10):stop.Kerb;
  Vector3 b=a+stop.Direction*4,c=d-stop.Direction*4;
  for(int i=0;i<=30;i++){float t=i/30f,u=1-t;points.Add(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);}
  var probe=gameObject.AddComponent<FuelStationProbe>();probe.externallyDriven=true;probe.session=s;
  yield return probe.Drive(points,leave?"passenger pocket exit":"passenger pocket entry");Check(probe.Passed,"passenger pocket manoeuvre "+(leave?"exit":"entry"));if(!probe.Passed)halt=true;Destroy(probe);
 }
 IEnumerator PassengerApproach(int index,string label){
  var stop=s.jobs.EffectiveStop(index);bool pocket=s.jobs.Catalog.IsAdditional(index);
  if(s.jobs.At(index)){Log("ARRIVED "+label+" already at legal curb");yield break;}
  if(pocket)s.jobs.nav.SetHdDestination(stop.laneEdge,stop.laneAt-10);
  yield return Leg(label);if(halt)yield break;
  if(pocket)yield return Pocket(stop,false);
 }
 public static List<Vector3> RestaurantDeparture(RestaurantSiteLayout.Site site,HdRoadGraph graph,Vector3 start){
  var points=RoadNavigation.FacilityDepartureToRoad(site.leave,site.approach,site.direction,graph);
  points.Insert(0,start);return points;
 }
 IEnumerator Ride(string preferred=null){var jobs=s.jobs;var zones=s.GetComponent<ServiceLawZones>();if(!firstFare)jobs.Generate();int chosen=firstFare?0:-1;
  if(historicRoute){
   if(jobs.stops.Length<=38){Check(false,"historical pickup/drop missing");halt=true;yield break;}
   var offer=new JobOffer{pickup=30,drop=38,distance=jobs.nav.Distance(jobs.stops[30],jobs.stops[38]),
    fare=6600,requiredScore=85,seconds=145.600006f,safeRouteSeconds=114.04261f,timingPolicyVersion=1,
    urgency=2,candidateSeed="04f6bd5ac5434cd8ac3b44edf7abb827:1:1:30:38:"};
   jobs.offers[0]=offer;chosen=0;
   Log("REPLAY original r47 30->38 offer seed, urgency, fare and passenger deadline; fresh traffic");
  }float best=float.MaxValue;
  if(preferred!=null){var course=zones.scenarios.Find(x=>x.id==preferred);var offer=new JobOffer();if(zones.ConfigureScenario(offer,course)){offer.fare=3000+Mathf.CeilToInt(offer.distance*4/100)*100;offer.seconds=Mathf.Max(120,Mathf.Ceil(offer.distance/4+90+offer.waitSeconds));offer.requiredScore=70;jobs.offers[0]=offer;chosen=0;}}
  if(chosen<0)for(int i=0;i<jobs.offers.Length;i++){var offer=jobs.offers[i];if(offer==null)continue;float at;int edge=jobs.nav.hd.Nearest(s.taxi.transform.position,s.taxi.transform.forward,out at);var stop=jobs.stops[offer.pickup];var r=jobs.nav.hd.Route(edge,at,stop.laneEdge,stop.laneAt);float length=(r==null?float.MaxValue:r.path.length)+offer.distance;if(length<best){best=length;chosen=i;}}
  Check(chosen>=0&&jobs.Accept(chosen),"accept slot="+chosen+" message="+jobs.message);if(jobs.trip.State!=TripState.ToPickup){halt=true;yield break;}Log("OFFER "+jobs.current.scenarioId+" pickup="+jobs.current.pickup+" drop="+jobs.current.drop+" distance="+jobs.current.distance+" fare="+jobs.current.fare);
  if(firstFare){yield return DriveGarage(true);if(halt)yield break;jobs.ResumePickupNavigation();}
  driver.legTimeout=Mathf.Max(600,jobs.trip.PickupRemaining+5);yield return PassengerApproach(jobs.current.pickup,"pickup");driver.legTimeout=600;if(halt)yield break;int pickup=jobs.current.pickup;bool board=jobs.Interact();Log("BOARD F accepted="+board+" state="+jobs.trip.State+" message="+jobs.message);
  for(int i=0;i<45&&board&&jobs.trip.State==TripState.ToPickup&&jobs.Behavior!=null;i++){
   if(jobs.BehaviorChoicesOpen)jobs.RespondBehavior(0);
   yield return new WaitForSeconds(1);
   if(jobs.trip.State==TripState.ToPickup&&jobs.Behavior.BoardingReady)jobs.Interact();
  }
  Check(jobs.trip.State==TripState.OnTrip,"physical curb boarding at metres="+s.business.ledger.Metres+" elapsed="+s.business.ledger.Elapsed);if(jobs.trip.State!=TripState.OnTrip){halt=true;yield break;}
  if(firstFare&&s.business.CabinEnabled&&!s.business.Cabin.MeterOn)Check(s.business.CabinInput(0)&&s.business.Cabin.MeterOn,"J starts meter");
  if(jobs.Catalog.IsAdditional(pickup))yield return Pocket(jobs.EffectiveStop(pickup),true);if(halt)yield break;
  Check(jobs.nav.RequiredVia<0,"no situation waypoint");int drop=jobs.current.drop;yield return PassengerApproach(drop,"delivery");if(halt)yield break;int before=s.business.ledger.Income;
  int receipts=s.business.passengers.Count;
  if(historicRoute&&jobs.trip.Remaining>0){
   Log("REPLAY arrived earlier than original; holding at curb for remaining "+jobs.trip.Remaining+" simulated seconds");
   while(jobs.trip.State==TripState.OnTrip&&jobs.trip.Remaining>0)yield return null;
  }
  yield return DropPassenger(drop);
  bool recorded=s.business.passengers.Count==receipts+1;
  bool late=jobs.trip.State==TripState.Failed&&jobs.trip.ArrivalLocked&&!jobs.trip.ArrivedOnTime&&jobs.DrunkRide!=null&&jobs.DrunkRide.Settled&&recorded&&s.business.passengers[receipts].paid==0&&s.business.ledger.Income==before;
  bool settled=historicRoute?late:recorded&&(jobs.trip.State==TripState.Complete||late);
  Check(settled,historicRoute?"historic late drop settled once with zero fare":"physical curb dropoff");
  if(!settled){halt=true;yield break;}
  if(historicRoute){rides++;Log("LATE_DROP settled=True arrivedOnTime=False paid=0 state=Failed receipts=1");yield break;}
  rides++;Log("FARE paid="+(s.business.ledger.Income-before)+" manner="+s.manners.ledger.Score+" scenarioCompleted="+zones.Completed);yield return new WaitForSecondsRealtime(1.1f);
  if(firstFare){Check(s.business.ledger.Income>before&&s.business.Story.FoundNotebook,"paid fare and notebook unlocked");var receipt=s.GetComponent<RideCompletionPanel>();if(receipt&&!receipt.Open)receipt.ShowLast();if(receipt&&receipt.Open){Log("RECEIPT "+receipt.Receipt);receipt.Close();}}
  if(jobs.Catalog.IsAdditional(drop))yield return Pocket(jobs.EffectiveStop(drop),true);
 }
 IEnumerator DropPassenger(int index){
  var jobs=s.jobs;
  for(int attempt=1;attempt<=40&&jobs.trip.State==TripState.OnTrip;attempt++){
   var stop=jobs.EffectiveStop(index);var delta=s.taxi.transform.position-stop.Kerb;
   bool at=jobs.At(index);bool accepted=jobs.Interact();
   Log("DROP F="+attempt+" accepted="+accepted+" state="+jobs.trip.State+" at="+at+" occupied="+jobs.StopOccupied(index)+" lateral="+Vector3.Dot(delta,TrafficSystem.Right(stop.Direction)).ToString("F2")+" forward="+Vector3.Dot(delta,stop.Direction).ToString("F2")+" heading="+Vector3.Dot(s.taxi.transform.forward,stop.Direction).ToString("F3")+" speed="+s.taxi.ObservedVelocity.magnitude.ToString("F2")+" locked="+jobs.trip.ArrivalLocked+" work="+jobs.Behavior?.WorkRemaining+" wake="+jobs.DrunkRide?.WaitElapsed+" message="+jobs.message);
   if(jobs.trip.State!=TripState.OnTrip)yield break;
   if(!accepted&&!at)yield break;
   yield return new WaitForSeconds(1);
  }
 }
 static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y;return Vector3.Distance(a,b);}
 void FixedUpdate(){
  if(!measuringGaragePath)return;
  var position=s.taxi.body.position;float step=FlatDistance(previousGaragePosition,position);
  garageMetres+=step;largestGarageStep=Mathf.Max(largestGarageStep,step);
  if(garageMetres>=nextGarageSample){
   nextGarageSample=garageMetres+4;float error,at=garageTracePath.Project(position,out error);
   Log("GARAGE_TRACE at="+at.ToString("F1")+" error="+error.ToString("F2")+" pos="+position+" target="+garageTracePath.Position(at+2.8f)+" heading="+s.taxi.transform.forward+" steer="+s.taxi.autoSteer.ToString("F2")+" speed="+s.taxi.Speed.ToString("F2"));
  }
  bool inside=HomeGarage.Contains(s.world.map,position);
  if(inside!=previousInside){
   crossedGarageBoundary=true;
   boundaryDoorDistance=Mathf.Min(boundaryDoorDistance,FlatDistance(garageDoor,position));
   Log("GARAGE_THRESHOLD inside="+inside+" position="+position+" door="+garageDoor+" metres="+garageMetres.ToString("F2"));
  }
  previousGaragePosition=position;previousInside=inside;
 }
 IEnumerator DriveGarage(bool leave){
  var map=s.world.map;string label=leave?"garage exit":"garage entry";
  var route=leave?HomeGarage.ExitPath(map):HomeGarage.EntryPath(map);
  var start=s.taxi.body.position;garageDoor=HomeGarage.Door(map);
  previousGaragePosition=start;previousInside=HomeGarage.Contains(map,start);
  if(route==null||route.Length<2||previousInside!=leave){
   Check(false,label+" invalid physical start inside="+previousInside+" position="+start);halt=true;yield break;
  }

  Log("GARAGE_PLAN "+label+" from="+route[0]+" through="+garageDoor+" to="+route[route.Length-1]);
  var points=new List<Vector3>{start};points.AddRange(route);
  garageTracePath=new AssistedRoadNetwork.Path(points.ToArray());nextGarageSample=0;
  float plannedDoorDistance=float.PositiveInfinity;
  for(int i=1;i<route.Length;i++){
   if(HomeGarage.Contains(map,route[i-1])==HomeGarage.Contains(map,route[i]))continue;
   float distance=Mathf.Min(FlatDistance(garageDoor,route[i-1]),FlatDistance(garageDoor,route[i]));
   plannedDoorDistance=Mathf.Min(plannedDoorDistance,distance);
  }
  garageMetres=0;largestGarageStep=0;boundaryDoorDistance=float.PositiveInfinity;crossedGarageBoundary=false;
  int collisions=s.manners.collisionEvents;
  measuringGaragePath=true;
  var probe=gameObject.AddComponent<FuelStationProbe>();probe.externallyDriven=true;probe.session=s;
  yield return probe.Drive(points,label);
  measuringGaragePath=false;
  bool continuous=garageMetres>=FlatDistance(start,s.taxi.body.position)-.3f&&largestGarageStep<.4f;
  bool doorway=crossedGarageBoundary&&!float.IsInfinity(plannedDoorDistance)&&boundaryDoorDistance<=plannedDoorDistance+1.5f;
  bool clear=probe.Passed&&s.manners.collisionEvents==collisions;
  var endpoint=leave?HomeGarage.ExitRoad(map):HomeGarage.Bay(map);
  bool ended=HomeGarage.Contains(map,s.taxi.body.position)!=leave&&FlatDistance(s.taxi.body.position,endpoint.Kerb)<1.5f&&Vector3.Dot(s.taxi.transform.forward,endpoint.Direction)>.85f;
  Log("GARAGE_PATH "+label+" metres="+garageMetres.ToString("F2")+" maxFixedStep="+largestGarageStep.ToString("F3")+" boundaryToDoor="+boundaryDoorDistance.ToString("F2")+" position="+s.taxi.body.position);
  Check(continuous&&doorway&&clear&&ended,label+" continuous drive through open doorway without side/curb contacts; continuous="+continuous+" doorway="+doorway+" helper="+probe.Passed+" collisionDelta="+(s.manners.collisionEvents-collisions)+" endInside="+HomeGarage.Contains(map,s.taxi.body.position)+" expectedDoorDistance="+plannedDoorDistance.ToString("F2"));
  if(!(continuous&&doorway&&clear&&ended))halt=true;
  Destroy(probe);
 }
 IEnumerator ReturnHome(){
  var b=s.business;var approach=HomeGarage.Approach(s.world.map);
  bool guided=s.services.GuideAfterTrip(2)&&s.jobs.nav.hdGoal==approach.laneEdge&&Mathf.Abs(s.jobs.nav.hdGoalAt-approach.laneAt)<.1f;
  Check(guided,"select guidance to garage road entrance");if(!guided){halt=true;yield break;}
  var nav=s.jobs.nav;
  if(nav.hdRoute!=null){
   float error,progress=nav.ProjectVehicle(out error);
   // H hands the final metres to manual driving when navigation switches to the driveway.
   float roadDistance=nav.hdRoute.path.length-progress-6;
   if(roadDistance>1){yield return Leg("home approach",roadDistance);if(halt)yield break;}
  }
  if(FlatDistance(s.taxi.body.position,approach.Kerb)>10){Check(false,"missed physical road-to-driveway handoff");halt=true;yield break;}
  yield return DriveGarage(false);if(halt)yield break;
  Check(s.services.HasHomeBase&&s.services.AtEndDayHub(),"taxi stopped at home parking bay");dayReady=true;
  bool closed=s.jobs.Interact();Check(closed,"F clock off at home "+b.notice);if(!closed)yield break;
  for(int frame=0;frame<180&&!b.ShowingSummary;frame++)yield return null;
  Check(b.ShowingSummary&&rides==1&&b.ledger.Income>0,"settlement after physical trip and return");Check(b.ShowingSummary&&b.SaveCheckpoint(),"save settled first shift");
 }
 IEnumerator Service(int kind){if(kind==3&&s.services.RestArea!=null){yield return Rest(true);yield break;}var stop=s.services.StopFor(kind);s.jobs.nav.SetDestination(stop);yield return Leg(kind==1?"meal approach":"sleep approach");if(halt)yield break;if(kind==1&&s.services.Restaurant!=null){yield return Restaurant();yield break;}s.services.Toggle();bool bought=s.services.RequestAction(kind)&&s.services.ConfirmAction(true);Check(bought,"service "+kind);if(s.services.open)s.services.Toggle();if(bought){if(kind==1)meals++;else sleeps++;}else halt=true;Time.timeScale=4;}
 IEnumerator Restaurant(){var site=s.services.Restaurant;var helper=gameObject.AddComponent<FuelStationProbe>();helper.externallyDriven=true;helper.session=s;var entry=new List<Vector3>{s.taxi.transform.position};entry.AddRange(site.enter);yield return helper.Drive(entry,"continuous restaurant entry");Check(helper.Passed&&s.services.AtRestaurant(),"restaurant bay physically reached");if(!helper.Passed||!s.services.AtRestaurant()){halt=true;Destroy(helper);yield break;}s.services.Toggle();bool bought=s.services.RequestAction(1)&&s.services.ConfirmAction(true);Check(bought,"restaurant meal purchase");if(bought)meals++;else halt=true;if(s.services.open)s.services.Toggle();Time.timeScale=4;if(halt){Destroy(helper);yield break;}var exit=RestaurantDeparture(site,s.jobs.nav.hd,s.taxi.transform.position);yield return helper.Drive(exit,"continuous restaurant exit");Check(helper.Passed,"restaurant physical exit");if(!helper.Passed)halt=true;Destroy(helper);}
 IEnumerator Rest(bool finishHere=false){var area=s.services.RestArea;s.jobs.nav.SetDestination(area.approach);yield return Leg("rest parking approach");if(halt)yield break;
  var probe=gameObject.AddComponent<FuelStationProbe>();probe.externallyDriven=true;probe.session=s;var entry=new List<Vector3>{s.taxi.transform.position};for(int i=0;i<25;i++)entry.Add(area.drivePath[i]);entry.Add(area.pump);yield return probe.Drive(entry,"rest parking entry");Check(probe.Passed&&s.services.AtRestArea(),"rest bay physically reached");if(!probe.Passed||!s.services.AtRestArea()){halt=true;yield break;}
  Check(!s.services.AtFuelPump()&&s.services.CanUse(2),"rest bay supports planned rest/close without refuelling");s.services.Toggle();double before=s.business.ledger.Elapsed;float fatigue=(float)s.business.ledger.Driver.fatigue;bool used=s.services.RequestAction(3)&&s.services.ConfirmAction(true);Check(used&&Math.Abs(s.business.ledger.Elapsed-before-100)<.01&&s.business.ledger.Driver.fatigue<fatigue,"sleep at additional parking costs exactly 100 seconds");if(used)sleeps++;else halt=true;if(s.services.open)s.services.Toggle();if(halt){Destroy(probe);yield break;}Time.timeScale=4;
  if(finishHere){Destroy(probe);yield break;}
  var exit=new List<Vector3>{area.pump};for(int i=26;i<area.drivePath.Length;i++)exit.Add(area.drivePath[i]);exit.Add(s.jobs.nav.hd.paths[area.approach.laneEdge].Position(area.approach.laneAt+63));yield return probe.Drive(exit,"rest parking exit");Check(probe.Passed,"rest exit safety");if(!probe.Passed)halt=true;Destroy(probe);
 }
 IEnumerator Fuel(){var stations=s.services.Stations;int selected=-1;float best=float.MaxValue,at;var hd=s.jobs.nav.hd;int edge=hd.Nearest(s.taxi.transform.position,s.taxi.transform.forward,out at);for(int i=0;i<stations.Count;i++){var a=stations[i].approach;var r=hd.Route(edge,at,a.laneEdge,a.laneAt);var parking=s.services.RestArea!=null?s.services.RestArea.approach:s.jobs.stops[s.services.indices[2]];var back=hd.Route(a.laneEdge,a.laneAt+63,parking.laneEdge,parking.laneAt);float cost=r==null||back==null?float.PositiveInfinity:r.path.length+back.path.length;Log("FUEL_PLAN "+stations[i].id+" approach="+(r==null?-1:r.path.length)+" parking="+(back==null?-1:back.path.length));if(cost<best){best=cost;selected=i;}}Check(selected>=0,"reachable fuel station");if(selected<0){halt=true;yield break;}var station=stations[selected];s.jobs.nav.SetDestination(station.approach);yield return Leg("fuel approach "+station.id);if(halt)yield break;
  int collisionBefore=s.manners.collisionEvents,lawBefore=s.law.ledger.Events.Length;var probe=gameObject.AddComponent<FuelStationProbe>();probe.externallyDriven=true;probe.session=s;var first=new List<Vector3>{s.taxi.transform.position};for(int i=0;i<25;i++)first.Add(station.drivePath[i]);first.Add(station.pump);yield return probe.Drive(first,"continuous fuel entry");Check(probe.Passed,"fuel entry safety");if(!probe.Passed){halt=true;yield break;}Check(s.services.FuelAtPump==selected,"physical pump reached");if(s.services.FuelAtPump!=selected){halt=true;yield break;}s.services.Toggle();bool bought=s.services.Buy(0);Check(bought,"ten percent fuel purchase");if(bought)fills++;else halt=true;if(s.services.open)s.services.Toggle();Time.timeScale=4;
  var second=new List<Vector3>{station.pump};for(int i=26;i<station.drivePath.Length;i++)second.Add(station.drivePath[i]);second.Add(hd.paths[station.approach.laneEdge].Position(station.approach.laneAt+63));yield return probe.Drive(second,"continuous fuel exit");Check(probe.Passed,"fuel exit safety");if(!probe.Passed)halt=true;Check(s.manners.collisionEvents==collisionBefore,"fuel no taxi collision");var fuelEvents=s.law.ledger.Events;int fuelDeduction=0;for(int i=lawBefore;i<fuelEvents.Length;i++)fuelDeduction+=fuelEvents[i].Deduction;Check(fuelDeduction==0,"fuel no law deduction");Destroy(probe);}
 bool CanFitRide(){s.jobs.Generate();foreach(var offer in s.jobs.offers){if(offer==null)continue;float pickup=PassengerJobs.DirectedDistance(s.jobs.nav.hd,s.taxi.transform.position,s.taxi.transform.forward,s.jobs.stops[offer.pickup]);if((pickup+offer.distance)/3+120<s.business.ledger.SecondsLeft)return true;}Log("PLAN no remaining offer fits estimated access + ride + signals; finish day without accepting doomed pickup");return false;}
 bool PlaceAtHistoricParking(){
  var map=s.world.map;
  foreach(var facility in ProjectMapScene.Services(map,"parking")){
   if(!HomeBasePresentation.IsFacility(map,facility))continue;
   var stop=ProjectMapScene.ParkingStop(facility);
   bool original=FlatDistance(stop.Kerb,new Vector3(296.2881f,0,506.4904f))<.1f&&Vector3.Dot(stop.Direction,Quaternion.Euler(0,282.3668f,0)*Vector3.forward)>.999f;
   Check(original,"original r47 roadside marker and departure heading");if(!original)return false;
   s.taxi.SetSpawn(stop.Kerb+Vector3.up*.65f,Quaternion.LookRotation(stop.Direction));s.taxi.ResetTaxi();
   s.law.ResetObservation();s.jobs.ResetOffers();
   Log("REPLAY isolated historical initial spawn before departure: roadside service:parking position="+s.taxi.body.position+" direction="+s.taxi.transform.forward+"; subsequent travel entirely physical");
   return true;
  }
  Check(false,"historical roadside service:parking marker missing");return false;
 }
 IEnumerator Start(){while(!(s=FindObjectOfType<DriveSession>())||!s.StartupReady)yield return null;yield return null;yield return null;startReal=Time.realtimeSinceStartup;var args=Environment.GetCommandLineArgs();firstFare=Array.IndexOf(args,"-firstFareCheck")>=0;historicRoute=Array.IndexOf(args,"-firstFareRoute3038")>=0;var b=s.business;b.preparing=true;Time.timeScale=0;if(historicRoute){if(!PlaceAtHistoricParking()){Finish();yield break;}}else yield return b.PlaceAtParking();Check(b.ledger.OneDay&&b.ledger.Settings.daySeconds==1200,"real 1200-second day");Check(s.traffic.cars.Count==DistrictTraffic.PoolCap,"production ambient car pool retained");npcStart=NpcDistance();s.taxi.automated=true;s.taxi.autoBrake=true;s.assisted.allowAutomated=true;driver=gameObject.AddComponent<LiveTrafficValidationTest>();driver.externallyDriven=true;driver.session=s;driver.cruise=8;driver.legTimeout=600;b.Depart();
  if(firstFare){while(b.IntroInputBlocked)yield return null;Check(b.Story.introSeen&&b.Cabin.OffDuty&&s.services.AtEndDayHub(),"normal first departure from home");Check(b.CabinInput(1)&&!b.Cabin.OffDuty,"K enables first near-home offers");}
  Time.timeScale=4;Log("START production parking spawn="+s.taxi.transform.position+" one continuous run; x4 simulation, physical fixed step unchanged");
  yield return Ride();if(halt){Finish();yield break;}if(firstFare){yield return ReturnHome();Finish();yield break;}if(historicRoute){Finish();yield break;}yield return Service(1);if(halt){Finish();yield break;}yield return Fuel();if(halt){Finish();yield break;}yield return Service(3);if(halt){Finish();yield break;}
  // The trip and all facilities have now been exercised; finish at this legal rest hub.
  dayReady=true;Check(s.services.AtEndDayHub(),"physical rest hub ready for closing");s.taxi.autoThrottle=0;s.taxi.autoBrake=true;while(b.ledger.Elapsed<1200)yield return null;Check(b.RequestEndDay(),"explicit end at physical parking after twenty minutes");if(!ok){Finish();yield break;}while(!b.ShowingSummary){if(s.jobs.trip.State==TripState.OnTrip){Check(false,"unexpected passenger at idle");break;}yield return null;}Check(b.ShowingSummary,"day settles on actual clock");Check(meals>0&&sleeps>0&&fills>0&&rides>0,"transport meal sleep fuel all exercised");Check(NpcDistance()>npcStart+1000,"aggregate ambient travel increased");Check(b.SaveCheckpoint(),"day result saved");Finish();
 }
 void Finish(){if(finished)return;finished=true;StopAllCoroutines();s.taxi.autoThrottle=0;s.taxi.autoBrake=true;int deductions=0;foreach(var e in s.law.ledger.Events)deductions+=e.Deduction;Check(deductions==0,"whole day no law deductions");Check(s.manners.collisionEvents==0,"whole day no taxi collisions");var l=s.business.ledger;Log("RESULT success="+ok+" elapsed="+l.Elapsed+" metres="+l.Metres+" rides="+rides+" income="+l.Income+" net="+l.ProjectedNet+" meals="+meals+" sleep="+sleeps+" refills="+fills+" fuel="+l.Account.Fuel+" collision="+s.manners.collisionEvents+" red="+s.manners.redEvents+" brakes="+s.manners.brakeEvents);foreach(var e in s.law.ledger.Events)Log("LAW "+e.family+" "+e.scenario+" "+e.junction+" deduction="+e.Deduction+" "+e.reason);Application.Quit(ok?0:1);}
}
