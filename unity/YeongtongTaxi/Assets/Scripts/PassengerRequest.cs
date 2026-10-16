public enum RequestKind { Quiet, Eta }
public class PassengerRequest {
 public string Trip {get;private set;} public RequestKind Kind {get;private set;}
 public bool Resolved {get;private set;} bool opportunity,finished;
 public bool Begin(string trip,RequestKind kind){if(Trip==trip)return false;Trip=trip;Kind=kind;Resolved=opportunity=finished=false;return true;}
 public void ObserveOpportunity(bool stopped){opportunity|=stopped;}
 public void Accept(){Resolved=true;}
 public void DeferSafety(){Resolved=true;}
 public int Finish(){if(finished)return 0;finished=true;return Kind==RequestKind.Quiet&&!Resolved&&opportunity?5:0;}
}
