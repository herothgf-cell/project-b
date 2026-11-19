using System;
using UnityEngine;

public enum DayLightPhase { Morning, Day, Sunset, Night }

public readonly struct DayLightSample {
 public readonly Color Ambient,SunColor,Sky;
 public readonly float SunIntensity,SunElevation,SunAzimuth,Glow;
 public DayLightSample(Color ambient,Color sunColor,Color sky,float intensity,float elevation,float azimuth,float glow){
  Ambient=ambient;SunColor=sunColor;Sky=sky;SunIntensity=intensity;SunElevation=elevation;SunAzimuth=azimuth;Glow=glow;
 }
 public static DayLightSample Blend(DayLightSample a,DayLightSample b,float t){
  return new DayLightSample(Color.Lerp(a.Ambient,b.Ambient,t),Color.Lerp(a.SunColor,b.SunColor,t),Color.Lerp(a.Sky,b.Sky,t),
   Mathf.Lerp(a.SunIntensity,b.SunIntensity,t),Mathf.Lerp(a.SunElevation,b.SunElevation,t),Mathf.Lerp(a.SunAzimuth,b.SunAzimuth,t),Mathf.Lerp(a.Glow,b.Glow,t));
 }
 public DayLightSample Rain(float wet){
  return new DayLightSample(Color.Lerp(Ambient,Ambient*.68f,wet),Color.Lerp(SunColor,new Color(.68f,.76f,.86f),wet*.5f),
   Color.Lerp(Sky,Sky*.69f+new Color(.07f,.08f,.105f,0),wet*.7f),
   SunIntensity*Mathf.Lerp(1,.68f,wet),SunElevation,SunAzimuth,Glow);
 }
}

// Owns only the local world's atmosphere and a bounded pool of headlamps.
[DisallowMultipleComponent,DefaultExecutionOrder(-20)]
public sealed class DayLightController : MonoBehaviour {
 public const int MaxLitVehicles=4;
 const float TrafficRange=55,ApplyInterval=.07f,TrafficInterval=.22f;
 const int TrafficCapacity=MaxLitVehicles-1;
 static readonly DayLightSample Morning=new DayLightSample(new Color(.64f,.59f,.52f),new Color(1,.84f,.65f),new Color(.58f,.68f,.81f),.92f,22,-95,0);
 static readonly DayLightSample DayStart=new DayLightSample(new Color(.65f,.69f,.75f),new Color(1,.98f,.94f),new Color(.58f,.73f,.87f),1.15f,43,-62,0);
 static readonly DayLightSample Midday=new DayLightSample(new Color(.65f,.69f,.75f),Color.white,new Color(.55f,.72f,.86f),1.15f,60,-35,0);
 static readonly DayLightSample SunsetStart=new DayLightSample(new Color(.64f,.65f,.69f),new Color(1,.9f,.76f),new Color(.58f,.66f,.78f),1.05f,35,14,0);
 static readonly DayLightSample Sunset=new DayLightSample(new Color(.54f,.45f,.41f),new Color(1,.67f,.44f),new Color(.65f,.43f,.39f),.82f,17,33,.62f);
 static readonly DayLightSample NightStart=new DayLightSample(new Color(.35f,.39f,.52f),new Color(.72f,.78f,1),new Color(.105f,.152f,.287f),.42f,12,52,1);
 static readonly DayLightSample Night=new DayLightSample(new Color(.28f,.33f,.46f),new Color(.64f,.73f,1),new Color(.068f,.105f,.224f),.34f,16,65,1);
 readonly bool forceNight=Array.IndexOf(Environment.GetCommandLineArgs(),"-dayLightNightProbe")>=0;
 readonly bool forceDay=Array.IndexOf(Environment.GetCommandLineArgs(),"-dayLightDayProbe")>=0;
 readonly VehicleCandidate[] candidates=new VehicleCandidate[TrafficCapacity];
 readonly DayLightHeadlamps[] trafficRigs=new DayLightHeadlamps[TrafficCapacity];
 readonly float[] candidateDistances=new float[TrafficCapacity];
 DriveSession session;
 Camera view,assistView;
 Color baseAmbient,baseBackground,baseAssist,baseSunColor,baseAsphaltColor;
 Quaternion baseSunRotation;
 float baseIntensity,baseAsphaltSmoothness,appliedWetness=-1;
 CameraClearFlags baseClear;
 Material baseSky,sky,asphalt;
 DayLightCity city;
 DayLightHeadlamps playerRig;
 float nextExposure,nextTraffic,weather;
 bool captured,probeLogged;
 public bool IsActive {get;private set;}
 public DayLightPhase CurrentPhase {get;private set;}
 public double ClockHour {get;private set;}=8;
 public int LitVehicleCount {get;private set;}
 public Light Sun {get;private set;}

