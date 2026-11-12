using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Diagnostic builds do not overwrite the user's FirstDrive scene or release folder.
public static class PvpStabilityBuild {
 public static void Build(){
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-profileInput")>=0)PvpProfileReport.Run();
  PvpStabilityTests.Run();
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-compareObservers")>=0)PvpStabilityTests.CompareObservers();
  const string scenePath="Assets/Scenes/PvpStabilityDiagnostic.unity";
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  new GameObject("Mode Selection").AddComponent<UnifiedModeBootstrap>().startupRegion="eungye";
  EditorSceneManager.SaveScene(scene,scenePath);
  try {
   bool release=Array.IndexOf(Environment.GetCommandLineArgs(),"-stabilityRelease")>=0;
   var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName="../../artifacts/pvp-stability/"+(release?"release":"candidate")+"/EungyeTaxi.exe",target=BuildTarget.StandaloneWindows64,options=release?BuildOptions.None:BuildOptions.Development});
   if(result.summary.result!=BuildResult.Succeeded)throw new Exception("PVP stability build failed");
  } finally {AssetDatabase.DeleteAsset(scenePath);}
 }
}
