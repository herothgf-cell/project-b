using UnityEngine;

// No throttle or brake references: this controller can only output steering.
public sealed class HdSteering {
 public RoadNavigation nav;
 public string status="H · 실제 차로 보조 조향";
 public bool changing,turning;
 public int turn,indicator;
 public bool Arm(TaxiController taxi){
  changing=turning=false;turn=indicator=0;
  if(nav!=null&&(!nav.active||nav.freeGuidance))nav.StartFreeGuidance();
  if(nav==null||!nav.active||nav.hdRoute==null||taxi.Speed<-.1f||Time.timeScale==0){status="목적지와 진행 차로를 먼저 확인하세요";return false;}
  float error;float at=nav.ProjectVehicle(out error);
  if(error>2.5f||Vector3.Dot(taxi.transform.forward,nav.hdRoute.path.Tangent(at))<.6f){status="안내 차로에 맞춰 진입한 뒤 H";return false;}
  status="경로 보조 · 속도와 제동은 직접 조작";return true;
 }
 public bool Request(int side,bool laneChange){
  bool accepted=nav!=null&&nav.RequestHdMovement(side,laneChange);
  status=accepted?(laneChange?"안내 연결 구간에서 차로 변경":"목적지 연결 회전 선택"):(laneChange?"이 방향에 가능한 변경 구간·목적지 연결이 없습니다":"현재 경로에서 가능한 회전이 아닙니다");return accepted;
 }
 public bool Apply(TaxiController taxi,ref float steering){
  if(nav==null||nav.hdRoute==null){status="연결 경로 없음 · 수동 운전";return false;}
  if(taxi.Speed<-.15f||taxi.ReverseRequested&&taxi.Speed<.2f){status="후진 · 수동 운전";return false;}
  var p=nav.hdRoute.path;float error;float at=nav.ProjectVehicle(out error);
  if(error>4||Vector3.Dot(taxi.transform.forward,p.Tangent(at))<.25f){status="안내 차로 이탈 · 수동 복귀";return false;}
  float remain=p.length-at;
  if(remain<.8f){status="안내 도착 · 직접 제동 · 수동 전환";return false;}
  var edge=nav.hd.edges[nav.hdRoute.EdgeAt(at)];changing=edge.change!=0;turning=edge.turn!=0;turn=edge.turn;
  indicator=0;float begin=0;
  for(int i=0;i<nav.hdRoute.edges.Count;i++){
   var next=nav.hd.edges[nav.hdRoute.edges[i]];
   if(nav.hdRoute.ends[i]>=at&&(next.change!=0||next.turn!=0)){if(begin-at<35)indicator=next.change!=0?next.change:UturnMovement.Indicator(next.turn);break;}
   begin=nav.hdRoute.ends[i];
  }
  float look=Mathf.Clamp(3+Mathf.Max(0,taxi.Speed)*.45f,3.5f,7);
  var target=p.Position(at+look);var local=taxi.transform.InverseTransformPoint(target+Vector3.up*taxi.transform.position.y);
  float angle=Mathf.Atan2(2*2.6f*local.x,Mathf.Max(1,local.x*local.x+local.z*local.z))*Mathf.Rad2Deg;
  steering=Mathf.Clamp(angle/DriveMath.SteerAngle(taxi.Speed*3.6f),-1,1);
  status=(changing?"차로 변경":turning?UturnMovement.Label(turn):"차로 유지")+" 보조 · 가감속 직접 · "+remain.ToString("0")+"m";
  return true;
 }
}
