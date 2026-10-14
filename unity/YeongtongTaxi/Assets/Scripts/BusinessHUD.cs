using UnityEngine;

public class BusinessHUD : MonoBehaviour {
 public DriveSession session;
 GUIStyle text,small,heading,number,button;
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
 void OnGUI(){if(!session||!session.business||!session.business.enabledCampaign)return;Styles();
  var b=session.business;var e=b.ledger;GUI.depth=-30;
  if(b.ShowingSummary){Summary(b,e);return;}
  Fill(new Rect(16,202,380,119),new Color(.04f,.09f,.12f,.94f));
  int remaining=Mathf.CeilToInt((float)e.SecondsLeft);
  GUI.Label(new Rect(28,209,245,25),e.Day+" / "+e.Settings.days+"일 · "+(e.State==BusinessState.Closing?"마감 대기":remaining/60+":"+(remaining%60).ToString("00")+" 남음"),text);
  GUI.enabled=e.CanAccept&&!session.paused;
  if(GUI.Button(new Rect(277,210,107,26),"N 영업 마감",button))b.RequestEndDay();GUI.enabled=true;
  GUI.Label(new Rect(28,240,355,22),"정산 누적 "+Won(e.TotalNet)+" / 목표 "+Won(e.Settings.target),small);
  GUI.Label(new Rect(28,263,355,22),"오늘 요금 "+Won(e.Income)+" · 예상 순수익 "+Won(e.ProjectedNet),small);
  Progress(new Rect(28,291,355,5),e);
  GUI.Label(new Rect(28,298,355,20),"목표까지 "+Won(e.RemainingGoal)+" · 보험·식비 포함 예상",small);
  if(!string.IsNullOrEmpty(b.notice)){Fill(new Rect(410,300,Screen.width-680,52),new Color(.04f,.09f,.12f,.94f));var wrap=new GUIStyle(small){wordWrap=true};GUI.Label(new Rect(420,305,Screen.width-700,44),b.notice,wrap);}
 }
 void Summary(BusinessProgress b,EconomyLedger e){
  Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.015f,.025f,.04f,.91f));
  float x=(Screen.width-640)/2f,y=(Screen.height-588)/2f;
  Fill(new Rect(x,y,640,588),new Color(.055f,.10f,.14f));
  string result=e.State==BusinessState.Won?"목표 달성!":e.State==BusinessState.Lost?"도전 종료 · 목표 금액 미달":"오늘 영업을 마쳤습니다";
  GUI.Label(new Rect(x+28,y+18,584,40),result,heading);
  var day=e.Last;
  GUI.Label(new Rect(x+28,y+62,584,28),e.Day+"일차 정산 · 운송 "+day.rides+"건 · 시간 초과 "+day.failedTrips+"건 · "+(day.metres/1000).ToString("0.00")+"km",text);
  Row(x,y+111,"운송 요금",Won(day.income));
  Row(x,y+149,"기름값 (주행 + 공회전)","− "+Won(day.fuel));
  Row(x,y+187,"하루 보험료","− "+Won(day.insurance));
  Row(x,y+225,"하루 식비","− "+Won(day.food));
  Fill(new Rect(x+28,y+268,584,1),new Color(.23f,.33f,.39f));
  GUI.Label(new Rect(x+28,y+281,260,34),"오늘 순수익",heading);
  GUI.Label(new Rect(x+290,y+281,322,34),Won(day.net),number);
  GUI.Label(new Rect(x+28,y+332,584,30),"누적 순수익  "+Won(e.TotalNet)+"     /     목표  "+Won(e.Settings.target),text);
  Progress(new Rect(x+28,y+372,584,10),e);
  GUI.Label(new Rect(x+28,y+395,584,30),"목표까지 "+Won(e.RemainingGoal)+"  ·  남은 영업일 "+(e.Settings.days-e.Day)+"일",text);
  GUI.Label(new Rect(x+28,y+435,470,24),b.saveStatus,small);
  if(b.SaveFailed&&GUI.Button(new Rect(x+506,y+432,106,28),"저장 재시도",button))b.SaveCheckpoint();
  GUI.Label(new Rect(x+28,y+459,584,24),"미탑승 호출은 마감 시 취소됩니다. 진행 중인 하루는 저장되지 않습니다.",small);
  if(e.State==BusinessState.Summary){if(GUI.Button(new Rect(x+28,y+505,365,48),(e.Day+1)+"일차 영업 시작",button))b.NextDay();}
  else if(GUI.Button(new Rect(x+28,y+505,365,48),"새 7일 도전 시작 (기록 초기화)",button))b.RestartCampaign();
  if(GUI.Button(new Rect(x+409,y+505,203,48),"게임 종료",button))Application.Quit();
 }
 void Row(float x,float y,string label,string value){GUI.Label(new Rect(x+28,y,340,29),label,text);GUI.Label(new Rect(x+375,y,237,29),value,new GUIStyle(text){alignment=TextAnchor.MiddleRight});}
}
