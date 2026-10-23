using UnityEngine;

// Compact vehicle-style gauges; geometry keeps icons readable without emoji fonts.
public static class DriverStatusGauges {
 static GUIStyle caption,number;
 public static void Draw(Rect rect,DriverCondition driver,CampaignAccount account){
  if(driver==null||account==null)return;
  if(caption==null){caption=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=12};number=new GUIStyle(caption){fontSize=16,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleRight};caption.normal.textColor=number.normal.textColor=Color.white;}
  float width=(rect.width-16)/3;
  DrawGauge(new Rect(rect.x,rect.y,width,rect.height),"포만감",(float)driver.satiety,0,new Color(.35f,.83f,.54f));
  DrawGauge(new Rect(rect.x+width+8,rect.y,width,rect.height),"피로",(float)driver.fatigue,1,new Color(.64f,.65f,1));
  DrawGauge(new Rect(rect.x+(width+8)*2,rect.y,width,rect.height),"연료",account.Fuel,2,new Color(1,.76f,.28f));
 }
 static void DrawGauge(Rect r,string text,float value,int icon,Color normal){
  value=Mathf.Clamp(value,0,100);Color tint=(icon==1?value>=75:value<=25)?new Color(1,.4f,.3f):normal;
  Icon(new Vector2(r.x+10,r.y+13),icon,tint);
  GUI.Label(new Rect(r.x+25,r.y+1,r.width-25,20),text,caption);
  GUI.Label(new Rect(r.x,r.y+21,r.width,23),Mathf.RoundToInt(value)+" / 100",number);
  Fill(new Rect(r.x,r.y+46,r.width,5),new Color(.16f,.22f,.26f));Fill(new Rect(r.x,r.y+46,r.width*value/100,5),tint);
 }
 static void Icon(Vector2 p,int kind,Color c){
  if(kind==0){ // Bowl and chopsticks.
   Fill(new Rect(p.x-8,p.y-2,16,3),c);Fill(new Rect(p.x-6,p.y+1,12,5),c);Fill(new Rect(p.x-3,p.y+6,6,2),c);
   Fill(new Rect(p.x-4,p.y-9,2,6),c);Fill(new Rect(p.x+2,p.y-9,2,6),c);
  }else if(kind==1){ // Resting head and pillow.
   Fill(new Rect(p.x-8,p.y-3,6,6),c);Fill(new Rect(p.x,p.y,9,4),c);Fill(new Rect(p.x-9,p.y+5,19,3),c);Fill(new Rect(p.x-9,p.y+2,2,8),c);
  }else{ // Fuel pump, window, nozzle and hose.
   Fill(new Rect(p.x-7,p.y-9,11,17),c);Fill(new Rect(p.x-5,p.y-7,7,5),new Color(.08f,.12f,.15f));Fill(new Rect(p.x-9,p.y+8,15,2),c);
   Fill(new Rect(p.x+5,p.y-6,3,2),c);Fill(new Rect(p.x+7,p.y-4,2,10),c);Fill(new Rect(p.x+4,p.y+5,5,2),c);
  }
 }
 static void Fill(Rect r,Color color){var previous=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=previous;}
}
