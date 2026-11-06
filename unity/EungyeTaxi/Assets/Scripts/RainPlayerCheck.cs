using System;
using System.Collections;
using System.IO;
using UnityEngine;

public sealed class RainPlayerCheck:MonoBehaviour {
 int checks;float started,stopDistance;DriveSession s;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-rainTest")>=0)new GameObject("Rain acceptance").AddComponent<RainPlayerCheck>();}
 void Check(bool ok,string why){Debug.Log("RAIN "+(ok?"PASS ":"FAIL ")+why);checks++;if(!ok)throw new Exception(why);}
 void Update(){if(Time.realtimeSinceStartup-started>240){Debug.LogError("RAIN timeout");Application.Quit(2);}}
 IEnumerator Start(){DontDestroyOnLoad(gameObject);started=Time.realtimeSinceStartup;var run=Run();while(true){object current;try{if(!run.MoveNext())yield break;current=run.Current;}catch(Exception e){Debug.LogError("RAIN_EXCEPTION "+e);Application.Quit(2);yield break;}yield return current;}}
 static void Rain(bool enabled){var p=ContentJson.Object(ContentJson.Parse(ContentTuning.CaptureJson()));foreach(var item in ContentJson.Array(p["values"])){var row=ContentJson.Object(item);if((string)row["id"]=="weather.rain.enabled")row["value"]=enabled?"true":"false";}p["contentHash"]=ContentTuning.ComputeHash(p);ContentTuning.UseJson(ContentJson.Stringify(p));}
 IEnumerator Stop(TaxiController car,bool rain){Rain(rain);car.automated=true;car.autoThrottle=0;car.autoServiceBrake=0;car.autoBrake=false;car.transform.SetPositionAndRotation(new Vector3(10000,.7f,10000),Quaternion.identity);car.body.position=car.transform.position;car.body.rotation=Quaternion.identity;car.body.velocity=Vector3.zero;car.body.angularVelocity=Vector3.zero;Physics.SyncTransforms();for(int i=0;i<60;i++)yield return new WaitForFixedUpdate();car.body.velocity=Vector3.forward*(20/3.6f);car.autoServiceBrake=.3f;float z=car.body.position.z;for(int i=0;i<400&&car.body.velocity.magnitude>.12f;i++)yield return new WaitForFixedUpdate();stopDistance=car.body.position.z-z;Check(car.body.velocity.magnitude<.2f,"20kmh stopped "+(rain?"rain":"dry")+" distance="+stopDistance);}
 IEnumerator Run(){
  Check(ContentTuningRuntime.TestMode,"isolated saves required");yield return null;FindObjectOfType<UnifiedModeBootstrap>().StartSingle();
  while(!s||!s.StartupReady||s.business.IsRestoring){foreach(var x in FindObjectsOfType<DriveSession>())if(!x.IsSimulationBot)s=x;yield return null;}
  Check(s.business.SavePath.StartsWith(ContentTuningRuntime.TestDirectory,StringComparison.OrdinalIgnoreCase),"save isolation");Rain(true);
  var panel=DrivingSettingsPanel.For(s);panel.PreferenceKey="rain-panel-test-"+Guid.NewGuid().ToString("N");s.paused=true;Time.timeScale=0;
  panel.Open();Check(!panel.WeatherTab,"default driving tab");panel.WeatherTab=true;panel.WeatherDraft.enabled=false;Check(RainPolicy.Enabled,"draft not applied");panel.Cancel();Check(RainPolicy.Enabled,"cancel retains rain");
  panel.Open();panel.WeatherTab=true;panel.WeatherDraft.enabled=false;panel.Apply();Check(!panel.IsOpen&&!RainPolicy.Enabled,"ingame dry applied");
  var savedDry=BusinessProgress.ParseCheckpoint(File.ReadAllText(s.business.SavePath));ContentTuningRuntime.Restore(savedDry.contentProfileJson);Check(!RainPolicy.Enabled,"ingame weather persisted");
  panel.Open();panel.WeatherTab=true;panel.WeatherDraft.enabled=true;panel.Apply();Check(!panel.IsOpen&&RainPolicy.Enabled,"ingame rain applied");
  panel.Open();panel.WeatherDraft.brakeMultiplier=float.NaN;panel.Apply();Check(panel.IsOpen&&RainPolicy.Enabled&&!string.IsNullOrEmpty(panel.Error),"invalid weather rolls back");panel.Cancel();
  string originalSave=File.ReadAllText(s.business.SavePath);panel.Open();panel.WeatherDraft.enabled=false;
  using(var lockedSave=new FileStream(s.business.SavePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){panel.Apply();Check(panel.IsOpen&&RainPolicy.Enabled&&!string.IsNullOrEmpty(panel.Error),"filesystem save failure rolls back weather");}
  Check(File.ReadAllText(s.business.SavePath)==originalSave,"failed save preserves checkpoint");panel.Cancel();
  var weatherWire=JsonUtility.FromJson<PvpNativeFrame>(JsonUtility.ToJson(new PvpNativeFrame{weather=RainSettings.Capture()}));Check(weatherWire.weather.Same(RainSettings.Capture()),"weather frame roundtrip");
  PlayerPrefs.DeleteKey(panel.PreferenceKey);PlayerPrefs.Save();s.paused=false;Time.timeScale=1;
  var visual=s.GetComponent<RainPresentation>();Check(visual,"presentation installed");s.drivingCamera.SetFirstPerson(true);yield return null;yield return null;Check(visual.WipersVisible,"front automatic wipers");s.drivingCamera.testRearView=true;yield return null;yield return null;Check(!visual.WipersVisible,"rear view clear");s.drivingCamera.testRearView=false;s.drivingCamera.SetFirstPerson(false);yield return null;Check(!visual.WipersVisible,"third person clear");
  var care=s.business.ledger.Care;care.Wash();var j=s.jobs;j.ClearForDay();j.ForcedRideCase=DrunkRideCase.Normal;j.offers[0]=new JobOffer{pickup=0,drop=3,fare=5000,seconds=400,requiredScore=70,distance=j.nav.Distance(j.stops[0],j.stops[3])};
  var stop=j.stops[0];s.taxi.body.position=(stop.Kerb+Vector3.up*.65f);s.taxi.body.rotation=Quaternion.LookRotation(stop.Direction);s.taxi.transform.SetPositionAndRotation((stop.Kerb+Vector3.up*.65f),Quaternion.LookRotation(stop.Direction));s.taxi.body.velocity=Vector3.zero;Physics.SyncTransforms();
  Check(j.Accept(0)&&j.Interact(),"real passenger boards");Check(j.trip.State==TripState.OnTrip&&care.Cleanliness==98,"one rain boarding -2");Check(j.CleanBonus==3,"bonus based on preboarding condition");Check(!care.Soil(s.law.tripId+":rain-boarding",2),"duplicate event rejected");
  int beforeWeatherToggle=care.Cleanliness;panel.Open();panel.WeatherTab=true;panel.WeatherDraft.enabled=false;panel.Apply();panel.Open();panel.WeatherDraft.enabled=true;panel.Apply();Check(care.Cleanliness==beforeWeatherToggle&&j.trip.State==TripState.OnTrip,"live toggle does not resoil current passenger");PlayerPrefs.DeleteKey(panel.PreferenceKey);PlayerPrefs.Save();
  var restored=VehicleCare.Restore(care.Snapshot());Check(restored.Cleanliness==98&&!restored.Soil(s.law.tripId+":rain-boarding",2),"care replay preserved");Check(s.business.SaveCheckpoint(),"checkpoint saved");var saved=BusinessProgress.ParseCheckpoint(File.ReadAllText(s.business.SavePath));Rain(false);ContentTuningRuntime.Restore(saved.contentProfileJson);Check(RainPolicy.Enabled,"saved weather restores");
  var threshold=new VehicleCare();threshold.Soil("setup",69);threshold.Soil("rain",2);Check(threshold.Cleanliness==29&&threshold.NeedsWash,"next boarding blocked at threshold");Check(j.trip.State==TripState.OnTrip,"current passenger retained");Check(!RainPolicy.ProbabilisticSlipEnabled,"probability locked");
  var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Rain isolated physical ground";ground.transform.position=new Vector3(10000,-.55f,10000);ground.transform.localScale=new Vector3(100,1,200);
  var car=new GameObject("Rain brake probe").AddComponent<TaxiController>();car.Create(new Vector3(10000,.7f,10000),Quaternion.identity);yield return Stop(car,false);float dry=stopDistance;yield return Stop(car,true);float wet=stopDistance;Check(wet>dry*1.025f&&wet<dry*1.6f,"rain stopping distance bounded dry="+dry+" wet="+wet);Destroy(car.gameObject);Destroy(ground);
  Rain(true);yield return new WaitForSeconds(1);Check(visual.ParticleCount>0,"rain emission active");
  s.drivingCamera.SetFirstPerson(true);yield return new WaitForSeconds(.4f);string shot=Path.Combine(Application.dataPath,"../../../artifacts/rain/first-person.png");var camera=s.drivingCamera.GetComponent<Camera>();var rt=new RenderTexture(1280,720,24);var previous=camera.targetTexture;var previousActive=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(shot,pixels.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=previousActive;Destroy(pixels);Destroy(rt);yield return null;
  Debug.Log("RAIN_PLAYER_OK checks="+checks);Application.Quit(0);
 }
}
