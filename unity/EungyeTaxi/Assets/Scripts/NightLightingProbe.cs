using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

// Opt-in Player regression with rendered frames and controlled stop placement.
public sealed class NightLightingProbe:MonoBehaviour {
 static readonly bool requested=Array.IndexOf(Environment.GetCommandLineArgs(),"-dayLightFrameCheck")>=0;
 const int FrameCount=480;
 DriveSession session;
 int checks;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){
  if(!requested)return;
  var probe=new GameObject("Night lighting acceptance");
  DontDestroyOnLoad(probe);probe.AddComponent<NightLightingProbe>();
 }
 void Check(bool pass,string context){
  checks++;Debug.Log("DAYLIGHT_PLAYER "+(pass?"PASS ":"FAIL ")+context);
  if(!pass)throw new InvalidOperationException(context);
 }
 IEnumerator Start(){
  var run=Run();
  while(true){
   object next;
   try{if(!run.MoveNext())yield break;next=run.Current;}
   catch(Exception error){Debug.LogException(error);Application.Quit(2);yield break;}
   yield return next;
  }
 }
 void CheckDisplaySettings(){
  var panel=DrivingSettingsPanel.For(session);
  string originalKey=panel.PreferenceKey;
  string fixture="display-night-test-"+Guid.NewGuid().ToString("N");
  panel.PreferenceKey=fixture;
  try{
   PlayerPrefs.SetString(fixture,"{\"sensitivity\":0.8}");
   var migrated=DrivingSettings.Load(fixture);
   Check(migrated.radioSubtitles&&migrated.pvpNightLighting==DrivingSettings.DefaultPvpNightLighting,"old driving settings inherit display defaults");
   panel.Open();panel.DisplayTab=true;panel.Draft.radioSubtitles=false;panel.Draft.pvpNightLighting=!DrivingSettings.DefaultPvpNightLighting;
   panel.Cancel();
   Check(DrivingSettings.Load(fixture).radioSubtitles,"cancel preserves display settings");
   panel.Open();panel.DisplayTab=true;panel.Draft.radioSubtitles=false;panel.Draft.pvpNightLighting=!DrivingSettings.DefaultPvpNightLighting;panel.Apply();
   var saved=DrivingSettings.Load(fixture);
   Check(!saved.radioSubtitles&&saved.pvpNightLighting!=DrivingSettings.DefaultPvpNightLighting,"display settings save and reload together");
   panel.Open();panel.DisplayTab=true;panel.ResetDefaults();
   Check(panel.Draft.radioSubtitles&&panel.Draft.pvpNightLighting==DrivingSettings.DefaultPvpNightLighting,"display reset restores defaults");panel.Cancel();
  }finally{panel.PreferenceKey=originalKey;PlayerPrefs.DeleteKey(fixture);PlayerPrefs.Save();}
 }
 IEnumerator Run(){
  Check(ContentTuningRuntime.TestMode,"isolated test profile and save");
  yield return null;
  var bootstrap=FindObjectOfType<UnifiedModeBootstrap>();if(bootstrap)bootstrap.StartSingle();
  while(!(session=FindObjectOfType<DriveSession>())||!session.StartupReady||session.business.IsRestoring)yield return null;
  yield return null;
  var business=session.business;
  Check(session.dayLight&&session.dayLight.IsActive&&session.dayLight.CurrentPhase==DayLightPhase.Night,"nighttime visual controller is active");
  Check(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null,"rendering device "+SystemInfo.graphicsDeviceType);
  Check(DayLightController.PhaseAt(8)==DayLightPhase.Morning&&DayLightController.PhaseAt(10)==DayLightPhase.Day&&
   DayLightController.PhaseAt(17)==DayLightPhase.Sunset&&DayLightController.PhaseAt(19)==DayLightPhase.Night,"authored lighting phase boundaries");
  Check(DayLightController.HourForElapsed(0,1200)==8&&DayLightController.HourForElapsed(1200,1200)==20&&
   DayLightController.HourForElapsed(1300,1200)>20,"clock includes overtime");
  Check(DayLightController.Sample(14,true).Ambient.grayscale<DayLightController.Sample(14,false).Ambient.grayscale,
    "rain reduces environmental light");
  CheckDisplaySettings();
  DrivingSettings.Current.radioSubtitles=true;
  business.ContinueFromMenu();
  float start=Time.realtimeSinceStartup;
  while(string.IsNullOrEmpty(session.radio.VisibleText)&&Time.realtimeSinceStartup-start<5)yield return null;
  Check(session.radio&&session.radio.VisibleText.Length>0&&session.radio.VisibleRemainingSeconds>0,"morning radio caption is scheduled");
  var map=session.GetComponent<FullMapOverlay>();Check(map.Open(),"test opened the actual map panel");
  yield return null;yield return null;
  Check(string.IsNullOrEmpty(session.radio.VisibleText),"map hides radio captions");
  map.Close();yield return null;yield return null;
  Check(session.radio.VisibleText.Length>0,"suppressed radio caption resumes after map");
  var state=session.radio.Capture();Check(state!=null,"active caption has checkpoint state");
  var copy=JsonUtility.FromJson<RadioScheduleSnapshot>(JsonUtility.ToJson(state));copy.Validate(business.ledger.Day);
  string spoken=session.radio.VisibleText;
  session.radio.Restore(copy);
  yield return null;
  Check(session.radio.VisibleText==spoken,"restored caption identity is unchanged");
  DrivingSettings.Current.radioSubtitles=false;yield return null;
  Check(string.IsNullOrEmpty(session.radio.VisibleText),"radio setting hides subtitles");
  DrivingSettings.Current.radioSubtitles=true;

  string output=Path.GetFullPath("artifacts/night-lighting-player");
  Directory.CreateDirectory(output);
  var frame=session.drivingCamera.CaptureCockpitFrame();
  Check(frame&&frame.IsCreated(),"night cockpit rendered into an offscreen camera target");
  var previous=RenderTexture.active;
  var image=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
  try{
   RenderTexture.active=frame;image.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);image.Apply();
   int brightest=0,darkest=765;
   foreach(var pixel in image.GetPixels32()){
    int brightness=pixel.r+pixel.g+pixel.b;
    brightest=Math.Max(brightest,brightness);darkest=Math.Min(darkest,brightness);
   }
   Check(brightest-darkest>=30,"night capture contains scene contrast");
   File.WriteAllBytes(Path.Combine(output,"night-cockpit.png"),image.EncodeToPNG());
  }finally{
   RenderTexture.active=previous;Destroy(image);frame.Release();Destroy(frame);
  }
  yield return new WaitForSecondsRealtime(3);
  var readings=new double[FrameCount];
  for(int i=0;i<readings.Length;i++){yield return null;readings[i]=Time.unscaledDeltaTime*1000;}
  Array.Sort(readings);
  int lights=session.dayLight.LitVehicleCount;
  Check(lights>0&&lights<=DayLightController.MaxLitVehicles,"night uses the bounded nearby vehicle lights");
  double p95=readings[Mathf.CeilToInt(.95f*readings.Length)-1];
  string token=p95.ToString("0.000",CultureInfo.InvariantCulture);
  Debug.Log("DAYLIGHT_SINGLE_PERF phase=Night frames="+readings.Length+" p95Ms="+token+" graphics="+SystemInfo.graphicsDeviceType+" litVehicles="+lights);
  Check(business.DevEndDayNow(),"day ends for room background verification");
  while(!business.ShowingSummary)yield return null;
  Check(business.RoomAvailable&&!PersonalRoomHUD.MorningPreviewAvailable(business),"settlement uses the tonight room surface");
  Check(business.BeginNextCampaignDay()&&PersonalRoomHUD.MorningPreviewAvailable(business),"next-day preparation offers the morning room surface");
  Debug.Log("DAYLIGHT_PLAYER_OK checks="+checks);Application.Quit(0);
 }
}
