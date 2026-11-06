using UnityEngine;

// One readable choice surface for requests, passenger types and road reactions.
public static class PassengerDialogueUI {
 public struct Option {
  public string key,line,effect;public bool enabled;
  public Option(string key,string line,string effect,bool enabled=true){this.key=key;this.line=line;this.effect=string.IsNullOrEmpty(effect)?"효과 없음":effect;this.enabled=enabled;}
 }
 public static string EffectText(string effect){return string.IsNullOrEmpty(effect)||effect=="효과 없음"?"효과 없음":"효과 · "+effect;}
 static GUIStyle heading,body,small,button;
 static Vector2 scroll;static string scrollDialogue="";static int lastDialogueFrame=-2;
 // All coordinates returned here use the same scaled GUI space as Draw.
 public static Rect PanelBounds(float screenWidth,float screenHeight,float scale,float requestedHeight){
  scale=Mathf.Max(.01f,scale);float viewportWidth=screenWidth/scale,viewportHeight=screenHeight/scale;
  float width=Mathf.Min(600,Mathf.Max(1,viewportWidth-40)),height=Mathf.Min(Mathf.Max(1,requestedHeight),Mathf.Max(1,viewportHeight-40));
  return new Rect((viewportWidth-width)/2,Mathf.Max(0,viewportHeight-height-20),width,height);
 }
 static void Styles(){if(heading!=null)return;heading=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=20};heading.fontStyle=FontStyle.Bold;body=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=17};small=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14};foreach(var s in new[]{heading,body,small}){s.wordWrap=true;s.normal.textColor=CasualUI.Ink;}button=new GUIStyle(GUI.skin.button){normal={background=Texture2D.whiteTexture,textColor=Color.clear},hover={background=Texture2D.whiteTexture,textColor=Color.clear},active={background=Texture2D.whiteTexture,textColor=Color.clear}};}
 public static int Draw(string title,string dialogue,Option[] options,string note=""){
  Styles();var matrix=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
  const float pad=16,gap=10;Rect bounds=PanelBounds(Screen.width,Screen.height,scale,1);float inner=bounds.width-pad*2;
  float contentWidth=Mathf.Max(1,inner-Mathf.Max(18,GUI.skin.verticalScrollbar.fixedWidth));
  float bubble=body.CalcHeight(new GUIContent(dialogue),Mathf.Max(1,contentWidth-24))+24;
  float noteHeight=string.IsNullOrEmpty(note)?0:small.CalcHeight(new GUIContent(note),contentWidth)+10;
  float[] heights=new float[options.Length];float total=0;
  string identity=title+"\n"+dialogue;
  for(int i=0;i<options.Length;i++){identity+="\n"+options[i].key+"\t"+options[i].line;heights[i]=Mathf.Max(76,body.CalcHeight(new GUIContent(options[i].line),Mathf.Max(1,contentWidth-70))+small.CalcHeight(new GUIContent(EffectText(options[i].effect)),Mathf.Max(1,contentWidth-70))+23);total+=heights[i]+gap;}
  if(scrollDialogue!=identity||Time.frameCount>lastDialogueFrame+1){scrollDialogue=identity;scroll=Vector2.zero;}lastDialogueFrame=Time.frameCount;
  float contentHeight=bubble+12+noteHeight+(options.Length>0?27+total:0);
  bounds=PanelBounds(Screen.width,Screen.height,scale,46+contentHeight+pad);int chosen=-1;
  CasualUI.Panel(bounds);GUI.Label(new Rect(bounds.x+pad,bounds.y+10,inner,30),title,heading);
  Rect viewport=new Rect(bounds.x+pad,bounds.y+46,inner,Mathf.Max(1,bounds.height-46-pad));
  scroll.y=Mathf.Clamp(scroll.y,0,Mathf.Max(0,contentHeight-viewport.height));
  scroll=GUI.BeginScrollView(viewport,scroll,new Rect(0,0,contentWidth,contentHeight),false,contentHeight>viewport.height);
  float x=0,row=0;inner=contentWidth;
  CasualUI.RoundedFill(new Rect(x,row,inner,bubble),new Color(.87f,.95f,1));GUI.Label(new Rect(x+12,row+10,inner-24,bubble-16),dialogue,body);row+=bubble+12;
  if(noteHeight>0){GUI.Label(new Rect(x,row,inner,noteHeight),note,small);row+=noteHeight;}
  if(options.Length>0){GUI.Label(new Rect(x,row,inner,25),"답변 선택 · 운행 중에도 선택 가능",small);row+=27;}
  for(int i=0;i<options.Length;i++){
   var o=options[i];Rect r=new Rect(x,row,inner,heights[i]);bool enabled=GUI.enabled;GUI.enabled=enabled&&o.enabled;
   bool hover=r.Contains(Event.current.mousePosition)&&o.enabled;var color=GUI.backgroundColor;GUI.backgroundColor=hover?CasualUI.Green:CasualUI.Blue;if(GUI.Button(r,GUIContent.none,button))chosen=i;GUI.backgroundColor=color;
   CasualUI.RoundedFill(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),o.enabled?(hover?new Color(.80f,.91f,1):CasualUI.Soft):new Color(.91f,.92f,.94f));
   CasualUI.RoundedFill(new Rect(r.x+10,r.y+12,34,30),o.enabled?CasualUI.Blue:CasualUI.Muted);
   var keyStyle=new GUIStyle(heading){alignment=TextAnchor.MiddleCenter};keyStyle.normal.textColor=Color.white;GUI.Label(new Rect(r.x+10,r.y+12,34,30),o.key,keyStyle);
   float lineHeight=body.CalcHeight(new GUIContent(o.line),inner-70);GUI.Label(new Rect(r.x+54,r.y+9,inner-70,lineHeight),o.line,body);
   GUI.Label(new Rect(r.x+54,r.y+13+lineHeight,inner-70,r.height-lineHeight-18),EffectText(o.effect),small);GUI.enabled=enabled;row+=heights[i]+gap;
  }
  GUI.EndScrollView();GUI.matrix=matrix;return chosen;
 }
}
