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
            string npc="";var car=sensedCollider?sensedCollider.GetComponentInParent<TrafficCar>():null;
            if(car&&StationarySeconds>10){
                float error=0,at=0,own=0;var path=curbPath??(s.jobs.nav.hdRoute==null?null:s.jobs.nav.hdRoute.path);
                if(path!=null){at=path.Project(car.transform.position,out error);float ownError;own=curbPath!=null?curbProgress:s.jobs.nav.ProjectVehicle(out ownError);}
                npc=" npc="+car.name+" pos="+car.transform.position+" heading="+car.transform.forward+" speed="+car.speed+" signal="+car.waitingForSignal+" obstacle="+car.waitingForObstacle+" junction="+car.waitingForJunction+" blocked="+car.blockedName+" obstacleCar="+(car.obstacleCar?car.obstacleCar.name:"none")+" routeDeviation="+error+" routeAhead="+(at-own);
            }
            if(StationarySeconds>10)foreach(var collider in Physics.OverlapSphere(t.transform.position+Vector3.up*.5f,3.5f,(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore))if(!collider.transform.IsChildOf(t.transform))obstacles+=collider.name+"/";
            int grounded=0;foreach(var wheel in t.wheels)if(wheel.isGrounded)grounded++;
            return "stationary="+StationarySeconds.ToString("0")+" speed="+t.Speed.ToString("0.00")+" target="+TargetSpeed+" obstacle="+ObstacleReason+" throttle="+t.autoThrottle+" brake="+t.autoBrake+" serviceBrake="+t.autoServiceBrake+" torque="+t.wheels[2].motorTorque+" grounded="+grounded+" assistance="+s.assisted.Status+" lock="+s.simulationMatch.ServiceRemaining(s)+" fatigue="+s.business.ledger.Driver.fatigue+" controlLost="+s.business.DrowsyControlsLost+" nearby="+obstacles+npc;
        }
    }
    float nextOffer;
    PvpSimulationBotServices services;
    AssistedRoadNetwork.Path curbPath;
    float curbProgress;
    int curbGoal=-1;
    TripState curbState;
    bool curbDeparture;
    public void Initialize(DriveSession session) { Session=session; }
    public static float ThrottleFor(float speed,float target){return target>.15f&&speed<target-.05f?.5f:0;}
    public bool RefreshTerminalCalls(){var jobs=Session.jobs;if(jobs.Active||Session.RideResultOpen)return false;bool terminal=jobs.trip.State==TripState.Complete||jobs.trip.State==TripState.Failed;jobs.PrepareForNextCall();if(terminal)jobs.Generate();return terminal;}
    void Stop(string reason) {Session.taxi.autoThrottle=0;Session.taxi.autoBrake=true;Status=reason;}
    void FixedUpdate() {
        var s=Session;if(!s||!s.simulationMatch||s.simulationMatch.Finished||Time.timeScale<=0)return;
        var taxi=s.taxi;var jobs=s.jobs;
        StationarySeconds=jobs.Active&&taxi.body.velocity.magnitude<.15f?StationarySeconds+Time.fixedDeltaTime:0;
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
                var route=jobs.nav.hd.Route(edge,at,stop.laneEdge,stop.laneAt);
                SelectionDiagnostics+="["+i+":"+offer.pickup+">"+offer.drop+" edge="+edge+" goal="+stop.laneEdge+" route="+(route==null?"null":route.path.length.ToString("0"))+"]";
                if(route!=null&&route.path.length<distance){distance=route.path.length;best=i;}
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
        var nav=jobs.nav;var routePath=nav.hdRoute;
        if(routePath==null){Stop("연결 경로 확인 중");nav.Refresh();return;}
        float error;float progress=nav.ProjectVehicle(out error);float remain=routePath.path.length-progress;
        if(curbPath!=null){FollowCurb();return;}
        var stopGoal=jobs.stops[goal];
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
        float target=bend>12||edgeNow.turn!=0||edgeNow.change!=0?3.5f:7f;
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
    }
    public static Vector3[] CurbPathPoints(Vector3 start,Vector3 startDirection,Vector3 end,Vector3 direction){
        var before=end-direction*6;float length=Vector3.Distance(start,before);
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
        if(error>4){Stop("승하차 접근 경로 확인 필요");return;}
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
    float ObstacleDistance() {
        var s=Session;var taxi=s.taxi;float result=100;var position=taxi.transform.position;var heading=taxi.transform.forward;
        ObstacleReason="clear";
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
            if(distance<-.5f||distance>35||lateral>j.halfWidth[axis]+2)continue;
            var observation=s.traffic.Observe(j,axis,sign,axisDirection*sign,taxi.gameObject);
            if((observation.forward!=SignalColor.Green||!observation.PedestrianClear)&&distance+3<result){result=distance+3;sensedCollider=null;ObstacleReason="junction:"+j.name+" axis="+axis+" sign="+sign+" distance="+distance.ToString("0.00")+" signal="+observation.forward+" pedestrianClear="+observation.PedestrianClear;}
        }
        return result;
    }
}
