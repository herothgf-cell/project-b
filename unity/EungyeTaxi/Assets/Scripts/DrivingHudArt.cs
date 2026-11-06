using UnityEngine;
// Presentation-only surfaces; never writes simulation state.
public static class DrivingHudArt {
 public static readonly Color Navy=new Color(.035f,.085f,.16f,.94f),Light=new Color(.25f,.36f,.50f),Cyan=new Color(.01f,.44f,.91f),Lime=new Color(.02f,.56f,.28f);
 static Texture2D dial,arc,capsule;
 public static GUIStyle Text(int size,Color? color=null,bool bold=false,TextAnchor anchor=TextAnchor.MiddleLeft){return new GUIStyle(GUI.skin.label){font=bold?KoreanText.BoldFont:KoreanText.Font,fontSize=UiReadability.Font(size),wordWrap=false,alignment=anchor,normal={textColor=color??CasualUI.Ink}};}
 public static void Glass(Rect r){CasualUI.RoundedFill(new Rect(r.x,r.y+4,r.width,r.height),new Color(0,.025f,.07f,.25f));CasualUI.RoundedFill(r,new Color(.60f,.77f,.96f));CasualUI.RoundedFill(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),new Color(.96f,.985f,1,.98f));Flat(new Rect(r.x+12,r.y+4,r.width-24,1),new Color(.76f,.90f,1,.22f));}
 public static void Flat(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
 static void Pill(Rect r,Color color){
  if(!capsule){capsule=new Texture2D(64,16,TextureFormat.RGBA32,false);for(int y=0;y<16;y++)for(int x=0;x<64;x++){float dx=x+.5f-Mathf.Clamp(x+.5f,8,56),dy=y+.5f-8;capsule.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8-Mathf.Sqrt(dx*dx+dy*dy))));}capsule.Apply(false,true);capsule.wrapMode=TextureWrapMode.Clamp;}
  var old=GUI.color;GUI.color=color;float cap=Mathf.Min(r.height/2,r.width/2);GUI.DrawTextureWithTexCoords(new Rect(r.x,r.y,cap,r.height),capsule,new Rect(0,0,.125f,1));GUI.DrawTexture(new Rect(r.x+cap,r.y,r.width-cap*2,r.height),Texture2D.whiteTexture);GUI.DrawTextureWithTexCoords(new Rect(r.xMax-cap,r.y,cap,r.height),capsule,new Rect(.875f,0,.125f,1));GUI.color=old;
 }
 public static void Meter(Rect r,float value,Color color){Pill(r,new Color(.72f,.80f,.88f));if(value>0)Pill(new Rect(r.x+1,r.y+1,(r.width-2)*Mathf.Clamp01(value/100),r.height-2),color);}
 public static void Dial(Rect r,DriveSession s){
  if(!dial){const int width=400,height=224;dial=new Texture2D(width,height,TextureFormat.RGBA32,false);dial.wrapMode=TextureWrapMode.Clamp;dial.filterMode=FilterMode.Bilinear;
   for(int y=0;y<height;y++)for(int x=0;x<width;x++){float gy=height-1-y;float d=Vector2.Distance(new Vector2(x+.5f,gy+.5f),new Vector2(200,200));Color c=Color.clear;if(d<194&&gy<208){c=Color.Lerp(new Color(.88f,.95f,1,.98f),new Color(1,1,1,.98f),Mathf.Clamp01((200-gy)/200));if(d>189)c=new Color(.56f,.68f,.82f,Mathf.Clamp01(194-d));else if(d>180)c=new Color(.04f,.075f,.13f);if(d>165&&d<174&&gy<=200)c=new Color(.68f,.79f,.88f);}dial.SetPixel(x,y,c);}dial.Apply(false,true);
  }
  if(!arc){arc=new Texture2D(400,224,TextureFormat.RGBA32,false);arc.wrapMode=TextureWrapMode.Clamp;for(int y=0;y<224;y++)for(int x=0;x<400;x++){float gy=223-y;float d=Vector2.Distance(new Vector2(x+.5f,gy+.5f),new Vector2(200,200));float a=gy<=200?Mathf.Clamp01(5-Mathf.Abs(d-170)):0;arc.SetPixel(x,y,new Color(Cyan.r,Cyan.g,Cyan.b,a));}arc.Apply(false,true);}
  GUI.DrawTexture(r,dial);float k=r.width/400;float speed=Mathf.Abs(s.taxi.Speed*3.6f);float edge=ArcCrop(speed);
  // Crop a cached semicircle in its own texture coordinates: no rotated/scaled GUI pivot.
  if(speed>0)GUI.DrawTextureWithTexCoords(new Rect(r.x,r.y,r.width*edge,r.height),arc,new Rect(0,0,edge,1));
  GUI.Label(new Rect(r.x+70*k,r.y+67*k,260*k,76*k),speed.ToString("0"),Text(62,null,true,TextAnchor.MiddleCenter));GUI.Label(new Rect(r.x+140*k,r.y+137*k,120*k,28*k),"km/h",Text(19,Light,false,TextAnchor.MiddleCenter));
  Glass(new Rect(r.x+28*k,r.y+183*k,344*k,38*k));GUI.Label(new Rect(r.x+125*k,r.y+184*k,150*k,34*k),s.assisted.Active?"조향 보조 · H":"수동 운전 · H",Text(13,Light,false,TextAnchor.MiddleCenter));GUI.Label(new Rect(r.x+174*k,r.y+163*k,52*k,29*k),s.taxi.Speed<-.2f?"R":"D",Text(24,Cyan,true,TextAnchor.MiddleCenter));
  var signal=s.signals;bool lit=signal&&signal.Lit;HudGlyph.Draw(new Rect(r.x+60*k,r.y+189*k,26*k,26*k),16,lit&&(signal.Hazards||signal.direction<0)?Lime:new Color(.37f,.46f,.57f));HudGlyph.Draw(new Rect(r.x+314*k,r.y+189*k,26*k,26*k),17,lit&&(signal.Hazards||signal.direction>0)?Lime:new Color(.37f,.46f,.57f));
 }
 public static float ArcCrop(float kmh){float f=Mathf.Clamp01(Mathf.Abs(kmh)/140f);return (200-Mathf.Cos(f*Mathf.PI)*175)/400;}
}
