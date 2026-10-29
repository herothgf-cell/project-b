using System;
using UnityEngine;
using Pvp.Map;
using Pvp.Driving;
namespace Pvp {
// Offline kinematic experiment. No passengers, match clock, score, or network authority.
public sealed class PvpPracticeSession : MonoBehaviour {
    public PvpMapRuntime Map { get; private set; }
    public PvpRoute CurrentRoute { get; private set; }
    public float DrivenDistance { get; private set; }
    public int BoundaryStops { get; private set; }
    PvpVehicleMotor motor;
    PvpRoadPosition road;
    GameObject world,car;
    Camera view;
    LineRenderer routeLine;
    Material carMaterial,routeMaterial;
    int target;
    PvpRouteMode routeMode;
    bool reverse,overview,scripted;
    float routeRefresh;
    string feedback="화살표 방향으로 주행하세요. 승객·정산·온라인은 아직 없습니다.";
    public void Begin(PvpMapDefinition definition) {
        if(Map!=null) throw new InvalidOperationException("Practice already started");
        var validated=new PvpMapRuntime(definition);
        if(validated.Spawns.Count==0||validated.Stops.Count==0)throw new ArgumentException("Practice requires spawn and stop points");
        foreach(int spawnPoint in validated.Spawns)foreach(var stop in validated.Stops)
            if(!PvpRoutePlanner.FindRoute(validated,spawnPoint,stop.Node).Found)throw new ArgumentException("Unreachable practice stop");
        foreach(var from in validated.Stops)foreach(var to in validated.Stops)
            if(!PvpRoutePlanner.FindRoute(validated,from.Node,to.Node).Found)throw new ArgumentException("Practice stop has no legal exit");
        world=new GameObject("PVP practice world");world.transform.SetParent(transform,false);
        Map=PvpMapLoader.Build(definition,world.transform);
        int spawn=Map.Spawns[0],edge=-1;
        for(int i=0;i<Map.Connections.Count;i++)if(Map.Connections[i].From==spawn){edge=i;break;}
        if(edge<0)throw new InvalidOperationException("Spawn has no outgoing lane");
        road=new PvpRoadPosition(edge,.05f);
        var forward=Map.Forward(road);
        motor=new PvpVehicleMotor();motor.Restore(new PvpVehicleState {Position=Map.Position(road),YawDegrees=Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg});
        car=GameObject.CreatePrimitive(PrimitiveType.Cube);car.name="PVP trial taxi";car.transform.SetParent(world.transform,false);car.transform.localScale=new Vector3(1.8f,1.3f,3.6f);
        carMaterial=new Material(Shader.Find("Standard")){color=new Color(1,.76f,.12f)};car.GetComponent<Renderer>().sharedMaterial=carMaterial;
        var nose=GameObject.CreatePrimitive(PrimitiveType.Cube);nose.name="Taxi front";nose.transform.SetParent(car.transform,false);nose.transform.localPosition=new Vector3(0,.3f,.35f);nose.transform.localScale=new Vector3(.75f,.3f,.2f);nose.GetComponent<Renderer>().sharedMaterial=carMaterial;
        var lightObject=new GameObject("PVP sun");lightObject.transform.SetParent(world.transform,false);lightObject.transform.rotation=Quaternion.Euler(55,-30,0);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;
        view=GetComponentInChildren<Camera>();if(view!=null){view.cullingMask=~0;view.farClipPlane=500;view.backgroundColor=new Color(.09f,.15f,.15f);}
        var lineObject=new GameObject("PVP legal route");lineObject.transform.SetParent(world.transform,false);routeLine=lineObject.AddComponent<LineRenderer>();routeLine.widthMultiplier=.65f;routeMaterial=new Material(Shader.Find("Sprites/Default"));routeLine.sharedMaterial=routeMaterial;routeLine.startColor=routeLine.endColor=new Color(.1f,1,.9f);
        var panel=GetComponent<PvpPrototypePanel>();if(panel!=null)panel.enabled=false;
        UpdateCar();RefreshRoute();UpdateCamera(true);
        Debug.Log("PVP_PRACTICE_STARTED map="+Map.MapHash+" stops="+Map.Stops.Count+" profile=A offline=true");
    }
    public PvpVehicleState Capture(){if(motor==null)throw new InvalidOperationException("No practice");return motor.Capture();}
    public void PresentNetworkState(PvpVehicleState state) { motor.Restore(in state);routeLine.enabled=false;UpdateCar(); }
    public void ClearNetworkRoute(){if(routeLine!=null)routeLine.enabled=false;}
    public void ToggleNetworkOverview(){overview=!overview;UpdateCamera(true);}
    public void PresentNetworkRoute(Pvp.Network.PvpDriveSnapshot pose,int stop) {
        if(motor==null||stop<0||stop>=Map.Stops.Count)return;road=new PvpRoadPosition(pose.Edge,pose.Fraction);target=stop;RefreshRoute();routeLine.enabled=true;
    }
    public void SetScripted(bool value){scripted=value;}
    public void Step(PvpVehicleInput input,float dt) {
        if(motor==null)throw new InvalidOperationException("No practice");
        var before=motor.Capture();motor.Simulate(in input,dt);var next=motor.Capture();PvpRoadPosition located;
        if(Map.TryLocate(next.Position,road,out located)) {
            road=located;DrivenDistance+=Vector3.Distance(before.Position,next.Position);
        } else {
            // Hard road boundary for this no-contact experiment; never advance the recovery anchor off-road.
            before.Speed=0;motor.Restore(in before);BoundaryStops++;feedback="도로 경계입니다. 감속해 방향을 바꾸거나 Backspace로 후방 복구하세요.";
        }
        UpdateCar();
    }
    public void Recover() {
        PvpRoadPosition recovered;if(!Map.TryRecover(road,3,out recovered))throw new InvalidOperationException("No safe recovery");
        var state=motor.Capture();var forward=Map.Forward(recovered);state.Position=Map.Position(recovered);state.Speed=0;state.Steering=0;state.YawDegrees=Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg;motor.Restore(in state);road=recovered;reverse=false;UpdateCar();RefreshRoute();feedback="같은 합법 차로의 후방으로 복구했습니다.";
        Debug.Log("PVP_PRACTICE_RECOVERY edge="+road.Edge+" fraction="+road.Fraction);
    }
    public void NextTarget(){target=(target+1)%Map.Stops.Count;RefreshRoute();}
    void RefreshRoute(){CurrentRoute=PvpRoutePlanner.FindRoute(Map,road,Map.Stops[target].Node,routeMode);routeLine.positionCount=CurrentRoute.Points.Length;for(int i=0;i<CurrentRoute.Points.Length;i++)routeLine.SetPosition(i,CurrentRoute.Points[i]+Vector3.up*.16f);}
    void UpdateCar(){if(car==null)return;var state=motor.Capture();car.transform.position=state.Position+Vector3.up*.75f;car.transform.rotation=Quaternion.Euler(0,state.YawDegrees,0);}
    void FixedUpdate(){if(motor==null||scripted)return;bool focus=Application.isFocused;float throttle=Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0;float brake=Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.Space)?1:0;float steer=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);var input=new PvpVehicleInput(throttle,brake,steer,reverse,focus);Step(input,Time.fixedDeltaTime);}
    void Update(){if(motor==null||scripted)return;
        if(Input.GetKeyDown(KeyCode.Tab))NextTarget();
        if(Input.GetKeyDown(KeyCode.M))overview=!overview;
        if(Input.GetKeyDown(KeyCode.Q)&&Mathf.Abs(motor.Capture().Speed)<.35f){reverse=!reverse;feedback=reverse?"후진 선택":"전진 선택";}
        if(Input.GetKeyDown(KeyCode.Backspace))Recover();
        if(Input.GetKeyDown(KeyCode.Alpha1))motor.SetProfile(PvpVehicleProfile.A);
        if(Input.GetKeyDown(KeyCode.Alpha2))motor.SetProfile(PvpVehicleProfile.B);
        if(Input.GetKeyDown(KeyCode.Escape)){ExitPractice();return;}
        routeRefresh+=Time.unscaledDeltaTime;if(routeRefresh>.25f){routeRefresh=0;RefreshRoute();}
    }
    void LateUpdate(){UpdateCamera(false);}
    void UpdateCamera(bool immediate){if(view==null||motor==null)return;var p=motor.Capture().Position;var desired=overview?new Vector3(0,175,-.1f):p+new Vector3(0,36,-27);view.transform.position=immediate?desired:Vector3.Lerp(view.transform.position,desired,1-Mathf.Exp(-7*Time.unscaledDeltaTime));view.transform.LookAt(overview?Vector3.zero:p);}
    void OnGUI(){if(motor==null||GetComponent<Pvp.Network.PvpNetworkSession>()!=null)return;var state=motor.Capture();GUILayout.BeginArea(new Rect(16,16,600,260),GUI.skin.box);GUILayout.Label("PVP 자체 시험장 / 오프라인 주행 후보 "+state.Profile);GUILayout.Label("속도 "+(Mathf.Abs(state.Speed)*3.6f).ToString("F0")+" km/h  | "+(reverse?"후진":"전진")+" | 주행 "+DrivenDistance.ToString("F0")+"m");GUILayout.Label("W/↑ 가속 · S/↓/Space 제동 · A/D 조향 · 정차 후 Q 전후진");GUILayout.Label("1/2 차량 후보 · Tab 목적지 · M 전체 지도 · Backspace 복구 · Esc 초기 화면");GUILayout.Label("목표: "+StopName(Map.Stops[target].Id)+" | "+(routeMode==PvpRouteMode.Shortest?"합법 최단 거리":"예상 최속 (시험 속도)")+" | "+(CurrentRoute.Found?CurrentRoute.Distance.ToString("F0")+"m":"경로 없음"));GUILayout.BeginHorizontal();if(GUILayout.Button("다음 정차점"))NextTarget();if(GUILayout.Button("경로 방식 변경")){routeMode=routeMode==PvpRouteMode.Shortest?PvpRouteMode.Fastest:PvpRouteMode.Shortest;RefreshRoute();}if(GUILayout.Button("초기 화면"))ExitPractice();GUILayout.EndHorizontal();GUILayout.Label(feedback);GUILayout.EndArea();}
    static string StopName(string id){if(id.Contains("terminal"))return "동쪽 터미널";if(id.Contains("market"))return "북쪽 상업 구역";if(id.Contains("warehouse"))return "서쪽 창고";return "남쪽 공원";}
    public void ExitPractice(){var panel=GetComponent<PvpPrototypePanel>();if(panel!=null)panel.enabled=true;DisposeWorld();if(Application.isPlaying)Destroy(this);else DestroyImmediate(this);}
    void OnDestroy(){DisposeWorld();}
    void DisposeWorld(){if(world!=null){if(Application.isPlaying)Destroy(world);else DestroyImmediate(world);world=null;}if(carMaterial!=null){if(Application.isPlaying)Destroy(carMaterial);else DestroyImmediate(carMaterial);carMaterial=null;}if(routeMaterial!=null){if(Application.isPlaying)Destroy(routeMaterial);else DestroyImmediate(routeMaterial);routeMaterial=null;}if(view!=null){view.cullingMask=0;view.backgroundColor=new Color(.035f,.05f,.08f);}motor=null;}
}}
