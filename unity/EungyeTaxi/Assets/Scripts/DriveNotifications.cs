using System.Collections.Generic;
using UnityEngine;

public class DriveNotifications : MonoBehaviour {
 public DriveSession session;
 class Notice {public string text;public bool positive,passenger;public float since;}
 readonly Queue<Notice> queue=new Queue<Notice>();Notice current;LawLedger ledger;int score;bool ready;string observedTrip;PassengerScoreView passenger;
 public bool FuelLow {get{return session.business.enabledCampaign&&session.business.ledger.Account!=null&&session.business.ledger.Account.Fuel<=15;}}
 public string VisibleText {get{return current==null?"":current.text;}}
 void LateUpdate(){
  if(!session||!session.law||!session.business)return;
  var journal=session.GetComponent<DrivingScoreJournal>();if(journal&&!journal.Loaded)return;
  RefreshScores();
 }
 // Observes the same resolved values as the gauges; never changes gameplay or re-awards points.
 public void RefreshScores(){
  var jobs=session.jobs;var view=jobs.ScoreView;string trip=jobs.HasPassengerScore?session.law.tripId:null;
  if(!ready||ledger!=session.law.ledger){ledger=session.law.ledger;score=ledger.TotalScore;observedTrip=trip;passenger=view;queue.Clear();current=null;ready=true;return;}
  if(trip!=observedTrip){ClearPassengerNotices();observedTrip=trip;passenger=view;if(trip!=null)Push("승객 탑승 · "+view.Caption+" · 청결 +"+view.clean+" 포함",true,true);}
  else if(trip!=null){int delta=view.value-passenger.value;if(delta!=0){string reason="";if(view.drive!=passenger.drive)reason+="주행 "+Delta(view.drive-passenger.drive)+" ("+session.manners.lastEvent+") ";if(view.request!=passenger.request)reason+="요청 이행 "+Delta(view.request-passenger.request)+" ";if(view.time!=passenger.time)reason+="도착 시간 "+Delta(view.time-passenger.time);Push("승객 만족도 "+Delta(delta)+" → "+view.value+" / 요구 "+view.required+" · "+reason,delta>0,true);}passenger=view;}
  int next=ledger.TotalScore;if(next!=score){var events=ledger.Events;string reason=events.Length>0?events[events.Length-1].action:"";Push("운행 점수 "+Delta(next-score)+" → "+next+" · "+(next>score?"준수 확인":reason),next>score);score=next;}
  if(current!=null&&Time.unscaledTime-current.since>=4)current=null;
  if(current==null&&queue.Count>0){current=queue.Dequeue();current.since=Time.unscaledTime;}
 }
 static string Delta(int n){return (n>0?"+":"")+n+"점";}
 void ClearPassengerNotices(){if(current!=null&&current.passenger)current=null;int count=queue.Count;while(count-->0){var item=queue.Dequeue();if(!item.passenger)queue.Enqueue(item);}}
 void Push(string text,bool good,bool passenger=false){if(queue.Count>=4)queue.Dequeue();queue.Enqueue(new Notice{text=text,positive=good,passenger=passenger});}
 void OnGUI(){
  if(!ready||session.FullMapOpen||session.ChoiceOpen||session.PracticeMenuOpen||session.law.HistoryOpen||session.business.preparing||session.business.ShowingSummary)return;
  float width=Mathf.Min(460,Screen.width*.38f),x=(Screen.width-width)/2,y=140*CockpitUI.Scale;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true,alignment=TextAnchor.MiddleLeft};GUI.depth=-80;
  if(FuelLow){Panel(new Rect(x,y,width,50),new Color(.23f,.12f,.015f,1));GUI.color=session.business.ledger.Account.Fuel<=5?Color.red:Color.yellow;
   // Pump icon uses geometry so missing font glyphs cannot hide the warning.
   GUI.DrawTexture(new Rect(x+12,y+10,17,29),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x+29,y+16,7,4),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x+33,y+16,4,21),Texture2D.whiteTexture);GUI.color=Color.black;GUI.DrawTexture(new Rect(x+15,y+13,11,9),Texture2D.whiteTexture);GUI.color=Color.white;
   GUI.Label(new Rect(x+47,y+3,width-55,44),"연료 부족 "+session.business.ledger.Account.Fuel.ToString("0")+"% · F7 주유소 안내",style);y+=56;
  }
  if(current==null)return;float age=Time.unscaledTime-current.since,alpha=Mathf.Clamp01(age/.25f)*Mathf.Clamp01((4-age)/.5f);y-=24*(1-alpha);
  float height=Mathf.Max(52,style.CalcHeight(new GUIContent(current.text),width-24)+12);Panel(new Rect(x,y,width,height),new Color(.035f,.07f,.09f,.95f*alpha));GUI.color=new Color(current.positive?.5f:1,current.positive?1:.7f,.6f,alpha);GUI.Label(new Rect(x+12,y+3,width-24,height-6),current.text,style);GUI.color=Color.white;
 }
 static void Panel(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
}
