using System;
using UnityEngine;
public static class EungyeSupplementTests {
 public static void Run(){
  var raw=Resources.Load<TextAsset>("eungye-hybrid-map");var map=JsonUtility.FromJson<MapData>(raw.text);MapSupplements.Apply(map,raw.bytes);
  var graph=new HdRoadGraph(map);int closed=0,names=0;
  for(int i=0;i<map.hdEdges.Length;i++){var e=map.hdEdges[i];if(!string.IsNullOrEmpty(e.roadName))names++;if(e.disabled){closed++;if(e.status=="ngii"||graph.Route(i,0,i,1)!=null)throw new Exception("Closed/source edge traversable");}foreach(int n in e.next)if(map.hdEdges[n].disabled)throw new Exception("Closure bypass");}
  if(closed!=7||names==0||map.osmCrossings.Length!=39)throw new Exception("Missing expected OSM supplements");
  foreach(var a in map.stops)foreach(var b in map.stops){if(a==b)continue;var route=graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);if(route==null)throw new Exception("Supplement breaks service connectivity");foreach(int edge in route.edges)if(map.hdEdges[edge].disabled)throw new Exception("Route crosses closure");}
  Debug.Log("EUNGYE_SUPPLEMENT_OK closed="+closed+" namedEdges="+names+" all 132 service pairs reachable");
 }
}
