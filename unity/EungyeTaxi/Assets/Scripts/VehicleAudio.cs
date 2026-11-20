using UnityEngine;

// Only the local driving session creates these sources. Clips are mono cabin recordings
// synthesized once per session, with independent playback and preference controls.
public sealed class VehicleAudio : MonoBehaviour {
 const int SampleRate=22050;
 DriveSession session;
 AudioSource engine,brake,horn,rain;

 public void Initialize(DriveSession owner){
  session=owner;
  engine=Source("Engine",true);
  brake=Source("Brake",true);
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
  int length=sound=="Horn"?SampleRate/2:SampleRate*2;
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
    case "Brake":
     samples[i]=.42f*rumble+.14f*noise+.14f*Mathf.Sin(tau*880*t+.8f*Mathf.Sin(tau*11*t));
     break;
    case "Rain":
     roof+=.33f*(noise-roof);
     droplet*=.984f;
     if(random.NextDouble()<.002)droplet=(float)random.NextDouble()*.48f;
     samples[i]=.6f*roof+droplet*Mathf.Sin(tau*1150*t);
     break;
    default:
     float phase=tau*39*t+.08f*Mathf.Sin(tau*6*t);
     samples[i]=.36f*Mathf.Sin(phase)+.24f*Mathf.Sin(phase*2)+.12f*Mathf.Sin(phase*4)+.17f*rumble;
     break;
   }
   // Taper the seam of finite noise loops.
   if(sound!="Horn")samples[i]*=Mathf.Clamp01(Mathf.Min(i,length-1-i)/64f);
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
 void LateUpdate(){
  if(!session||!session.StartupReady)return;
  var taxi=session.taxi;
  var settings=DrivingSettings.Current;
  bool driving=Time.timeScale>0&&!session.paused&&!session.FullMapOpen&&!session.SettingsOpen&&
   !session.PracticeMenuOpen&&!session.ChoiceOpen&&!session.services.open&&!session.services.PostTripOpen&&
   !session.business.preparing&&!session.business.ShowingSummary&&!PvpNativeLan.UiBlocked(session);
  float speed=Mathf.Abs(taxi.Speed);
  float power=Mathf.Clamp01(Mathf.Abs(taxi.wheels[2].motorTorque)/1100f);
  float braking=Mathf.Clamp01(taxi.wheels[0].brakeTorque/3500f);
  engine.pitch=.83f+.85f*Mathf.Clamp01(speed/22)+.32f*power;
  Loop(engine,driving&&settings.engineSound&&ContentTuning.Flag("display.sound.engine",true)?.10f+.10f*power+.09f*Mathf.Clamp01(speed/20):0);
  brake.pitch=.85f+.25f*Mathf.Clamp01(speed/12);
  Loop(brake,driving&&settings.brakeSound&&ContentTuning.Flag("display.sound.brake",true)?.38f*braking*Mathf.Clamp01(speed/3):0);
  Loop(rain,driving&&settings.rainSound&&ContentTuning.Flag("display.sound.rain",true)?.58f*RainPolicy.Intensity:0);
  if(!driving||!settings.hornSound||!ContentTuning.Flag("display.sound.horn",true)){if(horn.isPlaying)horn.Stop();}
  else if(Input.GetKeyDown(KeyCode.Y)||Input.GetKeyDown(KeyCode.JoystickButton3)){
   horn.volume=.48f;horn.Play();
  }
 }
 void OnDestroy(){
  foreach(var source in new[]{engine,brake,horn,rain})if(source&&source.clip)Destroy(source.clip);
 }
}
