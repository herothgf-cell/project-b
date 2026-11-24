using UnityEngine;

// One readable choice surface for requests, passenger types and road reactions.
public static class PassengerDialogueUI {
 public struct Option {
  public string key,line,effect;public bool enabled;
  public Option(string key,string line,string effect,bool enabled=true){this.key=key;this.line=line;this.effect=string.IsNullOrEmpty(effect)?"효과 없음":effect;this.enabled=enabled;}
 }
 public static string EffectText(string effect){return string.IsNullOrEmpty(effect)||effect=="효과 없음"?"효과 없음":"효과 · "+effect;}
 // Existing offer identity survives boarding, route changes, save/restore and LAN replication.
 public static string PortraitIdentity(string offerId,string tripId){return !string.IsNullOrEmpty(offerId)?offerId:!string.IsNullOrEmpty(tripId)?tripId:"passenger";}
 public static string PortraitIdentity(DriveSession session){return PortraitIdentity(session&&session.jobs?session.jobs.OfferId:null,session&&session.law?session.law.tripId:null);}
 public static int PortraitIndex(string offerId,string tripId){return CasualUI.PortraitVariant(PortraitIdentity(offerId,tripId));}
 static GUIStyle heading,body,small,button,speech;
 static Vector2 scroll;static string scrollDialogue="";static int lastDialogueFrame=-2;
 // All coordinates returned here use the same scaled GUI space as Draw.
 public static Rect PanelBounds(float screenWidth,float screenHeight,float scale,float requestedHeight){
  scale=Mathf.Max(.01f,scale);float viewportWidth=screenWidth/scale,viewportHeight=screenHeight/scale;
  float left=viewportWidth>=1400?420:20,right=viewportWidth>=1400?viewportWidth-332:viewportWidth-20;
  float width=Mathf.Min(860,Mathf.Max(1,right-left)),height=Mathf.Min(Mathf.Max(1,requestedHeight),Mathf.Max(1,Mathf.Min(420,viewportHeight*.48f)));
  return new Rect(left+(right-left-width)/2,Mathf.Max(0,viewportHeight-height-(viewportHeight>=800?252:20)),width,height);
 }
 // Keep every authored term and number verbatim; only separate outcome clauses visually.
 public static string[] EffectParts(string effect){
  var main=new System.Collections.Generic.List<string>();var success=new System.Collections.Generic.List<string>();var failure=new System.Collections.Generic.List<string>();
  foreach(var part in System.Text.RegularExpressions.Regex.Split(string.IsNullOrEmpty(effect)?"효과 없음":effect,@" · |, (?=(?:즉시 |운행 중 |자연 피로 증가율 |급가감속 감점 |성공 종료 |실패 종료 |판정 없음 종료 ))")){
   if(part.StartsWith("성공 종료",System.StringComparison.Ordinal))success.Add(part);
   else if(part.StartsWith("실패 종료",System.StringComparison.Ordinal))failure.Add(part);
   else main.Add(part);
  }
  return new[]{string.Join(" · ",main.ToArray()),string.Join(" · ",success.ToArray()),string.Join(" · ",failure.ToArray())};
 }
 static void Styles(){
  if(heading==null){heading=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont};body=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont};small=new GUIStyle(GUI.skin.label){font=KoreanText.Font};speech=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont};foreach(var s in new[]{heading,body,small,speech}){s.wordWrap=true;s.normal.textColor=CasualUI.Ink;}button=new GUIStyle(GUIStyle.none);}
  heading.fontSize=UiReadability.Font(18);body.fontSize=UiReadability.Font(18);small.fontSize=UiReadability.Font(13);speech.fontSize=UiReadability.Font(22);
 }
 static float TextHeight(string value,float width,GUIStyle style){return string.IsNullOrEmpty(value)?0:style.CalcHeight(new GUIContent(value),Mathf.Max(1,width));}
 // 2026-11-24 play-test round 4: effects read as a wall of text. Numeric clauses become icon chips
 // (만족 +5 green, 피로 +4 amber, 성공 시/실패 시 labels); the remaining explanation is one muted line.
 public struct Chip {public string text;public int glyph;public Color color;}
 static readonly System.Text.RegularExpressions.Regex Numeric=new System.Text.RegularExpressions.Regex(@"^(즉시|성공 종료|실패 종료|판정 없음 종료|운행 중) ?(현재 )?(만족도|피로) ([+-−]?[0-9.]+)(점/분|점|%)?$");
 public static void Split(string effect,out System.Collections.Generic.List<Chip> chips,out string rest){
  chips=new System.Collections.Generic.List<Chip>();var words=new System.Collections.Generic.List<string>();
  if(string.IsNullOrEmpty(effect)||effect=="효과 없음"){chips.Add(new Chip{text="변화 없음",glyph=15,color=CasualUI.Muted});rest="";return;}
  foreach(var raw in System.Text.RegularExpressions.Regex.Split(effect,@" · |, ")){var part=raw.Trim();if(part.Length==0)continue;var m=Numeric.Match(part);
   if(!m.Success){if(part=="만족도 변화 0")chips.Add(new Chip{text="만족 변화 없음",glyph=6,color=CasualUI.Muted});else words.Add(part);continue;}
   string when=m.Groups[1].Value=="즉시"?"":m.Groups[1].Value=="성공 종료"?"성공 시 ":m.Groups[1].Value=="실패 종료"?"실패 시 ":m.Groups[1].Value=="운행 중"?"운행 중 ":"보통 ";
   bool fatigue=m.Groups[3].Value=="피로";float v;float.TryParse(m.Groups[4].Value.Replace("−","-"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v);
   string unit=m.Groups[5].Value=="%"?"%":m.Groups[5].Value=="점/분"?"/분":"";bool good=fatigue?v<0:v>0;
   chips.Add(new Chip{text=when+(fatigue?"피로 ":"만족 ")+(v>0?"+":v<0?"−":"")+Mathf.Abs(v).ToString("0.#")+unit,glyph=fatigue?11:6,color=v==0?CasualUI.Muted:good?CasualUI.Green:fatigue?CasualUI.Amber:CasualUI.Danger});}
  rest=string.Join(" · ",words.ToArray());
 }
 static float ChipWidth(Chip c){return small.CalcSize(new GUIContent(c.text)).x+40;}
 static float EffectsHeight(string effect,float width){System.Collections.Generic.List<Chip> chips;string rest;Split(effect,out chips,out rest);float x=0,rows=1;foreach(var c in chips){float w=ChipWidth(c);if(x>0&&x+w>width){rows++;x=0;}x+=w+6;}return rows*30+(rest.Length>0?22:0);}
 static void Effects(Rect r,string effect){
  System.Collections.Generic.List<Chip> chips;string rest;Split(effect,out chips,out rest);float x=r.x,y=r.y;
  foreach(var c in chips){float w=ChipWidth(c);if(x>r.x&&x+w>r.xMax){x=r.x;y+=30;}var chip=new Rect(x,y,w,26);var tint=c.color;tint.a=.14f;CasualUI.RoundedFill(chip,tint);HudGlyph.Draw(new Rect(chip.x+8,chip.y+4,18,18),c.glyph,c.color);
   GUI.Label(new Rect(chip.x+30,chip.y,w-34,26),c.text,new GUIStyle(small){font=KoreanText.BoldFont,wordWrap=false,alignment=TextAnchor.MiddleLeft,normal={textColor=c.color}});x+=w+6;}
  if(rest.Length>0){var muted=new GUIStyle(small){wordWrap=false,normal={textColor=CasualUI.Muted}};GUI.Label(new Rect(r.x,y+30,r.width,20),new GUIContent(UiReadability.Ellipsis(rest,muted,r.width),rest),muted);}
 }
 public static int Draw(string title,string dialogue,Option[] options,string note="",string passengerIdentity=null){
  dialogue=PassengerPresentation.Speech(dialogue);
  Styles();var matrix=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
  const float pad=16,gap=10;Rect bounds=PanelBounds(Screen.width,Screen.height,scale,1);float inner=bounds.width-pad*2;
  float contentWidth=Mathf.Max(1,inner-Mathf.Max(18,GUI.skin.verticalScrollbar.fixedWidth));
  float portraitSize=Mathf.Min(112,contentWidth*.25f),speechX=portraitSize+12,speechWidth=Mathf.Max(1,contentWidth-speechX);
  float bubble=Mathf.Max(portraitSize,speech.CalcHeight(new GUIContent(dialogue),Mathf.Max(1,speechWidth-24))+24);
  float noteHeight=string.IsNullOrEmpty(note)?0:small.CalcHeight(new GUIContent(note),contentWidth)+10;
  float[] heights=new float[options.Length];float total=0;
  string identity=title+"\n"+dialogue;
  for(int i=0;i<options.Length;i++){identity+="\n"+options[i].key+"\t"+options[i].line;heights[i]=Mathf.Max(64,body.CalcHeight(new GUIContent(options[i].line),Mathf.Max(1,contentWidth-70))+EffectsHeight(options[i].effect,Mathf.Max(1,contentWidth-70))+22);total+=heights[i]+gap;}
  if(scrollDialogue!=identity||Time.frameCount>lastDialogueFrame+1){scrollDialogue=identity;scroll=Vector2.zero;}lastDialogueFrame=Time.frameCount;
  float contentHeight=bubble+12+noteHeight+total-(options.Length>0?gap:0);
  float header=Mathf.Max(36,heading.CalcHeight(new GUIContent(title),Mathf.Max(1,inner-24))+12);
  bounds=PanelBounds(Screen.width,Screen.height,scale,header+contentHeight+pad);int chosen=-1;
  CasualUI.Panel(bounds);DrivingHudArt.Glass(new Rect(bounds.x,bounds.y,bounds.width,header));GUI.Label(new Rect(bounds.x+pad,bounds.y+6,inner,header-6),title,new GUIStyle(heading){normal={textColor=CasualUI.Ink}});
  Rect viewport=new Rect(bounds.x+pad,bounds.y+header,inner,Mathf.Max(1,bounds.height-header-pad));
  scroll.y=Mathf.Clamp(scroll.y,0,Mathf.Max(0,contentHeight-viewport.height));
  scroll=GUI.BeginScrollView(viewport,scroll,new Rect(0,0,contentWidth,contentHeight),false,contentHeight>viewport.height);
  float x=0,row=0;inner=contentWidth;
  CasualUI.Portrait(new Rect(x,row,portraitSize,portraitSize),PortraitIndex(passengerIdentity,null));
  CasualUI.RoundedFill(new Rect(x+speechX,row,speechWidth,bubble),new Color(.87f,.95f,1));GUI.Label(new Rect(x+speechX+12,row+12,speechWidth-24,bubble-20),dialogue,speech);row+=bubble+12;
  if(noteHeight>0){GUI.Label(new Rect(x,row,inner,noteHeight),note,small);row+=noteHeight;}
  for(int i=0;i<options.Length;i++){
   var o=options[i];Rect r=new Rect(x,row,inner,heights[i]);bool enabled=GUI.enabled;GUI.enabled=enabled&&o.enabled;
   bool hover=r.Contains(Event.current.mousePosition)&&o.enabled;Color accent=i==0?CasualUI.Green:CasualUI.Blue;
   CasualUI.RoundedFill(r,o.enabled?accent:CasualUI.Track);
   CasualUI.RoundedFill(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),o.enabled?(hover?new Color(.82f,.92f,1):i==0?new Color(.94f,.99f,.95f):CasualUI.Soft):new Color(.91f,.92f,.94f));
   if(GUI.Button(r,new GUIContent("",o.line),button)&&NumericChoiceInput.TryConsume(Time.frameCount))chosen=i;
   CasualUI.RoundedFill(new Rect(r.x+10,r.y+12,34,30),o.enabled?accent:CasualUI.Muted);
   var keyStyle=new GUIStyle(heading){alignment=TextAnchor.MiddleCenter};keyStyle.normal.textColor=Color.white;GUI.Label(new Rect(r.x+10,r.y+12,34,30),NumericChoiceInput.Label(i),keyStyle);
   float lineHeight=body.CalcHeight(new GUIContent(o.line),inner-70);GUI.Label(new Rect(r.x+54,r.y+9,inner-70,lineHeight),o.line,body);
   Effects(new Rect(r.x+54,r.y+13+lineHeight,inner-70,r.height-lineHeight-19),o.effect);GUI.enabled=enabled;row+=heights[i]+gap;
  }
  GUI.EndScrollView();GUI.matrix=matrix;return chosen;
 }
}
