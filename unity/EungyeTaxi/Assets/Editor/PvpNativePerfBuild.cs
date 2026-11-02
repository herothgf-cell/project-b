using UnityEditor;using System.Reflection;
public static class PvpNativePerfBuild {
 public static void Build(){BrightUIThemeTests.Run();NavigationCueTests.Run();PvpNativeLanTests.Run();ProjectMapBuild.RunTests();ProjectMapBuild.Prepare();typeof(PrototypeBuild).GetMethod("BuildAt",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"../../artifacts/pvp/native-perf-candidate5/EungyeTaxi.exe","eungye"});}
}
