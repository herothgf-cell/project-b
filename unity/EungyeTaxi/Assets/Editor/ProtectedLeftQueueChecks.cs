using System;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProtectedLeftQueueChecks {
 static readonly MethodInfo tick=typeof(TrafficCar).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
 public static void Run(){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var root=new GameObject("Protected left queue");
  bool automatic=Physics.autoSimulation;
  try{
   Physics.autoSimulation=false;
   var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();
   var system=root.AddComponent<TrafficSystem>();
   typeof(TrafficSystem).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(system,null);
   system.Build(map,false);system.manualClock=true;
   var district=root.AddComponent<DistrictTraffic>();district.Initialize(system,map,false);
   var j=system.junctions.Single(x=>x.name=="J7973_3744");
   var graph=district.graph;
   int incoming=Array.FindIndex(graph.edges,e=>e.id=="A2218G611886:0");
   int upstream=Array.FindIndex(graph.edges,e=>!e.TraversalDisabled&&e.next.Contains(incoming));
   int left=Array.FindIndex(graph.edges,e=>e.id=="A2218G611452:0");
   int exit=graph.edges[left].next.First(n=>!graph.edges[n].TraversalDisabled);
   var cars=new TrafficCar[5];var stops=new TrafficStop[cars.Length];
   for(int i=0;i<cars.Length;i++){
    // Distinct trips share the native connector, as independent NPC journeys do.
    var route=district.CreateRoute(upstream,i*.1f,exit,graph.paths[exit].length);
    var stop=route.stops.Single(s=>s.junction==j&&s.Movement(route)==-1);
    float position=stop.distance-.1f-i*6.8f;
    Check(position>=0,"enough inbound road for five stopped cars");
    var car=new GameObject("queued "+i).AddComponent<TrafficCar>();car.transform.SetParent(root.transform);
    car.Initialize(system,route,position,i);car.district=district;
    cars[i]=car;stops[i]=stop;
   }
   var first=stops[0];int sign=system.Sign(cars[0].route.Tangent(first.distance),first.axis,j);
   var program=j.gameProgram;float start=0,green=0;
   foreach(var stage in program.stages){
    if(program.Movement(first.axis,sign,-1,start+.1f)==SignalColor.Green){green=stage.green;break;}
    start+=stage.green+stage.yellow+stage.clearance;
   }
   Check(green>0,"real protected left stage found");
   system.manualTime=start+.1f-j.offset;
   cars[0].Place(stops[0].distance+12);cars[0].speed=4;
   cars[1].Place(stops[1].distance);
   bool sameStream=system.MovingLeader(cars[1],cars[0]);
   bool follows=system.MayEnter(cars[1],stops[1]);
   Debug.Log("LEFT_MOVING_LEADER edge="+graph.edges[left].id+" sourceRule="+graph.edges[left].ruleId+" recognized="+sameStream+" admitted="+follows);
   system.Release(cars[1]);
   cars[0].speed=0;
   Check(!system.MayEnter(cars[1],stops[1]),"stopped vehicle inside turn blocks entry");
   cars[0].Place(stops[0].exitDistance);
   Check(!system.MayEnter(cars[1],stops[1]),"blocked exit holds follower");
   cars[0].Place(stops[0].distance+12);cars[0].speed=4;
   j.observationAxis=first.axis;j.observationSign=sign;
   j.rightTurnObservation=new RoadObservation{known=true,pedestrian=PedestrianState.Crossing};
   Check(!system.CanEnter(cars[1],stops[1]),"occupied crosswalk holds protected turn");
   j.rightTurnObservation=null;
   cars[0].Place(stops[0].distance-.1f);cars[1].Place(stops[1].distance-6.9f);
   var entered=Enumerable.Repeat(-1f,cars.Length).ToArray();float minSpacing=float.MaxValue;
   var bodies=cars.Select(c=>c.GetComponent<BoxCollider>()).ToArray();
   for(int step=-50;step<(int)(green/Time.fixedDeltaTime);step++){
    float elapsed=step*Time.fixedDeltaTime;system.manualTime=start+elapsed-j.offset;
    foreach(var car in cars)tick.Invoke(car,null);
    Physics.Simulate(Time.fixedDeltaTime);
    for(int i=0;i<cars.Length;i++){
     if(cars[i].distance>stops[i].distance+.05f&&entered[i]<0){Check(step>=0,"no red entry");entered[i]=elapsed;}
     if(i==0)continue;
     minSpacing=Mathf.Min(minSpacing,Vector3.Distance(cars[i].PhysicsPosition,cars[i-1].PhysicsPosition));
     Check(!Physics.ComputePenetration(bodies[i],cars[i].PhysicsPosition,cars[i].PhysicsRotation,bodies[i-1],cars[i-1].PhysicsPosition,cars[i-1].PhysicsRotation,out _,out _),"queued vehicle bodies overlap");
    }
   }
   int count=entered.Count(t=>t>=0);
   Debug.Log("PROTECTED_LEFT_QUEUE junction="+j.name+" green="+green+" entered="+count+" times="+string.Join(",",entered.Select(t=>t.ToString("F2")))+" minCenterSpacing="+minSpacing.ToString("F2")+" blocked="+string.Join(",",cars.Select(c=>c.waitingForJunction+"/"+c.blockedName)));
   Check(minSpacing>=4.2f,"convoy keeps vehicle footprints apart");
   Check(sameStream&&follows,"independent routes follow moving leader on native left");
   Check(count>=4,"four queued NPCs should enter during the existing protected arrow");
  }finally{Physics.autoSimulation=automatic;UnityEngine.Object.DestroyImmediate(root);}
 }
}
