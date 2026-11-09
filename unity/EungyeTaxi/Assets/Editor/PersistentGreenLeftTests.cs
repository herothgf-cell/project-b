using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Standalone position-controlled investigation of the reported empty-taxi queue.
// Does not enter play mode, change the map, or read/write campaign saves.
public static class PersistentGreenLeftTests {
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Set(object target,string field,object value){target.GetType().GetField(field,Private).SetValue(target,value);}
 static int[] Scan(TrafficSystem traffic,TrafficJunction junction){typeof(TrafficSystem).GetMethod("ScanSignalDemand",Private).Invoke(traffic,null);return ((Dictionary<TrafficJunction,int[]>)typeof(TrafficSystem).GetField("signalDemand",Private).GetValue(traffic))[junction];}
 public static void Diagnose(){Execute(false);}
 public static void Run(){Execute(true);}
 // Uses the actual parking destination setter and navigation refresh, together
 // with the production FindObjects driver discovery (no injected driver cache).
 public static void Lifecycle(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var host=new GameObject("persistent green lifecycle traffic");var taxiObject=new GameObject("empty taxi lifecycle fixture");taxiObject.SetActive(false);float scale=Time.timeScale;int checks=0,failures=0;
  try{
   var traffic=host.AddComponent<TrafficSystem>();traffic.Build(map,false);var driver=taxiObject.AddComponent<DriveSession>();driver.IsSimulationBot=true;driver.enabled=false;
   var taxi=taxiObject.AddComponent<TaxiController>();taxi.enabled=false;var indicators=taxiObject.AddComponent<TaxiSignals>();indicators.enabled=false;var jobs=taxiObject.AddComponent<PassengerJobs>();jobs.enabled=false;var nav=taxiObject.AddComponent<RoadNavigation>();nav.enabled=false;
   driver.taxi=taxi;driver.jobs=jobs;driver.traffic=traffic;jobs.nav=nav;taxiObject.SetActive(true);nav.Build(map,taxi.transform);
   var j=traffic.junctions.Single(x=>x.name=="J7917_2565");var graph=nav.hd;int incoming=Array.FindIndex(graph.edges,e=>e.id=="local:7322:2546:7735:2544");var lane=graph.paths[incoming];var parking=ProjectMapScene.Services(map,"parking").Single(o=>o.id=="service:parking");var stop=ProjectMapScene.ServiceStop(map,parking);
   int stage=Array.FindIndex(j.gameProgram.stages,s=>s.groups.Any(id=>j.gameProgram.groups.Any(g=>g.id==id&&!g.pedestrian&&g.axis==1&&g.sign==-1&&g.turn==-1)));
   foreach(string mode in new[]{"fresh-parking","after-free-guidance","restore-point","after-clear"}){
    nav.Clear();taxi.transform.SetPositionAndRotation(lane.Position(lane.length-35)+Vector3.up*.464f,Quaternion.LookRotation(lane.Tangent(lane.length-35)));
    if(mode=="after-free-guidance")nav.StartFreeGuidance();
    if(mode=="restore-point"){nav.SetDestination(stop);var saved=nav.destination;nav.Clear();nav.SetDestination(saved);}else{if(mode=="after-clear"){nav.SetDestination(stop);nav.Clear();}nav.SetDestination(stop);}
    foreach(float back in new[]{35f,20f,10f,6f,4f,3f}){
     float at=lane.length-back;taxi.transform.SetPositionAndRotation(lane.Position(at)+Vector3.up*.464f,Quaternion.LookRotation(lane.Tangent(at)));
     for(int refresh=0;refresh<4;refresh++){
      Set(nav,"nextHdCosts",float.NegativeInfinity);nav.Refresh();int off=Scan(traffic,j)[stage];indicators.Toggle(-1);int on=Scan(traffic,j)[stage];indicators.Clear();checks+=2;
      float error,progress=nav.ProjectVehicle(out error);var cue=NavigationCue.FromRoute(graph,nav.hdRoute,progress);bool wantsLeft=cue.Valid&&cue.Move==NavigationMove.Left&&cue.Distance<=40;
      if(on==0||wantsLeft&&off==0)failures++;
      var route=nav.hdRoute;int current=route==null?-1:route.EdgeAt(progress);float goalError=nav.hdGoal<0?-1:Vector3.Distance(graph.paths[nav.hdGoal].Position(nav.hdGoalAt),nav.destination);
      Debug.Log("PERSISTENT_GREEN_LIFECYCLE mode="+mode+" back="+back+" refresh="+refresh+" active="+nav.active+" free="+nav.freeGuidance+" retaining="+nav.RetainingPreviousRoute+" cue="+cue.Move+" cueDistance="+cue.Distance+" remaining="+nav.remaining+" edges="+(route==null?0:route.edges.Count)+" current="+(current<0?"none":graph.edges[current].id)+" goal="+nav.hdGoal+" goalError="+goalError+" Qoff="+off+" Qon="+on);
     }
    }
   }
   Debug.Log("PERSISTENT_GREEN_LIFECYCLE checks="+checks+" failures="+failures);
   if(failures>0)throw new Exception("PERSISTENT_GREEN_LIFECYCLE: "+failures+" discovered driver/current parking route queue failures");
  }finally{UnityEngine.Object.DestroyImmediate(taxiObject);UnityEngine.Object.DestroyImmediate(host);Time.timeScale=scale;}
 }
 static void Execute(bool assert){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var host=new GameObject("persistent green left investigation");var taxiObject=new GameObject("inactive empty taxi demand fixture");taxiObject.SetActive(false);int failures=0,checks=0;
  try{
   var traffic=host.AddComponent<TrafficSystem>();traffic.Build(map,false);
   var driver=taxiObject.AddComponent<DriveSession>();var taxi=taxiObject.AddComponent<TaxiController>();var indicators=taxiObject.AddComponent<TaxiSignals>();var jobs=taxiObject.AddComponent<PassengerJobs>();var nav=taxiObject.AddComponent<RoadNavigation>();
   driver.taxi=taxi;driver.jobs=jobs;driver.traffic=traffic;jobs.nav=nav;nav.hd=new HdRoadGraph(map);nav.vehicle=taxi.transform;Set(traffic,"signalDrivers",new[]{driver});Set(traffic,"nextDriverScan",float.PositiveInfinity);
   var j=traffic.junctions.Single(x=>x.name=="J7917_2565");var graph=nav.hd;int incoming=Array.FindIndex(graph.edges,e=>e.id=="local:7322:2546:7735:2544"),left=Array.FindIndex(graph.edges,e=>e.id=="join:local:7322:2546:7735:2544:-1");int exit=graph.edges[left].next.First(n=>!graph.edges[n].disabled);
   var lane=graph.paths[incoming];var heading=lane.Tangent(lane.length);var side=TrafficSystem.Right(heading);var approach=traffic.AxisDirection(j,1)*-1;
   int stage=Array.FindIndex(j.gameProgram.stages,s=>s.groups.Any(id=>j.gameProgram.groups.Any(g=>g.id==id&&!g.pedestrian&&g.axis==1&&g.sign==-1&&g.turn==-1)));
   if(stage<0)throw new Exception("PERSISTENT_GREEN: reported approach has no protected left stage");
   float planeAt=Vector3.Dot(traffic.StopPosition(j,1,-1)-lane.Position(0),approach)/Vector3.Dot(heading,approach);
   foreach(float back in new[]{6f,lane.length-planeAt+2.2f})foreach(float lateral in new[]{0f,-1.5f,1.5f,-1.7f,1.7f}){
    float at=lane.length-back;taxi.transform.SetPositionAndRotation(lane.Position(at)+side*lateral+Vector3.up*.464f,Quaternion.LookRotation(heading));
    nav.active=true;nav.freeGuidance=false;nav.hdGoal=exit;nav.hdGoalAt=graph.paths[exit].length;nav.destination=graph.paths[exit].Position(nav.hdGoalAt);nav.hdRoute=graph.Compose(new List<int>{incoming,left,exit},Mathf.Max(0,at-15),nav.hdGoalAt);nav.path.Clear();nav.path.AddRange(nav.hdRoute.path.points);
    float nearestAt;int current=graph.Nearest(taxi.transform.position,taxi.transform.forward,out nearestAt);float projectionError;nav.hdRoute.path.Project(taxi.transform.position,out projectionError);
    indicators.Clear();int off=Scan(traffic,j)[stage];indicators.Toggle(-1);int on=Scan(traffic,j)[stage];indicators.Clear();checks+=2;if(off==0||on==0)failures++;
    float frontGap=Vector3.Dot(traffic.StopPosition(j,1,-1)-(taxi.transform.position+heading*2),approach);
    Debug.Log("PERSISTENT_GREEN_QUEUE back="+back+" lateral="+lateral+" pose="+taxi.transform.position.ToString("F4")+" frontGap="+frontGap+" nearest="+(current<0?"none":graph.edges[current].id)+" nearestAt="+nearestAt+" routeError="+projectionError+" Qoff="+off+" Qon="+on);
    // No other vehicles/pedestrians are fabricated. The taxi alone must request
    // the legal existing left stage while standing inside its current lane.
    if(off>0){var counts=(int[])Scan(traffic,j).Clone();var clock=new DemandSignalClock(j.gameProgram);bool green=false;for(int tick=0;tick<1200;tick++){clock.Step(.1f,counts,false);if(j.gameProgram.Movement(1,-1,-1,clock.ProgramTime)==SignalColor.Green){green=true;break;}}checks++;if(!green)failures++;}
   }
   Debug.Log("PERSISTENT_GREEN_DIAGNOSTIC checks="+checks+" failures="+failures);
   if(assert&&failures>0)throw new Exception("PERSISTENT_GREEN: "+failures+" legal queue poses fail left demand or eventual green");
  }finally{UnityEngine.Object.DestroyImmediate(taxiObject);UnityEngine.Object.DestroyImmediate(host);}
 }
}
