using System;
using System.Collections.Generic;
using UnityEngine;

// Presentation-only classification of existing native paths near one selected
// junction. Source turn values, graph connections and U-turn conditions stay intact.
public static class ReviewedLaneMovementTrace {
 const int MaximumHops=32;
 const float JunctionRadius=90,MaximumDistance=240,ConnectionTolerance=.25f;
 static bool Native(HdEdge e){return e!=null&&!e.disabled&&e.status=="ngii"&&e.change==0&&e.points!=null&&e.points.Length>=2;}
 static Vector3 Start(HdEdge e){return e.points[0].V();}
 static Vector3 End(HdEdge e){return e.points[e.points.Length-1].V();}
 static Vector3 StartHeading(HdEdge e){for(int i=1;i<e.points.Length;i++){var d=e.points[i].V()-Start(e);if(d.sqrMagnitude>.0001f)return d.normalized;}return Vector3.zero;}
 static Vector3 EndHeading(HdEdge e){for(int i=e.points.Length-2;i>=0;i--){var d=End(e)-e.points[i].V();if(d.sqrMagnitude>.0001f)return d.normalized;}return Vector3.zero;}
 static float Length(HdEdge e){float length=0;for(int i=1;i<e.points.Length;i++)length+=Vector3.Distance(e.points[i-1].V(),e.points[i].V());return length;}
 static bool Connected(HdEdge from,HdEdge to){return to!=null&&to.points!=null&&to.points.Length>=2&&(End(from)-Start(to)).sqrMagnitude<=ConnectionTolerance*ConnectionTolerance;}
 static bool Near(HdEdge e,HybridJunctionRule junction){foreach(var p in e.points)if((p.V()-junction.V()).sqrMagnitude>JunctionRadius*JunctionRadius)return false;return true;}
 static HdEdge Target(MapData map,int index){return index>=0&&index<map.hdEdges.Length?map.hdEdges[index]:null;}
 static bool Exit(HdEdge e){return e!=null&&!e.disabled&&e.change==0&&e.points!=null&&e.points.Length>=2&&(e.status=="ngii"||e.status=="provisional-portal");}
 public static int Mask(MapData map,HdEdge incoming,HybridJunctionRule junction){
  if(map==null||map.hdEdges==null||junction==null||!Native(incoming)||incoming.turn!=0||(End(incoming)-junction.V()).sqrMagnitude>80*80)return 0;
  var heading=EndHeading(incoming);if(heading.sqrMagnitude<.5f)return 0;
  var visited=new HashSet<HdEdge>{incoming};int result=0;
  foreach(int index in incoming.next??new int[0]){var next=Target(map,index);if(Connected(incoming,next))result|=Walk(map,next,junction,heading,visited,0,0);}
  return result;
 }
 static int Walk(MapData map,HdEdge edge,HybridJunctionRule junction,Vector3 incoming,HashSet<HdEdge> visited,int hops,float distance){
  if(!Native(edge)||hops>=MaximumHops||visited.Contains(edge)||!Near(edge,junction))return 0;
  distance+=Length(edge);if(distance>MaximumDistance)return 0;
  visited.Add(edge);int result=0;
  foreach(int index in edge.next??new int[0]){
   var next=Target(map,index);if(!Exit(next)||visited.Contains(next)||!Connected(edge,next))continue;
   var outgoing=StartHeading(next);float alignment=Vector3.Dot(incoming,outgoing);
   // Source U-turn curves are sometimes encoded turn=0. Require the existing
   // curve to finish in the reverse direction and have a connected reverse exit.
   if(edge.turn==UturnMovement.Turn||edge.turn==0&&alignment<-.8f&&Vector3.Dot(EndHeading(edge),outgoing)>.75f)result|=LaneDirectionPaint.Uturn;
   else if(edge.turn!=0)result|=UturnMovement.PaintMask(edge.turn);
   // A short approach segment before the junction is not a through movement.
   // Finish on an aligned existing exit beyond this junction's crossing plane.
   else if(alignment>.8f&&Vector3.Dot(End(edge)-junction.V(),incoming)>=5)result|=LaneDirectionPaint.Straight;
   else if(Native(next))result|=Walk(map,next,junction,incoming,visited,hops+1,distance);
  }
  visited.Remove(edge);return result;
 }
}
