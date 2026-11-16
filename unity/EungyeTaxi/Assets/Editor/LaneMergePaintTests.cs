using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LaneMergePaintTests {
 sealed class Stroke {public string name;public Vector3 p,d;public float w,l;}
 static void Check(bool ok,string message){if(!ok)throw new Exception("LANE_MERGE_PAINT: "+message);}
 static RoadMapFacility Feature(string type,float station){return new RoadMapFacility{id=type,kind="lanePresentation",type=type,laneId="aux",startAt=station,width=1.8f,length=4,scale=1,rule=false,x=999,z=999,evidence=new RoadMapEvidence{state="observed",appearanceConfirmed=true,url="https://example.test/observed",month="2026-05"}};}
 public static void Run(){
  var type=typeof(LaneDirectionPaint).Assembly.GetType("LaneMergePaint");Check(type!=null,"explicit merge/yield renderer exists");
  var paint=type.GetMethod("Paint");var validate=type.GetMethod("ValidateAssignments");Check(paint!=null&&validate!=null,"paint and validation API");
  foreach(float angle in new[]{0f,90f,217f}){
   var direction=Quaternion.Euler(0,angle,0)*Vector3.forward;var right=TrafficSystem.Right(direction);var origin=new Vector3(100,0,100);
   var lane=new RoadMapLane{id="aux",sourceId="fixture",status="provisional",width=3.5f,points=new[]{new MapPoint{x=origin.x,z=origin.z},new MapPoint{x=(origin+direction*40).x,z=(origin+direction*40).z}},heights=new float[2]};
   var merge=Feature("좌측 합류 화살표",12);var yield=Feature("양보 삼각형",24);
   var doc=new RoadMapDocument{lanes=new[]{lane},facilities=new[]{merge,yield}};
   var edge=new HdEdge{id=lane.id,sourceId=lane.sourceId,status="provisional",points=lane.points,next=new int[0]};
   var map=new MapData{hybrid=true,hdEdges=new[]{edge},editorDocument=doc,junctionRules=new HybridJunctionRule[0],roads=new MapRoad[0]};
   Func<List<Stroke>> draw=()=>{var lines=new List<Stroke>();Action<string,Vector3,Vector3,float,float,bool> mark=(n,p,d,w,l,y)=>{Check(!y,"merge paint is white");lines.Add(new Stroke{name=n,p=p,d=d,w=w,l=l});};paint.Invoke(null,new object[]{map,mark});return lines;};
   Func<List<RoadMapIssue>> issues=()=>(List<RoadMapIssue>)validate.Invoke(null,new object[]{map,doc});
   Check(issues().Count==0,"valid authored declarations accepted");Check(ReviewedNativeLanePresentation.ValidateAssignments(map,doc).Count==0,"presentation validator accepts both explicit types");
   var lines=draw();Check(lines.Count==7,"four merge strokes and three triangle sides only");
   foreach(var f in doc.facilities){var centre=origin+direction*f.startAt;var strokes=lines.Where(s=>s.name.EndsWith(f.id)).ToArray();Check(strokes.Length==(f==merge?4:3),"one explicitly declared shape");foreach(var s in strokes){float x=Mathf.Abs(Vector3.Dot(s.p-centre,right)),z=Mathf.Abs(Vector3.Dot(s.p-centre,direction));float hx=Mathf.Abs(Vector3.Dot(s.d,right))*s.l*.5f+Mathf.Abs(Vector3.Dot(TrafficSystem.Right(s.d),right))*s.w*.5f,hz=Mathf.Abs(Vector3.Dot(s.d,direction))*s.l*.5f+Mathf.Abs(Vector3.Dot(TrafficSystem.Right(s.d),direction))*s.w*.5f;Check(x+hx<=f.width*.5f+.001f&&z+hz<=f.length*.5f+.001f,"full strokes fit declared bounds at rotated heading");}}
   var diagonal=lines[1];Check(Vector3.Dot(diagonal.d,right)<0&&Vector3.Dot(diagonal.d,direction)>0,"merge points forward-left, never a perpendicular left-turn glyph");
   var triangle=lines.Skip(4).ToArray();Check(triangle.Count(s=>Vector3.Dot(s.p-origin-direction*24,direction)<-.01f)==0,"triangle sides extend from upstream apex to downstream base");
   Check(LaneDirectionPaint.Layout(map).Count==0,"presentation cannot create ordinary legal turn masks");
   int integrated=0;LaneDirectionPaint.Paint(map,(n,p,d,w,l,y)=>integrated++);Check(integrated==7,"normal paint entry point includes merge paint");
   var end=origin+direction*40;var beyond=end+direction*5;
   map.hdEdges=new[]{edge,new HdEdge{id="join:straight",status="provisional",turn=0,next=new[]{2}},new HdEdge{id="exit",sourceId="other",status="ngii",points=new[]{new MapPoint{x=end.x,z=end.z},new MapPoint{x=beyond.x,z=beyond.z}},next=new int[0]}};edge.next=new[]{1};
   var ordinary=LaneDirectionPaint.Layout(map);Check(ordinary.Count==1&&ordinary[0].turns==LaneDirectionPaint.Straight,"merge-left paint does not add Left to a real straight-only movement mask");
   doc.facilities=new RoadMapFacility[0];var without=LaneDirectionPaint.Layout(map);Check(without.Count==ordinary.Count&&without[0].turns==ordinary[0].turns&&without[0].centre==ordinary[0].centre,"ordinary arrow mask and placement unchanged by explicit merge facilities");doc.facilities=new[]{merge,yield};
   edge.next=new int[0];map.hdEdges=new[]{edge};
   string saved=doc.ToJson();draw();Check(doc.ToJson()==saved,"paint never mutates graph or facility data");
   doc.facilities=new RoadMapFacility[0];Check(draw().Count==0,"no inferred paint on unannotated lanes");doc.facilities=new[]{merge,yield};
   edge.disabled=true;Check(draw().Count==0,"disabled runtime lane has no paint");edge.disabled=false;
   merge.startAt=1;Check(issues().Any(i=>i.error),"glyph crossing lane tip rejected");merge.startAt=12;
   merge.width=4;Check(issues().Any(i=>i.error),"glyph wider than lane rejected");merge.width=1.8f;
   merge.startAt=float.NaN;Check(issues().Any(i=>i.error),"nonfinite station rejected");merge.startAt=12;
   merge.evidence.appearanceConfirmed=false;Check(issues().Any(i=>i.error),"missing observed appearance rejected");merge.evidence.appearanceConfirmed=true;
   map.junctionRules=new[]{new HybridJunctionRule{id="crossing",x=(origin+direction*12).x,z=(origin+direction*12).z,dx=direction.x,dz=direction.z}};
   Check(issues().Any(i=>i.error),"glyph overlapping junction clearance rejected");
  }
  Debug.Log("LANE_MERGE_PAINT_TEST_OK rotated glyph bounds, authored evidence, validator integration, no legal mask changes");
 }
}
