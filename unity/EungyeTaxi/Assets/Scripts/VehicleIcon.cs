using System.Collections.Generic;
using UnityEngine;

// Anti-aliased side-view taxi icon rendered once per (model, colour, ornament) into a cached texture.
// Proportions and paint follow VehicleSpec, matching the 3D variant. Front faces right.
public static class VehicleIcon {
 const int W=320,H=160;
 static readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
 public static void Draw(Rect r,VehicleSpec v,int color,bool ornament,bool dim=false){
  var t=Get(v,color,ornament);var old=GUI.color;if(dim)GUI.color=new Color(1,1,1,.38f);
  float scale=Mathf.Min(r.width/W,r.height/H);var fit=new Rect(r.center.x-W*scale/2,r.center.y-H*scale/2,W*scale,H*scale);GUI.DrawTexture(fit,t,ScaleMode.StretchToFill,true);GUI.color=old;
 }
 public static Texture2D Get(VehicleSpec v,int color,bool ornament){
  v=v??VehicleCatalog.Standard;color=color==1?1:0;string key=v.id+":"+color+":"+ornament;Texture2D t;
  if(cache.TryGetValue(key,out t)&&t)return t;t=Render(v,color,ornament);cache[key]=t;return t;
 }
 // Signed distances in pixels (negative inside).
 static float Box(Vector2 p,Vector2 c,Vector2 half,float radius){var q=new Vector2(Mathf.Abs(p.x-c.x),Mathf.Abs(p.y-c.y))-half+new Vector2(radius,radius);return new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;}
 static float Circle(Vector2 p,Vector2 c,float radius){return (p-c).magnitude-radius;}
 static float Ellipse(Vector2 p,Vector2 c,Vector2 radii){var d=new Vector2((p.x-c.x)/radii.x,(p.y-c.y)/radii.y);return (d.magnitude-1)*Mathf.Min(radii.x,radii.y);}
 // Convex quad (counter-clockwise), rounded by radius.
 static float Quad(Vector2 p,Vector2[] q,float radius){
  float inside=-1e9f,edge=1e9f;
  for(int i=0;i<4;i++){var a=q[i];var b=q[(i+1)%4];var e=b-a;var n=new Vector2(e.y,-e.x).normalized;inside=Mathf.Max(inside,Vector2.Dot(p-a,n));float h=Mathf.Clamp01(Vector2.Dot(p-a,e)/e.sqrMagnitude);edge=Mathf.Min(edge,(p-a-e*h).magnitude);}
  return (inside<=0?-edge:edge)-radius;
 }
 static Vector2[] Inset(Vector2[] q,float d){var c=(q[0]+q[1]+q[2]+q[3])/4;var r=new Vector2[4];for(int i=0;i<4;i++){var dir=(c-q[i]).normalized;r[i]=q[i]+dir*d;}return r;}
 static void Paint(Color[] px,System.Func<Vector2,float> shape,Color c,float alpha=1){
  for(int y=0;y<H;y++)for(int x=0;x<W;x++){float d=shape(new Vector2(x+.5f,y+.5f));float a=Mathf.Clamp01(.5f-d)*alpha*c.a;if(a<=0)continue;int i=y*W+x;var dst=px[i];float outA=a+dst.a*(1-a);if(outA<=0)continue;var rgb=(new Color(c.r,c.g,c.b)*a+new Color(dst.r,dst.g,dst.b)*dst.a*(1-a))/outA;px[i]=new Color(rgb.r,rgb.g,rgb.b,outA);}
 }
 static Texture2D Render(VehicleSpec v,int color,bool ornament){
  var px=new Color[W*H];for(int i=0;i<px.Length;i++)px[i]=new Color(0,0,0,0);
  var c=v.colors[color];var paint=new Color(c[0],c[1],c[2]);var shade=Color.Lerp(paint,Color.black,.28f);var light=Color.Lerp(paint,Color.white,.35f);
  var ink=new Color(.10f,.12f,.16f);var glass=new Color(.18f,.30f,.42f);var glassLight=new Color(.42f,.58f,.72f);var chrome=new Color(.78f,.82f,.86f);
  float sx=v.scaleZ,sy=v.scaleY,cx=W/2f,ground=22,wheelR=19,wheelY=ground+wheelR;
  float bodyBottom=ground+12,bodyTop=bodyBottom+46*sy,half=126*sx;
  float roof=bodyTop+38*sy;
  // Shadow, wheel wells, body, bumpers.
  Paint(px,p=>Ellipse(p,new Vector2(cx,ground),new Vector2(half+6,7)),new Color(0,0,0,.18f));
  var cabin=new[]{new Vector2(cx-half*.62f,bodyTop-2),new Vector2(cx+half*.50f,bodyTop-2),new Vector2(cx+half*.26f,roof),new Vector2(cx-half*.42f,roof)};
  Paint(px,p=>Quad(p,cabin,7),paint);
  Paint(px,p=>Box(p,new Vector2(cx,(bodyBottom+bodyTop)/2),new Vector2(half,(bodyTop-bodyBottom)/2),17),paint);
  Paint(px,p=>Box(p,new Vector2(cx,bodyTop-7),new Vector2(half-14,3),3),light,.55f);
  Paint(px,p=>Box(p,new Vector2(cx,bodyBottom+5),new Vector2(half+1,5),5),shade);
  // Windows with a pillar, glint.
  var win=Inset(cabin,7);
  Paint(px,p=>Quad(p,win,3),glass);
  Paint(px,p=>Box(p,new Vector2(cx-half*.05f,(bodyTop+roof)/2),new Vector2(3.5f,(roof-bodyTop)/2),1.5f),paint);
  Paint(px,p=>Box(p,new Vector2(cx+half*.12f,roof-11),new Vector2(half*.12f,2.2f),2),glassLight,.7f);
  if(ornament)Paint(px,p=>Circle(p,new Vector2(cx+half*.22f,bodyTop+8),6),new Color(.30f,.66f,.32f));
  // Door seams and handles.
  foreach(float at in new[]{-.05f,.40f})Paint(px,p=>Box(p,new Vector2(cx+half*at,(bodyBottom+bodyTop)/2+3),new Vector2(1,(bodyTop-bodyBottom)/2-8),1),shade,.7f);
  foreach(float at in new[]{-.25f,.20f})Paint(px,p=>Box(p,new Vector2(cx+half*at,bodyTop-15),new Vector2(7,2),2),chrome);
  // Lamps (front = right) and roof sign.
  Paint(px,p=>Box(p,new Vector2(cx+half-6,bodyTop-15),new Vector2(7,6),4),new Color(1,.96f,.78f));
  Paint(px,p=>Box(p,new Vector2(cx-half+5,bodyTop-15),new Vector2(5,7),3),new Color(.86f,.13f,.12f));
  Paint(px,p=>Box(p,new Vector2(cx-half*.08f,roof+7),new Vector2(17,6.5f),4),new Color(1,.86f,.28f));
  Paint(px,p=>Box(p,new Vector2(cx-half*.08f,roof+7),new Vector2(10,1.6f),1),ink,.55f);
  // Wheels: arch, tyre, hub.
  foreach(float at in new[]{-.66f,.64f}){var w=new Vector2(cx+half*at,wheelY);
   Paint(px,p=>Circle(p,w,wheelR+4),shade);Paint(px,p=>Circle(p,w,wheelR),ink);Paint(px,p=>Circle(p,w,8.5f),chrome);Paint(px,p=>Circle(p,w,3),new Color(.45f,.5f,.55f));}
  var t=new Texture2D(W,H,TextureFormat.RGBA32,false){name="Vehicle icon "+v.id+color,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear,hideFlags=HideFlags.DontSave};
  t.SetPixels(px);t.Apply(false,true);return t;
 }
}
