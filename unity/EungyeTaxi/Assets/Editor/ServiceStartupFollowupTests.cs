public static class ServiceStartupFollowupTests {
 public static void Run(){
  ServicePickupTests.Run();
  PedestrianSignalConsistencyTests.Run();
  StartupPathTests.Run();
  StartupPathTests.RunCurrentMapRoutes();
  StartupPathTests.RunValidationBroadPhase();
  NavigationStopRegressionTests.AuditRoutes();
  ProjectMapSceneTests.Run();
  ProjectMapStoreTests.Run();
  UnityEngine.Debug.Log("SERVICE_STARTUP_FOLLOWUP_TESTS_OK");
 }
}
