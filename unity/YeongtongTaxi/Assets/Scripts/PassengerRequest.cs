public enum RequestKind { Quiet, Eta }
public class PassengerRequest {
 public string Trip {get;private set;} public RequestKind Kind {get;private set;}
 public bool Resolved {get;private set;} bool opportunity,finished,safety;
 public bool Begin(string trip,RequestKind kind){if(Trip==trip)return false;Trip=trip;Kind=kind;Resolved=opportunity=finished=safety=false;return true;}
 public void ObserveOpportunity(bool stopped){opportunity|=stopped;}
 public void Accept(){if(Resolved||finished)return;Resolved=true;}
 public void DeferSafety(){if(Resolved||finished)return;Resolved=true;safety=true;}
 public string ResponseText(){return safety?"안전상 보류 · 감점 없음":Resolved?(Kind==RequestKind.Quiet?"조용히 운행하겠습니다":"예상 도착 시간을 안내했습니다"):"응답 대기";}
 public int Finish(){if(finished)return 0;finished=true;return Kind==RequestKind.Quiet&&!Resolved&&opportunity?5:0;}
}
