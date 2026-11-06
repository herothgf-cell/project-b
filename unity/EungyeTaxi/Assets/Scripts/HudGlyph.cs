using UnityEngine;
// Filled, antialiased casual-game icon family. Existing IDs retain their meaning.
// 0 home, 1 car, 2 warning, 3 location, 4 money, 5 profit, 6 mood,
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
    case 0: Poly(false,2,10,12,2,22,10,20,12,19,11,19,21,5,21,5,11,4,12);RoundBox(10,14,4,8,1,true);break;
    case 1: Car();break;
    case 2: Poly(false,12,2,23,21,1,21);RoundBox(10.6f,8,2.8f,7,1,true);Disc(12,18,1.5f,true);break;
    case 3: Disc(12,9,7.7f);Poly(false,5.4f,12,12,23,18.6f,12);Disc(12,8.8f,3,true);break;
    case 4: Disc(12,12,10);Ring(12,12,7.8f,true,.55f);Stroke(7.4f,8.5f,9.3f,15.5f,1,true);Stroke(9.3f,15.5f,12,10.5f,1,true);Stroke(12,10.5f,14.7f,15.5f,1,true);Stroke(14.7f,15.5f,16.6f,8.5f,1,true);Stroke(7,11.8f,17,11.8f,.65f,true);break;
    case 5: RoundBox(3,14,4,7,1);RoundBox(9,10,4,11,1);RoundBox(15,5,4,16,1);Poly(false,12,4,22,2,21,12,18,8);break;
    case 6: Disc(12,12,10);Disc(8.3f,9,1.35f,true);Disc(15.7f,9,1.35f,true);Poly(true,6.7f,13.5f,9,15,15,15,17.3f,13.5f,16,17,12,18.2f,8,17);break;
    case 7: Poly(false,12,1.8f,21.5f,5.5f,20.5f,15,17,19.5f,12,23,7,19.5f,3.5f,15,2.5f,5.5f);Stroke(7,12,10.5f,15.5f,1.5f,true);Stroke(10.5f,15.5f,17,8.5f,1.5f,true);break;
    case 8: RoundBox(3,2,12,19,2);RoundBox(5.5f,4.5f,7,5.5f,1,true);RoundBox(1,20,16,3,1);Stroke(15,11,17,13,1.4f);Stroke(17,13,17,18,1.4f);Stroke(17,18,21,18,1.4f);Stroke(21,18,21,9,1.4f);Stroke(18,5,21,9,1.4f);RoundBox(19,7,3,5,1);break;
    case 9: RoundBox(3,2,2,7,.7f);RoundBox(6.5f,2,2,7,.7f);RoundBox(10,2,2,7,.7f);RoundBox(3,7,9,4,1.5f);RoundBox(6,9,3,13,1.3f);Poly(false,19,2,21,2,21,22,17.5f,22,17.5f,14,14.5f,14,14.5f,9,16,5);break;
    case 10: Disc(12,12,10);RoundBox(8,5,3,14,1,true);RoundBox(9,5,8,8,3,true);RoundBox(11,7.5f,3.5f,3,1);break;
    case 11: RoundBox(2,6,3,16,1);RoundBox(19,12,3,10,1);RoundBox(4,12,16,7,1.5f);RoundBox(6,7,5,4,1.5f);RoundBox(12,9,7,4,1.5f);break;
    case 12: Stroke(5,19,16,8,2.6f);Disc(5,19,3);Disc(17,7,6);Poly(true,17,0,16,6,19,9,24,7,24,0);Disc(5,19,1,true);break;
    case 13: for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Stroke(12+6*Mathf.Cos(a),12+6*Mathf.Sin(a),12+9.5f*Mathf.Cos(a),12+9.5f*Mathf.Sin(a),2);}Disc(12,12,8);Disc(12,12,3.3f,true);break;
    case 14: Car();Poly(false,20,1,21,4,24,5,21,6,20,9,19,6,16,5,19,4);break;
    case 15: Disc(12,12,10);break;
    case 16: Poly(false,11,3,1,12,11,21,11,16,22,16,22,8,11,8);break;
    case 17: Poly(false,13,3,23,12,13,21,13,16,2,16,2,8,13,8);break;
    case 18: RoundBox(8,1,8,3,1);RoundBox(10.5f,3,3,4,1);Stroke(18,5,20,7,1.6f);Disc(12,14,9);Disc(12,14,6.1f,true);Stroke(12,9.5f,12,14,1.25f);Stroke(12,14,15.5f,16,1.25f);break;
    case 19: Stroke(5,5,19,19,2);Stroke(5,19,19,5,2);break;
   }
   var texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false);texture.name="Filled HUD icon "+kind;texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;texture.SetPixels(pixels);texture.Apply();cache[kind]=texture;pixels=null;
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
 static void Ring(float x,float y,float radius,bool clear=false,float stroke=1.4f){Paint(p=>Mathf.Abs(Vector2.Distance(p,new Vector2(x,y))-radius)-stroke,clear);}
 static void Box(float x,float y,float w,float h,bool clear=false){RoundBox(x,y,w,h,0,clear);}
 static void RoundBox(float x,float y,float w,float h,float radius,bool clear=false){Paint(p=>{var q=new Vector2(Mathf.Abs(p.x-x-w/2)-w/2+radius,Mathf.Abs(p.y-y-h/2)-h/2+radius);return new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;},clear);}
 static void Car(){Poly(false,3,11,6,4,18,4,21,11);RoundBox(2,10,20,9,2.5f);RoundBox(4,17,4,5,1.3f);RoundBox(16,17,4,5,1.3f);Poly(true,6,10,8,6.5f,16,6.5f,18,10);RoundBox(4,12.5f,4,2.5f,1,true);RoundBox(16,12.5f,4,2.5f,1,true);}
 static void Stroke(float x,float y,float u,float v,float radius,bool clear=false){var a=new Vector2(x,y);var d=new Vector2(u-x,v-y);float length=d.sqrMagnitude;Paint(p=>Vector2.Distance(p,a+d*(length==0?0:Mathf.Clamp01(Vector2.Dot(p-a,d)/length)))-radius,clear);}
 static void Poly(bool clear,params float[] xy){Paint(p=>{bool inside=false;float distance=float.MaxValue;int count=xy.Length/2;for(int i=0,j=count-1;i<count;j=i++){var a=new Vector2(xy[j*2],xy[j*2+1]);var b=new Vector2(xy[i*2],xy[i*2+1]);var d=b-a;distance=Mathf.Min(distance,Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude)));if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;}return inside?-distance:distance;},clear);}
 // Color badges are opt-in; Draw remains tintable white for blue headers and map tools.
 public static void DrawBadge(Rect r,int kind,Color? accent=null){
  if(kind<0||kind>=cache.Length)return;
  Color tint=accent??(kind==4?new Color(.96f,.62f,.08f):kind==6||kind==7?new Color(.13f,.64f,.34f):kind==2?CasualUI.Danger:CasualUI.Blue);
  Draw(new Rect(r.x,r.y+r.height*.055f,r.width,r.height),15,new Color(.08f,.18f,.29f,.17f));
  Draw(r,15,Color.Lerp(tint,Color.black,.22f));
  Rect face=new Rect(r.x+r.width*.025f,r.y,r.width*.95f,r.height*.95f);
  Draw(face,15,Color.Lerp(tint,Color.white,.35f));face.y+=r.height*.035f;Draw(face,15,tint);
  float inset=kind==4||kind==6?.06f:.17f;
  var symbol=new Rect(r.x+r.width*inset,r.y+r.height*inset,r.width*(1-2*inset),r.height*(1-2*inset));
  if(kind==4||kind==6){Draw(symbol,15,Color.Lerp(tint,Color.black,kind==4?.40f:.65f));Draw(symbol,kind,kind==4?new Color(1,.85f,.31f):Color.Lerp(tint,Color.white,.57f));}
  else if(kind==7){Draw(face,15,Color.Lerp(tint,Color.black,.48f));Draw(symbol,kind,Color.Lerp(tint,Color.white,.30f));}
  else Draw(symbol,kind,Color.white);
 }
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
