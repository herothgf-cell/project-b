using UnityEngine;

// Logical layout scales up on large displays; key text has a physical pixel floor.
public static class UiReadability {
 public static float HudScale(float width,float height){return Mathf.Clamp(Mathf.Min(width/1600f,height/900f),.5f,2.5f);}
 public static float MapScale(float width,float height){return Mathf.Max(1,HudScale(width,height));}
 public static int FontFor(int reference,float scale){return Mathf.Max(reference+2,Mathf.CeilToInt(16/Mathf.Max(.1f,scale)));}
 public static int Font(int reference){return FontFor(reference,CockpitUI.Scale);}
 public static string Ellipsis(string value,GUIStyle style,float width){
  if(string.IsNullOrEmpty(value)||style.CalcSize(new GUIContent(value)).x<=width)return value;
  int count=value.Length;while(count>0&&style.CalcSize(new GUIContent(value.Substring(0,count)+"…")).x>width)count--;
  return value.Substring(0,count)+"…";
 }
 public static Rect ScreenRect(Rect logical,float scale){return new Rect(logical.x*scale,logical.y*scale,logical.width*scale,logical.height*scale);}
}
