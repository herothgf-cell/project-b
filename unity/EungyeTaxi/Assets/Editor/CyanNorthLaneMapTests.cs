using System;
using System.Linq;
using UnityEngine;

// Run Verify before authoring for the red result; PreviewChecks checks an
// in-memory candidate, and Verify checks the saved map after explicit Apply.
public static class CyanNorthLaneMapTests {
 static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException("CYAN_NORTH: "+reason);}
 public static void Verify(){var p=ProjectMapStore.Load(ProjectMapBuild.Root);Validate(p,p.document);}
 public static void PreviewChecks(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);string before=JsonUtility.ToJson(p.document);
  var d=CyanNorthLaneMapEdit.Preview(p);
  Check(before==JsonUtility.ToJson(p.document),"Preview mutated its input");
  var again=CyanNorthLaneMapEdit.Preview(p);
  Check(JsonUtility.ToJson(d)==JsonUtility.ToJson(again),"Preview must be deterministic");
  for(int i=0;i<p.document.lanes.Length;i++){
   var old=p.document.lanes[i];var now=d.lanes[i];
   Check(old.id==now.id&&old.sourceId==now.sourceId&&old.sourceOffset==now.sourceOffset,"existing index/lineage changed "+old.id);
  }
  Check(d.facilities.Length==p.document.facilities.Length+2,"exactly two no-arrow metadata assignments appended");
  Check(JsonUtility.ToJson(new RoadMapDocument{id="",facilities=p.document.facilities})==JsonUtility.ToJson(new RoadMapDocument{id="",facilities=d.facilities.Take(p.document.facilities.Length).ToArray()}),"existing facilities changed");
  Check(JsonUtility.ToJson(new RoadMapDocument{id="",signals=p.document.signals})==JsonUtility.ToJson(new RoadMapDocument{id="",signals=d.signals}),"signal programs changed");
  Check(p.document.scene.objects.Length==d.scene.objects.Length,"scene object count changed");
  for(int i=0;i<p.document.scene.objects.Length;i++){
   var old=p.document.scene.objects[i];var now=d.scene.objects[i];
   Check(old.id==now.id,"scene order changed");
   bool duplicate=CyanNorthLaneMapEdit.North.Concat(CyanNorthLaneMapEdit.South).Any(id=>old.id=="road:polish:"+id);
   if(!CyanNorthLaneMapEdit.SelectedRoad(old)&&!duplicate)Check(JsonUtility.ToJson(old)==JsonUtility.ToJson(now),"unrelated scene/service binding changed "+old.id);
  }
  Validate(p,d);Debug.Log("CYAN_NORTH_PREVIEW_OK input-immutable deterministic identity-preserved");
 }
 public static void Validate(ProjectMapPackage p,RoadMapDocument d){
  var errors=RoadMapValidator.Validate(p.baseline,d).Where(e=>e.error).ToArray();
  Check(errors.Length==0,string.Join(" / ",errors.Take(8).Select(e=>e.ToString())));
  var map=RoadMapComposer.Compose(p.baseline,d);EungyeConnectivityRepair.Apply(map,p.baseline);
  var graph=new HdRoadGraph(map);
  for(int group=0;group<3;group++){
   string outerId=CyanNorthLaneMapEdit.North[group],innerId=CyanNorthLaneMapEdit.Inner(outerId),southId=CyanNorthLaneMapEdit.South[group];
   var outer=map.hdEdges.Single(e=>e.id==outerId);var inner=map.hdEdges.SingleOrDefault(e=>e.id==innerId);var south=map.hdEdges.Single(e=>e.id==southId);
   Check(inner!=null&&!inner.disabled,"missing second north lane "+outerId);
   Check(inner.number==1&&outer.number==2&&south.number==1,"median-to-curb lane numbers "+outerId);
   var a=new AssistedRoadNetwork.Path(outer.points.Select(v=>v.V()).ToArray());
   var b=new AssistedRoadNetwork.Path(inner.points.Select(v=>v.V()).ToArray());
   var c=new AssistedRoadNetwork.Path(south.points.Select(v=>v.V()).ToArray());
   foreach(float t in new[]{.2f,.5f,.8f}){
    var centre=b.Position(b.length*t);var right=TrafficSystem.Right(b.Tangent(b.length*t));
    float error;float outerAt=a.Project(centre,out error),southAt=c.Project(centre,out error);
    float lateralOuter=Vector3.Dot(a.Position(outerAt)-centre,right),lateralSouth=Vector3.Dot(c.Position(southAt)-centre,right);
    Check(Mathf.Abs(lateralOuter-3.5f)<.2f&&Mathf.Abs(lateralSouth+3.5f)<.2f,"N2/S1 3.5m cross section "+outerId+" t="+t);
    var crossing=map.hdEdges.Where(e=>!e.disabled&&e.turn==0&&e.change==0&&!e.id.StartsWith("join:")&&e.sourceId==outer.sourceId).Count(e=>Crosses(e.points,centre,b.Tangent(b.length*t)));
    Check(crossing==3,"world cross section expected 3, actual "+crossing+" "+outerId);
   }
   int[] innerTurns=CyanNorthLaneMapEdit.InnerTurns(group),outerTurns=CyanNorthLaneMapEdit.OuterTurns(group);
   if(group<2){
    foreach(var approach in new[]{inner,outer}){
     string target=approach==inner?CyanNorthLaneMapEdit.Inner(CyanNorthLaneMapEdit.North[group+1]):CyanNorthLaneMapEdit.North[group+1];
     Check(approach.next.Select(n=>graph.edges[n]).Any(j=>!j.disabled&&j.turn==0&&j.next.Any(n=>graph.edges[n].id==target)),"continuous same-order north lane "+approach.id);
    }
   }
   foreach(var pair in new[]{Tuple.Create(inner,innerTurns),Tuple.Create(outer,outerTurns)}){
    int index=Array.IndexOf(graph.edges,pair.Item1);
    Check(graph.edges.Any(e=>!e.disabled&&e.next.Contains(index)),"incoming "+pair.Item1.id);
    var actual=pair.Item1.next.Select(i=>graph.edges[i]).Where(e=>!e.disabled&&e.change==0).Select(e=>e.turn).Distinct().OrderBy(t=>t).ToArray();
    Check(actual.SequenceEqual(pair.Item2.OrderBy(t=>t)),"directional movement wiring "+pair.Item1.id+": "+string.Join(",",actual));
    CheckCorridorFootprint(map,pair.Item1);
   }
   CheckCorridorFootprint(map,south);
  }
  var changedJoins=d.lanes.Where(e=>!e.disabled&&e.id.StartsWith("join:")&&e.evidence!=null&&e.evidence.note.StartsWith("Cyan north")).Select(e=>e.id).ToArray();
  foreach(var edge in graph.edges.Where(e=>changedJoins.Contains(e.id))){
   foreach(var previous in graph.edges.Where(e=>!e.disabled&&e.next.Contains(Array.IndexOf(graph.edges,edge))))Check(Vector3.Distance(previous.points.Last().V(),edge.points[0].V())<.03f,"join entry seam "+edge.id);
   foreach(int n in edge.next)Check(Vector3.Distance(edge.points.Last().V(),graph.edges[n].points[0].V())<.03f,"join exit seam "+edge.id);
  }
  foreach(var road in d.scene.objects.Where(CyanNorthLaneMapEdit.SelectedRoad))Check(road.width==11&&road.lanes==3&&!road.oneWay,"3-lane road surface "+road.id);
  foreach(var road in map.roads.Where(r=>RoadviewCorridorPresentation.CyanNorthSource(r.id)))Check(road.width==11&&road.DisplayLanes==3,"composed road override "+road.id);
  var arrows=LaneDirectionPaint.Layout(map);
  foreach(string id in CyanNorthLaneMapEdit.North){
   int group=Array.IndexOf(CyanNorthLaneMapEdit.North,id);
   foreach(string laneId in new[]{id,CyanNorthLaneMapEdit.Inner(id)}){
    int index=Array.FindIndex(map.hdEdges,e=>e.id==laneId);
    var turns=laneId==id?CyanNorthLaneMapEdit.OuterTurns(group):CyanNorthLaneMapEdit.InnerTurns(group);
    int mask=turns.Aggregate(0,(value,turn)=>value|UturnMovement.PaintMask(turn));
    if(group==1){
     Check(d.facilities.Count(f=>LaneDirectionPaint.IsArrowSuppression(f)&&f.laneId==laneId)==1,"missing no-arrow evidence "+laneId);
     Check(!arrows.Any(a=>a.edgeIndex==index),"invented J7292 painted arrow "+laneId);
    }else Check(arrows.Any(a=>a.edgeIndex==index&&a.turns==mask),"missing/incorrect movement paint "+laneId);
   }
  }
  // Exercise the actual world paint sink, not just the legal movement mask.
  var painted=new System.Collections.Generic.List<string>();
  ReviewedLaneLayout.Paint(map,(name,point,direction,width,length,yellow)=>painted.Add(name));
  foreach(string laneId in new[]{CyanNorthLaneMapEdit.North[1],CyanNorthLaneMapEdit.Inner(CyanNorthLaneMapEdit.North[1])})Check(!painted.Contains("Lane arrow "+laneId),"world painted unmarked approach "+laneId);
  var unsuppressed=d.Copy();unsuppressed.facilities=unsuppressed.facilities.Where(f=>!LaneDirectionPaint.IsArrowSuppression(f)).ToArray();
  var unsuppressedMap=RoadMapComposer.Compose(p.baseline,unsuppressed);var controlArrows=LaneDirectionPaint.Layout(unsuppressedMap);
  foreach(string laneId in new[]{CyanNorthLaneMapEdit.North[1],CyanNorthLaneMapEdit.Inner(CyanNorthLaneMapEdit.North[1])}){
   Check(controlArrows.Any(a=>unsuppressedMap.hdEdges[a.edgeIndex].id==laneId),"suppression control should otherwise paint "+laneId);
   var current=map.hdEdges.Single(e=>e.id==laneId);var control=unsuppressedMap.hdEdges.Single(e=>e.id==laneId);
   Check(current.next.Select(i=>map.hdEdges[i].id).SequenceEqual(control.next.Select(i=>unsuppressedMap.hdEdges[i].id)),"paint metadata changed navigation "+laneId);
  }
  var stops=PassengerStopCatalog.For(map).Stops;int pairs=0;
  foreach(var start in stops)foreach(var end in stops)if(start!=end){Check(graph.Route(start.laneEdge,start.laneAt,end.laneEdge,end.laneAt)!=null,"stop route "+start.name+" -> "+end.name);pairs++;}
  Debug.Log("CYAN_NORTH_CHECKS_OK stops="+stops.Length+" pairs="+pairs);
 }
 static bool Crosses(MapPoint[] points,Vector3 centre,Vector3 forward){for(int i=1;i<points.Length;i++){float a=Vector3.Dot(points[i-1].V()-centre,forward),b=Vector3.Dot(points[i].V()-centre,forward);if(a<=0&&b>0||b<=0&&a>0)return true;}return false;}
 static void CheckCorridorFootprint(MapData map,HdEdge lane){
  var path=new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());string reason;
  for(int i=1;i<path.points.Length;i++){
   var delta=path.points[i]-path.points[i-1];
   Check(delta.magnitude>.001f&&delta.magnitude<5,"discontinuous/doubled corridor points "+lane.id+" index="+i+" distance="+delta.magnitude);
   if(i>1)Check(Vector3.Dot(delta.normalized,(path.points[i-1]-path.points[i-2]).normalized)>.8f,"abrupt local corridor bend "+lane.id+" index="+i);
  }
  // The production checker constrains heading against its FIRST tangent,
  // because it checks short merge connectors. A valid 48-degree road bend is
  // not a reversing connector. Walk overlapping local windows so its physical
  // vehicle/road/sidewalk/building tests still cover the ENTIRE curved lane.
  if(!EungyeConnectivityRepair.FootprintClear(map,lane,out reason))Debug.Log("CYAN_NORTH_WHOLE_EDGE_DIAGNOSTIC id="+lane.id+" reason="+reason);
  for(float start=0;start<path.length;start+=8){
   float end=Mathf.Min(path.length,start+10);var points=new System.Collections.Generic.List<MapPoint>();
   for(float at=start;at<end;at+=.5f){var p=path.Position(at);points.Add(new MapPoint{x=p.x,z=p.z});}
   var last=path.Position(end);points.Add(new MapPoint{x=last.x,z=last.z});
   var window=new HdEdge{id=lane.id,points=points.ToArray()};
   if(EungyeConnectivityRepair.FootprintClear(map,window,out reason))continue;
   var centre=path.Position(start);
   var near=map.roads.Select(road=>{
    var ribbon=new AssistedRoadNetwork.Path(road.points.Select(p=>p.V()).ToArray());float error;ribbon.Project(centre,out error);
    return new{id=road.id,distance=error,halfWidth=road.width*.5f};
   }).OrderBy(r=>r.distance-r.halfWidth).Take(3);
   Check(false,"vehicle footprint "+lane.id+" station="+start.ToString("F2")+".."+end.ToString("F2")+" centre="+centre.ToString("F2")+" reason="+reason+" nearestRoads="+string.Join(" | ",near.Select(r=>r.id+" centreDistance="+r.distance.ToString("F2")+" halfWidth="+r.halfWidth.ToString("F2"))));
  }
 }
}
