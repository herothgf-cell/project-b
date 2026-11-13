using UnityEngine;
public class TrafficCar : MonoBehaviour {
    public bool respawn=true;
    public Vector3 PhysicsPosition {get{return body?body.position:transform.position;}}
    public Quaternion PhysicsRotation {get{return body?body.rotation:transform.rotation;}}
    public Vector3 StepVelocity {get;private set;}
    public Vector3 StepAngularVelocity {get;private set;}
    public TrafficDriverProfile Profile {get;private set;}
    public string ProfileLabel {get{return Profile==null?"보통":Profile.Label;}}
    double lastLaneChangeCompleted=-100000;int observedLaneEdge=-1;
    // Cooldowns slow the real approach to the next legal lane-change edge.
    // They never release a reservation, remove a body or skip a route segment.
    float ProfileLaneChangeGap(){
        if(district==null||route.graphPath==null)return float.PositiveInfinity;
        int edge=route.graphPath.EdgeAt(distance);
        if(observedLaneEdge>=0&&edge!=observedLaneEdge&&district.graph.edges[observedLaneEdge].change!=0)lastLaneChangeCompleted=Time.time;
        observedLaneEdge=edge;
        if(district.graph.edges[edge].change!=0||Profile.LaneChangeReady(Time.time,lastLaneChangeCompleted))return float.PositiveInfinity;
        float begin=0;
        for(int i=0;i<route.graphPath.edges.Count;i++){int next=route.graphPath.edges[i];if(begin>=distance&&district.graph.edges[next].change!=0)return Mathf.Max(0,begin-distance-2);begin=route.graphPath.ends[i];}
        return float.PositiveInfinity;
    }
    // Single-map lane changes (PvP uses authored merge zones) wait at the change start while the
    // target lane has a vehicle alongside or approaching from behind, including the player taxi.
    public bool laneYielding;float laneYieldSince;int laneYieldEdge=-1;
    float LaneChangeYieldGap(){
        if(district==null||route.graphPath==null||system.AuthoredMergesActive)return float.PositiveInfinity;
        float begin=0;
        for(int i=0;i<route.graphPath.edges.Count;i++){int next=route.graphPath.edges[i];float end=route.graphPath.ends[i];
            if(district.graph.edges[next].change!=0&&end>distance){
                if(begin<distance-.5f)return float.PositiveInfinity; // already moving across: finish the change
                if(begin-distance>Mathf.Max(12,speed*3))return float.PositiveInfinity;
                // Bounded wait per change edge: forward body sensing on the change edge still prevents contact.
                if(laneYieldEdge==i&&Time.time-laneYieldSince>6)return float.PositiveInfinity;
                var target=route.Position(Mathf.Min(route.length,end));var facing=route.Tangent(Mathf.Min(route.length,end));facing.y=0;if(facing.sqrMagnitude<.001f)return float.PositiveInfinity;facing.Normalize();
                float rear=Mathf.Max(8,speed*3+8);var center=target-facing*(rear-3)*.5f+Vector3.up*.85f;
                int contacts=overlaps.Box(center,new Vector3(1.1f,.6f,(rear+3)*.5f),Quaternion.LookRotation(facing),(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore);
                for(int c=0;c<contacts;c++){var h=overlaps.hits[c];var rb=h.attachedRigidbody;if(rb==null||rb==body)continue;
                    var other=h.GetComponentInParent<TrafficCar>();float otherSpeed=other?other.speed:rb.velocity.magnitude;
                    // Two cars swapping lanes side by side: the lower instance id goes first.
                    if(other&&other.laneYielding&&other.GetInstanceID()>GetInstanceID())continue;
                    float along=Vector3.Dot(rb.position-target,facing);
                    // Alongside the merge point, or behind and actually closing in.
                    if(along>-6||otherSpeed>1f){if(laneYieldEdge!=i){laneYieldEdge=i;laneYieldSince=Time.time;}return Mathf.Max(0,begin-distance-.5f);}}
                return float.PositiveInfinity;}
            begin=end;}
        return float.PositiveInfinity;
    }
    public int mitFollowingTicks;public float baselineTarget,followingTarget;
    public DistrictTraffic district;
    readonly System.Collections.Generic.HashSet<TrafficStop> released=new System.Collections.Generic.HashSet<TrafficStop>();
    public bool Visible {get{return !recycling;}}
    bool networkView;int networkTurn;
    public int CurrentTurn {get{if(networkView)return networkTurn;if(route.graphPath==null)return route.turn;foreach(var stop in route.stops)if(distance<=stop.exitDistance&&stop.distance-distance<30)return stop.Movement(route);return 0;}}
    public void Continue(TrafficRoute next){laneYieldEdge=-1;system.Release(this);released.Clear();stoppedAt=null;route=next;distance=0;observedLaneEdge=-1;completedRoutes++;}

    public TrafficJunction stoppedAt;
    public TrafficRoute route; public TrafficSystem system;
    public TrafficCar obstacleCar { get; private set; }
    public float distance, speed,totalTravel;public int completedRoutes; public bool waitingForSignal, waitingForObstacle, waitingForJunction,entryBlocked,waitingForMerge;public string mergeStatus="";
    public string blockedName="";public int redStops, departures; bool previouslyStopped, recycling;
    Rigidbody body; BoxCollider hit; Renderer[] renderers; Renderer[] brakeLights;
    readonly TrafficOverlapQuery overlaps=new TrafficOverlapQuery();
    Material brakeOn, brakeOff,indicatorOn,indicatorOff;Renderer[] indicators;
    public void Initialize(TrafficSystem owner,TrafficRoute path,float initial,int index) {
        system=owner;route=path;Profile=TrafficDriverProfile.ForIndex(index,owner.EungyeSingle);gameObject.name+=" · "+ProfileLabel;
        body=gameObject.AddComponent<Rigidbody>(); body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        hit=gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,.8f,0);hit.size=new Vector3(1.8f,1.25f,4.2f);
        Color[] colors={new Color(.15f,.34f,.62f),new Color(.72f,.16f,.13f),new Color(.75f,.77f,.77f),new Color(.18f,.4f,.33f)};
        var paint=WorldBuilder.Mat("AI paint",colors[index%colors.Length]);var glass=WorldBuilder.Mat("AI glass",new Color(.08f,.17f,.22f));var tyre=WorldBuilder.Mat("AI tyres",new Color(.035f,.04f,.05f));
        WorldBuilder.Box("Body",new Vector3(0,.65f,0),new Vector3(1.8f,.65f,4.2f),paint,transform,false);
        WorldBuilder.Box("Cabin",new Vector3(0,1.2f,-.2f),new Vector3(1.5f,.65f,2),glass,transform,false);
        WorldBuilder.Box("Roof",new Vector3(0,1.55f,-.2f),new Vector3(1.55f,.1f,2.1f),paint,transform,false);
        for(int i=0;i<4;i++){var w=GameObject.CreatePrimitive(PrimitiveType.Cylinder);w.transform.SetParent(transform,false);w.transform.localPosition=new Vector3(i%2==0?-.87f:.87f,.34f,i<2?1.3f:-1.3f);w.transform.localRotation=Quaternion.Euler(0,0,90);w.transform.localScale=new Vector3(.68f,.12f,.68f);w.GetComponent<Renderer>().sharedMaterial=tyre;var wheelCollider=w.GetComponent<Collider>();if(Application.isPlaying)Destroy(wheelCollider);else DestroyImmediate(wheelCollider);}
        brakeOn=WorldBuilder.Mat("Brake lit",Color.red);brakeOn.EnableKeyword("_EMISSION");brakeOn.SetColor("_EmissionColor",Color.red*2);brakeOff=WorldBuilder.Mat("Brake off",new Color(.25f,.015f,.01f));
        brakeLights=new Renderer[2];for(int i=0;i<2;i++)brakeLights[i]=WorldBuilder.Box("Brake light",new Vector3(i==0?-.6f:.6f,.7f,-2.11f),new Vector3(.38f,.18f,.035f),brakeOff,transform,false).GetComponent<Renderer>();
        indicatorOn=WorldBuilder.Mat("AI indicator lit",new Color(1,.45f,.02f));indicatorOn.EnableKeyword("_EMISSION");indicatorOn.SetColor("_EmissionColor",new Color(1,.4f,0)*2);indicatorOff=WorldBuilder.Mat("AI indicator off",new Color(.25f,.1f,0));indicators=new Renderer[4];
        for(int i=0;i<4;i++)indicators[i]=WorldBuilder.Box("AI direction indicator",new Vector3(i%2==0?-.78f:.78f,.8f,i<2?2.12f:-2.12f),new Vector3(.18f,.15f,.05f),indicatorOff,transform,false).GetComponent<Renderer>();
        foreach(var child in GetComponentsInChildren<Transform>()) child.gameObject.layer=9;
        renderers=GetComponentsInChildren<Renderer>(); Place(initial);
    }
    public void Place(float position) {
        StepVelocity=StepAngularVelocity=Vector3.zero;
        system.Release(this);released.Clear();stoppedAt=null;distance=position;speed=0;observedLaneEdge=-1;recycling=false;SetVisible(true);
        body.position=route.Position(distance);body.rotation=Quaternion.LookRotation(route.Tangent(distance));transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();
    }
    // A client replica receives an authoritative pose and lamp state. Disabling
    // this component stops its route AI/FixedUpdate; snapshots still call here.
    public void ApplyNetworkView(Vector3 position,Quaternion rotation,float speed,bool visible,int turn,bool brake,float clock) {
        networkView=true;networkTurn=turn;respawn=false;enabled=false;
        this.speed=Mathf.Max(0,speed);recycling=!visible;
        if(!body)body=GetComponent<Rigidbody>();
        if(body){body.isKinematic=true;body.interpolation=RigidbodyInterpolation.None;body.position=position;body.rotation=rotation;}
        transform.SetPositionAndRotation(position,rotation);
        if(renderers==null)renderers=GetComponentsInChildren<Renderer>();
        SetVisible(visible);
        if(brakeLights!=null)foreach(var lamp in brakeLights)if(lamp)lamp.sharedMaterial=brake?brakeOn:brakeOff;
        bool blinking=turn!=0&&Mathf.Repeat(clock,.8f)<.4f;
        if(indicators!=null)for(int i=0;i<indicators.Length;i++)if(indicators[i])indicators[i].sharedMaterial=blinking&&(i%2==0?turn<0:turn>0)?indicatorOn:indicatorOff;
    }
    void SetVisible(bool value){if(hit)hit.enabled=value;if(renderers!=null)foreach(var r in renderers)if(r)r.enabled=value;}
    public void Recycle(){if(distance>=route.length-.01f)completedRoutes++;system.Release(this);recycling=true;speed=0;StepVelocity=StepAngularVelocity=Vector3.zero;SetVisible(false);body.position=new Vector3(-100,-100,-100);}
    void OnDisable(){if(system)system.Release(this);}
    void FixedUpdate() {
        StepVelocity=StepAngularVelocity=Vector3.zero;
        using(TrafficPerformanceProbe.Measure("TrafficCar.FixedUpdate"))Tick();
    }
    // One conservative broad query skips dozens of narrow probes only when the
    // entire original swept sampling envelope contains no other body. Dense or
    // curved traffic still uses the unchanged per-pose collision checks below.
    bool HasApproachBody(float detection){
        var bounds=new Bounds(route.Position(Mathf.Min(route.length,distance+.5f))+Vector3.up*.85f,Vector3.zero);
        float vertical=0;
        for(float ahead=.5f;ahead<detection+3;ahead+=.5f){float at=Mathf.Min(route.length,distance+ahead);bounds.Encapsulate(route.Position(at)+Vector3.up*.85f);vertical=Mathf.Max(vertical,Mathf.Abs(route.Tangent(at).y));}
        bounds.Expand(new Vector3(5.2f,1.1f+4.5f*vertical,5.2f));
        int count=overlaps.Box(bounds.center,bounds.extents,Quaternion.identity,(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)if(overlaps.hits[i].attachedRigidbody!=body)return true;
        return false;
    }
    void Tick() {
        if(recycling) {
            if(!respawn)return;
            if(overlaps.Box(route.start+Vector3.up*.8f,new Vector3(1.1f,.7f,4),Quaternion.LookRotation(route.direction),(1<<8)|(1<<9))==0) Place(0);
            return;
        }
        if(district!=null&&distance>=route.length-.01f){if(!district.Continue(this))speed=0;return;}
        float gap=1000;blockedName=""; waitingForSignal=false;waitingForObstacle=false;waitingForJunction=false;waitingForMerge=false;mergeStatus="";
        foreach(var stop in route.stops) {
            float remaining=stop.distance-distance;
            if(remaining>=-.05f&&remaining<.4f&&speed<.05f)stoppedAt=stop.junction;
            if(distance>stop.exitDistance&&released.Add(stop)){system.Release(this,stop.junction);if(stoppedAt==stop.junction)stoppedAt=null;}
            if(remaining>=-.05f&&remaining<=system.ReservationDistance(this))system.Request(this,stop);
            // Braking for a queue shrinks the speed-dependent reservation horizon.
            // Do not hold a previously acquired approach reservation outside it:
            // that queue may need a conflicting green movement to drain first.
            if(remaining>=-.05f&&remaining>system.ReservationDistance(this)&&stop.junction.reservations.Contains(this))system.CancelApproach(this,stop);
            // A red stop line can lie inside another movement's curved corridor.
            // Hold upstream until its committed owner clears; never move the line
            // or grant full-stop evidence before actually reaching it.
            float reservedGap=system.ReservedApproachGap(this,stop);
            if(reservedGap<gap){gap=reservedGap;waitingForJunction=true;}
            // Once beyond the stop line, clear the intersection even when its signal changes.
            if(remaining>=-.05f && remaining<=TrafficRules.SignalObservationDistance(speed) && !system.CanEnter(this,stop)) {
                system.CancelApproach(this,stop);
                if(remaining<gap){gap=Mathf.Max(0,remaining);waitingForSignal=true;}
            }
        }
        gap=Mathf.Min(gap,ProfileLaneChangeGap());
        float laneYield=LaneChangeYieldGap();laneYielding=laneYield<gap;if(laneYielding){gap=laneYield;blockedName="lane change yield";}
        float mergeGap=system.MergeApproachGap(this,out mergeStatus);
        if(mergeGap<gap){gap=mergeGap;waitingForMerge=true;waitingForJunction=true;}
        obstacleCar=null;float detection=Mathf.Max(8,speed*speed/6+6);
        if(system.ambientPedestrians!=null){float pedestrianGap=system.ambientPedestrians.OccupiedCrossingGap(this,detection);if(pedestrianGap<gap){gap=pedestrianGap;waitingForObstacle=true;blockedName="occupied pedestrian crossing";}}
        if(HasApproachBody(detection))for(float ahead=.5f;ahead<detection+3;ahead+=.5f){float at=Mathf.Min(route.length,distance+ahead);var facing=route.Tangent(at);bool blocked=false;
            int contacts=overlaps.Box(route.Position(at)+Vector3.up*.85f,new Vector3(.95f,.55f,2.25f),Quaternion.LookRotation(facing),(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore);
            for(int contact=0;contact<contacts;contact++){var h=overlaps.hits[contact];if(h.attachedRigidbody!=body){blockedName=h.name;obstacleCar=h.GetComponentInParent<TrafficCar>();blocked=true;break;}}
            if(blocked){float obstacleGap=Mathf.Max(0,ahead-1.5f);if(obstacleGap<gap){gap=obstacleGap;waitingForObstacle=true;waitingForSignal=false;}break;}
        }
        entryBlocked=false;foreach(var stop in route.stops){float remaining=stop.distance-distance;if(remaining>=-.05f&&remaining<=system.ReservationDistance(this)&&system.CanEnter(this,stop)){
            // Do not reserve a junction when a queue blocks the path to its stop line.
            bool blocked=waitingForObstacle&&gap<remaining+2&&!system.MovingLeader(this,obstacleCar);entryBlocked|=blocked;
            if(blocked||!system.MayEnter(this,stop)){system.CancelApproach(this,stop);gap=Mathf.Min(gap,Mathf.Max(0,remaining));waitingForJunction=true;}
        }}
        float cruise=route.turn!=0?4.5f:route.axis==0?9:7;
        if(district!=null){float bend=Vector3.Angle(route.Tangent(distance),route.Tangent(Mathf.Min(route.length,distance+14)));cruise=bend>12||CurrentTurn!=0?4.5f:system.EungyeSingle?13:9;}
        float editorLimit=system.EditorSpeedLimit(this);if(editorLimit>0)cruise=Mathf.Min(cruise,editorLimit/3.6f);cruise*=RainPolicy.TrafficSpeedMultiplier;float followingGap=waitingForObstacle?Mathf.Max(0,gap-RainPolicy.ExtraFollowingGap(speed)):gap;float target=Profile.LimitSpeed(followingGap,cruise,waitingForObstacle&&obstacleCar!=null);baselineTarget=target;
        target=MitTrafficFollowing.Limit(system.mitOptions.Enabled,target,gap,
            obstacleCar?obstacleCar.speed:0,obstacleCar?Vector3.Dot(transform.forward,obstacleCar.transform.forward):0,
            waitingForObstacle&&obstacleCar!=null&&obstacleCar.Visible);
        followingTarget=target;if(target<baselineTarget-.001f)mitFollowingTicks++;
        float previous=speed; speed=Mathf.MoveTowards(speed,target,(target<speed?5:Profile.Acceleration)*Time.fixedDeltaTime);
        float step=Mathf.Min(speed*Time.fixedDeltaTime,Mathf.Max(0,gap)); distance+=step;totalTravel+=step;
        if(step<.0001f)speed=0;
        bool stopped=waitingForSignal&&speed<.1f;
        if(stopped&&!previouslyStopped)redStops++;
        if(previouslyStopped&&speed>.1f){departures++;previouslyStopped=false;}
        if(stopped)previouslyStopped=true;
        var nextPosition=route.Position(distance);
        var nextRotation=Quaternion.LookRotation(route.Tangent(distance));
        StepVelocity=(nextPosition-body.position)/Time.fixedDeltaTime;
        var rotationDelta=nextRotation*Quaternion.Inverse(body.rotation);
        var rotationVector=new Vector3(rotationDelta.x,rotationDelta.y,rotationDelta.z);
        if(rotationDelta.w<0)rotationVector=-rotationVector;
        float halfSine=rotationVector.magnitude;
        if(halfSine>.00001f)
            StepAngularVelocity=rotationVector*(2*Mathf.Atan2(halfSine,Mathf.Abs(rotationDelta.w))/(halfSine*Time.fixedDeltaTime));
        body.MovePosition(nextPosition);body.MoveRotation(nextRotation);
        foreach(var lamp in brakeLights)lamp.sharedMaterial=speed<.2f||speed<previous?brakeOn:brakeOff;
        int currentTurn=CurrentTurn;bool blinking=currentTurn!=0&&Time.time%.8f<.4f;
        for(int i=0;i<indicators.Length;i++)indicators[i].sharedMaterial=blinking&&(i%2==0?currentTurn<0:currentTurn>0)?indicatorOn:indicatorOff;
        if(distance>=route.length){distance=route.length;if(district!=null){if(!district.Continue(this))speed=0;}else Recycle();}
    }
}
