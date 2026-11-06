using System;
using UnityEditor;
using UnityEngine;

public static class FullUIBuild {
 public static void PrepareArt(){
  const string path="Assets/Resources/UI/passenger-portraits.png";
  var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new Exception("Missing full UI portrait atlas");
  importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.SaveAndReimport();
  var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!atlas||atlas.width<512||atlas.height<512)throw new Exception("Portrait atlas is too small");
  Debug.Log("FULL_UI_ART_OK "+atlas.width+"x"+atlas.height);
 }
 public static void Build(){
  PrepareArt();BrightUIThemeTests.Run();DrivingSettingsChecks.Run();DrivingSettingsChecks.Menus();DrivingSettingsChecks.Modal();
  // Includes existing HUD, save/restore, settings and content-copy contracts.
  ContentTuningBuild.Build();
 }
}
