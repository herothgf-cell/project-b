using System;
using System.Collections;
using System.IO;
using UnityEngine;
public sealed class PvpSimulationCapture : MonoBehaviour {
 IEnumerator Start(){
  var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-pvpSimulationOutput");string folder=index>=0&&index+1<args.Length?args[index+1]:Application.persistentDataPath;Directory.CreateDirectory(folder);
  if(Array.IndexOf(args,"-pvpSimulationMenuCapture")>=0){while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;yield return new WaitForSecondsRealtime(2);yield return Save(Path.Combine(folder,"simulation-menu.png"));FindObjectOfType<Pvp.PvpBootstrap>().StartSimulation(true,7200);}
  PvpSimulationMatch match=null;float limit=Time.realtimeSinceStartup+180;
  while(!match&&Time.realtimeSinceStartup<limit){match=FindObjectOfType<PvpSimulationMatch>();yield return null;}
  if(!match||!match.Human){Debug.LogError("PVP_SIMULATION_CAPTURE_FAIL initialization");Application.Quit(1);yield break;}
  while(UnityEngine.Rendering.SplashScreen.isFinished==false)yield return null;
  yield return new WaitForSecondsRealtime(3);

  string prefix=Path.Combine(folder,"simulation-"+(PvpSimulationMode.Expanded?"city":"eungye"));
  yield return Save(prefix+"-drive.png");
  match.Human.drivingCamera.SetFirstPerson(true);yield return new WaitForSecondsRealtime(1);yield return Save(prefix+"-cockpit.png");
  match.Human.GetComponent<FullMapOverlay>().Open();yield return new WaitForSecondsRealtime(1);yield return Save(prefix+"-map.png");
  if(Array.IndexOf(args,"-pvpSignalAlleyCapture")>=0){yield return SignalAlley(match,folder);if(signalFailed)yield break;}
  Debug.Log("PVP_SIMULATION_CAPTURE_OK "+prefix);Application.Quit(0);
 }
 // 2026-11-10 signal/back-lane review (position-controlled, not a driving test):
 // hold the human taxi before the signal at 1:1, wait for the real red, measure
 // how long it stays red, then show one of the added back lanes.
 bool signalFailed;
 IEnumerator SignalAlley(PvpSimulationMatch match,string folder){
  var s=match.Human;var map=s.world.map;var graph=new HdRoadGraph(map);var traffic=s.traffic;
  var overlay=s.GetComponent<FullMapOverlay>();if(overlay)overlay.Close();s.drivingCamera.SetFirstPerson(false);
  int edge=Array.FindIndex(map.hdEdges,e=>e.id=="city:0:1>1:1:bay:post"&&e.ruleId=="city-junction:1:1");var j=traffic.junctions.Find(x=>x.name=="city-junction:1:1");
  if(edge<0||j==null||!j.signalized){Debug.LogError("PVP_SIGNAL_CAPTURE_FAIL fixture");signalFailed=true;Application.Quit(1);yield break;}
  var path=graph.paths[edge];float at=path.length-6;var dir=path.Tangent(at);Place(s,path.Position(at),dir);
  int axis=Mathf.Abs(Vector3.Dot(dir,traffic.AxisDirection(j,0)))>.7f?0:1;int sign=traffic.Sign(dir,axis,j);
  float limit=Time.realtimeSinceStartup+120;
  // Start from green so the logged duration is one whole red interval.
  while(traffic.Signal(j,axis,sign)!=SignalColor.Green&&Time.realtimeSinceStartup<limit){Place(s,path.Position(at),dir);yield return null;}
  while(traffic.Signal(j,axis,sign)!=SignalColor.Red&&Time.realtimeSinceStartup<limit){Place(s,path.Position(at),dir);yield return null;}
  if(traffic.Signal(j,axis,sign)!=SignalColor.Red){Debug.LogError("PVP_SIGNAL_CAPTURE_FAIL no green-to-red change within 120s");signalFailed=true;Application.Quit(1);yield break;}
  float redStart=Time.time;yield return new WaitForSecondsRealtime(2);yield return Save(Path.Combine(folder,"signal-wait.png"));
  s.drivingCamera.SetFirstPerson(true);yield return new WaitForSecondsRealtime(1);yield return Save(Path.Combine(folder,"signal-wait-cockpit.png"));s.drivingCamera.SetFirstPerson(false);
  while(traffic.Signal(j,axis,sign)==SignalColor.Red&&Time.realtimeSinceStartup<limit+120){Place(s,path.Position(at),dir);yield return null;}
  Debug.Log("PVP_SIGNAL_WAIT junction="+j.name+" redSeconds="+(Time.time-redStart).ToString("0.0")+" signals="+traffic.junctions.FindAll(x=>x.signalized).Count);
  int back=Array.FindIndex(map.hdEdges,e=>e.id=="city:alley:1:1:east>alley:1:1:back");
  if(back<0){Debug.LogError("PVP_SIGNAL_CAPTURE_FAIL back lane");signalFailed=true;Application.Quit(1);yield break;}
  var lane=graph.paths[back];Place(s,lane.Position(lane.length*.3f),lane.Tangent(lane.length*.3f));yield return new WaitForSecondsRealtime(2);yield return Save(Path.Combine(folder,"back-lane.png"));
  if(overlay){overlay.Open();yield return new WaitForSecondsRealtime(1);yield return Save(Path.Combine(folder,"back-lane-map.png"));}
 }
 static void Place(DriveSession s,Vector3 p,Vector3 d){var b=s.taxi.body;var rotation=Quaternion.LookRotation(d);var position=p+Vector3.up*.65f;b.isKinematic=true;b.position=position;b.rotation=rotation;s.taxi.transform.SetPositionAndRotation(position,rotation);Physics.SyncTransforms();}
 IEnumerator Save(string path){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();if(!texture){Debug.LogError("PVP_SIMULATION_CAPTURE_FAIL texture");Application.Quit(1);yield break;}var pixels=texture.GetPixels32();int varied=0;for(int i=0;i<pixels.Length;i+=127)if(Math.Abs(pixels[i].r-pixels[0].r)+Math.Abs(pixels[i].g-pixels[0].g)+Math.Abs(pixels[i].b-pixels[0].b)>35)varied++;if(varied<100){Debug.LogError("PVP_SIMULATION_CAPTURE_FAIL blank");Application.Quit(1);yield break;}File.WriteAllBytes(path,texture.EncodeToPNG());Destroy(texture);Debug.Log("PVP_SIMULATION_CAPTURE_FRAME "+path);}
}
