using System;
using System.Runtime.CompilerServices;
using UnityEngine;

// These are native-simulation views, deliberately independent of the old flat motor protocol.
[Serializable] public sealed class PvpNativeFrame {
    public float elapsed,trafficClock;
    public DemandSignalState[] signals;
    public bool finished,full;
    public int recipient;
    public int[] scores,claims;
    public JobOffer[] offers,roadside;
    public PvpNativeActor[] actors;
    public PvpNativeNpc[] npcs;
    public PvpNativeOwn own;
    public StopOccupancySnapshot occupancy;
    public int occupancyStop=-1;
    public AmbientPedestrianSnapshot ambientPedestrians;
    public RoadConditionsSnapshot roadConditions;
}
[Serializable] public sealed class PvpNativeActor {
    public int slot,signal,conditionKind=-1,completed,failed,balance,inputAck,pedalDirection,resetEpoch;
    public Vector3 position,velocity,angularVelocity;
    public Quaternion rotation;
    public float steering,conditionRemaining;
    public string status;
    public bool hazards,assisted,hasFreeRoute,accidentLocked;
    public PvpNativeFreeRoute freeRoute;
    public Vector3[] wheelPositions;
    public Quaternion[] wheelRotations;
}
[Serializable] public sealed class PvpNativeNpc {
    public int index,turn;
    public Vector3 position;
    public Quaternion rotation;
    public float speed;
    public bool visible,brake;
}
[Serializable] public sealed class PvpNativeOwn {public AccidentState accident;
    public int workshopKind;public float workshopRemaining;public string workshopNotice;
    public EconomySnapshot economy;
    public AccountSnapshot account;
    public PassengerJobsSnapshot jobs;
    public LawEvent[] lawEvents;
    public DrivingMannersSnapshot manners;
    public PassengerResult[] passengers;
    public bool hasDestination,freeGuidance;
    public Vector3 destination;
    public string tripId,mannersLastEvent;
    public bool serviceOpen,serviceQueueWaiting;
    public int servicePending=-1,serviceMeal,serviceFuel=-1,conditionKind=-1,pvpMeals,pvpRests;
    public float conditionRemaining;
    public string serviceNotice,serviceDestination,serviceQueueStatus;
    public FacilityQueueView[] serviceFacilities;
}

public static class PvpNativeReplica {
    sealed class Pose {
        readonly PvpNativePoseTimeline timeline=new PvpNativePoseTimeline();
        public void Target(Transform current,Vector3 position,Quaternion rotation,double time,bool snap){timeline.Push(time,position,rotation,snap);if(snap)current.SetPositionAndRotation(position,rotation);}
        public void Draw(Transform target,double time){Vector3 p;Quaternion q;if(timeline.Sample(time,out p,out q))target.SetPositionAndRotation(p,q);}
    }
    sealed class View {
        public bool bound,ownApplied;
        public double received,serverTime,renderTime,lastDraw;public float clock;
        public readonly Pose[] taxis={new Pose(),new Pose(),new Pose(),new Pose()};
        public Pose[] npcs=new Pose[0];
        public PvpNativeNpc[] latestNpcs;
        public Transform[][] wheels=new Transform[4][];
        public readonly PvpNativeWheelView[,] wheelPoses=new PvpNativeWheelView[4,4];
    }
    static readonly ConditionalWeakTable<PvpNativeLan,View> views=new ConditionalWeakTable<PvpNativeLan,View>();
    static View State(PvpNativeLan lan){return views.GetValue(lan,key=>new View());}

