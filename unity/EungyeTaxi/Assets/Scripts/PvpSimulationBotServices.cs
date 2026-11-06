using System;
using System.Collections.Generic;
using UnityEngine;

// A physical facility trip: native navigation to the driveway, pedal/steering
// inputs on its authored paving, native purchase, then the separate exit.
// No vehicle placement, condition injection or elapsed-time shortcuts.
public sealed class PvpSimulationBotServices : MonoBehaviour {
    public string Status {get;private set;}
    public bool Active {get{return stage!=0;}}
    public int DecisionCount {get;private set;}
    int stage,kind;
    int conditionTripCount=-1,conditionLastKind=-1,conditionMealsBefore,conditionRestsBefore;
    float progress,retryAt,routeWait,offPathSeconds,nextDecisionAt;
    MapStop approach;
    Vector3 bay,direction;
    Vector3[] entry,departure;
    AssistedRoadNetwork.Path manoeuvre;
    bool purchaseComplete;FacilityOption selectedSite;float queueWaitStarted;string avoidSite;float avoidUntil;
    bool conditionPurchased;float selectedTravelSeconds;

    // Compatibility for older diagnostic callers; runtime uses the forecast overload below.
    public static int NeededService(float fuel,double fatigue,int cleanliness,bool needsWash,int durability) {
        return BotServiceDecision.Needed(fuel,fatigue,80,cleanliness,needsWash,durability);
    }
    // Production policy uses the same condition forecast as the human HUD.
    // Detours include the legal approach, driveway and return to the road.
    public static int NeededConditionService(DriverCondition driver,double tripSeconds,double remaining,double mealDetour,double restDetour) {
        if(driver==null||remaining<=0)return -1;
        double trip=Math.Max(0,tripSeconds);
        double recoveryAccess=double.IsInfinity(restDetour)?0:Math.Max(0,restDetour);
        double horizon=Math.Min(remaining,trip+recoveryAccess+PvpConditionRules.RestSeconds);
        var continued=PvpConditionRules.Forecast(driver,horizon);
        // No compulsory stop when the remaining race can finish below fatigue100.
        if(PvpConditionRules.Forecast(driver,remaining).fatigue<100&&remaining<=trip+recoveryAccess+PvpConditionRules.RestSeconds)return -1;
        bool risk=continued.fatigue>=100;
        bool mealFits=Finite(mealDetour)&&mealDetour+PvpConditionRules.MealSeconds+trip<remaining;
        bool restFits=Finite(restDetour)&&restDetour+PvpConditionRules.RestSeconds<remaining;
        bool mealUseful=false;
        if(mealFits&&driver.fatigue<100) {
            var arrival=AdvanceCopy(driver,mealDetour);
            var fed=PvpConditionRules.PreviewMeal(arrival);
            var after=PvpConditionRules.Forecast(fed,Math.Max(0,horizon-mealDetour-PvpConditionRules.MealSeconds));
            // A meal can precede complementary rest; fatigue100 clamping hides its benefit.
            bool moreSafeTime=PvpConditionRules.SecondsToFatigue100(fed)>PvpConditionRules.SecondsToFatigue100(arrival)+60;
            mealUseful=continued.satiety<25&&arrival.fatigue<100&&((after.fatigue<100&&after.fatigue+1<continued.fatigue)||arrival.satiety<25&&moreSafeTime);
        }
        if(mealUseful&&(!risk||!restFits||mealDetour+PvpConditionRules.MealSeconds<restDetour+PvpConditionRules.RestSeconds))return 1;
        if(risk&&restFits)return 3;
        return -1;
    }
    public static bool ReconsiderCondition(int lastKind,DriverCondition driver){return driver!=null&&(lastKind==3&&driver.satiety<=100-PvpConditionRules.MealSatiety||lastKind==1&&driver.fatigue>=PvpConditionRules.RestFatigue);}
    // A human can take another native action while already parked. Recover here
    // rather than driving a legal loop whose fatigue cancels the preceding rest.
    public static int RecoveryAtBay(DriverCondition driver,bool meal,bool rest){
        if(rest&&driver.fatigue>=PvpConditionRules.RestFatigue)return 3;
        if(meal&&driver.satiety<80)return 1;
        return -1;
    }
    public static bool AllowConditionDetour(int lastKind,int candidate,bool critical){return critical||lastKind!=candidate;}
    static bool Finite(double value){return !double.IsNaN(value)&&!double.IsInfinity(value)&&value>=0;}
    static DriverCondition AdvanceCopy(DriverCondition driver,double seconds){
        var value=PvpConditionRules.Forecast(driver,seconds);var copy=driver.Copy();
        copy.satiety=value.satiety;copy.fatigue=value.fatigue;copy.drowsiness.visionSeconds=value.drowsySeconds;return copy;
    }
    public static int NeededService(float fuel,DriverCondition driver,int cleanliness,bool needsWash,int durability,double tripSeconds,double remaining,double mealDetour,double restDetour){
        if(durability<=20)return 5;
        if(fuel<8)return 0;
        if(needsWash||cleanliness<45)return 4;
        int condition=NeededConditionService(driver,tripSeconds,remaining,mealDetour,restDetour);
        if(condition>=0)return condition;
        if(fuel<25)return 0;
        if(durability<45)return 5;
        return -1;
    }
    public static bool ConditionServiceCompleted(EconomyLedger ledger,int mealsBefore,int restsBefore){return ledger!=null&&(ledger.PvpMeals>mealsBefore||ledger.PvpRests>restsBefore);}
    public static bool AtApproach(MapStop stop,Vector3 position,Vector3 heading){return stop!=null&&stop.CanStop(position,heading,0);}
    static bool Critical(DriveSession s){var e=s.business.ledger;return e.Account.Fuel<8||e.Care.NeedsWash||e.Care.Cleanliness<30||e.Care.Damage.Durability<=20||(e.PvpProfile?e.Driver.fatigue>=100:e.Driver.fatigue>=90);}
    bool HoldCritical(DriveSession s){if(!Critical(s))return false;Stop(s,"필수 시설 연결/대기 재시도 · 새 호출 보류");return true;}
    void Retry(DriveSession s,string reason){
        s.services.CancelQueue();if(selectedSite!=null){avoidSite=selectedSite.Id;avoidUntil=Time.time+60;}
        routeWait=offPathSeconds=0;retryAt=Time.time+15;
        // An unusable pump must not leave a taxi parked outside the road
        // graph. Physically follow its authored exit before choosing again.
        if(stage==2&&departure!=null&&departure.Length>1){
            BeginManoeuvre(s,departure);stage=4;Stop(s,reason+" · 출구 복귀");return;
        }
        stage=0;manoeuvre=null;s.assisted.SetActive(false);s.jobs.nav.Clear();s.taxi.autoSteer=0;Stop(s,reason);
    }
    public bool Tick(DriveSession s) {
        if(!s||!s.simulationMatch)return false;s.services.TickFacilityQueue();
        if(s.simulationMatch.Finished||s.jobs.Active){s.services.CancelQueue();stage=0;return false;}
        if(s.simulationMatch.ServiceLocked(s)){Stop(s,"서비스 진행 중");return true;}
        if(stage==0) {
            if(s.jobs.trip.State!=TripState.Idle)return false;
            if(Time.time<retryAt||Time.time<nextDecisionAt)return HoldCritical(s);
            nextDecisionAt=Time.time+5;DecisionCount++;
            var e=s.business.ledger;
            if(e.PvpProfile&&!Critical(s)&&!PvpSimulationBot.HasTimeViableOffer(s))return false;
            if(e.PvpProfile){
                double mealDetour=double.PositiveInfinity,restDetour=double.PositiveInfinity;
                bool sameTrip=conditionTripCount==s.jobs.completed+s.jobs.failed;
                if(!sameTrip||Critical(s)||ReconsiderCondition(conditionLastKind,e.Driver)){
                    kind=1;if(e.Account.Cash>=PvpConditionRules.MealCost&&Select(s,true))mealDetour=selectedTravelSeconds;
                    kind=3;if(e.Account.Cash>=PvpConditionRules.RestCost&&Select(s,true))restDetour=selectedTravelSeconds;
                }
                if(sameTrip&&!Critical(s)){
                    if(!AllowConditionDetour(conditionLastKind,3,false))restDetour=double.PositiveInfinity;
                    if(!AllowConditionDetour(conditionLastKind,1,false))mealDetour=double.PositiveInfinity;
                }
                kind=NeededService(e.Account.Fuel,e.Driver,e.Care.Cleanliness,e.Care.NeedsWash,e.Care.Damage.Durability,PvpSimulationBot.UpcomingTripSeconds(s),s.simulationMatch.Remaining,mealDetour,restDetour);
            }else kind=BotServiceDecision.Needed(e.Account.Fuel,e.Driver.fatigue,e.Driver.satiety,e.Care.Cleanliness,e.Care.NeedsWash,e.Care.Damage.Durability);
            if(kind<0)return HoldCritical(s);
            if(!Select(s)){retryAt=Time.time+15;return HoldCritical(s);}
            s.jobs.nav.SetDestination(approach);routeWait=offPathSeconds=0;conditionPurchased=false;stage=1;
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
                Stop(s,"시설 진입 대기");
                if(!s.services.CanEnterFacility(selectedSite)){
                    if(s.services.CanWaitAtWorkshop(selectedSite)){BeginManoeuvre(s,selectedSite.WaitingEntry);stage=5;return true;}
                    if(queueWaitStarted<=0)queueWaitStarted=Time.time;Status=s.services.QueueStatus;
                    if(Time.time-queueWaitStarted>45){Retry(s,"시설 혼잡 · 다른 시설 확인");}return true;
                }
                queueWaitStarted=0;BeginManoeuvre(s,entry);stage=2;
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
        if(stage==5){
            Status="공업사 대기칸 · 결제 없음";if(!Follow(s))return true;
            Stop(s,Status);if(!s.services.CanEnterFacility(selectedSite))return true;
            BeginManoeuvre(s,selectedSite.WaitingToBay);stage=2;
        }
        if(stage==2) {
            Status="시설 진입 · "+approach.name;
            if(s.services.CanUse(kind)){Stop(s,"시설 정차");stage=3;}
            else {if(Follow(s))Retry(s,"시설 정차점 이용 불가 · 접근 재시도");return true;}
        }
        if(stage==3) {
            Stop(s,"시설 이용 · "+approach.name);
            // Each purchase retains its native cash/condition/time validation.
            // Each additional action passes the same native bay/cash/time gates.
            var e=s.business.ledger;
            if(conditionPurchased&&ConditionServiceCompleted(e,conditionMealsBefore,conditionRestsBefore)){
                conditionTripCount=s.jobs.completed+s.jobs.failed;conditionLastKind=kind;conditionPurchased=false;
                int recovery=RecoveryAtBay(e.Driver,s.services.CanUse(1),s.services.CanUse(3));
                if(recovery>=0){kind=recovery;purchaseComplete=false;}
            }
            int action=purchaseComplete?-1:kind;
            if(action>=0&&s.services.CanUse(action)) {
                float scale=Time.timeScale;bool paused=s.paused;
                // The bot has no modal. Buy's native Close is not allowed to
                // change the human's pause or the shared simulation speed.
                s.services.open=true;
                int mealsBefore=e.PvpMeals,restsBefore=e.PvpRests;
                bool bought=s.services.Buy(action);
                bool pending=bought&&e.PvpProfile&&(action==1||action==3);
                s.services.open=pending;s.paused=paused;Time.timeScale=scale;
                if(bought){
                    purchaseComplete=true;
                    if(pending){conditionPurchased=true;conditionMealsBefore=mealsBefore;conditionRestsBefore=restsBefore;}
                    s.simulationMatch.ObserveServiceTime(s);return true;
                }
            }
            BeginManoeuvre(s,departure);stage=4;
        }
        if(stage==4) {
            Status="시설 출구 합류";
            if(Follow(s)) {
                s.services.CancelQueue();stage=0;manoeuvre=null;s.jobs.nav.Clear();s.jobs.ResetOffers();
                bool critical=Critical(s);retryAt=critical?0:Time.time+10;if(critical)nextDecisionAt=0;s.taxi.autoSteer=0;s.taxi.autoServiceBrake=0;
                // Departure closes this paid visit. A different critical need
                // keeps caller control and receives a fresh plan next Tick.
                return critical&&HoldCritical(s);
            }
        }
        return true;
    }
    public void LeaveWorkshop(DriveSession s,WorkshopLayout.Site site){
        s.services.CancelQueue();routeWait=offPathSeconds=0;conditionPurchased=false;queueWaitStarted=0;selectedSite=null;
        // Towed taxis begin in a parallel bay. Clear its nose before merging
        // into the ordinary driveway; never cross another parked tow bay.
        approach=site.approach;direction=site.direction;departure=site.departure;
        var points=new List<Vector3>{s.taxi.transform.position,s.taxi.transform.position+direction*9};
        for(int i=12;i<site.departure.Length;i++)points.Add(site.departure[i]);
        float at;int edge=s.jobs.nav.hd.Nearest(points[points.Count-1],direction,out at);if(edge>=0)points.Add(s.jobs.nav.hd.paths[edge].Position(Mathf.Min(s.jobs.nav.hd.paths[edge].length,at+12)));
        BeginManoeuvre(s,points.ToArray());stage=4;purchaseComplete=true;
    }
    bool Select(DriveSession s,bool measureOnly=false) {
        var e=s.business.ledger;var options=s.services.FacilityOptions(kind);FacilityOption best=null;float bestTime=float.PositiveInfinity;
        int nextFare=0;float nextSeconds=120;
        foreach(var offer in s.jobs.offers)if(offer!=null&&offer.fare>nextFare){nextFare=offer.fare;nextSeconds=Mathf.Max(30,offer.safeRouteSeconds+offer.waitSeconds+30);}
        bool critical=Critical(s);
        foreach(var option in options){
            if(!option.Reachable||option.Quote==null||!option.Quote.Available||Time.time<avoidUntil&&option.Id==avoidSite)continue;
            // An unknown queue is not a zero wait. Optional recovery chooses
            // an available alternative; critical needs may physically queue.
            if(!critical&&(option.Waiting>0||option.PhysicallyOccupied))continue;
            double wait=option.Waiting*option.Quote.UseSeconds;
            if(!measureOnly&&!(e.PvpProfile&&(kind==1||kind==3))&&!BotServiceDecision.Worthwhile(kind,option.TravelSeconds,s.simulationMatch.Remaining,option.Quote.UseSeconds,wait,option.Quote.Price,nextFare,nextSeconds,e.Driver.satiety,e.Driver.fatigue,critical))continue;
            float score=option.TravelSeconds+(float)wait+(option.PhysicallyOccupied?60:0);
            if(score<bestTime){bestTime=score;best=option;}
        }
        if(best==null)return false;selectedSite=best;approach=best.Approach;bay=best.Bay;direction=best.Direction;entry=best.Entry;
        departure=best.Departure;selectedTravelSeconds=best.TravelSeconds;purchaseComplete=false;queueWaitStarted=0;return true;
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

