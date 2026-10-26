using UnityEngine;
public static class UturnIntegrationChecks {
 public static void Run(){
  ProjectMapBuild.Prepare();ReviewedUturnMap.Validate();UturnTests.Run();ReviewedRoadFacilitiesTests.Run();ImportedMarkingRendererTests.Run();
  ProjectMapTrafficToolsTests.Run();ProjectMapUnifiedTests.Run();ReviewedLaneLayoutTests.Run();
  Debug.Log("UTURN_INTEGRATION_CHECKS_OK");
 }
}
