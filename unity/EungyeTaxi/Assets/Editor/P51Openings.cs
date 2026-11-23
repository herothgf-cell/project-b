using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 2026-11-23 P51 gate openings, r51 -> r52.
// r51 drew the two right turns into the apartment gates with the generic editor cubic
// (handle = 40% of the chord). From the stop line that sweeps a wide fan, so the south
// (Hanyang) kerb opening ran x 652-669 and the north (Hoban) one x 664-682, against gate
// crossings of x 659.4-669 and x 665.9-674.2. Each right turn now runs straight to a
// 6.5 m centre-line radius corner, which keeps the opening near the gate crossing.
// Gate roads, crossings, signals and stop lines are unchanged.
public static class P51Openings {
 const float Radius=6.5f;
 static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException("P51 openings: "+message);}
 static MapPoint P(Vector3 p)=>new MapPoint{x=p.x,z=p.z};
 static RoadMapLane Lane(RoadMapDocument d,string id){var lane=d.lanes.SingleOrDefault(l=>l.id==id);Require(lane!=null,"lane "+id);return lane;}
 static Vector3 End(RoadMapLane l)=>l.points.Last().V();
 static Vector3 Start(RoadMapLane l)=>l.points[0].V();
 static Vector3 EndHeading(RoadMapLane l)=>(l.points[l.points.Length-1].V()-l.points[l.points.Length-2].V()).normalized;
 static Vector3 StartHeading(RoadMapLane l)=>(l.points[1].V()-l.points[0].V()).normalized;
 // Straight, circular corner of the given radius, straight: a -> corner -> b.
 internal static Vector3[] Corner(Vector3 a,Vector3 forward,Vector3 b,Vector3 heading,float radius){
  forward.y=0;heading.y=0;forward.Normalize();heading.Normalize();
  // Corner = a + forward*s = b - heading*t.
  float det=forward.x*(-heading.z)-forward.z*(-heading.x);Require(Mathf.Abs(det)>.5f,"turn is not a corner");
  var delta=b-a;float s=(delta.x*(-heading.z)-delta.z*(-heading.x))/det;var corner=a+forward*s;
  float angle=Vector3.Angle(forward,heading)*Mathf.Deg2Rad,tangent=radius*Mathf.Tan(angle*.5f);
  Require(s>tangent+.5f&&Vector3.Distance(corner,b)>tangent+.5f,"radius does not fit between the stop line and the gate");
  var arcStart=corner-forward*tangent;var arcEnd=corner+heading*tangent;
  var side=Vector3.Cross(Vector3.up,forward).normalized;if(Vector3.Dot(side,heading)<0)side=-side;var centre=arcStart+side*radius;
  var points=new List<Vector3>();
  float lead=Vector3.Distance(a,arcStart);int leadSteps=Mathf.Max(1,Mathf.CeilToInt(lead));
  for(int i=0;i<leadSteps;i++)points.Add(Vector3.Lerp(a,arcStart,i/(float)leadSteps));
  var from=arcStart-centre;var to=arcEnd-centre;int arcSteps=16;
  for(int i=0;i<=arcSteps;i++)points.Add(centre+Vector3.Slerp(from,to,i/(float)arcSteps).normalized*radius);
  float tail=Vector3.Distance(arcEnd,b);int tailSteps=Mathf.Max(1,Mathf.CeilToInt(tail));
  for(int i=1;i<=tailSteps;i++)points.Add(Vector3.Lerp(arcEnd,b,i/(float)tailSteps));
  return points.ToArray();
 }
 static void Recorner(RoadMapDocument d,string joinId){
  var join=Lane(d,joinId);var from=d.lanes.Single(l=>!l.disabled&&l.next.Contains(joinId));var to=Lane(d,join.next.Single());
  var points=Corner(End(from),EndHeading(from),Start(to),StartHeading(to),Radius);
  float h=join.heights!=null&&join.heights.Length>0?join.heights[0]:0;
  join.points=points.Select(P).ToArray();join.heights=Enumerable.Repeat(h,points.Length).ToArray();
  var scene=d.scene.objects.SingleOrDefault(o=>o.id=="road:polish:"+joinId);Require(scene!=null,"scene road:polish:"+joinId);scene.points=join.points.ToArray();
 }
 // West crosswalk x 653.2 and east crosswalk x 683, each 2.4 m deep (P51Relocate).
 const float WestDeparture=651.5f,EastDeparture=684.5f;
 // Cut the departure lane at x = startX, keep its global source station, and run the through join straight to it.
 static void Through(RoadMapDocument d,string joinId,string departureId,float startX){
  var join=Lane(d,joinId);var departure=Lane(d,departureId);
  Require(join.next.Single()==departureId,"through join "+joinId);
  var pts=departure.points.Select(p=>p.V()).ToList();float sign=Mathf.Sign(pts[1].x-pts[0].x);
  Require(sign*(startX-pts[0].x)>1f,"departure "+departureId+" already starts past x "+startX);
  int keep=pts.FindIndex(p=>sign*(p.x-startX)>.05f);Require(keep>0,"cut point on "+departureId);
  var a=pts[keep-1];var b=pts[keep];var cut=Vector3.Lerp(a,b,(startX-a.x)/(b.x-a.x));
  float removed=0;for(int i=1;i<keep;i++)removed+=Vector3.Distance(pts[i-1],pts[i]);removed+=Vector3.Distance(a,cut);
  var rest=new List<Vector3>{cut};rest.AddRange(pts.Skip(keep));
  float dh=departure.heights!=null&&departure.heights.Length>0?departure.heights[0]:0;
  departure.points=rest.Select(P).ToArray();departure.heights=Enumerable.Repeat(dh,rest.Count).ToArray();departure.sourceOffset+=removed;
  // Unmarked-road bands (global lineage stations) that began inside the junction now begin at the
  // new departure start: the junction part is a join and never receives corridor paint.
  string origin=RoadMapAuthoringOperations.Origin(departure);
  foreach(var band in d.facilities.Where(f=>ReviewedUnmarkedRoadPaint.IsFeature(f))){
   var owner=d.lanes.FirstOrDefault(l=>l.id==band.laneId);if(owner==null||RoadMapAuthoringOperations.Origin(owner)!=origin||band.startAt>=departure.sourceOffset)continue;
   Require(band.endAt>departure.sourceOffset+1f,"band "+band.id+" would be empty");
   Debug.Log("P51_BAND "+band.id+" startAt "+band.startAt.ToString("F2")+" -> "+departure.sourceOffset.ToString("F2"));band.startAt=departure.sourceOffset;
  }
  var from=Start(join);int steps=Mathf.Max(2,Mathf.CeilToInt(Vector3.Distance(from,cut)));
  var line=Enumerable.Range(0,steps+1).Select(i=>Vector3.Lerp(from,cut,i/(float)steps)).ToArray();
  float jh=join.heights!=null&&join.heights.Length>0?join.heights[0]:0;
  join.points=line.Select(P).ToArray();join.heights=Enumerable.Repeat(jh,line.Length).ToArray();
  var scene=d.scene.objects.SingleOrDefault(o=>o.id=="road:polish:"+joinId);Require(scene!=null,"scene road:polish:"+joinId);scene.points=join.points.ToArray();
  Debug.Log("P51_THROUGH "+joinId+" -> x "+startX.ToString("F1")+" departure trimmed "+removed.ToString("F2")+" m");
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Require(before.document.revision==51,"expected map r51, found r"+before.document.revision);
  var d=before.document.Copy();
  Recorner(d,"join:service:reviewed:r42:P51:hanyang-gate"); // eastbound right turn, south gate
  Recorner(d,"join:reviewed:P15:WB:hoban");                  // westbound right turn, north gate
  // Both carriageways share one source corridor. Through r51 the departure lanes began inside
  // the junction (WB x 675.6, EB x 675) while the opposite side was only turn joins, so the
  // corridor painter saw one lane there and drew its edge half a road width out, on the north
  // footway (x 652-665, z 99) and the south-east kerb (x 675-683). The through joins now cross
  // the whole junction and each departure lane starts just past the far crosswalk.
  Through(d,"join:reviewed:P15:WB:through","editor:segment:bG9jYWw6NzY2MDo5MTg6NTIwNTo5Mjc:p15-wb-departure",WestDeparture);
  Through(d,"join:reviewed:r42:P51:through","editor:segment:bG9jYWw6NTIwNDo4MjY6NzY1OTo4MTc:r42-P51-departure",EastDeparture);

  // Same kerb rule as r51: a generated kerb tile is open only where a driven lane envelope crosses it.
  var nearby=d.lanes.Where(l=>!l.disabled&&l.points!=null&&l.points.Any(p=>p.x>630&&p.x<710&&p.z>55&&p.z<120)).ToArray();
  bool Driven(MapPoint[] poly){
   foreach(var l in nearby){var path=new AssistedRoadNetwork.Path(l.points.Select(q=>q.V()).ToArray());
    for(float at=0;at<=path.length;at+=.5f){var q=path.Position(at);var side=TrafficSystem.Right(path.Tangent(at));
     for(float o=-l.width*.5f;o<=l.width*.5f+.01f;o+=l.width*.25f)if(RoadMapRules.Polygon(poly,q+side*o))return true;}}
   return false;
  }
  int opened=0,closed=0;var south=new List<float>();var north=new List<float>();
  foreach(var o in d.scene.objects.Where(o=>o.kind=="sidewalk"&&o.id.StartsWith("sidewalk:generated:")&&o.points.Length>0&&o.points.All(p=>p.x>638&&p.x<705&&(p.z>76&&p.z<81||p.z>93&&p.z<97)))){
   bool want=!Driven(o.points);if(want!=o.enabled){if(want)opened++;else closed++;o.enabled=want;}
   if(!want)(o.points[0].z<88?south:north).AddRange(o.points.Select(p=>p.x));
  }
  Require(south.Count>0&&north.Count>0,"both gates keep a kerb opening");
  Debug.Log("P51_OPENINGS sidewalk enabled+"+opened+" disabled+"+closed+" south="+south.Min().ToString("F1")+".."+south.Max().ToString("F1")+" north="+north.Min().ToString("F1")+".."+north.Max().ToString("F1"));
  Require(south.Min()>=657.5f&&north.Max()<=677f,"openings still wider than the gate crossings");

  d.revision=52;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  var result=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,result,token);
  var saved=ProjectMapStore.Load(root);
  Require(saved.contentHash==result.contentHash&&saved.document.revision==52,"saved map identity");
  Debug.Log("P51_OPENINGS_SAVED "+saved.contentHash);
 }
}
