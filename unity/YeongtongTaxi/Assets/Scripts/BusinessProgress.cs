using System;
using System.IO;
using UnityEngine;

[Serializable] public class CampaignCheckpoint { public int version=2; public EconomySnapshot days; public AccountSnapshot account; public LawEvent[] law; public PrototypeIds ids; public PassengerResult[] passengers; public PracticeResult[] practices; }

public class BusinessProgress : MonoBehaviour {
 public DriveSession session;
 public EconomyLedger ledger;
 public readonly System.Collections.Generic.List<PassengerResult> passengers=new System.Collections.Generic.List<PassengerResult>();
 public readonly System.Collections.Generic.List<PracticeResult> practices=new System.Collections.Generic.List<PracticeResult>();
 public bool enabledCampaign=true;
 public string notice="",saveStatus="하루 정산 시 자동 저장됩니다";
 public string SavePath {get;private set;}
 bool testMode;float noticeUntil; CampaignCheckpoint loaded; public bool useV2; public bool taxPrompt,preparing; public PrototypeSettings settings=new PrototypeSettings();
 public bool SaveFailed {get;private set;}
 public int pendingPayment=-1;
 public void RequestPayment(int kind){if((preparing||ShowingSummary)&&kind>=0&&kind<=1&&!session.GetComponent<PracticeSession>().Busy)pendingPayment=kind;}
 public bool ConfirmPayment(bool accept){int kind=pendingPayment;pendingPayment=-1;if(!accept||kind<0||!(preparing||ShowingSummary))return false;bool ok=kind==0?ledger.Account.Renew(session.law.ids.Next("insurance"),ledger.Day):ledger.Account.PayTax(session.law.ids.Next("tax"),ledger.Day);notice=ok?"결제 완료":"이미 결제했거나 현금 부족";if(ok&&ShowingSummary)SaveCheckpoint();return ok;}

 public bool ShowingSummary {get{return enabledCampaign&&ledger!=null&&ledger.ShowingSummary;}}
 public bool CanAccept {get{return !PracticeSession.Enabled&&!preparing&&(!enabledCampaign||ledger.CanAccept);}}
 public void Initialize(DriveSession owner){
  session=owner;var args=Environment.GetCommandLineArgs();
  foreach(var arg in args)if(arg.EndsWith("Test")){testMode=true;if(arg!="-economyTest"&&arg!="-hybridTest"&&arg!="-hybridQuickTest")enabledCampaign=false;}
  if(Array.IndexOf(args,"-assistedTrial")>=0||RoadRulesTrial.Enabled||HdRouteTrial.Enabled){testMode=true;enabledCampaign=false;}
  if(PracticeSession.Enabled){testMode=true;enabledCampaign=false;}
  useV2=PracticeSession.Enabled||!testMode||(Array.IndexOf(args,"-campaignTest")>=0||Array.IndexOf(args,"-completionTest")>=0||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-campaignDriveTest")>=0)||Array.IndexOf(args,"-scenarioTest")>=0||Array.IndexOf(args,"-contentFlowTest")>=0||Array.IndexOf(args,"-exitTransportTest")>=0||Array.IndexOf(args,"-hybridV2")>=0;
  if((Array.IndexOf(args,"-campaignTest")>=0||Array.IndexOf(args,"-completionTest")>=0||Array.IndexOf(args,"-completionReloadTest")>=0||Array.IndexOf(args,"-campaignDriveTest")>=0))enabledCampaign=true;
  SavePath=testMode?Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts",useV2?"campaign-v2-test.json":"economy-player-checkpoint.json")):Path.Combine(Application.persistentDataPath,PrototypeSettings.SaveFile);
  ledger=useV2?new EconomyLedger(settings):new EconomyLedger(new EconomySettings());
  if(!testMode||Array.IndexOf(args,"-completionReloadTest")>=0){
   foreach(var path in new[]{SavePath,SavePath+".bak"}){
    if(!File.Exists(path))continue;
    try{var candidate=JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(path));if(candidate==null||candidate.version!=2||candidate.law==null||candidate.ids==null||string.IsNullOrEmpty(candidate.ids.campaign)||candidate.ids.sequence<0)throw new Exception("Invalid v2 checkpoint");var restored=EconomyLedger.RestoreCampaign(candidate.days,candidate.account);var checkedLaw=new LawLedger();foreach(var e in candidate.law)checkedLaw.Record(e);ledger=restored;loaded=candidate;saveStatus=path==SavePath?"저장된 정산 결과를 불러왔습니다":"백업에서 정산 결과를 복구했습니다";break;}
    catch(Exception ex){saveStatus="저장 읽기 실패 · 새 영업을 시작합니다";Debug.LogWarning("Campaign load failed: "+ex.Message);}
   }
  }
  if(loaded!=null){if(loaded.passengers!=null)passengers.AddRange(loaded.passengers);if(loaded.practices!=null)practices.AddRange(loaded.practices);}
  if(ShowingSummary)Freeze();
  else if(useV2&&!testMode){preparing=true;Freeze();}
 }
 void Start(){if(loaded!=null&&session.law){session.law.ids=loaded.ids;foreach(var e in loaded.law)session.law.ledger.Record(e);}if(preparing)StartCoroutine(PlaceAtParking());}
 System.Collections.IEnumerator PlaceAtParking(){yield return null;var stop=session.jobs.stops[session.services.indices[2]];session.taxi.SetSpawn(stop.Kerb+Vector3.up*.65f,Quaternion.LookRotation(stop.Direction));session.taxi.ResetTaxi();session.law.ResetObservation();}

