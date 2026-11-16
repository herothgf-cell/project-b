using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SouthboundSurveyMapEdit {
 const string PreviousHash="c37d14d6f34b192ca582a7d87c6630839787b30fcf3df1e7ef2d416ba3485ce3";
 const string Bank="J1927_5181", Eungye="J7841_867";
 const string BankHead="local:4442:8620:2062:5530", BankOuterPrefix="reviewed:P02:SB:right-approach";
 const string BankInner="portal:A2218G611310:0:in", BankOuter="portal:A222BC004232:0:in";
 const string BankLeft="A222BC004192:0", BankStraight="A2218G611310:0", BankRight="A222BC004232:0";
 const string EungyePrefix="reviewed:G04:SB:";
 const int EungyeApproach=36, EungyeTaper=21;
 static readonly string[] EungyeHeads={"local:7830:1476:7811:1051","local:7754:1479:7736:1054"};
 static readonly string[] EungyeExits={"local:7798:686:7778:21","local:7722:689:7702:23"};
 static readonly string[] Roles={"left","straight-1","straight-2","right"};
 static readonly float[] EungyeOffsets={-1.35f,1.6f,-2.9f,0};
 static readonly string EungyeInner="editor:segment:"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(EungyeHeads[0])).TrimEnd('=')+":g04-sb-left";

 static void Check(bool condition,string reason){if(!condition)throw new InvalidOperationException("SOUTHBOUND_SURVEY: "+reason);}
 static RoadMapLane Copy(RoadMapLane lane)=>JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));
 static AssistedRoadNetwork.Path Path(RoadMapLane lane)=>new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());
 static string Join(int source,int turn)=>"join:"+EungyeHeads[source]+":"+turn;
 static void FollowSurface(RoadMapDocument doc,RoadMapLane lane){
  var surface=doc.scene.objects.SingleOrDefault(o=>o.id=="road:polish:"+lane.id);
  if(surface!=null)surface.points=lane.points;
 }

 static Vector3 StopIntersection(RoadMapLane lane,Vector3 center,Vector3 normal){
  for(int i=1;i<lane.points.Length;i++){
   var from=lane.points[i-1].V();var to=lane.points[i].V();
   float a=Vector3.Dot(from-center,normal),b=Vector3.Dot(to-center,normal);
   if(a<=0&&b>0)return Vector3.Lerp(from,to,-a/(b-a));
  }
  throw new InvalidOperationException("Stop line does not cross "+lane.id);
 }

 static void PatchBank(RoadMapDocument doc,Dictionary<string,RoadMapLane> lanes){
  var evidence=new RoadMapEvidence{
   state="observed",month="2025-10",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
   url="https://map.naver.com/p?c=19,0,0,0,adh&p=Mvy6ZKeOOEaVGXYXYVBy7g,-145,-16,47,Float",
   note="Marker 39. User confirmed the inner southbound lane permits straight and left. October 2025 shows the outer right arrow; May 2026 is obscured. Captured turn destinations are retained. Lane placement and taper are game estimates."
  };
  var inner=lanes[BankInner];var outer=lanes[BankOuter];var right=lanes[BankRight];
  var head=lanes[BankHead];var trunk=Path(head);var innerPath=Path(inner);
  var side=lanes["join:"+BankHead+":1"];
  const int approach=18;
  float cut=trunk.length-approach;
  Check(!doc.facilities.Any(f=>f.laneId==BankHead)&&!doc.scene.objects.Any(o=>o.IsService&&o.laneId==BankHead&&o.laneAt>=cut),
   "39 upstream anchors need explicit remap");
  var outerLead=Copy(head);
  outerLead.id=BankOuterPrefix;outerLead.toNode=head.toNode;outerLead.fromNode=BankOuterPrefix+":start";
  outerLead.number=2;outerLead.sourceOffset=head.sourceOffset+cut;outerLead.ruleId=Bank;
  Y06EastApproachEdit.Assign(outerLead,Enumerable.Range(0,approach+1).Select(i=>trunk.Position(cut+i)),evidence);
  outerLead.next=new[]{outer.id,side.id};
  Y06EastApproachEdit.Assign(head,G05NorthWestMapEdit.OriginalHead(trunk,cut),evidence);
  head.toNode=outerLead.fromNode;head.next=new[]{inner.id,outerLead.id};
  inner.fromNode=head.toNode;
  const float separation=2.9f, spread=12f;
  Y06EastApproachEdit.Assign(inner,Enumerable.Range(0,approach+(int)Mathf.Ceil(innerPath.length)+1).Select(i=>{
   bool before=i<approach;
   float at=before?cut+i:Mathf.Min(i-approach,innerPath.length);
   var path=before?trunk:innerPath;
   float weight=before?Mathf.SmoothStep(0,1,i/spread):Mathf.SmoothStep(0,1,(innerPath.length-at)/16);
   return path.Position(at)-TrafficSystem.Right(path.Tangent(at))*separation*weight;
  }),evidence);
  doc.lanes=doc.lanes.Append(outerLead).ToArray();
  var outerPath=Path(outer);
  var lateral=TrafficSystem.Right(outerPath.Tangent(outerPath.length));
  const float spacing=3.15f, taper=12;
  Y06EastApproachEdit.Assign(outer,Enumerable.Range(0,25).Select(i=>{
   float at=outerPath.length*i/24;
   return outerPath.Position(at)+lateral*spacing*Mathf.SmoothStep(0,1,Mathf.Clamp01(at/taper));
  }),evidence);
  outer.width=3.05f;outer.number=2;outer.toNode=BankOuter+":end";
  Y06EastApproachEdit.Reconnect(right,outer,lanes[right.next.Single()],evidence);
  inner.ruleId=outer.ruleId=Bank;inner.evidence=evidence;
  inner.next=new[]{BankStraight,BankLeft};outer.next=new[]{BankRight};
  foreach(string id in inner.next.Concat(outer.next))lanes[id].ruleId=Bank;
  FollowSurface(doc,outer);
  var stop=doc.facilities.Single(f=>f.junctionId==Bank&&f.kind=="stopLine"&&f.axis==0&&f.sign==1);
  var normal=Quaternion.Euler(0,stop.yaw,0)*Vector3.forward;
  var innerStop=StopIntersection(inner,stop.Position,normal);var outerStop=StopIntersection(outer,stop.Position,normal);
  float stopSpacing=Vector3.Dot(outerStop-innerStop,TrafficSystem.Right(normal));
  Check(stopSpacing>=2.75f&&stopSpacing<=4.2f,"39 stop-line lanes are not distinct and parallel: "+stopSpacing);
  var stopCenter=(innerStop+outerStop)*.5f;
  stop.x=stopCenter.x;stop.z=stopCenter.z;stop.width=stopSpacing+(inner.width+outer.width)*.5f;
  stop.evidence=evidence;
  var leadPath=Path(outerLead);var innerLane=Path(inner);
  var divider=new List<RoadMapFacility>();
  foreach(int at in new[]{8,13,17}){
   var point=leadPath.Position(at);var heading=leadPath.Tangent(at);
   float distance;float measured=innerLane.Project(point,out distance);
   if(distance<2.55f)continue;
   var center=(point+innerLane.Position(measured))*.5f;
   divider.Add(new RoadMapFacility{id=BankOuterPrefix+":dash:"+at,kind="line",junctionId=Bank,axis=0,sign=1,
    x=center.x,z=center.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,width=.13f,length=2.5f,evidence=evidence});
  }
  Check(divider.Count>=2,"39 approach divider needs a separated section before the stop");
  doc.facilities=doc.facilities.Concat(divider).ToArray();
 }

 static void PatchEungye(ProjectMapPackage before,RoadMapDocument doc,Dictionary<string,RoadMapLane> lanes){
  var evidence=new RoadMapEvidence{
   state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
   url="https://map.naver.com/p?c=18.00,0,0,0,adh&p=H4u2Vo_uwxpOa5AldTRoNA,-175,-17.61,80,Float",
   note="Marker 37 southbound. May 2026 and March 2024 show 4 approach lanes before the stop and 3 departure lanes. Inner left and signed U-turn, two through lanes, user-confirmed outer right-only. Taper, lane centres and U-turn curve are game estimates."
  };
  var heads=EungyeHeads.Select(id=>lanes[id]).ToArray();
  var paths=heads.Select(Path).ToArray();
  Check(paths.All(p=>p.length>EungyeApproach),"37 southbound tapers fit");
  Check(!doc.facilities.Any(f=>EungyeHeads.Contains(f.laneId))&&!doc.scene.objects.Any(o=>o.IsService&&EungyeHeads.Contains(o.laneId)),
   "37 approach anchors need explicit remap");
  Check(!before.baseline.stops.Any(s=>s.graphStop&&EungyeHeads.Contains(before.baseline.hdEdges[s.laneEdge].id)),"37 graph stop needs explicit remap");
  var exits=EungyeExits.Select(id=>lanes[id]).ToArray();
  var exitPaths=exits.Select(Path).ToArray();
  var middle=Copy(exits[0]);middle.id=EungyePrefix+"departure-middle";middle.number=2;
  middle.fromNode=middle.id+":start";middle.toNode=middle.id+":end";
  Y06EastApproachEdit.Assign(middle,Enumerable.Range(0,35).Select(i=>
   (exitPaths[0].Position(exitPaths[0].length*i/34)+exitPaths[1].Position(exitPaths[1].length*i/34))*.5f),evidence);
  exits[1].number=3;
  var tails=new RoadMapLane[4];
  for(int i=0;i<tails.Length;i++){
   int source=i/2;var path=paths[source];float cut=path.length-EungyeApproach;
   var tail=Copy(heads[source]);tails[i]=tail;
   tail.id=i==0?EungyeInner:EungyePrefix+Roles[i];tail.number=i+1;tail.width=2.9f;
   tail.sourceOffset=heads[source].sourceOffset+cut;
   tail.toNode=tail.id+":end";tail.fromNode=heads[source].toNode;
   var lateral=TrafficSystem.Right(path.Tangent(path.length));
   float offset=EungyeOffsets[i];
   Y06EastApproachEdit.Assign(tail,Enumerable.Range(0,EungyeApproach+1).Select(step=>
    path.Position(cut+step)+lateral*offset*Mathf.SmoothStep(0,1,Mathf.Clamp01(step/(float)EungyeTaper))),evidence);
  }
  var connectors=new[]{lanes[Join(0,-1)],lanes[Join(0,0)],lanes[Join(1,0)],lanes[Join(1,1)]};
  var targets=new[]{lanes[connectors[0].next.Single()],exits[0],middle,lanes[connectors[3].next.Single()]};
  for(int i=0;i<tails.Length;i++){
   var join=connectors[i];var tail=tails[i];
   if(i==3)join.fromNode=tail.toNode;
   else {
    Y06EastApproachEdit.Reconnect(join,tail,targets[i],evidence);
    FollowSurface(doc,join);
   }
   tail.next=new[]{join.id};
  }
  for(int i=0;i<heads.Length;i++){
   Y06EastApproachEdit.Assign(heads[i],G05NorthWestMapEdit.OriginalHead(paths[i],paths[i].length-EungyeApproach),evidence);
   heads[i].next=new[]{tails[i*2].id,tails[i*2+1].id};
  }
  doc.lanes=doc.lanes.Concat(tails).Append(middle).ToArray();
  G05NorthWestMapEdit.AddUturn(before,doc,tails[0],lanes,EungyePrefix,Eungye,new[]{"local:7963:1044:7982:1469","local:7887:1047:7906:1473"},evidence);
 }

 static void Validate(ProjectMapPackage package){
  var map=package.Compose();
  EungyeConnectivityRepair.Apply(map,package.baseline);
  var graph=new HdRoadGraph(map);
  var indices=Enumerable.Range(0,graph.edges.Length).ToDictionary(i=>graph.edges[i].id);
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>map.hdEdges[a.edgeIndex].id);
  var expectations=new Dictionary<string,int>{
   {BankInner,LaneDirectionPaint.Left|LaneDirectionPaint.Straight},{BankOuter,LaneDirectionPaint.Right},
   {EungyeInner,LaneDirectionPaint.Left|LaneDirectionPaint.Uturn},{EungyePrefix+"straight-1",LaneDirectionPaint.Straight},
   {EungyePrefix+"straight-2",LaneDirectionPaint.Straight},{EungyePrefix+"right",LaneDirectionPaint.Right}
  };
  foreach(var expected in expectations){
   var lane=graph.edges[indices[expected.Key]];
   Check(lane.next.Aggregate(0,(mask,n)=>mask|UturnMovement.PaintMask(graph.edges[n].turn))==expected.Value,"incorrect allowed movement "+lane.id);
   string paintLane=lane.id==BankOuter?BankOuterPrefix:lane.id;
   Check(arrows[paintLane].Any(a=>a.turns==expected.Value),"missing or conflicting arrow "+paintLane);
   foreach(int next in lane.next)Check(graph.edges[next].next.Length>0,"dead-end exit "+graph.edges[next].id);
   Y07MapTests.Footprint(map,lane);
  }
  foreach(var id in new[]{BankOuterPrefix,BankRight,Join(0,0),Join(1,0),Join(1,1),"join:"+EungyePrefix+"uturn",EungyePrefix+"departure-middle"})
   Y07MapTests.Footprint(map,graph.edges[indices[id]]);
  Check(graph.edges[indices[Join(0,0)]].next.Single()!=graph.edges[indices[Join(1,0)]].next.Single(),"37 through lanes must have distinct exits");
  Check(UturnMovement.Condition(package.document,EungyeInner)?.type==UturnMovement.LeftOrPedestrianGreen,"37 southbound U-turn condition");
  VerifyEungyeDividers(map,graph,indices);
  ConfirmRoutes(map,graph,indices);
  Debug.Log("SOUTHBOUND_SURVEY_OK 39 LS/R; 37 LU/S/S/R to three departure lanes; 39 stops, 1482 routes");
 }

 static void VerifyEungyeDividers(MapData map,HdRoadGraph graph,Dictionary<string,int> indices){
  var paths=new[]{EungyeInner,EungyePrefix+"straight-1",EungyePrefix+"straight-2",EungyePrefix+"right"}.Select(id=>graph.paths[indices[id]]).ToArray();
  var reference=paths[0];float at=reference.length-11;
  var origin=reference.Position(at);var direction=reference.Tangent(at);var right=TrafficSystem.Right(direction);
  var offsets=paths.Select(path=>{
   float error;var point=path.Position(path.Project(origin,out error));
   return Vector3.Dot(point-origin,right);
  }).ToArray();
  var boundaries=Enumerable.Range(1,3).Select(i=>(offsets[i-1]+offsets[i])*.5f).ToArray();
  var drawn=new HashSet<int>();
  RoadPaintGeometry.Paint(map,null,(kind,point,heading,_,__,yellow)=>{
   if(kind!="Lane divider"||Mathf.Abs(Vector3.Dot(heading,direction))<.9f||Mathf.Abs(Vector3.Dot(point-origin,direction))>3.5f)return;
   float lateral=Vector3.Dot(point-origin,right);
   for(int i=0;i<boundaries.Length;i++)if(Mathf.Abs(lateral-boundaries[i])<.55f)drawn.Add(i);
  });
  Check(drawn.Count==3,"37 four-lane approach must render its three lane dividers, got "+drawn.Count);
 }

 static void ConfirmRoutes(MapData map,HdRoadGraph graph,Dictionary<string,int> indices){
  int source=indices[BankHead];
  foreach(var route in new[]{
   (end:"A222BC004193:0",entry:BankInner,turn:BankLeft),
   (end:"A2218G611305:0",entry:BankOuter,turn:BankRight),
   (end:"repair:J1927_5181:south-portal",entry:BankInner,turn:BankStraight)}){
   var result=graph.Route(source,graph.paths[source].length-10,indices[route.end],1);
   Check(result!=null&&result.edges.Contains(indices[route.entry])&&result.edges.Contains(indices[route.turn]),"39 route missing "+route.turn);
  }
  var stops=PassengerStopCatalog.For(map).Stops;
  Check(stops.Length==39,"39 passenger stops");
  int pairs=0;
  foreach(var from in stops)foreach(var to in stops)if(from!=to){
   Check(graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"lost passenger route "+from.name+" → "+to.name);
   pairs++;
  }
  Check(pairs==1482,"all directed passenger routes");
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Check(before.document.revision==37&&before.contentHash==PreviousHash,"exact r37 required");
  var doc=before.document.Copy();
  var lanes=doc.lanes.ToDictionary(l=>l.id);
  PatchBank(doc,lanes);
  PatchEungye(before,doc,lanes);
  doc.revision=38;doc.projectRevision="";doc.savedAt=DateTime.UtcNow.ToString("o");
  var issues=RoadMapValidator.Validate(before.baseline,doc).Where(i=>i.error).ToArray();
  Check(issues.Length==0,string.Join(" / ",issues.Take(12).Select(i=>i+" at "+i.position)));
  var package=ProjectMapPackage.Create(before.baseline,doc);
  Validate(package);
  ProjectMapStore.Save(root,package,token);
  var saved=ProjectMapStore.Load(root);
  Check(saved.contentHash==package.contentHash&&saved.baselineHash==before.baselineHash,"saved map identity");
  Validate(saved);
  ProjectMapBuild.Prepare();
  Debug.Log("SOUTHBOUND_SURVEY_APPLIED revision=38 contentHash="+saved.contentHash);
 }

 public static void BuildReviewed(){
  NativeLeftSignalRegressionTests.Run();
  ApproachSignalHeadTests.Run();
  JunctionRouteFeedbackTests.VerifyMigration();
  FlexibleBollardTests.BuildCommercialMap();
 }
}
