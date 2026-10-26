using System;
class PassengerRequestTests {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static void Main(){
  foreach(RequestKind kind in Enum.GetValues(typeof(RequestKind))){
   var choices=PassengerRequestChoices.For(kind);Check(choices.Length>=2&&choices.Length<=4,"visible replies for "+kind);
   Check(choices[choices.Length-1].reply==PassengerReply.Defer,"safety defer available "+kind);
   foreach(var choice in choices)Check(!string.IsNullOrEmpty(choice.label)&&!string.IsNullOrEmpty(choice.effect),"choice and actual effect explained "+kind);
   Check(choices[0].reply==(kind==RequestKind.Eta?PassengerReply.EtaFaster:PassengerReply.Accept),"primary dispatch "+kind);
  }
  Check(PassengerRequestChoices.For(RequestKind.Eta).Length==4,"three ETA buttons and defer");
  Check(PassengerRequestChoices.For(RequestKind.Comfort)[0].effect.Contains("-4"),"comfort warns actual stronger penalty");
  Check(PassengerRequestChoices.For(RequestKind.Music)[0].effect.Contains("변경 없음"),"music must not claim audio changed");
  Check(PassengerRequestChoices.For(RequestKind.RejectIllegal)[0].label.Contains("지켜"),"illegal request has no accept violation action");
  var r=new PassengerRequest();r.Begin("trip1",RequestKind.Quiet);
  Check(PassengerRequest.EtaSeconds(240,-1)==216&&PassengerRequest.EtaSeconds(240,0)==240&&PassengerRequest.EtaSeconds(240,1)==264,"three ETA estimates");
  Check(PassengerRequest.EtaComparison(216,232).Contains("늦어짐")&&PassengerRequest.EtaComparison(216,201).Contains("예상 범위"),"ETA tolerance");
  var reward=PassengerReward.Calculate(RequestKind.Comfort,85,85,5000,5000,8,true,true);
  Check(reward.satisfaction==93&&reward.bonus==400&&reward.Total==5400,"high requirement bonus 10 won floor");
  Check(PassengerReward.Calculate(RequestKind.Comfort,100,85,5000,5000,8,true,true).bonus==650,"high requirement cap");
  Check(PassengerReward.Calculate(RequestKind.Comfort,80,85,5000,3500,8,true,true).baseFare==5000&&PassengerReward.Calculate(RequestKind.Comfort,80,85,5000,3500,8,true,true).bonus==230,"final S replaces old M fare threshold");
  Check(PassengerReward.Calculate(RequestKind.Comfort,100,85,5000,0,8,false,true).bonus==0,"late excludes bonus");
  Check(PassengerReward.Calculate(RequestKind.Comfort,100,85,5000,5000,8,true,false).bonus==650,"law flag does not erase independent satisfaction bonus");
  foreach(var noBonus in new[]{RequestKind.Music,RequestKind.FareHelp,RequestKind.LostItem,RequestKind.RejectIllegal,RequestKind.Quiet,RequestKind.Eta})Check(PassengerReward.Calculate(noBonus,100,60,9000,9000,8,true,true).bonus==400,"all request kinds use weighted bonus "+noBonus);
  foreach(RequestKind category in new[]{RequestKind.Comfort,RequestKind.Empathy,RequestKind.DestinationChange}){var q=new PassengerRequest();q.Begin("outcome-"+category,category);q.Accept();q.CompleteGoal(true,"observed");q.Finish();Check(q.SatisfactionDelta==(category==RequestKind.Comfort?8:category==RequestKind.Empathy?5:6),"category positive delta");q.CompleteGoal(false,"late");Check(q.SatisfactionDelta>0,"result delta immutable");}
  var ignored=new PassengerRequest();ignored.Begin("ignored",RequestKind.Empathy);ignored.ObserveOpportunity(true);Check(ignored.Finish()==0&&ignored.SatisfactionDelta==-5,"CH ignore changes only satisfaction");
  var unknown=new PassengerRequest();unknown.Begin("unknown",RequestKind.LargeRoad);unknown.Accept();unknown.CompleteUnknown("우회 사유 불명");unknown.Finish();Check(unknown.SatisfactionDelta==0,"unknown neutral");
  Check(MannerLedger.MotionPenalty(true)==4&&MannerLedger.MotionPenalty(false)==2,"comfort replaces motion deduction");
  Check(float.IsNaN(PassengerRequest.EtaSeconds(float.PositiveInfinity,0))&&float.IsNaN(PassengerRequest.EtaSeconds(-1,0)),"unavailable ETA blocked");
  Check(PassengerRequest.EtaSeconds(600,-1)==570&&PassengerRequest.EtaComparison(216,200).Contains("예상보다 빠름"),"ETA cap and early arrival");
  Check(PassengerReward.Calculate(RequestKind.Empathy,100,70,9000,9000,5,true,true).bonus==500&&PassengerReward.Calculate(RequestKind.DestinationChange,100,70,9000,9000,6,true,true).bonus==500,"weighted CH and EV cap");
  Check(PassengerReward.Calculate(RequestKind.Comfort,77,85,5000,5000,8,true,true).bonus==130,"exact requirement receives minimum bonus");
  foreach(var category in new[]{RequestKind.Comfort,RequestKind.Empathy,RequestKind.DestinationChange}){var q=new PassengerRequest();q.Begin("failed-"+category,category);q.Accept();Check(q.SatisfactionDelta==0,"selection alone not reward");q.CompleteGoal(false,"observed failure");q.Finish();Check(q.SatisfactionDelta==(category==RequestKind.Comfort?-10:category==RequestKind.Empathy?-5:-8),"category negative delta");}
  foreach(RequestKind kind in Enum.GetValues(typeof(RequestKind))){var q=new PassengerRequest();q.Begin("deferred-"+kind,kind);q.ObserveOpportunity(true);q.DeferSafety();q.Finish();Check(q.SatisfactionDelta==0,"every safety defer neutral");}
  var settledTrip=new FareTrip();settledTrip.Accept(5000,100);settledTrip.Board();int fare=settledTrip.Complete();Check(PassengerReward.Calculate(RequestKind.Comfort,85,85,5000,fare,8,true,true).Total==5400&&settledTrip.Complete()==0,"delivery state gates one-time base and bonus settlement");
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
