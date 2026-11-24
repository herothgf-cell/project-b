using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Out-of-town call, player acceptance (Eungye single, 2026-11-24). A 20-minute shift; an ordinary call is turned
// into the out-of-town call, boarded and delivered at the edge stop (stops are posed, not driven). Checks the card,
// that no hurry request, mission or chained call rides along, the blackout passing 6 minutes, fuel and fatigue
// once, the fare, and the daily limit.
public sealed class OutOfTownProbe : MonoBehaviour {
 const string Flag="-outOfTownCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Out-of-town acceptance").AddComponent<OutOfTownProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("OUT_OF_TOWN_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Info(string line){report.AppendLine("INFO "+line);}
 void Pose(MapStop p){s.taxi.body.velocity=Vector3.zero;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=p.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(p.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();}
 bool Feed(string text){var n=s.GetComponent<DriveNotifications>();return n&&n.Feed.Any(line=>line.Contains(text));}
 IEnumerator Start(){
  float limit=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limit){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-outOfTownOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/out-of-town");Directory.CreateDirectory(output);
  yield return null;string error=null;var j=s.jobs;var b=s.business;JobOffer o=null;int edge=OutOfTownRules.EdgeStop;
  try{
   b.settings=PrototypeSettings.ForThreeDays();b.settings.daySeconds=1200;b.ledger=new EconomyLedger(b.settings);b.passengers.Clear();b.enabledCampaign=true;b.useV2=true;b.preparing=false;s.paused=false;Time.timeScale=1;
   j.ClearForDay();s.law.ids=new PrototypeIds(Guid.NewGuid().ToString("N"));s.law.ledger=new LawLedger();s.law.tripId="";b.Story.firstDispatchAccepted=true;
   Check(s.outOfTown,"the out-of-town component is attached");
   Check(j.OutOfTownBlocked()=="","a fresh 20-minute shift allows the call ("+j.OutOfTownBlocked()+")");
   Check(edge<j.stops.Length,"edge stop "+edge+" exists: "+KoreanText.Stop(j.stops[edge].name));
   Array.Clear(j.cooldown,0,j.cooldown.Length);
   for(int p=0;p<j.stops.Length&&o==null;p++){if(p==edge)continue;var c=new JobOffer{pickup=p,drop=(p+3)%j.stops.Length,fare=6000,seconds=600,requiredScore=60,safeRouteSeconds=120};c.distance=j.nav.Distance(j.stops[p],j.stops[c.drop]);Pose(j.stops[p]);
    if(!j.MakeOutOfTown(c)){if(p<4)Info("not converted at "+p+": "+j.OutOfTownReject);}else{j.offers[0]=c;j.ForcedRideCase=DrunkRideCase.Normal;j.ForcedPassengerType=PassengerType.General;Pose(j.stops[p]);if(j.Accept(0))o=c;else Info("accept refused at "+p+": "+j.message);}}
   Check(o!=null,"an ordinary call becomes the out-of-town call and is accepted");
   Info("pickup "+KoreanText.Stop(j.stops[o.pickup].name)+" -> "+KoreanText.Stop(j.stops[o.drop].name)+" "+Mathf.RoundToInt(o.distance)+" m, fare "+o.fare+", deadline "+o.seconds);
   Check(o.variety==CallBoardRules.OutOfTown&&o.drop==edge&&o.mission==MissionRules.None,"the call drops at the edge stop, no mission");
   Check(o.fare==OutOfTownRules.Fare(o.distance/8f)&&o.fare>=20000,"fare by shift time ("+o.fare.ToString("N0")+" won)");
   var parts=j.CallCardParts(0,100);Check(parts.tags.Contains("시외(")&&parts.tags.Contains("시외 구간 6분"),"the card shows the out-of-town tag and the leg ("+parts.tags+")");
   for(int i=0;i<4&&j.trip.State==TripState.ToPickup;i++)j.Interact();Check(j.trip.State==TripState.OnTrip,"board");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(4);
  try{Check(!s.hurry.Showing&&s.hurry.LastBlocked=="시외 콜","no hurry request on the out-of-town call ("+s.hurry.LastBlocked+")");Check(!j.ChainWindow,"no chained call window");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  var l=b.ledger;double elapsedBefore=l.Elapsed;float fuelBefore=l.Account.Fuel;
  j.trip.Tick(30);Pose(j.stops[edge]);
  {float until=Time.realtimeSinceStartup+8;while(j.trip.State==TripState.OnTrip&&Time.realtimeSinceStartup<until){if(s.requests.HasPendingChoice)s.requests.request.DeferSafety();j.Interact();yield return null;}}
  {float until=Time.realtimeSinceStartup+3;while(!s.outOfTown.Blackout&&Time.realtimeSinceStartup<until)yield return null;}
  try{Check(s.outOfTown.Blackout,"the screen fades out after the drop");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSecondsRealtime(1.2f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"blackout.png"));
  {float until=Time.realtimeSinceStartup+8;while(s.outOfTown.Blackout&&Time.realtimeSinceStartup<until)yield return null;}
  if(j.trip.State!=TripState.Idle&&j.trip.State!=TripState.OnTrip)for(int i=0;i<2;i++)j.Interact();
  var panel=s.GetComponent<RideCompletionPanel>();if(panel&&panel.Open)panel.Close();s.paused=false;Time.timeScale=1;yield return null;
  try{
   var r=b.passengers.Last();
   Check(!s.outOfTown.Blackout&&s.outOfTown.Legs==1,"the leg passed once ("+s.outOfTown.LastOutcome+")");
   double leg=s.outOfTown.LastElapsedAfter-s.outOfTown.LastElapsedBefore;Check(Math.Abs(leg-360)<.5,"6 minutes of shift passed in the blackout ("+leg.ToString("0.0")+" s)");
   float fuel=s.outOfTown.LastFuelBefore-s.outOfTown.LastFuelAfter;Check(fuel>0,"fuel used on the leg ("+fuel.ToString("0.0")+")");
   Check(s.outOfTown.LastFatigueAfter>s.outOfTown.LastFatigueBefore,"fatigue rose ("+s.outOfTown.LastFatigueBefore.ToString("0.0")+" -> "+s.outOfTown.LastFatigueAfter.ToString("0.0")+")");
   Check(r.paid>0&&r.quoted==o.fare,"the passenger paid the out-of-town fare (quoted "+r.quoted+", paid "+r.paid+")");
   Check(Feed("시외 구간 · 영업 시간 6분"),"a card tells the driver");
   Check(j.OutOfTownDoneToday==1&&j.OutOfTownBlocked()=="하루 한도","no second out-of-town call today");
   Info("shift "+elapsedBefore.ToString("0")+" -> "+l.Elapsed.ToString("0")+" s, fuel "+fuelBefore.ToString("0.0")+" -> "+l.Account.Fuel.ToString("0.0"));
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // A short shift: the share rule keeps it off the board.
  try{b.settings.daySeconds=720;b.ledger=new EconomyLedger(b.settings);Check(j.OutOfTownBlocked()=="영업 시간이 짧음","a 12-minute shift gets no out-of-town call");}catch(Exception e){error=e.ToString();}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("OUT_OF_TOWN_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
