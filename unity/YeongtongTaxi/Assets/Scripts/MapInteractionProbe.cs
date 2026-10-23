using System;
using System.Collections;
using UnityEngine;
public class MapInteractionProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-mapInteractionTest")>=0)new GameObject("Map UI checks").AddComponent<MapInteractionProbe>();}
 int count;bool ok=true;void Check(bool v,string name){count++;ok&=v;Debug.Log((v?"PASS ":"FAIL ")+name);}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);var s=FindObjectOfType<DriveSession>();s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);s.business.enabledCampaign=true;s.business.Depart();s.jobs.ClearForDay();s.jobs.Generate();var m=s.GetComponent<FullMapOverlay>();
  Check(!m.ShowLaw,"law starts hidden");Check(!m.ShowTraffic,"traffic starts hidden");Check(m.Open()&&Time.timeScale==0,"map pauses simulation");int cash=s.business.ledger.Account.Cash;bool nav=s.jobs.nav.HasDestination;
  Check(m.FocusStop(0)&&m.ZoomLevel>=2,"stop focus zooms");Check(!m.FocusStop(-1)&&!m.FocusStop(s.jobs.stops.Length),"invalid stops ignored");
  Check(s.business.ledger.Account.Cash==cash&&s.jobs.nav.HasDestination==nav&&!s.jobs.Active,"focus does not start trip or navigation");
  Check(m.SelectOffer(0)&&!s.jobs.Active&&m.SelectedOffer==0,"call select only focuses");Check(m.FocusSelectedStop(true)&&m.FocusSelectedStop(false),"pickup and destination both focus");
  Check(m.PreviewPointCount>1,"selected call previews connected pickup to drop route");var places=m.Places();Check(places.Count>=3,"facilities available");Check(m.SelectPlace(places[0])&&m.PreviewPointCount>1&&!s.jobs.Active&&s.jobs.nav.HasDestination==nav,"facility select previews without changing navigation");m.ShowLocation();Check(m.LocationOpen&&m.LocationText.Contains("지도 좌표")&&m.LocationText.Contains("eungye"),"location popup has live data");m.HideLocation();Check(m.SelectOffer(0),"return from facility to call");
  Check(FullMapOverlay.CongestionColor(0).g>FullMapOverlay.CongestionColor(0).r&&FullMapOverlay.CongestionColor(3).r>FullMapOverlay.CongestionColor(3).g&&FullMapOverlay.CongestionColor(9).g<.3f,"green orange red delay bands");
  var conditions=s.roadConditions;var testIncident=conditions.AddIncident(new Vector3(-900,0,-895),Vector3.forward,3.2f,20);Check(conditions.ExtraSeconds(new Vector3(-900,0,-900),new Vector3(-900,0,-890),Vector3.forward,3.2f)>=6&&conditions.ExtraSeconds(new Vector3(-900,0,-890),new Vector3(-900,0,-900),Vector3.back,3.2f)==0,"congestion affects only matching direction");conditions.incidents.Remove(testIncident);
  Check(m.AcceptSelected()&&s.jobs.Active&&!m.IsOpen&&Time.timeScale>0,"explicit accept closes map and accepts once");Check(!m.AcceptSelected(),"closed map cannot accept twice");
  Check(m.Open()&&m.FocusSelectedStop(false),"active trip destination focus");m.Close();Check(Time.timeScale>0&&!s.paused,"map restores driving");
  s.paused=true;Time.timeScale=0;Check(m.Open(),"map opens from paused state");m.Close();Check(s.paused&&Time.timeScale==0,"map restores prior pause");s.paused=false;Time.timeScale=1;
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080)}){Check(CockpitUI.LayoutFits(size.x,size.y),"HUD panels fit "+size);float scale=Mathf.Min(1,size.x/1600,size.y/900);var help=DriveHUD.HelpBounds(size.x,size.y);Check(!help.Overlaps(new Rect(24*scale,size.y-220*scale,320*scale,196*scale)),"help avoids speed panel "+size);}
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-uiCapture")>=0){
   foreach(var size in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080)}){
    Screen.SetResolution((int)size.x,(int)size.y,false);yield return new WaitForSecondsRealtime(1);
    ScreenCapture.CaptureScreenshot("D:/Git/project-b/artifacts/hud-live-"+(int)size.x+".png");yield return new WaitForSecondsRealtime(.5f);
    s.GetComponent<DriveHUD>().ToggleHelp();yield return new WaitForSecondsRealtime(.2f);ScreenCapture.CaptureScreenshot("D:/Git/project-b/artifacts/help-live-"+(int)size.x+".png");yield return new WaitForSecondsRealtime(.5f);s.GetComponent<DriveHUD>().ToggleHelp();
    m.Open();m.FocusSelectedStop(false);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot("D:/Git/project-b/artifacts/map-live-"+(int)size.x+".png");yield return new WaitForSecondsRealtime(.5f);m.ShowLocation();yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot("D:/Git/project-b/artifacts/location-live-"+(int)size.x+".png");yield return new WaitForSecondsRealtime(.5f);m.HideLocation();m.SelectPlace(places.Find(p=>p.glyph==9)??places[0]);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot("D:/Git/project-b/artifacts/facility-live-"+(int)size.x+".png");yield return new WaitForSecondsRealtime(.5f);Check(!m.StartPlaceGuidance(),"facility cannot replace active trip");m.Close();
   }
  }
  s.jobs.ClearForDay();m.Open();Check(m.SelectPlace(places[0])&&m.StartPlaceGuidance()&&s.jobs.nav.HasDestination&&!m.IsOpen,"explicit facility guidance starts route");m.Open();Check(m.SelectRoadPoint(places[0].position)&&m.StartPlaceGuidance()&&s.jobs.nav.HasDestination,"map coordinate snaps to connected road and starts navigation");m.Close();
  Debug.Log("MAP_INTERACTION_CHECKS "+count+" ok="+ok);Application.Quit(ok?0:1);
 }
}
