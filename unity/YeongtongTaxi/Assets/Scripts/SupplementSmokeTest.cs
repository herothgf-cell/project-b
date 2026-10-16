using System.Collections;
using System.IO;
using UnityEngine;
public class SupplementSmokeTest:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-supplementTest")>=0)new GameObject("Supplement check").AddComponent<SupplementSmokeTest>();}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(1);var s=FindObjectOfType<DriveSession>();var map=s.world.map;var district=s.traffic.GetComponent<DistrictTraffic>();bool ok=map.supplementApplied&&object.ReferenceEquals(s.jobs.nav.hd.edges,district.graph.edges);int routes=0,closed=0;
  foreach(var e in map.hdEdges)if(e.disabled)closed++;
  foreach(var a in map.stops)foreach(var b in map.stops){if(a==b)continue;var r=district.CreateRoute(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);ok&=r!=null;if(r!=null){routes++;foreach(int edge in r.graphPath.edges)ok&=!map.hdEdges[edge].disabled;}}
  foreach(var car in s.traffic.cars)foreach(int edge in car.route.graphPath.edges)ok&=!map.hdEdges[edge].disabled;
  ok&=routes==132&&closed==7;Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/supplement-test.txt","success="+ok+" sharedGraph="+object.ReferenceEquals(s.jobs.nav.hd.edges,district.graph.edges)+" routes="+routes+" closed="+closed+" cars="+s.traffic.cars.Count);
  if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-captureSupplement")>=0){s.drivingCamera.enabled=false;var c=System.Array.Find(map.osmCrossings,x=>x.render);var dir=new Vector3(c.dx,0,c.dz);Camera.main.transform.SetPositionAndRotation(new Vector3(c.x,0,c.z)-dir*16+Vector3.up*14,Quaternion.LookRotation(dir*16-Vector3.up*14));yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/osm-crossing.png"));yield return new WaitForSecondsRealtime(1);}
  Application.Quit(ok?0:1);
 }
}
