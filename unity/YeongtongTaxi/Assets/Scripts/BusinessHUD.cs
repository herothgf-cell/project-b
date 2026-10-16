using UnityEngine;

public class BusinessHUD : MonoBehaviour {
 public DriveSession session;
 GUIStyle text,small,heading,number,button;int practiceVariant=1;
 string Won(int amount){return amount.ToString("N0")+"원";}
 void Styles(){if(text!=null)return;
  text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=17};
  small=new GUIStyle(text){fontSize=13};heading=new GUIStyle(text){fontSize=26,fontStyle=FontStyle.Bold};
  number=new GUIStyle(heading){fontSize=23,alignment=TextAnchor.MiddleRight};
  button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=15};
  text.normal.textColor=small.normal.textColor=heading.normal.textColor=number.normal.textColor=Color.white;
 }
 void Fill(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 void Progress(Rect r,EconomyLedger e){Fill(r,new Color(.16f,.23f,.28f));Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01((float)e.TotalNet/e.Settings.target),r.height),new Color(.25f,.85f,.67f));}
 void OnGUI(){if(!session||!session.business||!session.business.enabledCampaign||session.services.open||session.law.HistoryOpen)return;Styles();
  var b=session.business;var e=b.ledger;GUI.depth=-30;
  if(b.pendingPayment>=0){float cx=(Screen.width-620)/2f,cy=(Screen.height-250)/2f;Fill(new Rect(cx,cy,620,250),new Color(.04f,.09f,.12f,1));GUI.Label(new Rect(cx+25,cy+25,570,110),b.pendingPayment==0?"보험 갱신 · 6,000원\n기존 계약: 2일차 만료 / 갱신 계약: 3일차 적용\n확정하면 현금에서 한 번 결제합니다.":"세금 직접 납부 · 1,500원\n기한: 3일차 / 미납 결제는 비용을 중복 차감하지 않습니다.",text);if(GUI.Button(new Rect(cx+25,cy+170,275,45),"결제 확정",button))b.ConfirmPayment(true);if(GUI.Button(new Rect(cx+320,cy+170,275,45),"취소",button))b.ConfirmPayment(false);return;}
  if(b.preparing){
   float px=(Screen.width-620)/2f,py=(Screen.height-440)/2f;Fill(new Rect(px,py,620,440),new Color(.04f,.09f,.12f,.98f));var account=e.Account;
   GUI.Label(new Rect(px+20,py+20,580,45),e.Day+"일차 영업 준비 · 3일 × 8분 / 순수익 30,000원",new GUIStyle(text){fontSize=20});
   GUI.Label(new Rect(px+20,py+80,580,65),"현금 "+Won(account.Cash)+" · 미납 "+Won(account.Debt)+" · 연료 "+account.Fuel.ToString("0")+"%\n보험 2일차 만료 / 세금 3일차 기한 · 갱신·납부가 성공 조건입니다.",text);
   if(GUI.Button(new Rect(px+20,py+155,280,40),account.Renewed?"보험 갱신 완료":"보험 갱신 · 6,000원",button))b.RequestPayment(0);
   if(GUI.Button(new Rect(px+320,py+155,280,40),account.TaxPaid?"세금 납부 완료":"세금 납부 · 1,500원",button))b.RequestPayment(1);
   GUI.Label(new Rect(px+20,py+205,580,45),"WASD 운전 · Space 제동 · F 승하차 · H 조향 보조 (가감속 직접)\nF7 거점 · F8 기록 · C 시점 · Q/E 깜빡이 · F10 안내 표시",small);
   if(GUI.Button(new Rect(px+20,py+265,180,40),"연습 조건 R"+(practiceVariant>=5?5:practiceVariant)+(practiceVariant==6?" 횡단 중":""),button))practiceVariant=practiceVariant%6+1;
   var practice=session.GetComponent<PracticeSession>();GUI.enabled=!practice.Busy;
   if(GUI.Button(new Rect(px+210,py+265,390,40),"영업과 별도로 우회전 연습",button))practice.Begin(new LawEvent{id=session.law.ids.Next("practice"),day=e.Day,scenario="R"+practiceVariant},true);
   GUI.Label(new Rect(px+20,py+315,580,45),practice.notice+" "+b.notice,small);
   if(GUI.Button(new Rect(px+20,py+370,580,45),"영업 시작",button))b.Depart();GUI.enabled=true;return;
  }
  if(b.taxPrompt&&e.State==BusinessState.Closing&&Time.timeScale==0){
   float tx=(Screen.width-600)/2f,ty=(Screen.height-220)/2f;Fill(new Rect(tx,ty,600,220),new Color(.04f,.09f,.12f,.98f));
   GUI.Label(new Rect(tx+20,ty+20,560,90),"3일차 세금 납부 기한 · 1,500원\n미납 시 현금 자동 출금 없이 부채로 기록됩니다.\n"+b.notice,text);
   if(GUI.Button(new Rect(tx+20,ty+140,270,50),"직접 납부 후 정산",button))b.ConfirmTaxClose(true);
   if(GUI.Button(new Rect(tx+310,ty+140,270,50),"미납 상태로 정산",button))b.ConfirmTaxClose(false);return;
  }
  if(b.ShowingSummary){Summary(b,e);return;}
  Fill(new Rect(16,202,380,119),new Color(.04f,.09f,.12f,.94f));
  int remaining=Mathf.CeilToInt((float)e.SecondsLeft);
  GUI.Label(new Rect(28,209,245,25),e.Day+" / "+e.Settings.days+"일 · "+(e.SecondsLeft<=60&&e.CanAccept?"마감 임박 · ":"")+(e.State==BusinessState.Closing?"마감 대기":remaining/60+":"+(remaining%60).ToString("00")+" 남음"),text);
  GUI.enabled=e.CanAccept&&!session.paused;
  if(GUI.Button(new Rect(277,210,107,26),"N 영업 마감",button))b.RequestEndDay();GUI.enabled=true;
  GUI.Label(new Rect(28,240,355,22),(e.Account==null?"정산 누적 ":"순자산 수익 ")+Won(e.TotalNet)+" / 목표 "+Won(e.Settings.target),small);
  GUI.Label(new Rect(28,263,355,22),"오늘 요금 "+Won(e.Income)+" · 예상 순수익 "+Won(e.ProjectedNet),small);
  Progress(new Rect(28,291,355,5),e);
  GUI.Label(new Rect(28,298,355,20),e.Account==null?"목표까지 "+Won(e.RemainingGoal):"현금 "+Won(e.Account.Cash)+" · 미납 "+Won(e.Account.Debt)+" · 연료 "+e.Account.Fuel.ToString("0")+"%",small);
  if(!string.IsNullOrEmpty(b.notice)){Fill(new Rect(410,300,Screen.width-680,52),new Color(.04f,.09f,.12f,.94f));var wrap=new GUIStyle(small){wordWrap=true};GUI.Label(new Rect(420,305,Screen.width-700,44),b.notice,wrap);}
 }
 void Summary(BusinessProgress b,EconomyLedger e){
  Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.015f,.025f,.04f,.91f));
  float x=(Screen.width-640)/2f,y=(Screen.height-588)/2f;
  Fill(new Rect(x,y,640,588),new Color(.055f,.10f,.14f));
  string result=e.State==BusinessState.Won?"목표 달성!":e.State==BusinessState.Lost?(e.TotalNet<e.Settings.target?"도전 종료 · 목표 금액 미달":"도전 종료 · 갱신·납부 미완료"):"오늘 영업을 마쳤습니다";
  GUI.Label(new Rect(x+28,y+18,584,40),result,heading);
  var day=e.Last;
  GUI.Label(new Rect(x+28,y+62,584,28),e.Day+"일차 정산 · 운송 "+day.rides+"건 · 운송 실패 "+day.failedTrips+"건 · "+(day.metres/1000).ToString("0.00")+"km",text);
  Row(x,y+111,"운송 요금",Won(day.income));
  Row(x,y+149,e.Account==null?"기름값 (주행 + 공회전)":"실제 주유·연료 지원","− "+Won(day.fuel));
  Row(x,y+187,e.Account==null?"하루 보험료":"보험 갱신 결제","− "+Won(day.insurance));
  Row(x,y+225,"하루 식비","− "+Won(day.food));
  Fill(new Rect(x+28,y+268,584,1),new Color(.23f,.33f,.39f));
  GUI.Label(new Rect(x+28,y+281,260,34),"오늘 순수익",heading);
  GUI.Label(new Rect(x+290,y+281,322,34),Won(day.net),number);
  GUI.Label(new Rect(x+28,y+332,584,30),"누적 순수익  "+Won(e.TotalNet)+"     /     목표  "+Won(e.Settings.target),text);
  Progress(new Rect(x+28,y+372,584,10),e);
  GUI.Label(new Rect(x+28,y+395,584,30),"목표까지 "+Won(e.RemainingGoal)+"  ·  남은 영업일 "+(e.Settings.days-e.Day)+"일",text);
  GUI.Label(new Rect(x+28,y+441,470,24),b.saveStatus,small);
  if(b.SaveFailed&&GUI.Button(new Rect(x+506,y+440,106,28),"저장 재시도",button))b.SaveCheckpoint();
  if(e.Account!=null){
   GUI.Label(new Rect(x+28,y+419,584,20),"주차·세금 등 "+Won(e.Account.Expense(e.Day,null)-day.fuel-day.insurance-day.food)+" · 미납 "+Won(e.Account.Debt)+" · 보험 "+(e.Account.Renewed?"갱신":"미갱신")+" · 세금 "+(e.Account.TaxPaid?"납부":e.Day>=b.settings.taxDueDay?"미납":"납부 예정"),small);
   if(e.State==BusinessState.Summary&&!e.DepartureAllowed&&GUI.Button(new Rect(x+28,y+480,365,24),"보험 갱신 6,000원 · 출발 전 필요",button))b.RequestPayment(0);
  }
  GUI.Label(new Rect(x+28,y+461,584,18),"미탑승 호출은 마감 시 취소됩니다. 진행 중인 하루는 저장되지 않습니다.",small);
  if(e.State==BusinessState.Summary&&!e.DepartureAllowed&&GUI.Button(new Rect(x+409,y+480,203,24),"갱신 불가 · 새 도전",button))b.RestartCampaign();
  GUI.enabled=!session.GetComponent<PracticeSession>().Busy;
  if(e.State==BusinessState.Summary){if(GUI.Button(new Rect(x+28,y+505,365,48),(e.Day+1)+"일차 영업 시작",button))b.NextDay();}
  else if(GUI.Button(new Rect(x+28,y+505,365,48),"새 "+e.Settings.days+"일 도전 시작 (기록 초기화)",button))b.RestartCampaign();
  GUI.enabled=true;
  if(GUI.Button(new Rect(x+409,y+505,203,48),"법규·운송 기록 / 복습",button))session.law.OpenHistory();
  GUI.Label(new Rect(x+28,y+560,584,24),session.law.ledger.DaySummary(e.Day).Split('\n')[0]+" · 종료는 창 닫기 / 기록은 정산 저장",small);
 }
 void Row(float x,float y,string label,string value){GUI.Label(new Rect(x+28,y,340,29),label,text);GUI.Label(new Rect(x+375,y,237,29),value,new GUIStyle(text){alignment=TextAnchor.MiddleRight});}
}
