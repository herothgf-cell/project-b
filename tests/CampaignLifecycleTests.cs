using System;
class CampaignLifecycleTests {
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main(){
  var e=new EconomyLedger(new PrototypeSettings());
  Check(e.Settings.days==3&&e.Settings.daySeconds==480,"v2 schedule");
  e.RecordFare("t1",40000);e.Advance(480,1000);Check(!e.CanAccept&&e.Operating,"close stops new jobs");
  e.Settle();Check(e.State==BusinessState.Summary&&e.TotalNet>=30000,"goal alone insufficient");
  Check(e.NextDay(),"second day");e.RequestClose();e.Settle();Check(!e.DepartureAllowed,"expired insurance gates day3");
  e.Account.Renew("insurance",2);e.Account.PayTax("tax",2);Check(e.DepartureAllowed,"renewal unlocks day3");
  e.RefreshSummary();Check(e.Last.insurance==6000&&e.Last.net==-11000,"summary payments reconcile daily record");
  var restored=EconomyLedger.RestoreCampaign(e.Snapshot(),e.Account.Snapshot());
  Check(restored.TotalNet==e.TotalNet&&restored.Account.Fuel==e.Account.Fuel,"whole-account daily restore");
  Check(!restored.Account.Fare("t1",40000),"fare id retained");
  Check(restored.NextDay(),"continue to final day");restored.RecordFare("t3",15000);restored.RequestClose();restored.Settle();
  Check(restored.State==BusinessState.Won,"net plus renewal plus tax wins");
  Console.WriteLine("PASS v2 lifecycle gates and account restoration");
 }
}
