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
   Check(prefs.engineSound&&prefs.brakeSound&&prefs.hornSound&&prefs.rainSound,"Legacy defaults");
   var panel=new GameObject("Sound settings fixture").AddComponent<DrivingSettingsPanel>();
   try {
    panel.PreferenceKey=key;panel.Open();panel.SoundTab=true;
    panel.Draft.engineSound=false;panel.Cancel();Check(DrivingSettings.Load(key).engineSound,"Cancel sound edit");
    panel.Open();panel.SoundTab=true;panel.Draft.engineSound=panel.Draft.hornSound=false;panel.Apply();
    prefs=DrivingSettings.Load(key);
    Check(!prefs.engineSound&&prefs.brakeSound&&!prefs.hornSound&&prefs.rainSound,"Independent saved switches");
    panel.Open();panel.ResetDefaults();Check(!panel.Draft.engineSound,"Driving reset preserves sound");
    panel.SoundTab=true;panel.ResetDefaults();Check(panel.Draft.engineSound&&panel.Draft.hornSound,"Sound reset");
   }finally{UnityEngine.Object.DestroyImmediate(panel.gameObject);}
  }finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
  foreach(var sound in new[]{"Engine","Brake","Horn","Rain"}){
   var clip=VehicleAudio.CreateClip(sound);
   try {
    var samples=new float[clip.samples];
    Check(clip.GetData(samples,0),"Read "+sound);
    float peak=0;foreach(float sample in samples)peak=Mathf.Max(peak,Mathf.Abs(sample));
    Check(peak>.1f&&peak<=1,sound+" amplitude");
    if(sound=="Horn")Check(samples[clip.frequency/5]==0,"Two horn attacks have a silent gap");
   }finally{UnityEngine.Object.DestroyImmediate(clip);}
  }
  Debug.Log("CABIN_SOUND_CHECKS_OK places="+seen.Count+" clips=4");
 }
 public static void VerifyAndBuild(){Run();PrototypeBuild.BuildHybrid();}
}
