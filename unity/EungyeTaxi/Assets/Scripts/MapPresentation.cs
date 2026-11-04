using System;
using System.Collections.Generic;
using UnityEngine;

// Display-only atlas: all footprints come from the loaded map, never synthetic streets.
public static class MapPresentation {
 public static readonly Color Land=new Color(.91f,.93f,.88f),Water=new Color(.60f,.82f,.94f),Park=new Color(.70f,.85f,.65f),Building=new Color(.82f,.84f,.87f),Road=new Color(.995f,.995f,.98f),RoadEdge=new Color(.81f,.84f,.80f);
 public static readonly Color Pickup=new Color(.04f,.40f,.94f),Passenger=new Color(.90f,.17f,.46f);
 public static readonly Color PreviousRoute=new Color(.45f,.60f,.66f);
 public static Color RouteColor(bool passenger){return passenger?Passenger:Pickup;}
 public static Texture2D Create(MapData map,int size){
  if(map==null||map.width<=0||map.depth<=0||size<2)throw new ArgumentException("Invalid map atlas dimensions");
  var raster=new MapRouteRaster(size,size);for(int i=0;i<raster.Pixels.Length;i++)raster.Pixels[i]=Land;
  if(map.areas!=null)foreach(var area in map.areas)if(area!=null&&area.kind!="road"&&area.kind!="sidewalk")Fill(raster,map,area.points,area.kind=="water"?Water:area.kind=="land"||area.kind=="park"||area.kind=="forest"?Park:Land);
  if(map.buildings!=null)foreach(var building in map.buildings)if(building!=null)Fill(raster,map,building.points,Building);
  // Actual widths retain major/minor road hierarchy. Two passes avoid seams at junctions.
  for(int pass=0;pass<2;pass++)if(map.roads!=null)foreach(var road in map.roads){if(road==null||road.points==null)continue;
   float width=Mathf.Max(2,road.width)+(pass==0?2*Mathf.Max(map.width,map.depth)/size:0);
   for(int i=1;i<road.points.Length;i++)Stroke(raster,map,road.points[i-1],road.points[i],width,pass==0?RoadEdge:Road);
  }
  // Surveyed surface polygons remain authoritative, including broad intersections.
  if(map.areas!=null)foreach(var area in map.areas)if(area!=null&&(area.kind=="road"||area.kind=="sidewalk"))Fill(raster,map,area.points,area.kind=="road"?Road:new Color(.88f,.89f,.86f));
  var texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name="Bright map footprints",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};texture.SetPixels32(raster.Pixels);texture.Apply(true,false);return texture;
 }
 static void Stroke(MapRouteRaster raster,MapData map,MapPoint a,MapPoint b,float width,Color32 color){
  var along=new Vector2(b.x-a.x,b.z-a.z).normalized*width*.5f;var side=new Vector2(-along.y,along.x);
  if(along.sqrMagnitude<.0001f){along=new Vector2(width*.5f,0);side=new Vector2(0,width*.5f);}
  Fill(raster,map,new[]{new MapPoint{x=a.x-along.x+side.x,z=a.z-along.y+side.y},new MapPoint{x=b.x+along.x+side.x,z=b.z+along.y+side.y},new MapPoint{x=b.x+along.x-side.x,z=b.z+along.y-side.y},new MapPoint{x=a.x-along.x-side.x,z=a.z-along.y-side.y}},color);
 }
 static void Fill(MapRouteRaster raster,MapData map,MapPoint[] points,Color32 color){
  if(points==null||points.Length<3)return;float min=float.MaxValue,max=float.MinValue;
  foreach(var p in points){min=Mathf.Min(min,p.z);max=Mathf.Max(max,p.z);}int size=raster.Width;
  int first=Mathf.Max(0,Mathf.CeilToInt(min/map.depth*size-.5f)),last=Mathf.Min(size-1,Mathf.FloorToInt(max/map.depth*size-.5f));var crossings=new List<float>(points.Length);
  for(int y=first;y<=last;y++){float z=(y+.5f)/size*map.depth;crossings.Clear();
   for(int i=0,j=points.Length-1;i<points.Length;j=i++){var a=points[j];var b=points[i];if((a.z>z)!=(b.z>z))crossings.Add((a.x+(z-a.z)*(b.x-a.x)/(b.z-a.z))/map.width*size);}
   crossings.Sort();for(int i=1;i<crossings.Count;i+=2){int left=Mathf.Max(0,Mathf.CeilToInt(crossings[i-1]-.5f)),right=Mathf.Min(size-1,Mathf.FloorToInt(crossings[i]-.5f));for(int x=left;x<=right;x++)raster.Pixels[y*size+x]=color;}
  }
 }
 static Texture2D pin;
 public static Rect PinBounds(Vector2 anchor,float size){return new Rect(anchor.x-size/2,anchor.y-size*4/3,size,size*4/3);}
 public static Rect PinHitBounds(Vector2 anchor,float size){var r=PinBounds(anchor,size);return new Rect(r.x-3,r.y-3,r.width+6,r.height+8);}
 public static void Pin(Vector2 center,Color color,float size=26,string number=""){
  if(!pin){pin=new Texture2D(48,64,TextureFormat.RGBA32,false){name="Map location pin",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
   for(int y=0;y<64;y++)for(int x=0;x<48;x++){float gx=x+.5f,gy=64-y-.5f;float circle=23-Vector2.Distance(new Vector2(gx,gy),new Vector2(24,24));float tip=gy>=24&&gy<=63?Mathf.Min(63-gy,(63-gy)*.58f-Mathf.Abs(gx-24)):-100;pin.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(Mathf.Max(circle,tip))));}pin.Apply();}
  var old=GUI.color;var r=PinBounds(center,size);GUI.color=Color.white;GUI.DrawTexture(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),pin);GUI.color=color;GUI.DrawTexture(r,pin);GUI.color=Color.white;
  if(string.IsNullOrEmpty(number))CasualUI.RoundedFill(new Rect(center.x-size*.13f,r.y+size*.37f,size*.26f,size*.26f),Color.white);
  else GUI.Label(new Rect(r.x,r.y,size,size),number,new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=12,alignment=TextAnchor.MiddleCenter,normal={textColor=Color.white}});
  GUI.color=old;
 }
 static readonly Texture2D[] players=new Texture2D[72];
 static Texture2D locationDisc;
 public static Vector2 PlayerAnchor(Rect viewport,Vector2 point){return new Vector2(Mathf.Clamp(point.x,viewport.xMin+38,viewport.xMax-38),Mathf.Clamp(point.y,viewport.yMin+82,viewport.yMax-38));}
 static void Disc(Vector2 p,float size,Color color){
  if(!locationDisc){locationDisc=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};for(int y=0;y<64;y++)for(int x=0;x<64;x++)locationDisc.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(32-Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*32))));locationDisc.Apply();}
  var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),locationDisc);GUI.color=old;
 }
 public static void EmphasizedPlayer(Vector2 point,Vector2 forward,Rect viewport,bool pulse){
  if(Event.current.type!=EventType.Repaint)return;var p=PlayerAnchor(viewport,point);bool outside=!viewport.Contains(point);
  float halo=pulse?76+8*Mathf.Sin(Time.unscaledTime*6):64;
  Disc(p,halo,new Color(.10f,.48f,1,.25f));Disc(p,56,Color.white);Disc(p,50,Pickup);Disc(p,43,Color.white);
  Player(p,outside?(point-p).normalized:forward,38);
  var label=new Rect(Mathf.Clamp(p.x-74,viewport.xMin,viewport.xMax-148),Mathf.Clamp(p.y+31,viewport.yMin,viewport.yMax-31),148,28);
  CasualUI.Panel(label);GUI.Label(label,outside?"내 차 · 지도 밖":"내 위치",new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=17,alignment=TextAnchor.MiddleCenter,normal={textColor=CasualUI.Blue}});
 }
 public static void Player(Vector2 center,Vector2 forward,float size=30){
  if(Event.current.type!=EventType.Repaint)return;
  int index=Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(forward.x,-forward.y)*Mathf.Rad2Deg,360)/5)%72;
  if(!players[index]){
   var texture=new Texture2D(48,48,TextureFormat.RGBA32,false){name="Map heading "+index,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
   float angle=index*5*Mathf.Deg2Rad;var direction=new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle));var side=new Vector2(-direction.y,direction.x);
   for(int y=0;y<48;y++)for(int x=0;x<48;x++){var d=new Vector2(x+.5f-24,47-y+.5f-24);float along=Vector2.Dot(d,direction),across=Mathf.Abs(Vector2.Dot(d,side));
    bool inside=along>=-14&&along<=20&&across<=(20-along)*.48f;
    bool inset=along>=-10&&along<=14&&across<=(14-along)*.43f;
    texture.SetPixel(x,y,!inside?Color.clear:inset?new Color(1,.81f,.12f):CasualUI.Ink);
   }texture.Apply();players[index]=texture;
  }
  var old=GUI.color;GUI.color=Color.white;GUI.DrawTexture(new Rect(center.x-size/2,center.y-size/2,size,size),players[index]);GUI.color=old;
 }
}
