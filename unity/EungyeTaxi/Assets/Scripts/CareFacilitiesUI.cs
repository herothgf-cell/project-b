using UnityEngine;

// Play feedback round 8 (2026-11-26, mock docs/ui/2026-11-26-care-facilities/mock-care-facilities.png): the police
// box and the emergency room are life facilities too. They get their own tabs and cards in the map's life-facility
// panel (distance, what you do there, route), and on the maps a larger coloured pin (navy shield / red cross) with a
// name label that is always shown, instead of a small blue icon like every other facility.
public static class CareFacilitiesUI {
 public const int Police=21,Emergency=22;
 public static readonly Color PoliceColor=new Color(.086f,.204f,.47f),EmergencyColor=new Color(.78f,.14f,.16f);
 public static bool IsCare(int glyph){return glyph==Police||glyph==Emergency;}
 public static Color ColorOf(int glyph){return glyph==Police?PoliceColor:EmergencyColor;}
 public static string MapLabel(int glyph){return glyph==Police?"파출소 · "+(LostItemRules.Police!=null?LostItemRules.Police.name:""):"응급실 · "+EmergencyRules.HospitalName;}
 public static void Pin(Vector2 p,int glyph,float size){
  if(Event.current==null||Event.current.type!=EventType.Repaint)return;
  HudGlyph.Draw(new Rect(p.x-size*.5f-3,p.y-size*.5f-3,size+6,size+6),15,Color.white);
  if(glyph==Police)HudGlyph.Draw(new Rect(p.x-size*.5f,p.y-size*.5f,size,size),15,PoliceColor);else CasualUI.RoundedFill(new Rect(p.x-size*.5f,p.y-size*.5f,size,size),EmergencyColor);
  float g=size*.62f;HudGlyph.Draw(new Rect(p.x-g*.5f,p.y-g*.5f,g,g),glyph,Color.white);
 }
 public static Rect LabelRect(Vector2 p,int glyph,float size){var style=LabelStyle();float w=style.CalcSize(new GUIContent(MapLabel(glyph))).x+22;return new Rect(p.x-w*.5f,p.y+size*.5f+6,w,28);}
 public static void Label(Vector2 p,int glyph,float size){if(Event.current.type!=EventType.Repaint)return;var r=LabelRect(p,glyph,size);CasualUI.RoundedFill(r,ColorOf(glyph));GUI.Label(r,MapLabel(glyph),LabelStyle());}
 static GUIStyle labelStyle;
 static GUIStyle LabelStyle(){if(labelStyle==null){labelStyle=new GUIStyle(DrivingHudArt.Text(15,Color.white,true)){alignment=TextAnchor.MiddleCenter,wordWrap=false};}return labelStyle;}

 // ---- life-facility panel cards ----
 public static float CardHeight(DriveSession s,int kind){return kind==Police?330:262;}
 public static void Card(DriveSession s,Rect r,int kind,System.Action close){
  var lost=s.lostItems;if(!lost)return;bool police=kind==Police;var color=ColorOf(kind);
  var stop=police?lost.PoliceStop:lost.HospitalApproach;
  CasualUI.Panel(r);var band=new Rect(r.x+4,r.y+4,r.width-8,46);CasualUI.RoundedFill(band,color);
  GUI.Label(new Rect(band.x+14,band.y,band.width-150,band.height),police?(LostItemRules.Police!=null?LostItemRules.Police.name:"파출소"):EmergencyRules.HospitalName,DrivingHudArt.Text(19,Color.white,true));
  if(police&&lost.HasItem){var b=new Rect(band.xMax-132,band.y+9,124,28);CasualUI.RoundedFill(b,new Color(1f,.84f,.35f));GUI.Label(b,"맡길 물건 있음",new GUIStyle(DrivingHudArt.Text(13,CasualUI.Ink,true)){alignment=TextAnchor.MiddleCenter});}
  float d=stop==null?float.PositiveInfinity:s.jobs.nav.DistanceFromVehicle(stop);float y=r.y+62;
  HudGlyph.Draw(new Rect(r.x+14,y+3,20,20),3,CasualUI.Ink);
  GUI.Label(new Rect(r.x+40,y,r.width-50,26),float.IsInfinity(d)?"경로 없음":(d>=1000?(d/1000).ToString("0.0")+"km":d.ToString("0")+"m")+" · 약 "+Mathf.Max(1,Mathf.CeilToInt(d/8f/60f))+"분",DrivingHudArt.Text(17,CasualUI.Ink,true));y+=34;
  var muted=new GUIStyle(DrivingHudArt.Text(14,CasualUI.Muted,false)){wordWrap=true};
  if(police){
   GUI.Label(new Rect(r.x+14,y,r.width-28,24),"분실물 맡기기 · 사례 +"+LostItemRules.PoliceReward.ToString("N0")+"원",DrivingHudArt.Text(16,CasualUI.Green,true));y+=30;
   string item=lost.HasItem?"뒷좌석 · "+LostItemRules.Names[lost.Item]+(lost.State==LostItems.Returning?" (주인 대기 "+Mathf.CeilToInt(lost.OwnerSecondsLeft/60f)+"분 남음)":""):"지금은 맡길 물건이 없어요";
   GUI.Label(new Rect(r.x+14,y,r.width-28,22),item,muted);y+=26;
   GUI.Label(new Rect(r.x+14,y,r.width-28,22),"앞 정차 칸에 서서 F → 맡기기",muted);y+=30;
  }else{
   GUI.Label(new Rect(r.x+14,y,r.width-28,22),"응급 손님은 앞 정차 칸에 내려 드려요",muted);y+=26;
   GUI.Label(new Rect(r.x+14,y,r.width-28,22),"게임 전용 · 실제 가장 가까운 곳: 신천연합병원",muted);y+=30;
  }
  bool onTrip=s.jobs.trip.State==TripState.OnTrip;bool enabled=GUI.enabled;GUI.enabled=enabled&&!onTrip&&stop!=null&&!float.IsInfinity(d);
  var style=police&&lost.HasItem?CasualUI.Action(17):KoreanText.Button(17);
  if(GUI.Button(new Rect(r.x+14,y,r.width-28,46),onTrip?"운송을 마친 뒤 길 안내":"길 안내",style)){if(police?lost.GuidePolice():lost.GuideHospital())close?.Invoke();}
  GUI.enabled=enabled;y+=54;
  if(police&&LostItemRules.Police!=null)GUI.Label(new Rect(r.x+14,y,r.width-28,22),LostItemRules.Police.address+" · 실제 위치",new GUIStyle(muted){fontSize=12});
 }
}
