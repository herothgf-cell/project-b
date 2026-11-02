using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
// Position-controlled live adapters and isolated checkpoint checks; not manual/physical driving acceptance.
public sealed class PassengerActivitiesProbe : MonoBehaviour {
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){var a=Environment.GetCommandLineArgs();if(Array.IndexOf(a,"-passengerActivitiesTest")>=0||Array.IndexOf(a,"-passengerActivitiesReloadTest")>=0)new GameObject("Passenger activities probe").AddComponent<PassengerActivitiesProbe>();}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Stop(int index){var stop=s.jobs.stops[index];s.taxi.body.position=stop.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(stop.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();s.requests.AdvanceSafeStop(2.1f);}
 void NextFrame(){typeof(PassengerRequestUI).GetField("responseBlockedFrame",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s.requests,Time.frameCount-1);}
 void Freeze(){s.business.enabled=s.jobs.enabled=s.jobs.nav.enabled=s.requests.enabled=s.law.enabled=s.manners.enabled=s.taxi.enabled=false;s.GetComponent<ServiceLawZones>().enabled=false;s.taxi.body.isKinematic=true;foreach(var car in s.traffic.cars){car.Recycle();car.enabled=false;}}
 void Fresh(){s.business.settings=PrototypeSettings.ForOneDay();s.business.ledger=new EconomyLedger(s.business.settings);s.business.passengers.Clear();s.business.enabledCampaign=s.business.useV2=true;s.business.preparing=false;s.paused=false;Time.timeScale=1;s.law.ids=new PrototypeIds("passenger-activities");s.law.ledger=new LawLedger();s.law.tripId="";s.jobs.ClearForDay();s.jobs.ForcedRideCase=DrunkRideCase.Random;s.jobs.ForcedPassengerType=PassengerType.General;s.GetComponent<RideCompletionPanel>().Close();}
 void Begin(PassengerType type=PassengerType.General){Fresh();s.jobs.ForcedPassengerType=type;var j=s.jobs;j.offers[0]=new JobOffer{pickup=0,drop=3,fare=5000,seconds=300,requiredScore=70,distance=j.nav.Distance(j.stops[0],j.stops[3]),candidateSeed="activities-trip"};Stop(0);Check(j.Accept(0)&&j.Interact()&&j.trip.State==TripState.OnTrip,"actual map boards passenger "+type);NextFrame();}
 PassengerJobsSnapshot RoundTrip(){var snapshot=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(s.jobs.CaptureLive()));s.jobs.RestoreLive(snapshot);NextFrame();s.requests.AdvanceSafeStop(2.1f);return snapshot;}
 void Agree(){s.requests.BeginKind(RequestKind.ViaStop);s.requests.AdvanceSafeStop(2.1f);Check(s.requests.PrepareProposal(),"existing authored via candidate available");Check(s.requests.Choose(PassengerReply.Accept)&&s.jobs.Errand!=null,"actual request explicitly agrees20second errand before route");}
 void TypedChecks(){
  Begin(PassengerType.Sleeping);var j=s.jobs;s.requests.BeginKind(RequestKind.Quiet);s.requests.request.DeferSafety();NextFrame();Check(j.RespondBehavior(0),"actual safe initial sleeping response");NextFrame();j.Behavior.Variant=0;j.Behavior.Advance(21,false,true,.3f,100);s.manners.brakeEvents++;j.AdvancePassengerActivities(1);Check(j.Behavior.JoltWoken&&!j.BehaviorAsleep&&j.SituationPending,"actual observed jolt wakes sleeping passenger and offers reassurance");RoundTrip();j.Behavior.Advance(10,false,true,.4f,100);Check(j.Behavior.JoltWoken&&!j.BehaviorAsleep,"awake flag survives live JSON and does not immediately sleep");Check(j.RespondSituation(1)&&j.Behavior.Sleeping&&j.Behavior.Quiet,"actual safe quiet reassurance resumes rest");
  Begin(PassengerType.Food);s.requests.BeginKind(RequestKind.Quiet);s.requests.request.DeferSafety();NextFrame();Check(j.RespondBehavior(1),"actual food permission response");NextFrame();j.Behavior.SpillPlanned=true;j.Behavior.Advance(20,false,true,.6f,100);j.ApplyBehaviorDirt();int clean=j.Cleanliness,score=s.manners.ledger.Score;Check(j.Behavior.ConsumptionOccurred&&!j.Behavior.SpillOccurred,"new actual food consumes before seeded spill");s.manners.necessaryBrakeEvents++;j.AdvancePassengerActivities(1);Check(j.Behavior.SpillOccurred&&j.Cleanliness==clean-6&&s.manners.ledger.Score==score,"necessary brake has one physical spill and no added driving penalty");RoundTrip();j.AdvancePassengerActivities(20);j.Behavior.Advance(20,false,true,.7f,100);j.ApplyBehaviorDirt();Check(j.Cleanliness==clean-6,"live restore and later natural food event cannot double soil");
 }
 void MainChecks(){
  TypedChecks();
  Begin();var j=s.jobs;int clean=j.Cleanliness,score=s.manners.ledger.Score;
  s.requests.Begin();NextFrame();s.manners.necessaryBrakeEvents++;j.AdvancePassengerActivities(1);Check(j.Situation.Pending==PassengerSituationKind.NecessaryBrake,"observed necessary braking creates neutral reaction");
  RoundTrip();Check(j.SituationPending&&j.Situation.Reactions==1,"pending reaction and budget survive live JSON restore");
  s.paused=true;j.AdvancePassengerActivities(100);Check(j.SituationPending&&j.Situation.Reactions==1,"pause never automatically answers");s.paused=false;Check(j.RespondSituation(0),"safe explicit reply consumed once");Check(j.Cleanliness==clean&&s.manners.ledger.Score==score&&j.InteractionSatisfactionDelta==0,"no duplicate soil or score effect");
  Begin();var accepted=j.current;int acceptedFare=j.trip.Fare;float acceptedRemaining=j.trip.Remaining;s.business.ledger.AdvanceOperation(700,0,true);j.Generate();Check(object.ReferenceEquals(j.current,accepted)&&j.trip.Fare==acceptedFare&&j.trip.Remaining==acceptedRemaining,"new demand phase only generates offers; accepted trip identity/fare/deadline unchanged");
  Begin();Agree();int via=j.PassengerViaStop,original=j.current.drop,fare=j.trip.Fare;string trip=s.law.tripId;Check(!j.Errand.Waiting&&j.Errand.Remaining==20,"acceptance preserves travel before work");Stop(via);NextFrame();Check(j.Interact()&&j.Errand.Waiting,"F at physical safe via starts waiting");float before=j.trip.Remaining;double business=s.business.ledger.Elapsed;
  j.AdvancePassengerActivities(7);j.trip.Tick(7);s.business.ledger.AdvanceOperation(7,0,true);Check(j.Errand.Remaining==13&&j.trip.Remaining==before-7&&s.business.ledger.Elapsed>=business+7,"waiting costs actual trip and business time");
  RoundTrip();Check(j.Errand.Waiting&&j.Errand.Remaining==13&&s.law.tripId==trip,"wait13seconds survives same-trip restore");
  s.paused=true;j.AdvancePassengerActivities(99);Check(j.Errand.Remaining==13,"pause does not finish work");s.paused=false;
  Stop(original);j.AdvancePassengerActivities(30);Check(j.PassengerViaPending&&j.Errand.Remaining==13&&!j.trip.ArrivalLocked,"departure cannot skip work or settle original destination");Stop(via);j.AdvancePassengerActivities(13);Check(j.Errand.Completed&&!j.PassengerViaPending&&j.current.drop==original&&s.law.tripId==trip&&j.trip.Fare==fare,"completion returns original same-trip destination without surcharge");
  j.AdvancePassengerActivities(30);Check(j.trip.Fare==fare&&j.Errand.Completed,"repeat advancement cannot restart or change fare");Stop(original);Check(j.Interact()&&!j.Active&&s.business.passengers.Count==1,"one original-trip settlement after errand");
  Begin();Agree();Stop(j.PassengerViaStop);NextFrame();Check(j.Interact(),"start saved errand fixture");j.AdvancePassengerActivities(7);Check(s.business.SavePath.Contains("artifacts")&&s.business.SaveCheckpoint(),"actual checkpoint writes only artifacts");
 }
 void ReloadChecks(){var j=s.jobs;Check(j.Active&&j.Errand!=null&&j.Errand.Waiting&&j.Errand.Remaining<=13&&j.Errand.Remaining>0,"fresh-process restore keeps remaining errand time without reroll");Check(j.Errand.TripId==s.law.tripId&&j.Errand.OriginalDestinationId==j.current.dropStopId,"fresh-process original destination/trip identity retained");float remaining=j.Errand.Remaining;Stop(j.PassengerViaStop);j.AdvancePassengerActivities(remaining);Check(j.Errand.Completed&&!j.PassengerViaPending,"restored wait finishes exactly once");}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);s=FindObjectOfType<DriveSession>();while(s&&!s.StartupReady)yield return null;yield return null;yield return null;yield return null;
  bool reload=Array.IndexOf(Environment.GetCommandLineArgs(),"-passengerActivitiesReloadTest")>=0;bool ok=false;
  try{Check(s!=null&&s.business.SavePath.Contains("artifacts"),"test session initialized with isolated save");Freeze();if(reload)ReloadChecks();else MainChecks();ok=true;}catch(Exception e){report.AppendLine(e.ToString());Debug.LogException(e);}
  Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/passenger-activities-"+(reload?"reload":"player")+".txt",(ok?"PASS":"FAIL")+" checks="+checks+"\n"+report);Debug.Log("PASSENGER_ACTIVITIES_"+(ok?"OK":"FAILED")+" checks="+checks);Application.Quit(ok?0:1);
 }
}
