using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ParkingEastLaneEdit {
 const string BeforeHash="a0591bde41c622c2dbee164aef373086226769e4c126611c9780d5607195eb70";
 const string East="local:6575:3281:7166:2686";
 const string West="local:6387:3402:5713:2694";
 const string EastPrefix="reviewed:P26:";
 const string WestPrefix="reviewed:P20:";
 const string Restaurant="service:restaurant";
 const float SplitAt=3f;
 const float Taper=10f;
 const float ChangeStart=36f;
 const float ChangeEnd=51f;
 const float Spacing=3.15f;

 static void Check(bool ok,string description){
  if(!ok)throw new InvalidOperationException("PARKING_LANES: "+description);
 }
 static RoadMapEvidence Evidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=false,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=LQKqHb7LW4RAt_LeeWBJfA,160,-19,76,Float",
  note="East of the parking crosswalk, May 2026: two lanes towards J7204, one away from it; inside lane turns left, outside turns right. Curve surveyed at BfrcfWhO7_OnVbFw9xazXA. Split station, taper, lane spacing and lane-change window are game estimates. Existing restaurant curb access is preserved."
 };
 static RoadMapEvidence WestEvidence()=>new RoadMapEvidence{
  state="observed",month="2024-10",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=false,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=gu7edWUCS5hhm0Ex2XBv2Q,-171,-33,80,Float",
  note="Towards J5750: use two lanes on the west/south approach, one opposing. The 2024 stop-line panorama shows inside left and outside right; May 2026 shows the same curb layout and faint worn divider. The offset and split beyond the parking crossing are game estimates."
 };
 static string Outer(string root,string part)=>"editor:segment:"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(root))
  .TrimEnd('=').Replace('+','-').Replace('/','_')+":parking-"+part;
 static string Join(string root,int turn)=>"join:"+root+":"+turn;
 static RoadMapLane Copy(RoadMapLane lane)=>JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));
 static AssistedRoadNetwork.Path Path(RoadMapLane lane)=>new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());

 static IEnumerable<Vector3> Slice(AssistedRoadNetwork.Path path,float start,float end){
  yield return path.Position(start);
  float at=0;
  for(int i=1;i<path.points.Length;i++){
   at+=Vector3.Distance(path.points[i-1],path.points[i]);
   if(at>start+.001f&&at<end-.001f)yield return path.points[i];
  }
  yield return path.Position(end);
 }

 static void RefreshSurface(RoadMapDocument document,RoadMapLane lane){
  var surface=document.scene.objects.SingleOrDefault(o=>o.id=="road:polish:"+lane.id);
  if(surface!=null)surface.points=lane.points;
 }

 static RoadMapDocument Preview(ProjectMapPackage package){
  Check(package.document.revision==32&&package.contentHash==BeforeHash,"exact r32 required");
  var d=package.document.Copy();
  SplitApproach(d,East,EastPrefix,Evidence(),true);
  SplitApproach(d,West,WestPrefix,WestEvidence(),false);
  RoadMapAuthoringOperations.RemapServiceBindings(d);
  return d;
 }

 static void SplitApproach(RoadMapDocument d,string root,string prefix,RoadMapEvidence evidence,bool restaurantApproach){
  var head=d.lanes.Single(l=>l.id==root);
  var path=Path(head);
  Check(new HashSet<string>(head.next).SetEquals(new[]{Join(root,-1),Join(root,1)})&&path.length>ChangeEnd+18,"unexpected approach layout "+root);
  if(restaurantApproach){
   var restaurant=d.scene.objects.Single(o=>o.id==Restaurant);
   Check(restaurant.laneId==root&&restaurant.laneAt>SplitAt&&restaurant.laneAt<ChangeStart,"restaurant must stay on outer upstream lane");
  }
  Check(!d.facilities.Any(f=>f.laneId==root)&&!d.signals.Any(p=>p.groups.Any(g=>g.laneIds.Contains(root))),"new binding on incoming lane "+root);
  var outerEnd=Copy(head);
  var innerEnd=Copy(head);
  var added=new List<RoadMapLane>();
  outerEnd.id=Outer(root,"approach");
  innerEnd.id=prefix+"left:part:1";
  head.toNode=prefix+"fork";
  head.next=new[]{outerEnd.id,innerEnd.id};
  outerEnd.fromNode=innerEnd.fromNode=head.toNode;
  innerEnd.toNode=prefix+"left-end";
  outerEnd.number=2;
  outerEnd.sourceOffset=restaurantApproach?ChangeEnd:SplitAt;
  Y06EastApproachEdit.Assign(head,Slice(path,0,SplitAt),evidence);
  Y06EastApproachEdit.Assign(outerEnd,Slice(path,outerEnd.sourceOffset,path.length),evidence);
  Vector3 Offset(float station){
   float offset=Spacing*Mathf.SmoothStep(0,1,Mathf.Clamp01((station-SplitAt)/Taper));
   // Smooth the lateral frame over the corner; a point tangent at a sharp
   // polyline vertex can reverse the order of successive offset points.
   var heading=(path.Position(Mathf.Min(path.length,station+6f))-path.Position(Mathf.Max(0f,station-6f))).normalized;
   return path.Position(station)-TrafficSystem.Right(heading)*offset;
  }
  Vector3 Inside(float station){
   int radius=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(restaurantApproach?5f:10f,station-SplitAt,path.length-station)));
   var sum=Vector3.zero;
   int weight=0;
   for(int step=-radius;step<=radius;step++){
    int share=1+radius-Mathf.Abs(step);
    sum+=Offset(station+step)*share;
    weight+=share;
   }
   return sum/weight;
  }
  IEnumerable<Vector3> SampleInside(float start,float end){
   for(float at=start;at<end-.001f;at+=1)yield return Inside(at);
   yield return Inside(end);
  }
  Y06EastApproachEdit.Assign(innerEnd,SampleInside(outerEnd.sourceOffset,path.length),evidence);
  if(restaurantApproach){
   var outer=Copy(outerEnd);
   var inner=Copy(innerEnd);
   outer.id=Outer(root,"service");
   inner.id=prefix+"left";
   head.next=new[]{outer.id,inner.id};
   outer.fromNode=inner.fromNode=head.toNode;
   outer.toNode=prefix+"outer-change-start";
   inner.toNode=prefix+"inner-change-end";
   outerEnd.fromNode=prefix+"outer-change-end";
   innerEnd.fromNode=inner.toNode;
   outer.sourceOffset=SplitAt;
   Y06EastApproachEdit.Assign(outer,Slice(path,SplitAt,ChangeStart),evidence);
   Y06EastApproachEdit.Assign(inner,SampleInside(SplitAt,ChangeEnd),evidence);
   var outerChangeWindow=Copy(outer);
   outerChangeWindow.id=Outer(root,"change-window");
   outerChangeWindow.fromNode=outer.toNode;
   outerChangeWindow.toNode=outerEnd.fromNode;
   outerChangeWindow.sourceOffset=ChangeStart;
   outerChangeWindow.next=new[]{outerEnd.id};
   Y06EastApproachEdit.Assign(outerChangeWindow,Slice(path,ChangeStart,ChangeEnd),evidence);
   var change=Copy(outer);
   change.id="change:"+prefix+"prepare-left";
   change.change=-1;
   Y06EastApproachEdit.Reconnect(change,outer,innerEnd,evidence);
   outer.next=new[]{outerChangeWindow.id,change.id};
   inner.next=new[]{innerEnd.id};
   added.AddRange(new[]{outer,outerChangeWindow,inner,change});
  }
  var left=d.lanes.Single(l=>l.id==Join(root,-1)&&!l.disabled);
  var right=d.lanes.Single(l=>l.id==Join(root,1)&&!l.disabled);
  innerEnd.next=new[]{left.id};
  outerEnd.next=new[]{right.id};
  Y06EastApproachEdit.Reconnect(left,innerEnd,d.lanes.Single(l=>l.id==left.next.Single()),evidence);
  Y06EastApproachEdit.Reconnect(right,outerEnd,d.lanes.Single(l=>l.id==right.next.Single()),evidence);
  RefreshSurface(d,left);
  RefreshSurface(d,right);
  d.lanes=d.lanes.Concat(new[]{outerEnd,innerEnd}).Concat(added).ToArray();
  var sources=head.sourceId.Split(',').Select(id=>"road:"+id).ToHashSet();
  foreach(var road in d.scene.objects.Where(o=>sources.Contains(o.id)))road.lanes=3;
 }

 static void Validate(ProjectMapPackage before,RoadMapDocument document){
  var errors=RoadMapValidator.Validate(before.baseline,document).Where(issue=>issue.error).ToArray();
  Check(errors.Length==0,string.Join(" / ",errors.Take(8).Select(issue=>issue.ToString())));
  var map=RoadMapComposer.Compose(before.baseline,document);
  EungyeConnectivityRepair.Apply(map,before.baseline);
  var graph=new HdRoadGraph(map);
  var edges=graph.edges.ToDictionary(e=>e.id);
  var paint=new RoadPaintGeometry(map);
  var masks=LaneDirectionPaint.Layout(map).ToLookup(a=>graph.edges[a.edgeIndex].id);
  foreach(var (root,prefix) in new[]{(East,EastPrefix),(West,WestPrefix)}){
   var original=Path(before.document.lanes.Single(l=>l.id==root));
   foreach(float at in new[]{17f,30f,original.length-15f}){
    var section=paint.Lanes(edges[root],original.Position(at),original.Tangent(at));
    Check(section.Count(l=>l.direction==1)==2&&section.Count(l=>l.direction==-1)==1,"2+1 near "+original.Position(at));
   }
   foreach(var (id,turn,mask) in new[]{(prefix+"left:part:1",-1,LaneDirectionPaint.Left),(Outer(root,"approach"),1,LaneDirectionPaint.Right)}){
    var edge=edges[id];
    var connector=graph.edges[edge.next.Single()];
    Check(connector.id==Join(root,turn)&&connector.turn==turn&&!connector.disabled,"wrong exit from "+edge.id);
    Check(masks[edge.id].Any(a=>a.turns==mask),"missing arrow on "+edge.id);
    Y07MapTests.Footprint(map,connector);
   }
  }
  foreach(string id in new[]{EastPrefix+"left",EastPrefix+"left:part:1","change:"+EastPrefix+"prepare-left"})
   Y07MapTests.Footprint(map,edges[id]);
  // Check against the local heading around the tight west bend. The longer
  // straight-road fixture treats normal changes of heading as reversals.
  var westInside=new AssistedRoadNetwork.Path(edges[WestPrefix+"left:part:1"].points.Select(p=>p.V()).ToArray());
  for(float at=0f;at<westInside.length-.01f;at+=1f){
   float end=Mathf.Min(at+1.5f,westInside.length);
   var sample=new HdEdge{points=Slice(westInside,at,end).Select(p=>new MapPoint{x=p.x,z=p.z}).ToArray()};
   Check(EungyeConnectivityRepair.FootprintClear(map,sample,out string reason),"west curve clearance at "+at.ToString("F1")+": "+reason);
  }
  var service=document.scene.objects.Single(o=>o.id==Restaurant);
  var originalService=before.document.scene.objects.Single(o=>o.id==Restaurant);
  var originalLane=before.document.lanes.Single(l=>l.id==East);
  var reboundLane=document.lanes.Single(l=>l.id==service.laneId);
  Check(service.laneId==Outer(East,"service")&&service.serviceX==originalService.serviceX&&service.serviceZ==originalService.serviceZ
   &&Vector3.Distance(Path(reboundLane).Position(service.laneAt),Path(originalLane).Position(originalService.laneAt))<.03f,"restaurant curb approach changed");
  var index=Array.IndexOf(graph.edges,edges[service.laneId]);
  foreach(string id in new[]{Join(East,-1),Join(East,1)}){
   int exit=Array.IndexOf(graph.edges,edges[id]);
   var route=graph.Route(index,service.laneAt,exit,1f);
   Check(route!=null&&route.path.length<110f&&route.edges.Any(n=>n==exit),"restaurant cannot leave directly via "+id);
  }
  var catalog=PassengerStopCatalog.For(map);
  Check(catalog.Stops.Length==39,"39 passenger stops: "+string.Join("; ",catalog.Rejected));
  int routes=0;
  foreach(var from in catalog.Stops)foreach(var to in catalog.Stops)if(from!=to){
   Check(graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"disconnected stop "+from.name+" → "+to.name);
   routes++;
  }
  Check(routes==1482,"39 stops / 1482 routes");
  Debug.Log("PARKING_LANES_VALIDATED P20 west/south 2/1, P26 east/south 2/1, both approaches L/R, restaurant both exits, 39 stops, 1482 routes");
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  var document=Preview(before);
  document.revision=33;
  document.savedAt=DateTime.UtcNow.ToString("o");
  document.projectRevision="";
  Validate(before,document);
  var candidate=ProjectMapPackage.Create(before.baseline,document);
  ProjectMapStore.Save(root,candidate,token);
  var reloaded=ProjectMapStore.Load(root);
  Check(reloaded.contentHash==candidate.contentHash&&reloaded.baselineHash==before.baselineHash,"saved identity");
  Validate(before,reloaded.document);
  ProjectMapBuild.Prepare();
  Debug.Log("P26_APPLIED revision=33 contentHash="+reloaded.contentHash);
 }
}
