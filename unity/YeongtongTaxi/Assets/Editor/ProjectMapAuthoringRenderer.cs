using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Scene objects keep identity separately from the runtime spatial render batches.
// No colliders, whole-map JSON, or whole-map composition are needed while dragging.
public sealed class ProjectMapAuthoringRenderer : IDisposable {
 sealed class Record {
  public string id,kind;public ProjectMapObject source;public Bounds bounds;public int[] triangles;public MapPoint[] topology;
  public readonly Dictionary<Vector2Int,WorldBuilder.GeometryBatch> geometry=new Dictionary<Vector2Int,WorldBuilder.GeometryBatch>();
 }
 sealed class Chunk {
  public string kind;public Vector2Int cell;public GameObject root;public Mesh mesh;
  public readonly Dictionary<string,WorldBuilder.GeometryBatch> members=new Dictionary<string,WorldBuilder.GeometryBatch>();
 }
 readonly Dictionary<string,Record> records=new Dictionary<string,Record>();
 readonly Dictionary<string,Chunk> chunks=new Dictionary<string,Chunk>();
 readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
 readonly Dictionary<string,GameObject> serviceRoots=new Dictionary<string,GameObject>();
 readonly HashSet<Object> owned=new HashSet<Object>();
 MapData map;
 public Transform Root {get;private set;}
 public int FullBuildCount {get;private set;}
 public int ChunkRebuildCount {get;private set;}
 public int LastUpdatedChunkCount {get;private set;}
 public int RenderChunkCount {get{return chunks.Values.Count(c=>c.root);}}
 public int ObjectCount {get{return records.Count;}}
 static string Key(string kind,Vector2Int cell){return kind+":"+cell.x+":"+cell.y;}
 public void Initialize(MapData source,Transform parent=null){
  if(source==null||source.projectScene==null)throw new ArgumentException("Composed project map required");
  Dispose();map=source;Root=new GameObject("Project map authoring world").transform;Root.SetParent(parent,false);
  // Use precisely the runtime terrain, fuel facilities, source road paint, crossings,
  // service signs and boundaries. Geometry below uses the same runtime generators.
  var observer=WorldBuilder.MaterialCreated;WorldBuilder.MaterialCreated=m=>{owned.Add(m);if(observer!=null)observer(m);};
  try{
   var builder=Root.gameObject.AddComponent<WorldBuilder>();builder.BuildStatic(map,false,true,false);Object.DestroyImmediate(builder);
   foreach(var o in map.projectScene.objects)if(o.IsService){var t=Root.Find(o.name);if(t)serviceRoots[o.id]=t.gameObject;}
   var objects=map.projectScene.objects.ToDictionary(o=>o.id);
   foreach(var road in map.roads??new MapRoad[0]){ProjectMapObject o;objects.TryGetValue("road:"+road.id,out o);var r=new Record{id="road:"+road.id,kind="road",source=o};WorldBuilder.AppendRoad(road,r.geometry);Add(r);}
   foreach(var b in map.buildings??new MapBuilding[0]){if(SceneBuildingVisibility.Hidden(map,b.id))continue;ProjectMapObject o;objects.TryGetValue("building:"+b.id,out o);var r=new Record{id="building:"+b.id,kind="building",source=o};r.triangles=b.triangles;r.topology=b.points;var g=WorldBuilder.BuildingGeometry(b);WorldBuilder.Batch(r.geometry,WorldBuilder.GeometryBounds(g).center).Add(g.vertices,g.indices);Add(r);}
   int index=0;foreach(var a in map.areas??new MapArea[0]){if(a.points==null||a.triangles==null){index++;continue;}string id=string.IsNullOrEmpty(a.evidence)?"area:"+index:a.evidence;ProjectMapObject o;objects.TryGetValue(id,out o);var r=new Record{id=id,kind=a.kind,source=o};r.triangles=a.triangles;r.topology=a.points;WorldBuilder.AppendArea(a,r.geometry);Add(r);index++;}
   foreach(var c in chunks.Values)Rebuild(c);
   foreach(var mesh in Root.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh))Track(mesh);
   // No editor physics world is needed, including decorative primitive colliders.
   foreach(var collider in Root.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
   FullBuildCount++;LastUpdatedChunkCount=chunks.Count;
  }catch{Dispose();throw;}finally{WorldBuilder.MaterialCreated=observer;}
 }
 void Track(Object value){if(value&&!AssetDatabase.Contains(value)&&!(value is Mesh&&value.name.StartsWith("Cube")))owned.Add(value);}
 void Add(Record r){
  bool first=true;foreach(var pair in r.geometry){var b=WorldBuilder.GeometryBounds(pair.Value);if(first){r.bounds=b;first=false;}else r.bounds.Encapsulate(b);var key=Key(r.kind,pair.Key);Chunk chunk;if(!chunks.TryGetValue(key,out chunk)){chunk=new Chunk{kind=r.kind,cell=pair.Key};chunks.Add(key,chunk);}chunk.members[r.id]=pair.Value;}
  records[r.id]=r;
 }
 Material MaterialFor(string kind){Material material;if(materials.TryGetValue(kind,out material))return material;
  var color=WorldBuilder.SurfaceColor(kind);
  material=WorldBuilder.Mat(kind=="road"?"Road":kind=="building"?"Buildings":kind,color);owned.Add(material);materials.Add(kind,material);return material;
 }
 void Rebuild(Chunk c){
  ChunkRebuildCount++;
  if(c.members.Count==0){if(c.root)Object.DestroyImmediate(c.root);if(c.mesh){owned.Remove(c.mesh);Object.DestroyImmediate(c.mesh);}chunks.Remove(Key(c.kind,c.cell));return;}
  if(!c.root){c.root=new GameObject(c.kind+" "+c.cell);c.root.transform.SetParent(Root,false);c.root.layer=Root.gameObject.layer;c.mesh=new Mesh{name="Authoring chunk "+c.kind};c.mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;owned.Add(c.mesh);c.root.AddComponent<MeshFilter>().sharedMesh=c.mesh;c.root.AddComponent<MeshRenderer>().sharedMaterial=MaterialFor(c.kind);}
  var batch=new WorldBuilder.GeometryBatch();foreach(var g in c.members.Values)batch.Add(g.vertices,g.indices);
  c.mesh.Clear();c.mesh.SetVertices(batch.vertices);c.mesh.SetTriangles(batch.indices,0);c.mesh.RecalculateNormals();c.mesh.RecalculateBounds();
 }
 public void UpdateObject(ProjectMapObject obj){
  if(!Root||obj==null)throw new InvalidOperationException("Initialize renderer before updating");
  Record old;records.TryGetValue(obj.id,out old);
  // Build/validate the replacement before removing any existing valid geometry.
  var next=new Record{id=obj.id,kind=obj.kind,source=obj,triangles=old==null?null:old.triangles,topology=old==null?null:old.topology};
  if(obj.enabled){
   if(obj.kind=="road")WorldBuilder.AppendRoad(new MapRoad{id=obj.id,points=obj.points,width=obj.width},next.geometry);
   else {int[] triangles;try{triangles=ProjectMapScene.Triangulate(obj.points);}catch(ArgumentException){if(old==null||old.triangles==null||!ProjectMapScene.AffineRing(old.topology,obj.points))throw;triangles=old.triangles;}
    next.triangles=triangles;next.topology=Array.ConvertAll(obj.points,p=>new MapPoint{x=p.x,z=p.z,elevation=p.elevation});
    if(obj.kind=="building"){var g=WorldBuilder.BuildingGeometry(new MapBuilding{points=obj.points,height=obj.height,triangles=triangles});WorldBuilder.Batch(next.geometry,WorldBuilder.GeometryBounds(g).center).Add(g.vertices,g.indices);}
    else WorldBuilder.AppendArea(new MapArea{kind=obj.kind,height=obj.height,points=obj.points,triangles=triangles},next.geometry);
   }
  }
  var affected=new HashSet<string>();if(old!=null)foreach(var pair in old.geometry){string key=Key(old.kind,pair.Key);affected.Add(key);chunks[key].members.Remove(obj.id);}
  Add(next);foreach(var pair in next.geometry)affected.Add(Key(next.kind,pair.Key));
  foreach(string key in affected)Rebuild(chunks[key]);LastUpdatedChunkCount=affected.Count;
  if(obj.IsService||serviceRoots.ContainsKey(obj.id))UpdateService(obj);
 }
 void UpdateService(ProjectMapObject obj){
  GameObject old;if(serviceRoots.TryGetValue(obj.id,out old)&&old){
   if(obj.enabled&&obj.IsService){old.name=obj.name;old.transform.position=obj.Position;old.transform.rotation=Quaternion.LookRotation(obj.Direction);var text=old.GetComponentInChildren<TextMesh>();if(text)text.text=obj.name+"\n"+(obj.kind=="restaurant"?"식사":obj.kind=="rest"?"휴식":"영업 주차")+" · 정차 후 F7";return;}
   Object.DestroyImmediate(old);
  }serviceRoots.Remove(obj.id);
  if(!obj.enabled||!obj.IsService)return;
  var host=new GameObject("Service update");host.transform.SetParent(Root,false);
  var observer=WorldBuilder.MaterialCreated;WorldBuilder.MaterialCreated=m=>owned.Add(m);
  try{ProjectMapScene.DrawServices(new MapData{projectScene=new ProjectMapScene{objects=new[]{obj}}},host.transform);var child=host.transform.GetChild(0);child.SetParent(Root,true);serviceRoots[obj.id]=child.gameObject;foreach(var t in child.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Root.gameObject.layer;}
  finally{WorldBuilder.MaterialCreated=observer;Object.DestroyImmediate(host);}
 }
 public Bounds BoundsFor(string id){Record r;return records.TryGetValue(id,out r)?r.bounds:new Bounds();}
 public string Pick(Ray ray,string kindFilter=""){
  if(!Root)return null;var local=new Ray(Root.InverseTransformPoint(ray.origin),Root.InverseTransformDirection(ray.direction));float closest=float.PositiveInfinity;string result=null;
  foreach(var r in records.Values){if(!string.IsNullOrEmpty(kindFilter)&&r.kind!=kindFilter)continue;float entry;var bounds=r.bounds;bounds.Expand(.002f);if(!bounds.IntersectRay(local,out entry)||entry>closest)continue;
   foreach(var g in r.geometry.Values)for(int i=0;i<g.indices.Count;i+=3){float distance;if(Hit(local,g.vertices[g.indices[i]],g.vertices[g.indices[i+1]],g.vertices[g.indices[i+2]],out distance)&&distance<closest){closest=distance;result=r.id;}}
  }return result;
 }
 static bool Hit(Ray ray,Vector3 a,Vector3 b,Vector3 c,out float distance){distance=0;var edge1=b-a;var edge2=c-a;var p=Vector3.Cross(ray.direction,edge2);float determinant=Vector3.Dot(edge1,p);if(Mathf.Abs(determinant)<.0000001f)return false;float inverse=1/determinant;var t=ray.origin-a;float u=Vector3.Dot(t,p)*inverse;if(u<0||u>1)return false;var q=Vector3.Cross(t,edge1);float v=Vector3.Dot(ray.direction,q)*inverse;if(v<0||u+v>1)return false;distance=Vector3.Dot(edge2,q)*inverse;return distance>=0;}
 public void Dispose(){if(Root)Object.DestroyImmediate(Root.gameObject);Root=null;foreach(var value in owned)if(value&&!AssetDatabase.Contains(value))Object.DestroyImmediate(value);owned.Clear();records.Clear();chunks.Clear();materials.Clear();serviceRoots.Clear();map=null;}
}
