using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class ApprovedEungyeRoads {
 static string SlipId(string corner)=>corner switch{
  "NE"=>"A2218G611469:0",
  "NW"=>"join:local:7918:6167:7847:3996:1",
  "SE"=>"A222BC004189:0",
  "SW"=>"join:local:6672:3876:7695:3629:1",
  _=>throw new ArgumentOutOfRangeException(nameof(corner))
 };
 static void RightTurns(RoadMapDocument doc){
  var prior=doc.scene.objects.Single(o=>o.id=="reviewed:2026-10-30:J7973-SE-island");
  prior.enabled=false;
  var oldNorthwest=Lane(doc,"A2218G611466:0");
  Require(!doc.lanes.Any(l=>!l.disabled&&l.next.Contains(oldNorthwest.id)),"obsolete northwest turn has an active entry");
  oldNorthwest.disabled=true;
  RetireAlternate(doc,"local:8039:2741:8076:3496:part:2","join:local:8039:2741:8076:3496:0","join:reviewed:J7917:entry:15");
  Slip(doc,"NE","local:8131:3977:8203:6159:part:1",21,V(818.9f,408),.49f,0,1,-1);
  Slip(doc,"NW","local:7779:3974:6755:4221",29,V(768,407),.49f,12,0,-1);
  Slip(doc,"SE","A222BC004188:0",16,V(832,352.3f),.43f,0,0,1);
  Slip(doc,"SW","editor:segment:bG9jYWw6Nzg0ODozNTA3Ojc4MTI6Mjc1MjpwYXJ0OjE:4fc75aa8708a4f7989f72a62d2e0dcb0",
   14,V(778.4f,335),.52f,9,1,1);
  // The second synthetic right turn crossed directly to the middle lane.
  // A single slip feeds the outer lane on exit; ordinary lane changes follow.
  ExtendSouthboundPocket(doc);
 }
 static void RetireAlternate(RoadMapDocument doc,params string[] ids){
  foreach(var id in ids)Lane(doc,id).disabled=true;
  foreach(var lane in doc.lanes.Where(l=>!l.disabled&&l.next.Any(ids.Contains)))
   lane.next=lane.next.Where(id=>!ids.Contains(id)).ToArray();
 }
 static void Slip(RoadMapDocument doc,string corner,string receiverId,float receiveAt,Vector3 runout,float crossFraction,float entryLead,int axis,int sign){
  const float width=3.5f;
  string id="J7973:"+corner+":";
  var slip=Lane(doc,SlipId(corner));
  var incoming=doc.lanes.Single(l=>!l.disabled&&l.next.Contains(slip.id));
  var entry=PathOf(incoming);
  if(entryLead>0){
   // The diverging right-turn path begins on the approach side of the island.
   // A through lane can still follow the unmodified terminal segment.
   var tail=Split(doc,incoming,entry.length-entryLead);
   if(tail.next.Length==1){
    Require(!doc.facilities.Any(f=>f.laneId==tail.id)&&
     !doc.scene.objects.Any(o=>o.IsService&&o.laneId==tail.id),"discarded stub has an authored binding");
    doc.lanes=doc.lanes.Where(l=>l!=tail).ToArray();
    incoming.next=new[]{slip.id};
   }else{
    tail.next=tail.next.Where(n=>n!=slip.id).ToArray();
    incoming.next=incoming.next.Append(slip.id).ToArray();
   }
   entry=PathOf(incoming);
  }
  var receiver=Lane(doc,receiverId);
  var receivingPath=PathOf(receiver);
  var target=Split(doc,receiver,receiveAt);
  var start=entry.points.Last();
  var startDir=entry.Tangent(entry.length);
  var endDir=receivingPath.Tangent(receiveAt);
  float handle=Vector3.Distance(start,runout)*.53f;
  var turn=Cubic(start,start+startDir*handle,runout-endDir*handle,runout,28).ToArray();
  var path=new AssistedRoadNetwork.Path(turn);
  slip.fromNode=incoming.toNode;
  slip.toNode=Prefix+id+"merge-start";slip.width=width;slip.next=Array.Empty<string>();
  Set(slip,turn);
  var finish=target.points[0].V();
  float mergeHandle=Vector3.Distance(runout,finish)*.36f;
  var merge=Branch(doc,id+"merge",slip,target,
   Cubic(runout,runout+endDir*mergeHandle,finish-endDir*mergeHandle,finish,18).ToArray(),-1);
  Surface(doc,id+"turn",turn,width);
  Surface(doc,id+"merge",merge.points.Select(p=>p.V()),width);
  ReviewedJunctionMap.TrimSlipSidewalk(doc,slip,width*.5f+.26f);
  ReviewedJunctionMap.TrimSlipSidewalk(doc,merge,width*.5f+.26f);

  var refuge=Refuge(doc,path,crossFraction,width);
  Sidewalk(doc,id+"island",refuge.island);
  Sidewalk(doc,id+"sidewalk",refuge.sidewalk);
  Crossing(doc,id+"crosswalk",refuge.curb,refuge.outer,2.3f);
  var support=Feature(id+"signal-pole","lanePresentation",refuge.pole,refuge.forward,1,1);
  support.type=ReviewedCorridorPaint.SignalPoleType;support.junctionId=Junction;support.axis=axis;support.sign=sign;
  Add(doc,support);

  // Delineate the short parallel outlet and carry its dashed boundary
  // toward the point where it joins the receiving carriageway.
  var right=TrafficSystem.Right(endDir);
  var separator=runout-right*width*.5f;
  Dashed(doc,id+"outlet",separator,separator+endDir*Mathf.Min(11,Vector3.Distance(finish,runout)*.63f));
 }

 static (Vector3[] island,Vector3[] sidewalk,Vector3 curb,Vector3 outer,Vector3 pole,Vector3 forward)
 Refuge(RoadMapDocument doc,AssistedRoadNetwork.Path path,float preferred,float width){
  var near=doc.lanes.Where(l=>!l.disabled&&l.points.Any(p=>path.points.Any(q=>(p.V()-q).sqrMagnitude<18*18))).ToArray();
  foreach(float offset in new[]{0f,-2f,2f,-4f,4f,-6f,6f,-8f,8f,-10f,10f}){
   float at=path.length*preferred+offset;
   if(at<4||at>path.length-4)continue;
   var centre=path.Position(at);var forward=path.Tangent(at);var side=TrafficSystem.Right(forward);
   foreach(float margin in new[]{.3f,.55f,.8f})foreach(float depth in new[]{2.8f,2.15f,1.7f}){
    float edge=width*.5f+margin;
    var curb=centre-side*edge;var outer=centre+side*edge;
    var strip=Enumerable.Range(-3,7).Select(i=>{
     float station=at+i*.8f;
     return path.Position(station)-TrafficSystem.Right(path.Tangent(station))*edge;
    });
    var island=strip.Concat(new[]{curb-side*depth+forward*1.7f,curb-side*depth-forward*1.7f}).ToArray();
    if(!Fits(island,near))continue;
    foreach(int reach in new[]{7,5,3,2}){
     var edgeStations=Enumerable.Range(-reach,reach*2+1)
      .Select(i=>Mathf.Clamp(at+i,0,path.length)).Distinct().ToArray();
     var outside=edgeStations.Select(station=>path.Position(station)+TrafficSystem.Right(path.Tangent(station))*edge);
     var far=edgeStations.Reverse().Select(station=>path.Position(station)+TrafficSystem.Right(path.Tangent(station))*(edge+2.2f));
     var walk=outside.Concat(far).ToArray();
     if(Fits(walk,near))return (island,walk,curb,outer,curb-side*1.15f+forward*.8f,forward);
    }
   }
  }
  throw new InvalidOperationException("No clear island and sidewalk fit near "+path.Position(path.length*preferred));
 }
 static bool Fits(IEnumerable<Vector3> polygon,IEnumerable<RoadMapLane> lanes){
  var ring=polygon.Select(p=>new RoadGeometry.Point(p.x,p.z)).ToArray();
  float minX=ring.Min(p=>p.x),maxX=ring.Max(p=>p.x),minZ=ring.Min(p=>p.z),maxZ=ring.Max(p=>p.z);
  foreach(var lane in lanes)for(int i=1;i<lane.points.Length;i++){
   var a=lane.points[i-1];var b=lane.points[i];float half=lane.width*.5f+.02f;
   if(Mathf.Max(a.x,b.x)+half<minX||Mathf.Min(a.x,b.x)-half>maxX||
      Mathf.Max(a.z,b.z)+half<minZ||Mathf.Min(a.z,b.z)-half>maxZ)continue;
   if(RoadGeometry.SweptLaneIntersects(new RoadGeometry.Point(a.x,a.z),new RoadGeometry.Point(b.x,b.z),ring,half))return false;
  }
  return true;
 }

 static void ExtendSouthboundPocket(RoadMapDocument doc){
  var pocket=Lane(doc,"reviewed:J7973:southbound-left");
  var lead=doc.lanes.Single(l=>l.next.Contains(pocket.id));
  var previous=doc.lanes.Single(l=>l.next.Contains(lead.id)&&l.change==0);
  var path=PathOf(lead);
  var old=PathOf(pocket);var original=old.points.Last();
  var points=Enumerable.Range(0,51).Select(i=>{
   float at=path.length*i/50;
   return path.Position(at)-TrafficSystem.Right(path.Tangent(at))*3.5f*Mathf.SmoothStep(0,1,at/22);
  }).Concat(Enumerable.Range(1,57).Select(i=>{
   float at=old.length*i/56;
   return old.Position(at)-TrafficSystem.Right(old.Tangent(at))*(3.5f*(1-Mathf.SmoothStep(0,1,at/24)));
  })).ToArray();
  points[points.Length-1]=original;
  lead.next=lead.next.Where(id=>id!=pocket.id).ToArray();
  pocket.fromNode=previous.toNode;previous.next=previous.next.Append(pocket.id).ToArray();
  Set(pocket,points);
 }
}
