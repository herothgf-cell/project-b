using System.Collections.Generic;
using UnityEngine;

// Synthetic paint follows the same compiled lanes used by navigation and AI.
// Imported HD markings remain in HdRouteTrial, with their original colours.
public sealed class RoadPaintGeometry {
 readonly MapData map;
 sealed class LanePath {public AssistedRoadNetwork.Path path;public string id;public int number;}
 readonly Dictionary<string,List<LanePath>> roads=new Dictionary<string,List<LanePath>>();
 readonly Dictionary<string,List<LanePath>> corridors=new Dictionary<string,List<LanePath>>();
 readonly Dictionary<string,float> widths=new Dictionary<string,float>();
 readonly HashSet<string> polished=new HashSet<string>();
 public RoadPaintGeometry(MapData map){
  this.map=map;
  if(!map.hybrid||!map.HasHd)return;
  foreach(var edge in map.hdEdges){
   if(edge.disabled||edge.status!="provisional"||edge.turn!=0||edge.change!=0||edge.id.StartsWith("join:")||edge.id.StartsWith("fuel:")||edge.sourceId.StartsWith("fuel:"))continue;
   var points=new Vector3[edge.points.Length];for(int i=0;i<points.Length;i++)points[i]=edge.points[i].V();var lane=new LanePath{path=new AssistedRoadNetwork.Path(points),id=edge.id,number=edge.number};
   if(CorridorPolish.Includes(map,edge.id))polished.Add(edge.sourceId);
   List<LanePath> group;if(!corridors.TryGetValue(edge.sourceId,out group)){group=new List<LanePath>();corridors.Add(edge.sourceId,group);}group.Add(lane);
   foreach(var source in edge.sourceId.Split(',')){List<LanePath> list;if(!roads.TryGetValue(source,out list)){list=new List<LanePath>();roads.Add(source,list);}list.Add(lane);}
  }
  foreach(var road in map.roads)widths[road.id]=road.width;
 }
 static string Family(string id){int part=id.IndexOf(":part:");return part<0?id:id.Substring(0,part);}
 static List<RoadGeometry.Lane> Sample(List<LanePath> list,Vector3 point,Vector3 direction,float width){
  var result=new List<RoadGeometry.Lane>();
  foreach(var lane in list){float error;float at=lane.path.Project(point,out error);if(error>width+1)continue;var p=lane.path.Position(at);var d=lane.path.Tangent(at);float along=Vector3.Dot(point-p,d),alignment=Vector3.Dot(d,direction);
   // Offset curves end at slightly different stations around a bend. Allow
   // one paint sample at the end; junction openings still clip the paint.
   if(Mathf.Abs(along)>1f||Mathf.Abs(alignment)<.85f)continue;
   if(lane.id.StartsWith("reviewed:")&&along<-.01f)continue;
   float offset=Vector3.Dot(p-point,TrafficSystem.Right(direction));int sign=alignment>0?1:-1;
   if(!result.Exists(x=>x.direction==sign&&Mathf.Abs(x.offset-offset)<.5f))result.Add(new RoadGeometry.Lane(offset,sign));
  }result.Sort((a,b)=>a.offset.CompareTo(b.offset));return result;
 }
 public List<RoadGeometry.Lane> Lanes(MapRoad road,Vector3 point,Vector3 direction){
  var result=new List<RoadGeometry.Lane>();List<LanePath> list;
  if(roads.TryGetValue(road.id??"",out list)){
   return Sample(list,point,direction,road.width);
  }else if(roads.Count==0){
   int count=Mathf.Max(1,road.DisplayLanes/2);foreach(int sign in new[]{-1,1}){if(road.oneWay!=0&&sign!=road.oneWay)continue;
    for(int i=0;i<count;i++)result.Add(new RoadGeometry.Lane(road.DisplayLanes==1?0:sign*(i+.5f)*road.width/(2*count),sign));
   }
  }
  result.Sort((a,b)=>a.offset.CompareTo(b.offset));return result;
 }
 public List<RoadGeometry.Lane> Lanes(HdEdge edge,Vector3 point,Vector3 direction){
  List<LanePath> group;return corridors.TryGetValue(edge.sourceId,out group)?Sample(group,point,direction,float.MaxValue):new List<RoadGeometry.Lane>();
 }
 public static void Paint(MapData map,List<TrafficJunction> junctions,System.Action<string,Vector3,Vector3,float,float,bool> mark){
  var layout=new RoadPaintGeometry(map);var openings=RoadJunctionGeometry.Build(map);
  if(layout.corridors.Count>0){layout.PaintCompiled(junctions,openings,mark);return;}
  foreach(var road in map.roads){if(road.suppressPaint||road.id!=null&&road.id.StartsWith("fuel:"))continue;
   float distance=0;
   for(int i=1;i<road.points.Length;i++){
    var a=road.points[i-1].V();var b=road.points[i].V();var dir=(b-a).normalized;float length=Vector3.Distance(a,b);if(length<.001f)continue;
    for(float at=.5f;at<length;at+=1){
     var p=a+dir*at;float piece=Mathf.Min(.96f,2*(length-at));
     var lanes=layout.Lanes(road,p,dir);if(lanes.Count==0)continue;
     foreach(var boundary in RoadGeometry.Boundaries(lanes)){
      if(boundary.dashed&&((distance+at)%6)>=3)continue;
      var q=p+TrafficSystem.Right(dir)*boundary.offset;
      if(InOpening(q,junctions,openings,piece*.5f))continue;
      mark(boundary.yellow?"Centre line":"Lane divider",q,dir,boundary.yellow?.14f:.13f,piece,boundary.yellow);
     }
     foreach(int side in new[]{-1,1}){var q=p+TrafficSystem.Right(dir)*side*(road.width*.5f-.3f);if(!InOpening(q,junctions,openings,piece*.5f))mark("Road edge",q,dir,.12f,piece,false);}
    }distance+=length;
   }
  }
 }
 void PaintCompiled(List<TrafficJunction> junctions,List<RoadJunctionGeometry.Opening> openings,System.Action<string,Vector3,Vector3,float,float,bool> mark){
  foreach(var corridor in corridors){
   var first=corridor.Value.Find(x=>x.number==1);if(first==null)continue;string family=Family(first.id);float width=float.MaxValue;
   foreach(var id in corridor.Key.Split(',')){float value;if(widths.TryGetValue(id,out value))width=Mathf.Min(width,value);}if(width==float.MaxValue)continue;
   float phase=0;
   foreach(var reference in corridor.Value){if(Family(reference.id)!=family)continue;
    var path=reference.path;
    for(float at=.5f;at<path.length;at+=1){
     // The exact compiled corridor limits membership. A source surface width
     // must never discard a real outer lane of that corridor.
     var p=path.Position(at);var dir=path.Tangent(at);float piece=Mathf.Min(.96f,2*(path.length-at));var lanes=Sample(corridor.Value,p,dir,float.MaxValue);
     if(lanes.Count==0)continue;
     foreach(var line in RoadGeometry.Boundaries(lanes)){
      if(line.dashed&&(phase+at)%6>=3)continue;var q=p+TrafficSystem.Right(dir)*line.offset;if(line.yellow)q=ReviewedLaneLayout.Centreline(map,corridor.Key,q);
      if(!InOpening(q,junctions,openings,piece*.5f))mark(line.yellow?"Centre line":"Lane divider",q,dir,line.yellow?.14f:.13f,piece,line.yellow);
     }
     float laneWidth=lanes.Count>1?float.MaxValue:width;
     for(int i=1;i<lanes.Count;i++)laneWidth=Mathf.Min(laneWidth,lanes[i].offset-lanes[i-1].offset);
     float outerHalf=polished.Contains(corridor.Key)?Mathf.Min(laneWidth*.5f,CorridorPolish.HalfLane):laneWidth*.5f;
     foreach(float edge in new[]{lanes[0].offset-outerHalf+.15f,lanes[lanes.Count-1].offset+outerHalf-.15f}){
      var q=p+TrafficSystem.Right(dir)*edge;if(!InOpening(q,junctions,openings,piece*.5f))mark("Road edge",q,dir,.12f,piece,false);
     }
    }phase+=path.length;
   }
  }
 }
 static bool InOpening(Vector3 point,List<TrafficJunction> junctions,List<RoadJunctionGeometry.Opening> openings,float extra){
  foreach(var j in junctions)if(Vector3.Distance(point,j.center)<18+extra)return true;
  foreach(var j in openings)if(Vector3.Distance(point,j.center)<j.radius+extra)return true;
  return false;
 }
}
