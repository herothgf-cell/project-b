using UnityEngine;
public static class PrototypeCareChecks {
 public static void Run(){PassengerCarePersistenceTests.Run();LiveCampaignTests.Run();PassengerStopCatalogTests.Run();EungyeConnectivityRepairTests.Run();MapReadabilityTests.Run();Debug.Log("PROTOTYPE_CARE_CHECKS_OK");}
 public static void Build(){Run();PrototypeBuild.BuildHybrid();}
}
