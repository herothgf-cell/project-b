using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Uses controlled stops and real boarding, request, settlement and reload entry points.
public sealed class PassengerStoryProbe:MonoBehaviour {
 const string Folder="artifacts/passenger-story-player";
 static bool Matrix {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-passengerStoryMatrixCheck")>=0;}}
 public static bool Reload {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-passengerStoryReloadCheck")>=0;}}
 public static bool Enabled {get{return Matrix||Reload||Array.IndexOf(Environment.GetCommandLineArgs(),"-passengerStoryCheck")>=0;}}
 public static string SavePath {get{return Path.GetFullPath(Path.Combine(Folder,Matrix?"matrix.json":"campaign.json"));}}
 DriveSession session;
 int checks;
 float started;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){
  if(!Enabled)return;
  var probe=new GameObject("Passenger story acceptance");
  DontDestroyOnLoad(probe);probe.AddComponent<PassengerStoryProbe>();
 }
 void Check(bool condition,string message){
  checks++;Debug.Log("PASSENGER_STORY_PLAYER "+(condition?"PASS ":"FAIL ")+message);
  if(!condition)throw new InvalidOperationException(message);
 }
 void Update(){if(Time.realtimeSinceStartup-started>180){Debug.LogError("PASSENGER_STORY_PLAYER timeout");Application.Quit(2);}}
 IEnumerator Start(){
  started=Time.realtimeSinceStartup;var run=Run();
  while(true){
   object step;
   try{if(!run.MoveNext())yield break;step=run.Current;}
   catch(Exception error){Debug.LogError("PASSENGER_STORY_PLAYER_EXCEPTION "+error);Application.Quit(2);yield break;}
   yield return step;
  }
 }
 void Pose(int index){
  var stop=session.jobs.EffectiveStop(index);
  var body=session.taxi.body;
  body.position=stop.Kerb+Vector3.up*.65f;body.rotation=Quaternion.LookRotation(stop.Direction);
  session.taxi.transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();
 }
 void CheckTerminalMatrix(){
  foreach(var story in PassengerStories.All){
   var facts=new PassengerJobs.StoryRideFacts{arrivedOnTime=true,safe=true,behaviorChoice=0,
    request=k=>new PassengerRequestSnapshot{kind=k,goalRecorded=true,outcome=1,accepted=true,replyCaptured=true,reply=PassengerReply.StoryFareAssist}};
   Check(PassengerJobs.EvaluateStoryEnding(story,facts)==PassengerStoryEnding.Good,"synthetic positive terminal evidence "+story.id);
   facts.arrivedOnTime=false;facts.safe=false;facts.jolt=true;facts.behaviorChoice=1;
   facts.request=k=>new PassengerRequestSnapshot{kind=k,goalRecorded=true,outcome=-1,accepted=true};
   Check(PassengerJobs.EvaluateStoryEnding(story,facts)==PassengerStoryEnding.Disappointing,"synthetic negative terminal evidence "+story.id);
   facts.arrivedOnTime=true;facts.safe=true;facts.jolt=false;facts.behaviorChoice=-1;facts.request=k=>null;
   Check(PassengerJobs.EvaluateStoryEnding(story,facts)==PassengerStoryEnding.Ordinary,"synthetic unobserved terminal evidence "+story.id);
  }
 }
 JobOffer FixtureOffer(string storyId){
  var j=session.jobs;
  var safeStops=Enumerable.Range(0,j.stops.Length).Where(i=>{
   if(j.StopOccupied(i)||Time.time<j.cooldown[i])return false;
   Pose(i);return j.At(i)&&session.requests.SafeStationarySpace;
  }).ToArray();
  foreach(int pickup in safeStops){
   Pose(pickup);
   foreach(int drop in safeStops){
    if(pickup==drop)continue;
    float distance=j.nav.Distance(j.stops[pickup],j.stops[drop]);
    if(distance<100||distance>2000)continue;
    if(storyId=="moving-day"&&!j.nav.HasLargeRoadAlternativeTo(j.stops[drop]))continue;
    return new JobOffer{pickup=pickup,drop=drop,fare=6000,seconds=600,requiredScore=65,distance=distance,storyId=storyId};
   }
  }
  throw new InvalidOperationException("No clear stops for story "+storyId);
 }
 void SupplyRequestEvidence(PassengerStoryDefinition story,bool goalSucceeded){
  var requests=session.requests;
  // This matrix controls the request observation result independently of road motion.
  // The normal outcome evaluator consumes it when the player actually alights.
  requests.BeginKind(story.request);
  var evidence=requests.CaptureLive();
  evidence.request=new PassengerRequestSnapshot{
   trip=session.law.tripId,kind=story.request,resolved=true,accepted=true,
   goalRecorded=true,outcomeVersion=1,outcome=goalSucceeded?1:-1,
   replyCaptured=story.request==RequestKind.FareHelp,
   reply=story.request==RequestKind.FareHelp?PassengerReply.StoryFareAssist:PassengerReply.Accept
  };
  requests.RestoreLive(evidence);
 }
 IEnumerator RunLocationMatrix(){
  var b=session.business;var j=session.jobs;var requests=session.requests;
  requests.enabled=false;
  foreach(var story in PassengerStories.All)foreach(bool good in new[]{true,false}){
   int day=b.ledger.Day;
   string label=story.id+" "+(good?"good":"disappointing");
   // Allow each case to exercise its preset regardless of the previous case's pool.
   // A plain encounter resets the same recovery flag that ordinary operation uses.
   string sequence="story-fixture-ordinary-"+day;
   b.ledger.Care.Wash();
   Check(b.ledger.Care.ReserveStoryPassenger(sequence,PassengerType.General)
    &&b.ledger.Care.CommitPassenger(sequence,PassengerType.General),"fixture supplies ordinary recovery encounter "+label);
   var offer=FixtureOffer(story.id);
   Pose(offer.pickup);j.offers[0]=offer;
   Check(j.Accept(0)&&j.Interact(),"location-controlled pickup "+label);
   yield return null;
   if(j.trip.State==TripState.ToPickup){
    requests.AdvanceSafeStop(3);
    int reply=good?0:1;
    Check(j.RespondBehavior(reply),"existing loading or protection choice "+label);
    if(j.trip.State==TripState.ToPickup)j.AdvanceBehavior(j.Behavior.WorkRemaining+1);
   }
   Check(j.trip.State==TripState.OnTrip&&j.CurrentStory?.id==story.id&&j.Behavior.PresetId==story.presetId
    &&b.Logbook.HasStoryAssignment(day,story.id),"real boarding and assignment "+label);
   yield return null;
   if(j.DrunkRide.IsDrunk)j.DrunkRide.Advance(8,0);
   if(j.Behavior.HasChoice)Check(j.RespondBehavior(0),"existing boarding response "+label);
   SupplyRequestEvidence(story,good||story.id!="feverish-child");
   if(!good)switch(story.id){
    case "moving-day":case "plant-gift":break; // Physical luggage/protection choice above.
    case "feverish-child":break; // Existing Comfort request's failed observation fixture.
    case "night-shift":
     j.Behavior.Advance(40,false,true,.3f,100);
     Check(j.Behavior.ObserveJolt(),"existing sleep interruption "+label);
     break;
    case "after-dinner":
     j.DrunkRide.VomitPlanned=true;
     j.DrunkRide.Advance(1,.9f);
     Check(j.DrunkRide.Advance(4,.9f),"existing vomiting event "+label);
     break;
    case "rough-day":
     Check(session.manners.Deduct("collision",3,"fixture recorded a driving contact",0),"existing safety observation "+label);
     break;
    default:j.trip.Tick(j.trip.Remaining+1);break; // The normal deadline determines the late ending.
   }
   Pose(j.current.drop);
   Check(j.Interact(),"location-controlled alighting entry "+label);
   if(j.Active){
    requests.AdvanceSafeStop(3);
    if(j.DrunkRide.Waiting)j.DrunkRide.Advance(16,1);
    if(!j.Behavior.UnloadReady)j.AdvanceBehavior(j.Behavior.WorkRemaining+1);
    if(j.Active)j.Interact();
   }
   var result=b.passengers.LastOrDefault();
   int expected=(int)(good?PassengerStoryEnding.Good:PassengerStoryEnding.Disappointing);
   Check(!j.Active&&result!=null&&result.day==day&&result.storyId==story.id&&result.storyEnding==expected
    &&b.Logbook.HasEnding(story.id,expected),"terminal receipt and journal after actual alighting "+label);
   if(day==PassengerStories.All.Count*2)continue;
   b.ledger.RequestClose("story fixture day boundary");
   Check(b.ledger.Settle()&&(b.ledger.State==BusinessState.Completed?b.BeginContinuousCareer():b.BeginNextCampaignDay()),"prepare separate day for "+label);
   yield return null;yield return null;
   b.ContinueFromMenu();
  }
  Debug.Log("PASSENGER_STORY_MATRIX_OK checks="+checks+" location_rides="+b.passengers.Count);
 }
 IEnumerator Run(){
  Check(ContentTuningRuntime.TestMode,"using isolated content test mode");
  Directory.CreateDirectory(Folder);
  yield return null;
  var bootstrap=FindObjectOfType<UnifiedModeBootstrap>();if(bootstrap)bootstrap.StartSingle();
  while(!(session=FindObjectOfType<DriveSession>())||!session.StartupReady||session.business.IsRestoring)yield return null;
  yield return null;
  var b=session.business;var j=session.jobs;var requests=session.requests;
  Check(b.PassengerStoriesEnabled&&!b.ResumeBlocked&&b.SavePath==SavePath,"Eungye story mode and isolated campaign (story="+b.PassengerStoriesEnabled+", resumeBlocked="+b.ResumeBlocked+", fixturePath="+(b.SavePath==SavePath)+")");
  b.enabled=false;j.enabled=false;j.nav.enabled=false;session.law.enabled=false;session.manners.enabled=false;
  session.taxi.enabled=false;session.taxi.body.isKinematic=true;session.taxi.body.interpolation=RigidbodyInterpolation.None;
  foreach(var car in session.traffic.cars){car.Recycle();car.enabled=false;}
  var occupancy=session.GetComponent<StopOccupancySystem>();if(occupancy)occupancy.enabled=false;
  var panel=session.GetComponent<RideCompletionPanel>();
  b.ContinueFromMenu();
  if(Matrix){
   var runs=RunLocationMatrix();while(runs.MoveNext())yield return runs.Current;
   Application.Quit(0);yield break;
  }
  if(Reload){
   Check(j.Active&&j.trip.State==TripState.OnTrip&&j.CurrentStory?.id=="last-train"&&j.Behavior.PresetId=="hurried","current story and established preset restore");
   var answer=requests.ResultFor(RequestKind.FareHelp);
   Check(answer!=null&&answer.accepted&&answer.reply==PassengerReply.StoryFareAssist,"student choice restores without rerolling");
   Check(b.Logbook.HasStoryAssignment(b.ledger.Day,"last-train")&&b.Logbook.HasPreset("hurried"),"assignment and first passenger sight restore");
   int fare=j.trip.Fare;Pose(j.current.drop);
   Check(j.Interact()&&!j.Active&&b.passengers.Count>0,"location-controlled destination settlement");
   var ride=b.passengers[b.passengers.Count-1];
   Check(ride.storyId=="last-train"&&ride.storyEnding==(int)PassengerStoryEnding.Good&&ride.storyLine.Length>0&&ride.quoted==fare,"real terminal outcome keeps ordinary fare agreement");
   Check(b.Logbook.HasEnding("last-train",(int)PassengerStoryEnding.Good),"receipt ending enters collecting book");
   if(!panel.Open)panel.ShowLast();
   Check(panel.Open&&ReferenceEquals(panel.Result,ride),"existing receipt presents the saved story delivery");
   yield return new WaitForEndOfFrame();
   ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Folder,"student-receipt.png")));
   yield return new WaitForSecondsRealtime(.5f);
   Debug.Log("PASSENGER_STORY_RELOAD_OK checks="+checks);Application.Quit(0);yield break;
  }
  CheckTerminalMatrix();
  Check(j.StoryAssignmentLimit>=2&&j.StoryAssignmentLimit<=3,"seeded day cap is within authored targets");
  var offer=FixtureOffer("last-train");
  Pose(offer.pickup);j.offers[0]=offer;
  Check(j.Accept(0),"accept a real call at a safe pickup");
  Check(j.Interact()&&j.trip.State==TripState.OnTrip&&j.CurrentStory?.id=="last-train","board through the story's established passenger module");
  yield return null;
  Check(j.RespondBehavior(0),"acknowledge student's ordinary ETA behavior");
  for(int i=0;i<8&&!requests.HasPendingChoice;i++)yield return null;
  Check(requests.HasPendingChoice&&requests.request.Kind==RequestKind.FareHelp&&requests.ScheduledChoiceCount==2,"story schedules fare help followed by ETA");
  Check(requests.ReplyChoices.Any(c=>c.reply==PassengerReply.StoryFareAssist),"student-specific option is available beside the generic options");
  Check(!PassengerRequestChoices.For(RequestKind.FareHelp).Any(c=>c.reply==PassengerReply.StoryFareAssist),"ordinary fare help is unchanged");
  yield return null;
  Check(requests.Choose(PassengerReply.StoryFareAssist)&&j.trip.Fare==offer.fare&&b.Logbook.StoryAssignments(b.ledger.Day)==1,"answer uses the existing request and preserves agreement");
  Check(b.SaveCheckpoint()&&File.Exists(SavePath),"save occupied story for second process");
  var checkpoint=BusinessProgress.ParseCheckpoint(File.ReadAllText(SavePath));
  Check(checkpoint.live!=null&&checkpoint.live.jobs.current.storyId=="last-train","live checkpoint contains the accepted story");
  Debug.Log("PASSENGER_STORY_PLAYER_OK checks="+checks);Application.Quit(0);
 }
}
