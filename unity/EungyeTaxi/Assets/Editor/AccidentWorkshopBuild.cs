using UnityEngine;
public static class AccidentWorkshopBuild {
 public static void Check(){if(ProjectMapBuild.HasSource)ProjectMapBuild.Prepare();AccidentIntegrationChecks.Run();WorkshopLayoutTests.Run();LiveCampaignTests.Run();CampaignCheckpointWriterTests.Run();Debug.Log("ACCIDENT_WORKSHOP_EDITOR_OK");}
 public static void Build(){Check();PrototypeBuild.BuildHybrid();}
}
