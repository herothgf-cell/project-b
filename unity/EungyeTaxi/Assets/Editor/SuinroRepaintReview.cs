using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class SuinroRepaintReview {
 const string PreviousHash="ac84fe3eae6e7985a77de9dd5becbd6f56811707bfe66ff2a9e6e1497d2be5ac";
 const string Island="r49:suinro:J6478:median:";
 const float West=627,East=674;
 static readonly RoadMapEvidence Evidence=new RoadMapEvidence{
  state="observed",month="2026-06",checkedAt="2026-11-20",appearanceConfirmed=true,
  url="https://map.kakao.com/",
  note="J6478 보행육교 아래 중앙 황색 빗금 안전지대를 카카오 스카이뷰로 확인. 끝점과 폭은 기존 주행 경로에 맞춘 게임 치수."
 };

 static void Check(bool pass,string message){if(!pass)throw new InvalidOperationException("SUINRO_REPAINT_REVIEW: "+message);}
 static bool ReplacedMedian(RoadMapFacility f)=>f.id.StartsWith("r48:suinro:centre:",StringComparison.Ordinal)&&
  f.x>West-4&&f.x<East+4&&Mathf.Abs(f.z-(410.5f-.235f*(f.x-647.8f)))<4;
 static (Vector3 a,Vector3 b) Span(RoadMapFacility f){
  var axis=Quaternion.Euler(0,f.yaw,0)*Vector3.forward*f.length*.5f;
  return axis.x>0?(f.Position-axis,f.Position+axis):(f.Position+axis,f.Position-axis);
 }
 static Vector3 AtX((Vector3 a,Vector3 b) line,float x)=>Vector3.Lerp(line.a,line.b,(x-line.a.x)/(line.b.x-line.a.x));
 static RoadMapFacility Stroke(string id,Vector3 a,Vector3 b,string type){
  var centre=(a+b)*.5f;
  return new RoadMapFacility{id=Island+id,kind="line",type=type,x=centre.x,z=centre.z,
   yaw=Quaternion.LookRotation(b-a).eulerAngles.y,width=type==ReviewedCorridorPaint.MedianType?.15f:.22f,
   length=Vector3.Distance(a,b),text="",evidence=Evidence};
 }
 static Vector3[] JoinAt(RoadMapFacility[] source,float x){
  var points=source.Select(Span).Where(l=>l.a.x<=x&&l.b.x>=x).Select(l=>AtX(l,x)).OrderBy(p=>p.z).ToArray();
  Check(points.Length>=2&&points.Last().z-points.First().z<.65f,"fit both ends to the r48 median");
  return new[]{points.First(),points.Last()};
 }
 static RoadMapFacility[] Paint(RoadMapDocument doc){
  var source=doc.facilities.Where(ReplacedMedian).ToArray();
  var west=JoinAt(source,West);var east=JoinAt(source,East);
  // The photographed channel widens on both sides of the footbridge.
  var x=new[]{West,637f,647.8f,657f,East};
  var z=new[]{(west[0].z+west[1].z)*.5f,412.55f,410.44f,408.66f,(east[0].z+east[1].z)*.5f};
  var widths=new[]{(west[1].z-west[0].z)*.5f,.75f,1.25f,1.05f,(east[1].z-east[0].z)*.5f};
  Vector3 Border(float at,int side){
   int i=1;
   while(i<x.Length-1&&at>x[i])i++;
   float t=Mathf.InverseLerp(x[i-1],x[i],at);
   return new Vector3(at,0,Mathf.Lerp(z[i-1],z[i],t)+side*Mathf.Lerp(widths[i-1],widths[i],t));
  }
  var output=new List<RoadMapFacility>();
  foreach(var f in doc.facilities){
   if(!ReplacedMedian(f)){output.Add(f);continue;}
   var l=Span(f);
   if(l.b.x<=West||l.a.x>=East){output.Add(f);continue;}
   if(l.a.x<West)output.Add(Stroke("west-seam:"+f.id,l.a,AtX(l,West),ReviewedCorridorPaint.MedianType));
   if(l.b.x>East)output.Add(Stroke("east-seam:"+f.id,AtX(l,East),l.b,ReviewedCorridorPaint.MedianType));
  }
  foreach(int side in new[]{-1,1})for(int i=1;i<x.Length;i++)
   output.Add(Stroke("border:"+side+":"+i,Border(x[i-1],side),Border(x[i],side),ReviewedCorridorPaint.MedianType));
  for(int n=0;n<19;n++){
   float at=West+4+n*2.15f;
   output.Add(Stroke("hatch:"+n,Border(at,-1),Border(at+1.35f,1),ReviewedCorridorPaint.YellowHatchType));
  }
  return output.ToArray();
 }
 static void Verify(ProjectMapPackage package){
  var fills=package.document.facilities.Where(f=>f.id.StartsWith(Island+"hatch:",StringComparison.Ordinal)).ToArray();
  Check(fills.Length==19,"authored centre hatch strokes");
  var paint=new ReviewedCorridorPaint(package.Compose());
  int rendered=0;
  paint.Draw((name,p,d,w,l,yellow)=>{
   if(name!="Safety island hatch")return;
   Check(yellow,"yellow fill without median scoring");
   rendered++;
  });
  Check(rendered==fills.Length,"all hatch strokes render");
  var lanes=package.document.lanes.Where(l=>!l.disabled&&l.points.Any(p=>p.x>West-20&&p.x<East+20&&p.z>393&&p.z<427));
  foreach(var f in package.document.facilities.Where(f=>f.id.StartsWith(Island,StringComparison.Ordinal))){
   var s=Span(f);
   var left=TrafficSystem.Right((s.b-s.a).normalized)*f.width*.5f;
   var ring=new[]{s.a-left,s.b-left,s.b+left,s.a+left}.Select(p=>new RoadGeometry.Point(p.x,p.z)).ToArray();
   foreach(var lane in lanes)for(int i=1;i<lane.points.Length;i++){
    var a=lane.points[i-1];var b=lane.points[i];
    if(Mathf.Max(a.x,b.x)+1.05f<s.a.x||Mathf.Min(a.x,b.x)-1.05f>s.b.x)continue;
    Check(!RoadGeometry.SweptLaneIntersects(new RoadGeometry.Point(a.x,a.z),new RoadGeometry.Point(b.x,b.z),ring,1.05f),
     "median overlaps taxi body on "+lane.id+" at "+f.Position);
   }
  }
  Debug.Log("SUINRO_REPAINT_REVIEW_OK J6478 19 hatch strokes, both seams joined, taxi paths clear");
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Check(before.document.revision==48&&before.contentHash==PreviousHash,"expected the reviewed r48 package");
  var doc=before.document.Copy();
  doc.facilities=Paint(doc);doc.revision=49;
  var result=ProjectMapPackage.Create(before.baseline,doc);
  Verify(result);
  ProjectMapStore.Save(root,result,token);
  Debug.Log("SUINRO_REPAINT_REVIEW_APPLIED "+result.contentHash);
 }
 public static void VerifyAndBuild(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);
  Verify(package);
  var map=package.Compose();
  EungyeConnectivityRepair.Apply(map,package.baseline);
  var graph=new HdRoadGraph(map);
  var stops=PassengerStopCatalog.For(map).Stops;
  Check(stops.Length==39,"all 39 passenger stops");
  int routes=0;
  foreach(var from in stops)foreach(var to in stops)if(from!=to){
   Check(graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"passenger route "+from.name+" → "+to.name);
   routes++;
  }
  Debug.Log("SUINRO_REPAINT_ROUTE_OK "+routes);
  ImportedMarkingRendererTests.Run();
  ProjectMapBuild.Prepare();
  JunctionRouteFeedbackTests.VerifyMigration();
  const string scene="Assets/Scenes/FirstDrive.unity";
  var bytes=File.ReadAllBytes(scene);
  try{PrototypeBuild.BuildHybrid();}finally{File.WriteAllBytes(scene,bytes);}
 }
}
