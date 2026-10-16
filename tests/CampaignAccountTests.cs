using System;
class CampaignAccountTests {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static void Main(){
  var a=new CampaignAccount(new PrototypeSettings());
  Check(a.Cash==10000&&a.Net==0&&a.Fuel==35,"initial assets excluded from profit");
  Check(a.Fare("trip1",5000)&&!a.Fare("trip1",5000)&&a.Net==5000,"fare exactly once");
  Check(a.Refuel("fuel1",50)&&a.Fuel==85&&a.Cash==13000,"actual fuel purchase");
  Check(!a.Refuel("fuel1",50)&&a.Fuel==85,"duplicate refuel rejected");
  Check(a.Refuel("fuel2",50)&&a.Fuel==100&&a.Cash==12400,"capped fuel charged actual quantity");
  a.Consume(60,1000);Check(Math.Abs(a.Fuel-87)<.001,"distance and idle consumption");
  Check(a.Meal("meal1",1)&&!a.Meal("meal2",1),"one meal per day");
  Check(a.Renew("renew1")&&!a.Renew("renew2")&&a.InsuranceValid(3),"early renewal preserves valid coverage");
  Check(a.PayTax("tax1")&&!a.PayTax("tax2"),"tax direct payment once");
  Check(a.CloseDay(1)&&!a.CloseDay(1),"settlement idempotency");
  var poor=new CampaignAccount(new PrototypeSettings());poor.Renew("renew");poor.Meal("meal",1);poor.CloseDay(1);
  Check(poor.Cash==500,"mandatory costs use available cash");
  poor.CloseDay(2);Check(poor.Cash==0&&poor.Debt==3000,"unfunded costs become debt");
  poor.CloseDay(3);Check(!poor.TaxPaid&&poor.Debt==8000,"unpaid tax recognized as debt, not auto paid");
  int net=poor.Net;poor.Fare("trip",10000);Check(poor.PayTax("late")&&poor.Net==net+10000,"debt payment is not a second expense");
  Check(!poor.PayTax("late")&&poor.TaxPaid,"late tax once");
  var clone=CampaignAccount.Restore(poor.Snapshot());Check(clone.Net==poor.Net&&clone.TaxPaid,"snapshot round trip");
  Check(!clone.Fare("trip",10000),"dedup survives restore");
  var balance=new CampaignAccount(new PrototypeSettings());
  for(int day=1;day<=3;day++){balance.Fare("day"+day,18000,day);if(day==1||day==3)balance.Refuel("fuel-day"+day,50,day);balance.Consume(480,3000);if(day==2)balance.Renew("balance-insurance",day);if(day==3)balance.PayTax("balance-tax",day);balance.CloseDay(day);}
  Check(balance.Net==32000&&balance.GoalMet,"accounting scenario: 54k revenue/22k costs achieves goal");
  Console.WriteLine("PASS campaign cash/debt/fuel/meal/renewal/tax/settlement/restore");
 }
}
