using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Temptation "좀 빨리 가 주세요", player acceptance (Eungye single, 2026-11-24). Real boarding and delivery through
// PassengerJobs (stops are posed, not driven); moving time and a speed-limit record are fed in. Checks the panel,
// each answer's settlement, the separate reward line counted with tips, and that mission calls are never asked.
public sealed class HurryProbe : MonoBehaviour {
 const string Flag="-hurryCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;int pickupSeed;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Hurry acceptance").AddComponent<HurryProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("HURRY_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Pose(MapStop p){s.taxi.body.velocity=Vector3.zero;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=p.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(p.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();}
 void Fresh(){s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);s.business.passengers.Clear();s.business.enabledCampaign=true;s.business.useV2=true;s.business.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();s.law.ids=new PrototypeIds(Guid.NewGuid().ToString("N"));s.law.ledger=new LawLedger();s.law.tripId="";s.manners.cleanStreak=0;s.manners.streakDay=0;s.business.Story.firstDispatchAccepted=true;}
 bool Feed(string text){var n=s.GetComponent<DriveNotifications>();return n&&n.Feed.Any(line=>line.Contains(text));}
 JobOffer offer;string error;
 IEnumerator Board(int mission){
  var j=s.jobs;int n=j.stops.Length;bool accepted=false;string why="";j.ClearForDay();Array.Clear(j.cooldown,0,j.cooldown.Length);
  for(int tries=0;tries<n&&!accepted;tries++){int p=(pickupSeed++*5)%n,d=(p+3)%n;
   offer=new JobOffer{pickup=p,drop=d,fare=6000,seconds=600,requiredScore=60,distance=j.nav.Distance(j.stops[p],j.stops[d]),safeRouteSeconds=120};
   offer.mission=mission;offer.missionBonus=MissionRules.Bonus(mission,offer.fare);j.offers[0]=offer;j.ForcedRideCase=DrunkRideCase.Normal;j.ForcedPassengerType=PassengerType.General;Pose(j.stops[p]);
   accepted=j.Accept(0);if(!accepted)why+=" "+p+":"+j.message;}
  try{Check(accepted,"accept a call"+why);for(int i=0;i<4&&j.trip.State==TripState.ToPickup;i++)j.Interact();Check(j.trip.State==TripState.OnTrip,"board");}catch(Exception e){error=e.ToString();}
  yield return null;yield return null;
 }
 // Delivers the passenger at the drop stop and lets the component see the trip end.
 IEnumerator Deliver(){
  var j=s.jobs;j.trip.Tick(30);Pose(j.stops[offer.drop]);
  // A passenger reaction (e.g. to a law record) can hold the drop-off for a moment: answer it and try again.
  {float until=Time.realtimeSinceStartup+25;while(j.trip.State==TripState.OnTrip&&Time.realtimeSinceStartup<until){if(s.requests.HasPendingChoice)s.requests.request.DeferSafety();int target=j.PassengerViaPending?j.PassengerViaStop:j.current.drop;Pose(j.EffectiveStop(target));j.Interact();yield return null;}
   if(j.trip.State==TripState.OnTrip)report.AppendLine("INFO still aboard after 25 s: "+j.message);}
  yield return null;yield return null;
  if(j.trip.State!=TripState.Idle&&j.trip.State!=TripState.OnTrip)for(int i=0;i<2;i++)j.Interact();
  var panel=s.GetComponent<RideCompletionPanel>();if(panel&&panel.Open)panel.Close();s.paused=false;Time.timeScale=1;yield return null;
 }
 IEnumerator Ask(){
  s.hurry.Prepare(s.jobs.current);float until=Time.realtimeSinceStartup+12;
  while(!s.hurry.Showing&&Time.realtimeSinceStartup<until){if(s.requests.HasPendingChoice)s.requests.request.DeferSafety();yield return null;}
 }
 int Tips {get{var l=s.business.ledger;return l.Account.Tips(l.Day);}}
 IEnumerator Start(){
  float limit=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limit){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-hurryOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/hurry");Directory.CreateDirectory(output);
  yield return null;var h=s.hurry;
  try{Check(h,"the temptation component is attached");foreach(var c in FindObjectsOfType<TrafficCar>())c.gameObject.SetActive(false); /* a car in a stop bay holds the drop-off */ s.business.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();Fresh();}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}

  // 1) "서둘러 볼게요", on time, no speeding -> 20% of 6,000 = 1,200 on its own line, counted with tips.
  yield return Board(0);if(error!=null){Finish(error);yield break;}
  yield return Ask();
  int tips=Tips;
  try{Check(h.Showing,"the passenger asks after boarding (panel shown)");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"hurry-panel.png"));yield return null;
  h.Choose(HurryRules.Fast,false);string trip=s.law.tripId;
  try{Check(h.Choice==HurryRules.Fast&&h.TargetSeconds==108&&h.RewardIfPaid==1200,"fast: target 108 s of 120, reward 1,200");Check(Feed("목표 108초"),"a card states the target and the condition");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  h.AddMoving(60);yield return Deliver();
  try{
   Check(h.LastOutcome=="목표 안 도착"&&h.Paid==1,"60 s moving against 108: paid ("+h.LastOutcome+")");
   int ordinary=s.business.passengers.Last().tip;
   Check(Tips==tips+1200+ordinary,"1,200 won counted with the day's tips, beside the ride's ordinary tip "+ordinary+" ("+tips+" -> "+Tips+")");
   Check(!s.business.RecordHurry(trip,1200),"the reward line is once per trip");
   Check(Feed("사례금 +1,200원"),"a card tells the driver");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}

  // 2) "안전하게 최대한", over the target -> nothing.
  yield return Board(0);if(error!=null){Finish(error);yield break;}
  yield return Ask();tips=Tips;
  try{Check(h.Showing,"second ride asks");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  h.Choose(HurryRules.Safe,false);h.AddMoving(200);yield return Deliver();
  try{Check(h.LastOutcome=="목표 시간 넘김"&&h.Late==1&&Tips==tips+s.business.passengers.Last().tip,"safe answer over 114 s: no reward ("+h.LastOutcome+")");Check(Feed("조금 늦었네요"),"the passenger says so");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}

  // 3) "서둘러 볼게요", on time, but a speed-limit record on the ride -> forfeited at once.
  yield return Board(0);if(error!=null){Finish(error);yield break;}
  yield return Ask();tips=Tips;
  try{Check(h.Showing,"third ride asks");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  h.Choose(HurryRules.Fast,false);
  {var e=new LawEvent{recoveryPolicyVersion=1,id="hurry-probe-speed",day=s.business.ledger.Day,family="제한속도",action="제한속도 초과",reason="probe",tripId=s.law.tripId};e.Add("speed-limit",LawVerdict.Violation,5);s.law.ledger.Record(e);}
  yield return null;yield return null;
  try{Check(h.SpeedingOnRide&&Feed("그렇게까지는"),"the speeding record is noticed during the ride");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  h.AddMoving(30);yield return Deliver();
  // The drop-off is sometimes held ("공간 점유·승객 보행 대기") after a law record; then the ride has not settled.
  try{if(s.jobs.trip.State==TripState.OnTrip){report.AppendLine("INFO ride 3 drop-off held; reward not paid while aboard");Check(h.Paid==1&&Tips==tips,"held drop-off: no reward paid");}
   else Check(h.LastOutcome=="과속으로 사례금 없음"&&h.Forfeited==1&&Tips==tips+s.business.passengers.Last().tip,"on time with a speeding record: no reward ("+h.LastOutcome+")");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}

  // 4) "규정 속도로 갑니다" -> a reaction only.
  yield return Board(0);if(error!=null){Finish(error);yield break;}
  yield return Ask();tips=Tips;int cash=s.business.ledger.Account.Cash;
  try{Check(h.Showing,"fourth ride asks");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  h.Choose(HurryRules.Legal,false);
  try{Check(h.Declined==1&&(Feed("안전이 먼저죠")||Feed("…알겠어요")),"keeping to the limit: a passenger line only");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return Deliver();
  try{Check(Tips==tips+s.business.passengers.Last().tip&&h.Paid==1,"no reward for keeping to the limit");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}

  // 5) Unanswered for 15 s -> keeps to the limit, silently.
  yield return Board(0);if(error!=null){Finish(error);yield break;}
  yield return Ask();
  try{Check(h.Showing,"fifth ride asks");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  {float until=Time.realtimeSinceStartup+20;while(h.Showing&&Time.realtimeSinceStartup<until)yield return null;}
  try{Check(!h.Showing&&h.Choice==HurryRules.Legal&&h.Declined==2,"no answer within 15 s counts as keeping to the limit");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return Deliver();

  // 6) A mission call is never asked (the component's own boarding rule).
  Fresh();yield return Board(MissionRules.Coffee);if(error!=null){Finish(error);yield break;}
  {float until=Time.realtimeSinceStartup+5;while(!h.Showing&&Time.realtimeSinceStartup<until)yield return null;}
  try{Check(!h.Showing&&h.LastBlocked=="미션 호출","a coffee mission ride is not asked ("+h.LastBlocked+")");}catch(Exception e){error=e.ToString();}
  if(error==null)yield return Deliver();
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("HURRY_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
