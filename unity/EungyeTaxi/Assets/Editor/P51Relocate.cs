using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 2026-11-23 P51 (Eungye-nam-ro, Hanyang Sujain / Hoban Summit gates) relocation, r50 -> r51.
// Kakao aerial (L0) and road view (2026-06) put the real junction about 8 m east of the
// r47 layout: west crosswalk x~653, east crosswalk x~683, the north (Hoban) gate road at
// x~670 and the south (Hanyang) gate road at x~664, with the diagonal crossings between
// x 661 and 681. The r47 vehicle heads stood on the carriageway near the stop lines;
// Korean practice (and the road view) mounts them on far-side right corner poles with an
// arm over the approach lanes. Game road widths and the E-W driving lanes are unchanged.
public static class P51Relocate {
 const string Gate="reviewed:r47:P51",B="reviewed:r47:B:";
 const float WestX=653.2f,EastX=683f,NorthLegX=670f,SouthLegX=664.2f,SouthCurb=78.3f,NorthCurb=96.2f,ApproachEnd=650.3f;
 static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException("P51 relocation: "+message);}
 static Vector3 V(float x,float z)=>new Vector3(x,0,z);
 static MapPoint P(Vector3 p)=>new MapPoint{x=p.x,z=p.z};
 static RoadMapLane Lane(RoadMapDocument d,string id){var lane=d.lanes.SingleOrDefault(l=>l.id==id);Require(lane!=null,"lane "+id);return lane;}
 static RoadMapFacility Fac(RoadMapDocument d,string id){var f=d.facilities.SingleOrDefault(x=>x.id==id);Require(f!=null,"facility "+id);return f;}
 static void SetPoints(RoadMapLane lane,IList<Vector3> points){
  float h=lane.heights!=null&&lane.heights.Length>0?lane.heights[0]:0;
  lane.points=points.Select(P).ToArray();lane.heights=Enumerable.Repeat(h,points.Count).ToArray();
 }
 // Same cubic as RoadMapAuthoringOperations.Connect, between the current lane ends.
 static Vector3[] Curve(Vector3 a,Vector3 forward,Vector3 b,Vector3 heading){
  float handle=Vector3.Distance(a,b)*.4f;var points=new Vector3[33];
  for(int i=0;i<33;i++){float t=i/32f,u=1-t;points[i]=u*u*u*a+3*u*u*t*(a+forward*handle)+3*u*t*t*(b-heading*handle)+t*t*t*b;}
  return points;
 }
 static Vector3 End(RoadMapLane l)=>l.points.Last().V();
 static Vector3 Start(RoadMapLane l)=>l.points[0].V();
 static Vector3 EndHeading(RoadMapLane l)=>(l.points[l.points.Length-1].V()-l.points[l.points.Length-2].V()).normalized;
 static Vector3 StartHeading(RoadMapLane l)=>(l.points[1].V()-l.points[0].V()).normalized;
 static void Recurve(RoadMapDocument d,string joinId){
  var join=Lane(d,joinId);var from=d.lanes.Single(l=>!l.disabled&&l.next.Contains(joinId));var to=Lane(d,join.next.Single());
  SetPoints(join,Curve(End(from),EndHeading(from),Start(to),StartHeading(to)));
 }
 static void ExtendTo(RoadMapLane lane,float x){
  var pts=lane.points.Select(p=>p.V()).ToList();var last=pts.Last();var dir=EndHeading(lane);
  Require(dir.x>.9f&&x>last.x,"extension direction for "+lane.id);
  float step=1f;for(float at=last.x+step;at<x-.05f;at+=step)pts.Add(last+dir*((at-last.x)/dir.x));
  pts.Add(last+dir*((x-last.x)/dir.x));SetPoints(lane,pts);
 }
 static void Place(RoadMapFacility f,Vector3 p,Vector3 heading,float width,float length){
  f.x=p.x;f.z=p.z;f.yaw=Quaternion.LookRotation(heading).eulerAngles.y;f.width=width;f.length=length;
 }
 static void Crossing(RoadMapFacility f,Vector3 a,Vector3 b){
  Place(f,(a+b)*.5f,TrafficSystem.Right((b-a).normalized),Vector3.Distance(a,b),f.length);
 }
 static void Scene(RoadMapDocument d,string id,IEnumerable<Vector3> points){
  var o=d.scene.objects.SingleOrDefault(x=>x.id==id);Require(o!=null,"scene "+id);o.points=points.Select(P).ToArray();
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Require(before.document.revision==50,"expected map r50, found r"+before.document.revision);
  var d=before.document.Copy();
  var evidence=new RoadMapEvidence{state="observed",url="https://map.kakao.com/link/roadview/37.4348047,126.8029377",month="2026-06",checkedAt="2026-11-23",appearanceConfirmed=true,ruleConfirmed=false,
   note="카카오 항공사진(L0)·로드뷰 대조: 교차로를 동쪽으로 옮김(서쪽 횡단보도 x≈653, 동쪽 x≈683, 북쪽 정문 x≈670, 남쪽 정문 x≈664). 차량 신호는 건너편 오른쪽 모서리 기둥과 팔. 치수는 게임 추정치."};

  // Gate roads (one-way into the apartment entrances).
  var hanyang=Lane(d,"service:reviewed:r42:P51:hanyang-gate");SetPoints(hanyang,new[]{V(SouthLegX,74f),V(SouthLegX+.1f,63f)});
  var hoban=Lane(d,"service:reviewed:r42:P51:hoban-gate");SetPoints(hoban,new[]{V(NorthLegX,101f),V(NorthLegX,112f)});
  // Eastbound approach and its left pocket now end just before the west crosswalk.
  ExtendTo(Lane(d,"editor:segment:bG9jYWw6NTIwNDo4MjY6NzY1OTo4MTc:r42-P51-approach"),ApproachEnd);
  ExtendTo(Lane(d,"reviewed:r42:P51:left"),ApproachEnd);
  foreach(var id in new[]{"join:reviewed:r42:P51:through","join:service:reviewed:r42:P51:hanyang-gate","join:service:reviewed:r42:P51:hoban-gate","join:reviewed:P15:WB:hanyang","join:reviewed:P15:WB:hoban"})
   Recurve(d,id);
  foreach(var id in new[]{"service:reviewed:r42:P51:hanyang-gate","service:reviewed:r42:P51:hoban-gate","join:service:reviewed:r42:P51:hanyang-gate","join:service:reviewed:r42:P51:hoban-gate","join:reviewed:r42:P51:through","join:reviewed:P15:WB:hanyang","join:reviewed:P15:WB:hoban"})
   Scene(d,"road:polish:"+id,Lane(d,id).points.Select(p=>p.V()));

  // Kerb openings follow the moved turning paths: generated sidewalk tiles along both kerbs
  // are enabled unless a driven lane envelope (validator envelope) passes over them.
  var nearby=d.lanes.Where(l=>!l.disabled&&l.points!=null&&l.points.Any(p=>p.x>630&&p.x<710&&p.z>55&&p.z<120)).ToArray();
  bool Driven(MapPoint[] poly){
   foreach(var l in nearby){var path=new AssistedRoadNetwork.Path(l.points.Select(q=>q.V()).ToArray());
    for(float at=0;at<=path.length;at+=.5f){var q=path.Position(at);var side=TrafficSystem.Right(path.Tangent(at));
     for(float o=-l.width*.5f;o<=l.width*.5f+.01f;o+=l.width*.25f)if(RoadMapRules.Polygon(poly,q+side*o))return true;}}
   return false;
  }
  int opened=0,closed=0;
  foreach(var o in d.scene.objects.Where(o=>o.kind=="sidewalk"&&o.id.StartsWith("sidewalk:generated:")&&o.points.Length>0&&o.points.All(p=>p.x>638&&p.x<705&&(p.z>76&&p.z<81||p.z>93&&p.z<97)))){
   bool want=!Driven(o.points);if(want!=o.enabled){if(want)opened++;else closed++;o.enabled=want;}
  }
  Debug.Log("P51_SIDEWALK enabled+"+opened+" disabled+"+closed);

  // Junction marker, crossings, pads and pedestrian heads.
  var marker=Fac(d,B+"gate");marker.x=(WestX+EastX)*.5f; // keeps its confirmed signal-program evidence (rule unchanged)
  var sw=V(WestX,SouthCurb);var nw=V(WestX,NorthCurb);var se=V(EastX,SouthCurb);var ne=V(EastX,NorthCurb);
  var walks=new[]{B+"walk-west",B+"walk-east",B+"walk-north",B+"walk-south",B+"walk-diagonal-a",B+"walk-diagonal-b"}.Select(id=>Fac(d,id)).ToArray();
  Crossing(walks[0],sw,nw);Crossing(walks[1],ne,se);
  Crossing(walks[2],V(NorthLegX-4.15f,99.2f),V(NorthLegX+4.15f,99.2f));
  Crossing(walks[3],V(SouthLegX+4.8f,75.1f),V(SouthLegX-4.8f,75.1f));
  Crossing(walks[4],V(658.5f,SouthCurb),V(681f,NorthCurb));Crossing(walks[5],V(681f,SouthCurb),V(661.3f,NorthCurb));
  for(int i=0;i<walks.Length;i++){
   var f=walks[i];f.evidence=evidence;var across=Quaternion.Euler(0,f.yaw,0)*Vector3.right;
   var curbA=f.Position-across*f.width*.5f;var curbB=f.Position+across*f.width*.5f;
   Scene(d,B+"walk-pad:"+i+":a",ProjectMapScene.Rectangle(curbA+(-across)*1.5f,-across,1.75f,3).Select(p=>p.V()));
   Scene(d,B+"walk-pad:"+i+":b",ProjectMapScene.Rectangle(curbB+across*1.5f,across,1.75f,3).Select(p=>p.V()));
   if(i<4)foreach(int side in new[]{-1,1}){
    var head=Fac(d,B+"walk-head:"+i+":"+side);var position=f.Position+across*side*(f.width*.5f+1.1f);
    head.x=position.x;head.z=position.z;head.yaw=Quaternion.LookRotation(across*side).eulerAngles.y;
   }
  }

  // Stop lines just before each crossing.
  Fac(d,B+"0:1:stop").x=ApproachEnd+.9f;
  Fac(d,B+"0:-1:stop").x=EastX+2.3f;
  Fac(d,B+"1:1:stop").x=NorthLegX-2.3f;
  Fac(d,B+"1:-1:stop").x=SouthLegX+.8f;
  d.facilities=d.facilities.Where(f=>f.id!="reviewed:r42:P51:stop").ToArray(); // duplicate eastbound stop line from r42

  // Vehicle heads: far-side right corner pole, arm (width) over the approach lanes.
  void Head(string id,float x,float z,Vector3 facing,float arm){var f=Fac(d,B+id);f.x=x;f.z=z;f.yaw=Quaternion.LookRotation(facing).eulerAngles.y;f.width=arm;f.height=5.2f;f.evidence=evidence;}
  Head("0:1:head",EastX-2f,SouthCurb-1.3f,Vector3.right,5.6f);     // eastbound: SE corner, arm north
  Head("0:-1:head",WestX+2f,NorthCurb+1.2f,Vector3.left,5.6f);     // westbound: NW corner, arm south
  Head("1:1:head",SouthLegX-4.4f,SouthCurb-1.3f,Vector3.back,3.3f); // from north: SW corner, arm east
  Head("1:-1:head",NorthLegX+4f,NorthCurb+1.2f,Vector3.forward,3.3f); // from south: NE corner, arm west

  var gap=d.facilities.SingleOrDefault(f=>f.id=="reviewed:r42:P51:yellow-gap");
  if(gap!=null){gap.x=marker.x;gap.width=EastX-WestX;}

  d.revision=51;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  var result=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,result,token);
  var saved=ProjectMapStore.Load(root);
  Require(saved.contentHash==result.contentHash&&saved.document.revision==51,"saved map identity");
  Debug.Log("P51_RELOCATED "+saved.contentHash);
 }
}
