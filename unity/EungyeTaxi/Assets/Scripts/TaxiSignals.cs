using UnityEngine;
[System.Serializable] public sealed class TaxiSignalsSnapshot {
    public int direction;public bool manualHazards,passengerStop;public ManualIndicatorState cancellation;
}
public class TaxiSignals : MonoBehaviour {
    public int direction { get; private set; }
    bool manualHazards,passengerStop;
    readonly ManualIndicatorPolicy cancellation=new ManualIndicatorPolicy();
    TaxiController taxi;Rigidbody taxiBody;AssistedDriveController assistance;
    void Awake(){taxi=GetComponent<TaxiController>();taxiBody=GetComponent<Rigidbody>();assistance=GetComponent<AssistedDriveController>();}
    float MotionHeading(){var v=taxiBody?taxiBody.velocity:Vector3.zero;return v.sqrMagnitude>.25f&&Vector3.Dot(v,transform.forward)>.5f?Mathf.Atan2(v.x,v.z)*Mathf.Rad2Deg:transform.eulerAngles.y;}
    public TaxiSignalsSnapshot CaptureLive(){return new TaxiSignalsSnapshot{direction=direction,manualHazards=manualHazards,passengerStop=passengerStop,cancellation=cancellation.Capture()};}
    public void RestoreLive(TaxiSignalsSnapshot saved){
        Clear();if(saved==null)return;
        if(saved.direction< -1||saved.direction>1||(saved.manualHazards||saved.passengerStop)&&saved.direction!=0)throw new System.ArgumentException("Invalid taxi signal state");
        cancellation.Restore(saved.cancellation);direction=saved.direction;manualHazards=saved.manualHazards;passengerStop=saved.passengerStop;
        if(saved.cancellation!=null&&saved.cancellation.direction!=0&&saved.cancellation.direction!=direction)throw new System.ArgumentException("Inconsistent taxi signal cancellation");
    }
    public void ObserveManualMotion(float signedSpeed,float heading,float steering,Vector3 position,float seconds,bool assisted){
        if(cancellation.Observe(signedSpeed,heading,steering,position.x,position.z,seconds,Hazards,assisted))direction=0;
    }
    public bool Hazards {get{return manualHazards||passengerStop;}}
    public bool Lit { get { return (direction!=0||Hazards) && (Time.time%.8f)<.4f; } }
    Renderer[] left=new Renderer[2],right=new Renderer[2];Material on,off;
    void Start() {
        on=WorldBuilder.Mat("Indicator amber",new Color(1,.48f,.015f));on.EnableKeyword("_EMISSION");on.SetColor("_EmissionColor",new Color(1,.35f,0)*3);
        off=WorldBuilder.Mat("Indicator dark",new Color(.25f,.1f,.015f));
        for(int side=-1;side<=1;side+=2)for(int end=0;end<2;end++) {
            var lamp=WorldBuilder.Box("Taxi indicator",new Vector3(side*.84f,.47f,end==0?2.03f:-2.03f),new Vector3(.18f,.2f,.06f),off,transform,false);lamp.layer=gameObject.layer;
            (side<0?left:right)[end]=lamp.GetComponent<Renderer>();
        }
    }
    public void Toggle(int requested){manualHazards=passengerStop=false;direction=TrafficRules.ToggleIndicator(direction,requested);cancellation.Begin(direction,MotionHeading(),transform.position.x,transform.position.z);}
    public void ToggleHazards(){bool enabled=!Hazards;manualHazards=enabled;passengerStop=false;if(enabled){direction=0;cancellation.Reset();}}
    public void BeginPassengerStop(){passengerStop=true;direction=0;cancellation.Reset();}
    public void Clear(){direction=0;manualHazards=passengerStop=false;cancellation.Reset();}
    public void ObserveMovement(float speed){if(passengerStop&&speed>1)passengerStop=false;}
    void Update() {
        if(Time.timeScale>0){
            if(!taxi)taxi=GetComponent<TaxiController>();if(!taxiBody)taxiBody=GetComponent<Rigidbody>();if(!assistance)assistance=GetComponent<AssistedDriveController>();
            if(taxiBody)ObserveMovement(taxiBody.velocity.magnitude);
            if(taxi&&taxiBody)ObserveManualMotion(taxi.Speed,MotionHeading(),taxi.SteeringInput,transform.position,Time.deltaTime,assistance&&assistance.Active);
        }
        if(Time.timeScale>0 && !(taxi&&taxi.business&&taxi.business.session.IsSimulationBot)) {
            if(Input.GetKeyDown(KeyCode.G))ToggleHazards();
            if(Input.GetKeyDown(KeyCode.Q)||Input.GetKeyDown(KeyCode.JoystickButton4)){if(assistance&&assistance.Active)assistance.RequestTurn(-1);else Toggle(-1);}
            if(Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.JoystickButton5)){if(assistance&&assistance.Active)assistance.RequestTurn(1);else Toggle(1);}
        }
        foreach(var lamp in left)if(lamp)lamp.sharedMaterial=(Hazards||direction<0)&&Lit?on:off;
        foreach(var lamp in right)if(lamp)lamp.sharedMaterial=(Hazards||direction>0)&&Lit?on:off;
    }
}
