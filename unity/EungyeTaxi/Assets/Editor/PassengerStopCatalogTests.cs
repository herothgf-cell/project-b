using System;
using System.Linq;
using UnityEngine;

public static class PassengerStopCatalogTests {
 static int checks;
 static void Check(bool okay,string message){checks++;if(!okay)throw new Exception("PASSENGER_STOP_CATALOG: "+message);}
 static MapData Load(){return ProjectMapStore.Load(ProjectMapBuild.Root).Compose();}
 public static void Run(){
  checks=0;var map=Load();var original=map.stops.ToArray();var originalJson=original.Select(stop=>JsonUtility.ToJson(stop)).ToArray();var snapshot=JsonUtility.ToJson(map.stops[0]);int roadCount=map.roads.Length;
  PassengerStopCatalog.Apply(map);var catalog=PassengerStopCatalog.For(map);
  Check(catalog.Stops.Length>=16,"at least four validated explicit game bays: "+string.Join(" / ",catalog.Rejected));
  Check(catalog.LegacyCount==12&&map.stops.Length==12,"original scenario indexing remains twelve");
  Check(catalog.Stops.Length==39&&catalog.Rejected.Count==0,"all 24 original stops and 15 opposite bays admitted: "+string.Join(" / ",catalog.Rejected));
  foreach(var id in new[]{"S01","S02","S03","S06","S07","S09","EG-GAME-09","EG-GAME-10"}){
   int index=catalog.IndexOf(id);
   Check(index>=0&&catalog.IndexOf(id+"-B")>=0&&catalog.OppositeIndex(index)==catalog.IndexOf(id+"-B"),"requested opposite bay "+id);
  }
  foreach(var entry in catalog.Entries.Where(e=>e.Category=="opposite-kerb")){
   int originalIndex=catalog.OppositeIndex(entry.Index);
   Check(originalIndex>=0&&catalog.OppositeIndex(originalIndex)==entry.Index,"both directions resolve to the same pair "+entry.StopId);
   var originalStop=catalog.Stops[originalIndex];
   Check(Vector3.Dot(originalStop.Direction,entry.Stop.Direction)<-.88f,"opposite headings "+entry.StopId);
  }
  for(int i=0;i<12;i++){
   Check(JsonUtility.ToJson(original[i])==originalJson[i],"source legacy stop unchanged "+i);
   if(ReferenceEquals(original[i],catalog.Stops[i]))continue;
   var source=original[i];var corrected=catalog.Stops[i];
   Check(i==8&&map.mapId=="eungye"&&source.name=="은계 승하차 9"&&Mathf.Abs(source.width-3)<.01f&&Mathf.Abs(corrected.width-6.6f)<.01f,"only documented S09 sidewalk correction may clone a legacy stop");
   Check(Vector3.Distance(source.V(),corrected.V())<.001f&&Vector3.Distance(source.Direction,corrected.Direction)<.001f&&source.laneEdge==corrected.laneEdge&&Mathf.Abs(source.laneAt-corrected.laneAt)<.001f&&source.graphStop==corrected.graphStop,"S09 correction preserves vehicle route anchor");
   Check(catalog.LegacyS09SidewalkRepaired&&ProjectMapScene.OnArea(map,corrected.Passenger,true),"corrected S09 passenger anchor is on repaired game-only sidewalk");
   foreach(float along in new[]{-6.5f,0,6.5f})foreach(float side in new[]{-1f,0,1f})Check(!ProjectMapScene.OnArea(map,corrected.Kerb+corrected.Direction*along+TrafficSystem.Right(corrected.Direction)*side,true),"S09 actual stopped body clear of raised sidewalk");
   foreach(float along in new[]{-8f,0,8})foreach(float side in new[]{-.7f,0,.7f})Check(ProjectMapScene.OnArea(map,corrected.Passenger+corrected.Direction*along+TrafficSystem.Right(corrected.Direction)*side,true),"S09 continuous full1.5m passenger strip");
  }
  Check(JsonUtility.ToJson(map.stops[0])==snapshot,"legacy position untouched");
  Check(map.roads.Length==roadCount+catalog.Stops.Length-12+catalog.Alternatives.Count(),"one fictional asphalt pocket per additional or alternative stop");
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
