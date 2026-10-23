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

// Mouse and keyboard share these replies; descriptions match implemented prototype effects.
public enum PassengerReply { Accept, Alternative, EtaFaster, EtaNormal, EtaRelaxed, Defer }
public sealed class PassengerRequestChoice {
 public readonly PassengerReply reply;public readonly string label,effect;
 public PassengerRequestChoice(PassengerReply reply,string label,string effect){this.reply=reply;this.label=label;this.effect=effect;}
}
public static class PassengerRequestChoices {
 static PassengerRequestChoice C(PassengerReply reply,string label,string effect){return new PassengerRequestChoice(reply,label,effect);}
 public static PassengerRequestChoice[] For(RequestKind kind){
  var choices=new System.Collections.Generic.List<PassengerRequestChoice>();
  switch(kind){
   case RequestKind.Quiet:choices.Add(C(PassengerReply.Accept,"조용히 모시겠습니다","후속 대사 억제 · 운임/기한 유지"));break;
   case RequestKind.Eta:choices.Add(C(PassengerReply.EtaFaster,"조금 빠르게 예상해요","내비보다 최대30초 빠른 예상 안내"));choices.Add(C(PassengerReply.EtaNormal,"내비 예상에 맞춰 안내","현재 내비 ETA로 안내"));choices.Add(C(PassengerReply.EtaRelaxed,"조금 여유롭게 예상해요","내비보다 최대30초 여유 있는 예상 안내"));break;
   case RequestKind.Comfort:choices.Add(C(PassengerReply.Accept,"부드럽게 모시겠습니다","수락 뒤 불필요한 급가감속 각 -4점"));break;
   case RequestKind.OnTime:choices.Add(C(PassengerReply.Accept,"규칙을 지키며 가겠습니다","기존 기한과 위반 여부 관찰 · 기한 유지"));break;
   case RequestKind.LargeRoad:choices.Add(C(PassengerReply.Accept,"가능한 큰길로 안내할게요","큰길 경로 적용 · 실제 큰길 비율 관찰"));break;
   case RequestKind.ShortRoad:choices.Add(C(PassengerReply.Accept,"짧은 길로 안내할게요","합법 최단 경로 적용 · 실제 거리 관찰"));break;
   case RequestKind.SafeStop:choices.Add(C(PassengerReply.Accept,"안전한 하차 지점으로 변경","미리 본 목적지/운임/기한으로 변경"));break;
   case RequestKind.Empathy:choices.Add(C(PassengerReply.Accept,"이야기 들어드릴게요","공감 대사 · 하차 때 응대 결과 반영"));choices.Add(C(PassengerReply.Alternative,"조용히 가실까요?","조용한 배려 · 후속 대사 억제"));break;
   case RequestKind.RouteAdvice:choices.Add(C(PassengerReply.Accept,"빠른 길을 추천할게요","빠른 경로 적용 · 운임/기한 유지"));choices.Add(C(PassengerReply.Alternative,"짧은 길을 추천할게요","짧은 경로 적용 · 운임/기한 유지"));break;
   case RequestKind.Interview:choices.Add(C(PassengerReply.Accept,"응원할게요!","격려 대사 · 면접 결과는 평가하지 않음"));choices.Add(C(PassengerReply.Alternative,"조용히 준비하시게 할게요","조용한 배려 · 후속 대사 억제"));break;
   case RequestKind.Music:choices.Add(C(PassengerReply.Accept,"가능한 범위를 안내할게요","프로토는 음악 변경 없음 · 보너스 없음"));break;
   case RequestKind.FareHelp:choices.Add(C(PassengerReply.Accept,"약정 요금을 안내할게요","지급 규칙 안내 · 할인/추가 부채 없음"));choices.Add(C(PassengerReply.Alternative,"결제는 하차 때 확인할게요","동일 운임 유지 · 보너스 없음"));break;
   case RequestKind.LocalChat:choices.Add(C(PassengerReply.Accept,"은계 주변을 다니고 있어요","지역 대사 · 하차 때 응대 결과 반영"));choices.Add(C(PassengerReply.Alternative,"안전하게 모셔다드릴게요","안전 운행 응대 · 동일 운임/목적지"));break;
   case RequestKind.DestinationChange:choices.Add(C(PassengerReply.Accept,"새 목적지로 변경할게요","미리 본 목적지/운임/기한으로 변경"));break;
   case RequestKind.ViaStop:choices.Add(C(PassengerReply.Accept,"안전한 지점에 들를게요","미리 본 약정 적용 · 경유지 F 정차 필요"));break;
   case RequestKind.LostItem:choices.Add(C(PassengerReply.Accept,"보관·신고를 안내할게요","하차 후 텍스트 사건 기록 · 보너스 없음"));choices.Add(C(PassengerReply.Alternative,"안전한 전달 절차 안내","보관/전달 대사 · 반환 퀘스트 없음"));break;
   case RequestKind.RejectIllegal:choices.Add(C(PassengerReply.Accept,"신호는 지켜야 해요","안전 거절 · 위반은 별도 관찰/보상 없음"));break;
   default:throw new System.ArgumentOutOfRangeException("kind");
  }
  choices.Add(C(PassengerReply.Defer,kind==RequestKind.SafeStop||kind==RequestKind.DestinationChange?"원래 목적지로 모실게요":kind==RequestKind.ViaStop?"이번에는 경유가 어려워요":"안전한 곳에서 확인할게요","원래 운행 유지 · 요청 감점/보너스 없음"));return choices.ToArray();
 }
}
