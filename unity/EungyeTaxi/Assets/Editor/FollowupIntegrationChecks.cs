using UnityEngine;
// One serialized integration entry point after the reviewed package has been saved.
public static class FollowupIntegrationChecks {
 public static void Run(){
  UturnIntegrationChecks.Run();
  ReviewedJunctionMapTests.Run();
  PassengerScoreRecoveryTests.Run();
  NavigationStopRegressionTests.Run();
  PedestrianSignalPlacementTests.Run();
  RoadMarkingAudit.Run();
  Debug.Log("FOLLOWUP_INTEGRATION_CHECKS_OK");
 }
}
