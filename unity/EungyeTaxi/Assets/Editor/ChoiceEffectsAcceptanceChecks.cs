using UnityEngine;
public static class ChoiceEffectsAcceptanceChecks {
 public static void Run(){ChoiceEffectsRuntimeChecks.Run();ContentTuningChecks.Run();ContentTuningBuildChecks.Run();Debug.Log("CHOICE_ACCEPTANCE_EDITOR_OK");}
 public static void Build(){Run();PrototypeBuild.BuildHybrid();}
}
