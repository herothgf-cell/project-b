using System.Collections.Generic;
using UnityEngine;
public class RoadNavigation : MonoBehaviour {
 public readonly List<Vector3> path=new List<Vector3>();public float remaining;public string instruction="호출을 선택하거나 길가 손님을 찾아보세요";
 public Transform vehicle;public Vector3 destination;public bool active;
 class Node {public Vector3 p;public List<int> outgoing=new List<int>();}
 class Edge {public int from,to,reverse;public float length,offset;public Vector3 direction;}
 readonly List<Node> nodes=new List<Node>();readonly List<Edge> edges=new List<Edge>();readonly Dictionary<string,int> ids=new Dictionary<string,int>();
 LineRenderer line;float nextUpdate;MapStop goal;
 int NodeFor(Vector3 p){string key=p.x.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+","+p.z.ToString("F2",System.Globalization.CultureInfo.InvariantCulture);int id;if(ids.TryGetValue(key,out id))return id;id=nodes.Count;ids[key]=id;nodes.Add(new Node{p=p});return id;}
 public void Build(MapData map,Transform taxi){vehicle=taxi;foreach(var road in map.roads)for(int i=1;i<road.points.Length;i++){int a=NodeFor(road.points[i-1].V()),b=NodeFor(road.points[i].V());if(a==b)continue;var d=(nodes[b].p-nodes[a].p).normalized;float len=Vector3.Distance(nodes[a].p,nodes[b].p),offset=road.width*.5f-road.width/road.DisplayLanes*.5f;int k=edges.Count;
  edges.Add(new Edge{from=a,to=b,reverse=k+1,length=len,offset=offset,direction=d});edges.Add(new Edge{from=b,to=a,reverse=k,length=len,offset=offset,direction=-d});nodes[a].outgoing.Add(k);nodes[b].outgoing.Add(k+1);}
  line=new GameObject("우측 차로 안내 경로").AddComponent<LineRenderer>();line.transform.SetParent(transform);line.startWidth=line.endWidth=.25f;line.material=WorldBuilder.Mat("Navigation cyan",Color.cyan);line.material.EnableKeyword("_EMISSION");line.material.SetColor("_EmissionColor",Color.cyan);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
 }
 int NearestNode(Vector3 p){int best=0;float dist=float.MaxValue;p.y=0;for(int i=0;i<nodes.Count;i++){float d=(nodes[i].p-p).sqrMagnitude;if(d<dist){dist=d;best=i;}}return best;}
 List<Vector3> Route(Vector3 start,Vector3 heading,Vector3 end,Vector3 arrival,bool requireArrival){start.y=0;int target=NearestNode(end),n=edges.Count;int initial=-1;float best=float.MaxValue,initialProgress=0;
  for(int i=0;i<n;i++){var e=edges[i];float along=Mathf.Clamp(Vector3.Dot(start-nodes[e.from].p,e.direction),0,e.length);var lane=nodes[e.from].p+e.direction*along+TrafficSystem.Right(e.direction)*e.offset;float cost=(lane-start).sqrMagnitude+(heading.sqrMagnitude>.1f?(1-Vector3.Dot(heading,e.direction))*90:0);if(cost<best){best=cost;initial=i;initialProgress=along;}}
  var result=new List<Vector3>();if(initial<0)return result;var costs=new float[n];var previous=new int[n];var visited=new bool[n];for(int i=0;i<n;i++){costs[i]=float.MaxValue;previous[i]=-1;}costs[initial]=edges[initial].length-initialProgress;int found=-1;
  for(int loop=0;loop<n;loop++){int cur=-1;float low=float.MaxValue;for(int i=0;i<n;i++)if(!visited[i]&&costs[i]<low){low=costs[i];cur=i;}if(cur<0)break;var e=edges[cur];if(e.to==target&&(!requireArrival||Vector3.Dot(e.direction,arrival)>.85f)){found=cur;break;}visited[cur]=true;
   foreach(int next in nodes[e.to].outgoing){if(next==e.reverse&&nodes[e.to].outgoing.Count>1)continue;float turn=Vector3.Angle(e.direction,edges[next].direction);float cost=low+edges[next].length+(turn>150?30:turn*.02f);if(cost<costs[next]){costs[next]=cost;previous[next]=cur;}}
  }
  if(found<0)return result;var chain=new List<int>();for(int at=found;at>=0;at=previous[at])chain.Add(at);chain.Reverse();
  var first=edges[chain[0]];result.Add(nodes[first.from].p+first.direction*initialProgress+TrafficSystem.Right(first.direction)*first.offset);
  for(int i=0;i<chain.Count;i++){var e=edges[chain[i]];var p=nodes[e.to].p+TrafficSystem.Right(e.direction)*e.offset;if(i+1<chain.Count){var next=edges[chain[i+1]];p=nodes[e.to].p+(TrafficSystem.Right(e.direction)*e.offset+TrafficSystem.Right(next.direction)*next.offset)*.5f;}result.Add(p);}
  return result;
 }
 public List<Vector3> Find(Vector3 a,Vector3 b){return Route(a,Vector3.zero,b,Vector3.zero,false);}
 public List<Vector3> Find(MapStop a,MapStop b){var result=Route(a.Kerb-a.Direction*3,a.Direction,b.V(),b.Direction,true);if(result.Count>0)FinishApproach(result,b);return result;}
 static void FinishApproach(List<Vector3> route,MapStop stop){if(route.Count>1)route.RemoveAt(route.Count-1);route.Add(stop.Kerb-stop.Direction*3);route.Add(stop.Kerb);}
 public float Distance(Vector3 a,Vector3 b){return Length(Find(a,b));}
 public float Distance(MapStop a,MapStop b){return Length(Find(a,b));}
 static float Length(List<Vector3> p){if(p.Count==0)return float.PositiveInfinity;float d=0;for(int i=1;i<p.Count;i++)d+=Vector3.Distance(p[i-1],p[i]);return d;}
 public void SetDestination(MapStop stop){goal=stop;destination=stop.Kerb;active=true;Refresh();}
 public void SetDestination(Vector3 point){goal=null;destination=point;active=true;Refresh();}
 public void Clear(){active=false;path.Clear();line.positionCount=0;remaining=0;instruction="호출을 선택하거나 길가 손님을 찾아보세요";}
 void Update(){if(active&&Time.time>=nextUpdate){Refresh();nextUpdate=Time.time+.8f;}}
 public void Refresh(){path.Clear();bool nearby=goal!=null&&Vector3.Dot(vehicle.forward,goal.Direction)>.85f&&Mathf.Abs(Vector3.Dot(vehicle.position-goal.Kerb,TrafficSystem.Right(goal.Direction)))<2.5f&&Vector3.Dot(vehicle.position-goal.Kerb,goal.Direction)>-10&&Vector3.Dot(vehicle.position-goal.Kerb,goal.Direction)<4.5f;
  if(nearby){var p=vehicle.position;p.y=0;path.Add(p);path.Add(goal.Kerb);}else path.AddRange(Route(vehicle.position,vehicle.forward,goal!=null?goal.V():destination,goal!=null?goal.Direction:Vector3.zero,goal!=null));
  if(path.Count==0){remaining=0;instruction="진행 방향을 확인하고 도로로 복귀하세요";line.positionCount=0;return;}
  if(goal!=null&&!nearby)FinishApproach(path,goal);remaining=Length(path);instruction="우측 차로를 따라 직진하세요";
  float until=0;for(int i=1;i<path.Count-1;i++){until+=Vector3.Distance(path[i-1],path[i]);var a=path[i]-path[i-1];var b=path[i+1]-path[i];if(a.magnitude<1||b.magnitude<1)continue;float angle=Vector3.SignedAngle(a,b,Vector3.up);if(Mathf.Abs(angle)>30){instruction=(angle>0?"우회전":"좌회전")+" · "+until.ToString("0")+"m";break;}}
  if(remaining<25)instruction="진행 방향 오른쪽 연석에 정차 후 F";
  line.positionCount=path.Count;for(int i=0;i<path.Count;i++)line.SetPosition(i,path[i]+Vector3.up*.19f);
 }
}
