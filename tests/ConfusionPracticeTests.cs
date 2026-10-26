using System;
class ConfusionPracticeTests {
 static void Check(bool b,string n){if(!b)throw new Exception(n);}
 static ConfusionPracticeState New(int v){return new ConfusionPracticeState(v);}
 static void Main(){try{
  var red=New(8);red.Advance(180,false,false);Check(red.Signal==SignalColor.Red,"flashing red persists on approach");red.Enter(false);red.Complete(true);Check(red.Event("red",1).Deduction>0,"rolling red flash violation");
  red=New(8);red.Enter(true);red.Complete(true);Check(red.Event("stop",1).Deduction==0,"red flash full stop permits passage");Check(red.Event("new-recovery",1).recoveryPolicyVersion==1&&LawLedger.ScoreAfter(70,red.Event("new-recovery",1))==73,"production confusion event uses three point recovery");
  var yellow=New(9);yellow.Enter(false);yellow.Complete(true);Check(yellow.Event("yellow",1).Deduction==0,"yellow flash does not require full stop");Check(yellow.Feedback().issues.Contains("시선"),"cannot infer driver attention");
  var blocked=New(10);blocked.Advance(180,false,false);Check(blocked.Blocked,"queue cannot expire on distant approach");blocked.Enter(true);blocked.Advance(10,true,true);blocked.Complete(true);Check(blocked.Event("bad",1).Deduction>0,"later opening does not erase illegal entry");
  blocked=New(10);blocked.Advance(5,true,true);Check(!blocked.Blocked,"queue opens after local wait");blocked.Enter(true);blocked.Complete(true);Check(blocked.Event("wait",1).Deduction==0,"wait outside box then enter");
  var clear=New(11);clear.Enter(false);clear.Complete(true);Check(clear.Event("clear",1).Deduction==0,"green clear does not demand stop");
  var left=New(12);left.Enter(true);left.Advance(5,true,true);left.Conflict();left.Complete(true);Check(left.Event("left-red",1).Deduction>0,"red entry retained after later green");
  left=New(12);left.Advance(5,true,true);left.Enter(true);left.Conflict();left.Complete(true);Check(left.Event("left-green",1).Deduction==0,"wait red then green left");
  left=New(13);left.Enter(false);left.Conflict();left.Advance(20,true,true);left.Complete(true);Check(left.Event("yield-bad",1).Deduction>0,"failure to yield retained");
  left=New(13);left.Advance(13,true,true);left.Enter(true);left.Conflict();left.Complete(true);Check(left.Event("yield-good",1).Deduction==0,"yield then left");
  left=New(14);Check(!left.Finished&&left.Feedback().outcome.Contains("미완료"),"no passage not success");left.Enter(false);left.Conflict();left.Complete(false);Check(left.Feedback().outcome.Contains("경로"),"wrong route not success");
  var unknown=New(8);unknown.Known=false;unknown.Enter(false);unknown.Complete(true);Check(unknown.Event("unknown",1).rules[0].verdict==LawVerdict.Deferred,"unknown defers");
  var observed=New(10);observed.Advance(20,true,true);observed.ObserveTraffic(true,false);observed.Enter(true);observed.Complete(true);Check(observed.Event("actual-block",1).Deduction>0,"actual observed blockage overrides fixture timer");
  var gap=New(13);gap.ObserveTraffic(false,false);gap.Enter(false);gap.Conflict();gap.Complete(true);Check(gap.Event("actual-gap",1).Deduction==0,"actual clear oncoming path overrides fixture timer");
  Check(New(10).Blocked&&New(13).Oncoming,"retry restores hazards");
  var preserved=ConfusionPracticeState.Restore(observed.Snapshot());Check(preserved.Elapsed==observed.Elapsed&&preserved.EntryBlocked&&preserved.Finished&&preserved.Event("restore-block",1).Deduction==observed.Event("before-block",1).Deduction,"save preserves observed illegal entry after later timing changes");
  preserved=ConfusionPracticeState.Restore(gap.Snapshot());Check(!preserved.Oncoming&&preserved.ConflictSeen&&preserved.CorrectRoute&&preserved.Event("restore-clear",1).Deduction==0,"save preserves actual clear observation and completed conflict");
  var walking=new RightPracticeState(6);walking.Advance(2.5f,true);var pedestrian=RightPracticeState.Restore(walking.Snapshot());Check(pedestrian.Started&&pedestrian.Elapsed==2.5f&&pedestrian.CrossingProgress==walking.CrossingProgress&&pedestrian.Observe().pedestrian==PedestrianState.Crossing,"PS09 pedestrian restore keeps elapsed crossing phase");
  Console.WriteLine("PASS confusion practice timing, entry snapshots, yield, wrong route, unknown and feedback");
 }catch(Exception e){Console.WriteLine(e);Environment.Exit(1);}}
}
