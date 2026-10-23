using UnityEngine;
public static class NativeMapEditorChecks {
 public static void Run(){ProjectMapRepairAuthoringTests.Run();RoadMapAuthoringOperationsTests.Run();ProjectMapTrafficSessionTests.Run();ProjectMapTrafficToolsTests.Run();ProjectMapUnifiedTests.Run();ProjectMapEditSessionTests.Run();ProjectMapSceneWindowTests.Run();ProjectMapAuthoringRendererTests.Run();ProjectMapAuthoringRendererTests.NewObjectIdentity();ProjectMapAuthoringRendererTests.FragmentedAreas();ProjectMapBuild.RunTests();Debug.Log("NATIVE_MAP_EDITOR_CHECKS_OK");}
 public static void BuildIntegrated(){ProjectMapUnifiedTests.Run();ProjectMapAuthoringRendererTests.NewObjectIdentity();ProjectMapTrafficToolsTests.Run();PrototypeCareChecks.Run();ProjectMapRepairAuthoringTests.Build();}
 public static void Build(){Run();PrototypeBuild.BuildHybrid();}
}
