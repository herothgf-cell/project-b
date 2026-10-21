using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class TrafficPathPerformanceTests {
 public static void Run(){
  for(float speed=0;speed<=30;speed+=.25f){float horizon=TrafficRules.SignalObservationDistance(speed);if(horizon<speed*speed/6+3)throw new Exception("Signal horizon shorter than reservation");for(float cruise=0;cruise<=9;cruise+=.25f)if(TrafficRules.SafeSpeed(horizon,cruise)!=cruise)throw new Exception("Ignored signal could affect cruise speed");}
  var points=new Vector3[4097];var lengths=new float[points.Length];
  for(int i=0;i<points.Length;i++){points[i]=new Vector3(i*.7f,0,Mathf.Sin(i*.03f)*12);if(i>0)lengths[i]=lengths[i-1]+Vector3.Distance(points[i-1],points[i]);}
  var route=new TrafficRoute();route.SetPoints(points);var random=new System.Random(45);var samples=new float[24000];
  for(int i=0;i<samples.Length;i++)samples[i]=(float)random.NextDouble()*(route.length+2)-1;
  Func<float,Vector3> linear=d=>{for(int i=1;i<points.Length;i++)if(d<=lengths[i])return Vector3.Lerp(points[i-1],points[i],Mathf.InverseLerp(lengths[i-1],lengths[i],d));return points[points.Length-1];};
  foreach(float d in samples)if((route.Position(d)-linear(d)).sqrMagnitude>1e-8f)throw new Exception("Traffic path position changed at "+d);
  for(int i=0;i<lengths.Length;i++)if((route.Position(lengths[i])-points[i]).sqrMagnitude>1e-8f)throw new Exception("Traffic path vertex mismatch");
  var timer=Stopwatch.StartNew();Vector3 sum=Vector3.zero;foreach(float d in samples)sum+=linear(d);double baseline=timer.Elapsed.TotalMilliseconds;
  timer.Restart();foreach(float d in samples)sum+=route.Position(d);double actual=timer.Elapsed.TotalMilliseconds;
  string report="samples="+samples.Length+" vertices="+points.Length+" linearMs="+baseline+" actualMs="+actual+" ratio="+(actual/baseline)+" checksum="+sum+"\n";
  Directory.CreateDirectory("../../artifacts");File.WriteAllText("../../artifacts/traffic-path-performance.txt",report);UnityEngine.Debug.Log(report);
  if(actual>baseline*.5)throw new Exception("Long traffic route sampling must cost less than half a linear scan");
 }
}
