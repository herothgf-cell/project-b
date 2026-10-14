using UnityEngine;
public static class KoreanText {
 static Font font;
 public static Font Font {get{if(!font)font=UnityEngine.Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);return font;}}
 public static void Apply(TextMesh text){text.font=Font;text.GetComponent<Renderer>().sharedMaterial=Font.material;}
 public static string Stop(string name){return name.Replace("Stop ","승하차 ");}
}
