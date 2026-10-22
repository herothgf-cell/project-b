public enum RequestKind { Quiet, Eta, Comfort, OnTime, LargeRoad, ShortRoad, SafeStop, Empathy, RouteAdvice, Interview, Music, FareHelp, LocalChat, DestinationChange, ViaStop, LostItem, RejectIllegal }
public class PassengerRequest {
 public string Trip {get;private set;} public RequestKind Kind {get;private set;}
 public bool Resolved {get;private set;} public bool Accepted {get;private set;} public bool SafetyDeferred {get{return safety;}}
 public int SatisfactionDelta {get;private set;}
 bool opportunity,finished,safety,goalRecorded;string response="";
 public string Id {get{return Kind<=RequestKind.Eta?Kind.ToString():Kind<=RequestKind.SafeStop?"DR0"+((int)Kind-1):Kind<=RequestKind.LocalChat?"CH0"+((int)Kind-6):"EV0"+((int)Kind-12);}}
 public bool Begin(string trip,RequestKind kind){if(Trip==trip)return false;Trip=trip;Kind=kind;Resolved=Accepted=opportunity=finished=safety=goalRecorded=false;SatisfactionDelta=0;response="";return true;}
 public void ObserveOpportunity(bool stopped){opportunity|=stopped;}
 public void Accept(){Accept(Kind==RequestKind.Quiet?"조용히 운행하겠습니다":Kind==RequestKind.Eta?"예상 도착 시간을 안내했습니다":"응답 완료");}
 public void Accept(string text){if(Resolved||finished)return;Resolved=Accepted=true;response=text;}
 public void DeferSafety(){if(Resolved||finished)return;Resolved=true;safety=true;response="안전상 보류 · 감점 없음";}
 public void CompleteGoal(bool success,string reason){if(!Accepted||finished||goalRecorded)return;goalRecorded=true;SatisfactionDelta=Delta(Kind,success);response+=(success?" · 달성: ":" · 미달: ")+reason;}
 public void CompleteUnknown(string reason){if(!Accepted||finished||goalRecorded)return;goalRecorded=true;response+=" · 판정 불가: "+reason+" · 만족도 변화 없음";}
 public static int Delta(RequestKind kind,bool success){return kind<=RequestKind.Eta?0:kind<=RequestKind.SafeStop?(success?8:-10):kind<=RequestKind.LocalChat?(success?5:-5):(success?6:-8);}
 public static float EtaSeconds(float eta,int option){if(float.IsNaN(eta)||float.IsInfinity(eta)||eta<0)return float.NaN;return eta+System.Math.Max(-1,System.Math.Min(1,option))*System.Math.Min(30,eta*.1f);}
 public static string EtaComparison(float predicted,float actual){float difference=actual-predicted;return difference< -15?"예상보다 빠름":difference>15?"늦어짐":"예상 범위";}
 public string ResponseText(){return (Kind<=RequestKind.Eta?"":Id+" · ")+(response.Length>0?response:Resolved?"응답 완료":"응답 대기");}
 public int Finish(){if(finished)return 0;finished=true;int deduction=Kind==RequestKind.Quiet&&!Resolved&&opportunity?5:0;if(!Resolved){bool ignored=opportunity&&(Kind==RequestKind.Empathy||Kind==RequestKind.Interview||Kind==RequestKind.LocalChat);if(ignored)SatisfactionDelta=-5;response=deduction>0?"정차 응답 기회 후 미응답 -5점":ignored?"미달: 안전한 응답 기회 후 미응답 · 하차 만족도 -5점":"안전한 응답 없이 운송 종료 · 감점 없음";Resolved=true;}return deduction;}
}
// Settlement is a value calculated once at delivery; fare policy continues to use M.
[System.Serializable] public class PassengerReward {
 public int baseFare,bonus,satisfaction,satisfactionDelta,excessScore,sourceManner,requiredScore;public string bonusReason="";
 public int Total {get{return baseFare+bonus;}}
 public static PassengerReward Calculate(RequestKind kind,int manner,int required,int fare,int paid,int delta,bool onTime,bool lawClear){
  var result=new PassengerReward{sourceManner=manner,requiredScore=required,baseFare=paid,satisfactionDelta=delta,satisfaction=System.Math.Max(0,System.Math.Min(100,manner+delta))};result.excessScore=System.Math.Max(0,result.satisfaction-required);
  bool eligible=kind>RequestKind.Eta&&kind!=RequestKind.Music&&kind!=RequestKind.FareHelp&&kind!=RequestKind.LostItem&&kind!=RequestKind.RejectIllegal;
  result.bonusReason=!eligible?"이 요청은 보너스 대상 아님":!onTime?"시간 초과":paid!=fare||manner<required?"기본 운임 100% 조건 미달":!lawClear?"확정 법규 위반 있음":delta<=0?"긍정 요청 이행 없음":result.excessScore<=0?"요구 점수 초과 없음":"요구 점수 초과 비율 적용";
  if(eligible&&onTime&&paid==fare&&manner>=required&&lawClear&&delta>0&&required<100){int percent=kind<=RequestKind.SafeStop?10:kind<=RequestKind.LocalChat?5:8;int limit=kind<=RequestKind.SafeStop?500:kind<=RequestKind.LocalChat?300:400;int cap=System.Math.Min(limit,(int)((long)fare*percent/100));result.bonus=(int)((long)cap*result.excessScore/(100-required)/10)*10;}
  return result;
 }
 public string Summary {get{return "기본 운임 "+baseFare.ToString("N0")+"원 · 최종 승객 만족도 "+satisfaction+"점 ("+sourceManner+(satisfactionDelta>=0?"+":"")+satisfactionDelta+") / 요구 "+requiredScore+"점 · 초과 "+excessScore+"점\n감사 보너스 "+bonus.ToString("N0")+"원 · "+bonusReason+" · 합계 "+Total.ToString("N0")+"원";}}
}
