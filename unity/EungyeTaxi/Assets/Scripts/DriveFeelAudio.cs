using System;
using System.Runtime.InteropServices;
using UnityEngine;

// 08 chapter 1 (2026-11-24, Eungye single): extra driving sound layers made only from the existing recordings
// (Engine.wav, Road.wav re-pitched), and gamepad vibration. Passenger lines and radio subtitles take priority:
// while either is on screen the extra layers duck. Strengths live in the feel tab; numbers are feel.* settings.
public static class DriveFeelAudio {
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 public static float Duck {get{return Mathf.Clamp01(F("feel.duckLevel",.45));}}
 // Engine load: extra gain on the engine voice while the driver is on the throttle (no second copy of the loop).
 public static float LoadLevel(float load,float rev){return F("feel.load.level",.16)*Mathf.Clamp01(load)*(.4f+.6f*Mathf.Clamp01(rev));}
 // Slip beyond the tyre's grip peak (0 while the tyre still grips). Longitudinal slip counts half, because
 // its peak sits about twice as far out as the sideways one.
 public static float SkidSlip(float sideways,float sidewaysPeak,float forward,float forwardPeak){
  return Mathf.Max(0,Mathf.Max(Mathf.Abs(sideways)-sidewaysPeak,(Mathf.Abs(forward)-forwardPeak)*.5f));
 }
 // Tyres past the grip limit: a hiss that grows with the skid once the taxi is moving (18 km/h and up).
 public static float TyreLevel(float slip,float speed){
  if(speed<5)return 0;float a=F("feel.tyre.slipStart",.12),b=Mathf.Max(a+.01f,F("feel.tyre.slipFull",.35));
  return F("feel.tyre.level",.22)*Mathf.SmoothStep(0,1,(Mathf.Abs(slip)-a)/(b-a));
 }
 // Wind: an airy layer that stays nearly silent in town and opens up on fast roads. The road loop is played
 // above its recorded pitch; played an octave down (as before) it was a low drone, not wind.
 public static float WindLevel(float speed){
  float kmh=Mathf.Abs(speed)*3.6f,a=F("feel.wind.startKmh",35),b=Mathf.Max(a+1,F("feel.wind.fullKmh",90));
  float t=Mathf.SmoothStep(0,1,(kmh-a)/(b-a));return F("feel.wind.level",.18)*t*t;
 }
 public static float WindPitch(float speed){return 1.3f+.2f*Mathf.Clamp01(Mathf.Abs(speed)*3.6f/90);}
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
