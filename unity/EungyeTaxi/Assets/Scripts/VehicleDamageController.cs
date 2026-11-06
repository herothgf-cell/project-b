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
    public sealed class Bay {public string id;public Vector3 position,direction;public bool legacy;}
    public readonly List<Bay> Bays=new List<Bay>();
    readonly HashSet<Collider> contacts=new HashSet<Collider>();
    public int ContactCount {get{return contacts.Count;}}
    VehicleImpactMotion motion;
    public string Notice {get;private set;}
    float noticeUntil,disabledSince=-1,fadeUntil;
    bool baysBuilt;
    int pendingWorkshopKind;float pendingWorkshopSeconds;string pendingWorkshopId;Bay pendingWorkshopBay;Vector3 pendingWorkshopPosition,repairedPosition;float repairedAt=-1;WorkshopLayout.Site repairedSite;
    public float PendingServiceRemaining {get{return pendingWorkshopKind==0?0:Mathf.Max(.001f,pendingWorkshopSeconds);}}
    int networkWorkshopKind;float networkWorkshopRemaining,networkWorkshopNoticeUntil;string networkWorkshopNotice="";
    public int PendingServiceKind {get{return pendingWorkshopKind;}}
    public float DisplayedServiceRemaining {get{return PvpNativeLan.IsReplica(session)?networkWorkshopRemaining:PendingServiceRemaining;}}
    public string DisplayedServiceNotice {get{return PvpNativeLan.IsReplica(session)?networkWorkshopNotice:Notice;}}
    // Presentation only: never starts a timer, reserves a bay or changes money.
    public void ApplyWorkshopNetworkView(int kind,float remaining,string notice){
        int valid=kind==1||kind==2?kind:0;
        networkWorkshopKind=valid;networkWorkshopRemaining=valid==0||float.IsNaN(remaining)||float.IsInfinity(remaining)?0:Mathf.Max(0,remaining);
        string text=notice??"";if(text.Length==0&&valid!=0)text=(valid==1?"공업사 수리":"공업사 견인")+" 진행 중 · 완료 시 결제";
        if(text!=networkWorkshopNotice)networkWorkshopNoticeUntil=Time.unscaledTime+6;networkWorkshopNotice=text;
    }
    public void CancelPendingService(){if(pendingWorkshopKind==2)Reservations.Release(GetInstanceID());pendingWorkshopKind=0;pendingWorkshopSeconds=0;pendingWorkshopId=null;pendingWorkshopBay=null;}
    bool BeginWorkshopService(int kind,float seconds,Bay bay=null){pendingWorkshopKind=kind;pendingWorkshopSeconds=seconds;pendingWorkshopBay=bay;pendingWorkshopPosition=transform.position;pendingWorkshopId=session.law.ids.Next(kind==1?"repair":"tow");session.assisted.SetActive(false);session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;session.taxi.autoThrottle=0;session.taxi.autoBrake=true;Notice=(kind==1?"공업사 수리":"공업사 견인")+" 진행 중 · 완료 시 결제";return true;}
    public bool AdvancePendingService(float seconds){
        if(pendingWorkshopKind==0)return false;
        if(!session.simulationMatch||session.simulationMatch.Finished||session.simulationMatch.Remaining<=0||session.business.ShowingSummary){CancelPendingService();return false;}
        if(session.accident&&session.accident.Locked){CancelPendingService();return false;}
        if(Vector3.Distance(transform.position,pendingWorkshopPosition)>2){CancelPendingService();Notice="서비스 중단 · 결제 없음";return false;}
        if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return true;
        float step=Mathf.Min(seconds,session.simulationMatch.Remaining);if(pendingWorkshopSeconds>0)step=Mathf.Min(step,pendingWorkshopSeconds);session.business.ledger.AdvanceAccidentProcessing(step);pendingWorkshopSeconds=Mathf.Max(0,pendingWorkshopSeconds-step);
        if(pendingWorkshopSeconds>0)return true;
        if(pendingWorkshopKind==2&&!BayClear(pendingWorkshopBay)){Notice="예약한 견인칸이 점유 중입니다 · 빈 공간 확인 후 완료 · 아직 결제 없음";return true;}
        int kind=pendingWorkshopKind;var bay=pendingWorkshopBay;string id=pendingWorkshopId;
        bool done=kind==1?session.business.ledger.CompletePvpRepair(id):session.business.ledger.CompletePvpTow(id,bay.id);
        if(!done){CancelPendingService();Notice="서비스 중단 · 결제 없음";return true;}
        pendingWorkshopKind=0;pendingWorkshopSeconds=0;pendingWorkshopBay=null;pendingWorkshopId=null;
        if(kind==1){NotifyWorkshopRepairCompleted();TechnicalRecovery();Notice="공업사 수리 완료 · 내구도 100 · 출차해 주세요";}
        else {PlaceTow(bay);Notice="공업사 견인 완료 · 수리는 별도입니다";}
        noticeUntil=Time.unscaledTime+6;session.business.SaveCheckpoint();return true;
    }
    void PlaceTow(Bay bay){session.taxi.SetSpawn(bay.position+Vector3.up*.15f,Quaternion.LookRotation(bay.direction));session.taxi.ResetTaxi();session.jobs.nav.Clear();fadeUntil=Time.unscaledTime+.4f;}
    public void NotifyWorkshopRepairCompleted(){
        Reservations.Release(GetInstanceID());repairedAt=Time.time;repairedPosition=transform.position;repairedSite=null;
        foreach(var site in session.services.Workshops)if(Vector3.Distance(transform.position,site.repair)<16){repairedSite=site;break;}
        if(session.IsSimulationBot&&!PvpNativeLan.IsRemoteHuman(session)&&repairedSite!=null){var bot=session.GetComponent<PvpSimulationBotServices>()??session.gameObject.AddComponent<PvpSimulationBotServices>();bot.LeaveWorkshop(session,repairedSite);}
    }
    void ClearRepairedBay(){
        if(!session.simulationMatch||session.IsSimulationBot&&!PvpNativeLan.IsRemoteHuman(session)||repairedAt<0||Time.time-repairedAt<10||repairedSite==null||PendingServiceRemaining>0)return;
        if(Vector3.Distance(transform.position,repairedPosition)>3||session.taxi.body.velocity.magnitude>.56f){repairedAt=-1;return;}
        var exit=new Bay{id=repairedSite.id+":departure",position=repairedSite.repair+repairedSite.direction*13+TrafficSystem.Right(repairedSite.direction)*5,direction=repairedSite.direction};
        if(!BayClear(exit))return;session.taxi.SetSpawn(exit.position+Vector3.up*.15f,Quaternion.LookRotation(exit.direction));session.taxi.ResetTaxi();repairedAt=-1;Notice="정비칸을 비웠습니다 · 같은 공업사 출차 공간에서 운행을 재개하세요";noticeUntil=Time.unscaledTime+6;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MapData,WorkshopReservations> reservations=new System.Runtime.CompilerServices.ConditionalWeakTable<MapData,WorkshopReservations>();
    WorkshopReservations Reservations {get{return reservations.GetOrCreateValue(session.world.map);}}
    void OnDestroy(){if(session&&session.world&&session.world.map!=null)Reservations.Release(GetInstanceID());}
    public bool RestoringReservedBay {get;private set;}
    float nextRestoreAttempt;
    public VehicleDamage Damage {get{return session.business.ledger.Care.Damage;}}
    public bool Disabled {get{return session&&session.business&&session.business.ledger!=null&&Damage.Disabled&&!PracticeSession.Enabled&&!session.PracticeBusy;}}
    bool Campaign {get{return session&&session.StartupReady&&session.business&&!session.business.IsRestoring&&session.business.enabledCampaign&&session.business.ledger.Account!=null&&!PracticeSession.Enabled&&!session.PracticeBusy&&!RoadMapRuntime.Trial;}}
    // The cockpit owns the scaled placement; retain the existing repair-guide eligibility.
    public bool ShowWorkshopShortcut {get{return Campaign&&!session.IsSimulationBot&&!PvpNativeLan.IsRemoteHuman(session)&&!session.business.preparing&&!session.business.ShowingSummary&&!session.RideResultOpen&&!session.FullMapOpen&&!session.PracticeMenuOpen&&!Disabled&&Damage.Durability<=50&&!session.jobs.Active&&!session.paused&&!session.services.open&&!(session.accident&&session.accident.Locked)&&PendingServiceRemaining<=0;}}
    void Start(){motion=GetComponent<VehicleImpactMotion>()??gameObject.AddComponent<VehicleImpactMotion>();motion.Sample();foreach(var body in FindObjectsOfType<Rigidbody>())if(!body.GetComponent<VehicleImpactMotion>())body.gameObject.AddComponent<VehicleImpactMotion>();BuildBays();Damage.Suppress(1);}
    public static float ClosingSpeed(Vector3 relative,Vector3 normal){return normal.sqrMagnitude<.01f?0:Mathf.Max(0,-Vector3.Dot(relative,normal.normalized));}
    public static bool SupportingContact(Collider collider,Vector3 normal,float height){return normal.y>.65f||collider&&!collider.attachedRigidbody&&collider.bounds.size.y<.4f&&collider.bounds.max.y<height+.2f;}
    void OnCollisionEnter(Collision collision){Observe(collision);}
    void OnCollisionStay(Collision collision){Observe(collision);}
    void OnCollisionExit(Collision collision){contacts.Remove(collision.collider);}
    void Observe(Collision collision){
        if(PvpNativeLan.IsReplica(session))return;
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
        if(!contact)return;contacts.Add(collision.collider);
        if(ApplyImpact(speed)>0&&session.accident){
            bool vehicle=collision.collider.GetComponentInParent<TaxiController>()||collision.collider.GetComponentInParent<TrafficCar>();
            session.accident.ReportCollision(vehicle?AccidentKind.Vehicle:AccidentKind.Property,"collision:"+collision.collider.GetInstanceID());
        }
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
        if(PvpNativeLan.IsReplica(session))return;
        if(!Campaign)return;contacts.RemoveWhere(c=>!c);Damage.Advance(Time.fixedDeltaTime,contacts.Count>0);
        if(!baysBuilt)BuildBays();
        // A paid reservation lasts until repair; temporary displacement never revokes it.
        if(!Disabled){disabledSince=-1;return;}
        if(disabledSince<0)disabledSince=Time.time;
        if(session.paused||Time.timeScale<=0)return;
        if(!(session.accident&&session.accident.Locked)&&session.taxi.body.velocity.magnitude<.56f)session.jobs.ResolveVehicleFailure();
    }
    public void BuildBays(){
        if(baysBuilt||!session||!session.services)return;baysBuilt=true;
        var workshops=session.services.Workshops;WorkshopLayout.Draw(session.world.map,session.world.transform);
        foreach(var workshop in workshops)for(int i=0;i<workshop.tow.Length;i++)Bays.Add(new Bay{id=workshop.id+":tow:"+i,position=workshop.tow[i],direction=workshop.direction});
        foreach(var station in session.services.Stations){if(!station.SupportsRepair)continue;
            var position=station.pump+station.direction*13+TrafficSystem.Right(station.direction)*4;
            // A dedicated space entirely inside the existing service forecourt.
            bool inside=true;foreach(float x in new[]{-1.5f,1.5f})foreach(float z in new[]{-2.8f,2.8f})if(!Inside(position+TrafficSystem.Right(station.direction)*x+station.direction*z,station.plot))inside=false;
            foreach(var stop in session.jobs.stops)if(Vector3.Distance(position,stop.Kerb)<6)inside=false;
            if(!inside)continue;var bay=new Bay{id="repair:"+station.id,position=position,direction=station.direction,legacy=workshops.Count>0};Bays.Add(bay);if(bay.legacy)continue;
            var root=new GameObject("견인 전용 정비 공간 "+bay.id);root.transform.position=position;root.transform.rotation=Quaternion.LookRotation(bay.direction);
            var paint=WorldBuilder.Mat("Repair bay orange",new Color(1,.55f,.1f));
            foreach(float x in new[]{-1.5f,1.5f})WorldBuilder.Box("정비 전용선",new Vector3(x,.06f,0),new Vector3(.12f,.02f,5.6f),paint,root.transform,false);
            var sign=new GameObject("정비 안내");sign.transform.SetParent(root.transform,false);sign.transform.localPosition=new Vector3(0,2,0);var label=sign.AddComponent<TextMesh>();label.text="견인·정비 전용\n일반 차량 진입 금지";label.fontSize=32;label.characterSize=.08f;label.anchor=TextAnchor.MiddleCenter;label.color=Color.yellow;KoreanText.Apply(label);sign.AddComponent<FaceCamera>();
        }
    }
    static bool Inside(Vector3 p,Vector3[] ring){bool inside=false;for(int i=0,j=ring.Length-1;i<ring.Length;j=i++){var a=ring[i];var b=ring[j];if((a.z>p.z)!=(b.z>p.z)&&p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z)+a.x)inside=!inside;}return inside;}
    public bool BayClear(Bay bay){
        if(!Reservations.Available(bay.id,GetInstanceID()))return false;
        // Paid reservations survive temporary displacement; other drivers cannot
        // claim that empty-looking bay. Shared map identity also scopes LAN hosts.
        foreach(var other in FindObjectsOfType<VehicleDamageController>())if(other!=this&&other.session&&other.session.world&&ReferenceEquals(other.session.world.map,session.world.map)&&other.session.business&&other.session.business.ledger!=null&&other.Damage.RepairBayId==bay.id)return false;
        foreach(var collider in Physics.OverlapBox(bay.position+Vector3.up*.65f,new Vector3(1.3f,.5f,2.5f),Quaternion.LookRotation(bay.direction),~0,QueryTriggerInteraction.Ignore)){
            if(collider.attachedRigidbody==session.taxi.body||collider.bounds.max.y<=bay.position.y+.16f)continue;
            return false;
        }
        return true;
    }
    Bay CurrentBay {get{return Bays.Find(b=>b.id==Damage.RepairBayId);}}
    // Mandatory accident transfer is included in its selected fee and duration.
    // It never starts the separately priced breakdown tow service.
    public bool TryAccidentTransfer(){
        if(!Campaign||PvpNativeLan.IsReplica(session)||!session.accident||!session.accident.Locked)return false;
        BuildBays();Bay free=null;float distance=float.MaxValue;
        foreach(var bay in Bays)if(!bay.legacy&&BayClear(bay)){float d=(bay.position-transform.position).sqrMagnitude;if(d<distance){free=bay;distance=d;}}
        if(free==null){Notice="공업사 빈 공간 대기";return false;}
        if(!Reservations.TryReserve(free.id,GetInstanceID(),BayClear(free)))return false;
        session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;
        if(Damage.Disabled)Damage.EnterRepairBay(free.id);
        PlaceTow(free);Physics.SyncTransforms();TechnicalRecovery();session.accident.Suppress(1);
        if(session.drivingCamera)session.drivingCamera.SetFirstPerson(session.drivingCamera.FirstPerson);
        if(!Damage.Disabled)Reservations.Release(GetInstanceID());
        Notice="공업사 이동 완료 · 수리는 별도";noticeUntil=Time.unscaledTime+6;return true;
    }
    public void ResumeAfterAccident(){
        if(!session.simulationMatch||!session.IsSimulationBot||PvpNativeLan.IsRemoteHuman(session)||Disabled||!session.accident.State.workshopTransferred)return;
        foreach(var site in session.services.Workshops)foreach(var point in site.tow)if(Vector3.Distance(transform.position,point)<3){
            var bot=session.GetComponent<PvpSimulationBotServices>()??session.gameObject.AddComponent<PvpSimulationBotServices>();bot.LeaveWorkshop(session,site);return;
        }
    }
    public bool AtRepairBay {get{var b=CurrentBay;return b!=null&&Vector3.Distance(transform.position,b.position)<3&&Vector3.Dot(transform.forward,b.direction)>.85f;}}
    public bool RestoreReservedBay(bool checkpoint=true){
        BuildBays();if(!Damage.Disabled||string.IsNullOrEmpty(Damage.RepairBayId)){RestoringReservedBay=false;return false;}
        var bay=CurrentBay;
        if(bay==null){RestoringReservedBay=true;Notice="저장된 정비 공간이 현재 지도에 없습니다 · 견인 지원을 확인해 주세요";return false;}
        if(AtRepairBay){RestoringReservedBay=false;return true;}
        RestoringReservedBay=true;
        if(!BayClear(bay)){Notice="이미 지불한 정비 공간 복원 대기 · 공간이 비면 복귀 · 추가 요금 없음";return false;}
        if(session.taxi.body.velocity.magnitude>=.56f){if(disabledSince<0)disabledSince=Time.time;if(Time.time-disabledSince<5){Notice="정차 후 지불한 정비 공간으로 복귀합니다 · 추가 요금 없음";return false;}session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;fadeUntil=Time.unscaledTime+.4f;}
        session.taxi.SetSpawn(bay.position+Vector3.up*.15f,Quaternion.LookRotation(bay.direction));session.taxi.ResetTaxi();RestoringReservedBay=false;
        Notice="이미 지불한 정비 공간 복원 완료 · 추가 견인비/시간 없음";noticeUntil=Time.unscaledTime+6;if(checkpoint)session.business.SaveCheckpoint();return true;
    }
    void Update(){if(PvpNativeLan.IsReplica(session))return;if(session&&session.simulationMatch&&session.simulationMatch.Finished)CancelPendingService();if(session)ClearRepairedBay();if(!session||!session.business||session.business.ledger==null)return;if(!Damage.Disabled||string.IsNullOrEmpty(Damage.RepairBayId)){if(PendingServiceRemaining<=0)Reservations.Release(GetInstanceID());RestoringReservedBay=false;return;}if(Campaign&&!AtRepairBay&&Time.unscaledTime>=nextRestoreAttempt){nextRestoreAttempt=Time.unscaledTime+.5f;RestoreReservedBay();}}
    public bool CanRepair {get{return !(session.accident&&session.accident.Locked)&&PendingServiceRemaining<=0&&!(session.simulationMatch&&session.simulationMatch.ServiceLocked(session))&&Campaign&&session.business.ledger.Operating&&Damage.Durability<100&&session.jobs.trip.State!=TripState.OnTrip&&(PvpNativeLan.IsReplica(session)?Mathf.Abs(session.taxi.Speed):session.taxi.body.velocity.magnitude)<.56f&&(AtRepairBay||session.services.AtRepairPump());}}
    public string RepairQuote {get{return "내구도 "+Damage.Durability+" → 100\n수리 "+Damage.RepairCost.ToString("N0")+"원 · "+Damage.RepairSeconds.ToString("0.#")+"초 (부족분 미납)\n청결·연료 유지 · 엔진 정지";}}
    public bool Repair(){
        if(PvpNativeLan.Route(session,"repair"))return true;
        if(!CanRepair||!AtRepairBay&&!session.services.AdmitRepair())return false;double seconds=Damage.RepairSeconds;int cost=Damage.RepairCost;
        if(session.simulationMatch)return BeginWorkshopService(1,(float)seconds);
        if(!session.business.ledger.UseRepair(session.law.ids.Next("repair")))return false;
        NotifyWorkshopRepairCompleted();
        session.jobs.AdvancePickupWait((float)seconds);TechnicalRecovery();Notice="수리 완료 · 내구도 100 · "+cost.ToString("N0")+"원 / "+seconds.ToString("0.#")+"초";noticeUntil=Time.unscaledTime+6;session.business.SaveCheckpoint();return true;
    }
    public bool Tow(){if(PvpNativeLan.Route(session,"tow"))return true;if(session.accident&&session.accident.Locked||PendingServiceRemaining>0)return false;if(session.simulationMatch&&session.simulationMatch.ServiceLocked(session))return false;
        if(!Campaign||!Disabled||!session.business.ledger.Operating||session.business.ShowingSummary||AtRepairBay||RestoringReservedBay||session.FullMapOpen)return false;
        if(session.taxi.body.velocity.magnitude>=.56f&&Time.time-disabledSince<5){Notice="제동 후 정차하면 견인할 수 있습니다";return false;}
        Bay free=null;float distance=float.MaxValue;foreach(var bay in Bays)if(!bay.legacy&&BayClear(bay)){float d=(bay.position-transform.position).sqrMagnitude;if(d<distance){free=bay;distance=d;}}
        if(free==null){Notice="견인 전용 공간이 사용 중입니다 · 비면 다시 신청해 주세요";return false;}
        // No yields between reservation, accounting, placement and durable checkpoint.
        session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;
        bool fixedArrival=session.jobs.trip.State==TripState.OnTrip&&session.jobs.trip.ArrivalLocked;
        if(!fixedArrival)session.jobs.ResolveVehicleFailure();
        if(session.jobs.Active&&!fixedArrival){Notice="승객의 안전 하차를 확인하고 다시 신청해 주세요";return false;}
        if(!Reservations.TryReserve(free.id,GetInstanceID(),BayClear(free)))return false;
        if(session.simulationMatch)return BeginWorkshopService(2,30,free);
        if(!session.business.ledger.UseTow(session.law.ids.Next("tow"),free.id)){Reservations.Release(GetInstanceID());return false;}
        session.taxi.SetSpawn(free.position+Vector3.up*.15f,Quaternion.LookRotation(free.direction));session.taxi.ResetTaxi();session.jobs.nav.Clear();fadeUntil=Time.unscaledTime+.4f;
        if(fixedArrival)session.jobs.ResolveVehicleFailure();
        Notice="견인 완료 · 3,000원 / 30초 · "+(session.jobs.Active?"확정 운임 유지 · 안전 하역 후 수리":"수리는 별도입니다");noticeUntil=Time.unscaledTime+6;session.business.SaveCheckpoint();return true;
    }
    public bool EndDisabledDay(){if(PvpSimulationMode.Active)return false;
        if(!Campaign||!Disabled||!AtRepairBay||session.jobs.Active)return false;
        var ledger=session.business.ledger;
        if(ledger.State==BusinessState.Open&&ledger.OneDay){
            if(ledger.Driver.drowsiness.Blackout)return session.business.RequestEmergencyHome();
            if(!ledger.UseSleep(session.law.ids.Next("disabled-day-rest")))return false;
        }
        ledger.RequestClose("차량 고장 · 정비 보류");session.business.SaveCheckpoint();return true;
    }
    void OnGUI(){if(session.IsSimulationBot||PvpNativeLan.IsRemoteHuman(session))return;
        if(!Campaign||session.business.preparing||session.business.ShowingSummary||session.RideResultOpen||session.FullMapOpen||session.PracticeMenuOpen)return;
        var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=16,wordWrap=true};style.normal.textColor=CasualUI.Ink;GUI.depth=-140;
        if(Time.unscaledTime<(PvpNativeLan.IsReplica(session)?networkWorkshopNoticeUntil:noticeUntil)&&!Disabled&&DisplayedServiceRemaining<=0)GUI.Label(new Rect(Screen.width/2-230,165,460,65),DisplayedServiceNotice,style);
        if(DisplayedServiceRemaining>0){
            if(session.accident&&session.accident.Locked)return;
            var matrix=GUI.matrix;float scale=WorkshopPresentation.Scale(Screen.width,Screen.height);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));GUI.depth=-650;
            try {bool repair=(PvpNativeLan.IsReplica(session)?networkWorkshopKind:pendingWorkshopKind)==1;float x=(Screen.width/scale-WorkshopPresentation.Width)/2,y=(Screen.height/scale-238)/2;
                WorkshopPresentation.Card(new Rect(x,y,WorkshopPresentation.Width,238),repair?"공업사 수리":"공업사 견인","작업 진행 중 · 경기는 계속됩니다",new[]{"예상 비용","남은 시간","완료 후"},new[]{(repair?Damage.RepairCost:3000).ToString("N0")+"원",DisplayedServiceRemaining.ToString("0.0")+"초",repair?"내구도 100":"공업사 · 수리 별도"},DisplayedServiceNotice);
            } finally {GUI.matrix=matrix;}return;
        }
        if(Disabled&&!(session.accident&&session.accident.Locked)&&!session.services.open&&!session.paused){
            var matrix=GUI.matrix;bool enabled=GUI.enabled;float scale=WorkshopPresentation.Scale(Screen.width,Screen.height);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));GUI.depth=-650;
            try {bool atBay=AtRepairBay,restoring=!AtRepairBay&&RestoringReservedBay;float height=atBay?328:294,x=(Screen.width/scale-WorkshopPresentation.Width)/2,y=(Screen.height/scale-height)/2;
                string notice=DisplayedServiceNotice??(atBay?"정비 후 운행을 다시 시작할 수 있습니다":"정차 후 견인을 신청하세요 · 수리는 별도입니다");
                WorkshopPresentation.Card(new Rect(x,y,WorkshopPresentation.Width,height),"차량 고장","내구도 0 · 운행 불가",new[]{atBay?"수리비":"견인비","소요 시간",atBay?"수리 후":"도착 후"},new[]{restoring?"추가 비용 없음":(atBay?Damage.RepairCost:3000).ToString("N0")+"원",restoring?"공간 확인 후":(atBay?Damage.RepairSeconds:30).ToString("0.#")+"초",atBay?"내구도 100":restoring?"예약한 정비 공간":"공업사 · 수리 별도"},notice);
                if(atBay){GUI.enabled=CanRepair;if(GUI.Button(new Rect(x+20,y+230,480,40),new GUIContent(session.jobs.Active?"안전 하역 중":"수리 확정",RepairQuote),CasualUI.Action(18)))Repair();GUI.enabled=!session.jobs.Active;if(GUI.Button(new Rect(x+20,y+278,480,30),new GUIContent("정비 보류 · 휴식 100초 후 종료","정비를 미루고 휴식 후 오늘 영업 종료 · 100초"),WorkshopPresentation.Secondary))EndDisabledDay();}
                else {GUI.enabled=!RestoringReservedBay;if(GUI.Button(new Rect(x+20,y+234,480,40),new GUIContent(RestoringReservedBay?"정비 공간 복원 대기":"견인 신청",restoring?"이미 지불한 정비 공간 복원 · 추가 요금 없음":"3,000원 / 30초 · 부족분 미납 · 수리비/시간 별도"),CasualUI.Action(18)))Tow();}
            } finally {GUI.matrix=matrix;GUI.enabled=enabled;}
        }
        if(Time.unscaledTime<fadeUntil){GUI.depth=-2000;GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;}
    }
}
