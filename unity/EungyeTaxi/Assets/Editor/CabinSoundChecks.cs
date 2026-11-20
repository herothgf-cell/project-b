using System;
using System.Collections.Generic;
using UnityEngine;

public static class CabinSoundChecks {
 static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);}
 public static void Run(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();
  var catalog=PassengerStopCatalog.For(map);
  var seen=new HashSet<string>();
  foreach(var stop in catalog.Stops){
   var name=KoreanText.Stop(stop.name);
   Check(!name.Contains("승하차")&&seen.Add(name),"Unknown or duplicate location: "+name);
  }
  Check(seen.Count==39&&KoreanText.Stop("게임 대체 승하차 2-A").Contains("은계남로"),"Alternative and regular locations");
  string key="cabin-audio-test-"+Guid.NewGuid();
  try {
   PlayerPrefs.SetString(key,"{}");var prefs=DrivingSettings.Load(key);
   Check(prefs.engineSound&&prefs.indicatorSound&&prefs.hornSound&&prefs.rainSound,"Legacy defaults");
   var panel=new GameObject("Sound settings fixture").AddComponent<DrivingSettingsPanel>();
   try {
    panel.PreferenceKey=key;panel.Open();panel.SoundTab=true;
    panel.Draft.engineSound=false;panel.Cancel();Check(DrivingSettings.Load(key).engineSound,"Cancel sound edit");
    panel.Open();panel.SoundTab=true;panel.Draft.engineSound=panel.Draft.hornSound=false;panel.Apply();
    prefs=DrivingSettings.Load(key);
    Check(!prefs.engineSound&&prefs.indicatorSound&&!prefs.hornSound&&prefs.rainSound,"Independent saved switches");
    panel.Open();panel.ResetDefaults();Check(!panel.Draft.engineSound,"Driving reset preserves sound");
    panel.SoundTab=true;panel.ResetDefaults();Check(panel.Draft.engineSound&&panel.Draft.hornSound,"Sound reset");
   }finally{UnityEngine.Object.DestroyImmediate(panel.gameObject);}
  }finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
  foreach(var sound in new[]{"Engine","IndicatorOn","IndicatorOff","Horn","Rain"}){
   var clip=VehicleAudio.CreateClip(sound);
   try {
    var samples=new float[clip.samples];
    Check(clip.GetData(samples,0),"Read "+sound);
    float peak=0;foreach(float sample in samples)peak=Mathf.Max(peak,Mathf.Abs(sample));
    Check(peak>.1f&&peak<=1,sound+" amplitude");
    if(sound=="Engine"){
     VehicleAudio.EngineMix(12.5f,.25f,out float cruise,out float cruisePitch);
     VehicleAudio.EngineMix(2,0,out float slow,out float slowPitch);
     double energy=0;foreach(float sample in samples)energy+=sample*sample;
     Check(Math.Sqrt(energy/samples.Length)*cruise>.10,"Audible cruising engine RMS");
     Check(cruise>slow&&cruisePitch>slowPitch,"Engine recedes toward stop");
    }
    if(sound.StartsWith("Indicator"))Check(clip.length<.08f&&Math.Abs(samples[samples.Length-1])<.001f,"Short decaying relay click");
    if(sound=="Horn")Check(samples[clip.frequency/5]==0,"Two horn attacks have a silent gap");
   }finally{UnityEngine.Object.DestroyImmediate(clip);}
  }
  // A muted, retired brake effect must not mute the new indicator in an existing campaign.
  ContentTuningRuntime.Initialize();
  var oldProfile=ContentJson.Object(ContentJson.Parse(ContentTuning.CaptureJson()));
  foreach(var item in ContentJson.Array(oldProfile["values"])){
   var row=ContentJson.Object(item);
   if((string)row["id"]=="display.sound.indicator"){row["id"]="display.sound.brake";row["value"]="false";}
  }
  oldProfile["contentHash"]=ContentTuning.ComputeHash(oldProfile);
  var upgraded=ContentJson.Object(ContentJson.Parse(ContentTuning.UpgradeJson(ContentJson.Stringify(oldProfile))));
  bool indicatorEnabled=false;
  foreach(var item in ContentJson.Array(upgraded["values"])){
   var row=ContentJson.Object(item);
   Check((string)row["id"]!="display.sound.brake","retired brake setting removed");
   if((string)row["id"]=="display.sound.indicator")indicatorEnabled=(string)row["value"]=="true";
  }
  Check(indicatorEnabled,"legacy brake mute is independent of relay");
  Debug.Log("CABIN_SOUND_CHECKS_OK places="+seen.Count+" clips=5");
 }
 public static void VerifyAndBuild(){Run();PrototypeBuild.BuildHybrid();}
}
