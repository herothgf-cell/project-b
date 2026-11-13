using System;
using System.Linq;
using UnityEngine;

public static class FuelStationLineageTests {
 public static void Run(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);var before=p.Compose();FuelStationLayout.Apply(before);
  var d=p.document.Copy();var lane=d.lanes.Single(l=>l.id==J8066EastArmMapEdit.East);
  // Split upstream of the reviewed fuel position, including on an already
  // split map. The facility must retain its world position and a valid binding.
  if(lane.points.Length<6)throw new Exception("Missing split test geometry");
  RoadMapAuthoringOperations.SplitLane(d,lane.id,4);
  var after=RoadMapComposer.Compose(p.baseline,d);FuelStationLayout.Apply(after);var graph=new HdRoadGraph(after);
  foreach(var expected in FuelStationLayout.For(before)){
   var actual=FuelStationLayout.For(after).Single(s=>s.id==expected.id);
   if(Vector3.Distance(expected.pump,actual.pump)>.02f||Vector3.Distance(expected.entrance,actual.entrance)>.02f||Vector3.Distance(expected.exit,actual.exit)>.02f||Vector3.Distance(expected.approach.V(),actual.approach.V())>.02f)throw new Exception("Fuel geometry moved after upstream split: "+expected.id);
   if(Vector3.Distance(graph.paths[actual.approach.laneEdge].Position(actual.approach.laneAt),actual.approach.V())>.01f)throw new Exception("Fuel stop binding lost after split");
  }
  Debug.Log("FUEL_STATION_LINEAGE_OK");
 }
}
