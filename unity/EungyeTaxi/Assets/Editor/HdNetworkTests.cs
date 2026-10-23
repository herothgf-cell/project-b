using System;
using UnityEngine;
public static class HdNetworkTests {
 static void Check(bool value,string name){if(!value)throw new Exception("HD: "+name);}
 public static void Run(){
  var map=JsonUtility.FromJson<MapData>(Resources.Load<TextAsset>("route42-map").text);
  var graph=new HdRoadGraph(map);int changes=0;
  foreach(var test in map.hdTrials){
   var route=graph.Route(test.startEdge,test.startAt,test.goalEdge,test.goalAt);
   Check(route!=null&&route.path.length>30,test.name+" route");
   bool turn=false;
   foreach(int id in route.edges){if(graph.edges[id].turn==test.turn)turn=true;if(graph.edges[id].change!=0)changes++;}
   Check(turn,test.name+" expected turn");
   Check(Vector3.Distance(route.path.Position(route.path.length),graph.paths[test.goalEdge].Position(test.goalAt))<.01f,"goal position");
   if(test.turn==1)Check(graph.Via(test.startEdge,test.startAt,test.goalEdge,test.goalEdge,test.goalAt)!=null,"right turn request can end within terminal curve");
   for(int i=1;i<route.edges.Count;i++){int a=route.edges[i-1],b=route.edges[i];Check(Array.IndexOf(graph.edges[a].next,b)>=0,"source adjacency");}
  }
  Check(changes>0,"verified lane change is routed");
  var t=map.hdTrials[0];float at;
  Check(graph.Nearest(graph.paths[t.startEdge].Position(t.startAt),-graph.paths[t.startEdge].Tangent(t.startAt),out at)!=t.startEdge,"wrong heading rejected");
  Check(graph.Nearest(new Vector3(-1000,0,-1000),Vector3.forward,out at)==-1,"off-map rejected");
  Check(graph.Route(t.goalEdge,t.goalAt,t.startEdge,t.startAt)==null,"captured side road is terminal, not fabricated reverse");
  Check(graph.Route(t.startEdge,t.startAt+1,t.startEdge,t.startAt)==null,"no backward travel within link");
  Debug.Log("HD_NETWORK_TEST_OK cases="+map.hdTrials.Length+" edges="+graph.edges.Length+" changes="+changes);
 }
}
