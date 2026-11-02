using System.Globalization;
using UnityEngine;

public enum NavigationMove { None,Left,Right,UTurn,LaneLeft,LaneRight,Straight,Arrival }
// Read-only presentation of the SAME directed edges used by HdRoadGraph.Instruction.
// Distances are along the selected route, never straight-line distance to a junction.
public struct NavigationCue {
 public bool Valid,HasNext;
 public NavigationMove Move,NextMove;
 public float Distance,NextDistance;
 public string Road,LaneHint;
 public static NavigationCue FromRoute(HdRoadGraph graph,HdRoadGraph.RoutePath route,float at){
  var cue=new NavigationCue();
  if(graph==null||route==null||route.path==null||route.edges.Count==0||route.ends.Count!=route.edges.Count||float.IsNaN(at)||float.IsInfinity(at))return cue;
  at=Mathf.Max(0,at);int first=-1;float firstEnd=0,begin=0;
  for(int i=0;i<route.edges.Count;i++){
   int id=route.edges[i];if(id<0||id>=graph.edges.Length)return new NavigationCue();
   var edge=graph.edges[id];if(route.ends[i]<at){begin=route.ends[i];continue;}
   var move=Movement(edge);if(move!=NavigationMove.None){
    if(first<0){
     first=i;firstEnd=route.ends[i];cue.Valid=true;cue.Move=move;cue.Distance=Mathf.Max(0,begin-at);cue.Road=OutgoingRoad(graph,route,i);
     int lane=graph.edges[route.edges[Mathf.Max(0,i-1)]].number;
     cue.LaneHint=edge.change!=0?"점선 구간":lane>0&&lane<10?lane+"차로 준비":"연결 차로";
    }else{cue.HasNext=true;cue.NextMove=move;cue.NextDistance=Mathf.Max(0,begin-firstEnd);break;}
   }begin=route.ends[i];
  }
  if(first<0){var current=graph.edges[route.EdgeAt(at)];cue.Valid=true;cue.Move=NavigationMove.Straight;cue.Distance=Mathf.Max(0,route.path.length-at);cue.Road=current.roadName??"";cue.LaneHint="직진";}
  return cue;
 }
 static NavigationMove Movement(HdEdge edge){return edge.change<0?NavigationMove.LaneLeft:edge.change>0?NavigationMove.LaneRight:edge.turn==UturnMovement.Turn?NavigationMove.UTurn:edge.turn<0?NavigationMove.Left:edge.turn>0?NavigationMove.Right:NavigationMove.None;}
 static string OutgoingRoad(HdRoadGraph graph,HdRoadGraph.RoutePath route,int index){
  for(int i=index+1;i<route.edges.Count;i++){var edge=graph.edges[route.edges[i]];if(edge.turn==0&&edge.change==0)return edge.roadName??"";}
  return "";
 }
 public static string Label(NavigationMove move){switch(move){case NavigationMove.Left:return "좌회전";case NavigationMove.Right:return "우회전";case NavigationMove.UTurn:return "유턴";case NavigationMove.LaneLeft:return "왼쪽 차로";case NavigationMove.LaneRight:return "오른쪽 차로";case NavigationMove.Arrival:return "도착";default:return "직진";}}
 public static string FormatDistance(float value){if(float.IsNaN(value)||float.IsInfinity(value))return "—";value=Mathf.Max(0,value);return value>=1000?(value/1000).ToString("0.0",CultureInfo.InvariantCulture)+"km":Mathf.CeilToInt(value)+"m";}
}
