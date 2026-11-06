using UnityEngine;
// Presentation-only surfaces; never writes simulation state.
public static class DrivingHudArt {
 public static readonly Color Navy=new Color(.035f,.085f,.16f,.94f),Light=new Color(.73f,.85f,.96f),Cyan=new Color(.08f,.85f,1),Lime=new Color(.40f,1,.35f);
 static Texture2D dial,capsule;
 public static GUIStyle Text(int size,Color? color=null,bool bold=false,TextAnchor anchor=TextAnchor.MiddleLeft){return new GUIStyle(GUI.skin.label){font=bold?KoreanText.BoldFont:KoreanText.Font,fontSize=UiReadability.Font(size),wordWrap=false,alignment=anchor,normal={textColor=color??Color.white}};}
 public static void Glass(Rect r){CasualUI.RoundedFill(new Rect(r.x,r.y+4,r.width,r.height),new Color(0,.025f,.07f,.25f));CasualUI.RoundedFill(r,new Color(.64f,.78f,.92f,.85f));CasualUI.RoundedFill(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),Navy);Flat(new Rect(r.x+12,r.y+4,r.width-24,1),new Color(.76f,.90f,1,.22f));}
 public static void Flat(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
 static void Pill(Rect r,Color color){
  if(!capsule){capsule=new Texture2D(64,16,TextureFormat.RGBA32,false);for(int y=0;y<16;y++)for(int x=0;x<64;x++){float dx=x+.5f-Mathf.Clamp(x+.5f,8,56),dy=y+.5f-8;capsule.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8-Mathf.Sqrt(dx*dx+dy*dy))));}capsule.Apply(false,true);capsule.wrapMode=TextureWrapMode.Clamp;}
  var old=GUI.color;GUI.color=color;float cap=Mathf.Min(r.height/2,r.width/2);GUI.DrawTextureWithTexCoords(new Rect(r.x,r.y,cap,r.height),capsule,new Rect(0,0,.125f,1));GUI.DrawTexture(new Rect(r.x+cap,r.y,r.width-cap*2,r.height),Texture2D.whiteTexture);GUI.DrawTextureWithTexCoords(new Rect(r.xMax-cap,r.y,cap,r.height),capsule,new Rect(.875f,0,.125f,1));GUI.color=old;
 }
 public static void Meter(Rect r,float value,Color color){Pill(r,new Color(.27f,.34f,.44f));if(value>0)Pill(new Rect(r.x+1,r.y+1,(r.width-2)*Mathf.Clamp01(value/100),r.height-2),color);}
 public static void Dial(Rect r,DriveSession s){
  if(!dial){const int width=400,height=224;dial=new Texture2D(width,height,TextureFormat.RGBA32,false);dial.wrapMode=TextureWrapMode.Clamp;dial.filterMode=FilterMode.Bilinear;
   for(int y=0;y<height;y++)for(int x=0;x<width;x++){float gy=height-1-y;float d=Vector2.Distance(new Vector2(x+.5f,gy+.5f),new Vector2(200,200));Color c=Color.clear;if(d<194&&gy<208){c=Color.Lerp(new Color(.025f,.045f,.09f,.98f),new Color(.10f,.17f,.27f,.98f),Mathf.Clamp01((200-gy)/200));if(d>189)c=new Color(.56f,.68f,.82f,Mathf.Clamp01(194-d));else if(d>180)c=new Color(.04f,.075f,.13f);if(d>165&&d<174&&gy<=200)c=new Color(.16f,.29f,.39f);}dial.SetPixel(x,y,c);}dial.Apply(false,true);
  }
  GUI.DrawTexture(r,dial);float k=r.width/400;var center=new Vector2(r.x+200*k,r.y+200*k);float speed=Mathf.Abs(s.taxi.Speed*3.6f);int segments=Mathf.RoundToInt(Mathf.Clamp01(speed/140)*64);
  for(int i=0;i<segments;i++){float a=Mathf.PI+i*Mathf.PI/64,b=Mathf.PI+(i+1)*Mathf.PI/64;Segment(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*170*k,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*170*k,Cyan,8*k);}
  GUI.Label(new Rect(r.x+70*k,r.y+67*k,260*k,76*k),speed.ToString("0"),Text(62,null,true,TextAnchor.MiddleCenter));GUI.Label(new Rect(r.x+140*k,r.y+137*k,120*k,28*k),"km/h",Text(19,Light,false,TextAnchor.MiddleCenter));
  Glass(new Rect(r.x+28*k,r.y+183*k,344*k,38*k));GUI.Label(new Rect(r.x+125*k,r.y+184*k,150*k,34*k),s.assisted.Active?"조향 보조 · H":"수동 운전 · H",Text(13,Light,false,TextAnchor.MiddleCenter));GUI.Label(new Rect(r.x+174*k,r.y+163*k,52*k,29*k),s.taxi.Speed<-.2f?"R":"D",Text(24,Cyan,true,TextAnchor.MiddleCenter));
  var signal=s.signals;bool lit=signal&&signal.Lit;HudGlyph.Draw(new Rect(r.x+60*k,r.y+189*k,26*k,26*k),16,lit&&(signal.Hazards||signal.direction<0)?Lime:new Color(.37f,.46f,.57f));HudGlyph.Draw(new Rect(r.x+314*k,r.y+189*k,26*k,26*k),17,lit&&(signal.Hazards||signal.direction>0)?Lime:new Color(.37f,.46f,.57f));
 }
 static void Segment(Vector2 a,Vector2 b,Color color,float width){var matrix=GUI.matrix;var old=GUI.color;GUI.color=color;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y-width/2,Vector2.Distance(a,b)+1,width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=old;}
}
