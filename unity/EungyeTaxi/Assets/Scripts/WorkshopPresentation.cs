using UnityEngine;

// Workshop display only. Quotes, service clocks and actions remain with their owners.
public static class WorkshopPresentation {
 public const float Width=520;
 public static float Scale(float width,float height){return Mathf.Min(1.2f,width/720f,height/560f);}
 static GUIStyle Text(int size,bool bold=false,TextAnchor alignment=TextAnchor.MiddleLeft){return new GUIStyle(GUI.skin.label){font=bold?KoreanText.BoldFont:KoreanText.Font,fontSize=size,wordWrap=false,alignment=alignment,normal={textColor=CasualUI.Ink}};}
 public static void Card(Rect bounds,string title,string subtitle,string[] labels,string[] values,string notice){
  float x=bounds.x,y=bounds.y;CasualUI.Panel(bounds);
  CasualUI.RoundedFill(new Rect(x+20,y+18,48,48),new Color(1,.91f,.80f));HudGlyph.Draw(new Rect(x+30,y+28,28,28),12,CasualUI.Amber);
  GUI.Label(new Rect(x+84,y+12,416,34),title,Text(24,true));GUI.Label(new Rect(x+84,y+46,416,24),subtitle,Text(14));
  for(int i=0;i<labels.Length;i++){float row=y+86+i*36;CasualUI.RoundedFill(new Rect(x+20,row,480,30),CasualUI.Soft);GUI.Label(new Rect(x+32,row,146,30),labels[i],Text(14));GUI.Label(new Rect(x+182,row,306,30),values[i],Text(17,true,TextAnchor.MiddleRight));}
  var style=Text(13);GUI.Label(new Rect(x+20,y+198,480,26),new GUIContent(UiReadability.Ellipsis(notice??"",style,480),notice??""),style);
 }
 public static GUIStyle Secondary {get{return KoreanText.Button(14);}}
}
