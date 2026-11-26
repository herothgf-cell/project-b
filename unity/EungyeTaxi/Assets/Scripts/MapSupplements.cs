using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
[Serializable] public class OsmRoadName {public string roadId,name,source,status;}
[Serializable] public class OsmRule {public string source,kind,status;public string[] edgeIds;}
[Serializable] public class OsmCrossing {public float x,z,dx,dz,width;public string roadId,source,status;public bool signalTagged,render;}
[Serializable] public class OsmSupplement {public string baseSha256,sourceTimestamp,attribution;public OsmRoadName[] names;public OsmRule[] rules;public OsmCrossing[] crossings;}
public static class MapSupplements {
 public static void Apply(MapData map,byte[] source){
  if(!map.hybrid||map.supplementApplied)return;
  var asset=Resources.Load<TextAsset>("eungye-osm-supplement");if(!asset)throw new InvalidOperationException("은계 OSM 보완 설정이 없습니다. BuildHybrid로 빌드하세요.");
  var data=JsonUtility.FromJson<OsmSupplement>(asset.text);string hash;
  using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(source)).Replace("-","").ToLowerInvariant();
  if(hash!=data.baseSha256)throw new InvalidOperationException("은계 원본 지도 변경: OSM 대조 설정을 다시 생성해야 합니다.");
  var roads=new Dictionary<string,MapRoad>();foreach(var r in map.roads)roads[r.id]=r;
  var edges=new Dictionary<string,HdEdge>();foreach(var e in map.hdEdges)edges[e.id]=e;
  var namedRoads=new HashSet<string>();foreach(var n in data.names){if(!roads.ContainsKey(n.roadId))throw new InvalidOperationException("Unknown supplement road "+n.roadId);roads[n.roadId].name=n.name;namedRoads.Add(n.roadId);}
  foreach(var rule in data.rules)foreach(var id in rule.edgeIds){HdEdge e;if(!edges.TryGetValue(id,out e)||e.status=="ngii")throw new InvalidOperationException("Invalid supplemental closure "+id);e.disabled=true;e.supplement=rule.source+" · "+rule.kind+" · 현장 미확인";}
  foreach(var e in map.hdEdges){
   var next=new List<int>();foreach(int id in e.next)if(!map.hdEdges[id].disabled)next.Add(id);e.next=e.disabled?new int[0]:next.ToArray();
   var names=new HashSet<string>();foreach(var id in e.sourceId.Split(',')){MapRoad road;if(namedRoads.Contains(id)&&roads.TryGetValue(id,out road)&&!string.IsNullOrEmpty(road.name))names.Add(road.name);}
   if(names.Count==1)foreach(var name in names)e.roadName=name;
  }
  map.osmCrossings=data.crossings;map.supplementApplied=true;map.supplementSummary="OSM 보완 · 도로명 "+data.names.Length+" / 통행규제 "+data.rules.Length+" / 횡단보도 "+data.crossings.Length+" · 현장 미확인";
 }
 const string LibraryNode="OSM node 12280761355";
 // Library entrance table. The NE edge meets sidewalks 517/518; the SW edge
 // runs from sidewalk 592 to the opposite shoulder on the bend in the main road.
 static readonly Vector3 LibrarySwLeft=new Vector3(146.675f,.105f,1272.370f);
 static readonly Vector3 LibrarySwRight=new Vector3(155.773f,.105f,1264.393f);
 static readonly Vector3 LibraryNeLeft=new Vector3(155.175f,.105f,1282.515f);
 static readonly Vector3 LibraryNeRight=new Vector3(164.055f,.105f,1274.295f);
 public static void ApplyReviewedCrossing(MapData map){
  if(map.mapId!="eungye"||map.osmCrossings==null)return;
  // The surveyed crossing spans Daeun-ro. The model has open side streets at the
  // OSM node, so use the opposing curb faces just southwest of their mouths.
  SetCrossing(map,"OSM node 12279718021","37612022:A0020000:1094:0",
   new Vector3(206.611f,0,1206.235f),new Vector3(220.842f,0,1183.173f)); // curbs 373/374
  SetCrossing(map,LibraryNode,"37612022:A0020000:2213:0",
   (LibrarySwLeft+LibraryNeLeft)*.5f,(LibrarySwRight+LibraryNeRight)*.5f);
 }
 static void SetCrossing(MapData map,string source,string road,Vector3 northwest,Vector3 southeast){
  var crossing=Array.Find(map.osmCrossings,c=>c.source==source);
  if(crossing==null)return;
  var center=(northwest+southeast)*.5f;
  var span=southeast-northwest;
  crossing.x=center.x;crossing.z=center.z;crossing.width=span.magnitude;
  var across=span.normalized;crossing.dx=-across.z;crossing.dz=across.x;
  crossing.roadId=road;
  crossing.status="aerial-reviewed-game-curbs";
 }
 static void DrawLibraryCrossing(Transform parent){
  // Four cross sections keep the ochre approach bands flush with the paving.
  var rows=new[]{0f,.075f,.925f,1f};
  var vertices=new Vector3[rows.Length*2];
  for(int i=0;i<rows.Length;i++){
   vertices[2*i]=Vector3.Lerp(LibrarySwLeft,LibraryNeLeft,rows[i]);
   vertices[2*i+1]=Vector3.Lerp(LibrarySwRight,LibraryNeRight,rows[i]);
  }
  var obj=new GameObject("대야 도서관 앞 보행 포장");obj.transform.SetParent(parent,false);
  var mesh=new Mesh{name="Library crossing pavement and approach bands",vertices=vertices,subMeshCount=2};
  mesh.SetTriangles(new[]{2,4,5,2,5,3},0);
  mesh.SetTriangles(new[]{0,2,3,0,3,1,4,6,7,4,7,5},1);mesh.RecalculateNormals();
  obj.AddComponent<MeshFilter>().sharedMesh=mesh;
  obj.AddComponent<MeshRenderer>().sharedMaterials=new[]{
   WorldBuilder.Mat("Library crossing paving",new Color(.56f,.55f,.52f)),
   WorldBuilder.Mat("Library crossing approach bands",new Color(.86f,.69f,.33f))};
 }
 static bool AuthoredCrosswalkCovers(MapData map,float x,float z){
  var facilities=map.editorDocument==null?null:map.editorDocument.facilities;if(facilities==null)return false;
  foreach(var f in facilities){if(f==null||f.kind!="crosswalk"||f.width<=0)continue;
   var forward=Quaternion.Euler(0,f.yaw,0)*Vector3.forward;var delta=new Vector3(x-f.x,0,z-f.z);
   if(Mathf.Abs(Vector3.Dot(delta,forward))<=f.length*.5f+.5f&&Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(forward)))<=f.width*.5f)return true;
  }return false;
 }
 public static void DrawCrossings(MapData map,Transform parent){
  if(map.osmCrossings==null)return;var paint=WorldBuilder.Mat("OSM crossing paint",new Color(.9f,.9f,.88f));
  foreach(var c in map.osmCrossings){if(!c.render)continue;
   if(map.mapId=="eungye"&&c.source==LibraryNode&&c.roadId=="37612022:A0020000:2213:0"){DrawLibraryCrossing(parent);continue;}
   // An authored crosswalk (also a "없음" one) supersedes an OSM crossing inside its band (2026-11-26 map review, J040-1).
   if(AuthoredCrosswalkCovers(map,c.x,c.z))continue;
   var direction=new Vector3(c.dx,0,c.dz);var right=TrafficSystem.Right(direction);
   for(float x=-c.width*.5f+.65f;x<c.width*.5f-.3f;x+=.95f){var stripe=WorldBuilder.Box("OSM crossing "+c.source,new Vector3(c.x,.065f,c.z)+right*x,new Vector3(.5f,.02f,2.4f),paint,parent,false);stripe.transform.rotation=Quaternion.LookRotation(direction);}
  }
 }
}
