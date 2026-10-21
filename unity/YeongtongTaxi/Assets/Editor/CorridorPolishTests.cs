using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class CorridorPolishTests {
 static void Check(bool yes,string message){if(!yes)throw new Exception("CORRIDOR_POLISH: "+message);}
 public static void Run(){
  var asset=Resources.Load<TextAsset>("eungye-hybrid-map");var map=JsonUtility.FromJson<MapData>(asset.text);MapSupplements.Apply(map,asset.bytes);
  var edges=JsonUtility.ToJson(new EdgeSnapshot{edges=map.hdEdges});
  int openings=RoadJunctionGeometry.Build(map).Count;
  CorridorPolish.Apply(map);var result=CorridorPolish.For(map);Check(result!=null&&result.widths.Count>0&&result.ribbons>20,"target corridor receives pavement corrections");
  Check(RoadJunctionGeometry.Build(map).Count==openings,"derived lane surfaces do not create fake junctions or gaps in paint");
  Check(edges==JsonUtility.ToJson(new EdgeSnapshot{edges=map.hdEdges}),"navigation/NPC lane identities, connections and HD geometry remain identical");
  int count=map.roads.Length;CorridorPolish.Apply(map);Check(count==map.roads.Length,"idempotent loading");
  var go=new GameObject("Corridor pavement checks");int samples=0,pairs=0;
  try{
   var env=go.AddComponent<RoadEnvironment>();env.Build(map);var graph=new HdRoadGraph(map);
   foreach(var stop in map.stops){Check(stop.hasPassengerAnchor&&!env.OnRoad(stop.Passenger),"passenger outside roadway "+stop.name);Check(stop.CanStop(stop.Kerb,stop.Direction,0),"curb interaction remains reachable "+stop.name);}
   foreach(int i in Enumerable.Range(0,map.hdEdges.Length).Where(i=>result.edges.Contains(map.hdEdges[i].id))){var p=graph.paths[i];for(float at=0;at<=p.length;at+=2){var point=p.Position(at);var right=TrafficSystem.Right(p.Tangent(at));foreach(float side in new[]{-1.5f,0,1.5f}){Check(env.OnRoad(point+right*side),"lane pavement clearance "+map.hdEdges[i].id);samples++;}}}
   foreach(var a in map.stops.Take(5))foreach(var b in map.stops.Take(5)){if(a==b)continue;Check(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)!=null,"service route retained");pairs++;}
   FuelStationLayout.Apply(map);Check(FuelStationLayout.For(map).Count==3,"fuel entries/exits still have clear plots");var fuel=FuelStationLayout.For(map)[0].approach;
   foreach(var stop in map.stops.Take(5)){Check(graph.Route(stop.laneEdge,stop.laneAt,fuel.laneEdge,fuel.laneAt)!=null&&graph.Route(fuel.laneEdge,fuel.laneAt,stop.laneEdge,stop.laneAt)!=null,"fuel bidirectional service reachability");pairs+=2;}
  }finally{UnityEngine.Object.DestroyImmediate(go);}
  var report=new StringBuilder("Shared lane envelopes; all widths provisional, not street-view measurements.\n");foreach(var c in result.widths)report.AppendLine(c.roadId+"\t"+c.before.ToString("F2")+" -> "+c.after.ToString("F2"));
  report.AppendLine("ribbons="+result.ribbons+" pavementSamples="+samples+" servicePairs="+pairs);
  foreach(var c in SceneBuildingVisibility.Conflicts(map))if(result.edges.Contains(c.edgeId))report.AppendLine("withheld building="+c.buildingId+" edge="+c.edgeId);
  Directory.CreateDirectory("../../artifacts");File.WriteAllText("../../artifacts/corridor-polish-test.txt",report.ToString());
  Debug.Log("CORRIDOR_POLISH_TEST_OK widths="+result.widths.Count+" samples="+samples+" pairs="+pairs);
 }
 [Serializable] sealed class EdgeSnapshot {public HdEdge[] edges;}
}
