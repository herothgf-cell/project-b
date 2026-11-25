using System;
using System.IO;
using UnityEngine;

[Serializable] public class CompletedDay {public string id,configuration;public PersonalRoomState room;public DayRecord result;public float fuel;public int cash,debt,score;public int days=1,totalIncome,totalExpenses,totalNet;}
[Serializable] public class CampaignCheckpoint { public const int CurrentVersion=9; public int version=CurrentVersion; public CampaignLogbook logbook; public int conditionFirstDay,conditionKind; public FatherStoryState story; public string contentProfileJson; public bool hasLive;public LiveCampaignCheckpoint live; public string comparisonVersion; public EconomySnapshot days; public AccountSnapshot account; public LawEvent[] law; public PrototypeIds ids; public PassengerResult[] passengers; public PracticeResult[] practices; public CompletedDay[] completedDays; }

[Serializable] public class LiveCampaignCheckpoint {public RadioScheduleSnapshot radio;public bool cabinCaptured;public CabinControlsState cabin;public AccidentState accident;public int operationVersion;public bool signalsPresent,lawObservationPresent;public TaxiSignalsSnapshot signals;public DrivingLawSnapshot lawObservation;public string mapRevision,tripId;public Vector3 position;public Quaternion rotation;public PassengerJobsSnapshot jobs;public AmbientPedestrianSnapshot ambientPedestrians;public StopOccupancySnapshot occupancy;public bool preparing,preferFastest,hasDestination;public Vector3 destination;public double realSeconds;}

public class BusinessProgress : MonoBehaviour {
 public DriveSession session;
 public double SimulationOperationSeconds {get;private set;}
 public bool SimulationSession {get;private set;}
 public EconomyLedger ledger;
 public readonly System.Collections.Generic.List<PassengerResult> passengers=new System.Collections.Generic.List<PassengerResult>();
 public readonly System.Collections.Generic.List<PracticeResult> practices=new System.Collections.Generic.List<PracticeResult>();
 public readonly System.Collections.Generic.List<CompletedDay> completedDays=new System.Collections.Generic.List<CompletedDay>();
 double realSeconds;bool initialFollowing,mixedFollowing;
 float fatigueBrakeUntil=-1;
 // Short physics-order grace only after this component actually removed speed.
 // Do not exempt ordinary player braking merely because the driver is tired.
 public bool SystemFatigueBraking {get{return Time.fixedTime<=fatigueBrakeUntil&&ledger!=null&&ledger.Driver!=null&&ledger.Driver.drowsiness.PassengerMustLeave;}}
 public bool DrowsinessRunning {get{return enabledCampaign&&ledger!=null&&ledger.Operating&&ledger.Driver!=null&&!preparing&&!restoring&&!resumeBlocked&&!PracticeSession.Enabled&&!session.PracticeBusy&&!session.PracticeMenuOpen&&!session.ChoiceOpen&&(!session.FullMapOpen||session.LivePvpMapOpen||PvpNativeLan.RunningOnline&&session.RideResultOpen)&&!session.paused&&(!session.services.open||SimulationSession)&&(!session.law.HistoryOpen||PvpNativeLan.RunningOnline)&&(!session.RideResultOpen||PvpNativeLan.RunningOnline)&&Time.timeScale>0;}}
 public bool DrowsinessEligible {get{return DrowsinessRunning&&session.taxi.body.velocity.magnitude>=.56f;}}
 public bool DrowsyControlsLost {get{return DrowsinessRunning&&ledger.Driver.drowsiness!=null&&(ledger.Driver.drowsiness.Blackout||ledger.Driver.drowsiness.PassengerMustLeave&&session.jobs.Active);}}
 public bool DrowsyMicrosleep {get{return DrowsinessRunning&&ledger.Driver.drowsiness!=null&&ledger.Driver.drowsiness.Microsleep;}}
 public string ComparisonVersion {get{if(loaded!=null&&ShowingSummary&&loaded.completedDays!=null)foreach(var day in loaded.completedDays)if(day.id==loaded.ids.campaign)return day.configuration;return (settings.days>1?PrototypeSettings.ThreeDayVersion:PrototypeSettings.OneDayVersion)+"-following-"+(mixedFollowing?"mixed":initialFollowing?"on":"off");}}
 public bool enabledCampaign=true;
 public string notice="",saveStatus="하루 정산 시 자동 저장됩니다";
 public string SavePath {get;private set;}
 bool testMode,storyTest;float noticeUntil; CampaignCheckpoint loaded; public bool useV2; public bool taxPrompt,preparing; public PrototypeSettings settings=new PrototypeSettings();
 public bool SaveFailed {get;private set;}public bool ResumeBlocked {get{return resumeBlocked;}}public bool IsRestoring {get{return restoring;}}
 public bool HasCampaignToContinue {get{return loaded!=null||!preparing||ledger!=null&&ledger.Day>1;}}
 bool NeedsFirstMemo => StoryEnabled&&preparing&&!Story.introSeen&&string.IsNullOrEmpty(WallMemoChoice);
 public string MenuPrimaryLabel {get{return NeedsFirstMemo?"목표 고르기":HasCampaignToContinue?"영업 계속하기":"영업 시작";}}
 public void ContinueFromMenu(){if(restoring||IntroInputBlocked)return;if(preparing||resumeBlocked){Depart();return;}session.paused=false;Time.timeScale=1;}
 public string LoadFailureDetail {get;private set;}="";
 public string RecoveryArchivePath {get;private set;}="";
 public int pendingPayment=-1; public bool restartPrompt;
 EconomySnapshot startDays;AccountSnapshot startAccount;
 void CaptureDayStart(){startDays=ledger.Snapshot();startAccount=ledger.Account==null?null:ledger.Account.Snapshot();}
 bool CanRestartNow {get{return !RoadMapRuntime.Trial&&enabledCampaign&&useV2&&!session.ChoiceOpen&&!session.FullMapOpen&&!session.PracticeMenuOpen&&!PracticeSession.Enabled&&!session.GetComponent<PracticeSession>().Busy&&pendingPayment<0&&!session.services.open&&!session.law.HistoryOpen&&!taxPrompt&&(preparing||ShowingSummary||session.paused);}}
 public bool RequestRestart(){if(session.accident&&session.accident.Locked)return false;if(restartPrompt||!CanRestartNow)return false;restartPrompt=true;return true;}
 public bool ConfirmRestart(bool accept){if(!restartPrompt)return false;if(!accept){restartPrompt=false;return false;}if(!CanRestartNow){restartPrompt=false;return false;}return ResetCampaign(true);}
 // Retained for older callers; excluded features never open a payment modal.
 public void RequestPayment(int kind){pendingPayment=-1;}
 public bool ConfirmPayment(bool accept){pendingPayment=-1;return false;}

