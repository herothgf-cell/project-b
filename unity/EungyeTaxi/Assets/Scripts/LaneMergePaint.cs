using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Explicit appearance only. These glyphs never add a turn, lane change or yield law.
// startAt is the glyph CENTRE station; width/length are its full painted bounds.
// x/z/yaw are editor anchors only. Actual placement follows the current bound lane.
public static class LaneMergePaint {
 public const string MergeLeftType="좌측 합류 화살표",YieldType="양보 삼각형";
 const float StrokeWidth=.14f;
 public static bool IsFeature(RoadMapFacility f){return f!=null&&f.kind=="lanePresentation"&&(f.type==MergeLeftType||f.type==YieldType);}
 static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
 static List<RoadJunctionGeometry.Clearance> Clearances(MapData map,RoadMapDocument document){
  var result=RoadJunctionGeometry.PaintClearances(new MapData{junctionRules=map==null?null:map.junctionRules,editorDocument=document});
  foreach(var f in document.facilities??new RoadMapFacility[0])if(f!=null&&(f.kind=="crosswalk"||f.kind=="stopLine")&&!RoadSurfaceMarks.IsRemovedCrosswalk(f))result.Add(new RoadJunctionGeometry.Clearance{kind=f.kind,center=f.Position,forward=Quaternion.Euler(0,f.yaw,0)*Vector3.forward,halfWidth=f.width*.5f,halfDepth=f.length*.5f});
  return result;
 }
 static string Error(RoadMapFacility f,RoadMapLane lane,MapPoint[] points,List<RoadJunctionGeometry.Clearance> clearances){
  if(f.rule||!string.IsNullOrEmpty(f.groupId)||!string.IsNullOrEmpty(f.targetId))return "합류 노면 표시는 외형 전용이며 규칙/신호 연결을 비워 두세요";
  if(lane==null||lane.disabled||lane.turn!=0||lane.change!=0||lane.id.StartsWith("join:")||points==null||points.Length<2)return "활성 본선/보조 차로에 합류 노면 표시를 지정하세요";
  if(f.evidence==null||!f.evidence.appearanceConfirmed||string.IsNullOrWhiteSpace(f.evidence.url)||string.IsNullOrWhiteSpace(f.evidence.month))return "합류 노면 표시의 현장 근거와 촬영월을 지정하세요";
  if(!Finite(f.startAt)||!Finite(f.width)||!Finite(f.length)||!Finite(f.scale)||!Finite(lane.width)||f.scale!=1||f.width<.4f||f.length<.4f||f.width>lane.width||f.length>40)return "합류 표시 거리/폭/길이와 배율1을 확인하세요";
  if(points.Any(p=>p==null||!Finite(p.x)||!Finite(p.z)))return "합류 차로 좌표가 유효하지 않습니다";
  var path=new AssistedRoadNetwork.Path(points.Select(p=>p.V()).ToArray());
  if(f.startAt-f.length*.5f<0||f.startAt+f.length*.5f>path.length)return "합류 표시 전체가 차로 구간 안에 있어야 합니다";
  var centre=path.Position(f.startAt);var forward=path.Tangent(f.startAt);
  if(forward.sqrMagnitude<.5f)return "합류 표시 방향을 정할 수 없습니다";
  if(clearances.Any(c=>c.Overlaps(centre,forward,f.width*.5f,f.length*.5f)))return "합류 표시가 교차로/횡단보도/정지선과 겹칩니다";
  // A tangent-aligned glyph on a sharp bend must still fit the lane corridor.
  var right=TrafficSystem.Right(forward);
  foreach(float x in new[]{-f.width*.5f,f.width*.5f})foreach(float z in new[]{-f.length*.5f,0,f.length*.5f}){
   float error;path.Project(centre+right*x+forward*z,out error);if(error>lane.width*.5f+.001f)return "합류 표시가 굽은 차로 폭을 벗어납니다";
  }
  return null;
 }
 public static List<RoadMapIssue> ValidateAssignments(MapData map,RoadMapDocument document){
  var result=new List<RoadMapIssue>();if(document==null)return result;var clearances=Clearances(map,document);
  foreach(var f in document.facilities??new RoadMapFacility[0])if(IsFeature(f)){
   var lane=(document.lanes??new RoadMapLane[0]).FirstOrDefault(l=>l!=null&&l.id==f.laneId);string error=Error(f,lane,lane==null?null:lane.points,clearances);
   if(error!=null)result.Add(new RoadMapIssue{error=true,id=f.id,message=error,position=f.Position});
  }return result;
 }
 public static void Paint(MapData map,Action<string,Vector3,Vector3,float,float,bool> mark){
  if(map==null||map.editorDocument==null)return;var document=map.editorDocument;var features=(document.facilities??new RoadMapFacility[0]).Where(IsFeature).ToArray();if(features.Length==0)return;
  var clearances=Clearances(map,document);
  foreach(var f in features){
   var lane=(document.lanes??new RoadMapLane[0]).FirstOrDefault(l=>l!=null&&l.id==f.laneId);var edge=(map.hdEdges??new HdEdge[0]).FirstOrDefault(l=>l!=null&&l.id==f.laneId);
   if(lane!=null&&lane.disabled||edge!=null&&edge.disabled)continue;
   string error=Error(f,lane,edge==null?null:edge.points,clearances);if(error!=null)throw new InvalidOperationException("Lane merge paint "+f.id+": "+error);
   var path=new AssistedRoadNetwork.Path(edge.points.Select(p=>p.V()).ToArray());var centre=path.Position(f.startAt);var forward=path.Tangent(f.startAt);var right=TrafficSystem.Right(forward);
   float w=(f.width-StrokeWidth)*.5f,l=(f.length-StrokeWidth)*.5f;
   Action<float,float,float,float> line=(x,z,u,v)=>{var a=centre+right*x+forward*z;var b=centre+right*u+forward*v;mark("Lane merge paint "+f.id,(a+b)*.5f,(b-a).normalized,StrokeWidth,Vector3.Distance(a,b),false);};
   if(f.type==MergeLeftType){line(w,-l,w,-l*.25f);line(w,-l*.25f,-w,l);line(-w,l,-w,l*.25f);line(-w,l,-w*.15f,l);}
   else{line(0,-l,w,l);line(w,l,-w,l);line(-w,l,0,-l);}
  }
 }
}
