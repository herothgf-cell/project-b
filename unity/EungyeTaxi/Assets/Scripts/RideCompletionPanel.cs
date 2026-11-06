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
 public int PortraitIndex {get{return PassengerDialogueUI.PortraitIndex(Result==null?null:Result.offerId,Result==null?null:Result.tripId);}}
 public bool CanOpenFacilities {get{return Open&&session&&session.services&&session.business&&session.business.ledger!=null&&!session.jobs.Active&&!session.business.ledger.Care.Damage.Disabled&&!session.business.ShowingSummary&&session.business.ledger.State==BusinessState.Open&&(!session.simulationMatch||!session.simulationMatch.Finished);}}
 public bool TryOpenFacilities(){
  if(!CanOpenFacilities)return false;Close();
  // A completed ride may already have opened the hub underneath this receipt.
  // Keep it open instead of toggling it closed; viewing it never selects a route or buys a service.
  if(session.services.CanOpenLifeHub&&(session.services.PostTripVisible||session.services.ToggleLifeHub()))return true;
  ShowLast();return false;
 }
 void OnGUI(){if(!Open||Result==null)return;
  var before=GUI.matrix;float scale=Mathf.Min(1,Screen.width/780f,Screen.height/740f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float sw=Screen.width/scale,sh=Screen.height/scale,x=(sw-650)/2,y=(sh-650)/2;GUI.depth=-180;
  Fill(new Rect(0,0,sw,sh),new Color(.07f,.15f,.29f,.42f));CasualUI.Panel(new Rect(x,y,650,650));
  var label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};var title=new GUIStyle(label){fontSize=34,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};var large=new GUIStyle(label){fontSize=26,fontStyle=FontStyle.Bold};var button=KoreanText.Button(18);var r=Result;bool success=r.paid>0,modern=!string.IsNullOrEmpty(r.bonusReason);int satisfaction=modern?r.satisfaction:r.score;
  CasualUI.Header(new Rect(x+20,y+18,610,58),success?"운송 완료!":"운송 종료",6,success?CasualUI.Green:CasualUI.Blue);
  CasualUI.Portrait(new Rect(x+25,y+92,108,108),PortraitIndex);
  string route=KoreanText.Stop(r.pickup)+"  →  "+KoreanText.Stop(r.destination);var routeStyle=new GUIStyle(label);while(routeStyle.fontSize>13&&routeStyle.CalcHeight(new GUIContent(route),474)>50)routeStyle.fontSize--;
  GUI.Label(new Rect(x+151,y+92,474,50),route,routeStyle);
  GUI.Label(new Rect(x+151,y+150,474,30),"만족도 "+satisfaction+" / 100 · 요구 "+r.required+"점",label);
  CasualUI.Bar(new Rect(x+151,y+188,474,12),satisfaction/100f,CasualUI.Green);
  HudGlyph.DrawBadge(new Rect(x+25,y+216,26,26),18);GUI.Label(new Rect(x+61,y+214,564,30),"운송 "+Duration(r.elapsed)+"  ·  운전 점수 "+(r.hasDrivingScore?r.drivingScore:session.law.ledger.TotalScore)+"점",label);
  string factors=modern?RideReceiptSummary.Factors(r):"만족도 기록 없음";var factorStyle=new GUIStyle(label);while(factorStyle.fontSize>14&&factorStyle.CalcHeight(new GUIContent(factors),600)>95)factorStyle.fontSize--;GUI.Label(new Rect(x+25,y+244,600,95),factors,factorStyle);
  CasualUI.Panel(new Rect(x+25,y+347,600,155),new Color(.91f,.98f,.94f));var receiptRows=Receipt.Split('\n');
  for(int i=0;i<receiptRows.Length;i++){var receiptStyle=new GUIStyle(large){fontSize=i==2?34:21};receiptStyle.normal.textColor=i==2?CasualUI.Green:CasualUI.Ink;while(receiptStyle.fontSize>18&&receiptStyle.CalcSize(new GUIContent(receiptRows[i])).x>(i==2?522:566))receiptStyle.fontSize--;if(i==2)HudGlyph.DrawBadge(new Rect(x+42,y+444,34,34),4);GUI.Label(new Rect(x+(i==2?86:42),y+356+i*42,i==2?522:566,i==2?52:38),receiptRows[i],receiptStyle);}
  GUI.Label(new Rect(x+25,y+514,600,25),RideReceiptSummary.PaymentStatus(r)+(r.wakeDelayed?" · 기상 대기":""),new GUIStyle(label){fontSize=16});
  if(GUI.Button(new Rect(x+335,y+580,290,46),(session.business.ShowingSummary||session.business.ledger.State==BusinessState.Closing)?"영업 정산":"계속 영업",CasualUI.Action(18)))Close();
  GUI.enabled=CanOpenFacilities;if(GUI.Button(new Rect(x+25,y+580,290,46),"시설 찾기",button))TryOpenFacilities();GUI.enabled=true;GUI.matrix=before;
 }
 static void Fill(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
}
