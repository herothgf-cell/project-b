using UnityEngine;
public class DrivingCamera : MonoBehaviour {
    public Transform target;
    public bool FirstPerson { get; private set; }
    public bool RearView { get; private set; }
    public bool testRearView;public float testLook;public float LookYaw {get;private set;}public bool cornerLook=true;TaxiController taxi;
    Camera view; GameObject cockpit; bool snap=true;
    void Start() {
        view=GetComponent<Camera>();taxi=target.GetComponent<TaxiController>();cockpit=new GameObject("Temporary cockpit");cockpit.transform.SetParent(target,false);cockpit.transform.localPosition=new Vector3(-.43f,1.07f,.6f);
        var trim=WorldBuilder.Mat("Dashboard",new Color(.055f,.065f,.075f));
        WorldBuilder.Box("Dashboard",new Vector3(.25f,-.6f,.65f),new Vector3(1.9f,.28f,.65f),trim,cockpit.transform,false);
        WorldBuilder.Box("Instrument top",new Vector3(0,-.4f,.57f),new Vector3(.48f,.14f,.15f),trim,cockpit.transform,false);
        var ring=new GameObject("Steering wheel");ring.transform.SetParent(cockpit.transform,false);ring.transform.localPosition=new Vector3(0,-.36f,.46f);
        for(int i=0;i<16;i++){float angle=i*Mathf.PI/8;var bar=WorldBuilder.Box("Wheel rim",new Vector3(Mathf.Sin(angle)*.16f,Mathf.Cos(angle)*.16f,0),new Vector3(.065f,.045f,.035f),trim,ring.transform,false);bar.transform.localRotation=Quaternion.Euler(0,0,-angle*Mathf.Rad2Deg);}
        WorldBuilder.Box("Wheel hub",Vector3.zero,new Vector3(.25f,.035f,.04f),trim,ring.transform,false);
        foreach(var child in cockpit.GetComponentsInChildren<Transform>())child.gameObject.layer=10;
        ApplyMode();
    }
    // Camera selection never changes the driver's explicit H assistance choice.
    public void SetFirstPerson(bool value){FirstPerson=value;snap=true;if(view)ApplyMode();}
    void ApplyMode(){view.cullingMask=FirstPerson?~(1<<8):~0;view.fieldOfView=FirstPerson?82:62;cockpit.SetActive(FirstPerson&&!RearView);}
    void Update() {
        if(Input.GetKeyDown(KeyCode.C)||Input.GetKeyDown(KeyCode.JoystickButton6))SetFirstPerson(!FirstPerson);
        if(Input.GetKeyDown(KeyCode.V))cornerLook=!cornerLook;
        bool rear=testRearView||Input.GetKey(KeyCode.B)||Input.GetKey(KeyCode.JoystickButton9);
        if(rear!=RearView){RearView=rear;snap=true;ApplyMode();}
    }
    void LateUpdate() {
        if(!target)return;
        if(FirstPerson){transform.position=target.TransformPoint(new Vector3(-.43f,1.07f,.6f));float manual=testLook!=0?testLook:(Input.GetKey(KeyCode.Z)?-1:Input.GetKey(KeyCode.X)?1:Input.GetAxisRaw("TaxiLook"));float desiredYaw=Mathf.Abs(manual)>.15f?manual*65:cornerLook?taxi.SteeringInput*18*Mathf.Clamp01(Mathf.Abs(taxi.Speed)/3):0;LookYaw=Mathf.Lerp(LookYaw,desiredYaw,1-Mathf.Exp(-5*Time.deltaTime));transform.rotation=target.rotation*Quaternion.Euler(2,RearView?180:LookYaw,0);return;}
        var focus=target.position+Vector3.up*1.3f;var forward=RearView?-target.forward:target.forward;var desired=focus-forward*9+Vector3.up*4.5f;
        RaycastHit hit;var delta=desired-focus;if(Physics.SphereCast(focus,.25f,delta.normalized,out hit,delta.magnitude,~((1<<8)|(1<<9))))desired=focus+delta.normalized*Mathf.Max(1,hit.distance-.3f);
        transform.position=snap?desired:Vector3.Lerp(transform.position,desired,1-Mathf.Exp(-7*Time.deltaTime));transform.LookAt(focus+forward*3);snap=false;
    }
}
