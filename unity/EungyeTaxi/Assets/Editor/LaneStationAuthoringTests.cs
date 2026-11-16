using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class LaneStationAuthoringTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception("LANE_STATION: "+message);}
 static void Near(float actual,float expected,string message){Check(Mathf.Abs(actual-expected)<.0001f,message+" actual="+actual);}
 static RoadMapLane Lane(string id,bool reverse=false){
  var lane=new RoadMapLane{id=id,sourceId="arbitrary-source",fromNode=id+":start",toNode=id+":end",points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=3,z=0},new MapPoint{x=3,z=4}},heights=new[]{0f,4f,4f},next=new[]{"exit"}};
  if(reverse){Array.Reverse(lane.points);Array.Reverse(lane.heights);}return lane;
 }
 public static void Run(){
  var type=typeof(LaneStationAuthoringTests).Assembly.GetType("LaneStationAuthoring");Check(type!=null,"exact station authoring helper exists");
  var sample=type.GetMethod("SampleAt");var split=type.GetMethod("SplitLineageAt");Check(sample!=null&&split!=null,"sampling and lineage split entry points exist");
  var lane=Lane("forward");var original=JsonUtility.ToJson(lane);
  Func<RoadMapLane,float,Vector3> at=(l,s)=>(Vector3)sample.Invoke(null,new object[]{l,s});
  var halfway=at(lane,2.5f);Near(halfway.x,1.5f,"exact inclined edge interpolation x");Near(halfway.y,2,"height interpolation");
  var aroundBend=at(lane,7);Near(aroundBend.x,3,"bend x");Near(aroundBend.z,2,"bend station");
  var reverse=at(Lane("reverse",true),2);Near(reverse.z,2,"reverse starts at its own tip");Near(reverse.y,4,"reverse height");
  Check(JsonUtility.ToJson(lane)==original,"sampling is read-only");
  var d=new RoadMapDocument{lanes=new[]{lane,Lane("other")},scene=new ProjectMapScene{objects=new[]{new ProjectMapObject{id="service",kind="parking",laneId=lane.id,laneAt=7,serviceX=3,serviceZ=2}}}};
  Func<string,float,string> cut=(id,s)=>(string)split.Invoke(null,new object[]{d,id,s});
  Check(cut(lane.id,0)==lane.id&&cut(lane.id,9)==lane.id&&d.lanes.Length==2,"zero/tip no-op never creates zero-length segments");
  string tail=cut(lane.id,2.5f);var tailLane=d.lanes.Single(l=>l.id==tail);
  Check(d.lanes.Single(l=>l.id=="forward")==lane,"original ID and object retained");Near(tailLane.sourceOffset,2.5f,"exact source offset");
  Near(lane.points.Last().x,1.5f,"head exact boundary");Near(tailLane.heights[0],2,"tail interpolated height");
  Check(tailLane.points.Length==3&&tailLane.points[1].x==3&&tailLane.points[1].z==0&&tailLane.points[2].z==4,"remaining vertices and bend order retained");
  Check(lane.next.SequenceEqual(new[]{tail})&&tailLane.next.SequenceEqual(new[]{"exit"}),"connectivity retained");
  Check(RoadMapAuthoringOperations.Origin(tailLane)==lane.id,"tail lineage retained");
  Check(d.scene.objects[0].laneId==tail,"service remapped onto tail");Near(d.scene.objects[0].laneAt,4.5f,"service source station preserved");
  int count=d.lanes.Length;Check(cut(tail,2.5f)==tail&&d.lanes.Length==count,"existing split boundary reuses downstream ID");
  string last=cut(tail,7);Check(d.lanes.Any(l=>l.id==tail),"splitting an existing lineage preserves its ID");
  Near(d.lanes.Single(l=>l.id==last).sourceOffset,7,"already split lineage uses global station");
  Check(d.scene.objects[0].laneId==last,"exact-boundary service belongs to downstream segment");Near(d.scene.objects[0].laneAt,0,"boundary service offset");
  Near(d.scene.objects[0].serviceX,3,"service world x unchanged");Near(d.scene.objects[0].serviceZ,2,"service world z unchanged");
  foreach(var l in d.lanes.Where(l=>l.id!="other"))Check(l.points.Length>=2&&l.heights.Length==l.points.Length,"no empty geometry and aligned heights");
  Debug.Log("LANE_STATION_AUTHORING_TEST_OK exact curved/inclined samples, reversed baseline, existing lineage IDs, service remap");
 }
}
