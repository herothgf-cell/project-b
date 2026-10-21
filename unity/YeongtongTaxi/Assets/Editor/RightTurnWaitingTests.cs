using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class RightTurnWaitingTests {
 public static void RunConvoy(){
  MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();var root=new GameObject("Right convoy regression");var t=root.AddComponent<TrafficSystem>();t.Build(map,false);var d=root.AddComponent<DistrictTraffic>();d.Initialize(t,map,false);t.manualClock=true;
  var seen=new HashSet<string>();var report=new StringBuilder();int count=0,blocked=0;
  foreach(var a in map.stops)foreach(var b in map.stops){if(a==b)continue;var r=d.CreateRoute(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);if(r==null)continue;
   foreach(var stop in r.stops){var j=stop.junction;if(stop.Movement(r)!=1||!j.signalized||stop.exitDistance-stop.distance<14)continue;int sign=t.Sign(r.Tangent(stop.distance),stop.axis,j);if(!seen.Add(j.name+":"+stop.axis+":"+sign))continue;
    t.manualTime=(j.leftTurnMode==LeftTurnMode.Permissive?stop.axis:stop.axis*2+(sign>0?0:1))*17+2-j.offset;
    var follower=new GameObject("follower").AddComponent<TrafficCar>();follower.Initialize(t,r,stop.distance,0);
    var leader=new GameObject("moving leader").AddComponent<TrafficCar>();leader.Initialize(t,r,stop.distance+12,1);leader.speed=4.5f;Physics.SyncTransforms();
    bool clear=t.CanEnter(follower,stop)&&t.MayEnter(follower,stop);count++;if(!clear)blocked++;report.AppendLine((clear?"PASS ":"FAIL ")+j.name+" green same-connector moving leader 12m ahead, signalAllowed="+t.CanEnter(follower,stop));
    t.Release(follower);leader.Place(stop.exitDistance);leader.speed=0;Physics.SyncTransforms();bool stoppedBlocked=!t.MayEnter(follower,stop);if(!stoppedBlocked)blocked++;report.AppendLine((stoppedBlocked?"PASS ":"FAIL ")+j.name+" stopped exit stays blocked");
    t.Release(follower);leader.Recycle();j.observationAxis=stop.axis;j.observationSign=sign;j.rightTurnObservation=new RoadObservation{known=true,pedestrian=PedestrianState.Crossing};bool pedestrianBlocked=!t.CanEnter(follower,stop);if(!pedestrianBlocked)blocked++;report.AppendLine((pedestrianBlocked?"PASS ":"FAIL ")+j.name+" crossing pedestrian stays blocked");j.rightTurnObservation=null;
    t.Release(follower);t.Release(leader);UnityEngine.Object.DestroyImmediate(follower.gameObject);UnityEngine.Object.DestroyImmediate(leader.gameObject);
   }
  }
  UnityEngine.Object.DestroyImmediate(root);Directory.CreateDirectory("../../artifacts");File.WriteAllText("../../artifacts/right-convoy-test.txt","checks="+count+" blocked="+blocked+"\n"+report);if(blocked>0)throw new Exception("Moving right-turn convoy blocked "+blocked+"/"+count);
 }
 public static void Run(){
  MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();var root=new GameObject("Right turn regression");var traffic=root.AddComponent<TrafficSystem>();traffic.Build(map,false);
  var district=root.AddComponent<DistrictTraffic>();district.Initialize(traffic,map,false);var routes=new List<TrafficRoute>();var report=new StringBuilder();int checks=0,failures=0;
  foreach(var a in map.stops)foreach(var b in map.stops){if(a==b)continue;var r=district.CreateRoute(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);if(r!=null)routes.Add(r);}
  var seen=new HashSet<string>();traffic.manualClock=true;
  foreach(var route in routes)foreach(var stop in route.stops){if(stop.Movement(route)!=1||!stop.junction.signalized)continue;
   var j=stop.junction;int sign=traffic.Sign(route.Tangent(stop.distance),stop.axis,j);string key=j.name+":"+stop.axis+":"+sign;if(!seen.Add(key))continue;
   traffic.manualTime=(stop.axis*2+(sign>0?0:1))*17+2-j.offset;
   var car=new GameObject("subject").AddComponent<TrafficCar>();car.Initialize(traffic,route,stop.distance,0);car.enabled=false;
   foreach(var otherRoute in routes)foreach(var otherStop in otherRoute.stops){if(otherStop.junction!=j)continue;int otherSign=traffic.Sign(otherRoute.Tangent(otherStop.distance),otherStop.axis,j);
    if(traffic.Signal(j,otherStop.axis,otherSign)!=SignalColor.Red)continue;
    var other=new GameObject("red waiting").AddComponent<TrafficCar>();other.Initialize(traffic,otherRoute,otherStop.distance-.1f,1);other.enabled=false;Physics.SyncTransforms();
    bool permits=traffic.MayEnter(car,stop);traffic.Release(car);checks++;if(!permits){failures++;report.AppendLine("BLOCKED "+key+" other="+otherStop.axis+":"+otherSign+" movement="+otherStop.Movement(otherRoute)+" at="+other.transform.position);}
    UnityEngine.Object.DestroyImmediate(other.gameObject);
   }
   UnityEngine.Object.DestroyImmediate(car.gameObject);
  }
  UnityEngine.Object.DestroyImmediate(root);Directory.CreateDirectory("../../artifacts");File.WriteAllText("../../artifacts/right-turn-waiting-test.txt","checks="+checks+" blocked="+failures+"\n"+report);Debug.Log("RIGHT_WAIT checks="+checks+" blocked="+failures);
 }
}
