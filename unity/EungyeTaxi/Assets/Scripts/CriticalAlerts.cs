using System.Collections.Generic;
using UnityEngine;

// 2026-11-24 play-test round 3: a mission that started and a passenger who threw up went unnoticed, because both
// only appeared as a line in the feed or the passenger card. Important changes now pop up as a card in the
// middle of the screen, like the accident popup but without stopping the drive: mission start, vomiting, and a
// vehicle/driver meter crossing into its warning or critical range. Each card stays 6 seconds of driving time
// (held while a menu, the map or the accident popup is open) and is also written to the feed.
public sealed class CriticalAlerts:MonoBehaviour {
 public DriveSession session;
 public sealed class Alert {public string title,body;public int glyph;public Color accent;public float shown;}
 public const float Seconds=6;
 readonly Queue<Alert> queue=new Queue<Alert>();Alert current;
 readonly int[] level=new int[5];bool primed;int reunionDay=-1;string missionTrip="",vomitTrip="";
 public Alert Current {get{return current;}}
 public int Pending {get{return queue.Count+(current!=null?1:0);}}
 public static readonly string[] Names={"연료","차량 내구도","청결도","포만감","피로"};
 static readonly int[] Glyphs={8,12,14,9,11};

 public void Raise(string title,string body,int glyph,Color accent){
  var a=new Alert{title=title,body=body,glyph=glyph,accent=accent};queue.Enqueue(a);
  var feed=GetComponent<DriveNotifications>();if(feed)feed.Card(title+" · "+body);
 }
 // 0 normal, 1 warning, 2 critical. Same thresholds as the meter colours on the HUD.
 public static int Level(int meter,float v){
  switch(meter){
   case 0:return v<=10?2:v<=25?1:0;
   case 1:return v<=20?2:v<=50?1:0;
   case 2:return v<30?2:0;
   case 3:return v<=10?2:v<=25?1:0;
   default:return v>=90?2:v>=75?1:0;
  }
 }
 // Back to normal needs 5 points of margin, so a value hovering on a threshold does not repeat the card.
 static bool Recovered(int meter,float v,int from){
  if(meter==4)return v<(from==2?90:75)-5;
  float edge=meter==0||meter==3?(from==2?10:25):meter==1?(from==2?20:50):30;return v>edge+5;
 }
 static string Body(int meter,int lv,float v){
  switch(meter){
   case 0:return lv==2?"곧 멈춰요 · 가까운 주유소에 들르세요":"주유소에서 주유하세요";
   case 1:return v<=0?"운행 불가 · 공업사 수리가 필요해요":lv==2?"수리 필요 · 공업사에 들르세요":"수리 권장 · 공업사에서 고칠 수 있어요";
   case 2:return "세차 전까지 새 손님을 태울 수 없어요";
   case 3:return "배고프면 피로가 빨리 쌓여요 · 식당에 들르세요";
   default:return lv==2?"졸음 위험 · 쉬거나 퇴근하세요":"피로가 높아요 · 쉬어 가세요";
  }
 }
 void Update(){
  if(!session||!session.StartupReady||session.IsSimulationBot||session.business==null)return;
  var e=session.business.ledger;if(e==null||e.Driver==null||e.Account==null||e.Care==null)return;
  float[] values={e.Account.Fuel,e.Care.Damage.Durability,e.Care.Cleanliness,(float)e.Driver.satiety,(float)e.Driver.fatigue};
  for(int i=0;i<5;i++){int lv=Level(i,values[i]);
   if(!primed){level[i]=lv;continue;}
   if(lv>level[i]){level[i]=lv;Raise(Names[i]+" "+Mathf.RoundToInt(values[i]),Body(i,lv,values[i]),Glyphs[i],lv==2?CasualUI.Danger:CasualUI.Amber);}
   else if(lv<level[i]&&Recovered(i,values[i],level[i]))level[i]=lv;}
  primed=true;
  var j=session.jobs;string trip=session.law?session.law.tripId:"";
  if(j&&j.trip.State==TripState.OnTrip&&j.current!=null&&j.current.mission!=MissionRules.None&&trip.Length>0&&trip!=missionTrip){
   missionTrip=trip;Raise("미션 시작 · "+MissionRules.Label(j.current.mission),MissionRules.Goal(j.current.mission,j.current)+(j.current.missionBonus>0?" · 성공 시 +"+j.current.missionBonus.ToString("N0")+"원":""),5,CasualUI.Blue);}
  if(j&&j.DrunkRide!=null&&j.DrunkRide.Vomited&&trip.Length>0&&trip!=vomitTrip){vomitTrip=trip;Raise("손님이 토했어요","청결도가 떨어졌어요 · 세차 전까지 새 손님을 태울 수 없어요",2,CasualUI.Danger);}
  // The student's reunion window opening is a promise coming due: tell the driver once that day.
  var book=session.business.Logbook;if(j&&book!=null&&book.studentPromised&&!book.studentReturned&&book.studentDay>0&&e.Day==book.studentDay+2&&reunionDay!=e.Day&&book.ReunionKind(e.Day,j.StudentMinute)==1){reunionDay=e.Day;Raise("학생과 약속 시간","지금 "+(string.IsNullOrEmpty(book.studentPlace)?"약속 장소":book.studentPlace)+"에서 학생이 기다려요",18,CasualUI.Green);}
  if(current==null&&queue.Count>0&&!Blocked)current=queue.Dequeue();
  if(current!=null&&!Blocked&&Time.timeScale>0){current.shown+=Time.unscaledDeltaTime;if(current.shown>=Seconds)current=null;}
 }
 bool Blocked {get{return session.paused||session.SettingsOpen||session.FullMapOpen||session.ChoiceOpen||session.business.ShowingSummary||session.business.preparing||session.business.LogbookOpen||session.services.open||session.law.HistoryOpen||session.accident&&session.accident.Locked;}}
 public void Dismiss(){current=null;}
 public static Rect Bounds(float w){return new Rect(w/2-280,178,560,118);}
 void OnGUI(){
  if(current==null||Blocked||!session)return;var a=current;
  var prior=GUI.matrix;int depth=GUI.depth;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));GUI.depth=-25;
  try{float w=Screen.width/scale;var r=Bounds(w);
   CasualUI.Panel(r);CasualUI.Header(new Rect(r.x+5,r.y+5,r.width-10,42),a.title,a.glyph,a.accent);
   var body=new GUIStyle(DrivingHudArt.Text(17,CasualUI.Ink,true)){wordWrap=true};GUI.Label(new Rect(r.x+18,r.y+52,r.width-36,44),a.body,body);
   if(Event.current.type==EventType.Repaint){var old=GUI.color;GUI.color=CasualUI.Track;GUI.DrawTexture(new Rect(r.x+18,r.yMax-12,r.width-36,4),Texture2D.whiteTexture);GUI.color=a.accent;GUI.DrawTexture(new Rect(r.x+18,r.yMax-12,(r.width-36)*Mathf.Clamp01(1-a.shown/Seconds),4),Texture2D.whiteTexture);GUI.color=old;}
   if(GUI.Button(new Rect(r.xMax-46,r.y+12,30,28),new GUIContent("",""),GUIStyle.none))Dismiss();HudGlyph.Draw(new Rect(r.xMax-42,r.y+15,22,22),19,Color.white);
  }finally{GUI.matrix=prior;GUI.depth=depth;}
 }
}
