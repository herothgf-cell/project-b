using System;
using UnityEngine;
public static class HudRevisionBuild {
 public static void Focused(){
  int failed=0;foreach(var name in new[]{"CockpitPresentationChecks","DayPreparationChecks","NavigationHudChecks"})try{typeof(HudRevisionBuild).Assembly.GetType(name).GetMethod("Run").Invoke(null,null);}catch(Exception e){failed++;Debug.LogError("HUD_REVISION_SUITE_FAIL "+name+" "+e.GetBaseException());}
  if(failed>0)throw new Exception("HUD_REVISION failures="+failed);Debug.Log("HUD_REVISION_FOCUSED_OK");
 }
 public static void Run(){Focused();UnifiedModeTests.Run();SignalCountdownTests.Run();NavigationCueTests.Run();ArrivalReadabilityChecks.Run();CampaignStartupTests.Run();CampaignCheckpointWriterTests.Run();LiveCampaignTests.Run();Debug.Log("HUD_REVISION_INTEGRATION_OK");}
 public static void Build(){Run();PrototypeBuild.BuildHybrid();}
 public static void Candidate(){Run();ProjectMapBuild.Prepare();typeof(PrototypeBuild).GetMethod("BuildAt",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{"../../artifacts/hud-revision-candidate/EungyeTaxi.exe","eungye"});}
}
