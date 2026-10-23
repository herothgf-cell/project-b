using System;
using System.Linq;
using UnityEngine;
public static class JunctionFacilityTests {
 static void Check(bool value,string message){if(!value)throw new Exception("FACILITIES: "+message);}
 public static void Run(){var map=JsonUtility.FromJson<MapData>(Resources.Load<TextAsset>("eungye-hybrid-map").text);var configured=map.junctionRules.Where(r=>r.signalMode>0).ToArray();
  Check(configured.Length>=12,"district signal coverage");var shown=configured.First(r=>r.id=="J5750_2576");Check(shown.crosswalk&&shown.stopLine&&shown.approaches.Length==3,"shown junction actual three approaches");
  foreach(var r in configured){Check(r.facilityStatus=="provisional","new facilities honestly provisional");Check(r.crosswalk&&r.stopLine,"crosswalk and stop line");Check(r.approaches.Length>=3,"connected approaches");}
  var host=new GameObject("facility rendering test");var carObject=new GameObject("facility permission test");int arms=0;
  try{var system=host.AddComponent<TrafficSystem>();system.Build(map,false);system.manualClock=true;var car=carObject.AddComponent<TrafficCar>();car.enabled=false;
   foreach(var j in system.junctions){Check(j.lamps.Count==j.facility.approaches.Length,"only mapped signal approaches "+j.name);Check(j.crosswalkApproaches==j.lamps.Count&&j.stopLineApproaches==j.lamps.Count,"visible markings per approach");arms+=j.lamps.Count;
    for(int a=0;a<j.lamps.Count;a++){var arm=j.facility.approaches[a];var direction=new Vector3(arm.dx,0,arm.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(direction,system.AxisDirection(j,0)))>.7f?0:1;int sign=system.Sign(direction,axis,j);Check(j.lampAxes[a]==axis&&j.lampSigns[a]==sign,"lamp direction matches entry");
     car.route=new TrafficRoute{graphPath=new HdRoadGraph.RoutePath()};car.route.SetPoints(new[]{j.center-direction*60,j.center+direction*60});var stop=new TrafficStop{junction=j,axis=axis,distance=30,turn=0};
     for(int t=0;t<68;t+=4){system.manualTime=t;bool green=system.Signal(j,axis,sign)==SignalColor.Green;stop.turn=0;Check(system.CanEnter(car,stop)==green,"straight permission");stop.turn=-1;Check(system.CanEnter(car,stop)==green,"protected left permission");}
     system.manualTime=axis*34+(sign>0?0:17)+2-j.offset;system.SendMessage("Update");Check(j.lamps[a][2].sharedMaterial.name=="Signal lit 2","matching visible green lamp");
    }
   }
   int polesInRoad=0;foreach(Transform item in host.GetComponentsInChildren<Transform>())if(item.name=="Pole"){
    var p=item.position;p.y=0;bool overlap=false;foreach(var road in map.roads)for(int k=1;k<road.points.Length;k++){var a=road.points[k-1].V();var delta=road.points[k].V()-a;var near=a+delta*Mathf.Clamp01(Vector3.Dot(p-a,delta)/delta.sqrMagnitude);if(Vector3.Distance(p,near)<road.width*.5f+.2f)overlap=true;}if(overlap)polesInRoad++;
   }Check(polesInRoad==0,"signal poles intersect road count="+polesInRoad);
   var district=host.AddComponent<DistrictTraffic>();district.Initialize(system,map,false);int checkedTurns=0;
   foreach(var a in map.stops)foreach(var b in map.stops){if(a==b)continue;var route=district.CreateRoute(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);foreach(var stop in route.stops){if(stop.junction.name!=shown.id||stop.turn!=1)continue;
    var heading=route.Tangent(stop.distance);var arm=shown.approaches.OrderByDescending(p=>Vector3.Dot(heading,new Vector3(p.dx,0,p.dz))).First();var inbound=new Vector3(arm.dx,0,arm.dz).normalized;var plane=stop.junction.center-inbound*stop.junction.StopDistance(stop.axis);
    Check(Mathf.Abs(Vector3.Dot(route.Position(stop.distance+2.1f)-plane,inbound))<.3f,"turning bumper matches painted line");checkedTurns++;
   }}Check(checkedTurns>0,"shown right turn regression exercised");Debug.Log("FACILITY_STOP_LINES_OK turns="+checkedTurns);
  }finally{UnityEngine.Object.DestroyImmediate(carObject);UnityEngine.Object.DestroyImmediate(host);}
  bool rejected=false;try{EungyeHybridBuilder.Generate(new HybridOverrides{junctions=new[]{new HybridJunctionRule{id=shown.id,facilityStatus="verified"}}},false);}catch(Exception ex){rejected=ex.Message.Contains("requires evidence");}Check(rejected,"unsubstantiated verified status rejected");
  Debug.Log("FACILITY_RENDERING_OK approaches="+arms);
  Debug.Log("FACILITY_SETTINGS_OK junctions="+configured.Length);
 }
}
