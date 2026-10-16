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
 public static void DrawCrossings(MapData map,Transform parent){
  if(map.osmCrossings==null)return;var paint=WorldBuilder.Mat("OSM crossing paint",new Color(.9f,.9f,.88f));
  foreach(var c in map.osmCrossings){if(!c.render)continue;var direction=new Vector3(c.dx,0,c.dz);var right=TrafficSystem.Right(direction);
   for(float x=-c.width*.5f+.65f;x<c.width*.5f-.3f;x+=.95f){var stripe=WorldBuilder.Box("OSM crossing "+c.source,new Vector3(c.x,.065f,c.z)+right*x,new Vector3(.5f,.02f,2.4f),paint,parent,false);stripe.transform.rotation=Quaternion.LookRotation(direction);}
  }
 }
}
