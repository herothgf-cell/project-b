using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static partial class ApprovedEungyeRoads {
 public const string Prefix="reviewed:r47:";
 const string Junction="J7973_3744";
 const string PriorHash="ebb810cbd2214e829af890708cd565022f449fcbcbd51039487402df81597161";
 static readonly RoadMapEvidence Evidence=new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-19",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=18,0,0,0,adh&p=cvqX4giBmV4gRebMl2D0xA,13.3,-8.82,80,Float",
  note="Approved A–D and J7973 intersection design. Lane width, stations, island dimensions and signal timings are game estimates."
 };
 static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException("Approved Eungye roads: "+message);}
 static Vector3 V(float x,float z)=>new Vector3(x,0,z);
 static MapPoint Point(Vector3 p)=>new MapPoint{x=p.x,z=p.z};
 static RoadMapLane Lane(RoadMapDocument doc,string id)=>doc.lanes.Single(l=>l.id==id);
 static AssistedRoadNetwork.Path PathOf(RoadMapLane l)=>new AssistedRoadNetwork.Path(l.points.Select(p=>p.V()).ToArray());
 static void Set(RoadMapLane lane,IEnumerable<Vector3> points){
  var unique=new List<Vector3>();
  foreach(var p in points)if(unique.Count==0||(p-unique.Last()).sqrMagnitude>=.00000225f)unique.Add(p);
  Y06EastApproachEdit.Assign(lane,unique,Evidence);
 }
 static void Add(RoadMapDocument doc,RoadMapFacility feature)=>doc.facilities=doc.facilities.Append(feature).ToArray();
 static void Add(RoadMapDocument doc,ProjectMapObject item)=>doc.scene.objects=doc.scene.objects.Append(item).ToArray();
 static RoadMapFacility Feature(string id,string kind,Vector3 p,Vector3 heading,float width,float length)=>
  new RoadMapFacility{id=Prefix+id,kind=kind,x=p.x,z=p.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,width=width,length=length,evidence=Evidence};

 static void Stroke(RoadMapDocument doc,string id,Vector3 a,Vector3 b,bool yellow=false){
  float length=Vector3.Distance(a,b);
  if(length<.04f)return;
  int pieces=Mathf.CeilToInt(length/38);
  for(int i=0;i<pieces;i++){
   var f=Feature(pieces==1?id:id+":"+i,"line",Vector3.Lerp(a,b,(i+.5f)/pieces),(b-a).normalized,yellow?.15f:.13f,length/pieces+.01f);
   f.type=yellow?ReviewedCorridorPaint.MedianType:ReviewedCorridorPaint.StrokeType;
   Add(doc,f);
  }
 }
 static void Dashed(RoadMapDocument doc,string id,Vector3 a,Vector3 b){
  float length=Vector3.Distance(a,b);var direction=(b-a).normalized;
  for(float at=0;at<length-.04f;at+=6)Stroke(doc,id+":"+at,a+direction*at,a+direction*Mathf.Min(at+3,length));
 }
 static void Mask(RoadMapDocument doc,string id,Vector3 a,Vector3 b,float width){
  int count=Mathf.CeilToInt(Vector3.Distance(a,b)/38);
  for(int i=0;i<count;i++){
   var f=Feature(id+":mask:"+i,"lanePresentation",Vector3.Lerp(a,b,(i+.5f)/count),(b-a).normalized,width,Vector3.Distance(a,b)/count+.04f);
   f.type=ReviewedCorridorPaint.MaskType;Add(doc,f);
  }
 }
 static void Sidewalk(RoadMapDocument doc,string id,IEnumerable<Vector3> polygon){
  var points=polygon.Select(Point).ToArray();ProjectMapScene.Triangulate(points);
  Add(doc,new ProjectMapObject{id=Prefix+id,kind="sidewalk",name="검토 보행 공간",height=.14f,points=points});
 }
 static void Surface(RoadMapDocument doc,string id,IEnumerable<Vector3> points,float width,int lanes=1){
  Add(doc,new ProjectMapObject{id="road:"+Prefix+id,kind="road",name="검토 차도",width=width,lanes=lanes,
   oneWay=lanes==1,suppressPaint=true,points=points.Select(Point).ToArray()});
 }
 static RoadMapFacility Crossing(RoadMapDocument doc,string id,Vector3 a,Vector3 b,float depth){
  var crossing=Feature(id,"crosswalk",(a+b)*.5f,TrafficSystem.Right((b-a).normalized),Vector3.Distance(a,b),depth);
  crossing.type="보도 접점 지정";Add(doc,crossing);return crossing;
 }
 static void Landing(RoadMapDocument doc,string id,Vector3 curb,Vector3 inward,float width=3.2f){
  Sidewalk(doc,id,ProjectMapScene.Rectangle(curb+inward*1.5f,inward,width,3).Select(p=>p.V()));
 }
 static RoadMapLane Split(RoadMapDocument doc,RoadMapLane original,float at){
  var path=PathOf(original);Require(at>.25f&&at<path.length-.25f,"split must be inside "+original.id);
  var head=Y07MapEdit.Slice(path,0,at);var tail=Y07MapEdit.Slice(path,at,path.length);
  Y06EastApproachEdit.Assign(original,head.Concat(tail.Skip(1)).Select(p=>p.V()),Evidence);
  return Lane(doc,RoadMapAuthoringOperations.SplitLane(doc,original.id,head.Length-1));
 }
 static IEnumerable<Vector3> Cubic(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int steps){
  for(int i=0;i<=steps;i++){float t=(float)i/steps,u=1-t;yield return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}
 }
 static RoadMapLane Branch(RoadMapDocument doc,string id,RoadMapLane from,RoadMapLane to,Vector3[] points,int change=0){
  var lane=new RoadMapLane{id=Prefix+id,sourceId=from.sourceId,roadName=from.roadName,width=3.3f,
   status="provisional",number=to.number,fromNode=from.toNode,toNode=to.fromNode,next=new[]{to.id},change=change};
  Set(lane,points);doc.lanes=doc.lanes.Append(lane).ToArray();
  from.next=from.next.Append(lane.id).ToArray();return lane;
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Require(before.document.revision==46&&before.contentHash==PriorHash,"expected reviewed r46");
  var doc=before.document.Copy();
  NorthTransition(doc);WestTransition(doc);ParkingLoop(doc);CommercialStreet(doc);RightTurns(doc);
  doc.revision=47;doc.projectRevision="";doc.savedAt=DateTime.UtcNow.ToString("o");
  VerifyDocument(before.baseline,doc);
  var result=ProjectMapPackage.Create(before.baseline,doc);
  Verify(result);
  ProjectMapStore.Save(root,result,token);
  var saved=ProjectMapStore.Load(root);
  Require(saved.contentHash==result.contentHash&&saved.baselineHash==before.baselineHash,"saved map identity");
  Debug.Log("APPROVED_EUNGYE_APPLIED "+saved.contentHash);
 }

 public static void VerifyAndBuild(){
  Verify(ProjectMapStore.Load(ProjectMapBuild.Root));
  StopUturnChecks.Map();
  ProjectMapBuild.Prepare();
  JunctionRouteFeedbackTests.VerifyMigration();
  const string scene="Assets/Scenes/FirstDrive.unity";
  var bytes=File.ReadAllBytes(scene);
  try{PrototypeBuild.BuildHybrid();}
  finally{File.WriteAllBytes(scene,bytes);}
 }
 static void VerifyDocument(MapData baseline,RoadMapDocument doc){
  var issues=RoadMapValidator.Validate(baseline,doc).Where(i=>i.error).ToArray();
  Require(issues.Length==0,string.Join(" / ",issues.Take(24).Select(i=>i+" "+i.position)));
 }
 static void Verify(ProjectMapPackage package){
  var doc=package.document;
  package.Validate();
  var map=package.Compose();EungyeConnectivityRepair.Apply(map,package.baseline);
  var graph=new HdRoadGraph(map);
  foreach(string id in new[]{"A:north-through","C:west-inner","D:west-inner"}){
   var branch=graph.edges.Single(e=>e.id==Prefix+id);
   Require(!branch.disabled&&branch.next.Length==1&&graph.edges.Any(e=>e.next.Any(n=>graph.edges[n]==branch)),
    "two-ended lane connection "+id);
  }
  foreach(string section in new[]{"A","B","C","D"}){
   var paint=doc.facilities.Where(f=>f.id.StartsWith(Prefix+section+":")).ToArray();
   Require(paint.Any(f=>f.type==ReviewedCorridorPaint.MaskType)&&
    paint.Any(f=>f.type==ReviewedCorridorPaint.MedianType)&&paint.Any(f=>f.type==ReviewedCorridorPaint.StrokeType),
    "replacement centreline and lane dividers "+section);
  }
  foreach(var f in doc.facilities.Where(f=>f.id.StartsWith(Prefix)&&f.kind=="crosswalk")){
   Require(CrosswalkLanding.Try(map,f,out _,out _),"crosswalk landings "+f.id);
  }
  foreach(string corner in new[]{"NE","NW","SE","SW"}){
   var slip=graph.edges.Single(e=>e.id==SlipId(corner));var merge=graph.edges[slip.next.Single()];
   Require(slip.turn==1&&merge.change==-1&&merge.next.Length==1,"slip merge "+corner);
   Require(graph.edges.Any(e=>!e.disabled&&e.next.Any(n=>graph.edges[n].id==slip.id)),"slip entry "+corner);
   var pole=doc.facilities.Single(f=>f.id==Prefix+"J7973:"+corner+":signal-pole");
   var island=doc.scene.objects.Single(o=>o.id==Prefix+"J7973:"+corner+":island");
   Require(ProjectMapScene.Contains(island.points,pole.Position),"signal pole on the refuge "+corner);
  }
  var walk=doc.signals.Single(s=>s.junctionId==WestGate);
  var pedestrianPhase=walk.stages.Last();
  Require(pedestrianPhase.groups.Length==6&&walk.groups.Where(g=>pedestrianPhase.groups.Contains(g.id)).All(g=>g.pedestrian),"all six crossings without vehicle signal conflict");
  var catalog=PassengerStopCatalog.For(map);
  var stops=catalog.Stops;
  Require(stops.Length==39,"expected 39 passenger stops, found "+stops.Length+": "+string.Join(" / ",catalog.Rejected));
  int count=0;
  foreach(var from in stops)foreach(var to in stops)if(from!=to){
   Require(graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"unreachable passenger route "+from.name+" → "+to.name);count++;
  }
  Debug.Log("APPROVED_EUNGYE_OK 4 slip lanes, 6 simultaneous crossings, 39 stops, "+count+" routes");
 }
}
