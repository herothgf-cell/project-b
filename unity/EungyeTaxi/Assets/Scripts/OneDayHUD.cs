using UnityEngine;

// Modern daily operation UI, shared by the one-day fixture and the three-day prototype.
public static class OneDayHUD {
 static GUIStyle text,small,title,button;
 static bool showDayDetails;static Vector2 detailScroll;
 public static bool DayDetailsOpen {get{return showDayDetails;}}
 public static void ToggleDayDetails(){showDayDetails=!showDayDetails;}
 static void Styles(){if(text!=null)return;text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};small=new GUIStyle(text){fontSize=14};title=new GUIStyle(text){fontSize=25,fontStyle=FontStyle.Bold};button=KoreanText.Button(16);}
 static string Money(int n){return n.ToString("N0")+"원";}
 static string Clock(double value){int n=Mathf.CeilToInt((float)System.Math.Max(0,value));return n/60+":"+(n%60).ToString("00");}
 static void Panel(Rect r){CasualUI.Panel(r);}
 public static void Draw(DriveSession s){if(s.RideResultOpen)return;Styles();var b=s.business;var e=b.ledger;var d=e.Driver;if(d==null)return;bool multi=e.Settings.days>1;GUI.depth=-35;
  if(!b.preparing&&!b.ShowingSummary&&!b.restartPrompt&&!b.ResumeBlocked){
   return; // Live operation is composed by CockpitUI.

  }
  if(b.preparing&&!b.restartPrompt&&!b.ResumeBlocked){showDayDetails=false;s.GetComponent<DriveHUD>().DrawSingleMenu();return;}
  var saved=GUI.matrix;float scale=Mathf.Min(1,Screen.width/800f,Screen.height/730f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);float x=(Screen.width/scale-720)/2,y=(Screen.height/scale-680)/2;GUI.depth=-150;Panel(new Rect(x,y,720,680));
  if(b.restartPrompt&&b.ResumeBlocked){GUI.Label(new Rect(x+25,y+25,670,65),"원본 보관 후 새로 시작할까요?",title);GUI.Label(new Rect(x+25,y+115,670,210),"현재 저장을 이어갈 수 없어 영업을 멈췄습니다.\n새로 시작하면 현재 진행 대신 현금 10,000원 · 연료 60%의 1일차로 시작합니다.\n\n기존 원본과 백업은 저장 폴더 안에 별도로 보관합니다. 보관하지 못하면 새로 시작하지 않습니다.\n취소하면 기존 파일과 현재 상태를 유지합니다.",text);GUI.Label(new Rect(x+25,y+360,670,75),b.saveStatus,small);if(GUI.Button(new Rect(x+25,y+590,325,55),"원본 보관 후 새로 시작",button))b.ConfirmRestart(true);if(GUI.Button(new Rect(x+370,y+590,325,55),"돌아가기",button))b.ConfirmRestart(false);}
  else if(b.restartPrompt){GUI.Label(new Rect(x+25,y+25,670,55),multi?"새 캠페인을 시작할까요?":"새 하루를 시작할까요?",title);GUI.Label(new Rect(x+25,y+100,670,210),"현금 10,000원 · 연료 60% · 포만감 80 · 피로 10\n1일차와 운전 점수 100점부터 시작합니다.\n이전 완료 결과는 보존합니다. 취소하면 현재 진행을 유지합니다.",text);GUI.Label(new Rect(x+25,y+360,670,65),b.saveStatus,small);if(GUI.Button(new Rect(x+25,y+590,325,55),multi?"새 3일 시작":"새 하루 시작",button))b.ConfirmRestart(true);if(GUI.Button(new Rect(x+370,y+590,325,55),"취소",button))b.ConfirmRestart(false);}
  else if(b.ResumeBlocked){GUI.Label(new Rect(x+25,y+25,670,65),"저장 파일을 읽지 못했어요",title);GUI.Label(new Rect(x+25,y+110,670,160),"기존 진행은 파일에 보존되어 있으며 영업은 멈춰 있습니다.\n저장과 맞는 지도·버전에서 이어가거나, 원본을 별도 보관한 뒤 새 캠페인을 선택할 수 있습니다.\n새 캠페인은 다음 화면에서 확인한 뒤 시작합니다.",text);GUI.Label(new Rect(x+25,y+290,670,60),b.saveStatus,small);GUI.Label(new Rect(x+25,y+365,670,180),"복원 오류\n"+b.LoadFailureDetail,small);if(GUI.Button(new Rect(x+25,y+590,670,55),"복구 방법 보기",button))b.RequestRestart();}
  else {
   var r=e.Last;bool complete=e.State==BusinessState.Completed;
   GUI.Label(new Rect(x+25,y+20,670,45),complete&&multi?"캠페인 완료!":e.Day+"일차 · 영업 결과",title);
   GUI.Label(new Rect(x+25,y+77,670,45),(complete&&multi?"누적 순수익  ":"오늘 순수익  ")+Money(complete&&multi?e.TotalNet:r.net),title);
   CasualUI.Panel(new Rect(x+25,y+134,670,82),CasualUI.Soft);
   GUI.Label(new Rect(x+42,y+142,320,66),"수입  "+Money(complete?e.TotalIncome:r.income)+"\n지출  "+Money(complete?e.TotalExpenses:r.income-r.net),text);
   GUI.Label(new Rect(x+375,y+142,300,66),"잔액  "+Money(e.Account.Cash)+"\n미납  "+Money(e.Account.Debt),text);
   GUI.Label(new Rect(x+25,y+234,670,30),"운송 "+r.rides+"건 · 미완료 "+r.failedTrips+"건 · 영업 "+Clock(r.elapsed),text);
   GUI.Label(new Rect(x+25,y+274,670,30),"운전 점수 "+s.law.ledger.TotalScore+" · 청결 "+e.Care.Cleanliness+" · 내구도 "+e.Care.Damage.Durability,text);
   GUI.Label(new Rect(x+25,y+310,670,28),"포만감 "+d.satiety.ToString("0")+" · 피로 "+d.fatigue.ToString("0"),text);
   if(GUI.Button(new Rect(x+25,y+352,670,36),showDayDetails?"상세 내역 닫기":"상세 내역 보기",button))ToggleDayDetails();
   if(showDayDetails){
    string rows="";foreach(var day in e.Snapshot().records)rows+=day.day+"일차 · 수입 "+Money(day.income)+" · 순수익 "+Money(day.net)+" · 운송 "+day.rides+"건\n";
    string content=rows+"\n"+OperationMetrics.Describe(r.metrics)+"\n세차 "+Money(r.wash)+" · 수리 "+Money(r.repair)+" · 견인 "+Money(r.tow)+"\n식사 "+d.meals+"회 · 수면 "+d.sleeps+"회\n누적 수입 "+Money(e.TotalIncome)+" · 지출 "+Money(e.TotalExpenses)+" · 순수익 "+Money(e.TotalNet)+"\n"+b.saveStatus;
    float ch=Mathf.Max(120,small.CalcHeight(new GUIContent(content),625)+12);
    detailScroll=GUI.BeginScrollView(new Rect(x+25,y+398,670,125),detailScroll,new Rect(0,0,640,ch));GUI.Label(new Rect(0,0,625,ch),content,small);GUI.EndScrollView();
   }else if(!complete&&multi){
    GUI.Label(new Rect(x+25,y+407,670,65),"누적 순수익  "+Money(e.TotalNet)+"\n다음 날 · 피로 "+System.Math.Max(0,d.fatigue-b.settings.nightlyFatigueRecovery).ToString("0")+" · 포만감 "+d.satiety.ToString("0"),small);
   }else {
    CompletedDay previous=null;for(int i=b.completedDays.Count-1;i>=0;i--){var candidate=b.completedDays[i];if(candidate.id!=s.law.ids.campaign&&candidate.days==e.Settings.days&&candidate.configuration==b.ComparisonVersion&&!candidate.configuration.EndsWith("mixed")){previous=candidate;break;}}
    GUI.Label(new Rect(x+25,y+407,670,65),previous==null?"이전 완료 기록 없음":"이전 순수익 "+Money(previous.totalNet)+" · 차이 "+Money(e.TotalNet-previous.totalNet),small);
   }
   GUI.enabled=!b.SaveFailed;
   if(!complete&&multi){if(GUI.Button(new Rect(x+370,y+590,325,55),(e.Day+1)+"일차 준비",CasualUI.Action(18)))b.BeginNextCampaignDay();}
   else if(GUI.Button(new Rect(x+370,y+590,325,55),multi?"새 캠페인":"새 하루",CasualUI.Action(18)))b.RequestRestart();
   GUI.enabled=true;
   if(GUI.Button(new Rect(x+25,y+590,325,55),"운행 기록",button))s.law.OpenHistory();
   if(b.SaveFailed){GUI.Label(new Rect(x+25,y+540,470,35),b.saveStatus,small);if(GUI.Button(new Rect(x+505,y+540,190,35),"저장 재시도",button))b.SaveCheckpoint();}
  }
  GUI.matrix=saved;
 }
}
