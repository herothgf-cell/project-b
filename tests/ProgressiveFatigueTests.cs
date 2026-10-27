using System;
class ProgressiveFatigueTests {
 static int checks;static void Check(bool b,string s){checks++;if(!b)throw new Exception(s);}
 static void Main(){try{
  var d=new Drowsiness();d.Advance(60,true,100);Check(d.Tunnel,"fatigue exposure must persistently narrow vision after 60 active seconds");d.Advance(5,true,100);Check(d.Tunnel,"vision must not recover after a three second blink");
  var field=typeof(Drowsiness).GetField("visionSeconds");Check(field!=null,"persistent exposure must be saved");
  var copy=d.Copy();copy.Advance(20,false,100);Check((double)field.GetValue(copy)==(double)field.GetValue(d),"menus freeze exposure");
  var e=new EconomyLedger(PrototypeSettings.ForOneDay());e.Driver.fatigue=100;e.Driver.drowsiness=d;var restored=EconomyLedger.RestoreCampaign(e.Snapshot(),e.Account.Snapshot());Check(restored.Driver.drowsiness.Tunnel,"reload preserves narrowed vision");
  var emergency=typeof(EconomyLedger).GetMethod("UseEmergencySleep");Check(emergency!=null,"home recovery must differ from normal rest");
  d.Advance(115,true,100);Check((bool)typeof(Drowsiness).GetProperty("Blackout").GetValue(d,null),"full loss reached");Check((bool)emergency.Invoke(e,new object[]{"home"}),"home sleep accepted");Check(e.Driver.fatigue==80&&e.Elapsed==100&&e.Day==1&&e.Operating,"home sleep recovers20 and keeps same day");Check(!(bool)emergency.Invoke(e,new object[]{"home"}),"duplicate home sleep rejected");Check(e.UseSleep("normal")&&e.Driver.fatigue==40,"normal sleep still recovers40");
  var c=new VehicleCare();var advance=typeof(VehicleCare).GetMethod("Advance");Check(advance!=null,"cleanliness must decay with active time");advance.Invoke(c,new object[]{119d});Check(c.Cleanliness==100,"fraction retained before full decay");var saved=VehicleCare.Restore(c.Snapshot());advance.Invoke(saved,new object[]{1d});Check(saved.Cleanliness==99,"fraction survives reload");saved.Wash();advance.Invoke(saved,new object[]{1d});Check(saved.Cleanliness==100,"wash resets fractional dirt");
  Console.WriteLine("PASS progressive fatigue "+checks+" checks");
 }catch(Exception ex){Console.WriteLine(ex);Environment.Exit(1);}}
}
