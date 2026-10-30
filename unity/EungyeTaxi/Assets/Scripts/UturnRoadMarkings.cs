using System.Collections.Generic;
using UnityEngine;

// Game approximation of the median opening used by each explicit U-turn.
// Width is measured across the incoming heading, never from world X/Z.
public static class UturnRoadMarkings {
 public sealed class Zone {
  public string incomingId,joinId;public Vector3 start,forward,across;public float width,length,sourceOffset;
  public bool Covers(Vector3 point,float extra=0){var delta=point-start;float along=Vector3.Dot(delta,forward),lateral=Vector3.Dot(delta,across);return along>=-extra&&along<=length+extra&&lateral>=Mathf.Min(0,-sourceOffset)-.45f-extra&&lateral<=Mathf.Max(0,-sourceOffset)+.45f+extra;}
 }
 public static List<Zone> Build(MapData map){var result=new List<Zone>();var document=map.editorDocument;if(document==null)return result;
  var lanes=document.lanes??new RoadMapLane[0];var used=new HashSet<string>();
  foreach(var join in lanes){if(join.disabled||join.turn!=UturnMovement.Turn||join.points==null||join.points.Length<2)continue;
   RoadMapLane incoming=null;float error=float.MaxValue;var first=join.points[0].V();
   foreach(var lane in lanes){if(lane.disabled||lane.turn!=0||lane.change!=0||lane.points==null||lane.points.Length<2||lane.next==null||System.Array.IndexOf(lane.next,join.id)<0)continue;
    float distance=(lane.points[lane.points.Length-1].V()-first).sqrMagnitude;if(distance<error){error=distance;incoming=lane;}
   }
   if(incoming==null||!used.Add(incoming.id))continue;
   var a=incoming.points[incoming.points.Length-1].V();var forward=(a-incoming.points[incoming.points.Length-2].V()).normalized;forward.y=0;forward.Normalize();var across=-TrafficSystem.Right(forward);
   var b=join.points[join.points.Length-1].V();float width=Vector3.Dot(b-a,across);if(width<=0)continue;
   float length=0;foreach(var point in join.points)length=Mathf.Max(length,Vector3.Dot(point.V()-a,forward));
   var zone=new Zone{incomingId=incoming.id,joinId=join.id,start=a+across*(width*.5f),forward=forward,across=across,width=width,length=length+2f};
   SnapToSourceMedian(map,zone);result.Add(zone);
  }return result;
 }
 // The source centreline can be slightly displaced from compiled lane-pair
 // midpoints. Follow the closest aligned local source axis, while masking
 // only the narrow span between that axis and the compiled midpoint.
 static void SnapToSourceMedian(MapData map,Zone zone){float nearest=float.MaxValue,offset=0;var reference=zone.start+zone.forward*(zone.length*.5f);
  foreach(var mark in map.hdMarks??new HdMark[0]){if(mark.kind!="501"||!(mark.type??"").StartsWith("1")||mark.points==null)continue;
   for(int i=1;i<mark.points.Length;i++){var a=mark.points[i-1].V();var b=mark.points[i].V();var delta=b-a;if(delta.sqrMagnitude<.001f||Mathf.Abs(Vector3.Dot(delta.normalized,zone.forward))<.98f)continue;
    float from=Vector3.Dot(a-zone.start,zone.forward),to=Vector3.Dot(b-zone.start,zone.forward);if(Mathf.Max(from,to)<0||Mathf.Min(from,to)>zone.length)continue;
    float t=Mathf.Clamp01(Vector3.Dot(reference-a,delta)/delta.sqrMagnitude);var point=a+delta*t;float lateral=Vector3.Dot(point-zone.start,zone.across),distance=(point-reference).sqrMagnitude;
    if(Mathf.Abs(lateral)>2.5f||distance>=nearest)continue;nearest=distance;offset=lateral;
   }
  }
  zone.sourceOffset=offset;zone.start+=zone.across*offset;
 }
 public static bool Covers(List<Zone> zones,Vector3 point,float extra=0){foreach(var zone in zones)if(zone.Covers(point,extra))return true;return false;}
 static bool Ordinary(RoadMapLane lane){return !lane.disabled&&lane.turn==0&&lane.change==0&&lane.points!=null&&lane.points.Length>=2;}
 // A split approach may be too short for the regular direction layout, or
 // have an ordinary continuation alongside its U-turn. Reuse only its unique
 // contiguous predecessor chain for this additional visual glyph.
 static AssistedRoadNetwork.Path IncomingPath(RoadMapDocument document,RoadMapLane incoming){
  var reverse=new List<Vector3>();var visited=new HashSet<string>();var current=incoming;float length=0;
  while(current!=null&&visited.Add(current.id)){
   for(int i=current.points.Length-1;i>=0;i--){var point=current.points[i].V();if(reverse.Count>0){var last=reverse[reverse.Count-1];float segment=Vector3.Distance(last,point);if(segment<.001f)continue;if(length+segment>=30){reverse.Add(Vector3.Lerp(last,point,(30-length)/segment));length=30;break;}length+=segment;}reverse.Add(point);}
   if(length>=15+2.2f)break;
   RoadMapLane predecessor=null;var start=current.points[0].V();var forward=(current.points[1].V()-start).normalized;
   foreach(var lane in document.lanes){if(!Ordinary(lane)||visited.Contains(lane.id)||lane.next==null||System.Array.IndexOf(lane.next,current.id)<0)continue;var end=lane.points[lane.points.Length-1].V();var heading=(end-lane.points[lane.points.Length-2].V()).normalized;
    if((end-start).sqrMagnitude>.25f||Vector3.Dot(heading,forward)<.98f)continue;if(predecessor!=null){predecessor=null;break;}predecessor=lane;
   }current=predecessor;
  }
  reverse.Reverse();return reverse.Count<2?null:new AssistedRoadNetwork.Path(reverse.ToArray());
 }
 static bool OverlapsGlyph(LaneDirectionPaint.Arrow arrow,List<LaneDirectionPaint.Arrow> placed){foreach(var prior in placed)foreach(var glyph in LaneDirectionPaint.Glyphs(prior)){
  var footprint=new RoadJunctionGeometry.Clearance{center=glyph.centre,forward=glyph.forward,halfWidth=1.3f,halfDepth=2.2f};if(footprint.Overlaps(arrow.centre,arrow.forward,1.3f,2.2f))return true;
 }return false;}
 public static List<LaneDirectionPaint.Arrow> FallbackArrows(MapData map){var result=new List<LaneDirectionPaint.Arrow>();if(map.editorDocument==null||!map.hybrid||!map.HasHd)return result;
  var placed=LaneDirectionPaint.Layout(map);var clearances=RoadJunctionGeometry.PaintClearances(map);
  foreach(var zone in Build(map)){int edge=System.Array.FindIndex(map.hdEdges,e=>e.id==zone.incomingId);if(edge<0||placed.Exists(a=>a.edgeIndex==edge&&(a.turns&LaneDirectionPaint.Uturn)!=0))continue;
   var incoming=System.Array.Find(map.editorDocument.lanes,l=>l.id==zone.incomingId);if(incoming==null)continue;var path=IncomingPath(map.editorDocument,incoming);if(path==null)continue;
   foreach(float before in new[]{10f,9f,8f,7f,6f,11f,12f,13f,14f,15f}){float at=path.length-before;if(at<2.2f)continue;
    var arrow=new LaneDirectionPaint.Arrow{edgeIndex=edge,turns=LaneDirectionPaint.Uturn,centre=path.Position(at),forward=path.Tangent(at)};
    if(!LaneDirectionPaint.Clear(arrow,clearances)||OverlapsGlyph(arrow,placed))continue;result.Add(arrow);placed.Add(arrow);break;
   }
  }return result;
 }
 public static void Paint(MapData map,System.Action<string,Vector3,Vector3,float,float,bool> mark){
  foreach(var zone in Build(map))for(float at=0;at<zone.length;at+=3f){float length=Mathf.Min(1.5f,zone.length-at);mark("U-turn permitted median",zone.start+zone.forward*(at+length*.5f),zone.forward,.16f,length,false);}
  foreach(var arrow in FallbackArrows(map))LaneDirectionPaint.Paint(arrow,"U-turn approach arrow "+map.hdEdges[arrow.edgeIndex].id,mark);
 }
}
