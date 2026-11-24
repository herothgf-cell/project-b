using System;
using System.Runtime.InteropServices;
using UnityEngine;

// 08 chapter 1 (2026-11-24, Eungye single): driving sound mix helpers and gamepad vibration. Passenger lines and radio subtitles take priority:
// while either is on screen the extra layers duck. Strengths live in the feel tab; numbers are feel.* settings.
public static class DriveFeelAudio {
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 public static float Duck {get{return Mathf.Clamp01(F("feel.duckLevel",.45));}}
 // Engine load: extra gain on the engine voice while the driver is on the throttle (no second copy of the loop).
 public static float LoadLevel(float load,float rev){return F("feel.load.level",.16)*Mathf.Clamp01(load)*(.4f+.6f*Mathf.Clamp01(rev));}
 // 2026-11-24: cabin sound from two interior recordings. Overall level follows speed; the highway
 // recording takes over from the town one between 25 and 70 km/h. Speeds are metres per second.
 public static float CabinLevel(float speed){return .62f*speed*speed/(speed*speed+90);}
 public static float HighwayShare(float speed){return Mathf.SmoothStep(0,1,(Mathf.Abs(speed)*3.6f-25)/45);}
 // Open window: outside air, silent when parked and strong on fast roads.
 public static float WindowLevel(float speed){return .5f*speed*speed/(speed*speed+150);}
 // Hard braking: the emergency brake above walking pace, or a pedal stop of 6.5 m/s2 and more.
 public static bool HardBraking(float deceleration,float speed,bool emergency){return speed>4&&(emergency||deceleration>=6.5f);}
 public static float HardBrakeLevel(float speed){return .5f*Mathf.Clamp(Mathf.Abs(speed)/12,.45f,1);}
 public static bool SpeechOnScreen(DriveSession s){
  if(!s)return false;
  bool passenger=s.requests&&s.requests.HasVisiblePanel;
  bool radio=s.radio&&s.radio.VisibleKind.HasValue;
  return passenger||radio;
 }
 // Gamepad motors (0-1): road texture on the low motor, braking on the high motor, a short contact pulse on both.
 public static void Rumble(float speed,float deceleration,bool contact,out float low,out float high){
  float road=F("feel.rumble.road",.08)*Mathf.Clamp01(Mathf.Abs(speed)/15f);
  float brake=Mathf.Clamp01((deceleration-F("feel.rumble.brakeStart",2))*F("feel.rumble.brakeScale",.12));
  float hit=contact?F("feel.rumble.contact",.9f):0;
  low=Mathf.Clamp01(Mathf.Max(road,hit));high=Mathf.Clamp01(Mathf.Max(brake,hit));
 }
 public static float ContactSeconds {get{return F("feel.rumble.contactSeconds",.25);}}
}

// Windows XInput (built into Windows 8+), so no extra package is needed. Anything else is silently ignored.
public static class XInputRumble {
 [StructLayout(LayoutKind.Sequential)] struct Vibration {public ushort left,right;}
#if UNITY_STANDALONE_WIN||UNITY_EDITOR_WIN
 [DllImport("xinput1_4.dll")] static extern uint XInputSetState(uint user,ref Vibration vibration);
#endif
 static bool failed;static float lastLow=-1,lastHigh=-1;
 public static bool Available {get{return !failed;}}
 public static void Set(float low,float high){
  low=Mathf.Clamp01(low);high=Mathf.Clamp01(high);
  if(failed||Mathf.Abs(low-lastLow)<.01f&&Mathf.Abs(high-lastHigh)<.01f)return;lastLow=low;lastHigh=high;
#if UNITY_STANDALONE_WIN||UNITY_EDITOR_WIN
  try{var v=new Vibration{left=(ushort)(low*65535),right=(ushort)(high*65535)};XInputSetState(0,ref v);}catch(Exception){failed=true;}
#else
  failed=true;
#endif
 }
 public static void Stop(){if(lastLow>0||lastHigh>0){lastLow=lastHigh=-1;Set(0,0);}}
}
