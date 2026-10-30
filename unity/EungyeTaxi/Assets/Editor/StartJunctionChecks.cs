using UnityEngine;
public static class StartJunctionChecks {
 public static void DiagnosticsBuild(){CurveFootprintTests.Run();ApproachSignalHeadTests.Run();CampaignStartupTests.Run();PrototypeBuild.BuildHybrid();}
 public static void Build(){CurveFootprintTests.Run();ApproachSignalHeadTests.Run();CampaignStartupTests.Run();WorldFollowupChecks.Run();PrototypeBuild.BuildHybrid();}
}
