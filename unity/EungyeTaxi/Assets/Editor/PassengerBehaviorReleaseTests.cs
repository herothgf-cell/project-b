using UnityEngine;

public static class PassengerBehaviorReleaseTests {
 public static void Build() { Run(); PrototypeBuild.BuildHybrid(); }
 public static void Run() {
  PassengerBehaviorIntegrationTests.Run();
  PassengerCarePersistenceTests.Run();
  LiveCampaignTests.Run();
  ProtoExpansionTests.RunP11();
        Pvp.Tests.EditMode.PvpEditorIsolationTests.Run();
  Debug.Log("PASSENGER_BEHAVIOR_RELEASE_EDITOR_OK");
 }
}
