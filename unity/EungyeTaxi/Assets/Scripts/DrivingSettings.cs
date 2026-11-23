using System;
using UnityEngine;

// Device preferences, independent of campaign saves and host content profiles.
[Serializable] public sealed class DrivingSettings {
 public const string Key="driving.controls.v1";
 public const bool DefaultRadioSubtitles=true,DefaultPvpNightLighting=false;
 public bool cabinAssist=true;
 // Missing fields in older preference JSON retain the enabled defaults.
 public bool engineSound=true,indicatorSound=true,hornSound=true,rainSound=true;
 public bool radioSubtitles=DefaultRadioSubtitles,pvpNightLighting=DefaultPvpNightLighting;
 public float sensitivity=1,response=1,centering=1,deadzone=.15f,braking=1;
 // Steering feel (08 chapter 1): 0 casual, 1 sim (r50 tuning). -1 only while loading: a device with no
 // saved preferences starts casual; preferences saved before this field existed stay sim.
 public const int Casual=0,Sim=1;
 public int steeringFeel=-1;
 // Presentation strengths, 0 (off) to 1. They never change physics, saves or PvP state.
 public float speedFov=1,steerLook=1,brakeDip=1,bodyLean=1;
 // 2026-11-24: extra engine-load/tyre/wind layers and gamepad vibration (0 = off).
 public float extraSound=1,rumble=1;
 public bool CasualSteering {get{return steeringFeel==Casual;}}
 static DrivingSettings current;
 public static DrivingSettings Current {get{return current??(current=Load(Key));}}
 public DrivingSettings Copy(){
  return new DrivingSettings{sensitivity=sensitivity,response=response,centering=centering,deadzone=deadzone,braking=braking,
   steeringFeel=steeringFeel,speedFov=speedFov,steerLook=steerLook,brakeDip=brakeDip,bodyLean=bodyLean,extraSound=extraSound,rumble=rumble,
   engineSound=engineSound,indicatorSound=indicatorSound,hornSound=hornSound,rainSound=rainSound,
   cabinAssist=cabinAssist,radioSubtitles=radioSubtitles,pvpNightLighting=pvpNightLighting};
 }
 public static float Bound(float value,float min,float max,float fallback){return float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,min,max);}
 public DrivingSettings Normalize(){sensitivity=Bound(sensitivity,.5f,1.5f,1);response=Bound(response,.25f,2,1);centering=Bound(centering,.25f,2,1);deadzone=Bound(deadzone,0,.4f,.15f);braking=Bound(braking,.5f,1.5f,1);
  if(steeringFeel!=Casual&&steeringFeel!=Sim)steeringFeel=Casual;
  speedFov=Bound(speedFov,0,1,1);steerLook=Bound(steerLook,0,1,1);brakeDip=Bound(brakeDip,0,1,1);bodyLean=Bound(bodyLean,0,1,1);extraSound=Bound(extraSound,0,1,1);rumble=Bound(rumble,0,1,1);return this;}
 public static float BrakeForce(float force,float gain,bool emergency,bool serviceInput){return emergency||!serviceInput?force:Mathf.Clamp01(force*Bound(gain,.5f,1.5f,1));}
 public float Input(float keyboard,float pad){return Mathf.Abs(pad)>deadzone?Mathf.Clamp(pad,-1,1):Mathf.Clamp(keyboard,-1,1);}
 public float Advance(float previous,float target,float speed,float seconds){return DriveFlowPolicy.AdvanceSteering(previous,target,speed,seconds*(Mathf.Abs(target)<.01f?centering:response));}
 public static float Angle(float input,float kph,float gain,bool casual=false){return Mathf.Clamp(input*DriveMath.ManualSteerAngle(kph,casual)*Bound(gain,.5f,1.5f,1),-52,52);}
 public static DrivingSettings Load(string key){
  var result=new DrivingSettings();try{string json=PlayerPrefs.GetString(key,"");if(!string.IsNullOrEmpty(json)){JsonUtility.FromJsonOverwrite(json,result);if(result.steeringFeel<0)result.steeringFeel=Sim;}}catch(ArgumentException){result=new DrivingSettings();}
  return result.Normalize();
 }
 public void Save(string key){var safe=Copy().Normalize();PlayerPrefs.SetString(key,JsonUtility.ToJson(safe));PlayerPrefs.Save();}
 public static void Commit(DrivingSettings value){var safe=value.Copy().Normalize();safe.Save(Key);current=safe;}
}