    public static PvpNativeFrame Capture(PvpNativeLan lan,int recipient,bool full=true){
        if(lan==null||!lan.IsHost||lan.Match==null)throw new InvalidOperationException("Only the native match host can capture a frame");
        var match=lan.Match;
        if(recipient<0||recipient>=match.Drivers.Count)throw new ArgumentOutOfRangeException("recipient");
        var human=match.Human;
        var frame=new PvpNativeFrame{elapsed=match.Elapsed,trafficClock=human.traffic.Clock,signals=human.traffic.CaptureSignalRuntime(),finished=match.Finished,full=full,recipient=recipient,
            scores=new int[4],claims=new int[human.jobs.stops.Length],offers=match.SharedCalls.Offers,roadside=match.SharedCalls.Roadside,
            actors=new PvpNativeActor[4],npcs=new PvpNativeNpc[human.traffic.cars.Count],
            occupancy=match.PassengerWorld?match.PassengerWorld.State:null,occupancyStop=match.PassengerWorld?match.PassengerWorld.BlockedStop:-1};
        if(frame.occupancy!=null&&frame.occupancy.active&&frame.occupancyStop<0){
            int stopNumber;var stopId=frame.occupancy.stopId;
            if(!string.IsNullOrEmpty(stopId)&&stopId.StartsWith("S",StringComparison.Ordinal)&&int.TryParse(stopId.Substring(1),out stopNumber))frame.occupancyStop=stopNumber-1;
        }
        for(int stop=0;stop<frame.claims.Length;stop++){
            frame.claims[stop]=-1;
            if(match.Ledger.IsClaimed(stop))for(int slot=0;slot<4;slot++)if(match.Ledger.Available(stop,slot)){frame.claims[stop]=slot;break;}
        }
        for(int slot=0;slot<4;slot++){
            var driver=match.Drivers[slot];var taxi=driver.taxi;
            frame.scores[slot]=match.Score(slot);
            var actor=new PvpNativeActor{slot=slot,inputAck=lan.ProcessedInput(slot),pedalDirection=taxi.PedalBrakingDirection,resetEpoch=taxi.ResetEpoch,hasFreeRoute=slot==recipient&&driver.jobs.nav.freeGuidance&&driver.jobs.nav.hdRoute!=null,freeRoute=slot==recipient&&driver.jobs.nav.freeGuidance?PvpNativeFreeRoute.Capture(driver.jobs.nav.hd,driver.jobs.nav.hdRoute):null,position=taxi.body.position,rotation=taxi.body.rotation,velocity=taxi.body.velocity,
                angularVelocity=taxi.body.angularVelocity,steering=taxi.SteeringInput,status=match.DriverStatus(slot),
                signal=driver.signals.direction,hazards=driver.signals.Hazards,assisted=driver.assisted.Active,accidentLocked=driver.accident&&driver.accident.Locked,
                conditionKind=match.ConditionServiceKind(driver),conditionRemaining=match.ServiceRemaining(driver),
                completed=driver.jobs.completed,failed=driver.jobs.failed,balance=driver.jobs.balance,
                wheelPositions=new Vector3[4],wheelRotations=new Quaternion[4]};
            for(int wheel=0;wheel<4;wheel++){
                Vector3 position;Quaternion rotation;taxi.wheels[wheel].GetWorldPose(out position,out rotation);
                actor.wheelPositions[wheel]=PvpNativeWheelView.MountPosition(taxi.transform.InverseTransformPoint(position),taxi.wheels[wheel].transform.localPosition);
                actor.wheelRotations[wheel]=Quaternion.Inverse(taxi.transform.rotation)*rotation*Quaternion.Euler(0,0,90);
            }
            if(PvpNativePerf.Moving&&slot==recipient&&taxi.body.velocity.magnitude>2)PvpNativePerf.Sample("source-render-age-ms",Vector3.Distance(taxi.transform.position,taxi.body.position)/taxi.body.velocity.magnitude*1000);
            frame.actors[slot]=actor;
        }
        for(int index=0;index<frame.npcs.Length;index++){
            var car=human.traffic.cars[index];
            frame.npcs[index]=new PvpNativeNpc{index=index,position=car.PhysicsPosition,rotation=car.PhysicsRotation,
                speed=car.speed,visible=car.Visible,turn=car.CurrentTurn,brake=car.speed<.2f||car.followingTarget<car.speed};
        }
        if(full){
            var driver=match.Drivers[recipient];var jobs=driver.jobs.CaptureLive();
            // Bot sessions intentionally have no campaign-only scenario registry.
            jobs.hasServiceZones=false;jobs.serviceZones=null;jobs.hasChoice=false;jobs.choice=null;
            frame.own=new PvpNativeOwn{workshopKind=driver.damage?driver.damage.PendingServiceKind:0,workshopRemaining=driver.damage?driver.damage.PendingServiceRemaining:0,workshopNotice=driver.damage?driver.damage.Notice:"",accident=driver.accident?driver.accident.Capture():null,economy=driver.business.ledger.Snapshot(),account=driver.business.ledger.Account.Snapshot(),jobs=jobs,
                lawEvents=driver.law.ledger.Events,manners=driver.manners.CaptureLive(),passengers=driver.business.passengers.ToArray(),tripId=driver.law.tripId,mannersLastEvent=driver.manners.lastEvent,
                hasDestination=driver.jobs.nav.HasDestination,freeGuidance=driver.jobs.nav.freeGuidance,destination=driver.jobs.nav.destination,serviceOpen=driver.services.open,servicePending=driver.services.pending,
                serviceNotice=driver.services.notice,serviceDestination=driver.services.destination,serviceMeal=driver.services.SelectedMeal,serviceQueueStatus=driver.services.QueueStatus,serviceQueueWaiting=driver.services.QueueWaiting,serviceFacilities=driver.services.CaptureFacilityQueues(),
                serviceFuel=driver.services.FuelAtPump,conditionKind=match.ConditionServiceKind(driver),conditionRemaining=match.ServiceRemaining(driver),
                pvpMeals=driver.business.ledger.PvpMeals,pvpRests=driver.business.ledger.PvpRests};
            if(human.ambientPedestrians)frame.ambientPedestrians=human.ambientPedestrians.Capture();
            if(human.roadConditions)frame.roadConditions=human.roadConditions.CaptureNetworkView();
        }
        return frame;
    }

