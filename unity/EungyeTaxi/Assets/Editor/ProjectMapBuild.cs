using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ProjectMapBuild {
 public static string Root { get { return Path.GetFullPath("../.."); } }
 public static bool HasSource { get { return File.Exists(Path.Combine(Root,ProjectMapStore.RelativePath)); } }
 static string ResourcePath { get { return "Assets/Resources/"+ProjectMapStore.ResourceName+".json"; } }
 [MenuItem("Taxi/Project Map/Import Current Map Once")]
 public static void Initialize(){
  if(HasSource)throw new InvalidOperationException("프로젝트 지도가 이미 있습니다. 편집기에서 열어 수정하세요.");
  if(File.Exists(ResourcePath))AssetDatabase.DeleteAsset(ResourcePath);
  foreach(string name in new[]{"eungye-reviewed-lanes","eungye-j5057-pavement","eungye-corridor-polish","eungye-osm-supplement","eungye-scene-overrides","eungye-road-facilities"})
   File.Copy(Path.Combine(Root,"config/maps/"+name+".json"),"Assets/Resources/"+name+".json",true);
  AssetDatabase.Refresh();EungyeHybridBuilder.Generate();AssetDatabase.Refresh();
  string previous=MapCatalog.LockedRegion;try{
   MapCatalog.LockedRegion="eungye";MapCatalog.Load();
   var baseline=RoadMapRuntime.Baseline;var doc=RoadMapDocument.Create(baseline);
   doc.name="은계 프로젝트 지도";doc.scene=ProjectMapScene.Capture(baseline);
   var package=ProjectMapPackage.Create(baseline,doc);ProjectMapStore.Save(Root,package,"");
   Debug.Log("PROJECT_MAP_INITIALIZED "+package.contentHash+" objects="+doc.scene.objects.Length);
  }finally{MapCatalog.LockedRegion=previous;}
  Prepare();
 }
 [MenuItem("Taxi/Project Map/Validate and Prepare")]
 public static void Prepare(){
  var package=ProjectMapStore.Load(Root);var map=package.Compose();
  if(map.buildings.Length==0||map.hdEdges.Length==0)throw new InvalidDataException("프로젝트 지도 건물/차로가 비었습니다.");
  File.Copy(ProjectMapStore.SourcePath(Root),ResourcePath,true);AssetDatabase.ImportAsset(ResourcePath,ImportAssetOptions.ForceUpdate);
  if(File.ReadAllText(ResourcePath)!=File.ReadAllText(ProjectMapStore.SourcePath(Root)))throw new IOException("지도 빌드 복사 검증 실패");
  string facilitySource=Path.Combine(Root,"config/maps/eungye-road-facilities.json"),facilityResource="Assets/Resources/eungye-road-facilities.json";
  File.Copy(facilitySource,facilityResource,true);AssetDatabase.ImportAsset(facilityResource,ImportAssetOptions.ForceUpdate);
  if(File.ReadAllText(facilitySource)!=File.ReadAllText(facilityResource))throw new IOException("검토 도로시설 빌드 복사 검증 실패");
  Debug.Log("PROJECT_MAP_PREPARED hash="+package.contentHash+" objects="+package.document.scene.objects.Length+" buildings="+map.buildings.Length+" lanes="+map.hdEdges.Length);
 }
 public static void RunTests(){
  if(HasSource)Prepare();ProjectMapSceneTests.Run();ProjectMapSceneEditorTests.Run();ProjectMapStoreTests.Run();
  EasyRoadEditorTests.Run();RoadMapEditorTests.Run();RoadMapExtendedTests.Run();
  Debug.Log("PROJECT_MAP_TESTS_OK");
 }
 [MenuItem("Taxi/Project Map/Test and Build")]
 public static void Build(){RunTests();PrototypeBuild.BuildHybrid();}
}
