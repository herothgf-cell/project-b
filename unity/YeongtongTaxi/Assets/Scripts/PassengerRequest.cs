public enum RequestKind { Quiet, Eta, Comfort, OnTime, LargeRoad, ShortRoad, SafeStop, Empathy, RouteAdvice, Interview, Music, FareHelp, LocalChat, DestinationChange, ViaStop, LostItem, RejectIllegal }
public class PassengerRequest {
 public string Trip {get;private set;} public RequestKind Kind {get;private set;}
 public bool Resolved {get;private set;} public bool Accepted {get;private set;} public bool SafetyDeferred {get{return safety;}}
 bool opportunity,finished,safety;string response="";
 public string Id {get{return Kind<=RequestKind.Eta?Kind.ToString():Kind<=RequestKind.SafeStop?"DR0"+((int)Kind-1):Kind<=RequestKind.LocalChat?"CH0"+((int)Kind-6):"EV0"+((int)Kind-12);}}
 public bool Begin(string trip,RequestKind kind){if(Trip==trip)return false;Trip=trip;Kind=kind;Resolved=Accepted=opportunity=finished=safety=false;response="";return true;}
 public void ObserveOpportunity(bool stopped){opportunity|=stopped;}
 public void Accept(){Accept(Kind==RequestKind.Quiet?"조용히 운행하겠습니다":Kind==RequestKind.Eta?"예상 도착 시간을 안내했습니다":"응답 완료");}
 public void Accept(string text){if(Resolved||finished)return;Resolved=Accepted=true;response=text;}
 public void DeferSafety(){if(Resolved||finished)return;Resolved=true;safety=true;response="안전상 보류 · 감점 없음";}
 public void CompleteGoal(bool success,string reason){if(!Accepted||finished)return;response+=(success?" · 달성: ":" · 미달: ")+reason;}
 public string ResponseText(){return (Kind<=RequestKind.Eta?"":Id+" · ")+(response.Length>0?response:Resolved?"응답 완료":"응답 대기");}
 public int Finish(){if(finished)return 0;finished=true;int deduction=Kind==RequestKind.Quiet&&!Resolved&&opportunity?5:0;if(!Resolved){response=deduction>0?"정차 응답 기회 후 미응답 -5점":"안전한 응답 없이 운송 종료 · 감점 없음";Resolved=true;}return deduction;}
}
