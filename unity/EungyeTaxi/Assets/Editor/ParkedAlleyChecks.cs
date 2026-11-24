using System;
using System.IO;
using UnityEngine;

// Alley parking (2026-11-24, Editor, no scene): data file, defaults, group layout and the pass rule.
public static class ParkedAlleyChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("PARKEDALLEY "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  string source=File.ReadAllText(Path.GetFullPath("../../config/maps/eungye-parked-cars.json"));
  Check(source==File.ReadAllText(Path.GetFullPath("Assets/Resources/eungye-parked-cars.json")),"the Resources copy matches config/maps");
  ParkedAlleyRules.Configure(source);
  Check(ParkedAlleyRules.Roads.Length==1&&ParkedAlleyRules.Roads[0].name=="은행로144번길"&&ParkedAlleyRules.Roads[0].evidence.Contains("게임 연출"),"only 은행로144번길, marked as a game staging");
  Check(ParkedAlleyRules.ExcludesTraffic("은행로144번길")&&!ParkedAlleyRules.ExcludesTraffic("은행로")&&!ParkedAlleyRules.ExcludesTraffic(""),"traffic is excluded from that alley only");
  Check(ParkedAlleyRules.Enabled&&ParkedAlleyRules.GroupMin==3&&ParkedAlleyRules.GroupMax==5&&Mathf.Approximately(ParkedAlleyRules.Gap,6.5f)&&ParkedAlleyRules.GroupSpacing==20&&ParkedAlleyRules.ClearMetres==15&&Mathf.Approximately(ParkedAlleyRules.NightMultiplier,1.5f)&&Mathf.Approximately(ParkedAlleyRules.PassMargin,.3f),"defaults");
  var slots=ParkedAlleyRules.Slots("lane-a",4,100,1);
  Check(slots.Count>0&&slots[0]==4,"slots start at the given distance");
  int groups=1,run=1,minRun=99,maxRun=0;
  for(int i=1;i<slots.Count;i++){float d=slots[i]-slots[i-1];if(Mathf.Abs(d-6.5f)<.01f)run++;else{Check(Mathf.Abs(d-20)<.01f,"groups are 20 m apart ("+d+")");groups++;minRun=Mathf.Min(minRun,run);maxRun=Mathf.Max(maxRun,run);run=1;}}
  Check(groups>=2&&minRun>=3&&maxRun<=5,"groups of 3..5 cars 6.5 m apart (groups "+groups+", "+minRun+".."+maxRun+")");
  Check(string.Join(",",slots)==string.Join(",",ParkedAlleyRules.Slots("lane-a",4,100,1)),"the same lane gives the same layout every time");
  Check(ParkedAlleyRules.Slots("lane-a",4,100,1.5f).Count>slots.Count,"night: more cars");
  Check(Mathf.Approximately(ParkedAlleyRules.Shift(1.5f),0)&&Mathf.Approximately(ParkedAlleyRules.Shift(-.2f),1.4f),"shift: none when the lane centre clears, 1.4 m when the car covers 0.2 m past the lane centre");
  Check(ParkedAlleyRules.Passable(-.2f,2.75f)&&!ParkedAlleyRules.Passable(-.2f,1.75f)&&!ParkedAlleyRules.Passable(-3,4),"passable only on the own side of the centre line with 0.5 m to spare");
  Debug.Log("PARKEDALLEY_CHECKS_OK checks="+checks);
 }
}
