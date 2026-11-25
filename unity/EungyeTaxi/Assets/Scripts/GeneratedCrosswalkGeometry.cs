using System.Collections.Generic;
using UnityEngine;

// Provisional approach widths predate the edited road surface. Fit only generated
// stripes to the rendered corridor footprint; authored island/slip crossings keep
// their explicit endpoints. This changes no lane, stop line or signal permission.
public static class GeneratedCrosswalkGeometry {
 // An authored full-road crossing already overlaps lanes moving both ways.
 // A short crossing serving one direction can end on a pedestrian island;
 // nearby lanes outside its painted span must not make it eligible.
 public static RoadMapFacility Effective(MapData map,RoadMapFacility source){
  if(source==null||source.kind!="crosswalk"||source.type=="보도 접점 지정"||map==null||!map.hybrid||map.hdEdges==null||source.scale!=1)return source;
  var center=source.Position;var direction=Quaternion.Euler(0,source.yaw,0)*Vector3.forward;var across=TrafficSystem.Right(direction);bool forward=false,backward=false;
  var widths=new Dictionary<string,float>();if(map.editorDocument!=null)foreach(var lane in map.editorDocument.lanes)widths[lane.id]=lane.width;
  foreach(var lane in map.hdEdges){if(lane.disabled||lane.change!=0||lane.points==null)continue;float width;if(!widths.TryGetValue(lane.id??"",out width))width=3.5f;
   for(int i=1;i<lane.points.Length;i++){
    var a=lane.points[i-1].V();var b=lane.points[i].V();float u=Vector3.Dot(a-center,direction),v=Vector3.Dot(b-center,direction);
    if(u*v>0||Mathf.Abs(u-v)<.001f)continue;float alignment=Vector3.Dot((b-a).normalized,direction);if(Mathf.Abs(alignment)<.85f)continue;
    var point=Vector3.Lerp(a,b,u/(u-v));if(Mathf.Abs(point.y-center.y)>1.5f||Mathf.Abs(Vector3.Dot(point-center,across))>(source.width+width)*.5f)continue;
    if(alignment>0)forward=true;else backward=true;
   }
   if(forward&&backward)break;
  }
  if(!forward||!backward)return source;float halfWidth=source.width*.5f;
  if(!Fit(map,ref center,direction,ref halfWidth))return source;
  // A crossing with a bicycle strip was trimmed at its corners (tools/map_review_r55_crosswalks.py):
  // keep the part of the fitted span that lies inside the authored span, so both the kerb and the trim hold.
  if(CrosswalkBikeCrossing.Side(source)!=0){float fitted=Vector3.Dot(center-source.Position,across),lo=Mathf.Max(fitted-halfWidth,-source.width*.5f),hi=Mathf.Min(fitted+halfWidth,source.width*.5f);if(hi-lo<1f)return source;center=source.Position+across*((lo+hi)*.5f);halfWidth=(hi-lo)*.5f;}
  if((center-source.Position).sqrMagnitude<.000001f&&Mathf.Abs(halfWidth*2-source.width)<.001f)return source;
  var result=JsonUtility.FromJson<RoadMapFacility>(JsonUtility.ToJson(source));result.x=center.x;result.z=center.z;result.width=halfWidth*2;return result;
 }
 struct Span {public float a,b;public Span(float a,float b){this.a=a;this.b=b;}}
 const float MaxAdjustment=12f;
 static float Cross(Vector3 a,Vector3 b){return a.x*b.z-a.z*b.x;}
 static void Intersect(Vector3[] polygon,Vector3 center,Vector3 across,List<Span> spans){
  var hits=new List<float>();
  for(int i=0;i<polygon.Length;i++){
   var a=polygon[i]-center;var edge=polygon[(i+1)%polygon.Length]-polygon[i];float denominator=Cross(across,edge);
   if(Mathf.Abs(denominator)<.00001f)continue;
   float at=Cross(a,across)/denominator;
   if(at>=0&&at<1)hits.Add(Cross(a,edge)/denominator);
  }
  hits.Sort();for(int i=1;i<hits.Count;i+=2)if(hits[i]-hits[i-1]>.001f)spans.Add(new Span(hits[i-1],hits[i]));
 }
 static bool Near(Vector3 a,Vector3 b,Vector3 center,float margin){return Mathf.Min(a.x,b.x)-margin<=center.x&&Mathf.Max(a.x,b.x)+margin>=center.x&&Mathf.Min(a.z,b.z)-margin<=center.z&&Mathf.Max(a.z,b.z)+margin>=center.z;}
 static void Cap(Vector3 p,float radius,Vector3 center,Vector3 across,List<Span> spans){
  var polygon=new Vector3[12];for(int i=0;i<12;i++){float angle=i*Mathf.PI/6;polygon[i]=p+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;}Intersect(polygon,center,across,spans);
 }
 public static bool Fit(MapData map,ref Vector3 center,Vector3 incoming,ref float halfWidth){
  if(map==null||!map.hybrid||map.roads==null||halfWidth<1||incoming.sqrMagnitude<.5f)return false;
  incoming.y=0;incoming.Normalize();var across=TrafficSystem.Right(incoming);var spans=new List<Span>();float range=halfWidth+MaxAdjustment;
  foreach(var road in map.roads){if(road==null||road.points==null||road.width<=0)continue;
   for(int i=1;i<road.points.Length;i++){
    var a=road.points[i-1].V();var b=road.points[i].V();float radius=road.width*.5f;
    if(!Near(a,b,center,range+radius)||Mathf.Abs((a.y+b.y)*.5f-center.y)>1.5f)continue;
    var direction=(b-a).normalized;if(Mathf.Abs(Vector3.Dot(direction,incoming))<.7f)continue;
    var side=TrafficSystem.Right(direction)*radius;
    Intersect(new[]{a-side,b-side,b+side,a+side},center,across,spans);
    // WorldBuilder.AppendRoad uses these same twelve-sided vertex caps.
    if((a-center).sqrMagnitude<(range+radius)*(range+radius))Cap(a,radius,center,across,spans);
    if((b-center).sqrMagnitude<(range+radius)*(range+radius))Cap(b,radius,center,across,spans);
   }
  }
  spans.Sort((a,b)=>a.a.CompareTo(b.a));var merged=new List<Span>();
  foreach(var span in spans){if(merged.Count==0||span.a>merged[merged.Count-1].b+.03f)merged.Add(span);else{var last=merged[merged.Count-1];last.b=Mathf.Max(last.b,span.b);merged[merged.Count-1]=last;}}
  foreach(var span in merged){if(span.a>0||span.b<0)continue;float low=span.a,high=span.b;
   // Raised sidewalks are the actual pedestrian landing boundary, including
   // islands. Do not paint across them just because source road caps overlap.
   foreach(var area in map.areas??new MapArea[0]){if(area.kind!="sidewalk"||area.height<.045f||area.points==null)continue;
    float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;foreach(var p in area.points){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
    if(center.x+range<minX||center.x-range>maxX||center.z+range<minZ||center.z-range>maxZ)continue;
    var polygon=new Vector3[area.points.Length];for(int i=0;i<polygon.Length;i++)polygon[i]=area.points[i].V();var walks=new List<Span>();Intersect(polygon,center,across,walks);
    foreach(var walk in walks){if(walk.a<=0&&walk.b>=0)return false;if(walk.b<0)low=Mathf.Max(low,walk.b);if(walk.a>0)high=Mathf.Min(high,walk.a);}
   }
   if(high-low<2||Mathf.Abs(low+halfWidth)>MaxAdjustment||Mathf.Abs(high-halfWidth)>MaxAdjustment)return false;
   center+=across*(low+high)*.5f;halfWidth=(high-low)*.5f;return true;
  }
  return false;
 }
}
