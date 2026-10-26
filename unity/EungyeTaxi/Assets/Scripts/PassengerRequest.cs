public enum RequestKind { Quiet, Eta, Comfort, OnTime, LargeRoad, ShortRoad, SafeStop, Empathy, RouteAdvice, Interview, Music, FareHelp, LocalChat, DestinationChange, ViaStop, LostItem, RejectIllegal }
[System.Serializable] public sealed class PassengerRequestSnapshot {public string trip,response;public RequestKind kind;public bool resolved,accepted,opportunity,finished,safety,goalRecorded;public int satisfactionDelta;}
public class PassengerRequest {
 public string Trip {get;private set;} public RequestKind Kind {get;private set;}
 public bool Resolved {get;private set;} public bool Accepted {get;private set;} public bool SafetyDeferred {get{return safety;}}
 public int SatisfactionDelta {get;private set;}
 bool opportunity,finished,safety,goalRecorded;string response="";
 public PassengerRequestSnapshot Snapshot(){return new PassengerRequestSnapshot{trip=Trip,kind=Kind,response=response,resolved=Resolved,accepted=Accepted,opportunity=opportunity,finished=finished,safety=safety,goalRecorded=goalRecorded,satisfactionDelta=SatisfactionDelta};}
 public void Restore(PassengerRequestSnapshot s){if(s==null||!System.Enum.IsDefined(typeof(RequestKind),s.kind)||s.satisfactionDelta< -10||s.satisfactionDelta>8)throw new System.ArgumentException("Invalid passenger request");Trip=s.trip;Kind=s.kind;response=s.response??"";Resolved=s.resolved;Accepted=s.accepted;opportunity=s.opportunity;finished=s.finished;safety=s.safety;goalRecorded=s.goalRecorded;SatisfactionDelta=s.satisfactionDelta;}
 public string Id {get{return Kind<=RequestKind.Eta?Kind.ToString():Kind<=RequestKind.SafeStop?"DR0"+((int)Kind-1):Kind<=RequestKind.LocalChat?"CH0"+((int)Kind-6):"EV0"+((int)Kind-12);}}
 public bool Begin(string trip,RequestKind kind){if(Trip==trip)return false;Trip=trip;Kind=kind;Resolved=Accepted=opportunity=finished=safety=goalRecorded=false;SatisfactionDelta=0;response="";return true;}
 public void ObserveOpportunity(bool stopped){opportunity|=stopped;}
 public void Accept(){Accept(Kind==RequestKind.Quiet?"조용히 운행하겠습니다":Kind==RequestKind.Eta?"예상 도착 시간을 안내했습니다":"응답 완료");}
 public void Accept(string text){if(Resolved||finished)return;Resolved=Accepted=true;response=text;}
 public void DeferSafety(){if(Resolved||finished)return;Resolved=true;safety=true;response="안전상 보류 · 감점 없음";}
 public void CompleteGoal(bool success,string reason){if(!Accepted||finished||goalRecorded)return;goalRecorded=true;SatisfactionDelta=Delta(Kind,success);response+=(success?" · 달성: ":" · 미달: ")+reason;}
 public void CompleteUnknown(string reason){if(!Accepted||finished||goalRecorded)return;goalRecorded=true;response+=" · 판정 불가: "+reason+" · 만족도 변화 없음";}
 public static int Delta(RequestKind kind,bool success){return kind==RequestKind.Quiet?(success?5:0):kind==RequestKind.Eta||kind==RequestKind.LostItem||kind==RequestKind.RejectIllegal?0:kind<=RequestKind.SafeStop?(success?8:-10):kind<=RequestKind.LocalChat?(success?5:-5):(success?6:-8);}
 public static float EtaSeconds(float eta,int option){if(float.IsNaN(eta)||float.IsInfinity(eta)||eta<0)return float.NaN;return eta+System.Math.Max(-1,System.Math.Min(1,option))*System.Math.Min(30,eta*.1f);}
 public static string EtaComparison(float predicted,float actual){float difference=actual-predicted;return difference< -15?"예상보다 빠름":difference>15?"늦어짐":"예상 범위";}
 public string ResponseText(){return (Kind<=RequestKind.Eta?"":Id+" · ")+(response.Length>0?response:Resolved?"응답 완료":"응답 대기");}
 public int Finish(){if(finished)return 0;finished=true;int deduction=Kind==RequestKind.Quiet&&!Resolved&&opportunity?5:0;if(!Resolved){bool ignored=opportunity&&(Kind==RequestKind.Empathy||Kind==RequestKind.Interview||Kind==RequestKind.LocalChat);if(ignored)SatisfactionDelta=-5;response=deduction>0?"정차 응답 기회 후 미응답 -5점":ignored?"미달: 안전한 응답 기회 후 미응답 · 하차 만족도 -5점":"안전한 응답 없이 운송 종료 · 감점 없음";Resolved=true;}return deduction;}
}
// Settlement is a value calculated once at delivery from final passenger satisfaction.
[System.Serializable] public class PassengerReward {
 public int boardingDrivingScore,drivingAdjustedFare,comfortDelta;
 public static int AdjustedFare(int fare,int score){score=System.Math.Max(0,System.Math.Min(100,score));return score<=70?(int)((long)fare*(630+score)/700/10*10):(int)((long)fare*(300+score-70)/300/10*10);}
 public static PassengerReward CalculateWithDrivingScore(int manner,int required,int fare,int delta,bool onTime,int start,int clean,float reference,float actual,bool unsafeDriving,int boardingScore,int comfort){
  var r=CalculateDetailed(manner,required,fare,0,delta,onTime,start,clean,reference,actual,unsafeDriving,comfort);
  r.farePolicyVersion=3;r.boardingDrivingScore=boardingScore;r.drivingAdjustedFare=AdjustedFare(fare,boardingScore);
  int missing=System.Math.Max(0,required-r.satisfaction),deduction=System.Math.Min(200,missing*5);
  r.baseFare=!onTime?0:missing==0?r.drivingAdjustedFare:(int)(((long)r.drivingAdjustedFare*(1000-deduction)/10000)*10);return r;
 }
 // Policy4 applies only to newly boarded rides; saved policy2/3 rides keep their agreement.
 public static PassengerReward CalculateCurrentFare(int manner,int required,int fare,int delta,bool onTime,int start,int clean,float reference,float actual,bool unsafeDriving,int boardingScore,int comfort){
  var r=CalculateWithDrivingScore(manner,required,fare,delta,onTime,start,clean,reference,actual,unsafeDriving,boardingScore,comfort);
  r.farePolicyVersion=4;
  if(r.satisfaction==0){r.baseFare=r.bonus=0;if(onTime)r.bonusReason="승객 만족도 0 · 운임 전액 감액";}
  else if(r.satisfaction<required){r.bonus=0;r.baseFare=onTime?(int)((long)System.Math.Min(fare,r.drivingAdjustedFare)*r.satisfaction/required/10*10):0;}
  return r;
 }
 public int baseFare,bonus,satisfaction,satisfactionDelta,excessScore,sourceManner,requiredScore,farePolicyVersion,quotedFare;public string bonusReason="";
 public int startSatisfaction,startDeficit,cleanBonus,driveDelta,timeDelta;public float referenceSeconds,actualSeconds;public string timeReason="";
 public int Total {get{return baseFare+bonus;}}
 public static PassengerReward Calculate(RequestKind kind,int manner,int required,int fare,int paid,int delta,bool onTime,bool lawClear){
  return CalculateDetailed(manner,required,fare,paid,delta,onTime,100,0,0,0,false);
 }
 // Signed legacy field: positive means below R, zero means at R, negative means above R.
 public static int Deficit(string trip){return new[]{-12,-8,-4,0,4,8,12}[(int)(DrunkRideState.Hash(trip+":satisfaction")%7)];}
 public static PassengerReward CalculateDetailed(int manner,int required,int fare,int paid,int delta,bool onTime,int start,int clean,float reference,float actual,bool unsafeDriving,int comfort=0){
  bool valid=reference>=1&&!float.IsNaN(reference)&&!float.IsInfinity(reference)&&actual>=0&&!float.IsNaN(actual)&&!float.IsInfinity(actual);int time=valid&&!unsafeDriving?System.Math.Max(0,System.Math.Min(12,(int)System.Math.Round(60*(reference-actual)/reference,System.MidpointRounding.AwayFromZero))):0;
  var r=new PassengerReward{comfortDelta=comfort,farePolicyVersion=2,quotedFare=fare,sourceManner=manner,requiredScore=required,startSatisfaction=start,cleanBonus=clean,driveDelta=manner-100,satisfactionDelta=delta,timeDelta=time,referenceSeconds=reference,actualSeconds=actual,timeReason=!valid?"기준 ETA 없음/1초 미만":unsafeDriving?"위험 운전으로 시간 가산 없음":time>0?"안전한 조기 도착":"조기 도착 가산 없음"};r.satisfaction=PassengerScoreView.Calculate(start,r.driveDelta,delta,time,comfort);r.excessScore=System.Math.Max(0,r.satisfaction-required);r.bonusReason=!onTime?"도착 기한 초과":r.satisfaction<required?"요구 만족도 미달":"요구 만족도 도달 · 가중 보너스";
  if(onTime){int missing=System.Math.Max(0,required-r.satisfaction);int deductionPermille=System.Math.Min(200,missing*5);r.baseFare=missing==0?fare:(int)(((long)fare*(1000-deductionPermille)/10000)*10);}
  if(onTime&&r.satisfaction>=required&&required<100){int weight=required==60?800:required==70?1000:required==85?1300:1000;int cap=(int)(((long)System.Math.Min(500,fare/10)*weight/10000)*10);r.bonus=(int)((long)cap*(100-required+4*r.excessScore)/(5*(100-required))/10)*10;}return r;
 }
 public string Breakdown {get{var view=PassengerScoreView.Settled(this);return view.Baseline+"\n"+view.Breakdown;}}
 public string Summary {get{string fare=farePolicyVersion>=2&&baseFare>0?"약정 "+quotedFare.ToString("N0")+"원 · 요구 미달 "+System.Math.Max(0,requiredScore-satisfaction)+"점 · 감액 "+System.Math.Max(0,quotedFare-baseFare).ToString("N0")+"원 · ":"약정 운임과 별도 · ";if(farePolicyVersion>=3)fare="원약정 "+quotedFare.ToString("N0")+"원 · 승차 운전 점수 "+boardingDrivingScore+" · 보정 "+(drivingAdjustedFare-quotedFare).ToString("+0;-0;0")+"원 → "+drivingAdjustedFare.ToString("N0")+"원 · 만족도 감액 "+(baseFare==0?0:System.Math.Max(0,drivingAdjustedFare-baseFare)).ToString("N0")+"원 · ";return fare+"기본 운임 "+baseFare.ToString("N0")+"원 · "+Breakdown+"\n보너스 "+bonus.ToString("N0")+"원 · "+bonusReason+" · 합계 "+Total.ToString("N0")+"원";}}
}

