using System;
public static class OperationPlanningTests {
 static int checks;
 static void Check(bool v,string message){checks++;if(!v)throw new Exception(message);}
 static void Near(double a,double b,string m){Check(Math.Abs(a-b)<.001,m+" actual="+a+" expected="+b);}
 public static int Main(){try{Run();Console.WriteLine("OPERATION_PLANNING_OK checks="+checks);return 0;}catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
 public static void Run(){
  var settings=new PrototypeSettings{oneDay=true,initialFuel=60};var account=new CampaignAccount(settings);var driver=new DriverCondition{satiety=26,fatigue=10};
  var forecast=OperationForecast.Calculate(account,driver,24,1000,true,20,false,0);
  Check(forecast.Available,"ETA produces forecast");Near(forecast.EndFatigue,100,"crossing campaign close forces fatigue100");Check(forecast.Overtime,"overtime explicit");
  Near(account.Fuel,60,"forecast leaves account unchanged");Near(driver.satiety,26,"forecast leaves driver unchanged");Near(driver.fatigue,10,"forecast leaves fatigue unchanged");
  var match=OperationForecast.Calculate(account,driver,24,1000,true,20,true,10);
  Check(!match.Overtime&&match.MatchEndsBeforeArrival,"match clock isolated from campaign");Near(match.EndFatigue,12.5,"actual PVP hunger threshold and fatigue multiplier forecast");
  Check(!OperationForecast.Calculate(account,driver,double.NaN,0,true,20,false,0).Available,"missing ETA is unavailable");
  Check(!OperationForecast.Calculate(account,driver,10,double.PositiveInfinity,true,20,false,0).Available,"missing route is unavailable");
  var recovered=new DriverCondition{satiety=80,fatigue=60};Near(OperationForecast.Calculate(account,recovered,15,0,true,0,false,0).EndFatigue,61,"already overtime preserves paid sleep recovery rather than reapplying boundary");
  Check(!OperationForecast.Calculate(account,null,10,0,true,20,false,0).Available,"missing driver condition is unavailable rather than invented zeros");
  var ledger=new EconomyLedger(new PrototypeSettings{oneDay=true,startCash=100,initialFuel=50});
  var quote=OperationServiceQuote.Calculate(ledger,1,24,1000);
  Check(quote.Available,"reachable meal quote");Near(quote.Price,3000,"native meal price");Near(quote.UseSeconds,50,"native meal use time distinct from travel");Near(quote.TravelSeconds,24,"route detour time distinct");Near(quote.NewDebt,2900,"meal unpaid quote exact");Near(ledger.Elapsed,0,"quote leaves live clock unchanged");Near(ledger.Account.Debt,0,"quote leaves live debt unchanged");Check(ledger.Account.Entries.Length==0,"quote creates no live transaction");
  var pvpLedger=new EconomyLedger(PrototypeSettings.ForOneDay());Check(pvpLedger.EnablePvpProfile(),"native PVP profile enabled");
  var pvpMeal=OperationServiceQuote.Calculate(pvpLedger,1,24,1000);Check(pvpMeal.Available&&pvpMeal.Price==PvpConditionRules.MealCost,"PVP quote uses actual1000 completion price");Near(pvpMeal.UseSeconds,PvpConditionRules.MealSeconds,"PVP quote uses native15 second completion");
  var pvpRest=OperationServiceQuote.Calculate(pvpLedger,3,24,1000);Check(pvpRest.Available&&pvpRest.Price==0,"PVP rest quote free");Near(pvpRest.UseSeconds,PvpConditionRules.RestSeconds,"PVP quote uses native45 second rest");Check(pvpLedger.PvpMeals==0&&pvpLedger.PvpRests==0&&pvpLedger.Elapsed==0&&pvpLedger.Account.Entries.Length==0,"PVP quotes do not grant native completion, clock or payment");
  var arrival=pvpLedger.Driver.Copy();arrival.Advance(24,false,PvpConditionRules.FatigueMultiplier);var expectedMeal=PvpConditionRules.PreviewMeal(arrival);var expectedRest=PvpConditionRules.PreviewRest(arrival);Near(pvpMeal.EndSatiety,expectedMeal.satiety,"PVP meal quote matches completion satiety");Near(pvpMeal.EndFatigue,expectedMeal.fatigue,"PVP meal quote matches service fatigue");Near(pvpRest.EndSatiety,expectedRest.satiety,"PVP rest quote matches service satiety");Near(pvpRest.EndFatigue,expectedRest.fatigue,"PVP rest quote matches completion fatigue");
  var q=new FacilityAdmissionQueue();q.Request("pump",1,0);q.Request("pump",2,1);q.Request("pump",3,2);q.Request("pump",4,3);
  Check(q.Position("pump",4)==4,"four taxi FIFO visible");Check(!q.TryAdmit("pump",1,false),"physical blocker prevents admission");Check(q.TryAdmit("pump",1,true),"head admitted");Check(!q.TryAdmit("pump",2,true),"no overlapping grant");
  Check(!q.TryAdmit("pump",1,false),"existing reservation never bypasses a newly observed physical blocker");
  q.Release(1);Check(q.TryAdmit("pump",2,true),"finish departure releases next");q.Release(3);Check(q.Position("pump",4)==2,"cancellation removes waiting ticket");q.RemoveAbsent(delegate(int id){return id==4;});Check(q.TryAdmit("pump",4,true),"disconnect releases owner");
  q.Release(4);Check(q.Position("pump",4)==0,"no permanent blocker");
  var restored=new FacilityAdmissionQueue();restored.Request("pump",4,0);Check(restored.TryAdmit("pump",4,true),"restore rebuilds transient queue without replay transaction");
  Check(BotServiceDecision.Needed(80,10,10,100,false,100)==1,"independent hunger chooses meal");
  Check(BotServiceDecision.Needed(1,100,0,10,true,5)==5,"critical damage precedes optional needs");
  Check(BotServiceDecision.Needed(1,100,0,10,true,100)==0,"critical fuel protected");
  Check(BotServiceDecision.Needed(80,10,80,100,true,100)==4,"mandatory wash protected");
  Check(!BotServiceDecision.Worthwhile(1,10,100,50,0,3000,5000,90,80,10,false),"skip late optional meal");
  Check(BotServiceDecision.Worthwhile(0,10,100,10,0,400,5000,120,1,10,true),"critical service remains protected");
  Check(!BotServiceDecision.Worthwhile(1,1000,100,50,0,3000,5000,90,80,10,false),"detour opportunity cost prevents unprofitable meal");
  Check(BotServiceDecision.Worthwhile(1,10,1200,50,0,3000,10000,90,10,70,false),"hunger meal profitable when it restores real safe operating horizon");
  Check(BotServiceDecision.Worthwhile(3,10,1200,100,0,0,10000,90,80,70,false),"rest profitable when actual safe horizon gains exceed detour time");
 }
}
