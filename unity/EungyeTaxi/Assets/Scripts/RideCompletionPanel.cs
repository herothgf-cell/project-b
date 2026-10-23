using System;
using UnityEngine;

// A settled receipt: closing this panel never recalculates the fare or replays payment.
public sealed class RideCompletionPanel:MonoBehaviour {
 public DriveSession session;public bool Open {get;private set;}
 public PassengerResult Result {get;private set;}
 int observed;bool previousPause;float previousScale;Vector2 detailScroll;
 void Start(){if(session&&session.business)observed=session.business.passengers.Count;}
 bool Automatic {get{foreach(var arg in Environment.GetCommandLineArgs())if(arg.EndsWith("Test"))return false;return !PracticeSession.Enabled&&!RoadMapRuntime.Trial;}}
 void Update(){if(!session||!session.business)return;int count=session.business.passengers.Count;if(count>observed){observed=count;if(Automatic)ShowLast();}else if(count<observed)observed=count;}
 public bool ShowLast(){if(Open||!session||session.jobs.Active||session.business.passengers.Count==0||session.EditorOpen)return false;
  Result=session.business.passengers[session.business.passengers.Count-1];detailScroll=Vector2.zero;previousPause=session.paused;previousScale=Time.timeScale;Open=true;session.paused=true;Time.timeScale=0;return true;
 }
 public void Close(){if(!Open)return;Open=false;session.paused=previousPause;Time.timeScale=(session.business.ShowingSummary||previousPause)?0:previousScale>0?previousScale:1;}
 public static string Duration(float value){int seconds=Mathf.CeilToInt(Mathf.Max(0,value));return seconds/60+"분 "+(seconds%60)+"초";}
 public string Receipt {get{if(Result==null)return "";var r=Result;bool modern=!string.IsNullOrEmpty(r.bonusReason);return "최종 약정  "+r.quoted.ToString("N0")+"원 · 기본 운임  "+(modern?r.baseFare:r.paid).ToString("N0")+"원\n보너스  "+r.bonus.ToString("N0")+"원\n합계  "+r.paid.ToString("N0")+"원";}}
 void OnGUI(){if(!Open||Result==null)return;
  var before=GUI.matrix;float scale=Mathf.Min(1,Screen.width/780f,Screen.height/740f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float sw=Screen.width/scale,sh=Screen.height/scale,x=(sw-650)/2,y=(sh-650)/2;GUI.depth=-180;
  Fill(new Rect(0,0,sw,sh),new Color(0,0,0,.65f));Fill(new Rect(x,y,650,650),new Color(.055f,.09f,.11f,1));
  var label=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};var title=new GUIStyle(label){fontSize=34,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};var large=new GUIStyle(label){fontSize=26,fontStyle=FontStyle.Bold};var button=KoreanText.Button(18);var r=Result;bool success=r.paid>0,modern=!string.IsNullOrEmpty(r.bonusReason);int satisfaction=modern?r.satisfaction:r.score;
  GUI.Label(new Rect(x+25,y+20,600,52),success?"운행 완료!":"운송 종료",title);
  GUI.Label(new Rect(x+25,y+84,600,50),KoreanText.Stop(r.pickup)+"  →  "+KoreanText.Stop(r.destination),label);
  GUI.Label(new Rect(x+25,y+139,600,30),"승객 만족도  "+satisfaction+" / 100 · 요구 "+r.required+"점",label);
  Fill(new Rect(x+25,y+174,600,12),new Color(.15f,.2f,.22f));Fill(new Rect(x+25,y+174,600*Mathf.Clamp01(satisfaction/100f),12),new Color(1,.64f,.17f));
  GUI.Label(new Rect(x+25,y+200,600,30),"운송 시간  "+Duration(r.elapsed)+"     운행 점수  "+(r.hasDrivingScore?r.drivingScore:session.law.ledger.TotalScore)+"점",label);
  var score=PassengerScoreView.Recorded(r);string detail=modern?score.Baseline+"\n"+score.Breakdown+"\n"+r.request:r.request;var detailStyle=new GUIStyle(label){fontSize=16};float detailHeight=Mathf.Max(95,detailStyle.CalcHeight(new GUIContent(detail),575));detailScroll=GUI.BeginScrollView(new Rect(x+25,y+241,600,95),detailScroll,new Rect(0,0,575,detailHeight));GUI.Label(new Rect(0,0,575,detailHeight),detail,detailStyle);GUI.EndScrollView();
  Fill(new Rect(x+25,y+347,600,155),new Color(.1f,.15f,.17f));GUI.Label(new Rect(x+42,y+358,566,138),Receipt,large);
  GUI.Label(new Rect(x+25,y+515,600,42),modern?r.bonusReason+" · "+r.timeReason+"\n도착 "+r.arrivalElapsed.ToString("0")+"초 / 하차 "+r.disembarkElapsed.ToString("0")+"초 · "+(r.drunk?"취객 ":"")+(r.vomited?"구토 ":"")+(r.wakeDelayed?"기상 지연 ":"")+"청결 "+r.cleanlinessAfter:r.mannerReason,new GUIStyle(label){fontSize=14});
  if(GUI.Button(new Rect(x+25,y+580,290,46),(session.business.ShowingSummary||session.business.ledger.State==BusinessState.Closing)?"영업 정산 확인":"계속 영업",button))Close();
  GUI.enabled=!session.business.ShowingSummary&&session.business.ledger.State==BusinessState.Open;if(GUI.Button(new Rect(x+335,y+580,290,46),"생활 거점 선택",button)){Close();session.services.Toggle();}GUI.enabled=true;GUI.matrix=before;
 }
 static void Fill(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
}
