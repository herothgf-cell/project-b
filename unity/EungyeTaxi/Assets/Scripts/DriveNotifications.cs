using System.Collections.Generic;
using UnityEngine;

public class DriveNotifications : MonoBehaviour {
 public DriveSession session;
 class Notice {public string text;public bool positive,passenger;public float since;}
 const float NoticeSeconds=4;
 readonly List<Notice> visible=new List<Notice>();LawLedger ledger;int score,lawCount,lawRevision;bool ready;string observedTrip;PassengerScoreView passenger;
 public bool FuelLow {get{return session.business.enabledCampaign&&session.business.ledger.Account!=null&&session.business.ledger.Account.Fuel<=15;}}
 public string VisibleText {get{return visible.Count==0?"":visible[0].text;}}
 public string[] VisibleTexts {get{return visible.ConvertAll(n=>(n.passenger?"만족도":"운전")+" | "+n.text).ToArray();}}
 public static Rect NoticeBounds(float width,float height,bool firstPerson){float scale=UiReadability.HudScale(width,height);var nav=DrivingReadability.NavigationBounds(width,height,firstPerson);return new Rect((width-460*scale)/2,nav.yMax+12*scale,460*scale,58*scale);}
 void LateUpdate(){
  if(!session||!session.law||!session.business)return;
  var journal=session.GetComponent<DrivingScoreJournal>();if(journal&&!journal.Loaded)return;
  RefreshScores();
 }
 // Values are observed only. Every new notice is immediately visible, without a FIFO delay.
 public void RefreshScores(){
  visible.RemoveAll(n=>Time.unscaledTime-n.since>=NoticeSeconds);
  var jobs=session.jobs;var view=jobs.ScoreView;string trip=jobs.HasPassengerScore?session.law.tripId:null;
  if(!ready||ledger!=session.law.ledger){ledger=session.law.ledger;score=ledger.TotalScore;lawCount=ledger.Count;lawRevision=ledger.Revision;observedTrip=trip;passenger=view;visible.Clear();ready=true;return;}
  if(trip!=observedTrip){visible.RemoveAll(n=>n.passenger);observedTrip=trip;passenger=view;}
  else if(trip!=null){
   int after=PassengerScoreView.Calculate(view.start,view.drive,passenger.request,passenger.time,passenger.comfort);if(after!=passenger.value)Push(ScoreFeedback.Text(session.manners.lastEvent,after-passenger.value),after>passenger.value,true);
   int requested=PassengerScoreView.Calculate(view.start,view.drive,view.request,passenger.time,passenger.comfort);if(requested!=after)Push(ScoreFeedback.Text(requested>after?"요청 이행":"요청 미이행",requested-after),requested>after,true);
   int comforted=PassengerScoreView.Calculate(view.start,view.drive,view.request,passenger.time,view.comfort);if(comforted!=requested)Push(ScoreFeedback.Text("안정 주행 안심",comforted-requested),comforted>requested,true);requested=comforted;
   if(view.value!=requested)Push(ScoreFeedback.Text("도착 시간",view.value-requested),view.value>requested,true);passenger=view;
  }
  if(lawRevision!=ledger.Revision){var events=ledger.Events;int prefix=100;for(int i=0;i<Mathf.Min(lawCount,events.Length);i++)prefix=LawLedger.ScoreAfter(prefix,events[i]);if(prefix!=score)Push(ScoreFeedback.Text(lawCount==events.Length?ScoreFeedback.LawReason(ledger.LastChanged,prefix-score):"법규 사건 갱신",prefix-score),prefix>score);score=prefix;for(int i=lawCount;i<events.Length;i++){int next=LawLedger.ScoreAfter(score,events[i]);if(next!=score)Push(ScoreFeedback.Text(ScoreFeedback.LawReason(events[i],next-score),next-score),next>score);score=next;}lawCount=events.Length;lawRevision=ledger.Revision;}
 }
 void Push(string text,bool good,bool passenger=false){visible.Insert(0,new Notice{text=text,positive=good,passenger=passenger,since=Time.unscaledTime});if(visible.Count>4)visible.RemoveAt(visible.Count-1);}
 void OnGUI(){
  if(!ready||!session||session.FullMapOpen||session.ChoiceOpen||session.PracticeMenuOpen||session.law.HistoryOpen||session.business.preparing||session.business.ShowingSummary)return;
  var matrix=GUI.matrix;var color=GUI.color;var background=GUI.backgroundColor;var content=GUI.contentColor;int depth=GUI.depth;bool enabled=GUI.enabled;
  try {
   // Draw in the cockpit reference space and isolate every inherited IMGUI tint.
   float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));GUI.color=GUI.backgroundColor=GUI.contentColor=Color.white;GUI.enabled=true;GUI.depth=-80;
   var anchor=NoticeBounds(Screen.width,Screen.height,session.drivingCamera&&session.drivingCamera.FirstPerson);float width=anchor.width/scale,x=anchor.x/scale,y=anchor.y/scale;
   var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true,alignment=TextAnchor.MiddleLeft};style.normal.textColor=CasualUI.Ink;
   var category=new GUIStyle(style){fontSize=14,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};category.normal.textColor=Color.white;
   if(FuelLow){Panel(new Rect(x,y,width,62),new Color(1,.95f,.81f,1));GUI.color=session.business.ledger.Account.Fuel<=5?CasualUI.Danger:CasualUI.Amber;
    // Pump icon uses geometry so missing font glyphs cannot hide the warning.
    GUI.DrawTexture(new Rect(x+12,y+16,17,29),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x+29,y+22,7,4),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x+33,y+22,4,21),Texture2D.whiteTexture);GUI.color=CasualUI.Ink;GUI.DrawTexture(new Rect(x+15,y+19,11,9),Texture2D.whiteTexture);GUI.color=Color.white;
    GUI.Label(new Rect(x+49,y+7,width-61,48),"연료 부족 "+session.business.ledger.Account.Fuel.ToString("0")+"% · M 주유소 안내 · 정차 후 F7",style);y+=70;
   }
   foreach(var notice in visible){
    if(Time.unscaledTime-notice.since>=NoticeSeconds)continue;
    float height=Mathf.Max(58,style.CalcHeight(new GUIContent(notice.text),width-116)+20);Color accent=notice.passenger?CasualUI.Green:CasualUI.Blue;
    Panel(new Rect(x,y,width,height),Color.white);
    CasualUI.RoundedFill(new Rect(x+8,y+8,88,height-16),accent);
    GUI.Label(new Rect(x+15,y+12,76,30),notice.passenger?"만족도":"운전",category);
    style.normal.textColor=notice.positive?CasualUI.Green:CasualUI.Danger;
    GUI.Label(new Rect(x+104,y+10,width-116,height-20),notice.text,style);y+=height+8;
   }
  } finally {GUI.matrix=matrix;GUI.color=color;GUI.backgroundColor=background;GUI.contentColor=content;GUI.depth=depth;GUI.enabled=enabled;}
 }
 // Explicit solid textures keep transient notices opaque regardless of skin backgrounds.
 static void Panel(Rect r,Color c){
  GUI.color=Color.white;CasualUI.RoundedFill(new Rect(r.x,r.y+3,r.width,r.height),new Color(.15f,.23f,.34f,.20f));
  CasualUI.RoundedFill(r,c);
 }
}
