using UnityEngine;

// Play feedback round 7 (2026-11-26): road-side action labels ("[F] 식사", "[F] 주유" ...) show the F as a key cap,
// like the HUD shortcut hints. The cap is a sprite (Resources/UI/key-cap-f.png, tools/art/draw_key_cap.py) placed
// left of the text and parented to it, so it turns with FaceCamera and hides with it.
public static class WorldKeyCap {
 static Sprite sprite;static bool loaded;
 static Sprite Sprite {get{if(!loaded){loaded=true;var t=Resources.Load<Texture2D>("UI/key-cap-f");if(t)sprite=UnityEngine.Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),t.height);}return sprite;}}
 // Turns a "[F] text" label into a key cap + "text". Other labels are left as they are.
 public static void Apply(TextMesh text){
  if(!text||text.text==null||!text.text.StartsWith("[F] "))return;
  text.text=text.text.Substring(4);if(!Sprite){text.text="F · "+text.text;return;}
  float h=text.fontSize*text.characterSize*.1f;float width=0;foreach(char c in text.text)width+=c>=0xAC00&&c<=0xD7A3?h*.95f:h*.5f;
  float offset=text.anchor==TextAnchor.MiddleCenter||text.anchor==TextAnchor.UpperCenter||text.anchor==TextAnchor.LowerCenter?width*.5f:0;
  var cap=new GameObject("키캡 F");cap.transform.SetParent(text.transform,false);
  float size=h*1.25f;cap.transform.localPosition=new Vector3(-offset-size*.75f,0,0);cap.transform.localScale=Vector3.one*size;
  var r=cap.AddComponent<SpriteRenderer>();r.sprite=Sprite;
 }
}
