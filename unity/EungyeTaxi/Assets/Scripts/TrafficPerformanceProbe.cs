using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Opt-in diagnostics; never changes the normal player's save or traffic count.
public sealed class TrafficPerformanceProbe : MonoBehaviour {
 public static readonly bool Enabled=Array.IndexOf(Environment.GetCommandLineArgs(),"-trafficPerformanceTest")>=0;
 static readonly Dictionary<string,long> ticks=new Dictionary<string,long>();
 static readonly Dictionary<string,long> intervalTicks=new Dictionary<string,long>();
 // A value scope avoids allocating a managed object for every car/pedestrian tick
 // in the very run that is meant to measure collection-related frame hitches.
 public struct Scope:IDisposable {readonly string name;readonly long start;public Scope(string n){name=n;start=Enabled?Stopwatch.GetTimestamp():0;}public void Dispose(){if(!Enabled)return;long elapsed=Stopwatch.GetTimestamp()-start,value;ticks.TryGetValue(name,out value);ticks[name]=value+elapsed;intervalTicks.TryGetValue(name,out value);intervalTicks[name]=value+elapsed;}}
 public static Scope Measure(string name){return new Scope(name);}
 readonly List<float> frames=new List<float>();bool measuring;float begin;int gc,focusedFrames;string diagnostics="";
 struct FrameSample {public float elapsed,ms,x,z;public int collections;public double car,pedestrian,start,navigation,readability,signal,raster;}
 readonly List<FrameSample> samples=new List<FrameSample>();DriveSession measuredSession;float maxFrame;int over50,over100;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Enabled)new GameObject("Traffic performance probe").AddComponent<TrafficPerformanceProbe>();}
 IEnumerator Start(){
  DriveSession s;while(!(s=FindObjectOfType<DriveSession>())||!s.StartupReady)yield return null;yield return new WaitForSecondsRealtime(3);s.business.Depart();s.taxi.automated=true;s.taxi.autoBrake=true;
  var diagnosticArgs=Environment.GetCommandLineArgs();
  if(Array.IndexOf(diagnosticArgs,"-trafficPerfNoHUD")>=0){s.GetComponent<DrivingReadability>().enabled=false;diagnostics+="diagnostic=no-readability-HUD\n";}
  if(Array.IndexOf(diagnosticArgs,"-trafficPerfNoShadows")>=0){foreach(var light in FindObjectsOfType<Light>())light.shadows=LightShadows.None;diagnostics+="diagnostic=no-shadows\n";}
  diagnostics+="quality="+QualitySettings.names[QualitySettings.GetQualityLevel()]+" shadowCascades="+QualitySettings.shadowCascades+" shadowDistance="+QualitySettings.shadowDistance+" firstPerson="+s.drivingCamera.FirstPerson+"\n";
  if(Array.IndexOf(diagnosticArgs,"-trafficPerfLegacyPopulation")>=0){for(int i=s.traffic.cars.Count-1;i>=48;i--){var car=s.traffic.cars[i];s.traffic.Release(car);s.traffic.cars.RemoveAt(i);car.gameObject.SetActive(false);Destroy(car.gameObject);}for(int i=s.ambientPedestrians.actors.Count-1;i>=6;i--){var actor=s.ambientPedestrians.actors[i];s.ambientPedestrians.actors.RemoveAt(i);actor.gameObject.SetActive(false);Destroy(actor.gameObject);}diagnostics+="diagnostic=legacy48cars6ped\n";}
  if(Array.IndexOf(diagnosticArgs,"-trafficPerfNoImportedPaint")>=0){foreach(var r in FindObjectsOfType<MeshRenderer>())if(r.name.StartsWith("원본 노면선"))r.enabled=false;diagnostics+="diagnostic=no-imported-paint\n";}
  diagnostics+="screen="+Screen.width+"x"+Screen.height+" targetFPS="+Application.targetFrameRate+" vSync="+QualitySettings.vSyncCount+"\n";
  var filters=FindObjectsOfType<MeshFilter>();foreach(var f in filters.Where(f=>f.sharedMesh).OrderByDescending(f=>f.sharedMesh.vertexCount).Take(8))diagnostics+="mesh="+f.name+" vertices="+f.sharedMesh.vertexCount+"\n";
  bool t2=Array.IndexOf(diagnosticArgs,"-trafficT2")>=0,commercial=Array.IndexOf(diagnosticArgs,"-trafficCommercial")>=0;
  var position=commercial?new Vector3(620,0,257.3f):t2?s.world.map.targets[1].V()-s.traffic.mainDirection*35:new Vector3(480,0,445);float at;var graph=s.jobs.nav.hd;int edge=graph.Nearest(position,commercial?Vector3.right:t2?s.traffic.mainDirection:new Vector3(.9737f,0,-.2278f),out at);
  if(t2&&!commercial){float best=float.MaxValue;edge=-1;for(int i=0;i<graph.paths.Length;i++){if(graph.edges[i].disabled||graph.edges[i].id.StartsWith("join:"))continue;float error;float projected=graph.paths[i].Project(s.world.map.targets[1].V(),out error);if(error<best){best=error;edge=i;at=Mathf.Max(0,projected-25);}}}
  if(edge<0){UnityEngine.Debug.LogError("Performance probe: no usable approach");Application.Quit(1);yield break;}
  s.taxi.SetSpawn(graph.paths[edge].Position(at)+Vector3.up*.65f,Quaternion.LookRotation(graph.paths[edge].Tangent(at)));s.taxi.ResetTaxi();UnityEngine.Debug.Log("PERF_LOCATION target="+(t2?s.world.map.targets[1].V():position)+" spawn="+s.taxi.transform.position+" edge="+edge+" laneAt="+at);s.jobs.nav.SetDestination(s.jobs.stops[11]);
  yield return new WaitForSecondsRealtime(3);Capture(Camera.main,commercial?"artifacts/commercial-road-review.png":t2?"artifacts/t2-road-review.png":"artifacts/j5057-road-review.png");var rest=FuelStationLayout.Rest(s.world.map);if(rest!=null){var cam=Camera.main;var positionBefore=cam.transform.position;var rotationBefore=cam.transform.rotation;var eye=rest.pump-rest.direction*30-TrafficSystem.Right(rest.direction)*25+Vector3.up*32;cam.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(rest.pump-eye));Capture(cam,"artifacts/rest-parking-review.png");cam.transform.SetPositionAndRotation(positionBefore,rotationBefore);}if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null){yield return new WaitForEndOfFrame();var ui=ScreenCapture.CaptureScreenshotAsTexture();try{Directory.CreateDirectory("artifacts");File.WriteAllBytes("artifacts/traffic-performance-ui.png",ui.EncodeToPNG());}finally{Destroy(ui);}}
  float duration=30;var args=Environment.GetCommandLineArgs();int secondsArg=Array.IndexOf(args,"-trafficPerfSeconds");float requested;
  if(secondsArg>=0&&secondsArg+1<args.Length&&float.TryParse(args[secondsArg+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out requested)&&!float.IsNaN(requested))duration=Mathf.Clamp(requested,10,1800);
  // Allocate storage before measurement; format CSV only after it, so diagnostics
  // do not add per-frame boxing/string allocations to the GC hitch evidence.
  frames.Capacity=samples.Capacity=Mathf.CeilToInt(duration*120)+120;
  ticks.Clear();intervalTicks.Clear();gc=GC.CollectionCount(0);begin=Time.realtimeSinceStartup;measuredSession=s;measuring=true;
  long memoryBefore=GC.GetTotalMemory(false);float travelBefore=0;foreach(var car in s.traffic.cars)travelBefore+=car.totalTravel;
  yield return new WaitForSecondsRealtime(duration);measuring=false;frames.Sort();float elapsed=Time.realtimeSinceStartup-begin;
  var report="stationary "+(commercial?"commercial J6480_2573 eastbound":t2?"T2 J6199_11987":"J5057_4449")+", ambient cars="+s.traffic.cars.Count+" seconds="+elapsed+" frames="+frames.Count+" avgFPS="+(frames.Count/elapsed)+" p50ms="+Percentile(.5f)+" p95ms="+Percentile(.95f)+" p99ms="+Percentile(.99f)+" maxMs="+maxFrame+" over50ms="+over50+" over100ms="+over100+" gc0="+(GC.CollectionCount(0)-gc)+" graphics="+SystemInfo.graphicsDeviceType+"\n";
  float travelAfter=0;foreach(var car in s.traffic.cars)travelAfter+=car.totalTravel;report+=diagnostics+"focusedFrames="+focusedFrames+"/"+frames.Count+"\n";report+="ambientMetres="+(travelAfter-travelBefore)+" managedBytesBefore="+memoryBefore+" managedBytesAfter="+GC.GetTotalMemory(false)+"\n";
  foreach(var pair in ticks)report+=pair.Key+" totalMs="+(pair.Value*1000.0/Stopwatch.Frequency)+"\n";
  report+="timeline CPU columns measure intervals between probe Update calls; nested scopes overlap, and do not include GPU time.\n";
  var timeline=new StringBuilder("elapsedSeconds,frameMs,taxiX,taxiZ,gc0,carMs,pedestrianMs,pedestrianStartMs,navigationMs,readabilityMs,signalMs,minimapRasterMs\n");
  foreach(var sample in samples)timeline.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0:F3},{1:F3},{2:F2},{3:F2},{4},{5:F3},{6:F3},{7:F3},{8:F3},{9:F3},{10:F3},{11:F3}\n",sample.elapsed,sample.ms,sample.x,sample.z,sample.collections,sample.car,sample.pedestrian,sample.start,sample.navigation,sample.readability,sample.signal,sample.raster);
  Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/traffic-performance.txt",report);File.WriteAllText("artifacts/traffic-performance-frames.csv",timeline.ToString());UnityEngine.Debug.Log(report);
  if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null){var fullMap=s.GetComponent<FullMapOverlay>();if(fullMap.Open()){yield return null;yield return null;yield return new WaitForEndOfFrame();var capture=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes("artifacts/traffic-performance-fullmap.png",capture.EncodeToPNG());}finally{Destroy(capture);fullMap.Close();}}}
  Application.Quit(0);
 }
 float Percentile(float p){return frames.Count==0?0:frames[Mathf.Min(frames.Count-1,Mathf.FloorToInt(frames.Count*p))];}
 static void Capture(Camera camera,string output="artifacts/j5057-road-review.png"){
  if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
  var texture=RenderTexture.GetTemporary(1280,720,24);var previous=RenderTexture.active;var target=camera.targetTexture;
  try{camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();Directory.CreateDirectory("artifacts");File.WriteAllBytes(output,pixels.EncodeToPNG());Destroy(pixels);}
  finally{camera.targetTexture=target;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(texture);}
 }
 double IntervalMs(string name){long value;return intervalTicks.TryGetValue(name,out value)?value*1000.0/Stopwatch.Frequency:0;}
 void Update(){if(measuring){float ms=Time.unscaledDeltaTime*1000;frames.Add(ms);maxFrame=Mathf.Max(maxFrame,ms);if(ms>50)over50++;if(ms>100)over100++;if(Application.isFocused)focusedFrames++;var p=measuredSession.taxi.transform.position;
  samples.Add(new FrameSample{elapsed=Time.realtimeSinceStartup-begin,ms=ms,x=p.x,z=p.z,collections=GC.CollectionCount(0)-gc,car=IntervalMs("TrafficCar.FixedUpdate"),pedestrian=IntervalMs("AmbientPedestrian.Update"),start=IntervalMs("AmbientPedestrian.MayStartCrossing"),navigation=IntervalMs("Navigation.Refresh"),readability=IntervalMs("DrivingReadability.OnGUI"),signal=IntervalMs("DrivingReadability.Signal"),raster=IntervalMs("DrivingReadability.RouteRaster")});intervalTicks.Clear();}}
}
