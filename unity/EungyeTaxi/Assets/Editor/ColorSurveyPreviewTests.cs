using System;
using UnityEngine;

// Serial Editor checks; no authoring map or build is written here.
public static class ColorSurveyPreviewTests {
 public static void BuildReviewedCurrent() {
  JunctionRouteFeedbackTests.VerifyMigration();
  PrototypeBuild.BuildHybrid();
 }
 public static void ApplyGrid() {
  ReviewedUnmarkedRoadPaintTests.Run();
  PinkGridUnmarkedMapEdit.Apply();
 }
 public static void MigrationAndGrid() {
  JunctionRouteFeedbackTests.VerifyMigration();
  PinkGridUnmarkedMapTests.PreviewChecks();
 }
 public static void MigrationAndGreen31() {
  JunctionRouteFeedbackTests.VerifyMigration();
  Green31MapTests.PreviewChecks();
 }
 public static void CurrentCandidates() {
  PinkPassageMapTests.PreviewChecks();
  PinkRoundaboutMapTests.Run();
 }
 public static void Run() {
  Y07MapTests.PreviewChecks();
  bool oldIslandCrossing=false;
  try { PinkRoundaboutMapTests.ExistingGeometryRed(); }
  catch(InvalidOperationException error) {
   if(!error.Message.Contains("old straight passes through central island"))throw;
   oldIslandCrossing=true;
   Debug.Log("PINK_ROUNDABOUT_EXPECTED_RED " + error.Message);
  }
  if(!oldIslandCrossing)throw new InvalidOperationException("Expected the existing straight route to cross the observed island");
  PinkRoundaboutMapTests.Run();
 }
}
