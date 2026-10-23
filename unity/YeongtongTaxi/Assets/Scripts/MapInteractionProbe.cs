using System;
using System.Collections;
using UnityEngine;
public class MapInteractionProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-mapInteractionTest")>=0)new GameObject("Map UI checks").AddComponent<MapInteractionProbe>();}
 int count;bool ok=true;void Check(bool v,string name){count++;ok&=v;Debug.Log((v?"PASS ":"FAIL ")+name);}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);var s=FindObjectOfType<DriveSession>();s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);s.business.enabledCampaign=true;s.business.Depart();s.jobs.ClearForDay();s.jobs.Generate();var m=s.GetComponent<FullMapOverlay>();
  Check(!m.ShowLaw,"law starts hidden");Check(m.Open()&&Time.timeScale==0,"map pauses simulation");int cash=s.business.ledger.Account.Cash;bool nav=s.jobs.nav.HasDestination;
  Check(m.FocusStop(0)&&m.ZoomLevel>=2,"stop focus zooms");Check(!m.FocusStop(-1)&&!m.FocusStop(s.jobs.stops.Length),"invalid stops ignored");
  Check(s.business.ledger.Account.Cash==cash&&s.jobs.nav.HasDestination==nav&&!s.jobs.Active,"focus does not start trip or navigation");
  Check(m.SelectOffer(0)&&!s.jobs.Active&&m.SelectedOffer==0,"call select only focuses");Check(m.FocusSelectedStop(true)&&m.FocusSelectedStop(false),"pickup and destination both focus");
  Check(m.AcceptSelected()&&s.jobs.Active&&!m.IsOpen&&Time.timeScale>0,"explicit accept closes map and accepts once");Check(!m.AcceptSelected(),"closed map cannot accept twice");
  Check(m.Open()&&m.FocusSelectedStop(false),"active trip destination focus");m.Close();Check(Time.timeScale>0&&!s.paused,"map restores driving");
  s.paused=true;Time.timeScale=0;Check(m.Open(),"map opens from paused state");m.Close();Check(s.paused&&Time.timeScale==0,"map restores prior pause");s.paused=false;Time.timeScale=1;
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080)}){Check(CockpitUI.LayoutFits(size.x,size.y),"HUD panels fit "+size);}
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-uiCapture")>=0){
   foreach(var size in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080)}){
    Screen.SetResolution((int)size.x,(int)size.y,false);yield return new WaitForSecondsRealtime(1);
    ScreenCapture.CaptureScreenshot("D:/Git/project-b/artifacts/hud-live-"+(int)size.x+".png");yield return new WaitForSecondsRealtime(.5f);
    m.Open();m.FocusSelectedStop(false);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot("D:/Git/project-b/artifacts/map-live-"+(int)size.x+".png");yield return new WaitForSecondsRealtime(.5f);m.Close();
   }
  }
  Debug.Log("MAP_INTERACTION_CHECKS "+count+" ok="+ok);Application.Quit(ok?0:1);
 }
}
