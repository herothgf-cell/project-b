using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
// Focused executable gate for the three additional surveyed approaches. No campaign or physical driving assertion.
public sealed class AdditionalUturnIntegrationProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-additionalUturnTest")>=0)new GameObject("U-turn integration probe").AddComponent<AdditionalUturnIntegrationProbe>();}
 readonly StringBuilder report=new StringBuilder();bool ok=true;
 void Check(bool value,string text){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+text);}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);var session=FindObjectOfType<DriveSession>();Time.timeScale=0;session.drivingCamera.enabled=false;
  session.business.preparing=false;session.business.enabledCampaign=false;session.business.enabled=false;session.jobs.enabled=false;
  foreach(var c in session.traffic.cars){c.respawn=false;c.Recycle();c.enabled=false;}
  var graph=session.jobs.nav.hd;var district=FindObjectOfType<DistrictTraffic>();var map=session.world.map;string folder=Path.GetFullPath("artifacts");Directory.CreateDirectory(folder);
  Check(ReferenceEquals(graph.edges,map.hdEdges)&&district&&ReferenceEquals(district.graph.edges,map.hdEdges),"shared player/NPC graph");
  var turns=graph.edges.Select((edge,index)=>new{edge,index}).Where(x=>x.edge.turn==-2&&!x.edge.disabled&&x.edge.id.StartsWith("join:reviewed:J")).ToArray();Check(turns.Length==3,"three reviewed movements");
  Physics.SyncTransforms();int blocked=0,samples=0;var environment=FindObjectOfType<RoadEnvironment>();
  foreach(var item in turns){var path=graph.paths[item.index];for(float d=0;d<=path.length;d+=1){samples++;var p=path.Position(d);if(Physics.OverlapBox(p+Vector3.up,new Vector3(.95f,.6f,1.8f),Quaternion.LookRotation(path.Tangent(d)),1,QueryTriggerInteraction.Ignore).Any(c=>c.name.StartsWith("Building ")||c.name.Contains("중앙분리")||c.name.Contains("Median")))blocked++;}}
  Check(blocked==0,"U-turn static collider samples="+samples+" blocked="+blocked);
  Camera.main.enabled=false; session.traffic.manualClock=true;
  foreach(var item in turns){
   int from=Array.FindIndex(graph.edges,e=>e.next.Contains(item.index));int target=item.edge.next[0];var route=district.CreateRoute(from,Mathf.Max(0,graph.paths[from].length-12),target,Mathf.Min(8,graph.paths[target].length));
   Check(route!=null&&route.stops.Any(s=>s.Movement(route)==-2),item.edge.id+" NPC route and movement");if(route==null)continue;
   var stop=route.stops.First(s=>s.Movement(route)==-2);int sign=session.traffic.Sign(route.Tangent(stop.distance),stop.axis,stop.junction);bool phase=false;
   for(int t=0;t<180;t++){session.traffic.manualTime=t+.25f;if(session.traffic.UturnPermitted(stop.junction,stop.axis,sign,graph.edges[from].id)){phase=true;break;}}
   Check(phase,item.edge.id+" allowed signal phase");var car=session.traffic.Spawn(route,0,0);Check(car!=null,item.edge.id+" spawned");if(!car)continue;car.respawn=false;
   Time.timeScale=2;float simStart=Time.time;float until=Time.realtimeSinceStartup+120;while(Time.realtimeSinceStartup<until&&Time.time-simStart<40&&car.completedRoutes==0){yield return null;}
   Check(car.completedRoutes>0,item.edge.id+" kinematic NPC completion distance="+car.distance+" / "+route.length+" blocked="+car.blockedName+" simSeconds="+(Time.time-simStart)+" speed="+car.speed+" signal="+car.waitingForSignal+" junction="+car.waitingForJunction+" enter="+session.traffic.CanEnter(car,stop));car.Recycle();car.enabled=false;Time.timeScale=0;
  }
  report.AppendLine("humanManualPlay=false campaignValidation=false");File.WriteAllText(Path.Combine(folder,"additional-uturn-probe.txt"),"success="+ok+"\n"+report);Debug.Log(report.ToString());Application.Quit(ok?0:1);
 }
}
