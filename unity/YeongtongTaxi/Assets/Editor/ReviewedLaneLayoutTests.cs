using System;
using System.Linq;
using UnityEngine;

public static class ReviewedLaneLayoutTests {
 public static void BuildChecks(){
  Run();const string scenePath="Assets/Scenes/ReviewedLaneChecks.unity";
  if(System.IO.File.Exists(scenePath))throw new Exception("Check scene already exists");
  try{
   var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
   new GameObject("Drive Session").AddComponent<DriveSession>().startupRegion="eungye";
   UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath);
   var report=UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions{scenes=new[]{scenePath},locationPathName="../../artifacts/T1PocketChecks/EungyeTaxi.exe",target=UnityEditor.BuildTarget.StandaloneWindows64,options=UnityEditor.BuildOptions.None});
   if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Reviewed lane check build failed");
   Debug.Log("REVIEWED_LANE_CHECK_BUILD_OK");
  }finally{UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);UnityEditor.AssetDatabase.DeleteAsset(scenePath);}
 }
 static void Check(bool value,string message){if(!value)throw new Exception("REVIEWED_LANE_LAYOUT: "+message);}
 public static void Run(){
  string previousRegion=MapCatalog.LockedRegion;try{
  MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();
  var pocket=Array.FindIndex(map.hdEdges,e=>e.id=="reviewed:T1-SE:pocket");
  Check(pocket>=0,"T1 southeast approach has a separate short left-turn lane");
  var graph=new HdRoadGraph(map);
  var edge=map.hdEdges[pocket];
  Check(edge.number==1&&edge.next.Length==1&&map.hdEdges[edge.next[0]].turn==-1,"pocket permits left only");
  var through=map.hdEdges.First(e=>e.fromNode=="reviewed:T1-SE:split:0");
  Check(through.number==2&&!through.next.Any(i=>map.hdEdges[i].turn<0),"inner through lane no longer turns left");
  Check(!map.hdEdges.Any(e=>e.next.Any(i=>map.hdEdges[i].disabled)),"no route enters disabled old left join");
  int count=map.hdEdges.Length;ReviewedLaneLayout.Apply(map);Check(count==map.hdEdges.Length,"layout is not duplicated");
  int arrows=0;ReviewedLaneLayout.Paint(map,(name,p,d,w,l,y)=>arrows++);Check(arrows>=6,"left and through pavement arrows exist");
  var go=new GameObject("Reviewed lane clearance");int samples=0;
  try{var env=go.AddComponent<RoadEnvironment>();env.Build(map);
   for(int i=0;i<map.hdEdges.Length;i++)if(map.hdEdges[i].id.Contains("reviewed:T1-SE:")){var path=graph.paths[i];for(float d=0;d<=path.length;d+=1)foreach(float side in new[]{-.95f,0,.95f}){Check(env.OnRoad(path.Position(d)+TrafficSystem.Right(path.Tangent(d))*side),"vehicle-width pavement "+map.hdEdges[i].id);samples++;}}
   var system=go.AddComponent<TrafficSystem>();var district=go.AddComponent<DistrictTraffic>();district.Initialize(system,map,false);
   int taper=Array.FindIndex(map.hdEdges,e=>e.id=="reviewed:T1-SE:taper");int exit=map.hdEdges[edge.next[0]].next[0];
   var route=district.CreateRoute(taper,0,exit,Mathf.Min(10,graph.paths[exit].length));
   Check(route!=null&&route.stops.Any(s=>s.junction.name=="J4396_13458"&&s.turn==-1),"NPC sees the pocket as a left turn at the shared junction");
  }finally{UnityEngine.Object.DestroyImmediate(go);}
  foreach(var a in map.stops)foreach(var b in map.stops)if(a!=b)Check(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)!=null,"service route "+a.name+" > "+b.name);
  Debug.Log("REVIEWED_LANE_LAYOUT_TEST_OK edges="+map.hdEdges.Length+" pocketLength="+graph.paths[pocket].length+" clearanceSamples="+samples+" arrows="+arrows);
  }finally{MapCatalog.LockedRegion=previousRegion;}
 }
}
