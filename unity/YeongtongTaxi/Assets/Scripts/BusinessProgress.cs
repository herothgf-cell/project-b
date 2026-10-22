using System;
using System.IO;
using UnityEngine;

[Serializable] public class CompletedDay {public string id,configuration;public DayRecord result;public float fuel;public int cash,debt,score;}
[Serializable] public class CampaignCheckpoint { public int version=2; public EconomySnapshot days; public AccountSnapshot account; public LawEvent[] law; public PrototypeIds ids; public PassengerResult[] passengers; public PracticeResult[] practices; public CompletedDay[] completedDays; }

public class BusinessProgress : MonoBehaviour {
 public DriveSession session;
 public EconomyLedger ledger;
 public readonly System.Collections.Generic.List<PassengerResult> passengers=new System.Collections.Generic.List<PassengerResult>();
 public readonly System.Collections.Generic.List<PracticeResult> practices=new System.Collections.Generic.List<PracticeResult>();
 public readonly System.Collections.Generic.List<CompletedDay> completedDays=new System.Collections.Generic.List<CompletedDay>();
 double realSeconds;bool initialFollowing,mixedFollowing;
 public string ComparisonVersion {get{if(loaded!=null&&ShowingSummary&&loaded.completedDays!=null)foreach(var day in loaded.completedDays)if(day.id==loaded.ids.campaign)return day.configuration;return PrototypeSettings.OneDayVersion+"-following-"+(mixedFollowing?"mixed":initialFollowing?"on":"off");}}
 public bool enabledCampaign=true;
 public string notice="",saveStatus="하루 정산 시 자동 저장됩니다";
 public string SavePath {get;private set;}
 bool testMode;float noticeUntil; CampaignCheckpoint loaded; public bool useV2; public bool taxPrompt,preparing; public PrototypeSettings settings=new PrototypeSettings();
 public bool SaveFailed {get;private set;}
 public int pendingPayment=-1; public bool restartPrompt;
 bool CanRestartNow {get{return !RoadMapRuntime.Trial&&enabledCampaign&&useV2&&!session.ChoiceOpen&&!session.FullMapOpen&&!session.PracticeMenuOpen&&!PracticeSession.Enabled&&!session.GetComponent<PracticeSession>().Busy&&pendingPayment<0&&!session.services.open&&!session.law.HistoryOpen&&!taxPrompt&&(preparing||ShowingSummary||session.paused);}}
 public bool RequestRestart(){if(restartPrompt||!CanRestartNow)return false;restartPrompt=true;return true;}
 public bool ConfirmRestart(bool accept){if(!restartPrompt)return false;if(!accept){restartPrompt=false;return false;}if(!CanRestartNow){restartPrompt=false;return false;}return ResetCampaign(true);}
 // Retained for older callers; excluded features never open a payment modal.
 public void RequestPayment(int kind){pendingPayment=-1;}
 public bool ConfirmPayment(bool accept){pendingPayment=-1;return false;}

