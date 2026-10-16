using System;

[Serializable] public class PracticeFeedback {
 public string outcome,good,issues,next;
 public string Details(){return "잘한 점\n"+good+"\n\n보완할 점\n"+issues+"\n\n다음 시도\n"+next;}
 public static PracticeFeedback Build(LawEvent e){
  var result=new PracticeFeedback{outcome="연습 미완료 · 아직 통과 판정 없음",good="아직 확인한 통과 기록이 없습니다.",issues="판정 전입니다. 정지선과 우회전 횡단보도를 지나야 결과를 확인할 수 있습니다.",next="안내 경로로 우회전한 뒤 오른쪽 도로까지 회전을 마무리하세요."};
  if(e==null)return result;
  if(e.family!="right"){result.outcome="경로 보완 필요 · 우회전 미완료";result.issues="이번 교차로에서 '"+e.action+"'이 관찰됐습니다. 우회전 연습 경로를 벗어났습니다.";result.next="같은 조건을 다시 시작하고 오른쪽 안내 경로를 따라 진행하세요.";return result;}
  if(e.observation==null||!e.observation.known||e.observation.pedestrian==PedestrianState.Unknown){result.outcome="판정 보류 · 관찰 정보 부족";result.issues="신호 또는 보행자 상태를 확정할 수 없어 성공으로 판정하지 않았습니다.";result.next="같은 조건을 다시 시작해 표시된 정지선과 횡단보도 경로로 진행하세요.";return result;}
  string good="",bad="",next="";
  foreach(var r in e.rules){
   if(r.rule=="L02"){
    if(r.verdict==LawVerdict.Compliant)good+="① 정지선 앞 완전 정지가 기록됐습니다.\n";
    if(r.verdict==LawVerdict.Violation){bad+="① 적색 신호 진입 전, 정지선 앞 완전 정지가 기록되지 않았습니다.\n";next+="차량 앞부분이 정지선을 넘기 전에 완전히 정지한 뒤 진행하세요.\n";}
   }
   if(r.rule=="L03"){
    if(r.verdict==LawVerdict.Compliant)good+="① 우회전 전용 녹색에서 진입했습니다.\n";
    if(r.verdict==LawVerdict.Violation){bad+="① 우회전 전용 적색일 때 정지선을 넘어 진입했습니다.\n";next+="전용 녹색 화살표가 켜질 때까지 정지선 앞에서 기다리세요.\n";}
   }
   if(r.rule=="L04"){
    if(r.verdict==LawVerdict.Compliant)good+="② 보행자를 기다린 뒤, 보행자가 없을 때 횡단보도로 진행했습니다.\n";
    if(r.verdict==LawVerdict.Violation){bad+="② 우회전 쪽 횡단보도 진입 시 "+(e.observation.pedestrian==PedestrianState.Intending?"건너려는 사람":"횡단 중인 사람")+"이 남아 있었습니다.\n";next+="횡단보도 앞에서 보행자가 통행을 마칠 때까지 기다리세요.\n";}
   }
  }
  if(!e.observation.dedicated&&e.observation.forward==SignalColor.Green)good+="① 차량 녹색 신호에서 진입했습니다.\n";
  if(e.observation.pedestrian==PedestrianState.None&&!good.Contains("②"))good+="② 횡단보도 진입 시 건너거나 건너려는 사람은 없었습니다.\n";
  bool deferred=e.rules.Exists(r=>r.rule!="L08"&&r.verdict==LawVerdict.Deferred);
  result.outcome=bad!=""?"보완 필요 · 놓친 조건이 있습니다":deferred?"판정 보류 · 일부 조건 미확인":"우회전 완료 · 관찰한 조건에서 위반 없음";
  result.good=good==""?"이번 기록에서 확인한 준수 항목이 없습니다.":good.TrimEnd();
  result.issues=bad!=""?bad.TrimEnd():deferred?"확정하지 못한 판정이 있어 완료로 표시하지 않습니다.":"이번 신호·정지·보행 통과 조건에서는 위반이 관찰되지 않았습니다.";
  result.next=next!=""?next.TrimEnd():"다른 조건을 선택해 신호와 보행자 변화에 맞춰 다시 연습해 보세요.";
  return result;
 }
}
