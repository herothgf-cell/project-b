using System.Collections.Generic;
using UnityEngine;

public class RoadNavigation : MonoBehaviour {
 public HdRoadGraph hd;
 public HdRoadGraph.RoutePath hdRoute;
 HdRoadGraph.RoutePath viaObservedRoute;Vector3 viaPreviousPosition;float viaPreviousRemaining,viaSampleTime;bool viaSampled;
 HdRoadGraph.RoutePath projectedRoute;Vector3 projectedPosition;float projectedAt;
 public float ProjectVehicle(out float error){
  if(hdRoute==null){error=float.MaxValue;return 0;}
  var p=vehicle.position;var body=vehicle.GetComponent<Rigidbody>();float reach=Mathf.Max(8,body?body.velocity.magnitude*.2f+2:8);
  float at;
  if(projectedRoute==hdRoute&&Vector3.Distance(projectedPosition,p)<reach)at=hdRoute.path.ProjectRange(p,Mathf.Max(0,projectedAt-2),Mathf.Min(hdRoute.path.length,projectedAt+reach),out error);
  else at=hdRoute.path.ProjectRange(p,0,Mathf.Min(hdRoute.path.length,reach),out error);
  if(error>4)at=hdRoute.path.Project(p,out error);
  projectedRoute=hdRoute;projectedPosition=p;projectedAt=at;return at;
 }
 // Observation never changes the destination, route costs, or required waypoints.
 public int ObservedEdge {get;private set;}=-1;
 public int PassedObservedEdge {get;private set;}=-1;
 public void ObserveEdge(int edge){ObservedEdge=edge;PassedObservedEdge=-1;viaSampled=false;}
 public int PassedRequiredVia {get;private set;}=-1;
 public int RequiredVia {get;private set;}=-1;
 public void RequireVia(int edge){viaSampled=false;PassedRequiredVia=-1;RequiredVia=edge;hdRoute=null;if(active)Refresh();}
 int avoidEdge=-1;
 public int AvoidEdge {get{return avoidEdge;}set{if(avoidEdge==value)return;avoidEdge=value;hdRoute=null;}}
 float[] WithAvoidance(float[] weights){if(hd==null||avoidEdge<0||avoidEdge>=hd.edges.Length)return weights;var result=weights==null?new float[hd.edges.Length]:(float[])weights.Clone();if(weights==null)for(int i=0;i<result.Length;i++)result[i]=hd.paths[i].length;result[avoidEdge]=float.PositiveInfinity;return result;}
 HdRoadGraph.RoutePath GoalRoute(int start,float at,float[] weights){weights=WithAvoidance(weights);var route=RequiredVia>=0?hd.Via(start,at,RequiredVia,hdGoal,hdGoalAt,weights):hd.Route(start,at,hdGoal,hdGoalAt,weights);return route!=null&&avoidEdge>=0&&route.edges.Contains(avoidEdge)?null:route;}
 public int hdGoal=-1; public float hdGoalAt; public bool freeGuidance; bool hybrid; float nextHdCosts; float[] hdSeconds;
 public readonly List<Vector3> path=new List<Vector3>(),alternativePath=new List<Vector3>();
 public float remaining,estimatedSeconds,alternativeSeconds,alternativeDistance;
 public string instruction="호출을 선택하거나 길가 손님을 찾아보세요",rerouteNotice="";
 public Transform vehicle;public Vector3 destination;public bool active;
 public RoadConditions conditions;
 public bool PreferFastest=true;
 public bool PreferLargeRoad {get;private set;}
 float[] passengerRoadCosts,passengerRoadWidths;
 public string PreferenceLabel {get{return PreferLargeRoad?"큰길 우선":PreferFastest?"빠른 길":"짧은 길";}}
 public int recalculationCount {get;private set;}
 class Node {public Vector3 p;public readonly List<int> outgoing=new List<int>();}
 class Edge {public int from,to,reverse=-1,lanes;public float length,offset,innerOffset,width;public Vector3 direction;}
 class RouteResult {public readonly List<int> chain=new List<int>();public float progress,seconds,distance;public List<Vector3> points=new List<Vector3>();}
 struct QueueItem {public float cost;public int edge,serial;}
 class QueueOrder : IComparer<QueueItem> {public int Compare(QueueItem a,QueueItem b){int c=a.cost.CompareTo(b.cost);return c!=0?c:a.serial.CompareTo(b.serial);}}
 readonly List<Node> nodes=new List<Node>();readonly List<Edge> edges=new List<Edge>();readonly Dictionary<string,int> ids=new Dictionary<string,int>();
 LineRenderer line;float nextUpdate,lastRouteChange=-100,noticeUntil;MapStop goal;RouteResult selected;float[] delays;
 int NodeFor(Vector3 p){string key=p.x.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+","+p.z.ToString("F2",System.Globalization.CultureInfo.InvariantCulture);int id;if(ids.TryGetValue(key,out id))return id;id=nodes.Count;ids[key]=id;nodes.Add(new Node{p=p});return id;}
 int AddEdge(int a,int b,float width,float offset,int lanes){int index=edges.Count;edges.Add(new Edge{from=a,to=b,length=Vector3.Distance(nodes[a].p,nodes[b].p),offset=offset,innerOffset=lanes==1?0:width/lanes*.5f,lanes=Mathf.Max(1,lanes/2),width=width,direction=(nodes[b].p-nodes[a].p).normalized});nodes[a].outgoing.Add(index);return index;}
 public void Build(MapData map,Transform taxi){
  vehicle=taxi;nodes.Clear();edges.Clear();ids.Clear();selected=null;path.Clear();alternativePath.Clear();
  hd=map.HasHd?new HdRoadGraph(map):null;hybrid=map.hybrid;hdSeconds=hd==null?null:new float[hd.edges.Length];
  if(hd==null)foreach(var road in map.roads)for(int i=1;i<road.points.Length;i++){
   int a=NodeFor(road.points[i-1].V()),b=NodeFor(road.points[i].V());if(a==b)continue;
   float offset=road.width*.5f-road.width/Mathf.Max(1,road.DisplayLanes)*.5f;int forward=-1,backward=-1;
   if(road.oneWay>=0)forward=AddEdge(a,b,road.width,offset,road.DisplayLanes);
   if(road.oneWay<=0)backward=AddEdge(b,a,road.width,offset,road.DisplayLanes);
   if(forward>=0&&backward>=0){edges[forward].reverse=backward;edges[backward].reverse=forward;}
  }
  delays=new float[edges.Count];
  if(hd!=null){passengerRoadWidths=new float[hd.edges.Length];passengerRoadCosts=new float[hd.edges.Length];
   for(int i=0;i<hd.edges.Length;i++){Vector3 mid=hd.paths[i].Position(hd.paths[i].length*.5f);float best=float.MaxValue,width=0;
    foreach(var road in map.roads){if(road.suppressPaint)continue;for(int k=1;k<road.points.Length;k++){Vector3 a=road.points[k-1].V(),d=road.points[k].V()-a;float t=Mathf.Clamp01(Vector3.Dot(mid-a,d)/Mathf.Max(.001f,d.sqrMagnitude));float dist=(mid-a-d*t).sqrMagnitude;if(dist<best){best=dist;width=road.width;}}}
    passengerRoadWidths[i]=width;passengerRoadCosts[i]=hd.paths[i].length*(width>=12?1:4);
   }
  }
  if(line==null){line=new GameObject("우측 차로 안내 경로").AddComponent<LineRenderer>();line.transform.SetParent(transform);line.startWidth=line.endWidth=.25f;line.material=WorldBuilder.Mat("Navigation cyan",Color.cyan);line.material.EnableKeyword("_EMISSION");line.material.SetColor("_EmissionColor",Color.cyan);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
  line.positionCount=0;
 }
 int NearestNode(Vector3 p){int best=-1;float dist=float.MaxValue;p.y=0;for(int i=0;i<nodes.Count;i++){float d=(nodes[i].p-p).sqrMagnitude;if(d<dist){dist=d;best=i;}}return best;}
 int InitialEdge(Vector3 start,Vector3 heading,out float progress){
  start.y=0;heading.y=0;heading.Normalize();int initial=-1;float best=float.MaxValue;progress=0;
  for(int i=0;i<edges.Count;i++){
   var e=edges[i];float along=Mathf.Clamp(Vector3.Dot(start-nodes[e.from].p,e.direction),0,e.length);
   var lane=nodes[e.from].p+e.direction*along+TrafficSystem.Right(e.direction)*e.offset;
   // Heading outweighs the adjacent opposing lane, preventing mid-block reversals.
   float cost=(lane-start).sqrMagnitude+(heading.sqrMagnitude>.1f?(1-Vector3.Dot(heading,e.direction))*120:0);
   if(cost<best){best=cost;initial=i;progress=along;}
  }
  if(initial>=0&&heading.sqrMagnitude>.1f&&Vector3.Dot(heading,edges[initial].direction)<-.25f)return -1;
  return initial;
 }
 static float CruiseSpeed(Edge edge){return edge.width>=12?12.5f:9f;}
 float TurnSeconds(int a,int b){float angle=Vector3.Angle(edges[a].direction,edges[b].direction);return angle>150?10:angle/90f*2.5f;}
 void ObserveConditions(){for(int i=0;i<edges.Count;i++){var e=edges[i];delays[i]=conditions==null?0:Mathf.Max(0,conditions.ExtraSeconds(nodes[e.from].p,nodes[e.to].p,e.direction,e.width));}}
 float EdgeSeconds(int index,float progress,bool live){var e=edges[index];float delay=0;
  if(live&&conditions!=null)delay=progress>.01f?Mathf.Max(0,conditions.ExtraSeconds(nodes[e.from].p+e.direction*progress,nodes[e.to].p,e.direction,e.width)):delays[index];
  return (e.length-progress)/CruiseSpeed(e)+delay;
 }
 RouteResult Route(Vector3 start,Vector3 heading,Vector3 end,Vector3 arrival,bool requireArrival,bool fastest,bool live){
  float progress;int initial=InitialEdge(start,heading,out progress),target=NearestNode(end),n=edges.Count;
  if(initial<0||target<0)return null;
  var costs=new float[n];var previous=new int[n];for(int i=0;i<n;i++){costs[i]=float.MaxValue;previous[i]=-1;}
  costs[initial]=fastest?EdgeSeconds(initial,progress,live):edges[initial].length-progress;
  var queue=new SortedSet<QueueItem>(new QueueOrder());int serial=0;queue.Add(new QueueItem{edge=initial,cost=costs[initial],serial=serial++});int found=-1;
  while(queue.Count>0){
   var item=queue.Min;queue.Remove(item);int cur=item.edge;if(item.cost>costs[cur])continue;var e=edges[cur];
   if(e.to==target&&(!requireArrival||Vector3.Dot(e.direction,arrival)>.85f)){found=cur;break;}
   foreach(int next in nodes[e.to].outgoing){
    // A one-way has no reverse edge. Only an actual two-way dead end permits reversal.
    if(Vector3.Dot(e.direction,edges[next].direction)<-.85f&&(next!=e.reverse||nodes[e.to].outgoing.Count!=1))continue;
    float candidate=item.cost+(fastest?EdgeSeconds(next,0,live)+TurnSeconds(cur,next):edges[next].length);
    if(candidate>=costs[next])continue;costs[next]=candidate;previous[next]=cur;queue.Add(new QueueItem{edge=next,cost=candidate,serial=serial++});
   }
  }
  if(found<0)return null;
  var result=new RouteResult{progress=progress};for(int at=found;at>=0;at=previous[at])result.chain.Add(at);result.chain.Reverse();Complete(result,live);return result;
 }
 float GuideOffset(RouteResult route,int at){
  var edge=edges[route.chain[at]];
  // Match assisted driving's provisional turn lanes: inner left, outer right.
  // Look through digitized straight segments so lane preparation precedes the junction.
  for(int i=at;i+1<route.chain.Count;i++){
   float angle=Vector3.SignedAngle(edges[route.chain[i]].direction,edges[route.chain[i+1]].direction,Vector3.up);
   if(Mathf.Abs(angle)>=35)return angle<0?edge.innerOffset:edge.offset;
  }
  return edge.offset;
 }
 void Complete(RouteResult route,bool live){
  route.points.Clear();route.seconds=0;var first=edges[route.chain[0]];
  route.points.Add(nodes[first.from].p+first.direction*route.progress+TrafficSystem.Right(first.direction)*GuideOffset(route,0));
  for(int i=0;i<route.chain.Count;i++){
   int index=route.chain[i];var e=edges[index];route.seconds+=EdgeSeconds(index,i==0?route.progress:0,live);if(i>0)route.seconds+=TurnSeconds(route.chain[i-1],index);
   float offset=GuideOffset(route,i);var p=nodes[e.to].p+TrafficSystem.Right(e.direction)*offset;
   if(i+1<route.chain.Count){var next=edges[route.chain[i+1]];
    float before=Mathf.Min(18,e.length*.35f);
    if(Vector3.Angle(e.direction,next.direction)>=35&&(i>0||e.length-route.progress>before))route.points.Add(p-e.direction*before);
    p=nodes[e.to].p+(TrafficSystem.Right(e.direction)*offset+TrafficSystem.Right(next.direction)*GuideOffset(route,i+1))*.5f;
   }
   route.points.Add(p);
  }
  route.distance=Length(route.points);
 }
 public List<Vector3> Find(Vector3 a,Vector3 b){if(hd!=null){float da,db;int ia=hd.Nearest(a,Vector3.zero,out da),ib=hd.Nearest(b,Vector3.zero,out db);var r=hd.Route(ia,da,ib,db);return r==null?new List<Vector3>():new List<Vector3>(r.path.points);}var route=Route(a,Vector3.zero,b,Vector3.zero,false,false,false);return route==null?new List<Vector3>():route.points;}
 public List<Vector3> Find(MapStop a,MapStop b){if(hd!=null&&a.graphStop&&b.graphStop){var r=hd.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);return r==null?new List<Vector3>():new List<Vector3>(r.path.points);}var route=Route(a.Kerb-a.Direction*3,a.Direction,b.V(),b.Direction,true,false,false);var result=route==null?new List<Vector3>():route.points;if(result.Count>0)FinishApproach(result,b);return result;}
 static void FinishApproach(List<Vector3> route,MapStop stop){if(route.Count>1)route.RemoveAt(route.Count-1);route.Add(stop.Kerb-stop.Direction*3);route.Add(stop.Kerb);}
 public float Distance(Vector3 a,Vector3 b){return Length(Find(a,b));}
 public float Distance(MapStop a,MapStop b){return Length(Find(a,b));}
 static float Length(List<Vector3> p){if(p.Count==0)return float.PositiveInfinity;float d=0;for(int i=1;i<p.Count;i++)d+=Vector3.Distance(p[i-1],p[i]);return d;}
 public void SetDestination(MapStop stop){avoidEdge=-1;if(hd!=null&&stop.graphStop){SetHdDestination(stop.laneEdge,stop.laneAt);goal=stop;return;}goal=stop;destination=stop.Kerb;active=true;selected=null;Refresh();}
 public void SetDestination(Vector3 point){avoidEdge=-1;if(hd!=null){float at;int id=hd.Nearest(point,Vector3.zero,out at);SetHdDestination(id,at);return;}goal=null;destination=point;active=true;selected=null;Refresh();}
 public void SetHdDestination(int edge,float at){avoidEdge=-1;PassedRequiredVia=RequiredVia=-1;freeGuidance=false;hdGoal=edge;hdGoalAt=at;hdRoute=null;goal=null;active=true;if(edge>=0)destination=hd.paths[edge].Position(at);Refresh();}
 public bool StartFreeGuidance(){avoidEdge=-1;if(!hybrid||hd==null||vehicle==null)return false;float at;int start=hd.Nearest(vehicle.position,vehicle.forward,out at);var route=hd.Forward(start,at);if(route==null)return false;freeGuidance=true;goal=null;active=true;UseFreeRoute(route);RefreshHd();return true;}
 void UseFreeRoute(HdRoadGraph.RoutePath route){hdRoute=route;hdGoal=route.edges[route.edges.Count-1];hdGoalAt=hd.paths[hdGoal].length;destination=route.path.Position(route.path.length);}
 public void TogglePreference(){PreferLargeRoad=false;PreferFastest=!PreferFastest;selected=null;if(hd!=null)hdRoute=null;rerouteNotice=PreferenceLabel+" 경로 선택";noticeUntil=Time.time+5;if(active)Refresh();}
 public void Clear(){avoidEdge=-1;PreferLargeRoad=false;PassedRequiredVia=RequiredVia=-1;freeGuidance=false;active=false;selected=null;hdRoute=null;path.Clear();alternativePath.Clear();if(line!=null)line.positionCount=0;remaining=estimatedSeconds=alternativeSeconds=alternativeDistance=0;rerouteNotice="";instruction="호출을 선택하거나 길가 손님을 찾아보세요";}
 void Update(){if(active&&Time.time>=nextUpdate){using(TrafficPerformanceProbe.Measure("Navigation.Refresh"))Refresh();nextUpdate=Time.time+1;}}
 static bool SameRoute(RouteResult a,RouteResult b){if(a==null||b==null||a.chain.Count!=b.chain.Count)return false;for(int i=0;i<a.chain.Count;i++)if(a.chain[i]!=b.chain[i])return false;return true;}
 RouteResult RemainingSelection(Vector3 start,Vector3 heading){
  if(selected==null)return null;float progress;int initial=InitialEdge(start,heading,out progress),at=selected.chain.IndexOf(initial);if(at<0)return null;
  var result=new RouteResult{progress=progress};for(int i=at;i<selected.chain.Count;i++)result.chain.Add(selected.chain[i]);Complete(result,true);return result;
 }
 void Approach(RouteResult route){if(route==null||goal==null)return;float before=route.distance;FinishApproach(route.points,goal);route.distance=Length(route.points);route.seconds=Mathf.Max(0,route.seconds+(route.distance-before)/CruiseSpeed(edges[route.chain[route.chain.Count-1]]));}
 public void Refresh(){
  if(!active||vehicle==null)return;
  if(hd!=null){RefreshHd();return;}
  if(Time.time>noticeUntil)rerouteNotice="";
  path.Clear();alternativePath.Clear();remaining=estimatedSeconds=alternativeSeconds=alternativeDistance=0;
  bool nearby=goal!=null&&Vector3.Dot(vehicle.forward,goal.Direction)>.85f&&Mathf.Abs(Vector3.Dot(vehicle.position-goal.Kerb,TrafficSystem.Right(goal.Direction)))<2.5f&&Vector3.Dot(vehicle.position-goal.Kerb,goal.Direction)>-10&&Vector3.Dot(vehicle.position-goal.Kerb,goal.Direction)<4.5f;
  if(nearby){var p=vehicle.position;p.y=0;path.Add(p);path.Add(goal.Kerb);selected=null;remaining=Length(path);estimatedSeconds=remaining/3;}
  else {
   ObserveConditions();Vector3 end=goal!=null?goal.V():destination,arrival=goal!=null?goal.Direction:Vector3.zero;
   var fastest=Route(vehicle.position,vehicle.forward,end,arrival,goal!=null,true,true);
   var shortest=Route(vehicle.position,vehicle.forward,end,arrival,goal!=null,false,true);
   var candidate=PreferFastest?fastest:shortest;var alternate=PreferFastest?shortest:fastest;
   var incumbent=RemainingSelection(vehicle.position,vehicle.forward);
   if(candidate!=null&&incumbent!=null&&!SameRoute(candidate,incumbent)&&PreferFastest){
    float improvement=incumbent.seconds-candidate.seconds;
    // Ignore small fluctuations; a substantial new obstruction overrides cooldown.
    if(improvement<Mathf.Max(3,incumbent.seconds*.1f)||(Time.time-lastRouteChange<6&&improvement<15))candidate=incumbent;
   }
   if(selected!=null&&candidate!=null&&(incumbent==null||!SameRoute(incumbent,candidate))){recalculationCount++;lastRouteChange=Time.time;rerouteNotice=incumbent==null?"현재 위치에서 경로 재탐색":"교통 상황 반영 · 빠른 길 재탐색";noticeUntil=Time.time+6;}
   selected=candidate;
   // Compare graph costs before appending the final curb approach.
   Approach(candidate);if(alternate!=candidate)Approach(alternate);
   if(candidate!=null){path.AddRange(candidate.points);remaining=candidate.distance;estimatedSeconds=candidate.seconds;}
   if(alternate!=null){alternativePath.AddRange(alternate.points);alternativeDistance=alternate.distance;alternativeSeconds=alternate.seconds;}
  }
  if(path.Count==0){instruction="진행 방향을 확인하고 도로로 복귀하세요";if(line!=null)line.positionCount=0;return;}
  instruction="진행 차로를 따라 직진하세요";
  float until=0;if(selected!=null)for(int i=0;i+1<selected.chain.Count;i++){
   var edge=edges[selected.chain[i]];until+=edge.length-(i==0?selected.progress:0);
   float angle=Vector3.SignedAngle(edge.direction,edges[selected.chain[i+1]].direction,Vector3.up);
   if(Mathf.Abs(angle)>=35){instruction=(angle>0?"우회전":"좌회전")+" · "+until.ToString("0")+"m · "+(angle<0?1:edge.lanes)+"차로 준비";break;}
  }
  if(remaining<25)instruction="진행 방향 오른쪽 연석에 정차 후 F";
  if(line!=null){line.positionCount=path.Count;for(int i=0;i<path.Count;i++)line.SetPosition(i,path[i]+Vector3.up*.19f);}
 }
 void FixedUpdate(){
  if(!active||hdRoute==null||vehicle==null){viaSampled=false;return;}
  float error;float progress=ProjectVehicle(out error);
  int target=RequiredVia>=0?RequiredVia:ObservedEdge;
  if(target<0){viaSampled=false;return;}
  if(viaObservedRoute!=hdRoute){viaSampled=false;viaObservedRoute=hdRoute;}
  int k=hdRoute.edges.IndexOf(target);if(k<0){viaSampled=false;return;}
  float remainingVia=hdRoute.ends[k]-progress;
  var body=vehicle.GetComponent<Rigidbody>();float speed=body?body.velocity.magnitude:0;
  float allowed=Mathf.Max(1,speed*Mathf.Min(.1f,Time.time-viaSampleTime)+.5f);
  if(error<3&&viaSampled&&Vector3.Distance(vehicle.position,viaPreviousPosition)<=allowed&&viaPreviousRemaining>=-.25f&&remainingVia<-.25f&&Vector3.Dot(vehicle.forward,hdRoute.path.Tangent(progress))>.6f){if(RequiredVia>=0){PassedRequiredVia=RequiredVia;RequiredVia=-1;}else{PassedObservedEdge=ObservedEdge;ObservedEdge=-1;}}
  viaSampled=error<3;viaPreviousPosition=vehicle.position;viaPreviousRemaining=remainingVia;viaSampleTime=Time.time;
 }
 void RefreshHd(){
  bool observe=Time.time>=nextHdCosts||hdRoute==null;
  if(observe){nextHdCosts=Time.time+6;for(int i=0;i<hd.paths.Length;i++){var p=hd.paths[i];float delay=0;if(conditions!=null)for(int k=1;k<p.points.Length;k++)delay+=conditions.ExtraSeconds(p.points[k-1],p.points[k],(p.points[k]-p.points[k-1]).normalized,3.2f);hdSeconds[i]=p.length/8+delay;}}
  float progress=0,error=float.MaxValue;
  if(hdRoute!=null)progress=ProjectVehicle(out error);

  if(freeGuidance&&hdRoute!=null&&hdRoute.path.length-progress<30&&hd.edges[hdGoal].next.Length>0){float at;int start=hd.Nearest(vehicle.position,vehicle.forward,out at);var forward=hd.Forward(start,at);if(forward!=null){UseFreeRoute(forward);progress=0;}}
  if(hdRoute==null||error>2.5f||Vector3.Dot(vehicle.forward,hdRoute.path.Tangent(progress))<.3f){
   float at;int start=hd.Nearest(vehicle.position,vehicle.forward,out at);
   bool previous=hdRoute!=null;
   if(freeGuidance){hdRoute=hd.Forward(start,at);if(hdRoute!=null)UseFreeRoute(hdRoute);}
   else hdRoute=GoalRoute(start,at,PreferLargeRoad?passengerRoadCosts:PreferFastest?hdSeconds:null);
   if(previous){recalculationCount++;rerouteNotice="실제 연결 차로에서 경로 재탐색";}
   if(hdRoute!=null)progress=ProjectVehicle(out error);
  }
  path.Clear();alternativePath.Clear();remaining=estimatedSeconds=alternativeSeconds=alternativeDistance=0;
  if(hdRoute==null){instruction="연결된 경로 없음 · 안내 차로로 복귀하세요";line.positionCount=0;return;}
  if(observe&&hybrid&&!freeGuidance){float at;int start=hd.Nearest(vehicle.position,vehicle.forward,out at);var candidate=GoalRoute(start,at,PreferLargeRoad?passengerRoadCosts:PreferFastest?hdSeconds:null);
   if(!PreferLargeRoad&&PreferFastest&&candidate!=null&&HdSeconds(candidate,0)<HdSeconds(hdRoute,progress)*.9f-3){hdRoute=candidate;progress=ProjectVehicle(out error);recalculationCount++;rerouteNotice="교통 상황 반영 · 경로 재탐색";}
  }
  path.Add(hdRoute.path.Position(progress));float accumulated=0;
  for(int i=1;i<hdRoute.path.points.Length;i++){accumulated+=Vector3.Distance(hdRoute.path.points[i-1],hdRoute.path.points[i]);if(accumulated>progress)path.Add(hdRoute.path.points[i]);}
  remaining=Mathf.Max(0,hdRoute.path.length-progress);estimatedSeconds=HdSeconds(hdRoute,progress);
  float altAt;int altStart=hd.Nearest(vehicle.position,vehicle.forward,out altAt);var alternate=GoalRoute(altStart,altAt,PreferFastest?null:hdSeconds);if(alternate!=null){alternativePath.AddRange(alternate.path.points);alternativeDistance=alternate.path.length;alternativeSeconds=HdSeconds(alternate,0);}
  instruction=freeGuidance?"자유 주행 · "+hd.Instruction(hdRoute,progress):remaining<18?(hybrid?"오른쪽 승하차 지점 · 정차 후 F":"시험 도착 지점 · 직접 감속하여 정차"):hd.Instruction(hdRoute,progress);
  line.positionCount=path.Count;for(int i=0;i<path.Count;i++)line.SetPosition(i,path[i]+Vector3.up*.19f);
 }
 float HdSeconds(HdRoadGraph.RoutePath route,float at){float result=0,begin=0;for(int i=0;i<route.edges.Count;i++){int e=route.edges[i];float length=Mathf.Max(0,route.ends[i]-Mathf.Max(at,begin));result+=hdSeconds[e]*length/Mathf.Max(.01f,hd.paths[e].length);begin=route.ends[i];}return result;}
 public float CurrentRoadWidth(){if(hd==null||vehicle==null)return 0;float at;int edge=hd.Nearest(vehicle.position,vehicle.forward,out at);return edge>=0?passengerRoadWidths[edge]:0;}
 public bool HasLargeRoadAlternative(){if(hd==null||!active)return false;float at;int edge=hd.Nearest(vehicle.position,vehicle.forward,out at);var r=GoalRoute(edge,at,passengerRoadCosts);if(r==null)return false;foreach(int e in r.edges)if(passengerRoadWidths[e]>=12)return true;return false;}
 public bool SetPassengerPreference(bool large,bool fastest){if(large&&!HasLargeRoadAlternative())return false;PreferLargeRoad=large;PreferFastest=fastest;selected=null;hdRoute=null;if(active)Refresh();return path.Count>1;}
 public float LargeRoadShare(){if(hdRoute==null)return 0;float broad=0,total=0;foreach(int e in hdRoute.edges){total+=hd.paths[e].length;if(passengerRoadWidths[e]>=12)broad+=hd.paths[e].length;}return total>0?broad/total:0;}
 public bool RequestHdMovement(int side,bool change){
  if(hd==null||hdRoute==null||Time.timeScale==0)return false;
  float at;int current=hd.Nearest(vehicle.position,vehicle.forward,out at);if(current<0)return false;
  // Only advertise a source-backed maneuver that is still ahead, reachable
  // without an earlier turn/change, and can still reach the selected goal.
  HdRoadGraph.RoutePath best=null;float nearest=float.MaxValue;
  for(int i=0;i<hd.edges.Length;i++){
   var e=hd.edges[i];if(change?e.change!=side:e.change!=0||e.turn!=side)continue;
   var approach=hd.Route(current,at,i,0,WithAvoidance(null));if(approach==null||approach.path.length>120||approach.path.length<2)continue;
   bool clear=true;for(int k=0;k<approach.edges.Count-1;k++)if(hd.edges[approach.edges[k]].turn!=0||hd.edges[approach.edges[k]].change!=0)clear=false;
   if(!clear)continue;
   HdRoadGraph.RoutePath route;
   if(freeGuidance){var forward=hd.Forward(i,0);var chain=new List<int>(approach.edges);if(forward==null)continue;for(int k=1;k<forward.edges.Count;k++)chain.Add(forward.edges[k]);route=hd.Compose(chain,at,hd.paths[chain[chain.Count-1]].length);}
   else if(RequiredVia>=0){var rest=hd.Via(i,0,RequiredVia,hdGoal,hdGoalAt,WithAvoidance(null));if(rest==null)continue;var chain=new List<int>(approach.edges);for(int k=1;k<rest.edges.Count;k++)chain.Add(rest.edges[k]);route=hd.Compose(chain,at,hdGoalAt);}
   else route=hd.Via(current,at,i,hdGoal,hdGoalAt,WithAvoidance(null));if(route==null||avoidEdge>=0&&route.edges.Contains(avoidEdge))continue;
   if(approach.path.length<nearest){nearest=approach.path.length;best=route;}
  }
  if(best==null)return false;if(freeGuidance)UseFreeRoute(best);else hdRoute=best;RefreshHd();return true;
 }
}
