using UnityEngine;
public class DriveSession : MonoBehaviour {
    public string startupRegion="";
    public TaxiController taxi; public WorldBuilder world; public TrafficSystem traffic; public DrivingCamera drivingCamera; public TaxiSignals signals;
    public CockpitMirrors mirrors;public PassengerJobs jobs;public AssistCamera assist;public RoadEnvironment roadEnvironment;public DrivingManners manners;
    public BusinessProgress business; public DrivingLaw law; public ServiceStops services; public PassengerRequestUI requests;
    public AssistedDriveController assisted;
    public RoadConditions roadConditions;
    public bool PracticeMenuOpen {get{var p=GetComponent<PracticeSession>();var c=GetComponent<PracticeCoach>();return p&&p.PickerOpen||c&&c.ResultOpen;}}
    public bool PracticeBusy {get{var p=GetComponent<PracticeSession>();return p&&p.Busy;}}
    public bool paused;public bool ShowHints; public int visits; bool inside;
    void Awake() {
        MapCatalog.LockedRegion=startupRegion;
        foreach(var arg in System.Environment.GetCommandLineArgs())if(arg.EndsWith("Test"))Application.runInBackground=true;
        Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
        world=new GameObject("NGII tile").AddComponent<WorldBuilder>(); world.Build();
        roadEnvironment=new GameObject("인도와 도로 경계").AddComponent<RoadEnvironment>();roadEnvironment.Build(world.map);
        var dir=(world.map.targets[1].V()-world.map.targets[0].V()).normalized; var right=new Vector3(dir.z,0,-dir.x);
        taxi=new GameObject("Temporary Taxi").AddComponent<TaxiController>(); taxi.Create(world.map.targets[0].V(.65f)-dir*(RoadRulesTrial.Enabled?90:23)+right*(RoadRulesTrial.Enabled?1.75f:2.5f),Quaternion.LookRotation(dir));
        signals=taxi.gameObject.AddComponent<TaxiSignals>();
        assisted=taxi.gameObject.AddComponent<AssistedDriveController>();assisted.Initialize(taxi,world.map);taxi.assistance=assisted;
        bool driveTest=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-smokeTest")>=0;
        traffic=new GameObject("Traffic simulation").AddComponent<TrafficSystem>();traffic.Build(world.map,!driveTest);
        var cam=new GameObject("Driving Camera"); cam.tag="MainCamera"; var c=cam.AddComponent<Camera>(); c.fieldOfView=62; c.nearClipPlane=.08f; c.farClipPlane=700; c.backgroundColor=new Color(.57f,.74f,.84f); c.clearFlags=CameraClearFlags.Skybox; cam.AddComponent<AudioListener>(); drivingCamera=cam.AddComponent<DrivingCamera>();drivingCamera.target=taxi.transform;
        RenderSettings.ambientLight=new Color(.65f,.69f,.75f); RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.15f; sun.transform.rotation=Quaternion.Euler(45,-35,0); sun.shadows=LightShadows.Soft;
        gameObject.AddComponent<DriveHUD>().session=this;gameObject.AddComponent<DrivingReadability>().session=this;
        manners=taxi.gameObject.AddComponent<DrivingManners>();manners.session=this;
        jobs=gameObject.AddComponent<PassengerJobs>();jobs.Initialize(this);
        if(world.map.HasHd){jobs.enabled=world.map.hybrid;assisted.hd.nav=jobs.nav;}
        business=gameObject.AddComponent<BusinessProgress>();business.Initialize(this);gameObject.AddComponent<BusinessHUD>().session=this;
        law=taxi.gameObject.AddComponent<DrivingLaw>();law.session=this;services=gameObject.AddComponent<ServiceStops>();services.session=this;requests=gameObject.AddComponent<PassengerRequestUI>();requests.session=this;gameObject.AddComponent<RightTurnSituation>().session=this;gameObject.AddComponent<ExitQueueObserver>().session=this;gameObject.AddComponent<ExitQueueSituation>().session=this;gameObject.AddComponent<PracticeSession>().session=this;gameObject.AddComponent<PracticeCoach>().session=this;gameObject.AddComponent<ConfusionPractice>().session=this;gameObject.AddComponent<ServiceLawZones>().session=this;
        roadConditions=gameObject.AddComponent<RoadConditions>();roadConditions.Initialize(this);jobs.nav.conditions=roadConditions;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-districtTrafficTest")>=0)gameObject.AddComponent<DistrictTrafficSmokeTest>().session=this;
        assist=new GameObject("Automatic camera monitor").AddComponent<AssistCamera>();assist.session=this;
        mirrors=new GameObject("상시 운전석 거울").AddComponent<CockpitMirrors>();mirrors.session=this;
        if(world.map.HasHd&&!world.map.hybrid){gameObject.AddComponent<HdRouteTrial>().session=this;if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-hdTest")>=0)gameObject.AddComponent<HdDriveSmokeTest>().session=this;}
        if(world.map.hybrid){gameObject.AddComponent<HybridMapStatus>().session=this;if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-hybridTest")>=0)gameObject.AddComponent<HybridDriveSmokeTest>().session=this;}
        if(RoadRulesTrial.Enabled)gameObject.AddComponent<RoadRulesTrial>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-presentationTest")>=0)gameObject.AddComponent<RoadPresentationProbe>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-visibilityTest")>=0)gameObject.AddComponent<VisibilityProbe>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-roadRulesTest")>=0)gameObject.AddComponent<RoadRulesSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-economyTest")>=0) gameObject.AddComponent<EconomySmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-assistedTest")>=0) gameObject.AddComponent<AssistedDriveSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-assistedTrial")>=0) gameObject.AddComponent<AssistedRouteTrial>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-trialTest")>=0) gameObject.AddComponent<AssistedTrialSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-turnTest")>=0) gameObject.AddComponent<TurnDriveTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-junctionTurnTest")>=0) gameObject.AddComponent<JunctionTurnTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-greenFlowTest")>=0)gameObject.AddComponent<GreenFlowSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-navigationChoiceTest")>=0)gameObject.AddComponent<NavigationChoiceSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-roadConditionsTest")>=0)gameObject.AddComponent<RoadConditionsSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-eungyeTest")>=0)gameObject.AddComponent<EungyeSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-roadIndexTest")>=0)gameObject.AddComponent<RoadEnvironmentIndexSmokeTest>();
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-liveExitChoicesTest")>=0)gameObject.AddComponent<LiveExitChoicesTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-liveTrafficTest")>=0)gameObject.AddComponent<LiveTrafficValidationTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-lawZoneLabelsTest")>=0)gameObject.AddComponent<LawZoneLabelsSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-serviceLawZonesTest")>=0)gameObject.AddComponent<ServiceLawZonesSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-confusionDriveTest")>=0)gameObject.AddComponent<ConfusionDriveSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-confusionPracticeTest")>=0)gameObject.AddComponent<ConfusionPracticeSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-drivingJudgmentTest")>=0)gameObject.AddComponent<DrivingJudgmentSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-exitSelectionTest")>=0)gameObject.AddComponent<ExitSelectionSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-exitFeedbackTest")>=0)gameObject.AddComponent<ExitFeedbackSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-practiceFeedbackTest")>=0)gameObject.AddComponent<PracticeFeedbackSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-practiceUXTest")>=0)gameObject.AddComponent<PracticeUXSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-completionTest")>=0)gameObject.AddComponent<CompletionSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-campaignTest")>=0)gameObject.AddComponent<CampaignSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-scenarioTest")>=0)gameObject.AddComponent<ScenarioSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-practiceCheckTest")>=0)gameObject.AddComponent<PracticeSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-contentFlowTest")>=0)gameObject.AddComponent<ContentFlowSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-campaignDriveTest")>=0)gameObject.AddComponent<CampaignDriveSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-exitTransportTest")>=0)gameObject.AddComponent<ExitTransportSmokeTest>().session=this;
        if(driveTest) gameObject.AddComponent<DriveSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-trafficTest")>=0) gameObject.AddComponent<TrafficSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-jobsTest")>=0) gameObject.AddComponent<JobsSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cockpitTest")>=0) gameObject.AddComponent<CockpitSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-flowTest")>=0) gameObject.AddComponent<TrafficFlowProbe>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-stabilityTest")>=0) gameObject.AddComponent<StabilitySmokeTest>().session=this;
    }
    void Update() {
        if(Input.GetKeyDown(KeyCode.F10))ShowHints=!ShowHints;
        if(Input.GetKeyDown(KeyCode.F11)){if(Screen.fullScreen)Screen.SetResolution(Mathf.Min(1600,Screen.currentResolution.width-80),Mathf.Min(900,Screen.currentResolution.height-100),FullScreenMode.Windowed);else Screen.SetResolution(Screen.currentResolution.width,Screen.currentResolution.height,FullScreenMode.FullScreenWindow);}
        if(!PracticeMenuOpen&&!PracticeBusy&&!business.restartPrompt&&!business.ShowingSummary&&!services.open&&!business.preparing&&!business.taxPrompt&&!law.HistoryOpen&&(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.JoystickButton7))) {paused=!paused;Time.timeScale=paused?0:1;}
        if(!paused&&!business.ShowingSummary&&Input.GetKeyDown(KeyCode.Tab))jobs.nav.TogglePreference();
        bool now=Vector3.Distance(taxi.transform.position,world.map.targets[1].V(taxi.transform.position.y))<12;
        if(now&&!inside) visits++; inside=now;
    }
    void OnDestroy(){Time.timeScale=1;}
}
