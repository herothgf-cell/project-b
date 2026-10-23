using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
public class RightTurnQueueProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-rightTurnQueueTest")>=0)new GameObject("Right queue checks").AddComponent<RightTurnQueueProbe>();}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);var s=FindObjectOfType<DriveSession>();s.business.enabledCampaign=false;s.business.Depart();s.paused=false;s.taxi.gameObject.SetActive(false);s.roadConditions.enabled=false;Time.timeScale=4;
  var t=s.traffic;var d=t.GetComponent<DistrictTraffic>();foreach(var c in t.cars){c.Recycle();c.enabled=false;}t.manualClock=true;var routes=new List<TrafficRoute>();foreach(var a in s.world.map.stops)foreach(var b in s.world.map.stops){if(a==b)continue;var r=d.CreateRoute(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);if(r!=null)routes.Add(r);}var seen=new HashSet<string>();var report=new StringBuilder();int count=0,failed=0;
  foreach(var route in routes){
   foreach(var stop in route.stops){if(stop.Movement(route)!=1||!stop.junction.signalized||stop.distance<18)continue;int sign=t.Sign(route.Tangent(stop.distance),stop.axis,stop.junction);string key=stop.junction.name+":"+stop.axis+":"+sign;if(!seen.Add(key))continue;
    t.manualTime=(stop.junction.leftTurnMode==LeftTurnMode.Permissive?stop.axis:stop.axis*2+(sign>0?0:1))*17+2-stop.junction.offset;
    var first=t.Spawn(route,stop.distance-1,1000);var second=t.Spawn(route,stop.distance-10,1001);if(!first||!second){report.AppendLine("SKIP_STATIC "+key);if(first){first.Recycle();first.enabled=false;}if(second){second.Recycle();second.enabled=false;}continue;}first.respawn=second.respawn=false;
    var others=new List<TrafficCar>();var arms=new HashSet<string>();foreach(var r in routes)foreach(var q in r.stops){if(q.junction!=stop.junction)continue;int otherSign=t.Sign(r.Tangent(q.distance),q.axis,q.junction);string arm=q.axis+":"+otherSign;if(q.axis==stop.axis&&otherSign==sign||arms.Contains(arm)||q.distance<8)continue;var c=t.Spawn(r,q.distance-1,1100+others.Count);if(c){arms.Add(arm);c.respawn=false;others.Add(c);}}
    int overlaps=0;float followerEntry=-1;for(int tick=0;tick<72;tick++){yield return new WaitForSeconds(.25f);if(followerEntry<0&&second.distance>stop.distance+.1f)followerEntry=(tick+1)*.25f;
     foreach(var c in new[]{first,second})if(c.Visible)foreach(var hit in Physics.OverlapBox(c.transform.position+Vector3.up*.85f,new Vector3(.7f,.4f,1.6f),c.transform.rotation,1<<9,QueryTriggerInteraction.Ignore)){var other=hit.GetComponentInParent<TrafficCar>();if(other&&other!=c&&other.Visible)overlaps++;}}
    count++;bool pass=first.distance>stop.exitDistance&&second.distance>stop.distance+2&&overlaps==0;report.AppendLine("FOLLOWER_ENTRY seconds="+followerEntry+" overlaps="+overlaps);
    if(!pass)failed++;report.AppendLine((pass?"PASS ":"FAIL ")+key+" first="+first.distance+" second="+second.distance+" stop="+stop.distance+" exit="+stop.exitDistance+" signal="+first.waitingForSignal+" obstacle="+first.blockedName+" junction="+first.waitingForJunction+" permitted="+t.CanEnter(first,stop));
    foreach(var c in others){report.AppendLine(" OTHER "+c.distance+" signal="+c.waitingForSignal+" junction="+c.waitingForJunction+" obstacle="+c.blockedName);c.Recycle();c.enabled=false;}first.Recycle();second.Recycle();first.enabled=second.enabled=false;
    Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/right-turn-queue-test.txt",report.ToString());
   }
  }report.AppendLine("fixtures="+count+" failures="+failed);File.WriteAllText("artifacts/right-turn-queue-test.txt",report.ToString());Application.Quit(failed==0?0:1);
 }
}
