using System;using System.Collections.Generic;using UnityEngine;
[Serializable]public sealed class PvpNativePerfView{public string name;public int count;public double averageMs,p95Ms,maxMs;}
public static class PvpNativePerf{
 static readonly Dictionary<string,List<double>> samples=new Dictionary<string,List<double>>();static double last,started;static readonly bool enabled=Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeLanCheck")>=0;
 public static bool Steady,Moving;
 struct MotionRow{public double now,source,playback,dt,travel,cameraTravel;public float speed;public Vector3 position;}
 static readonly List<MotionRow> motion=new List<MotionRow>(enabled?4096:0);
 public static int MovingFrames,FrozenFrames;public static float MovingMaxSpeed;public static double MovingSeconds,MovingMetres,MaxFreezeMs;static double freezeSeconds;public static readonly bool LegacyRouting=enabled&&Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeLegacyRouting")>=0;
 public static void Count(string name){if(enabled)Add(name,1);}
 public static long Start(){return System.Diagnostics.Stopwatch.GetTimestamp();}
 public static void End(string name,long start){if(!enabled)return;double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;Add(name,ms);if(Steady&&name=="own")Add("own-steady",ms);}
 public static void Sample(string name,double ms){if(enabled)Add(name,ms);}
 public static void Motion(double now,double source,double playback,Vector3 position,float speed,Vector3 camera){
  if(!enabled||!Moving)return;
  double dt=motion.Count==0?0:now-motion[motion.Count-1].now;double travel=motion.Count==0?0:Vector3.Distance(position,motion[motion.Count-1].position);
  if(dt>0&&dt<.5&&speed>2){MovingFrames++;MovingMaxSpeed=Mathf.Max(MovingMaxSpeed,speed);MovingSeconds+=dt;MovingMetres+=travel;Add("moving-frame-ms",dt*1000);if(travel<.001){FrozenFrames++;freezeSeconds+=dt;MaxFreezeMs=Math.Max(MaxFreezeMs,freezeSeconds*1000);}else freezeSeconds=0;}
  if(motion.Count<20000)motion.Add(new MotionRow{now=now,source=source,playback=playback,position=position,speed=speed,dt=dt,travel=travel,cameraTravel=Vector3.Distance(camera,previousCamera)});previousCamera=camera;
 }
 static Vector3 previousCamera;
 public static void WriteMotion(string path){if(!enabled||motion.Count==0)return;var b=new System.Text.StringBuilder("realtime,source,playback,dt,x,y,z,speed,travel,cameraTravel,headroomMs\n");foreach(var r in motion)b.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0:F6},{1:F6},{2:F6},{3:F6},{4:F4},{5:F4},{6:F4},{7:F4},{8:F5},{9:F5},{10:F3}\n",r.now,r.source,r.playback,r.dt,r.position.x,r.position.y,r.position.z,r.speed,r.travel,r.cameraTravel,(r.source+.1-r.playback)*1000);System.IO.File.WriteAllText(path,b.ToString());}
 static void Add(string name,double ms){List<double> a;if(!samples.TryGetValue(name,out a)){a=new List<double>();samples[name]=a;}if(a.Count<20000)a.Add(ms);}
 public static void Frame(){if(!enabled)return;double now=Time.realtimeSinceStartupAsDouble;if(started==0)started=now;if(last>0&&now-started>2&&now-last<1)Add("frame",(now-last)*1000);last=now;}
 public static PvpNativePerfView[] Capture(){var result=new List<PvpNativePerfView>();foreach(var p in samples){var a=p.Value.ToArray();Array.Sort(a);double total=0;foreach(var x in a)total+=x;if(a.Length>0)result.Add(new PvpNativePerfView{name=p.Key,count=a.Length,averageMs=total/a.Length,p95Ms=a[Math.Min(a.Length-1,(int)(a.Length*.95))],maxMs=a[a.Length-1]});}return result.ToArray();}
}