    public static void BindClient(PvpNativeLan lan){
        if(lan==null||lan.IsHost||lan.Match==null)return;
        var view=State(lan);if(view.bound)return;
        var match=lan.Match;
        for(int local=0;local<match.Drivers.Count;local++){
            var driver=match.Drivers[local];var taxi=driver.taxi;
            taxi.NetworkReplica=true;taxi.ReplicaVelocity=Vector3.zero;
            taxi.autoThrottle=0;taxi.autoSteer=0;taxi.autoBrake=true;
            foreach(var wheel in taxi.wheels){wheel.motorTorque=0;wheel.brakeTorque=0;wheel.enabled=local==0;}
            taxi.body.velocity=Vector3.zero;taxi.body.angularVelocity=Vector3.zero;
            taxi.NetworkPredicted=local==0;taxi.body.isKinematic=local!=0;taxi.body.interpolation=local==0?RigidbodyInterpolation.Interpolate:RigidbodyInterpolation.None;taxi.body.detectCollisions=true;
            if(local==0)lan.Prediction=new PvpNativePrediction(taxi);
            var pilot=driver.GetComponent<PvpSimulationBot>();if(pilot)pilot.enabled=false;
            driver.assisted.enabled=local==0;driver.assisted.allowAutomated=local==0;
            var list=new System.Collections.Generic.List<Transform>();
            foreach(var child in taxi.GetComponentsInChildren<Transform>())if(child.name=="Tyre visual")list.Add(child);
            view.wheels[local]=list.ToArray();
            for(int i=0;i<4;i++)view.wheelPoses[local,i]=new PvpNativeWheelView();
        }
        if(match.PassengerWorld)match.PassengerWorld.enabled=false;
        foreach(var driver in match.Drivers){driver.roadConditions=match.Human.roadConditions;driver.jobs.nav.conditions=match.Human.roadConditions;}
        var traffic=match.Human.traffic;traffic.manualClock=true;
        var district=traffic.GetComponent<DistrictTraffic>();if(district)district.enabled=false;
        view.npcs=new Pose[traffic.cars.Count];
        for(int index=0;index<traffic.cars.Count;index++){
            var car=traffic.cars[index];car.enabled=false;
            var body=car.GetComponent<Rigidbody>();if(body){body.isKinematic=true;body.detectCollisions=true;}
            view.npcs[index]=new Pose();
        }
        var pedestrians=match.Human.ambientPedestrians;
        if(pedestrians){pedestrians.enabled=false;foreach(var actor in pedestrians.actors)actor.enabled=false;}
        view.bound=true;
    }