 public static double HourForElapsed(double elapsed,double shiftSeconds){
  if(double.IsNaN(elapsed)||double.IsInfinity(elapsed)||double.IsNaN(shiftSeconds)||double.IsInfinity(shiftSeconds)||shiftSeconds<=0)return 8;
  return 8+12*Math.Max(0,elapsed)/shiftSeconds;
 }
 public static DayLightPhase PhaseAt(double hour){
  return hour>=19?DayLightPhase.Night:hour>=17?DayLightPhase.Sunset:hour>=10?DayLightPhase.Day:DayLightPhase.Morning;
 }
 static DayLightSample Between(double hour,double start,double end,DayLightSample a,DayLightSample b){
  return DayLightSample.Blend(a,b,Mathf.SmoothStep(0,1,(float)((hour-start)/(end-start))));
 }
 public static DayLightSample Sample(double hour,bool rain){
  if(double.IsNaN(hour))hour=8;
  var sample=hour<10?Between(hour,8,10,Morning,DayStart)
   :hour<13?Between(hour,10,13,DayStart,Midday)
   :hour<17?Between(hour,13,17,Midday,SunsetStart)
   :hour<18?Between(hour,17,18,SunsetStart,Sunset)
   :hour<19?Between(hour,18,19,Sunset,NightStart)
   :Between(hour,19,20,NightStart,Night);
  return rain?sample.Rain(1):sample;
 }

