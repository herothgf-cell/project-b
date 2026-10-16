using System;
using UnityEngine;
public static class DistrictTrafficTests {
 static void Check(bool v,string s){if(!v)throw new Exception("DISTRICT: "+s);}
 public static void Run(){
  var map=JsonUtility.FromJson<MapData>(Resources.Load<TextAsset>("eungye-hybrid-map").text);
  var host=new GameObject("district test");
  try{var system=host.AddComponent<TrafficSystem>();system.Build(map,false);var district=host.AddComponent<DistrictTraffic>();district.Initialize(system,map,false);
   int pairs=0,left=0,right=0,signals=0;
   foreach(var a in map.stops)foreach(var b in map.stops){if(a==b)continue;var route=district.CreateRoute(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);Check(route!=null,"service route");Check(route.graphPath!=null,"same navigation graph");foreach(var stop in route.stops){Check(stop.distance>=0&&stop.exitDistance<=route.length,"stop bounds");if(stop.turn<0)left++;if(stop.turn>0)right++;if(stop.junction.signalized)signals++;}pairs++;}
   var again=district.CreateRoute(map.stops[5].laneEdge,map.stops[5].laneAt,map.stops[9].laneEdge,map.stops[9].laneAt);int visits=0;float first=-1;foreach(var stop in again.stops)if(stop.junction.name=="J6199_11987"){if(first>=0)Check(stop.distance-first>300,"separate local projection for repeated junction");first=stop.distance;visits++;}Check(visits>=2,"every repeat signal traversal controlled");
   var probe=host.AddComponent<TrafficCar>();probe.enabled=false;var j=system.junctions[0];probe.route=new TrafficRoute();var early=new TrafficStop{junction=j,distance=10,exitDistance=20};var later=new TrafficStop{junction=j,distance=500,exitDistance=520};probe.route.stops.Add(early);probe.route.stops.Add(later);probe.distance=12;j.reservations.Add(probe);system.CancelApproach(probe,later);Check(j.reservations.Contains(probe),"future red cannot release active traversal");probe.distance=499;system.CancelApproach(probe,later);Check(!j.reservations.Contains(probe),"approached red releases unused reservation");
   Check(left>0&&right>0&&signals>0,"turns and signals used");Debug.Log("DISTRICT_NETWORK_OK pairs="+pairs+" left="+left+" right="+right+" signals="+signals);
  }finally{UnityEngine.Object.DestroyImmediate(host);}
 }
}
