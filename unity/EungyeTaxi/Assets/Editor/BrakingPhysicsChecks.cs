using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

// Native WheelCollider/PhysX stopping, independent of routes or position control.
public static class BrakingPhysicsChecks {
 [Serializable] public sealed class Sample {public float gain,distance,seconds,peakDeceleration,initialRpm,firstBrakeTorque;public bool emergency,human,reverse,stopped,unintendedReverse;}
 [Serializable] public sealed class Report {public List<Sample> samples=new List<Sample>();}
 static readonly MethodInfo tick=typeof(TaxiController).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
 static void Check(bool value,string text){if(!value)throw new Exception("BRAKING_PHYSICS "+text);}
 static void Step(TaxiController taxi){tick.Invoke(taxi,null);Physics.Simulate(.02f);Physics.SyncTransforms();}
 static Sample Stop(GameObject root,float gain,bool emergency,bool human,bool reverse=false){
  var go=new GameObject("physical braking taxi");go.transform.SetParent(root.transform);var taxi=go.AddComponent<TaxiController>();
  try{
   taxi.Create(new Vector3(20000,.5f,20000),Quaternion.identity);taxi.automated=true;taxi.AutomatedHumanInput=human;taxi.autoBrake=true;Physics.SyncTransforms();for(int i=0;i<80;i++)Step(taxi);
   // Roll the wheels up to speed first. Injecting body velocity leaves RPM0
   // and starts with locked-wheel friction before the brake input is tested.
   taxi.ManualBrakeGain=gain;taxi.autoBrake=false;taxi.autoThrottle=reverse?-1:1;float target=(reverse?20:30)/3.6f;
   for(int i=0;i<1500&&Mathf.Abs(taxi.Speed)<target;i++)Step(taxi);
   Check(Mathf.Abs(taxi.Speed)>=target-.1f,"fixture reaches speed with rolling wheels");
   var start=taxi.body.position;float previous=Mathf.Abs(taxi.Speed);var result=new Sample{gain=gain,emergency=emergency,human=human,reverse=reverse,initialRpm=taxi.wheels[0].rpm};
   taxi.autoBrake=emergency;taxi.autoThrottle=reverse?1:-1;
   for(int i=0;i<1000;i++){Step(taxi);if(i==0)result.firstBrakeTorque=taxi.wheels[0].brakeTorque;float now=Mathf.Abs(taxi.Speed);result.peakDeceleration=Mathf.Max(result.peakDeceleration,(previous-now)/.02f);previous=now;result.seconds=(i+1)*.02f;if(now<.3f){result.stopped=true;break;}}
   result.distance=Vector3.Distance(start,taxi.body.position);for(int i=0;i<100;i++)Step(taxi);result.unintendedReverse=reverse?taxi.Speed>.3f:taxi.Speed<-.3f;return result;
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 public static void Run(){
  bool auto=Physics.autoSimulation;Physics.autoSimulation=false;var root=new GameObject("isolated braking physics");var report=new Report();
  try{
   var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform);ground.transform.position=new Vector3(20000,-.5f,20000);ground.transform.localScale=new Vector3(1000,1,1000);
   foreach(float gain in new[]{.5f,1f,1.5f})report.samples.Add(Stop(root,gain,false,true));
   report.samples.Add(Stop(root,1,false,true,true));
   foreach(float gain in new[]{.5f,1.5f})report.samples.Add(Stop(root,gain,true,true));
   foreach(float gain in new[]{.5f,1.5f})report.samples.Add(Stop(root,gain,false,false));
   Directory.CreateDirectory("../../artifacts");File.WriteAllText("../../artifacts/braking-physics.json",JsonUtility.ToJson(report,true));
   foreach(var sample in report.samples){Debug.Log("BRAKING_PHYSICS_SAMPLE "+JsonUtility.ToJson(sample));Check(sample.stopped,"all test conditions stop");if(sample.human)Check(!sample.unintendedReverse,"held brake never automatically changes gear");}
   Check(report.samples[0].distance>report.samples[1].distance*1.1f,"lower pressure increases actual stopping distance");
   Check(report.samples[0].firstBrakeTorque<report.samples[1].firstBrakeTorque*.51f&&report.samples[2].firstBrakeTorque>report.samples[1].firstBrakeTorque*1.49f,"three pressure settings reach native wheels");
   Check(Mathf.Abs(report.samples[4].distance-report.samples[5].distance)<.01f,"emergency independent of preferences");
   Check(Mathf.Abs(report.samples[6].distance-report.samples[7].distance)<.01f,"bot braking independent of preferences");
   Debug.Log("BRAKING_PHYSICS_OK samples="+report.samples.Count);
  }finally{UnityEngine.Object.DestroyImmediate(root);Physics.autoSimulation=auto;}
 }
}
