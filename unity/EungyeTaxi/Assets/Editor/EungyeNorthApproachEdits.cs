using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EungyeNorthApproachEdits {
 const string Junction="J8066_6364";
 const string Prefix="reviewed:J8066-N:";
 const string View="https://map.naver.com/p/entry/coordinates/3zao1h,2AGJhI?c=15,0,0,0,adh&p=VzXWr0yZ-kjmidpHNnS74w,0,-10,80,Float";

 static RoadMapLane Lane(RoadMapDocument doc,string id){return doc.lanes.Single(l=>l.id==id);}
 static RoadMapLane Segment(RoadMapDocument doc,string suffix){return doc.lanes.Single(l=>l.id.EndsWith(suffix,StringComparison.Ordinal));}

 static RoadMapLane AddThroughLane(RoadMapDocument doc,string role,int number,float offset,RoadMapLane[] path,string junctionConnector){
  var entry=path[0];
  var originalEnd=path[path.Length-1];
  var direction=(originalEnd.points.Last().V()-originalEnd.points[0].V()).normalized;
  var right=TrafficSystem.Right(direction);
  var points=new List<MapPoint>();
  var heights=new List<float>();
  float station=0;
  foreach(var segment in path){
   for(int i=points.Count==0?0:1;i<segment.points.Length;i++){
    if(i>0)station+=Vector3.Distance(segment.points[i-1].V(),segment.points[i].V());
    var p=segment.points[i].V()+right*offset*Mathf.SmoothStep(0,1,station/22f);
    points.Add(new MapPoint{x=p.x,z=p.z});
    heights.Add(segment.heights[i]);
   }
  }
  var added=new RoadMapLane{id=Prefix+role,sourceId=entry.sourceId,roadName=entry.roadName,
   ruleId=Junction,status="provisional",number=number,width=2.85f,
   fromNode=entry.fromNode,toNode=Prefix+role+":end",points=points.ToArray(),heights=heights.ToArray(),
   next=new[]{junctionConnector},evidence=new RoadMapEvidence{state="observed",url=View,month="2026-05",
    checkedAt="2026-11-12",appearanceConfirmed=true,note="북행 5차로 중 직진 전용. 분기 시작과 차로 폭은 게임 치수."}};
  var connector=Lane(doc,junctionConnector);
  var shift=added.points.Last().V()-connector.points[0].V();
  var connectorPath=new AssistedRoadNetwork.Path(connector.points.Select(p=>p.V()).ToArray());
  float at=0;
  for(int i=0;i<connector.points.Length;i++){
   if(i>0)at+=Vector3.Distance(connectorPath.points[i-1],connectorPath.points[i]);
   var p=connectorPath.points[i]+shift*(1-Mathf.SmoothStep(0,1,at/connectorPath.length));
   connector.points[i]=new MapPoint{x=p.x,z=p.z};
  }
  connector.fromNode=added.toNode;
  originalEnd.next=originalEnd.next.Where(n=>n!=connector.id).ToArray();
  foreach(var previous in doc.lanes.Where(l=>l.next.Contains(entry.id)&&l.toNode==entry.fromNode))
   previous.next=previous.next.Concat(new[]{added.id}).ToArray();
  return added;
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var package=ProjectMapStore.Load(root);
  var doc=package.document;
  if(doc.lanes.Any(l=>l.id.StartsWith(Prefix,StringComparison.Ordinal)))throw new InvalidOperationException("J8066 northbound approach already edited.");
  var inner=AddThroughLane(doc,"inner-through",4,2.85f,new[]{
   Segment(doc,"41498ec46fc345ac8d5485b89bb28430"),
   Segment(doc,"b2f9bc24706e4e44abf167cec1db3a0e"),
   Segment(doc,"60c8ed8bd47a417f919b82406b8f64c6")},"join:local:8017:3985:8089:6162:0");
  var outer=AddThroughLane(doc,"outer-through",5,-2.85f,new[]{
   Segment(doc,"c6993f3176eb4531a1f571f8efb69a3c"),
   Segment(doc,"6b782ce1cd034cccbe353b2edaebf042")},"join:local:8131:3977:8203:6159:0");
  doc.lanes=doc.lanes.Concat(new[]{inner,outer}).ToArray();

  // The existing portal leads to a left curve from the northeast approach onto Suin-ro eastbound.
  // The native path and endpoints are unchanged.
  var left=Lane(doc,"A222BC004192:0");
  left.turn=-1;
  left.evidence.note+=" J1927_5181 북동쪽 진입→수인로 동향 기존 곡선을 좌회전으로 분류.";
  doc.revision++;
  doc.savedAt=DateTime.UtcNow.ToString("o");
  doc.projectRevision="";
  package.contentHash=ProjectMapPackage.Digest(JsonUtility.ToJson(doc));
  ProjectMapStore.Save(root,package,token);
  ProjectMapBuild.Prepare();
 }
}