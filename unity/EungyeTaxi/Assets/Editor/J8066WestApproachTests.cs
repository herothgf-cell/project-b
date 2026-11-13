using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class J8066WestApproachTests {
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 public static void Diagnose(){Run(false);}
 public static void Verify(){Geometry();Run(true);FuelStationLineageTests.Run();JunctionRouteFeedbackTests.VerifyMigration();JunctionRouteFeedbackTests.Run();Debug.Log("J8066_FEEDBACK_REGRESSION_OK");}
 static void Geometry(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();var paint=new RoadPaintGeometry(map);
  var reference=map.hdEdges.First(e=>e.id==J8066EastArmMapEdit.West);
  foreach(float x in new[]{845f,875f,960f}){
   var lanes=map.hdEdges.Where(e=>!e.disabled&&e.status=="provisional"&&e.turn==0&&e.change==0&&!e.id.StartsWith("join:")&&e.sourceId==reference.sourceId&&e.points.Min(p=>p.x)<x&&e.points.Max(p=>p.x)>x&&e.points.Min(p=>p.z)<655&&e.points.Max(p=>p.z)>615).Where(e=>e.points.Zip(e.points.Skip(1),(a,b)=>new[]{a,b}).Any(ab=>(ab[0].x-x)*(ab[1].x-x)<=0&&ab.All(p=>p.z>610&&p.z<660))).ToArray();
   int west=lanes.Count(e=>e.points.Zip(e.points.Skip(1),(a,b)=>new[]{a,b}).Any(ab=>(ab[0].x-x)*(ab[1].x-x)<=0&&ab.All(p=>p.z>610&&p.z<660)&&ab[1].x<ab[0].x)),east=lanes.Length-west;
   if(west!=(x<900?2:1)||east!=(x<900?1:2))throw new Exception("J8066 cross section "+x+" W="+west+" E="+east);
   Debug.Log("J8066_CROSS_SECTION x="+x+" W="+west+" E="+east);
  }
  var arrows=LaneDirectionPaint.Layout(map);
  foreach(string id in new[]{J8066EastArmMapEdit.Pocket,J8066EastArmMapEdit.ApartmentPocket})if(!arrows.Any(a=>map.hdEdges[a.edgeIndex].id==id&&a.turns==LaneDirectionPaint.Left))throw new Exception("Missing left-only arrow "+id);
  var stops=PassengerStopCatalog.For(map).Stops;if(stops.Length!=39)throw new Exception("Passenger stop catalog changed");
 }
 static void Run(bool assert){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();EungyeConnectivityRepair.Apply(map,package.baseline);
  var host=new GameObject("J8066 west approach test");var obj=new GameObject("inactive signal driver");obj.SetActive(false);
  try{
   var traffic=host.AddComponent<TrafficSystem>();traffic.Build(map,false);var driver=obj.AddComponent<DriveSession>();var taxi=obj.AddComponent<TaxiController>();var signals=obj.AddComponent<TaxiSignals>();var jobs=obj.AddComponent<PassengerJobs>();var nav=obj.AddComponent<RoadNavigation>();
   driver.taxi=taxi;driver.jobs=jobs;driver.traffic=traffic;jobs.nav=nav;nav.hd=new HdRoadGraph(map);nav.vehicle=taxi.transform;
   typeof(TrafficSystem).GetField("signalDrivers",Private).SetValue(traffic,new[]{driver});typeof(TrafficSystem).GetField("nextDriverScan",Private).SetValue(traffic,float.PositiveInfinity);
   var j=traffic.junctions.Single(x=>x.name=="J8066_6364");var g=nav.hd;
   taxi.transform.SetPositionAndRotation(new Vector3(834.2589f,.464f,637.2146f),Quaternion.Euler(0,267.310242f,0));
   float at;int start=g.Nearest(taxi.transform.position,taxi.transform.forward,out at),left=Array.FindIndex(g.edges,e=>e.id=="join:local:9032:10757:8270:6402:-1"),exit=g.edges[left].next[0];
   nav.active=true;nav.freeGuidance=false;nav.hdGoal=exit;nav.hdGoalAt=80;nav.destination=g.paths[exit].Position(80);nav.hdRoute=g.Route(start,at,exit,80);if(nav.hdRoute!=null)nav.path.AddRange(nav.hdRoute.path.points);
   int axis=Vector3.Dot(taxi.transform.forward,traffic.AxisDirection(j,0))*Vector3.Dot(taxi.transform.forward,traffic.AxisDirection(j,0))>.5f?0:1;int sign=traffic.Sign(taxi.transform.forward,axis,j);
   int stage=Array.FindIndex(j.gameProgram.stages,s=>s.groups.Any(id=>j.gameProgram.groups.Any(q=>q.id==id&&!q.pedestrian&&q.axis==axis&&q.sign==sign&&q.turn==-1)));
   float error; if(start>=0)g.paths[start].Project(taxi.transform.position,out error);else error=-1;
   Debug.Log("J8066_WEST start="+(start<0?"none":g.edges[start].id)+" error="+error+" at="+at+" axis="+axis+" sign="+sign+" stage="+stage+" left="+traffic.HasLeftMovement(j,axis,sign)+" normal="+traffic.StopNormal(j,axis,sign)+" stop="+traffic.StopPosition(j,axis,sign)+" route="+(nav.hdRoute==null?"none":string.Join("|",nav.hdRoute.edges.Select(i=>g.edges[i].id))));
   foreach(bool indicator in new[]{false,true}){
    signals.Clear();if(indicator)signals.Toggle(-1);typeof(TrafficSystem).GetMethod("ScanSignalDemand",Private).Invoke(traffic,null);
    var counts=((Dictionary<TrafficJunction,int[]>)typeof(TrafficSystem).GetField("signalDemand",Private).GetValue(traffic))[j];int demand=stage<0?0:counts[stage];bool green=false;
    var clock=new DemandSignalClock(j.gameProgram);for(int i=0;i<1800;i++){clock.Step(.1f,counts,false);if(j.gameProgram.Movement(axis,sign,-1,clock.ProgramTime)==SignalColor.Green){green=true;break;}}
    Debug.Log("J8066_WEST indicator="+indicator+" demand="+demand+" green="+green);
    if(assert&&(demand==0||!green))throw new Exception("Reported westbound left never requested/served");
   }
  }finally{UnityEngine.Object.DestroyImmediate(obj);UnityEngine.Object.DestroyImmediate(host);}
 }
}
