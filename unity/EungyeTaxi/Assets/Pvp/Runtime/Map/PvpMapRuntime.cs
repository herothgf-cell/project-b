using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace Pvp.Map {
public enum PvpRouteMode { Shortest, Fastest }
public struct PvpRoadPosition { public int Edge; public float Fraction; public PvpRoadPosition(int edge,float fraction){Edge=edge;Fraction=fraction;} }
public sealed class PvpMapRuntime {
 readonly Vector3[] nodes; readonly PvpConnection[] edges; readonly PvpStop[] stops; readonly int[] spawns;
 public IReadOnlyList<Vector3> Nodes {get{return Array.AsReadOnly(nodes);}} public IReadOnlyList<PvpConnection> Connections {get{return Array.AsReadOnly(edges);}}
 public IReadOnlyList<PvpStop> Stops {get{return Array.AsReadOnly(stops);}} public IReadOnlyList<int> Spawns {get{return Array.AsReadOnly(spawns);}}
 public float RoadWidth {get;private set;} public string ContentHash {get;private set;} public string MapHash {get{return ContentHash;}}
 static bool Finite(float f){return !float.IsNaN(f)&&!float.IsInfinity(f);}
 public PvpMapRuntime(PvpMapDefinition d){
  if(d==null||d.Nodes==null||d.Nodes.Length<2||d.Connections==null||d.Stops==null||d.Spawns==null||!Finite(d.RoadWidth)||d.RoadWidth<=0||string.IsNullOrEmpty(d.MapId))throw new ArgumentException("Invalid map definition");
  nodes=(Vector3[])d.Nodes.Clone();edges=(PvpConnection[])d.Connections.Clone();stops=(PvpStop[])d.Stops.Clone();spawns=(int[])d.Spawns.Clone();RoadWidth=d.RoadWidth;
  foreach(var n in nodes)if(!Finite(n.x)||!Finite(n.y)||!Finite(n.z)||Mathf.Abs(n.y)>0.001f)throw new ArgumentException("Planar finite nodes required");
  var keys=new HashSet<string>();foreach(var e in edges){if(!Finite(e.NominalSpeed)||e.NominalSpeed<=0||e.From<0||e.To<0||e.From>=nodes.Length||e.To>=nodes.Length||Vector3.Distance(nodes[e.From],nodes[e.To])<0.01f||!keys.Add(e.From+":"+e.To))throw new ArgumentException("Invalid or duplicate connection");}
  keys.Clear();foreach(var s in stops)if(s.Node<0||s.Node>=nodes.Length||string.IsNullOrEmpty(s.Id)||!keys.Add(s.Id)||s.Position!=nodes[s.Node])throw new ArgumentException("Invalid stop");
  foreach(var s in spawns)if(s<0||s>=nodes.Length)throw new ArgumentException("Invalid spawn");
  var b=new StringBuilder();b.Append(d.MapId).Append('|').Append(d.Version).Append('|').Append(RoadWidth.ToString("R",CultureInfo.InvariantCulture));
  foreach(var n in nodes)b.Append('|').Append(n.x.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(n.z.ToString("R",CultureInfo.InvariantCulture));
  foreach(var e in edges)b.Append('|').Append(e.From).Append('>').Append(e.To).Append('@').Append(e.NominalSpeed.ToString("R",CultureInfo.InvariantCulture));foreach(var s in stops)b.Append('|').Append(s.Id).Append(':').Append(s.Name).Append(':').Append(s.Node);foreach(var s in spawns)b.Append('|').Append(s);
  using(var sha=SHA256.Create())ContentHash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString()))).Replace("-","").ToLowerInvariant();
 }
 public bool IsLegalTransition(PvpRoadPosition previous,PvpRoadPosition next){if(previous.Edge<0||next.Edge<0||previous.Edge>=edges.Length||next.Edge>=edges.Length||!Finite(previous.Fraction)||!Finite(next.Fraction)||previous.Fraction<0||previous.Fraction>1||next.Fraction<0||next.Fraction>1)return false;if(previous.Edge==next.Edge)return true;var a=edges[previous.Edge];var b=edges[next.Edge];return a.To==b.From && Vector3.Distance(Position(previous),nodes[a.To])<=RoadWidth*2 && Vector3.Distance(Position(next),nodes[b.From])<=RoadWidth*2;}
 public bool HasConnection(int from,int to){foreach(var e in edges)if(e.From==from&&e.To==to)return true;return false;}
 public Vector3 Position(PvpRoadPosition p){return Vector3.Lerp(nodes[edges[p.Edge].From],nodes[edges[p.Edge].To],p.Fraction);}
 public Vector3 Forward(PvpRoadPosition p){var e=edges[p.Edge];return (nodes[e.To]-nodes[e.From]).normalized;}
 public bool TryLocate(Vector3 position,out PvpRoadPosition result) {
  return Locate(position,false,default(PvpRoadPosition),out result);
 }
 // Track only the prior directed lane and its explicit legal successors.
 // At shared endpoints, keep the prior lane until motion selects a branch.
 public bool TryLocate(Vector3 position,PvpRoadPosition previous,out PvpRoadPosition result) {
  return Locate(position,true,previous,out result);
 }
 bool Locate(Vector3 position,bool track,PvpRoadPosition previous,out PvpRoadPosition result) {
  result=default(PvpRoadPosition);
  if(!Finite(position.x)||!Finite(position.y)||!Finite(position.z)||Mathf.Abs(position.y)>4)return false;
  if(track&&!IsLegalTransition(previous,previous))return false;
  float best=RoadWidth*RoadWidth*0.25f;bool found=false;
  for(int i=0;i<edges.Length;i++) {
   var e=edges[i];var a=nodes[e.From];var delta=nodes[e.To]-a;
   float t=Mathf.Clamp01(Vector3.Dot(position-a,delta)/delta.sqrMagnitude);
   var candidate=new PvpRoadPosition(i,t);
   if(track&&!IsLegalTransition(previous,candidate))continue;
   var q=a+delta*t;
   float distance=(new Vector3(position.x,0,position.z)-q).sqrMagnitude;
   bool tied=found&&Mathf.Abs(distance-best)<0.0001f;
   if(distance<=best && (!tied || (track&&i==previous.Edge))) {
    best=distance;result=candidate;found=true;
   }
  }
  return found;
 }
 public bool TryRecover(PvpRoadPosition previous,float rearwardDistance,out PvpRoadPosition result){result=default(PvpRoadPosition);if(previous.Edge<0||previous.Edge>=edges.Length||!Finite(previous.Fraction)||previous.Fraction<0||previous.Fraction>1||!Finite(rearwardDistance)||rearwardDistance<0)return false;var e=edges[previous.Edge];result=new PvpRoadPosition(previous.Edge,Mathf.Max(0,previous.Fraction-rearwardDistance/Vector3.Distance(nodes[e.From],nodes[e.To])));return true;}
}
public sealed class PvpRoute { public bool Found {get;private set;} public int[] Nodes {get;private set;} public Vector3[] Points {get;private set;} public float Distance {get;private set;} internal PvpRoute(bool found,int[] nodes,Vector3[] points,float distance){Found=found;Nodes=nodes;Points=points;Distance=distance;} }
public static class PvpRoutePlanner {
 static PvpRoute Missing(){return new PvpRoute(false,new int[0],new Vector3[0],0);}
 public static PvpRoute FindRoute(PvpMapRuntime map,int from,int to,PvpRouteMode mode=PvpRouteMode.Shortest){
  if(map==null||from<0||to<0||from>=map.Nodes.Count||to>=map.Nodes.Count)return Missing();int count=map.Nodes.Count;var dist=new float[count];var prev=new int[count];var seen=new bool[count];for(int i=0;i<count;i++){dist[i]=float.PositiveInfinity;prev[i]=-1;}dist[from]=0;
  for(int step=0;step<count;step++){int current=-1;for(int i=0;i<count;i++)if(!seen[i]&&(current<0||dist[i]<dist[current]))current=i;if(current<0||float.IsPositiveInfinity(dist[current]))break;if(current==to)break;seen[current]=true;foreach(var e in map.Connections)if(e.From==current){float next=dist[current]+Vector3.Distance(map.Nodes[e.From],map.Nodes[e.To])/(mode==PvpRouteMode.Fastest?e.NominalSpeed:1);if(next<dist[e.To]){dist[e.To]=next;prev[e.To]=current;}}}
  if(float.IsPositiveInfinity(dist[to]))return Missing();var chain=new List<int>();for(int n=to;n>=0;n=prev[n])chain.Add(n);chain.Reverse();var points=new Vector3[chain.Count];for(int i=0;i<points.Length;i++)points[i]=map.Nodes[chain[i]];float distance=0;for(int i=1;i<points.Length;i++)distance+=Vector3.Distance(points[i-1],points[i]);return new PvpRoute(true,chain.ToArray(),points,distance);
 }
 public static PvpRoute FindRoute(PvpMapRuntime map,PvpRoadPosition from,int to,PvpRouteMode mode=PvpRouteMode.Shortest){if(map==null||from.Edge<0||from.Edge>=map.Connections.Count||float.IsNaN(from.Fraction)||from.Fraction<0||from.Fraction>1)return Missing();var edge=map.Connections[from.Edge];var tail=FindRoute(map,edge.To,to,mode);if(!tail.Found)return tail;var points=new Vector3[tail.Points.Length+1];points[0]=map.Position(from);Array.Copy(tail.Points,0,points,1,tail.Points.Length);return new PvpRoute(true,tail.Nodes,points,tail.Distance+Vector3.Distance(points[0],map.Nodes[edge.To]));}
}}
