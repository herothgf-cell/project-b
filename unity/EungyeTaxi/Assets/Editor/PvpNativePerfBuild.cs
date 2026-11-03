using UnityEditor;using System.Reflection;
public static class PvpNativePerfBuild {
 public static void Build(){
  BrightUIThemeTests.Run();NavigationCueTests.Run();PvpNativeLanTests.Run();
  PvpCityA3Tests.Run();PvpCityA2Tests.Run();PvpCityA2Tests.LaneChoices();PvpCityA2Tests.Spawn();
  PvpNpcMergeTests.Run();PvpFacilityAccessGeometryTests.Run();
  Finish();
 }
 // Rerun only migrated fixtures that failed the recorded final map bundle.
 public static void BuildRemaining(){PvpNativePoseTests.Run();PvpNpcMergeTests.Guards();PvpNpcMergeTests.DriverIntent();PvpFacilityAccessGeometryTests.Run();Finish();}
 public static void BuildFollowup(){TrafficColliderGeometryTests.Run();PvpA3RecoveryTests.Run();PvpConditionChecks.Run();PvpNativePoseTests.Run();Finish();}
 public static void BuildRecoveryFinal(){PvpA3RecoveryTests.Run();PvpConditionChecks.Run();Finish();}
 public static void BuildJunction(){SignalCountdownTests.Run();PvpNpcMergeTests.Uncontrolled();Finish();}
 public static void BuildProbeOnly(){Finish();}
 public static void BuildPickup(){PvpPickupSearchTests.Run();PvpRoadConditionIndexTests.Run();PvpA3RecoveryTests.Run();Finish();}
 public static void BuildIndexed(){PvpRoadConditionIndexTests.Run();PvpNativePoseTests.Run();PvpA3RecoveryTests.Run();Finish();}
 public static void BuildIntegrated(){TrafficColliderGeometryTests.Run();PvpA3RecoveryTests.Run();PvpNativeLanTests.Run();BrightUIThemeTests.Run();NavigationCueTests.Run();Finish();}
 static void Finish(){
  ProjectMapBuild.Prepare();
  typeof(PrototypeBuild).GetMethod("BuildAt",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"../../artifacts/pvp/a3-motion-candidate11/EungyeTaxi.exe","eungye"});
 }
}
