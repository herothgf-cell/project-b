using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Player check for the single shift-length selector and the developer end-day button.
// Uses a private temp campaign, never the player's save.
public sealed class ShiftDevEndProbe : MonoBehaviour {
 public static bool HomeCheck {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-homeBaseCheck")>=0;}}
 public static bool CareerCheck {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-careerCheck")>=0;}}
 public static bool CareerReload {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-careerReload")>=0;}}
 public static bool Enabled {get{return CareerCheck||CareerReload||HomeCheck||Array.IndexOf(Environment.GetCommandLineArgs(),"-shiftDevEndCheck")>=0;}}
 public static readonly string CampaignPath=Path.Combine(Path.GetTempPath(),"taxi-shift-"+Guid.NewGuid().ToString("N"),"campaign.json");
 static bool attached;DriveSession s;int checks;bool done;float start;string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Enabled&&!attached){attached=true;new GameObject("Shift/dev end acceptance").AddComponent<ShiftDevEndProbe>();}}
 void Awake(){DontDestroyOnLoad(gameObject);start=Time.realtimeSinceStartup;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-shiftCaptureOutput");output=i>=0&&i+1<args.Length?Path.GetFullPath(args[i+1]):Path.GetFullPath("artifacts/shift-dev-end-captures");Directory.CreateDirectory(output);}
 void Update(){if(!done&&Time.realtimeSinceStartup-start>240){done=true;Debug.LogError("SHIFT_PLAYER_TIMEOUT");Application.Quit(2);}}
 void Check(bool value,string message){checks++;Debug.Log("SHIFT_PLAYER_"+(value?"PASS ":"FAIL ")+message);if(!value)throw new Exception(message);}
 IEnumerator Start(){var run=Run();while(!done){object next;try{if(!run.MoveNext())yield break;next=run.Current;}catch(Exception e){done=true;Debug.LogException(e);Application.Quit(1);yield break;}yield return next;}}
 IEnumerator Ready(){while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady||s.business.IsRestoring)yield return null;yield return null;yield return null;}
 IEnumerator Capture(string name){yield return null;yield return null;yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());Destroy(texture);Debug.Log("SHIFT_PLAYER_CAPTURE "+name);}
 IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
 IEnumerator Run(){
  yield return null;yield return null;
  FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();
  var b=s.business;
  if(CareerReload){
   Check(b.ledger.Day==5&&b.ledger.IsContinuousCareer&&b.preparing&&!b.ResumeBlocked,"same slot restores day 5");
   Check(b.ledger.Account.Room.lampOwned&&b.ledger.Account.Vehicle.modelId=="lpg"&&b.ledger.Account.Vehicle.altColorOwned&&b.ledger.Account.Vehicle.ornamentOwned,"career owns lamp and customized vehicle after process restart");
   Check(b.ledger.Account.CareerGoal=="home","selected goal survives reload");
   string previous=b.SavePath;string bytes=File.ReadAllText(previous);
   Check(b.RequestRestart()&&b.ConfirmRestart(true)&&b.SavePath!=previous&&b.ledger.Day==1,"new career opens a separate slot");
   Check(File.ReadAllText(previous)==bytes&&File.Exists(b.SavePath)&&CampaignSaveLocation.SelectedCareer(previous)==b.SavePath,"old career and fresh active slot both preserved");
   done=true;Debug.Log("CAREER_RELOAD_PLAYER_OK checks="+checks);Application.Quit(0);yield break;
  }
  if(CareerCheck){
   Check(b.SupportsCareer&&b.preparing&&b.ledger.Day==1&&b.SavePath.Contains("career-player"),"isolated three-day introduction");
   Check(b.ChooseCareerGoal("car","hybrid"),"preparation can pin a vehicle goal");
   for(int day=1;day<=3;day++){
    b.ContinueFromMenu();yield return Frames(3);
    b.ledger.RecordFare("career-fixture-"+day,12000);
    var home=s.services.ParkingStop;Pose(home.Kerb,home.Direction);
    Check(b.RequestEndDay(),"clock off day "+day);
    while(!b.ShowingSummary)yield return null;
    if(day==1)Check(b.ChangeRoom("buy","lamp",1),"fixture purchases lamp");
    if(day==2)Check(b.ChangeVehicle("buy","lpg")&&b.ChangeVehicle("color-buy")&&b.ChangeVehicle("ornament-buy"),"fixture purchases and customizes car");
    if(day<3){Check(b.BeginNextCampaignDay(),"intro advances day "+day);yield return Frames(3);}
   }
   Check(b.CanContinueCareer&&b.ChangeRoom("finish"),"completed 3-day archive is available");
   int cash=b.ledger.Account.Cash;string slot=b.SavePath;
   Check(b.BeginContinuousCareer()&&b.ledger.Day==4&&b.preparing&&b.SavePath==slot,"completed save continues at day 4 in same slot");
   Check(b.ledger.Account.Cash==cash&&b.ledger.Account.Room.lampOwned&&!b.ledger.Account.Room.finalized&&b.ledger.Account.Vehicle.modelId=="lpg","day 4 carries possessions and balance");
   Check(b.ChooseCareerGoal("home"),"goal can be changed after continuing");
   yield return Frames(3);b.ContinueFromMenu();yield return Frames(3);
   b.ledger.RecordFare("career-fixture-4",12000);var bay=s.services.ParkingStop;Pose(bay.Kerb,bay.Direction);
   Check(b.RequestEndDay(),"career day 4 settles");while(!b.ShowingSummary)yield return null;
   Check(b.RoomAvailable&&b.BeginNextCampaignDay()&&b.ledger.Day==5,"room accessible and day 5 prepares");
   Check(BusinessProgress.ParseCheckpoint(File.ReadAllText(slot)).version==4,"checkpoint format 4 written");
   done=true;Debug.Log("CAREER_PLAYER_OK checks="+checks);Application.Quit(0);yield break;
  }
  if(HomeCheck){
   Check(b.SavePath==CampaignPath&&s.services.HasHomeBase,"isolated Eungye single home");
   var home=s.services.ParkingStop;
   Check(home.name==HomeBasePresentation.ParkingName&&Mathf.Abs(home.x-296.3f)<.1f&&Mathf.Abs(home.z-506.5f)<.1f,"designated home bay");
   var places=s.GetComponent<FullMapOverlay>().Places();
   Check(places[0].home&&places[0].glyph==0&&places[0].name=="숙소"&&places.Count(p=>p.home)==1,"home is first map facility");
   b.ContinueFromMenu();yield return Frames(3);
   var rest=s.services.RestArea;
   Pose(rest.pump,rest.direction);
   Check(s.services.AtRestArea()&&s.services.CanUse(3)&&!s.services.CanUse(2),"east rest is rest only");
   Check(!s.jobs.Interact()&&b.notice==HomeBasePresentation.EndNotice,"F interaction at rest refuses end day");
   s.services.Toggle();
   Check(s.services.PostTripOpen,"F7 opens life facilities after denied clock-off");
   Check(s.services.GuideAfterTrip(2)&&s.jobs.nav.HasDestination&&s.services.destination.Contains(HomeBasePresentation.ParkingName),"F7 home guidance targets designated bay");
   Pose(home.Kerb,home.Direction);
   Check(s.jobs.Interact()&&b.ledger.State==BusinessState.Closing,"F interaction at home requests settlement");
   while(!b.ShowingSummary)yield return null;
   int rent=ContentTuning.Int("home.rent.daily",-1);
   Check(rent>0&&b.ledger.Account.Expense(1,"home-rent")==rent&&b.ledger.Account.Entries.Count(e=>e.kind=="home-rent")==1,"rent is posted exactly once");
   yield return Capture("home-settlement");
   Check(b.BeginNextCampaignDay(),"next day begins");
   yield return Frames(3);
   Check(DayPreparationPresentation.Create(b.ledger).expenses.Any(e=>e.kind=="home-rent"&&e.amount==rent),"preparation lists settled rent");
   yield return Capture("home-preparation");
   b.ContinueFromMenu();yield return Frames(3);
   Pose(rest.pump,rest.direction);
   b.ledger.Driver.fatigue=100;
   b.ledger.Driver.drowsiness.Advance(180,true,100);
   Check(b.RequestEmergencyHome()&&s.services.AtEndDayHub(),"drowsiness closure places taxi in home bay");
   while(!b.ShowingSummary)yield return null;
   var restored=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));
   Check(restored.account.version==4&&EconomyLedger.RestoreCampaign(restored.days,restored.account).Account.Expense(2,"home-rent")==rent,"home rent survives checkpoint roundtrip");
   done=true;Debug.Log("HOME_BASE_PLAYER_OK checks="+checks);Application.Quit(0);yield break;
  }
  Check(b.SavePath==CampaignPath&&b.preparing&&b.ledger.Day==1,"single uses isolated campaign and day-1 preparation");
  Check(b.CanChangeShiftLength&&b.ShiftMinutes==20,"day-1 preparation offers shift length, default 20 minutes");
  Check(!b.DevEndDayNow(),"dev end refused while preparing");
  yield return Capture("01-day1-prep-20min");
  Check(!b.SetShiftLength(15),"non-choice rejected");
  Check(b.SetShiftLength(10)&&b.ledger.Settings.daySeconds==600&&b.settings.daySeconds==600,"10 minutes applied");
  yield return Capture("02-day1-prep-10min");
  b.ContinueFromMenu();yield return Frames(10);
  Check(!b.preparing&&!b.CanChangeShiftLength&&b.ledger.SecondsLeft<=600,"locked once operating; remaining time within 10 minutes");
  yield return Capture("03-operating-10min");
  s.paused=true;Time.timeScale=0;var panel=DrivingSettingsPanel.For(s);panel.Open();panel.DevTab=true;
  yield return Capture("04-settings-dev-tab");
  Check(b.CanDevChangeShiftLength&&b.DevSetShiftLength(20)&&b.ShiftMinutes==20&&b.ledger.SecondsLeft>600,"dev tab changes shift length mid-shift");
  yield return Capture("04b-settings-dev-tab-20min");
  Check(b.DevEndDayNow(),"dev end accepted while operating");panel.Cancel();
  float wait=Time.realtimeSinceStartup;while(!b.ShowingSummary&&Time.realtimeSinceStartup-wait<20)yield return null;
  Check(b.ShowingSummary&&b.ledger.Last!=null&&b.ledger.Last.endReason=="개발용 바로 퇴근"&&b.ledger.Last.day==1,"day 1 settles with dev end reason");
  yield return Capture("05-summary-after-dev-end");
  Check(b.BeginNextCampaignDay(),"day 2 preparation opens");yield return Frames(10);
  Check(b.preparing&&b.ledger.Day==2&&b.ShiftMinutes==20&&b.CanChangeShiftLength,"day 2 keeps the dev-changed 20 minutes and remains changeable");
  yield return Capture("06-day2-prep-20min");
  Check(b.SetShiftLength(30)&&b.ShiftMinutes==30,"day 2 changed to 30 minutes");
  yield return Capture("07-day2-prep-30min");
  var saved=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));
  Check(EconomyLedger.RestoreCampaign(saved.days,saved.account).Settings.daySeconds==1800,"saved campaign restores 30 minutes");
  done=true;Debug.Log("SHIFT_PLAYER_OK checks="+checks);Application.Quit(0);
 }
 void Pose(Vector3 kerb,Vector3 direction){
  var position=kerb+Vector3.up*.65f;
  var rotation=Quaternion.LookRotation(direction);
  s.taxi.body.position=position;s.taxi.body.rotation=rotation;
  s.taxi.transform.SetPositionAndRotation(position,rotation);
  s.taxi.body.velocity=Vector3.zero;s.taxi.body.angularVelocity=Vector3.zero;
  Physics.SyncTransforms();
 }
}
