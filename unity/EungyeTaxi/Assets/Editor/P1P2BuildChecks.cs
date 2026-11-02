using UnityEngine;
public static class P1P2BuildChecks {
 public static void ValidateMerged(){CompileGate();PvpFacilityAccessGeometryTests.Run();BrightUIThemeTests.Run();MapPresentationTests.Run();NavigationCueTests.Run();ProjectMapBuild.Prepare();PvpNativeLanTests.Run();PvpConditionChecks.Run();Debug.Log("P1P2_MERGED_VALIDATION_OK");}
 public static void CompileGate(){
  PassengerActivitiesIntegrationTests.Run();
  TrafficDemandIndicatorIntegrationTests.Run();
  OperationPlanningEditorTests.Run();
  P1P2CheckpointLawTests.Run();
  StopOccupancyAlternativeIntegrationTests.Run();
  Debug.Log("P1P2_INTEGRATED_EDITOR_GATE_OK");
 }
}
