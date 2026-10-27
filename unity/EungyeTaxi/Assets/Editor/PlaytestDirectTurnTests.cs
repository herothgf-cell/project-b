using System;
using System.Linq;
using UnityEngine;

// Regression for the newly reported J7973 -> J8066 service route.
// Does not infer real-world permissions; every movement must be authored.
public static class PlaytestDirectTurnTests {
 static void Check(bool value,string message){if(!value)throw new Exception("PLAYTEST_DIRECT_TURN_FAIL "+message);Debug.Log("PASS "+message);}
 static int Edge(HdRoadGraph graph,string id){int i=Array.FindIndex(graph.edges,e=>e.id==id);Check(i>=0,"fixture "+id);return i;}
 static void Direct(HdRoadGraph graph,string source,float at,string firstTurn){
  int start=Edge(graph,source),goal=Edge(graph,"local:8265:6305:9056:10851");
  var route=graph.Route(start,at,goal,434.46582f);Check(route!=null,"reported destination is reachable");
  Debug.Log("PLAYTEST_DIRECT_ROUTE "+source+" distance="+route.path.length+" edges="+string.Join(" | ",route.edges.Select(i=>graph.edges[i].id)));
  Check(route.edges.Contains(Edge(graph,firstTurn)),"uses direct reported turn "+firstTurn);
  Check(route.edges.Contains(Edge(graph,"join:local:8131:3977:8203:6159:1")),"turns right at J8066 instead of northern block loop");
  Check(!route.edges.Any(i=>graph.edges[i].ruleId=="J7801_9629"),"never travels to the next northern junction to recover the missed right turn");
  Check(graph.edges[route.edges.First(i=>graph.edges[i].turn!=0)].id==firstTurn,"first junction maneuver is the requested direct turn");
  Check(route.path.length<1500,"reported route avoids the southern and northern block loops");
 }
 public static void Run(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var graph=new HdRoadGraph(package.Compose());
  Direct(graph,"A2218G611417:3",8,"A2218G611452:0");
  Direct(graph,"A2218G611886:0",0,"A2218G611452:0");
  var segment=RoadMapAuthoringOperations.LineageSegment(package.document,"local:8017:3985:8089:6162:part:2",40);
  Direct(graph,segment.id,40-segment.sourceOffset,"join:local:8131:3977:8203:6159:1");
  Debug.Log("PLAYTEST_DIRECT_TURNS_OK");
 }
 public static void Preview(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);string before=JsonUtility.ToJson(package.document);
  var evidence=new RoadMapEvidence{state="observed",url="https://example.invalid/test-fixture",checkedAt="2026-10-27",note="Synthetic test permission; never persisted",appearanceConfirmed=true};
  var repaired=PlaytestLaneChangeRepair.Apply(package,evidence,evidence);
  Check(JsonUtility.ToJson(package.document)==before,"preview leaves source document unchanged");
  var graph=new HdRoadGraph(RoadMapComposer.Compose(package.baseline,repaired));
  Direct(graph,"A2218G611417:3",8,"A2218G611452:0");
  Direct(graph,"A2218G611886:0",0,"A2218G611452:0");
  // After splitting, resolve the actual downstream segment owning the same
  // physical starting distance, preserving lane identity rather than snapping.
  var segment=RoadMapAuthoringOperations.LineageSegment(repaired,"local:8017:3985:8089:6162:part:2",40);
  Direct(graph,segment.id,40-segment.sourceOffset,"join:local:8131:3977:8203:6159:1");
  Check(repaired.lanes.Count(l=>l.id.StartsWith(PlaytestLaneChangeRepair.Prefix))==14,"twelve northbound and two sequential west-approach lane changes");
  bool rejected=false;try{PlaytestLaneChangeRepair.Apply(package,new RoadMapEvidence(),evidence);}catch(ArgumentException){rejected=true;}Check(rejected,"missing separator evidence is rejected");
  Debug.Log("PLAYTEST_LANE_CHANGE_PREVIEW_OK");
 }
}
