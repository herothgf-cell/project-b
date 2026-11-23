using System;
using System.IO;
using UnityEngine;

// 08 chapter 1 (2026-11-24): extra sound layers and pad vibration mixes (Editor, no scene).
public static class FeelSoundChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("FEEL_SOUND "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  Check(DriveFeelAudio.LoadLevel(0,1)==0&&DriveFeelAudio.LoadLevel(1,1)>.15f&&DriveFeelAudio.LoadLevel(1,0)<DriveFeelAudio.LoadLevel(1,1),"engine load only on throttle, louder at high revs");
  Check(DriveFeelAudio.TyreLevel(.05f,10)==0&&DriveFeelAudio.TyreLevel(.5f,10)>.2f&&DriveFeelAudio.TyreLevel(.5f,1)==0,"tyre layer starts near the grip limit, not when parked");
  Check(DriveFeelAudio.WindLevel(30/3.6f)==0&&DriveFeelAudio.WindLevel(60/3.6f)>0&&DriveFeelAudio.WindLevel(100/3.6f)>=DriveFeelAudio.WindLevel(60/3.6f),"wind grows from 35 km/h");
  Check(DriveFeelAudio.Duck>0&&DriveFeelAudio.Duck<1,"speech ducks the extra layers");
  float low,high;
  DriveFeelAudio.Rumble(0,0,false,out low,out high);Check(low==0&&high==0,"parked: no vibration");
  DriveFeelAudio.Rumble(15,0,false,out low,out high);Check(low>0&&low<.2f&&high==0,"road texture on the low motor");
  DriveFeelAudio.Rumble(10,6,false,out low,out high);Check(high>.4f,"hard braking on the high motor");
  DriveFeelAudio.Rumble(10,0,true,out low,out high);Check(low>=.9f&&high>=.9f,"contact pulse on both motors");
  var s=new DrivingSettings{extraSound=2,rumble=-1}.Normalize();Check(s.extraSound==1&&s.rumble==0,"strengths clamp to 0-1");
  var legacy=JsonUtility.FromJson<DrivingSettings>("{\"sensitivity\":1}");Check(legacy.extraSound==1&&legacy.rumble==1,"older settings default to full strength");
  Check(new DrivingSettings{extraSound=.3f,rumble=.6f}.Copy().rumble==.6f,"copy keeps strengths");
  Debug.Log("FEEL_SOUND_OK checks="+checks);
 }
}
