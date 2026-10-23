using System;
using System.IO;
using System.Linq;
using UnityEngine;
public static class ProjectMapStoreTests {
 static int count;
 static void Check(bool value,string label){if(!value)throw new Exception("PROJECT_STORE_FAIL "+label);count++;}
 static void Reject(Action action,string label){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,label);}
 public static void Run(){
  count=0;MapCatalog.LockedRegion="eungye";MapCatalog.Load();var baseline=RoadMapRuntime.Baseline;var doc=RoadMapRuntime.Active==null?RoadMapDocument.Create(baseline):RoadMapRuntime.Active.Copy();
  if(doc.scene==null)doc.scene=ProjectMapScene.Capture(baseline);
  string root=Path.GetFullPath("../../artifacts/project-map/clone-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path.Combine(root,".git"));Directory.CreateDirectory(Path.Combine(root,"unity/EungyeTaxi/ProjectSettings"));File.WriteAllText(Path.Combine(root,"unity/EungyeTaxi/ProjectSettings/ProjectVersion.txt"),"m_EditorVersion: 2022.3.62f2");
  var package=ProjectMapPackage.Create(baseline,doc);string original=JsonUtility.ToJson(baseline);string token=ProjectMapStore.Save(root,package,"");Check(File.Exists(ProjectMapStore.SourcePath(root)),"canonical source saved inside repository");
  var loaded=ProjectMapStore.Load(root);Check(loaded.document.scene!=null&&loaded.document.scene.objects.Length==doc.scene.objects.Length,"all authored objects survive repository JSON");Check(loaded.baselineHash==package.baselineHash,"baseline identity survives clone");Check(JsonUtility.ToJson(baseline)==original,"publishing preserves in-memory baseline");
  var built=loaded.Compose();Check(built.editorDocument!=null&&built.buildings.Length>0&&built.hdEdges.Length==doc.lanes.Length,"source composes complete playable map");
  Reject(()=>ProjectMapStore.Save(root,package,""),"stale writer cannot overwrite existing source");Check(ProjectMapStore.Fingerprint(root)==token,"conflict preserves previous bytes");
  var corrupt=ProjectMapPackage.Parse(File.ReadAllText(ProjectMapStore.SourcePath(root)));corrupt.document.name="tampered";Reject(()=>ProjectMapPackage.Parse(JsonUtility.ToJson(corrupt)),"document checksum mismatch rejected");
  var bad=doc.Copy();bad.lanes[0].width=-1;Reject(()=>ProjectMapStore.Save(root,ProjectMapPackage.Create(baseline,bad),token),"invalid map cannot publish");Check(ProjectMapStore.Fingerprint(root)==token,"invalid save preserves previous bytes");
  Reject(()=>ProjectMapStore.Save(Path.Combine(root,"no-repository"),package,""),"invalid project root rejected");
  string nested=Path.Combine(root,"builds/EungyeTaxi/EungyeTaxi_Data");Directory.CreateDirectory(nested);Check(ProjectMapStore.FindRoot(nested)==root,"packaged exe discovers repository by ancestors");
  var copy=ProjectMapPackage.Parse(JsonUtility.ToJson(loaded));var rendered=copy.Compose();Check(JsonUtility.ToJson(built)==JsonUtility.ToJson(rendered),"second machine with only package produces identical map");
  var next=doc.Copy();next.name="다른 PC의 편집";next.revision++;string token2=ProjectMapStore.Save(root,ProjectMapPackage.Create(baseline,next),token);Check(token2!=token&&ProjectMapStore.Load(root).document.name==next.name,"expected version can publish next revision");Check(File.Exists(ProjectMapStore.SourcePath(root)+".bak"),"previous project snapshot retained");
  Check(ProjectMapPackage.Parse(File.ReadAllText(ProjectMapStore.SourcePath(root))).document.projectRevision=="","published data has no machine-local project token");
  Debug.Log("PROJECT_MAP_STORE_TESTS_OK assertions="+count);
 }
}
