using UnityEngine;
public class DrivingCamera : MonoBehaviour {
    static readonly Vector3 DriverEye = new Vector3(-.43f,1.07f,.6f);
    const float DriverPitch = 2f, DriverFieldOfView = 82f;
    public Transform target;
    public bool FirstPerson { get; private set; }
    public bool RearView { get; private set; }
    public bool testRearView;public float testLook;public float LookYaw {get;private set;}public bool cornerLook=false;TaxiController taxi;
    float stableYaw;
    Camera view; GameObject cockpit; Transform steeringWheel; bool snap=true;
    void Start() {
        view=GetComponent<Camera>();taxi=target.GetComponent<TaxiController>();cockpit=TaxiArt.Cockpit(target,out steeringWheel);
        if(taxi.business&&taxi.business.SupportsCareer)CockpitPresentation.AddFatherPhoto(cockpit.transform);
        ApplyMode();
    }
    // Camera selection never changes the driver's explicit H assistance choice.
    public void SetFirstPerson(bool value){FirstPerson=value;snap=true;if(view)ApplyMode();}
    void ApplyMode(){view.cullingMask=FirstPerson?~(1<<8):~0;view.fieldOfView=FirstPerson?DriverFieldOfView:62;cockpit.SetActive(FirstPerson&&!RearView);}
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
        if(steeringWheel)steeringWheel.localRotation=Quaternion.Euler(0,0,-taxi.SteeringInput*100);
        if(FirstPerson){
            float yaw=target.eulerAngles.y;
            stableYaw=snap?yaw:Mathf.LerpAngle(stableYaw,yaw,1-Mathf.Exp(-14*Time.deltaTime));
            // Keep the eye attached to the driver, but do not inherit suspension roll/pitch.
            transform.position=target.position+Quaternion.Euler(0,yaw,0)*DriverEye;
            float manual=testLook!=0?testLook:(Input.GetKey(KeyCode.Z)?-1:Input.GetKey(KeyCode.X)?1:Input.GetAxisRaw("TaxiLook"));
            float desiredYaw=Mathf.Abs(manual)>.15f?manual*65:cornerLook?taxi.SteeringInput*6*Mathf.Clamp01(Mathf.Abs(taxi.Speed)/3):0;
            LookYaw=Mathf.Lerp(LookYaw,desiredYaw,1-Mathf.Exp(-5*Time.deltaTime));
            transform.rotation=Quaternion.Euler(DriverPitch,stableYaw+(RearView?180:LookYaw),0);snap=false;return;
        }
        var focus=target.position+Vector3.up*1.3f;var forward=RearView?-target.forward:target.forward;var desired=focus-forward*9+Vector3.up*4.5f;
        RaycastHit hit;var delta=desired-focus;if(Physics.SphereCast(focus,.25f,delta.normalized,out hit,delta.magnitude,~((1<<8)|(1<<9))))desired=focus+delta.normalized*Mathf.Max(1,hit.distance-.3f);
        transform.position=snap?desired:Vector3.Lerp(transform.position,desired,1-Mathf.Exp(-7*Time.deltaTime));transform.LookAt(focus+forward*3);snap=false;
    }
}
