using UnityEngine;

// Reads the existing trip/vehicle state; it never changes fare or deadlines.
public static class CockpitPresentation {
 public static string TaxiStatus(TripState state){return state==TripState.OnTrip?"":state==TripState.ToPickup?"예약":"빈차";}
 public static string Destination(string stop){return "목적지 ("+(stop??"")+")";}
 public static string Deadline(float remaining){return float.IsNaN(remaining)||float.IsInfinity(remaining)?"—":CockpitUI.Clock(Mathf.Max(0,remaining));}
 static void Indicator(Rect r,bool left,Color color){
  var previous=GUI.color;GUI.color=color;float unit=r.width/24f;
  GUI.DrawTexture(new Rect(r.x+5*unit,r.y+9*unit,14*unit,6*unit),Texture2D.whiteTexture);
  for(int i=0;i<9;i++){float x=left?r.x+(2+i)*unit:r.x+(21-i)*unit;GUI.DrawTexture(new Rect(x,r.y+(12-i)*unit,unit,(i*2+1)*unit),Texture2D.whiteTexture);}
  GUI.color=previous;
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
  var color=seconds<=30?CasualUI.Danger:CasualUI.Ink;var previous=GUI.color;
  // A small timer face and a large countdown make this a deadline, not a wait.
  var center=new Vector2(r.x+13,r.y+17);for(int i=0;i<24;i++){float angle=i*Mathf.PI/12;var p=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*10;GUI.color=color;GUI.DrawTexture(new Rect(p.x,p.y,2,2),Texture2D.whiteTexture);}
  GUI.color=color;GUI.DrawTexture(new Rect(center.x,center.y-7,2,8),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(center.x,center.y,6,2),Texture2D.whiteTexture);GUI.color=previous;
  GUI.Label(new Rect(r.x+34,r.y,r.width-118,36),Deadline(seconds),new GUIStyle(text){font=KoreanText.BoldFont,fontSize=UiReadability.Font(26),normal={textColor=color}});
  GUI.Label(new Rect(r.x+r.width-84,r.y+7,84,26),"안에 도착",new GUIStyle(text){fontSize=UiReadability.Font(12)});
 }
}
