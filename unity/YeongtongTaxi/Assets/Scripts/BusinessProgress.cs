using System;
using System.IO;
using UnityEngine;

public class BusinessProgress : MonoBehaviour {
 public DriveSession session;
 public EconomyLedger ledger;
 public bool enabledCampaign=true;
 public string notice="",saveStatus="하루 정산 시 자동 저장됩니다";
 public string SavePath {get;private set;}
 bool testMode;float noticeUntil;
 public bool SaveFailed {get;private set;}
 public bool ShowingSummary {get{return enabledCampaign&&ledger!=null&&ledger.ShowingSummary;}}
 public bool CanAccept {get{return !enabledCampaign||ledger.CanAccept;}}
 public void Initialize(DriveSession owner){
  session=owner;var args=Environment.GetCommandLineArgs();
  foreach(var arg in args)if(arg.EndsWith("Test")){testMode=true;if(arg!="-economyTest")enabledCampaign=false;}
  SavePath=testMode?Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts","economy-player-checkpoint.json")):Path.Combine(Application.persistentDataPath,"campaign-v1.json");
  ledger=new EconomyLedger(new EconomySettings());
  if(!testMode){
   foreach(var path in new[]{SavePath,SavePath+".bak"}){
    if(!File.Exists(path))continue;
    try{ledger=EconomyLedger.Restore(JsonUtility.FromJson<EconomySnapshot>(File.ReadAllText(path)));saveStatus=path==SavePath?"저장된 정산 결과를 불러왔습니다":"백업에서 정산 결과를 복구했습니다";break;}
    catch(Exception ex){saveStatus="저장 읽기 실패 · 새 영업을 시작합니다";Debug.LogWarning("Campaign load failed: "+ex.Message);}
   }
  }
  if(ShowingSummary)Freeze();
 }
 void FixedUpdate(){
  if(!enabledCampaign||!ledger.Operating)return;
  var v=session.taxi.body.velocity;double metres=Math.Sqrt(v.x*v.x+v.z*v.z)*Time.fixedDeltaTime;
  ledger.Advance(Time.fixedDeltaTime,metres);
 }
 void Update(){if(enabledCampaign&&Input.GetKeyDown(KeyCode.N))RequestEndDay();if(noticeUntil>0&&Time.unscaledTime>noticeUntil&&ledger.State!=BusinessState.Closing){notice="";noticeUntil=0;}}
 void LateUpdate(){
  if(!enabledCampaign||ledger.State!=BusinessState.Closing)return;
  if(session.jobs.trip.State==TripState.OnTrip){notice="영업 종료 대기 · 현재 승객 운송 후 정산합니다";return;}
  session.jobs.RecordExpiredTrip();
  if(ledger.Settle()){session.jobs.ClearForDay();Freeze();SaveCheckpoint();}
 }
 void Freeze(){session.paused=false;Time.timeScale=0;}
 public bool RequestEndDay(){
  if(!enabledCampaign||!ledger.CanAccept||session.paused||Time.timeScale==0)return false;
  if(session.taxi.body.velocity.magnitude>=.56f){notice="안전한 곳에 정차한 뒤 N으로 영업을 마치세요";noticeUntil=Time.unscaledTime+4;return false;}
  ledger.RequestClose();return true;
 }
 public void RecordFare(int amount){if(enabledCampaign)ledger.RecordFare(amount);}
 public void RecordFailure(){if(enabledCampaign)ledger.RecordFailure();}
 public bool NextDay(){
  if(!enabledCampaign||ledger.State!=BusinessState.Summary)return false;
  if(!SaveCheckpoint())return false;
  ledger.NextDay();PrepareDay();return true;
 }
 public void RestartCampaign(){
  if(ledger.State!=BusinessState.Won&&ledger.State!=BusinessState.Lost)return;
  var next=new EconomyLedger(new EconomySettings());if(!WriteCheckpoint(next))return;ledger=next;PrepareDay();
 }
 void PrepareDay(){session.jobs.ClearForDay();session.taxi.ResetTaxi();session.manners.BeginRide();session.paused=false;notice="";Time.timeScale=1;}
 public bool SaveCheckpoint(){return WriteCheckpoint(ledger);}
 bool WriteCheckpoint(EconomyLedger value){
  try{
   Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
   File.WriteAllText(SavePath+".tmp",JsonUtility.ToJson(value.Snapshot(),true));
   if(File.Exists(SavePath))File.Replace(SavePath+".tmp",SavePath,SavePath+".bak");else File.Move(SavePath+".tmp",SavePath);
   SaveFailed=false;saveStatus="정산 저장 완료 · 종료 후에도 이어할 수 있습니다";return true;
  }catch(Exception ex){SaveFailed=true;saveStatus="저장 실패 · 저장 재시도 버튼을 눌러주세요";Debug.LogWarning("Campaign save failed: "+ex.Message);return false;}
 }
}
