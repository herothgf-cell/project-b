using System;
class FatigueOvertimeTests {
 static int checks;
 static void Check(bool b,string s){checks++;if(!b)throw new Exception(s);}
 static void Main(){try{Run();Console.WriteLine("PASS fatigue overtime "+checks+" checks");}catch(Exception e){Console.WriteLine(e);Environment.Exit(1);}}
 static void Run(){
  var e=new EconomyLedger(PrototypeSettings.ForOneDay());e.Driver.fatigue=100;Check(e.CanAccept,"fatigue100 must permit calls");
  e.Driver.fatigue=0;e.Advance(1200,0);Check(e.CanAccept&&e.Driver.fatigue==100&&!e.Settle(),"budget forces fatigue100 without closing");
  Check(e.UseSleep("overtime-rest")&&e.Driver.fatigue==60&&e.Elapsed==1300,"overtime free sleep recovers fatigue");
  e.Advance(1,0);Check(e.Driver.fatigue<100,"budget transition only once");Check(e.UseMeal("overtime-meal")&&e.UseWash("overtime-wash"),"overtime services available");
  var crossing=new EconomyLedger(PrototypeSettings.ForOneDay());crossing.Advance(1150,0);Check(crossing.UseSleep("crossing")&&crossing.Driver.fatigue==60,"sleep spanning budget restores fatigue after threshold");
    var previewMethod=typeof(EconomyLedger).GetMethod("PreviewConditionService");Check(previewMethod!=null,"service previews must include overtime transition");
  foreach(bool sleeping in new[]{true,false}){var previewLedger=new EconomyLedger(PrototypeSettings.ForOneDay());previewLedger.Advance(sleeping?1150:1175,0);previewLedger.Driver.fatigue=20;var preview=(DriverCondition)previewMethod.Invoke(previewLedger,new object[]{sleeping});Check(preview.fatigue==(sleeping?60:100),"boundary preview uses transition order");Check(previewLedger.Driver.fatigue==20&&previewLedger.Driver.sleeps==0&&previewLedger.Driver.meals==0,"preview cannot mutate real condition");if(sleeping)previewLedger.UseSleep("preview");else previewLedger.UseMeal("preview");Check(previewLedger.Driver.fatigue==preview.fatigue&&previewLedger.Driver.satiety==preview.satiety,"confirmed service equals shown condition");}
  var copy=EconomyLedger.RestoreCampaign(e.Snapshot(),e.Account.Snapshot());Check(copy.CanAccept&&copy.Elapsed==e.Elapsed,"overtime reload stays open");
  e.RequestClose();Check(e.Settle()&&e.State==BusinessState.Completed,"explicit rest stop close settles");
  var d=new Drowsiness();d.Advance(19,true,100);Check(d.Warning&&!d.Tunnel,"early exposure warns");d.Advance(500,false,100);Check(d.visionSeconds==19,"menus freeze exposure");d.Advance(41,true,100);Check(d.Tunnel,"sixty active seconds narrow vision");var saved=d.Copy();d.Advance(3,true,100);saved.Advance(3,true,100);Check(d.Tunnel&&saved.Tunnel&&d.VisionLoss==saved.VisionLoss,"restored tunnel persists beyond old blink");
  d.Advance(57,true,100,false);Check(d.PassengerMustLeave,"stopping alone does not reset worsening fatigue");d.Advance(60,true,100,false);Check(d.ControlsLost&&d.Blackout,"blackout stops controls");var driver=new DriverCondition{fatigue=100,drowsiness=d};var snapshot=driver.Copy();driver.Sleep();Check(driver.drowsiness.visionSeconds==0,"sleep clears persistent exposure");Check(snapshot.drowsiness.Blackout,"driver snapshot deeply independent");
  var whole=new Drowsiness();var split=new Drowsiness();whole.Advance(160,true,100);for(int i=0;i<1600;i++)split.Advance(.1,true,100);Check(Math.Abs(whole.visionSeconds-split.visionSeconds)<.001,"frame independent progressive vision");
  var field=typeof(DriverCondition).GetField("drowsiness");Check(field!=null,"persistent drowsiness missing");
 }
}
