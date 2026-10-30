using System;
using System.Linq;
using UnityEngine;
public static class PvpCityMapTests {
 static int checks;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception("PVP_CITY: "+label);}
 public static void Run(){
  checks=0;
  var type=typeof(MapData).Assembly.GetType("PvpCityMap");Check(type!=null,"expanded city factory exists");
  var map=(MapData)type.GetMethod("Create").Invoke(null,null);
  Check(map.width==3600&&map.depth==2800,"approved dimensions");
  Check(map.stops.Length==18&&map.landmarks.Length==9,"18 hubs / nine districts");
  Check(map.buildings.Length>=900,"urban block frontage density");
  Check(map.areas.Count(a=>a.kind=="sidewalk")>=1500,"continuous street sidewalks with access cutouts");
  Check(JsonUtility.ToJson(map)==JsonUtility.ToJson((MapData)type.GetMethod("Create").Invoke(null,null)),"deterministic authored geometry");
  Check(map.hybrid&&map.HasHd,"native hybrid directed graph");
  Check(ProjectMapScene.Services(map,"parking").Count()==4,"four start areas");
  var graph=new HdRoadGraph(map);int pairs=0;
  foreach(var a in map.stops){
   Check(a.graphStop&&a.laneEdge>=0,"directed passenger approach");
   Check(a.CanStop(a.Kerb,a.Direction,0)&&!a.CanStop(a.Kerb,-a.Direction,0),"directional boarding");
   Check(ProjectMapScene.OnArea(map,a.Passenger,true),"passenger stands on sidewalk");
   Check(!ProjectMapScene.OnArea(map,a.Kerb,true),"taxi clear of sidewalk");
   foreach(var r in map.roads.Where(r=>r.id.StartsWith("city-road:")))for(int i=1;i<r.points.Length;i++)Check(Distance(a.Kerb,r.points[i-1].V(),r.points[i].V())>r.width*.5f+1.2f,"boarding pocket clears through lane");
   foreach(var b in map.stops)if(a!=b){var route=graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);Check(route!=null&&route.path.length>0,"all directed hub pairs");pairs++;}
  }
  var stations=FuelStationLayout.For(map);Check(stations.Count==3,"three native fuel stations");
  Check(stations.Count(s=>s.SupportsWash)==2&&stations.Count(s=>s.SupportsRepair)==2,"two wash and repair sites");
  foreach(var station in stations){
   Check(station.CanRefuel(station.pump,station.direction,0)&&!station.CanRefuel(station.pump,-station.direction,0),"native pump interaction");
   foreach(var stop in map.stops)Check(graph.Route(stop.laneEdge,stop.laneAt,station.approach.laneEdge,station.approach.laneAt)!=null,"fuel approach reachable");
   Check(station.drivePath.Length>20,"smooth separate service access");
   foreach(var p in station.drivePath)foreach(float side in new[]{-.95f,0,.95f})Check(!ProjectMapScene.OnArea(map,p+TrafficSystem.Right(station.direction)*side,true),"service access clear of raised sidewalk");
  }
  foreach(var b in map.buildings)foreach(var p in b.points)foreach(var r in map.roads)for(int i=1;i<r.points.Length;i++)Check(Distance(p.V(),r.points[i-1].V(),r.points[i].V())>=r.width*.5f+2,"building clears road: "+b.id);
  foreach(var e in map.hdEdges){Check(e.next.Length>0,"no dead-end lane "+e.id);foreach(int next in e.next){var a=(e.points.Last().V()-e.points[e.points.Length-2].V()).normalized;var b=(map.hdEdges[next].points[1].V()-map.hdEdges[next].points[0].V()).normalized;Check(Vector3.Dot(a,b)>.97f,"continuous approach tangent "+e.id);}}
  Debug.Log("PVP_CITY_TESTS_OK checks="+checks+" directedPairs="+pairs+" lanes="+map.hdEdges.Length);
 }
 static float Distance(Vector3 p,Vector3 a,Vector3 b){var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude));}
}




