using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Runs in a disposable player folder. No user settings or campaign are saved.
public sealed class DevRoadOverviewProbe : MonoBehaviour {
 int checks;string folder;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-devRoadOverviewCheck")>=0)new GameObject("Developer overview acceptance").AddComponent<DevRoadOverviewProbe>();}
 void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;Debug.Log("DEV_OVERVIEW PASS "+message);}
 IEnumerator Start(){folder=Path.Combine(Directory.GetCurrentDirectory(),"artifacts/dev-overview");Directory.CreateDirectory(folder);Application.runInBackground=true;var run=Run();while(true){object next;try{if(!run.MoveNext())break;next=run.Current;}catch(Exception e){Debug.LogException(e);File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL "+e);Application.Quit(1);yield break;}yield return next;}File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS checks="+checks);Debug.Log("DEV_OVERVIEW_PLAYER_OK checks="+checks);Application.Quit(0);}
 IEnumerator Run(){
  DriveSession session=null;float until=Time.realtimeSinceStartup+180;
  while(!(session=FindObjectsOfType<DriveSession>().FirstOrDefault(s=>!s.IsSimulationBot))||!session.StartupReady){if(!session){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>until)throw new TimeoutException("Startup");yield return null;}
  session.business.preparing=false;session.paused=true;Time.timeScale=0;session.GetComponent<DriveHUD>().enabled=true;
  var panel=DrivingSettingsPanel.For(session);panel.Open();panel.DevTab=true;panel.Draft.sensitivity=1.23f;var draft=panel.Draft;
  yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"settings-developer.png"));yield return new WaitForSecondsRealtime(.3f);
  var camera=Camera.main;var pos=camera.transform.position;var rotation=camera.transform.rotation;var player=session.taxi.transform.position;bool ortho=camera.orthographic;
  panel.OpenRoadOverview();yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
  var view=panel.RoadOverview;Check(view&&view.IsOpen&&view.Texture&&view.RenderCount>0,"settings opens an actual rendered overview");
  ScreenCapture.CaptureScreenshot(Path.Combine(folder,"whole-map.png"));yield return new WaitForSecondsRealtime(.3f);
  view.View.Focus(new Vector3(640,0,260));view.View.ZoomAt(2,new Vector2(500,300),new Rect(0,0,1000,600));yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
  ScreenCapture.CaptureScreenshot(Path.Combine(folder,"pink-detail.png"));yield return new WaitForSecondsRealtime(.3f);
  var viewport=new Rect(12,106,Screen.width-24,Screen.height-148);
  view.SelectAt(viewport.center,viewport);
  Debug.Log("ROAD_REPORT_DIAGNOSTIC selected="+view.Selected+" center="+view.View.Center+" report="+view.Report+" clipboard="+RoadImageClipboard.ReadText());
  Check(view.Selected.HasValue&&Vector2.Distance(view.Selected.Value,view.View.Center)<.01f&&RoadImageClipboard.ReadText()==view.Report&&view.Report.Contains("가까운 교차로: J"),"selected road point and nearest junction copied");
  view.Capture(viewport);yield return new WaitForSecondsRealtime(.3f);
  Check(view.Editor.IsOpen&&view.Editor.Snapshot&&view.Editor.Description==view.Report,"capture freezes selected location");
  var arrow=new RoadScreenshotEditor.Annotation{tool=1,color=Color.red};arrow.points.Add(new Vector2(.25f,.3f));arrow.points.Add(new Vector2(.5f,.5f));view.Editor.Items.Add(arrow);
  var note=new RoadScreenshotEditor.Annotation{tool=2,color=Color.yellow,text="차선 연결 수정 필요"};note.points.Add(new Vector2(.3f,.2f));view.Editor.Items.Add(note);
  yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"annotated-report.png"));yield return new WaitForSecondsRealtime(.3f);
  view.Editor.RequestCopy();yield return new WaitForSecondsRealtime(.5f);
  Check(view.Editor.CopyCount==1&&RoadImageClipboard.HasImage&&RoadImageClipboard.ReadText()==view.Report,"annotated image and coordinates on Windows clipboard");
  view.Editor.Undo();Check(view.Editor.Items.Count==1,"annotation undo");
  Check(DrivingSettingsPanel.Back(session)&&view.IsOpen&&!view.Editor.IsOpen,"Esc closes screenshot editor first");
  Check(camera.transform.position==pos&&Quaternion.Angle(camera.transform.rotation,rotation)<.01f&&camera.orthographic==ortho,"driving camera unchanged");
  Check(session.taxi.transform.position==player&&session.paused&&Time.timeScale==0,"viewing does not move taxi or resume session");
  Check(DrivingSettingsPanel.Back(session)&&panel.IsOpen&&!view.IsOpen&&!view.Texture,"Esc returns to settings and releases render target");
  Check(ReferenceEquals(draft,panel.Draft)&&panel.Draft.sensitivity==1.23f,"unapplied settings survive overview round trip");
  panel.OpenRoadOverview();Screen.SetResolution(800,600,false);yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
  ScreenCapture.CaptureScreenshot(Path.Combine(folder,"small-window.png"));yield return new WaitForSecondsRealtime(.3f);
  Check(view.IsOpen&&view.Texture&&view.RenderCount>1,"reopen renders after resize");
  panel.Cancel();yield return null;Check(!panel.IsOpen&&!view.IsOpen&&!view.Texture,"settings cancel closes overview resources");
 }
}
