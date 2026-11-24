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
 // Drawn in CockpitUI's scaled HUD space, in the right column.
 public static void Draw(DriveSession s,float w){
  var items=Items(s);if(items.Count==0)return;float x=w-312,y=s.jobs.Active?540:382;
  var style=new GUIStyle(DrivingHudArt.Text(13,CasualUI.Ink,true)){wordWrap=true};
  foreach(var it in items){float hgt=Mathf.Clamp(style.CalcHeight(new GUIContent(it.text),236)+12,38,58);var r=new Rect(x,y,288,hgt);CasualUI.Panel(r,it.now?new Color(.90f,.98f,.92f):CasualUI.Card);
   if(Event.current.type==EventType.Repaint){var old=GUI.color;GUI.color=it.accent;GUI.DrawTexture(new Rect(r.x+2,r.y+6,4,r.height-12),Texture2D.whiteTexture);GUI.color=old;}HudGlyph.Draw(new Rect(r.x+14,r.y+(hgt-22)/2,22,22),it.glyph,it.accent);
   GUI.Label(new Rect(r.x+44,r.y+2,r.width-52,r.height-4),new GUIContent(it.text,it.text),style);y+=hgt+6;}
 }
}
