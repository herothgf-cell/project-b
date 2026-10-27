using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class RoadNavigation {
 // Only authored facility footprints qualify. This is an explicit manual exit
 // leg, never a broader Nearest radius or an invented public-road connection.
 bool ServiceDeparture(int target,float targetAt,out List<Vector3> result){
  result=null;if(editorMap==null||vehicle==null||target<0)return false;
  var p=vehicle.position;if(Mathf.Abs(p.y)>2)return false;
  var sites=FuelStationLayout.For(editorMap).ToList();var rest=FuelStationLayout.Rest(editorMap);if(rest!=null)sites.Add(rest);
  foreach(var site in sites){
   var ring=Array.ConvertAll(site.plot,q=>new MapPoint{x=q.x,z=q.z});
   int pump=0;float best=float.MaxValue;for(int i=0;i<site.drivePath.Length;i++){float d=(site.drivePath[i]-site.pump).sqrMagnitude;if(d<best){pump=i;best=d;}}
   var exit=site.drivePath.Skip(pump).ToArray();
   if((ProjectMapScene.Contains(ring,p)||OnExit(exit,p))&&ExitLeg(exit,target,targetAt,out result))return true;
  }
  if(ProjectMapScene.Active(editorMap))foreach(var o in editorMap.projectScene.objects.Where(o=>o.enabled&&o.IsService)){
   if(o.kind=="restaurant"){var site=RestaurantSiteLayout.FromObject(editorMap,o);if((ProjectMapScene.Contains(o.points,p)||OnExit(site.leave,p))&&ExitLeg(site.leave,target,targetAt,out result))return true;}
   else if(o.kind=="parking"&&ProjectMapScene.Contains(o.points,p)){var stop=ProjectMapScene.ServiceStop(editorMap,o);if(Vector3.Distance(p,stop.V())<8&&ExitLeg(new[]{stop.V(),stop.V()+stop.Direction},target,targetAt,out result))return true;}
  }
  return false;
 }
 static bool OnExit(Vector3[] exit,Vector3 point){if(exit==null||exit.Length<2)return false;float error;new AssistedRoadNetwork.Path(exit).Project(point,out error);return error<=2.4f;}
 bool ExitLeg(Vector3[] exit,int target,float targetAt,out List<Vector3> result){
  result=null;if(exit==null||exit.Length<2)return false;float at;int start=hd.Nearest(exit[exit.Length-1],exit[exit.Length-1]-exit[exit.Length-2],out at);var route=hd.Route(start,at,target,targetAt);if(route==null)return false;
  var path=new AssistedRoadNetwork.Path(exit);float error;float progress=path.Project(vehicle.position,out error);if(error>8)return false;
  result=new List<Vector3>{new Vector3(vehicle.position.x,0,vehicle.position.z),path.Position(progress)};float walked=0;for(int i=1;i<exit.Length;i++){walked+=Vector3.Distance(exit[i-1],exit[i]);if(walked>progress)result.Add(exit[i]);}result.AddRange(route.path.points);return true;
 }
}
