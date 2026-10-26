using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Focused canonical-map regression: counts visible cars, never allocated objects.
public static class TrafficDistributionSnapshot {
 public static string Inspect(TrafficSystem traffic,out bool safe,out int visible,out int cells){
  safe=true;visible=0;var occupied=new HashSet<Vector2Int>();var starts=new HashSet<int>();var routes=new HashSet<TrafficRoute>();float nearest=float.MaxValue;int staticOverlap=0,ordinaryFailures=0,playerOverlap=0;
  var district=traffic.GetComponent<DistrictTraffic>();
  foreach(var car in traffic.cars){if(!car.Visible)continue;visible++;var p=car.route.Position(car.distance);occupied.Add(new Vector2Int(Mathf.FloorToInt(p.x/250),Mathf.FloorToInt(p.z/250)));routes.Add(car.route);
   int edge=car.route.graphPath.EdgeAt(car.distance);starts.Add(edge);var e=district.graph.edges[edge];
   if(e.disabled||e.turn!=0||e.change!=0||e.id.StartsWith("join:")||e.id.IndexOf("parking",StringComparison.OrdinalIgnoreCase)>=0||e.id.IndexOf("service",StringComparison.OrdinalIgnoreCase)>=0)ordinaryFailures++;
   if(Physics.CheckBox(p+Vector3.up*.85f,new Vector3(1.05f,.55f,2.3f),Quaternion.LookRotation(car.route.Tangent(car.distance)),1,QueryTriggerInteraction.Ignore))staticOverlap++;
   if(Physics.CheckBox(p+Vector3.up*.85f,new Vector3(1.1f,.55f,4),Quaternion.LookRotation(car.route.Tangent(car.distance)),1<<8,QueryTriggerInteraction.Ignore))playerOverlap++;
   foreach(var other in traffic.cars)if(other!=car&&other.Visible)nearest=Mathf.Min(nearest,Vector3.Distance(p,other.route.Position(other.distance)));
  }
  cells=occupied.Count;safe=nearest>=10-.01f&&staticOverlap==0&&ordinaryFailures==0&&playerOverlap==0;
  return "allocated="+traffic.cars.Count+" visible="+visible+" cells250m="+cells+" lanes="+starts.Count+" routes="+routes.Count+" minimumSpacing="+nearest+" staticOverlap="+staticOverlap+" playerOverlap="+playerOverlap+" ordinaryStartFailures="+ordinaryFailures;
 }
}
