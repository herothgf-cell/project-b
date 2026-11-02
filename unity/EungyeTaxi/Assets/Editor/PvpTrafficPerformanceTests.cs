using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Independent pre-optimization reference implementations. These compare exact
// decisions; they do not claim a frame-rate gain from synthetic timings.
public static class PvpTrafficPerformanceTests {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception("PVP_TRAFFIC_PERFORMANCE: "+message);}
 static int LegacyNearest(HdRoadGraph graph,Vector3 position,Vector3 heading,out float at){
  int best=-1;float score=4;at=0;heading.y=0;
  for(int i=0;i<graph.edges.Length;i++){if(graph.edges[i].disabled||graph.edges[i].change!=0)continue;float error;float d=graph.paths[i].Project(position,out error);if(error>=score||heading.sqrMagnitude>.1f&&Vector3.Dot(heading.normalized,graph.paths[i].Tangent(d))<.6f)continue;best=i;score=error;at=d;}return best;
 }
 static void Nearest(HdRoadGraph graph,Vector3 point,Vector3 heading){float expectedAt,actualAt;int expected=LegacyNearest(graph,point,heading,out expectedAt),actual=graph.Nearest(point,heading,out actualAt);Check(expected==actual&&Mathf.Abs(expectedAt-actualAt)<.0001f,"nearest lane/order/heading/projection at "+point+" expected="+expected+" actual="+actual);}
 static HdEdge Edge(string id,bool reverse=false){return new HdEdge{id=id,fromNode=id+"a",toNode=id+"b",points=reverse?new[]{new MapPoint{x=10,z=0},new MapPoint{x=0,z=0}}:new[]{new MapPoint{x=0,z=0},new MapPoint{x=10,z=0}},next=new int[0]};}
 static SignalColor LegacyColor(RoadMapSignalProgram program,string id,float time){float cycle=program.stages.Sum(s=>s.green+s.yellow+s.clearance);if(cycle<=0)return SignalColor.Red;float t=(time%cycle+cycle)%cycle;foreach(var stage in program.stages){float length=stage.green+stage.yellow+stage.clearance;if(t<length){if(!stage.groups.Contains(id))return SignalColor.Red;var group=program.groups.FirstOrDefault(g=>g.id==id);return t<stage.green?SignalColor.Green:t<stage.green+stage.yellow&&(group==null||!group.pedestrian)?SignalColor.Yellow:SignalColor.Red;}t-=length;}return SignalColor.Red;}
 static SignalColor LegacyMovement(RoadMapSignalProgram p,int axis,int sign,int turn,float time,string lane){var matches=p.groups.Where(g=>!g.pedestrian&&g.axis==axis&&g.sign==sign&&g.turn==turn&&(lane==null||g.laneIds.Length==0||g.laneIds.Contains(lane))).ToArray();return matches.Length==0?SignalColor.Red:matches.Select(g=>LegacyColor(p,g.id,time)).OrderByDescending(c=>c==SignalColor.Green?2:c==SignalColor.Yellow?1:0).First();}
 static void Signals(RoadMapSignalProgram program){
  var times=new List<float>{-1,0,program.Cycle,program.Cycle+.01f};float at=0;
  foreach(var stage in program.stages){foreach(float boundary in new[]{at,at+stage.green,at+stage.green+stage.yellow}){times.Add(boundary-.001f);times.Add(boundary);times.Add(boundary+.001f);}at+=stage.green+stage.yellow+stage.clearance;}
  Check(program.Cycle==program.stages.Sum(s=>s.green+s.yellow+s.clearance),"signal cycle preserves float selector/double sum semantics");
  foreach(var group in program.groups){
   Check(program.NeedsLaneId(group.axis,group.sign)==program.groups.Any(g=>!g.pedestrian&&g.axis==group.axis&&g.sign==group.sign&&(g.turn==0||g.turn==1)&&g.laneIds.Length>0),"signal lane requirement");
   foreach(float time in times){
    Check(program.Color(group.id,time)==LegacyColor(program,group.id,time),"phase color at boundary "+time);
    Check(program.Walk(group.axis,group.sign,time)==program.groups.Any(g=>g.pedestrian&&g.axis==group.axis&&g.sign==group.sign&&LegacyColor(program,g.id,time)==SignalColor.Green),"walk phase");
    foreach(string lane in new[]{(string)null,"missing",group.laneIds.Length==0?"missing":group.laneIds[0]})Check(program.Movement(group.axis,group.sign,group.turn,time,lane)==LegacyMovement(program,group.axis,group.sign,group.turn,time,lane),"movement/lane filtering at boundary "+time);
   }
  }
 }
 public static void Run(){
  checks=0;var map=PvpCityMap.Create();var graph=new HdRoadGraph(map);
  Nearest(graph,new Vector3(1271.5697f,0,1777.091f),Quaternion.Euler(0,358.478821f,0)*Vector3.forward);
  for(int i=0;i<graph.paths.Length;i+=Mathf.Max(1,graph.paths.Length/48))foreach(float fraction in new[]{0f,.5f,1f}){
   var path=graph.paths[i];var point=path.Position(path.length*fraction);var heading=path.Tangent(path.length*fraction);Nearest(graph,point,heading);Nearest(graph,point,-heading);Nearest(graph,point+Vector3.up*80,Vector3.zero);
  }
  var simple=new HdRoadGraph(new MapData{hdEdges=new[]{Edge("tie0"),Edge("tie1"),Edge("opposing",true)}});
  Nearest(simple,new Vector3(5,0,0),Vector3.right);Nearest(simple,new Vector3(5,0,0),Vector3.left);Nearest(simple,new Vector3(5,0,4),Vector3.zero);Nearest(simple,new Vector3(5,0,3.999f),Vector3.zero);
  simple.edges[0].disabled=true;Nearest(simple,new Vector3(5,0,0),Vector3.right);simple.edges[1].change=1;Nearest(simple,new Vector3(5,0,0),Vector3.right);Nearest(simple,new Vector3(5,0,0),Vector3.left);
  foreach(var program in map.editorDocument.signals)Signals(program);
  Signals(new RoadMapSignalProgram{groups=new[]{new RoadMapSignalGroup{id="v",axis=0,sign=1,turn=0,laneIds=new[]{"a"}},new RoadMapSignalGroup{id="p",pedestrian=true,axis=0,sign=1}},stages=new[]{new RoadMapStage{green=.1f,yellow=.2f,clearance=.3f,groups=new[]{"v"}},new RoadMapStage{green=.7f,yellow=.2f,clearance=.9f,groups=new[]{"p"}}}});
  var rules=new MapData{editorDocument=new RoadMapDocument{facilities=new[]{new RoadMapFacility{kind="sign",type="제한속도",laneId="a",rule=false,startAt=0,endAt=100,speed=5},new RoadMapFacility{kind="sign",type="제한속도",laneId="a",rule=true,startAt=10,endAt=100,speed=30},new RoadMapFacility{kind="sign",type="제한속도",laneId="a",rule=true,startAt=20,endAt=60,speed=15}}}};
  foreach(string lane in new[]{"a","missing"})foreach(float at in new[]{0f,10,20,60,100,101}){var sign=rules.editorDocument.facilities.FirstOrDefault(f=>f.rule&&f.kind=="sign"&&f.type=="제한속도"&&f.laneId==lane&&at>=f.startAt&&at<=f.endAt);Check(RoadMapRules.SpeedLimit(rules,lane,at)==(sign==null?0:sign.speed),"first authored speed rule including endpoint/overlap order");}
  foreach(string lane in new[]{"a","missing"})foreach(float length in new[]{0f,10,50,100,150}){float expected=length/8;foreach(var f in rules.editorDocument.facilities.Where(f=>f.kind=="sign"&&f.rule&&f.type=="제한속도"&&f.laneId==lane)){float overlap=Mathf.Max(0,Mathf.Min(length,f.endAt)-Mathf.Max(0,f.startAt));expected+=overlap*(1/Mathf.Min(8,f.speed/3.6f)-1f/8);}Check(RoadMapRules.TravelSeconds(rules,lane,length,8)==expected,"travel cost preserves overlapping authored rules");}
  Debug.Log("PVP_TRAFFIC_PERFORMANCE_EQUIVALENCE_OK checks="+checks);
 }
}
