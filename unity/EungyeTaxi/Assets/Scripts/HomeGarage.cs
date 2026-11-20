using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

// The ground floor of the mapped Deokseong Plaza. Local +z goes into its south facade.
public static class HomeGarage {
 public const string BuildingId="37612022:B0010000:7014";
 public const float FloorTop=.06f;
 const string RoadSource="A2218G611368";
 const float Ceiling=4.8f,Track=7;
 sealed class Layout {
  public MapBuilding building;
  public HdEdge[] edges;
  public Vector3 origin,right,inward;
  public MapStop approach,exit,bay,stored;
  public Vector3[] entryPath,exitPath;
  public Vector3 P(float x,float z){return origin+right*x+inward*z;}
  public Vector3 Local(Vector3 p){return new Vector3(Vector3.Dot(p-origin,right),0,Vector3.Dot(p-origin,inward));}
 }
 static readonly ConditionalWeakTable<MapData,Layout> layouts=new ConditionalWeakTable<MapData,Layout>();
 public static void Reset(MapData map){layouts.Remove(map);}
 static Layout Get(MapData map){
  if(map==null||map.mapId!="eungye")return null;
  var layout=layouts.GetValue(map,Create);
  if(layout.edges==map.hdEdges)return layout;
  layouts.Remove(map);return layouts.GetValue(map,Create);
 }
 public static bool Available(MapData map){var g=Get(map);return g!=null&&g.building!=null;}
 static MapStop Stop(Vector3 p,Vector3 facing){
  return new MapStop{name=HomeBasePresentation.ParkingName,x=p.x,z=p.z,dx=facing.x,dz=facing.z,width=3};
 }
 static MapStop RoadStop(HdEdge[] edges,Vector3 near,Vector3 facing){
  MapStop best=null;float nearest=3;
  for(int i=0;i<edges.Length;i++){
   var edge=edges[i];
   if(edge.TraversalDisabled||edge.sourceId!=RoadSource||edge.turn!=0||edge.change!=0)continue;
   var path=new AssistedRoadNetwork.Path(edge.points.Select(p=>p.V()).ToArray());
   float error,at=path.Project(near,out error);var direction=path.Tangent(at);
   if(error>=nearest||Vector3.Dot(facing,direction)<.95f)continue;
   best=Stop(path.Position(at),direction);best.graphStop=true;best.laneEdge=i;best.laneAt=at;nearest=error;
  }
  return best;
 }
 static bool Connected(HdEdge[] edges,MapStop approach,MapStop exit){
  if(approach==null||exit==null)return false;
  if(!edges[exit.laneEdge].next.Any(i=>!edges[i].TraversalDisabled))return false;
  if(!edges.Any(e=>!e.TraversalDisabled&&Array.IndexOf(e.next,approach.laneEdge)>=0))return false;
  if(approach.laneEdge==exit.laneEdge)return exit.laneAt>approach.laneAt;
  var seen=new HashSet<int>{approach.laneEdge};var queue=new Queue<int>();queue.Enqueue(approach.laneEdge);
  while(queue.Count>0)foreach(int next in edges[queue.Dequeue()].next){
   var edge=edges[next];
   if(edge.TraversalDisabled||edge.sourceId!=RoadSource||edge.change!=0||edge.turn!=0||!seen.Add(next))continue;
   if(next==exit.laneEdge)return true;
   queue.Enqueue(next);
  }
  return false;
 }
 static Layout Create(MapData map){
  var g=new Layout{edges=map.hdEdges};
  var b=Array.Find(map.buildings??new MapBuilding[0],item=>item.id==BuildingId);
  if(b==null||b.points.Length!=8||g.edges==null)return g;
  var a=b.points[5].V();var c=b.points[4].V();
  if((c-a).magnitude<27||b.height<Ceiling+1)return g;
  g.right=(c-a).normalized;g.inward=new Vector3(-g.right.z,0,g.right.x);g.origin=(a+c)*.5f;
  g.bay=Stop(g.P(-Track,8),-g.inward);g.stored=Stop(g.P(0,8),-g.inward);
  g.approach=RoadStop(g.edges,g.P(26,-14),-g.right);g.exit=RoadStop(g.edges,g.P(-26,-14),-g.right);
  if(!Connected(g.edges,g.approach,g.exit))return g;
  g.building=b;
  Debug.Log("HOME_GARAGE_ROAD entry="+g.edges[g.approach.laneEdge].id+" "+g.approach.Kerb+" exit="+g.edges[g.exit.laneEdge].id+" "+g.exit.Kerb);
  var inbound=new List<Vector3>();
  Curve(inbound,g.approach.Kerb,g.approach.Kerb+g.approach.Direction*13,g.P(Track,-9),g.P(Track,0));
  inbound.Add(g.P(Track,14));
  for(int i=1;i<=18;i++){float angle=i*Mathf.PI/18;inbound.Add(g.P(Track*Mathf.Cos(angle),14+Track*Mathf.Sin(angle)));}
  inbound.Add(g.bay.Kerb);g.entryPath=inbound.ToArray();
  var outbound=new List<Vector3>{g.bay.Kerb};
  Curve(outbound,g.P(-Track,0),g.P(-Track,-9),g.exit.Kerb-g.exit.Direction*13,g.exit.Kerb);
  g.exitPath=outbound.ToArray();return g;
 }
 static void Curve(List<Vector3> path,Vector3 a,Vector3 b,Vector3 c,Vector3 d){
  for(int i=0;i<=14;i++){float t=i/14f,s=1-t;path.Add(s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d);}
 }
 public static MapStop Bay(MapData map){return Get(map).bay;}
 public static MapStop StoredBay(MapData map){return Get(map).stored;}
 public static MapStop Approach(MapData map){return Get(map).approach;}
 public static MapStop ExitRoad(MapData map){return Get(map).exit;}
 public static Vector3[] EntryPath(MapData map){return Get(map).entryPath;}
 public static Vector3[] ExitPath(MapData map){return Get(map).exitPath;}
 public static Vector3 Door(MapData map){return Get(map).origin;}
 public static bool Contains(MapData map,Vector3 position){
  if(!Available(map))return false;
  var ring=Get(map).building.points;bool inside=false;
  for(int i=0,j=ring.Length-1;i<ring.Length;j=i++){
   var a=ring[i];var b=ring[j];
   if((a.z>position.z)!=(b.z>position.z)&&position.x<(b.x-a.x)*(position.z-a.z)/(b.z-a.z)+a.x)inside=!inside;
  }
  return inside;
 }
 public static bool Paved(MapData map,Vector3 position){
  if(!Available(map))return false;var g=Get(map);var p=g.Local(position);
  if(p.z>=0)return Mathf.Abs(p.x)<14.6f&&p.z<32&&Contains(map,position);
  if(p.z < -19||Mathf.Abs(p.x)>30)return false;
  return InDriveway(g.entryPath,position)||InDriveway(g.exitPath,position);
 }
 static bool InDriveway(Vector3[] path,Vector3 position){
  position.y=0;
  for(int i=1;i<path.Length;i++){var a=path[i-1];var d=path[i]-a;float t=Mathf.Clamp01(Vector3.Dot(position-a,d)/d.sqrMagnitude);if((a+d*t-position).sqrMagnitude<12.25f)return true;}
  return false;
 }
 public static void Draw(MapData map,Transform parent,bool collisions){
  var g=Get(map);var root=new GameObject("기사 숙소 · 덕성프라자 1층 차고");root.transform.SetParent(parent,false);
  root.transform.position=g.origin;root.transform.rotation=Quaternion.LookRotation(g.inward);
  var wall=WorldBuilder.Mat("차고 콘크리트",new Color(.76f,.77f,.72f));
  var floor=WorldBuilder.Mat("차고 바닥",new Color(.31f,.37f,.38f));
  var line=WorldBuilder.Mat("차고 주차선",new Color(.88f,.87f,.69f));
  var teal=WorldBuilder.Mat("숙소 청록 안내",new Color(.05f,.27f,.29f));
  var light=WorldBuilder.Mat("차고 천장등",new Color(.96f,.94f,.79f));light.EnableKeyword("_EMISSION");light.SetColor("_EmissionColor",new Color(.85f,.82f,.65f));
  for(int i=0;i<g.building.points.Length;i++){
   var a=g.Local(g.building.points[i].V());var b=g.Local(g.building.points[(i+1)%g.building.points.Length].V());
   if(i==4){Wall(a,new Vector3(11,0,0),wall,root.transform,collisions);Wall(new Vector3(-11,0,0),b,wall,root.transform,collisions);}
   else Wall(a,b,wall,root.transform,collisions);
  }
  Box("1층 천장",0,Ceiling,14,28.2f,.26f,28.5f,wall,root.transform,collisions);
  Box("실내 주차장 바닥",0,FloorTop*.5f,14,28.2f,FloorTop,28.4f,floor,root.transform,false);
  foreach(float x in new[]{-Track,0f}){
   foreach(float side in new[]{-1.55f,1.55f})Box("주차 칸",x+side,.077f,8,.1f,.016f,6.4f,line,root.transform,false);
   Box("주차 칸 끝선",x,.077f,11.15f,3.2f,.016f,.1f,line,root.transform,false);
  }
  foreach(float x in new[]{-7f,0f,7f})foreach(float z in new[]{4f,14f,24f})Box("천장 조명",x,Ceiling-.18f,z,.45f,.08f,3,light,root.transform,false);
  foreach(float x in new[]{-11.4f,11.4f})Box("차고 입구 기둥",x,2.4f,.02f,.75f,4.8f,.55f,teal,root.transform,collisions);
  Box("차고 입구 간판",0,5.42f,-.22f,22.8f,1.1f,.25f,teal,root.transform,false);
  Label("덕성프라자  ·  기사 숙소 주차장",new Vector3(0,5.44f,-.39f),root.transform,false,21,.82f);
  Box("차고 안쪽 표지",0,2.5f,27.5f,15,1.15f,.18f,teal,root.transform,false);
  Label("기사 숙소     1F     영업차 주차",new Vector3(0,2.5f,27.36f),root.transform,false,14,.85f);
  Box("입차 표지",Track,3.8f,.5f,6.6f,.95f,.14f,teal,root.transform,false);
  Label("입구   ↑",new Vector3(Track,3.8f,.38f),root.transform,false,6,.75f);
  Box("출차 표지",-Track,3.8f,.5f,7.6f,.95f,.14f,teal,root.transform,false);
  Label("출구   ↑   수인로",new Vector3(-Track,3.8f,.62f),root.transform,true,7,.75f);
  foreach(int direction in new[]{-1,1}){
   var arrow=new GameObject(direction<0?"출차 노면 화살표":"입차 노면 화살표");
   arrow.transform.SetParent(root.transform,false);arrow.transform.localPosition=new Vector3(direction*Track,.093f,1);
   if(direction<0)arrow.transform.localRotation=Quaternion.Euler(0,180,0);
   Box("방향 표시",0,0,0,.3f,.016f,2.6f,line,arrow.transform,false);
   foreach(int side in new[]{-1,1}){
    var wing=Box("화살촉",side*.34f,0,.81f,.26f,.016f,1.23f,line,arrow.transform,false);
    wing.transform.localRotation=Quaternion.Euler(0,-side*36,0);
   }
  }
  // These sidewalk tiles bracket both doors. Their street-facing vertices
  // define the curb across the two lowered driveway openings.
  var west=Array.Find(map.areas??new MapArea[0],area=>area.evidence=="sidewalk:generated:3073");
  var east=Array.Find(map.areas??new MapArea[0],area=>area.evidence=="sidewalk:generated:3085");
  if(west?.points?.Length==4&&east?.points?.Length==4){
   var curbStart=g.Local(west.points[1].V());var curbEnd=g.Local(east.points[2].V());
   DrawApron(g,g.entryPath,Track,curbStart,curbEnd,floor,root.transform);
   DrawApron(g,g.exitPath,-Track,curbStart,curbEnd,floor,root.transform);
  }
 }
 static void DrawApron(Layout g,Vector3[] path,float doorX,Vector3 curbStart,Vector3 curbEnd,Material floor,Transform root){
  var curb=curbEnd-curbStart;
  float CurbZ(float x)=>curbStart.z+(x-curbStart.x)*curb.z/curb.x;
  for(int i=1;i<path.Length;i++){
   var a=g.Local(path[i-1]);var b=g.Local(path[i]);
   float da=a.z-CurbZ(a.x),db=b.z-CurbZ(b.x);
   if(da*db>0||Mathf.Approximately(da,db))continue;
   float crossingX=Mathf.Lerp(a.x,b.x,da/(da-db));
   float left=crossingX-3.2f,right=crossingX+3.2f;
   var mesh=new Mesh{name="차고 진입 포장"};
   mesh.vertices=new[]{new Vector3(left,.07f,CurbZ(left)),new Vector3(doorX-3.2f,.07f,0),new Vector3(doorX+3.2f,.07f,0),new Vector3(right,.07f,CurbZ(right))};
   mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
   var apron=new GameObject(mesh.name);apron.transform.SetParent(root,false);
   apron.AddComponent<MeshFilter>().sharedMesh=mesh;apron.AddComponent<MeshRenderer>().sharedMaterial=floor;
   return;
  }
 }
 static GameObject Box(string name,float x,float y,float z,float w,float h,float d,Material mat,Transform root,bool solid){
  return WorldBuilder.Box(name,new Vector3(x,y,z),new Vector3(w,h,d),mat,root,solid);
 }
 static void Wall(Vector3 a,Vector3 b,Material wall,Transform root,bool solid){
  var mid=(a+b)*.5f;
  var box=Box("숙소 차고 벽",mid.x,Ceiling*.5f,mid.z,.26f,Ceiling,(a-b).magnitude,wall,root,solid);
  box.transform.localRotation=Quaternion.LookRotation(b-a);
 }
 static void Label(string value,Vector3 p,Transform parent,bool reverse,float width,float height){
  var obj=new GameObject(value);obj.transform.SetParent(parent,false);obj.transform.localPosition=p;
  if(reverse)obj.transform.localRotation=Quaternion.Euler(0,180,0);
  var text=obj.AddComponent<TextMesh>();text.text=value;text.color=Color.white;KoreanText.Fit(text,width,height);
 }
}
