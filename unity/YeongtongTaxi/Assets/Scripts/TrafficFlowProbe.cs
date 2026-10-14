using System.Collections;
using System.IO;
using UnityEngine;
public class TrafficFlowProbe : MonoBehaviour {
 public DriveSession session;
 IEnumerator Start(){yield return new WaitForSeconds(1);var t=session.taxi;t.automated=true;t.body.isKinematic=true;t.transform.position=new Vector3(-100,1,-100);Physics.SyncTransforms();Time.timeScale=8;
  var traffic=session.traffic;float[] last=new float[traffic.cars.Count],still=new float[last.Length],longest=new float[last.Length];
  for(int tick=0;tick<384;tick++){yield return new WaitForSeconds(1);for(int i=0;i<last.Length;i++){var c=traffic.cars[i];if(c.totalTravel-last[i]<.05f)still[i]++;else still[i]=0;longest[i]=Mathf.Max(longest[i],still[i]);last[i]=c.totalTravel;}
   if(tick%64==63)foreach(var c in traffic.cars){string owner="";foreach(var j in traffic.junctions)owner+=j.name+":"+(j.owner?j.owner.name:"none")+" ";Debug.Log("FLOW t="+(tick+1)+" car="+c.name+" d="+c.distance+" laps="+c.completedRoutes+" speed="+c.speed+" obstacle="+c.blockedName+" signal="+c.waitingForSignal+" junction="+c.waitingForJunction+" owners="+owner);}
  }
  bool ok=true;string report="";for(int i=0;i<last.Length;i++){var c=traffic.cars[i];ok&=c.completedRoutes>=2&&longest[i]<60;report+=c.name+" laps="+c.completedRoutes+" maxStopped="+longest[i]+" travel="+c.totalTravel+"\n";}report="success="+ok+"\n"+report;
  File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../../artifacts/traffic-flow.txt")),report);Debug.Log(report);Application.Quit(ok?0:1);
 }
}
