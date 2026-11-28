using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// 2026-11-28: finds lanes the construction day card can close besides survey #48.
// A candidate is a straight curb-side (outermost) lane with an open lane of the same direction
// on its left, 25-90 m long, without a passenger stop, whose predecessors and successors keep
// another way on, and whose closure leaves every stop pair reachable. Report only; the chosen
// lane ids are copied into DailyRoadConstruction.Sites by hand.
public static class DailyRoadConstructionSiteFinder {
 public static void Run(){
  string region=MapCatalog.LockedRegion;MapData map;
  try{MapCatalog.LockedRegion="eungye";map=MapCatalog.Load();}finally{MapCatalog.LockedRegion=region;}
  var graph=new HdRoadGraph(map);var stops=PassengerStopCatalog.For(map).Stops;
  var stopEdges=new HashSet<int>(stops.Select(s=>s.laneEdge));
  var prev=new List<int>[graph.edges.Length];for(int i=0;i<prev.Length;i++)prev[i]=new List<int>();
  for(int i=0;i<graph.edges.Length;i++)if(graph.edges[i].next!=null)foreach(int n in graph.edges[i].next)if(n>=0&&n<prev.Length)prev[n].Add(i);
  var report=new StringBuilder();var found=new List<(int edge,float len,Vector3 mid,string road)>();
  for(int e=0;e<graph.edges.Length;e++){
   var edge=graph.edges[e];var path=graph.paths[e];
   if(edge.disabled||edge.id==null||edge.id.StartsWith("change:")||edge.number<2||edge.turn!=0||stopEdges.Contains(e))continue;
   if(path==null||path.length<25||path.length>90)continue;
   if(!Straight(path))continue;
   var mid=path.Position(path.length*.5f);var t=path.Tangent(path.length*.5f);t.y=0;t.Normalize();var left=new Vector3(-t.z,0,t.x);
   if(!SameDirectionLaneAt(graph,e,mid+left*3.2f,t)||SameDirectionLaneAt(graph,e,mid-left*3.2f,t))continue;
   if(prev[e].Count==0||prev[e].Any(p=>!graph.edges[p].next.Any(n=>n!=e&&!graph.edges[n].disabled)))continue;
   if(edge.next==null||edge.next.Length==0||edge.next.Any(n=>!prev[n].Any(p=>p!=e&&!graph.edges[p].disabled)))continue;
   found.Add((e,path.length,mid,edge.roadName??""));
  }
  report.AppendLine("geometric candidates="+found.Count);
  int kept=0;
  foreach(var c in found.OrderBy(c=>c.mid.x)){
   map.hdEdges[c.edge].ClosedForConstruction=true;
   int unreachable=0,pairs=0;
   foreach(var a in stops)foreach(var b in stops){if(a==b)continue;pairs++;var r=graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);if(r==null||r.edges.Contains(c.edge))unreachable++;}
   map.hdEdges[c.edge].ClosedForConstruction=false;
   bool ok=unreachable==0;if(ok)kept++;
   report.AppendLine((ok?"OK ":"NO ")+graph.edges[c.edge].id+" road="+c.road+" number="+graph.edges[c.edge].number+" len="+c.len.ToString("F1")+" mid=("+c.mid.x.ToString("F0")+","+c.mid.z.ToString("F0")+") unreachable="+unreachable+"/"+pairs);
  }
  report.AppendLine("reachable candidates="+kept);
  // landmarks near the shortlisted lanes, to name each site
  foreach(var c in found.Where(c=>Array.IndexOf(Shortlist,graph.edges[c.edge].id)>=0)){
   var near=map.buildings.Where(b=>!string.IsNullOrEmpty(b.name)&&b.points!=null&&b.points.Length>0)
    .Select(b=>(b.name,d:Vector2.Distance(new Vector2(b.points[0].x,b.points[0].z),new Vector2(c.mid.x,c.mid.z))))
    .Where(x=>x.d<120).OrderBy(x=>x.d).Select(x=>x.name+"@"+x.d.ToString("F0")).Distinct().Take(8);
   report.AppendLine("NEAR "+graph.edges[c.edge].id+" "+c.road+": "+string.Join(", ",near));
  }
  var folder=Path.GetFullPath(Path.Combine("..","..","artifacts","construction-sites"));Directory.CreateDirectory(folder);
  File.WriteAllText(Path.Combine(folder,"candidates.txt"),report.ToString());
  Debug.Log("CONSTRUCTION_SITES\n"+report);
 }
 static readonly string[] Shortlist={
  "local:11589:16123:10729:15920:part:1","local:3323:12871:2647:12412:part:1","local:4457:13181:5915:11994:part:1",
  "local:2189:7834:983:5801:part:1","local:7918:6167:7847:3996:part:1","local:14344:13354:10560:1555:part:1",
  "local:5992:16789:7141:14981:part:1","A2218G611523:1"};
 static bool Straight(AssistedRoadNetwork.Path p){var a=p.Tangent(1);var b=p.Tangent(p.length-1);a.y=b.y=0;return Vector3.Angle(a,b)<12;}
 static bool SameDirectionLaneAt(HdRoadGraph g,int self,Vector3 point,Vector3 dir){
  for(int i=0;i<g.edges.Length;i++){
   if(i==self||g.edges[i].disabled||g.paths[i]==null)continue;var b=g.PathBounds(i);b.Expand(4);point.y=b.center.y;if(!b.Contains(point))continue;
   float err;float at=g.paths[i].Project(point,out err);if(err>1.3f)continue;var t=g.paths[i].Tangent(at);t.y=0;if(Vector3.Dot(t.normalized,dir)>.9f)return true;
  }
  return false;
 }
}
