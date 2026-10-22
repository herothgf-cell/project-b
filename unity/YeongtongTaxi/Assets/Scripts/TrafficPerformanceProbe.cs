using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

// Opt-in diagnostics; never changes the normal player's save or traffic count.
public sealed class TrafficPerformanceProbe : MonoBehaviour {
 public static readonly bool Enabled=Array.IndexOf(Environment.GetCommandLineArgs(),"-trafficPerformanceTest")>=0;
 static readonly Dictionary<string,long> ticks=new Dictionary<string,long>();
 sealed class Scope:IDisposable {readonly string name;readonly long start=Stopwatch.GetTimestamp();public Scope(string n){name=n;}public void Dispose(){long value;ticks.TryGetValue(name,out value);ticks[name]=value+Stopwatch.GetTimestamp()-start;}}
 public static IDisposable Measure(string name){return Enabled?new Scope(name):null;}
 readonly List<float> frames=new List<float>();bool measuring;float begin;int gc;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Enabled)new GameObject("Traffic performance probe").AddComponent<TrafficPerformanceProbe>();}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(3);var s=FindObjectOfType<DriveSession>();s.business.Depart();s.taxi.automated=true;s.taxi.autoBrake=true;
  var position=new Vector3(480,0,445);float at;var graph=s.jobs.nav.hd;int edge=graph.Nearest(position,new Vector3(.9737f,0,-.2278f),out at);
  s.taxi.SetSpawn(graph.paths[edge].Position(at)+Vector3.up*.65f,Quaternion.LookRotation(graph.paths[edge].Tangent(at)));s.taxi.ResetTaxi();s.jobs.nav.SetDestination(s.jobs.stops[11]);
  yield return new WaitForSecondsRealtime(3);Capture(Camera.main);var rest=FuelStationLayout.Rest(s.world.map);if(rest!=null){var cam=Camera.main;var positionBefore=cam.transform.position;var rotationBefore=cam.transform.rotation;var eye=rest.pump-rest.direction*30-TrafficSystem.Right(rest.direction)*25+Vector3.up*32;cam.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(rest.pump-eye));Capture(cam,"artifacts/rest-parking-review.png");cam.transform.SetPositionAndRotation(positionBefore,rotationBefore);}ticks.Clear();gc=GC.CollectionCount(0);begin=Time.realtimeSinceStartup;measuring=true;
  yield return new WaitForSecondsRealtime(30);measuring=false;frames.Sort();float elapsed=Time.realtimeSinceStartup-begin;
  var report="stationary J5057_4449, ambient cars="+s.traffic.cars.Count+" seconds="+elapsed+" frames="+frames.Count+" avgFPS="+(frames.Count/elapsed)+" p50ms="+Percentile(.5f)+" p95ms="+Percentile(.95f)+" p99ms="+Percentile(.99f)+" gc0="+(GC.CollectionCount(0)-gc)+" graphics="+SystemInfo.graphicsDeviceType+"\n";
  foreach(var pair in ticks)report+=pair.Key+" totalMs="+(pair.Value*1000.0/Stopwatch.Frequency)+"\n";
  Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/traffic-performance.txt",report);UnityEngine.Debug.Log(report);Application.Quit(0);
 }
 float Percentile(float p){return frames.Count==0?0:frames[Mathf.Min(frames.Count-1,Mathf.FloorToInt(frames.Count*p))];}
 static void Capture(Camera camera,string output="artifacts/j5057-road-review.png"){
  if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
  var texture=RenderTexture.GetTemporary(1280,720,24);var previous=RenderTexture.active;var target=camera.targetTexture;
  try{camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();Directory.CreateDirectory("artifacts");File.WriteAllBytes(output,pixels.EncodeToPNG());Destroy(pixels);}
  finally{camera.targetTexture=target;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(texture);}
 }
 void Update(){if(measuring)frames.Add(Time.unscaledDeltaTime*1000);}
}
