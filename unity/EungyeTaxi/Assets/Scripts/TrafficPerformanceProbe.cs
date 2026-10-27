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
  DriveSession s;while(!(s=FindObjectOfType<DriveSession>())||!s.StartupReady)yield return null;yield return new WaitForSecondsRealtime(3);s.business.Depart();s.taxi.automated=true;s.taxi.autoBrake=true;
  bool t2=Array.IndexOf(Environment.GetCommandLineArgs(),"-trafficT2")>=0;var position=t2?s.world.map.targets[1].V()-s.traffic.mainDirection*35:new Vector3(480,0,445);float at;var graph=s.jobs.nav.hd;int edge=graph.Nearest(position,t2?s.traffic.mainDirection:new Vector3(.9737f,0,-.2278f),out at);
  if(t2){float best=float.MaxValue;edge=-1;for(int i=0;i<graph.paths.Length;i++){if(graph.edges[i].disabled||graph.edges[i].id.StartsWith("join:"))continue;float error;float projected=graph.paths[i].Project(s.world.map.targets[1].V(),out error);if(error<best){best=error;edge=i;at=Mathf.Max(0,projected-25);}}}
  if(edge<0){UnityEngine.Debug.LogError("Performance probe: no usable approach");Application.Quit(1);yield break;}
  s.taxi.SetSpawn(graph.paths[edge].Position(at)+Vector3.up*.65f,Quaternion.LookRotation(graph.paths[edge].Tangent(at)));s.taxi.ResetTaxi();UnityEngine.Debug.Log("PERF_LOCATION target="+(t2?s.world.map.targets[1].V():position)+" spawn="+s.taxi.transform.position+" edge="+edge+" laneAt="+at);s.jobs.nav.SetDestination(s.jobs.stops[11]);
  yield return new WaitForSecondsRealtime(3);Capture(Camera.main,t2?"artifacts/t2-road-review.png":"artifacts/j5057-road-review.png");var rest=FuelStationLayout.Rest(s.world.map);if(rest!=null){var cam=Camera.main;var positionBefore=cam.transform.position;var rotationBefore=cam.transform.rotation;var eye=rest.pump-rest.direction*30-TrafficSystem.Right(rest.direction)*25+Vector3.up*32;cam.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(rest.pump-eye));Capture(cam,"artifacts/rest-parking-review.png");cam.transform.SetPositionAndRotation(positionBefore,rotationBefore);}ticks.Clear();gc=GC.CollectionCount(0);begin=Time.realtimeSinceStartup;measuring=true;
  float duration=30;var args=Environment.GetCommandLineArgs();int secondsArg=Array.IndexOf(args,"-trafficPerfSeconds");float requested;
  if(secondsArg>=0&&secondsArg+1<args.Length&&float.TryParse(args[secondsArg+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out requested)&&!float.IsNaN(requested))duration=Mathf.Clamp(requested,10,1800);
  long memoryBefore=GC.GetTotalMemory(false);float travelBefore=0;foreach(var car in s.traffic.cars)travelBefore+=car.totalTravel;
  yield return new WaitForSecondsRealtime(duration);measuring=false;frames.Sort();float elapsed=Time.realtimeSinceStartup-begin;
  var report="stationary "+(t2?"T2 J6199_11987":"J5057_4449")+", ambient cars="+s.traffic.cars.Count+" seconds="+elapsed+" frames="+frames.Count+" avgFPS="+(frames.Count/elapsed)+" p50ms="+Percentile(.5f)+" p95ms="+Percentile(.95f)+" p99ms="+Percentile(.99f)+" gc0="+(GC.CollectionCount(0)-gc)+" graphics="+SystemInfo.graphicsDeviceType+"\n";
  float travelAfter=0;foreach(var car in s.traffic.cars)travelAfter+=car.totalTravel;report+="ambientMetres="+(travelAfter-travelBefore)+" managedBytesBefore="+memoryBefore+" managedBytesAfter="+GC.GetTotalMemory(false)+"\n";
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
