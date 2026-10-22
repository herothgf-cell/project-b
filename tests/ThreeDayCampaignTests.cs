using System;
class ThreeDayCampaignTests {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e);Environment.Exit(1);}}
 static PrototypeSettings Settings(){var factory=typeof(PrototypeSettings).GetMethod("ForThreeDays");Check(factory!=null,"Missing three-day 20-minute campaign factory");return (PrototypeSettings)factory.Invoke(null,null);}
 static void Run(){
  var s=Settings();var e=new EconomyLedger(s);
  Check(e.OneDay&&e.Driver!=null,"modern condition and optional meals active for three days");
  e.RecordFare("large",50000);e.Advance(1199,0);Check(e.State==BusinessState.Open,"profit never closes early");e.Advance(1,0);Check(e.State==BusinessState.Closing&&e.Operating,"timeout keeps occupied ride operating");e.RecordFare("late",5000);e.Advance(30,10);Check(e.Settle()&&e.State==BusinessState.Summary&&e.Last.net==54500,"first day only settles its parking expense");
  var reload=EconomyLedger.RestoreCampaign(e.Snapshot(),e.Account.Snapshot());Check(reload.State==BusinessState.Summary&&reload.Day==1&&reload.TotalNet==54500,"settled day resumes at summary");
  Check(reload.NextDay()&&reload.Day==2&&reload.Income==0&&reload.Elapsed==0,"next day resets operating counters");Check(reload.Account.Cash==64500&&reload.Account.Fuel<58,"cash and consumed fuel carry");Check(reload.Driver.satiety==0&&reload.Driver.fatigue==40,"overnight sleep recovers sixty fatigue without free food or day time");
  var day2=EconomyLedger.RestoreCampaign(reload.Snapshot(),reload.Account.Snapshot());Check(day2.Day==2&&day2.State==BusinessState.Open&&day2.Income==0,"next-day start checkpoint restores prepared day instead of previous summary");Check(day2.Driver.satiety==0&&day2.Driver.fatigue==40,"reloading day2 applies overnight recovery exactly once");
  Check(day2.UseMeal("meal2")&&day2.UseSleep("sleep2"),"daily modern services available");day2.RequestClose();day2.Settle();Check(day2.State==BusinessState.Summary&&day2.Last.net==-3500,"day2 costs independent and deficit is valid");Check(day2.NextDay(),"third day starts");Check(day2.Driver.meals==0&&day2.Driver.sleeps==0,"daily condition counters restart");day2.RequestClose();day2.Settle();Check(day2.State==BusinessState.Completed&&!day2.NextDay()&&day2.TotalNet==50500,"three-day completion uses actual cumulative net with no target verdict");
  var final=EconomyLedger.RestoreCampaign(day2.Snapshot(),day2.Account.Snapshot());Check(final.State==BusinessState.Completed&&final.TotalNet==50500,"completed campaign reloads");
  foreach(var entry in final.Account.Entries)Check(entry.kind!="insurance"&&entry.kind!="tax-due"&&entry.kind!="tax-payment","excluded taxes and insurance never return");
  Console.WriteLine("PASS three-day settlement, carry, day-boundary reload and target-free completion");
 }
}
