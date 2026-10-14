using UnityEngine;
public class DriveSession : MonoBehaviour {
    public TaxiController taxi; public WorldBuilder world; public TrafficSystem traffic; public DrivingCamera drivingCamera; public TaxiSignals signals;
    public CockpitMirrors mirrors;public PassengerJobs jobs;public AssistCamera assist;public RoadEnvironment roadEnvironment;public DrivingManners manners;
    public BusinessProgress business;
    public AssistedDriveController assisted;
    public bool paused; public int visits; bool inside;
    void Awake() {
        foreach(var arg in System.Environment.GetCommandLineArgs())if(arg.EndsWith("Test"))Application.runInBackground=true;
        Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
        world=new GameObject("NGII tile").AddComponent<WorldBuilder>(); world.Build();
        roadEnvironment=new GameObject("인도와 도로 경계").AddComponent<RoadEnvironment>();roadEnvironment.Build(world.map);
        var dir=new Vector3(.62f,0,.78f).normalized; var right=new Vector3(dir.z,0,-dir.x);
        taxi=new GameObject("Temporary Taxi").AddComponent<TaxiController>(); taxi.Create(world.map.targets[0].V(.65f)-dir*23+right*2.5f,Quaternion.LookRotation(dir));
        signals=taxi.gameObject.AddComponent<TaxiSignals>();
        assisted=taxi.gameObject.AddComponent<AssistedDriveController>();assisted.Initialize(taxi,world.map);taxi.assistance=assisted;
        bool driveTest=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-smokeTest")>=0;
        traffic=new GameObject("Traffic simulation").AddComponent<TrafficSystem>();traffic.Build(world.map,!driveTest);
        var cam=new GameObject("Driving Camera"); cam.tag="MainCamera"; var c=cam.AddComponent<Camera>(); c.fieldOfView=62; c.nearClipPlane=.08f; c.farClipPlane=700; c.backgroundColor=new Color(.57f,.74f,.84f); c.clearFlags=CameraClearFlags.Skybox; cam.AddComponent<AudioListener>(); drivingCamera=cam.AddComponent<DrivingCamera>();drivingCamera.target=taxi.transform;
        RenderSettings.ambientLight=new Color(.65f,.69f,.75f); RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.15f; sun.transform.rotation=Quaternion.Euler(45,-35,0); sun.shadows=LightShadows.Soft;
        gameObject.AddComponent<DriveHUD>().session=this;
        manners=taxi.gameObject.AddComponent<DrivingManners>();manners.session=this;
        jobs=gameObject.AddComponent<PassengerJobs>();jobs.Initialize(this);
        business=gameObject.AddComponent<BusinessProgress>();business.Initialize(this);gameObject.AddComponent<BusinessHUD>().session=this;
        assist=new GameObject("Automatic camera monitor").AddComponent<AssistCamera>();assist.session=this;
        mirrors=new GameObject("상시 운전석 거울").AddComponent<CockpitMirrors>();mirrors.session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-economyTest")>=0) gameObject.AddComponent<EconomySmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-assistedTest")>=0) gameObject.AddComponent<AssistedDriveSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-assistedTrial")>=0) gameObject.AddComponent<AssistedRouteTrial>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-trialTest")>=0) gameObject.AddComponent<AssistedTrialSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-turnTest")>=0) gameObject.AddComponent<TurnDriveTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-junctionTurnTest")>=0) gameObject.AddComponent<JunctionTurnTest>().session=this;
        if(driveTest) gameObject.AddComponent<DriveSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-trafficTest")>=0) gameObject.AddComponent<TrafficSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-jobsTest")>=0) gameObject.AddComponent<JobsSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cockpitTest")>=0) gameObject.AddComponent<CockpitSmokeTest>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-flowTest")>=0) gameObject.AddComponent<TrafficFlowProbe>().session=this;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-stabilityTest")>=0) gameObject.AddComponent<StabilitySmokeTest>().session=this;
    }
    void Update() {
        if(!business.ShowingSummary&&(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.JoystickButton7))) {paused=!paused;Time.timeScale=paused?0:1;}
        bool now=Vector3.Distance(taxi.transform.position,world.map.targets[1].V(taxi.transform.position.y))<12;
        if(now&&!inside) visits++; inside=now;
    }
    void OnDestroy(){Time.timeScale=1;}
}
