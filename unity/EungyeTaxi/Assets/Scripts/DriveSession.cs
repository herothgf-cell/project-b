using UnityEngine;
using System.Collections;
public class DriveSession : MonoBehaviour {
    public string startupRegion="";
    public TaxiController taxi; public WorldBuilder world; public TrafficSystem traffic; public DrivingCamera drivingCamera; public TaxiSignals signals;
    public CockpitMirrors mirrors;public PassengerJobs jobs;public AssistCamera assist;public RoadEnvironment roadEnvironment;public DrivingManners manners;
    public BusinessProgress business; public DrivingLaw law; public ServiceStops services; public PassengerRequestUI requests;
    public AssistedDriveController assisted;
    public RoadConditions roadConditions;
    public bool PracticeMenuOpen {get{var p=GetComponent<PracticeSession>();var c=GetComponent<PracticeCoach>();return p&&p.PickerOpen||c&&c.ResultOpen;}}
    public bool PracticeBusy {get{var p=GetComponent<PracticeSession>();return p&&p.Busy;}}
    public bool ChoiceOpen {get{var c=GetComponent<ScenarioChoiceSituation>();return c&&(c.Open||c.ClosedThisFrame);}}
    public AmbientPedestrianSystem ambientPedestrians;
    public bool paused;public bool ShowHints; public int visits; bool inside;
    public bool RideResultOpen {get{var result=GetComponent<RideCompletionPanel>();return result&&result.Open;}}
    public bool EditorOpen {get{var e=GetComponent<RoadMapEditor>();return e&&(e.IsOpen||e.ClosedThisFrame);}}
    public bool FullMapOpen {get{var map=GetComponent<FullMapOverlay>();return map&&map.IsOpen||EditorOpen||RideResultOpen;}}
    public bool StartupReady {get;private set;}
    string loadingStage="지도를 준비 중입니다.";
    Camera loadingCamera;
    GUIStyle loadingTitle,loadingDetail;
    void Awake() {
        MapCatalog.LockedRegion=startupRegion;
        foreach(var arg in System.Environment.GetCommandLineArgs())if(arg.EndsWith("Test"))Application.runInBackground=true;
        Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
        Time.timeScale=0;paused=true;
        loadingCamera=new GameObject("시작 준비 화면").AddComponent<Camera>();
        loadingCamera.clearFlags=CameraClearFlags.SolidColor;loadingCamera.backgroundColor=new Color(.06f,.09f,.13f);loadingCamera.cullingMask=0;
    }
    IEnumerator Start() {
        // Render the loading screen before synchronous map construction.
        yield return null;yield return null;
        var startupTimer=System.Diagnostics.Stopwatch.StartNew();var stageTimer=System.Diagnostics.Stopwatch.StartNew();
        world=new GameObject("NGII tile").AddComponent<WorldBuilder>(); world.Build();
        Debug.Log("STARTUP_TIMING world_ms="+stageTimer.ElapsedMilliseconds);loadingStage="도로와 교통을 준비하고 있습니다";
        yield return null;stageTimer.Restart();
        roadEnvironment=new GameObject("인도와 도로 경계").AddComponent<RoadEnvironment>();roadEnvironment.Build(world.map);
        var dir=(world.map.targets[1].V()-world.map.targets[0].V()).normalized; var right=new Vector3(dir.z,0,-dir.x);
        taxi=new GameObject("Temporary Taxi").AddComponent<TaxiController>(); taxi.Create(world.map.targets[0].V(.65f)-dir*(RoadRulesTrial.Enabled?90:23)+right*(RoadRulesTrial.Enabled?1.75f:2.5f),Quaternion.LookRotation(dir));
        signals=taxi.gameObject.AddComponent<TaxiSignals>();
        assisted=taxi.gameObject.AddComponent<AssistedDriveController>();assisted.Initialize(taxi,world.map);taxi.assistance=assisted;
        bool driveTest=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-smokeTest")>=0;
        traffic=new GameObject("Traffic simulation").AddComponent<TrafficSystem>();traffic.Build(world.map,!driveTest);
        Debug.Log("STARTUP_TIMING roads_taxi_traffic_ms="+stageTimer.ElapsedMilliseconds);loadingStage="보행자와 영업 상태를 준비하고 있습니다";
        yield return null;stageTimer.Restart();
        if(world.map.hybrid){ambientPedestrians=new GameObject("일반 보행자").AddComponent<AmbientPedestrianSystem>();ambientPedestrians.Initialize(this);traffic.ambientPedestrians=ambientPedestrians;}
        Debug.Log("STARTUP_TIMING ambient_ms="+stageTimer.ElapsedMilliseconds);stageTimer.Restart();
        var cam=new GameObject("Driving Camera"); cam.tag="MainCamera"; var c=cam.AddComponent<Camera>(); c.fieldOfView=62; c.nearClipPlane=.08f; c.farClipPlane=700; c.backgroundColor=new Color(.57f,.74f,.84f); c.clearFlags=CameraClearFlags.Skybox; cam.AddComponent<AudioListener>(); drivingCamera=cam.AddComponent<DrivingCamera>();drivingCamera.target=taxi.transform;
        RenderSettings.ambientLight=new Color(.65f,.69f,.75f); RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.15f; sun.transform.rotation=Quaternion.Euler(45,-35,0); sun.shadows=LightShadows.Soft;
        gameObject.AddComponent<RoadMapEditor>().session=this;gameObject.AddComponent<RoadMapTrialSession>();gameObject.AddComponent<DriveHUD>().session=this;gameObject.AddComponent<DrivingReadability>().session=this;gameObject.AddComponent<FullMapOverlay>().session=this;
        manners=taxi.gameObject.AddComponent<DrivingManners>();manners.session=this;
        jobs=gameObject.AddComponent<PassengerJobs>();jobs.Initialize(this);
        if(world.map.HasHd){jobs.enabled=world.map.hybrid;assisted.hd.nav=jobs.nav;}
        paused=false;Time.timeScale=1;
        business=gameObject.AddComponent<BusinessProgress>();business.Initialize(this);gameObject.AddComponent<BusinessHUD>().session=this;
        law=taxi.gameObject.AddComponent<DrivingLaw>();law.session=this;services=gameObject.AddComponent<ServiceStops>();services.session=this;requests=gameObject.AddComponent<PassengerRequestUI>();requests.session=this;gameObject.AddComponent<RightTurnSituation>().session=this;gameObject.AddComponent<ExitQueueObserver>().session=this;gameObject.AddComponent<ExitQueueSituation>().session=this;gameObject.AddComponent<PracticeSession>().session=this;gameObject.AddComponent<PracticeCoach>().session=this;gameObject.AddComponent<ConfusionPractice>().session=this;gameObject.AddComponent<ServiceLawZones>().session=this;
        gameObject.AddComponent<RoadSignage>().session=this;gameObject.AddComponent<DriveNotifications>().session=this;gameObject.AddComponent<DrivingScoreJournal>().session=this;
        gameObject.AddComponent<RideCompletionPanel>().session=this;gameObject.AddComponent<StopOccupancySystem>().session=this;
        roadConditions=gameObject.AddComponent<RoadConditions>();roadConditions.Initialize(this);jobs.nav.conditions=roadConditions;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-districtTrafficTest")>=0)gameObject.AddComponent<DistrictTrafficSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-mitTrafficTest")>=0)gameObject.AddComponent<MitTrafficSmokeTest>().session=this;
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
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-roadReadabilityTest")>=0)gameObject.AddComponent<RoadReadabilityProbe>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-playabilityUITest")>=0)gameObject.AddComponent<PlayabilityUISmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-manualHandlingTest")>=0)gameObject.AddComponent<ManualHandlingSmokeTest>().session=this;
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
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-driveFeedbackTest")>=0)gameObject.AddComponent<DriveFeedbackSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-roadMapEditorTest")>=0)gameObject.AddComponent<RoadMapEditorProbe>();
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-ambientPedestrianTest")>=0||System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-ambientPedestrianReloadTest")>=0)gameObject.AddComponent<AmbientPedestrianProbe>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-projectMapTest")>=0||System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-projectMapStaleTest")>=0)gameObject.AddComponent<ProjectMapEditorProbe>();
        if(driveTest) gameObject.AddComponent<DriveSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-trafficTest")>=0) gameObject.AddComponent<TrafficSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-jobsTest")>=0) gameObject.AddComponent<JobsSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cockpitTest")>=0) gameObject.AddComponent<CockpitSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-flowTest")>=0) gameObject.AddComponent<TrafficFlowProbe>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-stabilityTest")>=0) gameObject.AddComponent<StabilitySmokeTest>().session=this;
        // Preserve the pause selected by BusinessProgress for preparation or saved-session restoration.
        StartupReady=true;
        if(loadingCamera)Destroy(loadingCamera.gameObject);
        Debug.Log("STARTUP_TIMING gameplay_ms="+stageTimer.ElapsedMilliseconds+" total_ms="+startupTimer.ElapsedMilliseconds);
    }
    void OnGUI(){
        if(StartupReady)return;
        GUI.depth=-10000;var oldColor=GUI.color;GUI.color=new Color(.06f,.09f,.13f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=oldColor;
        if(loadingTitle==null){loadingTitle=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=28,alignment=TextAnchor.MiddleCenter};loadingDetail=new GUIStyle(loadingTitle){fontSize=18};}
        GUI.Label(new Rect(0,Screen.height*.43f,Screen.width,50),"택시 시뮬래이터",loadingTitle);
        GUI.Label(new Rect(0,Screen.height*.43f+55,Screen.width,40),loadingStage,loadingDetail);
    }
    void Update() {
        if(!StartupReady)return;
        var choice=GetComponent<ScenarioChoiceSituation>();if(choice&&(choice.Open||choice.ClosedThisFrame))return;
        if(FullMapOpen||GetComponent<FullMapOverlay>().ClosedThisFrame)return;
        if(Input.GetKeyDown(KeyCode.F10))ShowHints=!ShowHints;
        if(Input.GetKeyDown(KeyCode.F11)){if(Screen.fullScreen)Screen.SetResolution(Mathf.Min(1600,Screen.currentResolution.width-80),Mathf.Min(900,Screen.currentResolution.height-100),FullScreenMode.Windowed);else Screen.SetResolution(Screen.currentResolution.width,Screen.currentResolution.height,FullScreenMode.FullScreenWindow);}
        if(!PracticeMenuOpen&&!PracticeBusy&&!business.restartPrompt&&!business.ShowingSummary&&!services.open&&!business.preparing&&!business.taxPrompt&&!law.HistoryOpen&&(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.JoystickButton7))) {paused=!paused;Time.timeScale=paused?0:1;}
        if(!paused&&!business.ShowingSummary&&Input.GetKeyDown(KeyCode.Tab))jobs.nav.TogglePreference();
        bool now=Vector3.Distance(taxi.transform.position,world.map.targets[1].V(taxi.transform.position.y))<12;
        if(now&&!inside) visits++; inside=now;
    }
    void OnDestroy(){Time.timeScale=1;}
}
