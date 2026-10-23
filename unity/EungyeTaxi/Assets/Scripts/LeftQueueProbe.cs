using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
public class LeftQueueProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-leftQueueTest")>=0)new GameObject("Left queue probe").AddComponent<LeftQueueProbe>();}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(1);var session=FindObjectOfType<DriveSession>();var traffic=session.traffic;var district=traffic.GetComponent<DistrictTraffic>();
  session.business.enabledCampaign=false;session.business.preparing=false;session.paused=false;Time.timeScale=8;session.roadConditions.enabled=false;session.taxi.gameObject.SetActive(false);
  foreach(var c in traffic.cars){c.Recycle();c.enabled=false;}Physics.SyncTransforms();
  var report=new StringBuilder();bool ok=true;int cases=0;var keys=new HashSet<string>();
  foreach(var a in session.world.map.stops)foreach(var b in session.world.map.stops){if(a==b)continue;var route=district.CreateRoute(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);if(route==null)continue;
   foreach(var stop in route.stops){if(stop.junction.name!="J2564_12237"||stop.Movement(route)>=0||stop.distance<30)continue;
    var heading=route.Tangent(stop.distance);string key=Mathf.RoundToInt(heading.x*10)+":"+Mathf.RoundToInt(heading.z*10);if(!keys.Add(key))continue;cases++;
    var j=stop.junction;traffic.manualClock=true;int sign=traffic.Sign(heading,stop.axis,j);int phase=stop.axis*2+(sign>0?0:1);traffic.manualTime=phase*17+2-j.offset;
    var first=traffic.Spawn(route,stop.distance-6,100);var second=traffic.Spawn(route,stop.distance-12,101);
    report.AppendLine("CASE "+key+" stop="+stop.distance+" exit="+stop.exitDistance+" signal="+traffic.Signal(j,stop.axis,sign)+" halfWidth="+j.halfWidth[0]+","+j.halfWidth[1]);
    if(!first||!second){ok=false;report.AppendLine("FAIL spawn");continue;}
    first.district=district;second.district=district;bool follows=false;int overlaps=0,leaderBlockedByFollower=0;
    for(int t=0;t<350;t++){yield return new WaitForSeconds(.1f);if(first.distance<stop.distance&&first.waitingForJunction)leaderBlockedByFollower++;follows|=second.distance>stop.distance+.1f&&first.distance<stop.exitDistance;foreach(var hit in Physics.OverlapBox(second.transform.position+Vector3.up*.85f,new Vector3(.7f,.4f,1.6f),second.transform.rotation,1<<9))if(hit.GetComponentInParent<TrafficCar>()==first)overlaps++;if(t%50==49)foreach(var c in new[]{first,second}){string owners="";foreach(var q in traffic.junctions)if(q.reservations.Contains(c))owners+=q.name+",";report.AppendLine("t="+t+" "+c.name+" d="+c.distance+" v="+c.speed+" signal="+c.waitingForSignal+" junction="+c.waitingForJunction+" obstacle="+c.blockedName+" reservations="+owners);}}
    bool pass=first.distance>stop.exitDistance+3&&second.distance>stop.exitDistance+3&&follows&&overlaps==0&&leaderBlockedByFollower==0;report.AppendLine("follows moving same-turn leader="+follows+" overlaps="+overlaps+" leaderBlocked="+leaderBlockedByFollower);ok&=pass;report.AppendLine((pass?"PASS ":"FAIL ")+"both left turn cars clear "+key);
    foreach(var c in new[]{first,second}){c.Recycle();c.enabled=false;}
   }
  }
  ok&=cases>0;Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/left-queue-test.txt","success="+ok+" cases="+cases+"\n"+report);Debug.Log(report.ToString());Application.Quit(ok?0:1);
 }
}
