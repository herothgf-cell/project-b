using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Geometry/collider inspection and fixed camera captures; not an automatic drive.
public class RoadReadabilityProbe : MonoBehaviour {
 public DriveSession session;
 IEnumerator Start(){
  yield return new WaitForSeconds(1);Time.timeScale=0;session.drivingCamera.enabled=false;
  var map=session.world.map;var report=new StringBuilder();bool ok=map.hybrid;var graph=session.jobs.nav.hd;
  int queries=0,blocked=0;Physics.SyncTransforms();
  for(int i=0;i<graph.edges.Length;i++){
   if(graph.edges[i].disabled)continue;var path=graph.paths[i];
   for(float at=2;at<path.length-2;at+=3){queries++;var p=path.Position(at)+Vector3.up;var direction=path.Tangent(at);
    foreach(var collider in Physics.OverlapBox(p,new Vector3(.94f,.65f,1.8f),Quaternion.LookRotation(direction),1,QueryTriggerInteraction.Ignore)){
     if(!collider.name.StartsWith("Building "))continue;blocked++;if(blocked<=12)report.AppendLine("BUILDING_BLOCK edge="+graph.edges[i].id+" at="+at+" building="+collider.name+" position="+p);break;
    }
   }
  }
  ok&=blocked==0;report.AppendLine("actual lane collider queries="+queries+" buildingBlocked="+blocked+" taxiHalfWidth=.94 lengthHalf=1.8");
  var conflicts=SceneBuildingVisibility.Conflicts(map);int withheld=map.buildings.Count(x=>SceneBuildingVisibility.Hidden(map,x.id));
  report.AppendLine("sourceBuildings="+map.buildings.Length+" withheld="+withheld+" retained="+(map.buildings.Length-withheld)+" activeLaneConflicts="+conflicts.Count);
  foreach(var c in conflicts)report.AppendLine("PROVISIONAL_BUILDING id="+c.buildingId+" height="+c.height+" position="+c.position+" lane="+c.edgeId+" colliderPresent="+(session.world.transform.Find("Building "+c.buildingId)!=null));
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../artifacts"));Directory.CreateDirectory(folder);var camera=Camera.main;
  foreach(var id in new[]{"37612022:B0010000:1","37612022:B0010000:158","37612023:B0010000:4537"}){
   var conflict=conflicts.FirstOrDefault(x=>x.buildingId==id);if(conflict==null){ok=false;report.AppendLine("MISSING_BOX_ID "+id);continue;}
   int e=System.Array.FindIndex(map.hdEdges,x=>x.id==conflict.edgeId);float error;float at=graph.paths[e].Project(conflict.position,out error);var direction=graph.paths[e].Tangent(at);var target=conflict.position+Vector3.up;
   string suffix=id.Substring(id.LastIndexOf(':')+1);Capture(camera,target-direction*24+Vector3.up*9,target,Path.Combine(folder,"road-box-"+suffix+"-cleared.png"),report);
  }
  var layout=new RoadPaintGeometry(map);bool four=false;
  foreach(var road in map.roads.Where(x=>!x.suppressPaint&&x.sourceLanes==4)){
   var path=new AssistedRoadNetwork.Path(road.points.Select(x=>x.V()).ToArray());
   for(float at=3;at<path.length-3;at+=3){var p=path.Position(at);var dir=path.Tangent(at);var lanes=layout.Lanes(road,p,dir);var lines=RoadGeometry.Boundaries(lanes);
    if(lanes.Count!=4||lines.Count(x=>x.yellow)!=1||lines.Count(x=>!x.yellow)!=2)continue;
    report.AppendLine("FOUR_LANE road="+road.id+" width="+road.width+" sourceLanes="+road.sourceLanes+" graph=2+2 yellowMedian=1 whiteDividers=2 position="+p);
    Capture(camera,p-dir*30+Vector3.up*14,p+dir*8,Path.Combine(folder,"road-four-lanes-view.png"),report);
    Capture(camera,p+Vector3.up*52,p+dir*.1f,Path.Combine(folder,"road-four-lanes-overview.png"),report);four=true;break;
   }if(four)break;
  }ok&=four;
  var junction=session.traffic.junctions.FirstOrDefault(x=>x.name=="J7801_9629");if(junction!=null)Capture(camera,junction.center+new Vector3(0,65,-.1f),junction.center,Path.Combine(folder,"road-junction-overview.png"),report);
  report.AppendLine("device="+SystemInfo.graphicsDeviceName+" success="+ok+" mode=geometry-and-collider-probe manualDriving=false");File.WriteAllText(Path.Combine(folder,"road-readability-probe.txt"),report.ToString());Debug.Log("ROAD_READABILITY_PROBE_"+(ok?"OK":"FAIL")+" queries="+queries+" buildingBlocked="+blocked);Application.Quit(ok?0:1);
 }
 void Capture(Camera camera,Vector3 eye,Vector3 target,string path,StringBuilder report){
  camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));camera.fieldOfView=52;
  var texture=new RenderTexture(1280,720,24);var previous=RenderTexture.active;camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
  var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;Destroy(pixels);texture.Release();Destroy(texture);
  report.AppendLine("CAPTURE "+Path.GetFileName(path)+" eye="+eye+" target="+target+" fov=52 dimensions=1280x720");
 }
}
