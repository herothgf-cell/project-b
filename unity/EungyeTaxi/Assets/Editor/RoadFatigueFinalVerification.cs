using UnityEngine;
public static class RoadFatigueFinalVerification {
 public static void Run(){
  PlaytestPresentationTests.RunFinal();
  PlaytestPresentationTests.RunReviewFindings();
  ProgressiveFatiguePersistenceTests.Run();
  FatigueSystemBrakeTests.Run();
  PlaytestDirectTurnTests.Run();RoadFollowupTests.Run();
  Debug.Log("ROAD_FATIGUE_FINAL_EDITOR_OK");
 }
}
