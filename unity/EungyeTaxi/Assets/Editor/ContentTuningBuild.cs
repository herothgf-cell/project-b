using System;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Fail a build if the editable source and its bundled runtime copy disagree.
public sealed class ContentTuningBuild:IPreprocessBuildWithReport {
 public int callbackOrder {get{return -1000;}}
 public void OnPreprocessBuild(BuildReport report){VerifyCopies(Path.GetFullPath("../.."));}
 public static void VerifyCopies(string root){
  foreach(string name in new[]{"catalog","default-profile"}){
   byte[] source=File.ReadAllBytes(Path.Combine(root,"config/content",name+".json"));
   byte[] bundled=File.ReadAllBytes(Path.Combine(root,"unity/EungyeTaxi/Assets/Resources/Content",name+".json"));
   bool same=source.Length==bundled.Length;for(int i=0;same&&i<source.Length;i++)same=source[i]==bundled[i];
   if(!same)throw new IOException("콘텐츠 원본과 Resources 사본 불일치: "+name);
  }
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.Combine(root,"config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.Combine(root,"config/content/default-profile.json")));
  Debug.Log("CONTENT_TUNING_BUILD_COPIES_OK");
 }
 public static void Build(){ContentTuningChecks.Run();ContentTuningBuildChecks.Run();var hud=typeof(ContentTuningBuild).Assembly.GetType("HudRevisionBuild");if(hud!=null)hud.GetMethod("Run").Invoke(null,null);PrototypeBuild.BuildHybrid();}
}
