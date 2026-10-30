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
    static int StopEdge(Fixture fixture,int index){return fixture.map.stops[index].laneEdge;}
    static void Pair(Fixture fixture,bool bay,float throughBack,bool reverse,float throughSpeed=0,float mergeSpeed=0){
        var root=new GameObject("arrival phase "+throughBack);root.transform.SetParent(fixture.root.transform);
        try{
            var district=fixture.district;var traffic=fixture.traffic;
            TrafficRoute throughRoute,mergeRoute;float throughAt,mergeAt;
            if(bay){
                throughRoute=district.CreateRoute(StopEdge(fixture,2),33.07626f,StopEdge(fixture,5),32.19606f);throughAt=541.1207f-throughBack;
                mergeRoute=district.CreateRoute(StopEdge(fixture,3),32.33162f,StopEdge(fixture,16),32.90477f);mergeAt=0;
            }else{
                throughRoute=district.CreateRoute(StopEdge(fixture,10),32.64895f,StopEdge(fixture,7),32.60079f);throughAt=1674.165f-throughBack;
                mergeRoute=district.CreateRoute(StopEdge(fixture,3),32.33162f,StopEdge(fixture,6),32.64586f);mergeAt=1283.292f-20;
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
            var path=fixture.district.graph.paths[StopEdge(fixture,3)];var point=path.Position(32.33162f);
            var parked=GameObject.CreatePrimitive(PrimitiveType.Cube);parked.name="manual player parked at passenger kerb";parked.transform.SetParent(fixture.root.transform);parked.layer=8;parked.transform.position=point+Vector3.up*.85f;parked.transform.rotation=Quaternion.LookRotation(path.Tangent(32.33162f));parked.transform.localScale=new Vector3(2,1.3f,5.4f);
            var route=fixture.district.CreateRoute(StopEdge(fixture,2),33.07626f,StopEdge(fixture,5),32.19606f);var through=Car(fixture.root,fixture,route,541.1207f-45,61);Physics.SyncTransforms();
            Steps(fixture,through,null,1000);
            Check(through.totalTravel>80,"a legitimately parked mid-bay player footprint must leave through traffic moving: "+through.totalTravel);
            Debug.Log("PVP_NPC_PARKED_BAY_OK through="+through.totalTravel+" metadata="+fixture.traffic.AuthoredMergeCount);
        }}finally{Physics.autoSimulation=automatic;}
    }
    public static void Guards(){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;
        try{using(var fixture=new Fixture()){
            var throughRoute=fixture.district.CreateRoute(StopEdge(fixture,10),32.64895f,StopEdge(fixture,7),32.60079f);
            var mergeRoute=fixture.district.CreateRoute(StopEdge(fixture,3),32.33162f,StopEdge(fixture,6),32.64586f);
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
            var route=fixture.district.CreateRoute(StopEdge(fixture,10),32.64895f,StopEdge(fixture,7),32.60079f);
            var mergingRoute=fixture.district.CreateRoute(StopEdge(fixture,3),32.33162f,StopEdge(fixture,6),32.64586f);
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

    static Vector3 ExitPoint(Fixture fixture,TrafficRoute route,float at,NativeTrafficMergeZone zone){
        float firstError;float first=fixture.district.graph.paths[route.graphPath.edges[0]].Project(route.Position(0),out firstError);
        float best=float.PositiveInfinity;
        for(int k=0;k<route.graphPath.edges.Count;k++)foreach(var interval in zone.intervals){
            if(!interval.clear||interval.edge!=route.graphPath.edges[k])continue;
            float origin=k==0?-first:route.graphPath.ends[k-1],end=origin+interval.end;
            if(end>=at&&end<best)best=end;
        }
        Check(!float.IsInfinity(best),"queue fixture has an authored exit on native route");return route.Position(best);
    }
    static void QueueCase(Fixture fixture,bool blockedExit,bool lateFront,bool reverse){
        const string id="lane:city:3:2>2:2:head";
        var root=new GameObject("native queue "+blockedExit+"/"+lateFront+"/"+reverse);root.transform.SetParent(fixture.root.transform);
        try{
            var traffic=fixture.traffic;traffic.manualTime=0;
            var frontRoute=fixture.district.CreateRoute(StopEdge(fixture,4),32.88552f,StopEdge(fixture,6),32.64586f);
            var rearRoute=fixture.district.CreateRoute(StopEdge(fixture,17),32.22271f,StopEdge(fixture,0),32.93151f);
            var zone=Array.Find(fixture.map.pvpTrafficMerges,z=>z.id==id);Check(zone!=null,"recorded queue zone exists");
            GameObject obstacle=null;
            if(blockedExit){
                var frontExit=ExitPoint(fixture,frontRoute,1820.988f,zone);var rearExit=ExitPoint(fixture,rearRoute,2903.78f,zone);
                Check(Vector3.Distance(frontExit,rearExit)>3,"front/rear fixture uses distinct merge exit lanes");
                obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="front exit player footprint";obstacle.transform.SetParent(root.transform);obstacle.layer=8;obstacle.transform.position=frontExit+Vector3.up*.85f;obstacle.transform.rotation=Quaternion.LookRotation(zone.direction);obstacle.transform.localScale=new Vector3(2.1f,1.3f,5.4f);
            }
            TrafficCar front=null,middle=null,rear;
            string reason;
            if(lateFront){
                rear=Car(root,fixture,rearRoute,2903.78f,62);Physics.SyncTransforms();
                traffic.MergeApproachGap(rear,out reason);
                Check(traffic.MergeDiagnostics(rear).Contains("owns:"+id+" "),"late-front fixture starts with a real unentered rear grant");
                middle=Car(root,fixture,frontRoute,1815.138f,35);front=Car(root,fixture,frontRoute,1820.988f,38);
            }else{
                front=Car(root,fixture,frontRoute,1820.988f,38);middle=Car(root,fixture,frontRoute,1815.138f,35);rear=Car(root,fixture,rearRoute,2903.78f,62);
            }
            Physics.SyncTransforms();
            traffic.MergeApproachGap(front,out reason);traffic.MergeApproachGap(rear,out reason);
            Debug.Log("PVP_NATIVE_QUEUE_SAMPLE blockedExit="+blockedExit+" lateFront="+lateFront+" reverse="+reverse+" front="+traffic.MergeDiagnostics(front)+" rear="+traffic.MergeDiagnostics(rear));
            Check(!traffic.MergeDiagnostics(rear).Contains("owns:"+id+" "),"an unentered rear grant must never hold a same-approach front queue, even if the front exit is blocked or the front arrives after the grant");
            if(obstacle){UnityEngine.Object.DestroyImmediate(obstacle);Physics.SyncTransforms();}
            var actors=reverse?new[]{rear,middle,front}:new[]{front,middle,rear};
            for(int frame=0;frame<1800;frame++){
                traffic.manualTime=frame*Time.fixedDeltaTime;
                foreach(var car in actors)tick.Invoke(car,null);
                Physics.Simulate(Time.fixedDeltaTime);Physics.SyncTransforms();
                for(int a=0;a<actors.Length;a++)for(int b=a+1;b<actors.Length;b++){
                    Vector3 direction;float depth;
                    Check(!Physics.ComputePenetration(actors[a].GetComponent<BoxCollider>(),actors[a].transform.position,actors[a].transform.rotation,actors[b].GetComponent<BoxCollider>(),actors[b].transform.position,actors[b].transform.rotation,out direction,out depth)||depth<.005f,"queued merge keeps all native bodies separated");
                }
            }
            Check(front.totalTravel>50&&middle.totalTravel>50&&rear.totalTravel>50,"all three native queued cars physically progress after front exit clears: "+front.totalTravel+"/"+middle.totalTravel+"/"+rear.totalTravel);
        }finally{foreach(var car in root.GetComponentsInChildren<TrafficCar>()){fixture.traffic.Release(car);fixture.traffic.cars.Remove(car);}UnityEngine.Object.DestroyImmediate(root);}
    }
    public static void Queue(){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;string errors="";
        try{using(var fixture=new Fixture()){
            foreach(bool blocked in new[]{false,true})foreach(bool late in new[]{false,true})foreach(bool reverse in new[]{false,true})try{QueueCase(fixture,blocked,late,reverse);}catch(Exception e){errors+=Detail(e)+"\n";}
        }Check(errors.Length==0,errors);Debug.Log("PVP_NPC_MERGE_QUEUE_OK");}
        finally{Physics.autoSimulation=automatic;}
    }
    public static void QueueHorizon(){
        using(var fixture=new Fixture()){
            var frontRoute=fixture.district.CreateRoute(StopEdge(fixture,4),32.88552f,StopEdge(fixture,6),32.64586f);
            var rearRoute=fixture.district.CreateRoute(StopEdge(fixture,17),32.22271f,StopEdge(fixture,0),32.93151f);
            var front=Car(fixture.root,fixture,frontRoute,1820.988f-25,38);
            var rear=Car(fixture.root,fixture,rearRoute,2903.78f-(34-11.7f),62);rear.speed=12.1f;
            Physics.SyncTransforms();string reason;fixture.traffic.MergeApproachGap(rear,out reason);
            Check(!fixture.traffic.MergeDiagnostics(rear).Contains("owns:lane:city:3:2>2:2:head "),"a slower physical front queue cannot disappear merely because it is beyond its shorter request horizon");
            Debug.Log("PVP_NPC_MERGE_QUEUE_HORIZON_OK");
        }
    }
    public static void FutureWindow(){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;
        try{using(var fixture=new Fixture()){
            int first=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:1:head");
            int second=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:2:head");
            int firstEnd=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:1");
            int secondEnd=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:2");
            Check(first>=0&&second>=0&&firstEnd>=0&&secondEnd>=0,"adjacent authored lane windows exist");
            var aRoute=fixture.district.CreateRoute(first,0,firstEnd,fixture.district.graph.paths[firstEnd].length-1);
            var bRoute=fixture.district.CreateRoute(second,0,secondEnd,fixture.district.graph.paths[secondEnd].length-1);
            var a=Car(fixture.root,fixture,aRoute,20,1);Physics.SyncTransforms();string reason;
            fixture.traffic.MergeApproachGap(a,out reason);Check(fixture.traffic.MergeDiagnostics(a).Contains("owns:lane:city:0:3>1:3:head "),"first-window fixture has a committed owner");
            var b=Car(fixture.root,fixture,bRoute,30,2);Physics.SyncTransforms();fixture.traffic.MergeApproachGap(b,out reason);
            Check(fixture.traffic.MergeDiagnostics(b).Contains("owns:lane:city:0:3>1:3:head "),"physically disjoint native lanes own the first window simultaneously");
            Steps(fixture,a,b,1800);
            Check(a.totalTravel>80&&b.totalTravel>80,"both native lanes progress across adjacent overlapping windows");
            Debug.Log("PVP_NPC_MERGE_FUTURE_WINDOW_OK");
        }}finally{Physics.autoSimulation=automatic;}
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
    public static void CommittedReroute(){
        using(var fixture=new Fixture()){
            int headA=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:1:head");
            int headB=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:2:head");
            int endA=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:1");
            int endB=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:2");
            var aRoute=fixture.district.CreateRoute(headA,0,endA,fixture.district.graph.paths[endA].length-1);
            var bRoute=fixture.district.CreateRoute(headB,0,endB,fixture.district.graph.paths[endB].length-1);
            var driver=Driver(fixture,aRoute,20);var neighbor=Car(fixture.root,fixture,bRoute,20,2);string reason;
            fixture.traffic.MergeApproachGap(driver,out reason);fixture.traffic.MergeApproachGap(neighbor,out reason);
            const string id="owns:lane:city:0:3>1:3:head ";
            Check(fixture.traffic.MergeDiagnostics(driver).Contains(id)&&fixture.traffic.MergeDiagnostics(neighbor).Contains(id),"reroute fixture starts with two compatible committed lane grants");
            // Traffic-aware navigation may replace hdRoute while already inside
            // the window. It must not inherit permission to cross the next lane.
            int change=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:1:head:change:2");
            var changed=fixture.district.graph.Via(headA,0,change,endB,fixture.district.graph.paths[endB].length-1);
            Check(changed!=null,"changed native route through the occupied first window exists");
            bool crosses=false;foreach(int edge in changed.edges)crosses|=fixture.map.hdEdges[edge].change!=0;
            Check(crosses,"changed native route really contains a lane change");driver.jobs.nav.hdRoute=changed;
            float gap=fixture.traffic.MergeApproachGap(driver,out reason);
            Check(gap<.1f,"committed reroute cannot enter a changed corridor occupied by a compatible original neighbor: "+gap+" "+reason);
            Check(fixture.traffic.MergeDiagnostics(driver).Contains(id)&&fixture.traffic.MergeDiagnostics(neighbor).Contains(id),"denied reroute retains both physically occupied original corridors");
            driver.jobs.nav.Clear();fixture.traffic.MergeApproachGap(driver,out reason);
            Check(fixture.traffic.MergeDiagnostics(driver).Contains(id),"clearing denied reroute does not abandon committed physical occupancy");
            driver.jobs.nav.hdRoute=changed;driver.jobs.nav.active=true;
            fixture.traffic.cars.Remove(neighbor);UnityEngine.Object.DestroyImmediate(neighbor.gameObject);Physics.SyncTransforms();fixture.traffic.manualTime=.02f;
            gap=fixture.traffic.MergeApproachGap(driver,out reason);
            Check(gap>5,"changed corridor is admitted once the conflicting physical neighbor has cleared: "+gap+" "+reason);
            Debug.Log("PVP_NATIVE_COMMITTED_REROUTE_OK");
        }
    }
    public static void Convoy(){
        bool automatic=Physics.autoSimulation;Physics.autoSimulation=false;
        try{using(var fixture=new Fixture()){
            int start=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:1:head");
            int end=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:1");
            var route=fixture.district.CreateRoute(start,0,end,fixture.district.graph.paths[end].length-1);
            var front=Car(fixture.root,fixture,route,20,1);var rear=Car(fixture.root,fixture,route,10,2);front.speed=rear.speed=4.5f;Physics.SyncTransforms();string reason;
            fixture.traffic.MergeApproachGap(front,out reason);fixture.traffic.manualTime=.02f;fixture.traffic.MergeApproachGap(rear,out reason);
            const string id="owns:lane:city:0:3>1:3:head ";
            Check(fixture.traffic.MergeDiagnostics(front).Contains(id)&&fixture.traffic.MergeDiagnostics(rear).Contains(id),"same authored straight stream follows a committed moving leader without waiting for the whole window to clear");
            float minimum=float.PositiveInfinity;int stopped=0;
            for(int i=0;i<600;i++){Steps(fixture,front,rear,1,.04f+i*Time.fixedDeltaTime);minimum=Mathf.Min(minimum,Vector3.Distance(front.transform.position,rear.transform.position));if(rear.speed<.1f)stopped++;}
            Check(front.totalTravel>50&&rear.totalTravel>50&&minimum>=4.2f&&stopped<20,"native convoy keeps continuous progress and body headway: front="+front.totalTravel+" rear="+rear.totalTravel+" gap="+minimum+" stopped="+stopped);
            Debug.Log("PVP_NATIVE_CONVOY_OK minHeadway="+minimum+" rearStoppedFrames="+stopped);
        }}finally{Physics.autoSimulation=automatic;}
    }
    static void AddMovementPaths(HdRoadGraph graph,System.Collections.Generic.Dictionary<int,NativeTrafficMergeInterval> intervals,int edge,System.Collections.Generic.List<Vector3> prefix,System.Collections.Generic.List<Vector3[]> result,int depth){
        Check(depth<16,"authored merge movements are acyclic");var interval=intervals[edge];var points=new System.Collections.Generic.List<Vector3>(prefix);var path=graph.paths[edge];
        int count=Mathf.Max(2,Mathf.CeilToInt((interval.end-interval.begin)/2)+1);
        for(int i=0;i<count;i++)points.Add(path.Position(Mathf.Lerp(interval.begin,interval.end,(float)i/(count-1))));
        bool continued=false;if(!interval.clear)foreach(int next in graph.edges[edge].next)if(intervals.ContainsKey(next)){continued=true;AddMovementPaths(graph,intervals,next,points,result,depth+1);}
        if(!continued)result.Add(points.ToArray());
    }
    public static void Corridors(){
        var a=new[]{Vector3.zero,new Vector3(0,0,40)};
        var b=new[]{new Vector3(3.2f,0,0),new Vector3(3.2f,0,40)};
        Check(!NativeTrafficMergePolicy.CorridorsConflict(a,b),"parallel 3.2m lanes must be compatible");
        Check(NativeTrafficMergePolicy.CorridorsConflict(a,new[]{new Vector3(3.2f,0,0),new Vector3(0,0,40)}),"lane change converging onto through lane must yield");
        Check(NativeTrafficMergePolicy.CorridorsConflict(a,new[]{new Vector3(-10,0,20),new Vector3(10,0,20)}),"crossing paths conflict even between sampling points");
        Check(!NativeTrafficMergePolicy.CorridorsConflict(a,new[]{new Vector3(-10,0,45),new Vector3(10,0,45)}),"separated paths remain independent");
        var map=PvpCityMap.Create();var graph=new HdRoadGraph(map);int pairs=0,compatible=0,conflicting=0;
        foreach(var zone in map.pvpTrafficMerges){
            var paths=new System.Collections.Generic.List<Vector3[]>();
            var intervals=new System.Collections.Generic.Dictionary<int,NativeTrafficMergeInterval>();var following=new System.Collections.Generic.HashSet<int>();
            foreach(var interval in zone.intervals){Check(interval.laneId==map.hdEdges[interval.edge].id,"authored interval preserves full lane identity");intervals[interval.edge]=interval;}
            foreach(var interval in zone.intervals)if(!interval.clear)foreach(int next in graph.edges[interval.edge].next)if(intervals.ContainsKey(next))following.Add(next);
            foreach(var interval in zone.intervals)if(!following.Contains(interval.edge))AddMovementPaths(graph,intervals,interval.edge,new System.Collections.Generic.List<Vector3>(),paths,0);
            Check(paths.Count>0,"every authored zone has full connected movements");
            foreach(var points in paths)Check(NativeTrafficMergePolicy.CorridorsConflict(points,points),"every authored full movement conflicts with itself");
            for(int i=0;i<paths.Count;i++)for(int j=i+1;j<paths.Count;j++){
                bool conflict=NativeTrafficMergePolicy.CorridorsConflict(paths[i],paths[j]);
                Check(conflict==NativeTrafficMergePolicy.CorridorsConflict(paths[j],paths[i]),"all authored movement pair conflicts are symmetric");
                if(Vector3.Distance(paths[i][paths[i].Length-1],paths[j][paths[j].Length-1])<.1f)Check(conflict,"movements sharing a destination conflict");
                pairs++;if(conflict)conflicting++;else compatible++;
            }
        }
        Check(pairs>500&&compatible>0&&conflicting>0,"whole authored map contains both compatible and conflicting movement pairs");
        Debug.Log("PVP_NATIVE_CORRIDORS_OK pairs="+pairs+" compatible="+compatible+" conflicting="+conflicting);
    }
    public static void PriorWindow(){
        using(var fixture=new Fixture()){
            int start=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:1:head");
            int end=Array.FindIndex(fixture.map.hdEdges,e=>e.id=="city:0:3>1:3:lane:1");
            var route=fixture.district.CreateRoute(start,0,end,fixture.district.graph.paths[end].length-1);
            var a=Car(fixture.root,fixture,route,20,1);string reason;Physics.SyncTransforms();fixture.traffic.MergeApproachGap(a,out reason);
            var b=Car(fixture.root,fixture,route,10,2);b.speed=15;Physics.SyncTransforms();fixture.traffic.MergeApproachGap(b,out reason);
            Check(!fixture.traffic.MergeDiagnostics(b).Contains("owns:lane:city:0:3>1:3:part:2 "),"denied same-corridor rear cannot reserve a later overlapping window");
            Debug.Log("PVP_NATIVE_PRIOR_WINDOW_OK");
        }
    }
    public static void Run(){
        string errors="";
        try{CommittedReroute();}catch(Exception e){errors+="committed reroute: "+Detail(e)+"\n";}
        try{Convoy();}catch(Exception e){errors+="convoy: "+Detail(e)+"\n";}
        try{Corridors();}catch(Exception e){errors+="corridors: "+Detail(e)+"\n";}
        try{PriorWindow();}catch(Exception e){errors+="prior window: "+Detail(e)+"\n";}
        try{Lane();}catch(Exception e){errors+="lane: "+Detail(e)+"\n";}
        try{Bay();}catch(Exception e){errors+="bay: "+Detail(e)+"\n";}
        try{Parking();}catch(Exception e){errors+="parking: "+Detail(e)+"\n";}
        try{Guards();}catch(Exception e){errors+="guards: "+Detail(e)+"\n";}
        try{DriverIntent();}catch(Exception e){errors+="driver intent: "+Detail(e)+"\n";}
        try{Queue();}catch(Exception e){errors+="queue: "+Detail(e)+"\n";}
        try{QueueHorizon();}catch(Exception e){errors+="queue horizon: "+Detail(e)+"\n";}
        try{FutureWindow();}catch(Exception e){errors+="future window: "+Detail(e)+"\n";}
        Check(errors.Length==0,errors);Debug.Log("PVP_NPC_MERGES_OK");
    }
}
