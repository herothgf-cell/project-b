using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
// Automated integration probe: fixed-camera/collider checks, kinematic NPC passage, then physical H steering.
public sealed class UturnIntegrationProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-uturnIntegrationTest")>=0)new GameObject("U-turn integration probe").AddComponent<UturnIntegrationProbe>();}
 readonly StringBuilder report=new StringBuilder();bool ok=true;
 void Check(bool value,string text){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+text);}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);var session=FindObjectOfType<DriveSession>();Time.timeScale=0;session.drivingCamera.enabled=false;
  session.business.preparing=false;session.business.enabledCampaign=false;session.business.enabled=false;session.jobs.enabled=false;
  foreach(var c in session.traffic.cars){c.respawn=false;c.Recycle();c.enabled=false;}
  var graph=session.jobs.nav.hd;var district=FindObjectOfType<DistrictTraffic>();var map=session.world.map;string folder=Path.GetFullPath("artifacts");Directory.CreateDirectory(folder);
  Check(ReferenceEquals(graph.edges,map.hdEdges)&&district&&ReferenceEquals(district.graph.edges,map.hdEdges),"shared player/NPC graph");
  var turns=graph.edges.Select((edge,index)=>new{edge,index}).Where(x=>x.edge.turn==-2&&!x.edge.disabled).ToArray();Check(turns.Length==3,"three reviewed movements");
  Physics.SyncTransforms();int blocked=0,samples=0;var environment=FindObjectOfType<RoadEnvironment>();
  foreach(var item in turns){var path=graph.paths[item.index];for(float d=0;d<=path.length;d+=1){samples++;var p=path.Position(d);if(Physics.OverlapBox(p+Vector3.up,new Vector3(.95f,.6f,1.8f),Quaternion.LookRotation(path.Tangent(d)),1,QueryTriggerInteraction.Ignore).Any(c=>c.name.StartsWith("Building ")||c.name.Contains("중앙분리")||c.name.Contains("Median")))blocked++;}}
  Check(blocked==0,"U-turn static collider samples="+samples+" blocked="+blocked);
  Capture(Camera.main,new Vector3(290,65,450),new Vector3(290,0,493),Path.Combine(folder,"suin-reviewed-median.png"));
  Capture(Camera.main,new Vector3(439.6f,135,1345.8f),new Vector3(439.6f,0,1345.9f),Path.Combine(folder,"t1-reviewed-uturns.png"));
  Capture(Camera.main,new Vector3(1058,85,141.5f),new Vector3(1058,0,141.6f),Path.Combine(folder,"j10580-centreline-audit.png"));
  session.traffic.manualClock=true;
  foreach(var item in turns){
   int from=Array.FindIndex(graph.edges,e=>e.next.Contains(item.index));int target=item.edge.next[0];var route=district.CreateRoute(from,Mathf.Max(0,graph.paths[from].length-12),target,Mathf.Min(8,graph.paths[target].length));
   Check(route!=null&&route.stops.Any(s=>s.Movement(route)==-2),item.edge.id+" NPC route and movement");if(route==null)continue;
   var stop=route.stops.First(s=>s.Movement(route)==-2);int sign=session.traffic.Sign(route.Tangent(stop.distance),stop.axis,stop.junction);bool phase=false;
   for(int t=0;t<180;t++){session.traffic.manualTime=t+.25f;if(session.traffic.UturnPermitted(stop.junction,stop.axis,sign,graph.edges[from].id)){phase=true;break;}}
   Check(phase,item.edge.id+" allowed signal phase");var car=session.traffic.Spawn(route,0,0);Check(car!=null,item.edge.id+" spawned");if(!car)continue;car.respawn=false;
   Time.timeScale=2;float until=Time.realtimeSinceStartup+35;while(Time.realtimeSinceStartup<until&&car.completedRoutes==0){yield return new WaitForFixedUpdate();}
   Check(car.completedRoutes>0,item.edge.id+" kinematic NPC completion distance="+car.distance+" / "+route.length+" blocked="+car.blockedName);car.Recycle();car.enabled=false;Time.timeScale=0;
  }
  // H only controls steering; the test driver provides low-speed accelerator/brake input.
  var selected=turns.FirstOrDefault(x=>x.edge.id.Contains("T1-SE"));if(selected!=null){int from=Array.FindIndex(graph.edges,e=>e.next.Contains(selected.index)),target=selected.edge.next[0];var taxi=session.taxi;var fromPath=graph.paths[from];float start=Mathf.Max(0,fromPath.length-10);taxi.SetSpawn(fromPath.Position(start)+Vector3.up*.65f,Quaternion.LookRotation(fromPath.Tangent(start)));taxi.ResetTaxi();taxi.automated=true;session.assisted.allowAutomated=true;session.jobs.nav.SetHdDestination(target,Mathf.Min(7,graph.paths[target].length));
   Time.timeScale=1;yield return new WaitForSeconds(.5f);Check(session.assisted.SetActive(true),"H steering enabled for physical U-turn");float until=Time.realtimeSinceStartup+50;bool reached=false;float worst=0;
   while(Time.realtimeSinceStartup<until){taxi.autoThrottle=taxi.Speed<2.6f?.28f:0;taxi.autoServiceBrake=taxi.Speed>3?.25f:0;float error;float at=graph.paths[target].Project(taxi.transform.position,out error);worst=Mathf.Max(worst,session.assisted.Active?0:1);if(error<2&&at>3&&Vector3.Dot(taxi.transform.forward,graph.paths[target].Tangent(at))>.8f){reached=true;break;}yield return new WaitForFixedUpdate();}
   taxi.autoThrottle=0;taxi.autoBrake=true;Check(reached,"physical H U-turn reached exit activeLost="+worst+" position="+taxi.transform.position);Time.timeScale=0;
  }
  report.AppendLine("humanManualPlay=false campaignValidation=false");File.WriteAllText(Path.Combine(folder,"uturn-integration-probe.txt"),"success="+ok+"\n"+report);Debug.Log(report.ToString());Application.Quit(ok?0:1);
 }
 static void Capture(Camera camera,Vector3 eye,Vector3 target,string file){camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));camera.fieldOfView=55;var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(file,pixels.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;Destroy(pixels);rt.Release();Destroy(rt);}
}
