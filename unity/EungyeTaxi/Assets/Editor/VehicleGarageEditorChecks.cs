using UnityEngine;

// Plan 22 batch entry: editor checks touched by the vehicle purchase / LAN v17 change.
// Unity.exe -batchmode -nographics -quit -projectPath unity/EungyeTaxi -executeMethod VehicleGarageEditorChecks.Run
public static class VehicleGarageEditorChecks {
 public static void Run(){
  MenuConditionFollowupTests.Run();
  BrakingSettingsChecks.Run();
  DrivingSettingsChecks.Run();
  ContentTuningChecks.Run();
  CasualDriveChecks.Run();
  VehicleDurabilityIntegrationTests.Run();
  DriveFlowBuild.Run();
  Debug.Log("VEHICLE_GARAGE_EDITOR_CHECKS_OK");
 }
}
