using System.Collections.Generic;
using UnityEngine;

// NPCs use precisely the directed lane edges used by navigation and H steering.
public sealed class DistrictTraffic : MonoBehaviour {
 public HdRoadGraph graph;public TrafficSystem system;public MapData map;
 public int continued,excluded;int sequence;
 readonly Dictionary<string,TrafficJunction> joins=new Dictionary<string,TrafficJunction>();
 readonly Dictionary<int,TrafficJunction> hdTurns=new Dictionary<int,TrafficJunction>();
 readonly Dictionary<string,TrafficRoute> routes=new Dictionary<string,TrafficRoute>();
 readonly HashSet<TrafficRoute> cleared=new HashSet<TrafficRoute>(),blocked=new HashSet<TrafficRoute>();
 readonly HashSet<int> service=new HashSet<int>();
 public void Initialize(TrafficSystem owner,MapData data,bool spawn){
  system=owner;map=data;graph=new HdRoadGraph(map);
  foreach(var j in system.junctions)joins[j.name]=j;
  var counts=new Dictionary<string,int>();foreach(var e in graph.edges)if(e.id.StartsWith("join:")){if(!counts.ContainsKey(e.ruleId))counts[e.ruleId]=0;counts[e.ruleId]++;}
  foreach(var r in map.junctionRules){int count;if(!counts.TryGetValue(r.id,out count)||count<3||joins.ContainsKey(r.id))continue;
   var j=new TrafficJunction{name=r.id,center=r.V(),direction=new Vector3(r.dx,0,r.dz).normalized,signalized=false,halfWidth=new[]{6f,6f}};joins[j.name]=j;system.junctions.Add(j);
  }
  for(int i=0;i<graph.edges.Length;i++){var e=graph.edges[i];if(e.status!="ngii"||e.turn==0)continue;var path=graph.paths[i];var mid=path.Position(path.length*.5f);TrafficJunction best=null;float distance=32;
   foreach(var j in system.junctions){float d=Vector3.Distance(mid,j.center);if(d<distance){best=j;distance=d;}}
   if(best==null){best=new TrafficJunction{name="임시 HD 교차로 "+i,center=mid,direction=path.Tangent(0),signalized=false,halfWidth=new[]{12f,12f}};system.junctions.Add(best);}hdTurns[i]=best;
  }
  if(!spawn)return;Physics.SyncTransforms();
  // Remove endpoints without a physically clear onward journey, including chains
  // into such endpoints. Every admitted goal can continue indefinitely.
  for(int i=0;i<map.stops.Length;i++)service.Add(i);bool changed;
  do{changed=false;foreach(int a in new List<int>(service)){bool onward=false;var start=map.stops[a];foreach(int b in service){if(a==b)continue;var end=map.stops[b];var r=CreateRoute(start.laneEdge,start.laneAt,end.laneEdge,end.laneAt);if(r!=null&&Clear(r)){onward=true;break;}}if(!onward){service.Remove(a);changed=true;}}}while(changed);
  SeedDistributed();
  int visible=0;var cells=new HashSet<Vector2Int>();foreach(var car in system.cars)if(car.Visible){visible++;cells.Add(Cell(car.transform.position));}
  Debug.Log("DISTRICT_TRAFFIC_READY allocated="+system.cars.Count+" visible="+visible+" cells250m="+cells.Count+" junctions="+system.junctions.Count+" service="+service.Count);
 }
 // Reuse physically verified journeys whose goals have an admitted onward route.
 // Sampling along them avoids filling the 48 slots with recycled copies at 12 stops.
 sealed class Seed {public TrafficRoute route;public float at;public Vector3 position;public Vector2Int cell;}
 static Vector2Int Cell(Vector3 p){return new Vector2Int(Mathf.FloorToInt(p.x/250),Mathf.FloorToInt(p.z/250));}
 bool OrdinaryStart(TrafficRoute route,float at){
  int edge=route.graphPath.EdgeAt(at);var e=graph.edges[edge];
  if(e.disabled||e.turn!=0||e.change!=0||e.id.StartsWith("join:")||e.id.IndexOf("parking",System.StringComparison.OrdinalIgnoreCase)>=0||e.id.IndexOf("service",System.StringComparison.OrdinalIgnoreCase)>=0)return false;
  float error;float local=graph.paths[edge].Project(route.Position(at),out error);
  if(error>.2f||local<8||local>graph.paths[edge].length-8)return false;
  // Never appear inside or immediately across a controlled junction or stop.
  foreach(var stop in route.stops)if(at>stop.distance-10&&at<stop.exitDistance+8)return false;
  foreach(var stop in map.stops)if(Vector3.Distance(route.Position(at),stop.V())<15)return false;
  return Vector3.Angle(route.Tangent(at-3),route.Tangent(at+3))<8;
 }
 void SeedDistributed(){
  var watch=System.Diagnostics.Stopwatch.StartNew();
  var candidates=new List<Seed>();var sampled=new HashSet<Vector2Int>();
  for(int a=0;a<map.stops.Length;a++){if(!service.Contains(a))continue;var start=map.stops[a];
   for(int b=0;b<map.stops.Length;b++){if(a==b||!service.Contains(b))continue;var end=map.stops[b];var route=CreateRoute(start.laneEdge,start.laneAt,end.laneEdge,end.laneAt);if(route==null||!Clear(route))continue;
    for(float at=12;at<route.length-12;at+=20){if(!OrdinaryStart(route,at))continue;var p=route.Position(at);var key=new Vector2Int(Mathf.RoundToInt(p.x/3),Mathf.RoundToInt(p.z/3));if(!sampled.Add(key))continue;
     candidates.Add(new Seed{route=route,at=at,position=p,cell=Cell(p)});
    }
   }
  }
  long samplingMs=watch.ElapsedMilliseconds;int candidateCount=candidates.Count;
  var loads=new Dictionary<Vector2Int,int>();
  var visiblePositions=new List<Vector3>(48);
  while(system.cars.Count<48&&candidates.Count>0){
   // Seeding is synchronous: positions cannot change during this candidate pass.
   // Read Unity transforms once instead of crossing into native code for every
   // candidate/car pair, retaining the same order and spacing score.
   visiblePositions.Clear();foreach(var car in system.cars)if(car.Visible)visiblePositions.Add(car.transform.position);
   int best=-1;float bestScore=-1;
   for(int i=0;i<candidates.Count;i++){var seed=candidates[i];float spacing=float.MaxValue;
    foreach(var position in visiblePositions)spacing=Mathf.Min(spacing,(seed.position-position).sqrMagnitude);
    if(spacing<100)continue;int load;loads.TryGetValue(seed.cell,out load);
    // Fill unrepresented cells first, then balance density and maximize spacing.
    float score=1000000f/(load+1)+Mathf.Min(spacing,100000);
    if(score>bestScore){bestScore=score;best=i;}
   }
   if(best<0)break;var chosen=candidates[best];candidates.RemoveAt(best);
   if(Occupied(chosen.position,chosen.route.Tangent(chosen.at),null))continue;
   var spawned=system.Spawn(chosen.route,chosen.at,system.cars.Count);if(!spawned)continue;
   spawned.district=this;int selectedLoad;loads.TryGetValue(chosen.cell,out selectedLoad);loads[chosen.cell]=selectedLoad+1;
  }
  Debug.Log("STARTUP_TRAFFIC candidates="+candidateCount+" sample_ms="+samplingMs+" seed_ms="+(watch.ElapsedMilliseconds-samplingMs));
 }
 public bool Occupied(Vector3 p,Vector3 heading,TrafficCar self){foreach(var hit in Physics.OverlapBox(p+Vector3.up*.85f,new Vector3(1.1f,.55f,4),Quaternion.LookRotation(heading),(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore))if(self==null||hit.GetComponentInParent<TrafficCar>()!=self)return true;return false;}
 // A saved live pose is restored after seeding. Defer NPCs intersecting the
 // restored player footprint; their usual occupancy-gated respawn remains intact.
 public void ClearPlayerOverlap(){
  Physics.SyncTransforms();
  foreach(var car in system.cars)if(car.Visible&&Physics.CheckBox(car.transform.position+Vector3.up*.85f,new Vector3(1.1f,.55f,4),car.transform.rotation,1<<8,QueryTriggerInteraction.Ignore))car.Recycle();
  Physics.SyncTransforms();
 }
 public TrafficRoute CreateRoute(int start,float at,int goal,float end){
  string key=start+":"+at.ToString("R")+":"+goal+":"+end.ToString("R");TrafficRoute route;if(routes.TryGetValue(key,out route))return route;
  var path=graph.Route(start,at,goal,end);if(path==null||path.path.length<10)return null;
  route=new TrafficRoute{graphPath=path,goalEdge=goal,goalAt=end};route.SetPoints(path.path.points);
  float begin=0;
  for(int k=0;k<path.edges.Count;k++){int id=path.edges[k];var edge=graph.edges[id];TrafficJunction j=null;
   if(edge.id.StartsWith("join:"))joins.TryGetValue(edge.ruleId,out j);else hdTurns.TryGetValue(id,out j);
   if(j!=null){var previous=route.stops.FindLast(s=>s.junction==j);if(previous!=null&&begin-2.5f<previous.exitDistance+2){previous.exitDistance=Mathf.Min(route.length-1,path.ends[k]+5);if(previous.turn==0)previous.turn=edge.turn;}else AddStop(route,j,begin-2.5f,path.ends[k]+5,edge.turn);}
   begin=path.ends[k];
  }
  // Source straight lanes can pass a junction without carrying a turn edge.
  // Each junction uses the same 2m samples. Interpolate once per route rather
  // than once per junction, preserving the original sample grid and boundaries.
  var samples=new Vector3[Mathf.FloorToInt(route.length/2)+1];
  for(int i=0;i<samples.Length;i++)samples[i]=route.Position(i*2f);
  foreach(var j in system.junctions){if(!j.signalized&&!j.name.StartsWith("임시 HD"))continue;
   float radius=Mathf.Max(j.halfWidth[0],j.halfWidth[1])+2,entry=-1;
   for(float d=0;d<=route.length+2;d+=2){bool inside=d<=route.length&&Vector3.Distance(samples[(int)(d/2)],j.center)<=radius;
    if(inside&&entry<0)entry=d;if(!inside&&entry>=0){float exit=Mathf.Min(d,route.length);bool exists=route.stops.Exists(s=>s.junction==j&&s.distance<=exit+5&&s.exitDistance>=entry-5);if(!exists)AddStop(route,j,entry-5,exit+5,0);entry=-1;}
   }
  }
  route.stops.Sort((a,b)=>a.distance.CompareTo(b.distance));routes[key]=route;return route;
 }
 void AddStop(TrafficRoute route,TrafficJunction j,float enter,float exit,int turn){
  if(enter<1||exit>route.length-1)return;var heading=route.Tangent(enter);int axis=Mathf.Abs(Vector3.Dot(heading,system.AxisDirection(j,0)))>.7f?0:1;
  // Match visible stop lines for configured signals; generated junctions use lane connectors.
  if(j.signalized){
   var inbound=heading;float alignment=.7f;
   if(j.facility!=null)foreach(var arm in j.facility.approaches){var direction=new Vector3(arm.dx,0,arm.dz).normalized;float dot=Vector3.Dot(direction,heading);if(dot>alignment){alignment=dot;inbound=direction;}}
   axis=Mathf.Abs(Vector3.Dot(inbound,system.AxisDirection(j,0)))>.7f?0:1;
   int sign=system.Sign(inbound,axis,j);bool edited=j.editorStops[axis*2+(sign>0?0:1)]>0;var plane=edited?system.StopPosition(j,axis,sign):j.center-inbound*j.StopDistance(axis);var normal=edited?system.StopNormal(j,axis,sign):inbound;float crossing=-1,nearest=float.MaxValue;
   float start=Mathf.Max(0,enter-50),previous=Vector3.Dot(route.Position(start)-plane,normal);
   for(float d=start+.5f;d<=Mathf.Min(route.length,exit)+.5f;d+=.5f){float value=Vector3.Dot(route.Position(d)-plane,normal);if(previous<=0&&value>0){float at=d-.5f+.5f*(-previous)/(value-previous);if(Mathf.Abs(at-enter)<nearest){nearest=Mathf.Abs(at-enter);crossing=at;}}previous=value;}
   // The bumper reaches the same plane used to draw the line, even on a curved turn.
   if(crossing>=0)enter=Mathf.Max(1,crossing-2.1f);
   exit=Mathf.Min(route.length-1,exit);
  }
  route.stops.Add(new TrafficStop{junction=j,axis=axis,turn=turn,distance=enter,exitDistance=exit});
 }
 bool Clear(TrafficRoute route){if(cleared.Contains(route))return true;if(blocked.Contains(route))return false;if(system.RouteClearOfStaticObstacles(route)){cleared.Add(route);return true;}blocked.Add(route);excluded++;return false;}
 TrafficRoute Next(int edge,float at){for(int n=0;n<map.stops.Length;n++){int index=(sequence++)%map.stops.Length;if(!service.Contains(index))continue;var end=map.stops[index];if(end.laneEdge==edge)continue;var route=CreateRoute(edge,at,end.laneEdge,end.laneAt);if(route!=null&&Clear(route))return route;}return null;}
 public bool Continue(TrafficCar car){var next=Next(car.route.goalEdge,car.route.goalAt);if(next==null)return false;car.Continue(next);continued++;return true;}
}
