using UnityEngine;

// Compact vehicle-style gauges; geometry keeps icons readable without emoji fonts.
public static class DriverStatusGauges {
 static GUIStyle caption,number,warning;
 public static void Draw(Rect rect,DriverCondition driver,CampaignAccount account,int cleanliness=100,int durability=100){
  if(driver==null||account==null)return;
  if(caption==null){caption=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=13,alignment=TextAnchor.MiddleCenter};number=new GUIStyle(caption){font=KoreanText.BoldFont,fontSize=14};warning=new GUIStyle(caption){font=KoreanText.BoldFont,fontSize=11};caption.normal.textColor=number.normal.textColor=CasualUI.Ink;warning.normal.textColor=CasualUI.Danger;}
  caption.fontSize=UiReadability.Font(13);number.fontSize=UiReadability.Font(14);warning.fontSize=UiReadability.Font(11);float width=(rect.width-32)/5;
  DrawGauge(new Rect(rect.x,rect.y,width,rect.height),"포만감",(float)driver.satiety,0,CasualUI.Green);
  DrawGauge(new Rect(rect.x+width+8,rect.y,width,rect.height),"피로",(float)driver.fatigue,1,new Color(.42f,.27f,.73f));
  DrawGauge(new Rect(rect.x+(width+8)*2,rect.y,width,rect.height),"연료",account.Fuel,2,CasualUI.Amber);
  DrawGauge(new Rect(rect.x+(width+8)*3,rect.y,width,rect.height),"청결도",cleanliness,3,CasualUI.Blue);
  DrawGauge(new Rect(rect.x+(width+8)*4,rect.y,width,rect.height),"내구도",durability,4,CasualUI.Muted);
 }
 static void DrawGauge(Rect r,string text,float value,int icon,Color normal){
  CasualUI.RoundedFill(new Rect(r.x-3,r.y,r.width+6,r.height),CasualUI.Soft);
  value=Mathf.Clamp(value,0,100);Color tint=icon==4?(value<=20?new Color(1,.4f,.3f):value<=50?CasualUI.Amber:normal):(icon==1?value>=75:icon==3?value<30:value<=25)?new Color(1,.4f,.3f):normal;
  var matrix=GUI.matrix;var pivot=new Vector2(r.center.x,r.y+15);Icon(pivot,icon,tint);GUI.matrix=matrix;
  GUI.Label(new Rect(r.x,r.y+31,r.width,22),text,caption);
  GUI.Label(new Rect(r.x,r.y+49,r.width,25),new GUIContent(Mathf.RoundToInt(value).ToString(),"0~100 상태 게이지"),number);
  Fill(new Rect(r.x,r.y+78,r.width,5),CasualUI.Track);Fill(new Rect(r.x,r.y+78,r.width*value/100,5),tint);
  if(icon==3&&value<30)GUI.Label(new Rect(r.x-2,r.y+86,r.width+4,24),"세차 필요",warning);
  if(icon==4&&value<=50)GUI.Label(new Rect(r.x-2,r.y+86,r.width+4,24),value==0?"운행 불가":value<=20?"수리 필요":"수리 권장",warning);
 }
 static void Icon(Vector2 p,int kind,Color c){
  int glyph=kind==0?9:kind==1?11:kind==2?8:kind==3?14:12;
  HudGlyph.DrawBadge(new Rect(p.x-16,p.y-16,32,32),glyph,c);
 }
 static void Fill(Rect r,Color color){var previous=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=previous;}
}
