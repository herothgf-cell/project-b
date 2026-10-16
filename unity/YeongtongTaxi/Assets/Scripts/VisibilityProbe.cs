using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
public class VisibilityProbe:MonoBehaviour {
 public DriveSession session;
 IEnumerator Start(){
  yield return new WaitForSeconds(2);var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../artifacts"));
  var report=new List<string>();var seen=new HashSet<string>();var graph=session.jobs.nav.hd;
  foreach(var path in graph.paths)for(float d=2;d<path.length;d+=4){var p=path.Position(d);foreach(var hit in Physics.OverlapBox(p+Vector3.up, new Vector3(.8f,.3f,1.5f),Quaternion.LookRotation(path.Tangent(d)),1))if(hit.name.StartsWith("Building ")&&seen.Add(hit.name))report.Add(hit.name+" x="+p.x.ToString("F1")+" z="+p.z.ToString("F1"));}
  File.WriteAllLines(Path.Combine(folder,"road-building-overlaps.txt"),report);Debug.Log("VISIBILITY_AUDIT buildingConflicts="+seen.Count);
  session.drivingCamera.enabled=false;session.taxi.body.isKinematic=true;var j=session.traffic.junctions.Find(x=>x.name=="J5750_2576");var dir=new Vector3(j.facility.approaches[0].dx,0,j.facility.approaches[0].dz);if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-boxLocationTest")>=0){j=session.traffic.junctions.Find(x=>x.name=="J7801_9629");var box=new Vector3(776.5f,0,1011.5f);dir=(j.center-box).normalized;Camera.main.transform.SetPositionAndRotation(box-dir*22+Vector3.up*5,Quaternion.LookRotation(dir));}else Camera.main.transform.SetPositionAndRotation(j.center-dir*32+Vector3.up*3,Quaternion.LookRotation(dir));
  session.jobs.Accept(0);
  foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1280,720)}){
   Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSeconds(3);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"visibility-hud-"+size.x+".png"));yield return new WaitForSeconds(2);Debug.Log("VISIBILITY_CAPTURE "+Screen.width+"x"+Screen.height);
  }Application.Quit(0);
 }
}
