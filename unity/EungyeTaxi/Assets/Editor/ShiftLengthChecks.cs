using System;
using UnityEngine;

// Single-mode shift length: ledger rule, persistence and legacy normalization.
public static class ShiftLengthChecks {
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception("SHIFT_LENGTH failed: "+label);}
 static EconomyLedger Roundtrip(EconomyLedger ledger){
  var days=JsonUtility.FromJson<EconomySnapshot>(JsonUtility.ToJson(ledger.Snapshot()));
  var account=JsonUtility.FromJson<AccountSnapshot>(JsonUtility.ToJson(ledger.Account.Snapshot()));
  return EconomyLedger.RestoreCampaign(days,account);
 }
 public static void Run(){
  checks=0;
  Check(PrototypeSettings.ShiftMinuteChoices.Length==4&&PrototypeSettings.IsShiftChoice(1200)&&!PrototypeSettings.IsShiftChoice(720),"choices include default 20 minutes only");
  var ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());
  Check(ledger.Settings.daySeconds==1200,"default 20 minutes");
  Check(!ledger.SetDaySeconds(900),"non-choice rejected");
  Check(ledger.SetDaySeconds(1800)&&ledger.Settings.daySeconds==1800&&ledger.Account.Snapshot().settings.daySeconds==1800,"30 minutes applied to ledger and account");
  Check(Math.Abs(ledger.SecondsLeft-1800)<.001,"remaining time follows chosen length");
  var restored=Roundtrip(ledger);
  Check(restored.Settings.daySeconds==1800,"chosen length survives save/restore");
  ledger.Advance(1,0);
  Check(!ledger.SetDaySeconds(600)&&ledger.Settings.daySeconds==1800,"locked after the day has started");
  var legacy=new EconomyLedger(new PrototypeSettings{oneDay=true,days=3,daySeconds=720});
  Check(Roundtrip(legacy).Settings.daySeconds==1200,"legacy non-choice length normalizes to 20 minutes");
  var pvp=new EconomyLedger(PrototypeSettings.ForOneDay());pvp.EnablePvpProfile();
  Check(!pvp.SetDaySeconds(600),"PVP profile cannot change single shift length");
  var shortDay=new EconomyLedger(PrototypeSettings.ForThreeDays());Check(shortDay.SetDaySeconds(600),"10 minutes accepted");shortDay.Advance(601,0);
  Check(shortDay.Driver.fatigue>=100,"10-minute day crosses the shift boundary at 600s");
  var dev=new EconomyLedger(PrototypeSettings.ForThreeDays());dev.Advance(300,0);
  Check(!dev.SetDaySeconds(3600)&&dev.DevSetDaySeconds(3600)&&Math.Abs(dev.SecondsLeft-3300)<.001,"dev change works mid-shift");
  Check(Roundtrip(dev).Settings.daySeconds==3600,"dev change persists");
  Check(!dev.DevSetDaySeconds(900),"dev change still limited to choices");
  double fatigue=dev.Driver.fatigue;Check(dev.DevSetDaySeconds(600)&&dev.Driver.fatigue==fatigue,"shortening above elapsed keeps fatigue");
  Check(dev.DevSetDaySeconds(1200),"lengthen again");dev.Advance(400,0);Check(dev.Elapsed>=700&&dev.Driver.fatigue<100,"700s into a 20-minute day is not overtime");
  Check(dev.DevSetDaySeconds(600)&&dev.Driver.fatigue>=100&&dev.SecondsLeft==0,"shortening past elapsed applies end-of-shift fatigue");
  var pvpDev=new EconomyLedger(PrototypeSettings.ForOneDay());pvpDev.EnablePvpProfile();Check(!pvpDev.DevSetDaySeconds(600),"PVP rejects dev change");
  Debug.Log("SHIFT_LENGTH_OK checks="+checks);
 }
 public static void RunAll(){
  int errors=0;
  foreach(Action test in new Action[]{Run,DayPreparationChecks.Run,DrivingSettingsChecks.Run})try{test();}catch(Exception ex){errors++;Debug.LogException(ex);}
  if(errors>0){Debug.LogError("SHIFT_LENGTH_ALL failed groups="+errors);EditorExit(1);}else {Debug.Log("SHIFT_LENGTH_ALL_OK");EditorExit(0);}
 }
 static void EditorExit(int code){if(Application.isBatchMode)UnityEditor.EditorApplication.Exit(code);}
}