 void FixedUpdate(){
  if(!enabledCampaign||!ledger.Operating)return;
  var v=session.taxi.body.velocity;double metres=Math.Sqrt(v.x*v.x+v.z*v.z)*Time.fixedDeltaTime;
  ledger.Advance(Time.fixedDeltaTime,metres);
  if(ledger.Account!=null&&ledger.Account.Fuel<=0)RecoverFuel();
 }
 void Update(){if(ledger.Account!=null&&ledger.CanAccept){if(ledger.Account.Fuel<=15)notice="연료 부족 · F7에서 주유소 길 안내를 선택하세요";else if(notice.StartsWith("연료 부족"))notice="";}if(enabledCampaign&&Input.GetKeyDown(KeyCode.N))RequestEndDay();if(noticeUntil>0&&Time.unscaledTime>noticeUntil&&ledger.State!=BusinessState.Closing){notice="";noticeUntil=0;}}
 void LateUpdate(){
  if(!enabledCampaign||ledger.State!=BusinessState.Closing)return;
  if(session.jobs.trip.State==TripState.OnTrip){notice="영업 종료 대기 · 현재 승객 운송 후 정산합니다";return;}
  session.jobs.RecordExpiredTrip();
  if(ledger.Account!=null&&ledger.Day==settings.taxDueDay&&!ledger.Account.TaxPaid&&!taxPrompt){taxPrompt=true;notice="세금 납부 기한입니다. 정산 전 납부하거나 미납으로 마감하세요";Freeze();return;}
  if(taxPrompt&&Time.timeScale==0)return;
  if(ledger.Settle()){taxPrompt=false;session.jobs.ClearForDay();Freeze();SaveCheckpoint();}
 }
 void Freeze(){session.paused=false;Time.timeScale=0;}
 public bool RequestEndDay(){
  if(!enabledCampaign||!ledger.CanAccept||session.paused||Time.timeScale==0)return false;
  if(session.taxi.body.velocity.magnitude>=.56f){notice="안전한 곳에 정차한 뒤 N으로 영업을 마치세요";noticeUntil=Time.unscaledTime+4;return false;}
  ledger.RequestClose();return true;
 }
 public void RecordPassenger(int paid,string outcome){
  if(!useV2||PracticeSession.Enabled||session.jobs.current==null||string.IsNullOrEmpty(session.law.tripId))return;
  foreach(var old in passengers)if(old.tripId==session.law.tripId)return;
  var jobs=session.jobs;var offer=jobs.current;
  passengers.Add(new PassengerResult{tripId=session.law.tripId,day=ledger.Day,pickup=jobs.stops[offer.pickup].name,destination=jobs.stops[offer.drop].name,outcome=outcome,quoted=offer.fare,paid=paid,score=session.manners.ledger.Score,required=offer.requiredScore,elapsed=Mathf.Max(0,offer.seconds-jobs.trip.Remaining),request=session.requests.request.Resolved?session.requests.request.ResponseText():"미응답 · 운송 종료",mannerReason=session.manners.RideReasons});
 }
 public void RecordFare(int amount){if(!enabledCampaign)return;if(ledger.Account!=null)ledger.RecordFare(session.law.tripId,amount);else ledger.RecordFare(amount);}
 public void RecordFailure(){if(enabledCampaign)ledger.RecordFailure();}
 public bool NextDay(){
  if(pendingPayment>=0||!enabledCampaign||ledger.State!=BusinessState.Summary||session.GetComponent<PracticeSession>().Busy)return false;
  if(!ledger.DepartureAllowed){notice="보험 만료 · 갱신 후 3일차 출발 가능 (6,000원)";return false;}
  if(!SaveCheckpoint())return false;
  ledger.NextDay();PrepareDay();return true;
 }
 public void RestartCampaign(){
  if(session.GetComponent<PracticeSession>().Busy)return;
  if(ledger.State!=BusinessState.Won&&ledger.State!=BusinessState.Lost&&!(ledger.State==BusinessState.Summary&&!ledger.DepartureAllowed))return;
  var next=useV2?new EconomyLedger(settings):new EconomyLedger(new EconomySettings());if(!WriteCheckpoint(next))return;ledger=next;passengers.Clear();practices.Clear();if(session.law){session.law.ledger=new LawLedger();session.law.ids=new PrototypeIds(Guid.NewGuid().ToString("N"));}PrepareDay();
 }
 void PrepareDay(){session.GetComponent<ExitQueueSituation>().Stop();session.GetComponent<RightTurnSituation>().Stop();taxPrompt=false;if(session.law)session.law.ResetObservation();session.jobs.ClearForDay();if(session.roadConditions)session.roadConditions.ResetForDay();session.taxi.ResetTaxi();session.manners.BeginRide();session.paused=false;notice="";preparing=useV2&&!testMode;Time.timeScale=preparing?0:1;if(preparing)StartCoroutine(PlaceAtParking());}
 public void Depart(){if(pendingPayment>=0||session.GetComponent<PracticeSession>().Busy)return;if(ledger.Account!=null&&!ledger.Account.InsuranceValid(ledger.Day)){notice="출발 전 보험 갱신이 필요합니다";return;}preparing=false;Time.timeScale=1;}
 public void ConfirmTaxClose(bool pay){if(pay&&!ledger.Account.PayTax(session.law.ids.Next("tax"),ledger.Day)){notice="현금 부족 · 세금은 직접 납부해야 합니다";return;}Time.timeScale=1;}
 void RecoverFuel(){
  string id=session.law.ids.Next("fuel-recovery");if(!ledger.Account.RecoverFuel(id,ledger.Day))return;
  if(session.jobs.trip.State==TripState.OnTrip){RecordPassenger(0,"연료 고갈");ledger.RecordFailure();session.jobs.message="연료 고갈 · 운송 실패 / 요금 0원";}
  session.jobs.ClearForDay();session.taxi.ResetTaxi();session.law.ResetObservation();ledger.Advance(30,0);notice="연료 고갈 · 20% 공급 + 지원비 1,800원 / 영업시간 30초 경과";noticeUntil=Time.unscaledTime+8;
 }
 public bool SaveCheckpoint(){ledger.RefreshSummary();return WriteCheckpoint(ledger);}
 bool WriteCheckpoint(EconomyLedger value){
  try{
   Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
   var data=value.Account==null?JsonUtility.ToJson(value.Snapshot(),true):JsonUtility.ToJson(new CampaignCheckpoint{days=value.Snapshot(),account=value.Account.Snapshot(),passengers=value==ledger?passengers.ToArray():new PassengerResult[0],practices=value==ledger?practices.ToArray():new PracticeResult[0],law=value==ledger&&session.law?session.law.ledger.Events:new LawEvent[0],ids=value==ledger&&session.law?session.law.ids:new PrototypeIds(Guid.NewGuid().ToString("N"))},true);
   File.WriteAllText(SavePath+".tmp",data);
   if(File.Exists(SavePath))File.Replace(SavePath+".tmp",SavePath,SavePath+".bak");else File.Move(SavePath+".tmp",SavePath);
   SaveFailed=false;saveStatus="정산 저장 완료 · 종료 후에도 이어할 수 있습니다";return true;
  }catch(Exception ex){SaveFailed=true;saveStatus="저장 실패 · 저장 재시도 버튼을 눌러주세요";Debug.LogWarning("Campaign save failed: "+ex.Message);return false;}
 }
}
