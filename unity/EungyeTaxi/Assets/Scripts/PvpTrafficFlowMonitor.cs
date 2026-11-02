using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Opt-in observer of production motion. It never moves actors, changes traffic,
// controls time, changes signals or terminates another acceptance probe.
public sealed class PvpTrafficFlowMonitor : MonoBehaviour {
    sealed class Motion {
        public TrafficRoute route;public Rigidbody body;public bool visible;
        public Vector3 position;public float physicalMetres,pause,mergePause,maxPause,maxMergePause;
        public int samples,moving,signal,merge,junction,obstacle,unexplained;
    }
    readonly Dictionary<TrafficCar,Motion> cars=new Dictionary<TrafficCar,Motion>();
    readonly Dictionary<PvpSimulationBot,float> bots=new Dictionary<PvpSimulationBot,float>();
    readonly HashSet<string> lanes=new HashSet<string>();
    readonly Dictionary<PvpSimulationBot,HashSet<string>> leftLinks=new Dictionary<PvpSimulationBot,HashSet<string>>(),rightLinks=new Dictionary<PvpSimulationBot,HashSet<string>>();
    DriveSession session;float next,begin,last,maxError,maxHeadingError,maxHeadingStep;string worstLane="none";
    int samples;bool written;
    public static PvpTrafficFlowMonitor Active {get;private set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpTrafficFlowMonitor")<0)return;var root=new GameObject("Read-only PvP natural flow monitor");DontDestroyOnLoad(root);root.AddComponent<PvpTrafficFlowMonitor>();}
    IEnumerator Start(){
        Active=this;PvpSimulationMatch match;
        while(!(match=FindObjectOfType<PvpSimulationMatch>())||!match.Human||!match.Human.StartupReady)yield return null;
        session=match.Human;begin=last=Time.time;next=Time.time+.25f;
        foreach(var driver in match.Drivers){var bot=driver.GetComponent<PvpSimulationBot>();if(bot)bots[bot]=bot.MetresDriven;}
    }
    void Update(){
        if(!session||Time.time<next)return;float elapsed=Time.time-last;last=Time.time;next=Time.time+.25f;samples++;
        foreach(var car in session.traffic.cars){
            if(!car)continue;Motion state;if(!cars.TryGetValue(car,out state)){state=new Motion{body=car.GetComponent<Rigidbody>()};cars[car]=state;}
            if(!car.Visible||!car.enabled||!car.gameObject.activeInHierarchy||car.route==null){state.visible=false;state.pause=state.mergePause=0;continue;}
            var position=state.body?state.body.position:car.transform.position;position.y=0;
            if(state.visible&&state.route==car.route){
                float distance=Vector3.Distance(position,state.position);
                if(distance<10){state.physicalMetres+=distance;state.samples++;
                    if(distance>.025f){state.moving++;state.pause=state.mergePause=0;}
                    else {state.pause+=elapsed;state.maxPause=Mathf.Max(state.maxPause,state.pause);
                        if(car.waitingForSignal){state.signal++;state.mergePause=0;}
                        else if(car.waitingForMerge){state.merge++;state.mergePause+=elapsed;state.maxMergePause=Mathf.Max(state.maxMergePause,state.mergePause);}
                        else {state.mergePause=0;if(car.waitingForJunction)state.junction++;else if(car.waitingForObstacle)state.obstacle++;else state.unexplained++;}
                    }
                }
                var path=car.route;float error;float at=path.ProjectNear(position,car.distance,out error);
                var forward=state.body?state.body.rotation*Vector3.forward:car.transform.forward;
                float headingError=Vector3.Angle(forward,path.Tangent(at));
                maxHeadingStep=Mathf.Max(maxHeadingStep,Vector3.Angle(path.Tangent(Mathf.Max(0,car.distance-car.speed*elapsed)),path.Tangent(car.distance)));
                maxHeadingError=Mathf.Max(maxHeadingError,headingError);maxError=Mathf.Max(maxError,error);
                if(car.route.graphPath!=null&&car.district){string lane=car.district.graph.edges[car.route.graphPath.EdgeAt(car.distance)].id;lanes.Add(lane);if(error>=maxError)worstLane=lane;}
            }
            state.visible=true;state.position=position;state.route=car.route;
        }
    }
    void LateUpdate(){
        if(!session||samples==0)return;
        foreach(var row in bots){var bot=row.Key;if(!bot||!bot.Session||!bot.Session.jobs||!bot.Session.jobs.nav)continue;var nav=bot.Session.jobs.nav;var route=nav.hdRoute;if(!nav.active||route==null||bot.Session.taxi.body.velocity.magnitude<.2f)continue;
            float error;float at=nav.ProjectVehicle(out error);if(error>2)continue;var edge=nav.hd.edges[route.EdgeAt(at)];var table=edge.turn==-1?leftLinks:edge.turn==1?rightLinks:null;if(table==null)continue;HashSet<string> links;if(!table.TryGetValue(bot,out links)){links=new HashSet<string>();table[bot]=links;}links.Add(edge.id);
        }
    }
    public string Snapshot(){
        var result=new StringBuilder();int movingCars=0,movingBots=0,total=0,moving=0,signal=0,merge=0,junction=0,obstacle=0,unexplained=0;float metres=0,maxPause=0,maxMergePause=0;
        foreach(var row in cars){var value=row.Value;if(value.physicalMetres>10)movingCars++;metres+=value.physicalMetres;total+=value.samples;moving+=value.moving;signal+=value.signal;merge+=value.merge;junction+=value.junction;obstacle+=value.obstacle;unexplained+=value.unexplained;maxPause=Mathf.Max(maxPause,value.maxPause);maxMergePause=Mathf.Max(maxMergePause,value.maxMergePause);}
        foreach(var row in bots)if(row.Key){float travel=row.Key.MetresDriven-row.Value;if(travel>10)movingBots++;result.AppendLine("bot="+row.Key.name+" metres="+travel+" observedUniqueLeftTurnLinks="+(leftLinks.ContainsKey(row.Key)?leftLinks[row.Key].Count:0)+" observedUniqueRightTurnLinks="+(rightLinks.ContainsKey(row.Key)?rightLinks[row.Key].Count:0)+" stationarySeconds="+row.Key.StationarySeconds+" status="+row.Key.Status);}
        result.Insert(0,"NATURAL_FLOW seconds="+(Time.time-begin)+" snapshots="+samples+" movingNPCs="+movingCars+" movingBots="+movingBots+" physicalNPCMetres="+metres+" observedLanes="+lanes.Count+"\nactorSamples="+total+" moving="+moving+" stoppedSignal="+signal+" stoppedMerge="+merge+" stoppedJunction="+junction+" stoppedObstacle="+obstacle+" stoppedUnexplained="+unexplained+" maxPauseSeconds="+maxPause+" maxMergePauseSeconds="+maxMergePause+"\nmaxRouteErrorMetres="+maxError+" maxHeadingErrorDegrees="+maxHeadingError+" maxPathHeadingStepDegrees="+maxHeadingStep+" worstDeviationLane="+worstLane+"\n");
        if(session)result.AppendLine("mergeCalls="+session.traffic.MergeGapCalls+" fleetObservations="+session.traffic.MergeFleetObservations+" mergePhysicsQueries="+session.traffic.MergePhysicsQueries+" peakCompatibleGrants="+session.traffic.MergePeakConcurrentGrants);
        foreach(var row in cars)if(row.Key)result.AppendLine("npc="+row.Key.name+" metres="+row.Value.physicalMetres+" maxPause="+row.Value.maxPause+" maxMergePause="+row.Value.maxMergePause+" samples="+row.Value.samples+" blocked="+row.Key.blockedName+" merge="+row.Key.mergeStatus);
        return result.ToString();
    }
    void OnApplicationQuit(){WriteReport();}
    public void WriteReport(){
        if(written||!session)return;written=true;
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-pvpFlowOutput");if(index<0)index=Array.IndexOf(args,"-trafficPerfOutput");
        string folder=index>=0&&index+1<args.Length?args[index+1]:"artifacts";Directory.CreateDirectory(folder);string report=Snapshot();File.WriteAllText(Path.Combine(folder,"pvp-natural-flow.txt"),report);Debug.Log(report);
    }
}
