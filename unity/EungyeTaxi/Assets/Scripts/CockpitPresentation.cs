using UnityEngine;

// Reads the existing trip/vehicle state; it never changes fare or deadlines.
public static class CockpitPresentation {
 public static string TaxiStatus(TripState state){return state==TripState.OnTrip?"":state==TripState.ToPickup?"예약":"빈차";}
 public static string Destination(string stop){return "목적지 ("+(stop??"")+")";}
 public static string Deadline(float remaining){return float.IsNaN(remaining)||float.IsInfinity(remaining)?"—":CockpitUI.Clock(Mathf.Max(0,remaining));}
 static void Indicator(Rect r,bool left,Color color){
  HudGlyph.Draw(r,left?16:17,color);
 }
 public static void DrawDriveState(Rect r,DriveSession session){
  var centered=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=UiReadability.Font(20),alignment=TextAnchor.MiddleCenter,normal={textColor=CasualUI.Ink}};
  CasualUI.RoundedFill(new Rect(r.x,r.y,40,30),CasualUI.Soft);GUI.Label(new Rect(r.x,r.y,40,30),session.taxi.Speed<-.2f?"R":"D",centered);
  CasualUI.RoundedFill(new Rect(r.x+50,r.y,112,30),CasualUI.Soft);GUI.Label(new Rect(r.x+50,r.y,112,30),session.assisted.Active?"조향 보조":"수동",new GUIStyle(centered){font=KoreanText.Font,fontSize=UiReadability.Font(13)});
  var signal=session.signals;bool lit=signal&&signal.Lit;var off=CasualUI.Track;
  Indicator(new Rect(r.x+r.width-72,r.y+3,24,24),true,lit&&(signal.Hazards||signal.direction<0)?CasualUI.Green:off);
  Indicator(new Rect(r.x+r.width-28,r.y+3,24,24),false,lit&&(signal.Hazards||signal.direction>0)?CasualUI.Green:off);
 }
 public static void DrawDeadline(Rect r,float seconds,GUIStyle text){
  var color=seconds<=30?CasualUI.Danger:CasualUI.Ink;
  HudGlyph.Draw(new Rect(r.x,r.y+3,26,26),18,color);
  GUI.Label(new Rect(r.x+34,r.y,r.width-118,36),Deadline(seconds),new GUIStyle(text){font=KoreanText.BoldFont,fontSize=UiReadability.Font(26),normal={textColor=color}});
  GUI.Label(new Rect(r.x+r.width-84,r.y+7,84,26),"안에 도착",new GUIStyle(text){fontSize=UiReadability.Font(12)});
 }
}
