using System;
using System.Collections.Generic;
using UnityEngine;

// Capture motion after all driving updates, before PhysX resolves contacts.
[DefaultExecutionOrder(10000)]
public sealed class VehicleImpactMotion : MonoBehaviour {
    public Vector3 linear,angular,centre;
    public void Sample() {
        var body=GetComponent<Rigidbody>();if(!body)return;
        var traffic=GetComponent<TrafficCar>();
        linear=traffic?transform.forward*traffic.speed:body.velocity;
        angular=body.angularVelocity;centre=body.worldCenterOfMass;
    }
    void FixedUpdate(){Sample();}
    public Vector3 At(Vector3 point){return linear+Vector3.Cross(angular,point-centre);}
}

public sealed class VehicleDamageController : MonoBehaviour {
    public DriveSession session;
    public sealed class Bay {public string id;public Vector3 position,direction;}
    public readonly List<Bay> Bays=new List<Bay>();
    readonly HashSet<Collider> contacts=new HashSet<Collider>();
    public int ContactCount {get{return contacts.Count;}}
    VehicleImpactMotion motion;
    public string Notice {get;private set;}
    float noticeUntil,disabledSince=-1,fadeUntil;
    bool baysBuilt;
    public bool RestoringReservedBay {get;private set;}
    float nextRestoreAttempt;
    public VehicleDamage Damage {get{return session.business.ledger.Care.Damage;}}
    public bool Disabled {get{return session&&session.business&&session.business.ledger!=null&&Damage.Disabled&&!PracticeSession.Enabled&&!session.PracticeBusy;}}
    bool Campaign {get{return session&&session.StartupReady&&session.business&&!session.business.IsRestoring&&session.business.enabledCampaign&&session.business.ledger.Account!=null&&!PracticeSession.Enabled&&!session.PracticeBusy&&!RoadMapRuntime.Trial;}}
    void Start(){motion=GetComponent<VehicleImpactMotion>()??gameObject.AddComponent<VehicleImpactMotion>();motion.Sample();foreach(var body in FindObjectsOfType<Rigidbody>())if(!body.GetComponent<VehicleImpactMotion>())body.gameObject.AddComponent<VehicleImpactMotion>();BuildBays();Damage.Suppress(1);}
    public static float ClosingSpeed(Vector3 relative,Vector3 normal){return normal.sqrMagnitude<.01f?0:Mathf.Max(0,-Vector3.Dot(relative,normal.normalized));}
    public static bool SupportingContact(Collider collider,Vector3 normal,float height){return normal.y>.65f||collider&&!collider.attachedRigidbody&&collider.bounds.size.y<.4f&&collider.bounds.max.y<height+.2f;}
    void OnCollisionEnter(Collision collision){Observe(collision);}
    void OnCollisionStay(Collision collision){Observe(collision);}
    void OnCollisionExit(Collision collision){contacts.Remove(collision.collider);}
    void Observe(Collision collision){
        if(!Campaign||!session.business.ledger.Operating||session.business.preparing||session.paused||Time.timeScale<=0)return;
        float speed=0;bool contact=false;var other=collision.collider.GetComponentInParent<VehicleImpactMotion>();
        for(int i=0;i<collision.contactCount;i++){
            var point=collision.GetContact(i);if(SupportingContact(collision.collider,point.normal,transform.position.y))continue;
            contact=true;
            // Unknown dynamic bodies use PhysX's pre-solver relative velocity.
            var relative=motion?motion.At(point.point):collision.relativeVelocity;
            if(other)relative-=other.At(point.point);else if(collision.rigidbody)relative=-collision.relativeVelocity;
            speed=Mathf.Max(speed,ClosingSpeed(relative,point.normal));
        }
        if(!contact)return;contacts.Add(collision.collider);ApplyImpact(speed);
    }
    public int ApplyImpact(float closingSpeed){
        if(!Campaign)return 0;int loss=Damage.Impact(closingSpeed);if(loss<=0)return 0;
        Notice="충돌 내구도 −"+loss+" · "+Damage.Durability+"/100"+(Disabled?" · 운행 불가":" · 수리비/시간 발생");noticeUntil=Time.unscaledTime+6;
        Debug.Log("VEHICLE_IMPACT closing="+closingSpeed.ToString("0.000")+" loss="+loss+" durability="+Damage.Durability);
        if(Disabled){disabledSince=Time.time;session.assisted.SetActive(false);session.services.open=false;session.services.pending=-1;}
        session.business.SaveCheckpoint();return loss;
    }
    public void TechnicalRecovery(){contacts.Clear();if(session&&session.business&&session.business.ledger!=null)Damage.Suppress(1);if(motion)motion.Sample();}
    void FixedUpdate(){
        if(!Campaign)return;contacts.RemoveWhere(c=>!c);Damage.Advance(Time.fixedDeltaTime,contacts.Count>0);
        if(!baysBuilt)BuildBays();
        // A paid reservation lasts until repair; temporary displacement never revokes it.
        if(!Disabled){disabledSince=-1;return;}
        if(disabledSince<0)disabledSince=Time.time;
        if(session.paused||Time.timeScale<=0)return;
        if(session.taxi.body.velocity.magnitude<.56f)session.jobs.ResolveVehicleFailure();
    }
    public void BuildBays(){
        if(baysBuilt||!session||!session.services)return;baysBuilt=true;
        foreach(var station in session.services.Stations){
            var position=station.pump+station.direction*13+TrafficSystem.Right(station.direction)*4;
            // A dedicated space entirely inside the existing service forecourt.
            bool inside=true;foreach(float x in new[]{-1.5f,1.5f})foreach(float z in new[]{-2.8f,2.8f})if(!Inside(position+TrafficSystem.Right(station.direction)*x+station.direction*z,station.plot))inside=false;
            foreach(var stop in session.jobs.stops)if(Vector3.Distance(position,stop.Kerb)<6)inside=false;
            if(!inside)continue;var bay=new Bay{id="repair:"+station.id,position=position,direction=station.direction};Bays.Add(bay);
            var root=new GameObject("견인 전용 정비 공간 "+bay.id);root.transform.position=position;root.transform.rotation=Quaternion.LookRotation(bay.direction);
            var paint=WorldBuilder.Mat("Repair bay orange",new Color(1,.55f,.1f));
            foreach(float x in new[]{-1.5f,1.5f})WorldBuilder.Box("정비 전용선",new Vector3(x,.06f,0),new Vector3(.12f,.02f,5.6f),paint,root.transform,false);
            var sign=new GameObject("정비 안내");sign.transform.SetParent(root.transform,false);sign.transform.localPosition=new Vector3(0,2,0);var label=sign.AddComponent<TextMesh>();label.text="견인·정비 전용\n일반 차량 진입 금지";label.fontSize=32;label.characterSize=.08f;label.anchor=TextAnchor.MiddleCenter;label.color=Color.yellow;KoreanText.Apply(label);sign.AddComponent<FaceCamera>();
        }
    }
    static bool Inside(Vector3 p,Vector3[] ring){bool inside=false;for(int i=0,j=ring.Length-1;i<ring.Length;j=i++){var a=ring[i];var b=ring[j];if((a.z>p.z)!=(b.z>p.z)&&p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z)+a.x)inside=!inside;}return inside;}
    public bool BayClear(Bay bay){
        foreach(var collider in Physics.OverlapBox(bay.position+Vector3.up*.65f,new Vector3(1.3f,.5f,2.5f),Quaternion.LookRotation(bay.direction),~0,QueryTriggerInteraction.Ignore)){
            if(collider.attachedRigidbody==session.taxi.body||collider.bounds.max.y<=bay.position.y+.16f)continue;
            return false;
        }
        return true;
    }
    Bay CurrentBay {get{return Bays.Find(b=>b.id==Damage.RepairBayId);}}
    public bool AtRepairBay {get{var b=CurrentBay;return b!=null&&Vector3.Distance(transform.position,b.position)<3&&Vector3.Dot(transform.forward,b.direction)>.85f;}}
    public bool RestoreReservedBay(bool checkpoint=true){
        BuildBays();if(!Damage.Disabled||string.IsNullOrEmpty(Damage.RepairBayId)){RestoringReservedBay=false;return false;}
        var bay=CurrentBay;
        if(bay==null){Damage.LeaveRepairBay();RestoringReservedBay=false;Notice="저장된 정비 공간이 현재 지도에 없습니다 · 견인 지원을 확인해 주세요";return false;}
        if(AtRepairBay){RestoringReservedBay=false;return true;}
        RestoringReservedBay=true;
        if(!BayClear(bay)){Notice="이미 지불한 정비 공간 복원 대기 · 공간이 비면 복귀 · 추가 요금 없음";return false;}
        if(session.taxi.body.velocity.magnitude>=.56f){if(disabledSince<0)disabledSince=Time.time;if(Time.time-disabledSince<5){Notice="정차 후 지불한 정비 공간으로 복귀합니다 · 추가 요금 없음";return false;}session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;fadeUntil=Time.unscaledTime+.4f;}
        session.taxi.SetSpawn(bay.position+Vector3.up*.15f,Quaternion.LookRotation(bay.direction));session.taxi.ResetTaxi();RestoringReservedBay=false;
        Notice="이미 지불한 정비 공간 복원 완료 · 추가 견인비/시간 없음";noticeUntil=Time.unscaledTime+6;if(checkpoint)session.business.SaveCheckpoint();return true;
    }
    void Update(){if(!session||!session.business||session.business.ledger==null)return;if(!Damage.Disabled||string.IsNullOrEmpty(Damage.RepairBayId)){RestoringReservedBay=false;return;}if(Campaign&&!AtRepairBay&&Time.unscaledTime>=nextRestoreAttempt){nextRestoreAttempt=Time.unscaledTime+.5f;RestoreReservedBay();}}
    public bool CanRepair {get{return Campaign&&session.business.ledger.Operating&&Damage.Durability<100&&session.jobs.trip.State!=TripState.OnTrip&&session.taxi.body.velocity.magnitude<.56f&&(AtRepairBay||session.services.AtFuelPump());}}
    public string RepairQuote {get{return "내구도 "+Damage.Durability+" → 100\n수리 "+Damage.RepairCost.ToString("N0")+"원 · "+Damage.RepairSeconds.ToString("0.#")+"초 (부족분 미납)\n청결·연료 유지 · 엔진 정지";}}
    public bool Repair(){
        if(!CanRepair)return false;double seconds=Damage.RepairSeconds;int cost=Damage.RepairCost;
        if(!session.business.ledger.UseRepair(session.law.ids.Next("repair")))return false;
        session.jobs.AdvancePickupWait((float)seconds);TechnicalRecovery();Notice="수리 완료 · 내구도 100 · "+cost.ToString("N0")+"원 / "+seconds.ToString("0.#")+"초";noticeUntil=Time.unscaledTime+6;session.business.SaveCheckpoint();return true;
    }
    public bool Tow(){
        if(!Campaign||!Disabled||!session.business.ledger.Operating||session.business.ShowingSummary||AtRepairBay||RestoringReservedBay||session.FullMapOpen)return false;
        if(session.taxi.body.velocity.magnitude>=.56f&&Time.time-disabledSince<5){Notice="제동 후 정차하면 견인할 수 있습니다";return false;}
        Bay free=null;float distance=float.MaxValue;foreach(var bay in Bays)if(BayClear(bay)){float d=(bay.position-transform.position).sqrMagnitude;if(d<distance){free=bay;distance=d;}}
        if(free==null){Notice="견인 전용 공간이 사용 중입니다 · 비면 다시 신청해 주세요";return false;}
        // No yields between reservation, accounting, placement and durable checkpoint.
        session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;
        bool fixedArrival=session.jobs.trip.State==TripState.OnTrip&&session.jobs.trip.ArrivalLocked;
        if(!fixedArrival)session.jobs.ResolveVehicleFailure();
        if(session.jobs.Active&&!fixedArrival){Notice="승객의 안전 하차를 확인하고 다시 신청해 주세요";return false;}
        if(!session.business.ledger.UseTow(session.law.ids.Next("tow"),free.id))return false;
        session.taxi.SetSpawn(free.position+Vector3.up*.15f,Quaternion.LookRotation(free.direction));session.taxi.ResetTaxi();session.jobs.nav.Clear();fadeUntil=Time.unscaledTime+.4f;
        if(fixedArrival)session.jobs.ResolveVehicleFailure();
        Notice="견인 완료 · 3,000원 / 30초 · "+(session.jobs.Active?"확정 운임 유지 · 안전 하역 후 수리":"수리는 별도입니다");noticeUntil=Time.unscaledTime+6;session.business.SaveCheckpoint();return true;
    }
    public bool EndDisabledDay(){
        if(!Campaign||!Disabled||!AtRepairBay||session.jobs.Active)return false;
        var ledger=session.business.ledger;
        if(ledger.State==BusinessState.Open&&ledger.OneDay){
            if(ledger.Driver.drowsiness.Blackout)return session.business.RequestEmergencyHome();
            if(!ledger.UseSleep(session.law.ids.Next("disabled-day-rest")))return false;
        }
        ledger.RequestClose("차량 고장 · 정비 보류");session.business.SaveCheckpoint();return true;
    }
    void OnGUI(){
        if(!Campaign||session.business.preparing||session.business.ShowingSummary||session.RideResultOpen||session.FullMapOpen||session.PracticeMenuOpen)return;
        var style=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};style.normal.textColor=Color.white;GUI.depth=-140;
        if(Time.unscaledTime<noticeUntil&&!Disabled)GUI.Label(new Rect(Screen.width/2-230,165,460,65),Notice,style);
        if(Disabled&&!session.services.open&&!session.paused){
            float w=Mathf.Min(580,Screen.width-30),x=(Screen.width-w)/2,y=(Screen.height-290)/2;GUI.depth=-150;GUI.color=new Color(.06f,.09f,.12f,.98f);GUI.DrawTexture(new Rect(x,y,w,290),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(x+18,y+12,w-36,110),"차량 내구도 0 · 운행 불가\n"+(AtRepairBay?RepairQuote:"정차 후 견인 3,000원 · 30초 (부족분 미납)\n전용 정비 공간으로 이동 · 수리비/시간 별도\n정차 불가 5초 후에는 화면 전환으로 안전 복구"),style);
            GUI.Label(new Rect(x+18,y+128,w-36,55),Notice??"승객은 안전 정차 후 하차합니다. 운송 중 고장은 요금 0원 취소입니다.",style);
            if(AtRepairBay){GUI.enabled=CanRepair;if(GUI.Button(new Rect(x+18,y+192,w-36,36),session.jobs.Active?"안전 하역 중 · 잠시 기다려 주세요":"전량 수리 확정"))Repair();GUI.enabled=!session.jobs.Active;if(GUI.Button(new Rect(x+18,y+237,w-36,34),"정비를 미루고 휴식 후 오늘 영업 종료 · 100초"))EndDisabledDay();GUI.enabled=true;}
            else {GUI.enabled=!RestoringReservedBay;if(GUI.Button(new Rect(x+18,y+215,w-36,45),RestoringReservedBay?"지불한 정비 공간 복원 대기 · 추가 요금 없음":"견인 신청 · 3,000원 / 30초"))Tow();GUI.enabled=true;}
        }
        if(Time.unscaledTime<fadeUntil){GUI.depth=-2000;GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;}
    }
}