    public static void Apply(PvpNativeLan lan,PvpNativeFrame frame){
        if(lan==null||lan.IsHost||lan.Match==null||frame==null)return;
        if(frame.recipient!=lan.LocalSlot||frame.actors==null||frame.actors.Length!=4||frame.scores==null||frame.scores.Length!=4||frame.claims==null)
            throw new ArgumentException("Invalid native match frame");
        BindClient(lan);
        var match=lan.Match;var view=State(lan);bool first=view.received<=0;
        if(!first&&PvpNativePerf.Moving)PvpNativePerf.Sample("moving-packet-gap-ms",(Time.realtimeSinceStartupAsDouble-view.received)*1000);
        var scores=new int[4];var conditions=new int[4];var remaining=new float[4];var claims=new int[frame.claims.Length];
        for(int i=0;i<4;i++)conditions[i]=-1;
        for(int server=0;server<4;server++)scores[lan.ServerToLocal(server)]=frame.scores[server];
        for(int stop=0;stop<claims.Length;stop++)claims[stop]=frame.claims[stop]<0?-1:lan.ServerToLocal(frame.claims[stop]);
        bool wasApplying=PvpNativeLan.ApplyingReplica;PvpNativeLan.ApplyingReplica=true;
        try{
            match.SharedCalls.ApplyNetworkView(frame.offers,frame.roadside);
            if(frame.full&&frame.roadConditions!=null&&match.Human.roadConditions)match.Human.roadConditions.ApplyNetworkView(frame.roadConditions);
            foreach(var actor in frame.actors){
                if(actor==null||actor.slot<0||actor.slot>=4)throw new ArgumentException("Invalid native actor slot");
                int local=lan.ServerToLocal(actor.slot);var driver=match.Drivers[local];var taxi=driver.taxi;
                conditions[local]=actor.conditionKind;remaining[local]=actor.conditionRemaining;
                if(local==0&&lan.Prediction!=null)lan.Prediction.Receive(actor.inputAck,new PvpPredictedMotion{pedalDirection=actor.pedalDirection,resetEpoch=actor.resetEpoch,position=actor.position,rotation=actor.rotation,velocity=actor.velocity,angularVelocity=actor.angularVelocity},first||frame.finished,actor.conditionRemaining>0||actor.accidentLocked||frame.finished);
                else view.taxis[local].Target(taxi.transform,actor.position,actor.rotation,frame.elapsed,first||frame.finished);
                taxi.ReplicaVelocity=actor.velocity;taxi.ReplicaSteering=actor.steering;
                // Read-only UI and navigation use Rigidbody velocity even on the kinematic view.
                // Kinematic view velocity is exposed through TaxiController.ObservedVelocity.
                driver.jobs.completed=actor.completed;driver.jobs.failed=actor.failed;driver.jobs.balance=actor.balance;
                driver.signals.ApplyNetworkView(actor.signal,actor.hazards);
                if(local==0&&actor.hasFreeRoute&&actor.freeRoute!=null)driver.jobs.nav.ApplyNetworkFreeRoute(actor.freeRoute);
                driver.assisted.ApplyNetworkView(actor.assisted,actor.status);
                var wheels=view.wheels[local];
                if(local!=0&&wheels!=null&&actor.wheelPositions!=null&&actor.wheelRotations!=null)
                    for(int i=0;i<Math.Min(wheels.Length,Math.Min(actor.wheelPositions.Length,actor.wheelRotations.Length));i++){
                        view.wheelPoses[local,i].Push(frame.elapsed,actor.wheelPositions[i],actor.wheelRotations[i],first||frame.finished);
                        if(first||frame.finished)view.wheelPoses[local,i].Draw(wheels[i],frame.elapsed);
                    }
            }
            match.ApplyNetworkView(frame.elapsed,frame.finished,scores,claims,conditions,remaining);
            var statuses=new string[4];foreach(var actor in frame.actors)statuses[lan.ServerToLocal(actor.slot)]=actor.status;
            match.ApplyNetworkStatuses(statuses);
            if(match.PassengerWorld)match.PassengerWorld.ApplyNetworkView(frame.occupancy,frame.occupancyStop);
            if(frame.full&&frame.own!=null)ApplyOwn(lan,frame.own);
            // All views must point to the shared stable offer objects, including after RestoreLive.
            foreach(var driver in match.Drivers)driver.jobs.offers=match.SharedCalls.Offers;
            if(frame.npcs!=null){
                if(frame.npcs.Length!=view.npcs.Length)throw new ArgumentException("Native traffic population differs from host");
                view.latestNpcs=frame.npcs;
                foreach(var npc in frame.npcs){
                    if(npc==null||npc.index<0||npc.index>=view.npcs.Length)throw new ArgumentException("Invalid native NPC index");
                    var car=match.Human.traffic.cars[npc.index];
                    view.npcs[npc.index].Target(car.transform,npc.position,npc.rotation,frame.elapsed,first||!npc.visible);
                    car.ApplyNetworkView(car.transform.position,car.transform.rotation,npc.speed,npc.visible,npc.turn,npc.brake,frame.trafficClock);
                }
            }
            if(frame.full&&frame.ambientPedestrians!=null&&match.Human.ambientPedestrians){
                match.Human.ambientPedestrians.Restore(frame.ambientPedestrians);
                match.Human.ambientPedestrians.ReconcileVisibility();
            }
            view.clock=frame.trafficClock;view.serverTime=frame.elapsed;view.received=Time.realtimeSinceStartupAsDouble;if(first){view.renderTime=frame.elapsed-.1;view.lastDraw=view.received;}
            match.Human.traffic.manualTime=frame.trafficClock;match.Human.traffic.RestoreSignalRuntime(frame.signals);
        }finally{PvpNativeLan.ApplyingReplica=wasApplying;}
    }

