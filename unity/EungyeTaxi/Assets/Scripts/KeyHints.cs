using UnityEngine;

// 2026-11-24 play-test round 3: shortcut hints were plain text ("기록 (F8)", "창문 내리기 · K"), which read as
// clutter. Buttons now carry a small key cap at their right edge, and Enter/Esc work as the common confirm/close.
public static class KeyHints {
 static GUIStyle capText,capShape;
 // CasualUI.RoundedFill slices 12 px corners, which turns a 22 px cap into a U. Caps use a 4 px corner.
 static void Fill(Rect r,Color color){
  if(capShape==null){var t=new Texture2D(16,16,TextureFormat.RGBA32,false);for(int y=0;y<16;y++)for(int x=0;x<16;x++){float dx=Mathf.Max(0,Mathf.Max(4.5f-(x+.5f),(x+.5f)-11.5f)),dy=Mathf.Max(0,Mathf.Max(4.5f-(y+.5f),(y+.5f)-11.5f));t.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(4.5f-Mathf.Sqrt(dx*dx+dy*dy)+.5f)));}t.Apply(false,true);t.wrapMode=TextureWrapMode.Clamp;capShape=new GUIStyle();capShape.normal.background=t;capShape.border=new RectOffset(5,5,5,5);}
  var old=GUI.color;GUI.color=color;capShape.Draw(r,GUIContent.none,false,false,false,false);GUI.color=old;
 }
 static readonly Color CapFace=new Color(.88f,.92f,.97f),CapEdge=new Color(.42f,.50f,.62f),CapInk=new Color(.10f,.16f,.26f);
 static readonly Color DarkFace=new Color(.16f,.24f,.36f),DarkEdge=new Color(.04f,.08f,.14f);
 // Width of a cap for the given key label at the given height.
 public static float Width(string key,float height){return Mathf.Max(height,height*.42f*key.Length+height*.5f);}
 // A key cap: a rounded face over a darker bottom edge, like a keyboard key.
 public static void Cap(Rect r,string key,bool onDark=false){
  if(Event.current==null||Event.current.type!=EventType.Repaint)return;
  // Outline all round, a thicker bottom edge, and the face inset inside it.
  Fill(r,onDark?DarkEdge:CapEdge);
  Fill(new Rect(r.x+1.5f,r.y+1.5f,r.width-3,r.height-4.5f),onDark?DarkFace:CapFace);
  if(capText==null)capText=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,padding=new RectOffset(0,0,0,0),wordWrap=false,clipping=TextClipping.Overflow};
  capText.font=KoreanText.BoldFont;capText.fontSize=Mathf.RoundToInt((r.height-3)*(key.Length>2?.48f:.6f));capText.normal.textColor=onDark?Color.white:CapInk;
  GUI.Label(new Rect(r.x,r.y+.5f,r.width,r.height-4),key,capText);
 }
 // A button whose label is centred in the space left of a key cap at its right edge.
 public static bool Button(Rect r,string label,string key,GUIStyle style,bool onDark=false){
  bool clicked=GUI.Button(r,"",style);
  float h=Mathf.Min(24,r.height-10),w=Width(key,h);var cap=new Rect(r.xMax-w-8,r.y+(r.height-h)/2,w,h);
  var text=new GUIStyle(style){alignment=TextAnchor.MiddleCenter,padding=new RectOffset(0,0,0,0)};var content=new GUIContent(label);
  var area=new Rect(r.x+6,r.y,cap.x-r.x-10,r.height);
  // Shrink a long label rather than let it run under the cap.
  int size=text.fontSize>0?text.fontSize:14;text.fontSize=size;while(text.fontSize>10&&text.CalcSize(content).x>area.width)text.fontSize--;
  if(Event.current.type==EventType.Repaint){var old=GUI.contentColor;GUI.Label(area,content,text);GUI.contentColor=old;}
  Cap(cap,key,onDark);return clicked;
 }
 // A label followed by a cap, for read-only hints such as "수동 운전 [H]".
 public static void LabelWithCap(Rect r,string label,string key,GUIStyle style,bool onDark=false){
  var content=new GUIContent(label);float h=Mathf.Min(22,r.height-2),w=Width(key,h),textWidth=Mathf.Min(style.CalcSize(content).x,r.width-w-6);
  float x=style.alignment==TextAnchor.MiddleCenter||style.alignment==TextAnchor.UpperCenter?r.x+(r.width-textWidth-w-6)/2:r.x;
  var left=new GUIStyle(style){alignment=TextAnchor.MiddleLeft};GUI.Label(new Rect(x,r.y,textWidth+2,r.height),content,left);Cap(new Rect(x+textWidth+6,r.y+(r.height-h)/2,w,h),key,onDark);
 }
 // Enter (main or keypad) once, consumed so a second panel drawn this frame does not also act on it.
 public static bool Enter(){var e=Event.current;if(e==null||e.type!=EventType.KeyDown||e.keyCode!=KeyCode.Return&&e.keyCode!=KeyCode.KeypadEnter||!GUI.enabled)return false;e.Use();return true;}
}

// Esc closes the panels that had only a 닫기 button: help, driving history, the ride result and a folded-away passenger action.
// It runs before DriveSession so the same Esc press does not also open the pause menu.
[DefaultExecutionOrder(-40)]
public sealed class EscapeShortcuts:MonoBehaviour {
 public DriveSession session;
 public int ClosedFrame {get;private set;}=-1;
 void Update(){
  if(!session||!session.StartupReady||session.IsSimulationBot||!Input.GetKeyDown(KeyCode.Escape))return;
  if(session.SettingsOpen||session.business.LogbookOpen||session.business.GoalComparison)return;
  var ride=GetComponent<RideCompletionPanel>();var hud=GetComponent<DriveHUD>();var action=GetComponent<PassengerActionUI>();
  bool closed=false;
  if(ride&&ride.Open){ride.Close();closed=true;}
  else if(session.law&&session.law.HistoryOpen){session.law.CloseHistory();closed=true;}
  else if(hud&&hud.HelpOpen){hud.CloseHelp();closed=true;}
  else if(action&&action.Showing){action.Fold();closed=true;}
  else{var alerts=GetComponent<CriticalAlerts>();if(alerts&&alerts.Current!=null){alerts.Dismiss();closed=true;}}
  if(closed){ClosedFrame=Time.frameCount;if(session.services)session.services.ConsumeEscape();}
 }
}
