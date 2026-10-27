using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
// Position-controlled rendered regression, opt-in and isolated by the *Test argument.
public sealed class UturnBlinkProbe : MonoBehaviour {
 int checks;float closedBand,openBand;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-uturnBlinkTest")>=0)new GameObject("Uturn blink validation").AddComponent<UturnBlinkProbe>();}
 void Check(bool ok,string label){if(!ok)throw new Exception("UTURN_BLINK_PLAYER_FAIL "+label);checks++;Debug.Log("UTURN_BLINK_PLAYER_PASS "+label);}
 IEnumerator Start(){var run=Run();while(true){object next=null;bool more;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}if(!more)break;yield return next;}Debug.Log("UTURN_BLINK_PLAYER_OK checks="+checks);Application.Quit(0);}
 IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();string path=Path.GetFullPath("artifacts/uturn-blink-"+name+".png");if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.4f);Check(File.Exists(path),name+" fresh rendered capture");var pixels=new Texture2D(2,2);pixels.LoadImage(File.ReadAllBytes(path));float band=0,low=1,high=0;int count=0;for(int x=pixels.width/3;x<pixels.width*2/3;x+=8){float value=pixels.GetPixel(x,pixels.height/5).grayscale;band+=value;count++;}for(int y=0;y<pixels.height;y+=24)for(int x=0;x<pixels.width;x+=24){float value=pixels.GetPixel(x,y).grayscale;low=Mathf.Min(low,value);high=Mathf.Max(high,value);}Check(high-low>.05f,name+" has rendered scene or UI contrast");if(name=="blink-1")closedBand=band/count;if(name=="reopen-1"){openBand=band/count;Check(openBand>closedBand+.03f,"rendered eyelid reopens and reveals scene pixels");}Destroy(pixels);}
 IEnumerator Run(){
  Screen.SetResolution(1280,720,FullScreenMode.Windowed);DriveSession s=null;while(!s){s=FindObjectOfType<DriveSession>();yield return null;}while(!s.StartupReady)yield return null;yield return null;
  s.business.enabled=false;s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);s.business.enabledCampaign=s.business.useV2=true;s.business.preparing=false;
  s.jobs.enabled=s.jobs.nav.enabled=s.law.enabled=s.manners.enabled=s.taxi.enabled=s.assisted.enabled=false;s.taxi.body.isKinematic=true;s.taxi.body.interpolation=RigidbodyInterpolation.None;s.jobs.ClearForDay();s.paused=false;Time.timeScale=1;
  s.taxi.transform.SetPositionAndRotation(new Vector3(807.6996f,.65f,607.1715f),Quaternion.Euler(0,3.280868f,0));s.taxi.body.position=s.taxi.transform.position;s.taxi.body.rotation=s.taxi.transform.rotation;Physics.SyncTransforms();
  var f=s.world.map.editorDocument.facilities.Single(x=>x.junctionId=="J8066_6364"&&x.kind=="sign"&&UturnMovement.IsCondition(x.type));
  var root=s.traffic.GetComponentsInChildren<Transform>().Single(x=>x.name=="사용자 시설 "+f.id);var housing=root.parent.Find("Signal housing");
  Check(housing&&root.position.y>housing.position.y+2,"actual J8066 board above native signal");Check(Vector2.Distance(new Vector2(root.position.x,root.position.z),new Vector2(housing.position.x,housing.position.z))<.1f,"actual board directly above housing");Check(!root.GetComponentsInChildren<Transform>().Any(x=>x.name=="지주"),"actual road-centre pole removed");
  yield return Shot("exact-pose");
  var d=s.business.ledger.Driver.drowsiness;s.business.ledger.Driver.fatigue=100;
  double[] times={30.42,35,70.91,75,150,180};string[] names={"blink-1","reopen-1","blink-3","reopen-3","closing","blackout"};
  for(int i=0;i<times.Length;i++){d.visionSeconds=times[i];yield return Shot(names[i]);Check(i==1||i==3?d.VisionLoss==0:d.VisionLoss>0,names[i]+" effect phase");}
 }
}
