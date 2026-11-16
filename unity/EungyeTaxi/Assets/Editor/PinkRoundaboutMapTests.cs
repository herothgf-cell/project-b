using System;
using System.Linq;
using UnityEngine;

public static class PinkRoundaboutMapTests {
 public static void Run(){ControlChecks();OccupancyChecks();PreviewChecks();}
 static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("PINK_ROUNDABOUT: "+why);}
 // Intentionally fails on the pre-roundabout document: old straight connectors
 // occupy the newly observed island. Never writes the map.
 public static void ExistingGeometryRed(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);var a=new PinkRoundaboutMapEdit.Parameters();var centre=new Vector3(a.centreX,0,a.centreZ);
  foreach(var lane in p.document.lanes.Where(l=>!l.disabled&&l.ruleId==PinkRoundaboutMapEdit.Junction&&l.id.StartsWith("join:")&&l.turn==0)){
   var path=new AssistedRoadNetwork.Path(lane.points.Select(q=>q.V()).ToArray());for(float at=0;at<path.length;at+=.5f)Check(Vector3.Distance(path.Position(at),centre)>a.islandRadius+lane.width*.5f,"old straight passes through central island: "+lane.id);
  }
 }
 public static void PreviewChecks(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);string before=JsonUtility.ToJson(p);var dimensions=new PinkRoundaboutMapEdit.Parameters();
  var d=PinkRoundaboutMapEdit.Preview(p,dimensions);Check(before==JsonUtility.ToJson(p),"preview input mutation");
  Check(d.ToJson()==PinkRoundaboutMapEdit.Preview(p,dimensions).ToJson(),"preview nondeterministic");
  Validate(p,d);
  Debug.Log("PINK_ROUNDABOUT_PREVIEW_OK deterministic input-immutable no-save");
 }
 // Compare either a candidate or a reloaded applied document to its original
 // pre-edit package. Never regenerate Preview from an already applied map.
 public static void Validate(ProjectMapPackage p,RoadMapDocument d){
  var dimensions=new PinkRoundaboutMapEdit.Parameters();
  var ribbons=Enumerable.Range(0,4).Select(i=>d.scene.objects.Single(o=>o.id=="reviewed:pink-roundabout:road:"+i)).ToArray();
  for(int i=0;i<4;i++){
   Check(ribbons[i].lanes==2&&Mathf.Abs(ribbons[i].width-dimensions.laneWidth*2)<.001f,"two-lane surface width changed");
   Check(Vector3.Distance(ribbons[i].points.Last().V(),ribbons[(i+1)%4].points.First().V())<.0001f,"circular surface seam");
   Check(Vector3.Distance(ribbons[i].points.First().V(),ribbons[i].points.Last().V())>1,"closed centreline ribbon regression");
  }
  var centre=new Vector3(dimensions.centreX,0,dimensions.centreZ);
  var joins=d.lanes.Where(l=>l.ruleId==PinkRoundaboutMapEdit.Junction&&l.id.StartsWith("join:")&&!l.disabled).ToArray();
  foreach(var join in joins){var pavement=d.scene.objects.Single(o=>o.id=="reviewed:pink-roundabout:connector-road:"+join.id);Check(pavement.enabled&&pavement.kind=="road"&&pavement.suppressPaint&&Mathf.Abs(pavement.width-join.width-.20f)<.001f,"missing real connector pavement");Check(pavement.points.Length==join.points.Length&&pavement.points.Zip(join.points,(a,b)=>Vector3.Distance(a.V(),b.V())<.001f).All(equal=>equal),"pavement does not follow connector");}
  Check(joins.Length==12&&d.lanes.Length==p.document.lanes.Length+4,"expected four outgoing lineage tails, all old IDs retained");
  var oldJoins=p.document.lanes.Where(l=>joins.Any(j=>j.id==l.id)).ToArray();var inputIds=p.document.lanes.Where(l=>l.next.Any(id=>oldJoins.Any(j=>j.id==id))).Select(l=>l.id).ToArray();var outputIds=oldJoins.Select(l=>l.next.Single()).Distinct().ToArray();
  foreach(var old in p.document.lanes){var lane=d.lanes.Single(l=>l.id==old.id);if(!joins.Contains(lane)){
    if(inputIds.Contains(old.id)){var beforePath=new AssistedRoadNetwork.Path(old.points.Select(q=>q.V()).ToArray());var afterPath=new AssistedRoadNetwork.Path(lane.points.Select(q=>q.V()).ToArray());Check(Mathf.Abs(beforePath.length-afterPath.length-PinkRoundaboutMapEdit.BoundaryTrim)<.02f,"input exact trim");for(float at=0;at<afterPath.length;at+=2)Check(Vector3.Distance(beforePath.Position(at),afterPath.Position(at))<.02f,"input changed outside final8m");Check(lane.next.SequenceEqual(old.next)&&lane.sourceOffset==old.sourceOffset,"input permissions/source station changed");}
    else if(outputIds.Contains(old.id)){var tail=d.lanes.Single(l=>l.id==lane.next.Single());var oldPath=new AssistedRoadNetwork.Path(old.points.Select(q=>q.V()).ToArray());var headPath=new AssistedRoadNetwork.Path(lane.points.Select(q=>q.V()).ToArray());var tailPath=new AssistedRoadNetwork.Path(tail.points.Select(q=>q.V()).ToArray());Check(Mathf.Abs(headPath.length-PinkRoundaboutMapEdit.BoundaryTrim)<.02f&&Mathf.Abs(tail.sourceOffset-old.sourceOffset-PinkRoundaboutMapEdit.BoundaryTrim)<.02f,"output exact station");Check(RoadMapAuthoringOperations.Origin(tail)==old.id&&tail.next.SequenceEqual(old.next),"outgoing lineage/permissions lost");
     if(old.id==PinkRoundaboutMapEdit.SouthExit){
      Check(tail.width==old.width&&Vector3.Distance(tail.points.Last().V(),old.points.Last().V())<.001f,"south width/endpoint changed");
      foreach(string buildingId in new[]{"37612023:B0010000:4518","37612023:B0010000:4520"}){var building=p.baseline.buildings.Single(b=>b.id==buildingId);bool blocked=false;for(float at=0;at<oldPath.length;at+=.5f)if(RoadMapRules.Polygon(building.points,oldPath.Position(at)+TrafficSystem.Right(oldPath.Tangent(at))*old.width*.5f)){blocked=true;break;}Check(blocked,"source-clearance regression fixture changed "+buildingId);}
      var opposite=new AssistedRoadNetwork.Path(d.lanes.Single(l=>l.id=="local:5121:18:5124:746").points.Select(q=>q.V()).ToArray());
      foreach(var point in tail.points){float error;float at=oldPath.Project(point.V(),out error);var original=oldPath.Position(at);Check(error<=1.21f&&point.x>=original.x-.01f,"south correction exceeds local eastward envelope");if(point.z>=57)Check(error<.01f,"south correction outside named window");float opposingError;opposite.Project(point.V(),out opposingError);Check(opposingError>7,"south correction enters opposing traffic");}
     }else for(float at=0;at<tailPath.length;at+=2)Check(Vector3.Distance(oldPath.Position(at+PinkRoundaboutMapEdit.BoundaryTrim),tailPath.Position(at))<.02f,"output changed outside first8m");}
    else Check(JsonUtility.ToJson(lane)==JsonUtility.ToJson(old),"unrelated lane changed "+old.id);continue;
   }
   var exit=d.lanes.Single(l=>l.id==lane.next.Single());Check(RoadMapAuthoringOperations.Origin(exit)==old.next.Single()&&lane.turn==old.turn&&lane.fromNode==old.fromNode&&lane.toNode==exit.fromNode&&lane.sourceId==old.sourceId&&lane.sourceOffset==old.sourceOffset,"permission/lineage mutation");
   var path=new AssistedRoadNetwork.Path(lane.points.Select(q=>q.V()).ToArray());
   for(float at=0;at<=path.length;at+=.25f){var point=path.Position(at);Check(Vector3.Distance(point,centre)>dimensions.islandRadius+lane.width*.5f,"movement intersects central island "+lane.id);}
   for(int i=1;i<lane.points.Length;i++){
    var a=lane.points[i-1].V()-centre;var b=lane.points[i].V()-centre;
    Check(a.x*b.z-a.z*b.x>-.02f,"clockwise shortcut "+lane.id);
   }
  }
  int oldBlocked=0;foreach(var old in p.document.lanes.Where(l=>l.ruleId==PinkRoundaboutMapEdit.Junction&&l.id.StartsWith("join:")&&l.turn==0)){
   var path=new AssistedRoadNetwork.Path(old.points.Select(q=>q.V()).ToArray());for(float at=0;at<=path.length;at+=.5f)if(Vector3.Distance(path.Position(at),centre)<dimensions.islandRadius){oldBlocked++;break;}
  }Check(oldBlocked==4,"RED fixture must expose four old straight-through-island movements");
  var parsed=RoadMapDocument.Parse(d.ToJson());var issues=RoadMapValidator.Validate(p.baseline,parsed).Where(e=>e.error).ToArray();Check(issues.Length==0,string.Join(" / ",issues.Take(8).Select(e=>e.ToString())));
  var map=RoadMapComposer.Compose(p.baseline,parsed);EungyeConnectivityRepair.Apply(map,p.baseline);var graph=new HdRoadGraph(map);
  Check(map.junctionRules.Single(j=>j.id==PinkRoundaboutMapEdit.Junction).signalMode==0,"false protected signals remain");
  foreach(var lane in joins){var edge=map.hdEdges.Single(e=>e.id==lane.id);var path=new AssistedRoadNetwork.Path(edge.points.Select(q=>q.V()).ToArray());
   var input=d.lanes.Single(l=>!l.disabled&&l.next.Contains(lane.id));int from=Array.FindIndex(graph.edges,e=>e.id==input.id),to=Array.FindIndex(graph.edges,e=>e.id==lane.next.Single());
   var route=graph.Route(from,Mathf.Max(0,graph.paths[from].length-1),to,Mathf.Min(1,graph.paths[to].length));Check(route!=null&&route.edges.Any(i=>graph.edges[i].id==lane.id)&&route.edges.Count==3,"retained direct movement detoured "+lane.id);
   for(float start=0;start<path.length;start+=4){float end=Mathf.Min(path.length,start+6);var points=new System.Collections.Generic.List<MapPoint>();for(float at=start;at<end;at+=.35f){var v=path.Position(at);points.Add(new MapPoint{x=v.x,z=v.z});}var last=path.Position(end);points.Add(new MapPoint{x=last.x,z=last.z});string reason;Check(EungyeConnectivityRepair.FootprintClear(map,new HdEdge{id=edge.id,points=points.ToArray()},out reason),"footprint "+edge.id+" at="+start+": "+reason);}
  }
  var southTail=d.lanes.Single(l=>l.id!=PinkRoundaboutMapEdit.SouthExit&&RoadMapAuthoringOperations.Origin(l)==PinkRoundaboutMapEdit.SouthExit);var southPath=new AssistedRoadNetwork.Path(southTail.points.Select(q=>q.V()).ToArray());
  for(float start=0;start<southPath.length;start+=4){float end=Mathf.Min(southPath.length,start+6);var points=new System.Collections.Generic.List<MapPoint>();for(float at=start;at<end;at+=.35f){var v=southPath.Position(at);points.Add(new MapPoint{x=v.x,z=v.z});}var last=southPath.Position(end);points.Add(new MapPoint{x=last.x,z=last.z});string reason;Check(EungyeConnectivityRepair.FootprintClear(map,new HdEdge{id=southTail.id,points=points.ToArray()},out reason),"south-clearance footprint at="+start+": "+reason);}
  var oldMap=RoadMapComposer.Compose(p.baseline,p.document);EungyeConnectivityRepair.Apply(oldMap,p.baseline);var oldStops=PassengerStopCatalog.For(oldMap).Stops;
  var stops=PassengerStopCatalog.For(map).Stops;foreach(var oldStop in oldStops){var actual=stops.Single(s=>s.name==oldStop.name);Check(Vector3.Distance(oldStop.V(),actual.V())<.02f,"passenger stop moved "+oldStop.name);}
  int pairs=0;foreach(var a in stops)foreach(var b in stops)if(a!=b){Check(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)!=null,"passenger route "+a.name+" -> "+b.name);pairs++;}
  Check(stops.Length==39&&pairs==1482,"passenger coverage");Debug.Log("PINK_ROUNDABOUT_VALIDATE_OK old-straights-blocked=4 retained-turns=12 two-physical-lanes outer-provisional-routing stops=39 pairs=1482");
 }
 public static void OccupancyChecks(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);var d=p.document.Copy();var f=PinkRoundaboutMapEdit.Control();d.facilities=d.facilities.Concat(new[]{f}).ToArray();var map=RoadMapComposer.Compose(p.baseline,d);
  var occupant=new GameObject("Roundabout player-style occupancy fixture");try{
   occupant.layer=8;occupant.AddComponent<BoxCollider>().size=new Vector3(1.9f,1.4f,4.5f);occupant.transform.position=f.Position+Vector3.right*13+Vector3.up*.7f;occupant.transform.forward=Vector3.forward;Physics.SyncTransforms();
   var entry=f.Position+Vector3.forward*17;
   Check(!ReviewedRoundaboutControl.EntryClear(map,PinkRoundaboutMapEdit.Junction,entry,Vector3.back,null),"entering vehicle failed to yield to circulating/player collider");
   ReviewedRoundaboutControl.Apply(map,p.document);
   Check(ReviewedRoundaboutControl.EntryClear(map,PinkRoundaboutMapEdit.Junction,entry,Vector3.back,null),"removed control remained cached");
   ReviewedRoundaboutControl.Apply(map,d);
   Check(!ReviewedRoundaboutControl.EntryClear(map,PinkRoundaboutMapEdit.Junction,entry,Vector3.back,null),"explicit cache refresh failed");
   Check(ReviewedRoundaboutControl.EntryClear(map,PinkRoundaboutMapEdit.Junction,f.Position+Vector3.right*13,Vector3.forward,occupant),"circulating vehicle incorrectly held at entry gate");
   Check(ReviewedRoundaboutControl.EntryClear(map,"unrelated-junction",entry,Vector3.back,null),"unrelated junction gated");
   occupant.transform.position=f.Position+Vector3.right*100;Physics.SyncTransforms();Check(ReviewedRoundaboutControl.EntryClear(map,PinkRoundaboutMapEdit.Junction,entry,Vector3.back,null),"remote collider blocks entry");
  }finally{UnityEngine.Object.DestroyImmediate(occupant);Physics.SyncTransforms();}
  Debug.Log("PINK_ROUNDABOUT_OCCUPANCY_OK entry-yields circulating-continues player-style-collider scoped");
 }
 // Control-only contract test. It does not claim geometry or circulating priority.
 public static void ControlChecks(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);string before=JsonUtility.ToJson(p.baseline);
  var withoutControl=RoadMapComposer.Compose(p.baseline,p.document);
  var d=p.document.Copy();d.facilities=d.facilities.Concat(new[]{PinkRoundaboutMapEdit.Control()}).ToArray();
  var parsed=RoadMapDocument.Parse(d.ToJson());
  Check(ReviewedRoundaboutControl.Validate(p.baseline,parsed).Count==0,"valid serialized control rejected");
  var map=RoadMapComposer.Compose(p.baseline,parsed);
  Check(map.junctionRules.Single(j=>j.id==PinkRoundaboutMapEdit.Junction).signalMode==0,"roundabout still signalized");
  foreach(var rule in withoutControl.junctionRules.Where(j=>j.id!=PinkRoundaboutMapEdit.Junction))
   Check(JsonUtility.ToJson(rule)==JsonUtility.ToJson(map.junctionRules.Single(j=>j.id==rule.id)),"unrelated junction changed "+rule.id);
  Check(before==JsonUtility.ToJson(p.baseline),"baseline mutation");
  parsed.facilities.Last().evidence.ruleConfirmed=false;
  Check(ReviewedRoundaboutControl.Validate(p.baseline,parsed).Any(e=>e.error),"unconfirmed control accepted");
  parsed=RoadMapDocument.Parse(d.ToJson());parsed.facilities=parsed.facilities.Concat(new[]{PinkRoundaboutMapEdit.Control()}).ToArray();
  Check(ReviewedRoundaboutControl.Validate(p.baseline,parsed).Any(e=>e.error),"duplicate control accepted");
  parsed=RoadMapDocument.Parse(d.ToJson());parsed.signals=parsed.signals.Concat(new[]{new RoadMapSignalProgram{junctionId=PinkRoundaboutMapEdit.Junction}}).ToArray();
  Check(ReviewedRoundaboutControl.Validate(p.baseline,parsed).Any(e=>e.error),"signal/control conflict accepted");
  parsed=RoadMapDocument.Parse(d.ToJson());parsed.facilities.Last().width=float.NaN;
  Check(ReviewedRoundaboutControl.Validate(p.baseline,parsed).Any(e=>e.error),"nonfinite dimension accepted");
  parsed=RoadMapDocument.Parse(d.ToJson());parsed.facilities.Last().x+=20;
  Check(ReviewedRoundaboutControl.Validate(p.baseline,parsed).Any(e=>e.error),"camera offset accepted as junction centre");
  Debug.Log("PINK_ROUNDABOUT_CONTROL_OK serialization baseline-immutable other-junctions-unchanged; geometry/priority untested");
 }
}
