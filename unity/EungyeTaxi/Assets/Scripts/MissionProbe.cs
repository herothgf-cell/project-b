using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// 08 chapter 3 stage 3 player acceptance (Eungye single): missions on the board, success/failure per kind, the
// late rule, ledger entry once per trip and the receipt row. Stops are posed, not driven; signal entries and
// moving time are fed through the same hooks the game uses.
public sealed class MissionProbe : MonoBehaviour {
 const string Flag="-missionCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;int pickupSeed;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Mission acceptance").AddComponent<MissionProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("MISSION_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Pose(MapStop p){s.taxi.body.velocity=Vector3.zero;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=p.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(p.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();}
 void Fresh(){s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);s.business.passengers.Clear();s.business.enabledCampaign=true;s.business.useV2=true;s.business.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();s.law.ids=new PrototypeIds(Guid.NewGuid().ToString("N"));s.law.ledger=new LawLedger();s.law.tripId="";s.manners.cleanStreak=0;s.manners.streakDay=0;s.business.Story.firstDispatchAccepted=true;}
 // One trip with the given mission; prepare runs while the passenger is aboard.
 PassengerResult Trip(int mission,Action<PassengerJobs> aboard,bool late=false){
  var j=s.jobs;int n=j.stops.Length;JobOffer o=null;bool accepted=false;string why="";j.ClearForDay();Array.Clear(j.cooldown,0,j.cooldown.Length);
  // Some stops refuse a call (occupied, cooldown, unreachable); try the next pickup like a player would.
  for(int tries=0;tries<n&&!accepted;tries++){int p=(pickupSeed++*5)%n,d=(p+3)%n;
   o=new JobOffer{pickup=p,drop=d,fare=6000,seconds=600,requiredScore=60,distance=j.nav.Distance(j.stops[p],j.stops[d]),safeRouteSeconds=120};
   o.mission=mission;o.missionBonus=MissionRules.Bonus(mission,o.fare);j.offers[0]=o;j.ForcedRideCase=DrunkRideCase.Normal;Pose(j.stops[p]);
   accepted=j.Accept(0);if(!accepted)why+=" "+p+":"+j.message;}
  Check(accepted,"accept "+MissionRules.Label(mission)+why);for(int i=0;i<4&&j.trip.State==TripState.ToPickup;i++)j.Interact();Check(j.trip.State==TripState.OnTrip,"board");
  var snap=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(j.CaptureLive()));snap.startDeficit=0;snap.startSatisfaction=o.requiredScore+3;j.RestoreLive(snap);
  Check(j.current.mission==mission,"saved trip keeps its mission");aboard?.Invoke(j);
  if(j.DrunkRide!=null)j.DrunkRide.Advance(30,.8f);j.trip.Tick(late?700:30);Pose(j.stops[o.drop]);
  int before=s.business.passengers.Count;for(int i=0;i<4&&j.trip.State==TripState.OnTrip;i++)j.Interact();
  if(j.trip.State!=TripState.Idle&&j.trip.State!=TripState.OnTrip)for(int i=0;i<2;i++)j.Interact();
  Check(s.business.passengers.Count==before+1,"trip settled");var r=s.business.passengers.Last();
  var panel=s.GetComponent<RideCompletionPanel>();if(panel&&panel.Open)panel.Close();s.paused=false;Time.timeScale=1;return r;
 }
 IEnumerator Start(){
  float until=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>until){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-missionOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/mission");Directory.CreateDirectory(output);
  yield return null;string error=null;PassengerResult shown=null;
  try{
   var j=s.jobs;s.business.preparing=false;s.paused=false;Time.timeScale=1;j.ClearForDay();
   // Board (before Fresh: a replaced ledger leaves the generator without calls): one mission per board.
   int boards=0,withMission=0;j.ResetOffers();for(int g=0;g<10;g++){Array.Clear(j.cooldown,0,j.cooldown.Length);if(g>0)j.Generate();boards++;var m=j.offers.Where(o=>o!=null&&o.mission!=0).ToArray();Check(m.Length<=1,"at most one mission call per board");if(m.Length==1){withMission++;int i=Array.IndexOf(j.offers,m[0]);string c=j.CallCardCaption(i,s.law.ledger.TotalScore);Check(c.Contains("[미션] ")&&c.Contains("성공 시 +"),"mission card shows icon text and extra");Check(m[0].missionBonus>=m[0].fare*MissionRules.BonusPercent(m[0].mission)/100&&m[0].missionBonus%100==0,"extra is the draft share of the fare");}}
   report.AppendLine("boards "+boards+" with mission "+withMission+" lastCall "+j.LastCallBoard+" left "+Mathf.RoundToInt((float)s.business.ledger.SecondsLeft));Check(withMission>=boards-2,"most boards carry one mission");
   Fresh();
   var express=Trip(MissionRules.Express,null);Check(express.missionKind==MissionRules.Express&&express.missionSuccess&&express.missionBonus==1800,"express: no moving time recorded -> success, +1,800");
   var green=Trip(MissionRules.Green,jobs=>{jobs.ObserveSignalEntry(false);jobs.ObserveSignalEntry(true);jobs.ObserveSignalEntry(false);jobs.ObserveSignalEntry(false);});
   Check(!green.missionSuccess&&green.missionBonus==0&&green.paid>0&&green.paid==green.baseFare+green.bonus-green.meterDeduction,"green: a stop breaks the run -> failure, fare unchanged ("+green.paid+")");
   var green2=Trip(MissionRules.Green,jobs=>{for(int i=0;i<3;i++)jobs.ObserveSignalEntry(false);});Check(green2.missionSuccess&&green2.missionBonus==1500,"green: three in a row -> +1,500");
   var coffee=Trip(MissionRules.Coffee,null);Check(coffee.missionSuccess&&coffee.missionBonus==1200,"coffee: no harsh motion -> +1,200");
   var precise=Trip(MissionRules.Precise,null);Check(precise.missionSuccess&&precise.missionBonus==1200,"precise: posed on the stop point -> +1,200");
   var slow=Trip(MissionRules.Express,jobs=>{jobs.current.missionDriveSeconds=999;});Check(!slow.missionSuccess&&slow.missionBonus==0&&slow.paid>0&&slow.paid==slow.baseFare+slow.bonus-slow.meterDeduction,"express too slow -> failure, fare unchanged");
   var late=Trip(MissionRules.Coffee,null,true);Check(late.paid==0&&!late.missionSuccess&&late.missionBonus==0,"late trip: existing 0 won rule, mission not paid");
   // "길 아는 기사": guidance hidden while aboard, success pays the tip row, detour / guidance back on fail without penalty.
   var read=s.GetComponent<DrivingReadability>();
   var know=Trip(MissionRules.KnowRoad,jobs=>{jobs.TickKnowRoad();Check(jobs.KnowRoadActive&&jobs.nav.GuidanceHidden&&jobs.nav.ShownPath.Count==0,"know-road: guidance hidden while the passenger is aboard");Check(jobs.nav.HasDestination&&!read.TurnCue.Valid&&read.GoalDistance=="길안내 꺼짐","know-road: no turn cue or route distance on the HUD");Check(jobs.KnowRoadHint.Contains("L 길안내 켜기"),"know-road: hint names the key that gives up");});
   Check(know.missionKind==MissionRules.KnowRoad&&know.missionSuccess&&know.missionBonus==900,"know-road: arrived without guidance -> +900 ("+know.missionBonus+")");
   Check(RideReceiptSummary.MissionRow(know).Contains("길 아는 기사 팁 (운임과 별도)")&&RideReceiptSummary.MissionRow(know).Contains(MissionRules.KnowRoadLine),"know-road: receipt row carries the passenger line");
   s.jobs.TickKnowRoad();Check(!s.jobs.nav.GuidanceHidden,"know-road: guidance returns after the trip");
   var detour=Trip(MissionRules.KnowRoad,jobs=>{jobs.TickKnowRoad();jobs.current.meterMetres=MissionRules.KnowRoadLimit(jobs.current)+1;jobs.TickKnowRoad();Check(jobs.current.missionVoid&&!jobs.nav.GuidanceHidden&&jobs.nav.ShownPath.Count>1,"know-road: large detour ends the mission and guidance comes back");});
   Check(!detour.missionSuccess&&detour.missionBonus==0&&detour.paid>0&&detour.paid==detour.baseFare+detour.bonus-detour.meterDeduction,"know-road detour -> failure, fare unchanged ("+detour.paid+")");
   var gaveUp=Trip(MissionRules.KnowRoad,jobs=>{jobs.TickKnowRoad();jobs.EndKnowRoad("길안내를 다시 켰어요");Check(jobs.current.missionVoid&&!jobs.KnowRoadActive,"know-road: giving up is recorded on the trip");var copy=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(jobs.CaptureLive()));Check(copy.current.missionVoid,"know-road: the ended mission survives a save");});
   Check(!gaveUp.missionSuccess&&gaveUp.paid>0&&gaveUp.paid==gaveUp.baseFare+gaveUp.bonus-gaveUp.meterDeduction,"know-road given up -> failure, fare unchanged");
   Check(s.GetComponent<DriveNotifications>().Items.Any(i=>i.text.Contains("미션 길 아는 기사 종료")),"know-road: ending posts one feed card");
   var account=s.business.ledger.Account;int day=s.business.ledger.Day;
   Check(account.Missions(day)==1800+1500+1200+1200+900,"mission extras are their own account entries ("+account.Missions(day)+")");
   Check(!account.Mission(green2.tripId,1500,day)&&account.Missions(day)==6600,"second payment for a trip is refused");
   Check(RideReceiptSummary.MissionRow(green2).Contains("미션 추가금 (운임과 별도)")&&RideReceiptSummary.MissionRow(green).Contains("실패")&&!RideReceiptSummary.Money(green2).Contains("미션"),"receipt row separate from the fare rows");
   Check(s.business.SaveCheckpoint(),"checkpoint saved");var saved=JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(s.business.SavePath));
   Check(saved.passengers.Any(x=>x.tripId==green2.tripId&&x.missionBonus==1500&&x.missionSuccess),"saved receipt keeps the mission");
   var legacy=JsonUtility.FromJson<JobOffer>("{\"pickup\":1,\"drop\":4,\"fare\":5000}");Check(legacy.mission==0&&legacy.missionBonus==0&&!legacy.missionVoid,"older offers have no mission");
   shown=green2;s.business.passengers.RemoveAll(x=>x!=green2);
  }catch(Exception e){error=e.ToString();}
  if(error==null){var panel=s.GetComponent<RideCompletionPanel>();try{if(panel.Open)panel.Close();panel.ShowLast();}catch(Exception e){Debug.LogException(e);}
   Screen.SetResolution(1600,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"receipt-mission.png"));yield return new WaitForSecondsRealtime(.6f);}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("MISSION_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
