using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Explicit command-line fixture only. Uses UnifiedModeProbe's temporary save route.
// Poses verify UI/state integration, not automatic driving or manual play.
public sealed class BrightUIProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-brightUiCheck")>=0&&!FindObjectOfType<BrightUIProbe>()){var root=new GameObject("Bright UI acceptance");DontDestroyOnLoad(root);root.AddComponent<BrightUIProbe>();}}
 bool ok=true,done;int checks;float started;string output;DriveSession s;
 void Check(bool value,string text){checks++;ok&=value;Debug.Log("BRIGHT_UI "+(value?"PASS ":"FAIL ")+text);}
 void Awake(){started=Time.realtimeSinceStartup;var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-brightUiOutput");output=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/ui-bright/player");Directory.CreateDirectory(output);Application.runInBackground=true;}
 void Update(){if(!done&&Time.realtimeSinceStartup-started>240){done=true;Debug.LogError("BRIGHT_UI_TIMEOUT");Application.Quit(2);}}
 IEnumerator Start(){var run=Run();while(!done){object next;try{if(!run.MoveNext())yield break;next=run.Current;}catch(Exception e){done=true;Debug.LogError("BRIGHT_UI_EXCEPTION "+e);Application.Quit(2);yield break;}yield return next;}}
 IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();var path=Path.Combine(output,name+".png");ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.3f);Check(File.Exists(path),"capture "+name);}
 IEnumerator Ready(){s=null;while(!s||!s.StartupReady||s.business.IsRestoring){foreach(var item in FindObjectsOfType<DriveSession>())if(!item.IsSimulationBot)s=item;yield return null;}yield return null;}
 void Pose(MapStop stop){var t=s.taxi;t.body.velocity=t.body.angularVelocity=Vector3.zero;t.body.position=stop.Kerb+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(stop.Direction);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();s.law.ResetObservation();s.manners.Suppress(2);}
 IEnumerator Run(){
  yield return new WaitForSecondsRealtime(1);yield return Capture("01-mode-menu");
  FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();
  Check(s.business.SavePath==UnifiedModeProbe.CampaignPath,"single isolated temporary campaign");
  yield return Capture("02-single-preparation");s.business.Depart();s.taxi.automated=true;s.taxi.autoBrake=true;
  var map=s.GetComponent<FullMapOverlay>();
  foreach(int width in new[]{1280,1920}){
   Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);
   Check(CockpitUI.LayoutFits(Screen.width,Screen.height),"HUD reference fits "+width);
   yield return Capture("03-single-hud-"+width);
   Check(map.Open(),"single map opens "+width);double elapsed=s.business.ledger.Elapsed;
   yield return Capture("04-single-map-"+width);Check(s.business.ledger.Elapsed==elapsed,"single map freezes timing "+width);
   int selected=Array.FindIndex(s.jobs.offers,o=>o!=null);Check(selected>=0,"forecast call available "+width);map.SelectOffer(selected);Check(map.SelectedOfferForecastText.Length>0,"selected forecast visible "+width);yield return Capture("04c-single-forecast-"+width);
   int expense=s.business.ledger.Account.Expense(s.business.ledger.Day,null);Check(map.ShowFacilityPlanner(0),"facility chooser opens "+width);yield return Capture("04d-single-facilities-"+width);Check(s.business.ledger.Account.Expense(s.business.ledger.Day,null)==expense&&s.business.ledger.Elapsed==elapsed,"facility browsing changes no payment/time "+width);map.Close();
  }
  s.drivingCamera.SetFirstPerson(true);yield return Capture("04a-single-cockpit");s.drivingCamera.SetFirstPerson(false);
  var editor=s.GetComponent<RoadMapEditor>();editor.Open();yield return Capture("04b-editor-colors");editor.Close();
  // Normal native methods with controlled location, as in the existing receipt probe.
  foreach(var c in s.traffic.cars){c.Recycle();c.enabled=false;}
  var jobs=s.jobs;jobs.ClearForDay();var forecastRoute=jobs.nav.hd.Route(jobs.stops[0].laneEdge,jobs.stops[0].laneAt,jobs.stops[3].laneEdge,jobs.stops[3].laneAt);jobs.offers[0]=new JobOffer{pickup=0,drop=3,fare=5000,seconds=2000,requiredScore=70,distance=jobs.nav.Distance(jobs.stops[0],jobs.stops[3]),pickupSeconds=600,timingPolicyVersion=1,safeRouteSeconds=OperationForecastAdapter.RouteSeconds(s,forecastRoute)};
  Pose(jobs.stops[0]);Check(jobs.Accept(0)&&jobs.Interact(),"fixture native call acceptance and boarding");
  var selectedRoute=jobs.nav.hdRoute;var cue=s.GetComponent<DrivingReadability>().TurnCue;Check(cue.Valid&&cue.Distance>=0,"single turn card reads live directed route");Check(jobs.nav.hdRoute==selectedRoute,"turn card never replaces selected route");yield return Capture("05a-single-navigation");
  s.requests.BeginKind(RequestKind.Comfort);s.requests.AdvanceSafeStop(3);yield return Capture("05-single-passenger-request");
  s.requests.request.DeferSafety();Pose(jobs.stops[jobs.current.drop]);Check(jobs.Interact(),"fixture native arrival and one settlement");
  var receipt=s.GetComponent<RideCompletionPanel>();if(!receipt.Open)receipt.ShowLast();int income=s.business.ledger.Income;
  Check(receipt.Open&&receipt.Receipt.Contains("실수령"),"real receipt fields available");yield return Capture("06-single-receipt");receipt.Close();Check(s.business.ledger.Income==income,"closing receipt does not pay twice");
  s.business.ledger.RequestClose("UI screenshot fixture");yield return new WaitForSecondsRealtime(.5f);yield return Capture("07-single-day-summary");
  int cash=s.business.ledger.Account.Cash;int net=s.business.ledger.TotalNet;
  Check(!OneDayHUD.DayDetailsOpen,"day summary starts compact");OneDayHUD.ToggleDayDetails();yield return Capture("07a-single-day-details");
  Check(OneDayHUD.DayDetailsOpen&&s.business.ledger.Account.Cash==cash&&s.business.ledger.TotalNet==net,"expanding full day details changes no transactions");OneDayHUD.ToggleDayDetails();
  FindObjectOfType<UnifiedModeBootstrap>().ReturnToMenu();yield return new WaitForSecondsRealtime(1);
  FindObjectOfType<UnifiedModeBootstrap>().StartPvp();yield return new WaitForSecondsRealtime(1);yield return Capture("08-pvp-setup");
  FindObjectOfType<Pvp.PvpBootstrap>().StartSimulation(true,180);yield return Ready();
  Check(s.simulationMatch&&s.simulationMatch.Drivers.Count==4,"PVP four native sessions");Check(s.business.SavePath=="","PVP has no campaign save");
  map=s.GetComponent<FullMapOverlay>();
  foreach(int width in new[]{1280,1920}){
   Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);yield return Capture("09-pvp-hud-"+width);
   Check(map.Open(),"PVP map opens "+width);float elapsed=s.simulationMatch.Elapsed;yield return Capture("10-pvp-map-"+width);Check(s.simulationMatch.Elapsed>elapsed&&!s.paused,"PVP map keeps match running "+width);Check(map.ShowFacilityPlanner(1),"PVP meal chooser opens "+width);yield return Capture("10b-pvp-facilities-"+width);map.Close();
  }
  int available=Array.FindIndex(s.jobs.offers,o=>o!=null);Check(available>=0&&s.jobs.Accept(available),"PVP human accepts real shared call for navigation");yield return new WaitForSecondsRealtime(.2f);Check(s.GetComponent<DrivingReadability>().TurnCue.Valid,"PVP turn card reads live pickup route");yield return Capture("10a-pvp-navigation");
  s.simulationMatch.Finish();yield return Capture("11-pvp-results");Check(Time.timeScale==0,"PVP final result pauses match");
  done=true;Debug.Log("BRIGHT_UI_RESULT "+(ok?"PASS":"FAIL")+" checks="+checks);Application.Quit(ok?0:1);
 }
}
