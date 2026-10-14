using UnityEngine;
public class TaxiController : MonoBehaviour {
    public Rigidbody body; public WheelCollider[] wheels=new WheelCollider[4]; Transform[] visuals=new Transform[4];
    public bool automated; public float autoThrottle,autoSteer; public bool autoBrake;
    float throttle,steer; bool brake; Vector3 spawn; Quaternion spawnRotation;
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
            float pad=Input.GetAxisRaw("TaxiSteer"); steer=Mathf.MoveTowards(steer,Mathf.Abs(pad)>.15f?pad:keyboard,Time.deltaTime*3);
            brake=Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.JoystickButton2);
            if(Input.GetKeyDown(KeyCode.R)||Input.GetKeyDown(KeyCode.JoystickButton3)) ResetTaxi();
        }
        for(int i=0;i<4;i++) {Vector3 p; Quaternion q; wheels[i].GetWorldPose(out p,out q); visuals[i].SetPositionAndRotation(p,q*Quaternion.Euler(0,0,90));}
        if(transform.position.y < -5 || transform.position.y > 50) ResetTaxi();
    }
    void FixedUpdate() {
        var manual=DriveMath.Resolve(throttle,Speed,brake);
        var demand=DriveMath.CornerAssist(manual,Speed,steer);
        CornerAssisting=demand.motor<manual.motor||demand.brake>manual.brake;
        for(int i=0;i<4;i++) { wheels[i].steerAngle=i<2?steer*DriveMath.SteerAngle(Speed*3.6f):0; wheels[i].motorTorque=i>=2?demand.motor*1100:0; wheels[i].brakeTorque=demand.brake*3500; }
        body.AddForce(Vector3.down*body.velocity.sqrMagnitude*6);
        int grounded=0;foreach(var wheel in wheels)if(wheel.isGrounded)grounded++;
        if(grounded>0) {
            var tilt=Vector3.Cross(transform.up,Vector3.up);
            var rollPitch=body.angularVelocity-Vector3.up*Vector3.Dot(body.angularVelocity,Vector3.up);
            body.AddTorque(tilt*35-rollPitch*8,ForceMode.Acceleration);
        }
    }
    public void ResetTaxi() {
        // A reset must not teleport the player inside an occupied traffic lane.
        foreach(var obstacle in Physics.OverlapBox(spawn+Vector3.up*.4f,new Vector3(1.2f,1,2.5f),spawnRotation,1<<9)) {
            var car=obstacle.GetComponentInParent<TrafficCar>();if(car)car.Recycle();
        }
        body.velocity=Vector3.zero; body.angularVelocity=Vector3.zero;
        body.position=spawn; body.rotation=spawnRotation;
        transform.SetPositionAndRotation(spawn,spawnRotation); Physics.SyncTransforms();
        foreach(var w in wheels){w.motorTorque=0;w.brakeTorque=0;w.steerAngle=0;}
        steer=0; throttle=0; brake=false;
        var indicators=GetComponent<TaxiSignals>();if(indicators)indicators.Clear();
        var manners=GetComponent<DrivingManners>();if(manners)manners.Suppress(.8f);
    }
}
