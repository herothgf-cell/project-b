using UnityEngine;

// The local driver hears a mono interior mix. Engine revs (through a modelled automatic gearbox)
// and wheel speed drive separate recorded layers.
public sealed class VehicleAudio : MonoBehaviour {
 DriveSession session;
 AudioSource engine,road,indicator,horn,rain,tyre,wind;
 float lastSpeed,contactUntil;int lastCollisions=-1;
 AudioClip indicatorOn,indicatorOff;
 bool lampWasLit;
 int gear;float rpm=IdleRpm,load;

 void OnDisable(){XInputRumble.Stop();}
 void OnApplicationFocus(bool focus){if(!focus)XInputRumble.Stop();}
 public void Initialize(DriveSession owner){
  session=owner;
  engine=Source("Engine",true);
  road=Source("Road",true);
  indicator=Source("IndicatorOn",false);
  indicatorOn=indicator.clip;
  indicatorOff=LoadClip("IndicatorOff");
  horn=Source("Horn",false);
  rain=Source("Rain",true);
  // 08 chapter 1 extra layers, re-pitched from the existing recordings (no new audio files).
  // On a child object so the primary engine/road sources stay unique on the session (checks look them up).
  var extra=new GameObject("Extra drive layers");extra.transform.SetParent(transform,false);
  tyre=ExtraSource(extra,"Road");wind=ExtraSource(extra,"Road");
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
 static AudioSource ExtraSource(GameObject host,string sound){var source=host.AddComponent<AudioSource>();source.playOnAwake=false;source.volume=0;source.loop=true;source.spatialBlend=0;source.clip=LoadClip(sound);return source;}
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
  float kmh=speed*3.6f,scale=1+.3f*Mathf.Clamp01(power);
  int gear=Mathf.Clamp(current,0,rpmPerKmh.Length-1);
  while(gear<upshiftKmh.Length&&kmh>upshiftKmh[gear]*scale)gear++;
  while(gear>0&&kmh<upshiftKmh[gear-1]*scale*.72f)gear--;
  return gear;
 }
 public static float Rpm(float speed,float power,int gear){
  float kmh=speed*3.6f;power=Mathf.Clamp01(power);
  float rpm=kmh*rpmPerKmh[Mathf.Clamp(gear,0,rpmPerKmh.Length-1)]+150*power;
  // Torque-converter flare: pulling away revs the engine before the car catches up.
  if(kmh<20)rpm=Mathf.Max(rpm,IdleRpm+500*power*(1-kmh/20));
  return Mathf.Clamp(Mathf.Max(IdleRpm,rpm),IdleRpm,3200);
 }
 public static void EngineSound(float rpm,float power,out float level,out float pitch){
  float rev=Mathf.Clamp01((rpm-IdleRpm)/2400);
  // An ordinary taxi: a muffled engine that stays under the road noise and never screams.
  level=.12f+.12f*rev+.14f*Mathf.Clamp01(power);
  pitch=Mathf.Clamp(.88f+.40f*rev,.88f,1.28f);
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
  // Extra layers and pad vibration: Eungye single only, scaled by the feel tab, ducked under speech.
  bool single=!session.simulationMatch&&!PvpSimulationMode.Active;
  float extra=driveSound&&single?settings.extraSound*(DriveFeelAudio.SpeechOnScreen(session)?DriveFeelAudio.Duck:1):0;
  float rev=Mathf.Clamp01((rpm-IdleRpm)/2400),slip=0;
  // 2026-11-24 test feedback ("another odd sound in the drive sound", "odd brake sound"):
  // - Engine load used to be a second, lower-pitched copy of the engine loop, which read as a second engine
  //   droning underneath. It is now a gain on the one engine voice.
  // - The tyre layer measured raw wheel slip, so ordinary braking and cornering hissed. It now only hears
  //   slip beyond each tyre's grip peak (a real skid or locked wheel).
  MotionLoop(engine,driveSound,level+DriveFeelAudio.LoadLevel(load,rev)*extra,pitch,blend);
  // Tyre and wind noise grows with speed and dominates the cabin above town speeds.
  MotionLoop(road,driveSound,.62f*speed*speed/(speed*speed+90),1,blend);
  foreach(var wheel in taxi.wheels){if(wheel&&wheel.GetGroundHit(out WheelHit hit))slip=Mathf.Max(slip,DriveFeelAudio.SkidSlip(hit.sidewaysSlip,wheel.sidewaysFriction.extremumSlip,hit.forwardSlip,wheel.forwardFriction.extremumSlip));}
  MotionLoop(tyre,extra>0,DriveFeelAudio.TyreLevel(slip,speed)*extra,1.9f,blend);
  MotionLoop(wind,extra>0,DriveFeelAudio.WindLevel(speed)*extra,DriveFeelAudio.WindPitch(speed),blend);
  float decel=dt>0?Mathf.Max(0,(lastSpeed-speed)/Mathf.Max(dt,.001f)):0;lastSpeed=speed;
  int collisions=session.manners?session.manners.collisionEvents:0;if(lastCollisions>=0&&collisions>lastCollisions)contactUntil=Time.unscaledTime+DriveFeelAudio.ContactSeconds;lastCollisions=collisions;
  if(driving&&single&&settings.rumble>0&&Input.GetJoystickNames().Length>0){DriveFeelAudio.Rumble(speed,decel,Time.unscaledTime<contactUntil,out float low,out float high);XInputRumble.Set(low*settings.rumble,high*settings.rumble);}
  else XInputRumble.Stop();
  UpdateIndicator(driving&&settings.indicatorSound&&ContentTuning.Flag("display.sound.indicator",true));
  Loop(rain,driving&&settings.rainSound&&ContentTuning.Flag("display.sound.rain",true)?.58f*RainPolicy.Intensity:0);
  if(!driving||!settings.hornSound||!ContentTuning.Flag("display.sound.horn",true)){if(horn.isPlaying)horn.Stop();}
  else if(Input.GetKeyDown(KeyCode.Y)||Input.GetKeyDown(KeyCode.JoystickButton3)){
   horn.volume=.48f;horn.Play();
  }
 }
}
