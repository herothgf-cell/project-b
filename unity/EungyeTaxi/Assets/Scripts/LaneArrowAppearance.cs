using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// A surveyed glyph can be narrower than legal movement permissions. This
// facility changes rendering only; neither lane.next nor connector.turn changes.
public static class LaneArrowAppearance {
 public const string StraightType="관측 직진 화살표";
 public const string StraightRightDrivewayType="관측 진입로 직진·우회전 화살표";
 public static bool IsFeature(RoadMapFacility f){return f!=null&&f.kind=="lanePresentation"&&(f.type==StraightType||f.type==StraightRightDrivewayType);}
 static int RequiredTurn(RoadMapFacility f)=>f.type==StraightType?0:1;
 public static List<RoadMapIssue> ValidateAssignments(MapData map,RoadMapDocument d){
  var result=new List<RoadMapIssue>();if(d==null)return result;
  foreach(var f in (d.facilities??new RoadMapFacility[0]).Where(IsFeature)){
   var lane=(d.lanes??new RoadMapLane[0]).FirstOrDefault(l=>l.id==f.laneId);string error=null;
   if(f.rule||!string.IsNullOrEmpty(f.groupId)||!string.IsNullOrEmpty(f.targetId)||lane==null||lane.disabled||lane.turn!=0||lane.change!=0||lane.id.StartsWith("join:"))error="관측 화살표는 활성 진입 차로의 외형 전용이어야 합니다";
   else if(f.evidence==null||!f.evidence.appearanceConfirmed||string.IsNullOrWhiteSpace(f.evidence.url)||string.IsNullOrWhiteSpace(f.evidence.month))error="관측 화살표의 현장 근거/촬영월이 필요합니다";
   else if(!d.lanes.Any(l=>!l.disabled&&lane.next.Contains(l.id)&&l.id.StartsWith("join:")&&l.turn==RequiredTurn(f)&&l.next.Any(n=>d.lanes.Any(t=>t.id==n&&!t.disabled))))error="관측 화살표에 해당하는 활성 연결이 필요합니다";
   else if(d.facilities.Count(g=>(IsFeature(g)||LaneDirectionPaint.IsArrowSuppression(g))&&g.laneId==f.laneId)!=1)error="같은 차로의 화살표 외형 지정이 중복되었습니다";
   if(error!=null)result.Add(new RoadMapIssue{error=true,id=f.id,message=error,position=f.Position});
  }return result;
 }
 public static int PaintedMask(MapData map,string laneId,int navigationMask){
  var feature=map?.editorDocument?.facilities?.FirstOrDefault(f=>IsFeature(f)&&f.laneId==laneId);
  if(feature==null)return navigationMask;
  int required=feature.type==StraightType?LaneDirectionPaint.Straight:LaneDirectionPaint.Right;
  if((navigationMask&required)==0)throw new InvalidOperationException("Observed arrow lacks matching navigation: "+laneId);
  return feature.type==StraightType?LaneDirectionPaint.Straight:LaneDirectionPaint.Straight|LaneDirectionPaint.Right;
 }
}
