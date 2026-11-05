using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Four independent native driver sessions share the rendered traffic world.
[DefaultExecutionOrder(10000)]
public sealed class PvpSimulationMatch : MonoBehaviour {
    readonly List<DriveSession> drivers=new List<DriveSession>();
    readonly List<DriveSession> bots=new List<DriveSession>();
    public IReadOnlyList<DriveSession> Drivers {get{return drivers;}}
    public IReadOnlyList<DriveSession> Bots {get{return bots;}}
    public DriveSession Human {get;private set;}
    public DriveSession Bot {get{return bots.Count>0?bots[0]:null;}}
    public PvpSimulationLedger Ledger {get;private set;}
    public PvpSimulationCallBoard SharedCalls {get;private set;}
    public PvpPassengerWorld PassengerWorld {get;private set;}
    public float Elapsed {get;private set;}
    public bool Finished {get{return Ledger!=null&&Ledger.Finished;}}
    public int HumanScore {get{return Score(0);}}
    public int BotScore {get{return Score(1);}}
    public int Score(int driver){return Ledger==null?0:Ledger.Score(driver);}
    public int DriverIndex(DriveSession driver){return drivers.IndexOf(driver);}
    string[] networkStatuses;
    public void ApplyNetworkStatuses(string[] statuses){networkStatuses=statuses;}
    public void ApplyNetworkView(float elapsed,bool finished,int[] scores,int[] claims,int[] kinds,float[] waits){Elapsed=elapsed;Ledger.ApplyNetworkView(scores,claims);for(int i=0;i<4;i++){conditionKinds[i]=kinds[i];conditionRemaining[i]=waits[i];}if(finished&&!Finished)Ledger.Finish();}
    public string DriverStatus(int driver){
        if(networkStatuses!=null&&driver>=0&&driver<networkStatuses.Length)return networkStatuses[driver]??"";
        if(driver<0||driver>=drivers.Count)return "";
        var session=drivers[driver];if(session.accident&&session.accident.Locked)return "사고 처리 · "+System.Math.Ceiling(session.accident.State.remaining)+"초";if(ConditionServiceActive(session))return (ConditionServiceKind(session)==1?"식사":"짧은 휴식")+" · "+ServiceRemaining(session).ToString("0.0")+"초";var pilot=session.GetComponent<PvpSimulationBot>();
        if(pilot&&pilot.enabled)return pilot.Status??"출발 준비";
        return session.jobs.trip.State==TripState.ToPickup?"승객 픽업":session.jobs.trip.State==TripState.OnTrip?"승객 운송":"호출 선택";
    }
    public float Remaining {get{return Mathf.Max(0,PvpSimulationMode.DurationSeconds-Elapsed);}}
    GUIStyle style,button;
    readonly double[] observedServiceSeconds=new double[PvpSimulationLedger.DriverCount];
    readonly int[] conditionKinds={-1,-1,-1,-1};
    readonly float[] conditionRemaining=new float[PvpSimulationLedger.DriverCount];
    readonly string[] conditionIds=new string[PvpSimulationLedger.DriverCount];
    readonly Vector3[] conditionSites=new Vector3[PvpSimulationLedger.DriverCount];
    public bool ConditionServiceActive(DriveSession driver){int i=DriverIndex(driver);return i>=0&&conditionKinds[i]>=0;}
    public int ConditionServiceKind(DriveSession driver){int i=DriverIndex(driver);return i<0?-1:conditionKinds[i];}
    public bool BeginConditionService(DriveSession driver,int kind){
        int i=DriverIndex(driver);
        if(i<0||Finished||ServiceLocked(driver)||(kind!=1&&kind!=3)||!driver.business.ledger.PvpProfile||!driver.services.open||!driver.services.CanContinueConditionService(kind)||(kind==1&&driver.business.ledger.Account.Cash<PvpConditionRules.MealCost))return false;
        conditionKinds[i]=kind;conditionRemaining[i]=(float)(kind==1?PvpConditionRules.MealSeconds:PvpConditionRules.RestSeconds);
        conditionIds[i]=driver.law.ids.Next(kind==1?"pvp-meal":"pvp-rest");conditionSites[i]=driver.taxi.transform.position;
        driver.assisted.SetActive(false);driver.taxi.autoThrottle=0;driver.taxi.autoBrake=true;
        return true;
    }
    public bool CancelConditionService(DriveSession driver){int i=DriverIndex(driver);if(i<0||conditionKinds[i]<0)return false;conditionKinds[i]=-1;conditionRemaining[i]=0;conditionIds[i]=null;return true;}
    // Called instead of operation progression, once per driver's physics tick.
    public bool AdvanceConditionService(DriveSession driver,float seconds){
        int i=DriverIndex(driver);if(i<0||conditionKinds[i]<0)return false;
        int kind=conditionKinds[i];
        if(Finished||!driver.services.open||!driver.services.CanContinueConditionService(kind)||(driver.taxi.transform.position-conditionSites[i]).sqrMagnitude>4){CancelConditionService(driver);driver.services.FinishConditionService(kind,false);return true;}
        float step=Mathf.Min(Mathf.Max(0,seconds),Mathf.Min(conditionRemaining[i],Remaining));
        driver.business.ledger.AdvancePvpConditionService(step,kind==3);
        if(kind==1)driver.business.ledger.Driver.drowsiness.Advance(step,true,driver.business.ledger.Driver.fatigue,false);
        // This elapsed service has already passed on the common clock; do not create a legacy lock.
        observedServiceSeconds[i]=driver.business.ledger.Elapsed-driver.business.SimulationOperationSeconds;
        conditionRemaining[i]=Mathf.Max(0,conditionRemaining[i]-step);
        if(conditionRemaining[i]<=.0001f&&Remaining>step){
            string id=conditionIds[i];CancelConditionService(driver);
            bool completed=kind==1?driver.business.ledger.CompletePvpMeal(id):driver.business.ledger.CompletePvpRest(id);
            Ledger.SyncExpenses(i,driver.business.ledger.Account.Expense(driver.business.ledger.Day,null));
            driver.services.FinishConditionService(kind,completed);
        }else if(Remaining<=step)CancelConditionService(driver);
        return true;
    }
    readonly float[] serviceUntil=new float[PvpSimulationLedger.DriverCount];
    public float ServiceRemaining(DriveSession driver){int index=DriverIndex(driver);return index<0?0:Mathf.Max(driver.damage?driver.damage.PendingServiceRemaining:0,Mathf.Max(conditionRemaining[index],Mathf.Max(0,serviceUntil[index]-Elapsed)));}
    public bool ServiceLocked(DriveSession driver){return Finished||driver.accident&&driver.accident.Locked||ServiceRemaining(driver)>0;}
    public void ObserveAccidentClock(DriveSession driver){int i=DriverIndex(driver);if(i<0)return;observedServiceSeconds[i]=driver.business.ledger.Elapsed-driver.business.SimulationOperationSeconds;Ledger.SyncExpenses(i,driver.business.ledger.Account.Expense(driver.business.ledger.Day,null));}
    public void ObserveServiceTime(DriveSession driver){
        if(Finished||!driver||!driver.business)return;
        int index=DriverIndex(driver);if(index<0)return;
        Ledger.SyncExpenses(index,driver.business.ledger.Account.Expense(driver.business.ledger.Day,null));
        double charged=driver.business.ledger.Elapsed-driver.business.SimulationOperationSeconds;
        double added=charged-observedServiceSeconds[index];
        if(conditionKinds[index]>=0)return;
        if(added>.05){serviceUntil[index]=Mathf.Max(Elapsed,serviceUntil[index])+(float)added;observedServiceSeconds[index]=charged;}
    }
    public void ReleaseCall(int pickup,int driver){if(SharedCalls!=null)SharedCalls.Release(pickup,driver);else Ledger.Release(pickup,driver);}
    public void Initialize(DriveSession human) {
        Human=human;drivers.Add(human);human.simulationMatch=this;Ledger=new PvpSimulationLedger();human.gameObject.AddComponent<PvpConditionHUD>().session=human;
        for(int index=1;index<PvpSimulationLedger.DriverCount;index++)CreateBot(human,index);
        SharedCalls=new PvpSimulationCallBoard(Ledger,human.jobs.CreateSharedCandidate,human.jobs.stops.Length,3);
        PassengerWorld=gameObject.AddComponent<PvpPassengerWorld>();PassengerWorld.Initialize(this);
        foreach(var driver in drivers){driver.jobs.offers=SharedCalls.Offers;driver.jobs.message="M 지도에서 공용 호출 선택·수락 · 정차 후 F 승하차";}
        if(PvpNativeLan.Instance)PvpNativeLan.Instance.Bind(this);
    }
    void CreateBot(DriveSession human,int index){
        var root=new GameObject("경쟁 택시 세션 "+index);root.SetActive(false);
        var bot=root.AddComponent<DriveSession>();bot.IsSimulationBot=true;bot.simulationMatch=this;bot.enabled=false;
        drivers.Add(bot);bots.Add(bot);
        bot.world=human.world;bot.traffic=human.traffic;bot.roadEnvironment=human.roadEnvironment;bot.ambientPedestrians=human.ambientPedestrians;
        bot.taxi=new GameObject("봇 "+index+" 택시").AddComponent<TaxiController>();
        // Separate districts and a clear directed lane keep competitors from
        // spawning abreast and blocking each other's first pickup manoeuvre.
        Vector3 spawn=Vector3.zero,heading=Vector3.forward;bool found=false;
        int count=human.jobs.stops.Length,first=(index*count/3+count/6)%count;
        for(int candidateIndex=0;candidateIndex<count&&!found;candidateIndex++){
            var stop=human.jobs.stops[(first+candidateIndex)%count];var lane=human.jobs.nav.hd.paths[stop.laneEdge];
            float start=Mathf.Clamp(stop.laneAt-35,8,Mathf.Max(8,lane.length-8));
            for(int offset=0;offset<8;offset++){
                float candidate=Mathf.Clamp(start-offset*8,4,Mathf.Max(4,lane.length-4));
                var point=lane.Position(candidate);var direction=lane.Tangent(candidate);
                bool close=false;foreach(var other in drivers)if(other!=bot&&other.taxi&&(other.taxi.transform.position-point).sqrMagnitude<18*18){close=true;break;}
                if(close||Physics.CheckBox(point+Vector3.up*.85f,new Vector3(1.2f,.55f,3),Quaternion.LookRotation(direction),(1<<8)|(1<<9),QueryTriggerInteraction.Ignore))continue;
                spawn=point;heading=direction;found=true;break;
            }
        }
        if(!found)throw new System.InvalidOperationException("No clear native spawn lane for bot "+index);
        bot.taxi.Create(spawn+Vector3.up*.65f,Quaternion.LookRotation(heading));bot.taxi.automated=true;
        foreach(var part in bot.taxi.GetComponentsInChildren<Transform>())part.gameObject.layer=9;
        bot.signals=bot.taxi.gameObject.AddComponent<TaxiSignals>();
        bot.assisted=bot.taxi.gameObject.AddComponent<AssistedDriveController>();bot.assisted.Initialize(bot.taxi,human.world.map);bot.assisted.allowAutomated=true;bot.taxi.assistance=bot.assisted;
        bot.manners=bot.taxi.gameObject.AddComponent<DrivingManners>();bot.manners.session=bot;
        bot.law=bot.taxi.gameObject.AddComponent<DrivingLaw>();bot.law.session=bot;
        bot.jobs=root.AddComponent<PassengerJobs>();bot.jobs.Initialize(bot);
        bot.assisted.hd.nav=bot.jobs.nav;
        bot.business=root.AddComponent<BusinessProgress>();bot.business.Initialize(bot);
        bot.services=root.AddComponent<ServiceStops>();bot.services.session=bot;bot.services.enabled=false;
        bot.requests=root.AddComponent<PassengerRequestUI>();bot.requests.session=bot;
        root.AddComponent<PassengerBehaviorUI>().session=bot;
        bot.damage=bot.taxi.gameObject.AddComponent<VehicleDamageController>();bot.damage.session=bot;bot.accident=bot.taxi.gameObject.AddComponent<AccidentController>();bot.accident.session=bot;
        root.AddComponent<PvpSimulationBot>().Initialize(bot);
        bot.MarkSharedSimulationReady();root.SetActive(true);
    }
    void Update() {
        if(PvpNativeLan.IsReplica(Human))return;
        if(Finished||!Human)return;
        foreach(var driver in drivers)ObserveServiceTime(driver);
    }
    // Run after BusinessProgress for every physics step, including catch-up steps
    // before a rendered Update. All drivers consume the same start-of-tick budget.
    void FixedUpdate(){
        if(PvpNativeLan.IsReplica(Human)||PvpNativeLan.WaitingForWorld)return;
        if(Finished||!Human||Time.timeScale<=0||Human.paused)return;
        Elapsed=Mathf.Min(PvpSimulationMode.DurationSeconds,Elapsed+Time.fixedDeltaTime);
        if(Remaining<=0)Finish();
    }
    public void Finish() {
        if(Finished)return;
        foreach(var driver in drivers)ObserveServiceTime(driver);
        foreach(var driver in drivers){CancelConditionService(driver);if(driver.damage)driver.damage.CancelPendingService();}
        Ledger.Finish();
        foreach(var driver in drivers){driver.paused=true;driver.taxi.autoThrottle=0;driver.taxi.autoBrake=true;}
        Time.timeScale=0;
    }
    void LateUpdate(){if(Finished){foreach(var driver in drivers)driver.paused=true;Time.timeScale=0;}}
    public void ReturnToMenu() { if(PvpNativeLan.RunningOnline){PvpNativeLan.Instance.LeaveToMenu();return;} var unified=FindObjectOfType<UnifiedModeBootstrap>();if(unified){unified.ReturnToMenu();return;} PvpSimulationMode.Reset();Time.timeScale=1;SceneManager.LoadScene("PvpBootstrap"); }
    public void Restart() { if(PvpNativeLan.RunningOnline){PvpNativeLan.Instance.LeaveToMenu();return;} bool expanded=PvpSimulationMode.Expanded;int seconds=PvpSimulationMode.DurationSeconds;PvpSimulationMode.Configure(expanded,seconds);Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    void OnGUI() {
        if(!Human||Ledger==null||!Finished)return;
        if(style==null){style=new GUIStyle(GUI.skin.box){font=KoreanText.Font,fontSize=16,alignment=TextAnchor.MiddleCenter,wordWrap=true};button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=16};}
        GUI.depth=-250;
        int best=int.MinValue,winners=0;string winner="";
        for(int i=0;i<drivers.Count;i++){int score=Score(i);if(score>best){best=score;winners=1;winner=PvpDriverPresentation.Name(i);}else if(score==best)winners++;}
        string summary="경기 종료 · "+(winners>1?"공동 1위":winner+" 승리");
        for(int i=0;i<drivers.Count;i++)summary+="\n"+(i==0?"나":"봇 "+i)+" · "+Score(i).ToString("N0")+"원 / "+drivers[i].jobs.completed+"회";
        summary+="\n운송 수입 − 시설 지출";
        float x=Screen.width/2-250,y=Screen.height/2-160;
        GUI.Box(new Rect(x,y,500,320),summary,style);
        if(GUI.Button(new Rect(x+30,y+260,210,40),"다시 경기",CasualUI.Action(16)))Restart();
        if(GUI.Button(new Rect(x+260,y+260,210,40),UnifiedModeBootstrap.IsUnified?"모드 선택":"경기 설정",button))ReturnToMenu();
    }
}
