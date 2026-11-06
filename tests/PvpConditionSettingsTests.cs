using System;
public static class PvpConditionSettingsTests {
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}static void Near(double a,double b,string m){Check(Math.Abs(a-b)<.0001,m);}
 public static int Main(){try{Run();Console.WriteLine("PVP_CONDITION_SETTINGS_OK checks="+checks);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{PvpConditionRules.Reset();}}
 static void Reject(PvpConditionSettings c){bool rejected=false;try{PvpConditionRules.Configure(c);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid settings rejected");}
 static void Run(){
  PvpConditionRules.Reset();Near(PvpConditionRules.FatigueMultiplier,1.25,"default fatigue");Near(PvpConditionRules.MealSeconds,15,"default meal seconds");Near(PvpConditionRules.RestSeconds,45,"default rest seconds");Check(PvpConditionRules.MealCost==1000&&PvpConditionRules.RestCost==0,"default costs");
  Reject(new PvpConditionSettings{fatigueMultiplier=double.NaN});Reject(new PvpConditionSettings{mealSeconds=double.PositiveInfinity});Reject(new PvpConditionSettings{restSeconds=0});Reject(new PvpConditionSettings{mealCost=-1});Reject(new PvpConditionSettings{restCost=100001});
  var custom=new PvpConditionSettings{fatigueMultiplier=2,mealSeconds=25,restSeconds=70,mealCost=300,restCost=500};PvpConditionRules.Configure(custom);custom.mealCost=999;Check(PvpConditionRules.MealCost==300,"configuration copied");var copy=PvpConditionRules.Settings;copy.restCost=999;Check(PvpConditionRules.RestCost==500,"snapshot copied");
  foreach(double satiety in new[]{80d,25,10,0}){var d=new DriverCondition{satiety=satiety,fatigue=20};var seconds=PvpConditionRules.SecondsToFatigue100(d);var before=d.Copy();before.Advance(seconds-.01,false,PvpConditionRules.FatigueMultiplier);Check(before.fatigue<100,"forecast threshold not early");d.Advance(seconds,false,PvpConditionRules.FatigueMultiplier);Near(d.fatigue,100,"forecast matches actual hunger integration");}
  var ledger=new EconomyLedger(new PrototypeSettings{oneDay=true,startCash=1000});Check(ledger.EnablePvpProfile(),"PVP ledger");Check(ledger.CompletePvpRest("r1"),"paid rest completes");Check(ledger.Account.Cash==500&&ledger.Account.Expense(1,"rest")==500,"rest expense accounted");Check(!ledger.CompletePvpRest("r1"),"rest idempotence");Check(ledger.CompletePvpMeal("m1")&&ledger.Account.Cash==200,"custom meal cost");int rests=ledger.PvpRests;Check(!ledger.CompletePvpRest("r2")&&ledger.PvpRests==rests,"insufficient rest funds grants nothing");
  PvpConditionRules.Reset();Check(PvpConditionRules.MealCost==1000&&PvpConditionRules.RestCost==0,"reset defaults");
 }
}
