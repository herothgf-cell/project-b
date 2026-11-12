using System;
using System.Linq;
using UnityEngine;

public static class J7973SouthboundMapEdit {
 const string Junction="J7973_3744";
 const string Pocket="reviewed:J7973:southbound-left";
 const string Connector="join:"+Pocket;
 const string Inner="editor:segment:bG9jYWw6ODAzMjo2MTY0Ojc5NjA6Mzk4ODpwYXJ0OjI:d3dd4746f874469bb9f2d73b6860323e";
 const string Outer="editor:segment:bG9jYWw6NzkxODo2MTY3Ojc4NDc6Mzk5NjpwYXJ0OjI:0fab0e3ade034d179017302129eee331";
 const string RemovedStraight="join:local:7918:6167:7847:3996:0";

 static MapPoint Point(Vector3 position){return new MapPoint{x=position.x,z=position.z};}
 static AssistedRoadNetwork.Path Path(RoadMapLane lane){return new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());}
 static float[] Stations(MapPoint[] points){
  var stations=new float[points.Length];
  for(int i=1;i<points.Length;i++)stations[i]=stations[i-1]+Vector3.Distance(points[i-1].V(),points[i].V());
  return stations;
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var package=ProjectMapStore.Load(root);
  var doc=package.document;
  if(doc.revision!=15||doc.lanes.Any(l=>l.id==Pocket))throw new InvalidOperationException("Expected the unmodified r15 project map.");
  var inner=doc.lanes.Single(l=>l.id==Inner);
  var innerPath=Path(inner);
  var direction=innerPath.Tangent(innerPath.length);
  var outward=-TrafficSystem.Right(direction);
  var stations=Stations(inner.points);
  const float width=3.3f;
  var evidence=new RoadMapEvidence{state="observed",appearanceConfirmed=true,month="2026-05",checkedAt="2026-11-12",
   url="https://map.naver.com/p/entry/coordinates/3zanLi,2AGD8X?c=15.00,0,0,0,adh&p=7vnm_N8HXCvrp53jDXLU3Q,-180,10,70,Float",
   note="은계로 수인로 교차로 남행 접근 4차로. 좌회전 1·직진 2·우회전 1은 사용자 요청. 테이퍼와 폭은 게임 치수."};
  var pocket=new RoadMapLane{id=Pocket,sourceId=inner.sourceId,roadName=inner.roadName,ruleId=Junction,
   fromNode=inner.fromNode,toNode=Pocket+":end",status="provisional",number=4,width=width,
   points=inner.points.Select((p,i)=>Point(p.V()+outward*width*Mathf.SmoothStep(0,1,stations[i]/24f))).ToArray(),
   heights=(float[])inner.heights.Clone(),next=new[]{Connector},evidence=evidence};
  var native=doc.lanes.Single(l=>l.id=="A2218G611449:0");
  var curveStart=pocket.points.Last().V();
  var shift=curveStart-native.points[0].V();
  var turnStations=Stations(native.points);
  var turn=new RoadMapLane{id=Connector,sourceId=inner.sourceId,roadName=inner.roadName,ruleId=Junction,
   fromNode=pocket.toNode,toNode=native.toNode,status="provisional",number=4,turn=-1,width=width,
   points=native.points.Select((p,i)=>Point(p.V()+shift*(1-Mathf.SmoothStep(0,1,turnStations[i]/turnStations.Last())))).ToArray(),
   heights=(float[])native.heights.Clone(),next=(string[])native.next.Clone(),evidence=evidence};
  turn.heights[0]=pocket.heights.Last();
  foreach(var previous in doc.lanes.Where(l=>!l.disabled&&l.toNode==inner.fromNode&&l.next.Contains(inner.id)))
   previous.next=previous.next.Concat(new[]{Pocket}).ToArray();

  var outer=doc.lanes.Single(l=>l.id==Outer);
  outer.next=outer.next.Where(id=>id!=RemovedStraight).ToArray();
  var unused=doc.lanes.Single(l=>l.id==RemovedStraight);
  unused.disabled=true;
  var stop=doc.facilities.Single(f=>f.junctionId==Junction&&f.kind=="stopLine"&&f.axis==1&&f.sign==1);
  // Extend the marked stop line toward the pocket without moving its plane or curb end.
  var stopLeft=-TrafficSystem.Right(Quaternion.Euler(0,stop.yaw,0)*Vector3.forward);
  const float addedStopWidth=2.5f;
  stop.x+=stopLeft.x*addedStopWidth*.5f;
  stop.z+=stopLeft.z*addedStopWidth*.5f;
  stop.width+=addedStopWidth;
  doc.lanes=doc.lanes.Concat(new[]{pocket,turn}).ToArray();
  doc.revision++;
  doc.savedAt=DateTime.UtcNow.ToString("o");
  doc.projectRevision="";
  package.contentHash=ProjectMapPackage.Digest(JsonUtility.ToJson(doc));
  ProjectMapStore.Save(root,package,token);
  ProjectMapBuild.Prepare();
  Debug.Log("J7973_SOUTHBOUND_MAP_OK "+package.contentHash);
 }

 public static void Verify(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);
  var map=package.Compose();
  EungyeConnectivityRepair.Apply(map,package.baseline);
  var arrows=LaneDirectionPaint.Layout(map).Where(a=>a.centre.z>396&&a.centre.z<424&&a.forward.z<-.95f&&map.hdEdges[a.edgeIndex].ruleId==Junction).OrderByDescending(a=>a.centre.x).ToArray();
  Require(arrows.Select(a=>a.turns).SequenceEqual(new[]{LaneDirectionPaint.Left,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight,LaneDirectionPaint.Right}),"southbound arrows left / two straight / right");
  var graph=new HdRoadGraph(map);
  int pocket=Array.FindIndex(graph.edges,e=>e.id==Pocket),turn=Array.FindIndex(graph.edges,e=>e.id==Connector);
  Require(graph.edges[pocket].next.SequenceEqual(new[]{turn})&&graph.edges[turn].turn==-1&&graph.edges[turn].next.Any(n=>!graph.edges[n].disabled),"pocket connects to the existing eastbound exit");
  Require(graph.edges.Any(e=>!e.disabled&&e.next.Contains(pocket)),"southbound upstream lane can enter the pocket");
  Require(!graph.edges.Any(e=>!e.disabled&&e.next.Any(n=>graph.edges[n].id==RemovedStraight)),"outer southbound lane cannot continue straight");
  var stops=PassengerStopCatalog.For(map).Stops;
  int pairs=0;
  foreach(var from in stops)foreach(var to in stops){
   if(from==to)continue;
   Require(graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"passenger route remains reachable: "+from.name+" -> "+to.name);
   pairs++;
  }
  var systemObject=new GameObject("J7973 southbound signal verification");
  try{
   var traffic=systemObject.AddComponent<TrafficSystem>();
   traffic.Build(map,false);
   var junction=traffic.junctions.Single(j=>j.name==Junction);
   Require(traffic.HasLeftMovement(junction,1,1)&&junction.gameProgram.groups.Any(g=>!g.pedestrian&&g.axis==1&&g.sign==1&&g.turn==-1),"left pocket has a protected signal");
   var path=graph.paths[pocket];float stopAt=path.length-4;
   Require(traffic.DirectedApproach(junction,1,1,path.Position(stopAt),path.Tangent(stopAt)),"pocket shares the stop plane and directed law observation");
  }finally{UnityEngine.Object.DestroyImmediate(systemObject);}
  Debug.Log("J7973_SOUTHBOUND_CHECKS_OK stops="+stops.Length+" pairs="+pairs);
 }
 static void Require(bool value,string message){if(!value)throw new InvalidOperationException("J7973_SOUTHBOUND: "+message);}
}
