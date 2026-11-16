using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Y05CorridorMapEdit {
 const string PreviousHash="b91b46e9051e5c1427a1ecd7ff40ca7fde370a0120efabe4e4918c63bb6a3634";
 const string EastJunction="J5692_14220";
 const string WestJunction="J4396_13458";
 const string Prefix="reviewed:Y05:";
 const string Gate="service:"+Prefix+"apartment-gate";
 const string Island=Prefix+"west-island";
 const string WestInner="local:5489:14147:4576:13604:part:2";
 const string WestMiddle="local:5458:14200:4544:13656:part:3";
 const string WestOuter="local:5427:14252:4512:13708:part:2:part:reviewed:T1-NE-right";
 const string WestSlip="reviewed:T1-NE-right:lane";
 static readonly string[] EastLabels={"left","straight-1","straight-2","straight-3","straight-right"};
 static readonly string[] EastRoots={"local:4608:13553:5519:14095","local:4640:13501:5550:14042","local:4672:13449:5581:13990"};
 static readonly string[] EastHeads={EastRoots[0]+":part:2",EastRoots[1]+":part:3",EastRoots[2]+":part:2"};

 static void Require(bool result,string message){
  if(!result)throw new InvalidOperationException("Y05_CORRIDOR: "+message);
 }

 static Vector3 Point(float x,float z)=>new Vector3(x,0,z);
 static MapPoint MapPosition(Vector3 p)=>new MapPoint{x=p.x,z=p.z};
 static RoadMapLane Copy(RoadMapLane lane)=>JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));
 static AssistedRoadNetwork.Path Path(RoadMapLane lane)=>new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());
 static string Join(string root,int turn)=>"join:"+root+":"+turn;

 static RoadMapEvidence Evidence(string month,string camera,string note)=>new RoadMapEvidence{
  state="observed",month=month,checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p="+camera+",Float",note=note
 };

 static void Pavement(RoadMapDocument doc,RoadMapLane lane,float width){
  string id="road:polish:"+lane.id;
  var surface=doc.scene.objects.SingleOrDefault(o=>o.id==id);
  if(surface==null){
   surface=new ProjectMapObject{id=id,kind="road",name="계수로 연결 노면",lanes=1,oneWay=true,suppressPaint=true};
   doc.scene.objects=doc.scene.objects.Append(surface).ToArray();
  }
  surface.width=width;surface.points=lane.points;
 }

 static void PatchEast(RoadMapDocument doc,Dictionary<string,RoadMapLane> lanes,HybridJunctionRule junction){
  var evidence=Evidence("2026-05","MlEGsgvMqyj2M7JYnOVUGA,51,-30,80",
   "Marker 44, L to the Umi Lin Lake main gate / S / S / S / SR. Four receiving lanes observed just after the east crosswalk. Taper, curb, gate and signal timings are game estimates.");
  var heads=EastHeads.Select(id=>lanes[id]).ToArray();
  var paths=heads.Select(Path).ToArray();
  const int approach=49;
  var origin=paths[0].points.Last();
  var side=TrafficSystem.Right(paths[0].Tangent(paths[0].length));
  float span=Vector3.Dot(paths[2].points.Last()-origin,side);
  Require(span>11&&span<13&&paths.All(p=>p.length>approach),"44 approach dimensions changed");
  int[] source={0,0,1,2,2};
  var tails=new RoadMapLane[5];
  for(int i=0;i<tails.Length;i++){
   int root=source[i];var path=paths[root];
   var tail=Copy(heads[root]);
   tail.id=Prefix+"east:"+EastLabels[i];tail.number=i+1;tail.width=span/4;
   tail.fromNode=heads[root].toNode;tail.toNode=tail.id+":end";
   float shift=span*i/4-Vector3.Dot(path.points.Last()-origin,side);
   float start=path.length-approach;
   Y06EastApproachEdit.Assign(tail,Enumerable.Range(0,approach+1).Select(step=>
    path.Position(start+step)+side*shift*Mathf.SmoothStep(0,1,Mathf.Clamp01(step/22f))),evidence);
   tails[i]=tail;
  }
  for(int i=0;i<heads.Length;i++){
   var head=heads[i];var path=paths[i];
   Y06EastApproachEdit.Assign(head,G05NorthWestMapEdit.OriginalHead(path,path.length-approach),evidence);
   int root=i;head.next=tails.Where((_,index)=>source[index]==root).Select(t=>t.id).ToArray();
  }
  doc.lanes=doc.lanes.Concat(tails).ToArray();

  string[] straightIds=EastRoots.Select(r=>Join(r,0)).ToArray();
  var landings=straightIds.Select(id=>lanes[lanes[id].next.Single()]).ToArray();
  var inserted=AddEastReceivingLane(doc,lanes,landings[1],landings[2],evidence);
  var third=Copy(lanes[straightIds[2]]);
  third.id="join:"+Prefix+"east:third-through";
  var straightJoins=new[]{lanes[straightIds[0]],lanes[straightIds[1]],third,lanes[straightIds[2]]};
  var receiving=new[]{landings[0],landings[1],inserted,landings[2]};
  for(int i=0;i<straightJoins.Length;i++){
   var join=straightJoins[i];var driving=tails[i+1];
   Y06EastApproachEdit.Reconnect(join,driving,receiving[i],evidence);
   driving.next=new[]{join.id};
   Pavement(doc,join,4.1f);
  }
  var right=lanes[Join(EastRoots[2],1)];
  right.fromNode=tails[4].toNode;
  tails[4].next=tails[4].next.Append(right.id).ToArray();
  foreach(var tail in tails)Pavement(doc,tail,3.8f);
  doc.lanes=doc.lanes.Append(third).ToArray();

  var gate=new RoadMapLane{
   id=Gate,sourceId=Gate,roadName="시흥은계우미린레이크 정문",status="provisional",number=1,width=3.5f,
   fromNode=Gate+":mouth",toNode=Gate+":end"
  };
  Y06EastApproachEdit.Assign(gate,new[]{Point(564.5f,1446.0f),Point(562.8f,1452.2f),Point(561.0f,1458.7f)},evidence);
  var left=Copy(straightJoins[0]);left.id="join:"+Prefix+"east:apartment-left";left.turn=-1;
  Y06EastApproachEdit.Reconnect(left,tails[0],gate,evidence);
  tails[0].next=new[]{left.id};
  doc.lanes=doc.lanes.Concat(new[]{left,gate}).ToArray();
  Pavement(doc,left,4.6f);Pavement(doc,gate,4.6f);
  ClearGateCurb(doc,left,gate);
  AddSignals(doc,junction,tails,lanes,evidence);
 }

 static RoadMapLane AddEastReceivingLane(RoadMapDocument doc,Dictionary<string,RoadMapLane> lanes,
  RoadMapLane middle,RoadMapLane outer,RoadMapEvidence evidence){
  var tail=lanes[outer.id+":part:2"];
  var original=new AssistedRoadNetwork.Path(new[]{outer,lanes[outer.id+":part:1"],tail}
   .SelectMany((l,i)=>l.points.Skip(i==0?0:1)).Select(p=>p.V()).ToArray());
  var offset=(middle.points[0].V()-outer.points[0].V())*.5f;
  var inserted=Copy(outer);
  inserted.id=Prefix+"east:receiving-3";inserted.number=3;inserted.width=3.1f;
  inserted.fromNode=inserted.id+":start";inserted.toNode=tail.toNode;
  inserted.next=new[]{"reviewed:Y06:east:straight-3"};
  int steps=Mathf.CeilToInt(original.length);
  Y06EastApproachEdit.Assign(inserted,Enumerable.Range(0,steps+1).Select(i=>{
   float at=original.length*i/steps;
   float scale=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((at-original.length+21)/21f));
   return original.Position(at)+offset*scale;
  }),evidence);
  foreach(var piece in new[]{outer,lanes[outer.id+":part:1"],tail})piece.number=4;
  doc.lanes=doc.lanes.Append(inserted).ToArray();
  Pavement(doc,inserted,3.8f);
  return inserted;
 }

 static void ClearGateCurb(RoadMapDocument doc,params RoadMapLane[] gatePaths){
  // Only the generated curb panels intersecting the new private opening are replaced.
  var paths=gatePaths.Select(Path).ToArray();
  foreach(var panel in doc.scene.objects.Where(o=>o.enabled&&o.id.StartsWith("sidewalk:generated:")&&
   o.points.Any(p=>p.x>550&&p.x<577&&p.z>1426&&p.z<1460))){
   if(paths.Any(path=>panel.points.Any(p=>{path.Project(p.V(),out float error);return error<2.55f;})))
    panel.enabled=false;
  }
 }

 static void AddSignals(RoadMapDocument doc,HybridJunctionRule junction,RoadMapLane[] east,
  Dictionary<string,RoadMapLane> lanes,RoadMapEvidence evidence){
  var groups=new List<RoadMapSignalGroup>();
  var stages=new List<RoadMapStage>();
  var facilities=new List<RoadMapFacility>();
  var forward=new Vector3(junction.dx,0,junction.dz).normalized;
  var branches=new[]{
   (name:"동행",axis:0,sign:1,incoming:east,turns:new[]{0,-1,1},green:22f),
   (name:"서행",axis:0,sign:-1,incoming:new[]{"left","straight-1","straight-2","straight-3"}
    .Select(id=>lanes["reviewed:Y06:west:"+id]).ToArray(),turns:new[]{0,-1},green:22f),
   (name:"은계호수로",axis:1,sign:-1,incoming:new[]{lanes.Values.Single(l=>!l.disabled&&l.next.Contains("join:local:6326:13507:5852:14076:-1"))},
    turns:new[]{0,-1,1},green:14f)
  };
  foreach(var branch in branches){
   var dir=(branch.axis==0?forward:TrafficSystem.Right(forward))*branch.sign;
   var first=branch.incoming.First();var last=branch.incoming.Last();
   var center=(first.points.Last().V()+last.points.Last().V())*.5f;
   var cross=TrafficSystem.Right(dir);
   float width=Mathf.Abs(Vector3.Dot(last.points.Last().V()-first.points.Last().V(),cross))+3.35f;
   string key=Prefix+"44:"+branch.axis+":"+branch.sign;
   var through=branch.turns.Select(turn=>new RoadMapSignalGroup{
    id=key+":vehicle:"+turn,axis=branch.axis,sign=branch.sign,turn=turn,name=branch.name
   }).ToArray();
   groups.AddRange(through);
   stages.Add(new RoadMapStage{name=branch.name,green=branch.green,groups=through.Select(g=>g.id).ToArray()});
   RoadMapFacility Mark(string suffix,string kind,Vector3 p,float w,float length)=>new RoadMapFacility{
    id=key+suffix,kind=kind,junctionId=EastJunction,axis=branch.axis,sign=branch.sign,
    x=p.x,z=p.z,yaw=Quaternion.LookRotation(dir).eulerAngles.y,width=w,length=length,evidence=evidence
   };
   var stop=Mark(":stop","stopLine",center-dir,Mathf.Max(3.5f,width),.4f);stop.laneId=first.id;
   var zebra=Mark(":zebra","crosswalk",center+dir*3.7f,branch.axis==0?31f:8.5f,3.4f);
   var lamp=Mark(":signal","vehicleSignal",center+dir*14+cross*(width*.5f+1),3.1f,.3f);
   lamp.height=5.2f;lamp.groupId=through[0].id;lamp.targetId=stop.id;lamp.laneId=first.id;
   var walk=new RoadMapSignalGroup{id=key+":walk",crosswalkId=zebra.id,name="보행 "+branch.name,
    pedestrian=true,axis=branch.axis,sign=branch.sign};
   groups.Add(walk);facilities.AddRange(new[]{stop,zebra,lamp});
  }
  stages.Add(new RoadMapStage{name="보행",green=25f,yellow=0,groups=groups.Where(g=>g.pedestrian).Select(g=>g.id).ToArray()});
  doc.signals=doc.signals.Append(new RoadMapSignalProgram{junctionId=EastJunction,groups=groups.ToArray(),stages=stages.ToArray()}).ToArray();
  doc.facilities=doc.facilities.Concat(facilities).ToArray();
 }

 static void PatchWest(RoadMapDocument doc,Dictionary<string,RoadMapLane> lanes){
  var evidence=Evidence("2024-03","_m3EZH519faVIC8zNyKK5A,-151,-34,74",
   "Marker 45: user-confirmed left/U, straight, straight and separated right via the pedestrian island. Existing U-turn timing is retained. The island and slip curb are game estimates.");
  var inside=lanes[WestInner];var middle=lanes[WestMiddle];var outside=lanes[WestOuter];
  var oldInner=lanes[Join("local:5489:14147:4576:13604",0)];
  var oldMiddle=lanes[Join("local:5458:14200:4544:13656",0)];
  inside.next=inside.next.Where(id=>id!=oldInner.id).ToArray();
  oldMiddle.disabled=true;oldMiddle.next=Array.Empty<string>();
  middle.next=new[]{oldInner.id};
  Y06EastApproachEdit.Reconnect(oldInner,middle,lanes[oldInner.next.Single()],evidence);
  Pavement(doc,oldInner,4.1f);
  Require(lanes[outside.next.Single()].next.Single()!=oldInner.next.Single(),"45 straight exits must differ");

  var slip=lanes[WestSlip];var slipPath=Path(slip);
  var offset=TrafficSystem.Right(slipPath.Tangent(0))*1.8f;
  Y06EastApproachEdit.Assign(slip,Enumerable.Range(0,31).Select(i=>{
   float at=slipPath.length*i/30;
   return slipPath.Position(at)+offset*Mathf.SmoothStep(0,1,Mathf.Clamp01(at/10f));
  }),evidence);
  var right=lanes[slip.next.Single()];
  Y06EastApproachEdit.Reconnect(right,slip,lanes[right.next.Single()],evidence);
  foreach(var lane in new[]{slip,right})Pavement(doc,lane,4.05f);
  var island=new ProjectMapObject{id=Island,kind="sidewalk",name="계수로 서행 우회전 보행섬",height=.14f,
   points=new[]{Point(446f,1370.63f),Point(450f,1373.14f),Point(455f,1376.25f),
    Point(455f,1375.67f),Point(450f,1372.58f)}.Select(MapPosition).ToArray()};
  doc.scene.objects=doc.scene.objects.Append(island).ToArray();
  var rightPath=Path(right);float at=rightPath.length*.54f;var crossing=rightPath.Position(at);
  doc.facilities=doc.facilities.Append(new RoadMapFacility{
   id=Island+":crosswalk",kind="crosswalk",type="황색",width=4f,length=2.5f,evidence=evidence,
   x=crossing.x,z=crossing.z,yaw=Quaternion.LookRotation(rightPath.Tangent(at)).eulerAngles.y
  }).ToArray();
 }

 public static void Verify(){
  Validate(ProjectMapStore.Load(ProjectMapBuild.Root));
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var previous=ProjectMapStore.Load(root);
  Require(previous.document.revision==40&&previous.contentHash==PreviousHash,"exact r40 starting map required");
  var doc=previous.document.Copy();
  var lanes=doc.lanes.ToDictionary(l=>l.id);
  PatchEast(doc,lanes,previous.baseline.junctionRules.Single(j=>j.id==EastJunction));
  PatchWest(doc,lanes);
  doc.revision=41;doc.projectRevision="";doc.savedAt=DateTime.UtcNow.ToString("o");
  var issues=RoadMapValidator.Validate(previous.baseline,doc).Where(issue=>issue.error).ToArray();
  Require(issues.Length==0,string.Join(" / ",issues.Take(12).Select(i=>i+" at "+i.position.ToString("F2"))));
  var package=ProjectMapPackage.Create(previous.baseline,doc);
  Validate(package);
  ProjectMapStore.Save(root,package,token);
  var saved=ProjectMapStore.Load(root);
  Require(saved.contentHash==package.contentHash&&saved.baselineHash==previous.baselineHash,"saved map identity");
  Validate(saved);
  ProjectMapBuild.Prepare();
  Debug.Log("Y05_CORRIDOR_APPLIED revision=41 contentHash="+saved.contentHash);
 }

 public static void BuildReviewed(){
  Verify();
  JunctionRouteFeedbackTests.VerifyMigration();
  FlexibleBollardTests.BuildCommercialMap();
 }

 static void Validate(ProjectMapPackage package){
  var map=package.Compose();
  EungyeConnectivityRepair.Apply(map,package.baseline);
  var graph=new HdRoadGraph(map);
  var ids=Enumerable.Range(0,graph.edges.Length).Where(i=>!graph.edges[i].disabled).ToDictionary(i=>graph.edges[i].id);
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>a.edgeIndex);
  var east=EastLabels.Select(label=>Prefix+"east:"+label).ToArray();
  Require(east.All(ids.ContainsKey)&&ids.ContainsKey(Gate),"44 lacks five authored lanes or the apartment gate connection");
  var west=new[]{WestInner,WestMiddle,WestOuter,WestSlip};

  void Movement(string id,int number,int mask,string junction){
   var index=ids[id];var lane=graph.edges[index];
   Require(lane.number==number&&lane.ruleId==junction,"wrong order or junction "+id);
   Require(lane.next.Aggregate(0,(m,n)=>m|UturnMovement.PaintMask(graph.edges[n].turn))==mask,"wrong movement "+id);
   Require(arrows[index].Any(a=>a.turns==mask),"missing or incorrect direction arrow "+id);
   Y07MapTests.Footprint(map,lane);
   foreach(int n in lane.next)Y07MapTests.Footprint(map,graph.edges[n]);
  }
  var eastMasks=new[]{LaneDirectionPaint.Left,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight,
   LaneDirectionPaint.Straight,LaneDirectionPaint.Straight|LaneDirectionPaint.Right};
  var westMasks=new[]{LaneDirectionPaint.Left|LaneDirectionPaint.Uturn,LaneDirectionPaint.Straight,
   LaneDirectionPaint.Straight,LaneDirectionPaint.Right};
  for(int i=0;i<east.Length;i++)Movement(east[i],i+1,eastMasks[i],EastJunction);
  for(int i=0;i<west.Length;i++)Movement(west[i],i+1,westMasks[i],WestJunction);
  Require(graph.edges[graph.edges[ids[east[0]]].next.Single()].next.SequenceEqual(new[]{ids[Gate]}),"44 left must land at the apartment entrance");
  Require(graph.Route(ids[east[0]],5,ids[Gate],5)!=null,"44 has a route into the apartment entrance");
  var eastLandings=east.Skip(1).Select(id=>graph.edges[ids[id]].next.Single(i=>graph.edges[i].turn==0))
   .Select(i=>graph.edges[i].next.Single()).ToArray();
  Require(eastLandings.Distinct().Count()==4,"44 through lanes have independent receiving lanes");
  var program=package.document.signals.Single(p=>p.junctionId==EastJunction);
  Require(map.junctionRules.Single(j=>j.id==EastJunction).signalMode>0,"44 uses a runtime vehicle signal");
  bool protectedGreen=false,protectedRed=false;
  for(float at=0;at<program.Cycle;at+=.5f){
   bool green=program.Movement(0,1,-1,at)==SignalColor.Green;
   protectedGreen|=green;protectedRed|=!green;
   if(green)Require(program.Movement(0,-1,0,at)!=SignalColor.Green&&!program.Walk(0,1,at),
    "44 left conflicts with oncoming traffic or pedestrians");
  }
  Require(protectedGreen&&protectedRed,"44 left receives a dedicated protected green");
  var westLandings=west.Skip(1).Take(2).Select(id=>graph.edges[graph.edges[ids[id]].next.Single()].next.Single()).ToArray();
  Require(westLandings.Distinct().Count()==2,"45 uses two independent receiving lanes");
  Require(UturnMovement.Condition(package.document,WestInner)?.type==UturnMovement.LeftOrPedestrianGreen,"45 retains the U-turn signal condition");
  Require(package.document.scene.objects.Any(o=>o.id==Island&&o.enabled&&o.kind=="sidewalk"),"45 lacks a pedestrian island");
  var stops=PassengerStopCatalog.For(map).Stops;
  Require(stops.Length==39,"39 passenger stops retained");
  foreach(var a in stops)foreach(var b in stops)if(a!=b)
   Require(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)!=null,"lost passenger route "+a.name+" → "+b.name);
  Debug.Log("Y05_CORRIDOR_OK 44 apartment-left/three straight/straight-right, 45 left-U/two straight/island right, 39 stops 1482 routes");
 }
}
