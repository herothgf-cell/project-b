using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Native startup/menu and WheelCollider angle acceptance, not a human road test.
public sealed class DrivingSettingsProbe : MonoBehaviour {
 DriveSession session;string output,key;int checks;bool capture,pvp,done;
 static bool Has(string flag){return Array.IndexOf(Environment.GetCommandLineArgs(),flag)>=0;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Has("-drivingSettingsCheck"))new GameObject("Driving settings acceptance").AddComponent<DrivingSettingsProbe>();}
 void Check(bool value,string text){checks++;if(!value)throw new Exception(text);Debug.Log("DRIVING_SETTINGS_PLAYER PASS "+text);}
 IEnumerator Start(){
  output=ContentTuningRuntime.Argument("-drivingSettingsOutput");Directory.CreateDirectory(output);key="driving.probe."+Guid.NewGuid().ToString("N");capture=Has("-drivingSettingsCapture");pvp=Has("-drivingSettingsPvp");Application.runInBackground=true;
  var run=Run();while(true){object next;try{if(!run.MoveNext())break;next=run.Current;}catch(Exception e){Finish(e);yield break;}yield return next;}Finish(null);
 }
 void Finish(Exception error){if(done)return;done=true;PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();File.WriteAllText(Path.Combine(output,"result.json"),"{\"success\":"+(error==null?"true":"false")+",\"checks\":"+checks+"}");Debug.Log("DRIVING_SETTINGS_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+error);Application.Quit(error==null?0:1);}
 IEnumerator Run(){
  float until=Time.realtimeSinceStartup+180;
  if(pvp){Pvp.PvpBootstrap lobby;while(!(lobby=FindObjectOfType<Pvp.PvpBootstrap>())){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartPvp();if(Time.realtimeSinceStartup>until)throw new TimeoutException("PVP lobby");yield return null;}lobby.StartSimulation(true,600);}
  while(!(session=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!session.StartupReady){if(!pvp&&!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>until)throw new TimeoutException("startup");yield return null;}
  session.business.preparing=false;session.paused=true;Time.timeScale=0;session.GetComponent<DriveHUD>().enabled=true;
  var panel=DrivingSettingsPanel.For(session);panel.PreferenceKey=key;
  new DrivingSettings{sensitivity=.7f,response=.6f,centering=1.8f,deadzone=.08f,braking=.65f}.Save(key);
  panel.Open();panel.Draft.sensitivity=1.4f;Check(DrivingSettings.Load(key).sensitivity==.7f,"editing leaves persisted profile unchanged");
  Check(DrivingSettingsPanel.Back(session)&&session.paused&&Time.timeScale==0,"back cancels without resuming driving");
  panel.Open();Check(panel.Draft.sensitivity==.7f&&panel.Draft.braking==.65f,"reopen restores persisted steering and braking");panel.ResetDefaults();Check(panel.Draft.sensitivity==1&&panel.Draft.deadzone==.15f&&panel.Draft.braking==1,"restore all five defaults");panel.Cancel();
  panel.Open();panel.Draft.sensitivity=1.2f;panel.Draft.braking=.75f;panel.Apply();Check(DrivingSettings.Load(key).sensitivity==1.2f&&DrivingSettings.Load(key).braking==.75f&&!panel.IsOpen,"apply saves then returns to pause");
  if(capture){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"pause.png"));yield return new WaitForSecondsRealtime(.3f);}
  panel.Open();Check(panel.Draft.sensitivity==1.2f,"saved settings survive panel reopen");
  session.law.OpenHistory();Check(!session.law.HistoryOpen&&panel.IsOpen,"history cannot replace settings");
  Check(!session.GetComponent<FullMapOverlay>().Open(),"map cannot replace settings");
  Check(!session.GetComponent<PracticeSession>().OpenPicker(),"practice cannot replace settings");
  session.services.Toggle();Check(!session.services.open&&panel.IsOpen,"service cannot replace settings");
  if(capture){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"settings.png"));yield return new WaitForSecondsRealtime(.3f);Screen.SetResolution(800,600,false);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"settings-small.png"));yield return new WaitForSecondsRealtime(.3f);}
  panel.Cancel();session.paused=false;Time.timeScale=1;var taxi=session.taxi;taxi.body.constraints=RigidbodyConstraints.FreezeAll;taxi.automated=true;taxi.AutomatedHumanInput=true;taxi.autoThrottle=0;taxi.autoBrake=true;taxi.autoSteer=.5f;taxi.ManualSteeringGain=.5f;
  yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();float low=Mathf.Abs(taxi.wheels[0].steerAngle);taxi.ManualSteeringGain=1;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();float high=Mathf.Abs(taxi.wheels[0].steerAngle);
  Check(high>low*1.5f&&low>1,"sensitivity changes actual WheelCollider steering angle");
  taxi.ManualSteeringGain=1.5f;taxi.AutomatedHumanInput=false;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Check(Mathf.Abs(Mathf.Abs(taxi.wheels[0].steerAngle)-high)<.01f,"bot wheel angle ignores human sensitivity");
  taxi.AutomatedHumanInput=true;taxi.autoSteer=0;taxi.body.velocity=Vector3.zero;taxi.RestorePedalIntent(-1);taxi.autoThrottle=-1;taxi.autoBrake=false;taxi.ManualBrakeGain=.5f;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();float weak=taxi.wheels[0].brakeTorque;
  taxi.ManualBrakeGain=1.5f;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Check(weak>0&&taxi.wheels[0].brakeTorque>weak*2.9f,"held service brake scales actual WheelCollider torque: low="+weak+" high="+taxi.wheels[0].brakeTorque);
  taxi.autoBrake=true;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Check(taxi.wheels[0].brakeTorque==3500,"emergency remains full wheel braking torque");
  if(!pvp){
   session.jobs.ClearForDay();taxi.body.velocity=Vector3.zero;taxi.autoThrottle=0;session.damage.Damage.ApplyAccidentLoss("repair-shortcut-render",49);
   Check(!session.services.RepairAttention,"healthy durability51 keeps repair dot hidden");session.damage.Damage.ApplyAccidentLoss("repair-shortcut-warning",28);
   Check(session.services.RepairAttention,"durability23 exposes life hub repair attention");
   if(capture){bool oldReplica=taxi.NetworkReplica,oldPredicted=taxi.NetworkPredicted;taxi.NetworkReplica=true;taxi.NetworkPredicted=false;taxi.ReplicaVelocity=taxi.transform.forward*(100/3.6f);session.signals.ToggleHazards();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"repair-shortcut-speed100.png"));yield return new WaitForSecondsRealtime(.3f);taxi.ReplicaVelocity=Vector3.zero;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"repair-shortcut-stopped.png"));yield return new WaitForSecondsRealtime(.3f);taxi.NetworkReplica=oldReplica;taxi.NetworkPredicted=oldPredicted;}
   session.services.OpenFacilityPlanner(5);Check(session.FullMapOpen,"workshop retains comparison/navigation planner");session.GetComponent<FullMapOverlay>().Close();
  }
  if(pvp){panel.Open();session.simulationMatch.Finish();yield return null;yield return null;Check(!panel.IsOpen&&session.simulationMatch.Finished,"match completion discards settings and reveals results");}
 }
}
