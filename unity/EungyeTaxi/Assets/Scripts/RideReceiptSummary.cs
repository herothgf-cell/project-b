using System.Collections.Generic;
public static class RideReceiptSummary {
 public static int InitialFare(PassengerResult r){return r.initialFare>0?r.initialFare:r.quoted;}
 public static string Money(PassengerResult r){int change=r.paid-InitialFare(r);return "기본 운임  "+InitialFare(r).ToString("N0")+"원\n보너스  "+(change>0?"+":"")+change.ToString("N0")+"원\n최종 운임  "+r.paid.ToString("N0")+"원";}
 public static string Rule(PassengerResult r){if(string.IsNullOrEmpty(r.bonusReason))return "";bool late=r.baseFare==0&&r.quoted>0;string baseReason=late?"기한 초과 0원":r.score>=r.required?"100%":"70%";string bonusReason=late?"기한 초과 0원":r.satisfaction>=r.required?"지급":"0원";return "기본 운임: 운전 만족도 M "+r.score+" / 요구 R "+r.required+" → "+baseReason+"\n보너스: 승객 만족도 S "+r.satisfaction+" / 요구 R "+r.required+" → "+bonusReason;}
 public static string Factors(PassengerResult r){
  var lines=new List<string>();if(r.cleanBonus!=0)lines.Add(ScoreFeedback.Text("청결",r.cleanBonus));if(r.driveDelta!=0)lines.Add(ScoreFeedback.Text("주행",r.driveDelta));if(r.satisfactionDelta!=0)lines.Add(ScoreFeedback.Text(r.satisfactionDelta>0?"요청 이행":"요청 미이행",r.satisfactionDelta));if(r.timeDelta!=0)lines.Add(ScoreFeedback.Text("도착 시간",r.timeDelta));return lines.Count==0?"만족도 변화 없음":string.Join("\n",lines.ToArray());
 }
}
