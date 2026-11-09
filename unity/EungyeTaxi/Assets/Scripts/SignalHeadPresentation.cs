using System.Linq;
using System.Collections.Generic;

// Physical apertures are independent of legal movements and signal timing.
// Legacy authored and generated heads have four apertures, including an unlit left lens.
public static class SignalHeadPresentation {
 public struct Head {
  public int lamps,turn;public bool verified,left;
  public string Caption {get{return lamps==0?"형식 미확인":lamps+"구 · "+(verified?"현장 확인":"게임 배치");}}
 }
 public static int LampCount(string type){return type=="3구"?3:4;}
 // Metadata for generated heads lives in the existing document, so adding a
 // physical-format assignment does not alter the baseline schema or its hash.
 public static Head Generated(MapData map,TrafficJunction junction,int axis,int sign){
  var facilities=map==null||map.editorDocument==null?null:map.editorDocument.facilities;
  var matches=facilities==null?new RoadMapFacility[0]:facilities.Where(f=>f!=null&&f.kind=="signalHead"&&f.junctionId==junction.name&&f.axis==axis&&f.sign==sign).ToArray();
  if(matches.Length==0)return new Head{lamps=4};
  if(matches.Length!=1||matches[0].type!="3구"&&matches[0].type!="4구")return new Head();
  var f=matches[0];return new Head{lamps=LampCount(f.type),verified=f.evidence!=null&&f.evidence.appearanceConfirmed&&!string.IsNullOrEmpty(f.evidence.url)};
 }
 public static List<RoadMapIssue> ValidateAssignments(MapData baseline,RoadMapDocument document){
  var issues=new List<RoadMapIssue>();if(document==null||document.facilities==null)return issues;
  var assignments=document.facilities.Where(f=>f!=null&&f.kind=="signalHead").ToArray();
  foreach(var f in assignments){
   var rule=baseline==null||baseline.junctionRules==null?null:baseline.junctionRules.FirstOrDefault(r=>r.id==f.junctionId);
   string error=null;
   if(rule==null||rule.signalMode<=0)error="신호등 외형은 기존 신호 교차로에 지정하세요";
   else if(f.axis<0||f.axis>1||System.Math.Abs(f.sign)!=1)error="신호등 외형의 접근 축0/1 · 방향±1 필요";
   else if(f.type!="3구"&&f.type!="4구")error="신호등 외형은 3구 또는 4구로 명시하세요";
   else if(f.rule||!string.IsNullOrEmpty(f.laneId)||!string.IsNullOrEmpty(f.groupId)||!string.IsNullOrEmpty(f.targetId))error="신호등 외형 메타데이터에는 통행 규칙/차로/그룹/제어 대상을 지정하지 마세요";
   else if(document.signals!=null&&document.signals.Any(p=>p!=null&&p.junctionId==f.junctionId))error="작성 신호의 외형은 차량 신호기 type에 지정하세요";
   else if(assignments.Count(g=>g.junctionId==f.junctionId&&g.axis==f.axis&&g.sign==f.sign)>1)error="같은 접근 방향의 신호등 외형이 중복되었습니다";
   if(error!=null)issues.Add(new RoadMapIssue{error=true,id=f.id,message=error,position=f.Position});
  }
  return issues;
 }
 public static Head Resolve(MapData map,TrafficJunction junction,int axis,int sign){
  if(junction==null||!junction.signalized)return new Head();
  if(junction.editorProgram!=null){
   var document=map==null?null:map.editorDocument;
   if(document==null)return new Head();
   // Match group direction (the same identity used by RoadMapLamp), not proximity.
   var heads=document.facilities.Where(f=>f.kind=="vehicleSignal"&&f.junctionId==junction.name)
    .Select(f=>new {facility=f,group=junction.editorProgram.groups.FirstOrDefault(g=>!g.pedestrian&&g.id==f.groupId)})
    .Where(x=>(x.group==null?x.facility.axis:x.group.axis)==axis&&(x.group==null?x.facility.sign:x.group.sign)==sign).ToArray();
   if(heads.Length==0)return new Head();
   var first=heads.FirstOrDefault(x=>x.group!=null&&x.group.turn==0)??heads[0];var f=first.facility;
   int lamps=LampCount(f.type);
   // Conflicting head formats require an explicit assignment; do not pick a random one.
   if(heads.Any(x=>LampCount(x.facility.type)!=lamps))return new Head();
   bool explicitType=f.type=="3구"||f.type=="4구";
   return new Head{lamps=lamps,turn=first.group==null?0:first.group.turn,left=lamps==4&&junction.editorProgram.groups.Any(g=>!g.pedestrian&&g.axis==axis&&g.sign==sign&&g.turn==-1),verified=explicitType&&f.evidence!=null&&f.evidence.appearanceConfirmed&&!string.IsNullOrEmpty(f.evidence.url)};
  }
  for(int i=0;i<junction.lamps.Count;i++)if(junction.lampAxes[i]==axis&&junction.lampSigns[i]==sign){
   var lamp=junction.lamps[i];if(lamp==null||lamp.Length==0||!lamp[0])return new Head();
   bool fourth=lamp[0].transform.parent.Find("Signal lamp Left")!=null;
   var assigned=Generated(map,junction,axis,sign);int lamps=fourth?4:3;
   return new Head{lamps=lamps,left=fourth&&junction.leftLamps[i].Length>0,verified=assigned.lamps==lamps&&assigned.verified};
  }
  return new Head();
 }
}
