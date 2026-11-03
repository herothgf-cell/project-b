using System;using System.Reflection;using UnityEngine;
public static class PvpRoadConditionIndexTests {
 public static void Run(){var root=new GameObject("Traffic sample index fixture");int checks=0;try{
  var indexed=root.AddComponent<RoadConditions>();var reference=root.AddComponent<RoadConditions>();var random=new System.Random(7361);var samples=new RoadTrafficSampleView[512];
  for(int i=0;i<samples.Length;i++)samples[i]=new RoadTrafficSampleView{position=new Vector3((float)random.NextDouble()*4000-200,0,(float)random.NextDouble()*3200-200),direction=Quaternion.Euler(0,i*45,0)*Vector3.forward,delay=(float)random.NextDouble()*7};
  samples[0]=new RoadTrafficSampleView{position=new Vector3(100,0,100),direction=Vector3.right,delay=3};
  var state=new RoadConditionsSnapshot{incidents=new RoadIncidentView[0],samples=samples};indexed.ApplyNetworkView(state);reference.ApplyNetworkView(state);typeof(RoadConditions).GetField("networkView",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(reference,false);
  for(int i=0;i<1500;i++){var a=i==0?new Vector3(99,0,100):new Vector3((float)random.NextDouble()*4000-200,0,(float)random.NextDouble()*3200-200);var d=Quaternion.Euler(0,i*45,0)*Vector3.forward;var b=a+d*(i==0?2:(float)random.NextDouble()*1300);float width=i%3==0?3.2f:i%3==1?14:28;float want=reference.ExtraSeconds(a,b,d,width),got=indexed.ExtraSeconds(a,b,d,width);if(Mathf.Abs(want-got)>.001f)throw new Exception("Indexed traffic cost changed "+i+": "+want+" / "+got);checks++;}
  indexed.ApplyNetworkView(new RoadConditionsSnapshot{incidents=new RoadIncidentView[0],samples=new RoadTrafficSampleView[0]});if(indexed.ExtraSeconds(new Vector3(99,0,100),new Vector3(101,0,100),Vector3.right,3.2f)!=0)throw new Exception("Stale sample cell after network replacement");checks++;
  Debug.Log("PVP_TRAFFIC_SAMPLE_INDEX_OK checks="+checks);
 }finally{UnityEngine.Object.DestroyImmediate(root);}}
}
