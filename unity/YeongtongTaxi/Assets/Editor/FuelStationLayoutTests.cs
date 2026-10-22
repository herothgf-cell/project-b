using System;
using System.IO;
using UnityEngine;

// Run against the actual generated hybrid input, without moving/clearing map buildings.
public static class FuelStationLayoutTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("FUEL_LAYOUT: "+message);}
 public static void Run(){
  var map=JsonUtility.FromJson<MapData>(File.ReadAllText("Assets/Resources/eungye-hybrid-map.json"));map.mapId="eungye";
  int roadCount=map.roads.Length,edgeCount=map.hdEdges.Length,buildingCount=map.buildings.Length;
  FuelStationLayout.Apply(map);var stations=FuelStationLayout.For(map);
  Check(stations.Count==3,"three game stations on real Eungye input");
  FuelStationLayout.Apply(map);Check(map.roads.Length==roadCount+12,"idempotent three surfaces per station and rest lot");
  Check(map.hdEdges.Length==edgeCount&&map.buildings.Length==buildingCount,"no graph jumps or building removal");
  var graph=new HdRoadGraph(map);var rest=FuelStationLayout.Rest(map);Check(rest!=null&&rest.name.Contains("게임시설"),"separate fictional rest lot");Check(Vector3.Distance(rest.pump,stations[2].pump)>60,"separate rest and fuel plots");Check(rest.approach.laneEdge==stations[2].approach.laneEdge&&rest.approach.laneAt>stations[2].approach.laneAt+63,"rest entry follows fuel exit on same shared lane");Check(rest.CanRefuel(rest.pump,rest.direction,0)&&!rest.CanRefuel(rest.entrance,rest.direction,0),"rest needs actual bay stop");foreach(var station in stations)Check(!station.CanRefuel(rest.pump,rest.direction,0),"rest never permits fueling");Debug.Log("REST_LAYOUT position="+rest.pump+" approach="+rest.approach.laneAt+" edge="+rest.approach.laneEdge);

  foreach(var station in stations){
   Check(station.name.Contains("게임시설"),"fictional status visible");
   Check(station.approach.graphStop,"entrance uses shared graph");
   Check(Vector3.Distance(graph.paths[station.approach.laneEdge].Position(station.approach.laneAt),station.approach.V())<.01f,"approach on graph");
   Check(Vector3.Distance(station.entrance,station.exit)>40,"separate entrance and exit");
   Check(Vector3.Dot(station.pump-station.entrance,TrafficSystem.Right(station.direction))>12,"forecourt right of arrival lane");
   Check(station.CanRefuel(station.pump,station.direction,0),"pump stopped allowed");
   Check(!station.CanRefuel(station.pump+Vector3.up*5,station.direction,0),"canopy height rejected");
   Check(!station.CanRefuel(station.entrance,station.direction,0),"roadside fuel rejected");
   Check(!station.CanRefuel(station.pump,-station.direction,0),"wrong heading rejected");
   Check(!station.CanRefuel(station.pump,station.direction,.56f),"moving fuel rejected");
   Check(!station.CanRefuel(station.pump+station.direction*3.1f,station.direction,0),"outside pump bay rejected");
   foreach(var stop in map.stops){Check(graph.Route(stop.laneEdge,stop.laneAt,station.approach.laneEdge,station.approach.laneAt)!=null,"reachable entrance "+station.name);Check(graph.Route(station.approach.laneEdge,station.approach.laneAt,stop.laneEdge,stop.laneAt)!=null,"reachable road exit");}
  }
  var second=JsonUtility.FromJson<MapData>(File.ReadAllText("Assets/Resources/eungye-hybrid-map.json"));second.mapId="eungye";FuelStationLayout.Apply(second);
  Check(second.roads.Length==roadCount+12&&!object.ReferenceEquals(stations,FuelStationLayout.For(second)),"per-map cache");
  var empty=new MapData{mapId="yeongtong"};FuelStationLayout.Apply(empty);Check(FuelStationLayout.For(empty).Count==0,"other maps keep original stops");
  Debug.Log("FUEL_STATION_LAYOUT_TEST_OK stations="+stations.Count+" originalRoads="+roadCount+" buildings="+buildingCount+" graph="+edgeCount);
 }
}
