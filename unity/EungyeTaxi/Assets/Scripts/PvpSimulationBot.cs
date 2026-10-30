using UnityEngine;

// Commands pedals on the same WheelCollider taxi as the human. Never advances
// fares by time, relocates a vehicle to a destination or removes shared traffic.
[DefaultExecutionOrder(-100)]
public sealed class PvpSimulationBot : MonoBehaviour {
    public DriveSession Session {get;private set;}
    public float MetresDriven {get;private set;}
    public string Status {get;private set;}
    public string SelectionDiagnostics {get;private set;}
    public float StationarySeconds {get;private set;}
    public float TargetSpeed {get;private set;}
    public string ObstacleReason {get;private set;}
    Collider sensedCollider;
    public string MotionDiagnostics {
        get {
            var s=Session;var t=s.taxi;string obstacles="";
            var zones=s.GetComponent<ServiceLawZones>();string zoneState=zones?" serviceZone="+(zones.Assigned==null?"none":zones.Assigned.junction.name+":"+zones.Assigned.variant)+" serviceLive="+(zones.Live&&zones.Live.Active):" serviceZone=none";
            string npc="";var car=sensedCollider?sensedCollider.GetComponentInParent<TrafficCar>():null;
            if(car&&StationarySeconds>10){
                float error=0,at=0,own=0;var path=curbPath??(s.jobs.nav.hdRoute==null?null:s.jobs.nav.hdRoute.path);
                if(path!=null){at=path.Project(car.transform.position,out error);float ownError;own=curbPath!=null?curbProgress:s.jobs.nav.ProjectVehicle(out ownError);}
                npc=" npc="+car.name+" pos="+car.transform.position+" heading="+car.transform.forward+" speed="+car.speed+" signal="+car.waitingForSignal+" obstacle="+car.waitingForObstacle+" junction="+car.waitingForJunction+" blocked="+car.blockedName+" obstacleCar="+(car.obstacleCar?car.obstacleCar.name:"none")+" routeDeviation="+error+" routeAhead="+(at-own)+" chain="+TrafficChain(car);
            }
            if(StationarySeconds>10)foreach(var collider in Physics.OverlapSphere(t.transform.position+Vector3.up*.5f,3.5f,(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore))if(!collider.transform.IsChildOf(t.transform))obstacles+=collider.name+"/";
            int grounded=0;foreach(var wheel in t.wheels)if(wheel.isGrounded)grounded++;
            return "stationary="+StationarySeconds.ToString("0")+" speed="+t.Speed.ToString("0.00")+" target="+TargetSpeed+" obstacle="+ObstacleReason+" throttle="+t.autoThrottle+" brake="+t.autoBrake+" serviceBrake="+t.autoServiceBrake+" torque="+t.wheels[2].motorTorque+" grounded="+grounded+" assistance="+s.assisted.Status+" merge="+s.traffic.MergeDiagnostics(s)+" lock="+s.simulationMatch.ServiceRemaining(s)+" fatigue="+s.business.ledger.Driver.fatigue+" controlLost="+s.business.DrowsyControlsLost+zoneState+" nearby="+obstacles+npc;
        }
    }
    static string TrafficChain(TrafficCar car){var seen=new System.Collections.Generic.HashSet<TrafficCar>();string value="";for(int i=0;i<8&&car;i++){if(!seen.Add(car)){value+="cycle:"+car.name;break;}value+="["+car.name+" pos="+car.transform.position+" speed="+car.speed+" signal="+car.waitingForSignal+" junction="+car.waitingForJunction+" blocked="+car.blockedName+(car.waitingForSignal||car.waitingForJunction?TrafficSignalDiagnostics(car):"")+"]";car=car.obstacleCar;}return value;}
    // Read-only native decision evidence: neither time nor signal state is changed.
    public static string TrafficSignalDiagnostics(TrafficCar car){
        if(!car||car.route==null||!car.system)return "";
        string value="";var traffic=car.system;
        foreach(var stop in car.route.stops){
            float remaining=stop.distance-car.distance;if(remaining<-.05f||remaining>40)continue;
            var j=stop.junction;var direction=car.route.graphPath==null?car.route.direction:car.route.Tangent(Mathf.Max(0,stop.distance));
            int turn=stop.Movement(car.route),sign=traffic.Sign(direction,stop.axis,j);
            string lane=null;var graph=car.district?car.district.graph:null;
            if(car.route.graphPath!=null&&graph!=null)lane=graph.edges[car.route.graphPath.EdgeAt(Mathf.Max(0,stop.distance))].id;
            var observation=traffic.Observe(j,stop.axis,sign,direction,car.gameObject);
            var movement=j.editorProgram!=null?j.editorProgram.Movement(stop.axis,sign,turn,traffic.Clock,lane):traffic.MovementSignal(j,stop.axis,turn,sign);
            value+=" stop="+j.name+" remaining="+remaining+" distance="+stop.distance+" exit="+stop.exitDistance+" axis="+stop.axis+" sign="+sign+" turn="+turn+" lane="+lane+" nearestLane="+traffic.EditorLaneId(car.transform.position,car.transform.forward)+" stoppedAt="+(car.stoppedAt==null?"none":car.stoppedAt.name)+" movement="+movement+" forward="+observation.forward+" pedestrian="+observation.pedestrian+" pedestrianGreen="+observation.pedestrianGreen+" pedestrianClear="+observation.PedestrianClear+" trafficClear="+observation.trafficClear+" yieldClear="+traffic.YieldClear(j,direction,turn,car.gameObject)+" canEnter="+traffic.CanEnter(car,stop);
            if(turn==UturnMovement.Turn)value+=" uturnPermitted="+traffic.UturnPermitted(j,stop.axis,sign,lane);
            value+=" scenarioSignal="+j.scenarioSignal+" scenarioForward="+j.scenarioForward+" scenarioFlashing="+j.scenarioFlashing+" leftMode="+j.leftTurnMode+" editorProgram="+(j.editorProgram!=null)+ReservationDiagnostics(j)+MovementOccupancyDiagnostics(car,stop);
        }
        return value;
    }
    static string ReservationDiagnostics(TrafficJunction junction){
        string value=" owner="+(junction.owner?junction.owner.name:"none")+" holders={";
        foreach(var holder in junction.reservations)value+=ReservationCar(holder,junction)+";";
        value+="} requests={";
        var requests=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<TrafficCar,float>>(junction.requests);
        requests.Sort((a,b)=>a.Value.CompareTo(b.Value));
        for(int i=0;i<Mathf.Min(8,requests.Count);i++)value+=requests[i].Value+":"+ReservationCar(requests[i].Key,junction)+";";
        return value+"}";
    }
    static string MovementOccupancyDiagnostics(TrafficCar car,TrafficStop stop){
        var traffic=car.system;var j=stop.junction;string value=" occupiedMovement={";
        var bounds=traffic.ReservedMovementBounds(car,stop);
        foreach(var hit in Physics.OverlapBox(bounds.center,bounds.extents,Quaternion.identity,(1<<8)|(1<<9))){
            var other=hit.GetComponentInParent<TrafficCar>();if(other==car||traffic.MovingLeader(car,other))continue;
            if(traffic.OccupiesMovement(car,stop,hit))value+=hit.name+"@"+hit.transform.position+";";
        }
        value+="} exitClear="+traffic.ExitClear(car,stop)+" exitBlockers={";
        foreach(var hit in Physics.OverlapBox(car.route.Position(stop.exitDistance)+Vector3.up*.85f,new Vector3(1.05f,.55f,4.5f),Quaternion.LookRotation(car.route.Tangent(stop.exitDistance)),(1<<8)|(1<<9)|1)){
            var other=hit.GetComponentInParent<TrafficCar>();if(other!=car&&!traffic.MovingLeader(car,other))value+=hit.name+"@"+hit.transform.position+";";
        }
        return value+"}";
    }
    static string ReservationCar(TrafficCar car,TrafficJunction junction){
        if(!car)return "destroyed";
        string value=TrafficCarMotion(car);
        if(car.route!=null)foreach(var stop in car.route.stops)if(stop.junction==junction&&stop.exitDistance>=car.distance-.1f){value+=" remaining="+(stop.distance-car.distance)+" exitRemaining="+(stop.exitDistance-car.distance)+" turn="+stop.Movement(car.route);break;}
        return value;
    }
    static string TrafficCarMotion(TrafficCar car){
        if(!car)return "destroyed";
        string value=car.name+" enabled="+car.enabled+" visible="+car.Visible+" pos="+car.transform.position+" heading="+car.transform.forward+" speed="+car.speed+" routeAt="+car.distance+" signal="+car.waitingForSignal+" junction="+car.waitingForJunction+" entryBlocked="+car.entryBlocked+" obstacle="+car.waitingForObstacle+" blocked="+car.blockedName+" obstacleCar="+(car.obstacleCar?car.obstacleCar.name:"none")+" baseline="+car.baselineTarget+" following="+car.followingTarget;
        if(car.route!=null){
            int edge=car.route.graphPath==null?-1:car.route.graphPath.EdgeAt(car.distance);
            value+=" routeLength="+car.route.length+" goalEdge="+car.route.goalEdge+" goalAt="+car.route.goalAt+" currentEdge="+edge+" currentTurn="+car.CurrentTurn;
            if(edge>=0&&car.district&&car.district.graph!=null)value+=" currentLane="+car.district.graph.edges[edge].id;
            foreach(var stop in car.route.stops)if(stop.exitDistance>=car.distance-.1f&&stop.distance-car.distance<40){value+=" activeStop="+stop.junction.name+" remaining="+(stop.distance-car.distance)+" exitRemaining="+(stop.exitDistance-car.distance);break;}
        }
        return value;
    }
    public static string TrafficCarDiagnostics(TrafficCar car){
        string value=TrafficCarMotion(car)+TrafficSignalDiagnostics(car)+" obstacleChain={";
        var seen=new System.Collections.Generic.HashSet<TrafficCar>();if(car)seen.Add(car);var next=car?car.obstacleCar:null;
        for(int i=0;i<8&&next;i++){if(!seen.Add(next)){value+="cycle:"+next.name;break;}value+="["+TrafficCarMotion(next)+"]";next=next.obstacleCar;}
        return value+"}";
    }
    float nextOffer,routeLostSeconds;
    public int RouteRecoveries {get;private set;}
    PvpSimulationBotServices services;
    AssistedRoadNetwork.Path curbPath;
    float curbProgress;
    int curbGoal=-1;
    TripState curbState;
    bool curbDeparture;
    sealed class StopEvidence {public int axis,sign;public bool crossed;}
    readonly System.Collections.Generic.Dictionary<TrafficJunction,StopEvidence> stoppedJunctions=new System.Collections.Generic.Dictionary<TrafficJunction,StopEvidence>();
    public void Initialize(DriveSession session) { Session=session;if(session.traffic)session.traffic.RegisterMergeDriver(session); }
    public static float PickupDistance(HdRoadGraph graph,Vector3 position,Vector3 direction,MapStop stop){
        // Native graph routes intentionally omit zero-length paths. Already
        // being in the legal stopping envelope is a valid pickup, too.
        if(stop.CanStop(position,direction,0))return 0;
        return PassengerJobs.DirectedDistance(graph,position,direction,stop);
    }
    public static float ThrottleFor(float speed,float target){return target>.15f&&speed<target-.05f?.5f:0;}
    public bool RefreshTerminalCalls(){var jobs=Session.jobs;if(jobs.Active||Session.RideResultOpen)return false;bool terminal=jobs.trip.State==TripState.Complete||jobs.trip.State==TripState.Failed;jobs.PrepareForNextCall();if(terminal)jobs.Generate();return terminal;}
    void Stop(string reason) {Session.taxi.autoThrottle=0;Session.taxi.autoBrake=true;Status=reason;}
    void FixedUpdate() {
        var s=Session;if(!s||!s.simulationMatch||s.simulationMatch.Finished||Time.timeScale<=0)return;
        var taxi=s.taxi;var jobs=s.jobs;
        StationarySeconds=taxi.body.velocity.magnitude<.15f?StationarySeconds+Time.fixedDeltaTime:0;
        if(s.requests.SafeToRespond()){if(s.requests.HasPendingChoice)s.requests.Respond(false);if(jobs.BehaviorChoicesOpen)jobs.RespondBehavior(0);}
        MetresDriven+=taxi.body.velocity.magnitude*Time.fixedDeltaTime;
        if(s.simulationMatch.ServiceLocked(s)){Stop("서비스 진행 중");return;}
        if(s.business.ledger.Driver.drowsiness.Blackout){Stop("졸음 긴급 휴식");s.business.RequestEmergencyHome();return;}
        if(s.damage.Disabled){Stop("차량 고장 · 견인/수리");if(taxi.body.velocity.magnitude<.56f){jobs.ResolveVehicleFailure();if(!jobs.Active){if(s.damage.AtRepairBay)s.damage.Repair();else s.damage.Tow();}}return;}
        RefreshTerminalCalls();
        if(!services)services=gameObject.AddComponent<PvpSimulationBotServices>();
        if(services.Tick(s)){Status=services.Status;return;}
        taxi.autoServiceBrake=0;
        if(!jobs.Active) {
            curbPath=null;
            Stop("호출 선택 중");if(Time.time<nextOffer)return;nextOffer=Time.time+2;
            if(jobs.trip.State!=TripState.Idle)return;
            int best=-1;float distance=float.PositiveInfinity;
            SelectionDiagnostics="accept="+s.business.CanAccept+" state="+jobs.trip.State+" ";
            // Shared claims are checked at selection and atomically at acceptance.
            for(int i=0;i<jobs.offers.Length;i++) {
                var offer=jobs.offers[i];string rejection=jobs.AcceptanceRejection(i);if(offer==null||rejection!=""){SelectionDiagnostics+="["+i+":"+rejection+"]";continue;}
                var stop=jobs.stops[offer.pickup];float at;int edge=jobs.nav.hd.Nearest(taxi.transform.position,taxi.transform.forward,out at);
                float pickupDistance=PickupDistance(jobs.nav.hd,taxi.transform.position,taxi.transform.forward,stop);
                SelectionDiagnostics+="["+i+":"+offer.pickup+">"+offer.drop+" edge="+edge+" goal="+stop.laneEdge+" route="+pickupDistance.ToString("0")+"]";
                if(pickupDistance<distance){distance=pickupDistance;best=i;}
            }
            if(best>=0&&jobs.Accept(best)){s.assisted.SetActive(true);Status="승객 픽업 주행";}else jobs.Generate();
            return;
        }
        int goal=jobs.trip.State==TripState.ToPickup?jobs.current.pickup:jobs.PassengerViaPending?jobs.PassengerViaStop:jobs.current.drop;
        if(curbPath!=null&&(curbGoal!=goal||curbState!=jobs.trip.State))curbPath=null;
        if(jobs.At(goal)) {
            curbPath=null;
            Stop("안전 정차 · 승하차");s.assisted.SetActive(false);
            jobs.Interact();
            if(jobs.Active&&jobs.trip.State==TripState.OnTrip&&!jobs.trip.ArrivalLocked)s.assisted.SetActive(true);
            return;
        }
        var nav=jobs.nav;var routePath=nav.hdRoute;var stopGoal=jobs.stops[goal];
        if(stopGoal.CanStop(taxi.transform.position,taxi.transform.forward,0)){Stop("승하차 지점 감속");return;}
        // Road navigation may disappear once the taxi leaves its lane toward
        // the authored kerb. Finish that physical manoeuvre before rerouting.
        if(curbPath!=null){FollowCurb();return;}
        if(routePath==null){
            Stop("연결 경로 확인 중");nav.Refresh();routeLostSeconds+=Time.fixedDeltaTime;
            if(routeLostSeconds>20&&jobs.CancelUnboardedCall("픽업 연결 불가 · 공용 호출 다시 선택")){routeLostSeconds=0;RouteRecoveries++;}
            return;
        }
        routeLostSeconds=0;
        float error;float progress=nav.ProjectVehicle(out error);float remain=routePath.path.length-progress;
        float offset=Vector3.Distance(routePath.path.Position(routePath.path.length),stopGoal.Kerb);
        // Native navigation ends on the road lane; curb geometry is a separate
        // physical stopping contract. Pull over before arrival, then merge
        // back using pedals/steering after boarding. Never snap the Rigidbody.
        if(remain<28&&offset>.65f&&offset<12&&Vector3.Dot(taxi.transform.forward,stopGoal.Direction)>.7f){
            BeginCurb(stopGoal.Kerb,stopGoal.Direction,goal,false);FollowCurb();return;
        }
        if(!s.assisted.Active&&!s.assisted.SetActive(true)){
            if(error<12){float join=Mathf.Min(routePath.path.length,progress+20);BeginCurb(routePath.path.Position(join),routePath.path.Tangent(join),goal,true);FollowCurb();return;}
            Stop("차로 복귀 필요");return;
        }
        var edgeNow=nav.hd.edges[routePath.EdgeAt(progress)];
        float bend=Vector3.Angle(routePath.path.Tangent(progress),routePath.path.Tangent(Mathf.Min(routePath.path.length,progress+12)));
        // Match the native contract ETA's cruise speeds; all existing road,
        // curvature, arrival and obstacle limits still reduce this target.
        float target=bend>12||edgeNow.turn!=0||edgeNow.change!=0?4.5f:8f;
        target=Mathf.Min(target,Mathf.Sqrt(Mathf.Max(0,remain-1.5f)*2.5f));
        float limit=s.traffic.EditorSpeedLimit(taxi.transform.position,taxi.transform.forward);if(limit>0)target=Mathf.Min(target,limit/3.6f);
        float barrier=ObstacleDistance();
        target=Mathf.Min(target,Mathf.Sqrt(Mathf.Max(0,barrier-3)*2.5f));
        TargetSpeed=target;taxi.autoThrottle=ThrottleFor(taxi.Speed,target);
        taxi.autoBrake=target<.15f||taxi.Speed>target+.25f;
        Status=target<.15f?"신호·교통 대기":jobs.trip.State==TripState.ToPickup?"승객 픽업 주행":"승객 운송 중";
    }
    void BeginCurb(Vector3 end,Vector3 direction,int goal,bool departure){
        var taxi=Session.taxi;var start=taxi.transform.position;start.y=end.y;
        var points=CurbPathPoints(start,taxi.transform.forward,end,direction);
        curbPath=new AssistedRoadNetwork.Path(points);curbProgress=0;curbGoal=goal;curbState=Session.jobs.trip.State;curbDeparture=departure;Session.assisted.SetActive(false);
        float lateral=Vector3.Dot(end-start,TrafficSystem.Right(taxi.transform.forward));if(Mathf.Abs(lateral)>.5f)Session.signals.Toggle(lateral>0?1:-1);
    }
    public static Vector3[] CurbPathPoints(Vector3 start,Vector3 startDirection,Vector3 end,Vector3 direction){
        var before=end-direction*Mathf.Min(6,Mathf.Max(0,Vector3.Dot(end-start,direction)*.35f));float length=Vector3.Distance(start,before);
        var control1=start+startDirection*Mathf.Max(4,length*.4f);
        var control2=before-direction*Mathf.Max(4,length*.4f);
        var points=new Vector3[31];
        for(int i=0;i<=24;i++){float t=i/24f,u=1-t;points[i]=u*u*u*start+3*u*u*t*control1+3*u*t*t*control2+t*t*t*before;}
        for(int i=25;i<points.Length;i++)points[i]=Vector3.Lerp(before,end,(i-24)/6f);
        return points;
    }
    void FollowCurb(){
        var s=Session;var taxi=s.taxi;float error;
        curbProgress=curbPath.ProjectRange(taxi.transform.position,Mathf.Max(0,curbProgress-2),Mathf.Min(curbPath.length,curbProgress+7),out error);
        if(error>4){
            Stop("승하차 접근 경로 재탐색");curbPath=null;s.jobs.nav.Refresh();RouteRecoveries++;
            if(StationarySeconds>20)s.jobs.CancelUnboardedCall("픽업 접근 불가 · 공용 호출 다시 선택");return;
        }
        float remain=curbPath.length-curbProgress;
        var target=curbPath.Position(Mathf.Min(curbPath.length,curbProgress+3));target.y=taxi.transform.position.y;
        var local=taxi.transform.InverseTransformPoint(target);
        float angle=Mathf.Atan2(2*2.6f*local.x,Mathf.Max(1,local.x*local.x+local.z*local.z))*Mathf.Rad2Deg;
        taxi.autoSteer=Mathf.Clamp(angle/DriveMath.ManualSteerAngle(taxi.Speed*3.6f),-1,1);
        float speed=Mathf.Min(2.5f,Mathf.Sqrt(Mathf.Max(0,remain-.4f)*1.8f));
        speed=Mathf.Min(speed,Mathf.Sqrt(Mathf.Max(0,ObstacleDistance()-3)*2.5f));
        TargetSpeed=speed;
        taxi.autoThrottle=Mathf.Clamp((speed-taxi.Speed)*.5f,0,.45f);taxi.autoBrake=speed<.08f;
        taxi.autoServiceBrake=taxi.Speed>speed+.1f?Mathf.Clamp((taxi.Speed-speed)*.7f,.08f,.6f):0;
        Status=curbDeparture?"연석 출발 · 차로 합류":"연석 접근 · 실제 승하차 정차";
        if(curbDeparture&&remain<1.2f&&taxi.body.velocity.magnitude<.56f){curbPath=null;taxi.autoServiceBrake=0;s.jobs.nav.Refresh();s.assisted.SetActive(true);}
    }
    public float ObstacleDistance(bool signals=true) {
        var s=Session;var taxi=s.taxi;float result=100;var position=taxi.transform.position;var heading=taxi.transform.forward;
        ObstacleReason="clear";
        string mergeStatus;float mergeGap=s.traffic.MergeApproachGap(s,out mergeStatus);
        if(mergeGap+3<result){result=mergeGap+3;ObstacleReason="merge:"+mergeStatus;}
        sensedCollider=null;
        foreach(var hit in Physics.SphereCastAll(position+Vector3.up*.45f+heading*2.2f,.65f,heading,35,(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore)) {
            if(hit.collider.transform.IsChildOf(taxi.transform)||hit.normal.y>.8f)continue;
            if(hit.distance<result){result=hit.distance;sensedCollider=hit.collider;ObstacleReason="collider:"+hit.collider.name+"@"+hit.distance.ToString("0.00");}
        }
        foreach(var j in s.traffic.junctions) {
            int axis=Mathf.Abs(Vector3.Dot(heading,s.traffic.AxisDirection(j,0)))>.7f?0:1;
            var axisDirection=s.traffic.AxisDirection(j,axis);int sign=Vector3.Dot(heading,axisDirection)>0?1:-1;
            var delta=j.center-position;float along=Vector3.Dot(delta,axisDirection)*sign;
            float lateral=Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(axisDirection)));
            float distance=along-j.StopDistance(axis,sign)-2.4f;
            if(distance<-.5f||distance>35||lateral>j.halfWidth[axis]+2){stoppedJunctions.Remove(j);continue;}
            StopEvidence evidence;stoppedJunctions.TryGetValue(j,out evidence);
            if(evidence!=null&&(evidence.axis!=axis||evidence.sign!=sign||evidence.crossed&&distance>=0)){stoppedJunctions.Remove(j);evidence=null;}
            // Preserve the current departure through the small crossing tolerance,
            // but consume its stop on any return to the approach side of the line.
            if(evidence!=null&&distance<0)evidence.crossed=true;
            if(!signals)continue;
            var observation=s.traffic.Observe(j,axis,sign,axisDirection*sign,taxi.gameObject);
            if(evidence==null&&distance<2&&taxi.body.velocity.magnitude<.1f&&StationarySeconds>=1){evidence=new StopEvidence{axis=axis,sign=sign,crossed=distance<0};stoppedJunctions[j]=evidence;}
            float routeError;float progress=s.jobs.nav.ProjectVehicle(out routeError);
            int turn=PlannedTurn(s.jobs.nav,j.name,progress);
            string laneId=s.jobs.nav.hdRoute==null?null:s.jobs.nav.hd.edges[s.jobs.nav.hdRoute.EdgeAt(progress)].id;
            SignalColor movement=j.editorProgram!=null?j.editorProgram.Movement(axis,sign,turn,s.traffic.Clock,laneId):s.traffic.MovementSignal(j,axis,turn,sign);
            bool yieldClear=s.traffic.YieldClear(j,axisDirection*sign,turn,taxi.gameObject);
            bool allowed=NativeMovementPermitted(j,observation,turn,evidence!=null,movement,yieldClear,turn==UturnMovement.Turn&&s.traffic.UturnPermitted(j,axis,sign,laneId));
            if(!allowed&&distance+3<result){result=distance+3;sensedCollider=null;ObstacleReason="junction:"+j.name+" axis="+axis+" sign="+sign+" turn="+turn+" lane="+laneId+" distance="+distance.ToString("0.00")+" signal="+movement+" pedestrianClear="+observation.PedestrianClear+" yield="+yieldClear;}
        }
        return result;
    }
    public static bool NativeMovementPermitted(TrafficJunction junction,RoadObservation observation,int turn,bool stopped,SignalColor movement,bool yieldClear,bool uturnPermitted=false){
        if(turn==UturnMovement.Turn)return uturnPermitted&&observation.PedestrianClear&&yieldClear;
        if(!junction.signalized&&!junction.scenarioSignal)return observation.PedestrianClear&&yieldClear;
        if(junction.editorProgram!=null)return movement==SignalColor.Green&&observation.PedestrianClear&&yieldClear;
        if(junction.scenarioFlashing)return (movement!=SignalColor.Red||stopped)&&observation.PedestrianClear&&yieldClear;
        if(turn>0)return observation.MayTurnRight(stopped);
        return movement==SignalColor.Green&&observation.PedestrianClear&&(turn==0||yieldClear);
    }
    public static int PlannedTurn(RoadNavigation nav,string junction,float progress){
        if(nav==null||nav.hdRoute==null)return 0;
        float begin=0;
        for(int i=0;i<nav.hdRoute.edges.Count;i++){
            var edge=nav.hd.edges[nav.hdRoute.edges[i]];
            if(nav.hdRoute.ends[i]>=progress-.1f&&begin-progress<90&&edge.ruleId==junction&&edge.turn!=0)return edge.turn;
            begin=nav.hdRoute.ends[i];if(begin-progress>=90)break;
        }
        return 0;
    }
}
