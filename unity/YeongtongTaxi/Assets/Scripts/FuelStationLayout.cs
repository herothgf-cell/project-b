using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// Fictional game facilities, selected on empty plots in the generated Eungye map.
// The road graph is unchanged: navigation/steering stops before the entrance.
public static class FuelStationLayout {
 public sealed class FuelStation {
  public readonly string name,id;
  public readonly MapStop approach;
  public readonly Vector3 entrance,exit,pump,direction;
  public readonly Vector3[] drivePath,plot;
  internal readonly MapRoad[] surfaces;
  internal FuelStation(string label,string key,MapStop stop,Vector3 entry,Vector3 leave,Vector3 bay,Vector3 heading,Vector3[] path,Vector3[] boundary,MapRoad[] paving){name=label;id=key;approach=stop;entrance=entry;exit=leave;pump=bay;direction=heading;drivePath=path;plot=boundary;surfaces=paving;}
  public bool CanRefuel(Vector3 position,Vector3 heading,float speed){
   var delta=position-pump;delta.y=0;heading.y=0;
   return Mathf.Abs(position.y-pump.y)<1.5f&&speed<.56f&&heading.sqrMagnitude>.1f&&Vector3.Dot(heading.normalized,direction)>.9f&&delta.sqrMagnitude<=2.8f*2.8f&&Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(direction)))<1.15f;
  }
 }
 sealed class Layout {public readonly IReadOnlyList<FuelStation> stations;public readonly FuelStation rest;public Layout(List<FuelStation> entries,FuelStation restArea=null){stations=entries.AsReadOnly();rest=restArea;}}
 static readonly ConditionalWeakTable<MapData,Layout> layouts=new ConditionalWeakTable<MapData,Layout>();
 static readonly IReadOnlyList<FuelStation> none=new List<FuelStation>().AsReadOnly();
 static readonly string[] keys={"north","south","east"},labels={"은계북부 주유소 (게임시설)","은계남부 주유소 (게임시설)","은계동부 주유소 (게임시설)"};
 static readonly string[] laneIds={"local:5978:17124:10508:15987","local:5204:826:7659:817","local:8265:6305:9056:10851"};
 static readonly float[] laneAts={196,42,434},depths={17.5f,17.5f,17};
 static MapPoint Point(Vector3 value){return new MapPoint{x=value.x,z=value.z};}
 static MapRoad Surface(string id,float width,IList<Vector3> positions){var pts=new MapPoint[positions.Count];for(int i=0;i<pts.Length;i++)pts[i]=Point(positions[i]);return new MapRoad{id=id,name="주유소 게임시설 노면",width=width,lanes=1,sourceLanes=1,oneWay=1,suppressPaint=true,points=pts};}
 public static IReadOnlyList<FuelStation> For(MapData map){if(map==null)return none;Layout layout;return layouts.TryGetValue(map,out layout)?layout.stations:none;}
 public static FuelStation Rest(MapData map){Layout layout;return map!=null&&layouts.TryGetValue(map,out layout)?layout.rest:null;}
 public static void Apply(MapData map){
  if(map==null)return;Layout existing;if(layouts.TryGetValue(map,out existing))return;
  var entries=new List<FuelStation>();
  if(map.mapId!="eungye"||!map.hybrid||!map.HasHd){layouts.Add(map,new Layout(entries));return;}
  var graph=new HdRoadGraph(map);
  for(int site=0;site<keys.Length;site++){
   int edge=Array.FindIndex(map.hdEdges,e=>e.id==laneIds[site]&&!e.disabled);
   if(edge<0){Debug.LogWarning("FUEL_LAYOUT_MISSING reviewed Eungye lane "+laneIds[site]);continue;}
   var path=graph.paths[edge];float at=laneAts[site];
   if(at<32||at+32>path.length)throw new InvalidOperationException("Fuel station reviewed lane changed: "+keys[site]);
   entries.Add(CreateFacility(map,graph,edge,at,depths[site],keys[site],labels[site]));
  }
  // Never leave a partly populated set of facilities if an input changed.
  if(entries.Count!=3){Debug.LogWarning("FUEL_LAYOUT_UNAVAILABLE expected three reviewed Eungye sites; no facilities appended");entries.Clear();}
  FuelStation rest=null;int restEdge=Array.FindIndex(map.hdEdges,e=>e.id==laneIds[2]&&!e.disabled);
  if(entries.Count==3&&restEdge>=0){for(float at=laneAts[2]+80;at+32<graph.paths[restEdge].length;at+=20){try{rest=CreateFacility(map,graph,restEdge,at,17,"rest-east","은계 동부 휴식 주차구역 (게임시설)");break;}catch(InvalidOperationException){}}
   if(rest==null)throw new InvalidOperationException("No clear rest parking plot along east service corridor");}
  var roads=new List<MapRoad>(map.roads);foreach(var station in entries)roads.AddRange(station.surfaces);if(rest!=null)roads.AddRange(rest.surfaces);map.roads=roads.ToArray();
  layouts.Add(map,new Layout(entries,rest));
 }
 static FuelStation CreateFacility(MapData map,HdRoadGraph graph,int edge,float at,float depth,string key,string label){var path=graph.paths[edge];
   var centre=path.Position(at);var dir=path.Tangent(at);var right=TrafficSystem.Right(dir);
   Func<float,float,Vector3> q=(along,across)=>centre+dir*along+right*across;
   // Two smooth, separate driveways. Both meet this same rightmost traffic lane.
   var enter=new List<Vector3>();var leave=new List<Vector3>();
   for(int i=0;i<=24;i++){float along,across;SOffset(i/24f,18,depth,out along,out across);enter.Add(q(-24+along,across));leave.Add(q(6+along,depth-across));}
   enter[0]=path.Position(at-24);leave[leave.Count-1]=path.Position(at+24);
   var surfaces=new[]{Surface("fuel:"+key+":court",20,new[]{q(-12,depth),q(12,depth)}),Surface("fuel:"+key+":entrance",7.2f,enter),Surface("fuel:"+key+":exit",7.2f,leave)};
   ValidatePlot(map,surfaces,key);
   var approachPosition=path.Position(at-28);var approachDirection=path.Tangent(at-28);
   var stop=new MapStop{name=label+" 입구 · 내부 수동 운전",x=approachPosition.x,z=approachPosition.z,dx=approachDirection.x,dz=approachDirection.z,width=3,graphStop=true,laneEdge=edge,laneAt=at-28};
   var drive=new List<Vector3>(enter);drive.Add(q(0,depth));drive.AddRange(leave);
   return new FuelStation(label,key,stop,enter[0],leave[leave.Count-1],q(0,depth),dir,drive.ToArray(),new[]{q(-23,depth-10),q(23,depth-10),q(23,depth+10),q(-23,depth+10)},surfaces);
 }
 // Two tangent circular arcs give a roughly 9m minimum turning radius for these plots.
 // The same lateral shift with a cubic smoothstep tightens to about 3m at the mouth.
 static void SOffset(float t,float advance,float offset,out float along,out float across){
  float radius=(advance*advance+offset*offset)/(4*offset),angle=2*Mathf.Atan2(offset,advance);
  if(t<=.5f){float a=angle*t*2;along=radius*Mathf.Sin(a);across=radius*(1-Mathf.Cos(a));}
  else {float a=angle*(2-2*t);along=advance-radius*Mathf.Sin(a);across=offset-radius*(1-Mathf.Cos(a));}
 }
 static bool Inside(Vector3 p,MapPoint[] ring){bool result=false;for(int i=0,j=ring.Length-1;i<ring.Length;j=i++){var a=ring[i];var b=ring[j];if((a.z>p.z)!=(b.z>p.z)&&p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z)+a.x)result=!result;}return result;}
 static float PointSegment(Vector3 p,Vector3 a,Vector3 b){var delta=b-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,delta)/Mathf.Max(.00001f,delta.sqrMagnitude));return Vector3.Distance(p,a+delta*t);}
 static float Cross(Vector3 a,Vector3 b){return a.x*b.z-a.z*b.x;}
 static float SegmentDistance(Vector3 a,Vector3 b,Vector3 c,Vector3 d){var ab=b-a;var cd=d-c;float den=Cross(ab,cd);if(Mathf.Abs(den)>.00001f){float t=Cross(c-a,cd)/den,u=Cross(c-a,ab)/den;if(t>=0&&t<=1&&u>=0&&u<=1)return 0;}return Mathf.Min(PointSegment(a,c,d),PointSegment(b,c,d),PointSegment(c,a,b),PointSegment(d,a,b));}
 static bool NearPolygon(Vector3 a,Vector3 b,float radius,MapPoint[] ring){
  float minX=float.MaxValue,minZ=float.MaxValue,maxX=float.MinValue,maxZ=float.MinValue;foreach(var p in ring){minX=Mathf.Min(minX,p.x);minZ=Mathf.Min(minZ,p.z);maxX=Mathf.Max(maxX,p.x);maxZ=Mathf.Max(maxZ,p.z);}
  if(maxX<Mathf.Min(a.x,b.x)-radius||minX>Mathf.Max(a.x,b.x)+radius||maxZ<Mathf.Min(a.z,b.z)-radius||minZ>Mathf.Max(a.z,b.z)+radius)return false;
  if(Inside(a,ring)||Inside(b,ring))return true;for(int i=0;i<ring.Length;i++)if(SegmentDistance(a,b,ring[i].V(),ring[(i+1)%ring.Length].V())<radius)return true;return false;
 }
 internal static void ValidatePlot(MapData map,MapRoad[] surfaces,string id){
  foreach(var surface in surfaces)for(int i=1;i<surface.points.Length;i++){
   var a=surface.points[i-1].V();var b=surface.points[i].V();float radius=surface.width*.5f;
   foreach(var p in new[]{a,b})if(p.x-radius<3||p.z-radius<3||p.x+radius>map.width-3||p.z+radius>map.depth-3)throw new InvalidOperationException("Fuel plot outside map "+id);
   foreach(var building in map.buildings)if(NearPolygon(a,b,radius+1,building.points))throw new InvalidOperationException("Fuel plot building overlap "+id+" / "+building.id);
   if(map.areas!=null)foreach(var area in map.areas)if(area.kind=="water"&&NearPolygon(a,b,radius+1,area.points))throw new InvalidOperationException("Fuel plot water overlap "+id);
   // The forecourt is wholly on an empty side plot. Only its two driveways touch the road.
   if(surface==surfaces[0])foreach(var road in map.roads)for(int j=1;j<road.points.Length;j++)if(SegmentDistance(a,b,road.points[j-1].V(),road.points[j].V())<radius+road.width*.5f+1)throw new InvalidOperationException("Fuel forecourt road overlap "+id+" / "+road.id);
  }
 }
 public static void Draw(MapData map,Transform parent){
  if(parent.Find("주유소 게임시설"))return;var root=new GameObject("주유소 게임시설");root.transform.SetParent(parent,false);
  var red=WorldBuilder.Mat("Fuel red",new Color(.76f,.12f,.11f));var white=WorldBuilder.Mat("Fuel white",new Color(.93f,.94f,.91f));var dark=WorldBuilder.Mat("Fuel pump screen",new Color(.025f,.10f,.13f));var yellow=WorldBuilder.Mat("Fuel arrows",new Color(1,.88f,.18f));var concrete=WorldBuilder.Mat("Fuel island",new Color(.55f,.56f,.54f));
  var rest=Rest(map);if(rest!=null){var lot=new GameObject(rest.name);lot.transform.SetParent(root.transform,false);lot.transform.position=rest.pump;lot.transform.rotation=Quaternion.LookRotation(rest.direction);var blue=WorldBuilder.Mat("Rest bay",new Color(.1f,.55f,.85f));
   foreach(float x in new[]{-1.4f,1.4f})WorldBuilder.Box("휴식 주차 경계",new Vector3(x,.065f,0),new Vector3(.12f,.02f,6),blue,lot.transform,false);
   foreach(float z in new[]{-3f,3f})WorldBuilder.Box("휴식 주차선",new Vector3(0,.065f,z),new Vector3(2.8f,.02f,.12f),blue,lot.transform,false);
   Label(lot.transform,"휴식 안내","P · 휴식 주차 (게임시설)\n빈차 정차 · F7 · 무료 수면 100초",new Vector3(0,2.7f,0),.065f);
   for(int i=5;i+2<rest.drivePath.Length;i+=9)Arrow(rest.drivePath[i],(rest.drivePath[i+2]-rest.drivePath[i]).normalized,root.transform,blue);
  }
  foreach(var station in For(map)){
   var obj=new GameObject(station.name);obj.transform.SetParent(root.transform,false);obj.transform.position=station.pump;obj.transform.rotation=Quaternion.LookRotation(station.direction);
   Transform t=obj.transform;
   WorldBuilder.Box("캐노피",new Vector3(0,4.8f,0),new Vector3(12,.45f,20),white,t);
   WorldBuilder.Box("캐노피 빨간 띠",new Vector3(0,4.98f,0),new Vector3(12.2f,.28f,20.2f),red,t);
   foreach(float x in new[]{-5f,5f})foreach(float z in new[]{-7f,7f})WorldBuilder.Box("캐노피 기둥",new Vector3(x,2.25f,z),new Vector3(.35f,4.5f,.35f),white,t);
   WorldBuilder.Box("주유기 섬",new Vector3(3,.11f,0),new Vector3(1.15f,.18f,7),concrete,t);
   foreach(float z in new[]{-2f,2f}){
    WorldBuilder.Box("주유기",new Vector3(3,.88f,z),new Vector3(.85f,1.4f,.72f),red,t);
    WorldBuilder.Box("주유기 표시창",new Vector3(2.55f,1.15f,z),new Vector3(.06f,.4f,.48f),dark,t);
    WorldBuilder.Box("주유 호스",new Vector3(2.42f,.7f,z+.4f),new Vector3(.09f,.9f,.09f),dark,t);
   }
   WorldBuilder.Box("관리실",new Vector3(7,1.45f,0),new Vector3(4,2.9f,8),white,t);
   WorldBuilder.Box("관리실 창",new Vector3(4.96f,1.7f,0),new Vector3(.05f,1.2f,5),dark,t,false);
   // Local x is road-right and local z follows traffic, keeping all signs out of the lane.
   WorldBuilder.Box("안내판 기둥",new Vector3(4,2.5f,-13),new Vector3(.22f,5,.22f),white,t);
   WorldBuilder.Box("안내판",new Vector3(4,5.5f,-13),new Vector3(3,1.4f,.25f),red,t);
   Label(t,"시설 이름",station.name+"\n입구 → 주유 구역 → 별도 출구",new Vector3(0,6.5f,0),.065f);
   Label(t,"주유 정차 안내","주유 정차 · F7\n빈차 · 화살표 방향",new Vector3(-1,1.9f,0),.055f);
   Label(t,"입구 안내","입구 · 여기부터 수동 운전",t.InverseTransformPoint(station.entrance)+new Vector3(0,2.7f,0),.05f);
   Label(t,"출구 안내","출구 · 도로 차량에 양보",t.InverseTransformPoint(station.exit)+new Vector3(0,2.7f,0),.05f);
   foreach(float z in new[]{-4f,4f})WorldBuilder.Box("주유 정차선",new Vector3(0,.063f,z),new Vector3(2.4f,.018f,.12f),yellow,t,false);
   foreach(float x in new[]{-1.2f,1.2f})WorldBuilder.Box("주유 구역 경계",new Vector3(x,.063f,0),new Vector3(.12f,.018f,8),yellow,t,false);
   for(int i=5;i+2<station.drivePath.Length;i+=9)Arrow(station.drivePath[i],(station.drivePath[i+2]-station.drivePath[i]).normalized,root.transform,yellow);
   Arrow(station.pump-station.direction*2,station.direction,root.transform,yellow);
  }
 }
 static void Label(Transform parent,string name,string words,Vector3 position,float size){var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;var text=obj.AddComponent<TextMesh>();text.text=words;text.fontSize=42;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.white;KoreanText.Apply(text);obj.AddComponent<FaceCamera>();}
 static void Arrow(Vector3 point,Vector3 direction,Transform parent,Material paint){
  point.y=.068f;var obj=new GameObject("주유소 진행 화살표");obj.transform.SetParent(parent,false);obj.transform.position=point;obj.transform.rotation=Quaternion.LookRotation(direction);
  WorldBuilder.Box("화살표 몸통",Vector3.zero,new Vector3(.18f,.016f,1.5f),paint,obj.transform,false);
  foreach(float side in new[]{-1f,1f}){var wing=WorldBuilder.Box("화살표 머리",new Vector3(side*.29f,0,.53f),new Vector3(.16f,.016f,.9f),paint,obj.transform,false);wing.transform.localRotation=Quaternion.Euler(0,-side*42,0);}
 }
}
