using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Fresh campaign startup uses the real preparation boundary, with an isolated save path.
public sealed class CampaignStartupProbe : MonoBehaviour {
 DriveSession session;int checks;string error="";
 bool Reload {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-campaignStartupPreparedReloadTest")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-campaignStartupTest")>=0||Array.IndexOf(args,"-campaignStartupPreparedReloadTest")>=0){Screen.SetResolution(1280,720,FullScreenMode.Windowed);new GameObject("Campaign startup check").AddComponent<CampaignStartupProbe>();}}
 void Check(bool value,string reason){checks++;if(!value)throw new Exception("CAMPAIGN_STARTUP_PROBE "+reason);Debug.Log("CAMPAIGN_STARTUP_PASS "+reason);}
 IEnumerator Start(){var test=Run();while(true){object next;try{if(!test.MoveNext())break;next=test.Current;}catch(Exception ex){error=ex.ToString();Debug.LogException(ex);break;}yield return next;}Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/campaign-startup-test.txt","success="+(error.Length==0)+" checks="+checks+"\n"+error);Application.Quit(error.Length==0?0:1);}
 IEnumerator Run(){
  while(!(session=FindObjectOfType<DriveSession>()))yield return null;
  Check(!session.StartupReady&&session.world==null,"loading boundary visible before geometry");
  yield return CaptureRendered("loading",true);if(error.Length>0)throw new Exception(error);
  while(!session.StartupReady)yield return null;yield return null;yield return null;
  var b=session.business;
  Check(Path.GetFileName(b.SavePath)==(Reload?"campaign-startup-prepared.json":"campaign-startup-test.json")&&b.SavePath.Contains("artifacts"),"save path isolated");
  Check(b.enabledCampaign&&b.useV2&&b.ledger.Settings.days==3&&b.ledger.Day==1,"normal three-day fresh campaign");
  Check(b.preparing&&!b.ResumeBlocked&&!session.paused&&Time.timeScale==0,"fresh preparation holds simulation");
  Check(!b.CanAccept&&b.ledger.Elapsed==0,"no calls or day time before departure");
  if(Reload){
   var saved=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));
   Check(!saved.hasLive&&saved.days.active.elapsed==0,"actual serialized preparation checkpoint loaded");
   Check(b.saveStatus.Contains("불러왔습니다")&&session.law.ids.campaign==saved.ids.campaign,"prepared save identity retained instead of fresh campaign");
   Check(b.ledger.Account.Cash==EconomyLedger.RestoreCampaign(saved.days,saved.account).Account.Cash,"prepared financial progress retained");
  }else{
   Check(b.SaveCheckpoint(),"preparation checkpoint serialized");
   File.Copy(b.SavePath,Path.Combine(Path.GetDirectoryName(b.SavePath),"campaign-startup-prepared.json"),true);
  }
  yield return CaptureRendered("preparation",false);if(error.Length>0)throw new Exception(error);
  string campaign=session.law.ids.campaign;int cash=b.ledger.Account.Cash;
  b.Depart();Check(!b.preparing&&!session.paused&&Time.timeScale==1&&b.CanAccept,"one departure opens business");
  Check(session.law.ids.campaign==campaign&&b.ledger.Account.Cash==cash&&!b.restartPrompt,"departure preserves progress without recovery reset");
  double before=b.ledger.Elapsed;yield return new WaitForSecondsRealtime(.4f);Check(b.ledger.Elapsed>before,"day time advances after first departure");
  Check(b.ledger.Day==1&&b.ledger.Rides==0,"no reset or synthetic trip needed");
  yield return CaptureRendered("ready",false);if(error.Length>0)throw new Exception(error);
  Debug.Log("CAMPAIGN_STARTUP_PROBE_OK checks="+checks);
 }
 IEnumerator CaptureRendered(string stage,bool loading){
  for(int frame=0;frame<4;frame++){
   yield return new WaitForEndOfFrame();
   if(loading&&session.StartupReady){error="CAMPAIGN_STARTUP_PROBE loading frame did not render before startup completed";yield break;}
   var shot=ScreenCapture.CaptureScreenshotAsTexture();bool rendered=false;
   if(shot){var pixels=shot.GetPixels32();byte minimum=255,maximum=0;for(int i=0;i<pixels.Length;i+=31){var p=pixels[i];byte luminance=(byte)((p.r+p.g+p.b)/3);minimum=Math.Min(minimum,luminance);maximum=Math.Max(maximum,luminance);}rendered=maximum-minimum>20;
    if(rendered){Directory.CreateDirectory("artifacts");File.WriteAllBytes(Path.GetFullPath("artifacts/campaign-startup-"+stage+".png"),shot.EncodeToPNG());}Destroy(shot);
   }
   if(rendered){Debug.Log("CAMPAIGN_STARTUP_CAPTURE_OK stage="+stage);yield break;}
   yield return null;
  }
  error="CAMPAIGN_STARTUP_PROBE black/unrendered screenshot: "+stage;
 }
}
