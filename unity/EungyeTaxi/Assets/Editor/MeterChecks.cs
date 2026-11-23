using System;
using System.IO;
using UnityEngine;

// 08 chapter 2 living meter (2026-11-24): switch default and fare arithmetic (Editor, no scene).
public static class MeterChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("METER "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  Check(!MeterRules.Enabled,"switch is off by default (agreed-fare economy unchanged)");
  Check(MeterRules.Fare(0,0)==3000,"base fare");
  Check(MeterRules.Fare(619,0)==5500&&MeterRules.Fare(821,0)==6300&&MeterRules.Fare(1341,0)==8400,"draft A matches the agreed fares for the same driven distance");
  Check(MeterRules.Fare(619,59)==5600&&MeterRules.Fare(619,60)==5700,"slow time adds 100 won per 30 s");
  Check(MeterRules.Fare(-5,-1)==3000,"negative inputs are ignored");
  var legacy=JsonUtility.FromJson<JobOffer>("{\"pickup\":1,\"drop\":4,\"fare\":5000}");Check(legacy.meterMetres==0&&legacy.meterSlowSeconds==0,"older trips have no meter record");
  var copy=JsonUtility.FromJson<JobOffer>(JsonUtility.ToJson(new JobOffer{meterMetres=640.5f,meterSlowSeconds=42}));Check(copy.meterMetres==640.5f&&copy.meterSlowSeconds==42,"meter record round-trips in saves");
  Debug.Log("METER_OK checks="+checks);
 }
}
