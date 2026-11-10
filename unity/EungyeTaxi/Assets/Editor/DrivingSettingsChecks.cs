using System;
using UnityEngine;
public static class DrivingSettingsChecks {
 static int checks;
 static void Check(bool value,string name){checks++;if(!value)throw new Exception("DRIVING_SETTINGS "+name);}
 public static void Run(){
  checks=0;
  var type=typeof(TaxiController).Assembly.GetType("DrivingSettings");
  Check(type!=null,"persistent personal steering settings exist");
  var defaults=new DrivingSettings();
  foreach(float speed in new[]{-15f,-5f,0f,5f,15f})foreach(float start in new[]{-1f,-.2f,0f,.2f,1f})foreach(float target in new[]{-1f,0f,1f}){
   Check(defaults.Advance(start,target,speed,.02f)==DriveFlowPolicy.AdvanceSteering(start,target,speed,.02f),"default interpolation identical");
   Check(Mathf.Abs(DrivingSettings.Angle(target,speed*3.6f,1)-target*DriveMath.ManualSteerAngle(speed*3.6f))<.0001f,"default angle identical");
  }
  var tuned=new DrivingSettings{response=.25f,centering=2,sensitivity=1.5f,deadzone=.05f};
  Check(tuned.Advance(0,1,5,.02f)<defaults.Advance(0,1,5,.02f),"response lowers initial turn rate");
  Check(tuned.Advance(1,0,5,.02f)<defaults.Advance(1,0,5,.02f),"return independently speeds centering");
  Check(tuned.Advance(-1,1,5,.02f)==-tuned.Advance(1,-1,5,.02f),"countersteer symmetric");
  Check(DrivingSettings.Angle(.5f,20,1.5f)>DrivingSettings.Angle(.5f,20,1),"gain increases keyboard angle");
  Check(DrivingSettings.Angle(1,0,1.5f)==52&&DrivingSettings.Angle(-1,0,1.5f)==-52,"parking angle bound");
  Check(defaults.Input(1,.1f)==1&&tuned.Input(1,.1f)==.1f,"deadzone changes pad not keyboard");
  Check(new DrivingSettings{deadzone=0}.Input(0,.01f)==.01f,"zero deadzone accepts tiny stick movement");
  var bounded=new DrivingSettings{sensitivity=float.NaN,response=float.PositiveInfinity,centering=-100,deadzone=10}.Normalize();
  Check(bounded.sensitivity==1&&bounded.response==1&&bounded.centering==.25f&&bounded.deadzone==.4f,"invalid preferences bounded");
  string key="driving.check."+Guid.NewGuid().ToString("N");try{
   tuned.Save(key);var read=DrivingSettings.Load(key);Check(read.sensitivity==1.5f&&read.response==.25f&&read.centering==2&&read.deadzone==.05f,"saved controls roundtrip");
   PlayerPrefs.SetString(key,"{\"sensitivity\":0.8}");read=DrivingSettings.Load(key);Check(read.sensitivity==.8f&&read.response==1&&read.centering==1&&read.deadzone==.15f,"missing members keep defaults");
   PlayerPrefs.SetString(key,"broken");Check(DrivingSettings.Load(key).response==1,"malformed stored JSON recovers");
  }finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
  var queue=new PvpNativeInputQueue();Check(queue.Enqueue(new PvpNativeDrive{sequence=1,steeringGain=99,steeringResponse=float.NaN,steeringReturn=-5}),"preference packet sanitized");var packet=queue.Take();
  Check(packet.steeringGain==1.5f&&packet.steeringResponse==1&&packet.steeringReturn==.25f,"host owns preference limits");
  var wire=JsonUtility.FromJson<PvpNativeDrive>(JsonUtility.ToJson(new PvpNativeDrive{steeringGain=.7f,steeringResponse=1.7f,steeringReturn=.6f}));
  Check(wire.steeringGain==.7f&&wire.steeringResponse==1.7f&&wire.steeringReturn==.6f,"driver preferences survive wire");
  var root=new GameObject("settings authority fixture");try{
   var a=root.AddComponent<TaxiController>();a.body=root.AddComponent<Rigidbody>();
   PvpNativeInputQueue.Apply(a,new PvpNativeDrive{steer=1,steeringGain=.7f,steeringResponse=.25f},false,.02f);
   Check(a.autoSteer==new DrivingSettings{response=.25f}.Advance(0,1,0,.02f)&&a.ManualSteeringGain==.7f,"authority and predicted path use same driver settings");
   Check(a.WheelCenterAngle(.5f,20,false)==DrivingSettings.Angle(.5f,20,.7f),"network human gain applied once");
   a.AutomatedHumanInput=false;Check(a.WheelCenterAngle(.5f,20,false)==.5f*DriveMath.ManualSteerAngle(20),"bot ignores personal gain");
   Check(a.WheelCenterAngle(.5f,20,true)==.5f*DriveMath.SteerAngle(20),"H ignores personal gain");
   PvpNativeInputQueue.Apply(a,null,true,.02f);Check(a.autoSteer==0&&a.autoBrake,"missing controls safely brake");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  Check(PvpNativeLan.Protocol=="native-city-lan-v17","incompatible authority contracts rejected");
  Debug.Log("DRIVING_SETTINGS_OK checks="+checks);
 }
 public static void Menus(){
  var type=typeof(TaxiController).Assembly.GetType("DrivingSettingsPanel");Check(type!=null,"pause driving settings panel exists");
  var root=new GameObject("settings menu fixture");string key="driving.menu.check."+Guid.NewGuid().ToString("N");try{
   var session=root.AddComponent<DriveSession>();session.enabled=false;session.paused=true;
   var panel=DrivingSettingsPanel.For(session);panel.PreferenceKey=key;new DrivingSettings{sensitivity=.8f}.Save(key);
   panel.Open();panel.Draft.sensitivity=1.4f;Check(DrivingSettings.Load(key).sensitivity==.8f,"draft not applied before confirmation");panel.Cancel();
   Check(!panel.IsOpen&&DrivingSettings.Load(key).sensitivity==.8f,"cancel discards draft");
   panel.Open();panel.Draft.centering=1.8f;panel.Apply();Check(!panel.IsOpen&&DrivingSettings.Load(key).centering==1.8f,"apply persists and closes");
   panel.Open();panel.ResetDefaults();Check(panel.Draft.centering==1&&DrivingSettings.Load(key).centering==1.8f,"defaults only edits draft");
   Check(DrivingSettingsPanel.Back(session)&&session.paused&&!panel.IsOpen&&DrivingSettings.Load(key).centering==1.8f,"back stays paused and cancels edits");
   Check(!DrivingSettingsPanel.Back(session),"closed panel leaves escape to pause owner");
   foreach(var size in new[]{new Vector2(800,600),new Vector2(1280,720),new Vector2(1920,1080),new Vector2(640,360)}){var rect=DrivingSettingsPanel.Bounds(size.x,size.y);Check(rect.x>=0&&rect.y>=0&&rect.xMax<=size.x&&rect.yMax<=size.y,"all settings fit small viewport");}
  }finally{UnityEngine.Object.DestroyImmediate(root);PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
  var assets=UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");var input=new UnityEditor.SerializedObject(assets[0]);var axes=input.FindProperty("m_Axes");bool raw=false;
  for(int i=0;i<axes.arraySize;i++){var axis=axes.GetArrayElementAtIndex(i);if(axis.FindPropertyRelative("m_Name").stringValue=="TaxiSteer")raw=axis.FindPropertyRelative("dead").floatValue==0;}
  Check(raw,"Unity input does not hide values below configurable deadzone");
  Debug.Log("DRIVING_SETTINGS_MENUS_OK checks="+checks);
 }
 public static void Modal(){
  var root=new GameObject("settings modal fixture");try{
   var s=root.AddComponent<DriveSession>();s.enabled=false;s.paused=true;
   s.business=root.AddComponent<BusinessProgress>();s.business.session=s;
   s.services=root.AddComponent<ServiceStops>();s.services.session=s;
   s.law=root.AddComponent<DrivingLaw>();s.law.session=s;
   var map=root.AddComponent<FullMapOverlay>();map.session=s;
   var practice=root.AddComponent<PracticeSession>();practice.session=s;
   var panel=DrivingSettingsPanel.For(s);panel.Open();s.law.OpenHistory();
   Check(!s.law.HistoryOpen&&panel.IsOpen&&s.paused,"F8 history cannot replace settings or resume driving");
   Check(!map.Open()&&panel.IsOpen,"M map cannot replace settings");
   Check(!practice.OpenPicker()&&panel.IsOpen,"F6 practice cannot replace settings");
   s.services.Toggle();Check(!s.services.open&&panel.IsOpen,"F7 service cannot replace settings");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  Debug.Log("DRIVING_SETTINGS_MODAL_OK");
 }
 public static void All(){Run();Menus();Modal();CasualIntegrationBuild.Run();ContentTuningChecks.Run();}
 public static void Build(){All();PrototypeBuild.BuildHybrid();}
}
