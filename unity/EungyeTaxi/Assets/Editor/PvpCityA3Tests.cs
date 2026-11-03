using System;
using System.Linq;
using UnityEngine;

// Part of the final bundled acceptance; this is graph/geometry validation,
// not a claim of physical driving or long-running LAN traffic acceptance.
public static class PvpCityA3Tests {
 static int checks;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception("PVP_CITY_A3: "+label);}
 public static void Run(){
  checks=0;var map=PvpCityMap.Create();var graph=new HdRoadGraph(map);
  Check(map.source.Contains("A3")&&map.editorDocument.id=="pvp-city-a3-native","A3 live native map");
  Check(map.width==3600&&map.depth==2800&&map.stops.Length==18,"dimensions and 18 stable demand slots");
  var streets=map.roads.Where(r=>r.id.StartsWith("city-road:")).ToArray();
  for(int district=0;district<9;district++)Check(streets.Any(r=>r.id.Contains("alley:a3:"+district+":")),"district geometry "+district);
  foreach(string feature in new[]{":fan",":tee",":rear",":southbound",":northbound",":station-rear-tee",":station-north-tee",":station-east-fan",":hospital-rear-tee",":west-north-tee",":office-south-chicane",":market-east-chicane"})Check(streets.Any(r=>r.id.Contains(feature)),"authored topology "+feature);
  var oneWays=streets.Where(r=>r.oneWay!=0).ToArray();Check(oneWays.Length==8&&oneWays.All(r=>r.id.Contains("alley:a3:1:")),"two market cells retain eight short paired one-way segments");
  foreach(var road in oneWays){
   var directed=map.hdEdges.Where(e=>e.sourceId==road.id&&e.turn==0&&e.change==0).ToArray();
   Check(directed.Length==1,"one native direction per market segment");
   var edge=Array.IndexOf(map.hdEdges,directed[0]);
   Check(graph.Route(edge,graph.paths[edge].length*.75f,edge,graph.paths[edge].length*.25f)!=null,"one-way segment has legal return route");
  }
  // A centerline crossing at the same level is legal only at an explicit
  // authored endpoint. Broad phase keeps the final acceptance inexpensive.
  for(int i=0;i<streets.Length;i++)for(int j=i+1;j<streets.Length;j++){
   var a=streets[i].points;var b=streets[j].points;
   if(a.Max(p=>p.x)<b.Min(p=>p.x)||b.Max(p=>p.x)<a.Min(p=>p.x)||a.Max(p=>p.z)<b.Min(p=>p.z)||b.Max(p=>p.z)<a.Min(p=>p.z))continue;
   bool shared=new[]{a.First(),a.Last()}.Any(p=>new[]{b.First(),b.Last()}.Any(q=>Vector3.Distance(p.V(),q.V())<.05f));
   if(shared)continue;
   for(int n=1;n<a.Length;n++)for(int m=1;m<b.Length;m++)Check(!Cross(a[n-1].V(),a[n].V(),b[m-1].V(),b[m].V()),"no unconnected centerline crossing "+streets[i].id+" / "+streets[j].id);
  }
  int pairs=0;foreach(var a in map.stops)foreach(var b in map.stops)if(a!=b){Check(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)!=null,"directed passenger pair");pairs++;}
  Check(pairs==306,"all 306 directed pairs inspected");
  var stations=FuelStationLayout.For(map);Check(stations.Count==3&&stations.Count(s=>s.SupportsRepair)==2&&stations.Count(s=>s.SupportsWash)==2,"native service counts");
  foreach(var station in stations){
   foreach(var stop in map.stops){Check(graph.Route(stop.laneEdge,stop.laneAt,station.approach.laneEdge,station.approach.laneAt)!=null,"service entry reachable");
    float at;int exit=graph.Nearest(station.drivePath.Last(),station.direction,out at);Check(exit>=0&&graph.Route(exit,at,stop.laneEdge,stop.laneAt)!=null,"service exit rejoins reachable graph");}
  }
  var parking=ProjectMapScene.Services(map,"parking").ToArray();Check(parking.Length==4&&ProjectMapScene.Services(map,"restaurant").Count()==2,"four parking / two food services");
  foreach(int index in new[]{0,4,12,16})foreach(var stop in map.stops)Check(graph.Route(map.stops[index].laneEdge,Mathf.Max(0,map.stops[index].laneAt-1),stop.laneEdge,stop.laneAt)!=null,"four start approaches reach every stop "+index+" -> "+stop.name);
  Debug.Log("PVP_CITY_A3_TESTS_OK checks="+checks+" directedPairs="+pairs+" roads="+streets.Length+" oneWay="+oneWays.Length);
 }
 static float Side(Vector3 a,Vector3 b,Vector3 p){return (b.x-a.x)*(p.z-a.z)-(b.z-a.z)*(p.x-a.x);}
 static bool Cross(Vector3 a,Vector3 b,Vector3 c,Vector3 d){return Side(a,b,c)*Side(a,b,d)<-.0001f&&Side(c,d,a)*Side(c,d,b)<-.0001f;}
}
