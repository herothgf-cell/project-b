using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EungyeCommerceLaneEdit {
 const string RequiredHash="731caa156ccba7c85d261df3ed1f44eef6315b725e0a69ce55b5f831030e3c48";
 const string P13East="local:5868:2554:7086:2547";
 const string P13WestInner="local:7086:2591:5868:2598";
 const string P13West="local:7087:2635:5869:2642";
 const string P14EastLeft="local:5194:2557:5632:2555";
 const string P14East="local:5194:2513:5632:2511";
 const string P14WestLeft="local:5632:2599:5194:2601";
 const string P14West="local:5633:2643:5195:2645";
 const string FacilityPrefix="observed:commercial-midblock:";
 const float TransitionX=541.3f;
 const float Taper=9f;

 static void Check(bool value,string message){
  if(!value)throw new InvalidOperationException("EUNGYE_COMMERCE: "+message);
 }
 static AssistedRoadNetwork.Path Path(RoadMapLane lane)=>new AssistedRoadNetwork.Path(lane.points.Select(point=>point.V()).ToArray());
 static RoadMapEvidence P13Evidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=false,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=34IejTemygTtWq63f1bfAw,92,-21,70,Float",
  note="May 2026 throughout P13: two eastbound and one westbound vehicle lanes. The latter approaches J5750 with a straight and right glyph. Geometry is fitted inside the existing curbs with a gradual offset at the existing westbound boarding point. J7204 eastbound inner permissions await confirmation."
 };
 static RoadMapEvidence P14Evidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=false,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=0ZepBNSBGqaTCGmHkm6bNA,300,-30,80,Float",
  note="P14 shifts from one eastbound and two westbound lanes to two eastbound and one westbound near the middle. Eastbound approach is L/S (also compared with Oct 2025 and Oct 2024). Westbound is L/S+R as painted; the straight arrow faces the apartment entrance, outside the public driving graph. The x541.3 switch and 9m tapers are game estimates."
 };
 static string Join(string root,int turn)=>"join:"+root+":"+turn;
 static string Terminal(string root)=>root+":part:2";
 static string ApproachId(string root)=>"editor:segment:"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(root))
  .TrimEnd('=').Replace('+','-').Replace('/','_')+":p14-approach";

 static IEnumerable<Vector3> Sample(float start,float end,Func<float,Vector3> position){
  for(float at=start;at<end-.001f;at+=1f)yield return position(at);
  yield return position(end);
 }

 static RoadMapLane SplitApproach(RoadMapLane head,RoadMapLane turnLane,RoadMapEvidence evidence){
  var outside=Path(head);
  var inside=Path(turnLane);
  float error;
  float split=outside.Project(new Vector3(TransitionX,0,outside.Position(0f).z),out error);
  Check(split>Taper&&outside.length-split>Taper,"lane taper must fit the approach");
  Vector3 Middle(float at){
   var outer=outside.Position(at);
   return (outer+inside.Position(inside.Project(outer,out _)))*.5f;
  }
  Vector3 Branch(float at,AssistedRoadNetwork.Path target){
   var middle=Middle(at);
   var dest=target==outside?outside.Position(at):inside.Position(inside.Project(outside.Position(at),out _));
   return Vector3.Lerp(middle,dest,Mathf.SmoothStep(0f,1f,Mathf.Clamp01((at-split)/Taper)));
  }

  var outerApproach=JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(head));
  outerApproach.id=ApproachId(head.id);
  outerApproach.fromNode=outerApproach.id+":start";
  turnLane.fromNode=outerApproach.fromNode;
  head.toNode=outerApproach.fromNode;
  head.number=1;
  head.next=new[]{outerApproach.id,turnLane.id};
  Y06EastApproachEdit.Assign(head,Sample(0f,split,Middle),evidence);
  outerApproach.sourceOffset=head.points.Zip(head.points.Skip(1),(a,b)=>Vector3.Distance(a.V(),b.V())).Sum();
  Y06EastApproachEdit.Assign(outerApproach,Sample(split,outside.length,at=>Branch(at,outside)),evidence);
  Y06EastApproachEdit.Assign(turnLane,Sample(split,outside.length,at=>Branch(at,inside)),evidence);
  return outerApproach;
 }

 static RoadMapDocument Preview(ProjectMapPackage package){
  Check(package.document.revision==30&&package.contentHash==RequiredHash,"requires exact r30 baseline");
  var document=package.document.Copy();
  var lanes=document.lanes.ToDictionary(lane=>lane.id);
  var p13=P13Evidence();
  var p14=P14Evidence();
  var p13Source=lanes[P13East].sourceId;
  var p14Source=lanes[P14East].sourceId;
  var oldWest=lanes.Values.Where(l=>l.id==P13WestInner||l.id.StartsWith(P13WestInner+":part:")).ToArray();
  Check(oldWest.Length==3,"P13 westbound segmentation");
  var disabled=oldWest.Select(l=>l.id).Concat(new[]{
   "change:"+P13WestInner+":"+P13West,"change:"+P13West+":"+P13WestInner,
   Join(P13WestInner,0),Join(P14EastLeft,0)
  }).ToHashSet();
  Check(!package.baseline.stops.Any(s=>s.graphStop&&disabled.Contains(package.baseline.hdEdges[s.laneEdge].id)),"disabled lane carries a stop");
  Check(!document.scene.objects.Any(o=>o.enabled&&disabled.Contains(o.laneId)),"disabled lane carries a scene binding");
  foreach(var facility in document.facilities.Where(f=>disabled.Contains(f.laneId))){
   Check(facility.id==FacilityPrefix+"west-stop"||facility.id==FacilityPrefix+"west-signal","unhandled binding "+facility.id);
   facility.laneId=Terminal(P13West);
   if(facility.kind=="stopLine")facility.width=4.4f;
  }

  var p13West=new[]{P13West,P13West+":part:1",Terminal(P13West)}.Select(id=>lanes[id]).ToArray();
  for(int i=0;i<p13West.Length;i++){
   var lane=p13West[i];
   var inner=Path(lanes[P13WestInner+lane.id.Substring(P13West.Length)]);
   var outer=Path(lane);
   lane.number=1;
   Y06EastApproachEdit.Assign(lane,Sample(0f,outer.length,at=>{
    var point=outer.Position(at);
    var middle=(point+inner.Position(inner.Project(point,out _)))*.5f;
    // Keep the existing boarding position beside the curb, including its heading.
    float blend=Mathf.SmoothStep(0f,1f,Mathf.Clamp01((Mathf.Abs(point.x-626.75f)-6f)/16f));
    return Vector3.Lerp(point,middle,blend);
   }),p13);
  }

  var eastApproach=SplitApproach(lanes[P14East],lanes[P14EastLeft],p14);
  var westApproach=SplitApproach(lanes[P14West],lanes[P14WestLeft],p14);
  // The complete westbound family is the reference for continuous centre and edge paint.
  document.lanes=document.lanes.Where(l=>l.id!=P14EastLeft&&l.id!=P14WestLeft)
   .Concat(new[]{eastApproach,westApproach,lanes[P14EastLeft],lanes[P14WestLeft]}).ToArray();
  lanes.Add(eastApproach.id,eastApproach);
  lanes.Add(westApproach.id,westApproach);

  var remap=new Dictionary<string,string>{{P13WestInner,P13West},{P14EastLeft,P14East}};
  var changed=new HashSet<string>(p13West.Select(l=>l.id)){P14East,P14West,P14EastLeft,P14WestLeft};
  foreach(var lane in document.lanes){
   if(disabled.Contains(lane.id)){
    lane.disabled=true;
    lane.next=Array.Empty<string>();
    lane.evidence=lane.id.StartsWith("join:"+P14EastLeft)?p14:p13;
   }else if(lane.id.StartsWith("join:")){
    lane.next=lane.next.Select(id=>remap.TryGetValue(id,out var target)?target:id).ToArray();
   }else lane.next=lane.next.Where(id=>!disabled.Contains(id)).ToArray();
  }
  changed.Add(eastApproach.id);
  changed.Add(westApproach.id);
  foreach(var join in document.lanes.Where(l=>!l.disabled&&l.id.StartsWith("join:")&&l.next.Any(changed.Contains)
   ||!l.disabled&&(eastApproach.next.Contains(l.id)||westApproach.next.Contains(l.id)||p13West.Last().next.Contains(l.id)
    ||l.id==Join(P14EastLeft,-1)||l.id==Join(P14WestLeft,-1)))){
   var from=document.lanes.Single(l=>!l.disabled&&l.next.Contains(join.id));
   var to=lanes[join.next.Single()];
   Y06EastApproachEdit.Reconnect(join,from,to,
    from.sourceId==p14Source||to.sourceId==p14Source?p14:p13);
   changed.Add(join.id);
  }
  foreach(var obj in document.scene.objects.Where(o=>o.kind=="road")){
   if(p13Source.Split(',').Concat(p14Source.Split(',')).Any(source=>obj.id=="road:"+source))obj.lanes=3;
   const string surface="road:polish:";
   if(!obj.id.StartsWith(surface))continue;
   string id=obj.id.Substring(surface.Length);
   if(disabled.Contains(id))obj.enabled=false;
   else if(changed.Contains(id))obj.points=id==P14East?lanes[id].points.Concat(eastApproach.points.Skip(1)).ToArray()
    :id==P14West?lanes[id].points.Concat(westApproach.points.Skip(1)).ToArray():lanes[id].points;
  }
  var paint=westApproach.points.Last();
  document.facilities=document.facilities.Concat(new[]{new RoadMapFacility{
   id="reviewed:P14:west-driveway-paint",kind="lanePresentation",type=LaneArrowAppearance.StraightRightDrivewayType,
   laneId=westApproach.id,junctionId=westApproach.ruleId,x=paint.x,z=paint.z,rule=false,scale=1,evidence=p14
  }}).ToArray();
  return document;
 }

 static void Validate(ProjectMapPackage before,RoadMapDocument document){
  var errors=RoadMapValidator.Validate(before.baseline,document).Where(issue=>issue.error).ToArray();
  Check(errors.Length==0,string.Join(" / ",errors.Take(8).Select(issue=>issue.ToString())));
  var map=RoadMapComposer.Compose(before.baseline,document);
  EungyeConnectivityRepair.Apply(map,before.baseline);
  var graph=new HdRoadGraph(map);
  var edgeById=graph.edges.ToDictionary(edge=>edge.id);
  var pavement=new RoadPaintGeometry(map);
  void Section(string family,float x,int east,int west){
   var section=pavement.Lanes(edgeById[family],new Vector3(x,0,257.5f),Vector3.right);
   Check(section.Count(l=>l.direction==1)==east&&section.Count(l=>l.direction==-1)==west,"lane cross section at x="+x);
  }
  Section(P13East,602,2,1);
  Section(P13East,626,2,1);
  Section(P13East,653,2,1);
  Section(P13East,690,2,1);
  Section(P14West,525,1,2);
  Section(P14East,557,2,1);
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>map.hdEdges[a.edgeIndex].id);
  void Movement(string id,int expectedPaint,params int[] expectedTurns){
   var edge=edgeById[id];
   Check(arrows[id].Any(a=>a.turns==expectedPaint),"missing painted movement "+id);
   Check(edge.next.Select(i=>graph.edges[i].turn).OrderBy(t=>t).SequenceEqual(expectedTurns),"wrong graph turns "+id);
   foreach(int i in edge.next){
    var join=graph.edges[i];
    Check(Vector3.Distance(edge.points.Last().V(),join.points[0].V())<.03f,"disconnected join "+join.id);
    Y07MapTests.Footprint(map,join);
   }
  }
  Movement(P14EastLeft,LaneDirectionPaint.Left,-1);
  Movement(ApproachId(P14East),LaneDirectionPaint.Straight,0);
  Movement(P14WestLeft,LaneDirectionPaint.Left,-1);
  Movement(ApproachId(P14West),LaneDirectionPaint.Straight|LaneDirectionPaint.Right,1);
  Movement(Terminal(P13West),LaneDirectionPaint.Straight|LaneDirectionPaint.Right,0,1);
  foreach(string name in new[]{"west-stop","west-signal"}){
   var facility=document.facilities.Single(f=>f.id==FacilityPrefix+name);
   Check(facility.laneId==Terminal(P13West),"midblock facility lost its westbound approach");
  }
  var stop=map.stops.Single(s=>s.name=="은계 승하차 12");
  var oldStop=before.baseline.stops.Single(s=>s.name==stop.name);
  Check(graph.edges[stop.laneEdge].id==Terminal(P13West)
   &&Mathf.Abs(stop.x-oldStop.x)<.4f&&Mathf.Abs(stop.z-oldStop.z)<.08f,"boarding curb alignment changed");
  var catalog=PassengerStopCatalog.For(map);
  Check(catalog.Stops.Length==39,"preserve all 39 stops: "+string.Join("; ",catalog.Rejected));
  int routes=0;
  foreach(var start in catalog.Stops)foreach(var end in catalog.Stops)if(start!=end){
   Check(graph.Route(start.laneEdge,start.laneAt,end.laneEdge,end.laneAt)!=null,"unreachable stop "+start.name+" → "+end.name);
   routes++;
  }
  Check(routes==1482,"all directed stop pairs");
  Debug.Log("EUNGYE_COMMERCE_VALIDATED P13 E2/W1 P14 E1/W2 to E2/W1; 39 stops, 1482 routes");
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  var document=Preview(before);
  document.revision=31;
  document.savedAt=DateTime.UtcNow.ToString("o");
  document.projectRevision="";
  Validate(before,document);
  var candidate=ProjectMapPackage.Create(before.baseline,document);
  ProjectMapStore.Save(root,candidate,token);
  var reloaded=ProjectMapStore.Load(root);
  Check(reloaded.contentHash==candidate.contentHash&&reloaded.baselineHash==before.baselineHash,"stored map identity");
  Validate(before,reloaded.document);
  ProjectMapBuild.Prepare();
  Debug.Log("EUNGYE_COMMERCE_APPLIED revision=31 contentHash="+reloaded.contentHash);
 }

 public static void Apply35(){
  const string reviewedR31="c42ef1391c7bf32f72a528ce2fd40d8ac20dcd13d37fd738ec501c4b98f43e54";
  const string outer="local:5868:2510:7086:2503";
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Check(before.document.revision==31&&before.contentHash==reviewedR31,"question 35 requires the exact r31 approach");
  var document=before.document.Copy();
  var approaches=new[]{Terminal(P13East),Terminal(outer)};
  var evidence=new RoadMapEvidence{
   state="user",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=false,ruleConfirmed=false,
   url="https://map.naver.com/p?c=19,0,0,0,adh&p=mIgobMxADqm9jq9J7Lw5pg,92,-21,70,Float",
   note="Question 35: user requested no lane direction arrows on this building entrance approach. Keep the inner eastbound left and straight connections and the outer through connection. The visible outer arrow was present in the panorama; this is an intentional game presentation choice."
  };
  var suppression=approaches.Select((id,i)=>{
   var lane=document.lanes.Single(l=>l.id==id&&!l.disabled);
   var end=lane.points.Last();
   return new RoadMapFacility{
    id="reviewed:P13:question35:no-arrows:"+(i+1),kind="lanePresentation",type=LaneDirectionPaint.NoArrowsType,
    laneId=id,junctionId=lane.ruleId,x=end.x,z=end.z,scale=1,evidence=evidence
   };
  }).ToArray();
  document.facilities=document.facilities.Concat(suppression).ToArray();
  document.revision=32;
  document.savedAt=DateTime.UtcNow.ToString("o");
  document.projectRevision="";
  void Verify35(RoadMapDocument result){
   Validate(before,result);
   Check(result.lanes.Select(l=>JsonUtility.ToJson(l)).SequenceEqual(before.document.lanes.Select(l=>JsonUtility.ToJson(l))),
    "arrow suppression altered the vehicle graph");
   var map=RoadMapComposer.Compose(before.baseline,result);
   var graph=new HdRoadGraph(map);
   var arrows=LaneDirectionPaint.Layout(map);
   Check(!arrows.Any(a=>approaches.Contains(graph.edges[a.edgeIndex].id)),"question 35 has a rendered turn arrow");
   for(int i=0;i<approaches.Length;i++){
    var entry=graph.edges.Single(e=>e.id==approaches[i]&&!e.disabled);
    var movements=entry.next.Select(n=>graph.edges[n]).Where(e=>!e.disabled).Select(e=>e.turn).OrderBy(turn=>turn).ToArray();
    Check(movements.SequenceEqual(i==0?new[]{-1,0}:new[]{0}),"question 35 lost left or straight navigation");
   }
  }
  Verify35(document);
  var candidate=ProjectMapPackage.Create(before.baseline,document);
  ProjectMapStore.Save(root,candidate,token);
  var reloaded=ProjectMapStore.Load(root);
  Check(reloaded.contentHash==candidate.contentHash&&reloaded.baselineHash==before.baselineHash,"stored question 35 map identity");
  Verify35(reloaded.document);
  ProjectMapBuild.Prepare();
  Debug.Log("QUESTION35_MAP_APPLIED revision=32 contentHash="+reloaded.contentHash+" no approach arrows; inner L/S outer S");
 }
}
