using System;
using System.Linq;
using UnityEngine;

public static class DailyRoadConstructionChecks {
 public static void Run(){
  string root=ProjectMapBuild.Root,source=ProjectMapStore.Fingerprint(root);
  string region=MapCatalog.LockedRegion;
  MapData map;
  try{MapCatalog.LockedRegion="eungye";map=MapCatalog.Load();}
  finally{MapCatalog.LockedRegion=region;}
  var stops=PassengerStopCatalog.For(map).Stops;
  var graph=new HdRoadGraph(map);
  var districtGraph=new HdRoadGraph(map);
  int lane=DailyRoadConstruction.EdgeIndex(map);
  int parallel=Array.FindIndex(graph.edges,x=>x.id=="local:11605:16081:10719:15876:part:1");
  Check(lane>=0&&parallel>=0&&graph.edges[lane].number==2&&graph.edges[parallel].number==1,"existing outer and inner westbound lanes");
  var center=DailyRoadConstruction.Preview(map);
  Check(center.Length>1&&center[0].x>center[center.Length-1].x,"westbound preview follows the lane");
  float surveyError;graph.paths[lane].Project(new Vector3(1134.18f,0,1610.23f),out surveyError);
  Check(surveyError<6&&graph.paths[lane].length>30&&graph.paths[lane].length<35,"48 survey point, 30–35m existing section");
  int upstream=Array.FindIndex(graph.edges,x=>x.id=="local:11589:16123:10729:15920");
  int merge=Array.FindIndex(graph.edges,x=>x.id=="change:local:11589:16123:10729:15920:local:11605:16081:10719:15876");
  int landing=Array.FindIndex(graph.edges,x=>x.id=="local:11605:16081:10719:15876:part:2");
  Check(upstream>=0&&merge>=0&&landing>=0&&graph.edges[upstream].next.Contains(merge)&&graph.edges[merge].next.Contains(landing),"existing merge lands on the downstream inner lane");
  var open=graph.Forward(upstream,1);
  Check(open!=null&&open.edges.Contains(lane),"open free guidance uses the outer lane");
  string before=JsonUtility.ToJson(graph.edges[lane]);
  Check(DailyRoadConstruction.Apply(map,true)&&graph.edges[lane].TraversalDisabled&&!graph.edges[lane].disabled,"runtime-only lane flag");
  Check(JsonUtility.ToJson(graph.edges[lane])==before&&ProjectMapStore.Fingerprint(root)==source,"closure does not serialize or change project inputs");
  Check(!graph.edges[parallel].TraversalDisabled&&!graph.edges[landing].TraversalDisabled&&districtGraph.edges[lane].TraversalDisabled,"traffic graph shares closure and the entire parallel lane remains open");
  var forward=graph.Forward(upstream,1);
  Check(forward!=null&&!forward.edges.Contains(lane)&&forward.edges.Take(3).SequenceEqual(new[]{upstream,merge,landing}),"free H steering follows the existing merge to the open lane");
  int pairs=0,unreachable=0;
  foreach(var a in stops)foreach(var b in stops){
   if(a==b)continue;
   pairs++;
   var path=graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);
   if(path==null){unreachable++;continue;}
   Check(!path.edges.Contains(lane),"planned route enters work zone");
  }
  Check(stops.Length==39&&pairs==1482&&unreachable==0,"construction trip pairs="+pairs+" unreachable="+unreachable);
  var search=districtGraph.SearchDistances(upstream,1);
  var searchRoute=search.RouteTo(landing,5);
  Check(searchRoute!=null&&searchRoute.edges.SequenceEqual(new[]{upstream,merge,landing})&&search.RouteTo(lane,5)==null,"distance search uses the existing merge and excludes the closure");
  Check(DailyRoadConstruction.Apply(map,false)&&!graph.edges[lane].TraversalDisabled&&graph.Forward(upstream,1).edges.Contains(lane),"opening restores original free route");
  VerifyTrafficCache(map,upstream,lane,landing);
  Check(ProjectMapStore.Fingerprint(root)==source,"runtime apply and remove preserve project file");
  Debug.Log("DAILY_ROAD_CONSTRUCTION_OK lane="+graph.edges[lane].id+" length="+graph.paths[lane].length.ToString("F2")+" mergeLanding="+graph.edges[landing].id+" stops="+stops.Length+" pairs="+pairs+" unreachable="+unreachable);
 }
 static void VerifyTrafficCache(MapData map,int upstream,int lane,int landing){
  var fixture=new GameObject("construction traffic routes");
  try{
   var traffic=fixture.AddComponent<TrafficSystem>();
   var district=fixture.AddComponent<DistrictTraffic>();
   district.Initialize(traffic,map,false);
   Check(district.CreateRoute(upstream,1,lane,20)!=null,"ordinary-day traffic caches the open route");
   DailyRoadConstruction.Apply(map,true);
   district.LaneAvailabilityChanged(lane,true);
   Check(district.CreateRoute(upstream,1,lane,20)==null,"traffic cannot retrieve the previous cached route");
   Check(district.CreateRoute(upstream,1,landing,10)!=null,"traffic can route through the existing adjacent-lane merge");
   DailyRoadConstruction.Apply(map,false);
   district.LaneAvailabilityChanged(lane,false);
   Check(district.CreateRoute(upstream,1,lane,20)!=null,"traffic can use the reopened lane");
  }finally{UnityEngine.Object.DestroyImmediate(fixture);}
 }
 static void Check(bool ok,string message){if(!ok)throw new Exception("DAILY_ROAD_CONSTRUCTION "+message);}
}
