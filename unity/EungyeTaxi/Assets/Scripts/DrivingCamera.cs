using UnityEngine;
public class DrivingCamera : MonoBehaviour {
    static readonly Vector3 DriverEye = new Vector3(-.43f,1.07f,.6f);
    const float DriverPitch = 2f, DriverFieldOfView = 82f;
    public Transform target;
    public bool FirstPerson { get; private set; }
    public bool RearView { get; private set; }
    public bool testRearView;public float testLook;public float LookYaw {get;private set;}public bool cornerLook=true;TaxiController taxi;
    // Presentation feel (08 chapter 1). Strengths come from DrivingSettings (0 = off); physics is untouched.
    float appliedFov,lastSpeed,longitudinal,lateral,dip;Transform exterior;Quaternion exteriorRest;
    public float SpeedFovOffset {get;private set;} public float BrakeDip {get;private set;} public Vector2 BodyLean {get;private set;}
    Transform driverGlass;
    float windowHeight=1;
    float stableYaw;
    Camera view; GameObject cockpit; Transform steeringWheel; bool snap=true;
    void Start() {
        view=GetComponent<Camera>();taxi=target.GetComponent<TaxiController>();cockpit=TaxiArt.Cockpit(target,out steeringWheel);
        if(taxi.business&&taxi.business.SupportsCareer){
            CockpitPresentation.AddFatherPhoto(cockpit.transform);
            driverGlass=CockpitPresentation.AddDriverWindow(cockpit.transform);
        }
        ApplyMode();
    }
    // Camera selection never changes the driver's explicit H assistance choice.
    public void SetFirstPerson(bool value){FirstPerson=value;snap=true;if(view)ApplyMode();}
    void ApplyMode(){view.cullingMask=FirstPerson?~(1<<8):~0;view.fieldOfView=appliedFov=FirstPerson?DriverFieldOfView:62;cockpit.SetActive(FirstPerson&&!RearView);}
    public RenderTexture CaptureCockpitFrame(){
        if(!view||!target||!cockpit)return null;
        var frame=new RenderTexture(480,270,24){name="Current cockpit comparison"};
        var capture=new GameObject("Cockpit comparison camera").AddComponent<Camera>();
        bool cockpitWasActive=cockpit.activeSelf;
        try{
            capture.CopyFrom(view);
            capture.enabled=false;
            capture.targetTexture=frame;
            capture.rect=new Rect(0,0,1,1);
            capture.aspect=(float)frame.width/frame.height;
            capture.cullingMask=~(1<<8);
            capture.fieldOfView=DriverFieldOfView;
            float yaw=target.eulerAngles.y;
            capture.transform.SetPositionAndRotation(target.position+Quaternion.Euler(0,yaw,0)*DriverEye,Quaternion.Euler(DriverPitch,yaw,0));
            cockpit.SetActive(true);
            capture.Render();
            return frame;
        }catch(System.Exception error){
            Debug.LogWarning("운전석 비교 화면을 촬영할 수 없습니다. " + error.Message);
            frame.Release();
            Destroy(frame);
            return null;
        }finally{
            cockpit.SetActive(cockpitWasActive);
            capture.targetTexture=null;
            Destroy(capture.gameObject);
        }
    }
    // Opening cutscene: renders a live shot of the parked taxi into the supplied texture.
    // cockpitView=false: slow dolly toward the taxi from the front-left (home garage).
    // cockpitView=true: driver's seat, starting on the father's photo and settling on the windshield.
    Camera introCapture;
    public bool RenderIntroShot(RenderTexture frame,bool cockpitView,float t){
        if(!view||!target||!cockpit||!frame)return false;
        if(!introCapture){
            introCapture=new GameObject("Intro shot camera").AddComponent<Camera>();
            introCapture.CopyFrom(view);
            introCapture.enabled=false;
        }
        bool cockpitWasActive=cockpit.activeSelf;
        try{
            introCapture.targetTexture=frame;
            introCapture.rect=new Rect(0,0,1,1);
            introCapture.aspect=(float)frame.width/frame.height;
            float k=Mathf.SmoothStep(0,1,t);
            float yaw=target.eulerAngles.y;var flat=Quaternion.Euler(0,yaw,0);
            if(cockpitView){
                introCapture.cullingMask=~(1<<8);
                introCapture.fieldOfView=DriverFieldOfView;
                var eye=target.position+flat*DriverEye;
                var ahead=Quaternion.Euler(DriverPitch,yaw,0);
                var photo=cockpit.transform.Find("아버지 사진 액자");
                var start=photo?Quaternion.LookRotation(photo.position-eye,Vector3.up):Quaternion.Euler(14,yaw+20,0);
                introCapture.transform.SetPositionAndRotation(eye,Quaternion.Slerp(start,ahead,Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.25f)/.6f))));
                cockpit.SetActive(true);
            }else{
                introCapture.cullingMask=~0;
                introCapture.fieldOfView=48;
                var from=target.position+flat*new Vector3(-3.4f,1.6f,7.8f);
                var to=target.position+flat*new Vector3(-2.3f,1.25f,5.2f);
                var pos=Vector3.Lerp(from,to,k);
                introCapture.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(target.position+Vector3.up*.8f-pos,Vector3.up));
                cockpit.SetActive(false);
            }
            introCapture.Render();
            return true;
        }catch(System.Exception error){
            Debug.LogWarning("인트로 장면을 촬영할 수 없습니다. "+error.Message);
            return false;
        }finally{
            cockpit.SetActive(cockpitWasActive);
            introCapture.targetTexture=null;
        }
    }
    void OnDestroy(){if(introCapture)Destroy(introCapture.gameObject);}
    void Update() {
        var business=taxi?taxi.business:null;
        if(business&&business.TryGetComponent<IntroCutscene>(out var intro)&&intro.BlocksInput)return;
        if(Input.GetKeyDown(KeyCode.C)||Input.GetKeyDown(KeyCode.JoystickButton6))SetFirstPerson(!FirstPerson);
        if(Input.GetKeyDown(KeyCode.V))cornerLook=!cornerLook;
        bool rear=testRearView||Input.GetKey(KeyCode.B)||Input.GetKey(KeyCode.JoystickButton9);
        if(rear!=RearView){RearView=rear;snap=true;ApplyMode();}
    }
    void LateUpdate() {
        if(!target)return;
        if(driverGlass){
            windowHeight=Mathf.MoveTowards(windowHeight,taxi.business.Cabin.WindowOpen?0:1,Time.deltaTime*2);
            driverGlass.localScale=new Vector3(1.04f,.48f*windowHeight,1);
        }
        if(steeringWheel)steeringWheel.localRotation=Quaternion.Euler(0,0,-taxi.SteeringInput*100);
        var feel=DrivingSettings.Current;float dt=Mathf.Max(Time.deltaTime,1e-4f),speed=taxi.Speed,kph=Mathf.Abs(speed)*3.6f;
        float rawLong=snap?0:Mathf.Clamp((speed-lastSpeed)/dt,-12,12);lastSpeed=speed;
        float rawLat=taxi.body?Mathf.Clamp(taxi.body.angularVelocity.y*speed,-12,12):0;
        longitudinal=Mathf.Lerp(longitudinal,rawLong,1-Mathf.Exp(-6*dt));lateral=Mathf.Lerp(lateral,rawLat,1-Mathf.Exp(-6*dt));
        // Speed field of view: up to +6 (cockpit) / +8 (chase) degrees from 20 to 80 km/h. A probe or
        // cutscene that set its own lens keeps it: only the value this camera applied is adjusted.
        SpeedFovOffset=RearView?0:feel.speedFov*(FirstPerson?6:8)*Mathf.SmoothStep(0,1,(kph-20)/60);
        if(view&&Mathf.Abs(view.fieldOfView-appliedFov)<.01f){appliedFov=Mathf.Lerp(view.fieldOfView,(FirstPerson?DriverFieldOfView:62)+SpeedFovOffset,1-Mathf.Exp(-3*dt));view.fieldOfView=appliedFov;}
        // Braking dip: deceleration in the travel direction, up to 2.5 degrees.
        float decel=speed>.5f?Mathf.Max(0,-longitudinal):speed< -.5f?Mathf.Max(0,longitudinal):0;
        dip=Mathf.Lerp(dip,feel.brakeDip*taxi.VehicleDip*Mathf.Min(2.5f,decel*.35f),1-Mathf.Exp(-8*dt));BrakeDip=dip;
        // Visible body lean on the exterior art only: roll toward the outside of a turn (up to 2 degrees),
        // nose down when braking and up when accelerating (up to 1.2 degrees).
        if(!exterior){exterior=target.Find("Taxi exterior");if(exterior)exteriorRest=exterior.localRotation;}
        BodyLean=new Vector2(feel.bodyLean*taxi.VehicleLean*Mathf.Clamp(-longitudinal*.15f,-1.2f,1.2f),feel.bodyLean*taxi.VehicleLean*Mathf.Clamp(lateral*.25f,-2f,2f));
        if(exterior)exterior.localRotation=Quaternion.Euler(BodyLean.x,0,BodyLean.y)*exteriorRest;
        if(FirstPerson){
            float yaw=target.eulerAngles.y;
            stableYaw=snap?yaw:Mathf.LerpAngle(stableYaw,yaw,1-Mathf.Exp(-14*Time.deltaTime));
            // Keep the eye attached to the driver, but do not inherit suspension roll/pitch.
            transform.position=target.position+Quaternion.Euler(0,yaw,0)*DriverEye;
            float manual=testLook!=0?testLook:(Input.GetKey(KeyCode.Z)?-1:Input.GetKey(KeyCode.X)?1:Input.GetAxisRaw("TaxiLook"));
            // V toggles the steering lead; its strength is the "steering look" setting.
            float desiredYaw=Mathf.Abs(manual)>.15f?manual*65:cornerLook?taxi.SteeringInput*6*feel.steerLook*Mathf.Clamp01(Mathf.Abs(taxi.Speed)/3):0;
            LookYaw=Mathf.Lerp(LookYaw,desiredYaw,1-Mathf.Exp(-5*Time.deltaTime));
            transform.rotation=Quaternion.Euler(DriverPitch+(RearView?0:dip),stableYaw+(RearView?180:LookYaw),RearView?0:BodyLean.y*.5f);snap=false;return;
        }
        var focus=target.position+Vector3.up*1.3f;var forward=RearView?-target.forward:target.forward;var desired=focus-forward*9+Vector3.up*4.5f;
        RaycastHit hit;var delta=desired-focus;if(Physics.SphereCast(focus,.25f,delta.normalized,out hit,delta.magnitude,~((1<<8)|(1<<9))))desired=focus+delta.normalized*Mathf.Max(1,hit.distance-.3f);
        transform.position=snap?desired:Vector3.Lerp(transform.position,desired,1-Mathf.Exp(-7*Time.deltaTime));
        // Chase lead: aim up to 8 degrees toward the steering side; braking tips the aim down slightly.
        float lead=RearView||!cornerLook?0:taxi.SteeringInput*8*feel.steerLook*Mathf.Clamp01(kph/20);
        transform.LookAt(focus+Quaternion.AngleAxis(lead,Vector3.up)*forward*3-Vector3.up*(RearView?0:dip*.12f));snap=false;
    }
}
