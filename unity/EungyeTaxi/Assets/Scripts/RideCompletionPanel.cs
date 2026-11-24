using System;
using UnityEngine;

// A settled receipt: closing this panel never recalculates the fare or replays payment.
public sealed class RideCompletionPanel:MonoBehaviour {
 public DriveSession session;public bool Open {get;private set;}
 public PassengerResult Result {get;private set;}
 int observed;bool previousPause;float previousScale;
 void Start(){if(session&&session.business)observed=session.business.passengers.Count;}
 bool Automatic {get{foreach(var arg in Environment.GetCommandLineArgs())if(arg.EndsWith("Test"))return false;return !PracticeSession.Enabled&&!RoadMapRuntime.Trial;}}
 void Update(){if(!session||!session.business)return;int count=session.business.passengers.Count;if(count>observed){observed=count;if(Automatic)ShowLast();}else if(count<observed)observed=count;}
 public bool ShowLast(){if(Open||!session||session.jobs.Active||session.business.passengers.Count==0||session.EditorOpen)return false;
  Result=session.business.passengers[session.business.passengers.Count-1];previousPause=session.paused;previousScale=Time.timeScale;Open=true;if(!PvpNativeLan.RunningOnline){session.paused=true;Time.timeScale=0;}return true;
 }
 public void Close(){if(!Open)return;Open=false;if(PvpNativeLan.RunningOnline){session.paused=false;Time.timeScale=1;return;}session.paused=previousPause;Time.timeScale=(session.business.ShowingSummary||previousPause)?0:previousScale>0?previousScale:1;}
 public static string Duration(float value){int seconds=Mathf.CeilToInt(Mathf.Max(0,value));return seconds/60+"분 "+(seconds%60)+"초";}
 public string Receipt {get{return Result==null?"":RideReceiptSummary.Money(Result);}}
 public int PortraitIndex {get{if(Result!=null&&!string.IsNullOrEmpty(Result.reunionLine))return PassengerPresentation.Persona(PassengerDialogueUI.StudentIdentity(session.business.Logbook)).portrait;return PassengerPresentation.Persona(PassengerPresentation.Identity(PassengerDialogueUI.PortraitIdentity(Result==null?null:Result.offerId,Result==null?null:Result.tripId),Result==null?null:Result.storyId)).portrait;}}
 public bool CanOpenFacilities {get{return Open&&session&&session.services&&session.business&&session.business.ledger!=null&&!session.jobs.Active&&!session.business.ledger.Care.Damage.Disabled&&!session.business.ShowingSummary&&session.business.ledger.State==BusinessState.Open&&(!session.simulationMatch||!session.simulationMatch.Finished);}}
 public bool TryOpenFacilities(){
  if(!CanOpenFacilities)return false;Close();
  // A completed ride may already have opened the hub underneath this receipt.
  // Keep it open instead of toggling it closed; viewing it never selects a route or buys a service.
  if(session.services.CanOpenLifeHub&&(session.services.PostTripVisible||session.services.ToggleLifeHub()))return true;
  ShowLast();return false;
 }
 void OnGUI(){if(!Open||Result==null)return;
  var r=Result;var story=PassengerStories.Find(r.storyId);bool hasStory=story!=null&&!string.IsNullOrEmpty(r.storyLine);
  var book=session.business.Logbook;
  bool ask=hasStory&&r.tripId==book.studentFirstTrip&&!book.studentAnswered;
  bool reunion=!string.IsNullOrEmpty(r.reunionLine);
  // 08 chapter 2: a tip gets its own row under the fare box; everything below moves down by tipRow.
  string[] extraRows=System.Array.FindAll(new[]{RideReceiptSummary.MissionRow(r),RideReceiptSummary.TipRow(r)},t=>t.Length>0);
  float tipRow=extraRows.Length*40;
  float height=(ask?834:hasStory||reunion?720:650)+tipRow;
  var before=GUI.matrix;float scale=Mathf.Min(1,Screen.width/780f,Screen.height/(height+20));GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float sw=Screen.width/scale,sh=Screen.height/scale,x=(sw-650)/2,y=(sh-height)/2;GUI.depth=-180;
  Fill(new Rect(0,0,sw,sh),new Color(.07f,.15f,.29f,.42f));CasualUI.Panel(new Rect(x,y,650,height));
  var label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};var large=new GUIStyle(label){fontSize=26,fontStyle=FontStyle.Bold};var button=KoreanText.Button(18);bool success=r.paid>0,modern=!string.IsNullOrEmpty(r.bonusReason);int satisfaction=modern?r.satisfaction:r.score;
  bool late=r.outcome=="시간 초과 후 목적지 도착";
  CasualUI.Header(new Rect(x+20,y+18,610,58),late?"하차 완료 · 도착 기한 초과":success?"운송 완료!":"운송 종료",6,success?CasualUI.Green:CasualUI.Blue);
  CasualUI.Portrait(new Rect(x+25,y+92,108,108),PortraitIndex);
  string route=KoreanText.Stop(r.pickup)+"  →  "+KoreanText.Stop(r.destination);var routeStyle=new GUIStyle(label);while(routeStyle.fontSize>13&&routeStyle.CalcHeight(new GUIContent(route),474)>50)routeStyle.fontSize--;
  GUI.Label(new Rect(x+151,y+92,474,50),route,routeStyle);
  GUI.Label(new Rect(x+151,y+150,474,30),"만족도 "+satisfaction+" / 100 · 요구 "+r.required+"점",label);
  CasualUI.Bar(new Rect(x+151,y+188,474,12),satisfaction/100f,CasualUI.Green);
  float rideSeconds=r.disembarkElapsed>0?r.disembarkElapsed:r.elapsed;
  HudGlyph.DrawBadge(new Rect(x+25,y+216,26,26),18);GUI.Label(new Rect(x+61,y+214,564,30),"운송 "+Duration(rideSeconds)+"  ·  운전 점수 "+(r.hasDrivingScore?r.drivingScore:session.law.ledger.TotalScore)+"점",label);
  string factors=modern?RideReceiptSummary.Factors(r):"만족도 기록 없음";var factorStyle=new GUIStyle(label);while(factorStyle.fontSize>14&&factorStyle.CalcHeight(new GUIContent(factors),600)>95)factorStyle.fontSize--;GUI.Label(new Rect(x+25,y+244,600,95),factors,factorStyle);
  CasualUI.Panel(new Rect(x+25,y+347,600,155),new Color(.91f,.98f,.94f));var receiptRows=Receipt.Split('\n');
  for(int i=0;i<receiptRows.Length;i++){var receiptStyle=new GUIStyle(large){fontSize=i==2?34:21};receiptStyle.normal.textColor=i==2?CasualUI.Green:CasualUI.Ink;while(receiptStyle.fontSize>18&&receiptStyle.CalcSize(new GUIContent(receiptRows[i])).x>(i==2?522:566))receiptStyle.fontSize--;if(i==2)HudGlyph.DrawBadge(new Rect(x+42,y+444,34,34),4);GUI.Label(new Rect(x+(i==2?86:42),y+356+i*42,i==2?522:566,i==2?52:38),receiptRows[i],receiptStyle);}
  for(int row=0;row<extraRows.Length;row++){float ry=y+508+row*40;CasualUI.Panel(new Rect(x+25,ry,600,34),new Color(1,.96f,.84f));var tipStyle=new GUIStyle(label){fontSize=18,font=KoreanText.BoldFont};string tipText=extraRows[row];while(tipStyle.fontSize>13&&tipStyle.CalcSize(new GUIContent(tipText)).x>580)tipStyle.fontSize--;GUI.Label(new Rect(x+37,ry+4,580,28),tipText,tipStyle);}
  GUI.Label(new Rect(x+25,y+514+tipRow,600,25),RideReceiptSummary.PaymentStatus(r)+(r.wakeDelayed?" · 기상 대기":""),new GUIStyle(label){fontSize=16});
  if(hasStory||reunion){
   CasualUI.Panel(new Rect(x+25,y+542+tipRow,600,78),CasualUI.Soft);
   GUI.Label(new Rect(x+37,y+548+tipRow,575,27),reunion?"실습 끝난 학생":story.title,new GUIStyle(label){font=KoreanText.BoldFont});
   var line=new GUIStyle(label){fontSize=17};
   string closing=reunion?r.reunionLine:r.storyLine;
   while(line.fontSize>13&&line.CalcHeight(new GUIContent(closing),575)>45)line.fontSize--;
   GUI.Label(new Rect(x+37,y+573+tipRow,575,45),closing,line);
  }
  if(r.foundFatherNotebook)GUI.Label(new Rect(x+25,y+(hasStory?622:538)+tipRow,600,27),"글러브박스에서 수첩을 발견했다",new GUIStyle(label){font=KoreanText.BoldFont});
  if(ask){
   GUI.Label(new Rect(x+25,y+651+tipRow,600,34),"“모레도 실습 끝나면 이 시간이에요. 여기서 다시 타도 돼요?”",label);
   if(GUI.Button(new Rect(x+25,y+693+tipRow,290,44),"그때 여기로 올게요",button))AnswerStudent(true);
   if(GUI.Button(new Rect(x+335,y+693+tipRow,290,44),"지나가다 만나면 태워 줄게요",button))AnswerStudent(false);
  }
  float controls=y+height-70;
  if(KeyHints.Button(new Rect(x+335,controls,290,46),(session.business.ShowingSummary||session.business.ledger.State==BusinessState.Closing)?"영업 정산":"계속 영업","Enter",CasualUI.Action(18),true)||KeyHints.Enter())Close();
  GUI.enabled=CanOpenFacilities;if(GUI.Button(new Rect(x+25,controls,290,46),"시설 찾기",button))TryOpenFacilities();GUI.enabled=true;GUI.matrix=before;
 }
 public bool AnswerStudent(bool promise){
  var b=session.business;var book=b.Logbook;
  if(!Open||Result==null||Result.tripId!=book.studentFirstTrip||book.studentAnswered)return false;
  book.studentAnswered=true;book.studentPromised=promise;
  if(b.SaveCheckpoint())return true;
  book.studentAnswered=book.studentPromised=false;return false;
 }
 static void Fill(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
}
