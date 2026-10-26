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
    var d=new Drowsiness{randomState=3};d.Advance(19,true,100);Check(d.phase==0,"no early event");d.Advance(500,false,100);Check(d.checkSeconds==19,"stationary/menu freezes exposure");d.Advance(1,true,100);Check(d.Warning,"seeded onset warns");var saved=d.Copy();d.Advance(2,true,100);saved.Advance(2,true,100);Check(d.Tunnel&&saved.Tunnel&&d.remaining==3,"restore preserves planned effect");d.Advance(3,true,100);Check(d.phase==0&&!d.Tunnel,"tunnel expires");
  var loss=new Drowsiness{randomState=13};loss.Advance(22,true,100);Check(loss.ControlsLost,"alternate event controls lost");loss.Advance(1,true,100,false);Check(!loss.ControlsLost,"control loss ends after one second even after coasting to stop");loss.Advance(19,true,100);var original=loss.Copy();var driver=new DriverCondition{fatigue=100,drowsiness=loss};var snapshot=driver.Copy();driver.Sleep();Check(driver.drowsiness.phase==0&&driver.drowsiness.checkSeconds==0,"sleep clears event");Check(snapshot.drowsiness.checkSeconds==original.checkSeconds,"driver snapshot deeply independent");
  var whole=new Drowsiness{randomState=3};var split=new Drowsiness{randomState=3};whole.Advance(900,true,100);for(int i=0;i<9000;i++)split.Advance(.1,true,100);Check(whole.phase==split.phase&&whole.randomState==split.randomState&&Math.Abs(whole.checkSeconds-split.checkSeconds)<.001&&Math.Abs(whole.remaining-split.remaining)<.001,"frame independent event timeline");
  var field=typeof(DriverCondition).GetField("drowsiness");Check(field!=null,"persistent drowsiness missing");
 }
}
