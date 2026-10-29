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
 readonly PvpSignalZone[] zones;readonly PvpNpcPath[] npcPaths;readonly PvpPedestrianPath[] walks;readonly PvpServicePoint[] services;readonly PvpDistrict[] districts;readonly int[] supplyOrder,destinations;
 public string MapId {get;private set;}public bool IsExpanded {get;private set;}public bool LegacyLayout {get{return MapId=="pvp_circuit_01"&&!IsExpanded&&nodes.Length==12&&edges.Length==18;}}
 public PvpMovingObstacle Obstacle {get;private set;}public Vector3 ObstacleAt(int tick){int at=tick%Obstacle.PeriodTicks;float t=(at<=Obstacle.PeriodTicks/2?at:Obstacle.PeriodTicks-at)/(Obstacle.PeriodTicks*.5f);return Vector3.Lerp(Obstacle.From,Obstacle.To,t);}
 public Vector3 BoundsMin {get;private set;}public Vector3 BoundsMax {get;private set;}public float TotalRoadLength {get;private set;}
 public IReadOnlyList<PvpSignalZone> SignalZones {get{return Array.AsReadOnly(zones);}}public IReadOnlyList<PvpPedestrianPath> PedestrianPaths {get{return Array.AsReadOnly(walks);}}public IReadOnlyList<PvpServicePoint> Services {get{return Array.AsReadOnly(services);}}public IReadOnlyList<PvpDistrict> Districts {get{return Array.AsReadOnly(districts);}}
 public IReadOnlyList<PvpNpcPath> NpcPaths {get{var copy=new PvpNpcPath[npcPaths.Length];for(int i=0;i<copy.Length;i++)copy[i]=new PvpNpcPath{Edges=(int[])npcPaths[i].Edges.Clone(),StartDistance=npcPaths[i].StartDistance,Speed=npcPaths[i].Speed};return Array.AsReadOnly(copy);}}
 public int SupplyStop(int index){return supplyOrder.Length==0?index%stops.Length:supplyOrder[index%supplyOrder.Length];}public int Destination(int stop){return destinations.Length==0?(stop+1+(stop%2))%stops.Length:destinations[stop];}
 public float SpeedLimit(int edge){return edges[edge].SpeedLimit;}
 public IReadOnlyList<Vector3> Nodes {get{return Array.AsReadOnly(nodes);}} public IReadOnlyList<PvpConnection> Connections {get{return Array.AsReadOnly(edges);}}
 public IReadOnlyList<PvpStop> Stops {get{return Array.AsReadOnly(stops);}} public IReadOnlyList<int> Spawns {get{return Array.AsReadOnly(spawns);}}
 public float RoadWidth {get;private set;} public string ContentHash {get;private set;} public string MapHash {get{return ContentHash;}}
 static bool Finite(float f){return !float.IsNaN(f)&&!float.IsInfinity(f);}
 public PvpMapRuntime(PvpMapDefinition d){
  if(d==null||d.Nodes==null||d.Nodes.Length<2||d.Connections==null||d.Stops==null||d.Spawns==null||!Finite(d.RoadWidth)||d.RoadWidth<=0||string.IsNullOrEmpty(d.MapId))throw new ArgumentException("Invalid map definition");
  nodes=(Vector3[])d.Nodes.Clone();edges=(PvpConnection[])d.Connections.Clone();stops=(PvpStop[])d.Stops.Clone();spawns=(int[])d.Spawns.Clone();RoadWidth=d.RoadWidth;MapId=d.MapId;IsExpanded=d.ExpandedDistrict;
  for(int i=0;i<edges.Length;i++){var e=edges[i];if(e.SpeedLimit==0)e.SpeedLimit=Mathf.Min(e.NominalSpeed,12);edges[i]=e;}
  foreach(var n in nodes)if(!Finite(n.x)||!Finite(n.y)||!Finite(n.z)||Mathf.Abs(n.y)>0.001f)throw new ArgumentException("Planar finite nodes required");
  BoundsMin=BoundsMax=nodes[0];foreach(var n in nodes){BoundsMin=Vector3.Min(BoundsMin,n);BoundsMax=Vector3.Max(BoundsMax,n);}
  var keys=new HashSet<string>();foreach(var e in edges){if(!Finite(e.NominalSpeed)||e.NominalSpeed<=0||!Finite(e.SpeedLimit)||e.SpeedLimit<=0||e.SpeedLimit>30||e.From<0||e.To<0||e.From>=nodes.Length||e.To>=nodes.Length||Vector3.Distance(nodes[e.From],nodes[e.To])<0.01f||!keys.Add(e.From+":"+e.To))throw new ArgumentException("Invalid or duplicate connection");TotalRoadLength+=Vector3.Distance(nodes[e.From],nodes[e.To]);}
  keys.Clear();foreach(var s in stops)if(s.Node<0||s.Node>=nodes.Length||string.IsNullOrEmpty(s.Id)||!keys.Add(s.Id)||s.Position!=nodes[s.Node])throw new ArgumentException("Invalid stop");
  foreach(var s in spawns)if(s<0||s>=nodes.Length)throw new ArgumentException("Invalid spawn");
  bool legacy=LegacyLayout;zones=(PvpSignalZone[])(d.SignalZones??(legacy?PvpExpandedMap.LegacyZones():new PvpSignalZone[0])).Clone();walks=(PvpPedestrianPath[])(d.PedestrianPaths??(legacy?PvpExpandedMap.LegacyPedestrians():new PvpPedestrianPath[0])).Clone();services=(PvpServicePoint[])(d.Services??new PvpServicePoint[0]).Clone();districts=(PvpDistrict[])(d.Districts??new PvpDistrict[0]).Clone();supplyOrder=(int[])(d.SupplyOrder??new int[0]).Clone();destinations=(int[])(d.Destinations??new int[0]).Clone();
  var paths=d.NpcPaths??(legacy?PvpExpandedMap.LegacyNpcs():new PvpNpcPath[0]);npcPaths=new PvpNpcPath[paths.Length];for(int i=0;i<paths.Length;i++){var p=paths[i];if(p==null||p.Edges==null)throw new ArgumentException("Missing NPC route");npcPaths[i]=new PvpNpcPath{Edges=(int[])p.Edges.Clone(),StartDistance=p.StartDistance,Speed=p.Speed};}
  keys.Clear();foreach(var z in zones){if(string.IsNullOrEmpty(z.Id)||!keys.Add(z.Id)||z.ApproachEdge<0||z.ApproachEdge>=edges.Length||z.RightTurnEdge>=edges.Length||z.RightTurnEdge< -1||z.RightTurnEdge>=0&&edges[z.ApproachEdge].To!=edges[z.RightTurnEdge].From||!Finite(z.StopFraction)||!Finite(z.EntryFraction)||z.StopFraction<=0||z.EntryFraction>1||z.EntryFraction<=z.StopFraction||z.GreenTicks<=0||z.RedTicks<=0||z.GreenTicks>60000||z.RedTicks>60000||z.OffsetTicks<0||z.OffsetTicks>60000)throw new ArgumentException("Invalid signal zone");}
  foreach(var z in zones)if(z.AlternateEdgePlusOne!=0){int alternate=z.AlternateEdgePlusOne-1;if(alternate<0||alternate>=edges.Length||edges[alternate].To!=edges[z.ApproachEdge].To||Vector3.Dot(Forward(new PvpRoadPosition(alternate,0)),Forward(new PvpRoadPosition(z.ApproachEdge,0)))<.99f)throw new ArgumentException("Invalid shared signal approach");var line=Position(new PvpRoadPosition(z.ApproachEdge,z.StopFraction));var start=nodes[edges[alternate].From];var forward=Forward(new PvpRoadPosition(alternate,0));float along=Vector3.Dot(line-start,forward);if(along<0||along>Vector3.Distance(start,nodes[edges[alternate].To])||(line-start-forward*along).magnitude>.1f)throw new ArgumentException("Shared approach must cross its signal line");}
  foreach(var p in npcPaths){float length=0;if(p.Edges.Length==0||!Finite(p.Speed)||p.Speed<=0||p.Speed>20||!Finite(p.StartDistance)||p.StartDistance<0)throw new ArgumentException("Invalid NPC path");for(int i=0;i<p.Edges.Length;i++){int e=p.Edges[i],next=p.Edges[(i+1)%p.Edges.Length];if(e<0||e>=edges.Length||next<0||next>=edges.Length||edges[e].To!=edges[next].From)throw new ArgumentException("NPC path must be a connected loop");length+=Vector3.Distance(nodes[edges[e].From],nodes[edges[e].To]);}if(p.StartDistance>=length)throw new ArgumentException("NPC start beyond path");}
  foreach(var p in walks)if(p.SignalZone<0||p.SignalZone>=zones.Length||!Finite(p.Speed)||p.Speed<=0||p.Speed>4||!Finite(p.From.x)||!Finite(p.From.z)||!Finite(p.To.x)||!Finite(p.To.z)||Mathf.Abs(p.From.y)>.001f||Mathf.Abs(p.To.y)>.001f||Vector3.Distance(p.From,p.To)<1||Vector3.Distance(p.From,p.To)>40)throw new ArgumentException("Invalid crossing");
  keys.Clear();foreach(var s in services)if(string.IsNullOrEmpty(s.Id)||!keys.Add(s.Id)||s.StopIndex<0||s.StopIndex>=stops.Length||s.DurationTicks<10||s.DurationTicks>3000||s.CostPoints<0||s.CostPoints>1000)throw new ArgumentException("Invalid service facility");
  if(supplyOrder.Length!=0){if(supplyOrder.Length!=stops.Length)throw new ArgumentException("Supply order must cover all stops");var seen=new HashSet<int>();foreach(int s in supplyOrder)if(s<0||s>=stops.Length||!seen.Add(s))throw new ArgumentException("Invalid supply order");}if(destinations.Length!=0){if(destinations.Length!=stops.Length)throw new ArgumentException("Destination table mismatch");for(int i=0;i<destinations.Length;i++)if(destinations[i]<0||destinations[i]>=stops.Length||destinations[i]==i)throw new ArgumentException("Invalid destination");}
  foreach(var district in districts)if(string.IsNullOrEmpty(district.Name)||!Finite(district.Center.x)||!Finite(district.Center.z)||!Finite(district.Size.x)||!Finite(district.Size.y)||district.Size.x<=0||district.Size.y<=0)throw new ArgumentException("Invalid district");
  Obstacle=d.Obstacle.PeriodTicks==0?new PvpMovingObstacle{From=new Vector3(-5,0,0),To=new Vector3(5,0,0),PeriodTicks=400,Radius=1.8f}:d.Obstacle;var obstacle=Obstacle;if(obstacle.PeriodTicks<2||obstacle.PeriodTicks>60000||obstacle.PeriodTicks%2!=0||!Finite(obstacle.Radius)||obstacle.Radius<=0||!Finite(obstacle.From.x)||!Finite(obstacle.From.z)||!Finite(obstacle.To.x)||!Finite(obstacle.To.z)||obstacle.From.y!=0||obstacle.To.y!=0)throw new ArgumentException("Invalid moving obstacle");
  var b=new StringBuilder();b.Append(d.MapId).Append('|').Append(d.Version).Append('|').Append(RoadWidth.ToString("R",CultureInfo.InvariantCulture));
  foreach(var n in nodes)b.Append('|').Append(n.x.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(n.z.ToString("R",CultureInfo.InvariantCulture));
  foreach(var e in edges)b.Append('|').Append(e.From).Append('>').Append(e.To).Append('@').Append(e.NominalSpeed.ToString("R",CultureInfo.InvariantCulture)).Append('/').Append(e.SpeedLimit.ToString("R",CultureInfo.InvariantCulture));foreach(var s in stops)b.Append('|').Append(s.Id).Append(':').Append(s.Name).Append(':').Append(s.Node);foreach(var s in spawns)b.Append('|').Append(s);
  b.Append("|map-content-v2|").Append(IsExpanded);foreach(var z in zones)b.Append('|').Append(JsonUtility.ToJson(z));foreach(var p in npcPaths)b.Append('|').Append(JsonUtility.ToJson(p));foreach(var p in walks)b.Append('|').Append(JsonUtility.ToJson(p));foreach(var s in services)b.Append('|').Append(JsonUtility.ToJson(s));foreach(var district in districts)b.Append('|').Append(JsonUtility.ToJson(district));foreach(int s in supplyOrder)b.Append("|offer").Append(s);foreach(int target in destinations)b.Append("|destination").Append(target);
  b.Append('|').Append(JsonUtility.ToJson(Obstacle));
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
