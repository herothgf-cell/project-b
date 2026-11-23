using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Player check for the single shift-length selector and the developer end-day button.
// Uses a private temp campaign, never the player's save.
public sealed class ShiftDevEndProbe : MonoBehaviour {
 static readonly string PartsReport=ContentTuningRuntime.Argument("-workshopPartsReport");
 public static readonly bool FacilityArtCheck=Array.IndexOf(Environment.GetCommandLineArgs(),"-facilityArtCheck")>=0;
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
 public static bool Enabled {get{return PartsReport!=null||FacilityArtCheck||DailyCheck||DailyReload||FatherStoryCheck||FatherStoryReload||CabinCheck||CareerCheck||CareerReload||HomeCheck||Array.IndexOf(Environment.GetCommandLineArgs(),"-shiftDevEndCheck")>=0;}}
 public static readonly string CampaignPath=Path.Combine(Path.GetTempPath(),"taxi-shift-"+Guid.NewGuid().ToString("N"),"campaign.json");
 static bool attached;DriveSession s;int checks;bool done;float start;string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Enabled&&!attached){attached=true;new GameObject("Shift/dev end acceptance").AddComponent<ShiftDevEndProbe>();}}
 void Awake(){DontDestroyOnLoad(gameObject);start=Time.realtimeSinceStartup;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-shiftCaptureOutput");output=i>=0&&i+1<args.Length?Path.GetFullPath(args[i+1]):Path.GetFullPath("artifacts/shift-dev-end-captures");Directory.CreateDirectory(output);}
 void Update(){if(!done&&Time.realtimeSinceStartup-start>240){done=true;Debug.LogError("SHIFT_PLAYER_TIMEOUT");Application.Quit(2);}}
 void Check(bool value,string message){checks++;Debug.Log("SHIFT_PLAYER_"+(value?"PASS ":"FAIL ")+message);if(!value)throw new Exception(message);}
 IEnumerator Start(){var run=Run();while(!done){object next;try{if(!run.MoveNext())yield break;next=run.Current;}catch(Exception e){done=true;Debug.LogException(e);Application.Quit(1);yield break;}yield return next;}}
 IEnumerator Ready(){while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady||s.business.IsRestoring)yield return null;yield return null;yield return null;}
 IEnumerator Capture(string name){yield return null;yield return null;yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());Destroy(texture);Debug.Log("SHIFT_PLAYER_CAPTURE "+name);}
 bool measureScore,scoreFits;
 void OnGUI(){
  if(!measureScore||Event.current.type!=EventType.Repaint)return;
  KoreanText.PrepareSkin();
  float w=Screen.width/CockpitUI.Scale,h=Screen.height/CockpitUI.Scale;
  Rect label=CockpitUI.ScoreLabelBounds(w,h,true),button=CockpitUI.NotebookBounds(w,h);
  var text=DrivingHudArt.Text(18,null,true).CalcSize(new GUIContent("운전 점수  100"));
  var caption=KoreanText.Button(UiReadability.Font(14)).CalcSize(new GUIContent("기사 수첩"));
  scoreFits=text.x<=label.width&&text.y<=label.height&&!label.Overlaps(button)&&caption.x<=button.width&&caption.y<=button.height;
  if(s.drivingCamera.FirstPerson){
   scoreFits&=s.mirrors&&s.mirrors.Frame(0).yMax+4<label.yMin*CockpitUI.Scale;
  }
  measureScore=false;
 }
 IEnumerator CaptureScoreCard(){
  bool firstPerson=s.drivingCamera.FirstPerson;
  foreach(int width in new[]{1280,1600,1920}){
   Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);
   yield return new WaitForSecondsRealtime(.6f);
   foreach(bool cockpit in new[]{false,true}){
    s.drivingCamera.SetFirstPerson(cockpit);
    measureScore=true;
    yield return null;yield return null;yield return new WaitForEndOfFrame();
    Check(!measureScore&&scoreFits&&s.law.ledger.TotalScore==100,"full score and notebook fit "+Screen.width+" cockpit="+cockpit);
    ScreenCapture.CaptureScreenshot(Path.Combine(output,"driver-notebook-"+Screen.width+(cockpit?"-first":"-third")+".png"));
    yield return new WaitForSecondsRealtime(.4f);
   }
  }
  s.drivingCamera.SetFirstPerson(firstPerson);
 }
 IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
 IEnumerator Run(){
  yield return null;yield return null;
  FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();
  var b=s.business;
  if(PartsReport!=null){var runParts=RunWorkshopParts(b);while(runParts.MoveNext())yield return runParts.Current;yield break;}
  if(FacilityArtCheck){yield return RunFacilityArt(b);yield break;}
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
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-taxiAudioCheck")>=0)yield return CheckTaxiAudio();
   var jobs=s.jobs;jobs.ForcedRideCase=DrunkRideCase.Normal;
   var offer=jobs.offers[0];
   Check(offer!=null&&b.CabinInput(CabinAction.Window)&&b.Cabin.WindowOpen&&jobs.offers[0]==offer,"opening window preserves first offer");
   Check(b.CabinInput(CabinAction.Window)&&!b.Cabin.WindowOpen,"window closes");
   var legacy=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath)).live;
   legacy.cabin=JsonUtility.FromJson<CabinControlsState>("{\"Vacancy\":2,\"Temperature\":1}");
   Array.Clear(legacy.jobs.offers,0,legacy.jobs.offers.Length);
   Array.Clear(legacy.jobs.offerPresent,0,legacy.jobs.offerPresent.Length);
   var restore=typeof(BusinessProgress).GetMethod("RestoreLive",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
   restore.Invoke(b,new object[]{legacy});
   Check(!b.ResumeBlocked&&b.Cabin.Target==20&&jobs.offers[0]!=null,"old off-duty cooling save restores temperature and repopulates empty calls");
   b.ContinueFromMenu();
   Check(b.CabinInput(CabinAction.Warmer)&&b.CabinInput(CabinAction.Warmer),"neutralize migrated cooling setpoint");
   yield return new WaitForSeconds(4);
   for(int i=0;i<2;i++){
    jobs.ClearForDay();s.GetComponent<RideCompletionPanel>().Close();
    var pick=jobs.stops[0];var drop=jobs.stops[3];Pose(pick.Kerb,pick.Direction);
    jobs.offers[0]=new JobOffer{pickup=0,drop=3,fare=6000,seconds=300,requiredScore=60,
      distance=jobs.nav.Distance(pick,drop),candidateSeed="meter-check-"+i};
    if(i==0){
     // Reserve the general pool in this fixture, then exercise its production selection path.
     string seed="cabin-window-7";
     jobs.offers[0].candidateSeed=seed;
     Check(jobs.Accept(0),"accepts window passenger fixture");
     var fixtureCare=new VehicleCare();
     fixtureCare.ReserveStoryPassenger(seed,PassengerType.General);
     var prepare=typeof(PassengerJobs).GetMethod("PrepareOrdinaryPassenger",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
     var rider=(PassengerBehaviorState)prepare.Invoke(jobs,new object[]{fixtureCare});
     typeof(PassengerJobs).GetProperty("Behavior").SetValue(jobs,rider);
     Check(jobs.Interact()&&rider.Boarded&&rider.RequestedControl==PassengerBehaviorState.CabinWish.OpenWindow,"boards a passenger who asks for the closed window to open");
     Check(b.CabinInput(CabinAction.Window)&&rider.CabinWishMet&&rider.TraitSatisfactionDelta==2,"K fulfills actual passenger request");
     var stored=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));
     Check(stored.live.jobs.behavior.CabinWishMet,"checkpoint retains fulfilled request");
     Check(s.GetComponent<VehicleAudio>()&&s.GetComponents<AudioSource>().Length>=4,"local vehicle audio attached in player");
    }else Check(jobs.Accept(0)&&jobs.Interact()&&jobs.trip.State==TripState.OnTrip,"boards trip "+i);
    if(i==1){
     Check(b.CabinInput(CabinAction.Meter)&&b.Cabin.MeterOn,"J enables meter on board");
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
     Check(b.CabinInput(behavior.TemperaturePreference==CabinControlsState.Warm?CabinAction.Warmer:CabinAction.Cooler),"set requested temperature");
     yield return new WaitForSeconds(2);
     Check(b.Cabin.Comfort==behavior.TemperaturePreference,"cabin reaches requested temperature");
     jobs.AdvanceBehavior((float)behavior.Content.Number("effect.temperature.comfortSeconds",15));
     Check(behavior.TraitSatisfactionDelta==behavior.Content.Int("effect.temperature.matchSatisfaction",3),"maintained matching air conditioning satisfies the request");
     var checkpoint=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));
     Check(checkpoint.version==CampaignCheckpoint.CurrentVersion&&checkpoint.live.cabinCaptured&&checkpoint.live.cabin.MeterOn&&checkpoint.live.cabin.Target==b.Cabin.Target,"active controls captured in the checkpoint");
    }
    Pose(drop.Kerb,drop.Direction);
    Check(jobs.Interact()&&!jobs.Active,"successful alighting "+i);
    var result=b.passengers.Last();
    Check(i==0?result.meterDeduction>0&&result.paid>0:result.meterDeduction==0&&result.paid>0,"correct payment for meter state "+i);
   }
   Check(jobs.AcceptanceRejection(new JobOffer{pickup=0,drop=3}).Contains("미터기"),"stale meter blocks accepting another call");
   s.GetComponent<RideCompletionPanel>().Close();
   Check(b.CabinInput(CabinAction.Meter)&&!b.Cabin.MeterOn&&b.CabinCallBlock=="","J clears stale meter");
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
   Check(places[0].home&&places[0].glyph==0&&places[0].name==HomeBasePresentation.Name&&places.Count(p=>p.home)==1,"home is first map facility");
   b.ContinueFromMenu();yield return Frames(3);
   Check(s.services.AtEndDayHub()&&Vector3.Distance(places[0].position,s.taxi.transform.position)<2,"shift starts at the mapped Deokseong Plaza bay");
   var feed=s.GetComponent<DriveNotifications>();
   var account=b.ledger.Account;account.Consume(0,12500);
   feed.ObserveFeed();feed.ObserveFeed();
   Check(feed.FuelActionVisible&&feed.Feed.Count(line=>line.StartsWith("연료 부족"))==1,"fuel threshold posts once");
   Check(s.services.GuideToFuel()&&s.jobs.nav.HasDestination,"feed action guides to reachable station");
   yield return Frames(2);
   Check(feed.Items.Any(i=>i.kind==FeedKind.Fuel)&&feed.Items.Any(i=>i.kind==FeedKind.Destination)&&feed.Items.Count(i=>i.kind==FeedKind.Tip)<=1,"feed keeps typed fuel, destination and a single TIP card");
   yield return Capture("chat-notices-fuel");
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-chatNoticeCapture")>=0){
    var signal=new LawEvent{id=s.law.ids.Next("chat-notice"),day=s.law.Day,action="정지선 침범"};signal.Add("stop-line",LawVerdict.Violation,1);s.law.ledger.Record(signal);
    var kept=new LawEvent{recoveryPolicyVersion=1,id=s.law.ids.Next("chat-notice"),day=s.law.Day,action="양보 준수"};kept.Add("yield",LawVerdict.Compliant,0);s.law.ledger.Record(kept);
    feed.RefreshScores();s.business.notice="오늘도 안전 운행 · 첫 손님을 찾아보세요";feed.ObserveFeed();yield return Frames(3);
    Check(feed.Items.Any(i=>i.kind==FeedKind.Law),"law score posts a typed law card");
    yield return Capture("chat-notices-chase");
    s.drivingCamera.SetFirstPerson(true);yield return Frames(4);yield return Capture("chat-notices-first-person");
    var zones=s.GetComponent<ServiceLawZones>();zones.ToggleTip();s.business.notice="숨긴 동안 온 알림";feed.ObserveFeed();yield return new WaitForSecondsRealtime(.4f);
    Check(feed.Unseen>0,"hidden panel counts unseen notices");yield return Capture("chat-notices-hidden");zones.ToggleTip();
    done=true;Debug.Log("CHAT_NOTICE_CAPTURE_OK checks="+checks);Application.Quit(0);yield break;
   }
   var station=s.services.Stations[0];Pose(station.pump,station.direction);
   Check(s.jobs.Interact()&&s.services.open&&s.services.LocalActions.Contains(0),"F opens fuel pump");
   s.services.Close();
   int previousTips=feed.TipsPosted;
   Time.timeScale=10;yield return new WaitForSeconds(22);Time.timeScale=1;feed.ObserveFeed();
   Check(!feed.FuelActionVisible&&!feed.Feed.Any(line=>line.StartsWith("연료 부족")),"low fuel alert expires while fuel stays low");
   Check(feed.TipsPosted>previousTips&&feed.Feed.Count(line=>line.StartsWith("TIP ·"))==1,"tips continue between notices and only the newest TIP stays");
   var dining=s.services.Restaurant;Pose(dining.parking,dining.direction);
   Check(s.jobs.Interact()&&s.services.open&&s.services.LocalActions.Contains(1),"F opens restaurant");s.services.Close();
   var shop=s.services.Workshops[0];Pose(shop.repair,shop.direction);
   Check(s.jobs.Interact()&&s.services.open&&s.services.LocalActions.Contains(5),"F opens workshop");s.services.Close();
   var approach=s.services.StopFor(2);
   Pose(approach.V(),approach.Direction);
   Check(!s.services.AtEndDayHub()&&!b.RequestEndDay(),"road at garage approach cannot close shift");
   var rest=s.services.RestArea;
   Pose(rest.pump,rest.direction);
   Check(s.services.AtRestArea()&&s.services.CanUse(3)&&!s.services.CanUse(2),"east rest is rest only");
   Check(s.jobs.Interact()&&s.services.open&&b.ledger.Operating,"F at rest opens local service without ending shift");
   s.services.Close();Check(s.services.ToggleLifeHub(),"life facilities can still open separately");
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
   Check(s.services.AtEndDayHub(),"next shift also departs from Plaza");
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
    if(!b.CabinAutomatic)b.CabinInput(CabinAction.Meter);
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
  Check(Mathf.Approximately(IntroCutscene.DurationSeconds,25)&&IntroCutscene.CutCount==4,"intro has four cuts in 25 seconds");
  // Recreate the backbuffer before capturing the initially hidden test window.
  Screen.SetResolution(1280,720,FullScreenMode.Windowed);
  yield return new WaitForSecondsRealtime(.6f);
  DrivingSettings.Current.cabinAssist=false;
  s.taxi.body.detectCollisions=false;s.taxi.body.useGravity=false;s.taxi.enabled=false;
  foreach(var car in s.traffic.cars)if(car)car.gameObject.SetActive(false);
  PersonalRoomHUD.OpenPreparationMemo(b);
  Check(b.RoomOpen&&b.preparing&&b.WallMemoChoice=="","goal card opens the unselected morning memo");
  b.RoomOpen=false;
  b.ContinueFromMenu();
  var tyres=s.taxi.transform.Cast<Transform>().Where(t=>t.name=="Tyre visual").ToArray();
  bool parkedWheels=tyres.Length==4&&s.taxi.wheels.All(w=>tyres.Any(t=>Vector3.Distance(t.position,w.transform.position)<.01f));
  Debug.Log("FIRST_DEPARTURE memo="+b.RoomOpen+" parkedWheels="+parkedWheels);
  Check(b.RoomOpen&&b.preparing&&!b.Intro&&Time.timeScale==0,"start opens the morning memo before the first intro");
  Check(parkedWheels,"four visible tyres occupy their parked axles before physics starts");
  yield return Capture("preparation-wall-memo");
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-wallMemoPointerCheck")>=0){
   foreach(var goal in new[]{"car","none","home"}){
    PersonalRoomHUD.OpenPreparationMemo(b);
    yield return Frames(2);
    Debug.Log("WALL_MEMO_AWAIT "+goal);
    float deadline=Time.realtimeSinceStartup+30;
    while(b.RoomOpen&&Time.realtimeSinceStartup<deadline)yield return null;
    Check(!b.RoomOpen&&b.WallMemoChoice==goal&&b.ledger.Account.CareerGoal==goal,"wall pointer selects "+goal);
   }
   foreach(int width in new[]{1280,1600,1920}){
    float h=width*9f/16;
    Rect chase=DriveNotifications.NoticeBounds(width,h,false),cockpit=DriveNotifications.NoticeBounds(width,h,true);
    Check(Mathf.Approximately(chase.x,cockpit.x)&&cockpit.x<width*.05f&&cockpit.yMax<h-282*UiReadability.HudScale(width,h),"camera switch keeps feed left and clear of mirror "+width);
   }
   done=true;Debug.Log("WALL_MEMO_POINTER_OK");Application.Quit(0);yield break;
  }
  Check(b.ChooseCareerGoal("home")&&b.WallMemoChoice=="home"&&!b.RoomOpen&&b.preparing,"morning memo saves the home goal and returns to preparation");
  yield return Capture("preparation-selected-goal");
  float startIntro=Time.realtimeSinceStartup;
  b.ContinueFromMenu();Check(b.Intro&&b.Intro.IsPlaying&&!b.Intro.IsReplay&&b.Intro.CurrentCut==0,"first career opens first intro cut");
  float floorY=s.services.ParkingStop.Kerb.y+HomeGarage.FloorTop;
  Check(tyres.All(t=>Mathf.Abs(t.position.y-s.taxi.wheels[0].radius-floorY)<.01f),"garage intro tyres rest on the visible floor");
  int cuts=0;
  int shotMask=0;
  while(b.Intro.IsPlaying){int c=b.Intro.CurrentCut;cuts|=1<<c;if((shotMask&(1<<c))==0&&b.Intro.CutElapsed>4.4f){shotMask|=1<<c;yield return Capture("intro-cut-"+(c+1));continue;}yield return null;}
  Check(cuts==15&&Time.realtimeSinceStartup-startIntro<=30,"uncut intro plays all four cuts within 30 seconds");
  Check(!b.preparing&&b.Story.introSeen&&s.drivingCamera.FirstPerson&&s.services.AtEndDayHub(),"intro ends in home cockpit ready for the first call");
  Check(b.FirstDriveFrame!=null,"first-day cockpit photo saved for the goal comparison");
  yield return null;
  Check(b.Story.firstVacancy&&s.jobs.offers[0]!=null,"first call automatically available");
  var roof=s.taxi.transform.Find("Taxi exterior").GetComponentsInChildren<TextMesh>().Where(t=>t.name.StartsWith("TAXI ")).ToArray();
  Check(roof.Length==2&&roof.All(t=>t.text=="TAXI"),"vacant taxi keeps TAXI on both faces of the roof sign");
  s.drivingCamera.SetFirstPerson(false);
  yield return Capture("vacant-taxi-roof");
  s.drivingCamera.SetFirstPerson(true);
  var jobs=s.jobs;var pick=jobs.stops[0];var drop=jobs.stops[3];
  jobs.ForcedPassengerType=PassengerType.General;jobs.ForcedRideCase=DrunkRideCase.Normal;
  Pose(pick.Kerb,pick.Direction);
  jobs.offers[0]=new JobOffer{pickup=0,drop=3,fare=6000,seconds=300,requiredScore=60,distance=jobs.nav.Distance(pick,drop),candidateSeed="father-first"};
  Check(jobs.Accept(0)&&jobs.Interact()&&jobs.trip.State==TripState.OnTrip,"first passenger boards");
  yield return null;
  Check(roof.All(t=>t.text=="TAXI"),"occupied taxi keeps TAXI on both faces of the roof sign");
  Check(b.notice.Contains("미터기")&&b.CabinInput(CabinAction.Meter)&&b.Cabin.MeterOn,"first passenger prompts J and meter starts");
  Pose(drop.Kerb,drop.Direction);Check(jobs.Interact()&&!jobs.Active,"first passenger alights");
  Check(b.Story.FoundNotebook&&b.notice.Contains("수첩")&&!b.Story.HasPage(1)&&!b.Story.HasPage(2),"first ride discovers notebook with later condition pages locked");
  yield return Frames(2);s.GetComponent<RideCompletionPanel>().Close();b.CabinInput(CabinAction.Meter);
  Pose(s.services.ParkingStop.Kerb,s.services.ParkingStop.Direction);
  yield return CaptureScoreCard();
  Check(b.OpenDrivingLogbook()&&b.LogbookOpen&&s.paused&&Time.timeScale==0,"HUD opens discovered notebook and pauses driving");
  yield return Capture("driver-notebook-open");
  b.LogbookOpen=false;
  Check(!s.paused&&Time.timeScale==1,"closing HUD notebook resumes driving");
  Check(b.RequestEndDay(),"first shift ends from home");
  while(!b.ShowingSummary)yield return null;
  Check(b.Story.HasPage(3)&&b.ledger.Account.Expense(1,"home-rent")>0&&b.StoryMonologue.Contains("월세"),"lawful first day opens page four and records rent");
  Check(!b.WallMemoPrompt&&b.ChooseCareerGoal("none")&&!b.WallMemoPrompt,"first night can replace the morning memo");
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
  b.CabinInput(CabinAction.Meter);Pose(drop.Kerb,drop.Direction);Check(jobs.Interact()&&!jobs.Active,"talkative ride completes");
  yield return Frames(2);s.GetComponent<RideCompletionPanel>().Close();b.CabinInput(CabinAction.Meter);
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
 IEnumerator CheckTaxiAudio(){
  var settings=DrivingSettings.Current;
  bool oldIndicator=settings.indicatorSound,oldEngine=settings.engineSound;
  var body=s.taxi.body;var position=body.position;var velocity=body.velocity;var constraints=body.constraints;
  var sources=s.GetComponents<AudioSource>();
  var engine=sources.Single(a=>a.clip&&a.clip.name=="Engine");
  var road=sources.Single(a=>a.clip&&a.clip.name=="Road");
  var relay=sources.Single(a=>a.clip&&a.clip.name.StartsWith("Indicator"));
  Check(!sources.Any(a=>a.clip&&a.clip.name=="Brake"),"no separate brake source");
  try{
   settings.engineSound=settings.indicatorSound=true;
   body.velocity=s.taxi.transform.forward*12.5f;
   yield return new WaitForSecondsRealtime(.6f);
   float fastPitch=engine.pitch,fastLevel=engine.volume,rollingLevel=road.volume;
   body.velocity=Vector3.zero;
   body.constraints=RigidbodyConstraints.FreezeAll;
   float settleDeadline=Time.realtimeSinceStartup+3;
   do{yield return new WaitForEndOfFrame();}while(road.isPlaying&&Time.realtimeSinceStartup<settleDeadline);
   Debug.Log("TAXI_ENGINE_MIX fastPitch="+fastPitch+" fastLevel="+fastLevel+" restPitch="+engine.pitch+" restLevel="+engine.volume);
   // Geared revs: 45 km/h holds ~1,900 rpm against an 800 rpm idle (pitch +.3), less just after an upshift.
   Check(fastPitch>engine.pitch+.15f&&fastLevel>engine.volume+.06f,"45 km/h to standstill lowers engine pitch and level");
   Debug.Log("TAXI_ROAD_MIX cruise="+rollingLevel+" rest="+road.volume+" pitch="+road.pitch+" speed="+s.taxi.Speed);
   Check(rollingLevel>.12f&&road.volume==0&&!road.isPlaying&&Mathf.Approximately(road.pitch,1),"rolling sound fades at rest without changing pitch");
   settings.engineSound=false;
   yield return new WaitForEndOfFrame();
   Check(!engine.isPlaying&&!road.isPlaying,"drive switch mutes both recorded layers");
   settings.engineSound=true;
   foreach(int mode in new[]{-1,1,0}){
    s.signals.Clear();
    if(mode==0)s.signals.ToggleHazards();else s.signals.Toggle(mode);
    bool on=false,off=false;
    float deadline=Time.realtimeSinceStartup+3;
    while(!off&&Time.realtimeSinceStartup<deadline){
     yield return new WaitForEndOfFrame();
     if(s.signals.Lit&&relay.clip.name=="IndicatorOn")on=true;
     if(on&&!s.signals.Lit&&relay.clip.name=="IndicatorOff")off=true;
    }
    Check(on&&off,"relay follows illuminated and dark lamp phases mode="+mode);
   }
   settings.indicatorSound=false;
   yield return new WaitForSecondsRealtime(.9f);
   Check(!relay.isPlaying,"muted indicator stops");
  }finally{
   settings.engineSound=oldEngine;settings.indicatorSound=oldIndicator;
   s.signals.Clear();body.constraints=constraints;body.position=position;body.velocity=velocity;Physics.SyncTransforms();
  }
 }
 IEnumerator RunWorkshopParts(BusinessProgress business){
  var services=s.services;
  Check(business.SavePath==CampaignPath&&business.SupportsCareer,"isolated single workshop fixture");
  business.ContinueFromMenu();yield return Frames(3);
  s.taxi.body.useGravity=false;s.taxi.body.detectCollisions=false;s.taxi.enabled=false;
  Check(!services.OpenWorkshopShop(),"remote workshop browsing rejected");
  Check(s.GetComponent<FullMapOverlay>().Places().Any(p=>p.capabilityLabel!=null&&p.capabilityLabel.Contains("부품")),"parts counter visible on map");
  var shop=services.Workshops[0];Pose(shop.repair,shop.direction);
  Check(s.jobs.Interact()&&services.OpenWorkshopShop(),"F opens physical workshop and catalog");
  foreach(var part in WorkshopPart.All)Check(Resources.Load<Texture2D>("WorkshopParts/"+part.id),"packaged icon "+part.id);
  int startCash=business.ledger.Account.Cash;
  Check(business.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Buy)
   &&!business.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Buy)
   &&business.ledger.Account.Cash==startCash-6000,"purchase and duplicate debit protection");
  Check(!business.ChangeWorkshopPart("comfort-damper",WorkshopPartAction.Buy),"insufficient balance blocks extra part");
  Check(business.ledger.RecordFare("workshop-probe-fare",30000),"funds available from fixture fare");
  float fuel=business.ledger.Account.Fuel;
  business.ledger.Account.Consume(0,1000);float normal=fuel-business.ledger.Account.Fuel;
  Check(business.ChangeWorkshopPart("eco-filter",WorkshopPartAction.Equip),"filter installed");
  fuel=business.ledger.Account.Fuel;business.ledger.Account.Consume(0,1000);
  Check(Mathf.Abs(fuel-business.ledger.Account.Fuel-.9f*normal)<.0001f,"active car consumes calibrated distance fuel");
  foreach(var part in new[]{WorkshopPart.Stripe,WorkshopPart.Spoiler,WorkshopPart.Damper})
   Check(business.ChangeWorkshopPart(part.id,WorkshopPartAction.Buy)&&business.ChangeWorkshopPart(part.id,WorkshopPartAction.Equip),"purchase and equip "+part.id);
  Check(!VehicleCatalog.HarshMotion(business.ActiveRideComfort,4.8f,8,false),"installed damper changes motion threshold");
  var visual=s.taxi.transform.Find("Taxi exterior");
  Check(visual.Find("Cream double stripe").gameObject.activeSelf&&visual.Find("Touring spoiler").gameObject.activeSelf,"both accessories on the player taxi");
  var saved=BusinessProgress.ParseCheckpoint(File.ReadAllText(business.SavePath));
  var restored=EconomyLedger.RestoreCampaign(saved.days,saved.account);
  Check(WorkshopPart.All.All(p=>restored.Account.OwnsPart(p)&&p.Fitted(restored.Account.ActiveVehicle)),"saved inventory and equipped parts restore");
  if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null){
   yield return Capture("workshop-parts-equipped");
   var taxi=s.taxi.transform;
   CaptureGarageView("workshop-parts-exterior",taxi.TransformPoint(new Vector3(4.3f,2.5f,-4.1f)),taxi.TransformPoint(new Vector3(0,.65f,0)));
  }
  Check(business.ChangeWorkshopPart("touring-spoiler",WorkshopPartAction.Remove)&&!visual.Find("Touring spoiler").gameObject.activeSelf
   &&business.ledger.Account.OwnsPart(WorkshopPart.Spoiler),"storing hides the accessory without removing ownership");
  services.Close();
  Check(!business.ChangeWorkshopPart("touring-spoiler",WorkshopPartAction.Equip),"closed shop blocks fitting");
  string report=Path.GetFullPath(PartsReport);Directory.CreateDirectory(Path.GetDirectoryName(report));
  File.WriteAllText(report,"{\"passed\":true,\"checks\":"+checks+"}");
  done=true;Debug.Log("WORKSHOP_PARTS_PLAYER_OK checks="+checks);Application.Quit(0);
 }
 IEnumerator RunFacilityArt(BusinessProgress business){
  var services=s.services;
  Check(business.SavePath==CampaignPath&&services.HasHomeBase,"isolated single career for facility art");
  foreach(var key in new[]{"diner","fuel","wash","workshop","rest","parking","garage","room","pork","stew","soup"})
   Check(FacilityArt.Texture(key)!=null,"packaged texture "+key);
  business.ContinueFromMenu();yield return Frames(3);
  s.taxi.body.useGravity=false;s.taxi.body.detectCollisions=false;s.taxi.enabled=false;
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-refreshmentCheck")>=0){
   CheckRefreshmentShops(business);
   yield return CaptureStorefronts();
   done=true;Debug.Log("REFRESHMENT_PLAYER_OK checks="+checks);Application.Quit(0);yield break;
  }
  var profilePath=ContentTuningRuntime.Argument("-facilityProfile");
  if(!string.IsNullOrEmpty(profilePath)){
   ContentTuningRuntime.LoadActivePath(profilePath);
   Check(ContentTuningRuntime.HasOverride,"editor profile loaded");
   Check(KoreanText.Stop("국도42 승하차 1 반대편")=="시험도로 반대편","edited stop name and direction");
   Check(!ContentTuning.Flag("display.sound.horn",true)&&ContentTuning.Flag("display.sound.rain",false),"independent profile sound flags");
   Check(FacilityArt.Texture("diner").name=="rest"&&FacilityArt.Texture("pork").name=="soup","editor background and menu images");
   var rider=PassengerBehaviorState.Create("editor-test",PassengerType.General);
   rider.RequestSetpoint(20);rider.Board();
   Check(rider.CabinRequestLine.Contains("시험 20도")&&rider.ObserveCabin(false,20)&&rider.TraitSatisfactionDelta==7,"edited passenger line and reward");
   Check(services.MealName=="시험 정식","edited menu caption");
  }
  var meal=services.MealParkingStop;
  Pose(meal.Kerb,meal.Direction);services.Toggle();
  Check(services.open,"restaurant popup opens");yield return Capture("diner-popup");
  Check(services.open&&services.RequestAction(1)&&services.InteriorFor(services.LocalActions[0],services.pending)=="diner","restaurant opens at parking bay");
  int cash=business.ledger.Account.Cash;
  for(int i=0;i<3;i++)Check(services.SelectMeal(i)&&services.SelectedMeal==i&&business.ledger.Account.Cash==cash,"meal card selection "+i);
  yield return Capture("meal-selected");
  services.ConfirmAction(false);
  Check(services.pending<0&&business.ledger.Account.Cash==cash,"cancel meal quote without charge");
  Check(services.RequestAction(1)&&services.ConfirmAction(true)&&business.ledger.Account.Cash==cash-3000,"meal charges exactly once");
  {var overlay=s.GetComponent<FullMapOverlay>();var cafe=overlay.Places().FirstOrDefault(p=>p.capabilityLabel=="아메리카노");
   Check(cafe!=null&&cafe.glyph==20,"americano cafe uses the cafe map icon");
   if(overlay.Open()){yield return Frames(3);yield return Capture("map-cafe-icon");overlay.Close();}}
  var pump=services.Stations[0];Pose(pump.pump,pump.direction);services.Toggle();
  Check(services.open&&services.InteriorFor(services.LocalActions[0],-1)=="fuel","fuel bay interior");
  Check(services.RequestAction(4)&&services.InteriorFor(services.LocalActions[0],services.pending)=="wash","wash quote changes interior");
  yield return Capture("wash");services.ConfirmAction(false);services.Close();
  var shop=services.Workshops[0];Pose(shop.repair,shop.direction);services.Toggle();
  Check(services.open&&services.InteriorFor(services.LocalActions[0],-1)=="workshop","repair bay interior");
  services.Close();
  var rest=services.RestArea;Pose(rest.pump,rest.direction);services.Toggle();
  Check(services.open&&services.InteriorFor(services.LocalActions[0],-1)=="rest","rest area interior");
  services.Close();
  var home=services.ParkingStop;Pose(home.Kerb,home.Direction);services.Toggle();
  Check(services.open&&services.InteriorFor(services.LocalActions[0],-1)=="garage","home garage interior");
  services.Close();
  Check(business.ledger.RecordFare("facility-art-fixture",20000),"fixture funds for room layers");
  Check(business.RequestEndDay(),"home reaches settlement");
  while(!business.ShowingSummary)yield return null;
  business.RoomOpen=true;
  Check(business.ChangeRoom("buy","lamp",0)&&business.ChangeRoom("buy","rug",0),"place purchased lamp and rug on approved room");
  yield return Capture("room-furnished");
  done=true;Debug.Log("FACILITY_ART_PLAYER_OK checks="+checks);Application.Quit(0);
 }
 void CheckRefreshmentShops(BusinessProgress business){
  var services=s.services;var shops=services.RefreshmentShops;
  Check(shops.Count==2&&Vector3.Distance(shops[0].Stop.Kerb,shops[1].Stop.Kerb)>100,"two distinct campaign shop locations");
  Check(business.ledger.RecordFare("shop-fixture",10000),"fund isolated shop test");
  business.ledger.Driver.satiety=35;business.ledger.Driver.fatigue=55;
  var map=s.GetComponent<FullMapOverlay>();
  foreach(var shop in shops){
   var item=shop.Item;var stop=shop.Stop;
   Check(map.Open(),"open map");
   var place=map.Places().Single(p=>p.name==item.Shop);
   Check(map.SelectPlace(place)&&map.PreviewPointCount>1,"preview shop "+item.Shop);
   Check(map.ShowFacilityPlanner(Refreshment.Snack),"open snack and coffee planner");
   var option=services.FacilityOptions(Refreshment.Snack).Single(o=>o.Kind==item.Kind);
   Check(option.Reachable&&map.GuidePlannerFacility(option)&&services.destination.Contains(item.Shop),"guide to shop "+item.Shop);
   Pose(stop.Kerb,-stop.Direction);Check(services.LocalRefreshment()==null,"shop rejects wrong heading");
   Pose(stop.Kerb,stop.Direction);
   s.jobs.cooldown[shop.StopIndex]=Time.time+60;
   Check(s.jobs.Interact()&&services.open&&services.LocalActions.SequenceEqual(new[]{item.Kind}),"F opens only the local shop product");
   var ledger=business.ledger;int cash=ledger.Account.Cash;double elapsed=ledger.Elapsed;
   Check(services.RequestAction(item.Kind)&&!services.ConfirmAction(false)&&ledger.Elapsed==elapsed&&ledger.Account.Cash==cash,"cancel preserves time and cash");
   var localQuote=OperationServiceQuote.Calculate(ledger,item.Kind,0,0);
   Check(services.RequestAction(item.Kind)&&services.ConfirmAction(true)&&!services.open,"confirm purchase "+item.Menu);
   Check(ledger.Account.Cash==cash-item.Price&&ledger.Elapsed==elapsed+item.Seconds&&
    Math.Abs(localQuote.EndSatiety-ledger.Driver.satiety)<.001&&Math.Abs(localQuote.EndFatigue-ledger.Driver.fatigue)<.001,"native payment and effect match shop quote");
   Check(!services.ConfirmAction(true)&&ledger.Account.Cash==cash-item.Price,"closed confirmation cannot charge twice");
   var save=BusinessProgress.ParseCheckpoint(File.ReadAllText(business.SavePath));
   var restored=EconomyLedger.RestoreCampaign(save.days,save.account);
   Check(restored.Account.Cash==ledger.Account.Cash&&restored.Elapsed==ledger.Elapsed,"saved shop payment and time");
  }
 }
 // Each shop is a building front with a door and a paved path from the pocket, not a street stall.
 IEnumerator CaptureStorefronts(){
  var map=s.world.map;var camera=s.drivingCamera;
  foreach(var shop in s.services.RefreshmentShops){
   var p=RefreshmentStorefronts.For(map,shop.Stop,shop.Item.Shop);
   Check(p!=null&&GameObject.Find(shop.Item.Shop+" 상가")&&!GameObject.Find("포장 매대"),"storefront replaces stall "+shop.Item.Shop);
   float path=Vector3.Distance(p.waiting,p.Door);
   Check(path>1&&path<20&&Vector3.Dot(p.outward,TrafficSystem.Right(shop.Stop.Direction))<-.7f,"door faces the pocket "+shop.Item.Shop+" path="+path.ToString("F1"));
   string slug=shop.Item.Kind==Refreshment.Snack?"snack":"coffee";
   camera.enabled=false;
   var view=p.P(-4.5f,2.4f,13);
   camera.transform.SetPositionAndRotation(view,Quaternion.LookRotation(p.P(0,2.2f,0)-view));
   yield return new WaitForSecondsRealtime(.5f);yield return Capture("storefront-"+slug+"-front");
   camera.enabled=true;
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
