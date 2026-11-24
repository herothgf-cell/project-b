using System;
using System.IO;
using UnityEngine;

// Out-of-town call, method A (2026-11-24, Editor, no scene): defaults, the fare by shift time, the shift share
// and time rules, and the direction label.
public static class OutOfTownChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("OUTOFTOWN "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  Check(OutOfTownRules.Enabled&&OutOfTownRules.DailyMax==1&&OutOfTownRules.Minutes==6&&Mathf.Approximately(OutOfTownRules.MaxShiftShare,.35f)&&Mathf.Approximately(OutOfTownRules.FareMultiplier,1.15f)&&OutOfTownRules.PerMinuteWon==3000,"defaults: once a day, 6 min, 35%, x1.15, 3,000 won/min");
  Check(OutOfTownRules.EdgeStop==1&&Mathf.Approximately(OutOfTownRules.FadeSeconds,3)&&OutOfTownRules.BoardChance==35&&OutOfTownRules.MinSecondsMargin==90,"defaults: edge stop 1, 3 s fade, 35% of boards, 90 s margin");
  Check(OutOfTownRules.Fare(180)==31100,"3 min ride + 6 min leg = 9 min x 3,450 = 31,050 -> 31,100 ("+OutOfTownRules.Fare(180)+")");
  Check(OutOfTownRules.Fare(0)==20700,"leg alone 6 x 3,450 = 20,700");
  Check(!OutOfTownRules.ShiftAllows(600)&&!OutOfTownRules.ShiftAllows(720)&&OutOfTownRules.ShiftAllows(1200)&&OutOfTownRules.ShiftAllows(3600),"none in 10- and 12-minute shifts, offered from 20 minutes (6 min <= 35%)");
  Check(OutOfTownRules.TimeAllows(700,250)&&!OutOfTownRules.TimeAllows(699,250),"time: pickup and ride + 360 s leg + 90 s margin");
  Check(Mathf.Abs(OutOfTownRules.LegSeconds-360)<.01f&&Mathf.Abs(OutOfTownRules.LegMetres-5000)<1,"leg 360 s, 5 km at 50 km/h");
  Check(OutOfTownRules.DirectionOf(new Vector3(500,0,1900),1000,2000)=="북쪽"&&OutOfTownRules.DirectionOf(new Vector3(500,0,100),1000,2000)=="남쪽"&&OutOfTownRules.DirectionOf(new Vector3(990,0,1000),1000,2000)=="동쪽"&&OutOfTownRules.DirectionOf(new Vector3(5,0,1000),1000,2000)=="서쪽","direction label from the map centre");
  Check(CallBoardRules.ShortLabel(CallBoardRules.OutOfTown).StartsWith("시외(")&&CallBoardRules.Label(CallBoardRules.OutOfTown).Contains("시외 구간 6분"),"card labels");
  Debug.Log("OUTOFTOWN_CHECKS_OK checks="+checks);
 }
}
