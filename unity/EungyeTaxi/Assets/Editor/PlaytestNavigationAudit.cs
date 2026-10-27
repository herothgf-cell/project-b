using System;
using System.Linq;
using UnityEngine;

// Read-only reproduction of the two lake-side playtest detours. This audits
// the authored directed graph; it does not certify real-world turn permissions.
public static class PlaytestNavigationAudit {
 static void Check(bool condition,string message){if(!condition)throw new Exception("PLAYTEST_NAV_AUDIT_FAIL "+message);Debug.Log("PASS "+message);}
 static int Edge(HdRoadGraph graph,string id){int index=Array.FindIndex(graph.edges,e=>e.id==id);Check(index>=0,"fixture exists "+id);return index;}
 static float Cost(HdRoadGraph graph,HdRoadGraph.RoutePath route){float result=route.path.length;for(int k=1;k<route.edges.Count;k++)if(graph.edges[route.edges[k]].change!=0)result+=12;return result;}
 static void Describe(HdRoadGraph graph,string name,HdRoadGraph.RoutePath route){Check(route!=null,name+" is reachable");Debug.Log("PLAYTEST_NAV_ROUTE "+name+" distance="+route.path.length.ToString("F2")+" cost="+Cost(graph,route).ToString("F2")+" edges="+string.Join(" | ",route.edges.Select(i=>graph.edges[i].id)));}
 public static void Run(){
  // Historical reproduction belongs to the unchanged imported baseline. The
  // current authored southbound corridor is covered by LatestFeedbackTests.SouthRoute.
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.baseline;var graph=new HdRoadGraph(map);
  int west=Edge(graph,"local:2103:5502:4463:8575"),blue=Edge(graph,"local:4711:8643:7608:9494"),northGoal=Edge(graph,"local:8265:6305:9056:10851");
  var first=graph.Route(west,0,northGoal,434.46582f);var blueFirst=graph.Via(west,0,blue,northGoal,434.46582f);
  Describe(graph,"image1 northbound S04",first);Describe(graph,"image1 through blue road",blueFirst);
  Check(!first.edges.Contains(blue)&&Cost(graph,first)+300<Cost(graph,blueFirst),"north detour is cheaper than blue road under current directed connections");
  int outerChange=Edge(graph,"change:local:7737:9374:7933:6566:local:7792:9389:7990:6566"),innerChange=Edge(graph,"change:local:7792:9389:7990:6566:local:7847:9404:8047:6565");
  Check(graph.edges[outerChange].next.All(i=>graph.edges[i].fromNode!=graph.edges[innerChange].fromNode),"3-to-2 change ends beyond the existing 2-to-1 departure");
  int outer=Edge(graph,"local:6266:11718:7563:9778:part:2"),inner=Edge(graph,"local:6355:11817:7686:9830:part:2"),southGoal=Edge(graph,"local:9032:10757:8270:6402");
  var fifth=graph.Route(outer,0,southGoal,520);var innerRoute=graph.Route(inner,0,southGoal,520);
  Describe(graph,"image5 outer lane after change window",fifth);Describe(graph,"image5 inner lane comparison",innerRoute);
  Check(fifth.edges.Any(i=>graph.edges[i].id=="local:7568:9586:4680:8738")&&Cost(graph,fifth)>Cost(graph,innerRoute)+900,"outer lane detours west while inner lane reaches northeast connector");
  Check(innerRoute.edges.Any(i=>graph.edges[i].id=="join:local:6355:11817:7686:9830:-1"),"short northeast connector is an authored inner-lane left turn");
  Check(graph.edges[outer].next.All(i=>graph.edges[i].change==0),"outer-lane screenshot position has no remaining authored lane change");
  Debug.Log("PLAYTEST_NAVIGATION_AUDIT_OK (directed-map audit, not manual driving)");
 }
}
