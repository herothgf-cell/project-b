using UnityEngine;
public class TrafficCar : MonoBehaviour {
    public bool respawn=true;
    public int mitFollowingTicks;public float baselineTarget,followingTarget;
    public DistrictTraffic district;
    readonly System.Collections.Generic.HashSet<TrafficStop> released=new System.Collections.Generic.HashSet<TrafficStop>();
    public bool Visible {get{return !recycling;}}
    public int CurrentTurn {get{if(route.graphPath==null)return route.turn;foreach(var stop in route.stops)if(distance<=stop.exitDistance&&stop.distance-distance<30)return stop.Movement(route);return 0;}}
    public void Continue(TrafficRoute next){system.Release(this);released.Clear();stoppedAt=null;route=next;distance=0;completedRoutes++;}

    public TrafficJunction stoppedAt;
    public TrafficRoute route; public TrafficSystem system;
    public TrafficCar obstacleCar { get; private set; }
    public float distance, speed,totalTravel;public int completedRoutes; public bool waitingForSignal, waitingForObstacle, waitingForJunction,entryBlocked;
    public string blockedName="";public int redStops, departures; bool previouslyStopped, recycling;
    Rigidbody body; BoxCollider hit; Renderer[] renderers; Renderer[] brakeLights;
    readonly TrafficOverlapQuery overlaps=new TrafficOverlapQuery();
    Material brakeOn, brakeOff,indicatorOn,indicatorOff;Renderer[] indicators;
    public void Initialize(TrafficSystem owner,TrafficRoute path,float initial,int index) {
        system=owner;route=path;
        body=gameObject.AddComponent<Rigidbody>(); body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        hit=gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,.8f,0);hit.size=new Vector3(1.8f,1.25f,4.2f);
        Color[] colors={new Color(.15f,.34f,.62f),new Color(.72f,.16f,.13f),new Color(.75f,.77f,.77f),new Color(.18f,.4f,.33f)};
        var paint=WorldBuilder.Mat("AI paint",colors[index%colors.Length]);var glass=WorldBuilder.Mat("AI glass",new Color(.08f,.17f,.22f));var tyre=WorldBuilder.Mat("AI tyres",new Color(.035f,.04f,.05f));
        WorldBuilder.Box("Body",new Vector3(0,.65f,0),new Vector3(1.8f,.65f,4.2f),paint,transform,false);
        WorldBuilder.Box("Cabin",new Vector3(0,1.2f,-.2f),new Vector3(1.5f,.65f,2),glass,transform,false);
        WorldBuilder.Box("Roof",new Vector3(0,1.55f,-.2f),new Vector3(1.55f,.1f,2.1f),paint,transform,false);
        for(int i=0;i<4;i++){var w=GameObject.CreatePrimitive(PrimitiveType.Cylinder);w.transform.SetParent(transform,false);w.transform.localPosition=new Vector3(i%2==0?-.87f:.87f,.34f,i<2?1.3f:-1.3f);w.transform.localRotation=Quaternion.Euler(0,0,90);w.transform.localScale=new Vector3(.68f,.12f,.68f);w.GetComponent<Renderer>().sharedMaterial=tyre;Destroy(w.GetComponent<Collider>());}
        brakeOn=WorldBuilder.Mat("Brake lit",Color.red);brakeOn.EnableKeyword("_EMISSION");brakeOn.SetColor("_EmissionColor",Color.red*2);brakeOff=WorldBuilder.Mat("Brake off",new Color(.25f,.015f,.01f));
        brakeLights=new Renderer[2];for(int i=0;i<2;i++)brakeLights[i]=WorldBuilder.Box("Brake light",new Vector3(i==0?-.6f:.6f,.7f,-2.11f),new Vector3(.38f,.18f,.035f),brakeOff,transform,false).GetComponent<Renderer>();
        indicatorOn=WorldBuilder.Mat("AI indicator lit",new Color(1,.45f,.02f));indicatorOn.EnableKeyword("_EMISSION");indicatorOn.SetColor("_EmissionColor",new Color(1,.4f,0)*2);indicatorOff=WorldBuilder.Mat("AI indicator off",new Color(.25f,.1f,0));indicators=new Renderer[4];
        for(int i=0;i<4;i++)indicators[i]=WorldBuilder.Box("AI direction indicator",new Vector3(i%2==0?-.78f:.78f,.8f,i<2?2.12f:-2.12f),new Vector3(.18f,.15f,.05f),indicatorOff,transform,false).GetComponent<Renderer>();
        foreach(var child in GetComponentsInChildren<Transform>()) child.gameObject.layer=9;
        renderers=GetComponentsInChildren<Renderer>(); Place(initial);
    }
    public void Place(float position) {
        system.Release(this);released.Clear();stoppedAt=null;distance=position;speed=0;recycling=false;SetVisible(true);
        body.position=route.Position(distance);body.rotation=Quaternion.LookRotation(route.Tangent(distance));transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();
    }
    void SetVisible(bool value){hit.enabled=value;foreach(var r in renderers)r.enabled=value;}
    public void Recycle(){if(distance>=route.length-.01f)completedRoutes++;system.Release(this);recycling=true;speed=0;SetVisible(false);body.position=new Vector3(-100,-100,-100);}
    void OnDisable(){if(system)system.Release(this);}
    void FixedUpdate() {using(TrafficPerformanceProbe.Measure("TrafficCar.FixedUpdate"))Tick();}
    void Tick() {
        if(recycling) {
            if(!respawn)return;
            if(overlaps.Box(route.start+Vector3.up*.8f,new Vector3(1.1f,.7f,4),Quaternion.LookRotation(route.direction),(1<<8)|(1<<9))==0) Place(0);
            return;
        }
        if(district!=null&&distance>=route.length-.01f){if(!district.Continue(this))speed=0;return;}
        float gap=1000;blockedName=""; waitingForSignal=false;waitingForObstacle=false;waitingForJunction=false;
        foreach(var stop in route.stops) {
            float remaining=stop.distance-distance;
            if(remaining>=-.05f&&remaining<.4f&&speed<.05f)stoppedAt=stop.junction;
            if(distance>stop.exitDistance&&released.Add(stop)){system.Release(this,stop.junction);if(stoppedAt==stop.junction)stoppedAt=null;}
            if(remaining>=-.05f&&remaining<=system.ReservationDistance(this))system.Request(this,stop);
            // Once beyond the stop line, clear the intersection even when its signal changes.
            if(remaining>=-.05f && remaining<=TrafficRules.SignalObservationDistance(speed) && !system.CanEnter(this,stop)) {
                system.CancelApproach(this,stop);
                if(remaining<gap){gap=Mathf.Max(0,remaining);waitingForSignal=true;}
            }
        }
        obstacleCar=null;float detection=Mathf.Max(8,speed*speed/6+6);
        if(system.ambientPedestrians!=null){float pedestrianGap=system.ambientPedestrians.OccupiedCrossingGap(this,detection);if(pedestrianGap<gap){gap=pedestrianGap;waitingForObstacle=true;blockedName="occupied pedestrian crossing";}}
        for(float ahead=.5f;ahead<detection+3;ahead+=.5f){float at=Mathf.Min(route.length,distance+ahead);var facing=route.Tangent(at);bool blocked=false;
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
        if(district!=null){float bend=Vector3.Angle(route.Tangent(distance),route.Tangent(Mathf.Min(route.length,distance+14)));cruise=bend>12||CurrentTurn!=0?4.5f:9;}
        float editorLimit=system.EditorSpeedLimit(this);if(editorLimit>0)cruise=Mathf.Min(cruise,editorLimit/3.6f);float target=TrafficRules.SafeSpeed(gap,cruise);baselineTarget=target;
        target=MitTrafficFollowing.Limit(system.mitOptions.Enabled,target,gap,
            obstacleCar?obstacleCar.speed:0,obstacleCar?Vector3.Dot(transform.forward,obstacleCar.transform.forward):0,
            waitingForObstacle&&obstacleCar!=null&&obstacleCar.Visible);
        followingTarget=target;if(target<baselineTarget-.001f)mitFollowingTicks++;
        float previous=speed; speed=Mathf.MoveTowards(speed,target,(target<speed?5:2.2f)*Time.fixedDeltaTime);
        float step=Mathf.Min(speed*Time.fixedDeltaTime,Mathf.Max(0,gap)); distance+=step;totalTravel+=step;
        if(step<.0001f)speed=0;
        bool stopped=waitingForSignal&&speed<.1f;
        if(stopped&&!previouslyStopped)redStops++;
        if(previouslyStopped&&speed>.1f){departures++;previouslyStopped=false;}
        if(stopped)previouslyStopped=true;
        body.MovePosition(route.Position(distance));body.MoveRotation(Quaternion.LookRotation(route.Tangent(distance)));
        foreach(var lamp in brakeLights)lamp.sharedMaterial=speed<.2f||speed<previous?brakeOn:brakeOff;
        int currentTurn=CurrentTurn;bool blinking=currentTurn!=0&&Time.time%.8f<.4f;
        for(int i=0;i<indicators.Length;i++)indicators[i].sharedMaterial=blinking&&(i%2==0?currentTurn<0:currentTurn>0)?indicatorOn:indicatorOff;
        if(distance>=route.length){distance=route.length;if(district!=null){if(!district.Continue(this))speed=0;}else Recycle();}
    }
}
