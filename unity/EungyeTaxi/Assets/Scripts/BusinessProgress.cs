using System;
using System.IO;
using UnityEngine;

[Serializable] public class CompletedDay {public string id,configuration;public DayRecord result;public float fuel;public int cash,debt,score;public int days=1,totalIncome,totalExpenses,totalNet;}
[Serializable] public class CampaignCheckpoint { public int version=3; public bool hasLive;public LiveCampaignCheckpoint live; public string comparisonVersion; public EconomySnapshot days; public AccountSnapshot account; public LawEvent[] law; public PrototypeIds ids; public PassengerResult[] passengers; public PracticeResult[] practices; public CompletedDay[] completedDays; }

[Serializable] public class LiveCampaignCheckpoint {public string mapRevision,tripId;public Vector3 position;public Quaternion rotation;public PassengerJobsSnapshot jobs;public AmbientPedestrianSnapshot ambientPedestrians;public StopOccupancySnapshot occupancy;public bool preparing,preferFastest,hasDestination;public Vector3 destination;public double realSeconds;}

public class BusinessProgress : MonoBehaviour {
 public DriveSession session;
 public EconomyLedger ledger;
 public readonly System.Collections.Generic.List<PassengerResult> passengers=new System.Collections.Generic.List<PassengerResult>();
 public readonly System.Collections.Generic.List<PracticeResult> practices=new System.Collections.Generic.List<PracticeResult>();
 public readonly System.Collections.Generic.List<CompletedDay> completedDays=new System.Collections.Generic.List<CompletedDay>();
 double realSeconds;bool initialFollowing,mixedFollowing;
 public bool DrowsinessRunning {get{return enabledCampaign&&ledger!=null&&ledger.Operating&&ledger.Driver!=null&&!preparing&&!restoring&&!resumeBlocked&&!PracticeSession.Enabled&&!session.PracticeBusy&&!session.PracticeMenuOpen&&!session.ChoiceOpen&&!session.FullMapOpen&&!session.paused&&!session.services.open&&!session.law.HistoryOpen&&!session.RideResultOpen&&Time.timeScale>0;}}
 public bool DrowsinessEligible {get{return DrowsinessRunning&&session.taxi.body.velocity.magnitude>=.56f;}}
 public bool DrowsyControlsLost {get{return DrowsinessRunning&&ledger.Driver.drowsiness!=null&&ledger.Driver.drowsiness.ControlsLost;}}
 public string ComparisonVersion {get{if(loaded!=null&&ShowingSummary&&loaded.completedDays!=null)foreach(var day in loaded.completedDays)if(day.id==loaded.ids.campaign)return day.configuration;return (settings.days>1?PrototypeSettings.ThreeDayVersion:PrototypeSettings.OneDayVersion)+"-following-"+(mixedFollowing?"mixed":initialFollowing?"on":"off");}}
 public bool enabledCampaign=true;
 public string notice="",saveStatus="하루 정산 시 자동 저장됩니다";
 public string SavePath {get;private set;}
 bool testMode;float noticeUntil; CampaignCheckpoint loaded; public bool useV2; public bool taxPrompt,preparing; public PrototypeSettings settings=new PrototypeSettings();
 public bool SaveFailed {get;private set;}public bool ResumeBlocked {get{return resumeBlocked;}}
 public int pendingPayment=-1; public bool restartPrompt;
 EconomySnapshot startDays;AccountSnapshot startAccount;
 void CaptureDayStart(){startDays=ledger.Snapshot();startAccount=ledger.Account==null?null:ledger.Account.Snapshot();}
 bool CanRestartNow {get{return !RoadMapRuntime.Trial&&enabledCampaign&&useV2&&!session.ChoiceOpen&&!session.FullMapOpen&&!session.PracticeMenuOpen&&!PracticeSession.Enabled&&!session.GetComponent<PracticeSession>().Busy&&pendingPayment<0&&!session.services.open&&!session.law.HistoryOpen&&!taxPrompt&&(preparing||ShowingSummary||session.paused);}}
 public bool RequestRestart(){if(restartPrompt||!CanRestartNow)return false;restartPrompt=true;return true;}
 public bool ConfirmRestart(bool accept){if(!restartPrompt)return false;if(!accept){restartPrompt=false;return false;}if(!CanRestartNow){restartPrompt=false;return false;}return ResetCampaign(true);}
 // Retained for older callers; excluded features never open a payment modal.
 public void RequestPayment(int kind){pendingPayment=-1;}
 public bool ConfirmPayment(bool accept){pendingPayment=-1;return false;}

