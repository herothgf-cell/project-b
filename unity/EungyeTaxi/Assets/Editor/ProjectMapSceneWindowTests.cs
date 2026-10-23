using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class ProjectMapSceneWindowTests {
 static void Check(bool value,string message){if(!value)throw new Exception("SCENE_WINDOW_FAIL "+message);}
 static object Call(object obj,string method,params object[] args){try{return obj.GetType().GetMethod(method).Invoke(obj,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 public static void RenderVisual(){string root=ProjectMapStore.FindRoot(Application.dataPath);var window=ProjectMapSceneWindow.OpenForTests(root);try{window.CapturePreview(Path.Combine(root,"artifacts/unity-scene-editor/game-camera-preview.png"));Debug.Log("SCENE_PREVIEW_RENDER_OK");}finally{window.CloseForTests();}}
 public static void Run(){
  var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ProjectMapSceneWindow")).FirstOrDefault(t=>t!=null);Check(type!=null,"native editor window exists");
  string source=ProjectMapStore.FindRoot(Application.dataPath),root=Path.Combine(source,"artifacts/unity-scene-editor/window-clone");Directory.CreateDirectory(Path.Combine(root,".git"));Directory.CreateDirectory(Path.Combine(root,"unity/EungyeTaxi/ProjectSettings"));File.WriteAllText(Path.Combine(root,"unity/EungyeTaxi/ProjectSettings/ProjectVersion.txt"),"2022.3.62f2");Directory.CreateDirectory(Path.GetDirectoryName(ProjectMapStore.SourcePath(root)));File.Copy(ProjectMapStore.SourcePath(source),ProjectMapStore.SourcePath(root),true);
  bool dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;int scenes=UnityEngine.SceneManagement.SceneManager.sceneCount;var sourceBytes=File.ReadAllText(ProjectMapStore.SourcePath(source));object window=type.GetMethod("OpenForTests").Invoke(null,new object[]{root});
  try{
   Call(window,"EnsureSceneView");var package=ProjectMapStore.Load(root);var o=package.document.scene.objects.First(x=>x.kind=="building"&&x.enabled);Call(window,"SelectObject",o.id);Check((string)type.GetProperty("SelectedId").GetValue(window)==o.id,"select stable ID");
   Call(window,"ApplyTransform",Vector3.zero,0f,Vector2.one,1.1f);Check(File.ReadAllText(ProjectMapStore.SourcePath(root))==sourceBytes,"drag does not write source");   var timings=new System.Collections.Generic.List<double>();for(int i=0;i<100;i++){var timer=System.Diagnostics.Stopwatch.StartNew();Call(window,"ApplyTransform",new Vector3(.001f,0,0),0f,Vector2.one,1f);timer.Stop();timings.Add(timer.Elapsed.TotalMilliseconds);}timings.Sort();Debug.Log("SCENE_EDIT_PERF updates=100 meanMs="+timings.Average().ToString("F3")+" p95Ms="+timings[94].ToString("F3")+" maxMs="+timings[99].ToString("F3"));Check((int)type.GetProperty("FullBuildCount").GetValue(window)==1,"100 drag samples retain one full build");Call(window,"SaveProject");var saved=ProjectMapStore.Load(root);Check(Mathf.Abs(saved.document.scene.objects.First(x=>x.id==o.id).height-o.height*1.1f)<.001f,"window edit publishes same geometry");Check(File.ReadAllText(ProjectMapStore.SourcePath(source))==sourceBytes,"real project untouched");
  }finally{Call(window,"CloseForTests");}
  Check(UnityEngine.SceneManagement.SceneManager.sceneCount==scenes,"authoring scene closes without replacing user scene");Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty==dirty,"original scene dirty state preserved");Debug.Log("PROJECT_MAP_SCENE_WINDOW_TESTS_OK");
 }
}
