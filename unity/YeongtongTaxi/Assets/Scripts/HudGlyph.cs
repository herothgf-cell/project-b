using UnityEngine;
// Small geometric glyphs: no emoji/font dependency. IDs: facility, traffic, law, location, wallet, profit.
public static class HudGlyph {
 static readonly Texture2D[] cache=new Texture2D[6];static Texture2D drawing;
 public static void Draw(Rect r,int kind,Color c){if(cache[kind]==null){drawing=new Texture2D(24,24,TextureFormat.RGBA32,false);drawing.SetPixels(new Color[24*24]);
  if(kind==0){Box(3,10,18,12);Line(2,10,12,2);Line(12,2,22,10);Box(10,15,4,7,true);}
  else if(kind==1){Box(3,6,18,12);Box(5,3,14,5);Box(4,18,4,4);Box(16,18,4,4);Box(6,8,12,5,true);}
  else if(kind==2){Line(12,2,22,12);Line(22,12,12,22);Line(12,22,2,12);Line(2,12,12,2);Box(11,6,2,8);Box(11,17,2,2);}
  else if(kind==3){Line(12,0,12,6);Line(12,18,12,24);Line(0,12,6,12);Line(18,12,24,12);for(int i=0;i<16;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Line(12+7*Mathf.Cos(a),12+7*Mathf.Sin(a),12+7*Mathf.Cos(b),12+7*Mathf.Sin(b));}}
  else if(kind==4){Box(2,5,20,15);Box(14,9,9,7,true);Box(17,11,2,2);Line(3,3,18,3);}
  else{Line(3,21,3,3);Line(3,21,22,21);Line(6,17,12,11);Line(12,11,16,13);Line(16,13,22,4);Line(17,4,22,4);}
  drawing.Apply();cache[kind]=drawing;drawing=null;}
  var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,cache[kind]);GUI.color=old;
 }
 static void Box(int x,int y,int w,int h,bool clear=false){for(int i=x;i<x+w;i++)for(int j=y;j<y+h;j++)if(i>=0&&i<24&&j>=0&&j<24)drawing.SetPixel(i,23-j,clear?Color.clear:Color.white);}
 static void Line(float x,float y,float u,float v){int count=Mathf.CeilToInt(Vector2.Distance(new Vector2(x,y),new Vector2(u,v))*2);for(int i=0;i<=count;i++){float t=count==0?0:(float)i/count;Box(Mathf.RoundToInt(Mathf.Lerp(x,u,t)),Mathf.RoundToInt(Mathf.Lerp(y,v,t)),2,2);}}
 public static bool Button(Rect r,int kind,string tooltip,bool active=false){bool hit=GUI.Button(r,new GUIContent("",tooltip));Draw(new Rect(r.x+10,r.y+8,r.width-20,r.height-16),kind,active?new Color(.4f,1,.8f):Color.gray);return hit;}
 // Axis-aligned stamps avoid Unity IMGUI's rotated clipping bug inside scaled groups/windows.
 public static void MapLine(Vector2 a,Vector2 b,Color c,float width,Rect clip){
  Vector2 delta=b-a;float lo=0,hi=1;
  if(!Cut(-delta.x,a.x-clip.xMin,ref lo,ref hi)||!Cut(delta.x,clip.xMax-a.x,ref lo,ref hi)||!Cut(-delta.y,a.y-clip.yMin,ref lo,ref hi)||!Cut(delta.y,clip.yMax-a.y,ref lo,ref hi))return;
  b=a+delta*hi;a+=delta*lo;float length=Vector2.Distance(a,b);int steps=Mathf.Max(1,Mathf.CeilToInt(length/Mathf.Max(1,width*.45f)));var old=GUI.color;GUI.color=c;
  for(int i=0;i<=steps;i++){var p=Vector2.Lerp(a,b,(float)i/steps);GUI.DrawTexture(new Rect(p.x-width/2,p.y-width/2,width,width),Texture2D.whiteTexture);}GUI.color=old;
 }
 static bool Cut(float p,float q,ref float lo,ref float hi){if(Mathf.Abs(p)<.00001f)return q>=0;float t=q/p;if(p<0){if(t>hi)return false;lo=Mathf.Max(lo,t);}else{if(t<lo)return false;hi=Mathf.Min(hi,t);}return true;}
}
