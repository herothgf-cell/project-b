using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
// Actual PhysX stepping on an isolated flat fixture, not path/pose interpolation.
public static class CasualDrivePhysics {
 [Serializable] public class Sample {public string test;public float speed,distance,seconds,peakDecel,filteredDecel,yaw,peakRoll;public bool stopped,reverse;}
 [Serializable] public class Report {public string label;public List<Sample> samples=new List<Sample>();}
 static readonly MethodInfo tick=typeof(TaxiController).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
 public static void Run(){
  string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-handlingLabel");string label=i>=0?args[i+1]:"current";
  bool auto=Physics.autoSimulation;Physics.autoSimulation=false;var report=new Report{label=label};var root=new GameObject("isolated handling fixture");
  try{
   var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform);ground.transform.position=new Vector3(20000,-.5f,20000);ground.transform.localScale=new Vector3(1000,1,1000);
   foreach(float speed in new[]{20f,30f,40f})report.samples.Add(Brake(root,speed));
   foreach(float speed in new[]{20f,30f,60f})foreach(int side in new[]{-1,1})report.samples.Add(Corner(root,speed,side));
   Directory.CreateDirectory("../../artifacts");File.WriteAllText("../../artifacts/casual-physics-"+label+".json",JsonUtility.ToJson(report,true));
   foreach(var s in report.samples)Debug.Log("CASUAL_PHYSICS "+JsonUtility.ToJson(s));Debug.Log("CASUAL_PHYSICS_OK samples="+report.samples.Count);
  }finally{UnityEngine.Object.DestroyImmediate(root);Physics.autoSimulation=auto;}
 }
 public static void PoweredCorners(){powered=true;try{Run();}finally{powered=false;}}
 static bool powered;
 static TaxiController Taxi(GameObject root){var go=new GameObject("actual wheel taxi");go.transform.SetParent(root.transform);var taxi=go.AddComponent<TaxiController>();taxi.Create(new Vector3(20000,.5f,20000),Quaternion.identity);taxi.automated=true;taxi.AutomatedHumanInput=true;taxi.autoBrake=true;Physics.SyncTransforms();for(int i=0;i<80;i++)Step(taxi);taxi.autoBrake=false;return taxi;}
 static void Step(TaxiController taxi){tick.Invoke(taxi,null);Physics.Simulate(.02f);Physics.SyncTransforms();}
 static Sample Brake(GameObject root,float speed){
  var taxi=Taxi(root);taxi.body.velocity=Vector3.forward*speed/3.6f;taxi.autoThrottle=-1;var start=taxi.body.position;var s=new Sample{test="service-stop",speed=speed};float old=speed/3.6f,filtered=0;
  for(int i=0;i<600;i++){Step(taxi);float now=taxi.Speed;float accel=(now-old)/.02f;filtered=Mathf.Lerp(filtered,accel,1-Mathf.Exp(-.02f/.25f));s.peakDecel=Mathf.Min(s.peakDecel,accel);s.filteredDecel=Mathf.Min(s.filteredDecel,filtered);old=now;s.seconds=(i+1)*.02f;if(now<.3f){s.stopped=true;break;}}
  s.distance=Vector3.Distance(start,taxi.body.position);for(int i=0;i<100;i++)Step(taxi);s.reverse=taxi.Speed<-.3f;UnityEngine.Object.DestroyImmediate(taxi.gameObject);return s;
 }
 static Sample Corner(GameObject root,float speed,int side){
  var taxi=Taxi(root);taxi.body.velocity=Vector3.forward*speed/3.6f;taxi.autoThrottle=powered?1:0;taxi.autoSteer=side;var start=taxi.body.position;var s=new Sample{test=side<0?"left":"right",speed=speed};float yaw=0,previous=taxi.body.rotation.eulerAngles.y;
  for(int i=0;i<(powered?400:250);i++){Step(taxi);float angle=taxi.body.rotation.eulerAngles.y;yaw+=Mathf.DeltaAngle(previous,angle);previous=angle;s.peakRoll=Mathf.Max(s.peakRoll,Vector3.Angle(taxi.transform.up,Vector3.up));s.seconds=(i+1)*.02f;if(Mathf.Abs(yaw)>=90)break;}
  s.yaw=yaw;s.distance=Vector3.Distance(start,taxi.body.position);UnityEngine.Object.DestroyImmediate(taxi.gameObject);return s;
 }
}
