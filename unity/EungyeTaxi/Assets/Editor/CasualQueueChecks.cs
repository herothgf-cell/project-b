using System;
using System.Reflection;
using UnityEngine;
public static class CasualQueueChecks {
 static int checks;static void Check(bool v,string text){checks++;if(!v)throw new Exception("CASUAL_QUEUE "+text);}
 static TrafficCar Car(GameObject root,TrafficSystem t,TrafficJunction j,float at,int id){
  var r=new TrafficRoute{axis=0,graphPath=new HdRoadGraph.RoutePath()};r.SetPoints(new[]{new Vector3(1.75f,0,-50),new Vector3(1.75f,0,90)});r.stops.Add(new TrafficStop{junction=j,axis=0,turn=0,distance=35,exitDistance=65});
  var c=new GameObject("convoy "+id).AddComponent<TrafficCar>();c.transform.SetParent(root.transform);c.Initialize(t,r,at,id);c.respawn=false;t.cars.Add(c);return c;
 }
 public static void Run(){
  checks=0;bool auto=Physics.autoSimulation;Physics.autoSimulation=false;var root=new GameObject("queue throughput actual PhysX");
  try{
   var t=root.AddComponent<TrafficSystem>();typeof(TrafficSystem).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(t,null);t.manualClock=true;t.mainDirection=Vector3.forward;
   var j=new TrafficJunction{name="convoy",direction=Vector3.forward,scenarioSignal=true,observationAxis=0,observationSign=1,scenarioForward=SignalColor.Red};t.junctions.Add(j);
   var a=Car(root,t,j,35,0);var b=Car(root,t,j,28.5f,1);var c=Car(root,t,j,22,2);var tick=typeof(TrafficCar).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic);
   Action step=()=>{foreach(var car in new[]{a,b,c})tick.Invoke(car,null);Physics.Simulate(.02f);Physics.SyncTransforms();};
   for(int i=0;i<50;i++)step();Check(a.distance<=35.01f,"red queue stays behind stop line");j.scenarioForward=SignalColor.Green;
   float[] crossing={-1,-1,-1};var cars=new[]{a,b,c};for(int i=0;i<500;i++){step();for(int n=0;n<3;n++){if(cars[n].distance>35.1f&&crossing[n]<0)crossing[n]=i*.02f;}for(int n=1;n<3;n++)Check(cars[n-1].distance-cars[n].distance>4.1f,"following bodies remain separated");}
   Check(crossing[2]>0&&crossing[2]<8,"three same-lane cars depart in a single green, without exclusive whole-junction wait");
   Debug.Log("CASUAL_QUEUE_CROSSINGS "+string.Join(",",crossing));
   foreach(var car in cars)t.Release(car);a.Place(65);a.speed=0;a.enabled=false;b.Place(35);b.speed=0;c.Recycle();Physics.SyncTransforms();Check(!t.MayEnter(b,b.route.stops[0]),"stationary blocked exit stays blocked");
   a.Recycle();Check(t.MayEnter(b,b.route.stops[0]),"exit clears when actual blocker leaves");Debug.Log("CASUAL_QUEUE_OK checks="+checks);
  }finally{UnityEngine.Object.DestroyImmediate(root);Physics.autoSimulation=auto;}
 }
}
