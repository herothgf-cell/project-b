using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Explicit, isolated diagnostics. Pose-controlled checks are not a manual drive.
public sealed class ArrivalReadabilityProbe:MonoBehaviour {
 DriveSession session;int checks;bool pvp,captureOk=true;readonly StringBuilder report=new StringBuilder();
 static bool Has(string value){return Array.IndexOf(Environment.GetCommandLineArgs(),value)>=0;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Has("-arrivalReadabilityTest")||Has("-pvpArrivalReadabilityCheck"))new GameObject("Arrival readability acceptance").AddComponent<ArrivalReadabilityProbe>();}
 void Check(bool ok,string text){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+text);Debug.Log((ok?"ARRIVAL_PLAYER_PASS ":"ARRIVAL_PLAYER_FAIL ")+text);if(!ok)throw new Exception(text);}
 void Pose(MapStop stop,float side=0,Vector3? direction=null,float speed=0){var heading=direction??stop.Direction;var point=stop.Kerb+TrafficSystem.Right(stop.Direction)*side+Vector3.up*.65f;session.taxi.body.position=point;session.taxi.body.rotation=Quaternion.LookRotation(heading);session.taxi.transform.SetPositionAndRotation(point,session.taxi.body.rotation);session.taxi.ReplicaVelocity=heading*speed;session.taxi.body.velocity=heading*speed;Physics.SyncTransforms();}
 IEnumerator Start(){
  pvp=Has("-pvpArrivalReadabilityCheck");float deadline=Time.realtimeSinceStartup+90;
  while(!session&&Time.realtimeSinceStartup<deadline){
   session=FindObjectsOfType<DriveSession>().FirstOrDefault(s=>!s.IsSimulationBot);
   if(pvp&&!session){var shell=FindObjectOfType<Pvp.PvpBootstrap>();if(shell)shell.StartSimulation(true,600);}
   yield return null;
  }
  while(session&&!session.StartupReady&&Time.realtimeSinceStartup<deadline)yield return null;
  bool ok=false;try{Check(session&&session.StartupReady,"native world initialized");Prepare();Run();ok=true;}catch(Exception e){report.AppendLine(e.ToString());Debug.LogException(e);}
  if(ok&&Has("-arrivalCapture")){yield return Capture();ok&=captureOk;}
  if(ok&&Has("-arrivalClick")){yield return Click();ok&=captureOk;}
  var output=Path.GetFullPath("artifacts/arrival-"+(pvp?"pvp":"single")+"-player.txt");Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,report+"\nchecks="+checks+"\nsuccess="+ok);Debug.Log("ARRIVAL_PLAYER_"+(ok?"OK":"FAILED")+" checks="+checks);Application.Quit(ok?0:1);
 }
 void Prepare(){
  Check(pvp==(session.simulationMatch!=null),"selected map mode matches requested mode");
  if(!pvp)Check(session.business.SavePath.Replace('\\','/').Contains("/artifacts/"),"single diagnostic uses artifact save only");
  else foreach(var bot in session.simulationMatch.Bots){var brain=bot.GetComponent<PvpSimulationBot>();if(brain)brain.enabled=false;bot.taxi.enabled=false;bot.taxi.body.isKinematic=true;for(int i=0;i<session.jobs.stops.Length;i++)session.simulationMatch.Ledger.Release(i,bot.SimulationDriver);}
  session.business.enabled=false;session.business.preparing=false;session.business.enabledCampaign=true;session.business.useV2=true;
  if(!pvp){session.business.settings=PrototypeSettings.ForThreeDays();session.business.ledger=new EconomyLedger(session.business.settings);}
  session.jobs.ClearForDay();session.jobs.enabled=false;session.requests.enabled=false;session.manners.enabled=false;session.law.enabled=false;session.assisted.enabled=false;
  session.taxi.enabled=false;session.taxi.body.isKinematic=true;session.taxi.body.interpolation=RigidbodyInterpolation.None;session.paused=false;Time.timeScale=1;
 }
 void Run(){
  var j=session.jobs;int pickup=-1,drop=-1;
  for(int a=0;a<j.stops.Length&&pickup<0;a++)for(int b=0;b<j.stops.Length;b++)if(a!=b&&!float.IsInfinity(j.nav.Distance(j.stops[a],j.stops[b]))){pickup=a;drop=b;break;}
  Check(pickup>=0,"directed pickup/drop pair exists");Pose(j.stops[pickup]);
  j.ForcedRideCase=DrunkRideCase.Normal;j.offers[0]=new JobOffer{pickup=pickup,drop=drop,fare=5000,requiredScore=70,seconds=1200,distance=j.nav.Distance(j.stops[pickup],j.stops[drop])};
  Check(j.Accept(0)&&j.trip.State==TripState.ToPickup,"accept live call");j.nav.Clear();
  j.nav.SetDestination(j.stops[drop]);Check(j.nav.path.Count>1,"actual map has route geometry before loss");j.nav.AvoidEdge=j.nav.hdGoal;j.nav.Refresh();
  Check(j.nav.RetainingPreviousRoute&&j.nav.DisplayPath.Count>1&&j.nav.path.Count==0&&j.nav.hdRoute==null,"unavailable route retains display without restoring control geometry");
  var painted=session.GetComponent<FullMapOverlay>();painted.RefreshRoutePaint(true);Check(painted.RoutePaint.Exists(segment=>segment.retained),"map and minimap receive a distinct previous-route style");j.nav.Clear();
  var h=StopArrivalGuidance.For(session);Check(h.Ready&&h.Action=="F · 승객 승차","actual pickup readiness survives a missing navigation route");
  Pose(j.stops[pickup],3);h=StopArrivalGuidance.For(session);Check(!h.Ready&&h.Detail.Contains("정차 칸"),"actual bay mismatch is explained");
  Pose(j.stops[pickup],0,-j.stops[pickup].Direction);h=StopArrivalGuidance.For(session);Check(!h.Ready&&h.Detail.Contains("방향"),"actual heading mismatch is explained");
  Pose(j.stops[pickup]);Check(j.Interact()&&j.trip.State==TripState.OnTrip,"F boards through existing interaction");
  Pose(j.stops[drop]);j.nav.Clear();h=StopArrivalGuidance.For(session);Check(h.Ready&&h.Action=="F · 승객 하차","actual disembark readiness survives a missing navigation route");
  int count=session.business.passengers.Count;Check(j.Interact()&&!j.Active&&session.business.passengers.Count==count+1,"F delivers and settles exactly one actual ride");Pose(j.stops[drop],50);j.Interact();Check(session.business.passengers.Count==count+1,"repeated F does not settle twice");
  j.PrepareForNextCall();j.offers[0]=new JobOffer{pickup=pickup,drop=drop,fare=5000,requiredScore=70,seconds=1200,distance=j.nav.Distance(j.stops[pickup],j.stops[drop])};Pose(j.stops[pickup]);j.cooldown[pickup]=0;Check(j.Accept(0),"next pickup available for display test");
  var map=session.GetComponent<FullMapOverlay>();Check(map.Open(),"M map opens in native mode");map.FocusPlayer();var initial=map.ZoomLevel;var facility=map.Places().FirstOrDefault();if(facility!=null){Check(map.SelectPlace(facility),"facility selection remains available with player emphasis");Check(map.ZoomLevel>=1,"facility focus remains within zoom bounds");}map.Close();
 }
 IEnumerator Capture(){
  var j=session.jobs;var map=session.GetComponent<FullMapOverlay>();var stop=j.stops[j.current.pickup];
  foreach(int width in new[]{1280,1600,1920,2560}){
   Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.35f);Pose(stop);j.nav.Clear();session.paused=false;Time.timeScale=0;
   yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/arrival-"+(pvp?"pvp":"single")+"-hud-"+width+".png"));yield return new WaitForSecondsRealtime(.15f);
   session.paused=false;int before=map.RepaintCount;bool opened=map.Open();map.FocusPlayer();yield return null;yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();
   bool rendered=opened&&map.RepaintCount>before;captureOk&=rendered;report.AppendLine((rendered?"PASS ":"FAIL ")+"M canvas repaint at requested "+width+" actual "+Screen.width+"x"+Screen.height+" passes="+(map.RepaintCount-before));
   ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/arrival-"+(pvp?"pvp":"single")+"-map-"+width+".png"));yield return new WaitForSecondsRealtime(.25f);map.Close();
  }
  session.paused=false;Time.timeScale=1;Pose(stop);if(j.Interact()&&j.trip.State==TripState.OnTrip){Pose(j.stops[j.current.drop]);j.nav.Clear();yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/arrival-"+(pvp?"pvp":"single")+"-drop.png"));yield return new WaitForSecondsRealtime(.2f);}
 }
 IEnumerator Click(){
  var j=session.jobs;j.ClearForDay();j.Generate();session.paused=false;Time.timeScale=1;var map=session.GetComponent<FullMapOverlay>();
  if(!map.Open()){captureOk=false;yield break;}bool selected=false;for(int i=0;i<j.offers.Length;i++)if(map.SelectOffer(i)&&map.AcceptBlockedReason==""){selected=true;break;}
  if(!selected){captureOk=false;yield break;}Debug.Log("ARRIVAL_CLICK_READY "+map.AcceptButtonBounds);float until=Time.realtimeSinceStartup+150;
  while(map.AcceptButtonClickCount==0&&Time.realtimeSinceStartup<until)yield return null;
  captureOk=map.AcceptButtonClickCount==1&&j.trip.State==TripState.ToPickup&&!map.IsOpen&&!session.paused;
  report.AppendLine((captureOk?"PASS ":"FAIL ")+"actual GUI click accepts exactly once using scaled map coordinates");yield return new WaitForSecondsRealtime(.5f);
 }
}
