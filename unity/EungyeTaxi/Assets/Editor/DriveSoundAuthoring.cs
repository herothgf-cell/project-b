using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// 2026-11-24 drive sound rework (user-selected CC0 recordings). Source MP3 previews are copied into
// Assets/TempAudioSrc only while this tool runs; the cut, filtered loops are written to Resources/Audio/Taxi.
//   Analyse: logs level and brightness over time so the excerpts can be chosen.
//   Build:   writes Road.wav (highway cabin), RoadSlow.wav (town cabin), WindowWind.wav, HardBrake.wav.
public static class DriveSoundAuthoring {
 const string Temp="Assets/TempAudioSrc",Out="Assets/Resources/Audio/Taxi";
 const int Rate=44100;
 static float[] Load(string file,out int rate){
  string path=Temp+"/"+file;
  var importer=(AudioImporter)AssetImporter.GetAtPath(path);
  if(importer==null)throw new InvalidOperationException("missing source "+path);
  importer.forceToMono=true;
  var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.preloadAudioData=true;settings.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=settings;
  importer.SaveAndReimport();
  var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
  if(!clip)throw new InvalidOperationException("no clip "+path);
  clip.LoadAudioData();
  var data=new float[clip.samples*clip.channels];
  if(!clip.GetData(data,0))throw new InvalidOperationException("cannot read "+path);
  rate=clip.frequency;
  if(clip.channels>1){var mono=new float[clip.samples];for(int i=0;i<mono.Length;i++){float sum=0;for(int c=0;c<clip.channels;c++)sum+=data[i*clip.channels+c];mono[i]=sum/clip.channels;}data=mono;}
  return data;
 }
 static double Db(double rms){return 20*Math.Log10(Math.Max(rms,1e-9));}
 static void Profile(string label,float[] x,int rate,float window){
  var text=new StringBuilder("DRIVE_SOUND_PROFILE "+label+" rate="+rate+" seconds="+(x.Length/(float)rate).ToString("F1")+"\n");
  int n=(int)(window*rate);
  for(int start=0;start+n<=x.Length;start+=n){
   double energy=0;int crossings=0;double high=0;float previous=0,lp=0;
   for(int i=start;i<start+n;i++){float v=x[i];energy+=v*v;if(v>=0!=previous>=0)crossings++;previous=v;lp+=.25f*(v-lp);float h=v-lp;high+=h*h;}
   text.Append((start/(float)rate).ToString("F2")).Append("s rms=").Append(Db(Math.Sqrt(energy/n)).ToString("F1")).Append(" high=").Append(Db(Math.Sqrt(high/n)).ToString("F1")).Append(" zc=").Append((crossings/(2f*window)).ToString("F0")).Append('\n');
  }
  Debug.Log(text.ToString());
 }
 public static void Analyse(){
  AssetDatabase.Refresh();
  int rate;
  var highway=Load("521766_3562198.mp3",out rate);Profile("highway",highway,rate,2);
  var journey=Load("570461_4437257.mp3",out rate);Profile("journey",journey,rate,10);
  var brake=Load("481706_2524442.mp3",out rate);Profile("brake",brake,rate,.25f);
  Debug.Log("DRIVE_SOUND_ANALYSE_OK");
 }

