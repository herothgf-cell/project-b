using System;
class CampaignLifecycleTests {
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main(){try{Run();}catch(Exception ex){Console.WriteLine(ex.Message);Environment.Exit(1);}}
 static void Run(){
  var e=new EconomyLedger(new PrototypeSettings());
  Check(e.Settings.days==3&&e.Settings.daySeconds==480,"v2 schedule");
  e.RecordFare("t1",15000);e.Advance(480,1000);Check(!e.CanAccept&&e.Operating,"close stops new jobs");
  e.Settle();Check(e.State==BusinessState.Summary&&e.TotalNet==11500,"meal and parking reduce first day profit");
  Check(e.NextDay(),"second day");e.RequestClose();e.Settle();Check(e.DepartureAllowed,"day3 departure needs no insurance");
  e.RefreshSummary();Check(e.Last.insurance==0&&e.Last.net==-3500,"summary has no excluded costs");
  var restored=EconomyLedger.RestoreCampaign(e.Snapshot(),e.Account.Snapshot());
  Check(restored.TotalNet==e.TotalNet&&restored.Account.Fuel==e.Account.Fuel,"whole-account daily restore");
  Check(!restored.Account.Fare("t1",15000),"fare id retained");
  Check(restored.NextDay(),"continue to final day");restored.RecordFare("t3",25500);restored.RequestClose();restored.Settle();
  Check(restored.State==BusinessState.Won&&restored.TotalNet==30000&&restored.Account.Debt==0,"target alone wins after final meal and parking");
  var early=new EconomyLedger(new PrototypeSettings());early.RecordFare("goal",33500);early.RequestClose();early.Settle();
  Check(early.State==BusinessState.Won&&early.TotalNet==30000,"goal met on first settlement needs no excluded prerequisites");
  LegacyDailyRecords();
  Console.WriteLine("PASS prototype lifecycle without payment gates and account restoration");
 }
 static AccountEntry Entry(string id,string kind,int day,int cash){return new AccountEntry{id=id,kind=kind,day=day,cash=cash};}
 static void LegacyDailyRecords(){
  var old=new AccountSnapshot{version=2,settings=new PrototypeSettings(),fuel=17,entries=new[]{
   Entry("fare:legacy1","fare",1,18000),Entry("insurance","insurance",1,-6000),Entry("meal1","meal",1,-3000),Entry("parking:1","parking",1,-500),Entry("settlement:1","settlement",1,0),
   Entry("fare:legacy2","fare",2,18000),Entry("tax","tax-payment",2,-1500),Entry("meal2","meal",2,-3000),Entry("parking:2","parking",2,-500),Entry("settlement:2","settlement",2,0)}};
  var days=new EconomySnapshot{settings=new EconomySettings(),records=new[]{
   new DayRecord{day=1,income=18000,rides=1,insurance=6000,food=3000,net=8500},
   new DayRecord{day=2,income=18000,rides=1,food=3000,net=13000}}};
  var restored=EconomyLedger.RestoreCampaign(days,old);
  Check(restored.Day==2&&restored.TotalNet==29000&&restored.Last.net==14500&&restored.Last.insurance==0,"migrated daily summary reconciles removed early payments");
  Check(restored.Snapshot().records[0].net==14500&&days.records[0].net==8500,"all migrated days normalized without mutating source checkpoint");
  var again=EconomyLedger.RestoreCampaign(restored.Snapshot(),restored.Account.Snapshot());
  Check(again.TotalNet==29000&&again.NextDay()&&again.Day==3,"migrated checkpoint continues after repeat restore");
  again.RecordFare("legacy3",4500);again.RequestClose();again.Settle();
  Check(again.State==BusinessState.Won&&again.TotalNet==30000,"migrated campaign completes with normal living costs");
  days.records[1].day=3;bool invalid=false;try{EconomyLedger.RestoreCampaign(days,old);}catch(ArgumentException){invalid=true;}
  Check(invalid,"migration retains daily sequence validation");
 }
}
