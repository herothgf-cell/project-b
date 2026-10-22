using UnityEngine;
public static class KoreanText {
 static Font font;
 public static Font Font {get{if(!font)font=UnityEngine.Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);return font;}}
 public static void Apply(TextMesh text){text.font=Font;text.GetComponent<Renderer>().sharedMaterial=Font.material;}
 // Fit the rendered Korean glyphs, including multiline height, inside a physical sign.
 public static void Fit(TextMesh text,float width,float height){
  text.fontSize=80;text.characterSize=.1f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
  Apply(text);Font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
  var size=text.GetComponent<Renderer>().localBounds.size;
  if(size.x>0&&size.y>0)text.characterSize*=Mathf.Min(width/size.x,height/size.y);
  if(!text.GetComponent<RoadSignLabel>())text.gameObject.AddComponent<RoadSignLabel>();
 }
 public static string Stop(string name){return name.Replace("Stop ","승하차 ");}
 public static GUIStyle Button(int size){return new GUIStyle(GUI.skin.button){font=Font,fontSize=size,wordWrap=true,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(5,5,2,2)};}
}
