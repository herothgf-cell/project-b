using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ReviewedUnmarkedRoadPaintTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception("UNMARKED_ROAD_PAINT: "+message);}
 public static void Run(){
  var type=typeof(LaneDirectionPaint).Assembly.GetType("ReviewedUnmarkedRoadPaint");Check(type!=null,"bounded unmarked-road helper exists");
  var lane=new RoadMapLane{id="street",sourceId="raw-source",status="provisional",width=3.5f,fromNode="a",toNode="b",points=new[]{new MapPoint{x=20,z=0},new MapPoint{x=20,z=20},new MapPoint{x=20,z=40}},heights=new float[3]};
  var feature=new RoadMapFacility{id="unmarked",kind="lanePresentation",type="차선 없는 생활도로",laneId=lane.id,startAt=10,endAt=30,width=4.2f,scale=1,rule=false,evidence=new RoadMapEvidence{state="observed",appearanceConfirmed=true,url="https://example.test/pano",month="2026-05",note="Global source stations; game width estimate."}};
  var doc=new RoadMapDocument{lanes=new[]{lane},facilities=new[]{feature}};
  Func<MapData> map=()=>new MapData{hybrid=true,editorDocument=doc,hdEdges=doc.lanes.Select(l=>new HdEdge{id=l.id,sourceId=l.sourceId,status=l.status,disabled=l.disabled,points=l.points,sourceOffset=l.sourceOffset,next=new int[0]}).ToArray(),junctionRules=new HybridJunctionRule[0],roads=new MapRoad[0]};
  var validate=type.GetMethod("ValidateAssignments");var suppress=type.GetMethod("SuppressesSynthetic");Check(validate!=null&&suppress!=null,"validation and scoped suppression APIs");
  Func<List<RoadMapIssue>> issues=()=>(List<RoadMapIssue>)validate.Invoke(null,new object[]{map(),doc});
  Func<object> build=()=>Activator.CreateInstance(type,new object[]{map()});
  Func<object,string,float,float,Vector3,bool> hidden=(instance,name,x,z,heading)=>(bool)suppress.Invoke(instance,new object[]{name,new Vector3(x,0,z),heading,.14f,1f});
  var original=doc.ToJson();var instance=build();Check(issues().Count==0&&ReviewedNativeLanePresentation.ValidateAssignments(map(),doc).Count==0,"new metadata is accepted without native-lane assignments");
  foreach(string kind in new[]{"Centre line","Lane divider"}){
   Check(hidden(instance,kind,20,20,Vector3.forward),"declared interior paint suppressed");
   Check(hidden(instance,kind,20,20,Vector3.back),"opposite stroke heading on same line accepted");
   Check(!hidden(instance,kind,20,9.9f,Vector3.forward)&&!hidden(instance,kind,20,30.1f,Vector3.forward),"cross-boundary strokes retained");
   Check(hidden(instance,kind,20,10.5f,Vector3.forward)&&hidden(instance,kind,20,29.5f,Vector3.forward),"whole stroke touching interval boundary suppressed");
   Check(!hidden(instance,kind,23,20,Vector3.forward),"adjacent parallel street outside width preserved");
   Check(!hidden(instance,kind,20,20,Vector3.right),"perpendicular crossing preserved");
   Check(!hidden(instance,kind,20,-1,Vector3.forward),"projection clamping cannot extend suppression beyond path tip");
  }
  foreach(string kind in new[]{"Road edge","Lane arrow street","Reviewed native lane divider"})Check(!hidden(instance,kind,20,20,Vector3.forward),"non-synthetic or independently controlled paint preserved");
  Check(doc.ToJson()==original,"helper never mutates graph or rules");
  string tail=RoadMapAuthoringOperations.SplitLane(doc,lane.id,1);Check(feature.laneId==tail,"existing generic remap points assignment at tail");
  Check(feature.startAt==10&&feature.endAt==30&&issues().Count==0,"global station range survives remap across split");
  instance=build();foreach(float station in new[]{15f,20f,25f})Check(hidden(instance,"Centre line",20,station,Vector3.forward),"same feature covers gapfree original lineage on both sides of split");
  var tailLane=doc.lanes.Single(l=>l.id==tail);tailLane.sourceOffset=21;Check(issues().Any(i=>i.error),"lineage station gap rejected");tailLane.sourceOffset=20;
  tailLane.disabled=true;Check(issues().Any(i=>i.error),"disabled bound lane rejected");tailLane.disabled=false;
  feature.startAt=float.NaN;Check(issues().Any(i=>i.error),"nonfinite range rejected");feature.startAt=10;
  feature.endAt=41;Check(issues().Any(i=>i.error),"range beyond lineage rejected");feature.endAt=30;
  feature.width=-1;Check(issues().Any(i=>i.error),"invalid corridor width rejected");feature.width=4.2f;
  feature.evidence.appearanceConfirmed=false;Check(issues().Any(i=>i.error),"unconfirmed metadata rejected");feature.evidence.appearanceConfirmed=true;
  feature.rule=true;Check(issues().Any(i=>i.error),"appearance feature cannot change legal rules");feature.rule=false;
  doc.facilities=new RoadMapFacility[0];Check(!hidden(build(),"Centre line",20,20,Vector3.forward),"no automatic suppression on undeclared roads");
  Integration();CurvedVertex();
  Debug.Log("UNMARKED_ROAD_PAINT_TEST_OK bounds/adjoining/perpendicular/lineage/evidence cases");
 }
 static void CurvedVertex(){
  var points=new[]{new MapPoint{x=20,z=0},new MapPoint{x=20,z=20},new MapPoint{x=30,z=37.32051f}};
  var lane=new RoadMapLane{id="bend",sourceId="bend",points=points,width=3.5f};
  var doc=new RoadMapDocument{lanes=new[]{lane},facilities=new[]{new RoadMapFacility{id="bend-unmarked",kind="lanePresentation",type=ReviewedUnmarkedRoadPaint.FeatureType,laneId="bend",startAt=0,endAt=39.9f,width=5.2f,scale=1,evidence=new RoadMapEvidence{appearanceConfirmed=true,url="https://example.test/bend",month="2026-05"}}}};
  var map=new MapData{editorDocument=doc,hdEdges=new[]{new HdEdge{id="bend",points=points}}};var helper=new ReviewedUnmarkedRoadPaint(map);
  var heading=new Vector3(.258819f,0,.965926f);var centre=new Vector3(17.8f,0,20.2f);
  Check(helper.SuppressesSynthetic("Centre line",centre,heading,.14f,.5f),"interior convex vertex inside curved corridor must not fail a tangent endpoint-overrun check");
  Check(!helper.SuppressesSynthetic("Centre line",new Vector3(16.8f,0,20.2f),heading,.14f,.5f),"outside curved corridor remains visible");
  Check(!helper.SuppressesSynthetic("Centre line",new Vector3(20,0,-.5f),Vector3.forward,.14f,.5f),"true start overrun remains visible");
  RoadMapAuthoringOperations.SplitLane(doc,"bend",1);map.hdEdges=doc.lanes.Select(l=>new HdEdge{id=l.id,points=l.points,sourceOffset=l.sourceOffset}).ToArray();helper=new ReviewedUnmarkedRoadPaint(map);
  Check(helper.SuppressesSynthetic("Centre line",centre,heading,.14f,.5f),"a split at the interior bend is not a declared suppression endpoint");
 }
 static void Integration(){
  var a=new RoadMapLane{id="north",sourceId="raw",status="provisional",number=1,points=new[]{new MapPoint{x=20,z=0},new MapPoint{x=20,z=40}}};
  var b=new RoadMapLane{id="south",sourceId="raw",status="provisional",number=1,points=new[]{new MapPoint{x=23.5f,z=40},new MapPoint{x=23.5f,z=0}}};
  var doc=new RoadMapDocument{lanes=new[]{a,b}};var map=new MapData{mapId="fixture",hybrid=true,editorDocument=doc,roads=new[]{new MapRoad{id="raw",width=8,lanes=2,points=new[]{new MapPoint{x=21.75f,z=0},new MapPoint{x=21.75f,z=40}}}},junctionRules=new HybridJunctionRule[0],hdEdges=new[]{a,b}.Select(l=>new HdEdge{id=l.id,sourceId=l.sourceId,status=l.status,number=l.number,points=l.points,next=new int[0]}).ToArray()};
  Func<Dictionary<string,int>> paint=()=>{var counts=new Dictionary<string,int>();RoadPaintGeometry.Paint(map,null,(n,p,d,w,l,y)=>{if(!counts.ContainsKey(n))counts[n]=0;counts[n]++;});return counts;};
  var before=paint();doc.facilities=new[]{new RoadMapFacility{id="review",kind="lanePresentation",type="차선 없는 생활도로",laneId="north",startAt=10,endAt=30,width=4.2f,scale=1,evidence=new RoadMapEvidence{appearanceConfirmed=true,url="https://example.test/pano",month="2026-05"}}};var after=paint();
  Check(before["Centre line"]>after["Centre line"]&&after["Centre line"]>0,"actual compiled paint suppresses only bounded centre section");Check(before["Road edge"]==after["Road edge"],"actual compiled road-edge paint unchanged");
 }
}
