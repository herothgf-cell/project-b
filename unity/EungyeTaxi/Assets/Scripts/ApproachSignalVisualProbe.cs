using System;
using System.Collections;
using System.IO;
using UnityEngine;
// Isolated, opt-in rendering check at the reported westbound approach.
public sealed class ApproachSignalVisualProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-approachSignalVisualTest")>=0)new GameObject("Approach signal visual check").AddComponent<ApproachSignalVisualProbe>();}
 IEnumerator Start(){
  DriveSession s;while(!(s=FindObjectOfType<DriveSession>())||!s.StartupReady)yield return null;yield return null;yield return null;
  s.business.enabledCampaign=true;s.business.preparing=false;s.paused=false;Time.timeScale=1;s.taxi.automated=true;s.taxi.autoBrake=true;
  s.taxi.SetSpawn(new Vector3(211.2f,.65f,519.08f),Quaternion.LookRotation(Vector3.left));s.taxi.ResetTaxi();
  var j=s.traffic.junctions.Find(x=>x.name=="J1927_5181");var hud=s.GetComponent<DrivingReadability>();
  yield return null;hud.SelectSignal();
  bool ok=j!=null&&hud.SignalTarget==j&&!s.traffic.HasLeftMovement(j,hud.SignalAxis,hud.SignalSign)&&s.business.SavePath.Contains("artifacts");
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){
   Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
   var shot=ScreenCapture.CaptureScreenshotAsTexture();if(shot){Directory.CreateDirectory("artifacts");File.WriteAllBytes("artifacts/approach-no-left-"+size.x+".png",shot.EncodeToPNG());Destroy(shot);}else ok=false;
  }
  File.WriteAllText("artifacts/approach-signal-visual-test.txt","success="+ok+"\nreported westbound approach has no left signal; two resolutions captured\n");Application.Quit(ok?0:1);
 }
}
