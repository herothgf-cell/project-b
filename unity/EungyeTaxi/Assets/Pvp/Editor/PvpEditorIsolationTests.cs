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
        try {
            opened=EditorSceneManager.OpenPreviewScene(PvpBuild.ScenePath);
            int bootstraps=0;
            foreach(var root in opened.GetRootGameObjects()) foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true)) {
                Check(component!=null,"scene has no missing script");
                Check(component is PvpBootstrap,"serialized startup has only PVP bootstrap behavior");bootstraps++;
            }
            Check(bootstraps==1,"exactly one explicit bootstrap");
        } finally { if(opened.IsValid()) EditorSceneManager.ClosePreviewScene(opened); }
        // Dirty-scene rejection must precede any build or PlayerSettings mutation.
        var active=SceneManager.GetActiveScene();
        var fixture=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var marker=new GameObject("PVP dirty-scene guard fixture");
        SceneManager.MoveGameObjectToScene(marker,fixture);
        try {
            EditorSceneManager.MarkSceneDirty(fixture);bool rejected=false;
            try { PvpBuild.BuildWithBoundary(options=>{throw new Exception("dirty scene reached build");}); } catch(InvalidOperationException) { rejected=true; }
            Check(rejected,"dirty scenes rejected");
        } finally {
            EditorSceneManager.CloseScene(fixture,true);
            SceneManager.SetActiveScene(active);
        }
        Debug.Log("PVP_EDITOR_ISOLATION_OK scene/player/build-list/input/failure/success/dirty-guard");
    }
    [Serializable] sealed class SceneList { public string[] paths; }
}

}
