using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
// Position-controlled Player acceptance probe; launch with -passengerRequestFailureTest -contentTest.
public sealed class PassengerRequestFailureProbe:MonoBehaviour {
 DriveSession s;int checks,serial;readonly StringBuilder report=new StringBuilder();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-passengerRequestFailureTest")>=0)new GameObject("Passenger request failure probe").AddComponent<PassengerRequestFailureProbe>();}
 void Check(bool ok,string why){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+why);if(!ok)throw new Exception(why);}
 void Stop(int i){var p=s.jobs.stops[i];s.taxi.body.position=p.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(p.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();s.requests.AdvanceSafeStop(2.1f);}
 void Freeze(){s.business.enabled=false;s.jobs.enabled=false;s.jobs.nav.enabled=false;s.requests.enabled=false;s.law.enabled=false;s.manners.enabled=false;s.GetComponent<ServiceLawZones>().enabled=false;s.taxi.enabled=false;s.taxi.body.isKinematic=true;s.taxi.body.interpolation=RigidbodyInterpolation.None;foreach(var c in s.traffic.cars){c.Recycle();c.enabled=false;}}
 void Begin(PassengerType type){s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);s.business.passengers.Clear();s.business.enabledCampaign=s.business.useV2=true;s.business.preparing=false;s.paused=false;Time.timeScale=1;s.law.ids=new PrototypeIds("request-failure-"+(serial++));s.law.ledger=new LawLedger();s.law.tripId="";s.jobs.ClearForDay();s.jobs.ForcedRideCase=DrunkRideCase.Normal;s.jobs.ForcedPassengerType=type;s.GetComponent<RideCompletionPanel>().Close();s.jobs.offers[0]=new JobOffer{pickup=0,drop=3,fare=5000,seconds=300,requiredScore=70,distance=s.jobs.nav.Distance(s.jobs.stops[0],s.jobs.stops[3]),candidateSeed="quiet-probe-"+serial};Stop(0);Check(s.jobs.Accept(0)&&s.jobs.Interact(),"actual boarding "+type);s.requests.BeginKind(RequestKind.Quiet);Check(s.requests.Respond(true),"accept Quiet through UI action");}
 void RoundTrip(){string file=Path.GetFullPath("artifacts/passenger-request-failure/live.json");File.WriteAllText(file,JsonUtility.ToJson(s.jobs.CaptureLive()));var state=JsonUtility.FromJson<PassengerJobsSnapshot>(File.ReadAllText(file));PassengerRequestUI.ValidateSnapshot(state.request);s.jobs.RestoreLive(state);s.requests.AdvanceSafeStop(2.1f);Check(s.jobs.trip.State==TripState.OnTrip,"onboard jobs and observations restored from disk JSON");}
 void LongReplyChecks(){
  string prior=ContentTuning.CaptureJson();try{foreach(int length in new[]{501,4000}){
   string line=new string('가',length);var profile=ContentJson.Object(ContentJson.Parse(prior));bool found=false;
   foreach(object item in ContentJson.Array(profile["values"])){var row=ContentJson.Object(item);if((string)row["id"]=="request.Quiet.reply.SmallTalkViolation"){row["value"]=line;found=true;}}
   Check(found,"editable Quiet dialogue ID exists");profile["contentHash"]=ContentTuning.ComputeHash(profile);ContentTuning.UseJson(ContentJson.Stringify(profile));
   Begin(PassengerType.General);Check(s.requests.TryDriverSmallTalk()&&s.requests.SmallTalkReply==line,"configured long NPC reply "+length);RoundTrip();Check(s.requests.SmallTalkReply==line&&s.requests.request.QuietViolationCount==1,"configured long reply restored intact "+length);
  }}finally{ContentTuning.UseJson(prior);}
 }
 void Checks(){
  Check(ContentTuningRuntime.TestMode&&s.business.SavePath.StartsWith(ContentTuningRuntime.TestDirectory,StringComparison.OrdinalIgnoreCase),"isolated content-test campaign path");Freeze();
  Begin(PassengerType.General);Check(s.requests.QuietPromiseActive,"accepted promise active");
  s.paused=true;Check(!s.requests.TryDriverSmallTalk(),"paused explicit input rejected");s.paused=false;Time.timeScale=0;Check(!s.requests.TryDriverSmallTalk(),"zero time scale explicit input rejected");Time.timeScale=1;
  s.jobs.Situation.Pending=PassengerSituationKind.NecessaryBrake;Check(!s.requests.TryDriverSmallTalk(),"pending safety response blocks explicit chat");s.jobs.Situation.Pending=PassengerSituationKind.None;
  s.jobs.Situation.ReSecureRemaining=2;Check(!s.requests.TryDriverSmallTalk(),"safety work blocks explicit chat");s.jobs.Situation.ReSecureRemaining=0;
  Check(s.requests.request.QuietViolationCount==0,"blocked and safety work never manufacture breach");
  Check(s.requests.TryDriverSmallTalk()&&s.requests.request.QuietViolationCount==1&&s.requests.SmallTalkReply.Length>0,"actual driver input produces NPC reply and observed violation");Check(!s.requests.TryDriverSmallTalk()&&s.requests.request.QuietViolationCount==1,"cooldown rejects repeated input");
  string evidence=s.requests.request.QuietEvidence;RoundTrip();Check(s.requests.request.QuietViolationCount==1&&s.requests.request.QuietEvidence==evidence&&!s.requests.TryDriverSmallTalk()&&s.requests.SmallTalkReply.Length>0,"disk restore preserves evidence cooldown and visible reply");
  s.requests.Finish();Check(s.requests.request.GoalOutcome==-1&&s.requests.Summary.Contains(evidence),"observation creates failed outcome with evidence");int delta=s.requests.TotalSatisfactionDelta;Check(delta<0,"failed choice applies actual negative satisfaction");s.requests.Finish();Check(s.requests.TotalSatisfactionDelta==delta,"repeated finish does not duplicate effect");RoundTrip();s.requests.Finish();Check(s.requests.TotalSatisfactionDelta==delta,"settled restore does not duplicate effect");
  Stop(s.jobs.current.drop);Check(s.jobs.Interact()&&!s.jobs.Active&&s.business.passengers.Count==1,"actual dropoff settles once");Check(s.business.passengers[0].request.Contains(evidence),"receipt retains observed failure reason");
  Begin(PassengerType.General);s.requests.Finish();Check(s.requests.request.GoalOutcome==1&&s.requests.request.QuietViolationCount==0,"Quiet with no deliberate input succeeds");
  Begin(PassengerType.Food);Check(s.jobs.BehaviorChoicesOpen&&!s.requests.TryDriverSmallTalk(),"pending passenger choice blocks remote-compatible chat action");
  LongReplyChecks();
  Begin(PassengerType.Sleeping);s.jobs.Behavior.Advance(40,false,true,.4f,100);Check(s.jobs.BehaviorAsleep&&!s.requests.TryDriverSmallTalk()&&s.requests.request.QuietViolationCount==0,"sleeping passenger blocks voluntary chat without breach");
 }
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);s=FindObjectOfType<DriveSession>();float deadline=Time.realtimeSinceStartup+30;while(s&&!s.StartupReady&&Time.realtimeSinceStartup<deadline)yield return null;yield return null;yield return null;Directory.CreateDirectory("artifacts/passenger-request-failure");bool ok=false;try{Check(s!=null&&s.StartupReady,"session ready");Checks();ok=true;}catch(Exception e){report.AppendLine(e.ToString());Debug.LogException(e);}File.WriteAllText("artifacts/passenger-request-failure/player.txt",(ok?"PASS":"FAIL")+" checks="+checks+"\n"+report);Debug.Log("PASSENGER_REQUEST_FAILURE_PLAYER_"+(ok?"OK":"FAILED")+" checks="+checks);Application.Quit(ok?0:1);}
}
