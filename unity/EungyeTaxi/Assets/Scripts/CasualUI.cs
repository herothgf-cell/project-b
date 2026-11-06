using UnityEngine;

// Presentation-only tokens shared by campaign and the integrated bot match.
// Textures are small nine-sliced masks, created once rather than per HUD frame.
public static class CasualUI {
 public static readonly Color Ink=new Color(.075f,.15f,.29f);
 public static readonly Color Muted=new Color(.30f,.39f,.51f);
 public static readonly Color Canvas=new Color(.88f,.94f,.99f);
 public static readonly Color Card=Color.white;
 public static readonly Color Soft=new Color(.93f,.97f,1);
 public static readonly Color Track=new Color(.80f,.86f,.93f);
 public static readonly Color Blue=new Color(.025f,.34f,.78f);
 public static readonly Color Green=new Color(.015f,.43f,.23f);
 public static readonly Color Danger=new Color(.76f,.12f,.17f);
 public static readonly Color Amber=new Color(.62f,.34f,.025f);
 public static readonly Color Border=new Color(.77f,.85f,.94f);
 public static readonly Color Warm=new Color(1,.97f,.85f);
 static Texture2D mask;static GUIStyle shape;
 public static Texture2D Rounded {
  get {if(mask)return mask;const int size=48;const float radius=12;
   mask=new Texture2D(size,size,TextureFormat.RGBA32,false);mask.name="Casual UI rounded mask";mask.wrapMode=TextureWrapMode.Clamp;mask.filterMode=FilterMode.Bilinear;
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    var p=new Vector2(x+.5f,y+.5f);var closest=new Vector2(Mathf.Clamp(p.x,radius,size-radius),Mathf.Clamp(p.y,radius,size-radius));
    mask.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(radius+.5f-Vector2.Distance(p,closest))));
   }mask.Apply();return mask;
  }
 }
 public static void RoundedFill(Rect r,Color color){
  if(Event.current==null||Event.current.type!=EventType.Repaint||r.width<=0||r.height<=0)return;
  if(shape==null){shape=new GUIStyle();shape.normal.background=Rounded;shape.border=new RectOffset(12,12,12,12);}
  var old=GUI.color;GUI.color=color;shape.Draw(r,GUIContent.none,false,false,false,false);GUI.color=old;
 }
 public static void Panel(Rect r){Panel(r,Card);}
 public static void Panel(Rect r,Color color){
  RoundedFill(new Rect(r.x,r.y+4,r.width,r.height),new Color(.10f,.22f,.40f,.15f));
  RoundedFill(new Rect(r.x-1,r.y-1,r.width+2,r.height+2),Border);
  RoundedFill(r,color);
 }
 public static void Header(Rect r,string title,int glyph=1,Color? accent=null){
  RoundedFill(r,accent??Blue);HudGlyph.Draw(new Rect(r.x+14,r.y+(r.height-28)/2,28,28),glyph,Color.white);
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=22,alignment=TextAnchor.MiddleLeft,wordWrap=false};style.normal.textColor=Color.white;
  while(style.fontSize>16&&style.CalcSize(new GUIContent(title)).x>r.width-70)style.fontSize--;
  GUI.Label(new Rect(r.x+54,r.y,r.width-68,r.height),new GUIContent(title,title),style);
 }
 static Texture2D portraits,backdrop;
 public static void Portrait(Rect r,int variant=0){
  if(!portraits)portraits=Resources.Load<Texture2D>("UI/passenger-portraits");RoundedFill(r,Soft);
  if(portraits){variant=((variant%4)+4)%4;var old=GUI.color;GUI.color=Color.white;GUI.DrawTextureWithTexCoords(r,portraits,new Rect((variant%2)*.5f,variant<2?.5f:0,.5f,.5f));GUI.color=old;}
  else HudGlyph.Draw(new Rect(r.center.x-16,r.center.y-16,32,32),6,Green);
 }
 public static int PortraitVariant(string identity){unchecked{uint hash=0;foreach(char c in identity??"")hash=hash*31+c;return (int)(hash%4);}}
 public static void AttentionDot(Rect r){HudGlyph.Draw(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),15,Card);HudGlyph.Draw(r,15,Danger);}
 public static void Divider(Rect r){var old=GUI.color;GUI.color=Border;GUI.DrawTexture(new Rect(r.x,r.y,r.width,1),Texture2D.whiteTexture);GUI.color=old;}
 public static void Backdrop(){
  if(!backdrop){backdrop=new Texture2D(1,64,TextureFormat.RGB24,false);for(int y=0;y<64;y++)backdrop.SetPixel(0,y,Color.Lerp(Canvas,new Color(1,.99f,.95f),y/63f));backdrop.Apply();backdrop.wrapMode=TextureWrapMode.Clamp;}
  var old=GUI.color;GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),backdrop);GUI.color=old;
 }
 public static void Bar(Rect r,float fraction,Color color){RoundedFill(r,Track);if(fraction>0)RoundedFill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(fraction),r.height),color);}
 public static GUIStyle Action(int size){var s=KoreanText.Button(size);PaintButton(s,Green,new Color(.02f,.51f,.28f),new Color(.01f,.34f,.18f),Color.white);return s;}
 static Texture2D green,greenHover,greenDown;
 static Texture2D Tinted(Color c){var t=new Texture2D(48,48,TextureFormat.RGBA32,false);var p=Rounded.GetPixels();for(int i=0;i<p.Length;i++)p[i]=new Color(c.r,c.g,c.b,p[i].a);t.SetPixels(p);t.Apply();t.wrapMode=TextureWrapMode.Clamp;return t;}
 static void PaintButton(GUIStyle s,Color normal,Color hover,Color down,Color text){
  if(!green){green=Tinted(normal);greenHover=Tinted(hover);greenDown=Tinted(down);}
  s.normal.background=green;s.hover.background=s.focused.background=greenHover;s.active.background=s.onNormal.background=s.onActive.background=greenDown;s.onHover.background=s.onFocused.background=greenHover;
  foreach(var state in new[]{s.normal,s.hover,s.active,s.focused,s.onNormal,s.onHover,s.onActive,s.onFocused})state.textColor=text;
 }
}
