using System;
using UnityEngine;
public static class EungyeSupplementTests {
 public static void RunDaeunCrossing(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);
  var map=package.Compose();
  var crossing=Array.Find(map.osmCrossings,c=>c.source=="OSM node 12279718021");
  if(crossing==null||!crossing.render||!crossing.signalTagged||crossing.roadId!="37612022:A0020000:1094:0")throw new Exception("J2225 crossing must span the Daeun-ro main road");
  var road=Array.Find(map.roads,r=>r.id==crossing.roadId);
  var forward=new Vector3(crossing.dx,0,crossing.dz);
  if(road==null||Mathf.Abs(road.width-crossing.width)>.3f||Vector3.Dot(forward,(road.points[1].V()-road.points[0].V()).normalized)<.999f)throw new Exception("J2225 crossing alignment or width");
  var center=new Vector3(crossing.x,0,crossing.z);
  var across=TrafficSystem.Right(forward);
  foreach(int sign in new[]{-1,1}){
   var curb=center+across*(sign*crossing.width*.5f);
   var sidewalk=Array.Find(map.areas,a=>a.evidence==(sign<0?"sidewalk:generated:373":"sidewalk:generated:374"));
   if(sidewalk==null||sidewalk.height<.045f)throw new Exception("Missing raised sidewalk at J2225");
   foreach(float along in new[]{-1.2f,0f,1.2f}){
    var end=curb+along*forward;
    if(!ProjectMapScene.Contains(sidewalk.points,end+sign*.3f*across)||ProjectMapScene.Contains(sidewalk.points,end-sign*.3f*across))throw new Exception("J2225 stripe band must meet the curb");
   }
  }
  var library=Array.Find(map.osmCrossings,c=>c.source=="OSM node 12280761355");
  if(library==null||!library.render||library.signalTagged||library.roadId!="37612022:A0020000:2213:0"||Mathf.Abs(library.width-12.1f)>.1f)throw new Exception("J1566 library crossing must cover the through road");
  var root=new GameObject("Library crossing render test");
  try{
   MapSupplements.DrawCrossings(new MapData{mapId="eungye",osmCrossings=new[]{library}},root.transform);
   var meshes=root.GetComponentsInChildren<MeshFilter>();
   if(meshes.Length!=1||meshes[0].sharedMesh.subMeshCount!=2||meshes[0].sharedMesh.GetTriangles(1).Length!=12||root.GetComponentInChildren<Collider>()!=null)throw new Exception("J1566 requires paved surface and two approach bands");
   var points=meshes[0].sharedMesh.vertices;
   var northeastLeft=points[points.Length-2];var northeastRight=points[points.Length-1];
   var lateral=(northeastRight-northeastLeft).normalized;
   var northeast=(northeastLeft+northeastRight)*.5f;
   var southwest=(points[0]+points[1])*.5f;
   if(Vector3.Distance(northeast,southwest)<12||Vector3.Distance(northeast,southwest)>14)throw new Exception("J1566 pavement extent");
   foreach(int sign in new[]{-1,1}){
    var area=Array.Find(map.areas,a=>a.evidence==(sign<0?"sidewalk:generated:517":"sidewalk:generated:518"));
    var curb=sign<0?northeastLeft:northeastRight;
    if(area==null||!ProjectMapScene.Contains(area.points,curb+sign*.3f*lateral)||ProjectMapScene.Contains(area.points,curb-sign*.3f*lateral))throw new Exception("J1566 entrance must meet opposing curbs");
   }
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  foreach(var original in package.baseline.osmCrossings){
   if(original.source==crossing.source||original.source==library.source)continue;
   if(JsonUtility.ToJson(original)!=JsonUtility.ToJson(Array.Find(map.osmCrossings,c=>c.source==original.source)))throw new Exception("Unrelated OSM crossing changed: "+original.source);
  }
  Debug.Log("DAEUN_CROSSING_OK span="+crossing.width+" center="+center+" both raised sidewalks");
 }
 public static void Run(){
  var raw=Resources.Load<TextAsset>("eungye-hybrid-map");var map=JsonUtility.FromJson<MapData>(raw.text);MapSupplements.Apply(map,raw.bytes);
  var graph=new HdRoadGraph(map);int closed=0,names=0;
  for(int i=0;i<map.hdEdges.Length;i++){var e=map.hdEdges[i];if(!string.IsNullOrEmpty(e.roadName))names++;if(e.disabled){closed++;if(e.status=="ngii"||graph.Route(i,0,i,1)!=null)throw new Exception("Closed/source edge traversable");}foreach(int n in e.next)if(map.hdEdges[n].disabled)throw new Exception("Closure bypass");}
  if(closed!=7||names==0||map.osmCrossings.Length!=39)throw new Exception("Missing expected OSM supplements");
  foreach(var a in map.stops)foreach(var b in map.stops){if(a==b)continue;var route=graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);if(route==null)throw new Exception("Supplement breaks service connectivity");foreach(int edge in route.edges)if(map.hdEdges[edge].disabled)throw new Exception("Route crosses closure");}
  Debug.Log("EUNGYE_SUPPLEMENT_OK closed="+closed+" namedEdges="+names+" all 132 service pairs reachable");
 }
}
