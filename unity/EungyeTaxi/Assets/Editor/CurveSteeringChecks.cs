using System;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class CurveSteeringChecks {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Run(){
  foreach(float kph in new[]{40f,45f,50f})PhysicsCurve(kph);
  Check(DriveMath.ManualSteerAngle(50)>DriveMath.SteerAngle(50)*1.15f,"50 km/h gentle curve has additional steering authority");
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
 static void PhysicsCurve(float kph){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;
  var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(0,-.5f,0);ground.transform.localScale=new Vector3(1000,1,1000);
  var root=new GameObject("Curve taxi");var taxi=root.AddComponent<TaxiController>();taxi.Create(Vector3.up*.65f,Quaternion.identity);taxi.automated=true;taxi.AutomatedHumanInput=true;
  try{
   Physics.SyncTransforms();for(int i=0;i<200;i++){taxi.SendMessage("FixedUpdate");Physics.Simulate(.02f);}
   taxi.autoSteer=.65f;float tilt=0,vy=0;
   // Constant forward test speed isolates steering; lateral velocity remains simulated.
   for(int i=0;i<200;i++){
    taxi.body.velocity+=taxi.transform.forward*(kph/3.6f-taxi.Speed);
    taxi.SendMessage("FixedUpdate");Physics.Simulate(.02f);
    tilt=Mathf.Max(tilt,Vector3.Angle(taxi.transform.up,Vector3.up));vy=Mathf.Max(vy,Mathf.Abs(taxi.body.velocity.y));
   }
   float heading=Mathf.Abs(Mathf.DeltaAngle(0,taxi.transform.eulerAngles.y));
   Debug.Log("CURVE_PHYSICS kph="+kph+" heading="+heading+" tilt="+tilt+" maxVy="+vy);
   Check(heading>15&&heading<150&&tilt<8&&vy<.5f,"stable gentle curve at "+kph);
  }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(ground);Physics.autoSimulation=automatic;}
 }
}