 public bool ShowingSummary {get{return enabledCampaign&&ledger!=null&&ledger.ShowingSummary;}}
 public bool CanAccept {get{return !PracticeSession.Enabled&&!preparing&&(!enabledCampaign||ledger.CanAccept);}}
 public void Initialize(DriveSession owner){
  session=owner;initialFollowing=owner.traffic.mitOptions.Enabled;var args=Environment.GetCommandLineArgs();
  foreach(var arg in args)if(arg.EndsWith("Test")){testMode=true;if(arg!="-economyTest"&&arg!="-hybridTest"&&arg!="-hybridQuickTest")enabledCampaign=false;}
  if(Array.IndexOf(args,"-assistedTrial")>=0||RoadRulesTrial.Enabled||HdRouteTrial.Enabled){testMode=true;enabledCampaign=false;}
  if(PracticeSession.Enabled||RoadMapRuntime.Trial){testMode=true;enabledCampaign=false;}
  if(!testMode||Array.IndexOf(args,"-oneDayTest")>=0||Array.IndexOf(args,"-oneDayReloadTest")>=0||Array.IndexOf(args,"-oneDayMode")>=0){settings=PrototypeSettings.ForOneDay();if(testMode)enabledCampaign=true;}
  useV2=Array.IndexOf(args,"-serviceLawZonesTest")>=0||PracticeSession.Enabled||!testMode||(Array.IndexOf(args,"-campaignTest")>=0||Array.IndexOf(args,"-completionTest")>=0||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-campaignDriveTest")>=0)||Array.IndexOf(args,"-scenarioTest")>=0||Array.IndexOf(args,"-contentFlowTest")>=0||Array.IndexOf(args,"-exitTransportTest")>=0||Array.IndexOf(args,"-hybridV2")>=0;
  if((Array.IndexOf(args,"-serviceLawZonesTest")>=0||Array.IndexOf(args,"-campaignTest")>=0||Array.IndexOf(args,"-completionTest")>=0||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-campaignDriveTest")>=0))enabledCampaign=true;
  if(RoadMapRuntime.Trial){settings=PrototypeSettings.ForOneDay();useV2=true;}useV2|=settings.oneDay;
  SavePath=testMode?Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts",useV2?"campaign-v2-test.json":"economy-player-checkpoint.json")):Path.Combine(Path.GetDirectoryName(Application.persistentDataPath),"영통 택시 시뮬레이터",PrototypeSettings.SaveFile);
  if(settings.oneDay)SavePath=Path.Combine(Path.GetDirectoryName(SavePath),testMode?"one-day-test.json":"campaign-day20-v1.json");
  ledger=useV2?new EconomyLedger(settings):new EconomyLedger(new EconomySettings());
  if(!testMode||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-oneDayReloadTest")>=0){
   foreach(var path in new[]{SavePath,SavePath+".bak"}){
    if(!File.Exists(path))continue;
    try{var candidate=JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(path));if(candidate==null||candidate.version!=2||candidate.law==null||candidate.ids==null||string.IsNullOrEmpty(candidate.ids.campaign)||candidate.ids.sequence<0)throw new Exception("Invalid v2 checkpoint");var restored=EconomyLedger.RestoreCampaign(candidate.days,candidate.account);var checkedLaw=new LawLedger();foreach(var e in candidate.law)checkedLaw.Record(e);ledger=restored;loaded=candidate;saveStatus=path==SavePath?"저장된 정산 결과를 불러왔습니다":"백업에서 정산 결과를 복구했습니다";break;}
    catch(Exception ex){saveStatus="저장 읽기 실패 · 새 영업을 시작합니다";Debug.LogWarning("Campaign load failed: "+ex.Message);}
   }
  }
  if(loaded!=null){if(loaded.passengers!=null)passengers.AddRange(loaded.passengers);if(loaded.practices!=null)practices.AddRange(loaded.practices);if(loaded.completedDays!=null)completedDays.AddRange(loaded.completedDays);}
  if(ShowingSummary)Freeze();
  else if(useV2&&!testMode){preparing=true;Freeze();}
 }
 void Start(){if(loaded!=null&&session.law){session.law.ids=loaded.ids;foreach(var e in loaded.law)session.law.ledger.Record(e);}if(preparing)StartCoroutine(PlaceAtParking());}
 public System.Collections.IEnumerator PlaceAtParking(){yield return null;var stop=session.jobs.stops[session.services.indices[2]];session.taxi.SetSpawn(stop.Kerb+Vector3.up*.65f,Quaternion.LookRotation(stop.Direction));session.taxi.ResetTaxi();session.law.ResetObservation();session.jobs.ResetOffers();}

 void FixedUpdate(){
  if(!enabledCampaign||!ledger.Operating)return;
  var v=session.taxi.body.velocity;double metres=Math.Sqrt(v.x*v.x+v.z*v.z)*Time.fixedDeltaTime;
  ledger.Advance(Time.fixedDeltaTime,metres);
  if(ledger.Account!=null&&ledger.Account.Fuel<=0)RecoverFuel();
 }
 void Update(){if(enabledCampaign&&!preparing&&!session.paused&&ledger.Operating){realSeconds+=Time.unscaledDeltaTime;mixedFollowing|=session.traffic.mitOptions.Enabled!=initialFollowing;}if(enabledCampaign&&Input.GetKeyDown(KeyCode.N))RequestEndDay();if(noticeUntil>0&&Time.unscaledTime>noticeUntil){notice="";noticeUntil=0;}}
 void LateUpdate(){
  if(enabledCampaign&&ledger.OneDay&&ledger.Operating&&ledger.Driver.fatigue>=100){notice="피로 한계 · 신규 승객 수락 불가 · F7 주차장 수면 또는 퇴근";if(session.jobs.trip.State==TripState.ToPickup){session.jobs.ClearForDay();session.jobs.message="피로 한계로 미탑승 호출 취소 · 점수 감점 없음";}}
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
  if(ledger.OneDay&&!session.jobs.At(session.services.indices[2])){notice="자발적 퇴근은 주차장에서 합니다 · F7 길 안내";noticeUntil=Time.unscaledTime+4;return false;}
  ledger.RequestClose();return true;
 }
 public void RecordPassenger(int paid,string outcome){
  if(!useV2||PracticeSession.Enabled||session.jobs.current==null||string.IsNullOrEmpty(session.law.tripId))return;
  foreach(var old in passengers)if(old.tripId==session.law.tripId)return;
  var jobs=session.jobs;var offer=jobs.current;var reward=jobs.RequestReward??PassengerReward.Calculate(RequestKind.Quiet,session.manners.ledger.Score,offer.requiredScore,offer.fare,paid,0,paid>0,true);
  passengers.Add(new PassengerResult{tripId=session.law.tripId,offerId=jobs.OfferId,day=ledger.Day,pickup=jobs.stops[offer.pickup].name,destination=jobs.stops[offer.drop].name,outcome=outcome,quoted=offer.fare,paid=paid,baseFare=reward.baseFare,bonus=reward.bonus,satisfaction=reward.satisfaction,satisfactionDelta=reward.satisfactionDelta,excessScore=reward.excessScore,bonusReason=reward.bonusReason,score=session.manners.ledger.Score,required=offer.requiredScore,elapsed=Mathf.Max(0,offer.seconds-jobs.trip.Remaining),request=session.requests.request.Resolved?session.requests.request.ResponseText():"미응답 · 운송 종료",mannerReason=session.manners.RideReasons});
 }
 public void RecordFare(int amount){if(!enabledCampaign)return;if(ledger.Account!=null)ledger.RecordFare(session.law.tripId,amount);else ledger.RecordFare(amount);}
 public void RecordFailure(){if(enabledCampaign)ledger.RecordFailure();}
 public bool NextDay(){
  if(ledger.OneDay)return false;
  if(restartPrompt||pendingPayment>=0||!enabledCampaign||ledger.State!=BusinessState.Summary||session.GetComponent<PracticeSession>().Busy)return false;
  if(!SaveCheckpoint())return false;
  ledger.NextDay();PrepareDay();return true;
 }
 public void RestartCampaign(){
  if(session.GetComponent<PracticeSession>().Busy)return;
  if(ledger.State!=BusinessState.Won&&ledger.State!=BusinessState.Lost)return;
  ResetCampaign(false);
 }
 bool ResetCampaign(bool showPreparation){
  var next=useV2?new EconomyLedger(settings):new EconomyLedger(new EconomySettings());
  var nextIds=new PrototypeIds(Guid.NewGuid().ToString("N"));
  if(!WriteCheckpoint(next,nextIds))return false;
  ledger=next;loaded=null;realSeconds=0;initialFollowing=session.traffic.mitOptions.Enabled;mixedFollowing=false;passengers.Clear();practices.Clear();pendingPayment=-1;restartPrompt=false;noticeUntil=0;
  if(session.law){session.law.ledger=new LawLedger();session.law.ids=nextIds;}
  session.jobs.balance=session.jobs.completed=session.jobs.failed=0;session.services.notice=session.services.destination="";session.services.pending=-1;
  session.GetComponent<PracticeSession>().notice="";session.visits=0;PrepareDay();
  session.jobs.ResetOffers();
  if(showPreparation&&!preparing){preparing=true;Time.timeScale=0;StartCoroutine(PlaceAtParking());}
  return true;
 }
 void PrepareDay(){session.GetComponent<ExitQueueSituation>().Stop();session.GetComponent<RightTurnSituation>().Stop();taxPrompt=false;if(session.law)session.law.ResetObservation();session.jobs.ClearForDay();if(session.roadConditions)session.roadConditions.ResetForDay();session.taxi.ResetTaxi();session.manners.BeginRide();session.paused=false;notice="";preparing=useV2&&!testMode;Time.timeScale=preparing?0:1;if(preparing)StartCoroutine(PlaceAtParking());}
 public void Depart(){if(session.FullMapOpen||restartPrompt||pendingPayment>=0||session.GetComponent<PracticeSession>().Busy)return;preparing=false;Time.timeScale=1;}
 public void ConfirmTaxClose(bool pay){taxPrompt=false;}
 void RecoverFuel(){
  string id=session.law.ids.Next("fuel-recovery");if(!ledger.Account.RecoverFuel(id,ledger.Day))return;
  bool occupied=session.jobs.trip.State==TripState.OnTrip;
  if(occupied){session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;session.jobs.trip.Tick(30);session.jobs.RecordExpiredTrip();}
  else {session.jobs.ClearForDay();session.taxi.ResetTaxi();session.law.ResetObservation();session.jobs.ResetOffers();}
  ledger.Advance(30,0);notice="연료 고갈 · "+(occupied?"현재 위치에 20% 공급 · 승객 운송 계속":"출발지 복귀 후 20% 공급")+" + 지원비 1,800원 / 30초 경과";noticeUntil=Time.unscaledTime+8;
 }
 public bool SaveCheckpoint(){if(RoadMapRuntime.Trial)return true;ledger.RefreshSummary();return WriteCheckpoint(ledger);}
 bool WriteCheckpoint(EconomyLedger value,PrototypeIds resetIds=null){if(RoadMapRuntime.Trial)return true;
  try{
   Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
   var archive=new System.Collections.Generic.List<CompletedDay>(completedDays);
   if(ledger.OneDay&&ledger.State==BusinessState.Completed&&session.law&&!archive.Exists(r=>r.id==session.law.ids.campaign))archive.Add(new CompletedDay{id=session.law.ids.campaign,configuration=ComparisonVersion,result=ledger.Last.Copy(),fuel=ledger.Account.Fuel,cash=ledger.Account.Cash,debt=ledger.Account.Debt,score=session.law.ledger.TotalScore});
   // Finance and condition both resume at the last settlement/start boundary.
   // The independent driving journal continues to persist every observed event.
   var boundary=value.OneDay&&value.Operating?new EconomyLedger(settings):value;
   var data=value.Account==null?JsonUtility.ToJson(value.Snapshot(),true):JsonUtility.ToJson(new CampaignCheckpoint{days=boundary.Snapshot(),account=boundary.Account.Snapshot(),completedDays=archive.ToArray(),passengers=value==ledger&&boundary==value?passengers.ToArray():new PassengerResult[0],practices=value==ledger?practices.ToArray():new PracticeResult[0],law=value==ledger&&session.law?session.law.ledger.Events:new LawEvent[0],ids=value==ledger&&session.law?session.law.ids:resetIds??new PrototypeIds(Guid.NewGuid().ToString("N"))},true);
   File.WriteAllText(SavePath+".tmp",data);
   if(File.Exists(SavePath))File.Replace(SavePath+".tmp",SavePath,SavePath+".bak");else File.Move(SavePath+".tmp",SavePath);
   completedDays.Clear();completedDays.AddRange(archive);SaveFailed=false;saveStatus="정산 저장 완료 · 진행 중 종료하면 하루 시작 상태로 복원됩니다";return true;
  }catch(Exception ex){SaveFailed=true;saveStatus="저장 실패 · 저장 재시도 버튼을 눌러주세요";Debug.LogWarning("Campaign save failed: "+ex.Message);return false;}
 }
}
