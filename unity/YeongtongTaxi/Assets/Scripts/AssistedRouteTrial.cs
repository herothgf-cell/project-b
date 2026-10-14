using System.Collections.Generic;
using UnityEngine;

// Opt-in, repeatable route comparison; never loads or saves the campaign.
public class AssistedRouteTrial : MonoBehaviour {
    public DriveSession session;
    public float Elapsed {get;private set;}
    public bool Finished {get;private set;}
    public bool Congested {get;private set;}
    public List<AssistedRoadNetwork.Lane> primary,alternative;
    public Vector3 goal;
    readonly List<GameObject> blockers=new List<GameObject>();
    float[] trafficPositions;bool[] trafficVisible;bool ready;GUIStyle style;
    string lastResult="",error="";float beginManners;
    const float ReleaseSeconds=120;
    void Start(){
        var map=session.world.map;var forward=(map.targets[1].V()-map.targets[0].V()).normalized;var exit=-TrafficSystem.Right(forward);float at;
        var network=session.assisted.network;
        var start=network.FindLane(map.targets[0].V()-forward*23+TrafficSystem.Right(forward)*2.5f,forward,out at);
        var finish=network.FindLane(map.targets[1].V()+exit*40+TrafficSystem.Right(exit)*2.5f,exit,out at);
        primary=network.FindRoute(start,finish,-1);alternative=network.Alternative(primary);
        if(primary.Count<2||alternative.Count<2){error="시험 경로를 구성하지 못했습니다. H로 일반 보조 운전을 시험하세요.";return;}
        goal=finish.path.Position(at);
        DrawRoute(primary,new Color(.1f,.8f,1),"본선");DrawRoute(alternative,new Color(1,.65f,.1f),"우회로");
        var marker=WorldBuilder.Box("시험 도착 구역",goal+Vector3.up*.12f,new Vector3(3,.04f,6),WorldBuilder.Mat("Trial finish",Color.green),transform,false);marker.transform.rotation=Quaternion.LookRotation(finish.path.Tangent(at));
        var label=new GameObject("시험 목적지");label.transform.SetParent(transform);label.transform.position=goal+Vector3.up*3;
        var text=label.AddComponent<TextMesh>();text.text="경로 시험 도착\n정차해서 기록";text.characterSize=.1f;text.fontSize=40;text.anchor=TextAnchor.MiddleCenter;KoreanText.Apply(text);label.AddComponent<FaceCamera>();
        var alternativeCorridors=new HashSet<int>();foreach(var lane in alternative)alternativeCorridors.Add(lane.corridor);
        AssistedRoadNetwork.Lane blocked=null;foreach(var lane in primary)if(!alternativeCorridors.Contains(lane.corridor)&&lane.path.length>30){blocked=lane;break;}
        if(blocked!=null){for(int i=0;i<3;i++){
            float d=Mathf.Min(blocked.path.length-5,12+i*7);var car=WorldBuilder.Box("시험 정체 차량",blocked.path.Position(d)+Vector3.up*.75f,new Vector3(1.8f,1.4f,4),WorldBuilder.Mat("Trial queue",new Color(.7f,.16f,.1f)),transform);
            car.transform.rotation=Quaternion.LookRotation(blocked.path.Tangent(d));car.layer=9;blockers.Add(car);
        }}
        trafficPositions=new float[session.traffic.cars.Count];trafficVisible=new bool[trafficPositions.Length];for(int i=0;i<trafficPositions.Length;i++){trafficPositions[i]=session.traffic.cars[i].distance;trafficVisible[i]=session.traffic.cars[i].GetComponent<Collider>().enabled;}
        ready=true;Restart(false);
    }
    public void Restart(bool congested){
        if(!ready||Time.timeScale==0)return;
        Congested=congested;Finished=false;Elapsed=0;session.jobs.ClearForDay();session.jobs.balance=0;session.jobs.completed=0;session.jobs.failed=0;
        session.traffic.manualClock=true;session.traffic.manualTime=0;
        for(int i=0;i<trafficPositions.Length;i++){var car=session.traffic.cars[i];car.enabled=true;if(trafficVisible[i])car.Place(trafficPositions[i]);else car.Recycle();}
        // Resolve taxi occupancy after restoring traffic, preserving initially recycled cars.
        session.taxi.ResetTaxi();session.manners.BeginRide();beginManners=session.manners.events;
        foreach(var blocker in blockers)blocker.SetActive(congested);
        session.assisted.SetActive(true);
    }
    void Update(){
        if(!ready||Time.timeScale==0)return;
        if(Input.GetKeyDown(KeyCode.F6))Restart(false);
        if(Input.GetKeyDown(KeyCode.F7))Restart(true);
        if(!Finished){Elapsed+=Time.deltaTime;session.traffic.manualTime=Elapsed;
            if(Elapsed>=ReleaseSeconds)foreach(var blocker in blockers)blocker.SetActive(false);
            var delta=session.taxi.transform.position-goal;delta.y=0;
            if(delta.magnitude<4&&session.taxi.body.velocity.magnitude<.56f){Finished=true;lastResult=(Congested?"정체":"정상")+" 완료 "+Elapsed.ToString("0.0")+"초 · 운전 감점 사건 "+(session.manners.events-beginManners).ToString("0")+"회";Debug.Log("ASSISTED_TRIAL_RESULT "+lastResult);}
        }
    }
    void DrawRoute(List<AssistedRoadNetwork.Lane> route,Color color,string title){
        var points=new List<Vector3>();
        for(int i=0;i<route.Count;i++){
            points.AddRange(route[i].path.points);
            if(i+1<route.Count&&route[i].corridor!=route[i+1].corridor)for(int turn=-1;turn<=1;turn++){var join=session.assisted.network.Connection(route[i],turn);if(join!=null&&join.target==route[i+1]){points.AddRange(join.path.points);break;}}
        }
        var obj=new GameObject(title+" 시험 경로");obj.transform.SetParent(transform);var line=obj.AddComponent<LineRenderer>();line.material=WorldBuilder.Mat(title+" 안내",color);line.startWidth=line.endWidth=.16f;line.positionCount=points.Count;
        for(int i=0;i<points.Count;i++)line.SetPosition(i,points[i]+Vector3.up*.15f);
    }
    void OnGUI(){
        if(session.business.ShowingSummary)return;
        if(style==null)style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=14,wordWrap=true};
        GUI.Box(new Rect(16,269,380,139),GUIContent.none);
        string text=ready?"경로 비교 시험 · "+(Congested?"정체":"정상")+" · "+Elapsed.ToString("0.0")+"초\n하늘색 본선 / 주황색 우회 / 초록 도착 구역\nF6 정상 재시작 · F7 정체 재시작\n"+(Congested&&Elapsed<ReleaseSeconds?"본선 정체 해제까지 "+Mathf.CeilToInt(ReleaseSeconds-Elapsed)+"초":"신호와 앞차를 확인하며 직접 제동하세요")+"\n"+lastResult:error;
        GUI.Label(new Rect(28,275,355,126),text,style);
    }
}
