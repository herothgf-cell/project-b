using System;
using UnityEngine;
public static class TrafficFlowTests {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static TrafficRoute Route(TrafficJunction j,float lane,bool crossing=false){var r=new TrafficRoute{axis=crossing?1:0};r.SetPoints(crossing?new[]{new Vector3(-50,0,lane),new Vector3(50,0,lane)}:new[]{new Vector3(lane,0,-50),new Vector3(lane,0,50)});r.stops.Add(new TrafficStop{junction=j,axis=r.axis,turn=0,distance=35,exitDistance=65});return r;}
 public static void Run(){var root=new GameObject("flow regression");try{var t=root.AddComponent<TrafficSystem>();t.manualClock=true;t.manualTime=2;var j=new TrafficJunction{name="test",direction=Vector3.forward,halfWidth=new[]{10f,10f},leftTurnMode=LeftTurnMode.ProtectedShared};t.mainDirection=Vector3.forward;t.junctions.Add(j);
  var r=Route(j,1.75f);var p=Route(j,5.25f);var a=new GameObject("straight lane one").AddComponent<TrafficCar>();a.transform.SetParent(root.transform);a.Initialize(t,r,35,0);a.speed=4;
  var b=new GameObject("straight lane two").AddComponent<TrafficCar>();b.transform.SetParent(root.transform);b.Initialize(t,p,35,1);b.speed=4;Physics.SyncTransforms();Check(t.MayEnter(a,r.stops[0]),"first green lane must enter");Check(t.MayEnter(b,p.stops[0]),"parallel green lane must not wait for entire junction");
  t.Release(b);b.Place(55);b.speed=4;Physics.SyncTransforms();Check(t.MayEnter(a,r.stops[0]),"parallel occupied corridor stays independent");
  t.Release(a);t.Release(b);b.route=r;b.Place(55);b.speed=0;Physics.SyncTransforms();Check(!t.MayEnter(a,r.stops[0]),"stopped same-stream car inside junction blocks entry");
  b.Place(65);b.speed=0;Physics.SyncTransforms();Check(!t.MayEnter(a,r.stops[0]),"stopped exit still blocks entry");
  b.Recycle();var cross=Route(j,0,true);b.route=cross;b.Place(50);Physics.SyncTransforms();Check(!t.MayEnter(a,r.stops[0]),"crossing occupied corridor must block");
  j.facility=new HybridJunctionRule{crosswalk=true};t.manualTime=70;Check(t.PedestrianGreen(j,0,1),"dedicated pedestrian interval visible");for(int axis=0;axis<2;axis++)for(int sign=-1;sign<=1;sign+=2)Check(t.Signal(j,axis,sign)==SignalColor.Red,"pedestrian interval must not conflict with green vehicle movement");
  t.manualTime=79;Check(!t.PedestrianGreen(j,0,1)&&t.Signal(j,0)==SignalColor.Red,"pedestrian clearance");t.manualTime=80;Check(t.Signal(j,0)==SignalColor.Green,"vehicle cycle resumes after pedestrian clearance");
  float deviation;Check(Mathf.Abs(r.ProjectNear(new Vector3(1.75f,0,7),50,out deviation)-57)<.001f&&deviation<.001f,"local route projection stays exact");
  Debug.Log("TRAFFIC_FLOW_TEST_OK parallel green, occupied parallel, blocked exit, crossing conflict, pedestrian phase, local projection");
 }finally{UnityEngine.Object.DestroyImmediate(root);}}
}
