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
  Glass(r);
  float speed=Mathf.Abs(s.taxi.Speed*3.6f);
  GUI.Label(new Rect(r.x+72,r.y+3,112,51),speed.ToString("0"),Text(40,null,true,TextAnchor.MiddleCenter));
  GUI.Label(new Rect(r.x+184,r.y+23,54,25),"km/h",Text(15,Light));
  Pill(new Rect(r.x+248,r.y+12,38,37),new Color(.86f,.93f,1));
  GUI.Label(new Rect(r.x+248,r.y+12,38,37),s.taxi.Speed<-.2f?"R":"D",Text(23,Cyan,true,TextAnchor.MiddleCenter));
  Meter(new Rect(r.x+18,r.y+55,r.width-36,5),Mathf.Clamp01(speed/140f)*100,Cyan);
  GUI.Label(new Rect(r.x+54,r.y+65,r.width-108,23),s.assisted.Active?"조향 보조 · H":"수동 운전 · H",Text(13,Light,false,TextAnchor.MiddleCenter));
  var signal=s.signals;bool lit=signal&&signal.Lit;
  HudGlyph.Draw(new Rect(r.x+18,r.y+66,22,22),16,lit&&(signal.Hazards||signal.direction<0)?Lime:Light);
  HudGlyph.Draw(new Rect(r.xMax-40,r.y+66,22,22),17,lit&&(signal.Hazards||signal.direction>0)?Lime:Light);
 }
 public static float ArcCrop(float kmh){float f=Mathf.Clamp01(Mathf.Abs(kmh)/140f);return (200-Mathf.Cos(f*Mathf.PI)*175)/400;}
}
