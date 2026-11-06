using UnityEngine;
// One rounded, antialiased icon family. Existing IDs retain their meaning.
// 0 home, 1 car, 2 warning, 3 location, 4 wallet, 5 profit, 6 mood,
// 7 safety, 8 fuel, 9 meal, 10 parking, 11 rest, 12 repair, 13 settings, 14 clean, 15 dot.
// 16/17 indicators, 18 timer, 19 close.
public static class HudGlyph {
 const int Size=96;
 static readonly Texture2D[] cache=new Texture2D[20];static Color[] pixels;
 public static void Draw(Rect r,int kind,Color c){
  if(kind<0||kind>=cache.Length)return;
  if(!cache[kind]){
   pixels=new Color[Size*Size];
   switch(kind){
    case 0: Path(3,10,12,3,21,10);Path(5,9,5,21,10,21,10,15,14,15,14,21,19,21,19,9);break;
    case 1: Car();break;
    case 2: Path(12,3,22,21,2,21,12,3);Line(12,9,12,14);Disc(12,18,1);break;
    case 3: Ring(12,9,7);Path(6,13,12,22,18,13);Disc(12,9,2);break;
    case 4: RoundBox(3,6,18,15,2);Box(5,8,14,11,true);Path(4,6,4,4,17,4);RoundBox(14,11,8,6,1.5f);Disc(17,14,.9f,true);break;
    case 5: Path(3,3,3,21,21,21);Path(6,16,11,11,15,14,21,5);Path(16,5,21,5,21,10);break;
    case 6: Ring(12,12,9);Disc(8,9,1);Disc(16,9,1);Path(7,14,9,17,15,17,17,14);break;
    case 7: Path(12,2,21,6,20,15,12,22,4,15,3,6,12,2);Path(7,11,11,15,17,8);break;
    case 8: RoundBox(4,3,11,18,1.5f);Box(6,5,7,5,true);Line(2,22,17,22);Path(16,7,20,11,20,19,17,19,17,12);break;
    case 9: Path(3,3,3,8,9,8,9,3);Line(6,3,6,21);Path(19,3,15,8,15,12,19,12,19,21);Line(19,3,19,12);break;
    case 10: Ring(12,12,9);Path(9,18,9,6,14,6,17,8,17,11,14,13,9,13);break;
    case 11: Path(3,5,3,21);Path(21,10,21,21);Line(3,18,21,18);RoundBox(5,9,5,6,1.4f);RoundBox(11,12,9,3,1);break;
    case 12: Path(14,4,13,8,17,12,21,11,22,7,18,9,16,7,17,3,14,4);Path(14,10,4,19,4,21,6,21,17,12);break;
    case 13: Ring(12,12,6.5f);Ring(12,12,2.4f);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(12+7*Mathf.Cos(a),12+7*Mathf.Sin(a),12+9.5f*Mathf.Cos(a),12+9.5f*Mathf.Sin(a));}break;
    case 14: Path(3,13,6,8,16,8,19,13);Path(3,13,3,20,19,20,19,13,3,13);Line(6,20,6,22);Line(16,20,16,22);Path(20,2,20,8);Line(17,5,23,5);break;
    case 15: Disc(12,12,10);break;
    case 16: Path(11,5,4,12,11,19);Line(4,12,21,12);break;
    case 17: Path(13,5,20,12,13,19);Line(3,12,20,12);break;
    case 18: Ring(12,13,8);Path(12,8,12,13,16,15);Line(9,2,15,2);Line(12,2,12,5);break;
    case 19: Line(5,5,19,19);Line(5,19,19,5);break;
   }
   var texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false);texture.name="Rounded HUD icon "+kind;texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;texture.SetPixels(pixels);texture.Apply();cache[kind]=texture;pixels=null;
  }
  var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,cache[kind]);GUI.color=old;
 }
 // Distances in the common 24-unit view box give round caps and subpixel edges.
 static void Paint(System.Func<Vector2,float> distance,bool clear=false){
  for(int y=0;y<Size;y++)for(int x=0;x<Size;x++){
   float coverage=Mathf.Clamp01(.5f-distance(new Vector2((x+.5f)*24/Size,(y+.5f)*24/Size))*Size/24);
   int at=(Size-1-y)*Size+x;float alpha=clear?pixels[at].a*(1-coverage):Mathf.Max(pixels[at].a,coverage);pixels[at]=new Color(1,1,1,alpha);
  }
 }
 static void Line(float x,float y,float u,float v){var a=new Vector2(x,y);var d=new Vector2(u-x,v-y);float length=d.sqrMagnitude;Paint(p=>Vector2.Distance(p,a+d*(length==0?0:Mathf.Clamp01(Vector2.Dot(p-a,d)/length)))-1);}
 static void Path(params float[] xy){for(int i=2;i<xy.Length;i+=2)Line(xy[i-2],xy[i-1],xy[i],xy[i+1]);}
 static void Disc(float x,float y,float radius,bool clear=false){Paint(p=>Vector2.Distance(p,new Vector2(x,y))-radius,clear);}
 static void Ring(float x,float y,float radius){Paint(p=>Mathf.Abs(Vector2.Distance(p,new Vector2(x,y))-radius)-1);}
 static void Box(float x,float y,float w,float h,bool clear=false){RoundBox(x,y,w,h,0,clear);}
 static void RoundBox(float x,float y,float w,float h,float radius,bool clear=false){Paint(p=>{var q=new Vector2(Mathf.Abs(p.x-x-w/2)-w/2+radius,Mathf.Abs(p.y-y-h/2)-h/2+radius);return new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;},clear);}
 static void Car(){Path(4,11,7,5,17,5,20,11);RoundBox(3,10,18,9,2);Box(5,12,14,5,true);Line(6,19,6,21);Line(18,19,18,21);Disc(6,14,1);Disc(18,14,1);}
 public static bool Button(Rect r,int kind,string tooltip,bool active=false){bool hit=GUI.Button(r,new GUIContent("",tooltip));Draw(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),kind,active?CasualUI.Blue:CasualUI.Muted);return hit;}
 // Axis-aligned stamps preserve clipping inside scaled IMGUI groups/windows.
 // The live minimap route is separately cached into one raster texture.
 public static void MapLine(Vector2 a,Vector2 b,Color c,float width,Rect clip){
  if(Event.current==null||Event.current.type!=EventType.Repaint||width<=0||float.IsNaN(width)||float.IsInfinity(width))return;
  Vector2 start,end;if(!TryClipMapLine(a,b,clip,out start,out end))return;
  float length=Vector2.Distance(start,end);int steps=Mathf.Max(1,Mathf.CeilToInt(length/Mathf.Max(1,width*.45f)));var oldColor=GUI.color;
  try{GUI.color=c;for(int i=0;i<=steps;i++){var p=Vector2.Lerp(start,end,(float)i/steps);GUI.DrawTexture(new Rect(p.x-width/2,p.y-width/2,width,width),Texture2D.whiteTexture);}}
  finally{GUI.color=oldColor;}
 }
 public static bool TryClipMapLine(Vector2 a,Vector2 b,Rect clip,out Vector2 start,out Vector2 end){
  start=a;end=b;if(float.IsNaN(a.x)||float.IsNaN(a.y)||float.IsNaN(b.x)||float.IsNaN(b.y)||float.IsInfinity(a.x)||float.IsInfinity(a.y)||float.IsInfinity(b.x)||float.IsInfinity(b.y))return false;
  Vector2 delta=b-a;float lo=0,hi=1;
  if(!Cut(-delta.x,a.x-clip.xMin,ref lo,ref hi)||!Cut(delta.x,clip.xMax-a.x,ref lo,ref hi)||!Cut(-delta.y,a.y-clip.yMin,ref lo,ref hi)||!Cut(delta.y,clip.yMax-a.y,ref lo,ref hi))return false;
  start=a+delta*lo;end=a+delta*hi;return true;
 }
 static bool Cut(float p,float q,ref float lo,ref float hi){if(Mathf.Abs(p)<.00001f)return q>=0;float t=q/p;if(p<0){if(t>hi)return false;lo=Mathf.Max(lo,t);}else{if(t<lo)return false;hi=Mathf.Min(hi,t);}return true;}
}
