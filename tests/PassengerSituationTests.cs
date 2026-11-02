using System;
public static class PassengerSituationTests {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok){Console.WriteLine("FAIL "+reason);Environment.Exit(1);}}
 static PassengerSituationState Fresh(){return PassengerSituationState.Create("trip",0,0,0);}
 static PassengerBehaviorState Sleeping(){var p=PassengerBehaviorState.Create("calm-rest",PassengerType.Sleeping);p.Variant=0;p.Respond(0,true);p.Board();p.Advance(21,false,true,.3f,100);return p;}
 static void CalmRestChecks(){
  foreach(string reason in new[]{"quiet","total-budget","reaction-budget","cooldown","modal"}){
   var passenger=Sleeping();var reaction=Fresh();if(reason=="quiet")passenger.Quiet=true;if(reason=="reaction-budget")reaction.Reactions=2;if(reason=="cooldown"){reaction.Elapsed=5;reaction.LastReactionAt=0;}
   Check(reaction.ApplyPhysicalObservation(passenger,1,0,0)&&!passenger.Sleeping,"physical wake despite blocked question: "+reason);
   Check(!reaction.Observe(1,false,true,reason!="modal",passenger.Quiet,reason=="total-budget"?3:1,1,0,0,false)&&reaction.Pending==PassengerSituationKind.None,"question stays suppressed: "+reason);
   passenger.Advance(14,false,true,.4f,100);Check(!passenger.Sleeping,"rest requires full15calm active seconds: "+reason);
   passenger.Advance(1,false,true,.4f,100);Check(passenger.Sleeping&&reaction.Pending==PassengerSituationKind.None&&passenger.Quiet==(reason=="quiet"),"blocked question has neutral bounded return to rest: "+reason);
  }
  var repeated=Sleeping();var events=Fresh();events.ApplyPhysicalObservation(repeated,1,0,0);repeated.Advance(14,false,true,.4f,100);Check(events.ApplyPhysicalObservation(repeated,2,0,0),"fresh jolt resets already-awake calm timer");repeated.Advance(14,false,true,.4f,100);Check(!repeated.Sleeping,"repeat jolt restarts15second calm window");repeated.Advance(1,false,true,.4f,100);Check(repeated.Sleeping,"repeated jolt still returns to rest after calm interval");
  var pending=Sleeping();var question=Fresh();question.ApplyPhysicalObservation(pending,1,0,0);question.Observe(1,false,true,true,false,1,1,0,0,false);pending.Advance(30,false,true,.4f,100,false,true);Check(!pending.Sleeping&&question.Pending!=PassengerSituationKind.None&&pending.JoltCalmRemaining==0,"bounded calm rest does not auto-answer or hide real pending reply");Check(question.Respond(1,true)&&pending.ReassureJolt(true)&&pending.Sleeping&&pending.Quiet,"pending reply still requires deliberate safe response");
  var restored=Sleeping();restored.ObserveJolt();restored.Advance(5,false,true,.4f,100);var saved=restored.Copy();saved.Validate();saved.Advance(99,false,true,.4f,100,true);Check(saved.JoltCalmRemaining==10&&!saved.Sleeping,"pause preserves saved remaining calm time");saved.Advance(10,false,true,.4f,100);Check(saved.Sleeping&&saved.JoltCalmRemaining==0&&restored.JoltCalmRemaining==10,"save copy restores exact calm time without mutating original");
  var legacy=Sleeping();legacy.ObserveJolt();legacy.JoltRestCaptured=false;legacy.JoltCalmRemaining=0;legacy.Advance(14,false,true,.4f,100);Check(!legacy.Sleeping&&legacy.JoltRestCaptured&&legacy.JoltCalmRemaining==1,"old awake save with missing calm fields starts neutral15second recovery");legacy.Advance(1,false,true,.4f,100);Check(legacy.Sleeping,"old awake save recovery completes without a new question");
  var arrived=Sleeping();arrived.ObserveJolt();arrived.Arrive();arrived.Advance(99,true,false,1,100);Check(!arrived.Sleeping,"calm return never starts sleep after arrival");
 }
 static void TypedChecks(){
  var sleeper=PassengerBehaviorState.Create("jolt-sleeper",PassengerType.Sleeping);sleeper.Variant=0;sleeper.Respond(0,true);sleeper.Board();sleeper.Advance(21,false,true,.3f,100);var s=Fresh();
  Check(s.ApplyPhysicalObservation(sleeper,1,0,0)&&!sleeper.Sleeping&&sleeper.JoltWoken,"observed jolt wakes sleeping passenger");
  sleeper.Advance(10,false,true,.4f,100);Check(!sleeper.Sleeping&&!sleeper.SleepInterrupted,"jolt wake persists and does not misuse shallow-sleep interruption");
  Check(s.Observe(1,false,true,true,false,1,1,0,0,false)&&s.Dialogue.Contains("잠"),"awake passenger receives typed neutral reassurance");
  var wakeCopy=sleeper.Copy();wakeCopy.Validate();Check(wakeCopy.JoltWoken&&!wakeCopy.Sleeping,"awake state survives save copy");
  Check(!s.ApplyPhysicalObservation(sleeper,1,0,0),"same observed jolt cannot wake twice");
  var oldSleep=PassengerBehaviorState.Create("old-sleeper",PassengerType.Sleeping);oldSleep.Variant=0;oldSleep.Board();oldSleep.Advance(21,false,true,.3f,100);var oldReaction=Fresh();oldReaction.PhysicalCaptured=false;Check(!oldReaction.ApplyPhysicalObservation(oldSleep,99,0,0)&&oldSleep.Sleeping&&!oldSleep.JoltWoken,"missing old physical-counter capture primes neutrally without retroactive wake");
  foreach(bool seed in new[]{false,true}){
   var food=PassengerBehaviorState.Create("jolt-food",PassengerType.Food);food.SpillPlanned=seed;food.BindEffectTrip("food-trip");food.Board();food.Respond(1,true);food.Advance(20,false,true,.6f,100);
   Check(food.ConsumptionOccurred&&!food.SpillOccurred&&food.DirtEvents.Length==1,"new food has five moving seconds between actual consumption and seeded spill");
   var reaction=Fresh();bool changed=reaction.ApplyPhysicalObservation(food,0,0,1);Check(changed==seed&&food.SpillOccurred==seed,"necessary brake only brings forward already seeded consumed-food spill");
   var care=new VehicleCare();foreach(var dirt in food.DirtEvents)care.Soil(dirt.Id,dirt.Amount);int clean=care.Cleanliness;
   Check(clean==(seed?92:98)&&food.DirtEvents[0].Id=="food-trip:crumbs"&&(!seed||food.DirtEvents[1].Id=="food-trip:spill"),"physical food effect retains original dirt IDs");
   var restored=food.Copy();restored.Advance(20,false,true,.7f,100);foreach(var dirt in restored.DirtEvents)care.Soil(dirt.Id,dirt.Amount);Check(care.Cleanliness==clean&&!reaction.ApplyPhysicalObservation(restored,0,0,1),"reload and natural spill cannot add another six dirt");
  }
  foreach(int choice in new[]{-1,0,2}){var food=PassengerBehaviorState.Create("sealed",PassengerType.Food);food.SpillPlanned=true;if(choice>=0)food.Respond(choice,true);if(choice==2)food.Advance(10,true,false,0,100);food.Board();food.Advance(20,false,true,.6f,100);Check(!Fresh().ApplyPhysicalObservation(food,1,0,0)&&food.DirtEvents.Length==0,"no spill without consumed permitted food");}
  var legacy=PassengerBehaviorState.Create("legacy-food",PassengerType.Food);legacy.FoodSpillTimingVersion=0;legacy.SpillPlanned=true;legacy.Board();legacy.Respond(1,true);legacy.Advance(20,false,true,.6f,100);Check(legacy.SpillOccurred,"old food timing remains simultaneous");
  var luggage=PassengerBehaviorState.Create("jolt-luggage",PassengerType.Luggage);luggage.Respond(0,true);luggage.Advance(10,true,false,0,100);luggage.Board();var l=Fresh();l.ApplyPhysicalObservation(luggage,1,0,0);Check(l.Observe(1,false,true,true,false,1,1,0,0,false)&&l.OffersReSecure&&l.Dialogue.Contains("짐"),"actual loaded luggage jolt offers inspection");
  Check(!l.Respond(0,false)&&l.Respond(0,true)&&l.ReSecureRemaining==5,"five second resecure requires safe explicit consent");
  Check(!l.AdvanceReSecure(4,false,false)&&l.ReSecureRemaining==5,"moving cannot resecure luggage");Check(!l.AdvanceReSecure(4,true,true)&&l.ReSecureRemaining==5,"pause cannot complete resecure");
  Check(!l.AdvanceReSecure(2,true,false)&&l.ReSecureRemaining==3,"resecure consumes real active safe time");var saved=l.Copy();saved.Validate();Check(saved.AdvanceReSecure(3,true,false)&&saved.ReSecureRemaining==0&&!saved.AdvanceReSecure(5,true,false),"restored work completes once");
 }
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
  TypedChecks();CalmRestChecks();Console.WriteLine("PASS PassengerSituationTests "+checks);
 }
}
