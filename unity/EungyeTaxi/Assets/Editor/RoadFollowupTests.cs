using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class RoadFollowupTests {
 static void Check(bool value,string message){if(!value)throw new Exception("ROAD_FOLLOWUP_FAIL "+message);Debug.Log("PASS "+message);}
 public static void Run(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();
  var catalog=JsonUtility.FromJson<ReviewedRoadFacilities.Catalog>(File.ReadAllText(Path.Combine(ProjectMapBuild.Root,"config/maps/eungye-road-facilities.json")));
  var layout=ReviewedRoadFacilities.Plan(map,catalog);
  var rails=layout.barriers.Where(l=>l.facilityId=="J7973-reviewed-median").ToArray();
  Check(rails.Length>20,"observed median present on both straight approaches");
  var graph=new HdRoadGraph(map);
  foreach(var rail in rails){
   var centre=(rail.a+rail.b)*.5f;
   Check(centre.z<344||centre.z>405,"median leaves junction and crossings open");
   for(int i=0;i<graph.paths.Length;i++){float error;graph.paths[i].Project(centre,out error);if(error<=1.3f)throw new Exception("ROAD_FOLLOWUP_FAIL median intersects "+graph.edges[i].id+" at "+centre+" clearance="+error);}
  }
  var junction=map.junctionRules.Single(j=>j.id=="J5750_2576");
  Check(junction.signalMode==0&&junction.crosswalk,"reviewed unsignalized crossing keeps crosswalk without fictitious signals");
  Debug.Log("ROAD_FOLLOWUP_TESTS_OK rails="+rails.Length);
 }
 public static void Preview(){PlaytestDirectTurnTests.Preview();Run();}
}
