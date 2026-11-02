using System;
using UnityEngine;

// Engine-independent behavior exercised from the final Editor validation bundle.
public static class PvpConditionModelTests {
 static void Check(bool value,string why){if(!value)throw new Exception("PVP condition model: "+why);}
 static void Near(double value,double expected,string why){Check(Math.Abs(value-expected)<.00001,why+" ("+value+" vs "+expected+")");}
 static EconomyLedger NewLedger(int cash=10000){var settings=PrototypeSettings.ForOneDay();settings.startCash=cash;return new EconomyLedger(settings);}
 public static void Run(){
  RatesAndForecast();CompletionAndAccounting();CampaignIsolation();Scenarios();
  Debug.Log("PVP_CONDITION_MODEL_TESTS_OK");
 }
 static void RatesAndForecast(){
  var legacy=new DriverCondition();legacy.Advance(600);Near(legacy.satiety,30,"legacy hunger unchanged");Near(legacy.fatigue,50,"legacy rate unchanged");
  var pvp=new DriverCondition();pvp.Advance(600,false,PvpConditionRules.FatigueMultiplier);Near(pvp.fatigue,60,"PVP25 percent fatigue");
  var exact=PvpConditionRules.Forecast(new DriverCondition(),1200);Near(exact.secondsToFatigue100,915,"initial time to fatigue100");Near(exact.fatigue,100,"20min fatigue");Near(exact.satiety,0,"20min satiety");Near(exact.drowsySeconds,180,"exposure clamp");Check(exact.Blackout,"20min blackout forecast");
  var at=new DriverCondition();at.Advance(915,false,1.25);Near(at.fatigue,100,"exact hunger boundary integration");
  var eye=PvpConditionRules.Forecast(new DriverCondition(),945);Near(eye.drowsySeconds,30,"eyes onset at15m45");Check(eye.EyesClosing&&!eye.PassengerMustLeave,"eyes risk flags");
  var stop=PvpConditionRules.Forecast(new DriverCondition(),1035);Near(stop.drowsySeconds,120,"passenger limit17m15");Check(stop.PassengerMustLeave,"passenger risk flag");
  var chunked=new DriverCondition();for(int i=0;i<1200;i++)chunked.Advance(1,false,1.25);Near(chunked.fatigue,exact.fatigue,"frame-size independent fatigue");Near(chunked.satiety,exact.satiety,"frame-size independent hunger");
  var copy=new DriverCondition();PvpConditionRules.PreviewMeal(copy);PvpConditionRules.PreviewRest(copy);Near(copy.fatigue,10,"forecasts do not mutate input");Near(copy.satiety,80,"forecast hunger input stable");
 }
 static void CompletionAndAccounting(){
  var e=NewLedger();Check(e.EnablePvpProfile(),"bind PVP runtime profile");e.Driver.satiety=30;e.Driver.fatigue=60;
  float fuel=e.Account.Fuel;double dirt=e.Care.Cleanliness;int cash=e.Account.Cash;
  var preview=e.PreviewConditionService(false);
  e.AdvancePvpConditionService(14,false);Near(e.Elapsed,14,"partial meal time");Near(e.Driver.satiety,30-14.0/12,"partial meal no effect");Check(e.Account.Cash==cash,"no early meal cost");
  e.AdvancePvpConditionService(1,false);double before=e.Elapsed;Check(e.CompletePvpMeal("meal"),"complete meal");Near(e.Elapsed,before,"meal completion no timejump");Near(e.Driver.satiety,preview.satiety,"preview matches meal");Near(e.Driver.fatigue,preview.fatigue,"meal normal fatigue");
  Check(!e.CompletePvpMeal("meal")&&!e.CompletePvpRest("meal"),"completion id exactly once across kinds");Check(e.Account.Cash==cash-1000&&e.Account.Debt==0,"cash meal1000");Check(e.Account.Expense(1,"meal")==1000&&e.TotalExpenses==1000,"native expense scoring");
  Near(e.Account.Fuel,fuel,"condition services no fuel duplication");Near(e.Care.Cleanliness,dirt,"condition service no care duplication");Near(e.Metrics.Capture().serviceSeconds,15,"service metrics once");Near(e.Metrics.Capture().emptyWaitingSeconds,0,"no duplicate operation metrics");
  e.Driver.fatigue=100;e.Driver.drowsiness.visionSeconds=140;var rest=e.PreviewConditionService(true);
  e.AdvancePvpConditionService(45,true);Near(e.Driver.fatigue,100,"rest no natural fatigue");Check(e.Driver.drowsiness.visionSeconds==140,"rest has no early recovery");before=e.Elapsed;
  Check(e.CompletePvpRest("rest"),"complete free rest");Near(e.Elapsed,before,"rest completion no timejump");Near(e.Driver.fatigue,60,"rest minus40");Near(e.Driver.satiety,rest.satiety,"rest hunger continues");Near(e.Driver.drowsiness.visionSeconds,0,"rest clears existing exposure");Check(!e.CompletePvpRest("rest"),"rest exactly once");
  e.Driver.Validate();Check(e.Driver.meals==0&&e.Driver.sleeps==0&&e.PvpMeals==1&&e.PvpRests==1,"legacy serialized counters preserved");
  Check(e.RecordFare("normal",5000)&&e.Income==5000&&e.TotalNet==4000,"full fare with native meal cost only");
  var poor=NewLedger(999);poor.EnablePvpProfile();Check(!poor.CompletePvpMeal("poor")&&poor.Account.Cash==999&&poor.Account.Debt==0,"insufficient cash no PVP meal debt");Check(poor.CompletePvpRest("free"),"rest no cash requirement");
  var bounded=NewLedger();bounded.EnablePvpProfile();bounded.Driver.satiety=99;bounded.Driver.fatigue=20;Check(bounded.CompletePvpMeal("cap"),"meal cap purchase");Near(bounded.Driver.satiety,100,"satiety cap");bounded.CompletePvpRest("floor");Near(bounded.Driver.fatigue,0,"fatigue floor");
 }
 static void CampaignIsolation(){
  var pvp=NewLedger();pvp.EnablePvpProfile();var ordinary=NewLedger();ordinary.Advance(600,0);Near(ordinary.Driver.fatigue,50,"fresh campaign profile isolated");
  var restored=EconomyLedger.RestoreCampaign(pvp.Snapshot(),pvp.Account.Snapshot());Check(!restored.PvpProfile,"runtime profile excluded from restoration");restored.Advance(60,0);Near(restored.Driver.fatigue,14,"restored campaign rate");
  Check(ordinary.UseMeal("legacy-meal"),"campaign meal retained");Near(ordinary.Elapsed,650,"legacy meal50s");Check(ordinary.Account.Expense(1,"meal")==3000,"legacy meal3000");ordinary.Driver.Validate();
  var poor=NewLedger(0);Check(poor.UseMeal("legacy-debt")&&poor.Account.Debt==3000,"campaign meal debt retained");
  var a=NewLedger();a.EnablePvpProfile();a.Advance(1199,0);a.Driver.fatigue=10;a.Advance(1,0);Near(a.Driver.fatigue,10+1.0/6,"PVP clock budget no campaign forced fatigue");
 }
 static void Scenarios(){
  // Exact approved20min comparison, including service durations in the horizon.
  var meal=new DriverCondition();meal.Advance(600,false,1.25);meal=PvpConditionRules.PreviewMeal(meal);meal.Advance(585,false,1.25);Near(meal.satiety,40,"meal-only20min satiety");Near(meal.fatigue,100,"meal-only20min fatigue");
  var rest=new DriverCondition();rest.Advance(720,false,1.25);rest=PvpConditionRules.PreviewRest(rest);rest.Advance(435,false,1.25);Near(rest.fatigue,100,"rest-only20min fatigue");Near(rest.satiety,0,"rest-only hunger");
  var both=new DriverCondition();both.Advance(600,false,1.25);both=PvpConditionRules.PreviewMeal(both);both.Advance(105,false,1.25);both=PvpConditionRules.PreviewRest(both);both.Advance(435,false,1.25);Near(both.satiety,40,"both20min hunger");Near(both.fatigue,66.25,"both20min fatigue");
 }
}
