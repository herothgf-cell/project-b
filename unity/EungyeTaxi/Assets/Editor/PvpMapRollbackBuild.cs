using UnityEditor;using System.Reflection;
public static class PvpMapRollbackBuild {
 public static void Build(){PvpCityA2Tests.Run();PvpCityA2Tests.LaneChoices();PvpCityA2Tests.Spawn();PvpNativeWheelTests.Run();PvpNativePoseTests.Run();ProjectMapBuild.Prepare();typeof(PrototypeBuild).GetMethod("BuildAt",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"../../artifacts/pvp/a2-rollback-candidate/EungyeTaxi.exe","eungye"});}
}
