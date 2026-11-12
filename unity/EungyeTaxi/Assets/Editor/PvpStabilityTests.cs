using System;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PvpStabilityTests {
 public static void Observers(){
  var report=new System.Text.StringBuilder();
  foreach(Action test in new Action[]{ReviewedJunctionObserverTests.Run,UturnMapObserverTests.Run,AdditionalUturnObserverTests.Run,NativeLeftSignalRegressionTests.Run,TrafficSignalLeftDemandRegressionTests.Run}){
   try{test();report.AppendLine(test.Method.DeclaringType.Name+": PASS");}
   catch(Exception e){report.AppendLine(test.Method.DeclaringType.Name+": "+e.GetBaseException().Message);}
  }
  var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-observerOutput");string output=index>=0?args[index+1]:"../../artifacts/pvp-stability/observers-after.txt";
  System.IO.File.WriteAllText(output,report.ToString());Debug.Log(report.ToString());
 }
 public static void Run(){
  PhysicsBaseline();NearestPair();PvpNativePredictionTests.Run();PvpNativePoseTests.Run();PlaytestBalanceTests.Run();
  var j=new TrafficJunction{name="test"};string key=j.ApproachKey(0,1);
  if(key!="test:0:1"||!ReferenceEquals(key,j.ApproachKey(0,1))||j.ApproachKey(0,-1)==key||j.ApproachKey(1,1)==key)throw new Exception("Approach keys must be stable and distinct");
  j.name="renamed";if(j.ApproachKey(0,1)!="renamed:0:1")throw new Exception("Renamed junction retained stale key");
  Debug.Log("PVP_STABILITY_CORE_OK");
 }
 public static void NearestPair(){
  var graph=new HdRoadGraph(PvpCityMap.Create());int checkedPoints=0;
  for(int sample=0;sample<120;sample++){
   int source=sample*(graph.edges.Length-1)/119;var path=graph.paths[source];
   var point=path.Position(path.length*(sample%5)*.25f)+new Vector3((sample%7-3)*.47f,sample%4,(sample%9-4)*.33f);
   int expected=-1;float distance=float.MaxValue,second=float.MaxValue,expectedAt=0;
   for(int i=0;i<graph.edges.Length;i++){if(graph.edges[i].disabled)continue;float e;float at=graph.paths[i].Project(point,out e);if(e<distance){second=distance;distance=e;expected=i;expectedAt=at;}else second=Mathf.Min(second,e);}
   float actualAt,actualDistance,actualSecond;int actual=graph.NearestPair(point,out actualAt,out actualDistance,out actualSecond);
   if(actual!=expected||Mathf.Abs(actualAt-expectedAt)>.0001f||Mathf.Abs(actualDistance-distance)>.0001f||Mathf.Abs(actualSecond-second)>.0001f)throw new Exception("Nearest-pair disagrees with exhaustive lane projection at sample "+sample);
   checkedPoints++;
  }
  Debug.Log("PVP_NEAREST_PAIR_EQUIVALENT samples="+checkedPoints);
 }
 public static void CompareObservers(){Observers();if(System.IO.File.ReadAllText("../../artifacts/pvp-stability/observers-before.txt")!=System.IO.File.ReadAllText("../../artifacts/pvp-stability/observers-after.txt"))throw new Exception("Observer outcomes changed from baseline");Debug.Log("PVP_OBSERVER_BASELINE_EQUIVALENT (known failures are listed in report)");}
 public static void PhysicsBaseline(){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;
  var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(0,-.5f,0);ground.transform.localScale=new Vector3(1000,1,1000);
  var root=new GameObject("Stability taxi");var taxi=root.AddComponent<TaxiController>();taxi.Create(Vector3.up*.65f,Quaternion.identity);taxi.automated=true;
  try {
   Physics.SyncTransforms();
   for(int i=0;i<200;i++){taxi.SendMessage("FixedUpdate");Physics.Simulate(.02f);}
   float rest=taxi.body.position.y;taxi.body.velocity=Vector3.forward*20;taxi.autoThrottle=1;
   float low=float.MaxValue,high=float.MinValue,vertical=0,tilt=0;
   for(int i=0;i<500;i++){
    taxi.SendMessage("FixedUpdate");Physics.Simulate(.02f);
    if(i<100)continue;
    low=Mathf.Min(low,taxi.body.position.y);high=Mathf.Max(high,taxi.body.position.y);vertical=Mathf.Max(vertical,Mathf.Abs(taxi.body.velocity.y));tilt=Mathf.Max(tilt,Vector3.Angle(taxi.transform.up,Vector3.up));
   }
   Debug.Log("PVP_STABILITY_FLAT rest="+rest+" range="+(high-low)+" vertical="+vertical+" tilt="+tilt+" speed="+taxi.Speed);
   if(high-low>.08f||vertical>.5f||tilt>8)throw new Exception("Flat-road native taxi suspension is unstable");
  }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(ground);Physics.autoSimulation=automatic;}
 }
}
