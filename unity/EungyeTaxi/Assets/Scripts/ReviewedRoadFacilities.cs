using System;
using System.Collections.Generic;
using UnityEngine;

public static class ReviewedRoadFacilities {
 [Serializable] public sealed class Catalog {public string mapId;public Facility[] facilities;}
 [Serializable] public sealed class Facility {public string id,kind,sourceUrl,captureMonth,checkedAt,observed,estimated;public string[] northMarkIds,southMarkIds;public float hatchFromX,hatchToX,barrierFromX,barrierToX,barrierWidth,barrierHeight,cushionLength,cushionWidth,cushionHeight;public Opening[] openings;public ExplicitSegment[] segments;}
 [Serializable] public sealed class Opening {public float fromX,toX;}
 [Serializable] public sealed class ExplicitSegment {public string kind;public MapPoint[] points;public float width,height;}
 public sealed class Line {public Vector3 a,b;public float width,height;public string facilityId;}
 public sealed class Layout {public readonly List<Line> barriers=new List<Line>();public readonly List<Line> hatches=new List<Line>();public readonly List<Line> cushions=new List<Line>();}
 static List<Vector2> ClearIntervals(float from,float to,Opening[] openings){
  var result=new List<Vector2>();if(to<=from)return result;
  var ordered=new List<Opening>(openings??new Opening[0]);ordered.Sort((a,b)=>a.fromX.CompareTo(b.fromX));float at=from;
  foreach(var opening in ordered){if(opening.toX<=at||opening.fromX>=to)continue;float start=Mathf.Clamp(opening.fromX,from,to);if(start>at+.001f)result.Add(new Vector2(at,start));at=Mathf.Max(at,Mathf.Clamp(opening.toX,from,to));}
  if(at<to-.001f)result.Add(new Vector2(at,to));return result;
 }
 static bool Boundary(MapData map,string[] ids,float x,out Vector3 point){
  foreach(string id in ids??new string[0])foreach(var mark in map.hdMarks??new HdMark[0]){
   if(mark.id!=id||mark.points==null)continue;
    for(int i=1;i<mark.points.Length;i++){MapPoint a=mark.points[i-1],b=mark.points[i];if(Mathf.Abs(b.x-a.x)<.0001f||x<Mathf.Min(a.x,b.x)-.001f||x>Mathf.Max(a.x,b.x)+.001f)continue;
    point=new Vector3(x,0,Mathf.Lerp(a.z,b.z,(x-a.x)/(b.x-a.x)));return true;
   }
  }point=Vector3.zero;return false;
 }
 static void Pair(MapData map,Facility facility,float x,out Vector3 north,out Vector3 south){
  if(!Boundary(map,facility.northMarkIds,x,out north)||!Boundary(map,facility.southMarkIds,x,out south))throw new InvalidOperationException("Reviewed facility source marking missing at x="+x+": "+facility.id);
  if(Vector3.Distance(north,south)<.5f)throw new InvalidOperationException("Reviewed facility strip too narrow at x="+x+": "+facility.id);
 }
 static Line Segment(Vector3 a,Vector3 b,float width,float height,string id){return new Line{a=a,b=b,width=width,height=height,facilityId=id};}
 public static Layout Plan(MapData map,Catalog catalog){
  var layout=new Layout();if(map==null||catalog==null||map.mapId!=catalog.mapId)return layout;
  foreach(var facility in catalog.facilities??new Facility[0]){
   if(facility.kind=="paired-median"){
    if(string.IsNullOrEmpty(facility.sourceUrl)||facility.barrierWidth<=0||facility.barrierHeight<=0)throw new InvalidOperationException("Reviewed facility lacks evidence or dimensions: "+facility.id);
    foreach(var interval in ClearIntervals(facility.barrierFromX,facility.barrierToX,facility.openings)){
     for(float x=interval.x;x<interval.y-.001f;){float end=Mathf.Min(interval.y,x+4f);Vector3 n0,s0,n1,s1;Pair(map,facility,x,out n0,out s0);Pair(map,facility,end,out n1,out s1);
      layout.barriers.Add(Segment((n0+s0)*.5f,(n1+s1)*.5f,facility.barrierWidth,facility.barrierHeight,facility.id));x=end;
     }
    }
    if(facility.cushionLength>0){float from=facility.barrierFromX-facility.cushionLength;var clear=ClearIntervals(from,facility.barrierFromX,facility.openings);
     if(from>=facility.hatchFromX&&clear.Count==1&&Mathf.Abs(clear[0].x-from)<.001f&&Mathf.Abs(clear[0].y-facility.barrierFromX)<.001f){Vector3 n0,s0,n1,s1;Pair(map,facility,from,out n0,out s0);Pair(map,facility,facility.barrierFromX,out n1,out s1);
      layout.cushions.Add(Segment((n0+s0)*.5f,(n1+s1)*.5f,facility.cushionWidth,facility.cushionHeight,facility.id));
     }
    }
    foreach(var interval in ClearIntervals(facility.hatchFromX,facility.hatchToX,facility.openings)){
     for(float x=interval.x;x<interval.y-.001f;x+=2.5f){float end=Mathf.Min(interval.y,x+1.5f);Vector3 n0,s0,n1,s1;Pair(map,facility,x,out n0,out s0);Pair(map,facility,end,out n1,out s1);
      layout.hatches.Add(Segment(Vector3.Lerp(n0,s0,.055f),Vector3.Lerp(s1,n1,.055f),.12f,.018f,facility.id));
     }
    }
   }
   foreach(var explicitSegment in facility.segments??new ExplicitSegment[0]){
    if(explicitSegment.points==null||explicitSegment.points.Length<2)continue;
    for(int i=1;i<explicitSegment.points.Length;i++){var line=Segment(explicitSegment.points[i-1].V(),explicitSegment.points[i].V(),explicitSegment.width,explicitSegment.height,facility.id);
     if(explicitSegment.kind=="barrier")layout.barriers.Add(line);else if(explicitSegment.kind=="hatch")layout.hatches.Add(line);
    }
   }
  }
  return layout;
 }
 static void Beam(string name,Line line,float y,float width,float height,Material material,Transform parent,bool solid){
  var delta=line.b-line.a;if(delta.sqrMagnitude<.0001f)return;
  var object3d=WorldBuilder.Box(name,(line.a+line.b)*.5f+Vector3.up*y,new Vector3(width,height,delta.magnitude),material,parent,solid);
  object3d.transform.localRotation=Quaternion.LookRotation(delta.normalized,Vector3.up);
 }
 public static void Draw(MapData map,Transform parent,bool collisions){
  var asset=Resources.Load<TextAsset>("eungye-road-facilities");if(!asset)return;
  var catalog=JsonUtility.FromJson<Catalog>(asset.text);var layout=Plan(map,catalog);if(layout.barriers.Count==0&&layout.hatches.Count==0)return;
  var steel=WorldBuilder.Mat("Reviewed W-beam",new Color(.57f,.59f,.60f));var highlight=WorldBuilder.Mat("Reviewed W-beam flange",new Color(.75f,.76f,.72f));var yellow=WorldBuilder.Mat("Reviewed safety hatch",new Color(.96f,.7f,.09f));var cushion=WorldBuilder.Mat("Reviewed crash cushion",new Color(.94f,.62f,.10f));
  foreach(var line in layout.hatches)Beam("안전지대 빗금 "+line.facilityId,line,.085f,line.width,line.height,yellow,parent,false);
  var posts=new HashSet<Vector2Int>();
  foreach(var line in layout.barriers){
   Beam("중앙분리 난간 "+line.facilityId,line,line.height*.5f,line.width*.55f,line.height,steel,parent,collisions);
   Beam("중앙분리 난간 윗날개 "+line.facilityId,line,line.height*.83f,line.width,.06f,highlight,parent,false);
   Beam("중앙분리 난간 아랫날개 "+line.facilityId,line,line.height*.54f,line.width,.055f,highlight,parent,false);
   foreach(var point in new[]{line.a,line.b}){var key=new Vector2Int(Mathf.RoundToInt(point.x*100),Mathf.RoundToInt(point.z*100));if(!posts.Add(key))continue;
    WorldBuilder.Box("중앙분리 난간 기둥 "+line.facilityId,point+Vector3.up*line.height*.5f,new Vector3(.13f,line.height,.13f),steel,parent,collisions);
   }
  }
  foreach(var line in layout.cushions)Beam("중앙분리 충격흡수 "+line.facilityId,line,line.height*.5f,line.width,line.height,cushion,parent,collisions);
 }
}
