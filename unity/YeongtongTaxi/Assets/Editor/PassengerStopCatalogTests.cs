using System;
using System.Linq;
using UnityEngine;

public static class PassengerStopCatalogTests {
 static int checks;
 static void Check(bool okay,string message){checks++;if(!okay)throw new Exception("PASSENGER_STOP_CATALOG: "+message);}
 static MapData Load(){return ProjectMapStore.Load(ProjectMapBuild.Root).Compose();}
 public static void Run(){
  checks=0;var map=Load();var original=map.stops.ToArray();var snapshot=JsonUtility.ToJson(map.stops[0]);int roadCount=map.roads.Length;
  PassengerStopCatalog.Apply(map);var catalog=PassengerStopCatalog.For(map);
  Check(catalog.Stops.Length>=16,"at least four validated explicit game bays: "+string.Join(" / ",catalog.Rejected));
  Check(catalog.LegacyCount==12&&map.stops.Length==12,"original scenario indexing remains twelve");
  for(int i=0;i<12;i++)Check(ReferenceEquals(original[i],catalog.Stops[i]),"legacy reference preserved "+i);
  Check(JsonUtility.ToJson(map.stops[0])==snapshot,"legacy position untouched");
  Check(map.roads.Length==roadCount+catalog.Stops.Length-12,"one fictional asphalt pocket per additional stop");
  Check(catalog.Entries.Select(e=>e.StopId).Distinct().Count()==catalog.Stops.Length,"stable distinct IDs");
  var graph=new HdRoadGraph(map);int routes=0;
  foreach(var entry in catalog.Entries.Skip(12)){
   var stop=entry.Stop;Check(entry.EvidenceStatus=="game-only-field-unverified","no legality claim");
   Check(stop.hasPassengerAnchor&&ProjectMapScene.OnArea(map,stop.Passenger,true),"passenger on constructed continuous sidewalk "+entry.StopId);
   Check(!ProjectMapScene.OnArea(map,stop.Kerb,true),"vehicle pocket has no remaining raised sidewalk "+entry.StopId);
   foreach(float a in new[]{-2f,0,2})foreach(float b in new[]{-.95f,0,.95f})Check(!ProjectMapScene.OnArea(map,stop.Kerb+stop.Direction*a+TrafficSystem.Right(stop.Direction)*b,true),"vehicle footprint clear of raised sidewalk");
   foreach(float a in new[]{-.7f,0,.7f})foreach(float b in new[]{-.7f,0,.7f})Check(ProjectMapScene.OnArea(map,stop.Passenger+stop.Direction*a+TrafficSystem.Right(stop.Direction)*b,true),"at least 1.5m pedestrian footprint");
   Check(stop.CanStop(stop.Kerb,stop.Direction,0)&&!stop.CanStop(stop.Kerb,-stop.Direction,0),"same-direction bay interaction");
   float at;Check(graph.Nearest(stop.Kerb,stop.Direction,out at)>=0,"bay can depart via directed lane");
   foreach(var target in catalog.Stops){if(target==stop)continue;float distance;Check(PassengerStopCatalog.Reachable(graph,stop,target,out distance)&&distance>0,"outward route "+entry.StopId);Check(PassengerStopCatalog.Reachable(graph,target,stop,out distance)&&distance>0,"inward route "+entry.StopId);routes+=2;}
  }
  int count=map.roads.Length;PassengerStopCatalog.Apply(map);Check(map.roads.Length==count&&ReferenceEquals(catalog,PassengerStopCatalog.For(map)),"idempotent install");
  Check(PassengerStopCatalog.RepeatPenalty(1,2,1,2)>PassengerStopCatalog.RepeatPenalty(3,4,1,2),"recent pair deprioritized");
  var first=catalog.Entries[12];var disabled=Load();var lane=Array.Find(disabled.hdEdges,e=>e.id==first.LaneId);lane.disabled=true;lane.next=new int[0];foreach(var e in disabled.hdEdges)e.next=e.next.Where(n=>disabled.hdEdges[n]!=lane).ToArray();Check(PassengerStopCatalog.For(disabled).IndexOf(first.StopId)<0,"disabled edited approach hides candidate");
  var blocked=Load();var blocker=new MapBuilding{id="catalog-test-blocker",height=4,points=ProjectMapScene.Rectangle(first.Stop.Passenger,first.Stop.Direction,4,4)};blocked.buildings=blocked.buildings.Concat(new[]{blocker}).ToArray();Check(PassengerStopCatalog.For(blocked).IndexOf(first.StopId)<0,"new building invalidates unsupported bay");
  var crossing=Load();crossing.osmCrossings=(crossing.osmCrossings??new OsmCrossing[0]).Concat(new[]{new OsmCrossing{x=first.Stop.Kerb.x,z=first.Stop.Kerb.z}}).ToArray();Check(PassengerStopCatalog.For(crossing).IndexOf(first.StopId)<0,"new crossing hides candidate");
  Debug.Log("PASSENGER_STOP_CATALOG_TESTS_OK checks="+checks+" added="+(catalog.Stops.Length-12)+" directedRoutes="+routes+" rejected="+string.Join(" / ",catalog.Rejected));
 }
}
