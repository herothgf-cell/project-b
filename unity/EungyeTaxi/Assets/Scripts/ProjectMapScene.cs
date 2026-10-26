using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable] public sealed class ProjectMapObject {
 public string id,kind,name,laneId="";
 public MapPoint[] points=new MapPoint[0];
 public float width=4,height,laneAt,serviceX,serviceZ,yaw;
 public int lanes=1;
 public bool enabled=true,oneWay,suppressPaint;
 public Vector3 Position {get{return new Vector3(serviceX,0,serviceZ);}}
 public Vector3 Direction {get{return Quaternion.Euler(0,yaw,0)*Vector3.forward;}}
 public bool IsService {get{return kind=="restaurant"||kind=="rest"||kind=="parking";}}
}

// The scene owns visible geometry. Lanes remain an independent directed graph.
[Serializable] public sealed class ProjectMapScene {
 public ProjectMapObject[] objects=new ProjectMapObject[0];
 sealed class Captured {public string digest;public ProjectMapScene scene;public Dictionary<string,ProjectMapObject> byId;}
 static readonly ConditionalWeakTable<MapData,Captured> captures=new ConditionalWeakTable<MapData,Captured>();
 static readonly List<Captured> recentCaptures=new List<Captured>();
 sealed class SceneIndex {
  public readonly Dictionary<string,ProjectMapObject[]> services=new Dictionary<string,ProjectMapObject[]>();
  public readonly Dictionary<Vector2Int,List<MapArea>> areas=new Dictionary<Vector2Int,List<MapArea>>();
  public SceneIndex(MapData map){foreach(string kind in new[]{"restaurant","rest","parking"})services[kind]=map.projectScene.objects.Where(o=>o.enabled&&o.kind==kind).ToArray();foreach(var a in map.areas){if(a.kind!="sidewalk"&&a.kind!="parking"&&a.kind!="restaurant"&&a.kind!="rest")continue;float minX=a.points.Min(p=>p.x),maxX=a.points.Max(p=>p.x),minZ=a.points.Min(p=>p.z),maxZ=a.points.Max(p=>p.z);for(int x=Mathf.FloorToInt(minX/32);x<=Mathf.FloorToInt(maxX/32);x++)for(int z=Mathf.FloorToInt(minZ/32);z<=Mathf.FloorToInt(maxZ/32);z++){var key=new Vector2Int(x,z);List<MapArea> list;if(!areas.TryGetValue(key,out list)){list=new List<MapArea>();areas.Add(key,list);}list.Add(a);}}}
 }
 static readonly ConditionalWeakTable<MapData,SceneIndex> indexes=new ConditionalWeakTable<MapData,SceneIndex>();
 static SceneIndex Index(MapData map){SceneIndex value;if(!indexes.TryGetValue(map,out value)){value=new SceneIndex(map);indexes.Add(map,value);}return value;}
 public static void InvalidateAreaIndex(MapData map){indexes.Remove(map);}
 public static bool OnArea(MapData map,Vector3 point,bool sidewalk){if(!Active(map))return false;List<MapArea> list;if(!Index(map).areas.TryGetValue(new Vector2Int(Mathf.FloorToInt(point.x/32),Mathf.FloorToInt(point.z/32)),out list))return false;return list.Any(a=>(a.kind=="sidewalk")==sidewalk&&Contains(a.points,point));}
 static readonly string[] kinds={"road","building","sidewalk","parking","restaurant","rest","water","land"};
 public static bool Active(MapData map){return map!=null&&map.projectScene!=null;}
 static MapPoint P(Vector3 p){return new MapPoint{x=p.x,z=p.z,elevation=p.y};}
 static MapPoint[] Copy(MapPoint[] points){return points.Select(p=>new MapPoint{x=p.x,z=p.z,elevation=p.elevation}).ToArray();}
 public static MapPoint[] Rectangle(Vector3 center,Vector3 direction,float width,float length){var side=TrafficSystem.Right(direction)*width*.5f;var along=direction*length*.5f;return new[]{P(center-side-along),P(center+side-along),P(center+side+along),P(center-side+along)};}
 static ProjectMapObject Service(string id,string kind,string name,MapPoint[] points,MapData map,MapStop stop,Vector3 bay,Vector3 dir){return new ProjectMapObject{id=id,kind=kind,name=name,points=points,serviceX=bay.x,serviceZ=bay.z,yaw=Quaternion.LookRotation(dir).eulerAngles.y,laneId=stop.graphStop?map.hdEdges[stop.laneEdge].id:"",laneAt=stop.laneAt};}
 // Cancel shared triangle edges, then stitch each simple outer loop. Source
 // meshes with holes or non-manifold seams stay explicit labelled segments.
 static List<MapPoint[]> PavementRings(ReviewedPavement.Surface surface,out bool segmented){
  var vertices=new List<MapPoint>();var keys=new Dictionary<Vector2,int>();var edges=new HashSet<long>();var triangles=new List<MapPoint[]>();double sourceArea=0;
  for(int t=0;t<surface.triangles.Length;t+=3){var p=Copy(new[]{surface.points[surface.triangles[t]],surface.points[surface.triangles[t+1]],surface.points[surface.triangles[t+2]]});double cross=Cross(p[0],p[1],p[2]);if(Math.Abs(cross)<.00000001)continue;if(cross<0)Array.Reverse(p);triangles.Add(p);sourceArea+=Math.Abs(cross)*.5;var indexes=new int[3];for(int i=0;i<3;i++){var key=new Vector2(p[i].x,p[i].z);int index;if(!keys.TryGetValue(key,out index)){index=vertices.Count;keys.Add(key,index);vertices.Add(p[i]);}indexes[i]=index;}for(int i=0;i<3;i++){int a=indexes[i],b=indexes[(i+1)%3];long edge=((long)a<<32)|(uint)b,reverse=((long)b<<32)|(uint)a;if(!edges.Remove(reverse))edges.Add(edge);}}
  var outgoing=new Dictionary<int,int>();bool valid=true;foreach(long edge in edges){int a=(int)(edge>>32),b=(int)edge;if(outgoing.ContainsKey(a)){valid=false;break;}outgoing.Add(a,b);}
  var rings=new List<MapPoint[]>();double resultArea=0;while(valid&&outgoing.Count>0){int start=outgoing.First().Key,current=start;var ring=new List<MapPoint>();int guard=edges.Count+1;do{int next;if(!outgoing.TryGetValue(current,out next)||guard--<=0){valid=false;break;}ring.Add(vertices[current]);outgoing.Remove(current);current=next;}while(current!=start);if(!valid)break;double area=0;for(int i=0;i<ring.Count;i++)area+=(double)ring[i].x*ring[(i+1)%ring.Count].z-(double)ring[(i+1)%ring.Count].x*ring[i].z;if(area<=0){valid=false;break;}try{Triangulate(ring.ToArray());}catch(ArgumentException){valid=false;break;}resultArea+=area*.5;rings.Add(ring.ToArray());}
  segmented=!valid||rings.Count==0||Math.Abs(resultArea-sourceArea)>.02;if(segmented)return triangles;return rings;
 }
 static Captured Baseline(MapData map){Captured cached;
  if(Active(map)){if(captures.TryGetValue(map,out cached))return cached;var own=JsonUtility.FromJson<ProjectMapScene>(JsonUtility.ToJson(map.projectScene));cached=new Captured{scene=own,byId=own.objects.ToDictionary(o=>o.id)};captures.Add(map,cached);return cached;}
  string serialized=JsonUtility.ToJson(map),digest;using(var hash=SHA256.Create())digest=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(serialized)));
  if(captures.TryGetValue(map,out cached)){if(cached.digest==digest)return cached;captures.Remove(map);}
  cached=recentCaptures.FirstOrDefault(c=>c.digest==digest);if(cached!=null){recentCaptures.Remove(cached);recentCaptures.Insert(0,cached);captures.Add(map,cached);return cached;}
  var source=JsonUtility.FromJson<MapData>(serialized);ReviewedLaneLayout.CopyState(map,source);
  if(source.roads==null)source.roads=new MapRoad[0];if(source.buildings==null)source.buildings=new MapBuilding[0];if(source.areas==null)source.areas=new MapArea[0];if(source.stops==null)source.stops=new MapStop[0];
  // Derived caches are not serialized. Recreate facilities on a clean copy once.
  source.roads=source.roads.Where(r=>!r.id.StartsWith("fuel:")&&!r.id.StartsWith("restaurant:")&&!r.id.StartsWith("polish:")).ToArray();
  if(!source.roads.Any(r=>r.id.StartsWith("polish:")))CorridorPolish.Apply(source);
  FuelStationLayout.Apply(source);RestaurantSiteLayout.Apply(source);
  var list=new List<ProjectMapObject>();
  foreach(var road in source.roads)if(road.id!="restaurant:court"&&road.id!="fuel:rest-east:court")list.Add(new ProjectMapObject{id="road:"+road.id,kind="road",name=road.name,points=Copy(road.points),width=road.width,lanes=road.DisplayLanes,oneWay=road.oneWay!=0,suppressPaint=road.suppressPaint});
  foreach(var b in source.buildings)if(!SceneBuildingVisibility.Hidden(source,b.id))list.Add(new ProjectMapObject{id="building:"+b.id,kind="building",name=b.name,height=b.height,points=Copy(b.points)});
  for(int i=0;i<source.areas.Length;i++){var a=source.areas[i];list.Add(new ProjectMapObject{id="area:"+i,kind=a.kind=="water"?"water":"land",name=a.name,points=Copy(a.points)});}
  if(source.mapId=="eungye"&&source.hybrid)foreach(var s in ReviewedPavement.Load().surfaces){bool segmented;var rings=PavementRings(s,out segmented);for(int t=0;t<rings.Count;t++)list.Add(new ProjectMapObject{id="sidewalk:reviewed:"+s.id+":"+t,kind="sidewalk",name=s.kind+(segmented?" · 분할 "+(t+1):rings.Count>1?" · 구역 "+(t+1):""),height=.14f,points=rings[t]});}
  list.AddRange(RoadSurfaceIndex.Capture(source));
  var restaurant=RestaurantSiteLayout.For(source);if(restaurant!=null){list.Add(Service("service:restaurant","restaurant",restaurant.approach.name,Rectangle(restaurant.parking,restaurant.direction,10,14),source,restaurant.approach,restaurant.parking,restaurant.direction));list.Add(new ProjectMapObject{id="building:restaurant",kind="building",name="은계 밥집",height=4,points=Rectangle(restaurant.building,restaurant.direction,5,10)});}
  var rest=FuelStationLayout.Rest(source);if(rest!=null)list.Add(Service("service:rest-east","rest",rest.name,Rectangle(rest.pump,rest.direction,20,24),source,rest.approach,rest.pump,rest.direction));
  if(source.stops.Length>0){var stop=source.stops[source.mapId=="eungye"?0:Math.Min(9,source.stops.Length-1)];list.Add(Service("service:parking","parking","영업 시작·마감 주차장",Rectangle(stop.Kerb,stop.Direction,4,10),source,stop,stop.Kerb,stop.Direction));}
  var scene=new ProjectMapScene{objects=list.ToArray()};cached=new Captured{digest=digest,scene=scene,byId=list.ToDictionary(o=>o.id)};captures.Add(map,cached);recentCaptures.Insert(0,cached);if(recentCaptures.Count>2)recentCaptures.RemoveAt(2);return cached;
 }
 public static ProjectMapScene Capture(MapData map){if(map==null)throw new ArgumentNullException("map");return JsonUtility.FromJson<ProjectMapScene>(JsonUtility.ToJson(Baseline(map).scene));}
 public static void Apply(MapData map,ProjectMapScene scene){if(scene==null)return;if(map==null||scene.objects==null)throw new ArgumentException("장면 또는 객체 배열 누락");
  var roads=new List<MapRoad>();var buildings=new List<MapBuilding>();var areas=new List<MapArea>();
  foreach(var o in scene.objects){if(o==null)throw new ArgumentException("빈 장면 객체");if(!o.enabled)continue;
   if(o.kind=="road"){roads.Add(new MapRoad{id=Strip(o.id,"road:"),name=o.name,width=o.width,lanes=o.lanes,sourceLanes=o.lanes,oneWay=o.oneWay?1:0,suppressPaint=o.suppressPaint,points=Copy(o.points)});continue;}
   int[] triangles;try{triangles=Triangulate(o.points);}catch(ArgumentException){
    // Coincident clipping vertices have trusted imported triangles. Transforming
    // that exact topology may retain them; arbitrary vertex edits still reject.
    triangles=SourceTriangles(map,o);if(!ValidTriangles(triangles,o.points.Length))throw new ArgumentException("다각형 삼각분할 실패: "+o.id);
   }
   if(o.kind=="building")buildings.Add(new MapBuilding{id=Strip(o.id,"building:"),name=o.name,height=o.height,points=Copy(o.points),triangles=triangles});
   else areas.Add(new MapArea{kind=o.kind,name=o.name,evidence=o.id,height=o.height,points=Copy(o.points),triangles=triangles});
  }
  map.roads=roads.ToArray();map.buildings=buildings.ToArray();map.areas=areas.ToArray();map.projectScene=JsonUtility.FromJson<ProjectMapScene>(JsonUtility.ToJson(scene));
  RestaurantSiteLayout.Reset(map);FuelStationLayout.Reset(map);SceneBuildingVisibility.Reset(map);indexes.Remove(map);captures.Remove(map);
  foreach(var stop in map.stops??new MapStop[0]){stop.hasPassengerAnchor=false;var obj=scene.objects.FirstOrDefault(o=>o.enabled&&o.id=="sidewalk:stop:"+Array.IndexOf(map.stops,stop));if(obj!=null){stop.passengerAnchor=Center(obj.points)+Vector3.up*obj.height;stop.hasPassengerAnchor=true;}}
 }
 static string Strip(string value,string prefix){return value!=null&&value.StartsWith(prefix)?value.Substring(prefix.Length):value;}
 public static Vector3 Center(MapPoint[] points){var p=Vector3.zero;foreach(var q in points)p+=q.V();return p/Mathf.Max(1,points.Length);}
 public static IEnumerable<ProjectMapObject> Services(MapData map,string kind){return Active(map)&&Index(map).services.ContainsKey(kind)?Index(map).services[kind]:Enumerable.Empty<ProjectMapObject>();}
 public static MapStop ServiceStop(MapData map,ProjectMapObject o){int edge=Array.FindIndex(map.hdEdges??new HdEdge[0],e=>e.id==o.laneId&&!e.disabled);if(edge<0)return new MapStop{name=o.name,x=o.serviceX,z=o.serviceZ,dx=o.Direction.x,dz=o.Direction.z,width=3};var path=new AssistedRoadNetwork.Path(map.hdEdges[edge].points.Select(p=>p.V()).ToArray());var pos=path.Position(o.laneAt);var dir=path.Tangent(o.laneAt);return new MapStop{name=o.name,x=pos.x,z=pos.z,elevation=pos.y,dx=dir.x,dz=dir.z,width=3,graphStop=true,laneEdge=edge,laneAt=o.laneAt};}
 public static MapStop ParkingStop(ProjectMapObject o){return new MapStop{name=o.name,x=o.serviceX,z=o.serviceZ,dx=o.Direction.x,dz=o.Direction.z,width=3};}
 public static bool CanUse(ProjectMapObject o,Vector3 position,Vector3 heading,float speed){var d=position-o.Position;return o.enabled&&o.IsService&&Contains(o.points,position)&&Mathf.Abs(d.y)<1.5f&&speed<.56f&&Vector3.Dot(heading.normalized,o.Direction)>.9f&&Mathf.Abs(Vector3.Dot(d,o.Direction))<2.7f&&Mathf.Abs(Vector3.Dot(d,TrafficSystem.Right(o.Direction)))<1.15f;}
 public static void DrawServices(MapData map,Transform parent){if(!Active(map))return;var paint=WorldBuilder.Mat("Service bay",new Color(.95f,.8f,.18f));foreach(var o in map.projectScene.objects.Where(x=>x.enabled&&x.IsService)){
  var root=new GameObject(o.name);root.transform.SetParent(parent,false);root.transform.position=o.Position;root.transform.rotation=Quaternion.LookRotation(o.Direction);
  foreach(float x in new[]{-1.4f,1.4f})WorldBuilder.Box("정차 칸",new Vector3(x,.07f,0),new Vector3(.12f,.02f,6),paint,root.transform,false);
  foreach(float z in new[]{-3f,3f})WorldBuilder.Box("정차 선",new Vector3(0,.07f,z),new Vector3(2.8f,.02f,.12f),paint,root.transform,false);
  var label=new GameObject("시설 안내");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,2.7f,0);var text=label.AddComponent<TextMesh>();text.text=o.name+"\n"+(o.kind=="restaurant"?"식사":o.kind=="rest"?"휴식":"영업 주차")+" · 정차 후 F7";text.fontSize=48;text.characterSize=.065f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;KoreanText.Apply(text);label.AddComponent<FaceCamera>();
 }}
 public static bool SamePoints(MapPoint[] a,MapPoint[] b){if(a==null||b==null||a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]==null||b[i]==null||a[i].x!=b[i].x||a[i].z!=b[i].z)return false;return true;}
 // A positive, nonsingular affine map preserves polygon topology and triangle
 // winding. Double calculations and float-ULP tolerance support large map coords.
 public static bool AffineRing(MapPoint[] before,MapPoint[] after){
  if(before==null||after==null||before.Length!=after.Length||before.Length<3||before.Any(p=>p==null||!Finite(p.x)||!Finite(p.z))||after.Any(p=>p==null||!Finite(p.x)||!Finite(p.z)))return false;
  if(SamePoints(before,after))return true;
  int second=0,third=0;double longest=0,best=0,ax=0,az=0,bx=0,bz=0;
  for(int i=1;i<before.Length;i++){double x=(double)before[i].x-before[0].x,z=(double)before[i].z-before[0].z,d=x*x+z*z;if(d>longest){longest=d;second=i;}}
  ax=(double)before[second].x-before[0].x;az=(double)before[second].z-before[0].z;
  for(int i=1;i<before.Length;i++){double x=(double)before[i].x-before[0].x,z=(double)before[i].z-before[0].z,d=ax*z-az*x;if(Math.Abs(d)>Math.Abs(best)){best=d;third=i;bx=x;bz=z;}}
  if(Math.Abs(best)<1e-10)return false;
  double ux=(double)after[second].x-after[0].x,uz=(double)after[second].z-after[0].z,vx=(double)after[third].x-after[0].x,vz=(double)after[third].z-after[0].z;
  if((ux*vz-uz*vx)/best<=1e-10)return false;
  for(int i=0;i<before.Length;i++){double x=(double)before[i].x-before[0].x,z=(double)before[i].z-before[0].z,u=(x*bz-z*bx)/best,v=(ax*z-az*x)/best;double expectedX=after[0].x+ux*u+vx*v,expectedZ=after[0].z+uz*u+vz*v;double tolerance=.0005+Math.Max(Math.Abs(after[i].x),Math.Abs(after[i].z))*2e-7;if(Math.Abs(expectedX-after[i].x)>tolerance||Math.Abs(expectedZ-after[i].z)>tolerance)return false;}
  return true;
 }
 static int[] SourceTriangles(MapData map,ProjectMapObject obj){
  if(obj.kind=="building"){var b=(map.buildings??new MapBuilding[0]).FirstOrDefault(x=>"building:"+x.id==obj.id&&AffineRing(x.points,obj.points));return b==null?null:b.triangles;}
  var area=(map.areas??new MapArea[0]).FirstOrDefault(a=>a.evidence==obj.id&&a.kind==obj.kind&&AffineRing(a.points,obj.points));
  if(area==null&&obj.id!=null&&obj.id.StartsWith("area:")){int n;if(int.TryParse(obj.id.Substring(5),out n)&&map.areas!=null&&n>=0&&n<map.areas.Length){var a=map.areas[n];if((a.kind==obj.kind||(a.kind=="water"?"water":"land")==obj.kind)&&AffineRing(a.points,obj.points))area=a;}}
  return area==null?null:area.triangles;
 }
 static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
 static bool ValidTriangles(int[] t,int count){return t!=null&&t.Length>=3&&t.Length%3==0&&t.All(i=>i>=0&&i<count);}
 static double Cross(MapPoint a,MapPoint b,MapPoint c){return ((double)b.x-a.x)*(c.z-a.z)-((double)b.z-a.z)*(c.x-a.x);}
 static bool OnSegment(MapPoint a,MapPoint b,MapPoint p){return Math.Abs(Cross(a,b,p))<.00001&&p.x>=Math.Min(a.x,b.x)-.00001&&p.x<=Math.Max(a.x,b.x)+.00001&&p.z>=Math.Min(a.z,b.z)-.00001&&p.z<=Math.Max(a.z,b.z)+.00001;}
 static bool Intersects(MapPoint a,MapPoint b,MapPoint c,MapPoint d){var abC=Cross(a,b,c);var abD=Cross(a,b,d);var cdA=Cross(c,d,a);var cdB=Cross(c,d,b);return abC*abD<0&&cdA*cdB<0||OnSegment(a,b,c)||OnSegment(a,b,d)||OnSegment(c,d,a)||OnSegment(c,d,b);}
 public static bool Contains(MapPoint[] ring,Vector3 point){if(ring==null||ring.Length<3)return false;var p=P(point);bool inside=false;for(int i=0,j=ring.Length-1;i<ring.Length;j=i++){var a=ring[i];var b=ring[j];if(OnSegment(a,b,p))return true;if((a.z>point.z)!=(b.z>point.z)&&point.x<((double)b.x-a.x)*(point.z-a.z)/(b.z-a.z)+a.x)inside=!inside;}return inside;}
 // Ear clipping handles concave simple polygons in either winding. The output
 // uses positive XZ winding; WorldBuilder reverses it for upward facing roofs.
 public static int[] Triangulate(MapPoint[] points){if(points==null||points.Length<3||points.Any(p=>p==null||!Finite(p.x)||!Finite(p.z)))throw new ArgumentException("유효한 꼭짓점 3개 이상 필요");int count=points.Length;double area=0;
  for(int i=0;i<count;i++){int n=(i+1)%count;if((points[i].V()-points[n].V()).sqrMagnitude<.000000000001f)throw new ArgumentException("중복 꼭짓점을 분리하세요");area+=(double)points[i].x*points[n].z-(double)points[n].x*points[i].z;for(int j=i+1;j<count;j++){int k=(j+1)%count;if(j==n||k==i)continue;if(Intersects(points[i],points[n],points[j],points[k]))throw new ArgumentException("자기 교차/겹친 다각형 변");}}
  if(Math.Abs(area)<.00000001)throw new ArgumentException("다각형 넓이가 너무 작습니다");var remaining=Enumerable.Range(0,count).ToList();if(area<0)remaining.Reverse();var triangles=new List<int>();int guard=count*count;
  while(remaining.Count>3&&guard-->0){bool clipped=false;for(int i=0;i<remaining.Count;i++){int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];double cross=Cross(points[a],points[b],points[c]);if(Math.Abs(cross)<.00000001){remaining.RemoveAt(i);clipped=true;break;}if(cross<=0)continue;bool occupied=false;foreach(int p in remaining){if(p==a||p==b||p==c)continue;if(Cross(points[a],points[b],points[p])>=-.000001&&Cross(points[b],points[c],points[p])>=-.000001&&Cross(points[c],points[a],points[p])>=-.000001){occupied=true;break;}}if(occupied)continue;triangles.AddRange(new[]{a,b,c});remaining.RemoveAt(i);clipped=true;break;}if(!clipped)throw new ArgumentException("다각형 삼각분할 실패");}
  if(remaining.Count!=3||Cross(points[remaining[0]],points[remaining[1]],points[remaining[2]])<=.00000001)throw new ArgumentException("다각형 삼각분할 실패");triangles.AddRange(remaining);return triangles.ToArray();
 }
 static bool GeometryChanged(ProjectMapObject o,Dictionary<string,ProjectMapObject> original){ProjectMapObject old;return !original.TryGetValue(o.id,out old)||old.kind!=o.kind||!SamePoints(old.points,o.points)||old.width!=o.width||old.serviceX!=o.serviceX||old.serviceZ!=o.serviceZ||old.yaw!=o.yaw;}
 static bool Overlap(MapPoint[] a,MapPoint[] b){if(a.Max(p=>p.x)<b.Min(p=>p.x)||b.Max(p=>p.x)<a.Min(p=>p.x)||a.Max(p=>p.z)<b.Min(p=>p.z)||b.Max(p=>p.z)<a.Min(p=>p.z))return false;if(Contains(a,b[0].V())||Contains(b,a[0].V()))return true;for(int i=0;i<a.Length;i++)for(int j=0;j<b.Length;j++)if(Intersects(a[i],a[(i+1)%a.Length],b[j],b[(j+1)%b.Length]))return true;return false;}
 sealed class ValidationShape {
  public float minX,maxX,minZ,maxZ;public RoadGeometry.Point[] ring;
  public ValidationShape(MapPoint[] points){minX=maxX=points[0].x;minZ=maxZ=points[0].z;ring=new RoadGeometry.Point[points.Length];for(int i=0;i<points.Length;i++){var p=points[i];minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);ring[i]=new RoadGeometry.Point(p.x,p.z);}}
 }
 static ValidationShape Shape(MapPoint[] points,Dictionary<MapPoint[],ValidationShape> shapes){ValidationShape shape;if(!shapes.TryGetValue(points,out shape)){shape=new ValidationShape(points);shapes.Add(points,shape);}return shape;}
 static bool Sweep(MapPoint[] path,float clearance,MapPoint[] polygon,Dictionary<MapPoint[],ValidationShape> shapes,bool broadPhase){
  if(path.Length<2)return false;var a=Shape(path,shapes);var b=Shape(polygon,shapes);float margin=Mathf.Abs(clearance)+.001f;
  // A swept segment cannot touch a polygon outside its expanded bounding box.
  // Cache only for this validation call: in-place editor changes are never stale.
  if(broadPhase&&(a.maxX+margin<b.minX||a.minX-margin>b.maxX||a.maxZ+margin<b.minZ||a.minZ-margin>b.maxZ))return false;
  for(int i=1;i<a.ring.Length;i++){
   var p=a.ring[i-1];var q=a.ring[i];
   if(broadPhase&&(Mathf.Max(p.x,q.x)+margin<b.minX||Mathf.Min(p.x,q.x)-margin>b.maxX||Mathf.Max(p.z,q.z)+margin<b.minZ||Mathf.Min(p.z,q.z)-margin>b.maxZ))continue;
   if(RoadGeometry.SweptLaneIntersects(p,q,b.ring,clearance))return true;
  }return false;
 }
 public static List<RoadMapIssue> Validate(MapData baseline,RoadMapDocument document){return ValidateCore(baseline,document,true);}
