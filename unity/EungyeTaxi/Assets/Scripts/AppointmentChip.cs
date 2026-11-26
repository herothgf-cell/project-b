using System.Collections.Generic;
using UnityEngine;

// 2026-11-24 play-test round 3: a promise made earlier (the student's "모레 이 시간" reunion, or the next call
// reserved from the drop-off point) was only in the logbook or the call board. The driving screen now shows
// each open promise as a chip under the minimap / ride card, with how long until it is due.
public static class AppointmentChip {
 public struct Item {public string text;public int glyph;public Color accent;public bool now;}
 public static string Clock(int minute){return (minute/60%24).ToString("00")+":"+(minute%60).ToString("00");}
 public static List<Item> Items(DriveSession s){
  var list=new List<Item>();if(!s||s.business==null||s.jobs==null||s.simulationMatch)return list;
  var b=s.business;var book=b.Logbook;int day=b.ledger.Day,minute=s.jobs.StudentMinute;
  if(book!=null&&book.studentDay>0&&book.studentPromised&&!book.studentReturned){
   int due=book.studentDay+2;string place=string.IsNullOrEmpty(book.studentPlace)?"약속 장소":book.studentPlace;
   if(day<due)list.Add(new Item{text="학생과 약속 · "+due+"일차 "+Clock(book.studentMinute)+" · "+place,glyph=18,accent=CasualUI.Blue});
   else if(day==due){int kind=book.ReunionKind(day,minute);
    if(kind==0){int left=book.studentMinute-minute;list.Add(new Item{text="학생과 약속 "+Clock(book.studentMinute)+" · "+place+" · "+(left>=60?left/60+"시간 ":"")+(left%60)+"분 뒤",glyph=18,accent=left<=60?CasualUI.Amber:CasualUI.Blue});}
    else if(kind==1)list.Add(new Item{text="학생과 약속 · 지금 · "+place+"에서 기다려요",glyph=18,accent=CasualUI.Green,now=true});}
  }
  var chained=s.jobs.ChainedOffer;
  if(chained!=null)list.Add(new Item{text="예약 호출 · "+KoreanText.Stop(s.jobs.stops[chained.pickup].name)+" → "+KoreanText.Stop(s.jobs.stops[chained.drop].name),glyph=3,accent=CasualUI.Blue});
  return list;
 }
 // Play feedback round 7 (2026-11-26): where the student waits, as a pin on the full map and the minimap (the chip
 // alone said only "수인로 서쪽"). Shown from the promise until the reunion is over.
 public static bool StudentPin(DriveSession s,out Vector3 position,out string label,out bool now){
  position=Vector3.zero;label="";now=false;if(!s||s.business==null||s.jobs==null||s.simulationMatch)return false;
  var book=s.business.Logbook;if(book==null||book.studentDay<=0||!book.studentPromised||book.studentReturned||string.IsNullOrEmpty(book.studentStop))return false;
  int day=s.business.ledger.Day,due=book.studentDay+2;if(day>due)return false;
  var j=s.jobs;int index=j.Catalog!=null?j.Catalog.IndexOf(book.studentStop):-1;if(index<0||index>=j.stops.Length)return false;
  position=j.stops[index].Kerb;now=day==due&&book.ReunionKind(day,j.StudentMinute)==1;
  label=now?"학생 · 지금 기다려요":"학생 약속 · "+due+"일차 "+Clock(book.studentMinute);return true;
 }
 public static int StudentPortrait(DriveSession s){return PassengerPresentation.Persona(PassengerDialogueUI.StudentIdentity(s.business.Logbook)).portrait;}
 public static void DrawPin(Vector2 p,string label,bool now,float size,bool withLabel,int portrait){
  if(Event.current==null||Event.current.type!=EventType.Repaint)return;
  var ring=now?CasualUI.Green:CasualUI.Blue;HudGlyph.Draw(new Rect(p.x-size*.5f-3,p.y-size*.5f-3,size+6,size+6),15,ring);
  CasualUI.Portrait(new Rect(p.x-size*.5f,p.y-size*.5f,size,size),portrait);
  if(!withLabel)return;var style=new GUIStyle(DrivingHudArt.Text(13,Color.white,true)){alignment=TextAnchor.MiddleCenter};float w=style.CalcSize(new GUIContent(label)).x+18;
  var r=new Rect(p.x-w*.5f,p.y+size*.5f+5,w,24);CasualUI.RoundedFill(r,ring);GUI.Label(r,label,style);
 }
 // Drawn in CockpitUI's scaled HUD space, in the right column.
 public static void Draw(DriveSession s,float w){
  var items=Items(s);if(items.Count==0)return;float x=w-312,y=s.jobs.Active?540:382;
  var style=new GUIStyle(DrivingHudArt.Text(13,CasualUI.Ink,true)){wordWrap=true};
  foreach(var it in items){float hgt=Mathf.Clamp(style.CalcHeight(new GUIContent(it.text),236)+12,38,58);var r=new Rect(x,y,288,hgt);CasualUI.Panel(r,it.now?new Color(.90f,.98f,.92f):CasualUI.Card);
   if(Event.current.type==EventType.Repaint){var old=GUI.color;GUI.color=it.accent;GUI.DrawTexture(new Rect(r.x+2,r.y+6,4,r.height-12),Texture2D.whiteTexture);GUI.color=old;}HudGlyph.Draw(new Rect(r.x+14,r.y+(hgt-22)/2,22,22),it.glyph,it.accent);
   GUI.Label(new Rect(r.x+44,r.y+2,r.width-52,r.height-4),new GUIContent(it.text,it.text),style);
   // Round 7: clicking the student chip opens the map on the meeting place.
   Vector3 sp;string sl;bool sn;if(it.glyph==18&&GUI.Button(r,GUIContent.none,GUIStyle.none)&&StudentPin(s,out sp,out sl,out sn)){var map=s.GetComponent<FullMapOverlay>();if(map&&map.Open())map.FocusWorld(sp);}
   y+=hgt+6;}
 }
}
