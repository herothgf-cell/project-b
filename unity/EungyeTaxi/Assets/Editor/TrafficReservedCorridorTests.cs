using System;
using System.Reflection;
using UnityEngine;

// Physical NPC regression for the J1927 failure: a curved crossing's reserved
// corridor reaches a red approach before that approach's painted stop line.
public static class TrafficReservedCorridorTests {
    static readonly MethodInfo tick=typeof(TrafficCar).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic);
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static TrafficSystem System(GameObject root,out TrafficJunction junction){
        var traffic=root.AddComponent<TrafficSystem>();typeof(TrafficSystem).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(traffic,null);
        traffic.manualClock=true;traffic.manualTime=19;traffic.mainDirection=Vector3.forward;
        junction=new TrafficJunction{name="curved reserved corridor",direction=Vector3.forward,halfWidth=new[]{10f,10f},leftTurnMode=LeftTurnMode.ProtectedShared};traffic.junctions.Add(junction);return traffic;
    }
    static TrafficRoute OwnerRoute(TrafficJunction junction){
        var route=new TrafficRoute{axis=0};route.SetPoints(new[]{new Vector3(8,0,40),new Vector3(8,0,4),new Vector3(14,0,-4),new Vector3(14,0,-40)});
        route.stops.Add(new TrafficStop{junction=junction,axis=0,distance=15,exitDistance=58});return route;
    }
    static TrafficRoute ApproachRoute(TrafficJunction junction,float stopX){
        var route=new TrafficRoute{axis=1};route.SetPoints(new[]{new Vector3(40,0,-3),new Vector3(-40,0,-3)});
        route.stops.Add(new TrafficStop{junction=junction,axis=1,distance=40-stopX,exitDistance=60});return route;
    }
    static TrafficCar Car(GameObject root,TrafficSystem traffic,TrafficRoute route,float at,int index){
        var car=new GameObject("corridor NPC "+index).AddComponent<TrafficCar>();car.transform.SetParent(root.transform);car.Initialize(traffic,route,at,index);return car;
    }
    static void Step(params TrafficCar[] cars){foreach(var car in cars)tick.Invoke(car,null);Physics.Simulate(Time.fixedDeltaTime);Physics.SyncTransforms();}
    static void OwnerFirst(){
        var root=new GameObject("owner first corridor fixture");
        try{
            TrafficJunction junction;var traffic=System(root,out junction);var ownerRoute=OwnerRoute(junction);var approachRoute=ApproachRoute(junction,11);
            var owner=Car(root,traffic,ownerRoute,28,0);var approach=Car(root,traffic,approachRoute,12,1);approach.speed=7;
            // A crossing already admitted before the red arrival must keep its reservation.
            junction.reservations.Add(owner);junction.owner=owner;
            Check(!traffic.CanEnter(approach,approachRoute.stops[0]),"fixture approach is red");
            for(int i=0;i<240;i++)Step(approach);
            Check(approach.distance<approachRoute.stops[0].distance-1,"red approach must stop upstream of conflicting reserved corridor, before its painted line");
            Check(!traffic.OccupiesMovement(owner,ownerRoute.stops[0],approach.GetComponent<BoxCollider>()),"red approach body must leave reserved crossing physically clear");
            Check(junction.reservations.Contains(owner),"committed owner reservation must not be discarded");
            for(int i=0;i<500&&(owner.distance<=ownerRoute.stops[0].exitDistance+.1f||junction.reservations.Contains(owner));i++){
                Step(owner,approach);Check(!(junction.reservations.Contains(owner)&&junction.reservations.Contains(approach)),"conflicting movements must never hold simultaneous reservations");
            }
            Check(owner.distance>ownerRoute.stops[0].exitDistance&&!junction.reservations.Contains(owner),"reserved owner physically clears and releases normally");
            // Still red: approach can now reach the real line and establish native stop evidence.
            for(int i=0;i<240;i++)Step(approach);
            Check(Mathf.Abs(approach.distance-approachRoute.stops[0].distance)<.1f&&approach.stoppedAt==junction,"after owner clears, red approach reaches its real stop line and records full stop");
            traffic.manualTime=53;Check(traffic.CanEnter(approach,approachRoute.stops[0]),"fixture approach turns green");
            for(int i=0;i<500&&approach.distance<=approachRoute.stops[0].exitDistance+.1f;i++)Step(approach);
            Check(approach.distance>approachRoute.stops[0].exitDistance,"approach physically resumes after green without teleport or reservation deletion");
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static void ReverseArrival(){
        var root=new GameObject("red first corridor fixture");
        try{
            TrafficJunction junction;var traffic=System(root,out junction);var route=OwnerRoute(junction);var crossing=Car(root,traffic,route,route.stops[0].distance-2,0);
            var redRoute=ApproachRoute(junction,16);var red=Car(root,traffic,redRoute,redRoute.stops[0].distance,1);Physics.SyncTransforms();
            Check(traffic.OccupiesMovement(crossing,route.stops[0],red.GetComponent<BoxCollider>()),"reverse fixture red body occupies curved corridor outside nominal junction box");
            Check(traffic.CanEnter(crossing,route.stops[0]),"reverse fixture crossing signal is green");
            Check(!traffic.MayEnter(crossing,route.stops[0])&&!junction.reservations.Contains(crossing),"reverse arrival must not reserve a crossing already obstructed outside nominal junction bounds");
            UnityEngine.Object.DestroyImmediate(red.gameObject);Physics.SyncTransforms();Check(traffic.MayEnter(crossing,route.stops[0]),"crossing may reserve once reverse-order fixture blocker is removed");
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static void MovingStream(){
        var root=new GameObject("same stream corridor fixture");
        try{
            TrafficJunction junction;var traffic=System(root,out junction);var route=OwnerRoute(junction);
            var leader=Car(root,traffic,route,23,0);var follower=Car(root,traffic,route,13,1);leader.speed=7;follower.speed=7;
            junction.reservations.Add(leader);junction.owner=leader;float before=follower.distance;
            Check(traffic.MovingLeader(follower,leader),"same-route fixture has a moving leader");Step(follower);
            Check(follower.distance>before&&follower.followingTarget>0,"moving same-stream follower retains native following progress");
            Check(junction.reservations.Contains(leader)&&junction.reservations.Contains(follower),"compatible moving stream retains shared reservations");
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    public static void Run(){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;string failures="";
        try{
            try{OwnerFirst();Debug.Log("RESERVED_CORRIDOR_OWNER_FIRST_OK");}catch(Exception e){failures+="owner first: "+e.Message+"\n";}
            try{ReverseArrival();Debug.Log("RESERVED_CORRIDOR_REVERSE_OK");}catch(Exception e){failures+="reverse arrival: "+e.Message+"\n";}
            try{MovingStream();Debug.Log("RESERVED_CORRIDOR_MOVING_STREAM_OK");}catch(Exception e){failures+="moving stream: "+e.Message+"\n";}
            Check(failures.Length==0,failures);Debug.Log("TRAFFIC_RESERVED_CORRIDOR_OK");
        }finally{Physics.autoSimulation=automatic;}
    }
}
