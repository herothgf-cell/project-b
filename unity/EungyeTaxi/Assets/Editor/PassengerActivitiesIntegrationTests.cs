using System;
using UnityEngine;
public static class PassengerActivitiesIntegrationTests {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception("PASSENGER_ACTIVITIES: "+reason);}
 static void Reject(Action action,string reason){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,reason);}
 public static void Run(){
  var old=JsonUtility.FromJson<PassengerJobsSnapshot>("{\"situation\":{},\"errand\":{}}");PassengerJobs.ValidateSituationSnapshot(old);Check(old.situation==null&&old.errand==null,"old saves do not invent reactions or errand contracts");
  var situation=PassengerSituationState.Create("trip",0,0,0);situation.Observe(18,true,false,true,false,1,0,0,0,false);
  var errand=PassengerErrandState.Agree("trip","via","destination");errand.Start(true);errand.Advance(7,true,false);
  var snapshot=new PassengerJobsSnapshot{activityVersion=1,situationPresent=true,errandPresent=true,situation=situation,errand=errand,changeApplied=true,passengerVia=0,stopIds=new[]{"via","destination"},current=new JobOffer{drop=1,dropStopId="destination"},trip=new FareTripSnapshot{state=TripState.OnTrip}};
  var copy=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(snapshot));PassengerJobs.ValidateSituationSnapshot(copy);
  Check(copy.situation.Pending==PassengerSituationKind.SignalWait&&copy.situation.Reactions==1,"JSON preserves pending reaction and spent budget");
  Check(copy.errand.Waiting&&copy.errand.Remaining==13&&copy.errand.TripId=="trip","JSON preserves exact waiting progress and trip identity");
  copy.situationPresent=copy.errandPresent=false;PassengerJobs.ValidateSituationSnapshot(copy);Check(copy.situation==null&&copy.errand==null,"explicit presence prevents JsonUtility phantom state");
  Reject(()=>PassengerJobs.ValidateSituationSnapshot(new PassengerJobsSnapshot{activityVersion=2}),"unknown version rejected");
  copy=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(snapshot));copy.errand.TripId="other";Reject(()=>PassengerJobs.ValidateSituationSnapshot(copy),"cross-trip errand rejected");
  copy=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(snapshot));copy.current.dropStopId="other";Reject(()=>PassengerJobs.ValidateSituationSnapshot(copy),"original destination cannot change during wait");
  var root=new GameObject("passenger observed event fixture");try{
   var s=root.AddComponent<DriveSession>();s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;s.jobs.nav=root.AddComponent<RoadNavigation>();s.jobs.current=new JobOffer();s.jobs.trip.Accept(5000,100);s.jobs.trip.Board();
   s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.taxi.body.useGravity=false;s.traffic=root.AddComponent<TrafficSystem>();s.law=root.AddComponent<DrivingLaw>();s.law.tripId="runtime-trip";s.manners=root.AddComponent<DrivingManners>();s.manners.session=s;s.requests=root.AddComponent<PassengerRequestUI>();s.requests.session=s;s.business=root.AddComponent<BusinessProgress>();s.business.session=s;s.business.useV2=true;s.business.ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());typeof(BusinessProgress).GetField("restoring",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s.business,true);
   var junction=new TrafficJunction{center=Vector3.zero,direction=Vector3.forward,halfWidth=new[]{5f,5f},scenarioSignal=true,scenarioForward=SignalColor.Red,observationAxis=0,observationSign=1};s.traffic.junctions.Add(junction);root.transform.position=new Vector3(2,0,-19);root.transform.rotation=Quaternion.identity;
   var signal=typeof(PassengerJobs).GetMethod("ObservedSignalWait",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   Check((bool)signal.Invoke(s.jobs,null),"native junction without editor graph still observes stopped red-signal queue");junction.scenarioForward=SignalColor.Green;Check(!(bool)signal.Invoke(s.jobs,null),"green signal is not a signal wait");junction.scenarioForward=SignalColor.Red;
   root.transform.rotation=Quaternion.Euler(0,90,0);typeof(PassengerJobs).GetField("nextReactionSignalSearch",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s.jobs,0f);Check(!(bool)signal.Invoke(s.jobs,null),"wrong heading cannot observe that approach");root.transform.rotation=Quaternion.identity;s.traffic.junctions.Clear();
   typeof(PassengerJobs).GetMethod("BeginPassengerActivities",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(s.jobs,null);int cleanliness=s.business.ledger.Care.Cleanliness,score=s.manners.ledger.Score;
   s.jobs.AdvancePassengerActivities(30);Check(!s.jobs.SituationPending,"elapsed time alone does not fabricate a jolt");
   s.manners.necessaryBrakeEvents++;s.jobs.AdvancePassengerActivities(1);Check(s.jobs.Situation.Pending==PassengerSituationKind.NecessaryBrake,"runtime uses existing observed necessary-brake counter");
   s.paused=true;float elapsed=s.jobs.Situation.Elapsed;s.jobs.AdvancePassengerActivities(99);Check(s.jobs.Situation.Elapsed==elapsed&&s.jobs.SituationPending,"pause preserves unanswered reaction");s.paused=false;
   s.taxi.body.velocity=Vector3.forward;s.requests.AdvanceSafeStop(3);Check(!s.jobs.RespondSituation(0)&&s.jobs.SituationPending,"motion defers an answer");s.taxi.body.velocity=Vector3.zero;s.requests.AdvanceSafeStop(2.1f);
   Check(s.jobs.RespondSituation(0)&&!s.jobs.SituationPending,"safe explicit runtime reply");Check(s.business.ledger.Care.Cleanliness==cleanliness&&s.manners.ledger.Score==score&&s.jobs.InteractionSatisfactionDelta==0,"reaction causes no duplicate dirt or satisfaction penalty");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  Debug.Log("PASSENGER_ACTIVITIES_INTEGRATION_OK checks="+checks);
 }
}
