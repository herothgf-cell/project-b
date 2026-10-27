using System.Text.RegularExpressions;
public static class ScoreFeedback {
 public static string Reason(string reason){if(string.IsNullOrEmpty(reason))return "주행 변화";return Regex.Replace(reason.Split('\n')[0],@"\s*[+-]\d+점\s*$","").Trim();}
 public static string Text(string reason,int delta){return Reason(reason)+" · "+(delta>0?"+":"")+delta;}
 public static string LawReason(LawEvent e,int delta){
  if(e==null)return delta>0?"법규 준수":"법규 위반";
  if(delta<0)foreach(var r in e.rules)if(r.verdict==LawVerdict.Violation&&r.penalty==e.Deduction){switch(r.rule){case "L01":return "신호 위반";case "L02":return "우회전 전 정지 위반";case "L03":return "우회전 신호 위반";case "L04":return "보행자 보호 위반";case "L06":return "대기열 끼어들기";case "L07":return "통행 방해";case "F02":return "적색 점멸 정지 위반";case "F05":return "비보호 좌회전 위반";case "F06":return "교차로 출구 막힘";}}
  if(delta<0&&e.rules.Exists(r=>r.rule=="L09"&&r.verdict==LawVerdict.Violation)&&e.Deduction==1)return "정지선 침범";
  if(delta<0&&e.rules.Exists(r=>r.rule=="L10"&&r.verdict==LawVerdict.Violation))return "역주행";
  string action=string.IsNullOrEmpty(e.action)?"법규":Reason(e.action);return action+(action.Contains("위반")||action.Contains("준수")?"":delta>0?" 준수":" 위반");
 }
}
