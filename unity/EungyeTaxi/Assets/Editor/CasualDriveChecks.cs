using System;
using System.Reflection;
using UnityEngine;
public static class CasualDriveChecks {
 static int checks;static void Check(bool v,string name){checks++;if(!v)throw new Exception("CASUAL_DRIVE "+name);}
 public static void Controls(){
  Check(DriveMath.Resolve(-1,8,false).brake>=.27f&&DriveMath.Resolve(-1,8,false).brake<=.36f,"ordinary S brake strengthened within approved1.5-2x range");
  Check(DriveMath.Resolve(-1,8,true).brake==1,"emergency remains full strength");
  Check(DriveMath.ManualSteerAngle(20)>=39&&DriveMath.ManualSteerAngle(20)<=41,"followup20kph steering is more forgiving");
  Check(DriveMath.ManualSteerAngle(30)>=24&&DriveMath.ManualSteerAngle(30)<=26,"followup30kph steering is more forgiving");
  Check(DriveMath.ManualSteerAngle(35)>=16&&DriveMath.ManualSteerAngle(35)<=18,"followup35kph avoids sharp loss of turning authority");
  Check(Math.Abs(DriveMath.ManualSteerAngle(60)-DriveMath.SteerAngle(60))<.001,"high speed/H geometry preserved");
  var method=typeof(DriveFlowPolicy).GetMethod("AdvanceSteering");Check(method!=null,"single and LAN use directional steering policy");
  Func<float,float,float> step=(current,target)=>(float)method.Invoke(null,new object[]{current,target,5f,.1f});
  Check(step(-1,1)>.75f,"followup countersteer changes direction promptly");Check(step(1,0)>0&&step(1,0)<.5f,"release centres smoothly without instant snap");
  Check(Math.Abs(step(-1,1)+step(1,-1))<.001,"countersteer remains left-right symmetric");
  Check(DriveMath.ManualSteerAngle(0)<=52&&Math.Abs(DriveMath.ManualSteerAngle(-30)-DriveMath.ManualSteerAngle(30))<.001,"parking maximum and reverse symmetry preserved");
  Check(Math.Abs(DriveMath.ManualSteerAngle(49.99f)-DriveMath.ManualSteerAngle(50))<.05,"city allowance blends smoothly into unchanged high-speed limit");
  var yaw=typeof(DriveMath).GetMethod("ManualYawAcceleration");Check(yaw!=null,"manual city turn has bounded yaw response assistance");
  Func<float,float,float,float> yawAt=(speed,angle,rate)=>(float)yaw.Invoke(null,new object[]{speed,angle,rate});
  Check(yawAt(5,25,0)>0&&Math.Abs(yawAt(5,25,0)+yawAt(5,-25,0))<.001,"yaw assistance is symmetric");
  Check(yawAt(0,25,0)==0&&yawAt(15,25,0)==0,"no stationary spin or high-speed yaw assistance");
  Check(yawAt(5,25,3)==0&&yawAt(-5,25,0)<0,"no excess same-direction spin and reverse remains correct");
  var gateMethod=typeof(DriveMath).GetMethod("MayAssistManualYaw");Check(gateMethod!=null,"yaw guard includes held-wheel states");
  Func<int,float,bool,bool,bool,bool> allowed=(grounded,up,assisted,human,frozen)=>(bool)gateMethod.Invoke(null,new object[]{grounded,up,assisted,human,frozen});
  Check(allowed(4,1,false,true,false)&&!allowed(4,1,false,true,true),"microsleep cannot steer body while wheel steering is frozen");
  Check(!allowed(4,1,true,true,false)&&!allowed(4,1,false,false,false),"H and bots keep their original steering dynamics");
  Check(!allowed(1,1,false,true,false)&&!allowed(2,.5f,false,true,false),"airborne or excessive tilt cannot receive yaw aid");
  var t=typeof(DriveMath).Assembly.GetType("DriverPedalIntent");Check(t!=null,"stateful service stop exists");var gate=Activator.CreateInstance(t);var resolve=t.GetMethod("Resolve");
  Func<float,float,DriveMath.Demand> pedal=(raw,speed)=>(DriveMath.Demand)resolve.Invoke(gate,new object[]{raw,speed,false});
  Check(pedal(-1,8).brake>0,"S brakes while moving forward");Check(pedal(-1,0).motor==0,"held S cannot automatically reverse");pedal(0,0);Check(pedal(-1,0).motor<0,"release/repress S allows intentional reverse");
  Check(pedal(1,-8).brake>0&&pedal(1,0).motor==0,"reverse braking is symmetric");pedal(0,0);Check(pedal(1,0).motor>0,"release/repress W allows forward");
  Check(!DriveMath.HarshPassengerMotion(-5.5f,8,false)&&DriveMath.HarshPassengerMotion(-7,8,false)&&!DriveMath.HarshPassengerMotion(-7,8,true),"comfortable stop/harsh/necessary braking remain distinct");
 }
 public static void Run(){checks=0;Controls();Debug.Log("CASUAL_DRIVE_OK checks="+checks);}
 public static void ResetRegression(){
  var root=new GameObject("pedal reset regression");try{var taxi=root.AddComponent<TaxiController>();taxi.body=root.AddComponent<Rigidbody>();
   var field=typeof(TaxiController).GetField("pedalIntent",BindingFlags.NonPublic|BindingFlags.Instance);var gate=(DriverPedalIntent)field.GetValue(taxi);gate.Resolve(-1,8,false);
   var prediction=new PvpNativePrediction(taxi);prediction.Receive(0,new PvpPredictedMotion{rotation=Quaternion.identity},true,false);
   Check(gate.Resolve(-1,0,false).motor<0,"authoritative reset clears stale local held-S stop state");
   var locked=new PvpPredictedMotion{rotation=Quaternion.identity,pedalDirection=-1};prediction.Receive(0,locked,true,false);Check(gate.Resolve(-1,0,false).motor==0,"authoritative held-S state is preserved during reseed");
   Check(JsonUtility.FromJson<PvpNativeActor>(JsonUtility.ToJson(new PvpNativeActor{pedalDirection=-1})).pedalDirection==-1,"pedal gate survives actual actor wire JSON");
  }finally{UnityEngine.Object.DestroyImmediate(root);}Debug.Log("CASUAL_RESET_OK");
 }
 public static void YellowRegression(){
  var root=new GameObject("yellow wait regression");try{var t=root.AddComponent<TrafficSystem>();t.manualClock=true;t.manualTime=10.5f;
   var j=new TrafficJunction{editorProgram=new RoadMapSignalProgram{groups=new[]{new RoadMapSignalGroup{id="a",axis=0,sign=1},new RoadMapSignalGroup{id="b",axis=1,sign=1}},stages=new[]{new RoadMapStage{groups=new[]{"a"},green=10,yellow=2,clearance=2},new RoadMapStage{groups=new[]{"b"},green=10,yellow=2,clearance=2}}}};
   Check(t.EstimatedGreenWait(j,0,1,0)>15,"authored yellow estimates next green rather than next red");
  }finally{UnityEngine.Object.DestroyImmediate(root);}Debug.Log("CASUAL_YELLOW_OK");
 }
 public static void NearbyResetRegression(){
  var root=new GameObject("nearby reset wire regression");try{var taxi=root.AddComponent<TaxiController>();taxi.body=root.AddComponent<Rigidbody>();taxi.RestorePedalIntent(-1);var prediction=new PvpNativePrediction(taxi);
   var old=new PvpPredictedMotion{rotation=Quaternion.identity,pedalDirection=-1};prediction.Receive(0,old,true,false);
   var epoch=typeof(PvpPredictedMotion).GetField("resetEpoch");Check(epoch!=null,"wire motion has explicit reset epoch for sub8m resets");
   object boxed=new PvpPredictedMotion{position=Vector3.right*.5f,rotation=Quaternion.identity,pedalDirection=0};epoch.SetValue(boxed,1);prediction.Receive(0,(PvpPredictedMotion)boxed,false,false);
   Check(taxi.PedalBrakingDirection==0&&prediction.HardResets==2,"nearby host reset reseeds even without caller reset flag or8m error");
  }finally{UnityEngine.Object.DestroyImmediate(root);}Debug.Log("CASUAL_NEARBY_RESET_OK");
 }
}
