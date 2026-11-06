using System;
using UnityEngine;

// One host-owned ledger for a passenger's choices. Business time also accounts for
// explicit service waits; a rendered frame or a replicated snapshot is not an effect.
public partial class PassengerJobs {
 ChoiceEffectLedger choiceEffects;
 double choiceEffectsElapsed;
 string choiceEffectsTrip="",choiceEffectsOwner="";
 bool choiceEffectsLegacyTransport;
 public PassengerContent PassengerContent {get{return Behavior==null?global::PassengerContent.Shared:Behavior.Content;}}
 public ChoiceEffectLedger ChoiceEffects {get{return choiceEffects;}}
 public bool ChoiceEffectsActive {get{return ChoiceEffectDefinition.Enabled&&choiceEffects!=null&&!choiceEffects.cancelled;}}
 double ChoiceSatisfaction {get{return Math.Max(0,Math.Min(100,StartSatisfaction+session.manners.ledger.Score-100+(session.requests?session.requests.TotalSatisfactionDelta:0)+ComfortRecovered+(choiceEffects==null?0:choiceEffects.satisfactionContribution)));}}
 double ChoiceFatigue {get{return session&&session.business&&session.business.ledger!=null&&session.business.ledger.Driver!=null?session.business.ledger.Driver.fatigue:0;}}
 string ChoiceTrip {get{return session&&session.law?session.law.tripId??"":"";}}
 string ChoiceOwner {get{return current==null?"":current.candidateSeed??OfferId??"";}}
 double ChoiceNow {get{return session&&session.business&&session.business.ledger!=null?session.business.ledger.Elapsed:0;}}
 bool ChoiceAuthority {get{return session&&!PvpNativeLan.IsReplica(session)&&session.business&&session.business.useV2;}}
 void ApplyChoiceDelta(ChoiceEffectDelta delta){
  var driver=session&&session.business&&session.business.ledger!=null?session.business.ledger.Driver:null;
  if(driver!=null){driver.fatigue=Math.Max(0,Math.Min(100,driver.fatigue+delta.fatigue));if(driver.fatigue<100&&driver.drowsiness!=null)driver.drowsiness.Recover();}
  RefreshChoiceFatigueModifier();
 }
 void RefreshChoiceFatigueModifier(){if(session&&session.business&&session.business.ledger!=null&&session.business.ledger.Driver!=null)session.business.ledger.Driver.choiceFatigueGrowthPercent=ChoiceEffectsActive?choiceEffects.FatigueGrowthPercent:0;}
 bool PrepareChoiceEffects(){
  if(!ChoiceAuthority||!ChoiceEffectDefinition.Enabled||choiceEffectsLegacyTransport)return false;
  if(trip.State==TripState.ToPickup){
   if(choiceEffects==null||choiceEffects.cancelled||choiceEffectsOwner!=ChoiceOwner){choiceEffects=new ChoiceEffectLedger();choiceEffectsOwner=ChoiceOwner;choiceEffectsTrip="";choiceEffectsElapsed=ChoiceNow;}
  }else if(trip.State!=TripState.OnTrip||trip.ArrivalLocked||!ChoiceEffectsActive)return false;
  return ChoiceEffectsActive;
 }
 public void BeginChoiceEffectsForTrip(){
  if(!ChoiceAuthority)return;
  if(choiceEffectsLegacyTransport){choiceEffects=null;RefreshChoiceFatigueModifier();return;}
  if(!ChoiceEffectDefinition.Enabled){choiceEffects=null;RefreshChoiceFatigueModifier();return;}
  if(ChoiceEffectsActive&&choiceEffectsTrip==ChoiceTrip&&choiceEffectsOwner==ChoiceOwner)return;
  if(choiceEffects==null||choiceEffects.cancelled||choiceEffectsOwner!=ChoiceOwner)choiceEffects=new ChoiceEffectLedger();
  string pickupPrefix="pickup:"+ChoiceOwner;
  foreach(var entry in choiceEffects.entries)if(!entry.active&&!entry.completed&&entry.eventId.StartsWith(pickupPrefix+":",StringComparison.Ordinal))entry.eventId=ChoiceTrip+entry.eventId.Substring(pickupPrefix.Length);
  choiceEffectsTrip=ChoiceTrip;choiceEffectsOwner=ChoiceOwner;choiceEffectsElapsed=ChoiceNow;
  ApplyChoiceDelta(choiceEffects.ActivatePending(ChoiceSatisfaction,ChoiceFatigue));
 }
 public void SelectChoiceEffect(string eventId,string choiceId,bool activate=true){
  if(!PrepareChoiceEffects())return;TickChoiceEffects();
  ApplyChoiceDelta(choiceEffects.Select(eventId,ChoiceEffectDefinition.Read(choiceId,PassengerContent),ChoiceSatisfaction,ChoiceFatigue,activate&&trip.State==TripState.OnTrip));
 }
 public void RecordRequestChoice(RequestKind kind,PassengerReply reply){if(kind==RequestKind.LostItem)return;SelectChoiceEffect(ChoiceTrip+":request:"+kind,"request."+kind+"."+reply);}
 void RecordBehaviorChoice(int index,bool followup){
  if(Behavior==null||Behavior.Type==PassengerType.General)return;
  string type=followup?"SleepingFollowup":Behavior.Type.ToString();
  string prefix=trip.State==TripState.ToPickup?"pickup:"+ChoiceOwner:ChoiceTrip;
  SelectChoiceEffect(prefix+":type:"+Behavior.Type+(followup?":followup":":initial"),"type."+type+"."+index,trip.State==TripState.OnTrip);
 }
 void RecordSituationChoice(PassengerSituationKind kind,int reaction,int index){SelectChoiceEffect(ChoiceTrip+":situation:"+reaction,"situation."+kind+"."+index);}
 public void TickChoiceEffects(){
  if(!ChoiceAuthority||!ChoiceEffectsActive)return;
  double now=ChoiceNow;
  if(session.paused||Time.timeScale==0||trip.State!=TripState.OnTrip||trip.ArrivalLocked){choiceEffectsElapsed=now;RefreshChoiceFatigueModifier();return;}
  double seconds=now-choiceEffectsElapsed;if(seconds< -0.01)throw new ArgumentException("Choice clock moved backwards");
  choiceEffectsElapsed=now;if(seconds>0)ApplyChoiceDelta(choiceEffects.Advance(seconds,ChoiceSatisfaction,ChoiceFatigue));
 }
 public int CompleteChoiceRequest(RequestKind kind,PassengerReply reply,int outcome){
  if(!ChoiceAuthority||!ChoiceEffectsActive)return 0;
  string id=ChoiceTrip+":request:"+kind;if(!choiceEffects.HasEvent(id))return 0;
  ApplyChoiceDelta(choiceEffects.Complete(id,outcome,ChoiceSatisfaction,ChoiceFatigue));return choiceEffects.RequestEndFor(id);
 }
 public void CompleteOtherChoiceEffects(){if(ChoiceAuthority&&ChoiceEffectsActive)ApplyChoiceDelta(choiceEffects.CompleteNonRequests(ChoiceSatisfaction,ChoiceFatigue));}
 public void CancelChoiceEffects(){choiceEffectsLegacyTransport=false;if(choiceEffects!=null)choiceEffects.Cancel();RefreshChoiceFatigueModifier();}
 void ResetChoiceEffectsForCall(){choiceEffects=null;choiceEffectsElapsed=0;choiceEffectsTrip=choiceEffectsOwner="";choiceEffectsLegacyTransport=false;RefreshChoiceFatigueModifier();}
 public int ChoiceMotionPenalty {get{return ChoiceEffectsActive?Math.Max(0,MannerLedger.MotionPenalty(false)+choiceEffects.MotionPenaltyDelta):MannerLedger.MotionPenalty(session&&session.manners&&session.manners.ComfortAccepted);}}
 public void CaptureChoiceEffects(PassengerJobsSnapshot snapshot){
  snapshot.choiceEffectsVersion=1;snapshot.choiceEffectsLegacyTransport=choiceEffectsLegacyTransport;snapshot.choiceEffectsPresent=choiceEffects!=null;snapshot.choiceEffects=choiceEffects==null?null:choiceEffects.Copy();
  snapshot.choiceEffectsElapsed=choiceEffects==null?0:choiceEffectsElapsed;snapshot.choiceEffectsTrip=choiceEffects==null?"":choiceEffectsTrip;snapshot.choiceEffectsOwner=choiceEffects==null?"":choiceEffectsOwner;
 }
 public static void ValidateChoiceEffectsSnapshot(PassengerJobsSnapshot snapshot){
  if(snapshot==null)throw new ArgumentException("Missing passenger snapshot");
  if(snapshot.choiceEffectsVersion==0){if(snapshot.choiceEffectsLegacyTransport||snapshot.choiceEffectsPresent||snapshot.choiceEffects!=null)throw new ArgumentException("Legacy snapshot has new effects");return;}
  // JsonUtility materializes null nested classes as empty objects. The explicit
  // presence flag owns absence; do not accept hidden applied effects behind it.
  if(snapshot.choiceEffectsVersion==1&&!snapshot.choiceEffectsPresent&&snapshot.choiceEffects!=null){var empty=snapshot.choiceEffects;if(empty.cancelled||empty.satisfactionContribution!=0||empty.requestEndContribution!=0||empty.entries!=null&&empty.entries.Count!=0)throw new ArgumentException("Hidden choice effects behind absence flag");snapshot.choiceEffects=null;}
  if(snapshot.choiceEffectsVersion!=1||snapshot.choiceEffectsPresent!=(snapshot.choiceEffects!=null)||double.IsNaN(snapshot.choiceEffectsElapsed)||double.IsInfinity(snapshot.choiceEffectsElapsed)||snapshot.choiceEffectsElapsed<0)throw new ArgumentException("Invalid choice effect snapshot");
  if(snapshot.choiceEffectsLegacyTransport&&(snapshot.choiceEffects!=null||snapshot.current==null||snapshot.trip==null||(snapshot.trip.state!=TripState.ToPickup&&snapshot.trip.state!=TripState.OnTrip)))throw new ArgumentException("Invalid legacy transport marker");
  if(snapshot.choiceEffects==null){if(snapshot.choiceEffectsElapsed!=0||!string.IsNullOrEmpty(snapshot.choiceEffectsTrip)||!string.IsNullOrEmpty(snapshot.choiceEffectsOwner))throw new ArgumentException("Missing choice ledger");return;}
  if(!ChoiceEffectDefinition.Enabled)throw new ArgumentException("Choice ledger requires its session profile");snapshot.choiceEffects.Validate(snapshot.behavior==null?null:snapshot.behavior.Content);
  if(snapshot.current!=null&&snapshot.choiceEffectsOwner!=(snapshot.current.candidateSeed??snapshot.offerId??""))throw new ArgumentException("Choice owner mismatch");
  if(snapshot.trip!=null&&snapshot.trip.state==TripState.ToPickup){if(!string.IsNullOrEmpty(snapshot.choiceEffectsTrip))throw new ArgumentException("Pickup choice already has trip");foreach(var entry in snapshot.choiceEffects.entries)if(entry.active||entry.completed)throw new ArgumentException("Pickup effects already applied");}
  string prefix=string.IsNullOrEmpty(snapshot.choiceEffectsTrip)?"pickup:"+snapshot.choiceEffectsOwner:snapshot.choiceEffectsTrip;
  if(prefix.Length>256)throw new ArgumentException("Choice identity too long");foreach(var entry in snapshot.choiceEffects.entries){
   if(!entry.eventId.StartsWith(prefix+":",StringComparison.Ordinal))throw new ArgumentException("Choice event belongs to another trip");
   string[] choice=entry.definition.choiceId.Split('.');string suffix=entry.eventId.Substring(prefix.Length);int index;
   if(choice.Length!=3)throw new ArgumentException("Invalid choice identity");
   if(choice[0]=="request"){
    if(suffix!=":request:"+choice[1])throw new ArgumentException("Request choice event mismatch");
    if(snapshot.request!=null&&(snapshot.request.version==2||snapshot.request.request!=null&&!string.IsNullOrEmpty(snapshot.request.request.trip))){PassengerRequestSnapshot response=null;bool observed=false;if(snapshot.request.version==2&&snapshot.request.observations!=null){observed=true;foreach(var observation in snapshot.request.observations)if(observation!=null&&observation.request!=null){observed=true;if(observation.request.kind.ToString()==choice[1])response=observation.request;}}else if(snapshot.request.request!=null){observed=true;if(snapshot.request.request.kind.ToString()==choice[1])response=snapshot.request.request;}
     if(observed&&(response==null||!response.replyCaptured||response.reply.ToString()!=choice[2]))throw new ArgumentException("Choice does not match saved request reply");
    }
   }else if(choice[0]=="type"){
    bool followup=choice[1]=="SleepingFollowup";string type=followup?"Sleeping":choice[1];if(suffix!=":type:"+type+(followup?":followup":":initial"))throw new ArgumentException("Type choice event mismatch");
    if(snapshot.behaviorVersion==1&&snapshot.behaviorPresent&&snapshot.behavior!=null&&snapshot.behavior.Type.ToString()!=type)throw new ArgumentException("Choice does not match passenger type");
   }else if(choice[0]=="situation"){
    string number=suffix.StartsWith(":situation:",StringComparison.Ordinal)?suffix.Substring(11):"";
    if(!int.TryParse(number,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out index)||index<0||index>2||number!=index.ToString(System.Globalization.CultureInfo.InvariantCulture))throw new ArgumentException("Situation choice event mismatch");
   }else throw new ArgumentException("Unsupported choice event group");
  }
 }
 public void RestoreChoiceEffects(PassengerJobsSnapshot snapshot){
  ValidateChoiceEffectsSnapshot(snapshot);
  choiceEffectsLegacyTransport=(snapshot.choiceEffectsVersion==0||snapshot.choiceEffectsLegacyTransport)&&snapshot.current!=null&&snapshot.trip!=null&&(snapshot.trip.state==TripState.ToPickup||snapshot.trip.state==TripState.OnTrip);
  choiceEffects=snapshot.choiceEffectsVersion==1&&snapshot.choiceEffectsPresent?snapshot.choiceEffects.Copy():null;
  choiceEffectsElapsed=choiceEffects==null?0:snapshot.choiceEffectsElapsed;choiceEffectsTrip=choiceEffects==null?"":snapshot.choiceEffectsTrip??"";choiceEffectsOwner=choiceEffects==null?"":snapshot.choiceEffectsOwner??"";
  if(choiceEffects!=null&&choiceEffectsElapsed>ChoiceNow+.01)throw new ArgumentException("Choice clock is ahead of business time");RefreshChoiceFatigueModifier();
 }
}
