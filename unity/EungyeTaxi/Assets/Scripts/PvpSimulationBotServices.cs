using System;
using System.Collections.Generic;
using UnityEngine;

// A physical facility trip: native navigation to the driveway, pedal/steering
// inputs on its authored paving, native purchase, then the separate exit.
// No vehicle placement, condition injection or elapsed-time shortcuts.
public sealed class PvpSimulationBotServices : MonoBehaviour {
    public string Status {get;private set;}
    public bool Active {get{return stage!=0;}}
    int stage,kind;
    float progress,retryAt,routeWait,offPathSeconds;
    MapStop approach;
    Vector3 bay,direction;
    Vector3[] entry,departure;
    AssistedRoadNetwork.Path manoeuvre;
    bool fuelSite;

    public static int NeededService(float fuel,double fatigue,int cleanliness,bool needsWash,int durability) {
        if(needsWash||cleanliness<45)return 4;
        if(fatigue>=55)return 3;
        if(fuel<25)return 0;
        if(durability<45)return 5;
        return -1;
    }
    public static bool AtApproach(MapStop stop,Vector3 position,Vector3 heading){return stop!=null&&stop.CanStop(position,heading,0);}
    void Retry(DriveSession s,string reason){
        routeWait=offPathSeconds=0;retryAt=Time.time+15;
        // An unusable pump must not leave a taxi parked outside the road
        // graph. Physically follow its authored exit before choosing again.
        if(stage==2&&departure!=null&&departure.Length>1){
            BeginManoeuvre(s,departure);stage=4;Stop(s,reason+" · 출구 복귀");return;
        }
        stage=0;manoeuvre=null;s.assisted.SetActive(false);s.jobs.nav.Clear();s.taxi.autoSteer=0;Stop(s,reason);
    }
    public bool Tick(DriveSession s) {
        if(!s||!s.simulationMatch||s.simulationMatch.Finished||s.jobs.Active)return false;
        if(s.simulationMatch.ServiceLocked(s)){Stop(s,"서비스 진행 중");return Active;}
        if(stage==0) {
            if(Time.time<retryAt||s.jobs.trip.State!=TripState.Idle)return false;
            var e=s.business.ledger;
            kind=NeededService(e.Account.Fuel,e.Driver.fatigue,e.Care.Cleanliness,e.Care.NeedsWash,e.Care.Damage.Durability);
            if(kind<0)return false;
            if(!Select(s)){retryAt=Time.time+15;return false;}
            s.jobs.nav.SetDestination(approach);routeWait=offPathSeconds=0;stage=1;
        }
        if(stage==1) {
            Status="시설 접근 · "+approach.name;
            var r=s.jobs.nav.hdRoute;
            bool here=AtApproach(approach,s.taxi.transform.position,s.taxi.transform.forward);
            if(r==null&&!here){
                s.jobs.nav.Refresh();Stop(s,"시설 연결 경로 재탐색");routeWait+=Time.fixedDeltaTime;
                if(routeWait>20)Retry(s,"시설 연결 불가 · 다음 경로 재시도");return true;
            }
            routeWait=0;float at=0,error;float remaining=0;
            if(r!=null){at=s.jobs.nav.ProjectVehicle(out error);remaining=r.path.length-at;}
            if((here||remaining<4)&&s.taxi.body.velocity.magnitude<.6f) {
                BeginManoeuvre(s,entry);stage=2;
            } else {
                if(here||r==null){Stop(s,"시설 진입 전 감속");return true;}
                if(!s.assisted.Active&&!s.assisted.SetActive(true)){Stop(s,"시설 접근 차로 정렬 대기");return true;}
                float bend=Vector3.Angle(r.path.Tangent(at),r.path.Tangent(Mathf.Min(r.path.length,at+12)));
                float target=bend>12?3:6;
                target=Mathf.Min(target,Mathf.Sqrt(Mathf.Max(0,remaining-1.5f)*2.5f));
                float limit=s.traffic.EditorSpeedLimit(s.taxi.transform.position,s.taxi.transform.forward);
                if(limit>0)target=Mathf.Min(target,limit/3.6f);
                DrivePedals(s,Mathf.Min(target,SafeSpeed(s,true)));
                return true;
            }
        }
        if(stage==2) {
            Status="시설 진입 · "+approach.name;
            if(s.services.CanUse(kind)){Stop(s,"시설 정차");stage=3;}
            else {if(Follow(s))Retry(s,"시설 정차점 이용 불가 · 접근 재시도");return true;}
        }
        if(stage==3) {
            Stop(s,"시설 이용 · "+approach.name);
            // Each purchase retains its native cash/condition/time validation.
            int action=kind;
            var e=s.business.ledger;
            if(fuelSite)action=(e.Care.NeedsWash||e.Care.Cleanliness<45)&&s.services.CanUse(4)?4:e.Care.Damage.Durability<70&&s.services.CanUse(5)?5:e.Account.Fuel<60&&s.services.CanUse(0)?0:-1;
            else if(e.Driver.fatigue<35)action=-1;
            if(action>=0&&s.services.CanUse(action)) {
                float scale=Time.timeScale;bool paused=s.paused;
                // The bot has no modal. Buy's native Close is not allowed to
                // change the human's pause or the shared simulation speed.
                s.services.open=true;
                bool bought=s.services.Buy(action);
                s.services.open=false;s.paused=paused;Time.timeScale=scale;
                if(bought)return true;
            }
            BeginManoeuvre(s,departure);stage=4;
        }
        if(stage==4) {
            Status="시설 출구 합류";
            if(Follow(s)) {
                stage=0;manoeuvre=null;s.jobs.nav.Clear();s.jobs.ResetOffers();
                retryAt=Time.time+10;s.taxi.autoSteer=0;s.taxi.autoServiceBrake=0;
                return false;
            }
        }
        return true;
    }
    bool Select(DriveSession s) {
        float best=float.PositiveInfinity;bool found=false;
        if(kind==3) {
            var rest=s.services.RestArea;
            if(rest!=null)found=Consider(s,rest.approach,rest.pump,rest.direction,rest.drivePath,false,ref best);
            if(ProjectMapScene.Active(s.world.map))foreach(var o in ProjectMapScene.Services(s.world.map,"parking")) {
                var access=Array.Find(s.world.map.roads,r=>r.id==o.id+":access");
                if(access==null||access.points==null||access.points.Length<4)continue;
                var points=new List<Vector3>();foreach(var p in access.points)points.Add(p.V());
                // Authored start parks provide a through driveway and a middle bay.
                points.Insert(points.Count/2,o.Position);
                float at;int edge=s.jobs.nav.hd.Nearest(points[0],o.Direction,out at);
                if(edge<0)continue;
                var path=s.jobs.nav.hd.paths[edge];at=Mathf.Max(0,at-4);var p0=path.Position(at);var d=path.Tangent(at);
                var stop=new MapStop{name=o.name,x=p0.x,z=p0.z,elevation=p0.y,dx=d.x,dz=d.z,width=3,graphStop=true,laneEdge=edge,laneAt=at};
                found=Consider(s,stop,o.Position,o.Direction,points.ToArray(),false,ref best)||found;
            }
            // Older maps use a legal roadside parking stop without a driveway.
            if(!found&&!ProjectMapScene.Active(s.world.map)) {
                var stop=s.services.ParkingStop;
                found=Consider(s,stop,stop.Kerb,stop.Direction,new[]{stop.Kerb,stop.Kerb+stop.Direction*12},false,ref best);
            }
        } else foreach(var station in s.services.Stations) {
            if(kind==4&&!station.SupportsWash||kind==5&&!station.SupportsRepair)continue;
            found=Consider(s,station.approach,station.pump,station.direction,station.drivePath,true,ref best)||found;
        }
        return found;
    }
    bool Consider(DriveSession s,MapStop stop,Vector3 parking,Vector3 heading,Vector3[] drive,bool isFuel,ref float best) {
        if(drive==null||drive.Length<2)return false;
        float length=PassengerJobs.DirectedDistance(s.jobs.nav.hd,s.taxi.transform.position,s.taxi.transform.forward,stop);
        if(float.IsNaN(length)||float.IsInfinity(length)||length>=best)return false;
        int split=0;float near=float.PositiveInfinity;
        for(int i=0;i<drive.Length;i++){float delta=(drive[i]-parking).sqrMagnitude;if(delta<near){near=delta;split=i;}}
        var enter=new List<Vector3>();for(int i=0;i<split;i++)enter.Add(drive[i]);enter.Add(parking);
        var leave=new List<Vector3>{parking};for(int i=split+1;i<drive.Length;i++)leave.Add(drive[i]);
        var last=leave[leave.Count-1];float laneAt;int edge=s.jobs.nav.hd.Nearest(last,heading,out laneAt);
        if(edge>=0){var lane=s.jobs.nav.hd.paths[edge];for(int step=1;step<=12;step++)leave.Add(lane.Position(Mathf.Min(lane.length,laneAt+step)));}
        best=length;approach=stop;bay=parking;direction=heading;entry=enter.ToArray();departure=leave.ToArray();fuelSite=isFuel;
        return true;
    }
    void BeginManoeuvre(DriveSession s,Vector3[] points) {
        s.assisted.SetActive(false);s.taxi.autoSteer=0;s.taxi.autoServiceBrake=0;
        var all=new List<Vector3>();var current=s.taxi.transform.position;current.y=points[0].y;all.Add(current);
        foreach(var point in points)if((all[all.Count-1]-point).sqrMagnitude>.0025f)all.Add(point);
        if(all.Count<2)all.Add(current+direction*.1f);
        manoeuvre=new AssistedRoadNetwork.Path(all.ToArray());progress=0;
    }
    bool Follow(DriveSession s) {
        float error;progress=manoeuvre.ProjectRange(s.taxi.transform.position,Mathf.Max(0,progress-2),Mathf.Min(manoeuvre.length,progress+6),out error);
        if(error>4){
            Stop(s,"시설 진입 경로 이탈 · 안전 정차");offPathSeconds+=Time.fixedDeltaTime;
            if(offPathSeconds>20)Retry(s,"시설 진입 경로 재선택");return false;
        }
        offPathSeconds=0;
        float remaining=manoeuvre.length-progress;
        var target=manoeuvre.Position(Mathf.Min(manoeuvre.length,progress+2.8f));
        var local=s.taxi.transform.InverseTransformPoint(target+Vector3.up*(s.taxi.transform.position.y-target.y));
        float angle=Mathf.Atan2(2*2.6f*local.x,Mathf.Max(1,local.x*local.x+local.z*local.z))*Mathf.Rad2Deg;
        s.taxi.autoSteer=Mathf.Clamp(angle/DriveMath.ManualSteerAngle(s.taxi.Speed*3.6f),-1,1);
        float desired=Mathf.Min(2.2f,Mathf.Sqrt(Mathf.Max(0,remaining-.3f)*1.5f));
        DrivePedals(s,Mathf.Min(desired,SafeSpeed(s,false)));
        if(remaining<.65f){Stop(s,Status);return s.taxi.body.velocity.magnitude<.12f;}
        return false;
    }
    void Stop(DriveSession s,string reason) {Status=reason;s.taxi.autoThrottle=0;s.taxi.autoBrake=true;s.taxi.autoServiceBrake=0;}
    static void DrivePedals(DriveSession s,float target) {
        float speed=s.taxi.body.velocity.magnitude;
        s.taxi.autoBrake=target<.05f;
        s.taxi.autoThrottle=Mathf.Clamp((target-speed)*.5f,0,.5f);
        s.taxi.autoServiceBrake=speed>target+.15f?Mathf.Clamp((speed-target)*.7f,.08f,.6f):0;
    }
    static float SafeSpeed(DriveSession s,bool signals) {
        var pilot=s.GetComponent<PvpSimulationBot>();
        // Street approaches share the same native movement signal / yield /
        // pedestrian policy as passenger routes. Driveways query colliders only.
        return pilot?Mathf.Sqrt(Mathf.Max(0,pilot.ObstacleDistance(signals)-3)*2.5f):0;
    }
}

