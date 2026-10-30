using System;
using System.Reflection;
using UnityEngine;

// Player7's observed pairs, restarted before convergence. Initial fixture poses
// are followed only by the production NPC Tick and native physics simulation.
public static class PvpNpcMergeTests {
    static readonly MethodInfo tick=typeof(TrafficCar).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic);
    static void Check(bool value,string why){if(!value)throw new Exception("PVP_NPC_MERGE: "+why);}
    static string Detail(Exception e){while(e is TargetInvocationException&&e.InnerException!=null)e=e.InnerException;return e.ToString();}
    sealed class Fixture : IDisposable {
        public GameObject root;public TrafficSystem traffic;public DistrictTraffic district;public MapData map;
        public Fixture(){
            root=new GameObject("native authored merge fixture");
            map=PvpCityMap.Create();traffic=root.AddComponent<TrafficSystem>();
            // Editor executeMethod does not run this MonoBehaviour's Awake.
            typeof(TrafficSystem).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(traffic,null);
            traffic.Build(map,false);traffic.manualClock=true;
            district=root.AddComponent<DistrictTraffic>();district.Initialize(traffic,map,false);
        }
        public void Dispose(){UnityEngine.Object.DestroyImmediate(root);}
    }
    static TrafficCar Car(GameObject root,Fixture fixture,TrafficRoute route,float at,int id){
        Check(route!=null,"recorded native route exists "+id);
        var car=new GameObject("merge fixture "+id).AddComponent<TrafficCar>();car.transform.SetParent(root.transform);
        car.Initialize(fixture.traffic,route,at,id);car.district=fixture.district;fixture.traffic.cars.Add(car);
        foreach(var collider in car.GetComponentsInChildren<Collider>())if(collider.gameObject!=car.gameObject)UnityEngine.Object.DestroyImmediate(collider);
        return car;
    }
    static void Pair(Fixture fixture,bool bay,float throughBack,bool reverse,float throughSpeed=0,float mergeSpeed=0){
        var root=new GameObject("arrival phase "+throughBack);root.transform.SetParent(fixture.root.transform);
        try{
            var district=fixture.district;var traffic=fixture.traffic;
            TrafficRoute throughRoute,mergeRoute;float throughAt,mergeAt;
            if(bay){
                throughRoute=district.CreateRoute(2399,33.07626f,2408,32.19606f);throughAt=541.1207f-throughBack;
                mergeRoute=district.CreateRoute(2402,32.33162f,2441,32.90477f);mergeAt=0;
            }else{
                throughRoute=district.CreateRoute(2423,32.64895f,2414,32.60079f);throughAt=1674.165f-throughBack;
                mergeRoute=district.CreateRoute(2402,32.33162f,2411,32.64586f);mergeAt=1283.292f-20;
            }
            var through=Car(root,fixture,throughRoute,throughAt,bay?61:16);
            var merging=Car(root,fixture,mergeRoute,mergeAt,bay?23:53);
            through.speed=throughSpeed;merging.speed=mergeSpeed;
            Physics.SyncTransforms();
            var a=through.GetComponent<BoxCollider>();var b=merging.GetComponent<BoxCollider>();float deepest=0;
            for(int frame=0;frame<1000;frame++){
                traffic.manualTime=frame*Time.fixedDeltaTime;
                tick.Invoke(reverse?merging:through,null);tick.Invoke(reverse?through:merging,null);
                Physics.Simulate(Time.fixedDeltaTime);Physics.SyncTransforms();
                Vector3 direction;float depth;
                if(Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out direction,out depth))deepest=Mathf.Max(deepest,depth);
            }
            float throughProgress=through.distance-throughAt,mergeProgress=merging.distance-mergeAt;
            Debug.Log("PVP_NATIVE_MERGE_SAMPLE kind="+(bay?"bay":"lane")+" phase="+throughBack+" reverse="+reverse+" initialSpeed="+throughSpeed+"/"+mergeSpeed+" through="+throughProgress+" merging="+mergeProgress+" overlap="+deepest+" throughPos="+through.transform.position+" mergePos="+merging.transform.position+" throughBlocked="+through.blockedName+" mergeBlocked="+merging.blockedName);
            Check(deepest<.005f,"native body footprints never overlap during "+(bay?"bay":"lane")+" merge: "+deepest);
            Check(throughProgress>50&&mergeProgress>50,"both native cars must physically clear "+(bay?"bay":"lane")+" merge without mutual stop; through="+throughProgress+" merging="+mergeProgress+" phase="+throughBack+" reverse="+reverse+" initialSpeed="+throughSpeed+"/"+mergeSpeed);
        }finally{
            foreach(var car in root.GetComponentsInChildren<TrafficCar>()){fixture.traffic.Release(car);fixture.traffic.cars.Remove(car);}
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
    static void Cases(bool bay){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;string errors="";
        try{
            using(var fixture=new Fixture()){
                foreach(float phase in bay?new[]{20f,27f}:new[]{20f,17f,23f})foreach(bool reverse in new[]{false,true})try{Pair(fixture,bay,phase,reverse);}catch(Exception e){errors+=Detail(e)+"\n";}
                if(bay)foreach(bool reverse in new[]{false,true})try{Pair(fixture,true,24.1f,reverse,9,9);}catch(Exception e){errors+=Detail(e)+"\n";}
            }
            Check(errors.Length==0,errors);
            Debug.Log(bay?"PVP_NPC_BAY_MERGE_OK":"PVP_NPC_LANE_MERGE_OK");
        }finally{Physics.autoSimulation=automatic;}
    }
    static void Steps(Fixture fixture,TrafficCar a,TrafficCar b,int frames,float from=0){
        for(int frame=0;frame<frames;frame++){
            fixture.traffic.manualTime=from+frame*Time.fixedDeltaTime;
            tick.Invoke(a,null);if(b)tick.Invoke(b,null);Physics.Simulate(Time.fixedDeltaTime);Physics.SyncTransforms();
            if(b){Vector3 direction;float depth;Check(!Physics.ComputePenetration(a.GetComponent<BoxCollider>(),a.transform.position,a.transform.rotation,b.GetComponent<BoxCollider>(),b.transform.position,b.transform.rotation,out direction,out depth)||depth<.005f,"guard fixture must preserve native body separation");}
        }
    }
    public static void Parking(){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;
        try{using(var fixture=new Fixture()){
            Check(fixture.traffic.AuthoredMergeCount>0&&NativeTrafficMergePolicy.Supports(fixture.map),"authored metadata survives native traffic Build");
            string original=fixture.map.mapId;fixture.map.mapId="eungye";Check(!NativeTrafficMergePolicy.Supports(fixture.map),"Eungye never activates PvP merge policy");fixture.map.mapId=original;
            Check(Attribute.IsDefined(typeof(MapData).GetField("pvpTrafficMerges"),typeof(NonSerializedAttribute)),"merge metadata never enters campaign map JSON");
            var path=fixture.district.graph.paths[2402];var point=path.Position(32.33162f);
            var parked=GameObject.CreatePrimitive(PrimitiveType.Cube);parked.name="manual player parked at passenger kerb";parked.transform.SetParent(fixture.root.transform);parked.layer=8;parked.transform.position=point+Vector3.up*.85f;parked.transform.rotation=Quaternion.LookRotation(path.Tangent(32.33162f));parked.transform.localScale=new Vector3(2,1.3f,5.4f);
            var route=fixture.district.CreateRoute(2399,33.07626f,2408,32.19606f);var through=Car(fixture.root,fixture,route,541.1207f-45,61);Physics.SyncTransforms();
            Steps(fixture,through,null,1000);
            Check(through.totalTravel>80,"a legitimately parked mid-bay player footprint must leave through traffic moving: "+through.totalTravel);
            Debug.Log("PVP_NPC_PARKED_BAY_OK through="+through.totalTravel+" metadata="+fixture.traffic.AuthoredMergeCount);
        }}finally{Physics.autoSimulation=automatic;}
    }
    public static void Guards(){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;
        try{using(var fixture=new Fixture()){
            var throughRoute=fixture.district.CreateRoute(2423,32.64895f,2414,32.60079f);
            var mergeRoute=fixture.district.CreateRoute(2402,32.33162f,2411,32.64586f);
            var through=Car(fixture.root,fixture,throughRoute,1674.165f-20,16);
            var merging=Car(fixture.root,fixture,mergeRoute,1283.292f-20,53);
            var zone=Array.Find(fixture.map.pvpTrafficMerges,z=>z.id=="lane:city:3:2>2:2:head");Check(zone!=null,"recorded lane merge metadata exists");
            float error;float exitAt=throughRoute.ProjectNear(zone.exit,through.distance+60,out error);
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="manual player exit footprint";obstacle.transform.SetParent(fixture.root.transform);obstacle.layer=8;obstacle.transform.position=throughRoute.Position(exitAt)+Vector3.up*.85f;obstacle.transform.rotation=Quaternion.LookRotation(throughRoute.Tangent(exitAt));obstacle.transform.localScale=new Vector3(2.2f,1.3f,5.4f);Physics.SyncTransforms();
            Steps(fixture,through,merging,150);
            Check(through.totalTravel<.05f&&merging.totalTravel<.05f,"physical player footprint at exit prevents admission into a blocked merge");
            UnityEngine.Object.DestroyImmediate(obstacle);Physics.SyncTransforms();
            Steps(fixture,through,merging,100,3);
            string before=fixture.traffic.MergeDiagnostics(through);Check(before.Contains("owns:"),"physically entered owner is recorded before route continuation");
            float local;int edge=fixture.district.graph.Nearest(through.transform.position,through.transform.forward,out local);Check(edge>=0,"continuation fixture remains on native graph");
            var next=fixture.district.CreateRoute(edge,local,through.route.goalEdge,through.route.goalAt);Check(next!=null,"native onward route exists");
            through.Continue(next);
            string wait;fixture.traffic.MergeApproachGap(merging,out wait);
            Check(fixture.traffic.MergeDiagnostics(through).Contains("owns:")&&wait.Contains(through.name),"native Continue does not release a physically occupied merge");
            Steps(fixture,through,merging,1000,5);
            Check(through.totalTravel>50&&merging.totalTravel>50,"both native cars progress after actual obstruction removal and physical owner clearance");
            Check(!fixture.traffic.MergeDiagnostics(through).Contains("owns:"),"no stale ownership after physical exit");
            Debug.Log("PVP_NPC_MERGE_GUARDS_OK through="+through.totalTravel+" merging="+merging.totalTravel);
        }}finally{Physics.autoSimulation=automatic;}
    }

    static DriveSession Driver(Fixture fixture,TrafficRoute route,float at){
        var root=new GameObject("registered native AI taxi");root.transform.SetParent(fixture.root.transform);root.SetActive(false);
        var driver=root.AddComponent<DriveSession>();driver.IsSimulationBot=true;driver.enabled=false;driver.traffic=fixture.traffic;
        driver.taxi=root.AddComponent<TaxiController>();driver.taxi.Create(route.Position(at)+Vector3.up*.65f,Quaternion.LookRotation(route.Tangent(at)));driver.taxi.body.isKinematic=true;driver.taxi.automated=true;
        driver.jobs=root.AddComponent<PassengerJobs>();driver.jobs.session=driver;driver.jobs.enabled=false;
        driver.jobs.nav=root.AddComponent<RoadNavigation>();driver.jobs.nav.enabled=false;driver.jobs.nav.hd=fixture.district.graph;driver.jobs.nav.vehicle=driver.taxi.transform;driver.jobs.nav.hdRoute=route.graphPath;driver.jobs.nav.active=true;
        root.AddComponent<PvpSimulationBot>().Initialize(driver);root.SetActive(true);Physics.SyncTransforms();return driver;
    }
    public static void DriverIntent(){
        using(var fixture=new Fixture()){
            var route=fixture.district.CreateRoute(2423,32.64895f,2414,32.60079f);
            var mergingRoute=fixture.district.CreateRoute(2402,32.33162f,2411,32.64586f);
            var upstream=Driver(fixture,route,1674.165f-40);string reason;
            fixture.traffic.MergeApproachGap(upstream,out reason);
            Check(fixture.traffic.MergeDiagnostics(upstream).Contains("owns:"),"upstream AI fixture acquires an unentered grant");
            upstream.jobs.nav.Clear();fixture.traffic.MergeApproachGap(upstream,out reason);
            Check(!fixture.traffic.MergeDiagnostics(upstream).Contains("owns:"),"unentered registered AI grant must release after native nav.Clear removes its departure intent");
            UnityEngine.Object.DestroyImmediate(upstream.gameObject);Physics.SyncTransforms();
            var committed=Driver(fixture,route,1674.165f-20);
            fixture.traffic.MergeApproachGap(committed,out reason);Check(fixture.traffic.MergeDiagnostics(committed).Contains("owns:"),"committed AI fixture acquires ownership");
            committed.jobs.nav.Clear();
            var contender=Car(fixture.root,fixture,mergingRoute,1283.292f-20,53);
            float gap=fixture.traffic.MergeApproachGap(contender,out reason);
            Check(gap<1&&reason.Contains(committed.taxi.name)&&fixture.traffic.MergeDiagnostics(committed).Contains("owns:"),"committed physical AI owner must retain protection across native nav.Clear");
            Debug.Log("PVP_NPC_MERGE_DRIVER_INTENT_OK");
        }
    }

    public static void Lane(){Cases(false);}
    public static void Bay(){Cases(true);}
    public static void BayExact(){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;string errors="";
        try{using(var fixture=new Fixture())foreach(bool reverse in new[]{false,true})try{Pair(fixture,true,24,reverse);}catch(Exception e){errors+=Detail(e)+"\n";}Check(errors.Length==0,errors);Debug.Log("PVP_NPC_BAY_EXACT_OK");}
        finally{Physics.autoSimulation=automatic;}
    }
    public static void BayMoving(){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;string errors="";
        try{
            using(var fixture=new Fixture())foreach(float throughSpeed in new[]{4.5f,9f})foreach(float mergeSpeed in new[]{4.5f,9f})foreach(float phase in new[]{21f,22f,23f,24f,25f,26f,27f,28f,29f,30f,31f,32f,33f,34f,35f,36f,37f,38f})try{Pair(fixture,true,phase,false,throughSpeed,mergeSpeed);}catch(Exception e){errors+=Detail(e)+"\n";}
            Check(errors.Length==0,errors);Debug.Log("PVP_NPC_BAY_MOVING_OK");
        }finally{Physics.autoSimulation=automatic;}
    }
    public static void BayFine(){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;
        try{using(var fixture=new Fixture())foreach(float speed in new[]{9f,4.5f})for(int phase=0;phase<=20;phase++)Pair(fixture,true,24+phase*.1f,false,speed,speed);Debug.Log("PVP_NPC_BAY_FINE_OK");}
        finally{Physics.autoSimulation=automatic;}
    }
    public static void Run(){
        string errors="";
        try{Lane();}catch(Exception e){errors+="lane: "+Detail(e)+"\n";}
        try{Bay();}catch(Exception e){errors+="bay: "+Detail(e)+"\n";}
        try{Parking();}catch(Exception e){errors+="parking: "+Detail(e)+"\n";}
        try{Guards();}catch(Exception e){errors+="guards: "+Detail(e)+"\n";}
        try{DriverIntent();}catch(Exception e){errors+="driver intent: "+Detail(e)+"\n";}
        Check(errors.Length==0,errors);Debug.Log("PVP_NPC_MERGES_OK");
    }
}
