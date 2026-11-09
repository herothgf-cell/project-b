using UnityEngine;

public static class RoadviewRevisionBuild {
 public static void Check(){TrafficSignalLeftDemandRegressionTests.Run();GeneratedApproachSignalHeadTests.Run();Debug.Log("ROADVIEW_SIGNAL_CHECKS_OK");}
 public static void Build(){Check();ReviewedNativeLanePresentationTests.Run();LaneDirectionPaintTests.Run();RoadviewMapEdits.Verify();ApproachSignalHeadTests.Run();HudDialogueReadabilityChecks.Run();NativeLeftSignalRegressionTests.Run();FullUIBuild.Build();}
 public static void PaintRed(){Check();ReviewedNativeLanePresentationTests.Run();}
 public static void Apply(){PaintRed();LaneDirectionPaintTests.Run();RoadviewMapEdits.Apply();}
 public static void Release(){ReviewedNativeLanePresentationTests.Run();LaneDirectionPaintTests.Run();RoadviewMapEdits.Verify();PrototypeBuild.BuildHybrid();}
 public static void Candidate(){Check();ReviewedNativeLanePresentationTests.Run();LaneDirectionPaintTests.Run();RoadviewMapEdits.Verify();ApproachSignalHeadTests.Run();HudDialogueReadabilityChecks.Run();NativeLeftSignalRegressionTests.Run();FullUIBuild.PrepareArt();BrightUIThemeTests.Run();BrakingSettingsChecks.All();ContentTuningChecks.Run();ContentTuningBuildChecks.Run();HudRevisionBuild.Run();ProjectMapBuild.Prepare();typeof(PrototypeBuild).GetMethod("BuildAt",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{"../../artifacts/roadview-candidate/EungyeTaxi.exe","eungye"});}
}
