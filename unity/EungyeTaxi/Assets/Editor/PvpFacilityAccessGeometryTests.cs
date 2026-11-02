using System;
using System.Linq;
using UnityEngine;
public static class PvpFacilityAccessGeometryTests {
 static int checks;
 static void Check(bool value,string why){checks++;if(!value)throw new Exception("PVP_FACILITY_ACCESS: "+why);}
 static float Distance(Vector3 p,Vector3 a,Vector3 b){var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.001f,d.sqrMagnitude)));}
 public static void Run(){
  checks=0;var map=PvpCityMap.Create();var graph=new HdRoadGraph(map);
  foreach(int index in new[]{0,4,12,16}){
   string id="city:start:"+index;var site=map.projectScene.objects.Single(o=>o.id==id);var road=map.roads.Single(r=>r.id==id+":access");
   Check(road.points.Length==4&&road.width>=7,"explicit paved driveway "+id);var points=road.points.Select(p=>p.V()).ToArray();
   float entryAt,exitAt;int entry=graph.Nearest(points[0],site.Direction,out entryAt),exit=graph.Nearest(points[3],site.Direction,out exitAt);
   Check(entry>=0&&exit>=0,"both ends match a forward lane "+id);Check(Vector3.Distance(graph.paths[entry].Position(entryAt),points[0])<.05f&&Vector3.Distance(graph.paths[exit].Position(exitAt),points[3])<.05f,"endpoints lie on lane centre "+id);
   Check(Vector3.Dot(graph.paths[entry].Tangent(entryAt),site.Direction)>.6f&&Vector3.Dot(graph.paths[exit].Tangent(exitAt),site.Direction)>.6f,"direction agrees with court "+id);
   Check(graph.Route(entry,entryAt,exit,exitAt)!=null,"directed road approach and exit connected "+id);
   Check(Distance(site.Position,points[1],points[2])<.05f,"parking centre lies on driveway "+id);
   for(int segment=1;segment<points.Length;segment++){
    var a=points[segment-1];var b=points[segment];var dir=(b-a).normalized;int n=Mathf.CeilToInt(Vector3.Distance(a,b));
    for(int step=0;step<=n;step++)foreach(float side in new[]{-1f,0,1f}){
     var p=Vector3.Lerp(a,b,(float)step/n)+TrafficSystem.Right(dir)*side;
     Check(Enumerable.Range(1,points.Length-1).Any(k=>Distance(p,points[k-1],points[k])<=road.width*.5f),"vehicle-width path stays paved "+id);
     Check(!ProjectMapScene.OnArea(map,p,true),"driveway avoids raised sidewalk "+id);
     Check(!map.buildings.Any(building=>ProjectMapScene.Contains(building.points,p)),"driveway avoids buildings "+id);
    }
   }
  }
  foreach(int index in new[]{4,12}){var meal=map.projectScene.objects.Single(o=>o.id=="city:meal:"+index);var parking=map.projectScene.objects.Single(o=>o.id=="city:start:"+index);Check(Vector3.Distance(meal.Position,parking.Position)<.01f,"meal uses tested court "+index);}
  Debug.Log("PVP_FACILITY_ACCESS_GEOMETRY_OK checks="+checks+" parking=4 meal=2 physicalDriving=false");
 }
}
