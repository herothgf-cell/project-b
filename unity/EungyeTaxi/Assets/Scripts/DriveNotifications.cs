using System.Collections.Generic;
using UnityEngine;

public class DriveNotifications : MonoBehaviour {
 public DriveSession session;
 class Notice {public string text;public bool positive,passenger;public float since;}
 readonly List<Notice> visible=new List<Notice>();LawLedger ledger;int score,lawCount,lawRevision;bool ready;string observedTrip;PassengerScoreView passenger;
 public bool FuelLow {get{return session.business.enabledCampaign&&session.business.ledger.Account!=null&&session.business.ledger.Account.Fuel<=15;}}
 public string VisibleText {get{return visible.Count==0?"":visible[0].text;}}
 public string[] VisibleTexts {get{return visible.ConvertAll(n=>(n.passenger?"만족도":"운전")+" | "+n.text).ToArray();}}
 void LateUpdate(){
  if(!session||!session.law||!session.business)return;
  var journal=session.GetComponent<DrivingScoreJournal>();if(journal&&!journal.Loaded)return;
  RefreshScores();
 }
 // Values are observed only. Every new notice is immediately visible, without a FIFO delay.
 public void RefreshScores(){
  visible.RemoveAll(n=>Time.unscaledTime-n.since>=3);
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
  if(!ready||session.FullMapOpen||session.ChoiceOpen||session.PracticeMenuOpen||session.law.HistoryOpen||session.business.preparing||session.business.ShowingSummary)return;
  float width=Mathf.Min(460,Screen.width*.38f),x=(Screen.width-width)/2,y=140*CockpitUI.Scale;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true,alignment=TextAnchor.MiddleLeft};GUI.depth=-80;
  if(FuelLow){Panel(new Rect(x,y,width,50),new Color(.23f,.12f,.015f,1));GUI.color=session.business.ledger.Account.Fuel<=5?Color.red:Color.yellow;
   // Pump icon uses geometry so missing font glyphs cannot hide the warning.
   GUI.DrawTexture(new Rect(x+12,y+10,17,29),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x+29,y+16,7,4),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x+33,y+16,4,21),Texture2D.whiteTexture);GUI.color=Color.black;GUI.DrawTexture(new Rect(x+15,y+13,11,9),Texture2D.whiteTexture);GUI.color=Color.white;
   GUI.Label(new Rect(x+47,y+3,width-55,44),"연료 부족 "+session.business.ledger.Account.Fuel.ToString("0")+"% · M 주유소 안내 · 정차 후 F7",style);y+=56;
  }
  foreach(var notice in visible){float age=Time.unscaledTime-notice.since,alpha=Mathf.Clamp01((3-age)/.35f);float height=Mathf.Max(38,style.CalcHeight(new GUIContent(notice.text),width-94)+10);
   Panel(new Rect(x,y,width,height),new Color(.035f,.07f,.09f,.98f*alpha));GUI.color=new Color(notice.passenger?.3f:.4f,notice.passenger?.9f:.7f,notice.passenger?.6f:1,alpha);GUI.Label(new Rect(x+10,y+5,65,height-5),notice.passenger?"만족도":"운전",style);
   GUI.color=new Color(notice.positive?.55f:1,notice.positive?1:.65f,.65f,alpha);GUI.Label(new Rect(x+82,y+5,width-94,height-5),notice.text,style);GUI.color=Color.white;y+=height+5;
  }
 }
 static void Panel(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
}
