using System;
using UnityEngine;
public static class PassengerBehaviorIntegrationTests {
 static int checks;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("PASSENGER_BEHAVIOR: "+message);}
 public static void Run(){
  Check(typeof(PassengerJobs).GetProperty("Behavior")!=null,"jobs must preserve a passenger behavior across the live lifecycle");
  LegacyPresence();BoundRequests();SafeChoiceAndSleep();
  Debug.Log("PASSENGER_BEHAVIOR_INTEGRATION_OK checks="+checks);
 }
 static void LegacyPresence(){
  var legacy=JsonUtility.FromJson<PassengerJobsSnapshot>("{\"trip\":{\"state\":2},\"behavior\":{}}");PassengerJobs.NormalizePresence(legacy);Check(legacy.behavior==null,"legacy missing version never creates phantom behavior");
  var state=new PassengerJobsSnapshot{behaviorVersion=1,behaviorPresent=true,behavior=PassengerBehaviorState.Create("food",PassengerType.Food)};
  var copy=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(state));PassengerJobs.NormalizePresence(copy);Check(copy.behavior.Type==PassengerType.Food&&copy.behavior.Choice==-1,"explicit new JSON type and unanswered choice preserved");
  copy.behaviorPresent=false;PassengerJobs.NormalizePresence(copy);Check(copy.behavior==null,"explicit absent behavior normalized");
  bool rejected=false;try{PassengerJobs.NormalizePresence(new PassengerJobsSnapshot{behaviorVersion=1,behaviorPresent=true,behavior=new PassengerBehaviorState{Version=0}});}catch(ArgumentException){rejected=true;}Check(rejected,"malformed claimed-present behavior rejected");
 }
 static void BoundRequests(){var kinds=(RequestKind[])Enum.GetValues(typeof(RequestKind));
  for(int n=0;n<150;n++)foreach(var type in new[]{PassengerType.Comfort,PassengerType.Hurried,PassengerType.RouteChecking,PassengerType.Errand,PassengerType.PrecisionStop}){
   var schedule=PassengerRequestSchedule.Create("bound"+n,kinds);int before=schedule.Capture().slots.Length;var kind=PassengerRequestUI.Representative(type,0).Value;bool bound=schedule.BindRepresentative(kind,kinds);var slots=schedule.Capture().slots;
   Check(bound==(before>0)&&slots.Length==before,"representative preserves zero/one/two request count");if(bound)Check(slots[0].kind==kind,"representative really assigned");if(slots.Length==2)Check(PassengerRequestSchedule.Compatible(slots[0].kind,slots[1].kind),"representative excludes conflicting route and speech promises");
  }
 }
 static void SafeChoiceAndSleep(){var root=new GameObject("behavior live fixture");try{
  var s=root.AddComponent<DriveSession>();s.business=root.AddComponent<BusinessProgress>();s.business.useV2=true;typeof(BusinessProgress).GetField("restoring",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s.business,true);s.business.ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());
  s.jobs=root.AddComponent<PassengerJobs>();s.jobs.session=s;s.jobs.nav=root.AddComponent<RoadNavigation>();s.jobs.trip.Accept(5000,100,60);s.jobs.trip.Board();
  s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.taxi.body.useGravity=false;s.traffic=root.AddComponent<TrafficSystem>();s.law=root.AddComponent<DrivingLaw>();s.law.tripId="fixture";s.manners=root.AddComponent<DrivingManners>();s.manners.session=s;s.requests=root.AddComponent<PassengerRequestUI>();s.requests.session=s;
  s.requests.AdvanceSafeStop(1.9f);Check(!s.requests.SafeToRespond(),"new choice requires full two second stop");s.requests.AdvanceSafeStop(.1f);Check(s.requests.SafeToRespond(),"two second safe stop opens response");s.taxi.body.velocity=Vector3.forward;Check(!s.requests.SafeToRespond(),"moving rejects response immediately");s.taxi.body.velocity=Vector3.zero;
  s.requests.BeginKind(RequestKind.Quiet);s.requests.request.ObserveOpportunity(true);s.requests.SuspendUnansweredForSleep();s.requests.Finish();Check(s.requests.TotalSatisfactionDelta==0,"sleep neutralizes earlier unanswered quiet opportunity");
  s.requests.BeginKind(RequestKind.Comfort);s.requests.request.Accept("keep observing");s.requests.SuspendUnansweredForSleep();Check(s.requests.request.Accepted&&!s.requests.request.SafetyDeferred,"sleep retains already accepted observation");

  var behavior=PassengerBehaviorState.Create("food-race",PassengerType.Food);behavior.Board();typeof(PassengerJobs).GetProperty("Behavior").SetValue(s.jobs,behavior,null);typeof(PassengerJobs).GetField("boardingDistance",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s.jobs,100f);s.jobs.nav.remaining=100;s.jobs.current=new JobOffer();var ui=root.AddComponent<PassengerBehaviorUI>();ui.session=s;
  s.requests.BeginKind(RequestKind.LocalChat);typeof(PassengerRequestUI).GetField("legacy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s.requests,false);s.requests.AdvanceSafeStop(2.1f);
  Check(s.requests.Choose(PassengerReply.Alternative),"prior request I is accepted");Check(!s.jobs.RespondBehavior(1)&&behavior.Choice==-1,"same I cannot authorize food in newly exposed type panel");
  typeof(PassengerRequestUI).GetField("responseBlockedFrame",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s.requests,Time.frameCount-1);Check(s.jobs.RespondBehavior(0),"following frame type response accepted");
  s.requests.BeginKind(RequestKind.LocalChat);typeof(PassengerRequestUI).GetField("legacy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s.requests,false);s.requests.AdvanceSafeStop(2.1f);Check(!s.requests.Choose(PassengerReply.Alternative),"type response blocks newly scheduled request in reverse Update order");
  Check(typeof(PassengerBehaviorUI).GetMethod("NotifyStateChanged")!=null,"semantic sleep and food changes need bounded visible feedback");
  s.requests.request.DeferSafety();typeof(PassengerRequestUI).GetField("responseBlockedFrame",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s.requests,Time.frameCount-1);
  var sleeper=PassengerBehaviorState.Create("sleep-notice",PassengerType.Sleeping);sleeper.Board();sleeper.Advance(40,false,true,.4f,100);typeof(PassengerJobs).GetProperty("Behavior").SetValue(s.jobs,sleeper,null);ui.NotifyStateChanged();Check(ui.Showing&&!ui.CanRespond,"sleep onset is visible without an input choice");
  typeof(PassengerBehaviorUI).GetField("noticeUntil",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(ui,Time.time-1);Check(!ui.Showing,"sleep notice expires instead of leaving a persistent panel");
  var food=PassengerBehaviorState.Create("late-food",PassengerType.Food);food.Board();food.Respond(1,true);food.Advance(50,false,true,.6f,100);typeof(PassengerJobs).GetProperty("Behavior").SetValue(s.jobs,food,null);ui.NotifyStateChanged();Check(ui.Showing&&!ui.CanRespond,"actual food effect after38seconds remains visible without new consent");
  var warning=DrunkRideState.CreateAssignedDrunk("critical-warning");warning.VomitPlanned=true;warning.Advance(1,1);typeof(PassengerJobs).GetProperty("DrunkRide").SetValue(s.jobs,warning,null);Check(!ui.Showing,"critical care warning owns display over semantic notice");typeof(PassengerJobs).GetProperty("DrunkRide").SetValue(s.jobs,null,null);s.requests.BeginKind(RequestKind.Quiet);Check(!ui.Showing,"existing unanswered request owns display over semantic notice");



  var errand=PassengerBehaviorState.Create("errand-context",PassengerType.Errand);errand.Board();errand.Respond(0,true);errand.Advance(35,false,true,.4f,100);typeof(PassengerJobs).GetProperty("Behavior").SetValue(s.jobs,errand,null);s.requests.SyncBehaviorContext();Check(errand.RepresentativeStatus==PassengerRepresentativeStatus.NotPresented&&!errand.Dialogue.Contains("확인한 경유 약정"),"unpresented EV02 cannot invent route agreement");
  s.requests.BeginKind(RequestKind.ViaStop);s.requests.SyncBehaviorContext();Check(errand.RepresentativeStatus==PassengerRepresentativeStatus.Pending&&!s.requests.request.Accepted,"type acknowledgement is not route consent");Check(s.requests.Choose(PassengerReply.Defer)&&errand.RepresentativeStatus==PassengerRepresentativeStatus.Deferred&&errand.CompletionReaction.Contains("보류"),"actual request safety deferral controls type reaction");
  var comfort=PassengerBehaviorState.Create("comfort-context",PassengerType.Comfort);comfort.Board();comfort.Respond(0,true);comfort.Advance(35,false,true,.4f,100);typeof(PassengerJobs).GetProperty("Behavior").SetValue(s.jobs,comfort,null);s.requests.BeginKind(RequestKind.Comfort);Check(s.requests.Respond(true)&&comfort.RepresentativeStatus==PassengerRepresentativeStatus.Accepted,"actual comfort promise captured");s.requests.Finish();Check(comfort.RepresentativeStatus==PassengerRepresentativeStatus.Succeeded&&s.requests.TotalSatisfactionDelta==8,"successful observed promise determines reaction and scores once");
  s.requests.BeginKind(RequestKind.Comfort);s.requests.Respond(true);s.manners.accelEvents++;s.requests.Finish();Check(comfort.RepresentativeStatus==PassengerRepresentativeStatus.Failed&&s.requests.TotalSatisfactionDelta==-10,"failed observed promise determines reaction without duplicate score");
 }finally{UnityEngine.Object.DestroyImmediate(root);}}
}
