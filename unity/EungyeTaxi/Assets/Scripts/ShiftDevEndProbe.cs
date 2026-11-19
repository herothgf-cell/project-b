using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Player check for the single shift-length selector and the developer end-day button.
// Uses a private temp campaign, never the player's save.
public sealed class ShiftDevEndProbe : MonoBehaviour {
 public static bool HomeCheck {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-homeBaseCheck")>=0;}}
 public static bool CabinCheck {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-cabinControlsCheck")>=0;}}
 public static bool CareerCheck {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-careerCheck")>=0;}}
 public static bool CareerReload {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-careerReload")>=0;}}
 public static bool FatherStoryCheck {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-fatherStoryCheck")>=0;}}
 public static bool FatherStoryReload {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-fatherStoryReloadCheck")>=0;}}
 public static string FatherStorySavePath {get{var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-shiftCaptureOutput");return Path.GetFullPath(Path.Combine(index>=0&&index+1<args.Length?args[index+1]:"artifacts/father-story-player","campaign.json"));}}
 public static readonly bool DailyCheck=Array.IndexOf(Environment.GetCommandLineArgs(),"-dailyConditionCheck")>=0;
 public static readonly bool DailyReload=Array.IndexOf(Environment.GetCommandLineArgs(),"-dailyConditionReloadCheck")>=0;
 public static string DailySavePath {get{var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-shiftCaptureOutput");return Path.GetFullPath(Path.Combine(index>=0&&index+1<args.Length?args[index+1]:"artifacts/daily-condition-player","campaign.json"));}}
 public static bool Enabled {get{return DailyCheck||DailyReload||FatherStoryCheck||FatherStoryReload||CabinCheck||CareerCheck||CareerReload||HomeCheck||Array.IndexOf(Environment.GetCommandLineArgs(),"-shiftDevEndCheck")>=0;}}
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
  if(DailyCheck||DailyReload){yield return RunDailyConditions(b);yield break;}
  if(FatherStoryCheck||FatherStoryReload){yield return RunFatherStory(b);yield break;}
  if(CabinCheck){
   Check(b.CabinEnabled&&b.SavePath==CampaignPath,"isolated single-player cabin fixture");
   DrivingSettings.Current.cabinAssist=true;
   Check(b.CabinAutomatic,"first shift starts with automatic support");
   DrivingSettings.Current.cabinAssist=false;
   s.taxi.body.detectCollisions=false;s.taxi.body.useGravity=false;s.taxi.enabled=false;
   foreach(var car in s.traffic.cars)if(car)car.gameObject.SetActive(false);
   b.ContinueFromMenu();yield return Frames(3);
   var jobs=s.jobs;jobs.ForcedRideCase=DrunkRideCase.Normal;
   bool reserved=b.CabinInput(1);int firstVacancy=b.Cabin.Vacancy;
   bool rested=b.CabinInput(1);
   Check(reserved&&rested&&b.Cabin.OffDuty&&jobs.offers.All(o=>o==null),"off duty clears and suppresses call offers (input="+reserved+"/"+rested+", vacancy="+firstVacancy+"/"+b.Cabin.Vacancy+", active="+jobs.Active+", running="+b.DrowsinessRunning+", blocked="+s.FullMapOpen+"/"+s.SettingsOpen+"/"+(s.accident&&s.accident.Locked)+")");
   Check(b.CabinInput(1)&&!b.Cabin.OffDuty,"vacancy restores calls");
   for(int i=0;i<2;i++){
    jobs.ClearForDay();s.GetComponent<RideCompletionPanel>().Close();
    var pick=jobs.stops[0];var drop=jobs.stops[3];Pose(pick.Kerb,pick.Direction);
    jobs.offers[0]=new JobOffer{pickup=0,drop=3,fare=6000,seconds=300,requiredScore=60,
      distance=jobs.nav.Distance(pick,drop),candidateSeed="meter-check-"+i};
    Check(jobs.Accept(0)&&jobs.Interact()&&jobs.trip.State==TripState.OnTrip,"boards trip "+i);
    if(i==1){
     Check(b.CabinInput(0)&&b.Cabin.MeterOn,"J enables meter on board");
     PassengerBehaviorState behavior=null;
     for(int n=0;n<200;n++){
      var choice=PassengerBehaviorState.CreateConfigured("cabin-probe",PassengerType.Comfort,null,"cabin-"+n);
      if(choice.HasTrait("body.temperatureSensitive")){behavior=choice;break;}
     }
     Check(behavior!=null&&behavior.Board(),"config contains boarded comfort trait");
     if(behavior.Type!=PassengerType.General&&behavior.Choice<0)behavior.Respond(0,true);
     typeof(PassengerJobs).GetProperty("Behavior").SetValue(jobs,behavior,null);
     yield return Frames(2);
     s.requests.AdvanceSafeStop(2.1f);
     if(s.requests.HasPendingChoice){s.requests.Choose(PassengerReply.Defer);yield return Frames(2);}
     Check(jobs.RespondBehavior(behavior.TemperaturePreference)&&behavior.TemperatureSetting==CabinControlsState.Off,"answer does not switch manual air conditioning");
     jobs.AdvanceBehavior((float)behavior.Content.Number("effect.temperature.discomfortSeconds",20));
     Check(behavior.TraitSatisfactionDelta==behavior.Content.Int("effect.temperature.mismatchSatisfaction",-2),"mismatched actual temperature penalizes the request");
     for(int k=0;k<3&&b.Cabin.Temperature!=behavior.TemperaturePreference;k++)Check(b.CabinInput(2),"L changes physical air conditioning");
     jobs.AdvanceBehavior((float)behavior.Content.Number("effect.temperature.comfortSeconds",15));
     Check(behavior.TraitSatisfactionDelta==behavior.Content.Int("effect.temperature.matchSatisfaction",3),"maintained matching air conditioning satisfies the request");
     var checkpoint=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));
     Check(checkpoint.version==CampaignCheckpoint.CurrentVersion&&checkpoint.live.cabinCaptured&&checkpoint.live.cabin.MeterOn&&checkpoint.live.cabin.Temperature==behavior.TemperaturePreference,"active controls captured in the checkpoint");
    }
    Pose(drop.Kerb,drop.Direction);
    Check(jobs.Interact()&&!jobs.Active,"successful alighting "+i);
    var result=b.passengers.Last();
    Check(i==0?result.meterDeduction>0&&result.paid>0:result.meterDeduction==0&&result.paid>0,"correct payment for meter state "+i);
   }
   Check(jobs.AcceptanceRejection(new JobOffer{pickup=0,drop=3}).Contains("미터기"),"stale meter blocks accepting another call");
   s.GetComponent<RideCompletionPanel>().Close();
   Check(b.CabinInput(0)&&!b.Cabin.MeterOn&&b.CabinCallBlock=="","J clears stale meter");
   var cockpit=CockpitUI.CabinBounds(1600,900,true);
   Check(cockpit.xMin>CockpitUI.FirstPersonSpeedRect(1600,900).xMax&&cockpit.xMax<1112,"cabin panel fits between speedometer and passenger");
   done=true;Debug.Log("CABIN_PLAYER_OK checks="+checks);Application.Quit(0);yield break;
  }
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
    b.ledger.RecordFare("career-fixture-"+day,16000);
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
   Check(BusinessProgress.ParseCheckpoint(File.ReadAllText(slot)).version==CampaignCheckpoint.CurrentVersion,"current checkpoint format written");
   done=true;Debug.Log("CAREER_PLAYER_OK checks="+checks);Application.Quit(0);yield break;
  }
  if(HomeCheck){
   Check(b.SavePath==CampaignPath&&s.services.HasHomeBase,"isolated Eungye single home");
   var home=s.services.ParkingStop;
   var map=s.world.map;
   Check(home.name==HomeBasePresentation.ParkingName&&HomeGarage.Contains(map,home.Kerb),"designated bay is inside home building");
   var door=HomeGarage.Door(map);
   var outside=home.Direction;
   CaptureGarageView("home-garage-exterior",door+outside*19+TrafficSystem.Right(outside)*6+Vector3.up*5,door+Vector3.up*2);
   CaptureGarageView("home-garage-interior",home.Kerb-outside*8+Vector3.up*2.8f,door+Vector3.up*1.4f);
   var places=s.GetComponent<FullMapOverlay>().Places();
   Check(places[0].home&&places[0].glyph==0&&places[0].name=="숙소"&&places.Count(p=>p.home)==1,"home is first map facility");
   b.ContinueFromMenu();yield return Frames(3);
   var approach=s.services.StopFor(2);
   Pose(approach.V(),approach.Direction);
   Check(!s.services.AtEndDayHub()&&!b.RequestEndDay(),"road at garage approach cannot close shift");
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
   Check(restored.account.version==b.ledger.Account.Snapshot().version&&EconomyLedger.RestoreCampaign(restored.days,restored.account).Account.Expense(2,"home-rent")==rent,"home rent survives checkpoint roundtrip");
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
 IEnumerator RunDailyConditions(BusinessProgress b){
  Check(b.DailyConditionsEnabled&&b.SavePath==DailySavePath&&!b.ResumeBlocked,"isolated daily conditions career");
  if(DailyReload){
   Check(b.ledger.Day==6&&b.preparing,"sixth day restores in preparation");
   var saved=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));
   Check((int)b.DayCondition==saved.conditionKind&&saved.conditionFirstDay==1,"reload preserves the sixth-day card and seed");
   Check(saved.days.records.Length==5&&saved.days.records.All(day=>day.conditionRecorded)&&saved.days.records.Select(day=>day.conditionKind).Distinct().Count()==5,"one recorded occurrence of every card restored");
   Check(b.Story.HasPage(1)&&b.Story.HasPage(2),"rain and Friday notebook pages persist");
   done=true;Debug.Log("DAILY_CONDITION_PLAYER_OK checks="+checks);Application.Quit(0);yield break;
  }
  s.taxi.body.detectCollisions=false;s.taxi.body.useGravity=false;s.taxi.enabled=false;
  var seen=new System.Collections.Generic.HashSet<DailyConditionKind>();
  for(int day=1;day<=5;day++){
   var condition=b.DayCondition;
   Check(b.ledger.Day==day&&b.preparing&&seen.Add(condition)&&condition==DailyConditions.ForDay(s.law.ids.campaign,day),"different seeded condition on day "+day+" · "+condition);
   b.ContinueFromMenu();yield return Frames(3);
   Check(!b.preparing&&!RainPolicy.Enabled,"day starts in dry morning");
   if(condition==DailyConditionKind.Rain){
    b.ledger.Advance(b.ledger.Settings.daySeconds*.45,0);yield return Frames(3);
    Check(RainPolicy.Enabled&&RainPolicy.BoardingDirt==2,"rain starts after noon with existing boarding dirt");
    var jobs=s.jobs;var pick=jobs.stops[0];var drop=jobs.stops[3];
    jobs.ForcedRideCase=DrunkRideCase.Normal;Pose(pick.Kerb,pick.Direction);
    jobs.offers[0]=new JobOffer{pickup=0,drop=3,fare=6000,seconds=300,requiredScore=60,distance=jobs.nav.Distance(pick,drop),candidateSeed="rain-daily"};
    int dirt=b.ledger.Care.Cleanliness;
    Check(jobs.Accept(0)&&jobs.Interact()&&jobs.trip.State==TripState.OnTrip&&b.ledger.Care.Cleanliness==dirt-2,"actual rain boarding deducts two cleanliness");
    Pose(drop.Kerb,drop.Direction);Check(jobs.Interact()&&!jobs.Active,"rain ride completes");
    yield return Frames(2);s.GetComponent<RideCompletionPanel>().Close();
    if(!b.CabinAutomatic)b.CabinInput(0);
   }
   if(condition==DailyConditionKind.Friday)Check(b.Story.HasPage(2),"first Friday unlocks father notebook page three");
   Pose(s.services.ParkingStop.Kerb,s.services.ParkingStop.Direction);
   Check(b.RequestEndDay(),"clocks off with active condition "+condition);
   while(!b.ShowingSummary)yield return null;
   Check(b.ledger.Last.conditionRecorded&&b.ledger.Last.conditionKind==(int)condition,"settlement records condition "+condition);
   if(condition==DailyConditionKind.Rain)Check(!RainPolicy.Enabled&&b.Story.HasPage(1),"settled rain closes weather and opens notebook page two");
   Check(day==3?b.BeginContinuousCareer():b.NextDay(),"condition rotates through day "+(day+1));yield return Frames(3);
  }
  Check(b.SaveCheckpoint(),"sixth preparation saves for fresh process");
  done=true;Debug.Log("DAILY_CONDITION_PLAYER_OK checks="+checks);Application.Quit(0);
 }
 IEnumerator RunFatherStory(BusinessProgress b){
  if(FatherStoryReload){
   Check(!b.ResumeBlocked&&b.ledger.Day==2&&b.ShowingSummary,"second day restores from the saved slot");
   Check(b.Story.introSeen&&b.Story.FoundNotebook&&b.Story.HasPage(3),"intro and notebook flags restored");
   Check(b.WallMemoChoice=="none"&&b.Story.answers.Count==1&&b.Story.answers[0].choice==1,"wall memo and passenger answer persist");
   Check(b.BeginNextCampaignDay()&&b.preparing,"third shift prepares from the existing slot");
   b.ContinueFromMenu();Check(!b.preparing&&!b.Intro,"continuing existing career does not play intro");
   Pose(s.services.ParkingStop.Kerb,s.services.ParkingStop.Direction);
   Check(b.RequestEndDay(),"third shift can end from home");
   while(!b.ShowingSummary)yield return null;
   Check(b.WallMemoPrompt&&b.ChooseCareerGoal("home")&&!b.WallMemoPrompt,"day three prompts again and accepts a different wall memo");
   done=true;Debug.Log("FATHER_STORY_PLAYER_OK checks="+checks);Application.Quit(0);yield break;
  }
  Check(b.StoryEnabled&&b.preparing&&!b.Story.introSeen&&b.SavePath==FatherStorySavePath,"fresh isolated Eungye career awaits intro");
  Check(IntroCutscene.DurationSeconds==26,"intro has 26 seconds of authored cuts");
  DrivingSettings.Current.cabinAssist=false;
  s.taxi.body.detectCollisions=false;s.taxi.body.useGravity=false;s.taxi.enabled=false;
  foreach(var car in s.traffic.cars)if(car)car.gameObject.SetActive(false);
  float startIntro=Time.realtimeSinceStartup;
  b.ContinueFromMenu();Check(b.Intro&&b.Intro.IsPlaying&&!b.Intro.IsReplay&&b.Intro.CurrentCut==0,"first career opens first intro cut");
  int cuts=0;
  while(b.Intro.IsPlaying){cuts|=1<<b.Intro.CurrentCut;yield return null;}
  Check(cuts==7&&Time.realtimeSinceStartup-startIntro<=30,"uncut intro plays all panels within 30 seconds");
  Check(!b.preparing&&b.Story.introSeen&&b.Cabin.OffDuty&&s.drivingCamera.FirstPerson&&s.services.AtEndDayHub(),"intro ends in home cockpit with vacant lamp off");
  yield return null;
  Check(b.CabinInput(1)&&b.Story.firstVacancy&&!b.Cabin.OffDuty,"K enables available vacancy");
  var jobs=s.jobs;var pick=jobs.stops[0];var drop=jobs.stops[3];
  jobs.ForcedPassengerType=PassengerType.General;jobs.ForcedRideCase=DrunkRideCase.Normal;
  Pose(pick.Kerb,pick.Direction);
  jobs.offers[0]=new JobOffer{pickup=0,drop=3,fare=6000,seconds=300,requiredScore=60,distance=jobs.nav.Distance(pick,drop),candidateSeed="father-first"};
  Check(jobs.Accept(0)&&jobs.Interact()&&jobs.trip.State==TripState.OnTrip,"first passenger boards");
  Check(b.notice.Contains("미터기")&&b.CabinInput(0)&&b.Cabin.MeterOn,"first passenger prompts J and meter starts");
  Pose(drop.Kerb,drop.Direction);Check(jobs.Interact()&&!jobs.Active,"first passenger alights");
  Check(b.Story.FoundNotebook&&b.notice.Contains("수첩")&&!b.Story.HasPage(1)&&!b.Story.HasPage(2),"first ride discovers notebook with later condition pages locked");
  yield return Frames(2);s.GetComponent<RideCompletionPanel>().Close();b.CabinInput(0);
  Pose(s.services.ParkingStop.Kerb,s.services.ParkingStop.Direction);
  Check(b.RequestEndDay(),"first shift ends from home");
  while(!b.ShowingSummary)yield return null;
  Check(b.Story.HasPage(3)&&b.ledger.Account.Expense(1,"home-rent")>0&&b.StoryMonologue.Contains("월세"),"lawful first day opens page four and records rent");
  Check(b.WallMemoPrompt&&b.ChooseCareerGoal("none")&&!b.WallMemoPrompt,"first night no-goal wall memo saves");
  Check(b.PlayIntroAgain()&&b.Intro.IsReplay,"menu can replay the opening");
  b.Intro.Skip();yield return null;
  Check(b.ShowingSummary&&b.Story.introSeen&&!b.Intro.IsPlaying,"skip returns to the same career state");
  Check(b.BeginNextCampaignDay(),"day two preparation opens · state="+b.ledger.State+", fullMap="+s.FullMapOpen+", save="+b.saveStatus);yield return Frames(3);
  b.ContinueFromMenu();Check(!b.preparing&&!b.Intro.IsPlaying,"first intro does not repeat on the next day");
  jobs.ForcedRideCase=DrunkRideCase.Random;jobs.ForcedPassengerType=PassengerType.Talkative;
  Pose(pick.Kerb,pick.Direction);
  jobs.offers[0]=new JobOffer{pickup=0,drop=3,fare=6000,seconds=300,requiredScore=60,distance=jobs.nav.Distance(pick,drop),candidateSeed="father-talk"};
  Check(jobs.Accept(0)&&jobs.Interact()&&jobs.trip.State==TripState.OnTrip,"talkative passenger boards on day two");
  Check(b.Story.Pending(s.law.tripId)&&!string.IsNullOrEmpty(b.Story.Question()),"day-two question is authored and bound to trip");
  yield return Frames(2);s.requests.AdvanceSafeStop(2.1f);
  if(s.requests.HasPendingChoice){s.requests.Choose(PassengerReply.Defer);yield return Frames(2);}
  if(jobs.BehaviorChoicesOpen){jobs.RespondBehavior(0);yield return Frames(2);}
  Check(b.AnswerStory(1)&&b.StorySatisfaction==1&&!b.AnswerStory(0),"one optional answer grants S+1");
  b.CabinInput(0);Pose(drop.Kerb,drop.Direction);Check(jobs.Interact()&&!jobs.Active,"talkative ride completes");
  yield return Frames(2);s.GetComponent<RideCompletionPanel>().Close();b.CabinInput(0);
  Pose(s.services.ParkingStop.Kerb,s.services.ParkingStop.Direction);
  Check(b.RequestEndDay(),"day two ends from home");
  while(!b.ShowingSummary)yield return null;
  var saved=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));
  Check(saved.version==CampaignCheckpoint.CurrentVersion&&saved.story.answers.Count==1&&saved.story.FoundNotebook,"current checkpoint has narrative progress");
  done=true;Debug.Log("FATHER_STORY_PLAYER_OK checks="+checks);Application.Quit(0);
 }
 void CaptureGarageView(string name,Vector3 position,Vector3 lookAt){
  var camera=new GameObject("Home garage inspection camera").AddComponent<Camera>();
  var texture=new RenderTexture(960,540,24);
  var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
  var previous=RenderTexture.active;
  try{
   camera.enabled=false;
   camera.transform.position=position;
   camera.transform.LookAt(lookAt);
   camera.nearClipPlane=.15f;
   camera.targetTexture=texture;
   camera.Render();
   RenderTexture.active=texture;
   pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);
   pixels.Apply();
   File.WriteAllBytes(Path.Combine(output,name+".png"),pixels.EncodeToPNG());
  }finally{
   camera.targetTexture=null;
   RenderTexture.active=previous;
   texture.Release();
   Destroy(texture);
   Destroy(pixels);
   Destroy(camera.gameObject);
  }
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
