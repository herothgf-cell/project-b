using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class P03WestRoadMapEdit {
 const string PreviousHash="353fe15fe9f383001e93deafe3905f983a3fe19c0e5f47811e17e85efcf245e7";
 const string Junction="J1919_877", SideJunction="J1516_974";
 const string Prefix="reviewed:P03:38:", RoadId="reviewed:P03:geom-bawi-1";
 const string EastIn="editor:segment:bG9jYWw6NDk0NDo5Mjg6MjA0OTo5Mjg:pink-roundabout";
 const string ThroughRight="editor:segment:bG9jYWw6NDk0NDo5Mjg6MjA0OTo5Mjg:p03-through-right";
 const string EastRoot="local:4944:928:2049:928", EastOut="local:2050:827:4943:827";
 const string NorthIn="local:1844:1310:1885:1004", NorthOut="local:1919:1008:1879:1313";
 const string SouthIn="local:2445:30:2004:778", SouthOut="local:1940:738:2373:9";
 const string SideIn="local:1220:14:1524:950", SideOut="local:1495:959:1192:24";
 const int Approach=48;
 const float Taper=21, InnerShift=3.15f;

 static void Check(bool condition,string reason){if(!condition)throw new InvalidOperationException("P03_38: "+reason);}
 static RoadMapEvidence Evidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=2bDxoMnEmxlrzWaEO8Z_tg,-80,10,78,Float",
  note="Marker 38: user confirmed westbound left-only inside and through/right outside. May 2026 shows three-aspect permissive-left heads and raised yellow crossings. vqrKXcxD0mAE1coYjjbQ0Q, lhcegjd_exwoAg_jemwO_g and PXrR7fbzEcYjSdoMClJqqg show Geombawi 1-ro reaching Geombawi 2-ro. Width, approach stations, stop and phase timing are game estimates."
 };
 static RoadMapLane Copy(RoadMapLane lane)=>JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));
 static MapPoint Point(Vector3 p)=>new MapPoint{x=p.x,z=p.z};
 static Vector3 V(float x,float z)=>new Vector3(x,0,z);

 static RoadMapLane AddLink(List<RoadMapLane> added,RoadMapLane from,RoadMapLane to,string label,int turn,string junction,RoadMapEvidence evidence){
  var link=Copy(from);
  link.id="join:"+Prefix+label;link.turn=turn;link.ruleId=junction;
  Y06EastApproachEdit.Reconnect(link,from,to,evidence);
  from.next=from.next.Append(link.id).ToArray();
  added.Add(link);
  return link;
 }
 static RoadMapLane WestLane(string id,string rule,IEnumerable<Vector3> points,RoadMapEvidence evidence){
  var lane=new RoadMapLane{id=Prefix+id,sourceId=RoadId,roadName="검바위1로",width=2.8f,number=1,status="provisional",
   ruleId=rule,fromNode=Prefix+id+":start",toNode=Prefix+id+":end",next=Array.Empty<string>()};
  Y06EastApproachEdit.Assign(lane,points,evidence);
  return lane;
 }

 static void AddFacilities(RoadMapDocument d,HybridJunctionRule rule,RoadMapLane[] incoming,RoadMapLane outer,RoadMapEvidence evidence){
  var additions=new List<RoadMapFacility>();
  var main=new Vector3(rule.dx,0,rule.dz).normalized;
  var widths=new[]{13.8f,6f,7f,11f};
  for(int i=0;i<incoming.Length;i++){
   var lane=incoming[i];
   var heading=RoadMapEasySelection.Heading(lane);
   int axis=Mathf.Abs(Vector3.Dot(heading,main))>.7f?0:1;
   int sign=Vector3.Dot(heading,axis==0?main:TrafficSystem.Right(main))>=0?1:-1;
   Vector3 direction=(axis==0?main:TrafficSystem.Right(main))*sign;
   var end=lane.points.Last().V();
   var stop=i==0?(end+outer.points.Last().V())*.5f:end;
   var crossing=rule.V()-direction*(Vector3.Dot(rule.V()-stop,direction)-2.65f);
   float yaw=Quaternion.LookRotation(direction).eulerAngles.y;
   string key=Prefix+axis+":"+sign;
   additions.Add(new RoadMapFacility{id=key+":stop",kind="stopLine",junctionId=Junction,axis=axis,sign=sign,
    x=stop.x,z=stop.z,yaw=yaw,width=i==0?6.6f:3.35f,length=.38f,evidence=evidence});
   additions.Add(new RoadMapFacility{id=key+":crosswalk",kind="crosswalk",type="황색",junctionId=Junction,axis=axis,sign=sign,
    x=crossing.x,z=crossing.z,yaw=yaw,width=widths[i],length=2.65f,evidence=evidence});
   additions.Add(new RoadMapFacility{id=key+":head",kind="signalHead",type="3구",junctionId=Junction,
    axis=axis,sign=sign,x=stop.x,z=stop.z,yaw=yaw,evidence=evidence});
   additions.Add(new RoadMapFacility{id=key+":yield-left",kind="sign",type=RoadMapRules.PermissiveLeftSign,rule=true,
    laneId=lane.id,junctionId=Junction,axis=axis,sign=sign,x=end.x,z=end.z,yaw=yaw,evidence=evidence});
  }
  d.facilities=d.facilities.Concat(additions).ToArray();
 }

 static ProjectMapPackage Preview(ProjectMapPackage before){
  Check(before.document.revision==36&&before.contentHash==PreviousHash,"exact r36 required");
  var baseline=JsonUtility.FromJson<MapData>(JsonUtility.ToJson(before.baseline));
  var d=before.document.Copy();
  var lanes=d.lanes.ToDictionary(l=>l.id);
  var added=new List<RoadMapLane>();
  var evidence=Evidence();
  var rule=baseline.junctionRules.Single(j=>j.id==Junction);
  Vector3 s=baseline.junctionRules.Single(j=>j.id==SideJunction).V();
  Vector3 j=rule.V();
  var westCentre=new[]{s,V(160,94.2f),V(171,90.8f),V(179.6f,89),j};
  var westPath=new AssistedRoadNetwork.Path(westCentre);
  var westPositions=Enumerable.Range(0,30).Select(i=>6.8f+(westPath.length-16.6f)*i/29).ToArray();
  Vector3 Offset(float at,float metres)=>westPath.Position(at)+TrafficSystem.Right(westPath.Tangent(at))*metres;
  var westOut=WestLane("westbound",SideJunction,westPositions.Reverse().Select(at=>Offset(at,-1.5f)),evidence);
  var westIn=WestLane("eastbound",Junction,Enumerable.Range(0,30).Select(i=>Offset(7.6f+(westPath.length-17.4f)*i/29,1.5f)),evidence);
  added.Add(westIn);added.Add(westOut);

  var eastHead=lanes[EastIn];
  var eastPath=new AssistedRoadNetwork.Path(eastHead.points.Select(p=>p.V()).ToArray());
  Check(eastPath.length>Approach,"east entry length");
  var eastRight=TrafficSystem.Right(eastPath.Tangent(eastPath.length));
  float cut=eastPath.length-Approach;
  var outer=Copy(eastHead);outer.id=ThroughRight;outer.number=2;outer.sourceOffset+=cut;
  var inner=Copy(eastHead);inner.id=Prefix+"left";inner.number=1;
  foreach(var tail in new[]{inner,outer}){
   float shift=tail==inner?-InnerShift:0;
   tail.fromNode=eastHead.toNode;tail.toNode=tail.id+":end";
   Y06EastApproachEdit.Assign(tail,Enumerable.Range(0,Approach+1).Select(i=>
    eastPath.Position(cut+i)+eastRight*shift*Mathf.SmoothStep(0,1,Mathf.Clamp01(i/Taper))),evidence);
   added.Add(tail);
  }
  var left=lanes["join:"+EastRoot+":-1"];
  var right=lanes["join:"+EastRoot+":1"];
  Y06EastApproachEdit.Reconnect(left,inner,lanes[SouthOut],evidence);
  d.scene.objects.Single(o=>o.id=="road:polish:"+left.id).points=left.points;
  right.fromNode=outer.toNode;
  inner.next=new[]{left.id};outer.next=new[]{right.id};
  eastHead.next=new[]{inner.id,outer.id};
  Y06EastApproachEdit.Assign(eastHead,G05NorthWestMapEdit.OriginalHead(eastPath,cut),evidence);

  AddLink(added,outer,westOut,"westbound-through",0,Junction,evidence);
  AddLink(added,westIn,lanes[EastOut],"eastbound-through",0,Junction,evidence);
  AddLink(added,westIn,lanes[NorthOut],"eastbound-left",-1,Junction,evidence);
  AddLink(added,lanes[SouthIn],westOut,"northbound-left",-1,Junction,evidence);
  var sideIn=lanes[SideIn];var sideOut=lanes[SideOut];
  Check(sideIn.next.Length==0&&!d.lanes.Any(l=>l.next.Contains(SideOut)),"original western endpoint topology");
  Y06EastApproachEdit.Assign(sideIn,sideIn.points.Take(sideIn.points.Length-3).Select(p=>p.V()),evidence);
  AddLink(added,westOut,sideOut,"to-geom-bawi-2",-1,SideJunction,evidence);
  AddLink(added,sideIn,westIn,"from-geom-bawi-2",1,SideJunction,evidence);
  d.lanes=d.lanes.Concat(added).ToArray();
  var road=new ProjectMapObject{id="road:"+RoadId,kind="road",name="검바위1로",width=6f,lanes=2,
   suppressPaint=true,points=new[]{V(145.7f,98f)}.Concat(westCentre).Select(Point).ToArray()};
  var mouth=new ProjectMapObject{id="road:"+RoadId+":side-mouth",kind="road",name="검바위2로 접속부",
   width=4.1f,lanes=1,suppressPaint=true,points=new[]{V(150.4f,90.3f),V(154.5f,93.1f),V(156,95.6f)}.Select(Point).ToArray()};
  d.scene.objects=d.scene.objects.Concat(new[]{road,mouth}).ToArray();
  // These placeholder sidewalk panels closed the open mouth of Geombawi 2-ro.
  foreach(string id in new[]{"sidewalk:generated:1854","sidewalk:generated:1855","sidewalk:generated:1856","sidewalk:generated:1857","sidewalk:generated:2496"})
   d.scene.objects.Single(o=>o.id==id).enabled=false;
  rule.signalMode=2;rule.crosswalk=true;rule.stopLine=true;
  rule.facilityStatus="verified";rule.facilityEvidence=evidence.url;rule.facilityCheckedAt=evidence.checkedAt;
  AddFacilities(d,rule,new[]{inner,westIn,lanes[NorthIn],lanes[SouthIn]},outer,evidence);
  d.baseHash=ProjectMapPackage.Digest(JsonUtility.ToJson(baseline));
  d.revision=37;d.projectRevision="";
  return ProjectMapPackage.Create(baseline,d);
 }

 static void Validate(ProjectMapPackage package){
  var map=package.Compose();
  EungyeConnectivityRepair.Apply(map,package.baseline);
  var graph=new HdRoadGraph(map);
  int Index(string id)=>Array.FindIndex(graph.edges,e=>e.id==id);
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>map.hdEdges[a.edgeIndex].id);
  foreach(var pair in new[]{(id:Prefix+"left",mask:LaneDirectionPaint.Left),(id:ThroughRight,mask:LaneDirectionPaint.Straight|LaneDirectionPaint.Right)}){
   var edge=graph.edges[Index(pair.id)];
   Check(edge.next.Aggregate(0,(mask,n)=>mask|UturnMovement.PaintMask(graph.edges[n].turn))==pair.mask,"wrong lane exits: "+pair.id);
   Check(arrows[pair.id].Any(a=>a.turns==pair.mask),"missing lane arrows: "+pair.id);
  }
  foreach(var edge in graph.edges.Where(e=>e.id==ThroughRight||e.id.StartsWith(Prefix)||e.id.StartsWith("join:"+Prefix)))Y07MapTests.Footprint(map,edge);
  int outer=Index(ThroughRight),south=Index(SideOut),northbound=Index(SideIn),east=Index(EastOut);
  var westRoute=graph.Route(outer,2,south,15);
  var eastRoute=graph.Route(northbound,graph.paths[northbound].length-15,east,15);
  Check(westRoute!=null&&westRoute.edges.Contains(Index("join:"+Prefix+"westbound-through")),"westbound route reaches Geombawi 2-ro");
  Check(eastRoute!=null&&eastRoute.edges.Contains(Index("join:"+Prefix+"eastbound-through")),"Geombawi 2-ro returns to Eungyenam-ro");
  var catalog=PassengerStopCatalog.For(map);
  var stops=catalog.Stops;
  Check(stops.Length==39,"39 passenger stops, found "+stops.Length+": "+string.Join(" / ",catalog.Rejected));
  Check(catalog.Entries.Any(e=>e.StopId=="EG-GAME-02-B"&&map.hdEdges[e.Stop.laneEdge].id==ThroughRight
   &&Vector3.Distance(e.Stop.V(),V(232.99628f,92.88786f))<.03f),"existing opposite pickup stays on the original exterior kerb");
  int pairs=0;
  foreach(var from in stops)foreach(var to in stops)if(from!=to){
   Check(graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"lost passenger route "+from.name+" → "+to.name);
   pairs++;
  }
  Check(pairs==1482,"all directed passenger routes");
  ValidateSignal(map);
  Debug.Log("P03_38_VALIDATED left / straight-right; Geombawi 1-ro connected in both directions; 39 stops, 1482 routes");
 }

 static void ValidateSignal(MapData map){
  var d=map.editorDocument;
  var rule=map.junctionRules.Single(j=>j.id==Junction);
  var junction=new TrafficJunction{center=rule.V(),name=Junction,facility=rule,signalized=true,
   leftTurnMode=LeftTurnMode.ProtectedShared,direction=new Vector3(rule.dx,0,rule.dz).normalized};
  var host=new GameObject("38 signal regression");
  var blocker=new GameObject("38 oncoming traffic");
  try{
   var system=host.AddComponent<TrafficSystem>();
   typeof(TrafficSystem).GetField("roadMap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(system,map);
   system.manualClock=true;
   junction.gameProgram=DriveFlowPolicy.Signals(system,junction);
   var plates=d.facilities.Where(f=>f.junctionId==Junction&&f.type==RoadMapRules.PermissiveLeftSign).ToArray();
   Check(plates.Length==4&&junction.gameProgram.groups.All(g=>g.turn!=-1),"each approach is permissive with no protected phase");
   foreach(var plate in plates)Check(SignalHeadPresentation.Generated(map,junction,plate.axis,plate.sign).lamps==3,"three-aspect signals");
   var westbound=plates.Single(p=>p.laneId==Prefix+"left");
   int axis=westbound.axis,sign=westbound.sign;bool green=false,red=false;
   for(float t=0;t<62;t+=.5f){
    system.manualTime=t;var through=system.Signal(junction,axis,sign);
    green|=through==SignalColor.Green;red|=through==SignalColor.Red;
    Check(system.LeftEntrySignal(junction,axis,sign,westbound.laneId)==through&&system.LeftSignal(junction,axis,sign)==SignalColor.Red,
     "westbound permissive left follows through green without a left lamp");
   }
   Check(green&&red,"westbound stage changes");
   var heading=system.AxisDirection(junction,axis)*sign;
   blocker.transform.SetPositionAndRotation(junction.center+heading*4,Quaternion.LookRotation(-heading));
   blocker.layer=9;var box=blocker.AddComponent<BoxCollider>();Physics.SyncTransforms();
   Check(!system.YieldClear(junction,heading,-1,host),"westbound left yields to oncoming traffic");
   box.enabled=false;Physics.SyncTransforms();
   Check(system.YieldClear(junction,heading,-1,host),"westbound left may proceed on clear green");
  }finally{
   UnityEngine.Object.DestroyImmediate(blocker);
   UnityEngine.Object.DestroyImmediate(host);
  }
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var package=Preview(ProjectMapStore.Load(root));
  Validate(package);
  ProjectMapStore.Save(root,package,token);
  var loaded=ProjectMapStore.Load(root);
  Check(loaded.contentHash==package.contentHash&&loaded.baselineHash==package.baselineHash,"saved map identity");
  Validate(loaded);
  ProjectMapBuild.Prepare();
  Debug.Log("P03_38_APPLIED revision=37 contentHash="+loaded.contentHash+" baselineHash="+loaded.baselineHash);
 }

 public static void BuildReviewed(){
  JunctionRouteFeedbackTests.VerifyMigration();
  FlexibleBollardTests.BuildCommercialMap();
 }
}
