using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Pvp;
using Pvp.Editor;

// Run on a clean project opened with -buildTarget Win64. No player build is
// performed here: only the expensive engine build boundary is replaced.
namespace Pvp.Tests.EditMode {
public static class PvpEditorIsolationTests {
    static void Check(bool ok,string message) { if(!ok) throw new Exception("PVP_EDITOR_ISOLATION: "+message); }
    public static void Run() {
        PvpIsolationTests.Run();
        PvpBuild.EnsureReady();
        PersistedPlayerSettingsBoundary();
        string product=PlayerSettings.productName,company=PlayerSettings.companyName;
        bool background=PlayerSettings.runInBackground;
        string scenes=JsonUtility.ToJson(new SceneList { paths=Array.ConvertAll(EditorBuildSettings.scenes,x=>x.path+"|"+x.enabled) });
        var setup=EditorSceneManager.GetSceneManagerSetup();
        byte[] input=File.ReadAllBytes("ProjectSettings/InputManager.asset");
        foreach(bool fail in new[]{false,true}) {
            bool called=false;var expected=new InvalidOperationException("intentional build-boundary failure");
            try {
                PvpBuild.BuildWithBoundary(options=>{
                    called=true;Check(PlayerSettings.productName=="PvpPrototype"&&PlayerSettings.runInBackground,"temporary player settings applied");
                    Check(options.scenes.Length==1&&options.scenes[0]==PvpBuild.ScenePath,"explicit PVP scene only");
                    Check(options.target==BuildTarget.StandaloneWindows64,"Windows64 target");
                    Check(options.locationPathName==Path.GetFullPath("../../builds/PvpPrototype/PvpPrototype.exe"),"separate output");
                    if(fail) throw expected;
                });
            } catch(InvalidOperationException e) { if(!fail||!System.Object.ReferenceEquals(e,expected)) throw; }
            Check(called,"boundary ran");
            Check(PlayerSettings.productName==product&&PlayerSettings.companyName==company&&PlayerSettings.runInBackground==background,"PlayerSettings restored");
            Check(JsonUtility.ToJson(new SceneList { paths=Array.ConvertAll(EditorBuildSettings.scenes,x=>x.path+"|"+x.enabled) })==scenes,"existing build scenes preserved");
            Check(PvpBuild.SameSceneSetup(setup,EditorSceneManager.GetSceneManagerSetup()),"active scene setup preserved");
            Check(Convert.ToBase64String(File.ReadAllBytes("ProjectSettings/InputManager.asset"))==Convert.ToBase64String(input),"InputManager unchanged");
        }
        Scene opened=default(Scene);
        var inspectionActive=SceneManager.GetActiveScene();
        bool openedForInspection=false;
        try {
            // Unity 2022.3 has no OpenPreviewScene API. Preserve existing loaded scenes.
            opened=SceneManager.GetSceneByPath(PvpBuild.ScenePath);
            if(!opened.IsValid()||!opened.isLoaded) { opened=EditorSceneManager.OpenScene(PvpBuild.ScenePath,OpenSceneMode.Additive);openedForInspection=true; }
            int bootstraps=0;
            foreach(var root in opened.GetRootGameObjects()) foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true)) {
                Check(component!=null,"scene has no missing script");
                Check(component is PvpBootstrap,"serialized startup has only PVP bootstrap behavior");bootstraps++;
            }
            Check(bootstraps==1,"exactly one explicit bootstrap");
        } finally {
            if(openedForInspection&&opened.IsValid()) EditorSceneManager.CloseScene(opened,true);
            if(inspectionActive.IsValid()&&inspectionActive.isLoaded) SceneManager.SetActiveScene(inspectionActive);
        }
        Check(PvpBuild.SameSceneSetup(setup,EditorSceneManager.GetSceneManagerSetup()),"scene inspection restores setup");
        // Dirty-scene rejection must precede any build or PlayerSettings mutation.
        var active=SceneManager.GetActiveScene();
        string fixturePath="Assets/Pvp/Scenes/PvpIsolationFixture-"+Guid.NewGuid().ToString("N")+".unity";
        Scene fixture=default(Scene);
        GameObject marker=null;
        try {
            File.Copy(PvpBuild.ScenePath,fixturePath);
            AssetDatabase.ImportAsset(fixturePath);
            fixture=EditorSceneManager.OpenScene(fixturePath,OpenSceneMode.Additive);
            marker=new GameObject("PVP dirty-scene guard fixture");
            SceneManager.MoveGameObjectToScene(marker,fixture);
            EditorSceneManager.MarkSceneDirty(fixture);bool rejected=false;
            try { PvpBuild.BuildWithBoundary(options=>{throw new Exception("dirty scene reached build");}); } catch(InvalidOperationException) { rejected=true; }
            Check(rejected,"dirty scenes rejected");
        } finally {
            if(marker!=null) UnityEngine.Object.DestroyImmediate(marker);
            if(fixture.IsValid()) EditorSceneManager.CloseScene(fixture,true);
            if(File.Exists(fixturePath)) AssetDatabase.DeleteAsset(fixturePath);
            if(active.IsValid()&&active.isLoaded) SceneManager.SetActiveScene(active);
        }
        Debug.Log("PVP_EDITOR_ISOLATION_OK scene/player/build-list/input/failure/success/dirty-guard");
    }
    // BuildPipeline serializes temporary PlayerSettings. Getter-only assertions
    // cannot detect restored values left dirty in memory after that write.
    static void PersistedPlayerSettingsBoundary() {
        const string path="ProjectSettings/ProjectSettings.asset";
        byte[] originalBytes=File.ReadAllBytes(path);
        string originalProduct=PlayerSettings.productName;
        bool originalBackground=PlayerSettings.runInBackground;
        try {
            foreach(bool fail in new[]{false,true}) {
                PlayerSettings.productName="Isolation original product";
                PlayerSettings.runInBackground=false;
                AssetDatabase.SaveAssets();
                byte[] expectedBytes=File.ReadAllBytes(path);
                var expected=new InvalidOperationException("intentional persisted-boundary failure");
                try {
                    PvpBuild.BuildWithBoundary(options=>{
                        // Simulate the engine's project-settings write on both
                        // successful and failed player builds, without a build.
                        AssetDatabase.SaveAssets();
                        Check(File.ReadAllText(path).Contains("productName: PvpPrototype"),"temporary product serialized by build fixture");
                        if(fail)throw expected;
                    });
                } catch(InvalidOperationException e) {if(!fail||!System.Object.ReferenceEquals(e,expected))throw;}
                Check(PlayerSettings.productName=="Isolation original product"&&!PlayerSettings.runInBackground,"persisted fixture restores memory");
                Check(Convert.ToBase64String(File.ReadAllBytes(path))==Convert.ToBase64String(expectedBytes),"PlayerSettings file restored immediately after "+(fail?"failed":"successful")+" serialized build");
                AssetDatabase.SaveAssets();
                Check(Convert.ToBase64String(File.ReadAllBytes(path))==Convert.ToBase64String(expectedBytes),"later Editor save cannot reserialize temporary player settings");
            }
        } finally {
            PlayerSettings.productName=originalProduct;
            PlayerSettings.runInBackground=originalBackground;
            AssetDatabase.SaveAssets();
            File.WriteAllBytes(path,originalBytes);
        }
    }
    [Serializable] sealed class SceneList { public string[] paths; }
}

}
