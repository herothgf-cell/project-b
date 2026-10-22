using System;
using UnityEngine;
public static class OfferAccessTests {
 public static void Run(){
  var map=JsonUtility.FromJson<MapData>(Resources.Load<TextAsset>("eungye-hybrid-map").text);var graph=new HdRoadGraph(map);int samples=0;
  foreach(var from in map.stops)foreach(var to in map.stops)foreach(bool kerb in new[]{false,true}){
   var p=kerb?from.Kerb:graph.paths[from.laneEdge].Position(from.laneAt);var dir=kerb?from.Direction:graph.paths[from.laneEdge].Tangent(from.laneAt);float at;int edge=graph.Nearest(p,dir,out at);
   float actual=PassengerJobs.DirectedDistance(graph,p,dir,to);var expected=graph.Route(edge,at,to.laneEdge,to.laneAt);
   if(edge==to.laneEdge&&Mathf.Abs(at-to.laneAt)<.25f){if(actual!=0)throw new Exception("same curb must be zero");}
   else if(expected==null){if(!float.IsPositiveInfinity(actual))throw new Exception("unreachable pickup");}
   else if(Mathf.Abs(actual-expected.path.length)>.01f)throw new Exception("pickup rank differs from shared route");samples++;
  }
  if(!float.IsPositiveInfinity(PassengerJobs.DirectedDistance(graph,new Vector3(-10000,0,-10000),Vector3.forward,map.stops[0])))throw new Exception("off-road must not invent route");
  var parking=map.stops[0];var pickup=map.stops[11];float directed=PassengerJobs.DirectedDistance(graph,parking.Kerb,parking.Direction,pickup);if(directed<Vector3.Distance(parking.Kerb,pickup.Kerb)*2)throw new Exception("known parking detour must not rank as straight-line proximity");
  Debug.Log("OFFER_ACCESS_TESTS_OK directed pairs="+samples);
 }
}
