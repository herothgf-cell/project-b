using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Static footprint/collider probes and renders, deliberately not called a drive test.
public class CorridorPolishProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-corridorPolishTest")>=0)new GameObject("Corridor verification").AddComponent<CorridorPolishProbe>();}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);var s=FindObjectOfType<DriveSession>();Time.timeScale=0;Physics.SyncTransforms();
  var map=s.world.map;var result=CorridorPolish.For(map);var district=FindObjectOfType<DistrictTraffic>();var report=new StringBuilder();bool ok=result!=null&&district&&ReferenceEquals(district.map,map)&&ReferenceEquals(district.graph.edges,s.jobs.nav.hd.edges);
  report.AppendLine("same map/edges for navigation and NPC="+ok);int blocked=0,samples=0;var graph=s.jobs.nav.hd;
  foreach(int i in Enumerable.Range(0,graph.edges.Length).Where(i=>result.edges.Contains(graph.edges[i].id))){var path=graph.paths[i];for(float at=2;at<path.length-2;at+=4){
   samples++;var p=path.Position(at);var d=path.Tangent(at);
   foreach(var hit in Physics.OverlapBox(p+Vector3.up*.7f,new Vector3(.94f,.48f,1.8f),Quaternion.LookRotation(d),1,QueryTriggerInteraction.Ignore))if(hit.name.StartsWith("Building ")){blocked++;if(blocked<10)report.AppendLine("blocked "+hit.name+" "+graph.edges[i].id);}
  }}ok&=blocked==0;
  string folder=Path.GetFullPath("artifacts");Directory.CreateDirectory(folder);var camera=Camera.main;s.drivingCamera.enabled=false;
  var stops=map.stops.Take(5).ToList();stops.Insert(1,FuelStationLayout.For(map)[0].approach);
  for(int i=0;i<stops.Count;i++){var stop=stops[i];var target=stop.V()+stop.Direction*10;camera.transform.SetPositionAndRotation(stop.V()-stop.Direction*22+Vector3.up*13,Quaternion.LookRotation(target-(stop.V()-stop.Direction*22+Vector3.up*13)));camera.fieldOfView=60;
   var texture=new RenderTexture(1280,720,24);var previous=RenderTexture.active;camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"corridor-site-"+i+".png"),pixels.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;Destroy(pixels);texture.Release();Destroy(texture);
  }
  report.AppendLine("widths="+result.widths.Count+" sharedLaneSurfaces="+result.ribbons+" samples="+samples+" buildingBlocked="+blocked+" staticProbe=true manualDrive=false success="+ok);File.WriteAllText(Path.Combine(folder,"corridor-runtime-test.txt"),report.ToString());Debug.Log(report.ToString());Application.Quit(ok?0:1);
 }
}
