using System.Collections.Generic;
using UnityEngine;

public class DriveNotifications : MonoBehaviour {
 public DriveSession session;
 class Notice {public string text;public bool positive;public float since;}
 readonly Queue<Notice> queue=new Queue<Notice>();Notice current;LawLedger ledger;int manner,score;bool ready;
 public bool FuelLow {get{return session.business.enabledCampaign&&session.business.ledger.Account!=null&&session.business.ledger.Account.Fuel<=15;}}
 public string VisibleText {get{return current==null?"":current.text;}}
 void LateUpdate(){
  if(!session||!session.law||!session.business)return;
  var journal=session.GetComponent<DrivingScoreJournal>();if(journal&&!journal.Loaded)return;
  if(!ready||ledger!=session.law.ledger){ledger=session.law.ledger;manner=session.manners.ledger.Score;score=ledger.TotalScore;queue.Clear();current=null;ready=true;return;}
  int next=session.manners.ledger.Score;
  if(next!=manner){Push(next>manner&&session.manners.lastEvent.Contains("100점으로 시작")?"새 승객 · 만족도 100점으로 초기화":"승객 만족도 "+Delta(next-manner)+" · "+session.manners.lastEvent,next>manner);manner=next;}
  next=ledger.TotalScore;if(next!=score){var events=ledger.Events;string reason=events.Length>0?events[events.Length-1].action:"";Push("운행 점수 "+Delta(next-score)+" · "+(next>score?"준수 확인":reason),next>score);score=next;}
  if(current!=null&&Time.unscaledTime-current.since>=4)current=null;
  if(current==null&&queue.Count>0){current=queue.Dequeue();current.since=Time.unscaledTime;}
 }
 static string Delta(int n){return (n>0?"+":"")+n+"점";}
 void Push(string text,bool good){if(queue.Count>=4)queue.Dequeue();queue.Enqueue(new Notice{text=text,positive=good});}
 void OnGUI(){
  if(!ready||session.FullMapOpen||session.ChoiceOpen||session.PracticeMenuOpen||session.law.HistoryOpen||session.business.preparing||session.business.ShowingSummary)return;
  float width=Mathf.Min(460,Screen.width*.38f),x=(Screen.width-width)/2,y=140*CockpitUI.Scale;
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true,alignment=TextAnchor.MiddleLeft};GUI.depth=-80;
  if(FuelLow){Panel(new Rect(x,y,width,50),new Color(.23f,.12f,.015f,.96f));GUI.color=session.business.ledger.Account.Fuel<=5?Color.red:Color.yellow;
   // Pump icon uses geometry so missing font glyphs cannot hide the warning.
   GUI.DrawTexture(new Rect(x+12,y+10,17,29),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x+29,y+16,7,4),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x+33,y+16,4,21),Texture2D.whiteTexture);GUI.color=Color.black;GUI.DrawTexture(new Rect(x+15,y+13,11,9),Texture2D.whiteTexture);GUI.color=Color.white;
   GUI.Label(new Rect(x+47,y+3,width-55,44),"연료 부족 "+session.business.ledger.Account.Fuel.ToString("0")+"% · F7 주유소 안내",style);y+=56;
  }
  if(current==null)return;float age=Time.unscaledTime-current.since,alpha=Mathf.Clamp01(age/.25f)*Mathf.Clamp01((4-age)/.5f);y-=24*(1-alpha);
  Panel(new Rect(x,y,width,52),new Color(.035f,.07f,.09f,.95f*alpha));GUI.color=new Color(current.positive?.5f:1,current.positive?1:.7f,.6f,alpha);GUI.Label(new Rect(x+12,y+3,width-24,46),current.text,style);GUI.color=Color.white;
 }
 static void Panel(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
}
