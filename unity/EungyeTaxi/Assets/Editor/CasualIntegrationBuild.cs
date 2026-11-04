using UnityEngine;
public static class CasualIntegrationBuild {
 public static void Run(){
  CasualDriveChecks.Run();CasualDriveChecks.ResetRegression();CasualDriveChecks.NearbyResetRegression();CasualDriveChecks.YellowRegression();DemandSignalChecks.Run();CasualQueueChecks.Run();DriveFlowBuild.Run();ArrivalReadabilityChecks.Run();NavigationCueTests.Run();MapReadabilityTests.Run();TrafficFlowTests.Run();TrafficReservationLifecycleTests.Run();TrafficReservedCorridorTests.Run();PedestrianSignalConsistencyTests.Run();PvpNativePredictionTests.Run();PvpNativePredictionTests.FreeGuide();PvpNativeWheelTests.Run();PvpNativePoseTests.Run();
  Debug.Log("CASUAL_INTEGRATION_OK");
 }
 public static void Build(){Run();PrototypeBuild.BuildHybrid();}
 public static void SteeringCandidate(){Run();ProjectMapBuild.Prepare();typeof(PrototypeBuild).GetMethod("BuildAt",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{"../../artifacts/steering-candidate/EungyeTaxi.exe","eungye"});}
}
