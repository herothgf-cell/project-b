using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Pvp;

namespace Pvp.Editor {
public static class PvpBuild {
    public const string ScenePath="Assets/Pvp/Scenes/PvpBootstrap.unity";
    [MenuItem("Taxi/PVP/Build isolated Windows client")]
    public static void BuildClient() {
        BuildWithBoundary(options=>{
            var report=BuildPipeline.BuildPlayer(options);
            if(report==null||report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("PVP build failed: "+(report==null?"no report":report.summary.result.ToString()));
        });
        string notices=Path.GetFullPath("../../builds/PvpPrototype/ThirdPartyNotices");
        Directory.CreateDirectory(notices);
        File.Copy("../../licenses/mchrbn-unity-traffic-simulation/LICENSE.txt",Path.Combine(notices,"mchrbn-unity-traffic-simulation-MIT.txt"),true);
        File.Copy("../../licenses/mchrbn-unity-traffic-simulation/NOTICE.txt",Path.Combine(notices,"mchrbn-unity-traffic-simulation-NOTICE.txt"),true);
        File.Copy("../../licenses/NotoSansKR-OFL.txt",Path.Combine(notices,"NotoSansKR-OFL.txt"),true);
        Debug.Log("PVP_BUILD_OK "+Path.GetFullPath("../../builds/PvpPrototype/PvpPrototype.exe"));
    }
    internal static void EnsureReady() {
        // Switching here can defer recompilation/domain reload and is unsupported
        // in batchmode. Require the caller to establish the target first.
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64) throw new InvalidOperationException("PVP requires active Windows x86_64 target. Switch target in Build Settings and wait for compilation, then rerun; batchmode: launch Unity with -buildTarget Win64 -executeMethod Pvp.Editor.PvpBuild.BuildClient.");
        for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save or discard dirty scenes before the PVP build. No scene is automatically saved or replaced.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)==null) throw new InvalidOperationException("Missing PVP bootstrap scene: "+ScenePath);
    }
    internal static void BuildWithBoundary(Action<BuildPlayerOptions> build) {
        EnsureReady();
        var setup=EditorSceneManager.GetSceneManagerSetup();
        var scenes=EditorBuildSettings.scenes;
        try {
            PvpBuildTransaction.Run(
                ()=>new PlayerSnapshot { product=PlayerSettings.productName,background=PlayerSettings.runInBackground },
                ()=>{PlayerSettings.productName="PvpPrototype";PlayerSettings.runInBackground=true;},
                ()=>{
                    string output=Path.GetFullPath("../../builds/PvpPrototype/PvpPrototype.exe");
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                    build(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
                },
                saved=>{PlayerSettings.productName=saved.product;PlayerSettings.runInBackground=saved.background;});
        } finally {
            // Neither list nor setup is edited by this build path. Restore only if
            // the engine/boundary changed them; avoid touching unchanged scenes.
            if(!SameBuildScenes(scenes,EditorBuildSettings.scenes)) EditorBuildSettings.scenes=scenes;
            if(!SameSceneSetup(setup,EditorSceneManager.GetSceneManagerSetup())) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
    static bool SameBuildScenes(EditorBuildSettingsScene[] a,EditorBuildSettingsScene[] b) {
        if(a.Length!=b.Length) return false;
        for(int i=0;i<a.Length;i++) if(a[i].path!=b[i].path||a[i].enabled!=b[i].enabled) return false;
        return true;
    }
    internal static bool SameSceneSetup(SceneSetup[] a,SceneSetup[] b) {
        if(a.Length!=b.Length) return false;
        for(int i=0;i<a.Length;i++) if(a[i].path!=b[i].path||a[i].isLoaded!=b[i].isLoaded||a[i].isActive!=b[i].isActive) return false;
        return true;
    }
    sealed class PlayerSnapshot { public string product;public bool background; }
}

}
