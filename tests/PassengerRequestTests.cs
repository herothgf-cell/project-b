using System;
class PassengerRequestTests {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static void Main(){
  var r=new PassengerRequest();r.Begin("trip1",RequestKind.Quiet);
  Check(!r.Begin("trip1",RequestKind.Eta),"one request total per trip");
  Check(r.Finish()==0,"no opportunity is not refusal");
  r.Begin("trip2",RequestKind.Quiet);r.ObserveOpportunity(true);r.DeferSafety();Check(r.Finish()==0,"safety defer exempt");
  r.Begin("trip3",RequestKind.Quiet);r.ObserveOpportunity(true);Check(r.Finish()==5&&r.Finish()==0,"unhandled with chance once");
  r.Begin("trip4",RequestKind.Eta);r.ObserveOpportunity(true);Check(r.Finish()==0,"ETA no penalty");
  r.Begin("trip5",RequestKind.Quiet);r.Accept();Check(r.Finish()==0,"accepted quiet");
  r.Begin("trip6",RequestKind.Comfort);r.Accept("부드러운 운전");r.CompleteGoal(false,"급가속 1회");Check(r.ResponseText().Contains("DR01")&&r.ResponseText().Contains("미달")&&r.ResponseText().Contains("급가속 1회"),"driving result retains actual reason");Check(r.Finish()==0,"request goal no duplicate penalty");r.CompleteGoal(true,"late mutation");Check(!r.ResponseText().Contains("late mutation"),"finished result immutable");
  foreach(RequestKind kind in Enum.GetValues(typeof(RequestKind))){r.Begin("safe-"+kind,kind);r.ObserveOpportunity(true);r.DeferSafety();Check(r.Finish()==0&&!r.Accepted,"all safe refusals exempt");}
  r.Begin("illegal",RequestKind.RejectIllegal);r.Accept("신호 준수 · 불법 요구 거절");Check(r.Id=="EV04"&&r.Finish()==0,"illegal request refusal not penalty");
  r.Begin("unanswered",RequestKind.DestinationChange);Check(r.Finish()==0&&r.Resolved&&r.ResponseText().Contains("감점 없음"),"unanswered expansion result preserved");
  Console.WriteLine("PASS one request/trip, safe defer, opportunity and one-time deduction");
 }
}
