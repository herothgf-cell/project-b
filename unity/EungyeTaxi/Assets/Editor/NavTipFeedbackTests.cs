using System;
using System.Linq;
using UnityEngine;
public static class NavTipFeedbackTests {
 public static void RunFinal(){Route();EungyeConnectivityRepairTests.Run();UturnBlinkFeedbackTests.RunFinal();ProjectMapBuild.RunTests();Debug.Log("NAV_TIP_FINAL_EDITOR_OK");}
 public static void Diagnose(){
  MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();var stops=PassengerStopCatalog.For(map).Stops;var g=new HdRoadGraph(map);
  foreach(var s in map.stops)Debug.Log("FEEDBACK_STOP "+s.name+" pos="+s.V()+" lane="+g.edges[s.laneEdge].id+" at="+s.laneAt);
  var a=stops.First(s=>s.name.Contains("국도42")&&s.name.EndsWith("1"));var b=stops.First(s=>s.name=="게임 승하차 2");
  var route=g.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);Debug.Log("FEEDBACK_ROUTE "+route.path.length+" "+string.Join(" | ",route.edges.Select(i=>g.edges[i].id)));
  Debug.Log("START_DETAIL "+g.edges[a.laneEdge].id+" at="+a.laneAt+" offset="+g.edges[a.laneEdge].sourceOffset);
  foreach(var edge in g.edges.Where(e=>e.id.StartsWith(EungyeConnectivityRepair.Prefix)&&e.change!=0))Debug.Log("CHANGE_DETAIL "+edge.id+" start="+edge.points.First().V()+" end="+edge.points.Last().V()+" next="+string.Join(",",edge.next.Select(k=>g.edges[k].id)));
  for(int i=0;i<g.edges.Length;i++){if(g.edges[i].disabled||g.edges[i].change!=0)continue;float err;float at=g.paths[i].Project(a.V(),out err);if(err>15||Vector3.Dot(g.paths[i].Tangent(at),a.Direction)<.6f)continue;var alt=g.Route(i,at,b.laneEdge,b.laneAt);Debug.Log("FEEDBACK_NEAR_START err="+err+" lane="+g.edges[i].id+" at="+at+" route="+(alt==null?-1:alt.path.length));}
  for(int i=0;i<g.edges.Length;i++){var e=g.edges[i];if(g.paths[i].points.Any(q=>q.x<225&&q.x>0&&q.z>470&&q.z<575))Debug.Log("WEST_EDGE "+e.id+" n="+e.number+" turn="+e.turn+" disabled="+e.disabled+" start="+g.paths[i].points.First()+" end="+g.paths[i].points.Last()+" len="+g.paths[i].length+" next="+string.Join(",",e.next.Select(k=>g.edges[k].id)));}
 }
 public static void Route(){Diagnose();var m=MapCatalog.Load();var stops=PassengerStopCatalog.For(m).Stops;var g=new HdRoadGraph(m);var a=stops.First(s=>s.name.Contains("국도42")&&s.name.EndsWith("1"));var b=stops.First(s=>s.name=="게임 승하차 2");var r=g.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);if(r==null||r.path.length>1000)throw new Exception("FEEDBACK_ROUTE_FAIL avoid northern loop on west-to-south drop");Debug.Log("FEEDBACK_ROUTE_OK distance="+r.path.length);}
}
