using UnityEngine;
public static class LaneFollowupBuild {
 public static void TraceCheck(){ReviewedLaneMovementTraceTests.Run();ReviewedNativeLanePresentationTests.Run();LaneDirectionPaintTests.Run();Debug.Log("LANE_FOLLOWUP_TRACE_OK");}
 public static void Drafts(){TraceCheck();ReviewedApproachLaneMapTests.Run();RoadviewCorridorTests.Run();RuntimeServiceAnchorRegressionTests.Run();Debug.Log("LANE_FOLLOWUP_DRAFTS_OK");}
 public static void ApplyMaps(){Drafts();ReviewedApproachLaneMap.Apply();RoadviewCorridorMapEdits.Apply();RoadviewMapEdits.Verify();EungyeConnectivityRepairTests.Run();TrafficSignalLeftDemandRegressionTests.Run();Debug.Log("LANE_FOLLOWUP_MAPS_OK");}
 public static void FinalBuild(){TraceCheck();ReviewedApproachLaneMap.Verify();RoadviewCorridorMapEdits.Verify();RuntimeServiceAnchorRegressionTests.Run();TrafficSignalLeftDemandRegressionTests.Run();PrototypeBuild.BuildHybrid();Debug.Log("LANE_FOLLOWUP_FINAL_BUILD_OK");}
 public static void CheckedDiagnostic(){RuntimeServiceAnchorRegressionTests.Run();EungyeConnectivityRepairTests.Run();TrafficSignalLeftDemandRegressionTests.Run();TraceCheck();Diagnostic();}
 public static void Diagnostic(){ProjectMapBuild.Prepare();typeof(PrototypeBuild).GetMethod("BuildAt",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{"../../artifacts/lane-followup-candidate/EungyeTaxi.exe","eungye"});}
}
