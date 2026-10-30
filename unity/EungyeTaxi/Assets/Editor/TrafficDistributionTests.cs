using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
public static class TrafficDistributionTests {
 public static void Run(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var root=new GameObject("canonical traffic distribution test");
  try{
   var world=root.AddComponent<WorldBuilder>();world.BuildStatic(map);
   var traffic=root.AddComponent<TrafficSystem>();traffic.Build(map,true);Physics.SyncTransforms();bool safe;int visible,cells;
   string result=TrafficDistributionSnapshot.Inspect(traffic,out safe,out visible,out cells);
   var oldCells=new HashSet<Vector2Int>();var currentCells=new HashSet<Vector2Int>();var graph=traffic.GetComponent<DistrictTraffic>().graph;
   foreach(var stop in map.stops){var p=graph.paths[stop.laneEdge].Position(stop.laneAt);oldCells.Add(Cell(p));}
   foreach(var car in traffic.cars)if(car.Visible)currentCells.Add(Cell(car.route.Position(car.distance)));
   int retained=0,newCells=0;foreach(var cell in oldCells)if(currentCells.Contains(cell))retained++;foreach(var cell in currentCells)if(!oldCells.Contains(cell))newCells++;
   result+=" oldCells="+oldCells.Count+" retainedOldCells="+retained+" newCells="+newCells;
   Directory.CreateDirectory("../../artifacts");File.WriteAllText("../../artifacts/traffic-distribution-editor.txt",result+"\nsafe="+safe);Debug.Log("TRAFFIC_DISTRIBUTION "+result);
   if(visible<48||cells<8||retained<12||newCells<2||!safe||traffic.cars.Count>DistrictTraffic.PoolCap)throw new Exception("TRAFFIC_DISTRIBUTION target >=48 visible / >=12 retained old cells / >=2 new cells / safe: "+result);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 static Vector2Int Cell(Vector3 p){return new Vector2Int(Mathf.FloorToInt(p.x/250),Mathf.FloorToInt(p.z/250));}
}
