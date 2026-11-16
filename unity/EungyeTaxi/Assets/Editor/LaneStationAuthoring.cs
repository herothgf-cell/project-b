using System;
using System.Linq;
using UnityEngine;

// Editor-only geometry utility. It never loads/saves a package or changes lane roles.
// Stations are 3D arc lengths, matching SplitLane/sourceOffset. Each directed
// baseline has its own station zero: never reuse the opposite direction's station.
public static class LaneStationAuthoring {
 const float Epsilon=.0001f;
 static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
 static Vector3[] Positions(RoadMapLane lane){
  if(lane==null||lane.points==null||lane.points.Length<2)throw new ArgumentException("A lane needs at least two points");
  if(lane.heights!=null&&lane.heights.Length!=0&&lane.heights.Length!=lane.points.Length)throw new ArgumentException("Lane heights must align with points");
  var result=new Vector3[lane.points.Length];
  for(int i=0;i<result.Length;i++){
   var point=lane.points[i];float height=lane.heights!=null&&lane.heights.Length>0?lane.heights[i]:0;
   if(point==null||!Finite(point.x)||!Finite(point.z)||!Finite(height))throw new ArgumentException("Lane geometry must be finite");
   result[i]=new Vector3(point.x,height,point.z);
  }return result;
 }
 static float Length(Vector3[] points){float result=0;for(int i=1;i<points.Length;i++)result+=Vector3.Distance(points[i-1],points[i]);return result;}
 public static Vector3 SampleAt(RoadMapLane lane,float localStation){
  var points=Positions(lane);float length=Length(points);
  if(!Finite(localStation)||localStation<0||localStation>length)throw new ArgumentOutOfRangeException("localStation");
  return new AssistedRoadNetwork.Path(points).Position(localStation);
 }
 // Mutates only the supplied document, normally a caller-owned Preview copy.
 // laneId may be the original ID or any existing segment in its lineage.
 // Returns the downstream segment at the split; at the final tip returns the
 // final existing segment. Existing boundaries (within 0.1mm) are no-ops.
 // Existing IDs survive; a genuinely new tail uses SplitLane's normal ID scheme.
 public static string SplitLineageAt(RoadMapDocument document,string laneId,float sourceStation){
  if(document==null||document.lanes==null)throw new ArgumentNullException("document");
  if(!Finite(sourceStation))throw new ArgumentOutOfRangeException("sourceStation");
  var selected=document.lanes.SingleOrDefault(l=>l.id==laneId&&!l.disabled);
  if(selected==null)throw new ArgumentException("Active lane not found: "+laneId);
  string origin=RoadMapAuthoringOperations.Origin(selected);
  var lineage=document.lanes.Where(l=>!l.disabled&&RoadMapAuthoringOperations.Origin(l)==origin).OrderByDescending(l=>l.sourceOffset).ToArray();
  RoadMapLane owner=null;Vector3[] positions=null;float local=0;
  foreach(var lane in lineage){
   var values=Positions(lane);float length=Length(values);
   if(sourceStation<lane.sourceOffset-Epsilon||sourceStation>lane.sourceOffset+length+Epsilon)continue;
   if(length<=Epsilon)throw new ArgumentException("Cannot partition a zero-length lane");
   local=sourceStation-lane.sourceOffset;
   if(local<=Epsilon||local>=length-Epsilon)return lane.id;
   owner=lane;positions=values;break;
  }
  if(owner==null)throw new ArgumentOutOfRangeException("sourceStation","Station is outside the active lineage");
  float walked=0;
  for(int i=1;i<positions.Length;i++){
   float edgeLength=Vector3.Distance(positions[i-1],positions[i]);float end=walked+edgeLength;
   if(Mathf.Abs(local-end)<=Epsilon&&i<positions.Length-1)return RoadMapAuthoringOperations.SplitLane(document,owner.id,i);
   if(local<end&&edgeLength>0){
    var point=Vector3.Lerp(positions[i-1],positions[i],(local-walked)/edgeLength);
    var points=owner.points.ToList();points.Insert(i,new MapPoint{x=point.x,z=point.z,elevation=point.y});
    var heights=positions.Select(p=>p.y).ToList();heights.Insert(i,point.y);
    owner.points=points.ToArray();owner.heights=heights.ToArray();
    // SplitLane retains old IDs/connectivity and remaps facilities, signal
    // references and scene service distances through the existing lineage code.
    return RoadMapAuthoringOperations.SplitLane(document,owner.id,i);
   }walked=end;
  }
  throw new InvalidOperationException("No containing edge for station");
 }
}