 public bool ShowingSummary {get{return enabledCampaign&&ledger!=null&&ledger.ShowingSummary&&!EveningDrive;}}
 // 08 chapter 5 "퇴근 후 드라이브" (2026-11-24, Eungye single): a free drive chosen on the settlement screen.
 // The ledger stays settled (not Operating), so the shift clock, income, fuel, fatigue, wear, accidents and
 // law records cannot change; the settlement screen simply steps aside until F in the home bay.
 public bool EveningDrive {get;private set;}
 public float EveningDriveSeconds {get;private set;}
 public bool CanStartEveningDrive {get{return EveningDriveRules.Enabled&&enabledCampaign&&!SimulationSession&&!PracticeSession.Enabled&&!EveningDrive&&ledger!=null&&ledger.Account!=null&&ledger.ShowingSummary&&!restartPrompt&&!ResumeBlocked&&!SaveFailed&&pendingPayment<0&&session.services&&session.services.HasHomeBase&&!(session.damage&&session.damage.Disabled)&&!session.GetComponent<PracticeSession>().Busy;}}
 public bool StartEveningDrive(){
  if(!CanStartEveningDrive)return false;
  EveningDrive=true;EveningDriveSeconds=0;RoomOpen=LogbookOpen=GoalComparison=false;session.jobs.ClearForDay();System.Array.Clear(session.jobs.offers,0,session.jobs.offers.Length);session.jobs.nav.Clear();session.assisted.SetActive(false);
  ParkAtShiftStart();session.law.ResetObservation();session.paused=false;Time.timeScale=1;
  if(!GetComponent<EveningDriveBanner>())gameObject.AddComponent<EveningDriveBanner>().business=this;
  notice="퇴근 후 드라이브 · 미터기와 호출 없이 달립니다 · 기록과 수익에 영향 없음";noticeUntil=Time.unscaledTime+8;return true;
 }
 public bool EndEveningDrive(){
  if(!EveningDrive)return false;
  if(!session.services.AtEndDayHub())return EndDayNotice("숙소 주차 칸에 반듯하게 세운 뒤 F를 누르면 드라이브를 마칩니다.");
  EveningDrive=false;session.assisted.SetActive(false);session.jobs.nav.Clear();ParkAtShiftStart();session.law.ResetObservation();Freeze();notice="";noticeUntil=0;return true;
 }
 public static CampaignCheckpoint ParseCheckpoint(string json){
  var checkpoint=JsonUtility.FromJson<CampaignCheckpoint>(json);
  // JsonUtility creates a nonnull class with actors=null for both an absent
  // legacy field and explicit JSON null. Normalize only those wire formats;
  // an actual object (including malformed/empty actors) must still validate.
  const string field=@"(?<!\\)""ambientPedestrians""\s*:\s*";
  if(checkpoint!=null&&checkpoint.live!=null&&(!System.Text.RegularExpressions.Regex.IsMatch(json,field)||System.Text.RegularExpressions.Regex.IsMatch(json,field+@"null(?=\s*[,}])")))checkpoint.live.ambientPedestrians=null;
  if(checkpoint!=null&&checkpoint.live!=null&&(!System.Text.RegularExpressions.Regex.IsMatch(json,@"(?<!\\)""radio""\s*:")||System.Text.RegularExpressions.Regex.IsMatch(json,@"(?<!\\)""radio""\s*:\s*null(?=\s*[,}])")))checkpoint.live.radio=null;
  if(checkpoint!=null&&checkpoint.account!=null&&(!System.Text.RegularExpressions.Regex.IsMatch(json,@"(?<!\\)""Room""\s*:")||System.Text.RegularExpressions.Regex.IsMatch(json,@"(?<!\\)""Room""\s*:\s*null(?=\s*[,}])")))checkpoint.account.Room=null;
  // Unity can serialize an uninitialized inline radio field as this empty schedule.
  var radio=checkpoint?.live?.radio;
  if(radio!=null&&radio.version==1&&radio.day==0&&radio.completedMask==0&&radio.activeCue==-1&&radio.remaining==0)
   checkpoint.live.radio=null;
  return checkpoint;
 }
 public FatherStoryState Story {get;private set;}=new FatherStoryState();
 public CampaignLogbook Logbook {get;private set;}=new CampaignLogbook();
 public bool PassengerStoriesEnabled {get{return SupportsCareer&&(storyEnabled&&!storyTest||PassengerStoryProbe.Enabled);}}
 void RestoreLogbook(){
  if(!SupportsCareer||loaded==null)return;
  if(loaded.logbook!=null&&loaded.logbook.version==CampaignLogbook.CurrentVersion){Logbook=loaded.logbook;return;}
  var days=loaded.days.records;
  foreach(var ride in passengers){
   if(ride==null)continue;
   var record=ride.day>0&&ride.day<=days.Length?days[ride.day-1]:null;
   Logbook.ObserveRide(ride,record!=null&&record.conditionRecorded&&record.conditionKind==(int)DailyConditionKind.Rain,
    record!=null&&record.conditionRecorded&&record.conditionKind==(int)DailyConditionKind.Friday);
  }
  foreach(var record in loaded.days.records)
   Logbook.ObserveSettlement(!Array.Exists(loaded.law,entry=>entry.day==record.day&&entry.HasViolation));
  Story.NeighborhoodProgress(Logbook.StarredNeighborhoods);
 }
 public IntroCutscene Intro {get;private set;}
 public bool IntroInputBlocked {get{return Intro&&Intro.BlocksInput;}}
 public bool StoryEnabled {get{return SupportsCareer&&storyEnabled;}}
 bool storyEnabled;
 bool logbookOpen,resumeAfterLogbook;
 public bool LogbookOpen {
  get{return logbookOpen;}
  set{
   logbookOpen=value;
   if(!value&&resumeAfterLogbook){resumeAfterLogbook=false;session.paused=false;Time.timeScale=1;}
  }
 }
 public bool OpenDrivingLogbook(){
  if(!CabinEnabled||!Story.FoundNotebook||!DrowsinessRunning||session.SettingsOpen||session.accident&&session.accident.Locked)return false;
  resumeAfterLogbook=true;LogbookOpen=true;session.paused=true;Time.timeScale=0;
  return true;
 }
 bool goalComparison;
 RenderTexture goalComparisonFrame;
 public RenderTexture GoalComparisonFrame {get{return goalComparisonFrame;}}
 public bool GoalComparison {
  get{return goalComparison;}
  set{
   if(value==goalComparison)return;
   goalComparison=value;
   if(value)goalComparisonFrame=session.drivingCamera.CaptureCockpitFrame();
   else ReleaseGoalComparisonFrame();
  }
 }
 void ReleaseGoalComparisonFrame(){
  if(!goalComparisonFrame)return;
  goalComparisonFrame.Release();Destroy(goalComparisonFrame);goalComparisonFrame=null;
 }
 void OnDestroy(){ReleaseGoalComparisonFrame();if(dailyEnabled)RainPolicy.RuntimeRain=null;}
 bool dailyEnabled;
 int dailyFirstDay=1;
 public bool DailyConditionsEnabled {get{return dailyEnabled&&SupportsCareer&&ledger!=null&&ledger.Day>=dailyFirstDay;}}
 public DailyConditionKind DayCondition {get;private set;}
 public float? PreviousWeekdayAverageRides {get{return ledger.PriorConditionAverageRides((int)DailyConditionKind.Weekday);}}
 public double DayPoolMultiplier(PassengerType pool){return DailyConditions.PoolMultiplier(DayCondition,pool);}
 public double DayPresetMultiplier(PassengerPresetDefinition preset){return DailyConditions.PresetMultiplier(DayCondition,preset);}
 void InitializeDailyCondition(){
  DayCondition=DailyConditionsEnabled?DailyConditions.ForDay(session.law.ids.campaign,ledger.Day-dailyFirstDay+1):DailyConditionKind.Weekday;
  if(!dailyEnabled)return;
  DailyRoadConstruction.Apply(session,DayCondition==DailyConditionKind.Construction);
  RefreshDailyWeather();
 }
 void RefreshDailyWeather(){
  if(dailyEnabled)RainPolicy.RuntimeRain=DailyConditionsEnabled?(bool?)(ledger.Operating&&DailyConditions.IsRaining(DayCondition,ledger.Elapsed,ledger.Settings.daySeconds)):null;
 }
 void ObserveDailyConditionDeparture(){
  if(!DailyConditionsEnabled)return;
  if(DayCondition==DailyConditionKind.Friday)Story.FridayOperated();
 }
 void ObserveDailyConditionSettlement(){
  if(!DailyConditionsEnabled)return;
  ledger.Last.conditionRecorded=true;ledger.Last.conditionKind=(int)DayCondition;
  if(DayCondition==DailyConditionKind.Rain)Story.Unlock(1);
  RefreshDailyWeather();
 }
 static void ValidateDailyCondition(CampaignCheckpoint candidate,int day){
  if(candidate.conditionFirstDay<0||(long)candidate.conditionFirstDay>(long)day+1||!DailyConditions.Valid(candidate.conditionKind))throw new ArgumentException("Invalid day condition");
  if(candidate.conditionFirstDay>0&&day>=candidate.conditionFirstDay&&
     candidate.conditionKind!=(int)DailyConditions.ForDay(candidate.ids.campaign,day-candidate.conditionFirstDay+1))throw new ArgumentException("Day condition does not match the saved seed");
  foreach(var record in candidate.days.records){
   if(record.conditionRecorded&&(!DailyConditions.Valid(record.conditionKind)||candidate.conditionFirstDay==0||record.day<candidate.conditionFirstDay||
      record.conditionKind!=(int)DailyConditions.ForDay(candidate.ids.campaign,record.day-candidate.conditionFirstDay+1)))throw new ArgumentException("Recorded day condition does not match the saved seed");
  }
 }
 public string StoryMonologue {get;private set;}="";
 public bool WallMemoAvailable {get{return StoryEnabled&&RoomAvailable&&ArchivedRoom==null;}}
 public string WallMemoChoice {get{return Story.wallChoice;}}
 public bool WallMemoPrompt {get{return WallMemoAvailable&&Story.wallPromptDay<ledger.Day&&((ledger.Day==1&&Story.wallChoice=="")||(ledger.Day==3&&(Story.wallChoice==""||Story.wallChoice=="none")));}}
 public void DismissWallMemoPrompt(){
  if(!WallMemoPrompt)return;
  var previous=Story.wallPromptDay;Story.wallPromptDay=ledger.Day;
  if(!SaveCheckpoint())Story.wallPromptDay=previous;
 }
 void RestoreStory(){
  if(!SupportsCareer||loaded==null)return;
  if(loaded.story!=null&&loaded.story.version==FatherStoryState.CurrentVersion){Story=loaded.story;return;}
  Story.introSeen=Story.firstVacancy=true;
  if(passengers.Exists(ride=>ride.outcome=="운송 완료"))Story.Unlock(0);
  if(ledger.Last!=null){
   foreach(var record in ledger.Snapshot().records){
    int day=record.day;
    if(ledger.Day==day&&!ShowingSummary)break;
    if(!Array.Exists(loaded.law,entry=>entry.day==day&&entry.HasViolation)){Story.Unlock(3);break;}
   }
  }
  if(ledger.Account.CareerGoal!="none")Story.wallChoice=ledger.Account.CareerGoal;
 }
 public bool PlayIntroAgain(){
  if(!SupportsCareer||IntroInputBlocked||restoring||session.SettingsOpen)return false;
  if(!Intro)Intro=session.gameObject.AddComponent<IntroCutscene>();
  Intro.Play(true,null);return true;
 }
 void BeginFirstIntro(){
  if(!Intro)Intro=session.gameObject.AddComponent<IntroCutscene>();
  // Park the father's taxi in the home bay first so the live garage and cockpit cuts show it.
  ParkAtShiftStart();
  Intro.Play(false,FinishFirstIntro);
 }
 // First-day cockpit photo for the goal comparison, stored beside the career save.
 Texture2D firstDriveFrame;
 string FirstDrivePath {get{return string.IsNullOrEmpty(SavePath)?null:Path.Combine(Path.GetDirectoryName(SavePath),Path.GetFileNameWithoutExtension(SavePath)+"-first-drive.png");}}
 public Texture FirstDriveFrame {get{
  if(firstDriveFrame)return firstDriveFrame;
  var path=FirstDrivePath;
  try{if(path!=null&&File.Exists(path)){firstDriveFrame=new Texture2D(2,2);if(!firstDriveFrame.LoadImage(File.ReadAllBytes(path))){Destroy(firstDriveFrame);firstDriveFrame=null;}}}catch(Exception e){Debug.LogWarning("첫날 운전석 사진을 읽지 못했습니다. "+e.Message);}
  return firstDriveFrame;
 }}
 void SaveFirstDriveFrame(){
  var path=FirstDrivePath;if(path==null)return;
  var frame=session.drivingCamera.CaptureCockpitFrame();if(!frame)return;
  var previous=RenderTexture.active;
  try{
   RenderTexture.active=frame;
   var image=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
   image.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);image.Apply();
   File.WriteAllBytes(path,image.EncodeToPNG());
   if(firstDriveFrame)Destroy(firstDriveFrame);firstDriveFrame=image;
  }catch(Exception e){Debug.LogWarning("첫날 운전석 사진을 저장하지 못했습니다. "+e.Message);}
  finally{RenderTexture.active=previous;frame.Release();Destroy(frame);}
 }
 void FinishFirstIntro(){
  Story.introSeen=Story.firstVacancy=true;
  session.drivingCamera.SetFirstPerson(true);
  ParkAtShiftStart();
  session.law.ResetObservation();session.jobs.ResetOffers();session.paused=false;preparing=false;Time.timeScale=1;
  SaveFirstDriveFrame();
  ObserveDailyConditionDeparture();
  EndDayNotice("M 지도에서 첫 호출을 선택하세요. 출구 화살표를 따라 출차하세요.");
  SaveCheckpoint();
 }
 public void ObserveStoryBoarding(PassengerBehaviorState behavior,bool allowQuestion=true){
  if(!StoryEnabled)return;
  if(!Story.FoundNotebook&&Story.introSeen)
   EndDayNotice(CabinAutomatic?"첫 손님 탑승 · 미터기 자동 보조가 켜졌습니다.":"첫 손님 탑승 · J로 미터기를 켜세요.");
  if(allowQuestion&&behavior!=null&&(behavior.Type==PassengerType.Talkative||behavior.HasTrait("speech.talkative")))Story.Ask(ledger.Day,behavior.Variant,session.law.tripId);
 }
 public void ObserveStoryDelivery(){
  if(!StoryEnabled||!Story.Unlock(0))return;
  if(passengers.Count>0)passengers[passengers.Count-1].foundFatherNotebook=true;
  EndDayNotice("글러브박스에서 수첩을 발견했다");
 }
 public bool StoryQuestionVisible {get{return StoryEnabled&&session.requests&&session.requests.SchedulePresentationAvailable&&!session.jobs.BehaviorChoicesOpen
    &&session.jobs.trip.State==TripState.OnTrip&&session.jobs.Behavior!=null&&!session.jobs.Behavior.Quiet&&Story.Pending(session.law.tripId);}}
 public bool CanAnswerStory {get{return StoryQuestionVisible&&session.requests.SafeToRespond()&&!session.requests.ResponseBlockedThisFrame;}}
 public bool AnswerStory(int choice){
  if(!CanAnswerStory||!Story.Answer(session.law.tripId,choice))return false;
  if(!SaveCheckpoint()){Story.UndoAnswer();return false;}
  session.requests.BlockResponseThisFrame();return true;
 }
 public int StorySatisfaction {get{return StoryEnabled?Story.Satisfaction(session.law.tripId):0;}}
 void ObserveStorySettlement(){
  if(!StoryEnabled)return;
  int day=ledger.Day;
  if(!Array.Exists(session.law.ledger.Events,entry=>entry.day==day&&entry.HasViolation))Story.Unlock(3);
  if(day==1)StoryMonologue="월세 빼고 이만큼. 내일도 나가야겠다.";
  ObserveStoryGoal();
 }
 void ObserveStoryGoal(){
  if(!StoryEnabled)return;
  var account=ledger.Account;
  bool car=account.CareerGoal=="car";
  int price=car?VehicleCatalog.Price(account.CareerVehicleGoal):account.CareerGoal=="home"?ContentTuning.Int("home.upgrade.price",0):0;
  if(price<=0)return;
  bool owned=car&&account.Vehicle.modelId==account.CareerVehicleGoal;
  if(!Story.goalHalf&&(owned||account.Cash>=price*.5)){
   Story.goalHalf=true;Story.Unlock(4);
   var last=Story.answers.Count>0?Story.answers[Story.answers.Count-1]:null;
   StoryMonologue=last!=null&&last.question==0&&last.choice==1?"아버지가 달리던 길이 조금 익숙해졌다.":"이 길이 조금 익숙해졌다.";
  }
  if(!Story.goalDone&&(owned||!car&&account.Cash>=price)){
   Story.goalDone=true;StoryMonologue="오늘부터, 다시 내 핸들로.";GoalComparison=true;
  }
 }
 public CabinControlsState Cabin {get;private set;}=new CabinControlsState();
 public bool CabinEnabled {get{return SupportsCareer&&ledger!=null&&ledger.OneDay;}}
 public bool CabinAutomatic {get{return !CabinEnabled||ledger.Day<=3&&DrivingSettings.Current.cabinAssist;}}
 public string CabinCallBlock {get{return CabinEnabled?Cabin.NewCallBlock(CabinAutomatic):"";}}
 public bool CabinInput(CabinAction action){
  if(!CabinEnabled||!DrowsinessRunning||session.SettingsOpen||session.accident&&session.accident.Locked)return false;
  switch(action){
   case CabinAction.Meter:
    if(CabinAutomatic||!Cabin.ToggleMeter(session.jobs.trip.State==TripState.OnTrip))return false;
    break;
   case CabinAction.Window: Cabin.WindowOpen=!Cabin.WindowOpen;break;
   case CabinAction.Cooler: if(!Cabin.AdjustTemperature(-1))return false;break;
   case CabinAction.Warmer: if(!Cabin.AdjustTemperature(1))return false;break;
   default:return false;
  }
  if(session.jobs.trip.State==TripState.OnTrip&&session.jobs.Behavior!=null)
   session.jobs.Behavior.ObserveCabin(Cabin.WindowOpen,Cabin.Target);
  SaveCheckpoint();
  return true;
 }
 public bool CanAccept {get{return !(session.accident&&session.accident.Locked)&&!(session.simulationMatch&&session.simulationMatch.ServiceLocked(session))&&!resumeBlocked&&!restoring&&!PracticeSession.Enabled&&!preparing&&(!enabledCampaign||ledger.CanAccept);}}
 public void Initialize(DriveSession owner){
  if(PvpSimulationMode.Active){SimulationSession=true;session=owner;owner.taxi.business=this;if(PvpNativeLan.IsReplica(owner)){owner.taxi.NetworkReplica=true;}if(!owner.IsSimulationBot)gameObject.AddComponent<DrowsinessHUD>().business=this;testMode=true;enabledCampaign=true;useV2=true;settings=PrototypeSettings.ForOneDay();settings.daySeconds=7200;ledger=new EconomyLedger(settings);ledger.EnablePvpProfile();SavePath="";saveStatus="오프라인 경기 · 캠페인 저장 안 함";CaptureDayStart();return;}
  session=owner;owner.taxi.business=this;gameObject.AddComponent<DrowsinessHUD>().business=this;initialFollowing=owner.traffic.mitOptions.Enabled;var args=Environment.GetCommandLineArgs();
  foreach(var arg in args)if(arg.EndsWith("Test")&&arg!="-contentTest"){testMode=true;if(arg!="-economyTest"&&arg!="-hybridTest"&&arg!="-hybridQuickTest")enabledCampaign=false;}
  if(Array.IndexOf(args,"-assistedTrial")>=0||RoadRulesTrial.Enabled||HdRouteTrial.Enabled){testMode=true;enabledCampaign=false;}
  bool ambientTest=Array.IndexOf(args,"-ambientPedestrianTest")>=0||Array.IndexOf(args,"-ambientPedestrianReloadTest")>=0;if(ambientTest){testMode=true;enabledCampaign=true;}
  bool activitiesReloadTest=Array.IndexOf(args,"-passengerActivitiesReloadTest")>=0;
  bool activitiesTest=Array.IndexOf(args,"-passengerActivitiesTest")>=0||activitiesReloadTest;
  bool startupReloadTest=Array.IndexOf(args,"-campaignStartupPreparedReloadTest")>=0||Array.IndexOf(args,"-accidentWorkshopReloadCheck")>=0;
  bool startupTest=Array.IndexOf(args,"-campaignStartupTest")>=0||startupReloadTest;
  bool protoTest=Array.IndexOf(args,"-protoExpansionTest")>=0||Array.IndexOf(args,"-protoExpansionReloadTest")>=0;
  if(PracticeSession.Enabled||RoadMapRuntime.Trial){testMode=true;enabledCampaign=false;}
  if(!testMode){settings=PrototypeSettings.ForThreeDays();}
  if(Array.IndexOf(args,"-oneDayTest")>=0||Array.IndexOf(args,"-oneDayReloadTest")>=0||Array.IndexOf(args,"-oneDayMode")>=0){settings=PrototypeSettings.ForOneDay();if(testMode)enabledCampaign=true;}
  if(Array.IndexOf(args,"-threeDayTest")>=0||Array.IndexOf(args,"-threeDayReloadTest")>=0){settings=PrototypeSettings.ForThreeDays();enabledCampaign=true;}
  useV2=Array.IndexOf(args,"-serviceLawZonesTest")>=0||PracticeSession.Enabled||!testMode||(Array.IndexOf(args,"-campaignTest")>=0||Array.IndexOf(args,"-completionTest")>=0||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-campaignDriveTest")>=0)||Array.IndexOf(args,"-scenarioTest")>=0||Array.IndexOf(args,"-contentFlowTest")>=0||Array.IndexOf(args,"-exitTransportTest")>=0||Array.IndexOf(args,"-hybridV2")>=0;
  if((Array.IndexOf(args,"-serviceLawZonesTest")>=0||Array.IndexOf(args,"-campaignTest")>=0||Array.IndexOf(args,"-completionTest")>=0||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-campaignDriveTest")>=0))enabledCampaign=true;
  if(RoadMapRuntime.Trial){settings=PrototypeSettings.ForOneDay();useV2=true;}useV2|=settings.oneDay;if(Array.IndexOf(args,"-liveSaveTest")>=0||(Array.IndexOf(args,"-liveReloadTest")>=0||Array.IndexOf(args,"-liveInvalidTest")>=0)){settings=PrototypeSettings.ForThreeDays();useV2=true;enabledCampaign=true;}if(Array.IndexOf(args,"-passengerCareTest")>=0){useV2=true;enabledCampaign=true;}
  SavePath=testMode?Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts",useV2?"campaign-v2-test.json":"economy-player-checkpoint.json")):Path.Combine(CampaignSaveLocation.DirectoryFor(Path.GetDirectoryName(Application.persistentDataPath)),PrototypeSettings.SaveFile);
  if(settings.oneDay)SavePath=Path.Combine(Path.GetDirectoryName(SavePath),settings.days>1?(testMode?"three-day-test.json":PrototypeSettings.ThreeDaySaveFile):(testMode?"one-day-test.json":"campaign-day20-v1.json"));
  if(Array.IndexOf(args,"-liveSaveTest")>=0||(Array.IndexOf(args,"-liveReloadTest")>=0||Array.IndexOf(args,"-liveInvalidTest")>=0))SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/live-campaign-test.json"));
  bool behaviorTest=Array.IndexOf(args,"-passengerBehaviorTest")>=0||(Array.IndexOf(args,"-passengerBehaviorReloadTest")>=0||(Array.IndexOf(args,"-vehicleDurabilityReloadTest")>=0||Array.IndexOf(args,"-vehicleDurabilitySummaryReloadTest")>=0||(Array.IndexOf(args,"-vehicleDurabilityPreparationReloadTest")>=0||Array.IndexOf(args,"-vehicleDurabilityPendingReloadTest")>=0)));
  if(behaviorTest){settings=PrototypeSettings.ForThreeDays();useV2=true;enabledCampaign=true;SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/passenger-behavior-test.json"));}
  bool durabilityTest=Array.IndexOf(args,"-vehicleDurabilityTest")>=0||(Array.IndexOf(args,"-vehicleDurabilityReloadTest")>=0||Array.IndexOf(args,"-vehicleDurabilitySummaryReloadTest")>=0||(Array.IndexOf(args,"-vehicleDurabilityPreparationReloadTest")>=0||Array.IndexOf(args,"-vehicleDurabilityPendingReloadTest")>=0));
  if(durabilityTest){settings=PrototypeSettings.ForThreeDays();useV2=true;enabledCampaign=true;SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/vehicle-durability-test.json"));}
  if(Array.IndexOf(args,"-passengerCareTest")>=0)SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/passenger-care-test.json"));
  if(Array.IndexOf(args,"-liveInvalidTest")>=0)SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/live-invalid-test.json"));
  if(ambientTest){settings=PrototypeSettings.ForOneDay();useV2=true;enabledCampaign=true;SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/ambient-pedestrian-test.json"));}
  if(activitiesTest){settings=PrototypeSettings.ForOneDay();useV2=true;enabledCampaign=true;SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/passenger-activities-test.json"));}
  if(protoTest){settings=PrototypeSettings.ForThreeDays();useV2=true;enabledCampaign=true;SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/proto-expansion-test.json"));}
  if(startupTest){settings=PrototypeSettings.ForThreeDays();useV2=true;enabledCampaign=true;SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/campaign-startup-test.json"));}
  if(startupReloadTest)SavePath=Path.Combine(Path.GetDirectoryName(SavePath),"campaign-startup-prepared.json");
  if(UnifiedModeProbe.Enabled)SavePath=UnifiedModeProbe.CampaignPath;
  bool accidentCheck=Array.IndexOf(args,"-accidentWorkshopCheck")>=0||Array.IndexOf(args,"-accidentWorkshopReloadCheck")>=0;
  if(accidentCheck){testMode=true;enabledCampaign=true;useV2=true;settings=PrototypeSettings.ForThreeDays();SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/accident-workshop-test.json"));}
  if(HudRevisionProbe.Enabled)SavePath=HudRevisionProbe.CampaignPath;
  if(ShiftDevEndProbe.Enabled)SavePath=ShiftDevEndProbe.CampaignPath;
  storyTest=Array.IndexOf(args,"-fatherStoryCheck")>=0||Array.IndexOf(args,"-fatherStoryReloadCheck")>=0;
  if(storyTest){testMode=true;enabledCampaign=true;useV2=true;settings=PrototypeSettings.ForThreeDays();SavePath=ShiftDevEndProbe.FatherStorySavePath;}
  if(ShiftDevEndProbe.DailyCheck||ShiftDevEndProbe.DailyReload){testMode=true;enabledCampaign=true;useV2=true;settings=PrototypeSettings.ForThreeDays();SavePath=ShiftDevEndProbe.DailySavePath;}
  if(PassengerStoryProbe.Enabled){testMode=true;enabledCampaign=true;useV2=true;settings=PrototypeSettings.ForThreeDays();}
  if(ShiftDevEndProbe.CareerCheck||ShiftDevEndProbe.CareerReload)SavePath=CampaignSaveLocation.SelectedCareer(Path.GetFullPath("artifacts/career-player/"+PrototypeSettings.ThreeDaySaveFile));
  bool roomCheck=Array.IndexOf(args,"-personalRoomCheck")>=0||Array.IndexOf(args,"-personalRoomReloadCheck")>=0;bool garageCheck=VehicleGarageProbe.Enabled;roomCheck|=garageCheck;
  if(roomCheck){testMode=true;enabledCampaign=true;useV2=true;settings=PrototypeSettings.ForThreeDays();}
  if(ContentTuningRuntime.TestMode)SavePath=ContentTuningRuntime.CampaignPath(Path.GetFileName(SavePath));
  bool firstFareCheck=Array.IndexOf(args,"-firstFareCheck")>=0&&Array.IndexOf(args,"-oneDayTrafficTest")>=0;
  if(firstFareCheck){
   testMode=true;
   enabledCampaign=true;
   useV2=true;
   settings=PrototypeSettings.ForThreeDays();
   SavePath=Path.GetFullPath("artifacts/first-fare-career.json");
  }
  if(PassengerStoryProbe.Enabled)SavePath=PassengerStoryProbe.SavePath;
  if(roomCheck)SavePath=Path.GetFullPath(garageCheck?"artifacts/vehicle-garage-player/campaign.json":"artifacts/personal-room-player/campaign.json");
  if(!testMode&&!UnifiedModeProbe.Enabled&&!HudRevisionProbe.Enabled&&!ShiftDevEndProbe.Enabled&&!ContentTuningRuntime.TestMode)try{SavePath=CampaignSaveLocation.Resolve(Path.GetDirectoryName(Application.persistentDataPath),Path.GetFileName(SavePath));if(settings.oneDay&&settings.days==3&&owner.world.map.mapId=="eungye")SavePath=CampaignSaveLocation.SelectedCareer(SavePath);}catch(Exception e){Debug.LogWarning("이전 캠페인 저장 복사 실패: "+e.Message);}
  var titleSlot=UnifiedModeBootstrap.ConsumeTitleSlot();
  if(titleSlot!=null){SavePath=titleSlot;selectAfterWrite=true;}
  settings.homeBase=settings.oneDay&&owner.world.map.mapId=="eungye"&&!PracticeSession.Enabled&&!RoadMapRuntime.Trial;
  storyEnabled=firstFareCheck||storyTest||!testMode&&!UnifiedModeProbe.Enabled&&!HudRevisionProbe.Enabled&&!ShiftDevEndProbe.Enabled&&!ContentTuningRuntime.TestMode;
  dailyEnabled=settings.homeBase&&(storyEnabled&&!storyTest||ShiftDevEndProbe.DailyCheck||ShiftDevEndProbe.DailyReload);
  ledger=useV2?new EconomyLedger(settings):new EconomyLedger(new EconomySettings());
  if((Array.IndexOf(args,"-passengerBehaviorReloadTest")>=0||(Array.IndexOf(args,"-vehicleDurabilityReloadTest")>=0||Array.IndexOf(args,"-vehicleDurabilitySummaryReloadTest")>=0||(Array.IndexOf(args,"-vehicleDurabilityPreparationReloadTest")>=0||Array.IndexOf(args,"-vehicleDurabilityPendingReloadTest")>=0)))||protoTest&&Array.IndexOf(args,"-protoExpansionReloadTest")>=0||!testMode||Array.IndexOf(args,"-personalRoomReloadCheck")>=0||Array.IndexOf(args,"-vehicleGarageReloadCheck")>=0||(Array.IndexOf(args,"-liveReloadTest")>=0||Array.IndexOf(args,"-liveInvalidTest")>=0)||startupReloadTest||activitiesReloadTest||ShiftDevEndProbe.FatherStoryReload||ShiftDevEndProbe.DailyReload||PassengerStoryProbe.Reload||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-oneDayReloadTest")>=0||Array.IndexOf(args,"-threeDayReloadTest")>=0||Array.IndexOf(args,"-ambientPedestrianReloadTest")>=0){
   foreach(var path in new[]{SavePath,SavePath+".bak"}){
    if(!File.Exists(path))continue;
    try{var candidate=ParseCheckpoint(File.ReadAllText(path));if(candidate==null||(candidate.version<2||candidate.version>CampaignCheckpoint.CurrentVersion)||candidate.law==null||candidate.ids==null||string.IsNullOrEmpty(candidate.ids.campaign)||candidate.ids.sequence<0)throw new Exception("Invalid v2 checkpoint");if(settings.oneDay&&(candidate.account==null||candidate.account.settings==null||!candidate.account.settings.oneDay||candidate.account.settings.days!=settings.days))throw new Exception("Different campaign mode");if(!candidate.hasLive)candidate.live=null;if(candidate.version>=3&&candidate.days!=null&&candidate.days.hasActive&&candidate.days.active!=null&&candidate.days.active.elapsed>0&&candidate.live==null)throw new Exception("Missing active session");EconomyLedger restored=null;ContentTuningRuntime.RestoreAndValidate(candidate.contentProfileJson,()=>{ValidateLivePayload(candidate.live,owner);restored=EconomyLedger.RestoreCampaign(candidate.days,candidate.account);if(candidate.version>=9&&candidate.live!=null&&candidate.live.radio!=null)candidate.live.radio.Validate(restored.Day);if(candidate.version>=7)ValidateDailyCondition(candidate,restored.Day);if(candidate.version>=8&&candidate.logbook!=null&&candidate.logbook.version!=0)candidate.logbook.Validate(restored.Day);if(candidate.version>=6&&candidate.story!=null&&candidate.story.version!=0)candidate.story.Validate(restored.Day);if(candidate.account.Room==null&&restored.State==BusinessState.Completed){restored.Account.Room.finalized=true;candidate.account.Room=restored.Account.Room.Copy();}var checkedLaw=new LawLedger();foreach(var e in candidate.law)checkedLaw.Record(e);});ledger=restored;loaded=candidate;saveStatus=path==SavePath?"저장된 정산 결과를 불러왔습니다":"백업에서 정산 결과를 복구했습니다";break;}
    catch(Exception ex){LoadFailureDetail+=(LoadFailureDetail.Length>0?"\n":"")+Path.GetFileName(path)+": "+ex.Message;saveStatus="저장 읽기 실패 · 기존 파일 확인 중";Debug.LogWarning("Campaign load failed ["+path+"]: "+ex.Message);}
   }
  }
  if(loaded==null&&(File.Exists(SavePath)||File.Exists(SavePath+".bak"))&&((Array.IndexOf(args,"-passengerBehaviorReloadTest")>=0||(Array.IndexOf(args,"-vehicleDurabilityReloadTest")>=0||Array.IndexOf(args,"-vehicleDurabilitySummaryReloadTest")>=0||(Array.IndexOf(args,"-vehicleDurabilityPreparationReloadTest")>=0||Array.IndexOf(args,"-vehicleDurabilityPendingReloadTest")>=0)))||protoTest&&Array.IndexOf(args,"-protoExpansionReloadTest")>=0||startupReloadTest||activitiesReloadTest||ShiftDevEndProbe.FatherStoryReload||ShiftDevEndProbe.DailyReload||PassengerStoryProbe.Reload||!testMode||Array.IndexOf(args,"-personalRoomReloadCheck")>=0||Array.IndexOf(args,"-vehicleGarageReloadCheck")>=0||(Array.IndexOf(args,"-liveReloadTest")>=0||Array.IndexOf(args,"-liveInvalidTest")>=0))){resumeBlocked=true;SaveFailed=true;saveStatus="저장 복원 실패 · 기존 파일 보존 중 · 지도/저장 확인 또는 새 캠페인 선택";}
  // Legacy accounts acquire the home rule at the next settlement. Posted days keep their original expenses.
  if(settings.homeBase&&ledger.Account!=null)ledger.Account.UseHomeBase();
  if(loaded!=null){if(loaded.passengers!=null)passengers.AddRange(loaded.passengers);if(loaded.practices!=null)practices.AddRange(loaded.practices);if(loaded.completedDays!=null)completedDays.AddRange(loaded.completedDays);}
  RestoreStory();RestoreLogbook();
  if(loaded!=null)dailyFirstDay=loaded.conditionFirstDay>0?loaded.conditionFirstDay:loaded.hasLive||ShowingSummary?checked(ledger.Day+1):1;
  if(loaded!=null&&!string.IsNullOrEmpty(loaded.comparisonVersion)){mixedFollowing=loaded.comparisonVersion.EndsWith("mixed");initialFollowing=loaded.comparisonVersion.EndsWith("on");mixedFollowing|=initialFollowing!=owner.traffic.mitOptions.Enabled;}
  CaptureDayStart();
  if(resumeBlocked){preparing=true;Freeze();}
  else if(loaded!=null&&loaded.live!=null)Freeze();
  else if(ShowingSummary)Freeze();
  else if(useV2&&(!testMode||storyTest||ShiftDevEndProbe.DailyCheck||ShiftDevEndProbe.DailyReload||PassengerStoryProbe.Enabled||startupTest||(Array.IndexOf(args,"-vehicleDurabilityPreparationReloadTest")>=0||Array.IndexOf(args,"-vehicleDurabilityPendingReloadTest")>=0))){preparing=loaded==null||loaded.live==null||loaded.live.preparing;Freeze();}
 }
 void Start(){if(loaded!=null&&session.law){session.law.ids=loaded.ids;foreach(var e in loaded.law)session.law.ledger.Record(e);}InitializeDailyCondition();if(loaded!=null&&loaded.live!=null){restoring=true;StartCoroutine(RestoreAfterInitialization(loaded.live));}else if(loaded!=null&&ledger.Care.Damage.Disabled&&!string.IsNullOrEmpty(ledger.Care.Damage.RepairBayId))StartCoroutine(RestoreReservedDamageBay());else if(preparing)StartCoroutine(PlaceAtParking());}
 bool resumeBlocked,restoring,selectAfterWrite;
 System.Collections.IEnumerator RestoreAfterInitialization(LiveCampaignCheckpoint snapshot){yield return null;RestoreLive(snapshot);restoring=false;}
 void RestoreLive(LiveCampaignCheckpoint snapshot){
  try{if(snapshot.jobs==null||!Valid(snapshot.position.x)||!Valid(snapshot.position.y)||!Valid(snapshot.position.z)||snapshot.position.x<0||snapshot.position.z<0||snapshot.position.x>session.world.map.width||snapshot.position.z>session.world.map.depth)throw new Exception("Invalid live vehicle position");
   if(snapshot.mapRevision!=session.world.map.editorRevision)throw new Exception("Map revision changed; cannot restore active trip on edited geometry");
   if(session.ambientPedestrians)session.ambientPedestrians.Restore(snapshot.ambientPedestrians);
   session.taxi.SetSpawn(snapshot.position,snapshot.rotation);session.taxi.ResetTaxi();var district=session.traffic.GetComponent<DistrictTraffic>();if(district)district.ClearPlayerOverlap();session.law.tripId=snapshot.tripId??"";session.law.ResetObservation();session.jobs.nav.PreferFastest=snapshot.preferFastest;
   if(snapshot.cabinCaptured)Cabin=snapshot.cabin.Copy();
   else if(CabinEnabled&&snapshot.jobs.trip.state==TripState.OnTrip){Cabin.MeterOn=true;var behavior=snapshot.jobs.behavior;if(behavior!=null&&behavior.TemperatureSetting>=0)Cabin.Temperature=behavior.TemperatureSetting;}
   session.jobs.RestoreLive(snapshot.jobs);if(session.radio)session.radio.Restore(snapshot.radio);if(session.accident)session.accident.Restore(snapshot.accident);var occupancy=session.GetComponent<StopOccupancySystem>();if(occupancy)occupancy.Restore(snapshot.occupancy);if(session.ambientPedestrians)session.ambientPedestrians.ReconcileVisibility();realSeconds=snapshot.realSeconds;preparing=snapshot.preparing;
   if(snapshot.cabinCaptured&&Cabin.Vacancy==2&&!session.jobs.Active&&Array.TrueForAll(session.jobs.offers,offer=>offer==null))session.jobs.Generate();
   Cabin.Vacancy=0;
   if(!session.jobs.Active&&snapshot.hasDestination)session.jobs.nav.SetDestination(snapshot.destination);
   session.jobs.nav.PreferFastest=snapshot.preferFastest;session.assisted.SetActive(false);if(snapshot.operationVersion==1){if(snapshot.signalsPresent)session.signals.RestoreLive(snapshot.signals);if(snapshot.lawObservationPresent)session.law.RestoreObservation(snapshot.lawObservation);}session.paused=!preparing;Time.timeScale=0;saveStatus="중단한 위치·운송·시간·장부 복원 완료 · 계속 운전";resumeBlocked=false;if(session.damage&&ledger.Care.Damage.Disabled&&!string.IsNullOrEmpty(ledger.Care.Damage.RepairBayId)&&!session.damage.AtRepairBay)session.damage.RestoreReservedBay(false);
  }catch(Exception ex){SaveFailed=true;LoadFailureDetail=ex.Message;preparing=false;session.paused=true;Time.timeScale=0;saveStatus="운송 복원 중단 · 지도/저장 상태 확인 필요: "+ex.Message;Debug.LogWarning(saveStatus);resumeBlocked=true;}
 }
 static void ValidateLivePayload(LiveCampaignCheckpoint live,DriveSession owner){
  if(live==null)return;
  if(live.cabinCaptured){if(live.cabin==null)throw new ArgumentException("Missing cabin controls");live.cabin.Validate();}
  if(live.accident!=null)live.accident.Validate();StopOccupancyPolicy.Restore(live.occupancy);ValidateOperationState(live);
  UpgradeEungyeApproachCheckpoint(live,owner.world.map);
  if(live.mapRevision!=owner.world.map.editorRevision)throw new Exception("Map revision mismatch: saved='"+live.mapRevision+"', current='"+owner.world.map.editorRevision+"'");
  if(live.jobs==null)throw new Exception("Missing live passenger jobs");
  if(live.jobs.trip==null)throw new Exception("Missing live trip state");
  if(live.jobs.offers==null||live.jobs.offers.Length!=3)throw new Exception("Invalid live offers: expected=3, actual="+(live.jobs.offers==null?"null":live.jobs.offers.Length.ToString()));
  int expectedCooldown=live.jobs.stopIds!=null?live.jobs.stopIds.Length:owner.jobs.stops.Length;
  if(live.jobs.cooldown==null||live.jobs.cooldown.Length!=expectedCooldown)throw new Exception("Invalid live cooldown count: expected="+expectedCooldown+", actual="+(live.jobs.cooldown==null?"null":live.jobs.cooldown.Length.ToString()));
  if(double.IsNaN(live.realSeconds)||double.IsInfinity(live.realSeconds)||live.realSeconds<0)throw new Exception("Invalid live elapsed time: "+live.realSeconds);
  PassengerJobs.NormalizePresence(live.jobs);PassengerJobs.ValidateStoryPassenger(live.jobs);PassengerJobs.ValidateSituationSnapshot(live.jobs);PassengerJobs.ValidateChoiceEffectsSnapshot(live.jobs);if(live.jobs.choiceEffectsPresent&&live.jobs.trip.state==TripState.OnTrip&&live.jobs.choiceEffectsTrip!=live.tripId)throw new Exception("Choice effects trip mismatch");if(live.jobs.situation!=null&&live.jobs.situation.TripId!=live.tripId||live.jobs.errand!=null&&live.jobs.errand.TripId!=live.tripId)throw new Exception("Passenger activity trip identity mismatch");
  new FareTrip().Restore(live.jobs.trip);
  if(owner.ambientPedestrians)owner.ambientPedestrians.Validate(live.ambientPedestrians);
  if(!Valid(live.position.x)||!Valid(live.position.y)||!Valid(live.position.z)||live.position.x<0||live.position.z<0||live.position.x>owner.world.map.width||live.position.z>owner.world.map.depth||!Valid(live.rotation.x)||!Valid(live.rotation.y)||!Valid(live.rotation.z)||!Valid(live.rotation.w)||Mathf.Abs(1-(live.rotation.x*live.rotation.x+live.rotation.y*live.rotation.y+live.rotation.z*live.rotation.z+live.rotation.w*live.rotation.w))>.05f)throw new Exception("Invalid live pose");
  var j=live.jobs;int stopCount=j.stops!=null?j.stops.Length:owner.jobs.stops.Length;
  foreach(var o in j.offers)ValidateOffer(o,stopCount);ValidateOffer(j.current,stopCount);
  if((j.trip.state==TripState.OnTrip||j.trip.state==TripState.ToPickup)&&j.current==null)throw new Exception("Missing live passenger");
  if(j.passengerVia< -1||j.passengerVia>=stopCount||j.completed<0||j.failed<0||j.balance<0||!Valid(j.referenceSeconds)||j.referenceSeconds<0||!Valid(j.boardingDistance)||j.boardingDistance<0||!Valid(j.resultSeconds)||j.resultSeconds<0)throw new Exception("Invalid passenger counters");
  foreach(float v in j.cooldown)if(!Valid(v)||v<0)throw new Exception("Invalid stop cooldown");
  if(j.manners!=null){if(j.manners.score<0||j.manners.score>100||j.manners.events<0||j.manners.redEvents<0||j.manners.collisionEvents<0||j.manners.accelEvents<0||j.manners.brakeEvents<0)throw new Exception("Invalid manner snapshot");}
  if(j.request!=null)PassengerRequestUI.ValidateSnapshot(j.request);
  if(j.trip.state==TripState.OnTrip){var d=j.drunk;if(d!=null)d.Validate();if(d==null||j.manners==null||j.request==null||d.ArrivalLocked!=j.trip.arrivalLocked||d.ArrivalLocked&&j.reward==null)throw new Exception("Incomplete occupied session");foreach(float v in new[]{d.Elapsed,d.ArrivalElapsed,d.DisembarkElapsed,d.WaitElapsed,d.VomitProgress})if(!Valid(v)||v<0)throw new Exception("Invalid drunk ride time");if(d.Waiting&&(!d.ArrivalLocked||!d.SleepPlanned)||d.Settled)throw new Exception("Inconsistent drunk ride");}

 }
 static void UpgradeEungyeApproachCheckpoint(LiveCampaignCheckpoint live,MapData map){
  // Exact reviewed versions only; a new revision needs its actual Apply hash.
  // Empty future pins deliberately reject until their saved result is verified.
  const string reviewedR25Hash="970525154e204fdb578e57f1c63cb9c2b602ace906b8d56a0b68baf8bf4ec5fb";
  const string reviewedR26Hash="f6f057397dfbf09070db6da001b1a69c0eed3a545b5e0b8ae824683c0c2c0d88";
  const string reviewedR27Hash="33db5a0c7c12a1bfc40d8c7cba19033173c74d866c92528c8b1992f91fd660bf";
  const string reviewedR28Hash="0b270a36e452439324eedb558a0f01c06f54d8aa11cdae6c666b5cc3a4ce784e";
  const string reviewedR29Hash="0ac94059842e80819c2182dc84c2600937e512c535e323022ad92d9bc8d296da";
  const string reviewedR30Hash="731caa156ccba7c85d261df3ed1f44eef6315b725e0a69ce55b5f831030e3c48";
  const string reviewedR31Hash="c42ef1391c7bf32f72a528ce2fd40d8ac20dcd13d37fd738ec501c4b98f43e54";
  const string reviewedR32Hash="a0591bde41c622c2dbee164aef373086226769e4c126611c9780d5607195eb70";
  const string reviewedR33Hash="575ca884e867d4d3eecd0ee2c2488fce2d4aee7d24d370413391ddb3c84fb791";
  const string reviewedR34Hash="12cb90de24c98418e2d8bb628ffd8f7eb7d51d79f7211f867b32f11cf8aa0ea5";
  const string reviewedR35Hash="a528216cb878fa6fce9af1e2fc64c067416078a2c8e6c4874c8563792a829591";
  const string reviewedR36Hash="353fe15fe9f383001e93deafe3905f983a3fe19c0e5f47811e17e85efcf245e7";
  const string reviewedR37Hash="c37d14d6f34b192ca582a7d87c6630839787b30fcf3df1e7ef2d416ba3485ce3";
  const string reviewedR38Hash="56b01ecd46c68cb164410622d5a340c37f7e105e44b92cac03b73e10a8e3468f";
  const string reviewedR39Hash="20c2236cae3f3e11fb7d6f2435fc41d8c2ce0e4c5331f42f779c21b4d6d14595";
  const string reviewedR40Hash="b91b46e9051e5c1427a1ecd7ff40ca7fde370a0120efabe4e4918c63bb6a3634";
  const string reviewedR41Hash="57ecdb77d3b473ac1e86e83c02a45219babce1118c3f36ea4d68919a771f12fe";
  const string reviewedR42Hash="0d7c0353d3bf477d7ab1d3ea1c6c8dc35a7281e30c10ccafd6f3c3b343f5633b";
  const string reviewedR43Hash="e0f8ce711e8c1b5dcd814d0bea2c9f7f454d63baa28be4b3d8cf86df37b4d6bb";
  string[] hashes={
   "d9596da33d4efc0d4f862c714eb05bb70c94365bfc30ab6171a702e2b672e3df",
   "cb8e0ec0ed3a48f7e9a33c4ff8dc3878bd9d59aafef13c0f0fe43c5616c9c808",
   "bd8ad754a0510f0ee6fac7c54121433e1ba65bbd4b739e824500c081b446c474",
   "345d93f5499e5dbf52da3a8d831935c1447980492055f192a3280f64b7fd4ef4",
   "7bf7fb04facef74f2c4027ac93cdbc24244a278a5980f805854c70611e2cc35b",
   "aadeeb2f24c026a000b393aa938b9f625bd3f0d9db4a8a85d79a5ac13408aa45",
   "8c9a5e7f386aeeac9114b7a9c7da0c938fb8019c5456bd20bf6e6b956de388f6",
   "1233c5ca6b88d5de3c75d7542ebc294409750bd1d2daa5041658149457f7e42d",
   "38bad072a26d23185eb9f902ea347eb84dbe34c7e554b22637916ee52645f8af",
   reviewedR25Hash,reviewedR26Hash,reviewedR27Hash,reviewedR28Hash,reviewedR29Hash,reviewedR30Hash,reviewedR31Hash,reviewedR32Hash,reviewedR33Hash,reviewedR34Hash,reviewedR35Hash,reviewedR36Hash,reviewedR37Hash,reviewedR38Hash,reviewedR39Hash,reviewedR40Hash,reviewedR41Hash,reviewedR42Hash,reviewedR43Hash,"4bc4690b4e3fd618571b4ed82a53e5bb1f49e5c3339a7a6af32fd5a403cb901f","be92b289b8c5243d653df405a689f5e032c34c6cda073b345b0aa2e0be314d26","ebb810cbd2214e829af890708cd565022f449fcbcbd51039487402df81597161"};
  const string reviewedR47Hash="2ee1a3fd415b6391086ea167da864dea1db68b58fe101af9105f5be6d482cf3e";
  // r48 (2026-11-20) repaints Suin-ro lane lines only; driving lanes are unchanged.
  const string reviewedR48Hash="ac84fe3eae6e7985a77de9dd5becbd6f56811707bfe66ff2a9e6e1497d2be5ac";
  const string reviewedR49Hash="9985476e9216d487885839e8da42aa81d9651c7266fce8d779a09af560151390";
  const string reviewedR50Hash="ff4b47dc6b0208a472e86dcff375f1966f54bd22a95d8802eec9915dcde22137";
  // r51 (2026-11-23) moves the P51 junction (Hanyang/Hoban gates) to its aerial position.
  const string reviewedR51Hash="46bf8e43cc37a1ee46d4a30e4b7d9ae36168479ec5fdc6677e9f534216b110df";
  // r52 (2026-11-23) tightens the two P51 gate right turns (kerb openings match the gate crossings) and
  // runs both P51 through joins across the junction so departure lanes start past the far crosswalk.
  const string reviewedR52Hash="3efe1faff89fe9f1fb377c5999f8939a4795ded1f78d588b703e79d155913beb";
  // r53 (2026-11-24) shortens the P51 signal cycle from 98 s to 70 s (durations only; lanes unchanged).
  const string reviewedR53Hash="bcc2105057e8a9fa2ec7559438d73965f8b393cdb6c5d7efe030ab7bc535c9da";
  // r54 (2026-11-25) moves the J5692_14220 westbound signal pole off the gate-road turn path (map review P01; facility only).
  const string reviewedR54Hash="9f37adc663ea7d2ff3a99394614de915917ad67a1ba37937fe34e7477a145b8d";
  // r55 (2026-11-25) map review markings batch 1: yellow school-zone crosswalks, bicycle crossings beside 8 junctions'
  // crosswalks (moved 2.3 m toward the junction), J7841_867 diagonal crossings (crosswalk facilities only).
  const string reviewedR55Hash="78fa0713e15f970199d561c8f3ec3466a755e1c867408c737dc9afcfb5e4e816";
  // r56 (2026-11-25) map review markings batch 2: new crosswalks, school-zone paving/text, diamonds, J7917 north crosswalk removed (facilities only).
  const string reviewedR56Hash="496208f50a71ecb743152296d68877b6c90046794419ac6d0abfed620b74efc3";
  // r57 (2026-11-25) map review: crosswalks moved/deepened with their stop lines (J1927, J866, J5692, P22:56, J2145, J5057, J7973, J7841, J7917, J8066), five removed, two added (facilities only).
  const string reviewedR57Hash="b0a1e15fb67f9e6329a77bf872f0626e629ec9eec8d03faced0bf2be3de3012d";
  // r58 (2026-11-26) map review: left-turn guide lines at 7 junctions, J4396 north-west zigzag approach (facilities only).
  const string reviewedR58Hash="7cf4928a98b9f8194bddaa67d9a52a40394d1477bb53a11cb2b063eb52728a20";
  // r59 (2026-11-26) map review: straight centre-line bands on 4 stretches where the lane-based yellow line wiggled (facilities only).
  const string reviewedR59Hash="1f30cf2d01ec6247041e3489e0b871ea497791ec169d5db7601a58dc4ca9e089";
  // r60 (2026-11-26) map review: roadview-confirmed Eungye pedestrian street (Daeeun-ro - J2468 - J2640 and its alleys) becomes a plaza; 38 lanes off, 8 surfaces swapped (graph changes: navigation is recaptured).
  const string reviewedR60Hash="7adfe9e5b6bd9737236847d2a20bce5df08bfb1aa2b4b0a22dec0769c33a09b9";
  // r61 (2026-11-26) map review: the J1897 five-way pedestrian cross north of Daeeun-ro becomes a plaza; 35 lanes off, 8 surfaces swapped (graph changes: navigation is recaptured).
  const string reviewedR61Hash="984c0a1f086756a9858ae1e72fab92870f717f5dbc831c06cfa1bfb9860f15bf";
  // r62 Yellow school-zone crossings at J044 (3 arms) and J2564 (roadview 2026-11-26)
  const string reviewedR62Hash="54c6f40a26e120a76cb61eb3c1bf51eb4a2a3bfacc28dd2cd873c80ace0decb5";
  int from=0,to=0;for(int revision=14;revision<=62;revision++){string label="은계 프로젝트 지도 · r"+revision;if(live.mapRevision==label)from=revision;if(map.editorRevision==label)to=revision;}
  if(map.mapId!="eungye"||from<14||from>=to||to<16||to>62)return;
  string reviewedHash=to==62?reviewedR62Hash:to==61?reviewedR61Hash:to==60?reviewedR60Hash:to==59?reviewedR59Hash:to==58?reviewedR58Hash:to==57?reviewedR57Hash:to==56?reviewedR56Hash:to==55?reviewedR55Hash:to==54?reviewedR54Hash:to==53?reviewedR53Hash:to==52?reviewedR52Hash:to==51?reviewedR51Hash:to==50?reviewedR50Hash:to==49?reviewedR49Hash:to==48?reviewedR48Hash:to==47?reviewedR47Hash:hashes[to-16];
  if(string.IsNullOrEmpty(reviewedHash)||map.editorDocument==null||map.editorDocument.projectRevision!=reviewedHash||RoadMapRuntime.ProjectDefault==null||map.editorDocument.ToJson()!=RoadMapRuntime.ProjectDefault.ToJson())return;
  ApplyReviewedEungyeCheckpointMigration(live,from,to);
 }
 static void ApplyReviewedEungyeCheckpointMigration(LiveCampaignCheckpoint live,int from,int to){
  live.mapRevision="은계 프로젝트 지도 · r"+to;
  // r26 and r32 change paint only. Revisions 27–31 and 33–47 rebuild graph indices.
  if(to>=18&&(from<25||to>=27&&from<31||to>=33)&&live.jobs!=null){live.jobs.navigationCaptured=false;live.jobs.navAvoidEdge=live.jobs.navObservedEdge=live.jobs.navPassedObservedEdge=-1;}
  var manners=live.jobs?.manners;
  if(manners!=null){
   if(RecentlyChangedEungyeApproach(from,to,manners.crossing?.junction))manners.crossing=null;
   if(RecentlyChangedEungyeApproach(from,to,manners.lawObservation?.entry?.junction)){
    manners.lawObservation.entry=null;manners.lawObservation.entryPresent=false;
   }
  }
  var entry=live.lawObservation==null?null:live.lawObservation.entry;if(entry==null)return;
  string junction=entry.junction;
  bool cyan=from<20&&to>=20&&(junction=="J8921_10835"||junction=="J7355_12821"||junction=="J7292_13794"||junction=="J7254_14780");
  bool gap=from<21&&to>=21&&(junction=="J1882_11791"||junction=="J2225_12001");
  bool y07=from<22&&to>=22&&(junction=="J7254_14780"||junction=="J10587_15883");
  bool pink=from<23&&to>=23&&(junction=="J7876_1656"||junction=="J8346_1658");
  bool roundabout=from<24&&to>=24&&junction=="J5074_877";
  bool green31=from<25&&to>=25&&junction=="J2145_15970";
  bool p29=from<27&&to>=27&&(junction=="J7917_2565"||junction=="J7204_2569");
  bool y06=from<28&&to>=28&&junction=="J7254_14780"||from<29&&to>=29&&junction=="J5692_14220";
  bool p10=from<30&&to>=30&&(junction=="J6478_4105"||junction=="J6481_3341");
  bool commerce=from<31&&to>=31&&(junction=="J5076_2580"||junction=="J5750_2576"||junction=="J6480_2573"||junction=="J7204_2569");
  bool parking=from<33&&to>=33&&(junction=="J6481_3341"||junction=="J5750_2576"||junction=="J7204_2569");
  bool g05=from<34&&to>=34&&junction=="J6199_11987";
  bool approaches=from<35&&to>=35&&(junction=="J3516_12881"||junction=="J7801_9629");
  bool g04=from<36&&to>=36&&junction=="J7841_867";
  bool p03=from<37&&to>=37&&junction=="J1919_877";
  bool southbound=from<38&&to>=38&&(junction=="J1927_5181"||junction=="J7841_867");
  bool parkingDaeun=from<39&&to>=39&&(junction=="J6481_3341"||junction=="J3516_12881");
  bool eungyeCrossroad=from<40&&to>=40&&junction=="J4396_13458";
  bool gyesuApproaches=from<41&&to>=41&&(junction=="J4396_13458"||junction=="J5692_14220");
  bool confirmedApproaches=RecentlyChangedEungyeApproach(from,to,junction);
  bool legacy=from<20&&(to>=19&&junction=="J8066_6364"||to>=17&&junction=="J7917_2565"||from!=16&&junction=="J7973_3744"&&entry.axis==1&&entry.sign==1||from==14&&(junction=="J8066_6364"||junction=="J1927_5181"));
  if(cyan||gap||y07||pink||roundabout||green31||p29||y06||p10||commerce||parking||g05||approaches||g04||p03||southbound||parkingDaeun||eungyeCrossroad||gyesuApproaches||confirmedApproaches||legacy){live.lawObservation.entry=null;live.lawObservation.entryPresent=false;}
 }
 static bool RecentlyChangedEungyeApproach(int from,int to,string junction){
  return from<42&&to>=42&&(junction=="J2564_12237"||junction=="J7254_14780"||junction=="J8921_10835"||
   junction=="J7841_867"||junction=="J5076_2580"||junction=="J8066_6364")||
   from<43&&to>=43&&(junction=="J1927_5181"||junction=="J8066_6364"||junction=="J7841_867"||
   junction=="J7876_1656"||junction=="J5852_16963"||junction=="J7254_14780")||from<44&&to>=44&&junction=="J1927_5181"||from<45&&to>=45&&junction=="J5692_14220"||from<46&&to>=46&&junction=="J7841_867"||
   from<47&&to>=47&&(junction=="J7841_867"||junction=="J7876_1656"||junction=="J6481_3341"||junction=="J6480_2573"||junction=="J7973_3744")||
   from<53&&to>=51&&junction=="reviewed:r47:P51";
 }
 static void ValidateOffer(JobOffer o,int count){if(o!=null&&(o.pickup<0||o.pickup>=count||o.drop<0||o.drop>=count||o.fare<0))throw new Exception("Invalid live stop reference");}
 static bool Valid(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
 public static void ValidateOperationState(LiveCampaignCheckpoint live){
  if(live==null)return;
  if(live.operationVersion==0){live.signals=null;live.lawObservation=null;return;}
  if(live.operationVersion!=1||live.signalsPresent&&live.signals==null||live.lawObservationPresent&&live.lawObservation==null)throw new ArgumentException("Invalid live operation state");
  if(!live.signalsPresent)live.signals=null;if(!live.lawObservationPresent)live.lawObservation=null;
  TaxiSignals.ValidateSnapshot(live.signals);var law=live.lawObservation;if(law==null)return;
  NormalizeLawObservation(law);
  foreach(float value in new[]{law.wrongSeconds,law.wrongDistance,law.clearSeconds})if(!Valid(value)||value<0)throw new ArgumentException("Invalid law observation time");

 }
 static bool EmptyRoadObservation(RoadObservation o){return o==null||!o.known&&!o.trafficClear&&!o.dedicated&&!o.pedestrianGreen&&(int)o.forward==0&&(int)o.right==0&&(int)o.pedestrian==0;}
 static bool EmptyLawEntry(LawEntrySnapshot e){return e==null||string.IsNullOrEmpty(e.junction)&&string.IsNullOrEmpty(e.id)&&e.axis==0&&e.sign==0&&e.direction==Vector3.zero&&!e.edited&&!e.uturnSigned&&!e.uturnAllowed&&!e.stopped&&!e.pedestrianYield&&!e.pedestrianConflict&&!e.encroached&&!e.entered&&(int)e.left==0&&e.necessaryBefore==0&&!e.slowPresent&&(e.slow==null||e.slow.version==1&&e.slow.observedSeconds==0&&e.slow.continuousExcess==0&&!e.slow.violated&&!e.slow.necessary)&&EmptyRoadObservation(e.observed);}
 static bool EmptyUpstream(UpstreamUturnSnapshot u){return u==null||string.IsNullOrEmpty(u.connector)&&string.IsNullOrEmpty(u.junction)&&string.IsNullOrEmpty(u.lane)&&string.IsNullOrEmpty(u.id)&&u.axis==0&&u.sign==0&&!u.allowed&&!u.confirmed&&!u.yieldViolated&&EmptyRoadObservation(u.observation);}
 public static void NormalizeLawObservation(DrivingLawSnapshot law){
  if(law==null)return;if(law.observationVersion<0||law.observationVersion>1)throw new ArgumentException("Unknown law observation version");
  if(law.observationVersion==1){if(!law.entryPresent)law.entry=null;if(!law.upstreamPresent)law.upstreamUturn=null;}
  else{if(EmptyLawEntry(law.entry))law.entry=null;if(EmptyUpstream(law.upstreamUturn))law.upstreamUturn=null;}
  var u=law.upstreamUturn;if(law.observationVersion==1&&law.upstreamPresent&&u==null)throw new ArgumentException("Missing upstream turn state");
  if(u!=null&&(string.IsNullOrEmpty(u.connector)||string.IsNullOrEmpty(u.junction)||string.IsNullOrEmpty(u.lane)||string.IsNullOrEmpty(u.id)||u.axis<0||u.axis>1||(u.sign!=-1&&u.sign!=1)||u.observation==null))throw new ArgumentException("Invalid upstream turn state");
  if(law.observationVersion==1&&law.entryPresent&&law.entry==null)throw new ArgumentException("Missing law entry");
  var e=law.entry;if(e==null)return;
  if(string.IsNullOrEmpty(e.junction)||string.IsNullOrEmpty(e.id)||e.axis<0||e.axis>1||(e.sign!=-1&&e.sign!=1)||e.observed==null||!Valid(e.direction.x)||!Valid(e.direction.y)||!Valid(e.direction.z)||e.direction.sqrMagnitude<.5f||e.necessaryBefore<0)throw new ArgumentException("Invalid live law entry");
  if(e.slowPresent){if(e.slow==null)throw new ArgumentException("Missing slow-turn state");e.slow.Validate();}else e.slow=null;
 }
 LiveCampaignCheckpoint CaptureLive(){return new LiveCampaignCheckpoint{radio=session.radio?session.radio.Capture():null,cabinCaptured=CabinEnabled,cabin=CabinEnabled?Cabin.Copy():null,accident=session.accident?session.accident.Capture():null,operationVersion=1,signalsPresent=session.signals!=null,signals=session.signals?session.signals.CaptureLive():null,lawObservationPresent=session.law!=null,lawObservation=session.law?session.law.CaptureObservation():null,occupancy=session.GetComponent<StopOccupancySystem>()?session.GetComponent<StopOccupancySystem>().Capture():null,mapRevision=session.world.map.editorRevision,tripId=session.law.tripId,position=session.taxi.transform.position,rotation=session.taxi.transform.rotation,jobs=session.jobs.CaptureLive(),ambientPedestrians=session.ambientPedestrians?session.ambientPedestrians.Capture():null,preparing=preparing,preferFastest=session.jobs.nav.PreferFastest,hasDestination=session.jobs.nav.HasDestination,destination=session.jobs.nav.destination,realSeconds=realSeconds};}
 float nextSave;int lastLawCount=-1;
 void OnApplicationQuit(){if(!testMode&&enabledCampaign&&session&&session.requests)SaveCheckpoint();}
 void OnApplicationPause(bool value){if(value&&!testMode&&enabledCampaign&&session&&session.requests)SaveCheckpoint();}
 System.Collections.IEnumerator RestoreReservedDamageBay(){while(!session.StartupReady)yield return null;session.damage.RestoreReservedBay();session.law.ResetObservation();session.jobs.ResetOffers();}
 void ParkAtShiftStart(){
  var stop=session.services.ParkingStop;
  float height=session.services.HasHomeBase?HomeGarage.FloorTop+session.taxi.wheels[0].radius:.65f;
  session.taxi.SetSpawn(stop.Kerb+Vector3.up*height,Quaternion.LookRotation(stop.Direction));session.taxi.ResetTaxi();
 }
 public System.Collections.IEnumerator PlaceAtParking(){yield return null;if(session.damage&&session.damage.Disabled&&!string.IsNullOrEmpty(ledger.Care.Damage.RepairBayId)){session.damage.RestoreReservedBay();session.law.ResetObservation();session.jobs.ResetOffers();yield break;}ParkAtShiftStart();session.law.ResetObservation();session.jobs.ResetOffers();}

 void FixedUpdate(){
  if(PvpNativeLan.IsReplica(session)||session&&session.simulationMatch&&PvpNativeLan.WaitingForWorld)return;
  if(session.accident&&session.accident.Locked){session.accident.Tick(Time.fixedDeltaTime);return;}
  if(session.damage&&session.damage.AdvancePendingService(Time.fixedDeltaTime)){session.simulationMatch.ObserveAccidentClock(session);return;}
  if(session.simulationMatch&&session.simulationMatch.AdvanceConditionService(session,Time.fixedDeltaTime))return;
  if(session.simulationMatch&&session.simulationMatch.ServiceLocked(session))return;
  if(!enabledCampaign||!ledger.Operating||preparing||restoring||resumeBlocked||session.paused||Time.timeScale<=0)return;
  if(ledger.Driver!=null&&!DrowsinessRunning)return;
  if(CabinEnabled){
   Cabin.AdvanceClimate(Time.fixedDeltaTime);
   if(session.jobs.Behavior!=null)session.jobs.Behavior.ApplyCabinTemperature(Cabin.Comfort);
  }
  var v=session.taxi.body.velocity;double metres=Math.Sqrt(v.x*v.x+v.z*v.z)*Time.fixedDeltaTime;
  if(session.simulationMatch)SimulationOperationSeconds+=Time.fixedDeltaTime;
  if(!session.PracticeBusy&&!PracticeSession.Enabled)ledger.AdvanceOperation(Time.fixedDeltaTime,metres,session.jobs.trip.State==TripState.OnTrip);else ledger.Advance(Time.fixedDeltaTime,metres);
  if(ledger.Driver!=null)ledger.Driver.drowsiness.Advance(Time.fixedDeltaTime,DrowsinessRunning,ledger.Driver.fatigue,session.taxi.body.velocity.magnitude>=.56f);
  if(DrowsyControlsLost&&!(session.damage&&session.damage.Disabled)){session.assisted.SetActive(false);if(session.taxi.body.velocity.sqrMagnitude>.001f)fatigueBrakeUntil=Time.fixedTime+.1f;session.taxi.body.velocity=Vector3.MoveTowards(session.taxi.body.velocity,Vector3.zero,6*Time.fixedDeltaTime);if(session.taxi.body.velocity.magnitude<.56f){session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;session.jobs.CancelForFatigue();}}
  if(ledger.Account!=null&&ledger.Account.Fuel<=0&&!(session.damage&&session.damage.Disabled))RecoverFuel();
 }
 void Update(){
  if(IntroInputBlocked)return;
  RefreshDailyWeather();
  if(CabinEnabled&&DrowsinessRunning){
   if(CabinAutomatic)Cabin.ToggleMeter(session.jobs.trip.State==TripState.OnTrip);
   if(Input.GetKeyDown(KeyCode.J))CabinInput(CabinAction.Meter);
   if(Input.GetKeyDown(KeyCode.K))CabinInput(CabinAction.Window);
   if(Input.GetKeyDown(KeyCode.LeftBracket))CabinInput(CabinAction.Cooler);
   if(Input.GetKeyDown(KeyCode.RightBracket))CabinInput(CabinAction.Warmer);
  }
  if(PvpNativeLan.IsReplica(session)||session&&session.simulationMatch&&PvpNativeLan.WaitingForWorld)return;if(enabledCampaign&&!preparing&&!session.paused&&ledger.Operating&&(ledger.Driver==null||DrowsinessRunning)){realSeconds+=Time.unscaledDeltaTime;mixedFollowing|=session.traffic.mitOptions.Enabled!=initialFollowing;}if(EveningDrive){if(Time.timeScale>0&&!session.paused){EveningDriveSeconds+=Time.deltaTime;if(!session.FullMapOpen&&(Input.GetKeyDown(KeyCode.F)||Input.GetKeyDown(KeyCode.JoystickButton8)))EndEveningDrive();}}else if(enabledCampaign&&Input.GetKeyDown(KeyCode.N))RequestEndDay();if(noticeUntil>0&&Time.unscaledTime>noticeUntil){notice="";noticeUntil=0;}SyncVehicle();}
 void LateUpdate(){
  if(PvpNativeLan.IsReplica(session)||session&&session.simulationMatch&&PvpNativeLan.WaitingForWorld)return;
  if(SimulationSession||session.accident&&session.accident.Locked)return;
  if(!restoring&&!testMode&&enabledCampaign&&session.law&&session.requests&&(Time.unscaledTime>=nextSave||session.law.ledger.Count!=lastLawCount)){nextSave=Time.unscaledTime+5;lastLawCount=session.law.ledger.Count;SaveCheckpoint();}
  if(session.FullMapOpen||!enabledCampaign||ledger.State!=BusinessState.Closing)return;
  if(session.jobs.trip.State==TripState.OnTrip){notice="영업 종료 대기 · 현재 승객 운송 후 정산합니다";return;}
  session.jobs.RecordExpiredTrip();
  if(session.damage&&session.damage.Disabled&&!session.damage.AtRepairBay)return;
  if(ledger.Settle()){ledger.Last.realSeconds=realSeconds;taxPrompt=false;ObserveDailyConditionSettlement();ObserveStorySettlement();if(PassengerStoriesEnabled)Logbook.ObserveSettlement(!Array.Exists(session.law.ledger.Events,entry=>entry.day==ledger.Day&&entry.HasViolation));session.jobs.ClearForDay();session.services.open=false;session.services.pending=-1;Freeze();SaveCheckpoint();}
 }
 void Freeze(){session.paused=false;Time.timeScale=0;}
 public bool RequestEmergencyHome(){if(session.accident&&session.accident.Locked)return false;
  if(PvpNativeLan.Route(session,"emergency-rest"))return true;
  if(session.simulationMatch){if(session.simulationMatch.ServiceLocked(session)||!ledger.Driver.drowsiness.Blackout||session.taxi.body.velocity.magnitude>=.56f)return false;session.jobs.CancelForFatigue();if(session.jobs.Active||!ledger.UseEmergencySleepInMatch(session.law.ids.Next("pvp-emergency-rest")))return false;session.jobs.ClearForDay();session.assisted.SetActive(false);notice="긴급 휴식 100초 · 피로 −20 · 경기 계속";noticeUntil=Time.unscaledTime+10;SaveCheckpoint();return true;}
  if(session.damage&&session.damage.Disabled&&!session.damage.AtRepairBay)return false;
  if(!DrowsinessRunning||!ledger.Driver.drowsiness.Blackout||session.taxi.body.velocity.magnitude>=.56f)return false;
  session.jobs.CancelForFatigue();if(session.jobs.Active||!ledger.UseEmergencySleep(session.law.ids.Next("emergency-home")))return false;
  session.jobs.ClearForDay();session.assisted.SetActive(false);if(!(session.damage&&session.damage.Disabled&&session.damage.AtRepairBay)){var stop=session.services.ParkingStop;session.taxi.SetSpawn(stop.Kerb+Vector3.up*.65f,Quaternion.LookRotation(stop.Direction));session.taxi.ResetTaxi();}session.law.ResetObservation();var district=session.traffic.GetComponent<DistrictTraffic>();if(district)district.ClearPlayerOverlap();notice="졸음 운전으로 위험합니다. 오늘 영업을 종료합니다. · 피로 −20";noticeUntil=Time.unscaledTime+10;SaveCheckpoint();return true;
 }
 public bool CanChangeShiftLength {get{return enabledCampaign&&!SimulationSession&&!PracticeSession.Enabled&&preparing&&!resumeBlocked&&!restoring&&!restartPrompt&&ledger!=null&&ledger.Account!=null&&ledger.OneDay&&ledger.State==BusinessState.Open&&ledger.Elapsed<=0;}}
 public int ShiftMinutes {get{return ledger==null?0:Mathf.RoundToInt(ledger.Settings.daySeconds/60f);}}
 // Applies to the day being prepared and persists in the campaign account for later days.
 public bool SetShiftLength(int minutes){if(!CanChangeShiftLength||Array.IndexOf(PrototypeSettings.ShiftMinuteChoices,minutes)<0)return false;float seconds=minutes*60f;if(!ledger.SetDaySeconds(seconds))return false;settings.daySeconds=seconds;notice="영업 시간 "+minutes+"분으로 설정했습니다.";noticeUntil=Time.unscaledTime+5;SaveCheckpoint();return true;}
 public bool CanDevChangeShiftLength {get{return enabledCampaign&&!SimulationSession&&!PracticeSession.Enabled&&!resumeBlocked&&!restoring&&ledger!=null&&ledger.Account!=null&&ledger.OneDay&&ledger.State==BusinessState.Open;}}
 // Developer test: same choices as preparation, but also while operating.
 public bool DevSetShiftLength(int minutes){if(!CanDevChangeShiftLength)return EndDayNotice("싱글 영업 중이나 영업 준비 중에만 바꿀 수 있습니다.");if(Array.IndexOf(PrototypeSettings.ShiftMinuteChoices,minutes)<0)return false;float seconds=minutes*60f;if(!ledger.DevSetDaySeconds(seconds))return EndDayNotice("영업 시간을 바꿀 수 없습니다.");settings.daySeconds=seconds;notice="(개발용) 영업 시간 "+minutes+"분으로 변경했습니다.";noticeUntil=Time.unscaledTime+5;SaveCheckpoint();return true;}
 // Developer test shortcut: skips the hub/stop/rest conditions of RequestEndDay and settles the day.
 public bool DevEndDayNow(){
  if(SimulationSession)return EndDayNotice("PVP에서는 사용할 수 없습니다.");
  if(!enabledCampaign||ledger==null||preparing||ledger.State!=BusinessState.Open)return EndDayNotice("영업 중일 때만 바로 퇴근할 수 있습니다.");
  if(session.accident&&session.accident.Locked)return EndDayNotice("사고 처리를 마친 뒤 사용해 주세요.");
  if(session.damage&&session.damage.Disabled&&!session.damage.AtRepairBay)return EndDayNotice("차량 고장 처리 중에는 사용할 수 없습니다.");
  if(session.jobs.trip.State==TripState.OnTrip)return EndDayNotice("승객을 내려드린 뒤 사용해 주세요.");
  if(session.jobs.Active)session.jobs.ClearForDay();
  ledger.RequestClose("개발용 바로 퇴근");session.paused=false;Time.timeScale=1;notice="개발용 바로 퇴근 · 정산합니다.";noticeUntil=Time.unscaledTime+8;return true;
 }
 bool EndDayNotice(string message){notice=message;noticeUntil=Time.unscaledTime+8;return false;}
 public bool RequestEndDay(){
  if(session.accident&&session.accident.Locked)return EndDayNotice("사고 처리를 마친 뒤 퇴근할 수 있습니다.");
  if(SimulationSession)return EndDayNotice("PVP 경기는 제한 시간이 끝나면 정산합니다.");
  if(session.FullMapOpen)return EndDayNotice("전체 지도를 닫고 퇴근을 눌러 주세요.");
  if(!enabledCampaign||ledger==null||ledger.State!=BusinessState.Open)return EndDayNotice("현재 영업 중이 아닙니다. 영업 상태를 확인해 주세요.");
  if(session.paused||Time.timeScale==0)return EndDayNotice("일시정지를 해제한 뒤 퇴근을 눌러 주세요.");
  if(session.jobs.trip.State==TripState.OnTrip)return EndDayNotice("승객을 목적지에 내려드린 뒤 퇴근할 수 있습니다.");
  if(session.taxi.body.velocity.magnitude>=.56f)return EndDayNotice("안전한 곳에 정차한 뒤 퇴근을 다시 눌러 주세요.");
  if(ledger.OneDay&&!session.services.AtEndDayHub())return EndDayNotice(session.services.HasHomeBase?HomeBasePresentation.EndNotice:"주차장 또는 휴식 거점에 정차해야 퇴근할 수 있습니다. 생활거점(F7)에서 길 안내를 받으세요.");
  if(ledger.OneDay&&!ledger.UseSleep(session.law.ids.Next("end-rest")))return EndDayNotice("휴식을 시작할 수 없습니다. 영업 상태와 거점 위치를 확인해 주세요.");
  ledger.RequestClose("생활 거점 휴식·영업 종료");notice="오늘 영업을 마칩니다. 정산을 준비하고 있습니다.";noticeUntil=Time.unscaledTime+8;return true;
 }
 public void RecordPassenger(int paid,string outcome){
  if(!useV2||PracticeSession.Enabled||session.jobs.current==null||string.IsNullOrEmpty(session.law.tripId))return;
  foreach(var old in passengers)if(old.tripId==session.law.tripId)return;
  var jobs=session.jobs;var offer=jobs.current;var reward=jobs.RequestReward??PassengerReward.Calculate(RequestKind.Quiet,session.manners.ledger.Score,offer.requiredScore,offer.fare,paid,0,paid>0,true);
  var result=new PassengerResult{missionKind=reward.missionKind,missionBonus=reward.missionBonus,missionSuccess=reward.missionSuccess,tip=reward.tip,tipChance=reward.tipChance,tipPoints=reward.tipPoints,tipStreak=reward.tipStreak,tipLine=reward.tipLine,rideDistance=offer.distance,meterDeduction=reward.meterDeduction,boardingDrivingScore=reward.boardingDrivingScore,drivingAdjustedFare=reward.drivingAdjustedFare,comfortDelta=reward.comfortDelta,initialFare=jobs.InitialFare,farePolicyVersion=reward.farePolicyVersion,hasDrivingScore=true,drivingScore=session.law.ledger.TotalScore,tripId=session.law.tripId,offerId=jobs.OfferId,day=ledger.Day,pickupStopId=offer.pickupStopId,dropStopId=offer.dropStopId,mapRevision=offer.mapRevision,catalogRevision=offer.catalogRevision,stopEvidence=offer.stopEvidence,pickup=KoreanText.Stop(jobs.stops[offer.pickup].name),destination=KoreanText.Stop(jobs.stops[offer.drop].name),outcome=outcome,quoted=offer.fare,paid=paid,baseFare=reward.baseFare,bonus=reward.bonus,satisfaction=reward.satisfaction,satisfactionDelta=reward.satisfactionDelta,excessScore=reward.excessScore,bonusReason=reward.bonusReason,startSatisfaction=reward.startSatisfaction,startDeficit=reward.startDeficit,cleanBonus=reward.cleanBonus,driveDelta=reward.driveDelta,timeDelta=reward.timeDelta,referenceSeconds=reward.referenceSeconds,timeReason=reward.timeReason,cleanlinessAfter=ledger.Care.Cleanliness,drunk=jobs.DrunkRide!=null&&jobs.DrunkRide.IsDrunk,vomited=jobs.DrunkRide!=null&&jobs.DrunkRide.Vomited,wakeDelayed=jobs.DrunkRide!=null&&jobs.DrunkRide.SleepPlanned,arrivalElapsed=jobs.DrunkRide==null?0:jobs.DrunkRide.ArrivalElapsed,disembarkElapsed=jobs.DrunkRide==null?0:jobs.DrunkRide.DisembarkElapsed,score=reward.sourceManner,required=offer.requiredScore,elapsed=Mathf.Max(0,offer.seconds-jobs.trip.Remaining),request=(jobs.Behavior==null?"":jobs.BehaviorSummary+"\n")+session.requests.Summary,mannerReason=session.manners.RideReasons};
  if(PassengerStoriesEnabled){
   result.passengerPresetId=jobs.Behavior?.PresetId;
   var story=jobs.CurrentStory;
   if(story!=null){result.storyId=story.id;result.storyEnding=(int)jobs.StoryEnding;result.storyLine=jobs.StoryEndingLine;}
   if(outcome=="운송 완료"||outcome=="시간 초과 후 목적지 도착"){
    if(offer.reunionKind!=0){
     Logbook.studentReturned=true;
     result.reunionLine=Logbook.studentNote?"그때 적어 드렸죠? 다음엔 꼭 인사드린다고.":"다음에 길에서 보면 제가 먼저 인사할게요.";
    }else if(story?.id=="last-train"&&Logbook.studentDay==0){
     Logbook.studentDay=ledger.Day;Logbook.studentMinute=offer.studentBoardMinute>0?offer.studentBoardMinute:jobs.StudentMinute;
     Logbook.studentStop=offer.pickupStopId;Logbook.studentPlace=KoreanText.Stop(result.pickup);
     Logbook.studentFirstTrip=result.tripId;Logbook.studentFirstOffer=result.offerId;Logbook.studentNote=result.storyEnding==(int)PassengerStoryEnding.Good;
    }
   }
   Logbook.ObserveRide(result,DayCondition==DailyConditionKind.Rain,DayCondition==DailyConditionKind.Friday);
   Story.NeighborhoodProgress(Logbook.StarredNeighborhoods);
  }
  passengers.Add(result);
 }
 public void RecordFatigueCancellation(){RecordPassenger(0,"졸음 위험 · 중도 하차/운송 취소");foreach(var result in passengers)if(result.tripId==session.law.tripId){result.cancelledForFatigue=true;result.baseFare=result.bonus=result.paid=0;result.bonusReason="졸음 위험 운송 취소";}}
 public void RecordDamageCancellation(){RecordPassenger(0,"차량 내구도 0 · 안전 하차/운송 취소");foreach(var result in passengers)if(result.tripId==session.law.tripId){result.baseFare=result.bonus=result.paid=0;result.bonusReason="차량 고장 운송 취소";}}
 public void RecordFare(int amount){if(!enabledCampaign)return;if(ledger.Account!=null)ledger.RecordFare(session.law.tripId,amount);else ledger.RecordFare(amount);}
 public void RecordFailure(){if(session&&session.manners)session.manners.FinishTrip(false,ledger.Day);if(enabledCampaign)ledger.RecordFailure();}
 // 08 chapter 2: the passenger tip is its own account entry ("tip:<trip>"), never part of the fare.
 public bool RecordReward(string id,int amount){return enabledCampaign&&amount>0&&!string.IsNullOrEmpty(id)&&ledger.RecordReward(id,amount);}
 public bool RecordHurry(string trip,int amount){return enabledCampaign&&amount>0&&!string.IsNullOrEmpty(trip)&&ledger.RecordHurry(trip,amount);}
 public void RecordMission(string trip,int amount){if(!enabledCampaign||amount<=0||string.IsNullOrEmpty(trip))return;ledger.RecordMission(trip,amount);}
 public void RecordTip(string trip,int amount){if(!enabledCampaign||amount<=0||string.IsNullOrEmpty(trip))return;ledger.RecordTip(trip,amount);}
 public bool RoomOpen; public PersonalRoomState ArchivedRoom;
 public bool RoomAvailable {get{return !SimulationSession&&!PracticeSession.Enabled&&ledger!=null&&ledger.Account!=null&&ledger.Settings.days==3&&ShowingSummary&&!restartPrompt&&!ResumeBlocked;}}
 public bool ChangeRoom(string action,string item="",int style=0){
  if(!RoomAvailable||ledger.Account.Room.finalized||style<0||style>1)return false;
  if(action=="finish"&&ledger.IsContinuousCareer)return false;
  if(action=="finish"){if(ledger.State!=BusinessState.Completed)return false;}else if(item!="lamp"&&item!="rug")return false;
  var previous=ledger;var candidate=EconomyLedger.RestoreCampaign(ledger.Snapshot(),ledger.Account.Snapshot());var room=candidate.Account.Room;
  if(action=="buy"){if(!candidate.Account.BuyRoomItem(item,style,ledger.Day))return false;}
  else if(action=="wish")room.wished=room.wished==item?"":item;
  else if(action=="finish")room.finalized=true;
  else if(item=="lamp"&&room.lampOwned){if(action=="style")room.lampStyle=style;else if(action=="place")room.lampPlaced=!room.lampPlaced;else if(action=="light")room.lampOn=!room.lampOn;else return false;}
  else if(item=="rug"&&room.rugOwned){if(action=="style")room.rugStyle=style;else if(action=="place")room.rugPlaced=!room.rugPlaced;else return false;}
  else return false;
  ledger=candidate;if(!SaveCheckpoint()){ledger=previous;return false;}return true;
 }
 // Garage tab of the personal space (plan 22). Same contract as ChangeRoom: mutate a restored
 // copy, persist once, keep the previous ledger if the save fails (no double charge on retry).
 public bool GarageTab;
 public VehicleSpec ActiveVehicle {get{return SimulationSession||ledger==null||ledger.Account==null?VehicleCatalog.Standard:ledger.Account.VehicleSpec;}}
 public double ActiveRideComfort {get{return SimulationSession||ledger==null||ledger.Account==null?1:ledger.Account.RideComfort;}}
 public bool ChangeWorkshopPart(string id,WorkshopPartAction action){
  if(!SupportsCareer||!session.services.WorkshopShopAvailable||!session.services.WorkshopShopOpen)return false;
  var previous=ledger;
  var candidate=EconomyLedger.RestoreCampaign(previous.Snapshot(),previous.Account.Snapshot());
  if(!candidate.Account.ChangeWorkshopPart(id,action,candidate.Day))return false;
  ledger=candidate;
  if(!SaveCheckpoint()){ledger=previous;return false;}
  SyncVehicle();return true;
 }
 public bool ChangeVehicle(string action,string item="",int value=0){
  if(!RoomAvailable||ledger.Account.Room.finalized)return false;
  var previous=ledger;var candidate=EconomyLedger.RestoreCampaign(ledger.Snapshot(),ledger.Account.Snapshot());
  bool ok=action=="buy"?candidate.PurchaseVehicle(item):action=="select"?candidate.SelectOwnedVehicle(item):action=="color-buy"?candidate.BuyVehicleColor(item):action=="ornament-buy"?candidate.BuyVehicleOrnament():action=="color"?candidate.Account.SelectVehicleColor(value,item):action=="ornament"&&candidate.Account.ToggleOrnament();
  if(!ok)return false;
  var priorStory=Story;string priorLine=StoryMonologue;
  bool priorComparison=GoalComparison;
  ledger=candidate;
  if(StoryEnabled){
   Story=Story.Copy();
   if(!Story.firstPurchase&&(action=="buy"||action=="color-buy"||action=="ornament-buy")){
    Story.firstPurchase=true;StoryMonologue="아버지 차에 처음으로 내 것을 달았다.";
   }
   ObserveStoryGoal();
   if(action=="buy")StoryMonologue="아버지 차는 집 앞에 세워 두었다.";
  }
  if(!SaveCheckpoint()){ledger=previous;Story=priorStory;StoryMonologue=priorLine;GoalComparison=priorComparison;return false;}
  VehicleUnlockStore.Record(ledger.Account.Vehicle);
  var father=ledger.Account.OwnedVehicle(VehicleCatalog.Starter);
  if(father!=null&&father.altColorOwned)VehicleUnlockStore.Record(father);
  SyncVehicle();return true;
 }
 string appliedVehicle;
 // Appearance and turning follow the owned car; PVP sessions keep the standard taxi here.
 public void SyncVehicle(){
  if(SimulationSession||!session||!session.taxi||ledger==null||ledger.Account==null)return;HomeGarageParking.Sync(session);var v=ledger.Account.ActiveVehicle;
  string key=v.modelId+":"+v.color+":"+(v.ornamentOwned&&v.ornamentPlaced)+":"+v.equippedParts;if(key==appliedVehicle)return;appliedVehicle=key;
  // The PVP unlock file lives beside the campaign save (test fixtures stay isolated with their save).
  if(!string.IsNullOrEmpty(SavePath))VehicleUnlockStore.UseDirectory(Path.GetDirectoryName(SavePath));
  VehicleAppearance.Apply(session.taxi,v);session.taxi.VehicleSteerFactor=(float)ActiveVehicle.steerFactor;VehicleFeel.Apply(session.taxi,ActiveVehicle.id);
  if(v.modelId!=VehicleCatalog.Starter||v.altColorOwned||v.ornamentOwned||!string.IsNullOrEmpty(v.tradedColorModel))VehicleUnlockStore.Record(v);
 }
 public bool NextDay(){
  if(EveningDrive||session.FullMapOpen||restartPrompt||pendingPayment>=0||!enabledCampaign||ledger.State!=BusinessState.Summary||session.GetComponent<PracticeSession>().Busy)return false;
  if(!SaveCheckpoint())return false;
  if(ledger.Account==null){if(!ledger.NextDay())return false;realSeconds=0;PrepareDay();return true;}
  var next=EconomyLedger.RestoreCampaign(ledger.Snapshot(),ledger.Account.Snapshot());if(!next.NextDay())return false;
  if(!WriteCheckpoint(next,session.law.ids))return false;ledger=next;RoomOpen=LogbookOpen=GoalComparison=false;StoryMonologue="";ArchivedRoom=null;CaptureDayStart();
  realSeconds=0;PrepareDay();return true;
 }
 public bool BeginNextCampaignDay(){return NextDay();}
 public bool SupportsCareer {get{return settings!=null&&settings.homeBase&&settings.days==3&&!SimulationSession&&!RoadMapRuntime.Trial&&!PracticeSession.Enabled;}}
 public bool CanContinueCareer {get{return SupportsCareer&&ShowingSummary&&ledger.State==BusinessState.Completed&&!ledger.IsContinuousCareer&&!ResumeBlocked;}}
 public bool BeginContinuousCareer(){
  if(!CanContinueCareer||restartPrompt||SaveFailed)return false;
  var next=EconomyLedger.RestoreCampaign(ledger.Snapshot(),ledger.Account.Snapshot());
  if(!next.ContinueCareer()||!WriteCheckpoint(next,session.law.ids))return false;
  ledger=next;RoomOpen=LogbookOpen=GoalComparison=false;StoryMonologue="";ArchivedRoom=null;CaptureDayStart();realSeconds=0;PrepareDay();return true;
 }
 public bool ChooseCareerGoal(string goal,string model=""){
  bool morningMemo=RoomOpen&&StoryEnabled&&PersonalRoomHUD.MorningPreviewAvailable(this);
  if(!SupportsCareer||ResumeBlocked||!(morningMemo||WallMemoAvailable||ShiftDevEndProbe.CareerCheck&&preparing))return false;
  var previous=ledger;
  var next=EconomyLedger.RestoreCampaign(ledger.Snapshot(),ledger.Account.Snapshot());
  if(goal=="car"&&string.IsNullOrEmpty(model))model=VehicleCatalog.Hybrid;
  if(!next.Account.SetCareerGoal(goal,model))return false;
  var priorStory=Story;string priorLine=StoryMonologue;
  bool priorComparison=GoalComparison;
  Story=Story.Copy();Story.wallChoice=goal;Story.wallPromptDay=ledger.Day;
  ledger=next;ObserveStoryGoal();
  if(SaveCheckpoint()){if(morningMemo)RoomOpen=false;return true;}
  ledger=previous;Story=priorStory;StoryMonologue=priorLine;GoalComparison=priorComparison;return false;
 }
 public bool CareerSlotsFull {get{return SupportsCareer&&CampaignSaveLocation.Full(Path.GetDirectoryName(SavePath));}}
 public string[] CareerSlots(){return SupportsCareer?CampaignSaveLocation.ListCareers(Path.GetDirectoryName(SavePath)):new string[0];}
 public bool OpenCareerSlot(string path){
  if(!SupportsCareer||!Array.Exists(CareerSlots(),slot=>slot==path))return false;
  if(path!=SavePath&&!ResumeBlocked&&!SaveCheckpoint())return false;
  try{CampaignSaveLocation.SelectCareer(path);}catch(Exception ex){EndDayNotice("경력 선택을 저장하지 못했습니다: "+ex.Message);return false;}
  UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);return true;
 }
 public void RestartCampaign(){
  if(session.GetComponent<PracticeSession>().Busy)return;
  if(ledger.State!=BusinessState.Won&&ledger.State!=BusinessState.Lost)return;
  ResetCampaign(false);
 }
 bool ResetCampaign(bool showPreparation){
  bool separate=SupportsCareer&&!testMode&&showPreparation;
  if(!separate&&!SimulationSession&&ledger!=null&&ledger.Account!=null&&ledger.Settings.days==3&&ledger.State==BusinessState.Completed&&!ledger.Account.Room.finalized){bool prompt=restartPrompt;restartPrompt=false;bool finished=ChangeRoom("finish");restartPrompt=prompt;if(!finished)return false;}
  if(SimulationSession)return false;
  if(resumeBlocked&&!PreserveRejectedFiles())return false;
  if(separate&&!resumeBlocked&&ledger.Operating&&File.Exists(SavePath)&&!SaveCheckpoint())return false;
  string previousProfile=ContentTuningRuntime.Snapshot();ContentTuningRuntime.LoadActive();
  var next=useV2?new EconomyLedger(settings):new EconomyLedger(new EconomySettings());
  var nextIds=new PrototypeIds(Guid.NewGuid().ToString("N"));
  string previousPath=SavePath;
  // Three slots only: with all three in use the new career takes the place of the current one.
  bool replace=separate&&CampaignSaveLocation.Full(Path.GetDirectoryName(previousPath));
  if(separate)SavePath=CampaignSaveLocation.NewCareer(Path.GetDirectoryName(previousPath));
  if(!WriteCheckpoint(next,nextIds)){SavePath=previousPath;ContentTuningRuntime.Restore(previousProfile);return false;}
  if(separate)try{CampaignSaveLocation.SelectCareer(SavePath);}catch(Exception ex){SavePath=previousPath;ContentTuningRuntime.Restore(previousProfile);return EndDayNotice("새 경력 선택을 기록하지 못했습니다: "+ex.Message);}
  if(replace)try{CampaignSaveLocation.Retire(previousPath);}catch(Exception ex){Debug.LogWarning("CAREER_SLOT_RETIRE_FAILED "+ex.Message);}
  ledger=next;RoomOpen=false;ArchivedRoom=null;CaptureDayStart();loaded=null;resumeBlocked=false;realSeconds=0;initialFollowing=session.traffic.mitOptions.Enabled;mixedFollowing=false;passengers.Clear();practices.Clear();pendingPayment=-1;restartPrompt=false;noticeUntil=0;
  if(session.law){session.law.ledger=new LawLedger();session.law.ids=nextIds;}
  session.jobs.balance=session.jobs.completed=session.jobs.failed=0;session.services.notice=session.services.destination="";session.services.pending=-1;
  session.GetComponent<PracticeSession>().notice="";session.visits=0;dailyFirstDay=1;PrepareDay();
  session.jobs.ResetOffers();
  if(showPreparation&&!preparing){preparing=true;Time.timeScale=0;StartCoroutine(PlaceAtParking());}
  Story=new FatherStoryState();Logbook=new CampaignLogbook();StoryMonologue="";LogbookOpen=GoalComparison=false;
  return true;
 }
 bool PreserveRejectedFiles(){
  try{
   bool primary=File.Exists(SavePath),backup=File.Exists(SavePath+".bak");
   if(!primary&&!backup)throw new IOException("보존할 기존 저장 파일을 찾지 못했습니다");
   string archive=SavePath+".recovery-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N");
   Directory.CreateDirectory(archive);
   if(primary)File.Copy(SavePath,Path.Combine(archive,"primary.json"),false);
   if(backup)File.Copy(SavePath+".bak",Path.Combine(archive,"backup.json"),false);
   RecoveryArchivePath=archive;return true;
  }catch(Exception ex){SaveFailed=true;saveStatus="원본 별도 보관 실패 · 기존 저장 유지 · 다시 시도해 주세요";Debug.LogWarning("Campaign recovery archive failed: "+ex.Message);return false;}
 }
 void PrepareDay(){InitializeDailyCondition();Cabin=new CabinControlsState();session.GetComponent<ExitQueueSituation>().Stop();session.GetComponent<RightTurnSituation>().Stop();taxPrompt=false;if(session.law)session.law.ResetObservation();session.jobs.ClearForDay();if(session.roadConditions)session.roadConditions.ResetForDay();if(session.surprises)session.surprises.ResetForDay();session.taxi.ResetTaxi();session.manners.BeginRide();session.paused=false;notice="";preparing=useV2&&(!testMode||storyTest||ShiftDevEndProbe.DailyCheck||ShiftDevEndProbe.DailyReload||PassengerStoryProbe.Enabled);Time.timeScale=preparing?0:1;if(preparing)StartCoroutine(PlaceAtParking());}
 public void Depart(){if(resumeBlocked){notice=saveStatus;RequestRestart();return;}if(session.FullMapOpen||restartPrompt||RoomOpen||pendingPayment>=0||session.GetComponent<PracticeSession>().Busy)return;if(NeedsFirstMemo){PersonalRoomHUD.OpenPreparationMemo(this);return;}if(StoryEnabled&&preparing&&!Story.introSeen){if(!IntroInputBlocked)BeginFirstIntro();return;}if(preparing&&session.services.HasHomeBase&&!(session.damage&&session.damage.Disabled))ParkAtShiftStart();preparing=false;Time.timeScale=1;ObserveDailyConditionDeparture();if(CabinEnabled&&ledger.Day==4&&DrivingSettings.Current.cabinAssist)EndDayNotice("오늘부터 미터기는 직접 켭니다 · 손님이 타면 J, 내리면 다시 J");}
 public void ConfirmTaxClose(bool pay){taxPrompt=false;}
 void RecoverFuel(){
  string id=session.law.ids.Next("fuel-recovery");if(!ledger.Account.RecoverFuel(id,ledger.Day))return;
  bool occupied=session.jobs.trip.State==TripState.OnTrip;
  if(occupied){session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;session.jobs.trip.Tick(30);if(session.jobs.DrunkRide!=null)session.jobs.DrunkRide.Advance(30,0);session.jobs.RecordExpiredTrip();}
  else {session.jobs.ClearForDay();session.taxi.ResetTaxi();session.law.ResetObservation();session.jobs.ResetOffers();}
  ledger.Advance(30,0);notice="연료 고갈 · "+(occupied?"현재 위치에 20% 공급 · 승객 운송 계속":"출발지 복귀 후 20% 공급")+" + 지원비 1,800원 / 30초 경과";noticeUntil=Time.unscaledTime+8;
 }
 public bool SaveCheckpoint(){if(session.accident&&session.accident.Atomic)return true;if(PvpNativeLan.IsReplica(session))return true;if(SimulationSession){if(session.simulationMatch)session.simulationMatch.ObserveServiceTime(session);return true;}if(resumeBlocked||restoring){SaveFailed=true;return false;}if(RoadMapRuntime.Trial)return true;ledger.RefreshSummary();return WriteCheckpoint(ledger);}
 bool WriteCheckpoint(EconomyLedger value,PrototypeIds resetIds=null){if(SimulationSession)return true;if(RoadMapRuntime.Trial)return true;
  try{
   Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
   var archive=new System.Collections.Generic.List<CompletedDay>(completedDays);
   if(ledger.OneDay&&ledger.State==BusinessState.Completed&&session.law&&!archive.Exists(r=>r.id==session.law.ids.campaign))archive.Add(new CompletedDay{id=session.law.ids.campaign,configuration=ComparisonVersion,result=ledger.Last.Copy(),days=ledger.Settings.days,totalIncome=ledger.TotalIncome,totalExpenses=ledger.TotalExpenses,totalNet=ledger.TotalNet,fuel=ledger.Account.Fuel,cash=ledger.Account.Cash,debt=ledger.Account.Debt,score=session.law.ledger.TotalScore});
   if(ledger.OneDay&&ledger.State==BusinessState.Completed&&session.law&&ledger.Account!=null){
    int ri=archive.FindIndex(r=>r.id==session.law.ids.campaign);
    if(ri>=0){var old=archive[ri];archive[ri]=new CompletedDay{id=old.id,configuration=old.configuration,result=old.result,fuel=old.fuel,cash=ledger.Account.Cash,debt=ledger.Account.Debt,score=old.score,days=old.days,totalIncome=old.totalIncome,totalExpenses=old.totalExpenses,totalNet=old.totalNet,room=JsonUtility.FromJson<PersonalRoomState>(JsonUtility.ToJson(ledger.Account.Room))};}
   }
   // Version 3 saves the same live boundary for time, account and passenger state.
   bool current=value==ledger||resetIds!=null&&session.law&&resetIds.campaign==session.law.ids.campaign;
   var boundaryDays=value.Snapshot();var boundaryAccount=value.Account==null?null:value.Account.Snapshot();
   var keptPassengers=current?passengers.ToArray():new PassengerResult[0];
   var configuration=current?ComparisonVersion:(settings.days>1?PrototypeSettings.ThreeDayVersion:PrototypeSettings.OneDayVersion)+"-following-"+(session.traffic.mitOptions.Enabled?"on":"off");
   var identity=current&&session.law?session.law.ids:resetIds??new PrototypeIds(Guid.NewGuid().ToString("N"));
   int start=dailyEnabled?(current?dailyFirstDay:1):0;
   int condition=start>0&&value.Day>=start?(int)DailyConditions.ForDay(identity.campaign,value.Day-start+1):0;
   var data=value.Account==null?JsonUtility.ToJson(value.Snapshot(),true):JsonUtility.ToJson(new CampaignCheckpoint{contentProfileJson=ContentTuningRuntime.Snapshot(),hasLive=value==ledger&&!preparing&&value.Operating&&session.requests,live=value==ledger&&!preparing&&value.Operating&&session.requests?CaptureLive():null,comparisonVersion=configuration,days=boundaryDays,account=boundaryAccount,story=SupportsCareer?(current?Story:new FatherStoryState()):null,logbook=SupportsCareer?(current?Logbook:new CampaignLogbook()):null,conditionFirstDay=start,conditionKind=condition,completedDays=archive.ToArray(),passengers=keptPassengers,practices=current?practices.ToArray():new PracticeResult[0],law=current&&session.law?session.law.ledger.Events:new LawEvent[0],ids=identity},true);
   string backupNote=CampaignCheckpointWriter.Write(SavePath,data);
   if(selectAfterWrite){CampaignSaveLocation.SelectCareer(SavePath);selectAfterWrite=false;}
   completedDays.Clear();completedDays.AddRange(archive);SaveFailed=false;saveStatus="저장 완료 · 중단한 운송·시간·장부를 함께 이어갑니다"+(backupNote.Length>0?" · "+backupNote:"");return true;
  }catch(Exception ex){SaveFailed=true;saveStatus="저장 실패 · 저장 재시도 버튼을 눌러주세요";Debug.LogWarning("Campaign save failed: "+ex.Message);return false;}
 }
}
