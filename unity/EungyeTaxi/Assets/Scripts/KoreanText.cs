using UnityEngine;
public static class KoreanText {
 static Font font,bold,sign;
 // Road labels must not share the frequently repacked HUD dynamic atlas.
 public static Font SignFont {get{if(!sign){sign=UnityEngine.Object.Instantiate(BoldFont);sign.name="Road sign Korean atlas";}return sign;}}
 public static Font Font {get{if(!font)font=Resources.Load<Font>("Fonts/NotoSansKR-Medium");if(!font)font=UnityEngine.Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);return font;}}
 public static Font BoldFont {get{if(!bold)bold=Resources.Load<Font>("Fonts/NotoSansKR-Bold");return bold?bold:Font;}}
 static Texture2D surface,control,hover,pressed;static GUISkin prepared;
 static Texture2D Solid(Color color){var t=new Texture2D(1,1);t.SetPixel(0,0,color);t.Apply();return t;}
 public static Texture2D Surface {get{if(!surface)surface=Solid(new Color(.035f,.065f,.09f,1));return surface;}}
 public static void Panel(Rect r){var c=GUI.color;GUI.color=Color.white;GUI.DrawTexture(r,Surface);GUI.color=c;}
 public static void PrepareSkin(){if(prepared==GUI.skin)return;prepared=GUI.skin;prepared.font=Font;
  if(!control){control=Solid(new Color(.12f,.20f,.25f,1));hover=Solid(new Color(.18f,.32f,.39f,1));pressed=Solid(new Color(.10f,.42f,.43f,1));}
  prepared.label.font=Font;prepared.label.fontSize=16;prepared.label.padding=new RectOffset(0,0,0,0);prepared.label.contentOffset=Vector2.zero;prepared.label.alignment=TextAnchor.MiddleLeft;prepared.label.normal.textColor=Color.white;
  foreach(var s in new[]{prepared.box,prepared.window}){s.normal.background=Surface;s.onNormal.background=Surface;s.normal.textColor=Color.white;s.font=Font;}
  prepared.button.font=BoldFont;prepared.button.padding=new RectOffset(6,6,0,0);prepared.button.alignment=TextAnchor.MiddleCenter;
  prepared.button.normal.background=control;prepared.button.hover.background=hover;prepared.button.active.background=pressed;prepared.button.focused.background=hover;prepared.button.onNormal.background=pressed;
  foreach(var state in new[]{prepared.button.normal,prepared.button.hover,prepared.button.active,prepared.button.focused,prepared.button.onNormal,prepared.button.onHover,prepared.button.onActive,prepared.button.onFocused})state.textColor=Color.white;
 }
 public static void Apply(TextMesh text){text.font=Font;text.GetComponent<Renderer>().sharedMaterial=Font.material;}
 // Fit the rendered Korean glyphs, including multiline height, inside a physical sign.
 public static void Fit(TextMesh text,float width,float height){
  text.fontSize=80;text.characterSize=.1f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
  text.font=SignFont;text.GetComponent<Renderer>().sharedMaterial=SignFont.material;SignFont.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
  var size=text.GetComponent<Renderer>().localBounds.size;
  if(size.x>0&&size.y>0)text.characterSize*=Mathf.Min(width/size.x,height/size.y);
  if(!text.GetComponent<RoadSignLabel>())text.gameObject.AddComponent<RoadSignLabel>();
 }
 public static string Stop(string name){return name.Replace("Stop ","승하차 ");}
 public static GUIStyle Button(int size){PrepareSkin();return new GUIStyle(GUI.skin.button){font=BoldFont,fontStyle=FontStyle.Normal,fontSize=size,wordWrap=true,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(6,6,0,0)};}
}