#if UNITY_EDITOR
 public static List<RoadMapIssue> ValidateWithoutBroadPhase(MapData baseline,RoadMapDocument document){return ValidateCore(baseline,document,false);}
#endif
 static List<RoadMapIssue> ValidateCore(MapData baseline,RoadMapDocument document,bool broadPhase){var issues=new List<RoadMapIssue>();if(document==null||document.scene==null)return issues;
  Action<ProjectMapObject,string> error=(o,m)=>issues.Add(new RoadMapIssue{error=true,id=o==null?"장면":o.id,message=m,position=o!=null&&o.points!=null&&o.points.Length>0&&o.points[0]!=null?o.points[0].V():Vector3.zero});
  var validationWatch=System.Diagnostics.Stopwatch.StartNew();
  if(document.scene.objects==null){error(null,"장면 객체 배열 누락");return issues;}var original=Baseline(baseline).byId;var ids=new HashSet<string>();var valid=new List<ProjectMapObject>();
  Debug.Log("STARTUP_SCENE baseline_ms="+validationWatch.ElapsedMilliseconds);validationWatch.Restart();
  var lanes=new Dictionary<string,RoadMapLane>();foreach(var l in document.lanes??new RoadMapLane[0])if(l!=null&&!string.IsNullOrEmpty(l.id)&&!lanes.ContainsKey(l.id))lanes.Add(l.id,l);
  foreach(var o in document.scene.objects){if(o==null||string.IsNullOrWhiteSpace(o.id)||!ids.Add(o.id)){error(o,"없는/중복 장면 ID");continue;}if(!kinds.Contains(o.kind)){error(o,"미지원 객체 종류");continue;}
   if(o.points==null||o.points.Length<(o.kind=="road"?2:3)||o.points.Any(p=>p==null||!Finite(p.x)||!Finite(p.z))){error(o,"유효한 형상 점이 부족합니다");continue;}
   if(new[]{o.width,o.height,o.laneAt,o.serviceX,o.serviceZ,o.yaw}.Any(n=>!Finite(n))){error(o,"유한한 숫자로 입력하세요");continue;}
   ProjectMapObject old;bool same=original.TryGetValue(o.id,out old)&&old.kind==o.kind&&SamePoints(old.points,o.points);bool unchanged=same&&old.width==o.width&&old.height==o.height&&old.serviceX==o.serviceX&&old.serviceZ==o.serviceZ&&old.yaw==o.yaw&&old.laneId==o.laneId&&old.laneAt==o.laneAt;
   if(!same&&o.points.Any(p=>p.x<0||p.z<0||p.x>baseline.width||p.z>baseline.depth))error(o,"지도 경계 밖 형상");
   if(o.kind=="road"){if(o.width<.3f||o.width>80||o.lanes<1||o.lanes>16)error(o,"도로 폭0.3~80m / 차로1~16 필요");if(!same)for(int i=1;i<o.points.Length;i++){if((o.points[i].V()-o.points[i-1].V()).sqrMagnitude<.000001f)error(o,"중복 도로 제어점");for(int j=i+2;j<o.points.Length;j++)if(Intersects(o.points[i-1],o.points[i],o.points[j-1],o.points[j]))error(o,"도로 중심선 자기 교차");}}
   else {try{Triangulate(o.points);}catch(ArgumentException ex){if(!same&&(old==null||old.kind!=o.kind||!ValidTriangles(SourceTriangles(baseline,o),o.points.Length)))error(o,ex.Message);}if(o.width<=0||o.width>1000)error(o,"객체 폭은 0보다 크고 1000m 이하여야 합니다");if(o.height<0||o.height>300||o.kind=="building"&&o.height<.2f||o.kind=="sidewalk"&&o.height>1)error(o,"건물 높이0.2~300m / 보도0~1m 범위");}
   if(!o.enabled)continue;valid.Add(o);
   if(o.IsService){RoadMapLane lane;if(string.IsNullOrEmpty(o.laneId)||!lanes.TryGetValue(o.laneId,out lane)||lane.disabled||lane.points==null||lane.points.Length<2||lane.points.Any(p=>p==null)){error(o,"시설 접근 차로를 지정하세요");continue;}var path=new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());if(o.laneAt<0||o.laneAt>path.length)error(o,"시설 접근 거리가 차로 길이를 벗어납니다");if(!Contains(o.points,o.Position))error(o,"정차 위치를 시설 부지 안에 두세요");if(!unchanged){foreach(var d in new[]{-2f,2f})foreach(var s in new[]{-.95f,.95f})if(!Contains(o.points,o.Position+o.Direction*d+TrafficSystem.Right(o.Direction)*s)){error(o,"정차 칸이 차량 폭/길이를 포함하지 못합니다");goto bayChecked;}bayChecked:;}}
  }
  if(baseline.mapId=="eungye"&&baseline.hybrid&&!valid.Any(o=>o.kind=="parking"))error(null,"영업 시작·마감을 위한 주차장 1개 이상이 필요합니다");
  if(issues.Any(i=>i.error))return issues;
  Debug.Log("STARTUP_SCENE objects_ms="+validationWatch.ElapsedMilliseconds);validationWatch.Restart();
  var obstacles=valid.Where(o=>o.kind=="building"||o.kind=="water").ToArray();
  var shapes=new Dictionary<MapPoint[],ValidationShape>();
  foreach(var service in valid.Where(o=>o.IsService)){var footprint=Rectangle(service.Position,service.Direction,1.9f,4);foreach(var obstacle in obstacles)if((GeometryChanged(service,original)||GeometryChanged(obstacle,original))&&Overlap(footprint,obstacle.points)){error(service,"차량 정차 공간이 건물/물과 겹칩니다: "+obstacle.id);break;}}
  foreach(var o in valid){if(!GeometryChanged(o,original))continue;
   if(o.kind=="building"||o.kind=="sidewalk"||o.kind=="water")foreach(var lane in lanes.Values){if(lane.disabled||lane.points==null||lane.points.Any(p=>p==null))continue;if(Sweep(lane.points,lane.width*.5f,o.points,shapes,broadPhase)){error(o,"주행 차로 침범: "+lane.id);break;}}
   if(o.kind=="building"||o.kind=="water")foreach(var road in valid.Where(x=>x.kind=="road")){if(Sweep(road.points,road.width*.5f,o.points,shapes,broadPhase)){error(o,"도로 노면 침범: "+road.id);break;}}
   if(o.kind=="road")foreach(var obstacle in obstacles){if(Sweep(o.points,o.width*.5f,obstacle.points,shapes,broadPhase)){error(o,"도로가 건물/물과 겹칩니다: "+obstacle.id);break;}}
  }
  Debug.Log("STARTUP_SCENE overlap_ms="+validationWatch.ElapsedMilliseconds);
  return issues;
 }
}