 // ---- processing helpers ------------------------------------------------
 static float[] Cut(float[] x,int rate,float from,float seconds){
  int a=(int)(from*rate),n=(int)(seconds*rate);var y=new float[n];Array.Copy(x,a,y,0,n);
  if(rate==Rate)return y;
  int m=(int)(n*(double)Rate/rate);var z=new float[m];
  for(int i=0;i<m;i++){double p=i*(double)rate/Rate;int k=(int)p;float t=(float)(p-k);z[i]=k+1<n?Mathf.Lerp(y[k],y[k+1],t):y[n-1];}
  return z;
 }
 // RBJ biquad, run forward. kind: 0 low-pass, 1 high-pass, 2 high-shelf (gain in dB).
 static void Biquad(float[] x,int kind,float hz,float q,float gainDb=0){
  double w=2*Math.PI*hz/Rate,cs=Math.Cos(w),sn=Math.Sin(w),alpha=sn/(2*q),b0,b1,b2,a0,a1,a2;
  if(kind==0){b0=(1-cs)/2;b1=1-cs;b2=(1-cs)/2;a0=1+alpha;a1=-2*cs;a2=1-alpha;}
  else if(kind==1){b0=(1+cs)/2;b1=-(1+cs);b2=(1+cs)/2;a0=1+alpha;a1=-2*cs;a2=1-alpha;}
  else{double A=Math.Pow(10,gainDb/40),beta=2*Math.Sqrt(A)*alpha;b0=A*((A+1)+(A-1)*cs+beta);b1=-2*A*((A-1)+(A+1)*cs);b2=A*((A+1)+(A-1)*cs-beta);a0=(A+1)-(A-1)*cs+beta;a1=2*((A-1)-(A+1)*cs);a2=(A+1)-(A-1)*cs-beta;}
  double x1=0,x2=0,y1=0,y2=0;
  for(int i=0;i<x.Length;i++){double v=x[i],y=(b0*v+b1*x1+b2*x2-a1*y1-a2*y2)/a0;x2=x1;x1=v;y2=y1;y1=y;x[i]=(float)y;}
 }
 // Slow gain riding: keeps a loop from swelling and fading (window in seconds, limited to +-range dB).
 static void Level(float[] x,float window,float rangeDb){
  int n=(int)(window*Rate);var power=new double[x.Length+1];
  for(int i=0;i<x.Length;i++)power[i+1]=power[i]+x[i]*(double)x[i];
  double overall=Math.Sqrt(power[x.Length]/x.Length);float limit=Mathf.Pow(10,rangeDb/20);
  for(int i=0;i<x.Length;i++){int a=Math.Max(0,i-n/2),b=Math.Min(x.Length,i+n/2);double local=Math.Sqrt((power[b]-power[a])/(b-a));x[i]*=Mathf.Clamp((float)(overall/Math.Max(local,1e-6)),1/limit,limit);}
 }
 // Equal-power crossfade of the tail into the head so the clip loops without a click.
 static float[] Loop(float[] x,float fadeSeconds){
  int f=(int)(fadeSeconds*Rate),n=x.Length-f;var y=new float[n];
  for(int i=0;i<n;i++)y[i]=x[i];
  for(int i=0;i<f;i++){float t=(i+.5f)/f;y[i]=x[i]*Mathf.Sin(t*Mathf.PI/2)+x[n+i]*Mathf.Cos(t*Mathf.PI/2);}
  return y;
 }
 static void Normalise(float[] x,float rmsDb,float peakLimit=.95f){
  double energy=0;foreach(float v in x)energy+=v*v;float gain=Mathf.Pow(10,rmsDb/20)/(float)Math.Max(Math.Sqrt(energy/x.Length),1e-6);
  float peak=0;foreach(float v in x)peak=Mathf.Max(peak,Mathf.Abs(v)*gain);
  if(peak>peakLimit)gain*=peakLimit/peak;
  for(int i=0;i<x.Length;i++)x[i]*=gain;
 }
 static void Fade(float[] x,float inSeconds,float outSeconds){
  int a=(int)(inSeconds*Rate),b=(int)(outSeconds*Rate);
  for(int i=0;i<a&&i<x.Length;i++)x[i]*=i/(float)a;
  for(int i=0;i<b&&i<x.Length;i++)x[x.Length-1-i]*=i/(float)b;
 }
 static void Write(string name,float[] x){
  WriteFile(Out+"/"+name+".wav",x);
  double energy=0;float peak=0;foreach(float v in x){energy+=v*v;peak=Mathf.Max(peak,Mathf.Abs(v));}
  Debug.Log("DRIVE_SOUND_WRITTEN "+name+" seconds="+(x.Length/(float)Rate).ToString("F2")+" rms="+Db(Math.Sqrt(energy/x.Length)).ToString("F1")+" peak="+peak.ToString("F2"));
 }
 static void WriteFile(string path,float[] x){
  using(var stream=new FileStream(path,FileMode.Create))using(var w=new BinaryWriter(stream)){
   int bytes=x.Length*2;
   w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+bytes);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(Rate);w.Write(Rate*2);w.Write((short)2);w.Write((short)16);
   w.Write(Encoding.ASCII.GetBytes("data"));w.Write(bytes);
   foreach(float v in x)w.Write((short)Mathf.RoundToInt(Mathf.Clamp(v,-1,1)*32767));
  }
 }
 static float Arg(string name,float fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?float.Parse(args[i+1],System.Globalization.CultureInfo.InvariantCulture):fallback;}
 // A 32 s listening sample rendered with the game's own mix formulas: pull away to 80 km/h, cruise,
 // open the window, close it, brake hard to a stop.
 public static void Preview(){
  AssetDatabase.Refresh();
  string[] names={"Engine","Road","RoadSlow","WindowWind","HardBrake"};var clips=new float[names.Length][];var rates=new int[names.Length];
  for(int i=0;i<names.Length;i++){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Out+"/"+names[i]+".wav");clip.LoadAudioData();clips[i]=new float[clip.samples];clip.GetData(clips[i],0);rates[i]=clip.frequency;}
  int total=32*Rate;var mix=new float[total];var phase=new double[names.Length];int gear=0;float rpm=VehicleAudio.IdleRpm,load=0,brakeAt=-1;
  float[] volume=new float[names.Length];float dt=1f/Rate;
  for(int i=0;i<total;i++){
   float t=i*dt;
   float speed=t<2?0:t<14?22.2f*Mathf.SmoothStep(0,1,(t-2)/12):t<26?22.2f:t<28.6f?22.2f*(1-(t-26)/2.6f):0;
   float power=t>=2&&t<14?.55f:t<26&&t>=14?.18f:0;bool windowOpen=t>=18&&t<23;
   if(i%64==0){
    float step=64*dt;load=Mathf.Lerp(load,power,1-Mathf.Exp(-step/.12f));gear=VehicleAudio.Gear(speed,load,gear);float target=VehicleAudio.Rpm(speed,load,gear);rpm=Mathf.Lerp(rpm,target,1-Mathf.Exp(-step/(target<rpm?.16f:.22f)));
   }
   VehicleAudio.EngineSound(rpm,load,out float level,out float pitch);
   float cabin=DriveFeelAudio.CabinLevel(speed),fast=DriveFeelAudio.HighwayShare(speed);
   float[] target2={level+DriveFeelAudio.LoadLevel(load,Mathf.Clamp01((rpm-VehicleAudio.IdleRpm)/2400)),cabin*(.35f+.65f*fast),cabin*(1-fast)*.85f,windowOpen?DriveFeelAudio.WindowLevel(speed):0,0};
   float sample=0;
   for(int c=0;c<4;c++){
    volume[c]+=(target2[c]-volume[c])*(1-Mathf.Exp(-dt/.08f));
    double rate=rates[c]/(double)Rate*(c==0?pitch:1);phase[c]+=rate;int n=clips[c].Length;if(phase[c]>=n)phase[c]-=n;
    int k=(int)phase[c];float f=(float)(phase[c]-k);sample+=volume[c]*Mathf.Lerp(clips[c][k],clips[c][(k+1)%n],f);
   }
   if(brakeAt<0&&t>=26)brakeAt=t;
   if(brakeAt>=0){double at=(t-brakeAt)*rates[4];int k=(int)at;if(k+1<clips[4].Length)sample+=DriveFeelAudio.HardBrakeLevel(22.2f)*Mathf.Lerp(clips[4][k],clips[4][k+1],(float)(at-k));}
   mix[i]=sample;
  }
  float peak=0;foreach(float v in mix)peak=Mathf.Max(peak,Mathf.Abs(v));
  if(peak>.95f)for(int i=0;i<total;i++)mix[i]*=.95f/peak;
  string keep=Out;
  string path=Path.GetFullPath("../../docs/content-design/audio/taxi-drive-cabin-2026-11-24.wav");
  WriteFile(path,mix);
  Debug.Log("DRIVE_SOUND_PREVIEW_OK peak="+peak.ToString("F2")+" "+path);
 }
 public static void Build(){
  AssetDatabase.Refresh();
  int rate;
  // Highway cabin: a steady stretch of "Highway drive interior".
  var highwaySource=Load("521766_3562198.mp3",out rate);int highwayRate=rate;
  var highway=Cut(highwaySource,rate,Arg("-highwayFrom",8),14);
  Biquad(highway,1,35,.7f);Level(highway,1.2f,4);highway=Loop(highway,2);Normalise(highway,-17);
  Write("Road",highway);
  // Town cabin: a slow stretch of "Interior of a car driving a highway".
  var journey=Load("570461_4437257.mp3",out rate);
  var slow=Cut(journey,rate,Arg("-slowFrom",510),14);
  Biquad(slow,1,35,.7f);Level(slow,1.2f,4);slow=Loop(slow,2);Normalise(slow,-19);
  Write("RoadSlow",slow);
  // Window down: the same two recordings, a fast stretch of each, with the rumble removed and the hiss
  // lifted so it reads as outside air rushing past an open window.
  var airA=Cut(highwaySource,highwayRate,Arg("-windFromA",24),12);
  var airB=Cut(journey,rate,Arg("-windFromB",664),12);
  var wind=new float[Math.Min(airA.Length,airB.Length)];
  Normalise(airA,-18);Normalise(airB,-18);
  for(int i=0;i<wind.Length;i++)wind[i]=.6f*airA[i]+.6f*airB[i];
  Biquad(wind,1,260,.7f);Biquad(wind,2,2200,.7f,9);Biquad(wind,0,9000,.7f);Level(wind,.8f,5);
  // A slow flutter, like gusts past the window edge.
  for(int i=0;i<wind.Length;i++){float t=i/(float)Rate;wind[i]*=1+.18f*Mathf.Sin(2*Mathf.PI*.37f*t)+.10f*Mathf.Sin(2*Mathf.PI*1.3f*t+1);}
  wind=Loop(wind,2);Normalise(wind,-17);
  Write("WindowWind",wind);
  // Hard braking: the squeal from "R04-12 Car Comes to a Stop". The recording is from outside the car,
  // so the top end is rolled off and a little low body is kept to make it sound heard through the cabin.
  var stop=Load("481706_2524442.mp3",out rate);
  var squeal=Cut(stop,rate,Arg("-brakeFrom",8.7f),Arg("-brakeSeconds",2.0f));
  Biquad(squeal,1,500,.7f);Biquad(squeal,0,3200,.7f);Biquad(squeal,0,4200,.7f);Fade(squeal,.04f,.5f);Normalise(squeal,-20,.8f);
  Write("HardBrake",squeal);
  AssetDatabase.Refresh();
  Debug.Log("DRIVE_SOUND_BUILD_OK");
 }
}
