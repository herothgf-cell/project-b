using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Reviewed document metadata selects existing native lanes for presentation.
// Source paths and widths remain game geometry, not surveyed paint positions.
public sealed class ReviewedNativeLanePresentation {
 sealed class Lane {public int index;public HdEdge edge;public RoadMapFacility metadata;public AssistedRoadNetwork.Path path;}
 sealed class Group {public string junction;public int axis,sign;public readonly List<Lane> lanes=new List<Lane>();}
 readonly MapData map;readonly List<Group> groups=new List<Group>();readonly List<AssistedRoadNetwork.Path> importedDividers=new List<AssistedRoadNetwork.Path>();
 public ReviewedNativeLanePresentation(MapData source){
  map=source;if(map==null||map.hdEdges==null||map.editorDocument==null||map.editorDocument.facilities==null)return;
  foreach(var f in map.editorDocument.facilities.Where(f=>f!=null&&f.kind=="lanePresentation")){
   int index=Array.FindIndex(map.hdEdges,e=>e.id==f.laneId);if(index<0)continue;var edge=map.hdEdges[index];
   if(edge.disabled||edge.status!="ngii"||edge.turn!=0||edge.change!=0||edge.points==null||edge.points.Length<2)continue;
   var group=groups.FirstOrDefault(g=>g.junction==f.junctionId&&g.axis==f.axis&&g.sign==f.sign);
   if(group==null){group=new Group{junction=f.junctionId,axis=f.axis,sign=f.sign};groups.Add(group);}
   group.lanes.Add(new Lane{index=index,edge=edge,metadata=f,path=new AssistedRoadNetwork.Path(edge.points.Select(p=>p.V()).ToArray())});
  }
  if(groups.Count>0)foreach(var mark in map.hdMarks??new HdMark[0])if(mark!=null&&(mark.kind=="503"||mark.kind=="506")&&!string.IsNullOrEmpty(mark.type)&&mark.type.StartsWith("2")&&mark.points!=null&&mark.points.Length>=2)importedDividers.Add(new AssistedRoadNetwork.Path(mark.points.Select(p=>p.V()).ToArray()));
 }
 public static List<RoadMapIssue> ValidateAssignments(MapData map,RoadMapDocument document){
  var issues=new List<RoadMapIssue>();if(document==null||document.facilities==null)return issues;
  var assignments=document.facilities.Where(f=>f!=null&&f.kind=="lanePresentation").ToArray();
  foreach(var f in assignments){
   var rule=map==null||map.junctionRules==null?null:map.junctionRules.FirstOrDefault(r=>r.id==f.junctionId);
   var edge=document.lanes==null?null:document.lanes.FirstOrDefault(e=>e!=null&&e.id==f.laneId);string error=null;
   if(rule==null||rule.signalMode<=0)error="원본 차로 표시는 기존 신호 교차로에 지정하세요";
   else if(f.axis<0||f.axis>1||Math.Abs(f.sign)!=1||f.type!="원본 차로"||f.rule||!string.IsNullOrEmpty(f.groupId)||!string.IsNullOrEmpty(f.targetId))error="원본 차로 표시의 접근 방향/형식을 지정하고 통행 규칙은 비워 두세요";
   else if(edge==null||edge.disabled||edge.status!="ngii"||edge.turn!=0||edge.change!=0||edge.points==null||edge.points.Length<2)error="활성 원본 진입 차로를 지정하세요";
   else if(f.evidence==null||!f.evidence.appearanceConfirmed||string.IsNullOrWhiteSpace(f.evidence.url)||string.IsNullOrWhiteSpace(f.evidence.month))error="해당 접근의 차로 외형 확인 자료/촬영월을 지정하세요";
   else if(assignments.Count(g=>g.laneId==f.laneId)>1)error="같은 원본 차로의 표시가 중복되었습니다";
   else {
    var path=new AssistedRoadNetwork.Path(edge.points.Select(p=>p.V()).ToArray());var direction=new Vector3(rule.dx,0,rule.dz).normalized;if(f.axis==1)direction=TrafficSystem.Right(direction);direction*=f.sign;
    if(float.IsNaN(f.startAt)||float.IsInfinity(f.startAt)||float.IsNaN(f.endAt)||float.IsInfinity(f.endAt)||f.startAt<0||f.endAt<=f.startAt||f.endAt>path.length+.01f||f.width<2.6f||f.width>5)error="원본 차로 표시 구간/게임 차로 폭 범위를 확인하세요";
    else if(Vector3.Dot(path.Tangent(path.length),direction)<.8f||(path.Position(path.length)-rule.V()).sqrMagnitude>80*80)error="원본 진입 차로와 교차로 접근 방향이 일치하지 않습니다";
   }
   if(error!=null)issues.Add(new RoadMapIssue{error=true,id=f.id,message=error,position=f.Position});
  }return issues;
 }
 public static int MovementMask(MapData map,HdEdge edge){
  int mask=0;if(edge.disabled)return mask;
  foreach(int n in edge.next??new int[0]){
   if(n<0||n>=map.hdEdges.Length)continue;var connector=map.hdEdges[n];
   if(connector.disabled||connector.change!=0||connector.status!="ngii"||!(connector.next??new int[0]).Any(i=>i>=0&&i<map.hdEdges.Length&&!map.hdEdges[i].disabled))continue;
   mask|=UturnMovement.PaintMask(connector.turn);
  }return mask;
 }
 public static int MovementMask(MapData map,HdEdge edge,HybridJunctionRule junction){return ReviewedLaneMovementTrace.Mask(map,edge,junction);}
 bool SelectedUpstream(Group group,Lane lane){
  foreach(var prior in group.lanes){
   if(prior.index==lane.index)continue;
   var heading=prior.path.Tangent(prior.path.length);
   if(Continues(prior.index,lane.index,heading,new HashSet<int>(),0,0))return true;
  }return false;
 }
 bool Continues(int from,int target,Vector3 heading,HashSet<int> visited,int hops,float distance){
  if(hops>=32||!visited.Add(from))return false;var edge=map.hdEdges[from];
  foreach(int index in edge.next??new int[0]){
   if(index<0||index>=map.hdEdges.Length||visited.Contains(index))continue;var next=map.hdEdges[index];
   if(next.disabled||next.status!="ngii"||next.turn!=0||next.change!=0||next.points==null||next.points.Length<2)continue;
   var start=next.points[0].V();
   if((edge.points[edge.points.Length-1].V()-start).sqrMagnitude>.25f*.25f)continue;
   var path=new AssistedRoadNetwork.Path(next.points.Select(p=>p.V()).ToArray());
   if(Vector3.Dot(path.Tangent(0),heading)<.85f||Vector3.Dot(path.Tangent(path.length),heading)<.85f)continue;
   float total=distance+path.length;if(total>120)continue;
   if(index==target||Continues(index,target,heading,visited,hops+1,total))return true;
  }return false;
 }
 public List<LaneDirectionPaint.Arrow> Arrows(List<RoadJunctionGeometry.Clearance> clearances){
  var result=new List<LaneDirectionPaint.Arrow>();foreach(var group in groups)foreach(var lane in group.lanes){
   // A selected terminal piece extends paint cleanup to the stop line. Its
   // selected upstream continuation already owns the full approach role glyph.
   if(SelectedUpstream(group,lane))continue;
   var junction=(map.junctionRules??new HybridJunctionRule[0]).FirstOrDefault(j=>j.id==group.junction);
   int mask=MovementMask(map,lane.edge,junction);if(mask==0)continue;float begin=lane.metadata.startAt,end=Mathf.Min(lane.metadata.endAt,lane.path.length);
   float minimum=(mask&LaneDirectionPaint.Uturn)!=0&&(mask&~LaneDirectionPaint.Uturn)!=0?LaneDirectionPaint.GlyphSpacing+2.2f:4;
   if(end-begin<Mathf.Max(9,minimum+2.2f))continue;
   for(float at=Mathf.Max(begin+minimum,end-10);at>=begin+minimum;at-=1){var arrow=new LaneDirectionPaint.Arrow{edgeIndex=lane.index,turns=mask,centre=lane.path.Position(at),forward=lane.path.Tangent(at)};if(at+2.2f>end||!LaneDirectionPaint.Clear(arrow,clearances))continue;result.Add(arrow);break;}
  }return result;
 }
 static bool Sample(Lane lane,Vector3 point,Vector3 heading,out Vector3 centre){
  float error;float at=lane.path.Project(point,out error);centre=lane.path.Position(at);var direction=lane.path.Tangent(at);
  return at>=lane.metadata.startAt-.01f&&at<=lane.metadata.endAt+.01f&&Mathf.Abs(Vector3.Dot(point-centre,direction))<.6f&&Mathf.Abs(Vector3.Dot(heading,direction))>.85f;
 }
 bool InBand(Group group,Vector3 point,Vector3 direction,bool role){
  var offsets=new List<float>();var right=TrafficSystem.Right(direction);float half=0;
  foreach(var lane in group.lanes){Vector3 centre;if(!Sample(lane,point,direction,out centre))continue;offsets.Add(Vector3.Dot(centre-point,right));half=Mathf.Max(half,lane.metadata.width*.5f);}
  // Role glyphs can belong to an older, wider provisional lane. Divider
  // suppression stays inside the selected native lane widths instead.
  if(offsets.Count==0)return false;float margin=role?6:half;return offsets.Min()-margin<=0&&offsets.Max()+margin>=0;
 }
 public bool SuppressesArrow(HdEdge edge,Vector3 point,Vector3 direction){
  if(edge.status!="provisional")return false;
  foreach(var group in groups){if(edge.ruleId!=group.junction)continue;var lane=group.lanes[0];if(Vector3.Dot(direction,lane.path.Tangent(lane.metadata.startAt))<.85f)continue;if(InBand(group,point,direction,true))return true;}return false;
 }
 public bool SuppressesDivider(Vector3 point,Vector3 direction){return groups.Any(group=>InBand(group,point,direction,false));}
 public bool SuppressesSynthetic(string kind,Vector3 point,Vector3 direction){return (kind=="Lane divider"||kind=="Centre line")&&SuppressesDivider(point,direction);}
 bool OriginalDivider(Vector3 point,Vector3 direction){
  foreach(var path in importedDividers){float error;float at=path.Project(point,out error);var centre=path.Position(at);var heading=path.Tangent(at);
   // Match a white source boundary, not its dash phase. The imported renderer
   // retains that boundary's original solid/dashed cadence without a second line.
   if(error<.5f&&Mathf.Abs(Vector3.Dot(point-centre,heading))<.25f&&Mathf.Abs(Vector3.Dot(heading,direction))>.95f)return true;
  }return false;
 }
 public void PaintDividers(List<RoadJunctionGeometry.Clearance> clearances,Action<string,Vector3,Vector3,float,float,bool> mark){
  foreach(var group in groups){if(group.lanes.Count<2)continue;var reference=group.lanes[0];
   for(float at=reference.metadata.startAt+.5f;at<Mathf.Min(reference.metadata.endAt,reference.path.length);at+=1){
    if((at-reference.metadata.startAt)%6>=3)continue;var point=reference.path.Position(at);var direction=reference.path.Tangent(at);var right=TrafficSystem.Right(direction);var centres=new List<Vector3>();
    foreach(var lane in group.lanes){Vector3 centre;if(Sample(lane,point,direction,out centre))centres.Add(centre);}centres.Sort((a,b)=>Vector3.Dot(a-point,right).CompareTo(Vector3.Dot(b-point,right)));
    for(int i=1;i<centres.Count;i++){float spacing=Vector3.Distance(centres[i-1],centres[i]);if(spacing<2.6f||spacing>5)continue;var centre=(centres[i-1]+centres[i])*.5f;if(clearances.Any(c=>c.Overlaps(centre,direction,.065f,.48f))||OriginalDivider(centre,direction))continue;mark("Reviewed native lane divider",centre,direction,.13f,.96f,false);}
   }
  }
 }
}
