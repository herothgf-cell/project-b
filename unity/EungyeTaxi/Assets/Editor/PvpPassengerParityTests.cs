using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
public static class PvpPassengerParityTests {
 static int checks;static void Check(bool v,string why){checks++;if(!v)throw new Exception("PVP_PASSENGER "+why);}
 static void Set(object o,string p,object v){o.GetType().GetProperty(p).SetValue(o,v,null);}
 public static void Run(){
  for(int phase=0;phase<3;phase++){int[] counts=new int[3];for(int seed=0;seed<12000;seed++){int pickup=PvpPassengerPolicy.Pickup(seed,18,new[]{100,600,1000}[phase],1200);Check(pickup>=0&&pickup<18,"valid pickup");counts[PvpPassengerPolicy.Region(pickup)]++;}Check(counts[phase]>counts[(phase+1)%3]*2&&counts[phase]>counts[(phase+2)%3]*2,"stage demand concentration");}
  var fresh=new DriverCondition{satiety=100,fatigue=0};var shortRide=new JobOffer{timingPolicyVersion=1,safeRouteSeconds=100,distance=600};var longRide=new JobOffer{timingPolicyVersion=1,safeRouteSeconds=800,distance=5000};Check(PvpPassengerPolicy.OfferRank(1000,shortRide,fresh,7200)<PvpPassengerPolicy.OfferRank(100,longRide,fresh,7200),"complete trip time beats deceptively near pickup");Check(double.IsInfinity(PvpPassengerPolicy.OfferRank(10,longRide,new DriverCondition{satiety=80,fatigue=95},7200)),"predicted drowsy passenger cancellation excludes impossible contract");Check(double.IsInfinity(PvpPassengerPolicy.OfferRank(10,shortRide,fresh,60)),"trip cannot finish after race end");
  Check(PvpPassengerPolicy.PursueRoadside(250)&&!PvpPassengerPolicy.PursueRoadside(251)&&!PvpPassengerPolicy.PursueRoadside(double.NaN),"nearby opportunistic roadside pursuit, no cross-city unreserved chase");
  Check(PvpPassengerPolicy.Phase(150,600)==1&&PvpPassengerPolicy.Phase(450,600)==2,"duration-relative phases, no fixed20minute clock");
  var ledger=new PvpSimulationLedger();var board=new PvpSimulationCallBoard(ledger,i=>new JobOffer{pickup=i%18,drop=(i+1)%18,distance=100},18,3);var seen=new HashSet<int>();
  foreach(var pool in new[]{board.Offers,board.Roadside})foreach(var o in pool)Check(o!=null&&seen.Add(o.pickup),"11 distinct shared waiting passengers");
  Check(board.Offers.Length==8&&board.Roadside.Length==3,"eight calls plus three independent roadside passengers");var offer=board.Roadside[0];
  Check(board.TryClaim(offer,1)&&!board.Contains(offer)&&ledger.IsClaimed(offer.pickup),"roadside identity atomically claimed and removed");Check(!board.TryClaim(offer,2),"stale roadside cannot be double claimed");board.Release(offer.pickup,2);Check(ledger.IsClaimed(offer.pickup),"wrong owner cannot release");board.Release(offer.pickup,1);Check(!ledger.IsClaimed(offer.pickup)&&board.RoadsideAt(offer.pickup)==null,"correct release and global cooldown");
  ledger.Finish();Check(!board.TryClaim(board.Offers[0],0),"finished match rejects late claims");
  Check(PvpPassengerPolicy.BehaviorChoice(PassengerType.Food,false,80,500)==1&&PvpPassengerPolicy.BehaviorChoice(PassengerType.Food,false,30,500)==0,"food respects cleanliness");
  Check(PvpPassengerPolicy.BehaviorChoice(PassengerType.MuddyLuggage,false,80,500)==0&&PvpPassengerPolicy.BehaviorChoice(PassengerType.MuddyLuggage,false,80,20)==2,"muddy luggage protection versus final seconds");
  Check(PvpPassengerPolicy.BehaviorChoice(PassengerType.Luggage,false,80,500)==0&&PvpPassengerPolicy.BehaviorChoice(PassengerType.Luggage,false,80,20)==1,"luggage honors loading/unloading time");
  Check(PvpPassengerPolicy.AcceptRoute(20,200,300)&&!PvpPassengerPolicy.AcceptRoute(180,200,300)&&!PvpPassengerPolicy.AcceptRoute(double.NaN,200,300),"safe route change budget");
  Check(!PvpPassengerPolicy.AcceptRoute(20,600,7200,250,new DriverCondition{satiety=80,fatigue=95}),"route response considers drowsy cancellation, not time budget alone");
  Check(!PvpPassengerPolicy.AcceptRoute(0,300,60,200),"full route cannot fit in final minute even with zero detour");
  bool active=PvpSimulationMode.Active,expanded=PvpSimulationMode.Expanded;int duration=PvpSimulationMode.DurationSeconds;float scale=Time.timeScale;var root=new GameObject("passenger parity fixture");root.SetActive(false);
  try{
   PvpSimulationMode.Configure(true,1200);Time.timeScale=1;var match=root.AddComponent<PvpSimulationMatch>();Set(match,"Ledger",new PvpSimulationLedger());var drivers=(List<DriveSession>)typeof(PvpSimulationMatch).GetField("drivers",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(match);var map=PvpCityMap.Create();
   for(int i=0;i<4;i++){var go=new GameObject("driver"+i);go.SetActive(false);go.transform.SetParent(root.transform);go.transform.position=new Vector3(-1000-i*10,1,-1000);var s=go.AddComponent<DriveSession>();s.IsSimulationBot=true;s.simulationMatch=match;s.taxi=go.AddComponent<TaxiController>();s.taxi.body=go.AddComponent<Rigidbody>();s.world=go.AddComponent<WorldBuilder>();s.world.map=map;s.jobs=go.AddComponent<PassengerJobs>();s.jobs.session=s;s.jobs.stops=map.stops;s.jobs.current=new JobOffer{pickup=0,drop=0,seconds=120};Set(s.jobs,"OfferId","call-"+i);s.jobs.trip.Accept(3000,120,150);if(i>=2)s.jobs.trip.Board();s.law=go.AddComponent<DrivingLaw>();s.law.tripId="fixture-"+i;s.requests=go.AddComponent<PassengerRequestUI>();s.requests.session=s;drivers.Add(s);}
   Set(match,"Human",drivers[0]);var world=root.AddComponent<PvpPassengerWorld>();world.Initialize(match);Set(match,"PassengerWorld",world);
   Check(world.TryBegin(0),"authored bay only, physical shared obstruction");for(int i=0;i<4;i++)Check(drivers[i].jobs.StopOccupied(0),"all drivers observe the same obstruction");Check(world.ProtectedCount==4,"each affected driver protected once");drivers[0].jobs.trip.Board();drivers[0].law.tripId="new boarded trip id";
   float pickup=drivers[0].jobs.trip.PickupRemaining,remaining=drivers[2].jobs.trip.Remaining;world.Advance(1);world.Advance(1);Check(world.ProtectedCount==4&&drivers[0].jobs.trip.PickupRemaining==pickup&&drivers[2].jobs.trip.Remaining==remaining,"no repeated deadline extension");world.Advance(35);Check(!world.IsOccupied(0),"actual clear driveway advances30secondwait and7m departure");
   var idle=drivers[1];idle.jobs.trip.Clear();idle.business=idle.gameObject.AddComponent<BusinessProgress>();idle.business.Initialize(idle);var services=idle.gameObject.AddComponent<PvpSimulationBotServices>();typeof(PvpSimulationBotServices).GetField("conditionTripCount",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(services,0);for(int tick=0;tick<150;tick++)services.Tick(idle);Check(services.DecisionCount==1,"idle unclaimed approach does not repeat facility graph decisions150 times per physics burst");
   drivers[0].taxi.transform.position=map.stops[1].Kerb;Check(!world.TryBegin(1),"cannot spawn on a nearby taxi");
  }finally{UnityEngine.Object.DestroyImmediate(root);if(active)PvpSimulationMode.Configure(expanded,duration);else PvpSimulationMode.Clear();Time.timeScale=scale;}
  PvpSimulationFourDriverTests.Run();PassengerBehaviorReleaseTests.Run();PvpConditionChecks.Run();Debug.Log("PVP_PASSENGER_EDITOR_OK checks="+checks);
 }
}
