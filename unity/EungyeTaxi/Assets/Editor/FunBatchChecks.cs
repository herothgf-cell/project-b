using System;
using System.IO;
using UnityEngine;

// 08 chapter 5 / chapter 3 additions of 2026-11-24 (Editor, no scene): mission "길 아는 기사" arithmetic,
// free-drive numbers and per-car feel defaults.
public static class FunBatchChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("FUNBATCH "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  Check(MissionRules.KnowRoadEnabled&&MissionRules.KindCount==5,"know-road mission is on, five kinds");
  Check(MissionRules.Bonus(MissionRules.KnowRoad,6000)==900&&MissionRules.Bonus(MissionRules.KnowRoad,8400)==1300,"tip 15% rounded up to 100");
  Check(Mathf.Approximately(MissionRules.KnowRoadLimit(new JobOffer{distance=800}),800*1.35f+120),"detour limit = route x1.35 + 120 m");
  Check(MissionRules.Label(MissionRules.KnowRoad)=="길 아는 기사"&&MissionRules.Goal(MissionRules.KnowRoad,new JobOffer()).Contains("길안내 없이"),"card text");
  var old=JsonUtility.FromJson<JobOffer>("{\"pickup\":1,\"drop\":4,\"fare\":5000,\"mission\":2}");Check(!old.missionVoid&&old.mission==2,"older trips load without the new field");
  var copy=JsonUtility.FromJson<JobOffer>(JsonUtility.ToJson(new JobOffer{mission=MissionRules.KnowRoad,missionVoid=true}));Check(copy.missionVoid&&copy.mission==5,"ended mission round-trips in saves");
  Check(EveningDriveRules.Enabled&&EveningDriveRules.Hour==21&&EveningDriveRules.RadioSeconds==45,"free-drive defaults");
  Check(EveningDriveRules.RadioLine(0)==""&&EveningDriveRules.RadioLine(5).Length>0&&EveningDriveRules.RadioLine(13)==""&&EveningDriveRules.RadioLine(50)!=EveningDriveRules.RadioLine(5)&&EveningDriveRules.RadioLine(140)==EveningDriveRules.RadioLine(5),"three radio lines rotate, 8 s each");
  Check(VehicleFeel.SteerResponse(VehicleCatalog.Starter)==1&&VehicleFeel.BrakeRamp(VehicleCatalog.Starter)==0,"starter keeps the standard steering and brake");
  Check(VehicleFeel.SteerResponse(VehicleCatalog.Hybrid)==1.25f&&VehicleFeel.Lean(VehicleCatalog.Hybrid)==.8f&&VehicleFeel.BrakeRamp(VehicleCatalog.Hybrid)==0,"hybrid defaults");
  Check(VehicleFeel.SteerResponse(VehicleCatalog.WideLpg)==.85f&&VehicleFeel.BrakeRamp(VehicleCatalog.WideLpg)==.2f&&VehicleFeel.Lean(VehicleCatalog.WideLpg)==1.1f,"LPG defaults");
  // Steering reaches the same angle; only the time differs (rate scales with the response).
  float a=0,b=0;var set=new DrivingSettings();a=set.Advance(a,1,10,.004f);b=set.Advance(b,1,10,.004f*1.25f);Check(b>a&&b<=1.0001f,"quicker response turns the wheel sooner ("+a+" -> "+b+")");
  Debug.Log("FUNBATCH_OK checks="+checks);
 }
}
