using System;
using System.Linq;
using UnityEngine;

public static class PinkPassageMapTests {
 static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("PINK_PASSAGE_TEST: "+why);}
 public static void PreviewChecks(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);string original=JsonUtility.ToJson(p);var closed=PinkPassageMapEdit.ClosedIds(p.document);var d=PinkPassageMapEdit.Preview(p);
  Check(original==JsonUtility.ToJson(p),"input/baseline mutated");Check(d.ToJson()==PinkPassageMapEdit.Preview(p).ToJson(),"nondeterministic preview");Check(d.lanes.Length==p.document.lanes.Length,"lane IDs added/removed");
  for(int i=0;i<d.lanes.Length;i++){
   var before=p.document.lanes[i];var after=d.lanes[i];Check(before.id==after.id&&before.sourceId==after.sourceId&&before.sourceOffset==after.sourceOffset,"identity/order/source changed");
   if(closed.Contains(after.id)){Check(after.disabled&&after.next.Length==0,"passage vehicle edge still active "+after.id);continue;}
   var expected=JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(before));expected.next=expected.next.Where(id=>!closed.Contains(id)).ToArray();Check(JsonUtility.ToJson(expected)==JsonUtility.ToJson(after),"unrelated lane modified or new permission "+after.id);
  }
  Check(!d.lanes.Where(l=>!l.disabled).Any(l=>l.next.Any(closed.Contains)),"active incoming reference to pedestrian passage");
  for(int i=0;i<p.document.scene.objects.Length;i++){var a=p.document.scene.objects[i];var b=d.scene.objects[i];if(PinkPassageMapEdit.ClosedRoad(a,closed)){Check(!b.enabled,"vehicle surface still active");continue;}Check(JsonUtility.ToJson(a)==JsonUtility.ToJson(b),"unrelated scene object changed "+a.id);}
  Check(d.facilities.Length==p.document.facilities.Length&&d.signals.Length==p.document.signals.Length,"new traffic permission introduced");
  var errors=RoadMapValidator.Validate(p.baseline,d).Where(e=>e.error).ToArray();Check(errors.Length==0,string.Join(" / ",errors.Take(10).Select(e=>e.ToString())));
  var map=RoadMapComposer.Compose(p.baseline,d);EungyeConnectivityRepair.Apply(map,p.baseline);var graph=new HdRoadGraph(map);
  Check(!graph.edges.Any(e=>!e.disabled&&(closed.Contains(e.id)||PinkPassageMapEdit.Family(e.sourceId))),"runtime connectivity repair reopened passage");
  var walk=d.scene.objects.Single(o=>o.id==PinkPassageMapEdit.Prefix+"pavers");var centre=walk.points.Aggregate(Vector3.zero,(sum,point)=>sum+point.V())/walk.points.Length;
  Check(centre.x>805&&centre.x<828&&centre.z>163&&centre.z<169,"paver polygon remains in observed passage bounds");
  var rendered=map.areas.SingleOrDefault(a=>a.kind=="sidewalk"&&a.evidence==walk.id);Check(rendered!=null,"authored pavers absent from composed sidewalk areas");
  // ProjectMapObject.Position is a service anchor, not polygon geometry.
  // Sample the complete paver centreline (away from ambiguous boundary edges).
  float west=walk.points.Min(p=>p.x),east=walk.points.Max(p=>p.x);for(float x=west+.5f;x<east-.5f;x+=.5f){var point=new Vector3(x,0,centre.z);Check(ProjectMapScene.Contains(rendered.points,point)&&ProjectMapScene.OnArea(map,point,true),"continuous raised pedestrian pavement absent x="+x);}
  var baselineMap=p.Compose();EungyeConnectivityRepair.Apply(baselineMap,p.baseline);var baselineCatalog=PassengerStopCatalog.For(baselineMap);var catalog=PassengerStopCatalog.For(map);
  Check(catalog.Entries.Select(e=>e.StopId).SequenceEqual(baselineCatalog.Entries.Select(e=>e.StopId)),"ordered stop identities changed");Check(catalog.Stops.Length==39,"39-stop contract");
  int pairs=0;foreach(var a in catalog.Stops)foreach(var b in catalog.Stops)if(a!=b){var route=graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);Check(route!=null,"route lost "+a.name+" -> "+b.name);Check(!route.edges.Any(i=>closed.Contains(graph.edges[i].id)||PinkPassageMapEdit.Family(graph.edges[i].sourceId)),"route traverses pedestrian passage");pairs++;}
  Check(pairs==1482,"all directed stop pairs checked");Debug.Log("PINK_PASSAGE_CHECKS_OK pedestrian-only closed="+closed.Count+" stops39 pairs1482 unrelated-grid-preserved no-save");
 }
}
