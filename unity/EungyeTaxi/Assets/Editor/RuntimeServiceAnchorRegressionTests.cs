using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Exercises the runtime-only connectivity supplement missing from the earlier
// demand fixtures. Changes an in-memory map only, with no resource/save IO.
public static class RuntimeServiceAnchorRegressionTests {
 sealed class Expected {public ProjectMapObject service;public Vector3 position,direction;public float authoredAt;}
 static int checks,failures;
 static void Check(bool value,string reason){checks++;if(!value)failures++;Debug.Log("RUNTIME_SERVICE_ANCHOR "+(value?"PASS ":"FAIL ")+reason);}
 static AssistedRoadNetwork.Path Path(HdEdge edge){return new AssistedRoadNetwork.Path(edge.points.Select(p=>p.V()).ToArray());}
 public static void Run(){
  checks=failures=0;var package=ProjectMapStore.Load(ProjectMapBuild.Root);string authored=JsonUtility.ToJson(package.document);var map=package.Compose();
  var expected=new List<Expected>();Action<ProjectMapObject> remember=o=>{int edge=Array.FindIndex(map.hdEdges,e=>e.id==o.laneId);if(edge<0)throw new Exception("Missing source service lane "+o.id);var path=Path(map.hdEdges[edge]);expected.Add(new Expected{service=o,position=path.Position(o.laneAt),direction=path.Tangent(o.laneAt),authoredAt=o.laneAt});};
  foreach(var service in map.projectScene.objects.Where(o=>o.enabled&&o.IsService))remember(service);
  var original=Array.Find(map.hdEdges,e=>e.id=="A2218G611368:2");var lane=Path(original);var parking=map.projectScene.objects.Single(o=>o.id=="service:parking");var objects=map.projectScene.objects.ToList();
  // Anchors before/after several repair cuts, including the exact first cut.
  // The same edge-local offsets are used by real authored facility anchors.
  float firstCut=60-original.sourceOffset;var offsets=new[]{0f,10f,firstCut-.01f,firstCut,firstCut+.01f,lane.length*.45f,parking.laneAt,lane.length*.85f,lane.length};
  for(int i=0;i<offsets.Length;i++){var clone=JsonUtility.FromJson<ProjectMapObject>(JsonUtility.ToJson(parking));clone.id="fixture:runtime-service-anchor:"+i;clone.laneAt=offsets[i];objects.Add(clone);remember(clone);}map.projectScene.objects=objects.ToArray();
  EungyeConnectivityRepair.Apply(map,package.baseline);var graph=new HdRoadGraph(map);Check(map.hdEdges.Any(e=>e.id.StartsWith(EungyeConnectivityRepair.Prefix)),"runtime supplement actually applied");
  foreach(var item in expected){var stop=ProjectMapScene.ServiceStop(map,item.service);bool valid=stop.graphStop&&stop.laneEdge>=0&&stop.laneAt>=0&&stop.laneAt<=graph.paths[stop.laneEdge].length+.0001f;float error=Vector3.Distance(item.position,stop.V());
   Debug.Log("RUNTIME_SERVICE_ANCHOR_DETAIL id="+item.service.id+" lane="+item.service.laneId+" authoredAt="+item.authoredAt+" runtimeAt="+stop.laneAt+" runtimeLength="+(stop.laneEdge<0?-1:graph.paths[stop.laneEdge].length)+" positionError="+error);
   Check(valid,"service goal offset is inside repaired lane "+item.service.id);Check(error<.02f,"service approach preserves authored world point "+item.service.id);Check(Vector3.Dot(stop.Direction,item.direction)>.999f,"service approach preserves direction "+item.service.id);
  }
  Check(expected.Where(e=>e.service.id.StartsWith("fixture:")).Select(e=>e.service.laneId).Distinct().Count()>2,"anchors exercise multiple repaired parts");
  string repaired=JsonUtility.ToJson(map);EungyeConnectivityRepair.Apply(map,package.baseline);Check(repaired==JsonUtility.ToJson(map),"second repair is idempotent for graph and service anchors");Check(authored==JsonUtility.ToJson(package.document),"authored document remains byte-equivalent");
  Debug.Log("RUNTIME_SERVICE_ANCHOR_RESULT checks="+checks+" failures="+failures);if(failures>0)throw new Exception("RUNTIME_SERVICE_ANCHOR: "+failures+" runtime service goal regressions");
 }
}
