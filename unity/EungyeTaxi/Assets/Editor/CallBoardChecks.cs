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
  // Stage 2 (idle time) defaults.
  Check(IdleTimeRules.StopBreakEnabled&&IdleTimeRules.StopBreakDelay==1.5f&&IdleTimeRules.LastCallEnabled&&IdleTimeRules.LastCallWindow==240&&IdleTimeRules.LastCallFarFare==1.3f,"idle-time defaults");
  Check(IdleTimeRules.Label(IdleTimeRules.LastHome).Contains("숙소")&&IdleTimeRules.Label(IdleTimeRules.LastFar).Contains("고액"),"last-call labels");
  // Stage 3 (missions).
  Check(MissionRules.Enabled&&MissionRules.BoardChance==100&&MissionRules.GreenCount==3&&MissionRules.ExpressFactor==1.1f,"mission defaults");
  Check(MissionRules.Bonus(MissionRules.Express,6000)==1800&&MissionRules.Bonus(MissionRules.Green,6000)==1500&&MissionRules.Bonus(MissionRules.Coffee,6000)==1200&&MissionRules.Bonus(MissionRules.Precise,6050)==1300,"extras 20-30% rounded up to 100");
  Check(MissionRules.ExpressTarget(new JobOffer{safeRouteSeconds=120})==132&&MissionRules.ExpressTarget(new JobOffer{distance=800})==110,"express target from safe route time");
  var m=JsonUtility.FromJson<JobOffer>(JsonUtility.ToJson(new JobOffer{mission=2,missionBonus=1500,missionGreenBest=2,missionDriveSeconds=40}));Check(m.mission==2&&m.missionBonus==1500&&m.missionGreenBest==2&&m.missionDriveSeconds==40,"mission progress round-trips");
  Debug.Log("CALL_BOARD_OK checks="+checks);
 }
}
