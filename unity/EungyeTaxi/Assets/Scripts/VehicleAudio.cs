using UnityEngine;

// The local driver hears a mono interior mix. Engine load and wheel speed drive separate recorded layers.
public sealed class VehicleAudio : MonoBehaviour {
 DriveSession session;
 AudioSource engine,road,indicator,horn,rain;
 AudioClip indicatorOn,indicatorOff;
 bool lampWasLit;

 public void Initialize(DriveSession owner){
  session=owner;
  engine=Source("Engine",true);
  road=Source("Road",true);
  indicator=Source("IndicatorOn",false);
  indicatorOn=indicator.clip;
  indicatorOff=LoadClip("IndicatorOff");
  horn=Source("Horn",false);
  rain=Source("Rain",true);
 }
 AudioSource Source(string sound,bool loop){
  var source=gameObject.AddComponent<AudioSource>();
  source.playOnAwake=false;
  source.loop=loop;
  source.spatialBlend=0;
  source.clip=LoadClip(sound);
  return source;
 }
 public static AudioClip LoadClip(string sound){
  return Resources.Load<AudioClip>("Audio/Taxi/"+sound);
 }
 static void Loop(AudioSource source,float volume){
  if(volume<=0){if(source.isPlaying)source.Stop();return;}
  source.volume=volume;
  if(!source.isPlaying)source.Play();
 }
 // Speed is in metres per second. Throttle adds load while speed carries the main pitch change.
 public static void EngineMix(float speed,float power,out float level,out float pitch){
  float rolling=speed/(speed+16);
  level=.25f+.48f*rolling+.19f*power;
  pitch=.76f+1.16f*rolling+.28f*power;
 }
 void UpdateIndicator(bool enabled){
  bool lit=enabled&&session.signals&&session.signals.Lit;
  if(enabled&&lit!=lampWasLit){
   indicator.clip=lit?indicatorOn:indicatorOff;
   indicator.volume=lit?.60f:.52f;
   indicator.Play();
  }
  else if(!enabled&&indicator.isPlaying)indicator.Stop();
  lampWasLit=lit;
 }
 static void MotionLoop(AudioSource source,bool enabled,float level,float pitch,float blend){
  if(!enabled){source.volume=0;Loop(source,0);return;}
  if(!source.isPlaying){if(level<=0)return;source.volume=0;source.pitch=pitch;}
  source.pitch=Mathf.Lerp(source.pitch,pitch,blend);
  float volume=Mathf.Lerp(source.volume,level,blend);
  Loop(source,volume<.001f&&level==0?0:volume);
 }
 void LateUpdate(){
  if(!session||!session.StartupReady)return;
  var taxi=session.taxi;
  var settings=DrivingSettings.Current;
  bool driving=Time.timeScale>0&&!session.paused&&!session.FullMapOpen&&!session.SettingsOpen&&
   !session.PracticeMenuOpen&&!session.ChoiceOpen&&!session.services.open&&!session.services.PostTripOpen&&
   !session.business.preparing&&!session.business.ShowingSummary&&!PvpNativeLan.UiBlocked(session);
  float speed=Mathf.Abs(taxi.Speed);
  float power=Mathf.Clamp01(Mathf.Abs(taxi.wheels[2].motorTorque)/1100f);
  EngineMix(speed,power,out float level,out float pitch);
  float blend=1-Mathf.Exp(-Time.unscaledDeltaTime/.065f);
  bool driveSound=driving&&settings.engineSound&&ContentTuning.Flag("display.sound.engine",true);
  MotionLoop(engine,driveSound,level,pitch,blend);
  MotionLoop(road,driveSound,.46f*speed/(speed+9),1,blend);
  UpdateIndicator(driving&&settings.indicatorSound&&ContentTuning.Flag("display.sound.indicator",true));
  Loop(rain,driving&&settings.rainSound&&ContentTuning.Flag("display.sound.rain",true)?.58f*RainPolicy.Intensity:0);
  if(!driving||!settings.hornSound||!ContentTuning.Flag("display.sound.horn",true)){if(horn.isPlaying)horn.Stop();}
  else if(Input.GetKeyDown(KeyCode.Y)||Input.GetKeyDown(KeyCode.JoystickButton3)){
   horn.volume=.48f;horn.Play();
  }
 }
}
