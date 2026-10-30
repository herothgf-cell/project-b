using System.Collections.Generic;
using UnityEngine;

public class TrafficJunction { public RoadMapSignalProgram editorProgram; public float[] editorStops=new float[4],editorCrossings=new float[4]; public float StopDistance(int axis,int sign){int i=axis*2+(sign>0?0:1);return editorStops[i]>0?editorStops[i]:halfWidth[1-axis]+4;} public float CrossDistance(int axis,int sign){int i=axis*2+(sign>0?0:1);return editorCrossings[i]>0?editorCrossings[i]:halfWidth[1-axis]+CrosswalkCentreOffset;}
    public bool scenarioOpposingGreen,scenarioFlashing,scenarioLampOn=true;public bool scenarioSignal; public SignalColor scenarioForward; public bool signalized=true; public RoadObservation rightTurnObservation; public int observationAxis,observationSign; public HybridJunctionRule facility; public int crosswalkApproaches,stopLineApproaches;
    public readonly List<int> lampAxes=new List<int>(),lampSigns=new List<int>();
    public readonly List<bool> leftMovements=new List<bool>();
    public readonly List<int> lampStates=new List<int>();
    public Vector3 direction;
    public LeftTurnMode leftTurnMode;
    public float[] halfWidth={5,10};public int[] laneCount={1,2};public bool[] centeredLane={false,false};
    // Sampling tolerance beyond the painted line; shared by independent law and passenger manners.
    public const float StopLineEntryTolerance=.25f;
    public const float CrosswalkCentreOffset=1.8f, CrosswalkDepth=2.4f;
    public float CrossingNearEdge(int incomingAxis){return halfWidth[incomingAxis]+CrosswalkCentreOffset-CrosswalkDepth*.5f;}
    public float StopDistance(int axis){return halfWidth[1-axis]+4;}
    public Vector3 center;public TrafficCar owner;
    public readonly HashSet<TrafficCar> reservations=new HashSet<TrafficCar>();
    public readonly Dictionary<TrafficCar,float> requests=new Dictionary<TrafficCar,float>();
    public readonly List<Renderer[]> leftLamps=new List<Renderer[]>();
    public string name;
    public float offset;
    public readonly List<Renderer[]> lamps = new List<Renderer[]>();
}
public class TrafficRoute {
    public HdRoadGraph.RoutePath graphPath; public int goalEdge; public float goalAt;
    public Vector3 start, end, direction;
    public float length;
    public int axis,turn;public Vector3[] points;float[] cumulative;
    public void SetPoints(Vector3[] values){points=values;cumulative=new float[points.Length];for(int i=1;i<points.Length;i++)cumulative[i]=cumulative[i-1]+Vector3.Distance(points[i-1],points[i]);start=points[0];end=points[points.Length-1];direction=(points[1]-points[0]).normalized;length=cumulative[cumulative.Length-1];}
    public Vector3 Position(float d){
        if(points==null)return start+direction*d;
        if(points.Length<2||d>length)return end;
        // Lower bound preserves the old first matching segment at duplicate vertices.
        // Collision lookahead samples this route many times per physics tick.
        int lo=1,hi=points.Length-1;
        while(lo<hi){int mid=lo+(hi-lo)/2;if(cumulative[mid]>=d)hi=mid;else lo=mid+1;}
        return Vector3.Lerp(points[lo-1],points[lo],Mathf.InverseLerp(cumulative[lo-1],cumulative[lo],d));
    }
    public Vector3 Tangent(float d){return (Position(Mathf.Min(length,d+.4f))-Position(Mathf.Max(0,d-.4f))).normalized;}
    public float ProjectNear(Vector3 point,float around,out float error){
        float low=Mathf.Max(0,around-65),high=Mathf.Min(length,around+65),best=float.PositiveInfinity,result=around;
        int lo=1,hi=cumulative.Length-1;while(lo<hi){int mid=(lo+hi)/2;if(cumulative[mid]>=low)hi=mid;else lo=mid+1;}
        for(int i=lo;i<points.Length&&cumulative[i-1]<=high;i++){var d=points[i]-points[i-1];float t=Mathf.Clamp01(Vector3.Dot(point-points[i-1],d)/Mathf.Max(.00001f,d.sqrMagnitude));float at=Mathf.Lerp(cumulative[i-1],cumulative[i],t);if(at<low||at>high)continue;float e=(point-points[i-1]-d*t).sqrMagnitude;if(e<best){best=e;result=at;}}
        error=Mathf.Sqrt(best);return result;
    }

    public readonly List<TrafficStop> stops = new List<TrafficStop>();
}
public class TrafficStop { public TrafficJunction junction; public float distance,exitDistance;public int axis;public int turn=99;public int Movement(TrafficRoute route){return turn==99?route.turn:turn;} }

public class TrafficSystem : MonoBehaviour {
    public AmbientPedestrianSystem ambientPedestrians;
    public MitTrafficOptions mitOptions {get;private set;}
    void Awake(){mitOptions=new MitTrafficOptions();}
    public readonly List<TrafficJunction> junctions = new List<TrafficJunction>();
    public readonly List<TrafficCar> cars = new List<TrafficCar>();
    NativeTrafficMergePolicy authoredMerges;
    public int AuthoredMergeCount {get{return authoredMerges==null?0:authoredMerges.Count;}}
    public void RegisterMergeDriver(DriveSession driver){if(authoredMerges!=null)authoredMerges.Register(driver);}
    public float MergeApproachGap(TrafficCar car,out string reason){reason="";return authoredMerges==null?float.PositiveInfinity:authoredMerges.Gap(car,out reason);}
    public float MergeApproachGap(DriveSession driver,out string reason){reason="";return authoredMerges==null?float.PositiveInfinity:authoredMerges.Gap(driver,out reason);}
    public string MergeDiagnostics(TrafficCar car){return authoredMerges==null?"off":authoredMerges.Diagnostics(car);}
    public string MergeDiagnostics(DriveSession driver){return authoredMerges==null?"off":authoredMerges.Diagnostics(driver);}
    public bool manualClock;
    public float manualTime;
    public float Clock { get { return manualClock ? manualTime : Time.time; } }
    public Vector3 mainDirection;
    Material white, yellow, dark, pole;
    Material[] lit, unlit;
    readonly List<Vector3> whitePaint=new List<Vector3>(), yellowPaint=new List<Vector3>();
    MapData roadMap;HdRoadGraph editorGraph;bool editorSpeedRules;public float EditorSpeedLimit(TrafficCar car){if(!editorSpeedRules||editorGraph==null||car.route.graphPath==null)return 0;int edge=car.route.graphPath.EdgeAt(car.distance);float error;float at=editorGraph.paths[edge].Project(car.transform.position,out error);return RoadMapRules.SpeedLimit(roadMap,editorGraph.edges[edge].id,at);}RoadMapWalker[] editorWalkers;bool EditorPedestrian(TrafficJunction j){return editorWalkers!=null&&System.Array.Exists(editorWalkers,w=>w&&w.crossing.junctionId==j.name&&w.Crossing);}public Vector3 StopPosition(TrafficJunction j,int axis,int sign){var f=roadMap.editorDocument==null?null:System.Array.Find(roadMap.editorDocument.facilities,x=>x.kind=="stopLine"&&x.junctionId==j.name&&x.axis==axis&&x.sign==sign);return f==null?j.center-AxisDirection(j,axis)*sign*j.StopDistance(axis,sign):f.Position;} public Vector3 StopNormal(TrafficJunction j,int axis,int sign){var f=roadMap.editorDocument==null?null:System.Array.Find(roadMap.editorDocument.facilities,x=>x.kind=="stopLine"&&x.junctionId==j.name&&x.axis==axis&&x.sign==sign);return f==null?AxisDirection(j,axis)*sign:Quaternion.Euler(0,f.yaw,0)*Vector3.forward;} public float EditorSpeedLimit(Vector3 position,Vector3 heading){if(editorGraph==null||!editorSpeedRules)return 0;float at;int edge=editorGraph.Nearest(position,heading,out at);return edge<0?0:RoadMapRules.SpeedLimit(roadMap,editorGraph.edges[edge].id,at);} public string EditorLaneId(Vector3 position,Vector3 heading){if(editorGraph==null)return null;float at;int edge=editorGraph.Nearest(position,heading,out at);return edge<0?null:editorGraph.edges[edge].id;}
    readonly TrafficOverlapQuery overlaps=new TrafficOverlapQuery();
    sealed class MovementEnvelope {public TrafficRoute route;public Vector3[] points;public float from,to;public Bounds bounds;}
    readonly Dictionary<TrafficStop,MovementEnvelope> movementEnvelopes=new Dictionary<TrafficStop,MovementEnvelope>();
    readonly Dictionary<TrafficStop,Dictionary<TrafficStop,bool>> conflicts=new Dictionary<TrafficStop,Dictionary<TrafficStop,bool>>();
    readonly Dictionary<TrafficStop,Dictionary<TrafficStop,float>> approachGates=new Dictionary<TrafficStop,Dictionary<TrafficStop,float>>();

