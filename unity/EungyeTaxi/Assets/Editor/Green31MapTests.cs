using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Editor-only reusable validation; preview and guarded Apply share these checks.
public static class Green31MapTests {
 static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("GREEN31_TEST: "+why);}
 public static void PreviewChecks(){var p=ProjectMapStore.Load(ProjectMapBuild.Root);PreviewChecks(Green31MapEdit.DefaultParameters(p));}
 public static void MissingAnchorGuards(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);string before=JsonUtility.ToJson(p);bool rejected=false;
  try{Green31MapEdit.Preview(p,new Green31MapEdit.Parameters());}catch(ArgumentException){rejected=true;}
  Check(rejected,"missing A/B anchors accepted");Check(before==JsonUtility.ToJson(p),"rejected preview changed input");
  Debug.Log("GREEN31_MISSING_ANCHORS_OK no-save");
 }
 public static void PreviewChecks(Green31MapEdit.Parameters anchors){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);string before=JsonUtility.ToJson(p);
  var d=Green31MapEdit.Preview(p,anchors);Check(before==JsonUtility.ToJson(p),"input mutation");
  Check(d.ToJson()==Green31MapEdit.Preview(p,anchors).ToJson(),"preview deterministic IDs and split nodes");
  Validate(p,d,anchors);Debug.Log("GREEN31_PREVIEW_OK deterministic input-immutable no-save");
 }
 public static void Validate(ProjectMapPackage before,RoadMapDocument after,Green31MapEdit.Parameters anchors){
  Check(before!=null&&after!=null&&anchors!=null,"before/after/anchors required");var p=before;var d=after;
  for(int i=0;i<p.document.lanes.Length;i++)Check(d.lanes[i].id==p.document.lanes[i].id&&d.lanes[i].sourceId==p.document.lanes[i].sourceId&&d.lanes[i].sourceOffset==p.document.lanes[i].sourceOffset,"original identity/order/source station retained");
  var replaced=anchors.A.replaceSceneIds.Concat(anchors.B.replaceSceneIds).ToArray();foreach(var o in p.document.scene.objects.Where(o=>!replaced.Contains(o.id)))Check(JsonUtility.ToJson(o)==JsonUtility.ToJson(d.scene.objects.Single(n=>n.id==o.id)),"unrelated scene geometry changed "+o.id);
  Check(p.document.lanes.All(l=>d.lanes.Any(n=>n.id==l.id)),"existing ID lost");
  foreach(string side in new[]{"A","B"}){
   var left=d.lanes.Single(l=>l.id==Green31MapEdit.Prefix+side+":left");
   var right=d.lanes.Single(l=>l.id==Green31MapEdit.Prefix+side+":right");
   Check(left.number==1&&right.number==5,"approach order");
   Check(left.next.Length==2&&left.next.Select(id=>d.lanes.Single(l=>l.id==id).turn).OrderBy(x=>x).SequenceEqual(new[]{-2,-1}),"left pocket has conditional U plus left only");
   var condition=UturnMovement.Condition(d,left.id);Check(condition!=null&&condition.type==UturnMovement.LeftOrPedestrianGreen&&condition.rule&&condition.evidence.ruleConfirmed,"user-confirmed conditional U facility");
   Check(right.next.Length==1&&d.lanes.Single(l=>l.id==right.next[0]).turn==1,"separated slip must be right-only");
   int index=side=="A"?0:1;
   foreach(string id in Green31MapEdit.Incoming[index]){
    var head=d.lanes.Single(l=>l.id==id);
    var tail=d.lanes.Single(l=>l.id==head.next.First());
    Check(tail.next.Length==1&&d.lanes.Single(l=>l.id==tail.next[0]).turn==0,"three through tails must stay straight-only");
   }
   Check(d.scene.objects.Any(o=>o.enabled&&o.id==Green31MapEdit.Prefix+side+":island"&&o.kind=="sidewalk"),"physical island absent");
   Check(d.facilities.Any(f=>f.id==Green31MapEdit.Prefix+side+":crosswalk"&&f.kind=="crosswalk"),"slip crossing absent");
  }
  Check(d.lanes.Count(l=>l.id.StartsWith("join:"+Green31MapEdit.Prefix)&&l.turn==UturnMovement.Turn)==2,"exactly two reviewed U movements");
  var errors=RoadMapValidator.Validate(p.baseline,d).Where(e=>e.error).ToArray();Check(errors.Length==0,string.Join(" / ",errors.Take(8).Select(e=>e.ToString())));
  var map=RoadMapComposer.Compose(p.baseline,d);EungyeConnectivityRepair.Apply(map,p.baseline);var graph=new HdRoadGraph(map);
  var paint=LaneDirectionPaint.Layout(map);
  foreach(string side in new[]{"A","B"}){
   string prefix=Green31MapEdit.Prefix+side+":";int sideIndex=side=="A"?0:1;
   var approaches=new[]{d.lanes.Single(l=>l.id==prefix+"left")}.Concat(Green31MapEdit.Incoming[sideIndex].Select(id=>d.lanes.Single(l=>l.id==d.lanes.Single(h=>h.id==id).next.First()))).Concat(new[]{d.lanes.Single(l=>l.id==prefix+"right")}).ToArray();
   var leftPath=new AssistedRoadNetwork.Path(approaches[0].points.Select(q=>q.V()).ToArray());var section=leftPath.Position(leftPath.length-25);var forward=leftPath.Tangent(leftPath.length-25);float previous=float.NegativeInfinity;
   for(int i=0;i<5;i++){
    var lane=approaches[i];int index=Array.FindIndex(graph.edges,e=>e.id==lane.id);int expected=i==0?LaneDirectionPaint.Left|UturnMovement.PaintMask(UturnMovement.Turn):i==4?LaneDirectionPaint.Right:LaneDirectionPaint.Straight;
    Check(paint.Any(a=>a.edgeIndex==index&&a.turns==expected),"actual approach arrows "+lane.id);
    Vector3 crossing=Vector3.zero;bool found=false;for(int k=1;k<lane.points.Length;k++){var a=lane.points[k-1].V();var b=lane.points[k].V();float da=Vector3.Dot(a-section,forward),db=Vector3.Dot(b-section,forward);if(da<=0&&db>0){crossing=Vector3.Lerp(a,b,-da/(db-da));found=true;break;}}
    Check(found,"five-lane physical crosssection "+lane.id);float lateral=Vector3.Dot(crossing-section,TrafficSystem.Right(forward));if(i>0)Check(lateral-previous>=3.3f,"five lanes have usable separation "+lane.id);previous=lateral;
   }
   var join=d.lanes.Single(l=>l.id=="join:"+prefix+"uturn");var target=d.lanes.Single(l=>l.id==join.next.Single());var from=approaches[0];Check(UturnMovement.GeometryError(join.points.Select(q=>q.V()).ToArray(),(from.points.Last().V()-from.points[from.points.Length-2].V()).normalized,(target.points[1].V()-target.points[0].V()).normalized)==null,"U radius and endpoint headings");
   int fromIndex=Array.FindIndex(graph.edges,e=>e.id==from.id),toIndex=Array.FindIndex(graph.edges,e=>e.id==target.id);var route=graph.Route(fromIndex,graph.paths[fromIndex].length-5,toIndex,5);Check(route!=null&&route.edges.Any(i=>graph.edges[i].id==join.id),"native graph uses reviewed U connector");
  }
  ConditionalSignals(map,d);
  var changed=d.lanes.Where(l=>!p.document.lanes.Any(o=>o.id==l.id&&JsonUtility.ToJson(o)==JsonUtility.ToJson(l))).Select(l=>l.id).ToArray();
  foreach(var edge in map.hdEdges.Where(e=>changed.Contains(e.id)&&!e.disabled)){
   Footprint(map,edge);
  }
  var stops=PassengerStopCatalog.For(map).Stops;int pairs=0;
  foreach(var a in stops)foreach(var b in stops)if(a!=b){Check(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)!=null,"route "+a.name+" -> "+b.name);pairs++;}
  Check(stops.Length==39&&pairs==1482,"passenger coverage changed");Debug.Log("GREEN31_VALIDATION_OK both=L+U/S/S/S/separatedR stops=39 pairs=1482 U=left-or-pedestrian-green");
 }
 static void ConditionalSignals(MapData map,RoadMapDocument document){
  string condition=UturnMovement.LeftOrPedestrianGreen;Check(UturnMovement.Allows(condition,true,false,true)&&UturnMovement.Allows(condition,false,true,true),"left or pedestrian green permits U");Check(!UturnMovement.Allows(condition,false,false,true)&&!UturnMovement.Allows(condition,true,true,false),"straight-only/red and obstructed U denied");
  var owner=new GameObject("Green31 conditional U signal test");try{
   var traffic=owner.AddComponent<TrafficSystem>();traffic.Build(map,false);traffic.manualClock=true;var junction=traffic.junctions.Single(j=>j.name==Green31MapEdit.Junction);
   foreach(string side in new[]{"A","B"}){var sign=UturnMovement.Condition(document,Green31MapEdit.Prefix+side+":left");bool allowed=false,denied=false,walkSeen=false,leftSeen=false;
    for(float time=0;time<300;time+=.5f){traffic.manualTime=time;bool left=traffic.LeftSignal(junction,sign.axis,sign.sign)==SignalColor.Green,walk=traffic.PedestrianGreen(junction,sign.axis,sign.sign);bool actual=traffic.UturnPermitted(junction,sign.axis,sign.sign,sign.laneId);Check(actual==(left||walk),"native U signal condition "+side+" t="+time);allowed|=actual;denied|=!actual;walkSeen|=walk;leftSeen|=left;}
    Check(allowed&&denied&&walkSeen&&leftSeen,"actual signal cycle exercises permitted and denied states "+side);
   }
  }finally{UnityEngine.Object.DestroyImmediate(owner);}
 }
 static void Footprint(MapData map,HdEdge edge){
  string reason;if(EungyeConnectivityRepair.FootprintClear(map,edge,out reason))return;Check(reason=="reverse/abrupt heading","footprint "+edge.id+": "+reason);
  var path=new AssistedRoadNetwork.Path(edge.points.Select(p=>p.V()).ToArray());float step=edge.id.StartsWith("join:")?1:4,window=edge.id.StartsWith("join:")?2:6;
  for(float start=0;start<path.length;start+=step){float end=Mathf.Min(path.length,start+window);var points=new List<MapPoint>();for(float at=start;at<end-.001f;at+=.5f){var p=path.Position(at);points.Add(new MapPoint{x=p.x,z=p.z});}var q=path.Position(end);points.Add(new MapPoint{x=q.x,z=q.z});Check(EungyeConnectivityRepair.FootprintClear(map,new HdEdge{points=points.ToArray()},out reason),"local swept footprint "+edge.id+" station="+start+": "+reason);}
 }
}
