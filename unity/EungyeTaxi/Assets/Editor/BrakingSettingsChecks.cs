using System;
using UnityEngine;
public static class BrakingSettingsChecks {
 static int checks;
 static void Check(bool value,string text){checks++;if(!value)throw new Exception("BRAKING_SETTINGS "+text);}
 public static void Run(){
  checks=0;Check(typeof(DrivingSettings).GetField("braking")!=null,"personal braking preference exists");
  Check(new DrivingSettings().braking==1,"braking defaults to existing feel");
  Check(new DrivingSettings{braking=1.5f}.Copy().braking==1.5f,"copy preserves fifth preference");
  Check(new DrivingSettings{braking=float.NaN}.Normalize().braking==1&&new DrivingSettings{braking=9}.Normalize().braking==1.5f,"nonfinite and oversized preferences bounded");
  foreach(float force in new[]{0f,.015f,.25f,.3f,1f})Check(DrivingSettings.BrakeForce(force,1,false,true)==force,"default demand identical");
  Check(Mathf.Abs(DrivingSettings.BrakeForce(.3f,.5f,false,true)-.15f)<.0001f&&Mathf.Abs(DrivingSettings.BrakeForce(.3f,1.5f,false,true)-.45f)<.0001f,"ordinary brake sensitivity spans half to1.5x");
  Check(DrivingSettings.BrakeForce(1,.5f,true,true)==1&&DrivingSettings.BrakeForce(.25f,.5f,false,false)==.25f,"emergency and idle hold unchanged");
  string key="braking.check."+Guid.NewGuid().ToString("N");try{
   PlayerPrefs.SetString(key,"{\"sensitivity\":0.8,\"deadzone\":0.1}");var read=DrivingSettings.Load(key);Check(read.braking==1&&read.sensitivity==.8f,"four-setting legacy save adds baseline braking");
   new DrivingSettings{braking=.65f}.Save(key);Check(DrivingSettings.Load(key).braking==.65f,"fifth preference persists");
  }finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
  var queue=new PvpNativeInputQueue();queue.Enqueue(new PvpNativeDrive{sequence=1,brakeSensitivity=float.PositiveInfinity});Check(queue.Take().brakeSensitivity==1,"invalid network gain defaults safely");queue.Enqueue(new PvpNativeDrive{sequence=2,brakeSensitivity=10});Check(queue.Take().brakeSensitivity==1.5f,"server bounds brake preference");
  var wire=JsonUtility.FromJson<PvpNativeDrive>(JsonUtility.ToJson(new PvpNativeDrive{brakeSensitivity=.75f}));Check(wire.brakeSensitivity==.75f,"brake gain survives actual wire JSON");
  var root=new GameObject("brake preference role fixture");try{var taxi=root.AddComponent<TaxiController>();taxi.body=root.AddComponent<Rigidbody>();
   PvpNativeInputQueue.Apply(taxi,new PvpNativeDrive{brakeSensitivity=.5f,throttle=-1},false,.02f);Check(taxi.ManualBrakeGain==.5f,"server and local prediction assign same per-driver brake gain");
   var normal=new DriveMath.Demand{brake=.3f};Check(taxi.ApplyBrakePreference(normal,false,true).brake==.15f,"human service brake scales");
   taxi.AutomatedHumanInput=false;Check(taxi.ApplyBrakePreference(normal,false,true).brake==.3f,"bot unaffected");taxi.NetworkPredicted=true;Check(taxi.ApplyBrakePreference(normal,false,true).brake==.15f,"owner prediction uses same gain");
   Check(taxi.ApplyBrakePreference(new DriveMath.Demand{brake=1},true,true).brake==1,"menu/emergency full stop cannot weaken");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  Check(PvpNativeLan.Protocol=="native-city-lan-v11","both peers require brake-aware contract");Debug.Log("BRAKING_SETTINGS_OK checks="+checks);
 }
 public static void ModelsAndPhysics(){Run();DrivingSettingsChecks.All();BrakingPhysicsChecks.Run();}
 public static void All(){ModelsAndPhysics();NativeLeftSignalRegressionTests.Run();}
 public static void Build(){All();PrototypeBuild.BuildHybrid();}
}
