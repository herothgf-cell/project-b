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
  Console.WriteLine("PASS one request/trip, safe defer, opportunity and one-time deduction");
 }
}
