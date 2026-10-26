using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Uses the shipped Eungye package rather than a synthetic lane fixture.
public static class NavigationStopRegressionTests {
 const string BeforeId="local:7003:14854:5837:14446";
 const string SourceId="local:7003:14854:5837:14446:part:1";
 const string GoalId="local:7023:14796:5857:14389:part:3";
 const string ChangeId="change:local:7003:14854:5837:14446:local:7023:14796:5857:14389";
 static void Check(bool value,string name){if(!value)throw new Exception("NAVIGATION_STOP_REGRESSION_FAIL "+name);Debug.Log("PASS "+name);}
 static MapData Map(){var package=ProjectMapStore.Load(ProjectMapBuild.Root);return RoadMapComposer.Compose(package.baseline,package.document);}
 static int Edge(MapData map,string id){return Array.FindIndex(map.hdEdges,e=>e.id==id);}
 // Diagnostic only: no traffic, no map edits, no vehicle/world creation.
 // An independent O(V^2) distance oracle catches wrong queue ordering or
 // relaxation in the production router; distance penalties remain explicit.
 public static void AuditRoutes(){
  var watch=System.Diagnostics.Stopwatch.StartNew();var map=Map();var graph=new HdRoadGraph(map);var stops=PassengerStopCatalog.For(map).Stops;
  var sources=new List<Tuple<string,int,float>>();
  var spawn=map.stops.OrderBy(s=>Vector3.Distance(s.V(),map.targets[0].V())).First();
  sources.Add(Tuple.Create("hybrid-initial",spawn.laneEdge,Mathf.Max(1,spawn.laneAt-10)));
  foreach(var parking in ProjectMapScene.Services(map,"parking")){var s=ProjectMapScene.ServiceStop(map,parking);sources.Add(Tuple.Create(parking.id,s.laneEdge,s.laneAt));}
  FuelStationLayout.Apply(map);
  foreach(var station in FuelStationLayout.For(map)){float at;int edge=graph.Nearest(station.exit,station.direction,out at);Debug.Log("NAV_AUDIT_PUMP "+station.id+" pump="+station.pump+" exit="+station.exit+" snapped="+edge);if(edge>=0)sources.Add(Tuple.Create("fuel-exit:"+station.id,edge,at));}
  for(int i=0;i<stops.Length;i++)sources.Add(Tuple.Create("S"+(i+1).ToString("00"),stops[i].laneEdge,stops[i].laneAt));
  var detours=new List<Tuple<float,string>>();int checkedRoutes=0,unreachable=0,failures=0;
  foreach(var source in sources){
   var oracle=AuditDistances(graph,source.Item2,source.Item3);
   for(int i=0;i<stops.Length;i++){
    var stop=stops[i];if(source.Item2==stop.laneEdge&&Mathf.Abs(source.Item3-stop.laneAt)<.1f)continue;
    var route=graph.Route(source.Item2,source.Item3,stop.laneEdge,stop.laneAt);
    float expected=source.Item2==stop.laneEdge&&source.Item3<=stop.laneAt?stop.laneAt-source.Item3:oracle[stop.laneEdge]+stop.laneAt;
    string key=source.Item1+" -> S"+(i+1).ToString("00")+" source="+graph.edges[source.Item2].id+"@"+source.Item3.ToString("F2")+" goal="+graph.edges[stop.laneEdge].id+"@"+stop.laneAt.ToString("F2");
    if(route==null){unreachable++;if(!float.IsInfinity(expected)){failures++;Debug.LogError("NAV_AUDIT_MISSING "+key+" oracle="+expected);}continue;}
    checkedRoutes++;float score=route.path.length;for(int k=1;k<route.edges.Count;k++)if(graph.edges[route.edges[k]].change!=0)score+=12;
    if(Mathf.Abs(score-expected)>.15f||float.IsInfinity(expected)){failures++;Debug.LogError("NAV_AUDIT_NONOPTIMAL "+key+" selected="+score+" oracle="+expected);}
    float direct=Vector3.Distance(graph.paths[source.Item2].Position(source.Item3),graph.paths[stop.laneEdge].Position(stop.laneAt));
    string turns=string.Join(",",route.edges.Where(e=>graph.edges[e].turn!=0).Select(e=>graph.edges[e].id).ToArray());
    detours.Add(Tuple.Create(route.path.length/Mathf.Max(1,direct),key+" direct="+direct.ToString("F1")+" legal="+route.path.length.ToString("F1")+" turns="+turns));
   }
  }
  foreach(var item in detours.OrderByDescending(x=>x.Item1).Take(12))Debug.Log("NAV_AUDIT_DETOUR ratio="+item.Item1.ToString("F2")+" "+item.Item2);
  Debug.Log("NAVIGATION_ROUTE_AUDIT routes="+checkedRoutes+" unreachable="+unreachable+" failures="+failures+" elapsed="+watch.Elapsed.TotalSeconds.ToString("F2"));
  Check(failures==0,"distance routes match independent minimum-cost oracle");
 }
 static float[] AuditDistances(HdRoadGraph graph,int start,float at){
  int n=graph.edges.Length;var costs=Enumerable.Repeat(float.PositiveInfinity,n).ToArray();var settled=new bool[n];
  foreach(int next in graph.edges[start].next)if(!graph.edges[next].disabled)costs[next]=graph.paths[start].length-at+(graph.edges[next].change!=0?12:0);
  for(int step=0;step<n;step++){
   int best=-1;for(int i=0;i<n;i++)if(!settled[i]&&(best<0||costs[i]<costs[best]))best=i;
   if(best<0||float.IsInfinity(costs[best]))break;settled[best]=true;
   foreach(int next in graph.edges[best].next)if(!graph.edges[next].disabled)costs[next]=Mathf.Min(costs[next],costs[best]+graph.paths[best].length+(graph.edges[next].change!=0?12:0));
  }
  return costs;
 }
 public static void Run(){
  var map=Map();var original=map.stops[8];var catalog=PassengerStopCatalog.For(map);var stop=catalog.Stops[8];
  Check(stop.name=="은계 승하차 9"&&stop.laneEdge==Edge(map,SourceId),"S09 uses the original directed outer lane");
  var right=TrafficSystem.Right(stop.Direction);var curbDistance=Vector3.Dot(stop.Kerb-stop.V(),right);
  Check(curbDistance>1.6f&&curbDistance<2.1f,"S09 taxi target is near actual right curb, not lane center");
  Debug.Log("S09_POSITION kerb="+stop.Kerb.ToString("F5")+" passenger="+stop.Passenger.ToString("F5")+" source="+stop.V().ToString("F5"));
  Check(original.hasPassengerAnchor&&!stop.hasPassengerAnchor,"only packaged S09 sidewalk anchor is replaced by curb-side waiting position");
  Check(Vector2.Distance(new Vector2(stop.Kerb.x,stop.Kerb.z),new Vector2(675.13423f,1478.49262f))<.05f&&Vector2.Distance(new Vector2(stop.Passenger.x,stop.Passenger.z),new Vector2(674.35916f,1480.71112f))<.05f,"S09 taxi and waiting passenger match measured curb-side positions");
  Check(Mathf.Abs(original.width-3)<.01f,"canonical package stop geometry is not mutated");
  Check(stop.CanStop(stop.Kerb+Vector3.up*.65f,stop.Direction,0),"S09 right-curb stop is valid");
  Check(!stop.CanStop(stop.V()+Vector3.up*.65f,stop.Direction,0),"S09 lane-center near miss does not count");
  Check(!stop.CanStop(stop.Kerb+right*1.3f+Vector3.up*.65f,stop.Direction,0),"S09 beyond curb tolerance does not count");
  Check(!stop.CanStop(stop.Kerb+Vector3.up*.65f,-stop.Direction,0),"S09 wrong-way stop does not count");
  var edited=Map();edited.stops[8].x+=5;var editedStop=PassengerStopCatalog.For(edited).Stops[8];Check(Mathf.Abs(editedStop.width-3)<.01f&&Mathf.Abs(editedStop.x-edited.stops[8].x)<.01f,"custom-edited S09 geometry is respected");
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var sidewalk=package.document.scene.objects.First(o=>o.id=="sidewalk:stop:8");foreach(var point in sidewalk.points){point.x+=.65f;point.z+=.45f;}var shiftedAnchor=ProjectMapScene.Center(sidewalk.points)+Vector3.up*sidewalk.height;var sidewalkEdited=RoadMapComposer.Compose(package.baseline,package.document);var customStop=PassengerStopCatalog.For(sidewalkEdited).Stops[8];
  Check(sidewalkEdited.stops[8].hasPassengerAnchor&&Vector3.Distance(sidewalkEdited.stops[8].passengerAnchor,shiftedAnchor)<.01f,"edited scene sidewalk supplies S09 waiting anchor");
  Check(Mathf.Abs(customStop.width-6.6f)<.01f&&customStop.hasPassengerAnchor&&Vector3.Distance(customStop.Passenger,shiftedAnchor)<.01f,"S09 curb correction retains independently edited sidewalk waiting anchor");
  var taxi=new GameObject("nav regression taxi");var root=new GameObject("nav regression");try{
   var nav=root.AddComponent<RoadNavigation>();var graph=new HdRoadGraph(map);int beforeIndex=Edge(map,BeforeId),sourceIndex=Edge(map,SourceId),goalIndex=Edge(map,GoalId),changeIndex=Edge(map,ChangeId);Check(beforeIndex>=0&&sourceIndex>=0&&goalIndex>=0&&changeIndex>=0,"actual directed fixture edges resolve by stable ID");var before=graph.paths[beforeIndex];taxi.transform.SetPositionAndRotation(before.Position(before.length-2)+Vector3.up*.65f,Quaternion.LookRotation(before.Tangent(before.length-2)));
   nav.Build(map,taxi.transform);nav.SetHdDestination(goalIndex,10);
   Check(nav.hdRoute!=null&&nav.hdRoute.edges.Contains(changeIndex),"actual legal lane-change connector is in the route");
   var source=nav.hd.paths[sourceIndex];taxi.transform.SetPositionAndRotation(source.Position(16)+Vector3.up*.65f,Quaternion.LookRotation(source.Tangent(16)));nav.Refresh();
   Check(nav.hdRoute!=null&&nav.hdRoute.edges.Contains(changeIndex)&&nav.path.Count>1,"guide survives source-side crossing of legal lane-change connector");
   taxi.transform.SetPositionAndRotation(source.Position(16)+Vector3.up*.65f,Quaternion.LookRotation(-source.Tangent(16)));nav.Refresh();
   Check(nav.hdRoute==null||!nav.hdRoute.edges.Contains(changeIndex),"wrong-way heading cannot retain lane-change guidance");
  }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(taxi);}
  Debug.Log("NAVIGATION_STOP_REGRESSION_OK");
 }
}
