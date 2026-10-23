using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
public static class PrototypeBuild {
    public static void BuildHd(){HdNetworkTests.Run();BuildAt("../../builds/Route42Taxi/Route42Taxi.exe");}
    [MenuItem("Taxi/Build Eungye Hybrid")]
    public static void BuildHybrid(){if(ProjectMapBuild.HasSource){ProjectMapBuild.Prepare();BuildAt("../../builds/EungyeTaxi/EungyeTaxi.exe","eungye");return;}if(System.IO.File.Exists("Assets/Resources/eungye-project-map.json"))AssetDatabase.DeleteAsset("Assets/Resources/eungye-project-map.json");System.IO.File.Copy("../../config/maps/eungye-reviewed-lanes.json","Assets/Resources/eungye-reviewed-lanes.json",true);System.IO.File.Copy("../../config/maps/eungye-j5057-pavement.json","Assets/Resources/eungye-j5057-pavement.json",true);System.IO.File.Copy("../../config/maps/eungye-corridor-polish.json","Assets/Resources/eungye-corridor-polish.json",true);System.IO.File.Copy("../../config/maps/eungye-osm-supplement.json","Assets/Resources/eungye-osm-supplement.json",true);System.IO.File.Copy("../../config/maps/eungye-scene-overrides.json","Assets/Resources/eungye-scene-overrides.json",true);AssetDatabase.Refresh();TrafficFlowTests.Run();MapReadabilityTests.Run();OfferAccessTests.Run();ReviewedPavementTests.Run();TrafficOverlapQueryTests.Run();TrafficPathPerformanceTests.Run();LawZoneMapLayoutTests.Run();AssistedNetworkTests.Run();HybridNetworkTests.Run();PrototypeScenarioTests.Run();FuelStationLayoutTests.Run();RoadReadabilityTests.Run();HdNetworkTests.Run();DistrictTrafficTests.Run();JunctionFacilityTests.Run();EungyeSupplementTests.Run();CorridorPolishTests.Run();RightTurnWaitingTests.RunConvoy();ReviewedLaneLayoutTests.Run();BuildAt("../../builds/EungyeTaxi/EungyeTaxi.exe","eungye");}
    public static void BuildAssisted(){if(ProjectMapBuild.HasSource)ProjectMapBuild.Prepare();BuildAt("../../builds/AssistedTaxi/AssistedTaxi.exe","eungye");}
    public static void BuildAssistedChecks(){if(ProjectMapBuild.HasSource)ProjectMapBuild.Prepare();BuildAt("../../builds/AssistedTaxiChecks/AssistedTaxiChecks.exe","eungye");}
    [MenuItem("Taxi/Build Windows Prototype")]
    public static void Build(){BuildHybrid();}
    public static void BuildEasyRoadMapEditor(){EasyRoadEditorTests.Run();BuildRoadMapEditor();}
    public static void BuildRoadMapEditor(){if(ProjectMapBuild.HasSource){ProjectMapBuild.RunTests();BuildHybrid();return;}EungyeHybridBuilder.Generate();RoadMapEditorTests.Run();RoadMapExtendedTests.Run();BuildHybrid();}
    public static void BuildReviewedMap(){LaneDirectionPaintTests.Run();BuildHybrid();}
    static void BuildAt(string destination,string region="") {
        PlayerSettings.companyName="Taxi Prototype";PlayerSettings.productName="택시 시뮬레이터";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;PlayerSettings.resizableWindow=true;
        var input=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);var axes=input.FindProperty("m_Axes");bool exists=false;for(int i=0;i<axes.arraySize;i++)if(axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue=="TaxiSteer")exists=true;
        if(!exists){axes.InsertArrayElementAtIndex(axes.arraySize);var a=axes.GetArrayElementAtIndex(axes.arraySize-1);a.FindPropertyRelative("m_Name").stringValue="TaxiSteer";a.FindPropertyRelative("type").intValue=2;a.FindPropertyRelative("axis").intValue=0;a.FindPropertyRelative("joyNum").intValue=0;a.FindPropertyRelative("dead").floatValue=.15f;a.FindPropertyRelative("sensitivity").floatValue=1;a.FindPropertyRelative("invert").boolValue=false;input.ApplyModifiedProperties();}
        bool lookExists=false;for(int i=0;i<axes.arraySize;i++)if(axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue=="TaxiLook")lookExists=true;
        if(!lookExists){axes.InsertArrayElementAtIndex(axes.arraySize);var look=axes.GetArrayElementAtIndex(axes.arraySize-1);look.FindPropertyRelative("m_Name").stringValue="TaxiLook";look.FindPropertyRelative("type").intValue=2;look.FindPropertyRelative("axis").intValue=3;look.FindPropertyRelative("dead").floatValue=.15f;look.FindPropertyRelative("sensitivity").floatValue=1;look.FindPropertyRelative("invert").boolValue=false;input.ApplyModifiedProperties();}
        // A referenced material ensures Standard is retained for generated geometry.
        if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RuntimeStandard.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/Resources/RuntimeStandard.mat");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Drive Session").AddComponent<DriveSession>().startupRegion=region;System.IO.Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/FirstDrive.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/FirstDrive.unity",true)};AssetDatabase.SaveAssets();
        string output=System.IO.Path.GetFullPath(destination);System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/FirstDrive.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.CleanBuildCache});
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
        var notices=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(output),"ThirdPartyNotices");System.IO.Directory.CreateDirectory(notices);
        System.IO.File.Copy("../../licenses/mchrbn-unity-traffic-simulation/LICENSE.txt",System.IO.Path.Combine(notices,"mchrbn-unity-traffic-simulation-MIT.txt"),true);
        System.IO.File.Copy("../../licenses/mchrbn-unity-traffic-simulation/NOTICE.txt",System.IO.Path.Combine(notices,"mchrbn-unity-traffic-simulation-NOTICE.txt"),true);
        System.IO.File.Copy("../../licenses/NotoSansKR-OFL.txt",System.IO.Path.Combine(notices,"NotoSansKR-OFL.txt"),true);
        Debug.Log("PROTOTYPE_BUILD_OK "+output);
    }
}
