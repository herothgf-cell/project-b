using System;
using System.Linq;
using System.Text;
using UnityEngine;

internal static partial class ConfirmedLaneSurveyR42 {
 const string PinkApproach="editor:segment:bG9jYWw6NTIwNDo4MjY6NzY1OTo4MTc:pink-roundabout";
 const string Pink52Root="local:5204:826:7659:817";
 const string Pink52Junction="J7841_867";
 const string Pink53Junction="J5076_2580";
 const string Pink53Left="reviewed:J5076-N:left";
 const string Pink53Outer="editor:segment:bG9jYWw6NTAzNDo0MjAzOjUwMzM6MjY5OA:e95362da57f848b691efebf061483b33";
 const string Pink51Left="reviewed:r42:P51:left";
 const string Pink51Westbound="local:7660:918:5205:927";
 const string Pink52Left="reviewed:r42:P52:left";
 const string Pink51Hanyang="service:reviewed:r42:P51:hanyang-gate";
 const string Pink51Hoban="service:reviewed:r42:P51:hoban-gate";
 const string Pink53Gate="service:reviewed:r42:P53:park-xi-gate";

 static void PinkCheck(bool condition,string failure){
  if(!condition)throw new InvalidOperationException("R42_PINK: "+failure);
 }

 static Vector3 PinkPoint(float x,float z)=>new Vector3(x,0,z);
 static RoadMapLane PinkLane(RoadMapDocument d,string id)=>d.lanes.Single(l=>l.id==id);
 static RoadMapLane PinkCopy(RoadMapLane l)=>JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(l));
 static AssistedRoadNetwork.Path PinkPath(RoadMapLane l)=>new AssistedRoadNetwork.Path(l.points.Select((p,i)=>
  p.V(l.heights.Length==l.points.Length?l.heights[i]:0)).ToArray());

 static RoadMapEvidence PinkEvidence(string camera,string note)=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p="+camera+",Float",note=note
 };

 // Keep a deterministic segment ID in the original corridor's stop/route lineage.
 static RoadMapLane PinkSplit(RoadMapDocument d,RoadMapLane head,float at,string label,RoadMapEvidence evidence){
  var path=PinkPath(head);
  PinkCheck(at>.01f&&at<path.length-.01f,"split outside "+head.id);
  PinkCheck(!d.facilities.Any(f=>f.laneId==head.id),"unexpected facility bound to "+head.id);
  var tail=PinkCopy(head);
  string origin=Convert.ToBase64String(Encoding.UTF8.GetBytes(RoadMapAuthoringOperations.Origin(head)))
   .TrimEnd('=').Replace('+','-').Replace('/','_');
  tail.id="editor:segment:"+origin+":r42-"+label;
  tail.fromNode=tail.id+":split";
  tail.sourceOffset+=at;
  Y06EastApproachEdit.Assign(tail,Y07MapEdit.Slice(path,at,path.length).Select(p=>p.V()),evidence);
  Y06EastApproachEdit.Assign(head,G05NorthWestMapEdit.OriginalHead(path,at),evidence);
  head.toNode=tail.fromNode;
  head.next=new[]{tail.id};
  foreach(var service in d.scene.objects.Where(o=>o.IsService&&o.laneId==head.id&&o.laneAt>=at)){
   service.laneId=tail.id;
   service.laneAt-=at;
  }
  d.lanes=d.lanes.Append(tail).ToArray();
  return tail;
 }

 static RoadMapLane PinkPocket(RoadMapDocument d,RoadMapLane head,RoadMapLane outer,string id,int number,
  RoadMapEvidence evidence,float taper){
  var lane=PinkCopy(outer);
  lane.id=id;lane.toNode=id+":end";lane.number=number;lane.width=3.25f;
  lane.sourceOffset=0;lane.next=Array.Empty<string>();
  var path=PinkPath(outer);
  int steps=Mathf.CeilToInt(path.length);
  Y06EastApproachEdit.Assign(lane,Enumerable.Range(0,steps+1).Select(i=>{
   float at=i*path.length/steps;
   float offset=3.25f*Mathf.SmoothStep(0,1,Mathf.Clamp01(at/taper));
   return path.Position(at)-TrafficSystem.Right(path.Tangent(at))*offset;
  }),evidence);
  head.next=head.next.Append(lane.id).ToArray();
  d.lanes=d.lanes.Append(lane).ToArray();
  return lane;
 }

 static RoadMapLane PinkGate(RoadMapDocument d,string id,string name,Vector3 start,Vector3 finish,RoadMapEvidence evidence){
  var lane=new RoadMapLane{id=id,sourceId=id,fromNode=id+":mouth",toNode=id+":gate",
   ruleId="",roadName=name,number=1,width=3.4f,status="provisional"};
  Y06EastApproachEdit.Assign(lane,new[]{start,finish},evidence);
  d.lanes=d.lanes.Append(lane).ToArray();
  PinkSurface(d,lane,5.2f);
  return lane;
 }

 static RoadMapLane PinkConnect(RoadMapDocument d,RoadMapLane from,RoadMapLane to,string id,int turn,RoadMapEvidence evidence){
  var join=new RoadMapLane{id=id,sourceId=from.sourceId,ruleId=from.ruleId,roadName=from.roadName,
   status="provisional",width=from.width,turn=turn};
  Y06EastApproachEdit.Reconnect(join,from,to,evidence);
  d.lanes=d.lanes.Append(join).ToArray();
  from.next=from.next.Append(join.id).ToArray();
  PinkSurface(d,join,turn==0?3.8f:5.2f);
  return join;
 }

 static void PinkSurface(RoadMapDocument d,RoadMapLane lane,float width){
  string id="road:polish:"+lane.id;
  var surface=d.scene.objects.SingleOrDefault(o=>o.id==id);
  if(surface==null){
   surface=new ProjectMapObject{id=id,kind="road",name=lane.roadName,lanes=1,oneWay=true,suppressPaint=true};
   d.scene.objects=d.scene.objects.Append(surface).ToArray();
  }
  surface.width=width;surface.points=lane.points;
 }

 static void PinkCurbOpening(RoadMapDocument d,RoadMapLane corner,RoadMapLane gate){
  var paths=new[]{PinkPath(corner),PinkPath(gate)};
  float minX=paths.Min(p=>p.points.Min(q=>q.x))-3,maxX=paths.Max(p=>p.points.Max(q=>q.x))+3;
  float minZ=paths.Min(p=>p.points.Min(q=>q.z))-3,maxZ=paths.Max(p=>p.points.Max(q=>q.z))+3;
  foreach(var panel in d.scene.objects.Where(o=>o.enabled&&o.id.StartsWith("sidewalk:generated:")&&
   o.points.Any(p=>p.x>=minX&&p.x<=maxX&&p.z>=minZ&&p.z<=maxZ))){
   var center=panel.points.Select(p=>p.V()).Aggregate(Vector3.zero,(a,b)=>a+b)/panel.points.Length;
   if(panel.points.Select(p=>p.V()).Append(center).Any(p=>paths.Any(path=>{
    path.Project(p,out float error);return error<2.65f;
   })))panel.enabled=false;
  }
 }

 static void PatchPink51(RoadMapDocument d){
  var e=PinkEvidence("TUjB5q0mBinydhLKhg6aSA,90,-19,85",
   "Marker 51. Left to Hoban main gate; outer lane painted straight and user-confirmed right access to Hanyang. Gate geometry and lane taper are game approximations.");
  var head=PinkLane(d,PinkApproach);
  var path=PinkPath(head);
  var outer=PinkSplit(d,head,path.Project(PinkPoint(612,82),out _),"P51-approach",e);
  var after=PinkSplit(d,outer,PinkPath(outer).Project(PinkPoint(645,82),out _),"P51-after-stop",e);
  var departure=PinkSplit(d,after,PinkPath(after).Project(PinkPoint(675,82),out _),"P51-departure",e);
  after.disabled=true;after.next=Array.Empty<string>();
  outer.next=Array.Empty<string>();outer.ruleId="";outer.number=2;outer.width=3.25f;
  var left=PinkPocket(d,head,outer,Pink51Left,1,e,21);
  var rightGate=PinkGate(d,Pink51Hanyang,"한양수자인더클래스 정문",PinkPoint(662,74),PinkPoint(662.1f,63),e);
  var leftGate=PinkGate(d,Pink51Hoban,"시흥은계호반써밋플레이스 정문",PinkPoint(659.5f,101),PinkPoint(659.5f,112),e);
  var right=PinkConnect(d,outer,rightGate,"join:"+Pink51Hanyang,1,e);
  var turn=PinkConnect(d,left,leftGate,"join:"+Pink51Hoban,-1,e);
  PinkConnect(d,outer,departure,"join:reviewed:r42:P51:through",0,e);
  PinkCurbOpening(d,right,rightGate);
  PinkCurbOpening(d,turn,leftGate);
  var paint=PinkEvidence("TUjB5q0mBinydhLKhg6aSA,90,-19,85","Outer lane retains the observed straight-only glyph.");
  paint.ruleConfirmed=false;
  d.facilities=d.facilities.Append(new RoadMapFacility{id="reviewed:r42:P51:outer-arrow",kind="lanePresentation",
   laneId=outer.id,type=LaneArrowAppearance.StraightType,x=642,z=82.2f,evidence=paint}).ToArray();
  var westbound=PinkLane(d,Pink51Westbound);
  var westPath=PinkPath(westbound);
  d.facilities=d.facilities.Append(new RoadMapFacility{id="reviewed:r42:P51:yellow-gap",kind="lanePresentation",
   laneId=westbound.id,type=ReviewedUnmarkedRoadPaint.FeatureType,width=15f,x=660,z=87.2f,
   startAt=westbound.sourceOffset+westPath.Project(PinkPoint(674,92),out _),
   endAt=westbound.sourceOffset+westPath.Project(PinkPoint(647,92),out _),evidence=paint}).ToArray();
  PinkApproachPaint(d,"P51",outer,645,87.2f,659, e);
 }

 static void PinkApproachPaint(RoadMapDocument d,string marker,RoadMapLane outer,float stopX,float roadZ,float centerX,RoadMapEvidence e){
  var position=outer.points.Last().V();
  var marks=new[]{
   new RoadMapFacility{id="reviewed:r42:"+marker+":stop",kind="stopLine",x=stopX-1,z=position.z+1.625f,
    yaw=90,width=6.5f,length=.4f,laneId=outer.id,evidence=e},
   new RoadMapFacility{id="reviewed:r42:"+marker+":crosswalk-west",kind="crosswalk",x=stopX+4,z=roadZ,
    yaw=90,width=13.8f,length=3,evidence=e},
   new RoadMapFacility{id="reviewed:r42:"+marker+":crosswalk-east",kind="crosswalk",x=centerX+12,z=roadZ,
    yaw=90,width=13.8f,length=3,evidence=e}
  };
  d.facilities=d.facilities.Concat(marks).ToArray();
 }

 static void PatchPink52(RoadMapDocument d){
  var e=PinkEvidence("yZrIHxtkhRqOK3xPz-bbXQ,90,-19,85",
   "Marker 52 at the Eungye-ro stop line. Inside left only; outside straight and right as confirmed by the user. Game taper and lane width approximated.");
  var head=d.lanes.Single(l=>!l.disabled&&l.next.Contains("join:"+Pink52Root+":-1"));
  var path=PinkPath(head);
  var outer=PinkSplit(d,head,path.Project(PinkPoint(723,82),out _),"P52-outer",e);
  var left=PinkPocket(d,head,outer,Pink52Left,1,e,21);
  var turn=PinkLane(d,"join:"+Pink52Root+":-1");
  left.next=new[]{turn.id};
  outer.next=outer.next.Where(id=>id!=turn.id).ToArray();
  outer.number=2;outer.width=3.25f;
  Y06EastApproachEdit.Reconnect(turn,left,PinkLane(d,turn.next.Single()),e);
  PinkSurface(d,turn,4.4f);
 }

 static void PatchPink53(RoadMapDocument d){
  var e=PinkEvidence("wVEsQPBikr-YFOKor3WSGQ,181,-19,85",
   "Marker 53. Existing left-only lane and straight receiving path retained; user confirmed the outside lane may turn right only to the Eungye Park Xi gate. Gate geometry estimated.");
  var outer=PinkLane(d,Pink53Outer);
  var gate=PinkGate(d,Pink53Gate,"은계파크자이 정문",PinkPoint(493.4f,255.3f),PinkPoint(488,255.25f),e);
  var right=PinkConnect(d,outer,gate,"join:"+gate.id,1,e);
  PinkCurbOpening(d,right,gate);
 }

 private static void EditPink(RoadMapDocument document){
  PatchPink51(document);
  PatchPink52(document);
  PatchPink53(document);
 }

 private static void VerifyPink(RoadMapDocument document){
  void Moves(string id,int number,params int[] turns){
   var lane=PinkLane(document,id);
   PinkCheck(!lane.disabled&&lane.number==number&&lane.next.Select(n=>PinkLane(document,n))
    .All(n=>!n.disabled&&n.change==0),"lane or movement unavailable "+id);
   PinkCheck(lane.next.Select(n=>PinkLane(document,n).turn).OrderBy(t=>t).SequenceEqual(turns),"incorrect movements "+id);
  }
  string Outer(string marker)=>document.lanes.Single(l=>l.id.EndsWith(":r42-"+marker)).id;
  var outer51=Outer("P51-approach");
  Moves(Pink51Left,1,-1);Moves(outer51,2,0,1);
  Moves(Pink52Left,1,-1);Moves(Outer("P52-outer"),2,0,1);
  Moves(Pink53Left,1,-1);Moves(Pink53Outer,2,0,1);
  foreach(var gate in new[]{Pink51Hanyang,Pink51Hoban,Pink53Gate}){
   var join=PinkLane(document,"join:"+gate);
   PinkCheck(join.next.SequenceEqual(new[]{gate})&&PinkLane(document,gate).next.Length==0,"private gate destination "+gate);
  }
  PinkCheck(PinkLane(document,Pink52Left).ruleId==Pink52Junction&&PinkLane(document,Pink53Outer).ruleId==Pink53Junction,
   "existing signal junction binding");
 }

 private static void VerifyPinkRuntime(MapData map){
  var graph=new HdRoadGraph(map);
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>map.hdEdges[a.edgeIndex].id);
  int s=LaneDirectionPaint.Straight,l=LaneDirectionPaint.Left,sr=s|LaneDirectionPaint.Right;
  var outer51=map.editorDocument.lanes.Single(x=>x.id.EndsWith(":r42-P51-approach")).id;
  var outer52=map.editorDocument.lanes.Single(x=>x.id.EndsWith(":r42-P52-outer")).id;
  var expected=new[]{(Pink51Left,l,l,Pink51Hoban),(outer51,sr,s,Pink51Hanyang),
   (Pink52Left,l,l,""),(outer52,sr,sr,""),(Pink53Left,l,l,""),(Pink53Outer,sr,sr,Pink53Gate)};
  foreach(var (id,movements,paint,gate) in expected){
   int index=Array.FindIndex(graph.edges,e=>e.id==id&&!e.disabled);
   PinkCheck(index>=0,"active runtime approach "+id);
   var approach=graph.edges[index];
   PinkCheck(approach.next.Aggregate(0,(mask,next)=>mask|UturnMovement.PaintMask(graph.edges[next].turn))==movements,
    "runtime movement mask "+id);
   PinkCheck(arrows[id].Any(a=>a.turns==paint),"rendered arrow mask "+id);
   Y07MapTests.Footprint(map,approach);
   foreach(int n in approach.next){
    var turn=graph.edges[n];
    PinkCheck(turn.next.Length==1&&(turn.points[0].V()-approach.points.Last().V()).sqrMagnitude<.001f,
     "connector entrance seam "+turn.id);
    var next=graph.edges[turn.next.Single()];
    PinkCheck((turn.points.Last().V()-next.points[0].V()).sqrMagnitude<.001f,"connector exit seam "+turn.id);
    if(turn.id.Contains("reviewed:r42:")||id==Pink52Left)Y07MapTests.Footprint(map,turn);
   }
   if(!string.IsNullOrEmpty(gate)){
    int destination=Array.FindIndex(graph.edges,e=>e.id==gate&&!e.disabled);
    var route=graph.Route(index,0,destination,3);
    PinkCheck(route!=null&&route.edges.Count==3&&graph.edges[route.edges[1]].id=="join:"+gate,
     "direct apartment gate route "+gate);
   }
  }
  Debug.Log("R42_PINK_OK 51 L/S+R (S glyph), 52 L/S+R, 53 L/S+ParkXi right");
 }
}
