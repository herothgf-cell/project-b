using System.Linq;
using UnityEngine;
// Read-only native graph inventory; does not infer runtime parking/spawn poses.
public static class PvpSimulationRouteInventory {
 public static void ValidateStopAndList(){PvpSimulationStopEvidenceTests.Run();Run();}
 public static void Run(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var graph=new HdRoadGraph(map);var stops=PassengerStopCatalog.For(map).Stops;
  for(int pickup=0;pickup<stops.Length;pickup++){
   var p=stops[pickup];var drops=Enumerable.Range(0,stops.Length).Where(i=>i!=pickup).Select(i=>new{i,route=graph.Route(p.laneEdge,p.laneAt,stops[i].laneEdge,stops[i].laneAt)}).Where(x=>x.route!=null&&x.route.path.length>80).OrderBy(x=>x.route.path.length).Take(3);
   foreach(var drop in drops)Debug.Log("NATIVE_ROUTE_INVENTORY pickup="+pickup+" drop="+drop.i+" rideMetres="+drop.route.path.length+" pickupName="+p.name+" dropName="+stops[drop.i].name);
  }
 }
}
