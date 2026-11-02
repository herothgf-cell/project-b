using System;
public static class PassengerSituationTests {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok){Console.WriteLine("FAIL "+reason);Environment.Exit(1);}}
 static PassengerSituationState Fresh(){return PassengerSituationState.Create("trip",0,0,0);}
 public static void Main(){
  var s=Fresh();Check(!s.Observe(17.9f,true,false,true,false,0,0,0,0,false),"signal wait below18 seconds has no reaction");
  Check(s.Observe(.1f,true,false,true,false,0,0,0,0,false)&&s.Pending==PassengerSituationKind.SignalWait,"observed signal wait18 seconds opens reaction");
  Check(!s.Respond(0,false)&&s.Pending!=PassengerSituationKind.None,"motion cannot answer or discard reaction");
  Check(s.Respond(0,true)&&s.Reactions==1,"safe explanation responds once");
  Check(!s.Observe(1,false,true,true,false,0,1,0,0,false),"20second cooldown suppresses observed jolt");
  Check(!s.Observe(19,false,true,true,false,0,1,0,0,false),"consumed jolt does not replay after cooldown");
  Check(s.Observe(.1f,false,true,true,false,0,2,0,0,false)&&s.Pending==PassengerSituationKind.Jolt,"fresh observed brake reacts after cooldown");
  Check(s.Respond(1,true)&&s.Quiet,"quiet choice is remembered");Check(!s.Observe(30,true,false,true,false,0,3,0,0,false),"quiet prevents followup");
  var budget=Fresh();Check(!budget.Observe(30,true,false,true,false,3,0,0,0,false),"reserved total3-question budget blocks reaction");
  var emergency=Fresh();Check(emergency.Observe(1,false,true,true,false,0,0,0,1,false)&&emergency.Pending==PassengerSituationKind.NecessaryBrake,"necessary brake has neutral safety wording");
  Check(emergency.Dialogue.Contains("안전")&&!emergency.Dialogue.Contains("잘못"),"necessary braking never blames driver");
  var copy=emergency.Copy();Check(copy.Pending==emergency.Pending&&copy.Respond(0,true),"pending choice survives snapshot copy");
  Check(!copy.Observe(30,false,true,true,false,0,0,0,1,false),"restored consumed emergency does not replay");
  var pause=Fresh();Check(!pause.Observe(30,true,false,true,false,0,0,0,0,true)&&pause.Elapsed==0,"pause freezes observation time");
  var blocked=Fresh();Check(!blocked.Observe(18,true,false,false,false,0,0,0,0,false),"stronger warning postpones reaction without answering");
  Check(blocked.Observe(.1f,true,false,true,false,0,0,0,0,false),"signal reaction waits for available display");
  var max=Fresh();for(int i=0;i<2;i++){Check(max.Observe(21,false,true,true,false,0,i+1,0,0,false),"two reactions allowed");max.Respond(0,true);}Check(!max.Observe(21,false,true,true,false,0,3,0,0,false),"third reaction prohibited");
  var e=PassengerErrandState.Agree("trip","via","original");Check(e.Remaining==20&&!e.Waiting&&!e.Completed,"agreement does not skip travel");
  Check(!e.Start(false)&&e.Start(true),"physical safe stop starts waiting");
  Check(!e.Advance(30,false,false)&&e.Remaining==20,"departing bay pauses work");
  Check(!e.Advance(30,true,true)&&e.Remaining==20,"pause adds no errand time");
  Check(!e.Advance(7,true,false)&&e.Remaining==13,"active safe waiting advances real time");
  var restored=e.Copy();restored.Validate();Check(restored.Advance(13,true,false)&&restored.Completed&&restored.OriginalDestinationId=="original"&&restored.TripId=="trip","restore completes original same-trip errand");
  Check(!restored.Advance(20,true,false)&&!restored.Start(true),"completion cannot restart or award twice");
  Check(e.Remaining==13,"snapshot copy is independent");
  Console.WriteLine("PASS PassengerSituationTests "+checks);
 }
}