 public bool ShowingSummary {get{return enabledCampaign&&ledger!=null&&ledger.ShowingSummary;}}
 public static CampaignCheckpoint ParseCheckpoint(string json){
  var checkpoint=JsonUtility.FromJson<CampaignCheckpoint>(json);
  // JsonUtility creates a nonnull class with actors=null for both an absent
  // legacy field and explicit JSON null. Normalize only those wire formats;
  // an actual object (including malformed/empty actors) must still validate.
  const string field=@"(?<!\\)""ambientPedestrians""\s*:\s*";
  if(checkpoint!=null&&checkpoint.live!=null&&(!System.Text.RegularExpressions.Regex.IsMatch(json,field)||System.Text.RegularExpressions.Regex.IsMatch(json,field+@"null(?=\s*[,}])")))checkpoint.live.ambientPedestrians=null;
  return checkpoint;
 }
 public bool CanAccept {get{return !resumeBlocked&&!restoring&&!PracticeSession.Enabled&&!preparing&&(!enabledCampaign||ledger.CanAccept);}}
 public void Initialize(DriveSession owner){
  session=owner;owner.taxi.business=this;gameObject.AddComponent<DrowsinessHUD>().business=this;initialFollowing=owner.traffic.mitOptions.Enabled;var args=Environment.GetCommandLineArgs();
  foreach(var arg in args)if(arg.EndsWith("Test")){testMode=true;if(arg!="-economyTest"&&arg!="-hybridTest"&&arg!="-hybridQuickTest")enabledCampaign=false;}
  if(Array.IndexOf(args,"-assistedTrial")>=0||RoadRulesTrial.Enabled||HdRouteTrial.Enabled){testMode=true;enabledCampaign=false;}
  bool ambientTest=Array.IndexOf(args,"-ambientPedestrianTest")>=0||Array.IndexOf(args,"-ambientPedestrianReloadTest")>=0;if(ambientTest){testMode=true;enabledCampaign=true;}
  bool startupTest=Array.IndexOf(args,"-campaignStartupTest")>=0;
  if(PracticeSession.Enabled||RoadMapRuntime.Trial){testMode=true;enabledCampaign=false;}
  if(!testMode){settings=PrototypeSettings.ForThreeDays();}
  if(Array.IndexOf(args,"-oneDayTest")>=0||Array.IndexOf(args,"-oneDayReloadTest")>=0||Array.IndexOf(args,"-oneDayMode")>=0){settings=PrototypeSettings.ForOneDay();if(testMode)enabledCampaign=true;}
  if(Array.IndexOf(args,"-threeDayTest")>=0||Array.IndexOf(args,"-threeDayReloadTest")>=0){settings=PrototypeSettings.ForThreeDays();enabledCampaign=true;}
  useV2=Array.IndexOf(args,"-serviceLawZonesTest")>=0||PracticeSession.Enabled||!testMode||(Array.IndexOf(args,"-campaignTest")>=0||Array.IndexOf(args,"-completionTest")>=0||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-campaignDriveTest")>=0)||Array.IndexOf(args,"-scenarioTest")>=0||Array.IndexOf(args,"-contentFlowTest")>=0||Array.IndexOf(args,"-exitTransportTest")>=0||Array.IndexOf(args,"-hybridV2")>=0;
  if((Array.IndexOf(args,"-serviceLawZonesTest")>=0||Array.IndexOf(args,"-campaignTest")>=0||Array.IndexOf(args,"-completionTest")>=0||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-campaignDriveTest")>=0))enabledCampaign=true;
  if(RoadMapRuntime.Trial){settings=PrototypeSettings.ForOneDay();useV2=true;}useV2|=settings.oneDay;if(Array.IndexOf(args,"-liveSaveTest")>=0||(Array.IndexOf(args,"-liveReloadTest")>=0||Array.IndexOf(args,"-liveInvalidTest")>=0)){settings=PrototypeSettings.ForThreeDays();useV2=true;enabledCampaign=true;}if(Array.IndexOf(args,"-passengerCareTest")>=0){useV2=true;enabledCampaign=true;}
  SavePath=testMode?Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts",useV2?"campaign-v2-test.json":"economy-player-checkpoint.json")):Path.Combine(Path.GetDirectoryName(Application.persistentDataPath),"은계 택시 시뮬레이터",PrototypeSettings.SaveFile);
  if(settings.oneDay)SavePath=Path.Combine(Path.GetDirectoryName(SavePath),settings.days>1?(testMode?"three-day-test.json":PrototypeSettings.ThreeDaySaveFile):(testMode?"one-day-test.json":"campaign-day20-v1.json"));
  if(Array.IndexOf(args,"-liveSaveTest")>=0||(Array.IndexOf(args,"-liveReloadTest")>=0||Array.IndexOf(args,"-liveInvalidTest")>=0))SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/live-campaign-test.json"));
  if(Array.IndexOf(args,"-passengerCareTest")>=0)SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/passenger-care-test.json"));
  if(Array.IndexOf(args,"-liveInvalidTest")>=0)SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/live-invalid-test.json"));
  if(ambientTest){settings=PrototypeSettings.ForOneDay();useV2=true;enabledCampaign=true;SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/ambient-pedestrian-test.json"));}
  if(startupTest){settings=PrototypeSettings.ForThreeDays();useV2=true;enabledCampaign=true;SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/campaign-startup-test.json"));}
  if(!testMode)try{SavePath=CampaignSaveLocation.Resolve(Path.GetDirectoryName(Application.persistentDataPath),Path.GetFileName(SavePath));}catch(Exception e){Debug.LogWarning("이전 캠페인 저장 복사 실패: "+e.Message);}
  ledger=useV2?new EconomyLedger(settings):new EconomyLedger(new EconomySettings());
  if(!testMode||(Array.IndexOf(args,"-liveReloadTest")>=0||Array.IndexOf(args,"-liveInvalidTest")>=0)||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-oneDayReloadTest")>=0||Array.IndexOf(args,"-threeDayReloadTest")>=0||Array.IndexOf(args,"-ambientPedestrianReloadTest")>=0){
   foreach(var path in new[]{SavePath,SavePath+".bak"}){
    if(!File.Exists(path))continue;
    try{var candidate=ParseCheckpoint(File.ReadAllText(path));if(candidate==null||(candidate.version!=2&&candidate.version!=3)||candidate.law==null||candidate.ids==null||string.IsNullOrEmpty(candidate.ids.campaign)||candidate.ids.sequence<0)throw new Exception("Invalid v2 checkpoint");if(settings.oneDay&&(candidate.account==null||candidate.account.settings==null||!candidate.account.settings.oneDay||candidate.account.settings.days!=settings.days))throw new Exception("Different campaign mode");if(!candidate.hasLive)candidate.live=null;if(candidate.version==3&&candidate.days!=null&&candidate.days.hasActive&&candidate.days.active!=null&&candidate.days.active.elapsed>0&&candidate.live==null)throw new Exception("Missing active session");ValidateLivePayload(candidate.live,owner);var restored=EconomyLedger.RestoreCampaign(candidate.days,candidate.account);var checkedLaw=new LawLedger();foreach(var e in candidate.law)checkedLaw.Record(e);ledger=restored;loaded=candidate;saveStatus=path==SavePath?"저장된 정산 결과를 불러왔습니다":"백업에서 정산 결과를 복구했습니다";break;}
    catch(Exception ex){saveStatus="저장 읽기 실패 · 새 영업을 시작합니다";Debug.LogWarning("Campaign load failed: "+ex.Message);}
   }
  }
  if(loaded==null&&(File.Exists(SavePath)||File.Exists(SavePath+".bak"))&&(!testMode||(Array.IndexOf(args,"-liveReloadTest")>=0||Array.IndexOf(args,"-liveInvalidTest")>=0))){resumeBlocked=true;SaveFailed=true;saveStatus="저장 복원 실패 · 기존 파일 보존 중 · 지도/저장 확인 또는 새 캠페인 선택";}
  if(loaded!=null){if(loaded.passengers!=null)passengers.AddRange(loaded.passengers);if(loaded.practices!=null)practices.AddRange(loaded.practices);if(loaded.completedDays!=null)completedDays.AddRange(loaded.completedDays);}
  if(loaded!=null&&!string.IsNullOrEmpty(loaded.comparisonVersion)){mixedFollowing=loaded.comparisonVersion.EndsWith("mixed");initialFollowing=loaded.comparisonVersion.EndsWith("on");mixedFollowing|=initialFollowing!=owner.traffic.mitOptions.Enabled;}
  CaptureDayStart();
  if(resumeBlocked){preparing=true;Freeze();}
  else if(loaded!=null&&loaded.live!=null)Freeze();
  else if(ShowingSummary)Freeze();
  else if(useV2&&(!testMode||startupTest)){preparing=loaded==null||loaded.live==null||loaded.live.preparing;Freeze();}
 }
 void Start(){if(loaded!=null&&session.law){session.law.ids=loaded.ids;foreach(var e in loaded.law)session.law.ledger.Record(e);}if(loaded!=null&&loaded.live!=null){restoring=true;StartCoroutine(RestoreAfterInitialization(loaded.live));}else if(preparing)StartCoroutine(PlaceAtParking());}
 bool resumeBlocked,restoring;
 System.Collections.IEnumerator RestoreAfterInitialization(LiveCampaignCheckpoint snapshot){yield return null;RestoreLive(snapshot);restoring=false;}
 void RestoreLive(LiveCampaignCheckpoint snapshot){
  try{if(snapshot.jobs==null||!Valid(snapshot.position.x)||!Valid(snapshot.position.y)||!Valid(snapshot.position.z)||snapshot.position.x<0||snapshot.position.z<0||snapshot.position.x>session.world.map.width||snapshot.position.z>session.world.map.depth)throw new Exception("Invalid live vehicle position");
   if(snapshot.mapRevision!=session.world.map.editorRevision)throw new Exception("Map revision changed; cannot restore active trip on edited geometry");
   if(session.ambientPedestrians)session.ambientPedestrians.Restore(snapshot.ambientPedestrians);
   session.taxi.SetSpawn(snapshot.position,snapshot.rotation);session.taxi.ResetTaxi();var district=session.traffic.GetComponent<DistrictTraffic>();if(district)district.ClearPlayerOverlap();session.law.tripId=snapshot.tripId??"";session.law.ResetObservation();session.jobs.RestoreLive(snapshot.jobs);var occupancy=session.GetComponent<StopOccupancySystem>();if(occupancy)occupancy.Restore(snapshot.occupancy);if(session.ambientPedestrians)session.ambientPedestrians.ReconcileVisibility();realSeconds=snapshot.realSeconds;preparing=snapshot.preparing;
   if(!session.jobs.Active&&snapshot.hasDestination)session.jobs.nav.SetDestination(snapshot.destination);
   session.jobs.nav.PreferFastest=snapshot.preferFastest;session.assisted.SetActive(false);session.paused=!preparing;Time.timeScale=0;saveStatus="중단한 위치·운송·시간·장부 복원 완료 · 계속 운전";resumeBlocked=false;
  }catch(Exception ex){preparing=false;session.paused=true;Time.timeScale=0;saveStatus="운송 복원 중단 · 지도/저장 상태 확인 필요: "+ex.Message;Debug.LogWarning(saveStatus);resumeBlocked=true;}
 }
 static void ValidateLivePayload(LiveCampaignCheckpoint live,DriveSession owner){
  if(live==null)return;StopOccupancyPolicy.Restore(live.occupancy);
  PassengerJobs.NormalizePresence(live.jobs);
  new FareTrip().Restore(live.jobs==null?null:live.jobs.trip);
  if(live.mapRevision!=owner.world.map.editorRevision||live.jobs==null||live.jobs.trip==null||live.jobs.offers==null||live.jobs.offers.Length!=3||live.jobs.cooldown==null||live.jobs.cooldown.Length!=(live.jobs.stopIds!=null?live.jobs.stopIds.Length:owner.jobs.stops.Length)||double.IsNaN(live.realSeconds)||double.IsInfinity(live.realSeconds)||live.realSeconds<0)throw new Exception("Invalid live checkpoint/map revision");
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
 static void ValidateOffer(JobOffer o,int count){if(o!=null&&(o.pickup<0||o.pickup>=count||o.drop<0||o.drop>=count||o.fare<0))throw new Exception("Invalid live stop reference");}
 static bool Valid(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
 LiveCampaignCheckpoint CaptureLive(){return new LiveCampaignCheckpoint{occupancy=session.GetComponent<StopOccupancySystem>()?session.GetComponent<StopOccupancySystem>().Capture():null,mapRevision=session.world.map.editorRevision,tripId=session.law.tripId,position=session.taxi.transform.position,rotation=session.taxi.transform.rotation,jobs=session.jobs.CaptureLive(),ambientPedestrians=session.ambientPedestrians?session.ambientPedestrians.Capture():null,preparing=preparing,preferFastest=session.jobs.nav.PreferFastest,hasDestination=session.jobs.nav.HasDestination,destination=session.jobs.nav.destination,realSeconds=realSeconds};}
 float nextSave;int lastLawCount=-1;
 void OnApplicationQuit(){if(!testMode&&enabledCampaign&&session&&session.requests)SaveCheckpoint();}
 void OnApplicationPause(bool value){if(value&&!testMode&&enabledCampaign&&session&&session.requests)SaveCheckpoint();}
 public System.Collections.IEnumerator PlaceAtParking(){yield return null;var stop=session.services.ParkingStop;session.taxi.SetSpawn(stop.Kerb+Vector3.up*.65f,Quaternion.LookRotation(stop.Direction));session.taxi.ResetTaxi();session.law.ResetObservation();session.jobs.ResetOffers();}

 void FixedUpdate(){
  if(!enabledCampaign||!ledger.Operating||preparing||restoring||resumeBlocked||session.paused||Time.timeScale<=0)return;
  var v=session.taxi.body.velocity;double metres=Math.Sqrt(v.x*v.x+v.z*v.z)*Time.fixedDeltaTime;
  if(!session.PracticeBusy&&!PracticeSession.Enabled)ledger.AdvanceOperation(Time.fixedDeltaTime,metres,session.jobs.trip.State==TripState.OnTrip);else ledger.Advance(Time.fixedDeltaTime,metres);
  if(ledger.Driver!=null)ledger.Driver.drowsiness.Advance(Time.fixedDeltaTime,DrowsinessRunning,ledger.Driver.fatigue,session.taxi.body.velocity.magnitude>=.56f);
  if(ledger.Account!=null&&ledger.Account.Fuel<=0)RecoverFuel();
 }
 void Update(){if(enabledCampaign&&!preparing&&!session.paused&&ledger.Operating){realSeconds+=Time.unscaledDeltaTime;mixedFollowing|=session.traffic.mitOptions.Enabled!=initialFollowing;}if(enabledCampaign&&Input.GetKeyDown(KeyCode.N))RequestEndDay();if(noticeUntil>0&&Time.unscaledTime>noticeUntil){notice="";noticeUntil=0;}}
 void LateUpdate(){
  if(!restoring&&!testMode&&enabledCampaign&&session.law&&session.requests&&(Time.unscaledTime>=nextSave||session.law.ledger.Count!=lastLawCount)){nextSave=Time.unscaledTime+5;lastLawCount=session.law.ledger.Count;SaveCheckpoint();}
  if(enabledCampaign&&ledger.OneDay&&ledger.Operating&&ledger.Driver.fatigue>=100){notice="피로 100 · 졸음 위험 · 계속 영업 가능 · F7 휴식/주차장 수면·영업 종료";noticeUntil=Time.unscaledTime+2;}
  if(session.FullMapOpen||!enabledCampaign||ledger.State!=BusinessState.Closing)return;
  if(session.jobs.trip.State==TripState.OnTrip){notice="영업 종료 대기 · 현재 승객 운송 후 정산합니다";return;}
  session.jobs.RecordExpiredTrip();
  if(ledger.Settle()){ledger.Last.realSeconds=realSeconds;taxPrompt=false;session.jobs.ClearForDay();session.services.open=false;session.services.pending=-1;Freeze();SaveCheckpoint();}
 }
 void Freeze(){session.paused=false;Time.timeScale=0;}
 public bool RequestEndDay(){
  if(session.FullMapOpen||!enabledCampaign||ledger.State!=BusinessState.Open||session.paused||Time.timeScale==0)return false;
  if(session.jobs.trip.State==TripState.OnTrip){notice="승객 탑승 중에는 영업을 마칠 수 없습니다 · 목적지 하차 후 N";noticeUntil=Time.unscaledTime+4;return false;}
  if(session.taxi.body.velocity.magnitude>=.56f){notice="안전한 곳에 정차한 뒤 N으로 영업을 마치세요";noticeUntil=Time.unscaledTime+4;return false;}
  if(ledger.OneDay&&!session.services.AtEndDayHub()){notice="주차장·휴식 거점에서 쉬며 영업을 마치세요 · F7 길 안내";noticeUntil=Time.unscaledTime+4;return false;}
  if(ledger.OneDay&&!ledger.UseSleep(session.law.ids.Next("end-rest")))return false;ledger.RequestClose("생활 거점 휴식·영업 종료");return true;
 }
 public void RecordPassenger(int paid,string outcome){
  if(!useV2||PracticeSession.Enabled||session.jobs.current==null||string.IsNullOrEmpty(session.law.tripId))return;
  foreach(var old in passengers)if(old.tripId==session.law.tripId)return;
  var jobs=session.jobs;var offer=jobs.current;var reward=jobs.RequestReward??PassengerReward.Calculate(RequestKind.Quiet,session.manners.ledger.Score,offer.requiredScore,offer.fare,paid,0,paid>0,true);
  passengers.Add(new PassengerResult{boardingDrivingScore=reward.boardingDrivingScore,drivingAdjustedFare=reward.drivingAdjustedFare,comfortDelta=reward.comfortDelta,initialFare=jobs.InitialFare,farePolicyVersion=reward.farePolicyVersion,hasDrivingScore=true,drivingScore=session.law.ledger.TotalScore,tripId=session.law.tripId,offerId=jobs.OfferId,day=ledger.Day,pickupStopId=offer.pickupStopId,dropStopId=offer.dropStopId,mapRevision=offer.mapRevision,catalogRevision=offer.catalogRevision,stopEvidence=offer.stopEvidence,pickup=jobs.stops[offer.pickup].name,destination=jobs.stops[offer.drop].name,outcome=outcome,quoted=offer.fare,paid=paid,baseFare=reward.baseFare,bonus=reward.bonus,satisfaction=reward.satisfaction,satisfactionDelta=reward.satisfactionDelta,excessScore=reward.excessScore,bonusReason=reward.bonusReason,startSatisfaction=reward.startSatisfaction,startDeficit=reward.startDeficit,cleanBonus=reward.cleanBonus,driveDelta=reward.driveDelta,timeDelta=reward.timeDelta,referenceSeconds=reward.referenceSeconds,timeReason=reward.timeReason,cleanlinessAfter=ledger.Care.Cleanliness,drunk=jobs.DrunkRide!=null&&jobs.DrunkRide.IsDrunk,vomited=jobs.DrunkRide!=null&&jobs.DrunkRide.Vomited,wakeDelayed=jobs.DrunkRide!=null&&jobs.DrunkRide.SleepPlanned,arrivalElapsed=jobs.DrunkRide==null?0:jobs.DrunkRide.ArrivalElapsed,disembarkElapsed=jobs.DrunkRide==null?0:jobs.DrunkRide.DisembarkElapsed,score=reward.sourceManner,required=offer.requiredScore,elapsed=Mathf.Max(0,offer.seconds-jobs.trip.Remaining),request=session.requests.Summary,mannerReason=session.manners.RideReasons});
 }
 public void RecordFare(int amount){if(!enabledCampaign)return;if(ledger.Account!=null)ledger.RecordFare(session.law.tripId,amount);else ledger.RecordFare(amount);}
 public void RecordFailure(){if(enabledCampaign)ledger.RecordFailure();}
 public bool NextDay(){
  if(session.FullMapOpen||restartPrompt||pendingPayment>=0||!enabledCampaign||ledger.State!=BusinessState.Summary||session.GetComponent<PracticeSession>().Busy)return false;
  if(!SaveCheckpoint())return false;
  if(ledger.Account==null){if(!ledger.NextDay())return false;realSeconds=0;PrepareDay();return true;}
  var previous=ledger;var next=EconomyLedger.RestoreCampaign(ledger.Snapshot(),ledger.Account.Snapshot());if(!next.NextDay())return false;
  if(!WriteCheckpoint(next,session.law.ids))return false;ledger=next;CaptureDayStart();
  realSeconds=0;PrepareDay();return true;
 }
 public bool BeginNextCampaignDay(){return NextDay();}
 public void RestartCampaign(){
  if(session.GetComponent<PracticeSession>().Busy)return;
  if(ledger.State!=BusinessState.Won&&ledger.State!=BusinessState.Lost)return;
  ResetCampaign(false);
 }
 bool ResetCampaign(bool showPreparation){
  var next=useV2?new EconomyLedger(settings):new EconomyLedger(new EconomySettings());
  var nextIds=new PrototypeIds(Guid.NewGuid().ToString("N"));
  if(!WriteCheckpoint(next,nextIds))return false;
  ledger=next;CaptureDayStart();loaded=null;resumeBlocked=false;realSeconds=0;initialFollowing=session.traffic.mitOptions.Enabled;mixedFollowing=false;passengers.Clear();practices.Clear();pendingPayment=-1;restartPrompt=false;noticeUntil=0;
  if(session.law){session.law.ledger=new LawLedger();session.law.ids=nextIds;}
  session.jobs.balance=session.jobs.completed=session.jobs.failed=0;session.services.notice=session.services.destination="";session.services.pending=-1;
  session.GetComponent<PracticeSession>().notice="";session.visits=0;PrepareDay();
  session.jobs.ResetOffers();
  if(showPreparation&&!preparing){preparing=true;Time.timeScale=0;StartCoroutine(PlaceAtParking());}
  return true;
 }
 void PrepareDay(){session.GetComponent<ExitQueueSituation>().Stop();session.GetComponent<RightTurnSituation>().Stop();taxPrompt=false;if(session.law)session.law.ResetObservation();session.jobs.ClearForDay();if(session.roadConditions)session.roadConditions.ResetForDay();session.taxi.ResetTaxi();session.manners.BeginRide();session.paused=false;notice="";preparing=useV2&&!testMode;Time.timeScale=preparing?0:1;if(preparing)StartCoroutine(PlaceAtParking());}
 public void Depart(){if(resumeBlocked){notice=saveStatus;return;}if(session.FullMapOpen||restartPrompt||pendingPayment>=0||session.GetComponent<PracticeSession>().Busy)return;preparing=false;Time.timeScale=1;}
 public void ConfirmTaxClose(bool pay){taxPrompt=false;}
 void RecoverFuel(){
  string id=session.law.ids.Next("fuel-recovery");if(!ledger.Account.RecoverFuel(id,ledger.Day))return;
  bool occupied=session.jobs.trip.State==TripState.OnTrip;
  if(occupied){session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;session.jobs.trip.Tick(30);if(session.jobs.DrunkRide!=null)session.jobs.DrunkRide.Advance(30,0);session.jobs.RecordExpiredTrip();}
  else {session.jobs.ClearForDay();session.taxi.ResetTaxi();session.law.ResetObservation();session.jobs.ResetOffers();}
  ledger.Advance(30,0);notice="연료 고갈 · "+(occupied?"현재 위치에 20% 공급 · 승객 운송 계속":"출발지 복귀 후 20% 공급")+" + 지원비 1,800원 / 30초 경과";noticeUntil=Time.unscaledTime+8;
 }
 public bool SaveCheckpoint(){if(resumeBlocked||restoring){SaveFailed=true;return false;}if(RoadMapRuntime.Trial)return true;ledger.RefreshSummary();return WriteCheckpoint(ledger);}
 bool WriteCheckpoint(EconomyLedger value,PrototypeIds resetIds=null){if(RoadMapRuntime.Trial)return true;
  try{
   Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
   var archive=new System.Collections.Generic.List<CompletedDay>(completedDays);
   if(ledger.OneDay&&ledger.State==BusinessState.Completed&&session.law&&!archive.Exists(r=>r.id==session.law.ids.campaign))archive.Add(new CompletedDay{id=session.law.ids.campaign,configuration=ComparisonVersion,result=ledger.Last.Copy(),days=ledger.Settings.days,totalIncome=ledger.TotalIncome,totalExpenses=ledger.TotalExpenses,totalNet=ledger.TotalNet,fuel=ledger.Account.Fuel,cash=ledger.Account.Cash,debt=ledger.Account.Debt,score=session.law.ledger.TotalScore});
   // Version 3 saves the same live boundary for time, account and passenger state.
   bool current=value==ledger||resetIds!=null&&session.law&&resetIds.campaign==session.law.ids.campaign;
   var boundaryDays=value.Snapshot();var boundaryAccount=value.Account==null?null:value.Account.Snapshot();
   var keptPassengers=current?passengers.ToArray():new PassengerResult[0];
   var configuration=current?ComparisonVersion:(settings.days>1?PrototypeSettings.ThreeDayVersion:PrototypeSettings.OneDayVersion)+"-following-"+(session.traffic.mitOptions.Enabled?"on":"off");
   var data=value.Account==null?JsonUtility.ToJson(value.Snapshot(),true):JsonUtility.ToJson(new CampaignCheckpoint{hasLive=value==ledger&&!preparing&&value.Operating&&session.requests,live=value==ledger&&!preparing&&value.Operating&&session.requests?CaptureLive():null,comparisonVersion=configuration,days=boundaryDays,account=boundaryAccount,completedDays=archive.ToArray(),passengers=keptPassengers,practices=current?practices.ToArray():new PracticeResult[0],law=current&&session.law?session.law.ledger.Events:new LawEvent[0],ids=current&&session.law?session.law.ids:resetIds??new PrototypeIds(Guid.NewGuid().ToString("N"))},true);
   File.WriteAllText(SavePath+".tmp",data);
   if(File.Exists(SavePath))File.Replace(SavePath+".tmp",SavePath,SavePath+".bak");else File.Move(SavePath+".tmp",SavePath);
   completedDays.Clear();completedDays.AddRange(archive);SaveFailed=false;saveStatus="저장 완료 · 중단한 운송·시간·장부를 함께 이어갑니다";return true;
  }catch(Exception ex){SaveFailed=true;saveStatus="저장 실패 · 저장 재시도 버튼을 눌러주세요";Debug.LogWarning("Campaign save failed: "+ex.Message);return false;}
 }
}
