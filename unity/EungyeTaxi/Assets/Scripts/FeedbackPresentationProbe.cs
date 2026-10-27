using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Opt-in position-controlled visual validation; never enabled in ordinary play.
public sealed class FeedbackPresentationProbe : MonoBehaviour {
 DriveSession s;int checks;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-feedbackUITest")>=0)new GameObject("playtest feedback validation").AddComponent<FeedbackPresentationProbe>();}
 void Check(bool ok,string why){if(!ok)throw new Exception("FEEDBACK_UI: "+why);checks++;Debug.Log("FEEDBACK_UI_PASS "+why);}
 IEnumerator Start(){var run=Run();while(true){object next=null;bool more;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}if(!more)break;yield return next;}Debug.Log("FEEDBACK_UI_OK checks="+checks);Application.Quit(0);}
 IEnumerator Run(){
  while(!s){s=FindObjectOfType<DriveSession>();yield return null;}
  yield return new WaitForEndOfFrame();Shot("startup");
  while(!s.StartupReady)yield return null;yield return null;yield return null;
  s.business.enabled=false;s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);s.business.enabledCampaign=s.business.useV2=true;s.business.preparing=false;
  s.jobs.enabled=s.jobs.nav.enabled=s.law.enabled=s.manners.enabled=s.taxi.enabled=s.assisted.enabled=false;s.taxi.body.isKinematic=true;s.taxi.body.interpolation=RigidbodyInterpolation.None;
  s.jobs.ClearForDay();s.services.enabled=true;s.paused=false;Time.timeScale=1;
  foreach(var c in s.traffic.cars)c.enabled=false;
  var restaurant=s.services.Restaurant;Check(restaurant!=null,"actual restaurant available");Pose(restaurant.parking,restaurant.direction);s.services.Toggle();Check(s.services.open&&s.services.CanUse(1)&&!s.services.CanUse(0),"restaurant local menu and billing action");
  yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Shot("restaurant-1280");s.services.Toggle();
  var station=s.services.Stations[0];Pose(station.pump,station.direction);s.services.Toggle();Check(s.services.open&&s.services.CanUse(0)&&s.services.CanUse(4)&&!s.services.CanUse(1),"gas local menu has fuel and wash only");
  yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Shot("fuel-1280");s.services.Toggle();
  s.services.destination="식당 입구 앞까지 안내 · H 해제 후 주차 칸에 진입 · 정차 후 F7";
  Pose(restaurant.approach.V(),restaurant.approach.Direction);yield return null;yield return new WaitForEndOfFrame();Shot("tip-1280");
  var zones=s.GetComponent<ServiceLawZones>();var zone=zones.zones.Find(x=>x.variant==14);Check(zone!=null&&zones.Live.StartInService(zone),"actual permissive service situation starts");var j=zone.junction;var forward=zones.Live.Direction;Pose(j.center-forward*(j.StopDistance(zones.Live.Axis)+12)+TrafficSystem.Right(forward)*2.5f,forward);s.services.destination="";
  yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();Shot("signals-1280");zones.Live.Stop();
  int p=s.jobs.Catalog.LegacyCount,d=p+1;var o=new JobOffer{pickup=p,drop=d,fare=5000,seconds=300,requiredScore=70,distance=s.jobs.nav.Distance(s.jobs.stops[p],s.jobs.stops[d])};s.jobs.offers[0]=o;Pose(s.jobs.stops[p].Kerb,s.jobs.stops[p].Direction);Check(s.jobs.Accept(0),"active pickup prepared for directional map pins");
  var map=s.GetComponent<FullMapOverlay>();
  s.jobs.ClearForDay();s.jobs.ResetOffers();var park=s.services.ParkingStop;Pose(park.Kerb,park.Direction);
  foreach(int width in new[]{1280,1920}){Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.4f);Check(map.Open(),"facility map opens "+width);var place=map.Places().Find(x=>x.glyph==8);Check(map.SelectPlace(place)&&map.PreviewPointCount>1,"parking facility preview is connected "+width);yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Shot("facility-map-"+width);Check(map.StartPlaceGuidance()&&s.jobs.nav.path.Count>1,"explicit facility navigation starts "+width);Check(map.Open(),"calls reopen "+width);map.ShowCalls();yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Shot("calls-"+width);map.Close();}
  s.jobs.offers[0]=o;Pose(s.jobs.stops[p].Kerb,s.jobs.stops[p].Direction);Check(s.jobs.Accept(0),"active pickup restored for final map captures");
  foreach(int width in new[]{1280,1920}){Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.6f);Check(map.Open(),"map opens "+width);yield return null;yield return new WaitForEndOfFrame();Shot("map-"+width);map.Close();}
 }
 void Pose(Vector3 p,Vector3 d){p.y=.65f;s.taxi.transform.SetPositionAndRotation(p,Quaternion.LookRotation(d));s.taxi.body.position=p;s.taxi.body.rotation=Quaternion.LookRotation(d);s.taxi.body.velocity=s.taxi.body.angularVelocity=Vector3.zero;var eye=p-d*10+Vector3.up*6;s.drivingCamera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(p+Vector3.up-eye));Physics.SyncTransforms();}
 void Shot(string name){var shot=ScreenCapture.CaptureScreenshotAsTexture();Check(shot&&shot.width>0,"rendered pixels "+name);File.WriteAllBytes(Path.GetFullPath("artifacts/feedback-"+name+".png"),shot.EncodeToPNG());float dark=1,light=0;for(int y=0;y<shot.height;y+=Mathf.Max(1,shot.height/32))for(int x=0;x<shot.width;x+=Mathf.Max(1,shot.width/48)){float value=shot.GetPixel(x,y).grayscale;dark=Mathf.Min(dark,value);light=Mathf.Max(light,value);}Check(light-dark>.05f,"rendered contrast "+name);if(name.StartsWith("facility-map-")||name.StartsWith("calls-")||name.StartsWith("map-"))Check(shot.GetPixel(8,shot.height-8).grayscale<.2f,"actual map modal rendered "+name);Destroy(shot);}
}
