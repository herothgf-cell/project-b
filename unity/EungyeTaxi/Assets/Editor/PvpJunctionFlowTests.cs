using System;
using System.Reflection;
using UnityEngine;

// Final-gate regression for the legacy nominal right-turn corridor disagreeing
// with the actual selected route. This is a controlled geometry fixture, not a
// reproduction of the user's unlocated live junction.
public static class PvpJunctionFlowTests {
    static void Check(bool value,string detail){if(!value)throw new Exception("PVP_JUNCTION_FLOW: "+detail);}
    static TrafficCar Car(GameObject root,TrafficSystem traffic,TrafficRoute route,float at,int id){
        var car=new GameObject("junction flow "+id).AddComponent<TrafficCar>();car.transform.SetParent(root.transform);car.Initialize(traffic,route,at,id);car.respawn=false;return car;
    }
    static TrafficRoute Right(TrafficJunction junction){
        var points=new System.Collections.Generic.List<Vector3>{new Vector3(1.75f,0,-50),new Vector3(1.75f,0,-20)};
        var a=points[1];var b=new Vector3(20,0,-5.25f);var c=a+Vector3.forward*12;var d=b-Vector3.right*12;
        for(int i=1;i<=32;i++){float t=i/32f,u=1-t;points.Add(u*u*u*a+3*u*u*t*c+3*u*t*t*d+t*t*t*b);}
        points.Add(new Vector3(70,0,-5.25f));
        var route=new TrafficRoute{turn=1};route.SetPoints(points.ToArray());route.stops.Add(new TrafficStop{junction=junction,axis=0,turn=1,distance=30,exitDistance=route.length-15});return route;
    }
    static void BotTurnSensor(int turn){
        var root=new GameObject("native bot turn sensor "+turn);var sessionObject=new GameObject("inactive bot session");sessionObject.transform.SetParent(root.transform);sessionObject.SetActive(false);
        try{
            var map=PvpCityMap.Create();var graph=new HdRoadGraph(map);
            int edge=Array.FindIndex(map.hdEdges,e=>e.ruleId=="city-junction:1:1"&&e.turn==turn);
            Check(edge>=0,"native turn exists "+turn);
            var path=graph.Route(edge,0,map.hdEdges[edge].next[0],40);Check(path!=null,"native turn continues onto actual outgoing lane");
            var session=sessionObject.AddComponent<DriveSession>();session.IsSimulationBot=true;
            // Keep the session inactive to avoid its normal world bootstrap, but
            // the sensor's real shape must participate in the physics scene.
            var taxiObject=new GameObject("active sensor taxi");taxiObject.transform.SetParent(root.transform);
            session.taxi=taxiObject.AddComponent<TaxiController>();session.taxi.enabled=false;
            session.taxi.body=taxiObject.AddComponent<Rigidbody>();session.taxi.body.isKinematic=true;
            var taxiBox=taxiObject.AddComponent<BoxCollider>();taxiBox.center=new Vector3(0,.42f,0);taxiBox.size=new Vector3(1.8f,.7f,4);
            session.traffic=sessionObject.AddComponent<TrafficSystem>();session.traffic.mainDirection=Vector3.forward;
            session.jobs=sessionObject.AddComponent<PassengerJobs>();session.jobs.nav=sessionObject.AddComponent<RoadNavigation>();
            session.jobs.nav.vehicle=taxiObject.transform;session.jobs.nav.hd=graph;session.jobs.nav.hdRoute=path;
            var bot=sessionObject.AddComponent<PvpSimulationBot>();bot.Initialize(session);
            float progress=turn<0?6:5;var heading=path.path.Tangent(progress);var position=path.path.Position(progress)+Vector3.up*.46f;
            session.taxi.body.position=position;taxiObject.transform.SetPositionAndRotation(position,Quaternion.LookRotation(heading));
            var npcRoute=new TrafficRoute();var outside=TrafficSystem.Right(heading)*(turn>0?-1:1);
            var npcPosition=path.path.Position(progress)+heading*7.5f+outside*1.5f;
            npcRoute.SetPoints(new[]{npcPosition,npcPosition+heading*50});var npc=Car(root,session.traffic,npcRoute,0,9);Physics.SyncTransforms();
            RaycastHit oldHit;
            Check(Physics.SphereCast(position+Vector3.up*.45f+heading*2.2f,.65f,heading,out oldHit,35,1<<9,QueryTriggerInteraction.Ignore)&&oldHit.distance<=3.01f,"legacy straight ray produces zero target on native turn "+turn+" hit="+oldHit.distance);
            float sensed=bot.ObstacleDistance();
            Check(sensed>30,"stopped tangent NPC clear of selected native turn does not strand bot "+turn+" sensed="+sensed+" reason="+bot.ObstacleReason);
            // The same native NPC, placed on the selected lane ahead, must still
            // stop this bot. No object is removed or ignored by identity.
            var blocked=path.path.Position(progress+6);npc.transform.SetPositionAndRotation(blocked,Quaternion.LookRotation(path.path.Tangent(progress+6)));npc.GetComponent<Rigidbody>().position=blocked;npc.GetComponent<Rigidbody>().rotation=npc.transform.rotation;Physics.SyncTransforms();
            float blockedDistance=bot.ObstacleDistance();
            Debug.Log("PVP_BOT_NATIVE_TURN_BLOCKER turn="+turn+" distance="+blockedDistance+" reason="+bot.ObstacleReason+" taxiActive="+taxiBox.gameObject.activeInHierarchy+" npcActive="+npc.gameObject.activeInHierarchy+" taxi="+taxiObject.transform.position+" npc="+npc.transform.position);
            Check(blockedDistance<=3,"actual selected-lane blocker retains a full stop "+turn+" distance="+blockedDistance+" reason="+bot.ObstacleReason);
            Debug.Log("PVP_BOT_NATIVE_TURN_SENSOR_OK turn="+turn+" lane="+map.hdEdges[edge].id+" oldHit="+oldHit.distance+" curvedClear="+sensed);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    public static void Run(){
        BotTurnSensor(-1);BotTurnSensor(1);
        var root=new GameObject("offset right turn yielding regression");bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;
        try{
            var traffic=root.AddComponent<TrafficSystem>();typeof(TrafficSystem).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(traffic,null);
            traffic.mainDirection=Vector3.forward;traffic.manualClock=true;traffic.manualTime=2;
            var junction=new TrafficJunction{name="offset fixture",direction=Vector3.forward,halfWidth=new[]{10f,10f},laneCount=new[]{4,4},scenarioSignal=true,scenarioForward=SignalColor.Green,scenarioOpposingGreen=true,leftTurnMode=LeftTurnMode.ProtectedShared};traffic.junctions.Add(junction);
            var right=Right(junction);var subject=Car(root,traffic,right,30,0);
            var parallel=new TrafficRoute();parallel.SetPoints(new[]{new Vector3(10,0,-10.75f),new Vector3(70,0,-10.75f)});
            parallel.stops.Add(new TrafficStop{junction=junction,axis=1,turn=0,distance=0,exitDistance=30});var other=Car(root,traffic,parallel,0,1);Physics.SyncTransforms();
            var legacy=typeof(TrafficSystem).GetMethod("RightTurnConflict",BindingFlags.Instance|BindingFlags.NonPublic);
            Check((bool)legacy.Invoke(traffic,new object[]{junction,Vector3.forward,other}),"nominal outer curve reproduces false conflict with offset selected route");
            Check(traffic.YieldClear(junction,Vector3.forward,1,subject.gameObject),"disjoint selected right route does not yield to nominal outer corridor");
            Check(traffic.MayEnter(subject,right.stops[0]),"disjoint physical corridor and exit admit right turn");
            var tick=typeof(TrafficCar).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic);float deepest=0;
            for(int frame=0;frame<900;frame++){
                tick.Invoke(subject,null);Physics.Simulate(Time.fixedDeltaTime);Physics.SyncTransforms();
                Vector3 direction;float depth;var a=subject.GetComponent<BoxCollider>();var b=other.GetComponent<BoxCollider>();
                if(Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out direction,out depth))deepest=Mathf.Max(deepest,depth);
            }
            Check(subject.distance>right.stops[0].exitDistance,"actual native Tick physically clears selected turn");Check(deepest<.005f,"stationary disjoint car never overlaps turning body");
            // Even if route metadata is disjoint, present occupancy must retain
            // the independent physical guard; never infer collision clearance.
            subject.Place(30);other.Place(0);other.transform.position=right.Position(45);other.GetComponent<Rigidbody>().position=other.transform.position;Physics.SyncTransforms();
            Check(!traffic.MayEnter(subject,right.stops[0]),"actual body inside selected turn still blocks admission");
            Debug.Log("PVP_JUNCTION_FLOW_OK nominal mismatch, disjoint right progress, present occupancy, overlap="+deepest);
        }finally{UnityEngine.Object.DestroyImmediate(root);Physics.autoSimulation=automatic;}
    }
}
