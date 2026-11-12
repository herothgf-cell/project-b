using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Position-controlled real-map demand regression; no save IO or player input.
public static class TrafficSignalLeftDemandRegressionTests {
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static int checks;
 static void Check(bool value,string reason){checks++;if(!value)throw new Exception("LEFT_DEMAND: "+reason);}
 static void Set(object target,string name,object value){target.GetType().GetField(name,Private).SetValue(target,value);}
 static int Stage(TrafficJunction j,int turn){return Array.FindIndex(j.gameProgram.stages,s=>s.groups.Any(id=>j.gameProgram.groups.Any(g=>g.id==id&&!g.pedestrian&&g.axis==1&&g.sign==-1&&g.turn==turn)));}
 static int[] Scan(TrafficSystem system,TrafficJunction j){typeof(TrafficSystem).GetMethod("ScanSignalDemand",Private).Invoke(system,null);return ((Dictionary<TrafficJunction,int[]>)typeof(TrafficSystem).GetField("signalDemand",Private).GetValue(system))[j];}
 public static void Baseline(){RunCore(true);}
 public static void Red(){Baseline();Run();}
 public static void Run(){RunCore(false);}
 static void RunCore(bool baseline){
  checks=0;var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var host=new GameObject("left demand regression");var driverObject=new GameObject("inactive demand fixture");driverObject.SetActive(false);
  try{
   var system=host.AddComponent<TrafficSystem>();system.Build(map,false);
   var driver=driverObject.AddComponent<DriveSession>();var taxi=driverObject.AddComponent<TaxiController>();var indicators=driverObject.AddComponent<TaxiSignals>();var nav=driverObject.AddComponent<RoadNavigation>();var jobs=driverObject.AddComponent<PassengerJobs>();
   driver.traffic=system;driver.taxi=taxi;driver.jobs=jobs;jobs.nav=nav;nav.vehicle=taxi.transform;nav.hd=new HdRoadGraph(map);
   Set(system,"signalDrivers",new[]{driver});Set(system,"nextDriverScan",float.PositiveInfinity);
   foreach(var entry in new[]{new[]{"J7917_2565","local:7322:2546:7735:2544","join:local:7322:2546:7735:2544:-1"},new[]{"J7973_3744","A2218G610050:0","A2218G611104:0"}}){
    var j=system.junctions.Single(x=>x.name==entry[0]);int incoming=Array.FindIndex(map.hdEdges,e=>e.id==entry[1]),left=Array.FindIndex(map.hdEdges,e=>e.id==entry[2]);
    Check(incoming>=0&&left>=0,"real incoming and connector "+j.name);var graph=nav.hd;int exit=graph.edges[left].next.First(n=>!graph.edges[n].disabled);float at=Mathf.Max(0,graph.paths[incoming].length-4);
    taxi.transform.SetPositionAndRotation(graph.paths[incoming].Position(at),Quaternion.LookRotation(graph.paths[incoming].Tangent(at)));
    Check(system.Sign(taxi.transform.forward,1,j)==-1&&Vector3.Dot(taxi.transform.forward,-system.AxisDirection(j,1))>.8f,"reported approach axis/sign "+j.name);
    nav.active=true;nav.freeGuidance=false;nav.hdGoal=exit;nav.hdGoalAt=graph.paths[exit].length;nav.destination=graph.paths[exit].Position(nav.hdGoalAt);nav.hdRoute=graph.Compose(new List<int>{incoming,left,exit},at,nav.hdGoalAt);nav.path.Clear();nav.path.AddRange(nav.hdRoute.path.points);
    int stage=Stage(j,-1);Check(stage>=0&&system.HasLeftMovement(j,1,-1),"existing left program authority "+j.name);
    indicators.Clear();indicators.Toggle(-1);var qOn=Scan(system,j);Check(qOn[stage]>0,"Q-on detects actual approach "+j.name);
    indicators.Clear();var qOff=Scan(system,j);Debug.Log("LEFT_DEMAND_BASELINE junction="+j.name+" Qoff="+qOff[stage]+" incoming="+entry[1]+" connector="+entry[2]);
    if(baseline)continue;
    Check(qOff[stage]>0,"Q-off valid current left route requests protected stage "+j.name);
    var program=JsonUtility.ToJson(j.gameProgram);var clock=(DemandSignalClock)typeof(TrafficSystem).GetMethod("DemandClock",Private).Invoke(system,new object[]{j});var competing=(int[])qOff.Clone();for(int i=0;i<competing.Length;i++)if(i!=stage)competing[i]=1;
    bool green=false;for(int tick=0;tick<1200;tick++){clock.Step(.1f,competing,false);if(system.LeftSignal(j,1,-1)==SignalColor.Green){green=true;break;}}Check(green,"Q-off real left reaches protected green with competing demand "+j.name);
    Check(system.CaptureSignalRuntime().Single(s=>s.junction==j.name).demand[stage]>0,"host captures consumed left demand "+j.name);
    Guards(system,j,taxi,indicators,nav,incoming,left,exit,at,stage);
    Check(JsonUtility.ToJson(j.gameProgram)==program,"authored program and safety timings unchanged "+j.name);
   }
   if(!baseline)CheckJ1927(system,nav,taxi,indicators);
   Debug.Log((baseline?"LEFT_DEMAND_BASELINE_OK":"LEFT_DEMAND_REGRESSION_OK")+" checks="+checks);
  }finally{UnityEngine.Object.DestroyImmediate(driverObject);UnityEngine.Object.DestroyImmediate(host);}
 }
 static void CheckJ1927(TrafficSystem system,RoadNavigation nav,TaxiController taxi,TaxiSignals indicators){
  var j=system.junctions.Single(x=>x.name=="J1927_5181");
  var graph=nav.hd;
  int portal=Array.FindIndex(graph.edges,e=>e.id=="portal:A222BC004232:0:in");
  int left=Array.FindIndex(graph.edges,e=>e.id=="A222BC004192:0");
  var pose=new Vector3(201.78096f,0,545.7985f);
  float error,at=graph.paths[portal].Project(pose,out error);
  int exit=graph.edges[left].next.First();
  nav.active=true;nav.freeGuidance=false;nav.hdGoal=exit;nav.hdGoalAt=graph.paths[exit].length;
  nav.destination=graph.paths[exit].Position(nav.hdGoalAt);
  nav.hdRoute=graph.Compose(new List<int>{portal,left,exit},Mathf.Max(0,at-4),nav.hdGoalAt);
  nav.path.Clear();nav.path.AddRange(nav.hdRoute.path.points);
  taxi.transform.SetPositionAndRotation(pose,Quaternion.Euler(0,212.90007f,0));
  int stage=Array.FindIndex(j.gameProgram.stages,s=>s.groups.Any(id=>j.gameProgram.groups.Any(g=>g.id==id&&!g.pedestrian&&g.axis==0&&g.sign==1&&g.turn==-1)));
  Check(stage>=0,"J1927 reported approach has a protected stage");
  indicators.Clear();indicators.Toggle(-1);
  Check(Scan(system,j)[stage]>0,"J1927 reported pose registers Q-on demand");
  indicators.Clear();
  Check(Scan(system,j)[stage]>0,"J1927 existing left route registers demand with Q off");
 }
 static void Guards(TrafficSystem system,TrafficJunction j,TaxiController taxi,TaxiSignals indicators,RoadNavigation nav,int incoming,int left,int exit,float at,int stage){
  var graph=nav.hd;var route=nav.hdRoute;var pose=taxi.transform.position;var rotation=taxi.transform.rotation;bool dedicated=graph.edges[incoming].next.Where(n=>!graph.edges[n].disabled).All(n=>graph.edges[n].turn==-1);
  Action<string> absent=reason=>Check(Scan(system,j)[stage]==0,reason+" "+j.name);
  Action<string> navigationUnavailable=reason=>Check(Scan(system,j)[stage]==(dedicated?1:0),reason+(dedicated?" retains actual dedicated-lane demand ":" rejects shared-lane inference ")+j.name);
  indicators.Clear();indicators.Toggle(1);absent("explicit right indicator overrides inferred left");indicators.Clear();
  nav.active=false;navigationUnavailable("no active destination");nav.active=true;
  nav.freeGuidance=true;navigationUnavailable("free guidance");nav.freeGuidance=false;
  // A retained line has no current path. Keep the real route object present to
  // prove that the old line/route cannot stand in for current guidance.
  var display=(List<Vector3>)typeof(RoadNavigation).GetField("previousDisplay",Private).GetValue(nav);
  display.AddRange(nav.path);nav.path.Clear();navigationUnavailable("retained previous route");nav.path.AddRange(display);display.Clear();
  nav.hdRoute=null;navigationUnavailable("missing current route");nav.hdRoute=route;
  nav.hdGoal=incoming;navigationUnavailable("stale route for previous destination");nav.hdGoal=exit;
  var destination=nav.destination;nav.destination+=Vector3.right*20;navigationUnavailable("stale route endpoint");nav.destination=destination;
  float originalGoalAt=nav.hdGoalAt;nav.hdGoalAt=float.NaN;navigationUnavailable("NaN route goal offset");nav.hdGoalAt=float.PositiveInfinity;navigationUnavailable("infinite route goal offset");nav.hdGoalAt=originalGoalAt;
  nav.destination=new Vector3(float.NaN,0,0);navigationUnavailable("NaN route destination");nav.destination=destination;
  int routedIncoming=route.edges[0];route.edges[0]=(incoming+1)%graph.edges.Length;navigationUnavailable("route on a different current lane");route.edges[0]=routedIncoming;
  var chain=new List<int>{incoming,left,exit};for(int k=0;k<2;k++)chain.Add(graph.edges[chain[chain.Count-1]].next.First(n=>!graph.edges[n].disabled&&!chain.Contains(n)));
  nav.hdGoal=chain[chain.Count-1];nav.hdGoalAt=graph.paths[nav.hdGoal].length;nav.destination=graph.paths[nav.hdGoal].Position(nav.hdGoalAt);nav.hdRoute=graph.Compose(chain,at,nav.hdGoalAt);
  Check(Scan(system,j)[stage]>0,"valid extended current route retains left demand "+j.name);
  int future=chain[3];graph.edges[future].disabled=true;navigationUnavailable("disabled distant route edge");graph.edges[future].disabled=false;
  var futureNext=graph.edges[future].next;graph.edges[future].next=new int[0];navigationUnavailable("broken distant route connection");graph.edges[future].next=futureNext;
  nav.hdRoute=route;nav.hdGoal=exit;nav.hdGoalAt=originalGoalAt;nav.destination=destination;
  if(!dedicated){
   int straight=graph.edges[incoming].next.First(n=>!graph.edges[n].disabled&&graph.edges[n].turn==0);int straightExit=graph.edges[straight].next.First(n=>!graph.edges[n].disabled);float goalAt=nav.hdGoalAt;
   nav.hdGoal=straightExit;nav.hdGoalAt=graph.paths[straightExit].length;nav.destination=graph.paths[straightExit].Position(nav.hdGoalAt);nav.hdRoute=graph.Compose(new List<int>{incoming,straight,straightExit},at,nav.hdGoalAt);
   absent("shared lane with a valid straight route cannot infer left");nav.hdRoute=route;nav.hdGoal=exit;nav.hdGoalAt=goalAt;nav.destination=destination;
  }
  taxi.transform.position=pose+TrafficSystem.Right(taxi.transform.forward)*8;absent("off-road projection cannot infer left");taxi.transform.position=pose;
  taxi.transform.rotation=Quaternion.LookRotation(-taxi.transform.forward);absent("wrong approach heading cannot infer left");taxi.transform.rotation=rotation;
  taxi.transform.position=j.center+taxi.transform.forward*4;absent("passed junction cannot infer left");taxi.transform.position=pose;
  graph.edges[left].disabled=true;absent("disabled connector cannot infer left");graph.edges[left].disabled=false;
  graph.edges[incoming].disabled=true;absent("disabled current lane cannot infer left");graph.edges[incoming].disabled=false;
  graph.edges[exit].disabled=true;absent("disabled exit cannot infer left");graph.edges[exit].disabled=false;
  int movement=graph.edges[left].turn;graph.edges[left].turn=UturnMovement.Turn;absent("U-turn connector cannot infer protected left");graph.edges[left].turn=movement;
  var next=graph.edges[incoming].next;graph.edges[incoming].next=new int[0];absent("connector proximity without directed link cannot infer left");graph.edges[incoming].next=next;
  var center=j.center;j.center=pose+taxi.transform.forward*95;absent("far approach cannot infer left");j.center=center;
  string rule=graph.edges[left].ruleId;graph.edges[left].ruleId="another-junction";string status=graph.edges[left].status;graph.edges[left].status="provisional";absent("another junction connector cannot infer left");graph.edges[left].ruleId=rule;graph.edges[left].status=status;
  bool allowed=j.facility.allowLeft;j.facility.allowLeft=false;absent("explicit junction prohibition cannot infer left");j.facility.allowLeft=allowed;
  Check(Scan(system,j)[stage]>0,"guard restoration returns real left demand "+j.name);
 }
}
