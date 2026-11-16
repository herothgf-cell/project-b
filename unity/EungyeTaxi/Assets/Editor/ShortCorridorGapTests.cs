using System;
using System.Linq;
using UnityEngine;

// Verify is intentionally RED on the current map. PreviewChecks does not save.
public static class ShortCorridorGapTests {
 static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException("SHORT_CORRIDOR_GAP: "+reason);}
 public static void Verify(){var p=ProjectMapStore.Load(ProjectMapBuild.Root);Validate(p,p.document);}
 public static void PreviewChecks(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);string before=JsonUtility.ToJson(p);
  var d=ShortCorridorGapEdit.Preview(p);
  Check(before==JsonUtility.ToJson(p),"preview mutated package input");
  Check(JsonUtility.ToJson(d)==JsonUtility.ToJson(ShortCorridorGapEdit.Preview(p)),"nondeterministic preview");
  Check(d.lanes.Length==p.document.lanes.Length+12,"expected four interior lanes and eight per-junction straight joins");
  var entries=ShortCorridorGapEdit.NorthEastIncoming.Concat(ShortCorridorGapEdit.SouthWestIncoming).ToArray();
  for(int i=0;i<p.document.lanes.Length;i++){
   var old=p.document.lanes[i];var actual=d.lanes[i];Check(old.id==actual.id,"reordered existing ID");
   var expected=JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(old));
   if(entries.Contains(old.id)){
    Check(actual.next.Length==old.next.Length+1&&actual.next.Take(old.next.Length).SequenceEqual(old.next),"existing movements removed/reordered "+old.id);
    expected.next=actual.next;
   }
   Check(JsonUtility.ToJson(expected)==JsonUtility.ToJson(actual),"unrelated existing lane mutation "+old.id);
  }
  var withoutLanes=d.Copy();withoutLanes.lanes=p.document.lanes;
  Check(JsonUtility.ToJson(withoutLanes)==JsonUtility.ToJson(p.document),"facilities/signals/scene/revision changed in preview");
  Validate(p,d);Debug.Log("SHORT_CORRIDOR_PREVIEW_OK deterministic input-immutable existingIDs-preserved no-save");
 }
 public static void Validate(ProjectMapPackage p,RoadMapDocument d){
  var errors=RoadMapValidator.Validate(p.baseline,d).Where(e=>e.error).ToArray();
  Check(errors.Length==0,string.Join(" / ",errors.Take(8).Select(e=>e.ToString())));
  var map=RoadMapComposer.Compose(p.baseline,d);EungyeConnectivityRepair.Apply(map,p.baseline);var graph=new HdRoadGraph(map);
  int direct=0;
  for(int direction=0;direction<2;direction++)for(int lane=0;lane<2;lane++){
   string input=direction==0?ShortCorridorGapEdit.NorthEastIncoming[lane]:ShortCorridorGapEdit.SouthWestIncoming[lane];
   string output=direction==0?ShortCorridorGapEdit.NorthEastOutgoing[lane]:ShortCorridorGapEdit.SouthWestOutgoing[lane];
   string midId=ShortCorridorGapEdit.LaneId(direction,lane);
   var mid=map.hdEdges.SingleOrDefault(e=>e.id==midId);
   Check(mid!=null&&!mid.disabled,"missing direct short mainline "+input+" -> "+output);
   int from=Array.FindIndex(graph.edges,e=>e.id==input),to=Array.FindIndex(graph.edges,e=>e.id==output);
   var entry=graph.edges[from].next.Select(i=>graph.edges[i]).SingleOrDefault(e=>e.id==ShortCorridorGapEdit.JoinId(direction,lane,true));
   Check(entry!=null&&entry.turn==0&&entry.change==0,"missing upstream straight join");
   Check(entry.ruleId==(direction==0?"J1882_11791":"J2225_12001"),"upstream junction bypassed");
   Check(entry.next.Length==1&&graph.edges[entry.next[0]].id==midId,"entry target");
   Check(mid.number==lane+1&&mid.next.Length==1,"interior order or new extra permission");
   var exit=graph.edges[mid.next[0]];
   Check(exit.id==ShortCorridorGapEdit.JoinId(direction,lane,false)&&exit.turn==0&&exit.change==0,"downstream straight join");
   Check(exit.ruleId==(direction==0?"J2225_12001":"J1882_11791")&&exit.ruleId!=entry.ruleId,"two distinct junction rules required");
   Check(exit.next.Length==1&&graph.edges[exit.next[0]].id==output,"exit target");
   var path=graph.Route(from,Mathf.Max(0,graph.paths[from].length-1),to,Mathf.Min(1,graph.paths[to].length));
   Check(path!=null&&path.edges.Contains(Array.IndexOf(graph.edges,mid)),"direct route still detours "+input);
   Check(path.edges.All(i=>graph.edges[i].id==input||graph.edges[i].id==output||graph.edges[i].id==midId||graph.edges[i].id==entry.id||graph.edges[i].id==exit.id),"route leaves five-edge mainline chain");
   foreach(var pair in new[]{Tuple.Create(graph.edges[from],entry),Tuple.Create(entry,mid),Tuple.Create(mid,exit),Tuple.Create(exit,graph.edges[to])})Check(Vector3.Distance(pair.Item1.points.Last().V(),pair.Item2.points.First().V())<.02f,"endpoint seam");
   foreach(var edge in new[]{entry,mid,exit}){
    Check(edge.points.Length>=2,"missing geometry");
    for(int k=1;k<edge.points.Length;k++){
     var delta=edge.points[k].V()-edge.points[k-1].V();Check(delta.magnitude>.001f&&delta.magnitude<3,"discontinuous geometry "+edge.id);
     if(k>1)Check(Vector3.Dot(delta.normalized,(edge.points[k-1].V()-edge.points[k-2].V()).normalized)>.9f,"reversed/tight bend "+edge.id);
    }
    string reason;Check(EungyeConnectivityRepair.FootprintClear(map,edge,out reason),"vehicle footprint "+edge.id+": "+reason);
   }
   Check(new AssistedRoadNetwork.Path(mid.points.Select(x=>x.V()).ToArray()).length>=8,"short interior length");
   direct++;
  }
  Check(d.lanes.Where(e=>e.id.StartsWith(ShortCorridorGapEdit.Prefix)||e.id.StartsWith("join:"+ShortCorridorGapEdit.Prefix)).All(e=>e.turn==0&&e.change==0),"invented turn/change permission");
  var stops=PassengerStopCatalog.For(map).Stops;int pairs=0;
  foreach(var start in stops)foreach(var end in stops)if(start!=end){Check(graph.Route(start.laneEdge,start.laneAt,end.laneEdge,end.laneAt)!=null,"passenger route "+start.name+" -> "+end.name);pairs++;}
  Check(stops.Length==39&&pairs==1482,"unexpected passenger coverage");
  Debug.Log("SHORT_CORRIDOR_CHECKS_OK direct="+direct+" stops="+stops.Length+" pairs="+pairs+" separate-junctions footprint-checked");
 }
}
