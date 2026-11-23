using UnityEngine;

// The local driver hears a mono interior mix. Engine revs (through a modelled automatic gearbox)
// and wheel speed drive separate recorded layers.
public sealed class VehicleAudio : MonoBehaviour {
 DriveSession session;
 AudioSource engine,road,indicator,horn,rain;
 AudioClip indicatorOn,indicatorOff;
 bool lampWasLit;
 int gear;float rpm=IdleRpm,load;

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
  source.volume=0;
  source.loop=loop;
  source.spatialBlend=0;
  source.clip=LoadClip(sound);
  return source;
 }
 public static AudioClip LoadClip(string sound){
  return Resources.Load<AudioClip>("Audio/Taxi/"+sound);
 }
 static void Loop(AudioSource source,float volume){
  source.volume=volume;
  if(volume<=0){if(source.isPlaying)source.Stop();return;}
  if(!source.isPlaying)source.Play();
 }
 // Speed is in metres per second. A six-speed automatic keeps the recorded loop inside a natural
 // pitch band: revs climb within a gear and drop on each upshift instead of rising like a siren.
 static readonly float[] upshiftKmh={18,32,48,62,75};
 static readonly float[] rpmPerKmh={105,60,42,33,28,24};
 public const float IdleRpm=800;
 public static int Gear(float speed,float power,int current){
  float kmh=speed*3.6f,scale=1+.6f*Mathf.Clamp01(power);
  int gear=Mathf.Clamp(current,0,rpmPerKmh.Length-1);
  while(gear<upshiftKmh.Length&&kmh>upshiftKmh[gear]*scale)gear++;
  while(gear>0&&kmh<upshiftKmh[gear-1]*scale*.72f)gear--;
  return gear;
 }
 public static float Rpm(float speed,float power,int gear){
  float kmh=speed*3.6f;power=Mathf.Clamp01(power);
  float rpm=kmh*rpmPerKmh[Mathf.Clamp(gear,0,rpmPerKmh.Length-1)]+260*power;
  // Torque-converter flare: pulling away revs the engine before the car catches up.
  if(kmh<20)rpm=Mathf.Max(rpm,IdleRpm+900*power*(1-kmh/20));
  return Mathf.Clamp(Mathf.Max(IdleRpm,rpm),IdleRpm,4200);
 }
 public static void EngineSound(float rpm,float power,out float level,out float pitch){
  float rev=Mathf.Clamp01((rpm-IdleRpm)/2400);
  level=.20f+.24f*rev+.25f*Mathf.Clamp01(power);
  pitch=Mathf.Clamp(.82f+.68f*rev,.82f,1.55f);
 }
 // Steady-state mix for checks: the gear an automatic would hold at this speed and load.
 public static void EngineMix(float speed,float power,out float level,out float pitch){
  EngineSound(Rpm(speed,power,Gear(speed,power,0)),power,out level,out pitch);
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
  float dt=Time.unscaledDeltaTime;
  // Load and revs carry inertia so lifting off or braking eases the note down instead of snapping it.
  load=Mathf.Lerp(load,power,1-Mathf.Exp(-dt/.12f));
  gear=Gear(speed,load,gear);
  float targetRpm=Rpm(speed,load,gear);
  rpm=Mathf.Lerp(rpm,targetRpm,1-Mathf.Exp(-dt/(targetRpm<rpm?.16f:.22f)));
  EngineSound(rpm,load,out float level,out float pitch);
  float blend=1-Mathf.Exp(-dt/.08f);
  bool driveSound=driving&&settings.engineSound&&ContentTuning.Flag("display.sound.engine",true);
  MotionLoop(engine,driveSound,level,pitch,blend);
  // Tyre and wind noise grows with speed and dominates the cabin above town speeds.
  MotionLoop(road,driveSound,.62f*speed*speed/(speed*speed+90),1,blend);
  UpdateIndicator(driving&&settings.indicatorSound&&ContentTuning.Flag("display.sound.indicator",true));
  Loop(rain,driving&&settings.rainSound&&ContentTuning.Flag("display.sound.rain",true)?.58f*RainPolicy.Intensity:0);
  if(!driving||!settings.hornSound||!ContentTuning.Flag("display.sound.horn",true)){if(horn.isPlaying)horn.Stop();}
  else if(Input.GetKeyDown(KeyCode.Y)||Input.GetKeyDown(KeyCode.JoystickButton3)){
   horn.volume=.48f;horn.Play();
  }
 }
}
