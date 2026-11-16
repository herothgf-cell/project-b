using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class P10PedestrianPassageEdit {
 const string RequiredHash="0ac94059842e80819c2182dc84c2600937e512c535e323022ad92d9bc8d296da";
 const string WalkId="reviewed:pink-P10:pavers";
 const string MainlineJoin="join:reviewed:P10:mainline-east";
 const string MainlineExit="A2218G611419:0";
 static readonly string[] Sources={"37612023:A0020000:242:0","37612023:A0020000:696:0","37612023:A0020000:697:0"};
 static readonly string[] Roots={"local:6450:3862:6452:3446","local:6509:3446:6507:3863"};

 static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("P10_PASSAGE: "+message);}
 static bool Family(string source)=>!string.IsNullOrEmpty(source)&&source.Split(',').Any(Sources.Contains);
 static RoadMapEvidence Evidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=RHKYG5qaPX6x-WSGz07_qw,14,-13,80,Float",
  note="User confirmed map 34 pedestrian-only. May 2026 view shows paving and a continuous kerb at the shop passage, on the north side of Eungyebeonyeong-gil. Paver width 5.5m and raised kerb height 0.14m are game estimates."
 };

 static HashSet<string> ClosedLanes(RoadMapDocument document){
  var closed=new HashSet<string>(document.lanes.Where(lane=>Family(lane.sourceId)).Select(lane=>lane.id));
  bool grew;
  do{
   grew=false;
   foreach(var lane in document.lanes.Where(l=>l.id.StartsWith("join:")||l.change!=0)){
    if(lane.next.Any(closed.Contains)&&closed.Add(lane.id))grew=true;
   }
  }while(grew);
  return closed;
 }

 static bool ClosedSurface(ProjectMapObject obj,HashSet<string> closed){
  if(obj.kind!="road")return false;
  return Sources.Any(source=>obj.id=="road:"+source)||closed.Any(id=>obj.id=="road:polish:"+id);
 }

 static RoadMapDocument Preview(ProjectMapPackage package,HashSet<string> closed){
  Check(package.document.revision==29&&package.contentHash==RequiredHash,"requires exact r29 baseline");
  Check(Roots.All(id=>closed.Contains(id)&&package.document.lanes.Any(l=>l.id==id&&!l.disabled)),"expected existing two-way passage");
  Check(!package.baseline.stops.Any(s=>s.graphStop&&closed.Contains(package.baseline.hdEdges[s.laneEdge].id)),"bound stop needs explicit relocation");
  Check(!package.document.scene.objects.Any(o=>o.enabled&&closed.Contains(o.laneId)),"lane-bound scene object");
  Check(!package.document.facilities.Any(f=>closed.Contains(f.laneId)),"lane-bound traffic facility");
  var document=package.document.Copy();
  foreach(var lane in document.lanes){
   if(closed.Contains(lane.id)){
    lane.disabled=true;
    lane.next=Array.Empty<string>();
    lane.evidence=Evidence();
   }else lane.next=lane.next.Where(id=>!closed.Contains(id)).ToArray();
  }
  foreach(var obj in document.scene.objects.Where(o=>ClosedSurface(o,closed)))obj.enabled=false;
  var mainlineEntry=document.lanes.Single(l=>l.id=="local:5252:4219:6200:3990");
  var mainlineExit=document.lanes.Single(l=>l.id==MainlineExit);
  Check(mainlineEntry.next.Length==0,"expected the old synthetic turn to be its only exit");
  var join=JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(mainlineEntry));
  join.id=MainlineJoin;
  join.turn=0;
  var mainlineEvidence=new RoadMapEvidence{
   state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
   url="https://map.naver.com/p?c=19,0,0,0,adh&p=NUSFrn6V0LhsvjbaU8QyyQ,103,-20,80,Float",
   note="Suin-ro remains continuous east-west along the shop frontage. Restore the provisional east approach onto the existing outer NGII through lane after removing its sole synthetic turn into the pedestrian passage. The other provisional tip crosses an existing building footprint, so use the existing paved corridor and native lane centre."
  };
  Y06EastApproachEdit.Reconnect(join,mainlineEntry,mainlineExit,mainlineEvidence);
  mainlineEntry.next=new[]{join.id};
  document.lanes=document.lanes.Concat(new[]{join}).ToArray();
  var southbound=document.lanes.Single(l=>l.id==Roots[0]);
  var northbound=document.lanes.Single(l=>l.id==Roots[1]);
  var south=(southbound.points.Last().V()+northbound.points[0].V())*.5f;
  var north=(southbound.points[0].V()+northbound.points.Last().V())*.5f;
  Check(Mathf.Abs(south.x-648)<1&&south.z>344&&north.z<389,"pedestrian polygon must stay within the confirmed entrance and corridor");
  south.z=342.2f;
  north.z=389.1f;
  var walk=new ProjectMapObject{
   id=WalkId,kind="sidewalk",name="34번 은계번영길 보행 통로",enabled=true,height=.14f,width=5.5f,
   points=ProjectMapScene.Rectangle((south+north)*.5f,(north-south).normalized,5.5f,Vector3.Distance(south,north))
  };
  document.scene.objects=document.scene.objects.Concat(new[]{walk}).ToArray();
  return document;
 }

 static void Validate(ProjectMapPackage before,RoadMapDocument document,HashSet<string> closed){
  var errors=RoadMapValidator.Validate(before.baseline,document).Where(issue=>issue.error).ToArray();
  Check(errors.Length==0,string.Join(" / ",errors.Take(8).Select(issue=>issue.ToString())));
  var map=RoadMapComposer.Compose(before.baseline,document);
  EungyeConnectivityRepair.Apply(map,before.baseline);
  var graph=new HdRoadGraph(map);
  var mainline=graph.edges.Single(edge=>edge.id==MainlineJoin);
  Check(!mainline.disabled&&mainline.turn==0&&graph.edges[mainline.next.Single()].id==MainlineExit,"restore only the straight mainline movement");
  Y07MapTests.Footprint(map,mainline);
  Check(!graph.edges.Any(edge=>!edge.disabled&&(closed.Contains(edge.id)||Family(edge.sourceId))),"runtime reopened the pedestrian passage");
  Check(!graph.edges.Any(edge=>!edge.disabled&&edge.next.Any(i=>closed.Contains(graph.edges[i].id))),"active vehicle entrance to the passage");
  Check(document.scene.objects.Where(o=>ClosedSurface(o,closed)).All(o=>!o.enabled),"passage pavement still rendered as a vehicle road");
  var pavers=map.areas.SingleOrDefault(area=>area.kind=="sidewalk"&&area.evidence==WalkId);
  Check(pavers!=null,"pedestrian pavement missing after compose");
  Check(ProjectMapScene.OnArea(map,new Vector3(648,0,365),true),"raised sidewalk absent in passage");
  var catalog=PassengerStopCatalog.For(map);
  Check(catalog.Stops.Length==39,"preserved 39 stops: "+string.Join("; ",catalog.Rejected));
  int routes=0;
  foreach(var start in catalog.Stops)foreach(var end in catalog.Stops)if(start!=end){
   Check(graph.Route(start.laneEdge,start.laneAt,end.laneEdge,end.laneAt)!=null,"unreachable stop "+start.name+" → "+end.name);
   routes++;
  }
  Check(routes==1482,"all directed stop pairs");
  Debug.Log("P10_PASSAGE_VALIDATED car lanes excluded="+closed.Count+", 39 stops, 1482 routes");
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  var closed=ClosedLanes(before.document);
  var document=Preview(before,closed);
  document.revision=30;
  document.savedAt=DateTime.UtcNow.ToString("o");
  document.projectRevision="";
  Validate(before,document,closed);
  var candidate=ProjectMapPackage.Create(before.baseline,document);
  ProjectMapStore.Save(root,candidate,token);
  var reloaded=ProjectMapStore.Load(root);
  Check(reloaded.contentHash==candidate.contentHash&&reloaded.baselineHash==before.baselineHash,"stored map identity");
  Validate(before,reloaded.document,closed);
  ProjectMapBuild.Prepare();
  Debug.Log("P10_PASSAGE_APPLIED revision=30 contentHash="+reloaded.contentHash);
 }
}
