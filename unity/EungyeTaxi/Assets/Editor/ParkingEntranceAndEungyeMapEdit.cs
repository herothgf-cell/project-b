using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ParkingEntranceAndEungyeMapEdit {
 const string PreviousHash="20c2236cae3f3e11fb7d6f2435fc41d8c2ce0e4c5331f42f779c21b4d6d14595";
 const string Northbound="local:5788:2694:6370:3328";
 const string Southbound="reviewed:P20:left:part:1";
 const string Entrance="reviewed:P20:parking-entry";
 const string Exit="reviewed:P20:parking-exit";
 const string Crossroad="J4396_13458";
 const string Left="local:3670:12947:4220:13307";
 const string Middle="reviewed:Y04:43:straight-1";
 const string Outer="local:3703:12897:4253:13257:part:reviewed:T1-SW-right";
 const string Slip="reviewed:T1-SW-right:lane";
 const string Island="reviewed:Y04:43:island";
 const string LeftThrough="join:"+Left+":0";

 static void Check(bool ok,string message){
  if(!ok)throw new InvalidOperationException("PARKING_EUNGYE: "+message);
 }

 static Vector3 P(float x,float z)=>new Vector3(x,0,z);
 static MapPoint Point(Vector3 p)=>new MapPoint{x=p.x,z=p.z};
 static RoadMapLane Copy(RoadMapLane lane)=>JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));
 static AssistedRoadNetwork.Path Path(RoadMapLane lane)=>new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());

 static float Station(AssistedRoadNetwork.Path path,Vector3 position){
  return path.Project(position,out _);
 }

 static RoadMapLane Split(RoadMapDocument doc,RoadMapLane lane,float cut,string label,RoadMapEvidence evidence){
  var original=Path(lane);
  Check(cut>0&&cut<original.length,"split inside "+lane.id);
  Check(!doc.facilities.Any(f=>f.laneId==lane.id)&&!doc.scene.objects.Any(o=>o.IsService&&o.laneId==lane.id&&o.laneAt>=cut),"references need to be reassigned for "+lane.id);
  var tail=Copy(lane);
  var source=Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(lane.id)).TrimEnd('=').Replace('+','-').Replace('/','_');
  tail.id="editor:segment:"+source+":"+label;
  tail.fromNode=lane.id+":"+label;
  tail.sourceOffset+=cut;
  var points=new List<Vector3>{original.Position(cut)};
  float distance=0;
  for(int i=1;i<original.points.Length;i++){
   distance+=Vector3.Distance(original.points[i-1],original.points[i]);
   if(distance>cut+.001f)points.Add(original.points[i]);
  }
  Y06EastApproachEdit.Assign(tail,points,evidence);
  Y06EastApproachEdit.Assign(lane,G05NorthWestMapEdit.OriginalHead(original,cut),evidence);
  lane.toNode=tail.fromNode;
  lane.next=new[]{tail.id};
  doc.lanes=doc.lanes.Append(tail).ToArray();
  return tail;
 }

 static ProjectMapObject Pavement(RoadMapLane lane,float width){
  return new ProjectMapObject{
   id="road:"+lane.id,kind="road",name=lane.roadName,points=lane.points,
   width=width,lanes=1,suppressPaint=true
  };
 }

 static void PatchParking(RoadMapDocument doc,Dictionary<string,RoadMapLane> lanes){
  var evidence=new RoadMapEvidence{
   state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
   url="https://map.naver.com/p?c=18.00,0,0,0,adh&p=gnX8QlbYZtX8nxUl0hllcA,95,10,80,Float",
   note="Marker 42 west gate. Naver shows the entrance arrow, 2.5m gantry and an opening in the separator. User confirmed left entry southbound. Lane and internal aisle dimensions are game approximations."
  };
  var exit=lanes[Exit];
  var entrance=new RoadMapLane{
   id=Entrance,sourceId=Entrance,fromNode=Entrance+":mouth",toNode=exit.fromNode,
   roadName="은계 공영주차장 입구",status="provisional",number=1,width=3.3f,next=new[]{Exit}
  };
  var end=exit.points[0].V();
  const float curveRadius=13f;
  var aisle=Enumerable.Range(0,46).Select(i=>P(587.5f+i,Mathf.Lerp(305.8f,end.z-curveRadius,
   Mathf.SmoothStep(0,1,Mathf.Clamp01((i-8)/30f)))));
  var corner=Enumerable.Range(1,26).Select(i=>{
   float angle=i*Mathf.PI/52f;
   return P(end.x-curveRadius+curveRadius*Mathf.Sin(angle),end.z-curveRadius*Mathf.Cos(angle));
  });
  Y06EastApproachEdit.Assign(entrance,aisle.Concat(corner),evidence);
  var north=lanes[Northbound];
  var northPath=Path(north);
  var roads=new List<ProjectMapObject>{Pavement(entrance,5.4f)};
  foreach(var approach in new[]{(lane:north,z:298f,turn:1,side:"north"),(lane:lanes[Southbound],z:315f,turn:-1,side:"south")}){
   Split(doc,approach.lane,Station(Path(approach.lane),P(576,approach.z)),"parking-entry-"+approach.side,evidence);
   var turn=Copy(entrance);
   turn.id="join:"+Entrance+":"+approach.side;
   turn.turn=approach.turn;
   Y06EastApproachEdit.Reconnect(turn,approach.lane,entrance,evidence);
   approach.lane.next=approach.lane.next.Append(turn.id).ToArray();
   doc.lanes=doc.lanes.Append(turn).ToArray();
   roads.Add(Pavement(turn,4.8f));
  }
  doc.lanes=doc.lanes.Append(entrance).ToArray();
  var opening=new HashSet<string>{"sidewalk:generated:7736","sidewalk:generated:7738","sidewalk:generated:7740","sidewalk:generated:7742",
   "sidewalk:generated:13500","sidewalk:generated:13501","sidewalk:generated:13502","sidewalk:generated:13503","sidewalk:generated:13504"};
  foreach(var panel in doc.scene.objects.Where(o=>opening.Contains(o.id)))panel.enabled=false;
  doc.scene.objects=doc.scene.objects.Concat(roads).ToArray();
  doc.facilities=doc.facilities.Append(new RoadMapFacility{
   id=Entrance+":centre-gap",kind="lanePresentation",type=ReviewedUnmarkedRoadPaint.FeatureType,laneId=north.id,
   width=5.4f,x=576.6f,z=308f,startAt=Station(northPath,P(578.8f,303f)),endAt=Station(northPath,P(578.8f,314f)),
   evidence=evidence
  }).ToArray();
 }

 static void PatchCrossroad(RoadMapDocument doc,Dictionary<string,RoadMapLane> lanes){
  var evidence=new RoadMapEvidence{
   state="observed",month="2025-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
   url="https://map.naver.com/p?c=19.00,0,0,0,adh&p=OOE_5RoZTFRvvlL-W6fsmg,63,-15,46,Float",
   note="Marker 43 southwest approach. User confirmed left, straight, straight and a separate right slip at a pedestrian island. Taper, width and island polygon are game approximations."
  };
  var head=lanes[Left];
  var path=Path(head);
  const int length=33;
  float cut=path.length-length;
  // Keep the receiving lanes and the existing turn channels, adding the missing middle approach.
  var left=Split(doc,head,cut,"43-left",evidence);
  var straight=lanes[LeftThrough];
  var middle=Copy(left);
  middle.id=Middle;middle.number=2;middle.toNode=Middle+":end";middle.next=new[]{straight.id};
  middle.sourceOffset=0;middle.width=3.05f;
  Y06EastApproachEdit.Assign(middle,Enumerable.Range(0,length+1).Select(i=>{
   float at=cut+i;
   float offset=3.05f*Mathf.SmoothStep(0,1,Mathf.Clamp01(i/17f));
   return path.Position(at)+TrafficSystem.Right(path.Tangent(at))*offset;
  }),evidence);
  left.next=left.next.Where(id=>id!=LeftThrough).ToArray();
  head.next=head.next.Append(middle.id).ToArray();
  Y06EastApproachEdit.Reconnect(straight,middle,lanes[straight.next.Single()],evidence);
  var outer=lanes[Outer];
  outer.number=3;outer.width=3.05f;
  lanes[Slip].number=4;
  lanes["reviewed:T1-SW-right:taper"].number=4;
  doc.lanes=doc.lanes.Append(middle).ToArray();
  var throughRoad=doc.scene.objects.Single(o=>o.id=="road:polish:"+LeftThrough);
  throughRoad.points=straight.points;
  var island=new ProjectMapObject{
   id=Island,kind="sidewalk",name="대은로 우회전 보행섬",height=.14f,
   points=new[]{P(430.3f,1326.43f),P(431.8f,1327.14f),P(432.35f,1327.24f),P(433.0f,1326.63f)}.Select(Point).ToArray()
  };
  doc.scene.objects=doc.scene.objects.Concat(new[]{Pavement(middle,4.1f),island}).ToArray();
  var slipPath=Path(lanes["join:reviewed:T1-SW-right:right"]);
  var crossingPoint=slipPath.Position(13f);
  doc.facilities=doc.facilities.Append(new RoadMapFacility{
   id=Island+":crosswalk",kind="crosswalk",type="황색",width=4.1f,length=2.6f,
   x=crossingPoint.x,z=crossingPoint.z,yaw=Quaternion.LookRotation(slipPath.Tangent(13f)).eulerAngles.y,
   evidence=evidence
  }).ToArray();
 }

 public static void Verify(){
  Validate(ProjectMapStore.Load(ProjectMapBuild.Root));
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Check(before.document.revision==39&&before.contentHash==PreviousHash,"exact r39 required");
  var doc=before.document.Copy();
  var lanes=doc.lanes.ToDictionary(l=>l.id);
  PatchParking(doc,lanes);
  PatchCrossroad(doc,lanes);
  doc.revision=40;doc.projectRevision="";doc.savedAt=DateTime.UtcNow.ToString("o");
  var errors=RoadMapValidator.Validate(before.baseline,doc).Where(i=>i.error).ToArray();
  Check(errors.Length==0,string.Join(" / ",errors.Take(12).Select(i=>i+" at "+i.position.ToString("F2"))));
  var result=ProjectMapPackage.Create(before.baseline,doc);
  Validate(result);
  ProjectMapStore.Save(root,result,token);
  var saved=ProjectMapStore.Load(root);
  Check(saved.contentHash==result.contentHash&&saved.baselineHash==before.baselineHash,"saved map identity");
  Validate(saved);
  ProjectMapBuild.Prepare();
  Debug.Log("PARKING_EUNGYE_APPLIED revision=40 contentHash="+saved.contentHash);
 }

 public static void BuildReviewed(){
  JunctionRouteFeedbackTests.VerifyMigration();
  FlexibleBollardTests.BuildCommercialMap();
 }

 static void Validate(ProjectMapPackage package){
  var map=package.Compose();
  EungyeConnectivityRepair.Apply(map,package.baseline);
  var graph=new HdRoadGraph(map);
  int Index(string id)=>Array.FindIndex(graph.edges,e=>e.id==id&&!e.disabled);
  int entrance=Index(Entrance),exit=Index(Exit);
  Check(entrance>=0&&exit>=0,"42 must have a separate entrance joined to 40");
  foreach(var pair in new[]{(id:Northbound,turn:1),(id:Southbound,turn:-1)}){
   int start=Index(pair.id);
   Check(start>=0,"missing adjacent street lane");
   int entry=graph.edges[start].next.Single(i=>graph.edges[i].turn==pair.turn);
   Check(graph.edges[entry].next.SequenceEqual(new[]{entrance}),"42 approach turn must enter the driveway");
   var route=graph.Route(start,1,exit,2);
   Check(route!=null&&route.edges.Contains(entrance),"42 is accessible through the lot to 40");
   Y07MapTests.Footprint(map,graph.edges[entry]);
   if(pair.turn==-1){
    var lines=new CentreLineCrossing();
    RoadPaintGeometry.Paint(map,new List<TrafficJunction>(),(name,p,d,_,length,yellow)=>{
     if(yellow&&name=="Centre line")lines.Add(p-d*(length*.5f),p+d*(length*.5f));
    });
    var path=graph.paths[entry];
    for(float at=0;at<path.length;at+=.4f)
     Check(!lines.Crossed(path.Position(at),path.Position(Mathf.Min(at+.4f,path.length))),"42 lawful left crosses rendered centre line");
   }
  }
  Check(graph.edges.Where(e=>e.next.Contains(exit)).Select(e=>e.id).SequenceEqual(new[]{Entrance}),"40 remains an exit from the lot only");
  Check(graph.edges[exit].next.Select(i=>graph.edges[i].turn).OrderBy(i=>i).SequenceEqual(new[]{-1,1}),"40 still exits both ways");
  Y07MapTests.Footprint(map,graph.edges[entrance]);

  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>a.edgeIndex);
  var through=new System.Collections.Generic.HashSet<int>();
  var leftTail=package.document.lanes.Single(l=>l.id==Left).next.Single(n=>n!=Middle);
  foreach(var movement in new[]{(id:leftTail,turn:-1),(id:Middle,turn:0),(id:Outer,turn:0),(id:Slip,turn:1)}){
   int i=Index(movement.id);
   Check(i>=0,"43 missing lane "+movement.id);
   var lane=graph.edges[i];
   var connector=graph.edges[lane.next.Single()];
   Check(lane.ruleId==Crossroad&&connector.turn==movement.turn,"43 movement "+movement.id);
   Check(arrows[i].Any(a=>a.turns==UturnMovement.PaintMask(movement.turn)),"43 arrow agrees with route "+movement.id);
   if(movement.turn==0)through.Add(connector.next.Single());
   Y07MapTests.Footprint(map,lane);
   Y07MapTests.Footprint(map,connector);
  }
  Check(through.Count==2,"43 must offer two distinct straight receiving lanes");
  Check(package.document.scene.objects.Any(o=>o.id==Island&&o.enabled&&o.kind=="sidewalk"),"43 raised pedestrian island");
  var stops=PassengerStopCatalog.For(map).Stops;
  Check(stops.Length==39,"39 stops retained");
  foreach(var from in stops)foreach(var to in stops)if(from!=to)
   Check(graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"disconnected stop "+from.name+" → "+to.name);
  Debug.Log("PARKING_EUNGYE_OK 42 southbound left and northbound right, 40 exit-only, 43 left/straight/straight/island right, 39 stops 1482 routes");
 }
}
