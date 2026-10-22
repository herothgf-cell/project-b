using System;
class OneDayCampaignTests {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static object Call(object o,string name,params object[] args){var m=o.GetType().GetMethod(name);Check(m!=null,"Missing behavior: "+name);return m.Invoke(o,args);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e.ToString());Environment.Exit(1);}}
 static void Run(){
  var factory=typeof(PrototypeSettings).GetMethod("ForOneDay");Check(factory!=null,"Missing one-day 1200-second settings");
  var s=(PrototypeSettings)factory.Invoke(null,null);Check(s.days==1&&s.daySeconds==1200,"one day budget");
  var e=new EconomyLedger(s);e.RecordFare("large",50000);e.Advance(1199,0);Check(e.State==BusinessState.Open,"profit is not a finish condition; fatigue may block acceptance");e.Advance(1,0);Check(!e.CanAccept&&e.Operating,"closing keeps current ride alive");e.Advance(60,10);e.RecordFare("last",5000);Check(e.Settle()&&e.Last.net==54500,"parking only, no fallback food");Check(e.State.ToString()=="Completed"&&!e.NextDay(),"independent completed day");
  var poor=new EconomyLedger(s);Check((bool)Call(poor,"UseMeal","meal-a"),"first meal");Check((bool)Call(poor,"UseMeal","meal-b"),"repeat meal");Check(!(bool)Call(poor,"UseMeal","meal-b"),"duplicate meal ID");Check(poor.Elapsed==100&&poor.Account.Cash==4000&&poor.Account.Fuel==60,"50s each engine off");Check((bool)Call(poor,"UseSleep","sleep-a"),"sleep");Check(poor.Elapsed==200&&poor.Account.Cash==4000&&poor.Account.Fuel==60,"100s free engine off");
  poor.Advance(951,0);Check(!(bool)Call(poor,"UseMeal","short"),"49 seconds cannot buy meal");Check(poor.Account.Cash==4000,"failed service no charge");
  var exact=new EconomyLedger(s);exact.Advance(1150,0);Check((bool)Call(exact,"UseMeal","exact"),"exact 50 seconds allowed");Check(!exact.CanAccept&&exact.Settle(),"finish after service");
  var debt=new EconomyLedger(s);for(int i=0;i<4;i++)Check((bool)Call(debt,"UseMeal","m"+i),"meal debt supported");debt.RequestClose();debt.Settle();Check(debt.TotalNet==-12500&&debt.Account.Debt==2500,"actual expense once including unpaid food");
  var restored=EconomyLedger.RestoreCampaign(debt.Snapshot(),debt.Account.Snapshot());Check(restored.TotalNet==debt.TotalNet&&restored.Elapsed==debt.Elapsed,"restore settlement time/account");
  Check(restored.Settings.daySeconds==1200&&restored.Driver.meals==4,"restore new mode and condition together");
  var whole=new DriverCondition();whole.Advance(700);var split=new DriverCondition();for(int i=0;i<7000;i++)split.Advance(.1);Check(Math.Abs(whole.satiety-split.satiety)<.0001&&Math.Abs(whole.fatigue-split.fatigue)<.0001,"hunger crossing is frame independent");Check(Math.Abs(whole.fatigue-58)<.001,"660 normal seconds and 40 hungry seconds");
  var tired=new EconomyLedger(s);tired.Advance(1140,0);Check(!tired.CanAccept&&tired.Operating,"fatigue 100 blocks acceptance not driving");var sleepy=new EconomyLedger(s);sleepy.Driver.fatigue=100;Check(!sleepy.CanAccept&&(bool)Call(sleepy,"UseSleep","rest")&&sleepy.CanAccept,"sleep reopens acceptance");
  var meal=new DriverCondition{satiety=26,fatigue=20};meal.Meal();Check(Math.Abs(meal.fatigue-24.6)<.0001&&Math.Abs(meal.satiety-(26-50.0/12+70))<.0001,"meal elapsed before recovery");
  var rest=new DriverCondition{satiety=20,fatigue=80};rest.Sleep();Check(rest.fatigue==40&&Math.Abs(rest.satiety-(20-100.0/12))<.0001,"sleep no awake fatigue");
  Check(OneDayOfferPolicy.Rank(500,100,1000,0,1,0)<OneDayOfferPolicy.Rank(2000,100,1000,0,2,0),"early shorter route first");Check(OneDayOfferPolicy.Rank(500,100,1000,1000,1,0)<OneDayOfferPolicy.Rank(2000,100,1000,1000,2,0),"late shorter route first");Check(OneDayOfferPolicy.Rank(1000,100,0,600,1,0)<OneDayOfferPolicy.Rank(1000,100,2000,600,1,0),"middle meal access priority");
  var budget=typeof(OneDayOfferPolicy).GetMethod("PickupSeconds");Check(budget!=null,"pickup budget policy exists");Check((float)budget.Invoke(null,new object[]{1732f})>=700,"measured 558-second signal-limited pickup has margin");Check((float)budget.Invoke(null,new object[]{0f})==180,"nearby pickup keeps three-minute minimum");
  Console.WriteLine("PASS one-day budget, repeated services, debt and settlement");
 }
}
