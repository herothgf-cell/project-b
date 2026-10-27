using UnityEngine;
public class TaxiController : MonoBehaviour {
    public Rigidbody body; public WheelCollider[] wheels=new WheelCollider[4]; Transform[] visuals=new Transform[4];
    public bool automated; public float autoThrottle,autoSteer,autoServiceBrake; public bool autoBrake;
    public AssistedDriveController assistance;public BusinessProgress business;
    float throttle,steer,motorDemand; bool brake; Vector3 spawn; Quaternion spawnRotation;
    public float SteeringInput {get{return steer;}}
    public bool CornerAssisting {get;private set;}
    public float Speed {get{return Vector3.Dot(body.velocity,transform.forward);}}
    public bool ReverseRequested {get{return throttle<-.1f;}}
    public void Create(Vector3 position,Quaternion rotation) {
        spawn=position; spawnRotation=rotation; transform.SetPositionAndRotation(position,rotation);
        body=gameObject.AddComponent<Rigidbody>(); body.mass=1400; body.drag=.04f; body.angularDrag=1.5f; body.interpolation=RigidbodyInterpolation.Interpolate; body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic; body.centerOfMass=new Vector3(0,-.18f,0);body.maxDepenetrationVelocity=3;body.maxAngularVelocity=4;body.solverIterations=12;
        var hit=gameObject.AddComponent<BoxCollider>(); hit.center=new Vector3(0,.42f,0); hit.size=new Vector3(1.8f,.7f,4f);
        var yellow=WorldBuilder.Mat("Taxi yellow",new Color(1f,.65f,.04f)); var glass=WorldBuilder.Mat("Glass",new Color(.08f,.22f,.29f)); var black=WorldBuilder.Mat("Tyres",new Color(.035f,.04f,.05f));
        WorldBuilder.Box("Body",new Vector3(0,.42f,0),new Vector3(1.8f,.7f,4),yellow,transform,false);
        WorldBuilder.Box("Cabin",new Vector3(0,1.02f,-.2f),new Vector3(1.5f,.62f,1.95f),glass,transform,false);
        WorldBuilder.Box("Roof",new Vector3(0,1.35f,-.2f),new Vector3(1.55f,.1f,2),yellow,transform,false);
        WorldBuilder.Box("Taxi sign",new Vector3(0,1.54f,-.2f),new Vector3(.6f,.23f,.3f),yellow,transform,false);
        var light=WorldBuilder.Mat("Headlights",new Color(1,1,.8f));
        for(int i=-1;i<=1;i+=2) WorldBuilder.Box("Headlamp",new Vector3(i*.62f,.47f,2.01f),new Vector3(.4f,.2f,.04f),light,transform,false);
        for(int i=0;i<4;i++) {
            var w=new GameObject("Wheel "+i); w.transform.SetParent(transform,false); w.transform.localPosition=new Vector3(i%2==0?-.85f:.85f,0,i<2?1.3f:-1.3f);
            var c=w.AddComponent<WheelCollider>(); c.radius=.34f; c.mass=25; c.suspensionDistance=.25f; var spring=c.suspensionSpring; spring.spring=35000; spring.damper=4500; spring.targetPosition=.5f; c.suspensionSpring=spring;
            var f=c.sidewaysFriction; f.stiffness=1.35f; c.sidewaysFriction=f; wheels[i]=c;
            var v=GameObject.CreatePrimitive(PrimitiveType.Cylinder); v.name="Tyre visual"; v.transform.SetParent(transform,false); v.transform.localScale=new Vector3(.68f,.12f,.68f); v.GetComponent<Renderer>().sharedMaterial=black; Destroy(v.GetComponent<Collider>()); visuals[i]=v.transform;
        }
        foreach(var tr in GetComponentsInChildren<Transform>()) tr.gameObject.layer=8;
    }
    void Update() {
        if(Time.timeScale==0)return;
        if(automated) {throttle=autoThrottle; steer=autoSteer; brake=autoBrake;} else {
            throttle=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.JoystickButton0)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.JoystickButton1)?1:0);
            float keyboard=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
            float pad=Input.GetAxisRaw("TaxiSteer");float lateral=Mathf.Abs(pad)>.15f?pad:keyboard;
            if(assistance)assistance.ReadInput(lateral);
            if(!assistance||!assistance.Active)steer=Mathf.MoveTowards(steer,lateral,Time.deltaTime*Mathf.Lerp(4.5f,2.2f,Mathf.Clamp01(Mathf.Abs(Speed)/14)));
            brake=Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.JoystickButton2);
        }
        for(int i=0;i<4;i++) {Vector3 p; Quaternion q; wheels[i].GetWorldPose(out p,out q); visuals[i].SetPositionAndRotation(p,q*Quaternion.Euler(0,0,90));}
        if(transform.position.y < -5 || transform.position.y > 50) RequestReset();
    }
    void FixedUpdate() {
        // Automated validation commands are produced per physics tick, including accelerated runs.
        if(automated){throttle=autoThrottle;steer=autoSteer;brake=autoBrake;}
        var manual=DriveMath.Resolve(throttle,Speed,brake);
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
        CornerAssisting=demand.motor<manual.motor||demand.brake>manual.brake;
        float centerAngle=steer*(assistedSteering?DriveMath.SteerAngle(Speed*3.6f):DriveMath.ManualSteerAngle(Speed*3.6f));
        for(int i=0;i<4;i++) { if(!microsleep)wheels[i].steerAngle=i<2?DriveMath.WheelSteerAngle(centerAngle,i==0):0; wheels[i].motorTorque=i>=2?demand.motor*1100:0; wheels[i].brakeTorque=demand.brake*3500; }
        body.AddForce(Vector3.down*body.velocity.sqrMagnitude*6);
        int grounded=0;foreach(var wheel in wheels)if(wheel.isGrounded)grounded++;
        if(grounded>0) {
            var tilt=Vector3.Cross(transform.up,Vector3.up);
            var rollPitch=body.angularVelocity-Vector3.up*Vector3.Dot(body.angularVelocity,Vector3.up);
            body.AddTorque(tilt*35-rollPitch*8,ForceMode.Acceleration);
        }
    }
    public void SetSpawn(Vector3 position,Quaternion rotation){spawn=position;spawnRotation=rotation;}
    public void RequestReset() {
        var coach=PracticeSession.Enabled?FindObjectOfType<PracticeCoach>():null;
        if(coach&&coach.Active&&coach.GetComponent<PracticeSession>().RetryCurrent())return;
        ResetTaxi();
    }
    public void ResetTaxi() {
        if(assistance)assistance.SetActive(false);
        // A reset must not teleport the player inside an occupied traffic lane.
        foreach(var obstacle in Physics.OverlapBox(spawn+Vector3.up*.4f,new Vector3(1.2f,1,2.5f),spawnRotation,1<<9)) {
            var car=obstacle.GetComponentInParent<TrafficCar>();if(car)car.Recycle();
        }
        body.velocity=Vector3.zero; body.angularVelocity=Vector3.zero;
        body.position=spawn; body.rotation=spawnRotation;
        transform.SetPositionAndRotation(spawn,spawnRotation); Physics.SyncTransforms();
        foreach(var w in wheels){w.motorTorque=0;w.brakeTorque=0;w.steerAngle=0;}
        steer=0; throttle=0; motorDemand=0; brake=false;
        var indicators=GetComponent<TaxiSignals>();if(indicators)indicators.Clear();
        var manners=GetComponent<DrivingManners>();if(manners)manners.Suppress(.8f);
    }
}
