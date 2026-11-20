using UnityEngine;
public static class KoreanText {
 static Font font,bold,sign;
 // Road labels must not share the frequently repacked HUD dynamic atlas.
 public static Font SignFont {get{if(!sign)sign=Resources.Load<Font>("Fonts/NotoSansKR-Signs");return sign?sign:BoldFont;}}
 public static Font Font {get{if(!font)font=Resources.Load<Font>("Fonts/NotoSansKR-Medium");if(!font)font=UnityEngine.Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);return font;}}
 public static Font BoldFont {get{if(!bold)bold=Resources.Load<Font>("Fonts/NotoSansKR-Bold");return bold?bold:Font;}}
 static Texture2D control,hover,pressed;static GUISkin prepared;
 public static Texture2D Surface {get{return CasualUI.Rounded;}}
 public static void Panel(Rect r){CasualUI.Panel(r);}
 public static void PrepareSkin(){if(prepared==GUI.skin)return;prepared=GUI.skin;prepared.font=Font;
  if(!control){control=Tinted(CasualUI.Soft);hover=Tinted(new Color(.80f,.90f,1));pressed=Tinted(new Color(.66f,.82f,.97f));}
  prepared.label.font=Font;prepared.label.fontSize=16;prepared.label.padding=new RectOffset(0,0,0,0);prepared.label.contentOffset=Vector2.zero;prepared.label.alignment=TextAnchor.MiddleLeft;prepared.label.normal.textColor=CasualUI.Ink;
  foreach(var s in new[]{prepared.box,prepared.window}){s.normal.background=Surface;s.onNormal.background=Surface;s.normal.textColor=CasualUI.Ink;s.font=Font;s.border=new RectOffset(12,12,12,12);s.padding=new RectOffset(12,12,12,12);}
  prepared.button.font=BoldFont;prepared.button.fontSize=18;prepared.button.padding=new RectOffset(10,10,3,3);prepared.button.alignment=TextAnchor.MiddleCenter;
  prepared.button.normal.background=control;prepared.button.hover.background=hover;prepared.button.active.background=pressed;prepared.button.focused.background=hover;prepared.button.onNormal.background=pressed;
  prepared.button.border=new RectOffset(12,12,12,12);
  foreach(var state in new[]{prepared.button.normal,prepared.button.hover,prepared.button.active,prepared.button.focused,prepared.button.onNormal,prepared.button.onHover,prepared.button.onActive,prepared.button.onFocused}){state.textColor=CasualUI.Ink;}
  prepared.button.onHover.background=hover;prepared.button.onActive.background=pressed;prepared.button.onFocused.background=hover;
  foreach(var s in new[]{prepared.textField,prepared.textArea}){s.font=Font;s.fontSize=16;s.normal.background=control;s.focused.background=hover;s.border=new RectOffset(12,12,12,12);s.padding=new RectOffset(8,8,3,3);s.normal.textColor=s.focused.textColor=CasualUI.Ink;}
  foreach(var state in new[]{prepared.toggle.normal,prepared.toggle.hover,prepared.toggle.active,prepared.toggle.focused,prepared.toggle.onNormal,prepared.toggle.onHover,prepared.toggle.onActive,prepared.toggle.onFocused})state.textColor=CasualUI.Ink;
  prepared.toggle.font=Font;prepared.toggle.fontSize=16;
  prepared.horizontalSlider.normal.background=Tinted(CasualUI.Track);prepared.horizontalSlider.border=new RectOffset(8,8,3,3);prepared.horizontalSlider.fixedHeight=7;
  prepared.horizontalSliderThumb.normal.background=prepared.horizontalSliderThumb.hover.background=prepared.horizontalSliderThumb.active.background=Tinted(CasualUI.Blue);prepared.horizontalSliderThumb.border=new RectOffset(8,8,8,8);prepared.horizontalSliderThumb.fixedWidth=18;prepared.horizontalSliderThumb.fixedHeight=18;
  prepared.verticalScrollbar.normal.background=control;prepared.verticalScrollbar.fixedWidth=16;prepared.verticalScrollbar.border=new RectOffset(6,6,6,6);
  prepared.verticalScrollbarThumb.normal.background=prepared.verticalScrollbarThumb.hover.background=prepared.verticalScrollbarThumb.active.background=Tinted(CasualUI.Blue);prepared.verticalScrollbarThumb.border=new RectOffset(7,7,7,7);
 }
 static Texture2D Tinted(Color color){var t=new Texture2D(48,48,TextureFormat.RGBA32,false);var p=CasualUI.Rounded.GetPixels();for(int i=0;i<p.Length;i++)p[i]=new Color(color.r,color.g,color.b,p[i].a);t.SetPixels(p);t.Apply();t.wrapMode=TextureWrapMode.Clamp;return t;}
 public static void Apply(TextMesh text){text.font=Font;text.GetComponent<Renderer>().sharedMaterial=Font.material;}
 // Fit the rendered Korean glyphs, including multiline height, inside a physical sign.
 public static void Fit(TextMesh text,float width,float height){
  text.fontSize=80;text.characterSize=.1f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
  text.font=SignFont;text.GetComponent<Renderer>().sharedMaterial=SignFont.material;SignFont.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
  var size=text.GetComponent<Renderer>().localBounds.size;
  if(size.x>0&&size.y>0)text.characterSize*=Mathf.Min(width/size.x,height/size.y);
  if(!text.GetComponent<RoadSignLabel>())text.gameObject.AddComponent<RoadSignLabel>();
 }
 // Keep authored and saved stop identities intact. Only the presentation names change.
 struct StopLabel {
  public string key,name,suffix;
  public StopLabel(string key,string name,string suffix=""){this.key=key;this.name=name;this.suffix=suffix;}
 }
 static readonly System.Collections.Generic.Dictionary<string,StopLabel> stopNames=BuildStopNames();
 static System.Collections.Generic.Dictionary<string,StopLabel> BuildStopNames(){
  var result=new System.Collections.Generic.Dictionary<string,StopLabel>(System.StringComparer.Ordinal);
  string[] scenario={"수인로 서쪽","계수로 동쪽","은행로173번길","은계중앙로 동쪽","수인로 남쪽 일대","은계로 북쪽","은계로 남쪽","은계남로 중부","계수로 서쪽","대은로104번길","검바위1로","은계번영길"};
  string[] bays={"은계중앙1길","은계남로 서쪽","은계중앙로 남동쪽","은행고길 북쪽","은계로 남단 일대","은계호수로 인근","은계중앙로 남쪽","은계중앙로 북동쪽","계수로 중부","은계로 중부","은행고길 남쪽","은계남로 동쪽"};
  for(int i=0;i<scenario.Length;i++){
   string legacy=(i==0?"국도42":"은계")+" 승하차 "+(i+1);
   string bay="게임 승하차 "+(i+1);
   string scenarioKey="stopName.scenario."+(i+1),bayKey="stopName.bay."+(i+1);
   result[legacy]=new StopLabel(scenarioKey,scenario[i]);result[legacy+" 반대편"]=new StopLabel(scenarioKey,scenario[i]," 반대편");
   result[bay]=new StopLabel(bayKey,bays[i]);result[bay+" 반대편"]=new StopLabel(bayKey,bays[i]," 반대편");
  }
  result["게임 대체 승하차 2-A"]=new StopLabel("stopName.alternate.1","은계남로 서쪽 대체 정차면");
  return result;
 }
 public static string Stop(string name){
  if(string.IsNullOrEmpty(name))return "";
  return stopNames.TryGetValue(name,out var label)?ContentTuning.Text(label.key,label.name)+label.suffix:name.Replace("Stop ","승하차 ");
 }
 public static GUIStyle Button(int size){PrepareSkin();return new GUIStyle(GUI.skin.button){font=BoldFont,fontStyle=FontStyle.Normal,fontSize=size,wordWrap=true,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(6,6,0,0)};}
}
