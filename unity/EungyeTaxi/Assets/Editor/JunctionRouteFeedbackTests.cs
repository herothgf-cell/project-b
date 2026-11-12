using System;
using System.Linq;
using UnityEngine;

public static class JunctionRouteFeedbackTests {
 public static void VerifyMigration(){
  MapCatalog.LockedRegion="eungye";var map=MapCatalog.Load();
  var method=typeof(BusinessProgress).GetMethod("UpgradeEungyeApproachCheckpoint",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
  foreach(int revision in new[]{14,15,16,17}){
   var saved=new LiveCampaignCheckpoint{mapRevision="은계 프로젝트 지도 · r"+revision,preferFastest=false,jobs=new PassengerJobsSnapshot{navigationCaptured=true,navAvoidEdge=2000,navObservedEdge=2001,navPassedObservedEdge=2002}};
   method.Invoke(null,new object[]{saved,map});
   if(saved.mapRevision!=map.editorRevision||saved.jobs.navigationCaptured||saved.jobs.navAvoidEdge!=-1||saved.jobs.navObservedEdge!=-1||saved.jobs.navPassedObservedEdge!=-1||saved.preferFastest)throw new Exception("r18 navigation migration failed from "+revision);
  }
  Debug.Log("JUNCTION_MIGRATION_OK versions=4");
 }
 public static void Run(){Verify();VerifyDestination();J7917LaneMapEdit.RunChecks();PlaytestBalanceTests.Run();Debug.Log("JUNCTION_FEEDBACK_REGRESSION_OK");}
 public static void Verify(){Verify(ProjectMapStore.Load(ProjectMapBuild.Root));}
 static void Verify(ProjectMapPackage package){
  var composed=package.Compose();EungyeConnectivityRepair.Apply(composed,package.baseline);var g=new HdRoadGraph(composed);
  foreach(var pair in new[]{new[]{"reviewed:J7917:middle:local:7848:3507:7812:2752:part:1","join:local:7924:3503:7887:2749:-1"},new[]{"reviewed:J7917:middle:local:8039:2741:8076:3496:part:1","A2218G611104:0"}}){
   int start=Array.FindIndex(g.edges,e=>e.id==pair[0]),turn=Array.FindIndex(g.edges,e=>e.id==pair[1]);var r=g.Route(start,1,turn,g.paths[turn].length);
   Debug.Log("JUNCTION_FEEDBACK_ROUTE "+pair[0]+" distance="+(r==null?-1:r.path.length));
   if(r==null||r.path.length>150)throw new Exception("Adjacent middle lane cannot prepare for local left turn: "+pair[0]);
  }
  Debug.Log("JUNCTION_FEEDBACK_OK");
 }
 public static void VerifyDestination(){
  var p=ProjectMapStore.Load(ProjectMapBuild.Root);var map=p.Compose();EungyeConnectivityRepair.Apply(map,p.baseline);var g=new HdRoadGraph(map);
  var goal=map.stops.First(s=>s.name.Contains("국도42")&&s.name.EndsWith("1"));
  foreach(var pose in new[]{new Vector3(791,0,423),new Vector3(797,0,470)}){
   float at;int start=g.Nearest(pose,Vector3.back,out at);var r=g.Route(start,at,goal.laneEdge,goal.laneAt);
   Debug.Log("JUNCTION_DESTINATION_ROUTE pose="+pose+" distance="+(r==null?-1:r.path.length));
   if(r==null||r.path.length>1150||!r.edges.Any(i=>g.edges[i].id=="A2218G611104:0"))throw new Exception("Westbound stop still bypasses J7973 northbound left");
   if(Vector3.Distance(r.path.Position(r.path.length),g.paths[goal.laneEdge].Position(goal.laneAt))>.01f)throw new Exception("Destination changed");
  }
  Debug.Log("JUNCTION_DESTINATION_OK");
 }
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var p=ProjectMapStore.Load(root);
  if(p.document.revision!=17)throw new Exception("Expected r17");
  p.document=PlaytestLaneChangeRepair.ApplyJunctionFeedback(p);p.document.revision=18;p.document.savedAt=DateTime.UtcNow.ToString("o");p.document.projectRevision="";
  p.contentHash=ProjectMapPackage.Digest(JsonUtility.ToJson(p.document));Verify(p);ProjectMapStore.Save(root,p,token);ProjectMapBuild.Prepare();Debug.Log("JUNCTION_FEEDBACK_SAVED "+p.contentHash);
 }
 public static void Diagnose(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();EungyeConnectivityRepair.Apply(map,package.baseline);var g=new HdRoadGraph(map);
  var goal=map.stops.First(s=>s.name.Contains("국도42")&&s.name.EndsWith("1"));
  Debug.Log("JUNCTION_GOAL "+g.edges[goal.laneEdge].id+" at="+goal.laneAt);
  foreach(string id in new[]{"A2218G611104:0","A2218G611447:0","local:7963:2745:8000:3499","local:8039:2741:8076:3496"}){int i=Array.FindIndex(g.edges,e=>e.id==id);var r=g.Route(i,0,goal.laneEdge,goal.laneAt);Debug.Log("JUNCTION_GOAL_FROM "+id+" length="+(r==null?-1:r.path.length)+" chain="+(r==null?"none":string.Join(" | ",r.edges.Select(e=>g.edges[e].id))));}
  foreach(float z in new[]{470f,440f,423f})foreach(float x in new[]{785f,791f,797f,800f}){
   float at;int start=g.Nearest(new Vector3(x,0,z),Vector3.back,out at);if(start<0)continue;
   var r=g.Route(start,at,goal.laneEdge,goal.laneAt);
   Debug.Log("JUNCTION_ROUTE pose="+x+","+z+" start="+g.edges[start].id+" distance="+(r==null?-1:r.path.length)+" turns="+(r==null?"none":string.Join(" | ",r.edges.Where(i=>g.edges[i].turn!=0).Select(i=>g.edges[i].id))));
  }
  foreach(var e in g.edges.Where(e=>!e.disabled&&e.turn==-1)){
   var a=e.points[0].V();if(Vector3.Distance(a,new Vector3(797.33f,0,374.4f))<65)Debug.Log("JUNCTION_LEFT "+e.id+" start="+a+" end="+e.points.Last().V());
  }
 }
}
