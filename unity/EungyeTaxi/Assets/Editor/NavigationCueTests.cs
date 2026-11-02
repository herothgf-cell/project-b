using System;
using System.Collections.Generic;
using UnityEngine;
// Checks directed route metadata and partial-route distances without moving a car.
public static class NavigationCueTests {
 static Type type;static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception("NAV_CUE: "+message);}
 static object Read(HdRoadGraph graph,HdRoadGraph.RoutePath route,float at){return type.GetMethod("FromRoute").Invoke(null,new object[]{graph,route,at});}
 static T Field<T>(object cue,string name){return (T)type.GetField(name).GetValue(cue);}
 static HdEdge Edge(string a,string b,int turn,string road,params Vector2[] points){var p=new MapPoint[points.Length];for(int i=0;i<p.Length;i++)p[i]=new MapPoint{x=points[i].x,z=points[i].y};return new HdEdge{id=a,fromNode=a,toNode=b,turn=turn,number=2,roadName=road,next=new int[0],points=p};}
 public static void Run(){
  checks=0;type=typeof(HdRoadGraph).Assembly.GetType("NavigationCue");Check(type!=null,"structured turn cue is available");
  var edges=new[]{Edge("a","b",0,"출발로",new Vector2(0,0),new Vector2(0,100)),Edge("b","c",-1,"",new Vector2(0,100),new Vector2(-10,110)),Edge("c","d",0,"은행로",new Vector2(-10,110),new Vector2(-100,110)),Edge("d","e",1,"",new Vector2(-100,110),new Vector2(-110,120)),Edge("e","f",0,"중앙로",new Vector2(-110,120),new Vector2(-200,120))};
  for(int i=0;i<edges.Length-1;i++)edges[i].next=new[]{i+1};
  var graph=new HdRoadGraph(new MapData{hdEdges=edges});var route=graph.Compose(new List<int>{0,1,2,3,4},0,90);
  var cue=Read(graph,route,62);Check(Field<bool>(cue,"Valid")&&Field<object>(cue,"Move").ToString()=="Left","next left uses directed movement metadata");
  Check(Mathf.Abs(Field<float>(cue,"Distance")-38)<.01f,"38m to first maneuver");Check(Field<string>(cue,"Road")=="은행로","outgoing road, not incoming road or connector id");
  Check(Field<bool>(cue,"HasNext")&&Field<object>(cue,"NextMove").ToString()=="Right","following turn available");Check(Mathf.Abs(Field<float>(cue,"NextDistance")-90)<.01f,"following distance measured AFTER first turn ends");
  var partial=graph.Compose(new List<int>{0,1,2,3,4},30,90);Check(Mathf.Abs(Field<float>(Read(graph,partial,32),"Distance")-38)<.01f,"partial first edge has no offset error");
  var during=Read(graph,route,103);Check(Field<object>(during,"Move").ToString()=="Left"&&Field<float>(during,"Distance")==0,"during turn stays at zero, never negative");
  var passed=Read(graph,route,route.ends[1]+1);Check(Field<object>(passed,"Move").ToString()=="Right"&&Field<string>(passed,"Road")=="중앙로","passed maneuver disappears");
  edges[1].turn=UturnMovement.Turn;Check(Field<object>(Read(graph,route,62),"Move").ToString()=="UTurn","U-turn not classified as left");edges[1].turn=-1;
  edges[0].change=-1;Check(Field<object>(Read(graph,route,62),"Move").ToString()=="LaneLeft","lane change not classified as turn");edges[0].change=0;
  edges[2].roadName="";Check(Field<string>(Read(graph,route,62),"Road")=="","missing outgoing name is not invented");edges[2].roadName="은행로";
  Check(!Field<bool>(Read(graph,null,0),"Valid")&&!Field<bool>(Read(graph,route,float.NaN),"Valid"),"missing route / invalid progress hide cue");
  var straight=Read(graph,route,route.ends[3]+1);Check(Field<object>(straight,"Move").ToString()=="Straight"&&!Field<bool>(straight,"HasNext"),"no future turn shows straight, not a fake turn");
  Check(route.edges.Count==5&&route.edges[0]==0&&route.ends[0]==100,"presentation does not mutate route");
  Debug.Log("NAVIGATION_CUE_OK checks="+checks);
 }
}
