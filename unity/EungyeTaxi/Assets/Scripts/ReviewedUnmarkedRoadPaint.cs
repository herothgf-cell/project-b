using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Appearance-only suppression of synthetic longitudinal paint. startAt/endAt
// are GLOBAL directed-lineage stations (sourceOffset basis), NOT segment-local.
// This survives SplitLane's laneId remap. Width is the total bounded corridor;
// x/z/yaw are editor anchors and do not position the suppression region.
public sealed class ReviewedUnmarkedRoadPaint {
 public const string FeatureType="차선 없는 생활도로";
 sealed class Piece {public string id;public float offset;public AssistedRoadNetwork.Path path;}
 sealed class Band {public RoadMapFacility feature;public readonly List<Piece> pieces=new List<Piece>();}
 readonly List<Band> bands=new List<Band>();
 public static bool IsFeature(RoadMapFacility f){return f!=null&&f.kind=="lanePresentation"&&f.type==FeatureType;}
 static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
 static string Plan(RoadMapFacility f,RoadMapDocument document,out Band band){
  band=new Band{feature=f};
  if(f.rule||!string.IsNullOrEmpty(f.groupId)||!string.IsNullOrEmpty(f.targetId))return "무차선 도로 표시는 외형 전용으로 지정하세요";
  if(f.evidence==null||!f.evidence.appearanceConfirmed||string.IsNullOrWhiteSpace(f.evidence.url)||string.IsNullOrWhiteSpace(f.evidence.month))return "무차선 도로의 현장 근거와 촬영월을 지정하세요";
  if(!Finite(f.startAt)||!Finite(f.endAt)||f.startAt<0||f.endAt<=f.startAt||!Finite(f.width)||f.width<=0||f.width>40||f.scale!=1)return "원본 차로 기준 시작/끝 거리, 도로 표시 폭과 배율1을 확인하세요";
  var lanes=document.lanes??new RoadMapLane[0];var selected=lanes.FirstOrDefault(l=>l!=null&&l.id==f.laneId);
  if(selected==null||selected.disabled||selected.turn!=0||selected.change!=0||selected.id.StartsWith("join:"))return "활성 주행 차로의 원본 계보를 지정하세요";
  string origin=RoadMapAuthoringOperations.Origin(selected);
  foreach(var lane in lanes.Where(l=>l!=null&&!l.disabled&&RoadMapAuthoringOperations.Origin(l)==origin).OrderBy(l=>l.sourceOffset)){
   if(lane.turn!=0||lane.change!=0||!Finite(lane.sourceOffset)||lane.sourceOffset<0||lane.points==null||lane.points.Length<2||lane.points.Any(p=>p==null||!Finite(p.x)||!Finite(p.z)))return "차로 계보의 좌표/원본 거리가 유효하지 않습니다";
   if(lane.heights!=null&&lane.heights.Length!=0&&(lane.heights.Length!=lane.points.Length||lane.heights.Any(h=>!Finite(h))))return "차로 높이 배열이 유효하지 않습니다";
   var path=new AssistedRoadNetwork.Path(lane.points.Select((p,i)=>p.V(lane.heights!=null&&lane.heights.Length>0?lane.heights[i]:0)).ToArray());
   if(path.length<=0)return "길이가 없는 차로 조각은 표시 범위를 만들 수 없습니다";
   if(lane.sourceOffset+path.length>=f.startAt&&lane.sourceOffset<=f.endAt)band.pieces.Add(new Piece{id=lane.id,offset=lane.sourceOffset,path=path});
  }
  float covered=f.startAt;Piece prior=null;
  foreach(var piece in band.pieces){
   if(piece.offset>covered+.01f)return "표시 범위에 비활성/누락 차로 구간이 있습니다";
   if(prior!=null&&(Mathf.Abs(piece.offset-(prior.offset+prior.path.length))>.01f||Vector3.Distance(prior.path.points.Last(),piece.path.points[0])>.02f))return "차로 계보의 거리/끝점이 연속되지 않습니다";
   covered=Mathf.Max(covered,piece.offset+piece.path.length);prior=piece;
  }
  if(band.pieces.Count==0||covered<f.endAt-.01f)return "표시 범위가 활성 차로 계보 밖으로 나갑니다";
  return null;
 }
 public static List<RoadMapIssue> ValidateAssignments(MapData map,RoadMapDocument document){
  var issues=new List<RoadMapIssue>();if(document==null)return issues;
  foreach(var f in document.facilities??new RoadMapFacility[0])if(IsFeature(f)){Band ignored;string error=Plan(f,document,out ignored);if(error!=null)issues.Add(new RoadMapIssue{error=true,id=f.id,message=error,position=f.Position});}
  return issues;
 }
 public ReviewedUnmarkedRoadPaint(MapData map){
  if(map==null||map.editorDocument==null)return;
  foreach(var f in map.editorDocument.facilities??new RoadMapFacility[0])if(IsFeature(f)){
   var selected=(map.editorDocument.lanes??new RoadMapLane[0]).FirstOrDefault(l=>l!=null&&l.id==f.laneId);if(selected!=null&&selected.disabled)continue;
   Band band;string error=Plan(f,map.editorDocument,out band);if(error!=null)throw new InvalidOperationException("Unmarked road paint "+f.id+": "+error);
   // Runtime-disabled pieces never suppress paint. Raw source IDs are not
   // expanded to neighbouring lanes or other streets sharing a source group.
   band.pieces.RemoveAll(p=>(map.hdEdges??new HdEdge[0]).Any(e=>e!=null&&e.id==p.id&&e.disabled));bands.Add(band);
  }
 }
 static bool Contains(Band band,Vector3 point,Vector3 heading){
  foreach(var piece in band.pieces){
   float error;float at=piece.path.Project(point,out error);float global=piece.offset+at;if(global<band.feature.startAt-.001f||global>band.feature.endAt+.001f)continue;
   var centre=piece.path.Position(at);var tangent=piece.path.Tangent(at);if(Mathf.Abs(Vector3.Dot(tangent,heading))<.85f)continue;
   var delta=point-centre;float along=Vector3.Dot(delta,tangent);
   // A nearest interior vertex can have a nonzero residual against Path's
   // smoothed tangent. Only the declared range's true ends reject overruns;
   // internal vertices/split seams remain part of the bounded curved corridor.
   if(global<=band.feature.startAt+.001f&&along<-.01f||global>=band.feature.endAt-.001f&&along>.01f)continue;
   if(error>band.feature.width*.5f+.001f)continue;
   if(Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(tangent)))<=band.feature.width*.5f+.001f)return true;
  }return false;
 }
 public bool SuppressesSynthetic(string kind,Vector3 point,Vector3 heading,float width,float length){
  if((kind!="Centre line"&&kind!="Lane divider")||heading.sqrMagnitude<.5f||!Finite(width)||!Finite(length)||width<=0||length<=0)return false;
  heading.Normalize();var right=TrafficSystem.Right(heading);
  foreach(var band in bands){
   bool inside=true;
   // Test both edges at both ends and midpoint; a stroke straddling a declared
   // boundary remains visible instead of erasing paint on the adjacent road.
   foreach(float along in new[]{-.5f,0,.5f})foreach(float side in new[]{-.5f,.5f})if(!Contains(band,point+heading*(length*along)+right*(width*side),heading))inside=false;
   if(inside)return true;
  }return false;
 }
}
