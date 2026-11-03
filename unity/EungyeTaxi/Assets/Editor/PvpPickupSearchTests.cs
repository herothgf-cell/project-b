using System;using UnityEngine;
public static class PvpPickupSearchTests {
 public static void Run(){var map=PvpCityMap.Create();var graph=new HdRoadGraph(map);int checks=0;
  foreach(var start in map.stops){var search=graph.SearchDistances(start.laneEdge,start.laneAt);foreach(var goal in map.stops){var expected=graph.Route(start.laneEdge,start.laneAt,goal.laneEdge,goal.laneAt);var actual=search.RouteTo(goal.laneEdge,goal.laneAt);if((expected==null)!=(actual==null)||expected!=null&&(Mathf.Abs(expected.path.length-actual.path.length)>.01f||!Same(expected,actual)))throw new Exception("Pickup search changed native route "+start.name+" -> "+goal.name);checks++;}
   var back=search.RouteTo(start.laneEdge,Mathf.Max(0,start.laneAt-1));var loop=graph.Route(start.laneEdge,start.laneAt,start.laneEdge,Mathf.Max(0,start.laneAt-1));if((back==null)!=(loop==null)||back!=null&&!Same(back,loop))throw new Exception("Pickup search lost directed same-lane loop");checks++;
  }Debug.Log("PVP_PICKUP_SEARCH_OK checks="+checks);
 }
 static bool Same(HdRoadGraph.RoutePath a,HdRoadGraph.RoutePath b){if(a.edges.Count!=b.edges.Count)return false;for(int i=0;i<a.edges.Count;i++)if(a.edges[i]!=b.edges[i])return false;return true;}
}
