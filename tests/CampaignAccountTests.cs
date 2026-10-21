using System;
class CampaignAccountTests {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static void Main(){try{Run();}catch(Exception ex){Console.WriteLine(ex.Message);Environment.Exit(1);}}
 static void Run(){
  var a=new CampaignAccount(new PrototypeSettings());
  Check(a.Cash==10000&&a.Net==0&&a.Fuel==35,"initial assets excluded from profit");
  Check(a.Fare("trip1",5000)&&!a.Fare("trip1",5000)&&a.Net==5000,"fare exactly once");
  Check(a.Refuel("fuel1",50)&&a.Fuel==85&&a.Cash==13000,"actual fuel purchase");
  Check(!a.Refuel("fuel1",50)&&a.Fuel==85,"duplicate refuel rejected");
  Check(a.Refuel("fuel2",50)&&a.Fuel==100&&a.Cash==12400,"capped fuel charged actual quantity");
  a.Consume(60,1000);Check(Math.Abs(a.Fuel-87)<.001,"distance and idle consumption");
  Check(a.Meal("meal1",1)&&!a.Meal("meal2",1),"one meal per day");
  int before=a.Cash;
  Check(!a.Renew("renew1")&&!a.PayTax("tax1")&&a.Cash==before,"excluded payments cannot deduct cash");
  Check(a.InsuranceValid(3),"third day has no insurance gate");
  Check(a.CloseDay(1)&&!a.CloseDay(1),"settlement idempotency");
  var poor=new CampaignAccount(new PrototypeSettings());poor.Meal("meal",1);poor.CloseDay(1);
  Check(poor.Cash==6500,"mandatory meal and parking costs");
  poor.CloseDay(2);Check(poor.Cash==3000&&poor.Debt==0,"second day costs preserved");
  Check(poor.PendingDayCost(3)==3500,"final projection excludes tax");
  poor.CloseDay(3);Check(poor.Cash==0&&poor.Debt==500,"final day accrues only unfunded meal and parking");
  int net=poor.Net;poor.Fare("trip",10000);Check(poor.Repay("repay",500,3)&&poor.Net==net+10000,"meal debt payment is not a second expense");
  var clone=CampaignAccount.Restore(poor.Snapshot());Check(clone.Net==poor.Net&&clone.Debt==0,"snapshot round trip");
  Check(!clone.Fare("trip",10000),"dedup survives restore");
  var balance=new CampaignAccount(new PrototypeSettings());
  for(int day=1;day<=3;day++){balance.Fare("day"+day,18000,day);if(day==1||day==3)balance.Refuel("fuel-day"+day,50,day);balance.Consume(480,3000);if(day==2)balance.Renew("balance-insurance",day);if(day==3)balance.PayTax("balance-tax",day);balance.CloseDay(day);}
  Check(balance.Net==39500&&balance.GoalMet,"54k revenue minus 4k fuel and 10.5k meal/parking achieves goal");
  Migration();
  Console.WriteLine("PASS campaign cash/debt/fuel/meal/parking/excluded costs/migration/restore");
 }
 static AccountEntry Entry(string id,string kind,int day,int cash,int debt){return new AccountEntry{id=id,kind=kind,day=day,cash=cash,debt=debt};}
 static void Migration(){
  var old=new AccountSnapshot{version=2,settings=new PrototypeSettings(),fuel=42,entries=new[]{
   Entry("fare:old","fare",1,20000,0),Entry("insurance","insurance",1,-6000,0),Entry("meal1","meal",1,-3000,0),Entry("parking:1","parking",1,-500,0),Entry("settlement:1","settlement",1,0,0),
   Entry("tax-due","tax-due",3,0,1500),Entry("tax-paid","tax-payment",3,-1500,-1500),Entry("meal3","meal",3,-3000,0),Entry("parking:3","parking",3,-500,0),Entry("settlement:3","settlement",3,0,0)}};
  var migrated=CampaignAccount.Restore(old);
  Check(migrated.Cash==23000&&migrated.Debt==0&&migrated.Net==13000&&migrated.Fuel==42,"legacy paid insurance and tax excluded with other costs/fuel retained");
  Check(!migrated.Fare("old",20000)&&!migrated.CloseDay(3),"migration keeps fare and settlement identities");
  var again=CampaignAccount.Restore(migrated.Snapshot());
  Check(again.Cash==23000&&again.Net==13000&&again.Entries.Length==7,"repeated save and restore never refunds twice");
  old.entries=new[]{Entry("meal","meal",1,-10000,3000),Entry("tax-due","tax-due",3,0,1500)};
  var unpaid=CampaignAccount.Restore(old);
  Check(unpaid.Cash==0&&unpaid.Debt==3000&&unpaid.Net==-13000,"unpaid tax forgiven without deleting meal debt");
  old.entries=new[]{Entry("insurance","insurance",1,-6000,0),Entry("bad","fuel",1,-5000,0)};
  bool invalid=false;try{CampaignAccount.Restore(old);}catch(ArgumentException){invalid=true;}
  Check(invalid,"legacy original cash validation happens before excluded transactions are removed");
  old.entries=new[]{Entry("duplicate","insurance",1,-6000,0),Entry("duplicate","meal",1,-1000,0)};
  invalid=false;try{CampaignAccount.Restore(old);}catch(ArgumentException){invalid=true;}
  Check(invalid,"excluded transaction still participates in duplicate validation");
 }
}
