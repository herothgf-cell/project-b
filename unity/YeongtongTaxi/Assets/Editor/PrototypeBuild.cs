using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
public static class PrototypeBuild {
    public static void BuildAssisted(){BuildAt("../../builds/AssistedTaxi/AssistedTaxi.exe");}
    public static void BuildAssistedChecks(){BuildAt("../../builds/AssistedTaxiChecks/AssistedTaxiChecks.exe");}
    [MenuItem("Taxi/Build Windows Prototype")]
    public static void Build(){BuildAt("../../builds/YeongtongTaxi/YeongtongTaxi.exe");}
    static void BuildAt(string destination) {
        PlayerSettings.companyName="Taxi Prototype";PlayerSettings.productName="영통 택시 시뮬레이터";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        var input=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);var axes=input.FindProperty("m_Axes");bool exists=false;for(int i=0;i<axes.arraySize;i++)if(axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue=="TaxiSteer")exists=true;
        if(!exists){axes.InsertArrayElementAtIndex(axes.arraySize);var a=axes.GetArrayElementAtIndex(axes.arraySize-1);a.FindPropertyRelative("m_Name").stringValue="TaxiSteer";a.FindPropertyRelative("type").intValue=2;a.FindPropertyRelative("axis").intValue=0;a.FindPropertyRelative("joyNum").intValue=0;a.FindPropertyRelative("dead").floatValue=.15f;a.FindPropertyRelative("sensitivity").floatValue=1;a.FindPropertyRelative("invert").boolValue=false;input.ApplyModifiedProperties();}
        bool lookExists=false;for(int i=0;i<axes.arraySize;i++)if(axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue=="TaxiLook")lookExists=true;
        if(!lookExists){axes.InsertArrayElementAtIndex(axes.arraySize);var look=axes.GetArrayElementAtIndex(axes.arraySize-1);look.FindPropertyRelative("m_Name").stringValue="TaxiLook";look.FindPropertyRelative("type").intValue=2;look.FindPropertyRelative("axis").intValue=3;look.FindPropertyRelative("dead").floatValue=.15f;look.FindPropertyRelative("sensitivity").floatValue=1;look.FindPropertyRelative("invert").boolValue=false;input.ApplyModifiedProperties();}
        // A referenced material ensures Standard is retained for generated geometry.
        if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RuntimeStandard.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/Resources/RuntimeStandard.mat");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Drive Session").AddComponent<DriveSession>();System.IO.Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/FirstDrive.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/FirstDrive.unity",true)};AssetDatabase.SaveAssets();
        string output=System.IO.Path.GetFullPath(destination);System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/FirstDrive.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);Debug.Log("PROTOTYPE_BUILD_OK "+output);
    }
}
