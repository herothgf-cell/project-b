using System;
using System.IO;
using UnityEngine;

// 08 chapter 3 stage 1: call card judgement material and mixed call characters (Editor, no scene).
public static class CallBoardChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("CALL_BOARD "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  Check(CallBoardRules.VarietyEnabled&&CallBoardRules.RushDeadline==.75f&&CallBoardRules.RushFare==1.4f&&CallBoardRules.LuggageFareBonus==800&&CallBoardRules.NearMaxPickup==450,"draft defaults");
  Check(CallBoardRules.Label(CallBoardRules.Ordinary)==""&&CallBoardRules.Label(CallBoardRules.Hidden).Contains("목적지")&&CallBoardRules.Label(CallBoardRules.Rush).Contains("고액"),"labels");
  Check(CallBoardRules.DemandLabel(2)=="높음"&&CallBoardRules.DemandLabel(1)=="보통"&&CallBoardRules.DemandLabel(0)=="낮음","demand labels");
  // Older saves: a JobOffer without the field is an ordinary call; the field round-trips.
  var legacy=JsonUtility.FromJson<JobOffer>("{\"pickup\":1,\"drop\":4,\"fare\":5000,\"seconds\":300}");Check(legacy.variety==CallBoardRules.Ordinary,"old offers load as ordinary");
  var copy=JsonUtility.FromJson<JobOffer>(JsonUtility.ToJson(new JobOffer{pickup=1,drop=4,fare=7000,variety=CallBoardRules.Hidden}));Check(copy.variety==CallBoardRules.Hidden,"variety round-trips");
  var snap=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(new PassengerJobsSnapshot{offers=new[]{new JobOffer{variety=CallBoardRules.Rush},null,null}}));Check(snap.offers[0].variety==CallBoardRules.Rush,"board snapshot keeps variety");
  Debug.Log("CALL_BOARD_OK checks="+checks);
 }
}