    static void ApplyOwn(PvpNativeLan lan,PvpNativeOwn own){
        long perf=PvpNativePerf.Start();
        if(own.economy==null||own.account==null||own.jobs==null)throw new ArgumentException("Missing native owner model");
        var driver=lan.Match.Human;
        long stage=PvpNativePerf.Start();driver.business.ledger=EconomyLedger.RestoreCampaign(own.economy,own.account);
        driver.business.ledger.Settings.daySeconds=own.economy.settings.daySeconds;
        driver.business.ledger.EnablePvpProfile();if(driver.accident)driver.accident.Restore(own.accident);
        driver.business.ledger.ApplyNetworkPvpCounters(own.pvpMeals,own.pvpRests);
        PvpNativePerf.End("economy",stage);stage=PvpNativePerf.Start();driver.law.tripId=own.tripId??"";
        // Keep observer identity after bootstrap; a full snapshot is not a new score session.
        var view=State(lan);if(!view.ownApplied)driver.law.ledger=new LawLedger();
        ApplyLawEvents(driver.law.ledger,own.lawEvents);view.ownApplied=true;
        if(own.manners!=null)driver.manners.RestoreLive(own.manners);
        own.jobs.hasServiceZones=false;own.jobs.serviceZones=null;own.jobs.hasChoice=false;own.jobs.choice=null;
        PvpNativePerf.End("law",stage);stage=PvpNativePerf.Start();driver.jobs.RestoreNetworkView(own.jobs,own.hasDestination,own.destination,own.freeGuidance);
        // RestoreNetworkView also restores manners; apply its authoritative feedback afterwards.
        driver.manners.lastEvent=own.mannersLastEvent??"운전 상태 변화";
        PvpNativePerf.End("jobs",stage);stage=PvpNativePerf.Start();if(own.hasDestination&&(!driver.jobs.nav.HasDestination||(driver.jobs.nav.destination-own.destination).sqrMagnitude>.01f))driver.jobs.nav.SetDestination(own.destination);
        PvpNativePerf.End("destination",stage);driver.business.passengers.Clear();if(own.passengers!=null)driver.business.passengers.AddRange(own.passengers);
        if(driver.damage)driver.damage.ApplyWorkshopNetworkView(own.workshopKind,own.workshopRemaining,own.workshopNotice);
        driver.services.ApplyNetworkView(own.serviceOpen,own.servicePending,own.serviceNotice,own.serviceDestination,own.serviceMeal,own.serviceFuel);
        PvpNativePerf.End("own",perf);
        driver.services.ApplyNetworkQueue(own.serviceQueueStatus,own.serviceQueueWaiting);
        driver.services.ApplyNetworkFacilities(own.serviceFacilities);
    }

    public static void ApplyLawEvents(LawLedger ledger,LawEvent[] events){
        // Host incidents are cumulative, including escalation of an existing id.
        if(events!=null)foreach(var entry in events)ledger.RecordIncident(entry);
    }

    public static void TraceMotion(PvpNativeLan lan){
        if(!PvpNativePerf.Moving||lan==null||lan.IsHost||!lan.Match)return;var view=State(lan);var taxi=lan.Match.Human.taxi;var camera=Camera.main;
        PvpNativePerf.Motion(Time.realtimeSinceStartupAsDouble,view.serverTime,view.renderTime,taxi.transform.position,taxi.ObservedVelocity.magnitude,camera?camera.transform.position:Vector3.zero);
    }

    public static void Tick(PvpNativeLan lan){
        if(lan==null||lan.IsHost||!lan.InMatch||lan.Match==null)return;
        var view=State(lan);if(!view.bound||view.received<=0)return;
        double now=Time.realtimeSinceStartupAsDouble,age=System.Math.Max(0,now-view.received),dt=System.Math.Max(0,now-view.lastDraw);view.lastDraw=now;view.renderTime=PvpNativePlaybackClock.Advance(view.renderTime,view.serverTime,age,dt);double playback=lan.Match.Finished?view.serverTime:view.renderTime;
        var match=lan.Match;
        for(int i=0;i<match.Drivers.Count;i++){
            if(i==0&&lan.Prediction!=null)continue;
            view.taxis[i].Draw(match.Drivers[i].taxi.transform,playback);
            var wheels=view.wheels[i];if(wheels!=null)for(int j=0;j<Math.Min(4,wheels.Length);j++)view.wheelPoses[i,j].Draw(wheels[j],playback);
        }
        for(int i=0;i<view.npcs.Length;i++)view.npcs[i].Draw(match.Human.traffic.cars[i].transform,playback);

        // Cap extrapolated signal time if the connection stops delivering snapshots.
        match.Human.traffic.manualTime=view.clock+(match.Finished?0:(float)System.Math.Min(.25,age));
    }
}
