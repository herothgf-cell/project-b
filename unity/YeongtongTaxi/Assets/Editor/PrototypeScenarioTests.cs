using System;
using UnityEngine;
public static class PrototypeScenarioTests {
 static void Check(bool value,string message){if(!value)throw new Exception("SCENARIO: "+message);}
 public static void Run(){var map=JsonUtility.FromJson<MapData>(Resources.Load<TextAsset>("eungye-hybrid-map").text);var hd=new HdRoadGraph(map);var courses=PrototypeScenario.Build(hd,map.stops);foreach(var s in courses)Debug.Log("SCENARIO_ROUTE "+s.id+" pickup="+(s.pickup+1)+" drop="+(s.drop+1)+" connector="+s.connector+" length="+s.route.path.length+" alternate="+s.alternate);
  Check(courses.Count==9,"all nine valid directed normal-map courses; actual="+courses.Count);
  foreach(var s in courses){Check(PrototypeScenario.Valid(s.route,s.connector),s.id+" approach/exit distance and no repeated mandatory connector");foreach(int e in s.route.edges)Check(!hd.edges[e].disabled,s.id+" never disabled");Check(s.route.edges[s.route.edges.Count-1]==map.stops[s.beforePickup?s.pickup:s.drop].laneEdge,s.id+" actual curb destination lane");if(s.id=="PS08")Check(s.alternative!=null&&!s.alternative.edges.Contains(s.connector)&&s.alternate>=0&&s.alternate!=s.alternative.edges[0]&&hd.edges[s.alternate].id.StartsWith("join:"),"PS08 distinct interior directed bypass");if(s.id=="PS09")Check(hd.edges[s.connector].turn==1,"PS09 actual right turn");}
  var exit=courses.Find(s=>s.id=="PS08");foreach(var point in new[]{exit.route.path.Position(0),map.stops[exit.pickup].Kerb}){float at;int start=hd.Nearest(point+Vector3.up*.65f,exit.route.path.Tangent(0),out at);var to=map.stops[exit.drop];var detour=hd.Via(start,at,exit.alternate,to.laneEdge,to.laneAt,PrototypeScenario.AvoidWeights(hd,exit.connector));Debug.Log("SCENARIO_DETOUR start="+start+" at="+at+" via="+exit.alternate+" goal="+to.laneEdge+" at="+to.laneAt+" route="+(detour==null?"null":string.Join(",",detour.edges)));Check(detour!=null&&!detour.edges.Contains(exit.connector),"runtime nearest pickup/kerb detour is valid");}
  Debug.Log("PROTOTYPE_SCENARIO_TESTS_OK");
 }
}
