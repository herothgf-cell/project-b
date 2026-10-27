using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
public class FatigueOvertimeProbe:MonoBehaviour {
 bool ok=true;StringBuilder report=new StringBuilder();
 static void Tick(object target){target.GetType().GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(target,null);}
 void CheckShippedRoads(DriveSession s){
  int changes=0;foreach(var edge in s.jobs.nav.hd.edges)if(edge.id.StartsWith("reviewed:playtest:lane-change:"))changes++;
  Check(changes==14,"actual Player includes all14 authored lane changes");
  var junction=Array.Find(s.world.map.junctionRules,j=>j.id=="J5750_2576");Check(junction!=null&&junction.signalMode==0&&junction.crosswalk,"actual Player includes user-corrected unsignalized crossing");
 }
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fatigueOvertimeTest")>=0)new GameObject("Fatigue overtime probe").AddComponent<FatigueOvertimeProbe>();}
 void Check(bool v,string label){ok&=v;report.AppendLine((v?"PASS ":"FAIL ")+label);Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/fatigue-overtime-runtime.txt","success="+ok+"\n"+report);}
 IEnumerator Capture(string name){
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fatigueCapture")<0)yield break;
  // Hidden player windows need the same explicit drawable surface as score HUD captures.
  Screen.SetResolution(1280,720,FullScreenMode.Windowed);
  yield return new WaitForSecondsRealtime(.6f);
  yield return new WaitForEndOfFrame();
  string path=Path.GetFullPath("artifacts/fatigue-"+name+".png");
  ScreenCapture.CaptureScreenshot(path);
  yield return new WaitForSecondsRealtime(.5f);
  Check(File.Exists(path),"rendered "+name+" capture saved");
  if(File.Exists(path)){
   var rendered=new Texture2D(2,2);rendered.LoadImage(File.ReadAllBytes(path));
   float darkest=1,lightest=0;
   for(int y=0;y<rendered.height;y+=Mathf.Max(1,rendered.height/24))for(int x=0;x<rendered.width;x+=Mathf.Max(1,rendered.width/32)){float value=rendered.GetPixel(x,y).grayscale;darkest=Mathf.Min(darkest,value);lightest=Mathf.Max(lightest,value);}
   Check(lightest-darkest>.05f,"rendered "+name+" has visible scene/UI contrast");Destroy(rendered);
  }
 }
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);var s=FindObjectOfType<DriveSession>();while(!s.StartupReady)yield return null;yield return null;var b=s.business;b.enabled=false;b.enabledCampaign=true;b.useV2=true;b.preparing=false;b.ledger=new EconomyLedger(PrototypeSettings.ForOneDay());b.passengers.Clear();s.jobs.ClearForDay();s.paused=false;Time.timeScale=1;
  CheckShippedRoads(s);s.jobs.enabled=false;s.requests.enabled=false;s.law.enabled=false;s.manners.enabled=false;s.taxi.enabled=false;s.taxi.body.detectCollisions=false;s.taxi.body.useGravity=false;s.taxi.automated=true;s.taxi.autoThrottle=1;s.taxi.autoSteer=.8f;s.taxi.autoBrake=true;
  b.ledger.Advance(1200,0);Check(b.CanAccept&&b.ledger.Driver.fatigue==100&&!b.ShowingSummary,"budget enters overtime and initial calls remain enabled");
  var d=b.ledger.Driver.drowsiness;d.Advance(30,true,100);yield return Capture("warning");d.Advance(60,true,100);double exposure=d.visionSeconds;double elapsed=b.ledger.Elapsed;int clean=b.ledger.Care.Cleanliness;
  s.paused=true;Tick(b);Check(!b.DrowsyControlsLost&&!b.DrowsinessRunning&&d.visionSeconds==exposure&&b.ledger.Elapsed==elapsed,"pause freezes exposure and economy");s.paused=false;s.services.open=true;Tick(b);Check(d.visionSeconds==exposure&&b.ledger.Elapsed==elapsed&&b.ledger.Care.Cleanliness==clean,"service menu freezes exposure economy and cleanliness");s.services.open=false;
  var saved=JsonUtility.FromJson<EconomySnapshot>(JsonUtility.ToJson(b.ledger.Snapshot()));var copy=EconomyLedger.RestoreCampaign(saved,b.ledger.Account.Snapshot());Check(copy.Driver.drowsiness.Tunnel&&copy.Driver.drowsiness.visionSeconds==90,"actual JsonUtility roundtrip retains persistent narrowed vision");yield return Capture("tunnel");
  var pickup=s.jobs.stops[0];s.taxi.body.position=pickup.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(pickup.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);s.taxi.body.velocity=Vector3.zero;Physics.SyncTransforms();s.jobs.offers[0]=new JobOffer{pickup=0,drop=3,fare=5000,seconds=240,requiredScore=70,distance=s.jobs.nav.Distance(s.jobs.stops[0],s.jobs.stops[3])};
  Check(s.jobs.Accept(0)&&s.jobs.Interact()&&s.jobs.trip.State==TripState.OnTrip,"actual passenger boarded before cancellation threshold");s.requests.BeginKind(RequestKind.Quiet);
  // Real grounded physics and the live manners observer, not a disabled-score assertion.
  foreach(var car in s.traffic.cars)if(car)car.gameObject.SetActive(false);
  var path=s.jobs.nav.hd.paths[Array.FindIndex(s.jobs.nav.hd.edges,e=>e.id=="local:8131:3977:8203:6159:part:2")];
  s.taxi.transform.SetPositionAndRotation(path.Position(0)+Vector3.up*.65f,Quaternion.LookRotation(path.Tangent(0)));s.taxi.body.position=s.taxi.transform.position;s.taxi.body.rotation=s.taxi.transform.rotation;s.taxi.body.velocity=Vector3.zero;s.taxi.body.detectCollisions=true;s.taxi.body.useGravity=true;s.taxi.autoThrottle=s.taxi.autoSteer=0;s.taxi.autoBrake=false;s.taxi.enabled=true;s.manners.enabled=true;s.manners.Suppress(.8f);Physics.SyncTransforms();
  yield return new WaitForSeconds(1);
  int grounded=0;foreach(var wheel in s.taxi.wheels)if(wheel.isGrounded)grounded++;Check(grounded>=2,"system stop fixture uses grounded tyre physics and active manners");
  int cash=b.ledger.Account.Cash;int law=s.law.ledger.TotalScore;int manner=s.manners.ledger.Score;d.Advance(30,true,100);s.taxi.body.velocity=s.taxi.transform.forward*16;Check(!s.jobs.CancelForFatigue()&&s.jobs.Active,"moving passenger cannot instantly disembark");b.enabled=true;float stopDeadline=Time.realtimeSinceStartup+8;while(s.jobs.Active&&Time.realtimeSinceStartup<stopDeadline)yield return new WaitForFixedUpdate();b.enabled=false;
  Check(!s.jobs.Active&&s.taxi.body.velocity.magnitude<.56f&&s.jobs.failed==1&&b.ledger.FailedTrips==1,"automatic deceleration stops before one passenger cancellation");Check(b.ledger.Rides==0&&b.ledger.Income==0&&b.ledger.Account.Cash==cash&&b.passengers.Count==1&&b.passengers[0].cancelledForFatigue&&b.passengers[0].paid==0,"cancellation records zero fare without payment");Check(!s.jobs.CancelForFatigue()&&b.passengers.Count==1&&b.ledger.FailedTrips==1,"repeat cancellation cannot replay result or failure");Check(s.law.ledger.TotalScore==law&&s.manners.ledger.Score==manner&&!b.CanAccept,"cancellation adds no law or manner penalty and blocks new calls");
  s.manners.enabled=false;s.taxi.enabled=false;d.Advance(60,true,100);Tick(s.taxi);bool suppressed=true;foreach(var w in s.taxi.wheels)suppressed&=w.motorTorque==0&&w.brakeTorque==0&&w.steerAngle==0;Check(d.Blackout&&b.DrowsyControlsLost&&suppressed,"blackout suppresses actual wheel input");yield return Capture("blackout");
  s.taxi.body.velocity=Vector3.forward*4;double before=b.ledger.Elapsed;Check(!b.RequestEmergencyHome()&&b.ledger.Elapsed==before,"moving blackout cannot teleport home");s.taxi.body.velocity=Vector3.zero;Check(b.RequestEmergencyHome()&&b.ledger.Driver.fatigue==80&&b.ledger.Elapsed==before+100&&b.ledger.Day==1&&b.ledger.Operating,"explicit home rest recovers20 and keeps same day");Check(!b.RequestEmergencyHome()&&b.ledger.Elapsed==before+100&&b.ledger.Driver.drowsiness.visionSeconds==0,"home recovery cannot repeat after exposure reset");Check(s.services.AtParking(),"home rest places taxi at existing temporary parking hub");
  var rest=s.services.RestArea;Check(rest!=null,"rest hub exists");if(rest!=null){s.taxi.body.position=rest.pump+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(rest.direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);s.taxi.body.velocity=Vector3.zero;Physics.SyncTransforms();Check(s.services.AtRestArea()&&s.services.CanUse(3)&&s.services.CanUse(2),"rest hub permits normal sleep and end");s.services.Toggle();Check(s.services.RequestAction(3)&&s.services.ConfirmAction(true)&&b.ledger.Driver.fatigue==40,"normal rest menu still recovers40");before=b.ledger.Elapsed;Check(b.RequestEndDay()&&b.ledger.Driver.fatigue==0&&b.ledger.Elapsed==before+100,"rest hub close includes normal100second rest once");Check(!b.RequestEndDay()&&b.ledger.Elapsed==before+100,"repeat close cannot charge rest time again");Check(b.ledger.Settle()&&b.ShowingSummary,"explicit close settles overtime");}
  Application.Quit(ok?0:1);
 }
}