// One presentation value for HUD, notifications and receipts. Time is awarded only at arrival.
public struct PassengerScoreView {
 public static int ComfortGain(int mannerGain,int satisfaction,int required,int recovered){return mannerGain>0?0:System.Math.Max(0,System.Math.Min(2,System.Math.Min(4-recovered,required-satisfaction)));}
 public int value,required,start,clean,drive,request,time,comfort;
 public static int Calculate(int start,int drive,int request,int time,int comfort=0){return System.Math.Max(0,System.Math.Min(100,start+drive+request+time+comfort));}
 public static PassengerScoreView Live(int start,int clean,int required,int manner,int request,int comfort=0){return new PassengerScoreView{value=Calculate(start,manner-100,request,0,comfort),comfort=comfort,required=required,start=start,clean=clean,drive=manner-100,request=request};}
 public static PassengerScoreView Settled(PassengerReward r){return new PassengerScoreView{value=r.satisfaction,comfort=r.comfortDelta,required=r.requiredScore,start=r.startSatisfaction,clean=r.cleanBonus,drive=r.driveDelta,request=r.satisfactionDelta,time=r.timeDelta};}
 public static PassengerScoreView Recorded(PassengerResult r){return new PassengerScoreView{value=r.satisfaction,comfort=r.comfortDelta,required=r.required,start=r.startSatisfaction,clean=r.cleanBonus,drive=r.driveDelta,request=r.satisfactionDelta,time=r.timeDelta};}
 public string Caption {get{return "승객 만족도 "+value+" · 요구 "+required;}}
 public string Baseline {get{return "기본 "+(start-clean)+" + 청결 "+clean+" = 시작 "+start+" (청결 포함)";}}
 public string Breakdown {get{return "시작 "+start+" + 주행 "+drive+" + 요청 "+request+(comfort==0?"":" + 안심 "+comfort)+" + 시간 "+time+" = "+value+" / 요구 "+required;}}
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
