using UnityEngine;

// The local driver hears a mono interior mix. Generate the clips once per session.
public sealed class VehicleAudio : MonoBehaviour {
 const int SampleRate=22050;
 DriveSession session;
 AudioSource engine,indicator,horn,rain;
 AudioClip indicatorOn,indicatorOff;
 bool lampWasLit;

 public void Initialize(DriveSession owner){
  session=owner;
  engine=Source("Engine",true);
  indicator=Source("IndicatorOn",false);
  indicatorOn=indicator.clip;
  indicatorOff=CreateClip("IndicatorOff");
  horn=Source("Horn",false);
  rain=Source("Rain",true);
 }
 AudioSource Source(string sound,bool loop){
  var source=gameObject.AddComponent<AudioSource>();
  source.playOnAwake=false;
  source.loop=loop;
  source.spatialBlend=0;
  source.clip=CreateClip(sound);
  return source;
 }
 public static AudioClip CreateClip(string sound){
  const float tau=2*Mathf.PI;
  bool relay=sound=="IndicatorOn"||sound=="IndicatorOff";
  if(!relay&&sound!="Engine"&&sound!="Horn"&&sound!="Rain")throw new System.ArgumentOutOfRangeException("sound");
  int length=relay?SampleRate/16:sound=="Horn"?SampleRate/2:SampleRate*2;
  var samples=new float[length];
  var random=new System.Random(763);
  float rumble=0,roof=0,droplet=0;
  for(int i=0;i<length;i++){
   float t=i/(float)SampleRate;
   float noise=(float)random.NextDouble()*2-1;
   rumble+=.052f*(noise-rumble);
   switch(sound){
    case "Horn":
     // A paired automotive horn, with two distinct attacks on each press.
     float local=t<.23f?t:t-.23f;
     float duration=t<.23f?.15f:.22f;
     float envelope=Mathf.Clamp01(local/.011f)*Mathf.Clamp01((duration-local)/.026f);
     samples[i]=envelope*(.34f*Mathf.Sin(tau*405*t)+.33f*Mathf.Sin(tau*495*t)+.14f*Mathf.Sin(tau*810*t)+.11f*Mathf.Sin(tau*990*t));
     break;
    case "IndicatorOn":
    case "IndicatorOff":
     // Separate pull-in and release clicks from the indicator relay.
     float contact=sound=="IndicatorOn"?1480:970;
     float hit=Mathf.Min(1,t*2400)*Mathf.Exp(-t*145);
     samples[i]=hit*(.43f*Mathf.Sin(tau*contact*t)+.25f*Mathf.Sin(tau*contact*1.63f*t)+.16f*noise);
     break;
    case "Rain":
     roof+=.33f*(noise-roof);
     droplet*=.984f;
     if(random.NextDouble()<.002)droplet=(float)random.NextDouble()*.48f;
     samples[i]=.6f*roof+droplet*Mathf.Sin(tau*1150*t);
     break;
    default:
     // Four-cylinder firing order, with intake harmonics audible above a small speaker's bass roll-off.
     float phase=tau*54*t+.12f*Mathf.Sin(tau*3*t)+.045f*Mathf.Sin(tau*8*t);
     float firing=.92f+.06f*Mathf.Sin(phase*.5f)+.02f*Mathf.Sin(phase*.25f);
     float block=.19f*Mathf.Sin(phase)+.22f*Mathf.Sin(2*phase+.35f);
     float intake=.23f*Mathf.Sin(3*phase+.7f)+.16f*Mathf.Sin(4*phase+1.1f)
                 +.10f*Mathf.Sin(5*phase+1.3f)+.055f*Mathf.Sin(7*phase+2);
     samples[i]=firing*(block+intake)+.22f*rumble;
     break;
   }
   // Taper the seam of finite noise loops.
   if(sound=="Engine"||sound=="Rain")samples[i]*=Mathf.Clamp01(Mathf.Min(i,length-1-i)/64f);
  }
  var clip=AudioClip.Create(sound,samples.Length,1,SampleRate,false);
  clip.SetData(samples,0);
  return clip;
 }
 static void Loop(AudioSource source,float volume){
  if(volume<=0){if(source.isPlaying)source.Stop();return;}
  source.volume=volume;
  if(!source.isPlaying)source.Play();
 }
 // Speed is in metres per second. Throttle adds load while speed carries the main pitch change.
 public static void EngineMix(float speed,float power,out float level,out float pitch){
  float rolling=speed/(speed+16);
  level=.25f+.48f*rolling+.16f*power;
  pitch=.76f+1.55f*rolling+.16f*power;
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
  MotionLoop(engine,driving&&settings.engineSound&&ContentTuning.Flag("display.sound.engine",true),level,pitch,blend);
  UpdateIndicator(driving&&settings.indicatorSound&&ContentTuning.Flag("display.sound.indicator",true));
  Loop(rain,driving&&settings.rainSound&&ContentTuning.Flag("display.sound.rain",true)?.58f*RainPolicy.Intensity:0);
  if(!driving||!settings.hornSound||!ContentTuning.Flag("display.sound.horn",true)){if(horn.isPlaying)horn.Stop();}
  else if(Input.GetKeyDown(KeyCode.Y)||Input.GetKeyDown(KeyCode.JoystickButton3)){
   horn.volume=.48f;horn.Play();
  }
 }
 void OnDestroy(){
  if(indicatorOn)Destroy(indicatorOn);
  if(indicatorOff)Destroy(indicatorOff);
  foreach(var source in new[]{engine,horn,rain})if(source&&source.clip)Destroy(source.clip);
 }
}
