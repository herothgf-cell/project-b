using UnityEngine;
public static class P1P2BuildChecks {
 public static void CompileGate(){
  PassengerActivitiesIntegrationTests.Run();
  TrafficDemandIndicatorIntegrationTests.Run();
  OperationPlanningEditorTests.Run();
  P1P2CheckpointLawTests.Run();
  StopOccupancyAlternativeIntegrationTests.Run();
  Debug.Log("P1P2_INTEGRATED_EDITOR_GATE_OK");
 }
}
