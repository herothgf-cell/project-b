using UnityEngine;
public class TaxiSignals : MonoBehaviour {
    public int direction { get; private set; }
    bool manualHazards,passengerStop;
    public bool Hazards {get{return manualHazards||passengerStop;}}
    public bool Lit { get { return (direction!=0||Hazards) && (Time.time%.8f)<.4f; } }
    Renderer[] left=new Renderer[2],right=new Renderer[2];Material on,off;
    void Start() {
        on=WorldBuilder.Mat("Indicator amber",new Color(1,.48f,.015f));on.EnableKeyword("_EMISSION");on.SetColor("_EmissionColor",new Color(1,.35f,0)*3);
        off=WorldBuilder.Mat("Indicator dark",new Color(.25f,.1f,.015f));
        for(int side=-1;side<=1;side+=2)for(int end=0;end<2;end++) {
            var lamp=WorldBuilder.Box("Taxi indicator",new Vector3(side*.84f,.47f,end==0?2.03f:-2.03f),new Vector3(.18f,.2f,.06f),off,transform,false);lamp.layer=8;
            (side<0?left:right)[end]=lamp.GetComponent<Renderer>();
        }
    }
    public void Toggle(int requested){manualHazards=passengerStop=false;direction=TrafficRules.ToggleIndicator(direction,requested);}
    public void ToggleHazards(){bool enabled=!Hazards;manualHazards=enabled;passengerStop=false;if(enabled)direction=0;}
    public void BeginPassengerStop(){passengerStop=true;direction=0;}
    public void Clear(){direction=0;manualHazards=passengerStop=false;}
    void Update() {
        if(Time.timeScale>0) {
            if(passengerStop){var body=GetComponent<Rigidbody>();if(body&&body.velocity.magnitude>1)passengerStop=false;}
            if(Input.GetKeyDown(KeyCode.G))ToggleHazards();
            var assistance=GetComponent<AssistedDriveController>();
            if(Input.GetKeyDown(KeyCode.Q)||Input.GetKeyDown(KeyCode.JoystickButton4)){if(assistance&&assistance.Active)assistance.RequestTurn(-1);else Toggle(-1);}
            if(Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.JoystickButton5)){if(assistance&&assistance.Active)assistance.RequestTurn(1);else Toggle(1);}
        }
        foreach(var lamp in left)if(lamp)lamp.sharedMaterial=(Hazards||direction<0)&&Lit?on:off;
        foreach(var lamp in right)if(lamp)lamp.sharedMaterial=(Hazards||direction>0)&&Lit?on:off;
    }
}
