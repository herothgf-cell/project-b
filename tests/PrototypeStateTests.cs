using System;
class PrototypeStateTests {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static void Main(){
  var s=new PrototypeSettings();Check(s.days==3&&s.daySeconds==480&&s.target==30000,"campaign defaults");
  var ids=new PrototypeIds("test");Check(ids.Next("trip")!=ids.Next("trip"),"unique trips");
  var ledger=new LawLedger();
  var e=new LawEvent{ id="exit-1",day=1,family="exit",tripId="",reason="observed insertion" };
  e.practice=new PracticeRoute{points=new float[]{1,2,3,4},junction="fixture"};
  e.Add("L06",LawVerdict.Violation,15);e.Add("L07",LawVerdict.Violation,15);
  Check(ledger.Record(e)&&ledger.Score(1)==85,"one event max deduction");
  e.Add("L04",LawVerdict.Violation,25);Check(ledger.Score(1)==85,"record is immutable copy");
  e.practice.points[0]=99;Check(ledger.Events[0].practice.points[0]==1,"replay template is immutable history");
  Check(!ledger.Record(e)&&ledger.Events.Length==1,"duplicate event ignored");
  Check(ledger.Score(2)==100&&ledger.Events.Length==1,"daily reset retains history");
  var bypass=new LawEvent{id="exit-2",day=1,family="exit"};bypass.Add("L06",LawVerdict.NoOpportunity,15);ledger.Record(bypass);
  Check(ledger.ValidCount("exit","L06")==1,"no opportunity excluded");
  Check(ledger.Summary("exit","L06").Contains("기록 부족"),"do not infer from few observations");
  var o=new RoadObservation{forward=SignalColor.Red,known=true,trafficClear=true};
  Check(!o.MayTurnRight(false)&&o.MayTurnRight(true),"R1 stop requirement");
  o.forward=SignalColor.Green;Check(o.MayTurnRight(false),"R2 clear green");
  o.dedicated=true;o.right=SignalColor.Red;Check(!o.MayTurnRight(true),"R3 dedicated red");
  o.right=SignalColor.Green;Check(o.MayTurnRight(false),"R4 arrow green");
  o.pedestrian=PedestrianState.Intending;Check(!o.MayTurnRight(true),"R5 intention protects pedestrian");
  o.pedestrian=PedestrianState.Crossing;Check(!o.MayTurnRight(true),"R5 crossing");
  o.known=false;Check(!o.MayTurnRight(true),"unknown is not permission");
  var r=LawEvaluator.RightTurn("r5",1,o,false);Check(r.rules[0].verdict==LawVerdict.Deferred,"unknown evaluation deferred");
  o.known=true;o.dedicated=false;o.forward=SignalColor.Red;
  r=LawEvaluator.RightTurn("r5",1,o,false);ledger.Record(r);Check(r.Deduction==25,"multiple rules use highest deduction");
  var exit=LawEvaluator.Exit("cut",1,true,true,true,true);Check(exit.Deduction==15&&exit.rules.Count==2,"insertion and interference one event");
  exit=LawEvaluator.Exit("bypass",1,true,false,false,false);Check(exit.rules[0].verdict==LawVerdict.NoOpportunity,"bypass not scored as compliant lane change");
  o.pedestrian=PedestrianState.None;r=LawEvaluator.RightTurn("yield",1,o,true,null,true);Check(r.rules.Find(x=>x.rule=="L04").verdict==LawVerdict.Compliant,"observed pedestrian yield retains evaluation opportunity");
  var abandoned=LawEvaluator.Exit("abort-before",1,true,false,false,false);Check(abandoned.Deduction==0&&abandoned.rules.TrueForAll(f=>f.verdict==LawVerdict.NoOpportunity),"indicator or approach without lane entry is no violation");
  var returned=LawEvaluator.Exit("return-after",1,true,true,true,true);Check(returned.Deduction==15,"returning after completed insertion does not erase violation");
  Console.WriteLine("PASS prototype identities, independent law ledger and R1-R5 observations");
 }
}
