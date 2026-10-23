using System;
using UnityEngine;

public static class TrafficOverlapQueryTests {
 public static void Run(){
  var root=new GameObject("Overlap regression");var origin=new Vector3(-10000,1000,-10000);
  try {
   for(int i=0;i<130;i++){var g=new GameObject("obstacle "+i);g.transform.SetParent(root.transform);g.transform.position=origin+Vector3.right*(i*.02f);g.AddComponent<BoxCollider>();}
   Physics.SyncTransforms();var query=new TrafficOverlapQuery();int count=query.Box(origin,Vector3.one*10,Quaternion.identity,1,QueryTriggerInteraction.Ignore);
   if(count!=130)throw new Exception("Saturated box query lost obstacles: "+count);
   if(query.Sphere(origin,10,1,QueryTriggerInteraction.Ignore)!=130)throw new Exception("Saturated sphere query lost obstacles");
   var warmed=query.hits;query.Box(origin,Vector3.one*10,Quaternion.identity,1,QueryTriggerInteraction.Ignore);if(!ReferenceEquals(warmed,query.hits))throw new Exception("Warm query replaces its result array");
   long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<2000;i++){query.Box(origin,Vector3.one*10,Quaternion.identity,1,QueryTriggerInteraction.Ignore);query.Sphere(origin,10,1,QueryTriggerInteraction.Ignore);}long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
   System.IO.File.WriteAllText("../../artifacts/traffic-overlap-test.txt","contacts=130 repeatedQueries=4000 allocatedBytes="+allocated+"\n");
   if(allocated>1024)throw new Exception("Repeated overlap queries allocate "+allocated+" bytes");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
