using UnityEngine;
public static class WorldFollowupChecks {
 public static void Run(){
  TrafficFlowTests.Run();PedestrianSignalConsistencyTests.Run();LaneDirectionPaintTests.Run();ImportedMarkingRendererTests.Run();ReviewedJunctionMapTests.Run();
  MapReadabilityTests.Run();NavigationStopRegressionTests.Run();PlaytestBalanceTests.RunFinalFindings();AmbientPedestrianGeometryAudit.Run();TrafficDistributionTests.Run();StartupPathTests.Run();
  Debug.Log("WORLD_FOLLOWUP_EDITOR_OK");
 }
 public static void ApplyAndBuild(){WorldFollowupMapEdits.Apply();ProjectMapBuild.Prepare();Run();PrototypeBuild.BuildHybrid();}
 public static void HudAndBuild(){ProjectMapBuild.Prepare();MapReadabilityTests.Run();PrototypeBuild.BuildHybrid();}
 public static void PaintAndBuild(){ProjectMapBuild.Prepare();LaneDirectionPaintTests.Run();ImportedMarkingRendererTests.Run();PrototypeBuild.BuildHybrid();}
 public static void Build(){ProjectMapBuild.Prepare();Run();PrototypeBuild.BuildHybrid();}
}
