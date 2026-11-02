using System;
using UnityEngine;
// Tests actual cached skin pixels, not a duplicate palette declaration.
public static class BrightUIThemeTests {
 public static void Build(){Run();ProjectMapBuild.RunTests();PvpFlowUnifiedChecks.Run();PrototypeBuild.BuildHybrid();}
 public static void Rebuild(){Run();NavigationCueTests.Run();PrototypeBuild.BuildHybrid();}
 public static void Run(){
  var surface=KoreanText.Surface;
  var center=surface.GetPixel(surface.width/2,surface.height/2);
  if(center.r<.9f||center.g<.9f||center.b<.9f)throw new Exception("Bright UI requires an opaque light card center");
  if(center.a<.99f||surface.GetPixel(0,0).a>.05f)throw new Exception("Rounded card must keep opaque text body and transparent corners");
  if(KoreanText.Surface!=surface)throw new Exception("Card texture must be reused across frames");
  foreach(var background in new[]{CasualUI.Card,CasualUI.Soft,CasualUI.Canvas})if(Contrast(CasualUI.Ink,background)<4.5f)throw new Exception("Body text contrast below 4.5:1");
  foreach(var text in new[]{CasualUI.Blue,CasualUI.Green,CasualUI.Danger,CasualUI.Amber,CasualUI.Muted})if(Contrast(text,CasualUI.Card)<4.5f)throw new Exception("Semantic text contrast below 4.5:1: "+text);
  if(Contrast(Color.white,CasualUI.Green)<4.5f)throw new Exception("Primary action text contrast below 4.5:1");
  Debug.Log("BRIGHT_UI_THEME_OK light opaque body / rounded corners / shared cache");
 }
 static float Linear(float c){return c<=.04045f?c/12.92f:Mathf.Pow((c+.055f)/1.055f,2.4f);}
 static float L(Color c){return .2126f*Linear(c.r)+.7152f*Linear(c.g)+.0722f*Linear(c.b);}
 static float Contrast(Color a,Color b){return (Mathf.Max(L(a),L(b))+.05f)/(Mathf.Min(L(a),L(b))+.05f);}
}
