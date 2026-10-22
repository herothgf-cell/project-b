using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Fixed-camera and collider inspection, not a manual or physical driving test.
public sealed class ReviewedLaneLayoutProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){var args=System.Environment.GetCommandLineArgs();if(System.Array.IndexOf(args,"-reviewedLaneProbe")>=0||System.Array.IndexOf(args,"-reviewedLaneTrafficTest")>=0)new GameObject("Reviewed lane inspection").AddComponent<ReviewedLaneLayoutProbe>();}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);var session=FindObjectOfType<DriveSession>();Time.timeScale=0;session.drivingCamera.enabled=false;
  var map=session.world.map;var graph=session.jobs.nav.hd;var district=FindObjectOfType<DistrictTraffic>();
  bool shared=ReferenceEquals(graph.edges,map.hdEdges)&&district&&ReferenceEquals(district.graph.edges,map.hdEdges);int samples=0,blocked=0;
  Physics.SyncTransforms();
  for(int i=0;i<graph.edges.Length;i++)if(graph.edges[i].id.Contains("reviewed:T1-")&&!graph.edges[i].disabled)for(float at=0;at<=graph.paths[i].length;at+=1){
   var p=graph.paths[i].Position(at)+Vector3.up;var direction=graph.paths[i].Tangent(at);samples++;
   if(Physics.OverlapBox(p,new Vector3(.95f,.65f,1.8f),Quaternion.LookRotation(direction),1,QueryTriggerInteraction.Ignore).Any(c=>c.name.StartsWith("Building ")))blocked++;
  }
  int pocket=System.Array.FindIndex(graph.edges,e=>e.id=="reviewed:T1-SE:pocket");var path=graph.paths[pocket];var midpoint=path.Position(path.length*.5f);var forward=path.Tangent(path.length*.5f);
  string folder=Path.GetFullPath("artifacts");Directory.CreateDirectory(folder);
  Capture(Camera.main,midpoint+Vector3.up*58,midpoint+forward*.1f,Path.Combine(folder,"t1-reviewed-lane-overview.png"));
  Capture(Camera.main,path.Position(0)-forward*30+Vector3.up*13,midpoint+forward*8,Path.Combine(folder,"t1-reviewed-lane-approach.png"));
  var junction=System.Array.Find(map.junctionRules,j=>j.id=="J4396_13458").V();
  Capture(Camera.main,junction+Vector3.up*145,junction+Vector3.forward*.1f,Path.Combine(folder,"t1-all-directions.png"));
  foreach(var arrow in LaneDirectionPaint.Layout(map).Where(a=>map.hdEdges[a.edgeIndex].ruleId=="J4396_13458")){
   string id=map.hdEdges[arrow.edgeIndex].id.Replace(':','_');
   Capture(Camera.main,arrow.centre-arrow.forward*12+Vector3.up*20,arrow.centre,Path.Combine(folder,"lane-arrow-"+id+".png"));
  }
  if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-reviewedLaneTrafficTest")>=0){
   session.business.preparing=false;session.business.enabledCampaign=false;session.business.enabled=false;
   foreach(var car in session.traffic.cars){car.respawn=false;car.Recycle();car.enabled=false;}
   int taper=System.Array.FindIndex(graph.edges,e=>e.id=="reviewed:T1-SE:taper");int left=graph.edges[pocket].next[0],exit=graph.edges[left].next[0];
   var route=district.CreateRoute(taper,0,exit,Mathf.Min(30,graph.paths[exit].length));route.turn=-1;
   foreach(var stop in route.stops)stop.junction.signalized=false;
   var first=session.traffic.Spawn(route,12,0);var second=session.traffic.Spawn(route,0,1);
   bool convoy=first&&second;float gap=float.MaxValue;
   if(convoy){first.respawn=second.respawn=false;Time.timeScale=4;float until=Time.realtimeSinceStartup+35;
    while(Time.realtimeSinceStartup<until&&(first.completedRoutes==0||second.completedRoutes==0)){
     if(first.Visible&&second.Visible)gap=Mathf.Min(gap,first.distance-second.distance);yield return new WaitForFixedUpdate();
    }
    convoy=first.completedRoutes>0&&second.completedRoutes>0&&gap>=4.2f;
   }
   shared&=convoy;File.WriteAllText(Path.Combine(folder,"t1-reviewed-lane-traffic.txt"),"success="+convoy+" firstCompleted="+(first?first.completedRoutes:0)+" secondCompleted="+(second?second.completedRoutes:0)+" minimumAlongGap="+gap+" kinematicNpc=true physicalTaxiDrive=false signalTimingTest=false\n");
   Time.timeScale=0;
  }
  bool ok=shared&&blocked==0;string report="success="+ok+" sharedGraph="+shared+" colliderSamples="+samples+" buildingBlocked="+blocked+" manualDriving=false physicalDrive=false\n";
  File.WriteAllText(Path.Combine(folder,"t1-reviewed-lane-probe.txt"),report);Debug.Log("REVIEWED_LANE_PROBE "+report);Application.Quit(ok?0:1);
 }
 static void Capture(Camera camera,Vector3 eye,Vector3 target,string path){
  camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));camera.fieldOfView=55;
  var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
  var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
  camera.targetTexture=null;RenderTexture.active=previous;Destroy(pixels);rt.Release();Destroy(rt);
 }
}
