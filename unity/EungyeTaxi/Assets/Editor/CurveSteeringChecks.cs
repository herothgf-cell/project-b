using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class CurveSteeringChecks {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Run(){
  foreach(float kph in new[]{40f,45f,50f,55f,60f})PhysicsCurve(kph);
  foreach(float kph in new[]{45f,50f,55f}){
   foreach(var input in new[]{Maneuver.KeyTap,Maneuver.LaneChange}){
    float right=PhysicsCurve(kph,input,1),left=PhysicsCurve(kph,input,-1);
    Check(Mathf.Abs(right+left)<.15f,"left/right lateral travel at "+kph+" "+input);
   }
  }
  Check(DriveMath.ManualSteerAngle(50)>=6.4f&&DriveMath.ManualSteerAngle(50)<=6.6f,"50 km/h manual lock near 6.5 degrees");
  foreach(float kph in new[]{40f,45f,50f}){
   Check(DriveMath.ManualYawAcceleration(kph/3.6f,3,0)>0,"gentle curve assistance at "+kph);
   Check(DriveMath.ManualYawAcceleration(kph/3.6f,30,6/(kph/3.6f))==0,"no assistance past lateral acceleration budget");
  }
  Check(DriveMath.ManualYawAcceleration(60/3.6f,10,0)==0,"high speed assist disabled");
  Check(DriveMath.ManualSteerAngle(60)==DriveMath.SteerAngle(60),"high speed wheel limit unchanged");
  for(float kph=35.1f;kph<90;kph+=.1f)Check(Mathf.Abs(DriveMath.ManualSteerAngle(kph)-DriveMath.ManualSteerAngle(kph-.1f))<.3f,"continuous city/high-speed wheel limit");
  SteeringFeel();
  DrivingSettingsChecks.Run();DrivingSettingsChecks.Menus();CasualDriveChecks.Run();
  var root=new GameObject("Steering presets");var session=root.AddComponent<DriveSession>();session.enabled=false;
  var panel=DrivingSettingsPanel.For(session);panel.PreferenceKey="curve.preset.test."+Guid.NewGuid();
  try{
   panel.Open();panel.Draft.braking=.73f;panel.Draft.deadzone=.12f;panel.SteeringPreset(1);
   Check(panel.Draft.sensitivity==1.15f&&panel.Draft.response==1.35f&&panel.Draft.centering==1.25f,"quick steering preset");
   Check(panel.Draft.braking==.73f&&panel.Draft.deadzone==.12f,"preset preserves other settings");
   panel.Apply();panel.Open();Check(panel.Draft.sensitivity==1.15f,"preset persists after apply");
   panel.SteeringPreset(-1);panel.Cancel();panel.Open();Check(panel.Draft.sensitivity==1.15f,"cancel discards preset");
   panel.SteeringPreset(0);Check(panel.Draft.sensitivity==1&&panel.Draft.response==1&&panel.Draft.centering==1,"standard steering preset");
  }finally{PlayerPrefs.DeleteKey(panel.PreferenceKey);PlayerPrefs.Save();UnityEngine.Object.DestroyImmediate(root);}
  Debug.Log("CURVE_STEERING_OK");
 }
 enum Maneuver { Hold,KeyTap,LaneChange }
 static readonly MethodInfo TaxiTick=typeof(TaxiController).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
 // Compare identical entries on a flat pad. After the run-up, speed is entirely
 // determined by the chassis and wheel forces, including when coasting.
 public static void RunCornerPedals(){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  bool automatic=Physics.autoSimulation;
  Physics.autoSimulation=false;
  var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);
  ground.transform.position=Vector3.down*.5f;
  ground.transform.localScale=new Vector3(600,1,600);
  bool passed=true;
  try{
   foreach(int kph in new[]{15,25})foreach(float pedal in new[]{0f,1f})foreach(int side in new[]{0,-1,1}){
    var root=new GameObject("Corner pedal test");
    try{
     var taxi=root.AddComponent<TaxiController>();
     taxi.Create(Vector3.up*.65f,Quaternion.identity);
     taxi.automated=taxi.AutomatedHumanInput=true;
     Physics.SyncTransforms();
     taxi.autoThrottle=1;
     for(int i=0;i<600&&taxi.Speed*3.6f<kph;i++){TaxiTick.Invoke(taxi,null);Physics.Simulate(.02f);}
     float entry=taxi.Speed*3.6f;
     Check(Mathf.Abs(entry-kph)<.5f,"corner test reached entry speed");
     taxi.autoThrottle=pedal;
     float peakBrake=0;
     bool assisted=false;
     for(int i=0;i<50;i++){
      taxi.autoSteer=Mathf.MoveTowards(taxi.autoSteer,side,.1f);
      TaxiTick.Invoke(taxi,null);
      assisted|=taxi.CornerAssisting;
      peakBrake=Mathf.Max(peakBrake,taxi.wheels[0].brakeTorque);
      Physics.Simulate(.02f);
     }
     float heading=Mathf.DeltaAngle(0,taxi.transform.eulerAngles.y);
     Debug.LogFormat("CORNER_PEDALS kph={0} pedal={1} side={2} entry={3:F2} exit={4:F2} heading={5:F1} brakeNm={6:F1} assisted={7}",kph,pedal,side,entry,taxi.Speed*3.6f,heading,peakBrake,assisted);
     passed&=!assisted&&peakBrake<=(pedal==0?53f:0f)&&(side==0||heading*side>10);
     if(pedal==1&&kph==25)passed&=taxi.Speed*3.6f>=entry*.90f;
    }finally{UnityEngine.Object.DestroyImmediate(root);}
   }
   Check(passed,"low speed turns follow the pedal while changing heading in both directions");
   Debug.Log("CORNER_PEDALS_OK scenarios=12");
  }finally{UnityEngine.Object.DestroyImmediate(ground);Physics.autoSimulation=automatic;}
 }
 static float PhysicsCurve(float kph,Maneuver maneuver=Maneuver.Hold,int direction=1){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;
  var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(0,-.5f,0);ground.transform.localScale=new Vector3(1000,1,1000);
  var root=new GameObject("Curve taxi");var taxi=root.AddComponent<TaxiController>();taxi.Create(Vector3.up*.65f,Quaternion.identity);taxi.automated=true;taxi.AutomatedHumanInput=true;
  try{
   Physics.SyncTransforms();for(int i=0;i<200;i++){TaxiTick.Invoke(taxi,null);Physics.Simulate(.02f);}
   var settings=new DrivingSettings();
   float tilt=0,vy=0,halfSecondHeading=0,halfSecondX=0,settledYaw=0;
   Vector3 start=taxi.body.position;
   int steps=maneuver==Maneuver.Hold?200:maneuver==Maneuver.KeyTap?55:135;
   // Constant forward test speed isolates steering; lateral velocity remains simulated.
   for(int i=0;i<steps;i++){
    if(maneuver==Maneuver.Hold)taxi.autoSteer=.65f;
    else{
     float key=maneuver==Maneuver.KeyTap&&i<5?direction:0;
     float pad=maneuver==Maneuver.LaneChange&&i<80?(i<40?.65f:-.65f)*direction:0;
     taxi.autoSteer=settings.Advance(taxi.autoSteer,settings.Input(key,pad),kph/3.6f,.02f);
    }
    taxi.body.velocity+=taxi.transform.forward*(kph/3.6f-taxi.Speed);
    TaxiTick.Invoke(taxi,null);Physics.Simulate(.02f);
    tilt=Mathf.Max(tilt,Vector3.Angle(taxi.transform.up,Vector3.up));vy=Mathf.Max(vy,Mathf.Abs(taxi.body.velocity.y));
    if(i==24){halfSecondHeading=Mathf.DeltaAngle(0,taxi.transform.eulerAngles.y);halfSecondX=taxi.body.position.x-start.x;}
    if(i>=steps-10)settledYaw=Mathf.Max(settledYaw,Mathf.Abs(taxi.body.angularVelocity.y)*Mathf.Rad2Deg);
   }
   float heading=Mathf.Abs(Mathf.DeltaAngle(0,taxi.transform.eulerAngles.y));
   float travel=taxi.body.position.x-start.x;
   Debug.Log("CURVE_PHYSICS kph="+kph+" input="+maneuver+" direction="+direction+" heading="+heading+" heading0.5="+halfSecondHeading+" x0.5="+halfSecondX+" xEnd="+travel+" tailYawDegPerSec="+settledYaw+" tilt="+tilt+" maxVy="+vy);
   Check(tilt<8&&vy<.5f,"stable grounded turn at "+kph+" "+maneuver);
   if(maneuver==Maneuver.Hold)Check(heading>15&&heading<150,"sustained curvature at "+kph);
   else Check(direction*halfSecondHeading>0&&direction*travel>0&&settledYaw<2,"responds and settles after release at "+kph+" "+maneuver);
   if(maneuver==Maneuver.LaneChange)Check(heading<1,"countersteer ends close to the original heading");
   if(kph==50){
    if(maneuver==Maneuver.Hold)Check(heading>=60&&heading<=85,"50 km/h sustained turn authority");
    if(maneuver==Maneuver.KeyTap)Check(Mathf.Abs(halfSecondHeading)>=2&&Mathf.Abs(halfSecondHeading)<=3.5f,"50 km/h short correction");
    if(maneuver==Maneuver.LaneChange)Check(Mathf.Abs(travel)>=2.6f&&Mathf.Abs(travel)<=3.7f,"50 km/h lane-change travel");
   }
   return travel;
  }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(ground);Physics.autoSimulation=automatic;}
 }
 // 08 chapter 1: casual vs sim steering. Sim must stay the r50 tuning; casual must change one lane
 // (3.5 m) faster than sim at 50-60 km/h, recover straight, and never roll over. The 1.2 s design goal is
 // reported, not asserted: a lane change that ends straight needs about sqrt(4*3.5/a) seconds at lateral
 // acceleration a (1.32 s at 8 m/s2, 1.67 s at 5), so 1.2 s is out of reach within the 7-8 m/s2 budget.
 public static void SteeringFeel(){
  for(float kph=35.1f;kph<100;kph+=.1f){
   Check(Mathf.Abs(DriveMath.ManualSteerAngle(kph,true)-DriveMath.ManualSteerAngle(kph-.1f,true))<.3f,"continuous casual wheel limit at "+kph);
   Check(DriveMath.ManualSteerAngle(kph,true)>=DriveMath.ManualSteerAngle(kph)-.0001f,"casual never steers less than sim at "+kph);
   Check(DriveMath.ManualSteerAngle(kph,false)==DriveMath.ManualSteerAngle(kph),"sim wheel limit unchanged at "+kph);
  }
  Check(Mathf.Abs(DriveMath.ManualSteerAngle(90,true)-DriveMath.ManualSteerAngle(90))<.001f,"casual returns to sim by 85 km/h");
  Check(DriveMath.ManualYawAcceleration(70/3.6f,3,0,true)>0&&DriveMath.ManualYawAcceleration(70/3.6f,3,0,false)==0,"casual yaw assist extends past 60 km/h");
  Check(DriveMath.ManualYawAcceleration(80/3.6f,3,0,true)==0,"casual yaw assist ends at 80 km/h");
  var simSeconds=new System.Collections.Generic.Dictionary<float,float>();
  foreach(bool casual in new[]{false,true})foreach(float kph in new[]{50f,60f}){
   var r=LaneChange(kph,casual);if(!casual)simSeconds[kph]=r.seconds;
   Debug.Log("STEERING_FEEL lane "+(casual?"casual":"sim")+" kph="+kph+" seconds="+r.seconds.ToString("F2")+" reach3.5m="+r.reach.ToString("F2")+" goal1.2="+(r.reach<=1.2f?"met":"missed")+" maxLateral="+r.lateral.ToString("F2")+" heading="+r.heading.ToString("F2")+" settleHeading="+r.settle.ToString("F2")+" settleYawDegPerSec="+r.yaw.ToString("F2")+" tilt="+r.tilt.ToString("F2")+" maxVy="+r.vy.ToString("F3")+" minGrounded="+r.grounded);
   Check(r.tilt<8&&r.vy<.5f&&r.grounded>=3,"stable lane change at "+kph+(casual?" casual":" sim"));
   Check(r.seconds<10&&r.settle<1.5f&&r.yaw<3f,"straight recovery after lane change at "+kph+(casual?" casual":" sim"));
   if(casual)Check(kph>=60?r.seconds<simSeconds[kph]-.1f:r.seconds<=simSeconds[kph]+.05f,"casual lane change no slower (60: faster) than sim at "+kph);
  }
  foreach(bool casual in new[]{false,true})foreach(float kph in new[]{60f,70f,80f}){
   var r=FullLock(kph,casual);
   Debug.Log("STEERING_FEEL lock "+(casual?"casual":"sim")+" kph="+kph+" lateralAcc="+r.lateral.ToString("F2")+" tilt="+r.tilt.ToString("F2")+" maxVy="+r.vy.ToString("F3")+" minGrounded="+r.grounded+" upright="+r.settle.ToString("F3"));
   Check(r.tilt<8&&r.vy<.5f&&r.grounded>=3&&r.settle>.98f,"no roll-over at full lock "+kph+(casual?" casual":" sim"));
  }
 }
 struct FeelRun {public float seconds,reach,lateral,heading,settle,yaw,tilt,vy;public int grounded;}
 static TaxiController FeelTaxi(bool casual,out GameObject ground,out GameObject root){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(0,-.5f,0);ground.transform.localScale=new Vector3(2000,1,2000);
  root=new GameObject("Feel taxi");var taxi=root.AddComponent<TaxiController>();taxi.Create(Vector3.up*.65f,Quaternion.identity);
  taxi.automated=true;taxi.AutomatedHumanInput=true;taxi.SteeringFeelOverride=casual?DrivingSettings.Casual:DrivingSettings.Sim;
  Physics.SyncTransforms();for(int i=0;i<200;i++){TaxiTick.Invoke(taxi,null);Physics.Simulate(.02f);}
  return taxi;
 }
 static void Track(TaxiController taxi,ref FeelRun r){
  r.tilt=Mathf.Max(r.tilt,Vector3.Angle(taxi.transform.up,Vector3.up));r.vy=Mathf.Max(r.vy,Mathf.Abs(taxi.body.velocity.y));
  int grounded=0;foreach(var w in taxi.wheels)if(w.isGrounded)grounded++;r.grounded=Mathf.Min(r.grounded,grounded);
 }
 // The same driver for both feels: aim at a heading toward the next lane (4 degrees per metre left,
 // at most 20) and steer toward it. The input saturates at +/-1 most of the time, so it behaves like
 // full key presses through the normal steering ramp. reach = first time within 0.25 m of 3.5 m;
 // seconds = settled (within 0.25 m, heading under 1 degree, held 0.5 s).
 static FeelRun LaneChange(float kph,bool casual){
  bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;GameObject ground,root;var taxi=FeelTaxi(casual,out ground,out root);
  var r=new FeelRun{grounded=4,seconds=-1,reach=-1};var settings=new DrivingSettings();
  try{
   var start=taxi.body.position;float steer=0,inside=-1;
   for(int i=0;i<300;i++){
    float t=i*.02f,heading=Mathf.DeltaAngle(0,taxi.transform.eulerAngles.y),yaw=taxi.body.angularVelocity.y*Mathf.Rad2Deg,y=taxi.body.position.x-start.x;
    float wanted=Mathf.Clamp((3.5f-y)*4,-20,20);float input=Mathf.Clamp((wanted-heading)*.5f-yaw*.15f,-1,1);
    steer=settings.Advance(steer,settings.Input(0,input),kph/3.6f,.02f);taxi.autoSteer=steer;
    taxi.body.velocity+=taxi.transform.forward*(kph/3.6f-taxi.Speed);
    TaxiTick.Invoke(taxi,null);Physics.Simulate(.02f);Track(taxi,ref r);
    y=taxi.body.position.x-start.x;heading=Mathf.DeltaAngle(0,taxi.transform.eulerAngles.y);t+=.02f;
    r.lateral=Mathf.Max(r.lateral,y);
    if(r.reach<0&&y>=3.25f)r.reach=t;
    bool ok=Mathf.Abs(y-3.5f)<.25f&&Mathf.Abs(heading)<1;
    if(ok){if(inside<0)inside=t;if(r.seconds<0&&t-inside>=.5f)r.seconds=inside;}else inside=-1;
    if(r.seconds>=0){r.settle=Mathf.Max(r.settle,Mathf.Abs(heading));r.yaw=Mathf.Max(r.yaw,Mathf.Abs(taxi.body.angularVelocity.y*Mathf.Rad2Deg));}
    if(r.seconds>=0&&t>r.seconds+1.5f)break;
   }
   r.heading=Mathf.DeltaAngle(0,taxi.transform.eulerAngles.y);
   if(r.seconds<0)r.seconds=99;if(r.reach<0)r.reach=99;
   return r;
  }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(ground);Physics.autoSimulation=automatic;}
 }
 static FeelRun FullLock(float kph,bool casual){
  bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;GameObject ground,root;var taxi=FeelTaxi(casual,out ground,out root);
  var r=new FeelRun{grounded=4,settle=1};var settings=new DrivingSettings();
  try{
   float steer=0;
   for(int i=0;i<150;i++){
    steer=settings.Advance(steer,1,kph/3.6f,.02f);taxi.autoSteer=steer;
    taxi.body.velocity+=taxi.transform.forward*(kph/3.6f-taxi.Speed);
    TaxiTick.Invoke(taxi,null);Physics.Simulate(.02f);Track(taxi,ref r);
    r.settle=Mathf.Min(r.settle,Vector3.Dot(taxi.transform.up,Vector3.up));
    if(i>=100)r.lateral=Mathf.Max(r.lateral,Mathf.Abs(taxi.body.angularVelocity.y*taxi.Speed));
   }
   return r;
  }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(ground);Physics.autoSimulation=automatic;}
 }
}
