using System;
using System.Linq;
using UnityEngine;

public static class RoundaboutFlowTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("ROUNDABOUT_FLOW "+message);}
 public static void Run(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();
  var control=map.editorDocument.facilities.Single(f=>f.kind==ReviewedRoundaboutControl.Kind&&f.junctionId=="J5074_877");
  var host=new GameObject("Roundabout render regression");var occupant=new GameObject("Roundabout gap fixture");
  try{
   RoadMapFacilities.Build(map.editorDocument,host.transform,null,map);
   Check(!host.GetComponentsInChildren<RoadMapLamp>().Any(l=>l.name=="사용자 시설 "+control.id),"control metadata rendered as a red signal");
   var centre=control.Position;var entry=centre+Vector3.forward*(control.width*.5f+2);
   occupant.layer=8;occupant.AddComponent<BoxCollider>().size=new Vector3(1.9f,1.4f,4.5f);
   Action<float> pose=degrees=>{float a=degrees*Mathf.Deg2Rad;var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));occupant.transform.position=centre+radial*13+Vector3.up*.7f;occupant.transform.forward=new Vector3(-radial.z,0,radial.x);Physics.SyncTransforms();};
   pose(-90);Check(ReviewedRoundaboutControl.EntryClear(map,control.junctionId,entry,Vector3.back,null),"distant opposite ring vehicle blocks entry");
   pose(80);Check(!ReviewedRoundaboutControl.EntryClear(map,control.junctionId,entry,Vector3.back,null),"near circulating vehicle must retain priority");
   pose(140);Check(ReviewedRoundaboutControl.EntryClear(map,control.junctionId,entry,Vector3.back,null),"departed circulating vehicle blocks entry");
   var moving=occupant.AddComponent<Rigidbody>();moving.useGravity=false;pose(0);moving.velocity=occupant.transform.forward*8;
   Check(!ReviewedRoundaboutControl.EntryClear(map,control.junctionId,entry,Vector3.back,null),"fast closing vehicle must retain priority");
   Check(ReviewedRoundaboutControl.EntryClear(map,control.junctionId,occupant.transform.position,occupant.transform.forward,occupant),"circulating self blocked");
   Check(ReviewedRoundaboutControl.EntryClear(map,"unrelated",entry,Vector3.back,null),"unrelated junction changed");
   Debug.Log("ROUNDABOUT_FLOW_OK metadata-not-lamp local-gap circulating-priority");
  }finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(occupant);Physics.SyncTransforms();}
 }
 public static void Motion(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var root=new GameObject("roundabout motion");bool auto=Physics.autoSimulation;
  try{
   var traffic=root.AddComponent<TrafficSystem>();typeof(TrafficSystem).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(traffic,null);traffic.Build(map,false);var district=root.AddComponent<DistrictTraffic>();district.Initialize(traffic,map,false);
   var graph=district.graph;int join=Array.FindIndex(graph.edges,e=>e.ruleId=="J5074_877"&&e.id.StartsWith("join:")&&e.turn==0&&!e.disabled);
   int from=Array.FindIndex(graph.edges,e=>!e.disabled&&e.next.Contains(join));int to=graph.edges[join].next.First();
   var route=district.CreateRoute(from,Mathf.Max(0,graph.paths[from].length-25),to,Mathf.Min(25,graph.paths[to].length));Check(route!=null,"actual circulating route missing");
   var stop=route.stops.Single(s=>s.junction.name=="J5074_877");var car=traffic.Spawn(route,Mathf.Max(0,stop.distance-12),99);Check(car,"actual route rejected");car.respawn=false;car.speed=5;
   Physics.autoSimulation=false;var tick=typeof(TrafficCar).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);float minimum=car.speed;
   for(int i=0;i<1000&&car.distance<stop.exitDistance;i++){tick.Invoke(car,null);Physics.Simulate(Time.fixedDeltaTime);minimum=Mathf.Min(minimum,car.speed);}
   Check(car.distance>=stop.exitDistance,"empty roundabout traversal stalled");Check(minimum>.5f,"empty entry forced a stop");
   Debug.Log("ROUNDABOUT_MOTION_OK actual-route min-speed="+minimum+" exit="+car.distance);
  }finally{Physics.autoSimulation=auto;UnityEngine.Object.DestroyImmediate(root);}
 }
 public static void BuildVerified(){Run();Motion();BankApproachRegression.Run();ConfirmedCorridorsR43.Verify();JunctionRouteFeedbackTests.VerifyMigration();PrototypeBuild.BuildHybrid();}
}
