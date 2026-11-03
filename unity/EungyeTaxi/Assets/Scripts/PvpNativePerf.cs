using System;using System.Collections.Generic;using UnityEngine;
[Serializable]public sealed class PvpNativePerfView{public string name;public int count;public double averageMs,p95Ms,maxMs;}
public static class PvpNativePerf{
 static readonly Dictionary<string,List<double>> samples=new Dictionary<string,List<double>>();static double last,started;static readonly bool enabled=Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeLanCheck")>=0;
 public static bool Steady;public static readonly bool LegacyRouting=enabled&&Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeLegacyRouting")>=0;
 public static void Count(string name){if(enabled)Add(name,1);}
 public static long Start(){return System.Diagnostics.Stopwatch.GetTimestamp();}
 public static void End(string name,long start){if(!enabled)return;double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;Add(name,ms);if(Steady&&name=="own")Add("own-steady",ms);}
 static void Add(string name,double ms){List<double> a;if(!samples.TryGetValue(name,out a)){a=new List<double>();samples[name]=a;}if(a.Count<20000)a.Add(ms);}
 public static void Frame(){if(!enabled)return;double now=Time.realtimeSinceStartupAsDouble;if(started==0)started=now;if(last>0&&now-started>2&&now-last<1)Add("frame",(now-last)*1000);last=now;}
 public static PvpNativePerfView[] Capture(){var result=new List<PvpNativePerfView>();foreach(var p in samples){var a=p.Value.ToArray();Array.Sort(a);double total=0;foreach(var x in a)total+=x;if(a.Length>0)result.Add(new PvpNativePerfView{name=p.Key,count=a.Length,averageMs=total/a.Length,p95Ms=a[Math.Min(a.Length-1,(int)(a.Length*.95))],maxMs=a[a.Length-1]});}return result.ToArray();}
}
