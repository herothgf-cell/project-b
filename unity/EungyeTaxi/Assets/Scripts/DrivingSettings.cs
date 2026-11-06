using System;
using UnityEngine;

// Device preferences, independent of campaign saves and host content profiles.
[Serializable] public sealed class DrivingSettings {
 public const string Key="driving.controls.v1";
 public float sensitivity=1,response=1,centering=1,deadzone=.15f;
 static DrivingSettings current;
 public static DrivingSettings Current {get{return current??(current=Load(Key));}}
 public DrivingSettings Copy(){return new DrivingSettings{sensitivity=sensitivity,response=response,centering=centering,deadzone=deadzone};}
 public static float Bound(float value,float min,float max,float fallback){return float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,min,max);}
 public DrivingSettings Normalize(){sensitivity=Bound(sensitivity,.5f,1.5f,1);response=Bound(response,.25f,2,1);centering=Bound(centering,.25f,2,1);deadzone=Bound(deadzone,0,.4f,.15f);return this;}
 public float Input(float keyboard,float pad){return Mathf.Abs(pad)>deadzone?Mathf.Clamp(pad,-1,1):Mathf.Clamp(keyboard,-1,1);}
 public float Advance(float previous,float target,float speed,float seconds){return DriveFlowPolicy.AdvanceSteering(previous,target,speed,seconds*(Mathf.Abs(target)<.01f?centering:response));}
 public static float Angle(float input,float kph,float gain){return Mathf.Clamp(input*DriveMath.ManualSteerAngle(kph)*Bound(gain,.5f,1.5f,1),-52,52);}
 public static DrivingSettings Load(string key){
  var result=new DrivingSettings();try{string json=PlayerPrefs.GetString(key,"");if(!string.IsNullOrEmpty(json))JsonUtility.FromJsonOverwrite(json,result);}catch(ArgumentException){result=new DrivingSettings();}
  return result.Normalize();
 }
 public void Save(string key){var safe=Copy().Normalize();PlayerPrefs.SetString(key,JsonUtility.ToJson(safe));PlayerPrefs.Save();}
 public static void Commit(DrivingSettings value){var safe=value.Copy().Normalize();safe.Save(Key);current=safe;}
}
