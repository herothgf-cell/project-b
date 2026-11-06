using UnityEditor;
using UnityEngine;
public static class RainBuild {
 public static void Build(){
  if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RuntimeRain.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Particles/Standard Unlit")),"Assets/Resources/RuntimeRain.mat");
  if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RuntimeWiper.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Sprites/Default")),"Assets/Resources/RuntimeWiper.mat");
  AssetDatabase.SaveAssets();ContentTuningBuild.VerifyCopies(System.IO.Path.GetFullPath("../.."));PrototypeBuild.BuildHybrid();
 }
}
