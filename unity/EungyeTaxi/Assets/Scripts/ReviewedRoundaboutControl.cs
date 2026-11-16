using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// Persisted authored control and a scoped circulating-occupancy entry gate.
// Existing unsignalized reservations/obstacle checks remain in TrafficSystem.
public static class ReviewedRoundaboutControl {
 public const string Kind="junctionControl",Type="회전교차로";
 sealed class Control {public Vector3 centre;public float inner,outer;}
 static readonly ConditionalWeakTable<MapData,Dictionary<string,Control>> Controls=new ConditionalWeakTable<MapData,Dictionary<string,Control>>();
 static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
 public static List<RoadMapIssue> Validate(MapData baseline,RoadMapDocument document){
  var issues=new List<RoadMapIssue>();var controls=document.facilities.Where(f=>f!=null&&f.kind==Kind).ToArray();
  foreach(var f in controls){
   string reason=null;
   if(f.type!=Type||!f.rule)reason="지원되는 규칙 적용 회전교차로 제어만 사용하세요";
   else if(string.IsNullOrEmpty(f.junctionId)||!baseline.junctionRules.Any(j=>j.id==f.junctionId))reason="회전교차로 대상 교차로가 없습니다";
   else if(controls.Count(c=>c.junctionId==f.junctionId)!=1)reason="교차로 제어가 중복되었습니다";
   else if(!string.IsNullOrEmpty(f.laneId))reason="교차로 제어는 특정 차로에 연결하지 않습니다";
   else if(!Finite(f.x)||!Finite(f.z)||!Finite(f.length)||!Finite(f.width))reason="회전교차로 중심/지름은 유한한 수여야 합니다";
   else if(f.length<12||f.width<f.length+10||f.width>40)reason="중앙섬 지름과 회전 노면 바깥 지름 범위를 확인하세요";
   else if(Vector3.Distance(f.Position,baseline.junctionRules.Single(j=>j.id==f.junctionId).V())>3)reason="회전교차로 중심은 대상 교차로의 3m 이내에 있어야 합니다";
   else if(f.evidence==null||!f.evidence.appearanceConfirmed||!f.evidence.ruleConfirmed||string.IsNullOrWhiteSpace(f.evidence.url)||string.IsNullOrWhiteSpace(f.evidence.month))reason="회전교차로와 제어 표지의 확인 근거/촬영월이 필요합니다";
   else if(document.signals.Any(s=>s.junctionId==f.junctionId)||document.facilities.Any(s=>s.junctionId==f.junctionId&&(s.kind=="vehicleSignal"||s.kind=="pedestrianSignal"||s.kind=="signalHead")))reason="회전교차로 제어와 신호 프로그램/신호등이 충돌합니다";
   if(reason!=null)issues.Add(new RoadMapIssue{error=true,id=f.id,message=reason,position=f.Position});
  }
  return issues;
 }
 public static bool IsRoundabout(RoadMapDocument document,string junctionId){return document!=null&&document.facilities!=null&&document.facilities.Any(f=>f!=null&&f.kind==Kind&&f.type==Type&&f.rule&&f.junctionId==junctionId);}
 public static void Apply(MapData composed,RoadMapDocument document){
  var errors=Validate(composed,document);if(errors.Count>0)throw new InvalidOperationException(string.Join(" / ",errors.Select(e=>e.ToString())));
  var lookup=new Dictionary<string,Control>();
  foreach(var f in document.facilities.Where(f=>f.kind==Kind)){
   composed.junctionRules.Single(j=>j.id==f.junctionId).signalMode=0;
   lookup.Add(f.junctionId,new Control{centre=f.Position,inner=f.length*.5f,outer=f.width*.5f});
  }
  // Composition calls this before assigning editorDocument. Snapshot the supplied
  // document here; a later document change must recompose or explicitly Apply.
  Controls.Remove(composed);Controls.Add(composed,lookup);
 }
 static readonly Collider[] Occupants=new Collider[256];
 public static bool Circulating(Vector3 centre,float inner,float outer,Vector3 position,Vector3 forward){
  var radial=position-centre;radial.y=0;float radius=radial.magnitude;if(radius<inner||radius>outer+1)return false;
  var tangent=new Vector3(-radial.z,0,radial.x).normalized;return Vector3.Dot(tangent,forward.normalized)>.55f;
 }
 // Narrow gate before unsignalized permission. Existing reservation, pedestrian,
 // obstacle and exit checks still run. Whole-ring yielding is conservative; this
 // is not a claim of traffic-capacity simulation or player-law scoring.
 public static bool EntryClear(MapData map,string junctionId,Vector3 position,Vector3 forward,GameObject self){
  Dictionary<string,Control> lookup;Control control;
  if(map==null||junctionId==null||!Controls.TryGetValue(map,out lookup)||!lookup.TryGetValue(junctionId,out control))return true;
  var centre=control.centre;float inner=control.inner,outer=control.outer;
  if(Circulating(centre,inner,outer,position,forward))return true;
  int count=Physics.OverlapSphereNonAlloc(centre+Vector3.up,outer+3,Occupants,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);
  if(count==Occupants.Length)return false; // crowded buffer never means clear
  for(int i=0;i<count;i++){
   var hit=Occupants[i];if(!hit||self&&(hit.gameObject==self||hit.transform.IsChildOf(self.transform)))continue;
   var radial=hit.bounds.center-centre;radial.y=0;float radius=radial.magnitude;
   if(radius<inner-1||radius>outer+1)continue;
   var heading=hit.attachedRigidbody?hit.attachedRigidbody.transform.forward:hit.transform.forward;
   var tangent=new Vector3(-radial.z,0,radial.x).normalized;
   // Tangential vehicles have ring priority; a deeply occupied ring also blocks
   // an entering vehicle even if the occupant is stationary or badly oriented.
   if(Mathf.Abs(Vector3.Dot(tangent,heading))>.55f||radius<outer-2)return false;
  }
  return true;
 }
}
