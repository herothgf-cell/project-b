using System;
using System.Reflection;
using UnityEngine;

public static class StabilizationRegressionTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("STABILIZATION: "+message);Debug.Log("STABILIZATION_PASS "+message);}
 public static void Run(){
  var root=new GameObject("validation signal regression");
  try{
   var s=root.AddComponent<DriveSession>();s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.taxi.body.isKinematic=true;s.jobs=root.AddComponent<PassengerJobs>();s.jobs.nav=root.AddComponent<RoadNavigation>();s.traffic=root.AddComponent<TrafficSystem>();s.traffic.manualClock=true;s.traffic.manualTime=1;
   root.transform.position=new Vector3(2,0,-22);
   var program=new RoadMapSignalProgram {
    groups=new[]{new RoadMapSignalGroup{id="through",axis=0,sign=1,turn=0},new RoadMapSignalGroup{id="left",axis=0,sign=1,turn=-1}},
    stages=new[]{new RoadMapStage{green=10,yellow=0,clearance=0,groups=new[]{"through"}},new RoadMapStage{green=10,yellow=0,clearance=0,groups=new[]{"left"}}}
   };
   var j=new TrafficJunction{name="fixture",direction=Vector3.forward,center=Vector3.zero,editorProgram=program};
   typeof(TrafficSystem).GetField("roadMap",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(s.traffic,new MapData());
   s.traffic.junctions.Add(j);var edge=new HdEdge{id="turn",ruleId="fixture",turn=-1,next=new int[0],points=new[]{new MapPoint{x=2,z=-22},new MapPoint{x=-20,z=0}}};s.jobs.nav.hd=new HdRoadGraph(new MapData{hdEdges=new[]{edge}});var route=new HdRoadGraph.RoutePath{path=s.jobs.nav.hd.paths[0]};route.edges.Add(0);route.ends.Add(route.path.length);s.jobs.nav.hdRoute=route;
   var driver=root.AddComponent<LiveTrafficValidationTest>();driver.externallyDriven=true;driver.session=s;
   var limit=typeof(LiveTrafficValidationTest).GetMethod("Limit",BindingFlags.NonPublic|BindingFlags.Instance);
   float red=(float)limit.Invoke(driver,new object[]{route.path,0f,8f});Check(red<8,"left-turn driver stops on left red even when through green");
   s.traffic.manualTime=11;float green=(float)limit.Invoke(driver,new object[]{route.path,0f,8f});Check(green==8,"left green permits the planned left while through red");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  Debug.Log("STABILIZATION_REGRESSION_OK");
 }
 public static void RunFinal(){Run();ProtoReviewRegressionTests.RunFinal();LiveCampaignTests.Run();UnityEngine.Debug.Log("STABILIZATION_EDITOR_OK");}
 public static void RunFacilities(){
  MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();FuelStationLayout.Apply(map);var graph=new HdRoadGraph(map);var site=RestaurantSiteLayout.For(map);
  var p=site.leave[site.leave.Length-1];var direction=(p-site.leave[site.leave.Length-2]).normalized;float at;int edge=graph.Nearest(p,direction,out at);
  Debug.Log("RESTAURANT_EXIT endpoint="+p+" heading="+direction+" lane="+edge+" at="+at+" approach="+site.approach.laneEdge+":"+site.approach.laneAt+" direction="+site.direction);
  float best=float.MaxValue;int nearest=-1;for(int i=0;i<graph.paths.Length;i++){if(graph.edges[i].disabled||graph.edges[i].change!=0)continue;float error;float q=graph.paths[i].Project(p,out error);if(error<best){best=error;nearest=i;Debug.Log("RESTAURANT_NEAR error="+error+" edge="+graph.edges[i].id+" facing="+Vector3.Dot(direction,graph.paths[i].Tangent(q))+" at="+q);}}
  Check(edge>=0,"restaurant departure joins an actual lane in the correct direction");
 }
 public static void RunFollowup(){ProtoReviewRegressionTests.Run();RunFacilities();}
}
