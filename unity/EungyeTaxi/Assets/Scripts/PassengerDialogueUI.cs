using UnityEngine;

// One readable choice surface for requests, passenger types and road reactions.
public static class PassengerDialogueUI {
 public struct Option {
  public string key,line,effect;public bool enabled;
  public Option(string key,string line,string effect,bool enabled=true){this.key=key;this.line=line;this.effect=string.IsNullOrEmpty(effect)?"효과 없음":effect;this.enabled=enabled;}
 }
 public static string EffectText(string effect){return string.IsNullOrEmpty(effect)||effect=="효과 없음"?"효과 없음":"효과 · "+effect;}
 static GUIStyle heading,body,small,button;
 static void Styles(){if(heading!=null)return;heading=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=20};heading.fontStyle=FontStyle.Bold;body=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=17};small=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14};foreach(var s in new[]{heading,body,small}){s.wordWrap=true;s.normal.textColor=CasualUI.Ink;}button=new GUIStyle(GUI.skin.button){normal={background=Texture2D.whiteTexture,textColor=Color.clear},hover={background=Texture2D.whiteTexture,textColor=Color.clear},active={background=Texture2D.whiteTexture,textColor=Color.clear}};}
 public static int Draw(string title,string dialogue,Option[] options,string note=""){
  Styles();var matrix=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
  const float width=600,pad=16,gap=10;float inner=width-pad*2;
  float bubble=body.CalcHeight(new GUIContent(dialogue),inner-24)+24;
  float noteHeight=string.IsNullOrEmpty(note)?0:small.CalcHeight(new GUIContent(note),inner)+10;
  float[] heights=new float[options.Length];float total=0;
  for(int i=0;i<options.Length;i++){heights[i]=Mathf.Max(76,body.CalcHeight(new GUIContent(options[i].line),inner-70)+small.CalcHeight(new GUIContent(EffectText(options[i].effect)),inner-70)+23);total+=heights[i]+gap;}
  float height=pad+30+bubble+12+noteHeight+(options.Length>0?27+total:0)+pad;
  float x=(Screen.width/scale-width)/2,y=Screen.height/scale-height-20;int chosen=-1;
  CasualUI.Panel(new Rect(x,y,width,height));GUI.Label(new Rect(x+pad,y+10,inner,30),title,heading);float row=y+46;
  CasualUI.RoundedFill(new Rect(x+pad,row,inner,bubble),new Color(.87f,.95f,1));GUI.Label(new Rect(x+pad+12,row+10,inner-24,bubble-16),dialogue,body);row+=bubble+12;
  if(noteHeight>0){GUI.Label(new Rect(x+pad,row,inner,noteHeight),note,small);row+=noteHeight;}
  if(options.Length>0){GUI.Label(new Rect(x+pad,row,inner,25),"답변 선택 · 운행 중에도 선택 가능",small);row+=27;}
  for(int i=0;i<options.Length;i++){
   var o=options[i];Rect r=new Rect(x+pad,row,inner,heights[i]);bool enabled=GUI.enabled;GUI.enabled=enabled&&o.enabled;
   bool hover=r.Contains(Event.current.mousePosition)&&o.enabled;var color=GUI.backgroundColor;GUI.backgroundColor=hover?CasualUI.Green:CasualUI.Blue;if(GUI.Button(r,GUIContent.none,button))chosen=i;GUI.backgroundColor=color;
   CasualUI.RoundedFill(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),o.enabled?(hover?new Color(.80f,.91f,1):CasualUI.Soft):new Color(.91f,.92f,.94f));
   CasualUI.RoundedFill(new Rect(r.x+10,r.y+12,34,30),o.enabled?CasualUI.Blue:CasualUI.Muted);
   var keyStyle=new GUIStyle(heading){alignment=TextAnchor.MiddleCenter};keyStyle.normal.textColor=Color.white;GUI.Label(new Rect(r.x+10,r.y+12,34,30),o.key,keyStyle);
   float lineHeight=body.CalcHeight(new GUIContent(o.line),inner-70);GUI.Label(new Rect(r.x+54,r.y+9,inner-70,lineHeight),o.line,body);
   GUI.Label(new Rect(r.x+54,r.y+13+lineHeight,inner-70,r.height-lineHeight-18),EffectText(o.effect),small);GUI.enabled=enabled;row+=heights[i]+gap;
  }
  GUI.matrix=matrix;return chosen;
 }
}
