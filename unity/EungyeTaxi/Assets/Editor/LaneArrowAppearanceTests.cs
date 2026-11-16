using System;
using UnityEngine;

public static class LaneArrowAppearanceTests {
 static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("ARROW_APPEARANCE: "+why);}
 public static void Run(){
  var lane=new RoadMapLane{id="approach",next=new[]{"join:s","join:r"}};var doc=new RoadMapDocument{lanes=new[]{lane,new RoadMapLane{id="join:s",turn=0,next=new[]{"exit"}},new RoadMapLane{id="join:r",turn=1,next=new[]{"exit"}},new RoadMapLane{id="exit"}}};var map=new MapData{editorDocument=doc};
  Check(LaneArrowAppearance.PaintedMask(map,lane.id,6)==6,"unassigned graph mask retained");
  var feature=new RoadMapFacility{id="observed",kind="lanePresentation",type=LaneArrowAppearance.StraightType,laneId=lane.id,rule=false,evidence=new RoadMapEvidence{appearanceConfirmed=true,url="https://map.naver.com/",month="2026-05"}};doc.facilities=new[]{feature};string before=doc.ToJson();
  Check(LaneArrowAppearance.ValidateAssignments(map,doc).Count==0,"valid observed appearance accepted");Check(LaneArrowAppearance.PaintedMask(map,lane.id,6)==2,"paint S while graph S+R");Check(LaneArrowAppearance.PaintedMask(map,"unrelated",6)==6,"unrelated approach unchanged");Check(before==doc.ToJson(),"appearance lookup mutated graph/facilities");
  feature.rule=true;Check(LaneArrowAppearance.ValidateAssignments(map,doc).Count==1,"rule-bearing override rejected");feature.rule=false;lane.next=new[]{"join:r"};Check(LaneArrowAppearance.ValidateAssignments(map,doc).Count==1,"no straight navigation rejected");
  Debug.Log("ARROW_APPEARANCE_OK explicit-only graph-preserved invalid-rule-and-movement-rejected");
 }
}