 public void Initialize(DriveSession owner,Light sun){
  session=owner;Sun=sun;view=session.drivingCamera.GetComponent<Camera>();
  baseAmbient=RenderSettings.ambientLight;baseSky=RenderSettings.skybox;
  baseBackground=view.backgroundColor;baseClear=view.clearFlags;
  baseSunRotation=Sun.transform.rotation;baseSunColor=Sun.color;baseIntensity=Sun.intensity;
  CaptureAsphalt();
  captured=true;
 }
 void CaptureAsphalt(){
  var world=session.world.transform;
  for(int i=0;i<world.childCount;i++){
   var child=world.GetChild(i);
   if(!child.name.StartsWith("도로 표면 ",StringComparison.Ordinal)||!child.TryGetComponent<MeshRenderer>(out var mesh))continue;
   var material=mesh.sharedMaterial;
   if(!material||!material.HasProperty("_Glossiness"))continue;
   // WorldBuilder creates one scene-owned Standard material for all road chunks.
   asphalt=material;baseAsphaltSmoothness=asphalt.GetFloat("_Glossiness");baseAsphaltColor=asphalt.color;
   return;
  }
 }
 void ApplyAsphalt(float wetness){
  if(!asphalt||appliedWetness==wetness||wetness!=0&&Mathf.Abs(appliedWetness-wetness)<.006f)return;
  appliedWetness=wetness;
  asphalt.SetFloat("_Glossiness",Mathf.Lerp(baseAsphaltSmoothness,.58f,wetness));
  asphalt.color=Color.Lerp(baseAsphaltColor,baseAsphaltColor*.72f,wetness);
 }
 bool Eligible {
  get {
   if(!captured||!session||!session.taxi||!session.world||session.IsSimulationBot||RoadMapRuntime.Trial||PracticeSession.Enabled||HdRouteTrial.Enabled)return false;
   if(forceNight||forceDay)return true;
   if(session.simulationMatch||session.business&&session.business.SimulationSession)return DrivingSettings.Current.pvpNightLighting;
   return session.business&&session.business.SupportsCareer;
  }
 }
 double ReadClock(){
  if(forceNight)return 20;
  if(forceDay)return 13;
  if(session.simulationMatch)return HourForElapsed(session.simulationMatch.Elapsed,PvpSimulationMode.DurationSeconds);
  var business=session.business;
  if(!business||business.ledger==null||business.preparing)return 8;
  return HourForElapsed(business.ledger.Elapsed,business.ledger.Settings.daySeconds);
 }
 void LateUpdate(){
  if(!Eligible){if(IsActive)RestoreDefaults();return;}
  double previousHour=ClockHour;
  ClockHour=ReadClock();CurrentPhase=PhaseAt(ClockHour);
  if(!IsActive)Activate();
  var now=Time.unscaledTime;
  float rain=RainPolicy.Enabled?Mathf.Clamp01(.5f+RainPolicy.Intensity*.5f):0;
  weather=Mathf.MoveTowards(weather,rain,Time.unscaledDeltaTime*.6f);
  if(now>=nextExposure||Math.Abs(previousHour-ClockHour)>.5){
   ApplySample(Sample(ClockHour,false).Rain(weather));ApplyAsphalt(weather);nextExposure=now+ApplyInterval;
  }
 }
 void Activate(){
  IsActive=true;
  weather=RainPolicy.Enabled?Mathf.Clamp01(.5f+RainPolicy.Intensity*.5f):0;
  if(session.assist&&!assistView){assistView=session.assist.GetComponent<Camera>();if(assistView)baseAssist=assistView.backgroundColor;}
  if(baseSky&&baseSky.HasProperty("_SkyTint")&&baseSky.HasProperty("_Exposure")){
   if(!sky)sky=new Material(baseSky){name="Local shift sky"};
   RenderSettings.skybox=sky;view.clearFlags=CameraClearFlags.Skybox;
  }else view.clearFlags=CameraClearFlags.SolidColor;
  nextExposure=nextTraffic=0;
 }
 void ApplySample(DayLightSample sample){
  Sun.color=sample.SunColor;Sun.intensity=sample.SunIntensity;
  Sun.transform.rotation=Quaternion.Euler(sample.SunElevation,sample.SunAzimuth,0);
  RenderSettings.ambientLight=sample.Ambient;
  view.backgroundColor=sample.Sky;
  if(assistView)assistView.backgroundColor=sample.Sky;
  if(sky){
   sky.SetColor("_SkyTint",sample.Sky);
   if(sky.HasProperty("_GroundColor"))sky.SetColor("_GroundColor",sample.Ambient*.72f);
   sky.SetFloat("_Exposure",Mathf.Lerp(1.05f,.48f,sample.Glow));
  }
  if(session.traffic)session.traffic.SetNightReadability(CurrentPhase==DayLightPhase.Night);
  if(sample.Glow<.035f&&city==null){LogProbe();return;}
  if(city==null)city=new DayLightCity(session.world.map,session.traffic,transform);
  city.UpdateGlow(sample.Glow);
  if(sample.Glow<.035f){TurnOffHeadlamps();return;}
  if(playerRig==null||!playerRig.Exists)playerRig=new DayLightHeadlamps(transform,city.LensMaterial,true);
  playerRig.Assign(session.taxi.transform,true,sample.Glow);
  if(Time.unscaledTime>=nextTraffic){
   RefreshTrafficHeadlamps(sample.Glow);
   nextTraffic=Time.unscaledTime+TrafficInterval;
  }else{
   for(int i=0;i<trafficRigs.Length;i++)if(trafficRigs[i]!=null)trafficRigs[i].Strength(sample.Glow);
  }
  LogProbe();
 }
 void LogProbe(){
  if(probeLogged||!(forceNight||forceDay))return;
  probeLogged=true;
  Debug.Log("DAYLIGHT_READY phase="+CurrentPhase+" lights="+LitVehicleCount+" graphics="+SystemInfo.graphicsDeviceType+" streetLamps="+(city?.LampCount??0)+" litWindows="+(city?.WindowCount??0));
 }
 struct VehicleCandidate {public Transform transform;public bool taxi;}
 void Consider(Transform candidate,bool taxi,Vector3 centre){
  if(!candidate)return;
  float distance=(candidate.position-centre).sqrMagnitude;
  for(int i=0;i<TrafficCapacity;i++){
   if(distance>=candidateDistances[i])continue;
   for(int j=TrafficCapacity-1;j>i;j--){candidateDistances[j]=candidateDistances[j-1];candidates[j]=candidates[j-1];}
   candidateDistances[i]=distance;candidates[i]=new VehicleCandidate{transform=candidate,taxi=taxi};break;
  }
 }
 void RefreshTrafficHeadlamps(float glow){
  LitVehicleCount=1;
  Vector3 centre=session.taxi.transform.position;
  for(int i=0;i<TrafficCapacity;i++){candidates[i]=default;candidateDistances[i]=TrafficRange*TrafficRange;}
  if(session.traffic){
   var cars=session.traffic.cars;
   for(int i=0;i<cars.Count;i++){var car=cars[i];if(car&&car.Visible)Consider(car.transform,false,centre);}
  }
  if(session.simulationMatch){
   var drivers=session.simulationMatch.Drivers;
   for(int i=0;i<drivers.Count;i++){var candidate=drivers[i];if(candidate&&candidate!=session&&candidate.taxi)Consider(candidate.taxi.transform,true,centre);}
  }
  for(int i=0;i<TrafficCapacity;i++){
   var current=candidates[i];
   if(!current.transform){trafficRigs[i]?.Assign(null,false,0);continue;}
   if(trafficRigs[i]==null||!trafficRigs[i].Exists)trafficRigs[i]=new DayLightHeadlamps(transform,city.LensMaterial,false);
   trafficRigs[i].Assign(current.transform,current.taxi,glow);LitVehicleCount++;
  }
 }
 void TurnOffHeadlamps(){
  playerRig?.Assign(null,true,0);
  for(int i=0;i<TrafficCapacity;i++)trafficRigs[i]?.Assign(null,false,0);
  LitVehicleCount=0;nextTraffic=0;
 }
 void RestoreDefaults(){
  IsActive=false;TurnOffHeadlamps();city?.UpdateGlow(0);
  RenderSettings.ambientLight=baseAmbient;
  if(sky&&RenderSettings.skybox==sky)RenderSettings.skybox=baseSky;
  if(Sun){Sun.transform.rotation=baseSunRotation;Sun.color=baseSunColor;Sun.intensity=baseIntensity;}
  if(view){view.backgroundColor=baseBackground;view.clearFlags=baseClear;}
  if(assistView)assistView.backgroundColor=baseAssist;
  ApplyAsphalt(0);
  if(session&&session.traffic)session.traffic.SetNightReadability(false);
 }
 void OnDisable(){if(IsActive)RestoreDefaults();}
 void OnDestroy(){
  if(IsActive)RestoreDefaults();
  playerRig?.Dispose();
  for(int i=0;i<TrafficCapacity;i++)trafficRigs[i]?.Dispose();
  city?.Dispose();
  if(sky)Destroy(sky);
 }
}
