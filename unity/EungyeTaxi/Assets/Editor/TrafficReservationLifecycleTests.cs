using System;
using System.Reflection;
using UnityEngine;

// Invoke only in the final integration gate; no scene/map inputs or route recycling.
public static class TrafficReservationLifecycleTests {
 static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
 static TrafficRoute Route(TrafficJunction junction,bool cross){
  var route=new TrafficRoute{axis=cross?1:0};
  route.SetPoints(cross?new[]{new Vector3(-50,0,1.75f),new Vector3(50,0,1.75f)}:new[]{new Vector3(1.75f,0,-50),new Vector3(1.75f,0,50)});
  route.stops.Add(new TrafficStop{junction=junction,axis=route.axis,turn=0,distance=35,exitDistance=65});return route;
 }
 static TrafficCar Car(GameObject root,TrafficSystem traffic,TrafficRoute route,float at,int index){var car=new GameObject("reservation car "+index).AddComponent<TrafficCar>();car.transform.SetParent(root.transform);car.Initialize(traffic,route,at,index);return car;}
 static void Tick(TrafficCar car){typeof(TrafficCar).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(car,null);Physics.SyncTransforms();}
 public static void Run(){
  var root=new GameObject("reservation lifecycle regression");
  try{
   var traffic=root.AddComponent<TrafficSystem>();typeof(TrafficSystem).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(traffic,null);traffic.manualClock=true;traffic.manualTime=2;traffic.mainDirection=Vector3.forward;
   var junction=new TrafficJunction{name="reservation regression",direction=Vector3.forward,halfWidth=new[]{10f,10f},leftTurnMode=LeftTurnMode.ProtectedShared};traffic.junctions.Add(junction);
   var route=Route(junction,false);var stop=route.stops[0];var approach=Car(root,traffic,route,stop.distance-12,0);approach.speed=9;
   Check(traffic.ReservationDistance(approach)>12&&traffic.MayEnter(approach,stop),"moving green approach reserves at 12m");
   var crossRoute=Route(junction,true);var cross=Car(root,traffic,crossRoute,crossRoute.stops[0].distance,1);approach.speed=0;traffic.manualTime=36;
   Check(traffic.CanEnter(cross,crossRoute.stops[0])&&!traffic.MayEnter(cross,crossRoute.stops[0]),"retained red approach reservation blocks a physically clear conflicting green");
   Tick(approach);
   Check(!junction.reservations.Contains(approach),"red queued approach must release reservation even after speed-dependent radius shrinks below its distance");
   Check(traffic.MayEnter(cross,crossRoute.stops[0]),"conflicting green resumes after red approach reservation releases");

   // Radius also shrinks while green when a queue stops a previously moving approach.
   cross.Recycle();traffic.manualTime=2;approach.Place(stop.distance-12);approach.speed=9;
   Check(traffic.MayEnter(approach,stop),"second moving green approach reserves at 12m");approach.speed=0;
   var queue=new GameObject("stopped queue");queue.transform.SetParent(root.transform);queue.transform.position=route.Position(approach.distance+6)+Vector3.up*.85f;queue.AddComponent<BoxCollider>().size=new Vector3(1.8f,1.1f,4.2f);Physics.SyncTransforms();
   Tick(approach);Check(approach.waitingForObstacle,"fixture has a real stopped queue before stop line");
   Check(!junction.reservations.Contains(approach),"queued approach outside reduced reservation radius releases even while green");
   UnityEngine.Object.DestroyImmediate(queue);

   // A car already committed across the line must keep protection until its exit.
   approach.Place(stop.distance+.5f);junction.reservations.Add(approach);junction.owner=approach;traffic.manualTime=36;
   traffic.CancelApproach(approach,stop);
   Check(junction.reservations.Contains(approach),"crossed stop line keeps reservation through signal change");
   cross.Place(crossRoute.stops[0].distance);Check(!traffic.MayEnter(cross,crossRoute.stops[0]),"conflicting green still waits for committed crossing car");
   Debug.Log("TRAFFIC_RESERVATION_LIFECYCLE_OK red release, queued green release, committed crossing protection");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
