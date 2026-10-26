using System;
using System.Linq;
using UnityEngine;

// Uses the shipped Eungye package rather than a synthetic lane fixture.
public static class NavigationStopRegressionTests {
 const string BeforeId="local:7003:14854:5837:14446";
 const string SourceId="local:7003:14854:5837:14446:part:1";
 const string GoalId="local:7023:14796:5857:14389:part:3";
 const string ChangeId="change:local:7003:14854:5837:14446:local:7023:14796:5857:14389";
 static void Check(bool value,string name){if(!value)throw new Exception("NAVIGATION_STOP_REGRESSION_FAIL "+name);Debug.Log("PASS "+name);}
 static MapData Map(){var package=ProjectMapStore.Load(ProjectMapBuild.Root);return RoadMapComposer.Compose(package.baseline,package.document);}
 static int Edge(MapData map,string id){return Array.FindIndex(map.hdEdges,e=>e.id==id);}
 public static void Run(){
  var map=Map();var original=map.stops[8];var catalog=PassengerStopCatalog.For(map);var stop=catalog.Stops[8];
  Check(stop.name=="은계 승하차 9"&&stop.laneEdge==Edge(map,SourceId),"S09 uses the original directed outer lane");
  var right=TrafficSystem.Right(stop.Direction);var curbDistance=Vector3.Dot(stop.Kerb-stop.V(),right);
  Check(curbDistance>1.6f&&curbDistance<2.1f,"S09 taxi target is near actual right curb, not lane center");
  Debug.Log("S09_POSITION kerb="+stop.Kerb.ToString("F5")+" passenger="+stop.Passenger.ToString("F5")+" source="+stop.V().ToString("F5"));
  Check(Vector2.Distance(new Vector2(stop.Kerb.x,stop.Kerb.z),new Vector2(675.13423f,1478.49262f))<.05f&&Vector2.Distance(new Vector2(stop.Passenger.x,stop.Passenger.z),new Vector2(674.35916f,1480.71112f))<.05f,"S09 taxi and waiting passenger match measured curb-side positions");
  Check(Mathf.Abs(original.width-3)<.01f,"canonical package stop geometry is not mutated");
  Check(stop.CanStop(stop.Kerb+Vector3.up*.65f,stop.Direction,0),"S09 right-curb stop is valid");
  Check(!stop.CanStop(stop.V()+Vector3.up*.65f,stop.Direction,0),"S09 lane-center near miss does not count");
  Check(!stop.CanStop(stop.Kerb+right*1.3f+Vector3.up*.65f,stop.Direction,0),"S09 beyond curb tolerance does not count");
  Check(!stop.CanStop(stop.Kerb+Vector3.up*.65f,-stop.Direction,0),"S09 wrong-way stop does not count");
  var edited=Map();edited.stops[8].x+=5;var editedStop=PassengerStopCatalog.For(edited).Stops[8];Check(Mathf.Abs(editedStop.width-3)<.01f&&Mathf.Abs(editedStop.x-edited.stops[8].x)<.01f,"custom-edited S09 geometry is respected");
  var taxi=new GameObject("nav regression taxi");var root=new GameObject("nav regression");try{
   var nav=root.AddComponent<RoadNavigation>();var graph=new HdRoadGraph(map);int beforeIndex=Edge(map,BeforeId),sourceIndex=Edge(map,SourceId),goalIndex=Edge(map,GoalId),changeIndex=Edge(map,ChangeId);Check(beforeIndex>=0&&sourceIndex>=0&&goalIndex>=0&&changeIndex>=0,"actual directed fixture edges resolve by stable ID");var before=graph.paths[beforeIndex];taxi.transform.SetPositionAndRotation(before.Position(before.length-2)+Vector3.up*.65f,Quaternion.LookRotation(before.Tangent(before.length-2)));
   nav.Build(map,taxi.transform);nav.SetHdDestination(goalIndex,10);
   Check(nav.hdRoute!=null&&nav.hdRoute.edges.Contains(changeIndex),"actual legal lane-change connector is in the route");
   var source=nav.hd.paths[sourceIndex];taxi.transform.SetPositionAndRotation(source.Position(16)+Vector3.up*.65f,Quaternion.LookRotation(source.Tangent(16)));nav.Refresh();
   Check(nav.hdRoute!=null&&nav.hdRoute.edges.Contains(changeIndex)&&nav.path.Count>1,"guide survives source-side crossing of legal lane-change connector");
   taxi.transform.SetPositionAndRotation(source.Position(16)+Vector3.up*.65f,Quaternion.LookRotation(-source.Tangent(16)));nav.Refresh();
   Check(nav.hdRoute==null||!nav.hdRoute.edges.Contains(changeIndex),"wrong-way heading cannot retain lane-change guidance");
  }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(taxi);}
  Debug.Log("NAVIGATION_STOP_REGRESSION_OK");
 }
}
