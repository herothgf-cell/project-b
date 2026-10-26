using UnityEngine;

public class AssistedDriveController : MonoBehaviour {
    public readonly AssistedRoadNetwork network=new AssistedRoadNetwork();
    public HdSteering hd;
    public bool Active {get;private set;}
    public bool Changing {get{return hd!=null?Active&&hd.changing:manoeuvre!=null&&turnJoin==null;}}
    public bool Turning {get{return hd!=null?Active&&hd.turning:turnJoin!=null;}}
    public int PendingTurn {get;private set;}
    public AssistedRoadNetwork.Lane CurrentLane {get;private set;}
    public string Status {get;private set;}
    public string AvailableTurns {get{if(hd!=null)return "내비 경로";if(!Active||CurrentLane==null)return "";return (network.Connection(CurrentLane,-1)!=null?"← ":"")+(network.Connection(CurrentLane,0)!=null?"↑ ":"")+(network.Connection(CurrentLane,1)!=null?"→":"");}}
    public bool allowAutomated;
    TaxiController taxi;TaxiSignals signals;
    readonly AssistedDrivingPolicy input=new AssistedDrivingPolicy();
    AssistedRoadNetwork.Path manoeuvre;
    AssistedRoadNetwork.Lane changeTarget;
    AssistedRoadNetwork.Join turnJoin;
    float progress,noticeUntil;string notice;
    public void Initialize(TaxiController vehicle,MapData map){taxi=vehicle;signals=GetComponent<TaxiSignals>();if(map.HasHd)hd=new HdSteering();else network.Build(map);SetActive(false);}
    void Notify(string message){notice=message;noticeUntil=Time.time+3;Status=message;}
    public bool SetActive(bool active){
        manoeuvre=null;turnJoin=null;changeTarget=null;PendingTurn=0;input.Reset();progress=0;
        if(signals)signals.Clear();Active=false;
        if(hd!=null){Active=active&&hd.Arm(taxi);Status=active?hd.status:"수동 운전 · H로 실제 차로 보조";return Active==active;}
        if(!active){Status="수동 운전 · H로 보조 운전";return true;}
        if(Time.timeScale==0||taxi.Speed<-.1f){Notify("정방향으로 정차하거나 주행 중 H를 누르세요");return false;}
        CurrentLane=network.FindLane(taxi.transform.position,taxi.transform.forward,out progress);
        if(CurrentLane==null){Notify("보조 구간의 차로에 맞춰 진입한 뒤 H를 누르세요");return false;}
        Active=true;noticeUntil=0;Status="보조 운전 · 가감속과 신호는 직접 판단";return true;
    }
    public void ReadInput(float lateral){
        if(Time.timeScale==0)return;
        if(Input.GetKeyDown(KeyCode.H))SetActive(!Active);
        if(!Active)return;
        int request=input.Pulse(lateral);if(request!=0)RequestLaneChange(request);
    }
    public bool RequestLaneChange(int side){
        if(!Active||Time.timeScale==0)return false;
        if(hd!=null){if(taxi.Speed<=.5f||Changing||Turning)return false;bool accepted=hd.Request(side,true);Status=hd.status;return accepted;}
        if(!AssistedDrivingPolicy.CanChange(CurrentLane.index,CurrentLane.count,side,manoeuvre!=null,taxi.Speed)){
            Notify(manoeuvre!=null?"이동 완료 후 다시 입력하세요":taxi.Speed<=.5f?"출발 후 차선을 변경하세요":"같은 방향의 옆 차로가 없습니다");return false;
        }
        float remaining=CurrentLane.path.length-progress;
        float travel=Mathf.Max(22,taxi.Speed*3);
        if(remaining<travel+5){Notify("교차로 전 차선 변경 구간을 지났습니다");return false;}
        var target=network.Adjacent(CurrentLane,side);if(target==null)return false;
        float error;float targetAt=target.path.Project(taxi.transform.position,out error);
        var forward=CurrentLane.path.Tangent(progress);var right=TrafficSystem.Right(forward);
        var destination=target.path.Position(targetAt);
        // Check the immediate swept corridor, ignoring the drivable ground below it.
        var origin=taxi.transform.position;origin.y=0;
        var midpoint=(origin+destination)*.5f+Vector3.up*.85f;
        float across=Mathf.Abs(Vector3.Dot(destination-origin,right));
        foreach(var hit in Physics.OverlapBox(midpoint,new Vector3(across*.5f+1,.45f,2.6f),Quaternion.LookRotation(forward),(1<<9)|1,QueryTriggerInteraction.Ignore)){
            Notify("옆 공간이 막혀 있습니다 · 확인 후 다시 입력하세요");return false;
        }
        bool warning=false;
        foreach(var hit in Physics.OverlapBox(destination-forward*15+Vector3.up*.85f,new Vector3(1.4f,.5f,15),Quaternion.LookRotation(forward),1<<9,QueryTriggerInteraction.Ignore)){
            var car=hit.GetComponentInParent<TrafficCar>();if(car&&AssistedDrivingPolicy.RearRisk(Vector3.Dot(destination-car.transform.position,forward),car.speed,taxi.Speed))warning=true;
        }
        var points=new Vector3[33];
        for(int i=0;i<points.Length;i++){float t=(float)i/(points.Length-1),blend=t*t*t*(t*(t*6-15)+10);points[i]=Vector3.Lerp(CurrentLane.path.Position(progress+travel*t),target.path.Position(targetAt+travel*t),blend);}
        points[0]=origin;
        manoeuvre=new AssistedRoadNetwork.Path(points);changeTarget=target;progress=0;PendingTurn=0;
        signals.Clear();signals.Toggle(side);
        Notify(warning?"뒤차가 빠르게 접근합니다 · 속도와 간격을 확인하세요":"차선 변경 중 · 주변 차량을 확인하세요");return true;
    }
    public bool RequestTurn(int side){
        if(!Active||Time.timeScale==0||side!= -2&&side!= -1&&side!=1)return false;
        if(hd!=null){if(Changing||Turning)return false;bool accepted=hd.Request(side,false);Status=hd.status;return accepted;}
        if(side==UturnMovement.Turn)return false;if(manoeuvre!=null){Notify("이동 중에는 회전 선택을 바꿀 수 없습니다");return false;}
        int wanted=PendingTurn==side?0:side;
        if(wanted!=0&&network.Connection(CurrentLane,wanted)==null){Notify("현재 차로에서 그 방향으로 회전할 수 없습니다");return false;}
        PendingTurn=wanted;signals.Clear();if(wanted!=0)signals.Toggle(wanted);Notify(wanted<0?"다음 교차로 좌회전 예약":wanted>0?"다음 교차로 우회전 예약":"다음 교차로 직진 선택");return true;
    }
    public void Apply(ref float steering,ref DriveMath.Demand demand){
        if(!Active||taxi.automated&&!allowAutomated)return;
        if(hd!=null){bool keep=hd.Apply(taxi,ref steering);if(!keep){Active=false;steering=0;if(signals)signals.Clear();}else if(signals&&signals.direction!=hd.indicator){signals.Clear();if(hd.indicator!=0)signals.Toggle(hd.indicator);}Status=hd.status;PendingTurn=hd.turn;return;}
        if(taxi.Speed<-.15f||taxi.ReverseRequested&&taxi.Speed<.2f){SetActive(false);Notify("후진 · 수동 운전으로 전환");return;}
        var path=manoeuvre??CurrentLane.path;float error;
        progress=path.Project(taxi.transform.position,out error);
        if(error>5||Vector3.Dot(taxi.transform.forward,path.Tangent(progress))<.25f){SetActive(false);steering=0;Notify("차로 이탈 · 수동으로 복귀한 뒤 H");return;}
        float remaining=path.length-progress;
        if(manoeuvre==null&&remaining<.65f&&network.Connection(CurrentLane,PendingTurn)==null){
            SetActive(false);steering=0;Notify("연결 구간 끝 · 수동 전환 · 직접 감속하세요");return;
        }
        if(remaining<.65f){
            if(manoeuvre!=null){
                CurrentLane=turnJoin!=null?turnJoin.target:changeTarget;manoeuvre=null;turnJoin=null;changeTarget=null;PendingTurn=0;signals.Clear();path=CurrentLane.path;progress=path.Project(taxi.transform.position,out error);remaining=path.length-progress;
            }else{
                var join=network.Connection(CurrentLane,PendingTurn);
                if(join!=null){turnJoin=join;manoeuvre=join.path;path=manoeuvre;progress=path.Project(taxi.transform.position,out error);remaining=path.length-progress;}
            }
        }
        float look=Mathf.Clamp(3+Mathf.Max(0,taxi.Speed)*.55f,4,10);
        var target=path.Position(progress+look);
        var upcoming=manoeuvre==null?network.Connection(CurrentLane,PendingTurn):null;
        if(progress+look>path.length){
            float over=progress+look-path.length;
            if(upcoming!=null)target=upcoming.path.Position(over);
            else if(manoeuvre!=null)target=(turnJoin!=null?turnJoin.target:changeTarget).path.Position((turnJoin!=null?0:ProjectedChangeEnd())+over);
        }
        var local=taxi.transform.InverseTransformPoint(target+Vector3.up*taxi.transform.position.y);
        float wheelAngle=Mathf.Atan2(2*2.6f*local.x,Mathf.Max(1,local.x*local.x+local.z*local.z))*Mathf.Rad2Deg;
        steering=Mathf.Clamp(wheelAngle/DriveMath.SteerAngle(taxi.Speed*3.6f),-1,1);
        bool end=manoeuvre==null&&upcoming==null;
        // Longitudinal control belongs to the driver, including turns and dead ends.
        if(Time.time>=noticeUntil)Status=end?"연결 구간 끝 "+remaining.ToString("0")+"m · 직접 감속":Turning?"교차로 회전 중 · 속도 직접 조절":Changing?"차선 변경 중 · 속도 직접 조절":"차로 "+(CurrentLane.index+1)+"/"+CurrentLane.count+" · 다음 "+(PendingTurn<0?"좌회전":PendingTurn>0?"우회전":"직진")+" · "+remaining.ToString("0")+"m";
    }
    float ProjectedChangeEnd(){float error;return changeTarget.path.Project(manoeuvre.Position(manoeuvre.length),out error);}
}
