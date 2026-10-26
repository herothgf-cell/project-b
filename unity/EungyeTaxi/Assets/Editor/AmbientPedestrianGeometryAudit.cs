using System;
using System.Linq;
using UnityEngine;

// Run with -executeMethod AmbientPedestrianGeometryAudit.Run on the tracked canonical map.
public static class AmbientPedestrianGeometryAudit {
 public static void Run(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();
  var root=new GameObject("ambient geometry audit");
  try{
   var pavement=root.AddComponent<RoadEnvironment>();pavement.Build(map);
   var traffic=root.AddComponent<TrafficSystem>();
   foreach(string name in new[]{"J5057_4449","J5076_2580","J4396_13458","J6199_11987"}){
    var rule=map.junctionRules.First(x=>x.id==name);
    var j=new TrafficJunction{name=rule.id,center=rule.V(),direction=new Vector3(rule.dx,0,rule.dz).normalized,facility=rule,signalized=rule.signalMode>0,halfWidth=new[]{0f,0f}};
    foreach(var approach in rule.approaches){var direction=new Vector3(approach.dx,0,approach.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(direction,j.direction))>.7f?0:1;j.halfWidth[axis]=Mathf.Max(j.halfWidth[axis],approach.width*.5f);}
    j.halfWidth[0]=Mathf.Max(3,j.halfWidth[0]);j.halfWidth[1]=Mathf.Max(3,j.halfWidth[1]);traffic.junctions.Add(j);
   }
   var timer=System.Diagnostics.Stopwatch.StartNew();
   var placed=new AmbientPedestrianPlacement(map,traffic,pavement);
   timer.Stop();Debug.Log("AMBIENT_PLACEMENT_MS "+timer.ElapsedMilliseconds);
   foreach(var line in placed.report)Debug.Log("AMBIENT_GEOMETRY "+line);
   foreach(var route in placed.routes)if(!route.crossing){if(route.Length<19.99f||route.Length>40.01f)throw new Exception("Invalid sidewalk length "+route.id+" length="+route.Length);for(int i=0;i<=32;i++)if(!placed.IsSafe(route,i/32f))throw new Exception("Unsafe sidewalk sample "+route.id+" step="+i);}
   if(placed.routes.Count>12)throw new Exception("Pedestrian budget exceeded");
   Debug.Log("AMBIENT_GEOMETRY_AUDIT_OK count="+placed.routes.Count+" sidewalks="+placed.routes.Count(x=>!x.crossing)+" crossings="+placed.routes.Count(x=>x.crossing));
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
