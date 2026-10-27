using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class EungyeConnectivityRepairTests {
 static int checks;
 static void Check(bool okay,string message){checks++;if(!okay)throw new Exception("CONNECTIVITY_REPAIR: "+message);}
 static MapData Clone(MapData map){var copy=JsonUtility.FromJson<MapData>(JsonUtility.ToJson(map));copy.projectScene=map.projectScene;copy.editorDocument=map.editorDocument.Copy();return copy;}
 public static void Run(){
  checks=0;var package=ProjectMapStore.Load(ProjectMapBuild.Root);var original=package.Compose();var map=Clone(original);var before=new HdRoadGraph(map);var positions=map.stops.Select(s=>s.V()).ToArray();var kerbs=map.stops.Select(s=>s.Kerb).ToArray();float oldDistance=before.Route(map.stops[0].laneEdge,map.stops[0].laneAt,map.stops[7].laneEdge,map.stops[7].laneAt).path.length;
  EungyeConnectivityRepair.Apply(map,package.baseline);var graph=new HdRoadGraph(map);var added=map.hdEdges.Where(e=>e.id.StartsWith(EungyeConnectivityRepair.Prefix)&&e.status=="provisional-connectivity-repair").ToArray();
  Check(added.Length==25&&added.Count(e=>e.change!=0)==21,"21 evidence-bounded lane changes and four forward connectors");
  Check(added.Any(e=>e.id.EndsWith("straight-third"))&&added.Any(e=>e.id.EndsWith("south-portal")),"missing straight third lane and south capture portal restored");
  Check(added.Any(e=>e.change==-1&&e.sourceId=="A2218G611368,A2218G611366"),"late outer-to-third change available");
  Check(added.All(e=>e.turn==0&&e.status=="provisional-connectivity-repair"&&!string.IsNullOrEmpty(e.evidence)),"no invented left/right turn or verification claim");
  Check(map.stops.Length==12,"legacy stop count");for(int i=0;i<12;i++){Check(Vector3.Distance(map.stops[i].V(),positions[i])<.001f&&Vector3.Distance(map.stops[i].Kerb,kerbs[i])<.001f,"stop world geometry stable "+i);Check(Vector3.Distance(graph.paths[map.stops[i].laneEdge].Position(map.stops[i].laneAt),positions[i])<.05f,"stop split distance remapped "+i);}
  foreach(var e in added){Check(EungyeConnectivityRepair.FootprintClear(map,e),"whole taxi footprint in existing road and outside buildings/sidewalk: "+e.id);foreach(int next in e.next)Check(e.toNode==map.hdEdges[next].fromNode&&Vector3.Distance(e.points.Last().V(),map.hdEdges[next].points[0].V())<.05f,"repair next has exact node/endpoint");}
  foreach(var old in original.hdEdges){var terminal=map.hdEdges.First(e=>e.id==old.id);if(terminal.toNode!=old.toNode)terminal=map.hdEdges.First(e=>e.sourceId==old.sourceId&&e.toNode==old.toNode&&e.id.StartsWith(EungyeConnectivityRepair.Prefix)&&e.change==0);foreach(int next in old.next)Check(terminal.next.Any(n=>map.hdEdges[n].id==original.hdEdges[next].id),"existing explicit outgoing turn preserved "+old.id);}
  int routes=0;foreach(var a in map.stops)foreach(var b in map.stops)if(a!=b){Check(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)!=null,"all legacy directed routes remain reachable");routes++;}
  var route=graph.Route(map.stops[0].laneEdge,map.stops[0].laneAt,map.stops[7].laneEdge,map.stops[7].laneAt);Check(route!=null,"parking to southwest remains reachable");Check(route.path.length<=oldDistance+.5f,"additional safe choices do not worsen shortest route");
  int south=Array.FindIndex(map.hdEdges,e=>e.id=="A2218G611310:0");var portal=added.FirstOrDefault(e=>e.id.EndsWith("south-portal"));if(portal!=null){Check(map.hdEdges[south].next.Length==1&&map.hdEdges[map.hdEdges[south].next[0]]==portal,"south endpoint connects only to forward merge");var dir=graph.paths[south].Tangent(graph.paths[south].length);Check(Vector3.Dot(portal.points.Last().V()-portal.points[0].V(),dir)>10,"south portal never reverses to local capture start");}
  var restricted=Clone(original);var edited=Array.Find(restricted.hdEdges,e=>e.id=="A2218G611368:2");edited.next=new int[0];EungyeConnectivityRepair.Apply(restricted,package.baseline);Check(!restricted.hdEdges.Any(e=>e.status=="provisional-connectivity-repair"&&e.sourceId.StartsWith("A2218G611368,")),"explicit editor next removal prevents adding lane changes");
  var blocked=Clone(original);var lane=Array.Find(blocked.hdEdges,e=>e.id=="A2218G611310:0");lane.disabled=true;EungyeConnectivityRepair.Apply(blocked,package.baseline);Check(!blocked.hdEdges.Any(e=>e.id==EungyeConnectivityRepair.Prefix+"south-portal"),"disabled source respects editor/closure");
  int count=map.hdEdges.Length;var stopIndices=map.stops.Select(s=>s.laneEdge).ToArray();EungyeConnectivityRepair.Apply(map,package.baseline);Check(count==map.hdEdges.Length&&stopIndices.SequenceEqual(map.stops.Select(s=>s.laneEdge)),"idempotent repair does not resplit stops");
  var catalog=PassengerStopCatalog.For(map);Check(catalog.Stops.Length==24&&catalog.Rejected.Count==0,"unchanged game stop catalog revalidates after graph-only repair: "+string.Join(" / ",catalog.Rejected));
  string report="EUNGYE_CONNECTIVITY_REPAIR_TESTS_OK checks="+checks+" added="+added.Length+" laneChanges="+added.Count(e=>e.change!=0)+" straightThird="+added.Any(e=>e.id.EndsWith("straight-third"))+" southPortal="+(portal!=null)+" legacyRoutes="+routes+" parkingToS08="+oldDistance.ToString("F2")+"->"+route.path.length.ToString("F2")+"m";
  Directory.CreateDirectory(Path.Combine(ProjectMapBuild.Root,"artifacts"));File.WriteAllText(Path.Combine(ProjectMapBuild.Root,"artifacts/eungye-connectivity-repair-summary.txt"),report+"\n"+string.Join("\n",route.edges.Select(i=>map.hdEdges[i].id)));Debug.Log(report);
 }
}
