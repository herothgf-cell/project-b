using System;
using System.Collections;
using System.IO;
using UnityEngine;
public sealed class PvpSimulationCapture : MonoBehaviour {
 IEnumerator Start(){
  PvpSimulationMatch match=null;float limit=Time.realtimeSinceStartup+180;
  while(!match&&Time.realtimeSinceStartup<limit){match=FindObjectOfType<PvpSimulationMatch>();yield return null;}
  if(!match||!match.Human){Debug.LogError("PVP_SIMULATION_CAPTURE_FAIL initialization");Application.Quit(1);yield break;}
  while(UnityEngine.Rendering.SplashScreen.isFinished==false)yield return null;
  yield return new WaitForSecondsRealtime(3);
  var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-pvpSimulationOutput");string folder=index>=0&&index+1<args.Length?args[index+1]:Application.persistentDataPath;Directory.CreateDirectory(folder);
  string prefix=Path.Combine(folder,"simulation-"+(PvpSimulationMode.Expanded?"city":"eungye"));
  yield return Save(prefix+"-drive.png");
  match.Human.drivingCamera.SetFirstPerson(true);yield return new WaitForSecondsRealtime(1);yield return Save(prefix+"-cockpit.png");
  match.Human.GetComponent<FullMapOverlay>().Open();yield return new WaitForSecondsRealtime(1);yield return Save(prefix+"-map.png");
  Debug.Log("PVP_SIMULATION_CAPTURE_OK "+prefix);Application.Quit(0);
 }
 IEnumerator Save(string path){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();if(!texture){Debug.LogError("PVP_SIMULATION_CAPTURE_FAIL texture");Application.Quit(1);yield break;}var pixels=texture.GetPixels32();int varied=0;for(int i=0;i<pixels.Length;i+=127)if(Math.Abs(pixels[i].r-pixels[0].r)+Math.Abs(pixels[i].g-pixels[0].g)+Math.Abs(pixels[i].b-pixels[0].b)>35)varied++;if(varied<100){Debug.LogError("PVP_SIMULATION_CAPTURE_FAIL blank");Application.Quit(1);yield break;}File.WriteAllBytes(path,texture.EncodeToPNG());Destroy(texture);Debug.Log("PVP_SIMULATION_CAPTURE_FRAME "+path);}
}
