using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// These checks describe existing enabled graph paths, not new traffic rights.
// Run before authoring paint metadata; no map package is saved by this fixture.
public static class ReviewedLaneMovementTraceTests {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception("REVIEWED_LANE_TRACE: "+reason);}
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
 static HdEdge Edge(string id,MapPoint[] points,int[] next=null,int turn=0){return new HdEdge{id=id,sourceId=id,ruleId="NGII:"+id,status="ngii",number=1,points=points,next=next??new int[0],turn=turn};}
 // The fallback deliberately executes today's one-hop implementation for RED.
 // Once the scoped overload exists, these same tests exercise that overload.
 static int Trace(MapData map,HdEdge lane,HybridJunctionRule junction){
  var method=typeof(ReviewedNativeLanePresentation).GetMethod("MovementMask",BindingFlags.Public|BindingFlags.Static,null,new[]{typeof(MapData),typeof(HdEdge),typeof(HybridJunctionRule)},null);
  return method==null?ReviewedNativeLanePresentation.MovementMask(map,lane):(int)method.Invoke(null,new object[]{map,lane,junction});
 }
 static MapData Fixture(){
  return new MapData{mapId="trace fixture",hybrid=true,roads=new MapRoad[0],editorDocument=new RoadMapDocument(),junctionRules=new[]{new HybridJunctionRule{id="trace junction",x=0,z=0,dx=0,dz=1,signalMode=2}},hdEdges=new[]{
   Edge("incoming",new[]{P(0,-50),P(0,-35)},new[]{1}),
   Edge("short upstream",new[]{P(0,-35),P(0,-27)},new[]{2}),
   Edge("short split",new[]{P(0,-27),P(0,-20)},new[]{3,5}),
   Edge("left curve",new[]{P(0,-20),P(0,-10),P(-10,0),P(-20,0)},new[]{4},-1),
   Edge("left exit",new[]{P(-20,0),P(-50,0)}),
   Edge("source turn0 return curve",new[]{P(0,-20),P(0,-12),P(-5,-7),P(-10,-12),P(-10,-20)},new[]{6}),
   Edge("return exit",new[]{P(-10,-20),P(-10,-50)}),
   Edge("through incoming",new[]{P(5,-50),P(5,-35)},new[]{8}),
   Edge("through short",new[]{P(5,-35),P(5,-20)},new[]{9}),
   Edge("through crossing",new[]{P(5,-20),P(5,20)},new[]{10}),
   Edge("through exit",new[]{P(5,20),P(5,50)})
  }};
 }
 static void FixtureChecks(){
  var map=Fixture();var j=map.junctionRules[0];string original=JsonUtility.ToJson(map);
  Check(Trace(map,map.hdEdges[0],j)==(LaneDirectionPaint.Left|LaneDirectionPaint.Uturn),"short native segments reach left and geometric U-turn branches instead of a false straight glyph");
  Check(Trace(map,map.hdEdges[7],j)==LaneDirectionPaint.Straight,"short native segments preserve the actual through exit");
  Check(JsonUtility.ToJson(map)==original,"role tracing never changes existing graph, turn values or metadata");
  map.hdEdges[6].disabled=true;Check(Trace(map,map.hdEdges[0],j)==LaneDirectionPaint.Left,"disabled U-turn exit contributes no U-turn role");
  map.hdEdges[4].disabled=true;Check(Trace(map,map.hdEdges[0],j)==0,"all disabled exits produce no role");
  map=Fixture();j=map.junctionRules[0];map.hdEdges[3].next=new int[0];map.hdEdges[5].next=new int[0];
  Check(Trace(map,map.hdEdges[0],j)==0,"terminal connector curves without an active exit cannot invent movement roles");
  map=Fixture();j=map.junctionRules[0];map.hdEdges[2].next=new[]{1};
  Check(Trace(map,map.hdEdges[0],j)==0,"an incoming short-segment cycle is bounded and never mistaken for a through exit");
  map=Fixture();j=map.junctionRules[0];map.hdEdges[2].next=new[]{3};map.hdEdges[3].points=new[]{P(0,-20),P(0,130),P(-20,130)};map.hdEdges[4].points=new[]{P(-20,130),P(-50,130)};
  Check(Trace(map,map.hdEdges[0],j)==0,"a turn at a different distant junction is excluded from this approach");
  map=Fixture();j=map.junctionRules[0];map.hdEdges[2].next=new[]{3};map.hdEdges[3].status="provisional";
  Check(Trace(map,map.hdEdges[0],j)==0,"unreviewed provisional branches are outside native movement tracing");
  map=Fixture();j=map.junctionRules[0];map.hdEdges[2].next=new[]{3};map.hdEdges[3].change=1;
  Check(Trace(map,map.hdEdges[0],j)==0,"lane-change branches cannot become intersection movement glyphs");
  map=Fixture();j=map.junctionRules[0];map.hdEdges[2].next=new[]{-1,10000};
  Check(Trace(map,map.hdEdges[0],j)==0,"invalid successor references are ignored without drawing a role");
  map=Fixture();j=map.junctionRules[0];map.hdEdges[1].disabled=true;
  Check(Trace(map,map.hdEdges[0],j)==0,"disabled intermediate segments stop the trace");
  map=Fixture();j=map.junctionRules[0];map.hdEdges[1].points=new[]{P(20,-35),P(20,-27)};
  Check(Trace(map,map.hdEdges[0],j)==0,"a disconnected native branch is not promoted to a movement");
  map=Fixture();j=map.junctionRules[0];map.hdEdges[9].next=new int[0];
  Check(Trace(map,map.hdEdges[7],j)==0,"a through curve without an active exit contributes no straight role");
 }
 static RoadMapFacility Metadata(MapData map,string junction,string lane,int axis,int sign){
  var e=map.hdEdges.Single(x=>x.id==lane);var path=new AssistedRoadNetwork.Path(e.points.Select(p=>p.V()).ToArray());
  return new RoadMapFacility{id="trace fixture:"+lane,kind="lanePresentation",type="원본 차로",junctionId=junction,laneId=lane,axis=axis,sign=sign,width=3.5f,startAt=0,endAt=Mathf.Min(40,path.length),evidence=new RoadMapEvidence{state="observed",appearanceConfirmed=true,url="fixture existing-path classification only",month="2026-05"}};
 }
 static void ActualChecks(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();
  var cases=new[]{
   Tuple.Create("J866_5285","A2218G611780:0",9),
   Tuple.Create("J866_5285","A2218G611284:0",2),
   Tuple.Create("J866_5285","A2218G611282:0",2),
   Tuple.Create("J866_5285","A2218G611305:0",6),
   Tuple.Create("J5057_4449","A2218G611350:2",9),
   Tuple.Create("J5057_4449","A222BC004202:4",2),
   Tuple.Create("J5057_4449","A222BC004204:3",2),
   Tuple.Create("J5057_4449","A222BC004206:4",2),
   Tuple.Create("J5057_4449","A222BC004201:0",0),
   Tuple.Create("J5057_4449","A2218G611401:0",9),
   Tuple.Create("J5057_4449","A2218G611404:0",2),
   Tuple.Create("J5057_4449","A2218G611403:0",2),
   Tuple.Create("J5057_4449","A2218G611402:0",2),
   Tuple.Create("J5057_4449","A2218G611321:0",0)
  };
  string graphBefore=JsonUtility.ToJson(map);
  foreach(var c in cases){var edge=map.hdEdges.Single(e=>e.id==c.Item2);var junction=map.junctionRules.Single(j=>j.id==c.Item1);Check(Trace(map,edge,junction)==c.Item3,"existing path mask "+c.Item1+" / "+c.Item2+" expected "+c.Item3);}
  Check(JsonUtility.ToJson(map)==graphBefore,"actual-site tracing preserves graph, permissions, facilities and metadata byte representation");
  var selected=new[]{cases[0].Item2,cases[4].Item2,cases[9].Item2};
  map.editorDocument.facilities=map.editorDocument.facilities.Where(f=>f.kind!="lanePresentation"||!selected.Contains(f.laneId)).ToArray();
  Check(!LaneDirectionPaint.Layout(map).Any(a=>selected.Contains(map.hdEdges[a.edgeIndex].id)),"unselected actual native approaches do not gain global paint");
  map.editorDocument.facilities=map.editorDocument.facilities.Concat(new[]{Metadata(map,"J866_5285",selected[0],0,-1),Metadata(map,"J5057_4449",selected[1],1,1),Metadata(map,"J5057_4449",selected[2],1,-1)}).ToArray();
  string selectedBefore=JsonUtility.ToJson(map);var arrows=LaneDirectionPaint.Layout(map);
  foreach(string id in selected)Check(arrows.Any(a=>map.hdEdges[a.edgeIndex].id==id&&a.turns==9),"only explicitly selected approach paints its existing left/U-turn path roles "+id);
  Check(JsonUtility.ToJson(map)==selectedBefore,"selected paint does not rewrite native turn=0 loops, routing or U-turn conditions");
 }
 public static void Coverage(){
  var map=Fixture();var j=map.junctionRules[0];
  map.hdEdges[0].points=new[]{P(0,-70),P(0,-50)};map.hdEdges[1].points=new[]{P(0,-50),P(0,-27)};
  map.editorDocument.facilities=new[]{Metadata(map,j.id,map.hdEdges[0].id,0,1),Metadata(map,j.id,map.hdEdges[1].id,0,1)};
  string before=JsonUtility.ToJson(map);var arrows=LaneDirectionPaint.Layout(map);
  Check(arrows.Count(a=>a.edgeIndex==0||a.edgeIndex==1)==1&&arrows.Any(a=>a.edgeIndex==0&&a.turns==9),"connected selected terminal coverage keeps only the upstream full-role glyph");
  var presentation=new ReviewedNativeLanePresentation(map);
  Check(presentation.SuppressesDivider(new Vector3(0,0,-60),Vector3.forward)&&presentation.SuppressesDivider(new Vector3(0,0,-35),Vector3.forward),"both selected upstream and terminal native intervals suppress stray synthetic lines");
  Check(JsonUtility.ToJson(map)==before,"physical-approach glyph deduplication preserves graph and coverage metadata");
  Debug.Log("REVIEWED_LANE_MOVEMENT_COVERAGE_OK");
 }
 public static void Run(){checks=0;FixtureChecks();ActualChecks();Coverage();Debug.Log("REVIEWED_LANE_MOVEMENT_TRACE_OK checks="+checks);}
}
