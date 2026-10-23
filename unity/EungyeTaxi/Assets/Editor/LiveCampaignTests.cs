using System;
using UnityEngine;

// Shipping JsonUtility and accounting snapshots must restore the same live boundary.
public static class LiveCampaignTests {
 static int count;
 static void Check(bool value,string name){count++;if(!value)throw new Exception("LIVE_CAMPAIGN: "+name);}
 static T Copy<T>(T value){return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));}
 static void Reject(Action action,string name){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,name);}
 public static void Run(){
  count=0;var ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());
  ledger.Advance(137,820);Check(ledger.RecordFare("live-paid",5300),"fixture fare posted");ledger.RecordFailure();
  Check(ledger.UseMeal("meal-live")&&ledger.UseSleep("sleep-live"),"fixture meal and sleep applied");ledger.Care.Finish("dirty-live",true);ledger.Care.Refuse("refused-live");Check(ledger.UseWash("wash-live"),"fixture wash applied");ledger.Care.Finish("normal-live",false);
  var days=Copy(ledger.Snapshot());var account=Copy(ledger.Account.Snapshot());var restored=EconomyLedger.RestoreCampaign(days,account);
  Check(restored.Operating&&restored.State==BusinessState.Open&&restored.Day==1,"active day stays open on same day");
  Check(restored.Elapsed==ledger.Elapsed&&restored.Metres==ledger.Metres&&restored.Income==ledger.Income&&restored.Rides==ledger.Rides&&restored.FailedTrips==ledger.FailedTrips,"active time distance and counters preserved");
  Check(restored.Account.Cash==ledger.Account.Cash&&restored.Account.Debt==ledger.Account.Debt&&Mathf.Abs(restored.Account.Fuel-ledger.Account.Fuel)<.0001f&&restored.TotalNet==ledger.TotalNet,"cash debt fuel and net use same live boundary");
  Check(Math.Abs(restored.Driver.satiety-ledger.Driver.satiety)<.000001&&Math.Abs(restored.Driver.fatigue-ledger.Driver.fatigue)<.000001&&restored.Driver.meals==1&&restored.Driver.sleeps==1,"driver consumption is not reapplied");
  Check(restored.Care.Cleanliness==98&&!restored.Care.AcceptanceLocked&&restored.Care.Refusals==1&&!restored.Care.Finish("normal-live",false),"care and settlement identities persist");
  int cash=restored.Account.Cash;double seconds=restored.Elapsed;Check(!restored.RecordFare("live-paid",5300)&&!restored.UseMeal("meal-live")&&!restored.UseSleep("sleep-live")&&!restored.UseWash("wash-live")&&restored.Account.Cash==cash&&restored.Elapsed==seconds,"restored transaction/service IDs reject replay");
  ledger.RequestClose("live close");var closing=EconomyLedger.RestoreCampaign(Copy(ledger.Snapshot()),Copy(ledger.Account.Snapshot()));Check(closing.State==BusinessState.Closing&&closing.EndReason=="live close"&&closing.Elapsed==ledger.Elapsed,"closing deadline is preserved before passenger finishes");
  Check(closing.Settle(),"restored close settles once");Check(!closing.Settle()&&closing.Last.wash==2000&&closing.Last.net==ledger.Income-closing.Account.Expense(1,null),"one settlement includes wash once");
  var summary=EconomyLedger.RestoreCampaign(Copy(closing.Snapshot()),Copy(closing.Account.Snapshot()));double satiety=summary.Driver.satiety;int clean=summary.Care.Cleanliness,cashAtNight=summary.Account.Cash;Check(summary.NextDay(),"summary advances next day");
  var next=EconomyLedger.RestoreCampaign(Copy(summary.Snapshot()),Copy(summary.Account.Snapshot()));Check(next.Day==2&&next.Elapsed==0&&next.Income==0&&next.Rides==0&&next.FailedTrips==0,"new day active counters stay reset after reload");Check(next.Care.Cleanliness==clean&&next.Account.Cash==cashAtNight&&Math.Abs(next.Driver.satiety-satiety)<.000001&&Math.Abs(next.Driver.fatigue-summary.Driver.fatigue)<.000001,"next day retains cash cleanliness and one night recovery");Check(next.UseSleep("sleep-live"),"new day clears service identity boundary");
  // Version 2 stored a start-of-day boundary with neither active counters nor care.
  var old=new EconomyLedger(PrototypeSettings.ForThreeDays());old.Advance(35,200);old.RequestClose();old.Settle();old.NextDay();var v2=Copy(old.Snapshot());v2.active=null;v2.hasActive=false;v2.care=null;var oldAccount=Copy(old.Account.Snapshot());oldAccount.version=2;var compatible=EconomyLedger.RestoreCampaign(v2,oldAccount);Check(compatible.Day==2&&compatible.Elapsed==0&&compatible.Rides==0&&compatible.Care.Cleanliness==100,"v2 day boundary restores with default cleanliness");
  var malformed=Copy(days);malformed.active.elapsed=double.NaN;Reject(()=>EconomyLedger.RestoreCampaign(malformed,account),"nonfinite live time rejected");malformed=Copy(days);malformed.active.services=new[]{"duplicate","duplicate"};Reject(()=>EconomyLedger.RestoreCampaign(malformed,account),"duplicate live service identity rejected");malformed=Copy(days);malformed.active.state=BusinessState.Completed;Reject(()=>EconomyLedger.RestoreCampaign(malformed,account),"settled state cannot masquerade as active day");malformed=Copy(days);malformed.active.driver.fatigue=101;Reject(()=>EconomyLedger.RestoreCampaign(malformed,account),"invalid live driver rejected");
  Check(ledger.Account.Cash==cash&&ledger.Elapsed==seconds&&ledger.Care.Cleanliness==98,"roundtrip and rejected restores never mutate source ledger");
  Debug.Log("LIVE_CAMPAIGN_TESTS_OK checks="+count);
 }
}
