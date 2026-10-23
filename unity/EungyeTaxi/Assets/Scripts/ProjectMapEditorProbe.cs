using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
public sealed class ProjectMapEditorProbe:MonoBehaviour {
 StringBuilder report=new StringBuilder();bool ok=true;string folder;DriveSession s;
 void Check(bool value,string label){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+label);}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);folder=Path.GetFullPath("artifacts/project-map");Directory.CreateDirectory(folder);s=FindObjectOfType<DriveSession>();
  Check(s&&ProjectMapScene.Active(s.world.map)&&RoadMapRuntime.ProjectDefault!=null,"bundled project is runtime source");
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-projectMapStaleTest")>=0){Check(RoadMapRuntime.Notice.Contains("버전이 다릅니다"),"stale personal override is rejected");Check(RoadMapRuntime.Active.projectRevision==RoadMapRuntime.ProjectToken,"current project remains active");Finish("stale-player.txt");yield break;}
  var e=s.GetComponent<RoadMapEditor>();Check(e.Open(),"open project editor");e.FilePath=Path.Combine(folder,"player-draft.json");string previousPreference=PlayerPrefs.GetString("roadEditorProjectRoot","");var original=e.document.Copy();double clock=s.business.ledger.Elapsed;
  Check(new[]{"building","sidewalk","parking","restaurant","rest","road"}.All(k=>e.document.scene.objects.Any(o=>o.kind==k)),"all requested scene kinds captured");
  var building=e.document.scene.objects.First(o=>o.kind=="building");e.SelectSceneObject(building.id);e.SceneObject.height+=.25f;
  var sidewalk=e.document.scene.objects.First(o=>o.kind=="sidewalk");sidewalk.height=.15f;
  var meal=e.document.scene.objects.First(o=>o.kind=="restaurant");meal.name="프로젝트 식당 검사";
  e.SelectSceneObject(meal.id);e.SetSceneServicePosition(meal.Position+meal.Direction*.1f);e.document.revision++;
  string root=Path.Combine(folder,"player-clone");Directory.CreateDirectory(Path.Combine(root,".git"));Directory.CreateDirectory(Path.Combine(root,"unity/EungyeTaxi/ProjectSettings"));File.WriteAllText(Path.Combine(root,"unity/EungyeTaxi/ProjectSettings/ProjectVersion.txt"),"m_EditorVersion: 2022.3.62f2");
  if(File.Exists(ProjectMapStore.SourcePath(root)))File.Delete(ProjectMapStore.SourcePath(root));
  string previousRoot=e.ProjectRoot;e.SetProjectRoot(root);Check(e.SaveProject(),"editor publishes complete project");
  if(File.Exists(ProjectMapStore.SourcePath(root))){
   var p=ProjectMapStore.Load(root);var map=p.Compose();
   Check(map.buildings.Any(b=>"building:"+b.id==building.id&&Mathf.Abs(b.height-building.height)<.001f),"authored building height reaches world map");
   Check(map.areas.Any(a=>a.evidence==sidewalk.id&&Mathf.Abs(a.height-.15f)<.001f),"authored sidewalk reaches world map");
   Check(ProjectMapScene.Services(map,"restaurant").Any(o=>o.name==meal.name&&Vector3.Distance(o.Position,meal.Position)<.001f),"restaurant service edits survive clone");
   Check(!Directory.Exists(Path.Combine(root,"data")),"portable source has no raw GIS inputs");
   e.document=original.Copy();Check(e.LoadProject()&&e.document.scene.objects.Any(o=>o.name==meal.name),"editor reloads published project");
   e.document.name="stale writing test";e.document.revision++;string before=ProjectMapStore.Fingerprint(root);var remote=p.document.Copy();remote.name="external revision";remote.revision++;ProjectMapStore.Save(root,ProjectMapPackage.Create(p.baseline,remote),before);
   Check(!e.SaveProject(),"editor rejects source changed by another process");
  }
  e.document=original.Copy();e.history=new RoadMapEditHistory(e.document);e.SetProjectRoot(previousRoot);if(string.IsNullOrEmpty(previousPreference))PlayerPrefs.DeleteKey("roadEditorProjectRoot");else PlayerPrefs.SetString("roadEditorProjectRoot",previousPreference);PlayerPrefs.Save();
  var stale=original.Copy();stale.projectRevision="old-project-token";RoadMapStore.Save(Path.Combine(folder,"stale-personal.json"),stale);
  foreach(var o in s.world.map.projectScene.objects.Where(o=>o.enabled&&o.IsService)){
   s.taxi.body.position=o.Position+Vector3.up*.65f;s.taxi.transform.position=s.taxi.body.position;s.taxi.transform.rotation=Quaternion.LookRotation(o.Direction);s.taxi.body.rotation=s.taxi.transform.rotation;s.taxi.body.velocity=Vector3.zero;
   Check(o.kind=="restaurant"?s.services.AtRestaurant():o.kind=="parking"?s.services.AtParking():s.services.AtRestArea(),"physical service bay "+o.kind);
   Check(s.GetComponent<FullMapOverlay>().VisibleMarkers().Any(m=>Vector3.Distance(m.position,o.Position)<.01f),"map pin uses authored bay "+o.kind);
  }
  foreach(string tab in new[]{"건물·부지","프로젝트"}){
   typeof(RoadMapEditor).GetField("tool",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(e,tab);
   if(tab=="건물·부지"){e.SelectSceneObject(meal.id);e.Centre=new Vector2(meal.serviceX,meal.serviceZ);e.Span=90;}
   foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080)}){
    Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(1);yield return new WaitForEndOfFrame();yield return null;yield return new WaitForEndOfFrame();
    Check(e.TextOverflowCount==0,"buttons fit "+tab+" "+size.x);var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"project-"+tab+"-"+size.x+".png"),shot.EncodeToPNG());Destroy(shot);yield return new WaitForSecondsRealtime(.2f);
   }
  }
  typeof(RoadMapEditor).GetField("tool",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(e,"건물·부지");
  typeof(RoadMapEditor).GetField("show3D",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(e,true);
  yield return new WaitForSecondsRealtime(.6f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"project-3d.png"));yield return new WaitForSecondsRealtime(.3f);
  Check(s.business.ledger.Elapsed==clock&&Time.timeScale==0,"project edit preserves paused business clock");e.Close();Finish("project-player.txt");
 }
 void Finish(string file){report.AppendLine("success="+ok);File.WriteAllText(Path.Combine(folder,file),report.ToString());Debug.Log(report.ToString());Application.Quit(ok?0:1);}
}
