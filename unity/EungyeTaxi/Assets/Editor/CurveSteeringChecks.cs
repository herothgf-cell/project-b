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
}
