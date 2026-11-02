using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
// Scheduler fixtures use controlled positions and ticks; Player acceptance separately waits on shared time.
public static class PvpConditionServiceTests {
 static void Check(bool value,string why){if(!value)throw new Exception("PVP_CONDITION_SERVICE "+why);}
 static void Set(object target,string property,object value){target.GetType().GetProperty(property).SetValue(target,value,null);}
 sealed class Fixture : IDisposable {
  public readonly GameObject root=new GameObject("condition service fixture");
  public DriveSession s;public PvpSimulationMatch match;
  readonly bool active=PvpSimulationMode.Active,expanded=PvpSimulationMode.Expanded;readonly int duration=PvpSimulationMode.DurationSeconds;readonly float scale=Time.timeScale;
  public Fixture(){
   root.SetActive(false);PvpSimulationMode.Configure(true,600);Time.timeScale=1;
   s=root.AddComponent<DriveSession>();s.IsSimulationBot=true;
   s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();
   s.assisted=root.AddComponent<AssistedDriveController>();
   s.world=root.AddComponent<WorldBuilder>();var stops=new MapStop[10];for(int i=0;i<10;i++)stops[i]=new MapStop{name="site"+i,x=i*20,dz=1,width=3};s.world.map=new MapData{mapId="fixture",stops=stops,roads=new MapRoad[0]};
   s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;s.jobs.stops=stops;s.jobs.nav=root.AddComponent<RoadNavigation>();
   s.law=root.AddComponent<DrivingLaw>();s.services=root.AddComponent<ServiceStops>();s.services.session=s;
   s.business=root.AddComponent<BusinessProgress>();s.business.Initialize(s);
   match=root.AddComponent<PvpSimulationMatch>();s.simulationMatch=match;Set(match,"Ledger",new PvpSimulationLedger());Set(match,"Human",s);
   ((List<DriveSession>)typeof(PvpSimulationMatch).GetField("drivers",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(match)).Add(s);
  }
  public void Site(int kind){s.taxi.transform.position=(kind==1?s.services.MealStop:s.services.ParkingStop).Kerb;s.taxi.transform.rotation=Quaternion.identity;s.taxi.body.velocity=Vector3.zero;s.services.open=true;}
  public void Tick(float value){match.AdvanceConditionService(s,value);}
  public void Dispose(){UnityEngine.Object.DestroyImmediate(root);if(active)PvpSimulationMode.Configure(expanded,duration);else PvpSimulationMode.Clear();Time.timeScale=scale;}
 }
 static void QueuedService(int kind,bool cancel){
  using(var f=new Fixture()){
   var s=f.s;var map=PvpCityMap.Create();s.world.map=map;s.jobs.stops=map.stops;s.jobs.nav.hd=new HdRoadGraph(map);s.IsSimulationBot=false;
   var start=map.stops[0];var path=s.jobs.nav.hd.paths[start.laneEdge];s.taxi.transform.SetPositionAndRotation(path.Position(start.laneAt),Quaternion.LookRotation(path.Tangent(start.laneAt)));
   var options=s.services.FacilityOptions(kind);FacilityOption site=null;foreach(var option in options)if(option.Reachable){site=option;break;}Check(site!=null,"queued service has reachable authored site");
   var otherObject=new GameObject("queue owner");otherObject.SetActive(false);otherObject.transform.SetParent(f.root.transform);var other=otherObject.AddComponent<DriveSession>();other.simulationMatch=f.match;other.IsSimulationBot=true;other.taxi=otherObject.AddComponent<TaxiController>();other.services=otherObject.AddComponent<ServiceStops>();other.services.session=other;
   Check(other.services.CanEnterFacility(site),"prior ticket occupies FIFO");s.taxi.transform.SetPositionAndRotation(site.Bay,Quaternion.LookRotation(site.Direction));s.taxi.body.velocity=Vector3.zero;s.services.open=true;
   var ledger=s.business.ledger;ledger.Driver.satiety=40;ledger.Driver.fatigue=80;int cash=ledger.Account.Cash;double before=ledger.Elapsed;
   Check(!s.services.Buy(kind)&&s.services.QueueWaiting,"purchase waits behind existing ticket without charge");Check(ledger.Elapsed==before&&ledger.Account.Cash==cash,"queued purchase has no time or payment");
   other.services.CancelQueue();s.services.TickFacilityQueue();Check(f.match.ConditionServiceActive(s)&&s.services.open,"actual queue admission keeps timed condition service open");
   if(cancel){f.Tick(5);s.services.Close();Check(!f.match.ConditionServiceActive(s)&&ledger.Account.Cash==cash&&ledger.Elapsed==before+5&&ledger.PvpMeals==0&&ledger.Driver.satiety<40,"explicit cancellation keeps elapsed time with no payment or recovery");}
   else {f.Tick(kind==1?15:45);Check(!f.match.ConditionServiceActive(s)&&ledger.Elapsed==before+(kind==1?15:45),"queued timed service completes exactly once");Check(kind==1?ledger.Account.Cash==cash-1000&&ledger.PvpMeals==1:ledger.Account.Cash==cash&&ledger.PvpRests==1&&Math.Abs(ledger.Driver.fatigue-40)<.001,"queued completion applies native payment and recovery");}
  }
 }
 public static void Run(){
  QueuedService(1,false);QueuedService(3,false);QueuedService(1,true);
  using(var f=new Fixture()){
   var l=f.s.business.ledger;f.Site(1);double before=l.Elapsed;int cash=l.Account.Cash;double fatigue=l.Driver.fatigue,satiety=l.Driver.satiety;
   Check(f.match.BeginConditionService(f.s,1),"meal begins only at real safe fixture site");Check(l.Elapsed==before&&l.Account.Cash==cash&&l.Driver.satiety==satiety,"start has no instant charge, time jump or recovery");
   f.Tick(10);Check(l.Elapsed==before+10&&l.Account.Cash==cash&&l.Driver.satiety<satiety&&l.Driver.fatigue>fatigue,"meal partial uses normal fatigue and satiety without charge");
   f.s.services.Close();Check(!f.match.ConditionServiceActive(f.s)&&l.Elapsed==before+10&&l.Account.Cash==cash,"close cancels and keeps elapsed, no effect or charge");
   f.Site(1);Check(f.match.BeginConditionService(f.s,1),"meal restart");before=l.Elapsed;f.Tick(15);Check(!f.match.ServiceLocked(f.s)&&l.Elapsed==before+15&&l.Account.Cash==cash-1000,"completion charges exactly once with no legacy lock");f.Tick(15);f.match.ObserveServiceTime(f.s);Check(l.Account.Cash==cash-1000&&!f.match.ServiceLocked(f.s),"repeated tick and observation cannot duplicate completion or wait");
   f.Site(3);l.Driver.fatigue=90;l.Driver.drowsiness.visionSeconds=80;satiety=l.Driver.satiety;
   Check(f.match.BeginConditionService(f.s,3),"rest start");f.Tick(20);Check(l.Driver.fatigue==90&&l.Driver.satiety<satiety&&l.Driver.drowsiness.visionSeconds==80,"partial rest stops fatigue but consumes satiety and does not recover early");f.Tick(25);Check(l.Driver.fatigue==50&&l.Driver.drowsiness.visionSeconds==0,"rest completion -40 and drowsiness reset");
   f.Site(1);Check(f.match.BeginConditionService(f.s,1),"site cancellation start");f.s.taxi.transform.position+=Vector3.right*10;before=l.Elapsed;f.Tick(1);Check(!f.match.ConditionServiceActive(f.s)&&l.Elapsed==before,"leaving safe site cancels before progression");
   f.Site(1);f.s.taxi.body.velocity=Vector3.forward;Check(!f.match.BeginConditionService(f.s,1),"moving actor cannot start");f.s.taxi.body.velocity=Vector3.zero;
   Check(f.match.BeginConditionService(f.s,1),"occupied cancellation start");cash=l.Account.Cash;f.s.jobs.trip.Accept(1000,60);f.s.jobs.trip.Board();f.Tick(1);Check(!f.match.ConditionServiceActive(f.s)&&l.Account.Cash==cash,"occupying actor cancels without charge");f.s.jobs.trip.Clear();f.Site(1);
   Check(f.match.BeginConditionService(f.s,1),"end cancellation start");cash=l.Account.Cash;Set(f.match,"Elapsed",590f);f.Tick(10);Check(!f.match.ConditionServiceActive(f.s)&&l.Account.Cash==cash,"unfinished service at end boundary has no charge");Set(f.match,"Elapsed",585f);Check(f.match.BeginConditionService(f.s,1),"exact boundary fixture starts");f.Tick(15);f.match.Finish();Check(!f.match.ConditionServiceActive(f.s)&&l.Account.Cash==cash,"completion tied with match end cancels without charge");
  }
  // Reproduce several physics catch-up steps without any rendered Update.
  float priorStep=Time.fixedDeltaTime,priorMaximum=Time.maximumDeltaTime;
  try{
   Time.fixedDeltaTime=1;
   using(var f=new Fixture()){
    f.Site(1);var l=f.s.business.ledger;int cash=l.Account.Cash;double before=l.Elapsed;
    Set(f.match,"Elapsed",590f);Check(f.match.BeginConditionService(f.s,1),"catch-up meal starts with only ten shared seconds left");
    var fixedClock=typeof(PvpSimulationMatch).GetMethod("FixedUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
    for(int tick=0;tick<10;tick++){
     f.Tick(Time.fixedDeltaTime);fixedClock.Invoke(f.match,null);
     Check(f.match.Elapsed==591+tick,"common clock advances once each catch-up physics tick before Update");
    }
    Check(f.match.Finished&&!f.match.ConditionServiceActive(f.s)&&l.Elapsed==before+10&&l.Account.Cash==cash,"ten catch-up ticks cannot spend stale end budget to finish fifteen-second meal");
    f.Tick(1);fixedClock.Invoke(f.match,null);Check(l.Elapsed==before+10&&l.Account.Cash==cash&&f.match.Elapsed==600,"finished match cannot advance pending service or common clock again");
   }
  }finally{Time.fixedDeltaTime=priorStep;Time.maximumDeltaTime=priorMaximum;}
  Debug.Log("PVP_CONDITION_SERVICE_TESTS_OK");
 }
}
