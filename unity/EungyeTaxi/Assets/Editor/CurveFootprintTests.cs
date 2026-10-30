using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Exact-map geometry diagnostic; does not draw the scene or modify the map.
public static class CurveFootprintTests {
 public static void Run(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var graph=new HdRoadGraph(map);
  int edge=Array.FindIndex(graph.edges,e=>e.id=="A222BC004192:0");if(edge<0)throw new Exception("CURVE_FOOTPRINT missing actual curve");
  var host=new GameObject("curve footprint regression");var report=new StringBuilder();
  try{
   var system=host.AddComponent<TrafficSystem>();var carHost=new GameObject("candidate car 32");carHost.transform.SetParent(host.transform);
   var candidate=carHost.AddComponent<TrafficCar>();candidate.enabled=false;candidate.route=new TrafficRoute();candidate.route.SetPoints(graph.paths[edge].points);candidate.distance=0;
   var rule=map.junctionRules.Single(j=>j.id=="J1927_5181");
   var junction=new TrafficJunction{name=rule.id,center=rule.V(),direction=new Vector3(rule.dx,0,rule.dz).normalized,halfWidth=new[]{5f,21.3f},signalized=true,leftTurnMode=LeftTurnMode.ProtectedShared,facility=rule};
   system.junctions.Add(junction);candidate.system=system;
   var stop=new TrafficStop{junction=junction,distance=0,exitDistance=candidate.route.length-2.25f,axis=0,turn=0};candidate.route.stops.Add(stop);
   var blockerHost=new GameObject("reported stopped car 40");blockerHost.transform.SetParent(host.transform);blockerHost.layer=9;
   blockerHost.transform.SetPositionAndRotation(new Vector3(203.396f,0,515.044f),Quaternion.LookRotation(new Vector3(-.988f,0,.153f)));
   var blocker=blockerHost.AddComponent<BoxCollider>();blocker.center=new Vector3(0,.8f,0);blocker.size=new Vector3(1.8f,1.25f,4.2f);Physics.SyncTransforms();
   bool occupied=system.OccupiesMovement(candidate,stop,blocker),swept=false;float min=float.PositiveInfinity,minAt=0,firstFootprint=-1;
   // Independent production-lookahead footprint, sampled over the actual route.
   for(float at=0;at<=candidate.route.length;at+=.1f){
    var point=candidate.route.Position(at)+Vector3.up*.85f;float gap=Vector3.Distance(blocker.ClosestPoint(point),point);if(gap<min){min=gap;minAt=at;}
    if(Physics.OverlapBox(point,new Vector3(.95f,.55f,2.25f),Quaternion.LookRotation(candidate.route.Tangent(at)),1<<9,QueryTriggerInteraction.Ignore).Contains(blocker)){swept=true;if(firstFootprint<0)firstFootprint=at;}
   }
   report.AppendLine("curve=A222BC004192:0 length="+candidate.route.length+" blockerPosition="+blockerHost.transform.position.ToString("F3")+" blockerForward="+blockerHost.transform.forward.ToString("F3"));
   report.AppendLine("sweptFootprint="+swept+" occupiesMovement="+occupied+" minimumCenterlineColliderGap="+min+" minimumAt="+minAt+" minimumPoint="+candidate.route.Position(minAt).ToString("F3")+" firstFootprintAt="+firstFootprint);
   foreach(var point in graph.paths[edge].points)report.AppendLine("CURVE_POINT "+point.ToString("F6"));
   bool nodeContains=Physics.OverlapBox(junction.center+Vector3.up*.85f,new Vector3(junction.halfWidth[0]+.4f,.55f,junction.halfWidth[1]+.4f),Quaternion.LookRotation(system.AxisDirection(junction,0)),(1<<8)|(1<<9)).Contains(blocker);
   system.manualClock=true;bool green=false;
   for(int t=0;t<100;t++){system.manualTime=t+.25f;if(system.CanEnter(candidate,stop)){green=true;break;}}
   bool exitClear=system.ExitClear(candidate,stop),admitted=system.MayEnter(candidate,stop);
   report.AppendLine("junctionDirection="+junction.direction.ToString("F6")+" halfWidths="+junction.halfWidth[0]+","+junction.halfWidth[1]+" oldNodeBroadphaseContainsBlocker="+nodeContains+" canEnter="+green+" exitClear="+exitClear+" manualClock="+system.manualTime+" mayEnter="+admitted);
   report.AppendLine("Narrow phase detects the actual curve obstruction; entry must reject it even when the old junction-node box excludes the blocker.");
   string folder=Path.Combine(ProjectMapBuild.Root,"artifacts");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"curve-footprint-test.txt"),report.ToString());Debug.Log(report.ToString());
   if(!swept)throw new Exception("CURVE_FOOTPRINT fixture must intersect actual swept footprint");
   if(!occupied)throw new Exception("CURVE_FOOTPRINT actual car footprint intersects reported stopped car but entry gate misses it");
   if(nodeContains)throw new Exception("CURVE_FOOTPRINT fixture must lie outside the old junction-node broad phase");
   if(!green||!exitClear)throw new Exception("CURVE_FOOTPRINT fixture needs green signal and clear exit to isolate entry broad phase");
   if(admitted)throw new Exception("CURVE_FOOTPRINT green movement admitted through a stopped car excluded by junction-node broad phase");
   blocker.enabled=false;Physics.SyncTransforms();
   if(!system.MayEnter(candidate,stop))throw new Exception("CURVE_FOOTPRINT clear green curve must resume after blocker leaves");
   Debug.Log("CURVE_FOOTPRINT_OK actual obstructed curve rejected; clear curve resumes");
  }finally{UnityEngine.Object.DestroyImmediate(host);}
 }
}
