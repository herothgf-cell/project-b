using UnityEngine;

// Compact vehicle-style gauges; geometry keeps icons readable without emoji fonts.
public static class DriverStatusGauges {
 static GUIStyle caption,number,warning;
 public static void Draw(Rect rect,DriverCondition driver,CampaignAccount account,int cleanliness=100,int durability=100){
  if(driver==null||account==null)return;
  if(caption==null){caption=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=12,alignment=TextAnchor.MiddleCenter};number=new GUIStyle(caption){font=KoreanText.BoldFont,fontSize=12};warning=new GUIStyle(caption){font=KoreanText.BoldFont,fontSize=9};caption.normal.textColor=number.normal.textColor=Color.white;warning.normal.textColor=new Color(1,.55f,.35f);}
  float width=(rect.width-32)/5;
  DrawGauge(new Rect(rect.x,rect.y,width,rect.height),"포만감",(float)driver.satiety,0,new Color(.35f,.83f,.54f));
  DrawGauge(new Rect(rect.x+width+8,rect.y,width,rect.height),"피로",(float)driver.fatigue,1,new Color(.64f,.65f,1));
  DrawGauge(new Rect(rect.x+(width+8)*2,rect.y,width,rect.height),"연료",account.Fuel,2,new Color(1,.76f,.28f));
  DrawGauge(new Rect(rect.x+(width+8)*3,rect.y,width,rect.height),"청결도",cleanliness,3,new Color(.4f,.85f,1));
  DrawGauge(new Rect(rect.x+(width+8)*4,rect.y,width,rect.height),"내구도",durability,4,new Color(.72f,.84f,.95f));
 }
 static void DrawGauge(Rect r,string text,float value,int icon,Color normal){
  value=Mathf.Clamp(value,0,100);Color tint=icon==4?(value<=20?new Color(1,.4f,.3f):value<=50?new Color(1,.76f,.28f):normal):(icon==1?value>=75:icon==3?value<30:value<=25)?new Color(1,.4f,.3f):normal;
  Icon(new Vector2(r.center.x,r.y+10),icon,tint);
  GUI.Label(new Rect(r.x,r.y+20,r.width,17),text,caption);
  GUI.Label(new Rect(r.x,r.y+36,r.width,19),Mathf.RoundToInt(value)+"/100",number);
  Fill(new Rect(r.x,r.y+56,r.width,4),new Color(.16f,.22f,.26f));Fill(new Rect(r.x,r.y+56,r.width*value/100,4),tint);
  if(icon==3&&value<30)GUI.Label(new Rect(r.x-2,r.y+60,r.width+4,12),"세차 필요",warning);
  if(icon==4&&value<=50)GUI.Label(new Rect(r.x-2,r.y+60,r.width+4,12),value==0?"운행 불가":value<=20?"수리 필요":"수리 권장",warning);
 }
 static void Icon(Vector2 p,int kind,Color c){
  if(kind==0){ // Bowl and chopsticks.
   Fill(new Rect(p.x-8,p.y-2,16,3),c);Fill(new Rect(p.x-6,p.y+1,12,5),c);Fill(new Rect(p.x-3,p.y+6,6,2),c);
   Fill(new Rect(p.x-4,p.y-9,2,6),c);Fill(new Rect(p.x+2,p.y-9,2,6),c);
  }else if(kind==1){ // Resting head and pillow.
   Fill(new Rect(p.x-8,p.y-3,6,6),c);Fill(new Rect(p.x,p.y,9,4),c);Fill(new Rect(p.x-9,p.y+5,19,3),c);Fill(new Rect(p.x-9,p.y+2,2,8),c);
  }else if(kind==2){ // Fuel pump, window, nozzle and hose.
   Fill(new Rect(p.x-7,p.y-9,11,17),c);Fill(new Rect(p.x-5,p.y-7,7,5),new Color(.08f,.12f,.15f));Fill(new Rect(p.x-9,p.y+8,15,2),c);
   Fill(new Rect(p.x+5,p.y-6,3,2),c);Fill(new Rect(p.x+7,p.y-4,2,10),c);Fill(new Rect(p.x+4,p.y+5,5,2),c);
  }else if(kind==3){ // Clean cabin with a small sparkle, independent of installed glyphs.
   Fill(new Rect(p.x-8,p.y-1,17,8),c);Fill(new Rect(p.x-5,p.y-6,11,6),c);Fill(new Rect(p.x-3,p.y-4,7,4),new Color(.08f,.12f,.15f));
   Fill(new Rect(p.x+6,p.y-11,2,7),c);Fill(new Rect(p.x+4,p.y-9,6,2),c);
  }else{ // Open wrench: no emoji or platform-specific font dependency.
   Fill(new Rect(p.x-7,p.y-8,4,8),c);Fill(new Rect(p.x+3,p.y-8,4,8),c);Fill(new Rect(p.x-5,p.y-1,10,4),c);
   Fill(new Rect(p.x-2,p.y+1,4,8),c);Fill(new Rect(p.x-4,p.y+7,8,3),c);Fill(new Rect(p.x-1,p.y+7,2,2),new Color(.08f,.12f,.15f));
  }
 }
 static void Fill(Rect r,Color color){var previous=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=previous;}
}
