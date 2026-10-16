using System;
using System.IO;
using System.Linq;
using UnityEngine;
public static class HybridNetworkTests {
 static void Check(bool yes,string message){if(!yes)throw new Exception("HYBRID: "+message);}
 public static void Run(){
  var map=EungyeHybridBuilder.Generate();Check(map.hybrid,"hybrid mode");
  Check(map.stops.Length>=8,"district service stops");
  var graph=new HdRoadGraph(map);int pairs=0;
  foreach(var a in map.stops)foreach(var b in map.stops){if(a==b)continue;Check(graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt)!=null,"stop route "+a.name+" > "+b.name);pairs++;}
  int provisional=0,portals=0,source=0;
  foreach(var edge in map.hdEdges){if(edge.status=="provisional")provisional++;else if(edge.status=="ngii")source++;if(edge.status=="provisional-portal")portals++;}
  Check(provisional>100&&source>300&&portals>=2,"both data sources and explicit portals");
  Check(map.junctionRules.Length>10,"editable junction catalog");
  var original=JsonUtility.FromJson<MapData>(File.ReadAllText("Assets/Resources/route42-map.json"));
  for(int i=0;i<original.hdEdges.Length;i++){var a=original.hdEdges[i];var b=map.hdEdges[i];Check(a.id==b.id&&a.fromNode==b.fromNode&&a.toNode==b.toNode&&a.points.Length==b.points.Length,"HD identity "+i);for(int k=0;k<a.points.Length;k++)Check(Vector3.Distance(a.points[k].V(),b.points[k].V())<.001f,"HD geometry "+i);foreach(int n in a.next)Check(b.next.Contains(n),"HD connection preserved "+i);}
  foreach(var stop in map.stops)Check(graph.Route(stop.laneEdge,stop.laneAt+1,stop.laneEdge,stop.laneAt)!=null,"passed stop circuit "+stop.name);
  var chosen=map.hdEdges.First(e=>e.id.StartsWith("join:")&&e.turn!=0);var settings=new HybridOverrides{junctions=new[]{new HybridJunctionRule{id=chosen.ruleId,allowLeft=false,allowRight=false,allowStraight=true}}};
  var blocked=EungyeHybridBuilder.Generate(settings,false);Check(!blocked.hdEdges.Any(e=>e.id.StartsWith("join:")&&e.ruleId==chosen.ruleId&&e.turn!=0),"editable junction prohibits turns");
  Check(graph.Forward(map.stops[2].laneEdge,map.stops[2].laneAt)!=null,"free driving continuation");
  bool detour=false;foreach(var a in map.stops){if(detour)break;foreach(var b in map.stops){if(a==b)continue;var shortest=graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);int avoid=shortest.edges[shortest.edges.Count/2];var weights=graph.paths.Select(p=>p.length/8).ToArray();weights[avoid]+=10000;var fast=graph.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt,weights);if(fast!=null&&!fast.edges.Contains(avoid)){detour=true;break;}}}Check(detour,"weighted traffic detour");
  var signalObject=new GameObject("hybrid signal test");var carObject=new GameObject("hybrid test car");
  try{var system=signalObject.AddComponent<TrafficSystem>();system.mainDirection=Vector3.forward;system.manualClock=true;var j=new TrafficJunction{direction=Vector3.back,leftTurnMode=LeftTurnMode.ProtectedShared};var car=carObject.AddComponent<TrafficCar>();car.route=new TrafficRoute{direction=Vector3.forward,turn=0};var stop=new TrafficStop{junction=j,axis=0};for(int t=0;t<80;t++){system.manualTime=t;Check(system.CanEnter(car,stop)==KoreanSignalRules.MayEnter(t,j.leftTurnMode,0,-1,0,false,true,true),"rotated signal entry "+t);}}
  finally{UnityEngine.Object.DestroyImmediate(carObject);UnityEngine.Object.DestroyImmediate(signalObject);}
  Debug.Log("HYBRID_NETWORK_TEST_OK edges="+map.hdEdges.Length+" stops="+map.stops.Length+" pairs="+pairs+" portals="+portals);
 }
}
