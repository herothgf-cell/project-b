using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ParkingExitAndDaeunMapEdit {
 const string PreviousHash="56b01ecd46c68cb164410622d5a340c37f7e105e44b92cac03b73e10a8e3468f";
 const string Parking="J6481_3341", Daeun="J3516_12881";
 const string Exit="reviewed:P20:parking-exit", Prefix="reviewed:Y04:NE:";
 const string East="join:local:5788:2694:6370:3328:0", West="join:local:7241:2687:6592:3354:0";
 const string Root="local:2721:12299:3399:12760", Outer="local:2757:12242:3437:12704";
 const int Approach=32, Taper=20;
 static readonly string Centre=SegmentId(Root,"y04-ne-straight");

 static void Check(bool pass,string reason){if(!pass)throw new InvalidOperationException("PARKING_DAEUN: "+reason);}
 static RoadMapLane Copy(RoadMapLane lane)=>JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));
 static AssistedRoadNetwork.Path Path(RoadMapLane lane)=>new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());
 static Vector3 V(float x,float z)=>new Vector3(x,0,z);
 static MapPoint Point(Vector3 p)=>new MapPoint{x=p.x,z=p.z};
 static string SegmentId(string origin,string name)=>"editor:segment:"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(origin))
  .TrimEnd('=').Replace('+','-').Replace('/','_')+":"+name;

 static RoadMapLane SplitLanding(RoadMapDocument doc,RoadMapLane lane,float cut,string label,RoadMapEvidence evidence){
  var path=Path(lane);var tail=Copy(lane);
  tail.id=SegmentId(lane.id,"parking-exit-"+label);
  tail.fromNode=Exit+":"+label;
  tail.sourceOffset+=cut;
  var points=new List<Vector3>{path.Position(cut)};
  float at=0;
  for(int i=1;i<path.points.Length;i++){
   at+=Vector3.Distance(path.points[i-1],path.points[i]);
   if(at>cut+.001f)points.Add(path.points[i]);
  }
  Y06EastApproachEdit.Assign(tail,points,evidence);
  Y06EastApproachEdit.Assign(lane,G05NorthWestMapEdit.OriginalHead(path,cut),evidence);
  lane.toNode=tail.fromNode;lane.next=new[]{tail.id};
  doc.lanes=doc.lanes.Append(tail).ToArray();
  return tail;
 }

 static void PatchParking(ProjectMapPackage package,RoadMapDocument doc,Dictionary<string,RoadMapLane> lanes){
  var evidence=new RoadMapEvidence{
   state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=false,
   url="https://map.naver.com/p?c=19.00,0,0,0,adh&p=RHKYG5qaPX6x-WSGz07_qw,-82.56,-20,80,Float",
   note="Marker 40. May 2026 and October 2025 show an exit gate and anti-entry X. Model the observed outbound driveway with both exits and no turn designation as requested. Geometry is a game estimate."
  };
  var east=lanes[East];var west=lanes[West];
  Check(!doc.facilities.Any(f=>f.laneId==East||f.laneId==West)&&!doc.scene.objects.Any(o=>o.IsService&&(o.laneId==East||o.laneId==West))&&
   !package.baseline.stops.Any(s=>s.graphStop&&(package.baseline.hdEdges[s.laneEdge].id==East||package.baseline.hdEdges[s.laneEdge].id==West)),"parking connector has a bound destination");
  var eastLanding=SplitLanding(doc,east,16,"east",evidence);
  var westLanding=SplitLanding(doc,west,15,"west",evidence);
  var lead=new RoadMapLane{id=Exit,sourceId=Exit,ruleId=Parking,status="provisional",roadName="은계 공영주차장 출구",
   width=3.35f,number=1,fromNode=Exit+":lot",toNode=Exit+":mouth",next=new[]{"join:"+Exit+":left","join:"+Exit+":right"}};
  var points=new[]{V(645.4f,313.5f),V(645.6f,319.5f),V(645.9f,325.3f)};
  Y06EastApproachEdit.Assign(lead,points,evidence);
  var turns=new[]{Copy(lead),Copy(lead)};
  turns[0].id=lead.next[0];turns[0].turn=-1;Y06EastApproachEdit.Reconnect(turns[0],lead,westLanding,evidence);
  turns[1].id=lead.next[1];turns[1].turn=1;Y06EastApproachEdit.Reconnect(turns[1],lead,eastLanding,evidence);
  doc.lanes=doc.lanes.Append(lead).Concat(turns).ToArray();
  var center=package.baseline.junctionRules.Single(j=>j.id==Parking).V();
  var opening=new ProjectMapObject{id="road:"+Exit,kind="road",name=lead.roadName,width=5.8f,lanes=1,suppressPaint=true,
   points=points.Append(center).Select(Point).ToArray()};
  doc.scene.objects=doc.scene.objects.Append(opening).ToArray();
  foreach(string id in new[]{"sidewalk:generated:5881","sidewalk:generated:6736"})
   doc.scene.objects.Single(o=>o.id==id).enabled=false;
  doc.facilities=doc.facilities.Append(new RoadMapFacility{
   id=Exit+":no-arrow",kind="lanePresentation",type=LaneDirectionPaint.NoArrowsType,rule=false,laneId=Exit,
   junctionId=Parking,x=points[0].x,z=points[0].z,evidence=evidence
  }).ToArray();
 }

 static void PatchDaeun(ProjectMapPackage package,RoadMapDocument doc,Dictionary<string,RoadMapLane> lanes){
  var evidence=new RoadMapEvidence{
   state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
   url="https://map.naver.com/p?c=19.00,0,0,0,adh&p=KXXu8EQoR0rF07lYls7WKw,69,-17,80,Float",
   note="Marker 41 northeast approach: May 2026 and December 2025 show three lanes and permissive left. User verified through and through/right in the other lanes. Length, taper, and centers are game estimates."
  };
  var head=lanes[Root+":part:2"];var outer=lanes[Outer+":part:2"];
  var path=Path(head);float cut=path.length-Approach;
  Check(cut>0&&!doc.facilities.Any(f=>f.laneId==head.id)&&!doc.scene.objects.Any(o=>o.IsService&&o.laneId==head.id&&o.laneAt>=cut),
   "41 center lane split requires free approach");
  var left=Copy(head);left.id=Prefix+"left";
  var middle=Copy(head);middle.id=Centre;middle.sourceOffset+=cut;
  left.fromNode=middle.fromNode=Prefix+"fork";
  left.toNode=left.id+":end";
  left.next=new[]{"join:"+Root+":-1"};middle.next=new[]{"join:"+Root+":0"};
  middle.number=2;outer.number=3;
  Y06EastApproachEdit.Assign(left,Enumerable.Range(0,Approach+1).Select(i=>{
   float at=cut+i;
   return path.Position(at)-TrafficSystem.Right(path.Tangent(at))*3.15f*Mathf.SmoothStep(0,1,Mathf.Clamp01(i/(float)Taper));
  }),evidence);
  Y06EastApproachEdit.Assign(middle,Enumerable.Range(0,Approach+1).Select(i=>path.Position(cut+i)),evidence);
  Y06EastApproachEdit.Assign(head,G05NorthWestMapEdit.OriginalHead(path,cut),evidence);
  head.toNode=left.fromNode;head.next=new[]{left.id,middle.id};
  var connector=lanes[left.next[0]];
  Y06EastApproachEdit.Reconnect(connector,left,lanes[connector.next.Single()],evidence);
  var surface=doc.scene.objects.SingleOrDefault(o=>o.id=="road:polish:"+connector.id);
  if(surface!=null)surface.points=connector.points;
  doc.lanes=doc.lanes.Concat(new[]{left,middle}).ToArray();
  var heading=RoadMapEasySelection.Heading(left);
  var rule=package.baseline.junctionRules.Single(j=>j.id==Daeun);
  var axisVector=new Vector3(rule.dx,0,rule.dz).normalized;
  int axis=Mathf.Abs(Vector3.Dot(heading,axisVector))>.7f?0:1;
  int sign=Vector3.Dot(heading,axis==0?axisVector:TrafficSystem.Right(axisVector))>=0?1:-1;
  var signAt=left.points.Last().V()-heading*5;
  doc.facilities=doc.facilities.Append(new RoadMapFacility{
   id=Prefix+"permissive",kind="sign",type=RoadMapRules.PermissiveLeftSign,rule=true,laneId=left.id,junctionId=Daeun,
   axis=axis,sign=sign,height=3,width=2.5f,x=signAt.x,z=signAt.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,evidence=evidence
  }).ToArray();
 }

 static void Validate(ProjectMapPackage package){
  var map=package.Compose();
  EungyeConnectivityRepair.Apply(map,package.baseline);
  var graph=new HdRoadGraph(map);
  var indices=Enumerable.Range(0,graph.edges.Length).ToDictionary(i=>graph.edges[i].id);
  int exit=indices[Exit];
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>graph.edges[a.edgeIndex].id);
  int Mask(HdEdge lane)=>lane.next.Aggregate(0,(mask,n)=>mask|UturnMovement.PaintMask(graph.edges[n].turn));
  Check(Mask(graph.edges[exit])==(LaneDirectionPaint.Left|LaneDirectionPaint.Right)&&!arrows[Exit].Any(),"40 requires both turns with no direction arrow");
  Check(!graph.edges.Any(e=>e.next.Contains(exit)),"40 must stay exit-only");
  foreach(var pair in new[]{(id:East,turn:1),(id:West,turn:-1)}){
   int landing=indices[SegmentId(pair.id,"parking-exit-"+(pair.turn>0?"east":"west"))];
   int join=graph.edges[exit].next.Single(n=>graph.edges[n].turn==pair.turn);
   Check(graph.Route(exit,2,landing,1)!=null&&graph.edges[join].next.SequenceEqual(new[]{landing}),"40 must enter the correct carriageway");
   Y07MapTests.Footprint(map,graph.edges[join]);
  }
  Y07MapTests.Footprint(map,graph.edges[exit]);
  foreach(var pair in new[]{
   (id:Prefix+"left",mask:LaneDirectionPaint.Left),(id:Centre,mask:LaneDirectionPaint.Straight),
   (id:Outer+":part:2",mask:LaneDirectionPaint.Straight|LaneDirectionPaint.Right)}){
   var lane=graph.edges[indices[pair.id]];
   Check(Mask(lane)==pair.mask&&arrows[lane.id].Any(a=>a.turns==pair.mask),"41 arrow and route disagreed: "+lane.id);
   Y07MapTests.Footprint(map,lane);
   foreach(int next in lane.next)Y07MapTests.Footprint(map,graph.edges[next]);
  }
  ValidateSignal(map);
  var catalog=PassengerStopCatalog.For(map);
  Check(catalog.Stops.Length==39,"expected 39 passenger stops: "+string.Join(" / ",catalog.Rejected));
  int routes=0;
  foreach(var from in catalog.Stops)foreach(var to in catalog.Stops)if(from!=to){
   Check(graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"disconnected stop "+from.name+" → "+to.name);
   routes++;
  }
  Check(routes==1482,"expected 1482 stop routes");
  Debug.Log("PARKING_DAEUN_OK 40 left/right, exit-only, arrow-free; 41 permissive left/straight/straight-right; 39 stops, 1482 routes");
 }

 static void ValidateSignal(MapData map){
  var rule=map.junctionRules.Single(j=>j.id==Daeun);
  var junction=new TrafficJunction{center=rule.V(),name=Daeun,facility=rule,signalized=true,leftTurnMode=LeftTurnMode.ProtectedShared,
   direction=new Vector3(rule.dx,0,rule.dz).normalized};
  var northeast=map.editorDocument.facilities.Single(f=>f.id==Prefix+"permissive");
  var southwest=map.editorDocument.facilities.Single(f=>f.id=="reviewed:Y04:SW:permissive");
  var host=new GameObject("Daeun signal regression");
  var blocker=new GameObject("Opposing traffic regression");
  try{
   var system=host.AddComponent<TrafficSystem>();
   typeof(TrafficSystem).GetField("roadMap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(system,map);
   system.manualClock=true;junction.gameProgram=DriveFlowPolicy.Signals(system,junction);
   Check(northeast.axis==southwest.axis&&northeast.sign==-southwest.sign,"41 and southwest are opposite approaches");
   foreach(var sign in new[]{northeast,southwest}){
    Check(system.HasLeftMovement(junction,sign.axis,sign.sign)&&system.AuthoredPermissiveLeft(junction,sign.axis,sign.sign),"both Daeun left turns stay enabled");
    Check(!junction.gameProgram.groups.Any(g=>!g.pedestrian&&g.axis==sign.axis&&g.sign==sign.sign&&g.turn==-1),"signed Daeun approach has no protected arrow");
    bool green=false,red=false;
    for(float t=0;t<90;t+=.5f){
     system.manualTime=t;var through=system.Signal(junction,sign.axis,sign.sign);
     green|=through==SignalColor.Green;red|=through==SignalColor.Red;
     Check(system.LeftEntrySignal(junction,sign.axis,sign.sign,sign.laneId)==through,"Daeun permissive follows through signal");
    }
    Check(green&&red,"Daeun signal alternates");
   }
   var heading=system.AxisDirection(junction,northeast.axis)*northeast.sign;
   blocker.transform.SetPositionAndRotation(junction.center+heading*4,Quaternion.LookRotation(-heading));
   blocker.layer=9;var box=blocker.AddComponent<BoxCollider>();Physics.SyncTransforms();
   Check(!system.YieldClear(junction,heading,-1,host),"41 yields to opposing traffic");
   box.enabled=false;Physics.SyncTransforms();
   Check(system.YieldClear(junction,heading,-1,host),"41 can turn after opposing traffic clears");
  }finally{UnityEngine.Object.DestroyImmediate(blocker);UnityEngine.Object.DestroyImmediate(host);}
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Check(before.document.revision==38&&before.contentHash==PreviousHash,"exact r38 required");
  var doc=before.document.Copy();var lanes=doc.lanes.ToDictionary(l=>l.id);
  PatchParking(before,doc,lanes);
  PatchDaeun(before,doc,lanes);
  doc.revision=39;doc.projectRevision="";doc.savedAt=DateTime.UtcNow.ToString("o");
  var errors=RoadMapValidator.Validate(before.baseline,doc).Where(x=>x.error).ToArray();
  Check(errors.Length==0,string.Join(" / ",errors.Take(12).Select(x=>x+" at "+x.position)));
  var package=ProjectMapPackage.Create(before.baseline,doc);
  Validate(package);ProjectMapStore.Save(root,package,token);
  var saved=ProjectMapStore.Load(root);
  Check(saved.contentHash==package.contentHash&&saved.baselineHash==before.baselineHash,"saved map identity");
  Validate(saved);ProjectMapBuild.Prepare();
  Debug.Log("PARKING_DAEUN_APPLIED revision=39 contentHash="+saved.contentHash);
 }

 public static void BuildReviewed(){
  JunctionRouteFeedbackTests.VerifyMigration();
  FlexibleBollardTests.BuildCommercialMap();
 }
}