    public Vector3 AxisDirection(TrafficJunction j,int axis){var d=j.direction.sqrMagnitude>.1f?j.direction:mainDirection;return axis==0?d:Right(d);}
    public int Sign(Vector3 direction,int axis,TrafficJunction j=null){return Vector3.Dot(direction,j!=null?AxisDirection(j,axis):axis==0?mainDirection:Right(mainDirection))>=0?1:-1;}
    public bool HasPedestrianPhase(TrafficJunction j){return j.editorProgram!=null?System.Array.Exists(j.editorProgram.groups,g=>g.pedestrian&&System.Array.Exists(j.editorProgram.stages,s=>System.Array.IndexOf(s.groups,g.id)>=0)):j.signalized&&j.facility!=null&&j.facility.crosswalk;}
    float VehicleCycle(TrafficJunction j){return 17*(j.leftTurnMode==LeftTurnMode.Permissive?2:4);}
    float PhaseTime(TrafficJunction j){float cycle=VehicleCycle(j)+(HasPedestrianPhase(j)?12:0);return ((Clock+j.offset)%cycle+cycle)%cycle;}
    public bool PedestrianGreen(TrafficJunction j,int axis,int sign){if(j.editorProgram!=null)return j.editorProgram.Walk(axis,sign,Clock);
        // Right-turn observations describe the crossing AFTER the turn, not the taxi's incoming arm.
        int crossingAxis=1-j.observationAxis,crossingSign=j.observationAxis==0?-j.observationSign:j.observationSign;
        if(j.rightTurnObservation!=null&&crossingAxis==axis&&crossingSign==sign)return j.rightTurnObservation.pedestrianGreen;if(j.scenarioSignal||!HasPedestrianPhase(j))return false;float phase=PhaseTime(j);return phase>=VehicleCycle(j)&&phase<VehicleCycle(j)+10;}
    public float PedestrianGreenRemaining(TrafficJunction j,int axis,int sign){if(!PedestrianGreen(j,axis,sign))return 0;if(j.editorProgram!=null)return -1;return Mathf.Max(0,VehicleCycle(j)+10-PhaseTime(j));}
    public SignalColor Signal(TrafficJunction junction, int axis,int sign=1) { if(junction.editorProgram!=null)return junction.editorProgram.Movement(axis,sign,0,Clock); return junction.scenarioSignal?(axis==junction.observationAxis&&(sign==junction.observationSign||junction.scenarioOpposingGreen)?junction.scenarioForward:SignalColor.Red):junction.signalized?(PhaseTime(junction)>=VehicleCycle(junction)?SignalColor.Red:KoreanSignalRules.Signal(PhaseTime(junction),junction.leftTurnMode,axis,sign)):SignalColor.Green; }
    public float SecondsToSignalChange(TrafficJunction j,int axis,int sign=1){if(j!=null&&j.editorProgram!=null)return -1;if(j==null||!j.signalized||j.scenarioSignal)return -1;float phase=PhaseTime(j),cycle=VehicleCycle(j);int approach=j.leftTurnMode==LeftTurnMode.Permissive?axis:axis*2+(sign>0?0:1);if(phase>=cycle)return cycle+12+approach*17-phase;float seconds=KoreanSignalRules.SecondsToChange(phase,j.leftTurnMode,axis,sign);return seconds+(HasPedestrianPhase(j)&&phase+seconds>=cycle-.001f?12:0);}
    public SignalColor LeftSignal(TrafficJunction junction,int axis,int sign=1){if(junction.editorProgram!=null)return junction.editorProgram.Movement(axis,sign,-1,Clock);if(junction.scenarioSignal&&junction.leftTurnMode==LeftTurnMode.Permissive)return SignalColor.Red;return Signal(junction,axis,sign);}
    public SignalColor MovementSignal(TrafficJunction junction,int axis,int turn,int sign=1){return turn==UturnMovement.Turn?SignalColor.Red:junction.editorProgram!=null?junction.editorProgram.Movement(axis,sign,turn,Clock):Signal(junction,axis,sign);}
    public RoadObservation Observe(TrafficJunction j,int axis,int sign,Vector3 direction,GameObject self){
        var o=j.rightTurnObservation!=null&&j.observationAxis==axis&&j.observationSign==sign?j.rightTurnObservation.Copy():new RoadObservation{known=true,pedestrian=PedestrianState.None};
        // Unbound programs cannot use a lane ID. Resolving it twice per GUI
        // pass scanned the complete graph at the commercial midblock signal.
        string signalLane=null;if(j.editorProgram!=null&&self&&j.editorProgram.NeedsLaneId(axis,sign))signalLane=EditorLaneId(self.transform.position,self.transform.forward);
        o.forward=j.editorProgram!=null?j.editorProgram.Movement(axis,sign,0,Clock,signalLane):Signal(j,axis,sign);if(j.editorProgram!=null){o.dedicated=true;o.right=j.editorProgram.Movement(axis,sign,1,Clock,signalLane);}if(j.rightTurnObservation==null)o.pedestrianGreen=PedestrianGreen(j,axis,sign);if(j.editorProgram!=null&&EditorPedestrian(j))o.pedestrian=PedestrianState.Crossing;if(ambientPedestrians!=null&&ambientPedestrians.ObservedPedestrian(j,direction,self))o.pedestrian=PedestrianState.Crossing;o.trafficClear=YieldClear(j,direction,1,self);return o;
    }
    public bool CanEnter(TrafficCar car,TrafficStop stop){
        if(stop.Movement(car.route)==UturnMovement.Turn){var uturnJunction=stop.junction;var heading=car.route.Tangent(stop.distance);int uturnSign=Sign(heading,stop.axis,uturnJunction);string lane=car.route.graphPath!=null&&editorGraph!=null?editorGraph.edges[car.route.graphPath.EdgeAt(Mathf.Max(0,stop.distance))].id:null;return UturnPermitted(uturnJunction,stop.axis,uturnSign,lane)&&Observe(uturnJunction,stop.axis,uturnSign,heading,car.gameObject).PedestrianClear&&!EditorPedestrian(uturnJunction)&&UturnClear(car,stop); }if(!stop.junction.signalized&&!stop.junction.scenarioSignal)return true;
        var direction=car.route.graphPath==null?car.route.direction:car.route.Tangent(Mathf.Max(0,stop.distance));int turn=stop.Movement(car.route);
        var j=stop.junction;int sign=Sign(direction,stop.axis,j);
        var observed=j.rightTurnObservation!=null&&j.observationAxis==stop.axis&&j.observationSign==sign?j.rightTurnObservation.Copy():new RoadObservation{known=true,pedestrian=PedestrianState.None};
        observed.forward=j.editorProgram!=null?j.editorProgram.Movement(stop.axis,sign,turn,Clock,car.route.graphPath!=null?editorGraph.edges[car.route.graphPath.EdgeAt(Mathf.Max(0,stop.distance))].id:null):MovementSignal(j,stop.axis,turn,sign);if(j.editorProgram!=null)return observed.forward==SignalColor.Green&&!EditorPedestrian(j)&&observed.PedestrianClear&&YieldClear(j,direction,turn,car.gameObject);observed.trafficClear=turn==0&&!j.scenarioFlashing||YieldClear(j,direction,turn,car.gameObject);
        if(stop.junction.scenarioFlashing)return (observed.forward!=SignalColor.Red||car.stoppedAt==stop.junction)&&observed.PedestrianClear&&YieldClear(stop.junction,direction,turn,car.gameObject);
        if(turn>0)return observed.MayTurnRight(car.stoppedAt==stop.junction);
        if(stop.junction.scenarioSignal)return observed.forward==SignalColor.Green&&(turn==0||observed.PedestrianClear)&&observed.trafficClear;
        return observed.forward==SignalColor.Green&&observed.trafficClear&&(turn==0||observed.PedestrianClear);
    }
    public RoadMapFacility UturnCondition(string laneId){return UturnMovement.Condition(roadMap==null?null:roadMap.editorDocument,laneId);}
    // Authored lanes can cross a skewed junction axis outside its legacy lateral strip.
    // Keep the graph search local to the observed stop line, then require a directed connection.
    int DirectedApproachEdge(TrafficJunction junction,int axis,int sign,Vector3 position,Vector3 heading){
        if(editorGraph==null||roadMap==null||roadMap.editorDocument==null)return -1;
        var direction=AxisDirection(junction,axis)*sign;if(Vector3.Dot(heading,direction)<.7f)return -1;
        var front=position+heading*2;var delta=front-StopPosition(junction,axis,sign);float radius=junction.halfWidth[axis]+4;
        if(Mathf.Abs(Vector3.Dot(delta,StopNormal(junction,axis,sign)))>8||delta.sqrMagnitude>radius*radius)return -1;
        float at;int edge=editorGraph.Nearest(position,heading,out at);if(edge<0)return -1;
        var lane=editorGraph.edges[edge];var path=editorGraph.paths[edge];
        if(lane.ruleId!=junction.name||Vector3.Dot(path.Tangent(at),direction)<.7f||Vector3.Dot(heading,path.Tangent(at))<.7f)return -1;
        var authored=System.Array.Find(roadMap.editorDocument.lanes,l=>l.id==lane.id);if(authored==null)return -1;
        float error;path.Project(position,out error);if(error>authored.width*.5f+.35f)return -1;
        return System.Array.Exists(lane.next,next=>!editorGraph.edges[next].disabled&&editorGraph.edges[next].change==0&&editorGraph.edges[next].ruleId==junction.name&&editorGraph.edges[next].fromNode==lane.toNode)?edge:-1;
    }
    public bool DirectedApproach(TrafficJunction junction,int axis,int sign,Vector3 position,Vector3 heading){return DirectedApproachEdge(junction,axis,sign,position,heading)>=0;}
    public bool SignedUturnApproach(TrafficJunction junction,int axis,int sign,Vector3 position,Vector3 heading){int edge=DirectedApproachEdge(junction,axis,sign,position,heading);if(edge<0)return false;var condition=UturnCondition(editorGraph.edges[edge].id);return condition!=null&&condition.junctionId==junction.name&&condition.axis==axis&&condition.sign==sign;}
    public bool UturnPermitted(TrafficJunction j,int axis,int sign,string laneId){var condition=UturnCondition(laneId);if(condition==null)return false;bool left=j.editorProgram!=null?j.editorProgram.Movement(axis,sign,-1,Clock,laneId)==SignalColor.Green:j.leftTurnMode!=LeftTurnMode.Permissive&&LeftSignal(j,axis,sign)==SignalColor.Green;bool walk=j.editorProgram!=null&&!string.IsNullOrEmpty(condition.groupId)?j.editorProgram.Color(condition.groupId,Clock)==SignalColor.Green:PedestrianGreen(j,axis,sign);return UturnMovement.Allows(condition.type,left,walk,true);}
    bool UturnClear(TrafficCar car,TrafficStop stop){int count=overlaps.Sphere(stop.junction.center,100,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);for(int i=0;i<count;i++){var hit=overlaps.hits[i];var root=hit.attachedRigidbody?hit.attachedRigidbody.gameObject:hit.gameObject;if(root==car.gameObject)continue;var other=root.GetComponent<TrafficCar>();float deviation;float followerAt=car.route.ProjectNear(root.transform.position,car.distance,out deviation);if(deviation<.8f&&followerAt<car.distance-4.5f&&Vector3.Dot(car.route.Tangent(followerAt),root.transform.forward)>.95f)continue;if(other&&MovingLeader(car,other))continue;float speed=other?other.speed:hit.attachedRigidbody?hit.attachedRigidbody.velocity.magnitude:0;for(float at=Mathf.Max(0,stop.distance);at<=Mathf.Min(car.route.length,stop.exitDistance+8);at+=1.5f){var point=car.route.Position(at);var offset=point-root.transform.position;float along=Vector3.Dot(offset,root.transform.forward);float side=Mathf.Abs(Vector3.Dot(offset,Right(root.transform.forward)));if(offset.sqrMagnitude<3.2f*3.2f||side<3.2f&&along>=-3&&along<Mathf.Max(8,speed*6+5))return false;}}return true;}
    public bool YieldClear(TrafficJunction j,Vector3 incoming,int turn,GameObject self){
        if(turn==0||turn==-1&&j.leftTurnMode!=LeftTurnMode.Permissive)return true;
        int contacts=overlaps.Sphere(j.center,100,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);
        for(int contact=0;contact<contacts;contact++){var hit=overlaps.hits[contact];
            var root=hit.attachedRigidbody?hit.attachedRigidbody.gameObject:hit.gameObject;if(root==self)continue;
            var car=root.GetComponent<TrafficCar>();var heading=root.transform.forward;
            // A moving leader on the same directed turn is a following gap, not crossing traffic.
            if(turn>0&&car&&MovingLeader(self.GetComponent<TrafficCar>(),car))continue;
            float facing=Vector3.Dot(heading,incoming);if(turn!=UturnMovement.Turn&&(turn<0?facing>-.7f:facing>.7f))continue;
            if(turn==-1&&car&&car.CurrentTurn<0)continue;
            int axis=Mathf.Abs(Vector3.Dot(heading,AxisDirection(j,0)))>.7f?0:1;
            float along=Vector3.Dot(j.center-root.transform.position,heading);
            float side=Mathf.Abs(Vector3.Dot(j.center-root.transform.position,Right(heading)));
            float speed=car?car.speed:hit.attachedRigidbody?hit.attachedRigidbody.velocity.magnitude:0;
            if(side>j.halfWidth[axis]+1||along< -j.StopDistance(axis))continue;
            bool inside=along<j.StopDistance(axis)+2;
            // Opposing permissive left must yield to this right turn. Once it has entered,
            // however, let it clear; protected left retains its signal priority.
            if(turn>0&&facing<-.7f&&car&&car.CurrentTurn<0&&j.leftTurnMode==LeftTurnMode.Permissive&&!inside)continue;
            if((inside||Signal(j,axis,Sign(heading,axis,j))==SignalColor.Green)&&along<Mathf.Max(12,speed*6+5)){
                if(turn>0&&car&&!RightTurnConflict(j,incoming,car))continue;
                // A manually driven opposing car travelling straight occupies the other half of the road.
                if(turn>0&&!car&&facing<-.7f){var indicator=root.GetComponent<TaxiSignals>();if(indicator&&indicator.direction>=0&&!inside)continue;}
                return false;
            }
        }
        return true;
    }
    bool RightTurnConflict(TrafficJunction j,Vector3 incoming,TrafficCar other){
        int axis=Mathf.Abs(Vector3.Dot(incoming,AxisDirection(j,0)))>.7f?0:1;var outgoing=Right(incoming);
        float inOffset=j.centeredLane[axis]?0:j.halfWidth[axis]*(1-.5f/j.laneCount[axis]);float outOffset=j.centeredLane[1-axis]?0:j.halfWidth[1-axis]*(1-.5f/j.laneCount[1-axis]);
        var a=j.center-incoming*(j.halfWidth[1-axis]+3)+Right(incoming)*inOffset;
        var b=j.center+outgoing*(j.halfWidth[axis]+3)+Right(outgoing)*outOffset;
        float handle=Vector3.Distance(a,b)*.55f;var c1=a+incoming*handle;var c2=b-outgoing*handle;
        float end=Mathf.Min(other.route.length,other.distance+Mathf.Max(15,other.speed*6+8));
        // Sample the actual AI trajectory against this right-turn corridor, including the merge exit.
        for(float at=Mathf.Max(0,other.distance-2);at<=end;at+=1.5f){var p=other.route.Position(at);
            for(int i=0;i<=40;i++){float t=i/24f,u=1-t;var q=i<=24?u*u*u*a+3*u*u*t*c1+3*u*t*t*c2+t*t*t*b:b+outgoing*(i-24);
                if((p-q).sqrMagnitude<2.5f*2.5f)return true;
            }
        }
        return false;
    }
    public void Request(TrafficCar car,TrafficStop stop){if(!stop.junction.requests.ContainsKey(car))stop.junction.requests[car]=Time.time;}
    public void Build(MapData map, bool spawnCars) {
        roadMap=map;if(map.editorDocument!=null){editorGraph=new HdRoadGraph(map);editorSpeedRules=System.Array.Exists(map.editorDocument.facilities,f=>f.kind=="sign"&&f.rule&&f.type=="제한속도");}
        authoredMerges=NativeTrafficMergePolicy.Supports(map)?new NativeTrafficMergePolicy(this,map,editorGraph??new HdRoadGraph(map)):null;
        mainDirection = (map.targets[1].V() - map.targets[0].V()).normalized;
        if(map.HasHd&&!map.hybrid)return;
        white = WorldBuilder.Mat("White road paint", new Color(.93f,.92f,.84f));
        yellow = WorldBuilder.Mat("Yellow centre paint", new Color(1,.71f,.13f));
        dark = WorldBuilder.Mat("Signal housing", new Color(.055f,.07f,.085f));
        pole = WorldBuilder.Mat("Signal pole", new Color(.28f,.32f,.35f));
        Color[] colors = { new Color(1,.035f,.02f), new Color(1,.55f,.015f), new Color(.025f,1,.22f) };
        lit = new Material[3]; unlit = new Material[3];
        for(int i=0;i<3;i++) {
            lit[i] = WorldBuilder.Mat("Signal lit " + i, colors[i]); lit[i].EnableKeyword("_EMISSION"); lit[i].SetColor("_EmissionColor", colors[i]*2);
            unlit[i] = WorldBuilder.Mat("Signal off " + i, colors[i]*.12f);
        }
        var configured=map.hybrid?System.Array.FindAll(map.junctionRules,r=>r.signalMode>0||r.crosswalk||r.stopLine):null;
        for(int i=0;i<(map.hybrid?configured.Length:2);i++){var target=map.hybrid?new MapTarget{x=configured[i].x,z=configured[i].z,name=configured[i].id,leftTurnMode=configured[i].signalMode==1?LeftTurnMode.Permissive:LeftTurnMode.ProtectedShared}:map.targets[i];var j=new TrafficJunction { center=target.V(), name=target.name, leftTurnMode=target.leftTurnMode, direction=map.hybrid?new Vector3(configured[i].dx,0,configured[i].dz).normalized:mainDirection,offset=i==0?0:7 };
            for(int axis=0;axis<2;axis++){float best=3;var heading=AxisDirection(j,axis);
                foreach(var road in map.roads)for(int n=1;n<road.points.Length;n++){var a=road.points[n-1].V();var d=road.points[n].V()-a;if(Mathf.Abs(Vector3.Dot(d.normalized,heading))<.85f)continue;float at=Mathf.Clamp01(Vector3.Dot(j.center-a,d)/d.sqrMagnitude);float error=Vector3.Distance(j.center,a+d*at);if(error<=best){best=error;j.halfWidth[axis]=road.width*.5f;j.laneCount[axis]=Mathf.Max(1,road.DisplayLanes/2);j.centeredLane[axis]=road.DisplayLanes==1;}}
            }
            if(map.hybrid){j.facility=configured[i];j.signalized=j.facility.signalMode>0;if(j.facility.approaches.Length>0){j.halfWidth=new[]{0f,0f};foreach(var a in j.facility.approaches){var d=new Vector3(a.dx,0,a.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(d,AxisDirection(j,0)))>.7f?0:1;j.halfWidth[axis]=Mathf.Max(j.halfWidth[axis],a.width*.5f);}for(int axis=0;axis<2;axis++){j.halfWidth[axis]=Mathf.Max(3,j.halfWidth[axis]);j.laneCount[axis]=Mathf.Max(1,Mathf.RoundToInt(j.halfWidth[axis]/3.5f));}}}
            if(map.editorDocument!=null){j.editorProgram=System.Array.Find(map.editorDocument.signals,p=>p.junctionId==j.name);foreach(var f in map.editorDocument.facilities){if(f.junctionId!=j.name)continue;int k=f.axis*2+(f.sign>0?0:1);var dir=AxisDirection(j,f.axis)*f.sign;float distance=Vector3.Dot(j.center-f.Position,dir);if(f.kind=="stopLine")j.editorStops[k]=distance;if(f.kind=="crosswalk")j.editorCrossings[k]=distance;}}junctions.Add(j);
        }
        PaintRoads(map);
        foreach(var junction in junctions) {
            if(junction.facility!=null&&junction.facility.approaches.Length>0){foreach(var a in junction.facility.approaches){var d=new Vector3(a.dx,0,a.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(d,AxisDirection(junction,0)))>.7f?0:1;AddApproach(junction,d,a.width*.5f,junction.halfWidth[1-axis]);}continue;}
            for(int axis=0;axis<2;axis++) for(int sign=-1;sign<=1;sign+=2) {
                Vector3 direction = AxisDirection(junction,axis)*sign;
                AddApproach(junction, direction, junction.halfWidth[axis], junction.halfWidth[1-axis]);
            }
        }
        PaintMesh(whitePaint,white);PaintMesh(yellowPaint,yellow);RoadMapFacilities.Build(map.editorDocument,transform,this);editorWalkers=GetComponentsInChildren<RoadMapWalker>();
        if(spawnCars&&map.hybrid){gameObject.AddComponent<DistrictTraffic>().Initialize(this,map,true);return;}
        if(spawnCars&&junctions.Count>0) {
            if(map.mapId=="eungye")Physics.SyncTransforms();
            var first=junctions.Find(j=>Vector3.Distance(j.center,map.targets[0].V())<.1f);var second=junctions.Find(j=>Vector3.Distance(j.center,map.targets[1].V())<.1f);
            if(first!=null&&second!=null){
            var mainStart=map.mapId=="eungye"?MappedCorridorEnd(first,-mainDirection,26,0,map):first.center-mainDirection*26;
            var mainEnd=map.mapId=="eungye"?MappedCorridorEnd(second,mainDirection,34,0,map):second.center+mainDirection*34;
            var forward = MakeRoute(mainStart, mainEnd, 0, map);
            var backward = MakeRoute(mainEnd, mainStart, 0, map);
            Spawn(forward,35,0); Spawn(forward,95,1); Spawn(backward,0,2); Spawn(backward,100,3);
            }
            foreach(var j in junctions) {
                var across=AxisDirection(j,1);
                // Only the verified straight approaches belong to these simple AI routes.
                Vector3 a=j.center-across*Mathf.Min(60,Vector3.Distance(j.center,Edge(j.center,-across,map)));
                Vector3 b=j.center+across*Mathf.Min(60,Vector3.Distance(j.center,Edge(j.center,across,map)));
                if(map.mapId=="eungye"){a=MappedCorridorEnd(j,-across,60,1,map);b=MappedCorridorEnd(j,across,60,1,map);}
                Spawn(MakeRoute(a,b,1,map),0,cars.Count); Spawn(MakeRoute(b,a,1,map),0,cars.Count);
            }
            foreach(var j in junctions){var rule=map.hybrid?System.Array.Find(map.junctionRules,r=>r.id==j.name):null;if(rule==null||rule.allowLeft){var left=Spawn(MakeTurn(j,1,-1),0,cars.Count);if(left)left.Recycle();}if(rule==null||rule.allowRight){var right=Spawn(MakeTurn(j,-1,1),0,cars.Count);if(right)right.Recycle();}}
        }
    }
    // Ask before comfortable braking would begin, so an available green does not become a stop target.
    public float ReservationDistance(TrafficCar car){return Mathf.Max(8,car.speed*car.speed/6+3);}
    bool SameTrafficStream(TrafficCar a,TrafficCar b){
        if(!a||!b)return false;
        if(a.route==b.route)return true;
        // A shared directed turn connector is one following stream, even while the
        // leader has rotated away from the follower's approach heading.
        if(a.CurrentTurn!=0&&a.route.graphPath!=null&&b.route.graphPath!=null){
            foreach(var entry in a.route.stops){if(entry.Movement(a.route)!=a.CurrentTurn||a.distance>entry.exitDistance||entry.distance-a.distance>30)continue;
                bool shared=false;
                foreach(int edge in a.route.graphPath.edges)if(roadMap.hdEdges[edge].ruleId==entry.junction.name&&roadMap.hdEdges[edge].turn==entry.Movement(a.route)&&b.route.graphPath.edges.Contains(edge)){shared=true;break;}
                if(!shared)continue;
                float deviation;float leaderAt=a.route.ProjectNear(b.transform.position,a.distance,out deviation);
                if(Mathf.Abs(leaderAt-a.distance)<60&&deviation<.8f&&Vector3.Dot(a.route.Tangent(leaderAt),b.transform.forward)>.95f)return true;
            }
        }
        if(a.CurrentTurn!=0||b.CurrentTurn!=0)return false;if(a.route==b.route)return true;
        if(a.route.graphPath==null||b.route.graphPath==null||Vector3.Dot(a.transform.forward,b.transform.forward)<.95f)return false;
        float error;float at=a.route.ProjectNear(b.transform.position,a.distance,out error);return error<.8f&&Mathf.Abs(at-a.distance)<60;
    }
    bool Ahead(TrafficCar a,TrafficCar b){if(a.route==b.route)return b.distance>a.distance;float error;return a.route.ProjectNear(b.transform.position,a.distance,out error)>a.distance+.1f;}
    TrafficStop ActiveStop(TrafficCar car,TrafficJunction j){foreach(var s in car.route.stops)if(s.junction==j&&s.exitDistance>=car.distance-.1f)return s;return null;}
    bool Conflicts(TrafficCar a,TrafficStop sa,TrafficCar b){
        if(!b)return true;var sb=ActiveStop(b,sa.junction);if(sb==null)return true;if(SameTrafficStream(a,b))return false;
        Dictionary<TrafficStop,bool> row;if(!conflicts.TryGetValue(sa,out row)){row=new Dictionary<TrafficStop,bool>();conflicts[sa]=row;}bool value;if(row.TryGetValue(sb,out value))return value;
        value=false;float aEnd=Mathf.Min(a.route.length,sa.exitDistance+8),bEnd=Mathf.Min(b.route.length,sb.exitDistance+8);
        for(float x=Mathf.Max(0,sa.distance-3);x<aEnd&&!value;x+=2){var p=a.route.Position(x);var q=a.route.Position(Mathf.Min(aEnd,x+2));for(float y=Mathf.Max(0,sb.distance-3);y<=bEnd;y+=2){var r=b.route.Position(y);var delta=q-p;float t=Mathf.Clamp01(Vector3.Dot(r-p,delta)/Mathf.Max(.00001f,delta.sqrMagnitude));if((r-p-delta*t).sqrMagnitude<2.6f*2.6f){value=true;break;}}}
        row[sb]=value;Dictionary<TrafficStop,bool> reverse;if(!conflicts.TryGetValue(sb,out reverse)){reverse=new Dictionary<TrafficStop,bool>();conflicts[sb]=reverse;}reverse[sa]=value;return value;
    }
    public bool MovingLeader(TrafficCar car,TrafficCar other){
        // Detection alone does not mean a stopped queue: a whole convoy can see the
        // car ahead while maintaining its cruise speed. Strictly increasing route
        // distance makes this chain acyclic and rejects static/stopped obstructions.
        while(SameTrafficStream(car,other)&&other.enabled&&Ahead(car,other)&&other.speed>1){
            if(!other.waitingForObstacle)return true;
            car=other;other=other.obstacleCar;
        }
        return false;
    }
    public bool MayEnter(TrafficCar car,TrafficStop stop){
        var j=stop.junction;
        Request(car,stop);float mine=j.requests[car];
        if(!CanEnter(car,stop))return false;
        foreach(var reserved in j.reservations)if(reserved!=car&&Conflicts(car,stop,reserved))return false;
        if(!j.reservations.Contains(car))foreach(var pair in j.requests){var other=pair.Key;if(other==car||!other||!other.enabled||other.entryBlocked||!Conflicts(car,stop,other))continue;foreach(var approach in other.route.stops)if(approach.junction==j&&approach.distance-other.distance>=-.05f&&approach.distance-other.distance<=ReservationDistance(other)&&CanEnter(other,approach)&&ExitClear(other,approach)){if(pair.Value<mine||(pair.Value==mine&&other.GetInstanceID()<car.GetInstanceID()))return false;}}

        // A moving same-lane leader is protected by the car-following gap check. All other
        // occupied movements stay exclusive, including stopped leaders and conflicting turns.
        // Authored curves may extend outside the nominal junction rectangle.
        // Query the complete reserved corridor before applying the exact filter.
        var bounds=ReservedMovementBounds(car,stop);
        int contacts=overlaps.Box(bounds.center,bounds.extents,Quaternion.identity,(1<<8)|(1<<9));
        for(int contact=0;contact<contacts;contact++){var hit=overlaps.hits[contact];var other=hit.GetComponentInParent<TrafficCar>();if(other==car||MovingLeader(car,other))continue;
            // This box is only a broad phase. A red approach or a queued follower
            // can touch it without occupying this green movement's swept corridor.
            if(OccupiesMovement(car,stop,hit))return false;
        }
        if(!ExitClear(car,stop))return false;
        j.reservations.Add(car);if(!j.owner)j.owner=car;return true;
    }
    public Bounds ReservedMovementBounds(TrafficCar car,TrafficStop stop){
        MovementEnvelope cached;
        if(movementEnvelopes.TryGetValue(stop,out cached)&&cached.route==car.route&&cached.points==car.route.points&&cached.from==stop.distance&&cached.to==stop.exitDistance)return cached.bounds;
        // Curves and repaired connectors can extend outside the junction node's
        // nominal rectangle. The broad phase must contain the whole corridor
        // inspected by OccupiesMovement, including its endpoint margins.
        float begin=Mathf.Clamp(stop.distance-2.25f,0,car.route.length),end=Mathf.Clamp(stop.exitDistance+2.25f,begin,car.route.length);
        var bounds=new Bounds(car.route.Position(begin)+Vector3.up*.85f,Vector3.zero);
        for(float at=begin;at<=end+.5f;at+=.5f)bounds.Encapsulate(car.route.Position(Mathf.Min(at,end))+Vector3.up*.85f);
        // 2.25m covers the 1.15m narrow-phase radius plus any shifted sample
        // origin at the stop boundary. Narrow-phase occupancy stays unchanged.
        bounds.Expand(Vector3.one*4.5f);
        movementEnvelopes[stop]=new MovementEnvelope{route=car.route,points=car.route.points,from=stop.distance,to=stop.exitDistance,bounds=bounds};
        return bounds;
    }
    public float ReservedApproachGap(TrafficCar car,TrafficStop stop){
        float remaining=stop.distance-car.distance;
        if(remaining<-.05f||remaining>40)return float.PositiveInfinity;
        float gap=float.PositiveInfinity;
        foreach(var owner in stop.junction.reservations){
            if(!owner||owner==car||!Conflicts(car,stop,owner))continue;
            var crossing=ActiveStop(owner,stop.junction);if(crossing==null)continue;
            Dictionary<TrafficStop,float> row;if(!approachGates.TryGetValue(stop,out row)){row=new Dictionary<TrafficStop,float>();approachGates[stop]=row;}
            float gate;if(!row.TryGetValue(crossing,out gate)){
                gate=float.PositiveInfinity;
                float end=Mathf.Min(owner.route.length,crossing.exitDistance+2.25f);
                for(float at=Mathf.Max(0,stop.distance-40);at<=stop.distance+.5f;at+=.5f){
                    var position=car.route.Position(Mathf.Min(at,stop.distance));var forward=car.route.Tangent(Mathf.Min(at,stop.distance));var right=Right(forward);bool touches=false;
                    for(float otherAt=Mathf.Max(0,crossing.distance-2.25f);otherAt<=end+.5f;otherAt+=.5f){
                        var delta=owner.route.Position(Mathf.Min(otherAt,end))-position;
                        float x=Mathf.Max(0,Mathf.Abs(Vector3.Dot(delta,right))-.95f),z=Mathf.Max(0,Mathf.Abs(Vector3.Dot(delta,forward))-2.25f);
                        if(x*x+z*z<1.15f*1.15f){touches=true;break;}
                    }
                    if(touches){gate=Mathf.Max(0,at-.6f);break;}
                }
                row[crossing]=gate;
            }
            gap=Mathf.Min(gap,Mathf.Max(0,gate-car.distance));
        }
        return gap;
    }
    public bool OccupiesMovement(TrafficCar car,TrafficStop stop,Collider blocker){
        // Include the bumpers and a conservative body width. Reservations still
        // protect future crossing conflicts; this checks actual present occupancy.
        float end=Mathf.Min(car.route.length,stop.exitDistance+2.25f);
        for(float at=Mathf.Max(car.distance,stop.distance)-2.25f;at<=end+.5f;at+=.5f){
            var point=car.route.Position(Mathf.Clamp(at,0,end))+Vector3.up*.85f;
            if((blocker.ClosestPoint(point)-point).sqrMagnitude<1.15f*1.15f)return true;
        }
        return false;
    }
    public bool ExitClear(TrafficCar car,TrafficStop stop){var exit=car.route.Position(stop.exitDistance);var dir=car.route.Tangent(stop.exitDistance);
        int contacts=overlaps.Box(exit+Vector3.up*.85f,new Vector3(1.05f,.55f,4.5f),Quaternion.LookRotation(dir),(1<<8)|(1<<9)|1);
        for(int contact=0;contact<contacts;contact++){var other=overlaps.hits[contact].GetComponentInParent<TrafficCar>();if(other!=car&&!MovingLeader(car,other))return false;}return true;
    }
    public void Release(TrafficCar car,TrafficJunction j,bool clearRequest=true){j.reservations.Remove(car);if(clearRequest)j.requests.Remove(car);if(j.owner==car){j.owner=null;foreach(var reserved in j.reservations){j.owner=reserved;break;}}}
    public void Release(TrafficCar car){foreach(var j in junctions)Release(car,j);}
    public void CancelApproach(TrafficCar car,TrafficStop stop){
        // Reservations can have been acquired at a larger braking distance before
        // the car slowed down. Distance is not evidence that no reservation exists.
        foreach(var active in car.route.stops)if(active.junction==stop.junction&&car.distance>active.distance+.05f&&car.distance<active.exitDistance)return;
        Release(car,stop.junction,false);
    }
    TrafficRoute MakeTurn(TrafficJunction j,int sign,int turn){
        var incoming=AxisDirection(j,0)*sign;var outgoing=Right(incoming)*turn;var route=new TrafficRoute{axis=0,turn=turn};
        float inLane=j.centeredLane[0]?0:j.halfWidth[0]/j.laneCount[0]*(turn>0?j.laneCount[0]-.5f:.5f),outLane=j.centeredLane[1]?0:j.halfWidth[1]/j.laneCount[1]*(turn>0?j.laneCount[1]-.5f:.5f);
        var begin=j.center-incoming*40+Right(incoming)*inLane;var entry=j.center-incoming*(j.halfWidth[1]+3)+Right(incoming)*inLane;
        var exit=j.center+outgoing*(j.halfWidth[0]+3)+Right(outgoing)*outLane;var finish=exit+outgoing*50;
        var p=new List<Vector3>{begin,entry};float handle=Vector3.Distance(entry,exit)*.55f;var c1=entry+incoming*handle;var c2=exit-outgoing*handle;
        for(int i=1;i<=24;i++){float t=i/24f,u=1-t;p.Add(u*u*u*entry+3*u*u*t*c1+3*u*t*t*c2+t*t*t*exit);}p.Add(finish);route.SetPoints(p.ToArray());
        route.stops.Add(new TrafficStop{junction=j,axis=0,distance=40-j.StopDistance(0)-2.1f,exitDistance=route.length-50+5});return route;
    }
    public static Vector3 Right(Vector3 dir) { return new Vector3(dir.z,0,-dir.x); }
    static Vector3 Edge(Vector3 center, Vector3 dir, MapData map) {
        float distance=1000;
        if(dir.x>.001f) distance=Mathf.Min(distance,(map.width-7-center.x)/dir.x);
        if(dir.x<-.001f) distance=Mathf.Min(distance,(7-center.x)/dir.x);
        if(dir.z>.001f) distance=Mathf.Min(distance,(map.depth-7-center.z)/dir.z);
        if(dir.z<-.001f) distance=Mathf.Min(distance,(7-center.z)/dir.z);
        return center+dir*distance;
    }
    TrafficRoute MakeRoute(Vector3 a, Vector3 b, int axis, MapData map) {
        var direction=(b-a).normalized;var near=junctions[0];foreach(var j in junctions)if(Vector3.Distance(a,j.center)<Vector3.Distance(a,near.center))near=j;
        axis=Mathf.Abs(Vector3.Dot(direction,AxisDirection(near,0)))>.7f?0:1;
        var lane=Right(direction)*(near.centeredLane[axis]?0:near.halfWidth[axis]/near.laneCount[axis]*.5f);
        var route=new TrafficRoute{start=a+lane,end=b+lane,direction=direction,length=Vector3.Distance(a,b),axis=axis};
        foreach(var j in junctions) {
            float along=Vector3.Dot(j.center-a,direction);
            float sideways=Vector3.Cross(j.center-a,direction).magnitude;
            int stopAxis=Mathf.Abs(Vector3.Dot(direction,AxisDirection(j,0)))>.7f?0:1;
            if(along>0 && along<route.length && sideways<2) route.stops.Add(new TrafficStop{junction=j,axis=stopAxis,distance=along-j.StopDistance(stopAxis)-2.1f,exitDistance=along+j.halfWidth[1-stopAxis]+5});
        }
        return route;
    }
    // Verify sampled approaches/exits against the nearest aligned reviewed road. Curved
    // junction connectors may cross other road centrelines, so check them outside the junction.
    public bool AllowsRoute(TrafficRoute route,MapData map){
        if(map==null)return true;
        if(map.hybrid)foreach(var stop in route.stops){var rule=System.Array.Find(map.junctionRules,r=>r.id==stop.junction.name);if(rule!=null&&(route.turn<0&&!rule.allowLeft||route.turn>0&&!rule.allowRight||route.turn==0&&!rule.allowStraight))return false;}
        if(map.mapId=="eungye"){
            if(!RouteOnMappedRoad(route,map)||!RouteClearOfStaticObstacles(route))return false;
            foreach(var stop in route.stops)if(stop.distance<0||stop.exitDistance>route.length)return false;
        }
        for(float at=0;at<=route.length;at+=2){var point=route.Position(at);bool inside=false;
            foreach(var j in junctions)if(Vector3.Distance(point,j.center)<Mathf.Max(j.halfWidth[0],j.halfWidth[1])+5){inside=true;break;}if(inside)continue;
            var heading=route.Tangent(at);float closest=float.PositiveInfinity;bool allowed=true;
            foreach(var road in map.roads)for(int i=1;i<road.points.Length;i++){var start=road.points[i-1].V();var segment=road.points[i].V()-start;float length=segment.magnitude;if(length<.01f)continue;
                var direction=segment/length;float facing=Vector3.Dot(heading,direction);if(Mathf.Abs(facing)<.85f)continue;float along=Vector3.Dot(point-start,direction);if(along<0||along>length)continue;
                float offset=Vector3.Distance(point,start+direction*along);if(offset>road.width*.5f+1||offset>closest+.01f)continue;
                bool candidate=road.oneWay==0||facing*road.oneWay>0;
                if(offset<closest-.01f){closest=offset;allowed=candidate;}else allowed|=candidate;
            }
            if(!allowed)return false;
        }
        return true;
    }
    // Imported roads are not an orthogonal grid. Stop extending a simple corridor at
    // the first uncovered footprint, and suppress turns whose full path leaves it.
    Vector3 MappedCorridorEnd(TrafficJunction junction,Vector3 direction,float limit,int axis,MapData map){
        float offset=junction.centeredLane[axis]?0:junction.halfWidth[axis]/junction.laneCount[axis]*.5f;
        var lateral=Right(direction)*offset;float distance=0;
        for(float at=1;at<=limit;at+=1){var p=junction.center+direction*at;if(!MappedFootprint(p+lateral,direction,map)||!MappedFootprint(p-lateral,direction,map))break;distance=at;}
        return junction.center+direction*distance;
    }
    static bool MappedPoint(Vector3 point,MapData map,float margin){
        foreach(var road in map.roads){float width=road.width*.5f-margin;if(width<0)continue;for(int i=1;i<road.points.Length;i++){
            var start=road.points[i-1].V();var delta=road.points[i].V()-start;if(delta.sqrMagnitude<.0001f)continue;
            float t=Mathf.Clamp01(Vector3.Dot(point-start,delta)/delta.sqrMagnitude);
            if((point-start-delta*t).sqrMagnitude<=width*width)return true;
        }}return false;
    }
    static bool MappedFootprint(Vector3 point,Vector3 heading,MapData map){
        if(!MappedPoint(point,map,1.2f))return false;
        var along=heading*2.25f;var side=Right(heading)*.95f;
        return MappedPoint(point+along+side,map,0)&&MappedPoint(point+along-side,map,0)&&MappedPoint(point-along+side,map,0)&&MappedPoint(point-along-side,map,0);
    }
    public bool RouteOnMappedRoad(TrafficRoute route,MapData map){
        if(float.IsNaN(route.length)||float.IsInfinity(route.length)||route.length<5)return false;
        for(float at=0;at<route.length;at+=1)if(!MappedFootprint(route.Position(at),route.Tangent(at),map))return false;
        return MappedFootprint(route.Position(route.length),route.Tangent(route.length),map);
    }
    public bool RouteClearOfStaticObstacles(TrafficRoute route){
        // A source road envelope can overlap a source building. Validate the actual
        // vehicle clearance against the built scene, above road and kerb surfaces.
        var half=new Vector3(1.05f,.55f,2.3f);
        for(float at=0;at<route.length+1;at+=1){float distance=Mathf.Min(at,route.length);
            if(Physics.CheckBox(route.Position(distance)+Vector3.up*.85f,half,Quaternion.LookRotation(route.Tangent(distance)),1,QueryTriggerInteraction.Ignore))return false;
        }
        return true;
    }
    public TrafficCar Spawn(TrafficRoute route,float distance,int index) {
        if(route.graphPath!=null?!RouteClearOfStaticObstacles(route):!AllowsRoute(route,roadMap))return null;
        var car=new GameObject("Traffic car " + index).AddComponent<TrafficCar>(); car.transform.SetParent(transform);
        car.Initialize(this,route,distance,index); cars.Add(car);return car;
    }
    void PaintRoads(MapData map) {
        RoadPaintGeometry.Paint(map,junctions,(name,p,dir,width,length,isYellow)=>Mark(name,p,dir,width,length,isYellow?yellow:white));
        ReviewedLaneLayout.Paint(map,(name,p,dir,width,length,isYellow)=>Mark(name,p,dir,width,length,isYellow?yellow:white));
        UturnRoadMarkings.Paint(map,(name,p,dir,width,length,isYellow)=>Mark(name,p,dir,width,length,isYellow?yellow:white));
    }
    void Mark(string name,Vector3 p,Vector3 dir,float width,float length,Material material) {
        p.y=.071f;var right=Right(dir)*width*.5f;var along=dir*length*.5f;var list=material==yellow?yellowPaint:whitePaint;list.AddRange(new[]{p-right-along,p-right+along,p+right+along,p+right-along});
    }
    void PaintMesh(List<Vector3> vertices,Material material){var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;var triangles=new List<int>();for(int i=0;i<vertices.Count;i+=4)triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();var obj=new GameObject(material.name);obj.transform.SetParent(transform);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;}
    public bool HasLeftMovement(TrafficJunction j,int axis,int sign) {
        if(j==null)return false;
        for(int i=0;i<j.leftMovements.Count;i++)if(j.lampAxes[i]==axis&&j.lampSigns[i]==sign)return j.leftMovements[i];
        if(j.facility!=null&&!j.facility.allowLeft)return false;
        bool known=false;var edges=roadMap==null?null:roadMap.hdEdges;
        if(edges!=null)foreach(var incoming in edges) {
            if(incoming.ruleId!=j.name||incoming.turn!=0||incoming.change!=0||incoming.points==null||incoming.points.Length<2)continue;
            var heading=(incoming.points[incoming.points.Length-1].V()-incoming.points[incoming.points.Length-2].V()).normalized;
            int incomingAxis=Mathf.Abs(Vector3.Dot(heading,AxisDirection(j,0)))>.7f?0:1;
            if(incomingAxis!=axis||Sign(heading,incomingAxis,j)!=sign)continue;
            // A closed authored lane is still evidence for this exact approach.
            known=true;if(incoming.disabled||incoming.next==null)continue;
            foreach(int next in incoming.next) {
                if(next<0||next>=edges.Length)continue;var left=edges[next];
                if(left.disabled||left.ruleId!=j.name||left.turn!=-1||left.next==null)continue;
                foreach(int exit in left.next)if(exit>=0&&exit<edges.Length&&!edges[exit].disabled)return true;
            }
        }
        // Missing approach data does not establish a new prohibition.
        return !known;
    }
    void AddApproach(TrafficJunction j,Vector3 direction,float halfWidth,float conflictHalfWidth) {
        var right=Right(direction);
        int editAxis=Mathf.Abs(Vector3.Dot(direction,AxisDirection(j,0)))>.7f?0:1;int editSign=Sign(direction,editAxis,j);bool editedStop=roadMap.editorDocument!=null&&System.Array.Exists(roadMap.editorDocument.facilities,f=>f.junctionId==j.name&&f.kind=="stopLine"&&f.axis==editAxis&&f.sign==editSign);bool editedCross=roadMap.editorDocument!=null&&System.Array.Exists(roadMap.editorDocument.facilities,f=>f.junctionId==j.name&&f.kind=="crosswalk"&&f.axis==editAxis&&f.sign==editSign);float stop=j.StopDistance(editAxis,editSign);
        if(!editedCross&&(j.facility==null||j.facility.crosswalk)){j.crosswalkApproaches++;for(float x=-halfWidth+.65f;x<halfWidth-.3f;x+=.95f) Mark("Crosswalk "+j.name,j.center-direction*(conflictHalfWidth+TrafficJunction.CrosswalkCentreOffset)+right*x,direction,.5f,TrafficJunction.CrosswalkDepth,white);}
        if(!editedStop&&(j.facility==null||j.facility.stopLine)){j.stopLineApproaches++;Mark("Stop line "+j.name,j.center-direction*stop+right*halfWidth*.5f,direction,halfWidth-.3f,.4f,white);}
        if(!j.signalized||j.editorProgram!=null)return;
        if(j.facility==null||j.facility.crosswalk)PedestrianSignalDisplay.Create(this,roadMap,j,direction,halfWidth,conflictHalfWidth,transform);
        // Far-side placement keeps the signal visible from a stopped driver's eye position.
        var root=new GameObject(j.name+" signal approach"); root.transform.SetParent(transform); root.transform.position=j.center+direction*stop+right*(halfWidth+.6f); root.transform.rotation=Quaternion.LookRotation(direction);
        var support=roadMap.hybrid?root.transform.InverseTransformPoint(RoadsidePlacement.FindPole(roadMap,root.transform.position)):Vector3.zero;
        WorldBuilder.Box("Pole",support+new Vector3(0,2.5f,0),new Vector3(.15f,5,.15f),pole,root.transform,false);
        float reach=halfWidth-2.5f+.6f;
        var head=new Vector3(-reach,0,0);var span=head-support;
        var arm=WorldBuilder.Box("Signal arm",(head+support)*.5f+Vector3.up*4.9f,new Vector3(.12f,.12f,span.magnitude),pole,root.transform,false);arm.transform.localRotation=Quaternion.LookRotation(span);
        bool permissive=j.leftTurnMode==LeftTurnMode.Permissive,hasLeft=HasLeftMovement(j,editAxis,editSign),threeLamps=permissive||!hasLeft;
        WorldBuilder.Box("Signal housing",new Vector3(-reach,4.55f,0),new Vector3(threeLamps?2.25f:2.9f,.85f,.35f),dark,root.transform,false);
        var lamps=new Renderer[3];
        for(int i=0;i<3;i++) {
            var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.name="Signal lamp "+(SignalColor)i; ball.transform.SetParent(root.transform,false);ball.transform.localPosition=new Vector3(-reach+(threeLamps?(i-1)*.6f:i==0?-.9f:i==1?-.3f:.9f),4.55f,-.22f);ball.transform.localScale=new Vector3(.56f,.56f,.18f); if(Application.isPlaying)Destroy(ball.GetComponent<Collider>());else DestroyImmediate(ball.GetComponent<Collider>());lamps[i]=ball.GetComponent<Renderer>();
        }
        j.lamps.Add(lamps);j.lampStates.Add(-1);j.lampAxes.Add(editAxis);j.lampSigns.Add(editSign);j.leftMovements.Add(hasLeft);
        if(j.facility!=null){var label=new GameObject("Facility provenance "+j.name);label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(-reach,2.8f,-.1f);var text=label.AddComponent<TextMesh>();text.text=j.facility.facilityStatus=="verified"?"현장 확인 시설":"임시 교통시설";text.fontSize=40;text.characterSize=.065f;text.anchor=TextAnchor.MiddleCenter;text.color=j.facility.facilityStatus=="verified"?Color.green:new Color(1,.75f,.2f);KoreanText.Apply(text);label.AddComponent<RoadSignLabel>();}

        if(threeLamps){
            if(permissive&&hasLeft)RoadSignage.CreatePermissive(root.transform,new Vector3(-reach,5.65f,0));
            j.leftLamps.Add(new Renderer[0]);return;
        }
        var arrow=new List<Renderer>();var origin=new Vector3(-reach+.3f,4.55f,-.24f);
        arrow.Add(WorldBuilder.Box("좌회전 화살표",origin,new Vector3(.36f,.075f,.04f),unlit[2],root.transform,false).GetComponent<Renderer>());
        foreach(int side in new[]{-1,1}){var tip=WorldBuilder.Box("좌회전 화살촉",origin+new Vector3(-.1f,side*.065f,0),new Vector3(.2f,.075f,.04f),unlit[2],root.transform,false);tip.transform.localRotation=Quaternion.Euler(0,0,side*45);arrow.Add(tip.GetComponent<Renderer>());}j.leftLamps.Add(arrow.ToArray());
    }
    void Update() {
        foreach(var j in junctions) for(int approach=0;approach<j.lamps.Count;approach++) {
            var left=LeftSignal(j,j.lampAxes[approach],j.lampSigns[approach]);int color=(int)Signal(j,j.lampAxes[approach],j.lampSigns[approach]);
            int state=color+((!j.scenarioFlashing||j.scenarioLampOn)?4:0)+(left==SignalColor.Green?8:0);if(j.lampStates[approach]==state)continue;j.lampStates[approach]=state;
            foreach(var arrow in j.leftLamps[approach])arrow.sharedMaterial=left==SignalColor.Green?lit[2]:unlit[2];
            for(int i=0;i<3;i++) j.lamps[approach][i].sharedMaterial=i==color&&(!j.scenarioFlashing||j.scenarioLampOn)?lit[i]:unlit[i];
        }
    }
}
