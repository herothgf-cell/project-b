using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// UI/state fixtures use a private campaign, never the player's files.
public sealed class HudRevisionProbe : MonoBehaviour {
 public static bool Enabled {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-hudRevisionCheck")>=0;}}
 public static readonly string CampaignPath=Path.Combine(Path.GetTempPath(),"taxi-hud-"+Guid.NewGuid().ToString("N"),"campaign.json");
 static bool attached;DriveSession s;int checks;bool done;float start;string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Enabled&&!attached){attached=true;new GameObject("HUD revision acceptance").AddComponent<HudRevisionProbe>();}}
 void Awake(){DontDestroyOnLoad(gameObject);start=Time.realtimeSinceStartup;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-hudCaptureOutput");output=i>=0&&i+1<args.Length?Path.GetFullPath(args[i+1]):Path.GetFullPath("artifacts/hud-revision-captures");Directory.CreateDirectory(output);}
 void Update(){if(!done&&Time.realtimeSinceStartup-start>240){done=true;Debug.LogError("HUD_PLAYER_TIMEOUT");Application.Quit(2);}}
 void Check(bool value,string message){checks++;Debug.Log("HUD_PLAYER_"+(value?"PASS ":"FAIL ")+message);if(!value)throw new Exception(message);}
 IEnumerator Start(){var run=Run();while(!done){object next;try{if(!run.MoveNext())yield break;next=run.Current;}catch(Exception e){done=true;Debug.LogException(e);Application.Quit(1);yield break;}yield return next;}}
 IEnumerator Ready(){while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady||s.business.IsRestoring)yield return null;yield return null;yield return null;}
 IEnumerator Capture(string name){yield return null;yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());Destroy(texture);Debug.Log("HUD_PLAYER_CAPTURE "+name);}
 void Pose(Vector3 position,Vector3 direction){s.taxi.SetSpawn(position+Vector3.up*.65f,Quaternion.LookRotation(direction));s.taxi.ResetTaxi();}
 IEnumerator Run(){
  yield return null;yield return null;Check(UnifiedModeBootstrap.Selected==UnifiedModeBootstrap.Mode.Menu,"mode menu opens without campaign");yield return Capture("00-mode");
  FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();
  Check(s.business.SavePath==CampaignPath&&s.business.preparing,"single uses isolated campaign and initial menu");
  Check(!DayPreparationPresentation.ShouldShow(1,true,false,false),"first day does not add settlement popup");s.business.ContinueFromMenu();Time.timeScale=0;
  Check(!s.business.preparing&&CockpitPresentation.TaxiStatus(s.jobs.trip.State)=="빈차","first start enters vacant operation");yield return Capture("01-vacant");
  int cash=s.business.ledger.Account.Cash;Check(s.services.ToggleLifeHub()&&s.services.PostTripVisible,"header opens life hubs before any delivery");yield return Capture("02-life-hubs");
  Check(s.business.ledger.Account.Cash==cash&&s.services.ToggleLifeHub()&&!s.services.PostTripVisible,"life hub view closes without payment");
  Time.timeScale=1;int offer=-1;float longest=-1;for(int i=0;i<s.jobs.offers.Length;i++){var candidate=s.jobs.offers[i];if(candidate!=null&&s.jobs.AcceptanceRejection(candidate).Length==0&&candidate.distance>longest){offer=i;longest=candidate.distance;}}
  Check(offer>=0&&s.jobs.Accept(offer),"existing real offer accepted");Time.timeScale=0;Check(CockpitPresentation.TaxiStatus(s.jobs.trip.State)=="예약","pickup shows reservation");
  var hud=s.GetComponent<DrivingReadability>();var route=s.jobs.nav.hdRoute;TrafficJunction junction=null;bool following=false;
  if(route!=null)for(float at=3;at<route.path.length-3;at+=5){Pose(route.path.Position(at),route.path.Tangent(at));hud.SelectSignal();var cue=hud.TurnCue;if(cue.Valid&&cue.HasNext&&hud.SignalTarget!=null){following=true;junction=hud.SignalTarget;break;}}
  Check(following,"actual directed route renders current and following maneuvers near a signal");
  if(junction!=null){
   var program=junction.gameProgram;var state=s.traffic.CaptureSignalRuntime().First(x=>x.junction==junction.name);state.stage=0;state.green=program.stages[0].green;state.elapsed=state.green-2;state.demand=Enumerable.Repeat(1,program.stages.Length).ToArray();s.traffic.RestoreSignalRuntime(new[]{state});
   var before=JsonUtility.ToJson(s.traffic.CaptureSignalRuntime()[0]);var preview=hud.NextSignalPreview;var after=JsonUtility.ToJson(s.traffic.CaptureSignalRuntime()[0]);Check(before==after,"drawing next signal leaves runtime authority unchanged");
  }
  yield return Capture("03-reserved-navigation");
  Check(s.jobs.trip.Board(),"occupied presentation fixture uses real trip transition");s.jobs.nav.SetDestination(s.jobs.EffectiveStop(s.jobs.current.drop));Check(CockpitPresentation.TaxiStatus(s.jobs.trip.State)=="","occupied trip hides vacant/reserved badge");yield return Capture("04-occupied");
  s.jobs.ClearForDay();var ledger=s.business.ledger;Check(ledger.UseMeal("hud-fixture-meal"),"actual meal booked to first-day account");ledger.Driver.satiety=50;ledger.Driver.fatigue=80;ledger.RequestClose("HUD preparation fixture");Check(ledger.Settle(),"first day settled by real ledger");
  Check(s.business.NextDay(),"next day uses real preparation boundary");yield return null;yield return null;
  var view=DayPreparationPresentation.Create(s.business.ledger);Check(view.previousDay==1&&view.totalExpense==3500,"day two shows actual first-day meal and parking expenses");Check(view.stats.First(x=>x.id=="fatigue").delta==-60,"day two shows actual fatigue recovery from day-end");
  string snapshot=JsonUtility.ToJson(s.business.ledger.Snapshot());DayPreparationPresentation.Create(s.business.ledger);Check(snapshot==JsonUtility.ToJson(s.business.ledger.Snapshot()),"preparation view never changes campaign");yield return Capture("05-day-two-1280");
  Screen.SetResolution(1920,1080,false);for(int i=0;i<12;i++)yield return null;yield return Capture("06-day-two-1920");s.business.Depart();Time.timeScale=0;Check(!s.business.preparing,"day-two start enters operation directly");yield return Capture("07-vacant-1920");
  Check(FindObjectOfType<UnifiedModeBootstrap>().ReturnToMenu(),"single saves before leaving mode");s=null;yield return null;while(UnifiedModeBootstrap.Selected!=UnifiedModeBootstrap.Mode.Menu)yield return null;
  FindObjectOfType<UnifiedModeBootstrap>().StartPvp();yield return null;FindObjectOfType<Pvp.PvpBootstrap>().StartSimulation(true,120);yield return Ready();Time.timeScale=0;
  Check(s.business.SimulationSession&&s.simulationMatch.Drivers.Count==4,"shared cockpit remains available in native PVP");yield return Capture("08-pvp-cockpit");done=true;Debug.Log("HUD_PLAYER_OK checks="+checks);Application.Quit(0);
 }
}
