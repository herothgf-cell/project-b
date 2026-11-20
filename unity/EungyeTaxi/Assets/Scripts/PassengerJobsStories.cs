using System;
using System.Collections.Generic;

public partial class PassengerJobs {
 int lastReunionKind;
 public int StudentMinute {get{return (int)Math.Min(1200,60*DayLightController.HourForElapsed(session.business.ledger.Elapsed,session.business.ledger.Settings.daySeconds));}}
 int AvailableReunion {get{return session.business&&session.business.PassengerStoriesEnabled?session.business.Logbook.ReunionKind(session.business.ledger.Day,StudentMinute):0;}}
 JobOffer ReunionOffer(JobOffer[] normal,int kind){
  var book=session.business.Logbook;
  int p=runtimeStopIds.IndexOf(book.studentStop);
  JobOffer offer=null;
  if(p>=0&&PickupAvailable(p,PickupDistance(p))){
   foreach(var other in normal){
    if(other.drop==p||other.drop==Catalog.OppositeIndex(p))continue;
    offer=OrdinaryOffer(p,other.drop,PickupDistance(p));if(offer!=null)break;
   }
  }
  if(offer==null&&kind==2&&normal.Length>0)offer=normal[0];
  if(offer!=null)offer.reunionKind=kind;
  return offer;
 }
 public PassengerStoryDefinition CurrentStory {get{
  return session&&session.business&&session.business.PassengerStoriesEnabled&&current!=null
   ?PassengerStories.Find(current.storyId):null;
 }}
 public int StoryAssignmentLimit {get{
  int minimum=PassengerStories.DailyMinimum,maximum=PassengerStories.DailyMaximum;
  return minimum+(int)(DrunkRideState.Hash(WaitingSeed+":story-count")%(uint)(maximum-minimum+1));
 }}
 PassengerBehaviorState PrepareStoryPassenger(VehicleCare care){
  var business=session.business;
  if(!business.PassengerStoriesEnabled||ForcedPassengerType.HasValue)return null;
  var book=business.Logbook;int day=business.ledger.Day;
  var reserved=CurrentStory;
  bool evening=StudentMinute>=17*60;
  int dailyLimit=StoryAssignmentLimit;
  int assignments=book.StoryAssignments(day);
  if(assignments>=dailyLimit){
   ReleaseStoryReservation();current.storyId=null;return null;
  }
  var eligible=session.requests.EligibleRequests();
  string contents=ContentTuning.Text("passenger.presets","");
  if(contents.Length==0){ReleaseStoryReservation();current.storyId=null;return null;}
  var presets=PassengerPresetCatalog.Parse(contents);
  var candidates=new List<PassengerStoryDefinition>();
  var weights=new List<double>();
  double total=0;
  foreach(var story in PassengerStories.All){
   if(reserved!=null&&reserved.id!=story.id||book.HasStoryAssignment(day,story.id)||Array.IndexOf(eligible,story.request)<0)continue;
   if(story.id=="last-train"&&reserved==null&&(book.studentDay!=0||!evening))continue;
   var preset=FindStoryPreset(story,presets);
   if(preset==null)continue;
   var pool=(PassengerType)Enum.Parse(typeof(PassengerType),preset.pool);
   if(!care.CanReserveStoryPassenger(current.candidateSeed,pool))continue;
   if(reserved==null&&book.studentDay==0&&story.id!="last-train"&&assignments>=dailyLimit-1)continue;
   double weight=story.Weight(business.DayCondition);
   if(weight<=0)continue;
   if(story.id=="last-train"&&book.studentDay==0&&evening){candidates.Clear();weights.Clear();candidates.Add(story);weights.Add(weight);total=weight;break;}
   candidates.Add(story);weights.Add(weight);total+=weight;
  }
  if(candidates.Count==0){ReleaseStoryReservation();current.storyId=null;return null;}
  double pick=DrunkRideState.Hash(WaitingSeed+":"+current.candidateSeed+":story-choice")/(uint.MaxValue+1d)*total;
  int selected=0;while(selected+1<candidates.Count&&pick>=weights[selected])pick-=weights[selected++];
  var chosen=candidates[selected];
  var chosenPreset=FindStoryPreset(chosen,presets);
  if(!care.ReserveStoryPassenger(current.candidateSeed,(PassengerType)Enum.Parse(typeof(PassengerType),chosenPreset.pool)))return null;
  current.storyId=chosen.id;
  return PassengerBehaviorState.CreatePreset(current.candidateSeed,chosenPreset,chosen.intro);
 }
 PassengerPresetDefinition FindStoryPreset(PassengerStoryDefinition story,PassengerPresetDefinition[] presets){
  var preset=Array.Find(presets,p=>p.id==story.presetId&&p.enabled&&p.weight>0);
  if(preset==null)return null;
  if(!session.business.ActiveVehicle.carriesLuggage&&(preset.ActionModule=="Luggage"||preset.ActionModule=="MuddyLuggage"))return null;
  foreach(var criterion in story.criteria){
   if(criterion==PassengerStoryCriterion.Luggage&&preset.ActionModule!="Luggage"
    ||criterion==PassengerStoryCriterion.Protection&&preset.ActionModule!="MuddyLuggage")return null;
  }
  return preset;
 }
 void ReleaseStoryReservation(){
  if(!string.IsNullOrEmpty(current?.storyId)&&session&&session.business&&session.business.ledger!=null)
   session.business.ledger.Care.ReleaseStoryPassenger(current.candidateSeed);
 }
 void RecordStoryPassenger(){
  var business=session.business;
  if(!business.PassengerStoriesEnabled||Behavior==null)return;
  if(!string.IsNullOrEmpty(Behavior.PresetId))business.Logbook.RecordPreset(Behavior.PresetId);
  var story=CurrentStory;
  if(story==null)return;
  business.Logbook.RecordStoryAssignment(business.ledger.Day,story.id);
  if(DrunkRide.IsDrunk)DrunkRide.Dialogue="손님  “"+story.intro+"”";
 }
 public static void ValidateStoryPassenger(PassengerJobsSnapshot snapshot){
  if(string.IsNullOrEmpty(snapshot?.current?.storyId))return;
  var story=PassengerStories.Find(snapshot.current.storyId);
  if(story==null)throw new ArgumentException("Invalid saved passenger story");
  if(snapshot.trip!=null&&(snapshot.trip.state==TripState.OnTrip||snapshot.behaviorPresent)
   &&(snapshot.behavior==null||snapshot.behavior.PresetId!=story.presetId))throw new ArgumentException("Saved story has the wrong passenger preset");
 }
 bool StorySafe {get{
  return session.requests.LawClear&&session.manners.collisionEvents==boardingCollisionEvents&&session.manners.redEvents==boardingRedEvents
   &&(DrunkRide==null||!DrunkRide.Vomited);
 }}
 internal struct StoryRideFacts {
  public bool lateOrCancelled,arrivedOnTime,safe,jolt;
  public int behaviorChoice;
  public Func<RequestKind,PassengerRequestSnapshot> request;
 }
 static PassengerStoryEnding RequestEnding(StoryRideFacts facts,RequestKind kind){
  var outcome=facts.request(kind);
  if(outcome==null||!outcome.goalRecorded)return PassengerStoryEnding.Ordinary;
  return outcome.outcome>0?PassengerStoryEnding.Good:outcome.outcome<0?PassengerStoryEnding.Disappointing:PassengerStoryEnding.Ordinary;
 }
 static PassengerStoryEnding EvaluateStoryCriterion(PassengerStoryDefinition story,StoryRideFacts facts,PassengerStoryCriterion criterion){
  switch(criterion){
   case PassengerStoryCriterion.Comfort:return RequestEnding(facts,RequestKind.Comfort);
   case PassengerStoryCriterion.OnTime:
    if(!facts.arrivedOnTime||!facts.safe)return PassengerStoryEnding.Disappointing;
    return story.request==RequestKind.OnTime?RequestEnding(facts,RequestKind.OnTime):PassengerStoryEnding.Good;
   case PassengerStoryCriterion.Quiet:
    return facts.jolt?PassengerStoryEnding.Disappointing:RequestEnding(facts,RequestKind.Quiet);
   case PassengerStoryCriterion.LargeRoad:return RequestEnding(facts,RequestKind.LargeRoad);
   case PassengerStoryCriterion.SafeStop:return RequestEnding(facts,RequestKind.SafeStop);
   case PassengerStoryCriterion.ViaStop:return RequestEnding(facts,RequestKind.ViaStop);
   case PassengerStoryCriterion.Empathy:return RequestEnding(facts,RequestKind.Empathy);
   case PassengerStoryCriterion.Luggage:case PassengerStoryCriterion.Protection:
    return facts.behaviorChoice<0?PassengerStoryEnding.Ordinary:facts.behaviorChoice==0?PassengerStoryEnding.Good:PassengerStoryEnding.Disappointing;
   case PassengerStoryCriterion.Conversation:
    if(facts.behaviorChoice==0)return PassengerStoryEnding.Good;
    return RequestEnding(facts,RequestKind.LocalChat);
   case PassengerStoryCriterion.Safety:return facts.safe?PassengerStoryEnding.Good:PassengerStoryEnding.Disappointing;
   case PassengerStoryCriterion.FareHelp:
    var fare=facts.request(RequestKind.FareHelp);
    return fare!=null&&fare.accepted&&fare.replyCaptured&&fare.reply==PassengerReply.StoryFareAssist?PassengerStoryEnding.Good:PassengerStoryEnding.Ordinary;
   default:return PassengerStoryEnding.Ordinary;
  }
 }
 internal static PassengerStoryEnding EvaluateStoryEnding(PassengerStoryDefinition story,StoryRideFacts facts){
  if(story==null)return PassengerStoryEnding.Ordinary;
  if(facts.lateOrCancelled)return PassengerStoryEnding.Disappointing;
  return story.EndingFor(criterion=>EvaluateStoryCriterion(story,facts,criterion));
 }
 public PassengerStoryEnding StoryEnding {get{
  var story=CurrentStory;
  if(story==null)return PassengerStoryEnding.Ordinary;
  return EvaluateStoryEnding(story,new StoryRideFacts{
   lateOrCancelled=trip.State==TripState.Failed||Behavior!=null&&Behavior.Cancelled,
   arrivedOnTime=trip.ArrivedOnTime,safe=StorySafe,jolt=Behavior!=null&&Behavior.JoltWoken,
   behaviorChoice=Behavior==null?-1:Behavior.Choice,request=session.requests.ResultFor
  });
 }}
 public string StoryEndingLine {get{return CurrentStory?.Ending(StoryEnding)??"";}}
}
