using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// 08 chapter 2 player acceptance: real boarding/drop-off, tip ledger entry, receipt row, streak and feed.
// Stops are posed directly (not driven) and the smooth-stop samples are injected into the real tracker;
// this checks the game wiring, not a human driving the car. Lane-change detection is covered in Editor checks.
public sealed class GoodDrivingProbe : MonoBehaviour {
 const string Flag="-goodDrivingCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Good driving acceptance").AddComponent<GoodDrivingProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("GOOD_DRIVING_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Pose(Vector3 point,Vector3 direction){s.taxi.body.velocity=Vector3.zero;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=point+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();}
 void Stop(int index){var p=s.jobs.stops[index];Pose(p.Kerb,p.Direction);}
 // A gentle 1.5 m/s2 stop from 25 km/h ending where the taxi now stands.
 void SmoothStopHere(){var t=s.manners.good;float now=Time.time,speed=25/3.6f;var at=s.taxi.transform.position;
  for(float time=now-6;time<=now;time+=.02f){speed=Mathf.Max(0,speed-1.5f*.02f);t.Observe(time,time>=now-.02f?0:Mathf.Max(.6f,speed),time>now-.5f?-.8f:-1.5f,false,at);}}
 void Fresh(){s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);s.business.passengers.Clear();s.business.enabledCampaign=true;s.business.useV2=true;s.business.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();s.law.ids=new PrototypeIds(Guid.NewGuid().ToString("N"));s.law.ledger=new LawLedger();s.law.tripId="";s.manners.cleanStreak=0;s.manners.streakDay=0;}
 int tripNumber;
 // Each trip uses its own pickup: a used pickup is on a 60 s cooldown.
 JobOffer Offer(){int n=s.jobs.stops.Length,p=(tripNumber++*5)%n,d=(p+3)%n;return new JobOffer{pickup=p,drop=d,fare=5000,seconds=600,requiredScore=60,distance=s.jobs.nav.Distance(s.jobs.stops[p],s.jobs.stops[d])};}
 // One trip: smooth pickup, one praised lane change, precise smooth drop-off, quiet request fulfilled.
 PassengerResult Trip(bool collide){
  var j=s.jobs;j.ForcedRideCase=DrunkRideCase.Normal;var o=Offer();j.offers[0]=o;Stop(o.pickup);
  Check(j.Accept(0)&&j.trip.State==TripState.ToPickup,"accept");SmoothStopHere();
  var g=s.manners.good;Check(g.HasStop&&g.StopSmooth&&Vector3.Distance(g.StopPosition,s.manners.transform.position)<1.5f,"synthetic smooth stop recorded (time "+g.StopTime+" now "+Time.time+" smooth "+g.StopSmooth+" at "+g.StopPosition+" taxi "+s.manners.transform.position+")");
  Check(j.Interact()&&j.trip.State==TripState.OnTrip,"board");
  Check(s.manners.goodSmooth==1,"smooth pickup stop praised once (got "+s.manners.goodSmooth+")");
  var snap=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(j.CaptureLive()));snap.startDeficit=0;snap.startSatisfaction=o.requiredScore+3;j.RestoreLive(snap);
  s.requests.BeginKind(RequestKind.Quiet);s.requests.request.Accept();s.requests.request.CompleteGoal(true,"probe quiet");
  s.manners.Praise(GoodDrivingRules.Lane);if(collide)s.manners.Deduct("collision",10,"충돌",0);
  if(j.DrunkRide!=null)j.DrunkRide.Advance(30,.8f);j.trip.Tick(30);Stop(o.drop);SmoothStopHere();
  int before=s.business.passengers.Count;
  for(int i=0;i<4&&j.trip.State==TripState.OnTrip;i++)j.Interact();
  Check(s.business.passengers.Count==before+1,"trip settled with a receipt");
  return s.business.passengers.Last();
 }
 IEnumerator Start(){
  float until=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>until){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-goodDrivingOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/good-driving");Directory.CreateDirectory(output);
  yield return null;yield return null;
  PassengerResult tipped=null;string error=null;
  try{
   Fresh();var feed=s.GetComponent<DriveNotifications>();feed.ObserveFeed();
   int streak=0,tips=0;
   for(int n=0;n<10&&tipped==null;n++){
    var r=Trip(false);streak++;
    Check(r.tipPoints==5,"points: 2 smooth + 1 lane + precise + request (got "+r.tipPoints+")");
    Check(r.tipStreak==streak-1&&s.manners.cleanStreak==streak,"clean streak counts finished clean trips ("+s.manners.cleanStreak+")");
    Check(r.tipChance==Mathf.Min(80,20+50+(streak-1)*5),"tip chance from points and streak (got "+r.tipChance+")");
    Check(r.paid>0&&r.tip>=0&&r.tip<=500,"fare paid; tip within 0-500");
    if(r.tip>0){tipped=r;tips+=r.tip;}
    var shown=s.GetComponent<RideCompletionPanel>();if(shown&&shown.Open)shown.Close();s.paused=false;s.business.preparing=false;feed.ObserveFeed();
   }
   Check(tipped!=null,"a tip occurred within ten eligible trips");
   var account=s.business.ledger.Account;
   Check(account.Tips(s.business.ledger.Day)==tips,"tip is its own account entry (sum "+account.Tips(s.business.ledger.Day)+")");
   Check(tipped.tip==Mathf.Min(500,100+5/2*100+100),"tip amount 100 + 2x100 + precise 100");
   Check(!account.Tip(tipped.tripId,tipped.tip,s.business.ledger.Day)&&account.Tips(s.business.ledger.Day)==tips,"second settlement of the same trip is refused");
   Check(RideReceiptSummary.TipRow(tipped).Contains("팁 (운임과 별도)")&&!RideReceiptSummary.Money(tipped).Contains("팁"),"receipt shows the tip on its own row, fare rows unchanged");
   Check(s.business.SaveCheckpoint(),"checkpoint saved");
   var saved=JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(s.business.SavePath));
   Check(saved.passengers.Any(p=>p.tripId==tipped.tripId&&p.tip==tipped.tip),"saved receipt keeps the tip");
   Check(feed.Items.Any(i=>i.text.StartsWith("칭찬 · "))&&feed.Items.Any(i=>i.text.StartsWith("팁 +")),"feed shows praise and tip cards");
   var bad=Trip(true);
   Check(bad.tip==0&&bad.tipChance==0&&s.manners.cleanStreak==0,"collision trip: no tip, streak back to 0, nothing else lost");
   var pvp=new PvpSimulationLedger();Check(pvp.Settle("trip-1",0,5000)&&pvp.Settle("tip:trip-1",0,300)&&!pvp.Settle("tip:trip-1",0,300)&&pvp.Score(0)==5300,"PvP score takes a tip once");
   var panel=s.GetComponent<RideCompletionPanel>();s.business.passengers.Remove(bad);if(panel.Open)panel.Close();
   Check(panel.ShowLast()&&panel.Result.tip>0,"completion panel opens on the tipped receipt");
  }catch(Exception e){error=e.ToString();Debug.LogException(e);}
  if(error==null){Screen.SetResolution(1600,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"receipt-tip.png"));yield return new WaitForSecondsRealtime(.6f);}
  Finish(error);
 }
 void Finish(string error){
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("GOOD_DRIVING_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
