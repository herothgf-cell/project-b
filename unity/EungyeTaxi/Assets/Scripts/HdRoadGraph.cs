using System;
using System.Collections.Generic;
using UnityEngine;

// One directed graph for the HD navigation line and the assisted steering path.
// Connections were validated by source node IDs by import_hdmap.py.
public sealed class HdRoadGraph {
 public sealed class RoutePath {
  public readonly List<int> edges=new List<int>();
  public readonly List<float> ends=new List<float>();
  public AssistedRoadNetwork.Path path;
  public int EdgeAt(float distance){for(int i=0;i<ends.Count;i++)if(distance<ends[i]-.01f)return edges[i];return edges[edges.Count-1];}
 }
 public readonly HdEdge[] edges;
 public readonly AssistedRoadNetwork.Path[] paths;
 public readonly HashSet<string> signalizedRules=new HashSet<string>();
 public HdRoadGraph(MapData map){
  if(map.junctionRules!=null)foreach(var rule in map.junctionRules)if(rule.signalMode>0)signalizedRules.Add(rule.id);
  edges=map.hdEdges;paths=new AssistedRoadNetwork.Path[edges.Length];
  for(int i=0;i<edges.Length;i++){
   if(edges[i].points==null||edges[i].points.Length<2)throw new ArgumentException("Invalid HD path");
   var p=new Vector3[edges[i].points.Length];for(int j=0;j<p.Length;j++)p[j]=edges[i].points[j].V();paths[i]=new AssistedRoadNetwork.Path(p);
  }
  for(int i=0;i<edges.Length;i++)foreach(int j in edges[i].next){
   if(j<0||j>=edges.Length||edges[i].toNode!=edges[j].fromNode||Vector3.Distance(paths[i].Position(paths[i].length),paths[j].Position(0))>.15f)throw new ArgumentException("Broken HD connection "+edges[i].id);
  }
 }
 public int Nearest(Vector3 position,Vector3 heading,out float at){
  int best=-1;float score=4;at=0;heading.y=0;
  for(int i=0;i<edges.Length;i++){
   if(edges[i].disabled||edges[i].change!=0)continue;float error;float d=paths[i].Project(position,out error);
   if(error>=score||heading.sqrMagnitude>.1f&&Vector3.Dot(heading.normalized,paths[i].Tangent(d))<.6f)continue;
   best=i;score=error;at=d;
  }return best;
 }
 public RoutePath Route(int start,float startAt,int goal,float goalAt,float[] weights=null){
  if(start<0||goal<0||start>=edges.Length||goal>=edges.Length||edges[start].disabled||edges[goal].disabled)return null;
  if(start==goal&&startAt>goalAt+.1f){RoutePath best=null;float score=float.MaxValue;foreach(int next in edges[start].next){if(next==start)continue;var rest=Route(next,0,goal,goalAt,weights);if(rest==null)continue;var chain=new List<int>{start};chain.AddRange(rest.edges);var candidate=Compose(chain,startAt,goalAt);float loopCost=0;foreach(int e in rest.edges)loopCost+=weights==null?paths[e].length:weights[e];if(candidate!=null&&loopCost<score){score=loopCost;best=candidate;}}return best;}
  var cost=new float[edges.Length];var prev=new int[edges.Length];var used=new bool[edges.Length];
  for(int i=0;i<cost.Length;i++){cost[i]=float.PositiveInfinity;prev[i]=-1;}
  cost[start]=0;var queue=new SortedSet<Tuple<float,int>>();queue.Add(Tuple.Create(0f,start));
  while(queue.Count>0){
   var item=queue.Min;queue.Remove(item);int cur=item.Item2;float value=item.Item1;if(used[cur]||value>cost[cur])continue;
   if(cur==goal){var chain=new List<int>();for(int i=cur;i>=0;i=prev[i])chain.Add(i);chain.Reverse();return Compose(chain,startAt,goalAt);}
   used[cur]=true;
   foreach(int next in edges[cur].next){float fraction=1-(cur==start?startAt/Mathf.Max(.01f,paths[cur].length):0);float c=value+(weights==null?paths[cur].length:weights[cur])*fraction+(edges[next].change!=0?(weights==null?12:1.5f):0);
    if(c<cost[next]){cost[next]=c;prev[next]=cur;queue.Add(Tuple.Create(c,next));}
   }
  }return null;
 }
 public RoutePath Compose(List<int> chain,float startAt,float goalAt){
  var result=new RoutePath();var points=new List<Vector3>();float total=0;
  for(int k=0;k<chain.Count;k++){
   int id=chain[k];if(edges[id].disabled)return null;var p=paths[id];float a=k==0?startAt:0,b=k==chain.Count-1?goalAt:p.length;
   if(b<a)return null;
   Append(points,p.Position(a),ref total);
   float d=0;for(int j=1;j<p.points.Length;j++){d+=Vector3.Distance(p.points[j-1],p.points[j]);if(d>a+.001f&&d<b-.001f)Append(points,p.points[j],ref total);}
   Append(points,p.Position(b),ref total);result.edges.Add(id);result.ends.Add(total);
  }
  if(points.Count<2)return null;result.path=new AssistedRoadNetwork.Path(points.ToArray());return result;
 }
 static void Append(List<Vector3> points,Vector3 p,ref float total){if(points.Count>0){float d=Vector3.Distance(points[points.Count-1],p);if(d<.0001f)return;total+=d;}points.Add(p);}
 public RoutePath Via(int start,float at,int via,int goal,float goalAt,float[] weights=null){
  if(via==goal)return Route(start,at,goal,goalAt,weights);
  var first=Route(start,at,via,paths[via].length,weights);var rest=Route(via,paths[via].length,goal,goalAt,weights);
  if(first==null||rest==null)return null;
  var chain=new List<int>(first.edges);for(int i=1;i<rest.edges.Count;i++)chain.Add(rest.edges[i]);return Compose(chain,at,goalAt);
 }
 public RoutePath Forward(int start,float at){
  if(start<0||edges[start].disabled)return null;var chain=new List<int>{start};int cur=start;float length=paths[cur].length-at;
  while(length<250&&chain.Count<40){int best=-1;float score=float.MaxValue;
   foreach(int next in edges[cur].next){if(chain.Contains(next)||edges[next].change!=0)continue;float angle=Vector3.Angle(paths[cur].Tangent(paths[cur].length),paths[next].Tangent(paths[next].length));if(angle<score){score=angle;best=next;}}
   if(best<0)break;cur=best;chain.Add(cur);length+=paths[cur].length;
  }return Compose(chain,at,paths[cur].length);
 }
 public string Instruction(RoutePath route,float at){
  float begin=0;
  for(int i=0;i<route.edges.Count;i++){
   var edge=edges[route.edges[i]];
   if(route.ends[i]<at){begin=route.ends[i];continue;}
   if(edge.change!=0)return (edge.change<0?"왼쪽":"오른쪽")+" 차로 변경 · "+Mathf.Max(0,begin-at).ToString("0")+"m · 점선 구간";
   if(edge.turn!=0){int number=edges[route.edges[Mathf.Max(0,i-1)]].number;return UturnMovement.Label(edge.turn)+" · "+Mathf.Max(0,begin-at).ToString("0")+"m · "+(number>0&&number<10?number+"차로":"연결 차로")+" 준비";}
   begin=route.ends[i];
  }var current=edges[route.EdgeAt(at)];return string.IsNullOrEmpty(current.roadName)?"안내 차로를 따라 직진":current.roadName+" 따라 직진"+(current.id.StartsWith("city:")?"":" · OSM 명칭");
 }
}
