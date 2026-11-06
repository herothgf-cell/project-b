using UnityEngine;
public class TaxiController : MonoBehaviour {
    public Rigidbody body; public WheelCollider[] wheels=new WheelCollider[4]; Transform[] visuals=new Transform[4]; float[] dryForwardStiffness=new float[4];
    public bool NetworkReplica,NetworkPredicted; public Vector3 ReplicaVelocity; public float ReplicaSteering;
    public bool automated,AutomatedHumanInput; public float autoThrottle,autoSteer,autoServiceBrake; public bool autoBrake;
    public float ManualSteeringGain=1,ManualBrakeGain=1;
    public AssistedDriveController assistance;public BusinessProgress business;
    float throttle,steer,motorDemand; bool brake; Vector3 spawn; Quaternion spawnRotation;
    readonly DriverPedalIntent pedalIntent=new DriverPedalIntent();
    public bool PvpMapInputBlocked {get{return business&&business.session&&(PvpNativeLan.UiBlocked(business.session)||business.session.simulationMatch&&(business.session.FullMapOpen||business.session.services&&business.session.services.open));}}
    public Vector3 ObservedVelocity {get{return NetworkReplica&&!NetworkPredicted?ReplicaVelocity:body.velocity;}}
    public float SteeringInput {get{return NetworkReplica&&!NetworkPredicted?ReplicaSteering:steer;}}
    public bool CornerAssisting {get;private set;}
    public float Speed {get{return Vector3.Dot(NetworkReplica&&!NetworkPredicted?ReplicaVelocity:body.velocity,transform.forward);}}
    public bool ReverseRequested {get{return throttle<-.1f;}}
    public void Create(Vector3 position,Quaternion rotation) {
        spawn=position; spawnRotation=rotation; transform.SetPositionAndRotation(position,rotation);
        body=gameObject.AddComponent<Rigidbody>(); body.mass=1400; body.drag=.04f; body.angularDrag=1.5f; body.interpolation=RigidbodyInterpolation.Interpolate; body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic; body.centerOfMass=new Vector3(0,-.18f,0);body.maxDepenetrationVelocity=3;body.maxAngularVelocity=4;body.solverIterations=12;
        var hit=gameObject.AddComponent<BoxCollider>(); hit.center=new Vector3(0,.42f,0); hit.size=new Vector3(1.8f,.7f,4f);
        TaxiArt.Exterior(transform);
        for(int i=0;i<4;i++) {
            var w=new GameObject("Wheel "+i); w.transform.SetParent(transform,false); w.transform.localPosition=new Vector3(i%2==0?-.85f:.85f,0,i<2?1.3f:-1.3f);
            var c=w.AddComponent<WheelCollider>(); c.radius=.34f; c.mass=25; c.suspensionDistance=.25f; var spring=c.suspensionSpring; spring.spring=35000; spring.damper=4500; spring.targetPosition=.5f; c.suspensionSpring=spring;
            var f=c.sidewaysFriction; f.stiffness=1.35f; c.sidewaysFriction=f; wheels[i]=c;dryForwardStiffness[i]=c.forwardFriction.stiffness;
            visuals[i]=TaxiArt.Wheel(transform);
        }
        foreach(var tr in GetComponentsInChildren<Transform>()) tr.gameObject.layer=8;
    }
    void Update() {
        if(NetworkReplica&&!NetworkPredicted)return;
        if(Time.timeScale==0)return;
        if(automated) {throttle=autoThrottle; steer=autoSteer; brake=autoBrake;} else {
            throttle=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.JoystickButton0)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.JoystickButton1)?1:0);
            float keyboard=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
            var controls=DrivingSettings.Current;float pad=Input.GetAxisRaw("TaxiSteer"),lateral=controls.Input(keyboard,pad);
            if(assistance)assistance.ReadInput(Mathf.Abs(pad)>.15f?pad:keyboard);
            if(!assistance||!assistance.Active)steer=controls.Advance(steer,lateral,Speed,Time.deltaTime);
            brake=Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.JoystickButton2);
        }
        for(int i=0;i<4;i++) {Vector3 p; Quaternion q; wheels[i].GetWorldPose(out p,out q); visuals[i].SetPositionAndRotation(p,q*Quaternion.Euler(0,0,90));}
        if(transform.position.y < -5 || transform.position.y > 50) RequestReset();
    }
    void FixedUpdate() {
        if(NetworkReplica&&!NetworkPredicted)return;
        // Automated validation commands are produced per physics tick, including accelerated runs.
        if(automated){throttle=autoThrottle;steer=autoSteer;brake=autoBrake;}
        var manual=!automated||NetworkPredicted||AutomatedHumanInput?pedalIntent.Resolve(throttle,Speed,brake):DriveMath.Resolve(throttle,Speed,brake);
        manual=ApplyBrakePreference(manual,brake,Mathf.Abs(throttle)>.01f);
        // Physics-time pedal ramp is identical for keyboard/pad and automated validation.
        // Braking and releasing the accelerator remain prompt, including a direction change.
        if(manual.brake>0||manual.motor*motorDemand<0)motorDemand=0;
        motorDemand=Mathf.MoveTowards(motorDemand,manual.motor,Time.fixedDeltaTime*(Mathf.Abs(manual.motor)>Mathf.Abs(motorDemand)?.9f:6f));
        manual.motor=motorDemand;
        if(automated&&autoServiceBrake>0){manual.motor=0;motorDemand=0;manual.brake=Mathf.Max(manual.brake,Mathf.Clamp01(autoServiceBrake));}
        bool assistedSteering=assistance&&assistance.Active&&(!automated||assistance.allowAutomated);
        if(assistance)assistance.Apply(ref steer,ref manual);
        var demand=assistedSteering?manual:DriveMath.CornerAssist(manual,Speed,steer);
        if(business&&business.DrowsyControlsLost){demand.motor=0;demand.brake=0;motorDemand=0;steer=0;}
        bool microsleep=business&&business.DrowsyMicrosleep;
        // Brief eye closure cuts propulsion and holds the last wheel angle.
        // Braking remains available; neither manual nor assisted steering bypasses it.
        if(microsleep){demand.motor=0;motorDemand=0;}
        var damage=GetComponent<VehicleDamageController>();
        if(business&&business.session.accident&&business.session.accident.Locked){demand.motor=0;motorDemand=0;demand.brake=1;steer=0;}if(damage&&damage.Disabled){demand.motor=0;motorDemand=0;demand.brake=1;}
        CornerAssisting=demand.motor<manual.motor||demand.brake>manual.brake;
        if(business&&business.session.simulationMatch&&business.session.simulationMatch.ServiceLocked(business.session)){demand.motor=0;demand.brake=1;motorDemand=0;}
        if(PvpMapInputBlocked){demand.motor=0;demand.brake=1;motorDemand=0;steer=0;}
        float centerAngle=WheelCenterAngle(steer,Speed*3.6f,assistedSteering);
        for(int i=0;i<4;i++) { var grip=wheels[i].forwardFriction;grip.stiffness=dryForwardStiffness[i]*(demand.brake>0?RainPolicy.BrakeMultiplier(Speed):1);wheels[i].forwardFriction=grip; if(!microsleep)wheels[i].steerAngle=i<2?DriveMath.WheelSteerAngle(centerAngle,i==0):0; wheels[i].motorTorque=i>=2?demand.motor*1100:0; wheels[i].brakeTorque=demand.brake*3500*RainPolicy.BrakeMultiplier(Speed); }
        body.AddForce(Vector3.down*body.velocity.sqrMagnitude*6);
        int grounded=0;foreach(var wheel in wheels)if(wheel.isGrounded)grounded++;
        if(grounded>0) {
            var tilt=Vector3.Cross(transform.up,Vector3.up);
            var rollPitch=body.angularVelocity-Vector3.up*Vector3.Dot(body.angularVelocity,Vector3.up);
            body.AddTorque(tilt*35-rollPitch*8,ForceMode.Acceleration);
        }
        // Only help a human's city turn catch up to their wheel input. Do not
        // steer a route, spin a parked car, override H, or push airborne wheels.
        if(DriveMath.MayAssistManualYaw(grounded,Vector3.Dot(transform.up,Vector3.up),assistedSteering,!automated||AutomatedHumanInput||NetworkPredicted,microsleep))
            body.AddTorque(Vector3.up*DriveMath.ManualYawAcceleration(Speed,centerAngle,body.angularVelocity.y),ForceMode.Acceleration);
    }
    public DriveMath.Demand ApplyBrakePreference(DriveMath.Demand command,bool emergency,bool pedalPressed){
        if(!automated||AutomatedHumanInput||NetworkPredicted)command.brake=DrivingSettings.BrakeForce(command.brake,automated?ManualBrakeGain:DrivingSettings.Current.braking,emergency,pedalPressed);
        return command;
    }
    public float WheelCenterAngle(float input,float kph,bool assisted){
        if(assisted)return input*DriveMath.SteerAngle(kph);
        float gain=!automated?DrivingSettings.Current.sensitivity:AutomatedHumanInput||NetworkPredicted?ManualSteeringGain:1;
        return DrivingSettings.Angle(input,kph,gain);
    }
    public void ClearPredictedPedals(){steer=throttle=motorDemand=autoSteer=autoThrottle=autoServiceBrake=0;brake=autoBrake=true;}
    public int ResetEpoch {get;private set;}
    public void ResetPedalIntent(){pedalIntent.Clear();ResetEpoch++;}
    public int PedalBrakingDirection {get{return pedalIntent.BrakingDirection;}}
    public void RestorePedalIntent(int direction){pedalIntent.Restore(direction);}
    public void SetSpawn(Vector3 position,Quaternion rotation){spawn=position;spawnRotation=rotation;}
    public void RequestReset() {if(business&&business.session.accident&&business.session.accident.Locked)return;
        if(PvpNativeLan.Route(business?business.session:null,"reset"))return;
        var coach=PracticeSession.Enabled?FindObjectOfType<PracticeCoach>():null;
        if(coach&&coach.Active&&coach.GetComponent<PracticeSession>().RetryCurrent())return;
        ResetTaxi();
    }
    public void ResetTaxi() {var accident=GetComponent<AccidentController>();if(accident)accident.Suppress(1);
        if(assistance)assistance.SetActive(false);
        // A reset must not teleport the player inside an occupied traffic lane.
        foreach(var obstacle in Physics.OverlapBox(spawn+Vector3.up*.4f,new Vector3(1.2f,1,2.5f),spawnRotation,1<<9)) {
            var car=obstacle.GetComponentInParent<TrafficCar>();if(car)car.Recycle();
        }
        body.velocity=Vector3.zero; body.angularVelocity=Vector3.zero;
        body.position=spawn; body.rotation=spawnRotation;
        transform.SetPositionAndRotation(spawn,spawnRotation); Physics.SyncTransforms();
        foreach(var w in wheels){w.motorTorque=0;w.brakeTorque=0;w.steerAngle=0;}
        steer=0; throttle=0; motorDemand=0; brake=false;ResetPedalIntent();
        var indicators=GetComponent<TaxiSignals>();if(indicators)indicators.Clear();
        var manners=GetComponent<DrivingManners>();if(manners)manners.Suppress(.8f);
        var damage=GetComponent<VehicleDamageController>();if(damage)damage.TechnicalRecovery();
    }
}
