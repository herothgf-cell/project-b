using UnityEngine;
using UnityEngine.SceneManagement;

// Owns only the competition. The two sessions run the original vehicle, jobs,
// passenger settlement, traffic observations and damage implementations.
public sealed class PvpSimulationMatch : MonoBehaviour {
    public DriveSession Human {get;private set;}
    public DriveSession Bot {get;private set;}
    public PvpSimulationLedger Ledger {get;private set;}
    public float Elapsed {get;private set;}
    public bool Finished {get{return Ledger!=null&&Ledger.Finished;}}
    public int HumanScore {get{return Ledger.Score(0);}}
    public int BotScore {get{return Ledger.Score(1);}}
    public float Remaining {get{return Mathf.Max(0,PvpSimulationMode.DurationSeconds-Elapsed);}}
    GUIStyle style,button;
    readonly double[] observedServiceSeconds=new double[2];
    readonly float[] serviceUntil=new float[2];
    public float ServiceRemaining(DriveSession driver){return Mathf.Max(0,serviceUntil[driver.SimulationDriver]-Elapsed);}
    public bool ServiceLocked(DriveSession driver){return Finished||ServiceRemaining(driver)>0;}
    public void ObserveServiceTime(DriveSession driver){
        if(Finished)return;
        int index=driver.SimulationDriver;
        Ledger.SyncExpenses(index,driver.business.ledger.Account.Expense(driver.business.ledger.Day,null));
        double charged=driver.business.ledger.Elapsed-driver.business.SimulationOperationSeconds;
        double added=charged-observedServiceSeconds[index];
        if(added>.05){serviceUntil[index]=Mathf.Max(Elapsed,serviceUntil[index])+(float)added;observedServiceSeconds[index]=charged;}
    }
    public void Initialize(DriveSession human) {
        Human=human;human.simulationMatch=this;Ledger=new PvpSimulationLedger();
        var root=new GameObject("경쟁 택시 세션");root.SetActive(false);
        Bot=root.AddComponent<DriveSession>();Bot.IsSimulationBot=true;Bot.simulationMatch=this;Bot.enabled=false;
        Bot.world=human.world;Bot.traffic=human.traffic;Bot.roadEnvironment=human.roadEnvironment;Bot.ambientPedestrians=human.ambientPedestrians;
        Bot.taxi=new GameObject("상대 봇 택시 · 실제 물리 운전").AddComponent<TaxiController>();
        var stop=human.jobs.stops[human.jobs.stops.Length/2];
        // Stop kerbs may be offset from their graph lane (the campaign driver
        // manually approaches them). Spawn on the actual directed lane so the
        // native steering can arm, rather than stranding a bot on a kerb.
        var lane=human.jobs.nav.hd.paths[stop.laneEdge];float spawnAt=Mathf.Clamp(stop.laneAt,3,Mathf.Max(3,lane.length-3));
        Vector3 spawn=lane.Position(spawnAt),heading=lane.Tangent(spawnAt);
        for(int offset=0;offset<8;offset++){
            float candidate=Mathf.Clamp(spawnAt-offset*7,3,Mathf.Max(3,lane.length-3));var point=lane.Position(candidate);var direction=lane.Tangent(candidate);
            if(Physics.CheckBox(point+Vector3.up*.85f,new Vector3(1.1f,.55f,2.7f),Quaternion.LookRotation(direction),(1<<8)|(1<<9),QueryTriggerInteraction.Ignore))continue;
            spawn=point;heading=direction;break;
        }
        Bot.taxi.Create(spawn+Vector3.up*.65f,Quaternion.LookRotation(heading));Bot.taxi.automated=true;
        // Layer 8 is the human's body, hidden by the original cockpit camera.
        // A competitor must remain visible in every camera and traffic query.
        foreach(var part in Bot.taxi.GetComponentsInChildren<Transform>())part.gameObject.layer=9;
        Bot.signals=Bot.taxi.gameObject.AddComponent<TaxiSignals>();
        Bot.assisted=Bot.taxi.gameObject.AddComponent<AssistedDriveController>();Bot.assisted.Initialize(Bot.taxi,human.world.map);Bot.assisted.allowAutomated=true;Bot.taxi.assistance=Bot.assisted;
        Bot.manners=Bot.taxi.gameObject.AddComponent<DrivingManners>();Bot.manners.session=Bot;
        Bot.law=Bot.taxi.gameObject.AddComponent<DrivingLaw>();Bot.law.session=Bot;
        Bot.jobs=root.AddComponent<PassengerJobs>();Bot.jobs.Initialize(Bot);
        Bot.assisted.hd.nav=Bot.jobs.nav;
        Bot.business=root.AddComponent<BusinessProgress>();Bot.business.Initialize(Bot);
        Bot.services=root.AddComponent<ServiceStops>();Bot.services.session=Bot;Bot.services.enabled=false;
        Bot.requests=root.AddComponent<PassengerRequestUI>();Bot.requests.session=Bot;
        root.AddComponent<PassengerBehaviorUI>().session=Bot;
        Bot.damage=Bot.taxi.gameObject.AddComponent<VehicleDamageController>();Bot.damage.session=Bot;
        root.AddComponent<PvpSimulationBot>().Initialize(Bot);
        Bot.MarkSharedSimulationReady();root.SetActive(true);Bot.jobs.ResetOffers();
    }
    void Update() {
        if(Finished||!Human)return;
        ObserveServiceTime(Human);ObserveServiceTime(Bot);
        // Offline menus pause the entire shared simulation and the competition clock.
        if(Time.timeScale>0&&!Human.paused)Elapsed+=Time.deltaTime;
        if(Remaining<=0)Finish();
    }
    public void Finish() {
        if(Finished)return;ObserveServiceTime(Human);ObserveServiceTime(Bot);Ledger.Finish();Human.paused=true;Bot.paused=true;
        Human.taxi.autoThrottle=Bot.taxi.autoThrottle=0;Human.taxi.autoBrake=Bot.taxi.autoBrake=true;
        Time.timeScale=0;
    }
    void LateUpdate(){if(Finished){Human.paused=true;Bot.paused=true;Time.timeScale=0;}}
    public void ReturnToMenu() { PvpSimulationMode.Reset();Time.timeScale=1;SceneManager.LoadScene("PvpBootstrap"); }
    public void Restart() { bool expanded=PvpSimulationMode.Expanded;int seconds=PvpSimulationMode.DurationSeconds;PvpSimulationMode.Configure(expanded,seconds);Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    void OnGUI() {
        if(!Human||Ledger==null)return;
        if(style==null){style=new GUIStyle(GUI.skin.box){font=KoreanText.Font,fontSize=16,alignment=TextAnchor.MiddleCenter,wordWrap=true};button=new GUIStyle(GUI.skin.button){font=KoreanText.Font,fontSize=16};}
        GUI.depth=-250;
        if(!Finished)return;
        float x=Screen.width/2-250,y=Screen.height/2-125;
        GUI.Box(new Rect(x,y,500,250),"경기 종료 · "+(HumanScore==BotScore?"무승부":HumanScore>BotScore?"승리":"봇 승리")+"\n나 "+HumanScore.ToString("N0")+"원 / "+Human.jobs.completed+"회\n봇 "+BotScore.ToString("N0")+"원 / "+Bot.jobs.completed+"회\n하차 완료 수입 − 실제 서비스 지출 · 캠페인 저장 없음",style);
        if(GUI.Button(new Rect(x+30,y+190,440,40),"경기 설정으로 돌아가기",button))ReturnToMenu();
    }
}
