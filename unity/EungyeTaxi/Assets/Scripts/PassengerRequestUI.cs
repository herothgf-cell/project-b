using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class PassengerRequestUISnapshot : PassengerRequestObservationSnapshot {
 public int version,active=-1,boardingLawCount;public float initialDistance;public bool finished,lostItem;
 public float smallTalkCooldown,smallTalkVisible;public string smallTalkReply;public string[] smallTalkSeen;
 public PassengerRequestScheduleSnapshot schedule;
 public PassengerRequestObservationSnapshot[] observations;
}
public class PassengerRequestUI : MonoBehaviour {
 public DriveSession session;
 readonly List<PassengerRequestObservation> observations=new List<PassengerRequestObservation>();
 readonly PassengerRequest empty=new PassengerRequest();
 float smallTalkCooldown,smallTalkVisible;string smallTalkReply="";
 readonly PassengerChatHistory chatHistory=new PassengerChatHistory();
 [Serializable] sealed class ChatLibrary {public string[] lines;}
 static string[] extraChatLines;
 static string[] ExtraChatLines {get{if(extraChatLines==null){var asset=Resources.Load<TextAsset>("Content/passenger-chat-lines");extraChatLines=asset?JsonUtility.FromJson<ChatLibrary>(asset.text).lines:new string[0];}return extraChatLines;}}
 int responseBlockedFrame=-1;
 public bool ResponseBlockedThisFrame {get{return responseBlockedFrame==Time.frameCount;}}
 public void BlockResponseThisFrame(){responseBlockedFrame=Time.frameCount;}
 PassengerRequestSchedule schedule;int active=-1,boardingLawCount;float initialDistance,stopSeconds;bool finished,lostItem,legacy;
 PassengerRequestObservation Current {get{return active>=0&&active<observations.Count?observations[active]:null;}}
 public PassengerRequest request {get{return Current?Current.request:empty;}}
 public bool SuppressFollowupDialogue {get{return observations.Exists(o=>o&&o.SuppressFollowupDialogue);}}
 double RequestEffectValue(PassengerRequestObservation observation){var ledger=session&&session.jobs&&session.jobs.ChoiceEffectsActive?session.jobs.ChoiceEffects:null;string id=observation.request.Trip+":request:"+observation.request.Kind;return ledger!=null&&ledger.HasEvent(id)?ledger.RawRequestEndFor(id):observation.request.SatisfactionDelta;}
 public int TotalSatisfactionDelta {get{
  double value=0;
  foreach(var o in observations)if(o)value+=RequestEffectValue(o);
  int cap=ContentTuning.Int("common.requestCap",10);return (int)Math.Max(-cap,Math.Min(cap,Math.Round(value,MidpointRounding.AwayFromZero)));
 }}
 public string Summary {get{var text=new List<string>();for(int i=0;i<observations.Count;i++)if(observations[i])text.Add((i+1)+". "+observations[i].request.ResponseText()+" ["+RequestEffectValue(observations[i]).ToString("+0.###;-0.###;0",System.Globalization.CultureInfo.InvariantCulture)+"]");if(schedule!=null)foreach(var slot in schedule.Capture().slots)if(!slot.presented)text.Add(slot.kind+" · 미발생 중립");if(text.Count==0)text.Add("요청 없음");if(lostItem&&finished)text.Add("하차 후 사건 "+session.law.tripId+":lost-item · 지갑 발견 → 보관·신고 절차 안내 (텍스트 사건)");return string.Join("\n",text.ToArray())+"\n요청 합계 "+TotalSatisfactionDelta.ToString("+0;-0;0");}}
 public bool LawClear {get{return LawLedger.ViolationCount(session.law.ledger.Events,boardingLawCount)==0;}}
 public void ExemptSystemRoute(string reason){foreach(var o in observations)if(o)o.SystemRouteReason=reason;}
 void Clear(){foreach(var o in observations)if(o){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}observations.Clear();active=-1;schedule=null;stopSeconds=0;finished=lostItem=legacy=false;smallTalkCooldown=smallTalkVisible=0;smallTalkReply="";chatHistory.Restore(null,null);empty.Restore(new PassengerRequestSnapshot{resolved=true,response="요청 없음"});}
 PassengerRequestObservation NewObservation(){var o=gameObject.AddComponent<PassengerRequestObservation>();o.enabled=false;o.session=session;o.request.Content=session.jobs.PassengerContent;return o;}
 public static RequestKind? Representative(PassengerType type,int variant){switch(type){case PassengerType.Comfort:return RequestKind.Comfort;case PassengerType.Hurried:return variant==0?RequestKind.Eta:RequestKind.OnTime;case PassengerType.RouteChecking:return variant==0?RequestKind.LargeRoad:variant==1?RequestKind.ShortRoad:RequestKind.RouteAdvice;case PassengerType.Errand:return RequestKind.ViaStop;case PassengerType.PrecisionStop:return RequestKind.SafeStop;default:return null;}}
 public RequestKind[] EligibleRequests(){
  var eligible=new List<RequestKind>((RequestKind[])Enum.GetValues(typeof(RequestKind)));eligible.Remove(RequestKind.LostItem);
  if(!(session.jobs.trip.State==TripState.ToPickup&&session.jobs.current!=null?session.jobs.nav.HasLargeRoadAlternativeTo(session.jobs.stops[session.jobs.current.drop]):session.jobs.nav.HasLargeRoadAlternative()))eligible.Remove(RequestKind.LargeRoad);
  if(session.jobs.nav.hd==null){eligible.Remove(RequestKind.Eta);eligible.RemoveAll(PassengerRequestSchedule.Route);}
  var zones=session.GetComponent<ServiceLawZones>();if(zones&&zones.Live&&zones.Live.Active||!HasSafeAlternative()){eligible.Remove(RequestKind.SafeStop);eligible.Remove(RequestKind.DestinationChange);eligible.Remove(RequestKind.ViaStop);}
  eligible.RemoveAll(k=>!ContentTuning.Flag("request."+k+".enabled",true)||ContentTuning.Int("request."+k+".weight",1)<=0);return eligible.ToArray();
 }
 bool HasSafeAlternative(){var j=session.jobs;if(j.current==null||j.nav.hd==null)return false;var from=j.stops[j.current.pickup];var to=j.stops[j.current.drop];for(int i=0;i<j.stops.Length;i++){if(i==j.current.pickup||i==j.current.drop||j.StopOccupied(i))continue;var stop=j.stops[i];if(j.nav.hd.Route(from.laneEdge,from.laneAt,stop.laneEdge,stop.laneAt)!=null&&j.nav.hd.Route(stop.laneEdge,stop.laneAt,to.laneEdge,to.laneAt)!=null)return true;}return false;}
 public PassengerType[] EligiblePassengerModules(){var kinds=EligibleRequests();int count=PassengerRequestSchedule.Create(session.jobs.current.candidateSeed,kinds).Capture().slots.Length;var result=new List<PassengerType>();bool trunk=session.business==null||session.business.ActiveVehicle.carriesLuggage;foreach(PassengerType type in Enum.GetValues(typeof(PassengerType))){if(type==PassengerType.General||type==PassengerType.Drunk)continue;if(!trunk&&(type==PassengerType.Luggage||type==PassengerType.MuddyLuggage))continue;var representative=Representative(type,PassengerBehaviorState.Create(session.jobs.current.candidateSeed,type).Variant);if(!representative.HasValue||count>0&&Array.IndexOf(kinds,representative.Value)>=0)result.Add(type);}return result.ToArray();}
 public PassengerType[] EligiblePassengerTypes(){var modules=EligiblePassengerModules();if(ContentTuning.Text("passenger.presets","").Length==0)return modules;var pools=new List<PassengerType>();string seed=session.jobs.current.candidateSeed;string selectionSeed=session.business.ledger.Care.PassengerSelectionSeed;foreach(PassengerType pool in Enum.GetValues(typeof(PassengerType))){if(pool==PassengerType.General||pool==PassengerType.Drunk)continue;var selected=PassengerBehaviorState.CreateConfigured(seed,pool,modules,selectionSeed);if(selected.Pool==pool)pools.Add(pool);}return pools.ToArray();}
 public int ScheduledChoiceCount {get{return schedule==null?observations.Count:schedule.Capture().slots.Length;}}
 public string CardReply {get{return Current?Current.CardReply:"";}}
 public bool HasPendingChoice {get{return Current&&!Current.request.Resolved;}}
 public bool HasVisiblePanel {get{return Current&&Current.PanelVisible&&PresentationAvailable;}}
 public void SyncBehaviorContext(){var b=session.jobs.Behavior;if(b==null)return;var kind=Representative(b.RepresentativeType,b.RepresentativeVariant);if(!kind.HasValue){b.SetRepresentativeContext(PassengerRepresentativeStatus.None,"");return;}PassengerRequestObservation found=null;foreach(var o in observations)if(o&&o.request.Kind==kind.Value){found=o;break;}if(!found){b.SetRepresentativeContext(PassengerRepresentativeStatus.NotPresented,"");return;}var state=found.request.Snapshot();var status=state.safety?PassengerRepresentativeStatus.Deferred:!state.accepted?(state.finished?PassengerRepresentativeStatus.Neutral:PassengerRepresentativeStatus.Pending):state.goalRecorded?(state.satisfactionDelta>0?PassengerRepresentativeStatus.Succeeded:state.satisfactionDelta<0?PassengerRepresentativeStatus.Failed:PassengerRepresentativeStatus.Neutral):PassengerRepresentativeStatus.Accepted;b.SetRepresentativeContext(status,found.request.ResponseText());}
 public void SuspendUnansweredForSleep(){bool changed=false;foreach(var o in observations)if(o&&!o.request.Resolved&&!o.request.Accepted){o.request.DeferSafety();changed=true;}if(changed){if(schedule!=null)schedule.Responded();SyncBehaviorContext();session.business.SaveCheckpoint();}}
 public void Begin(){
  if(!session.business.useV2)return;Clear();boardingLawCount=session.law.ledger.Events.Length;initialDistance=Mathf.Max(1,session.jobs.nav.remaining);
  var eligible=EligibleRequests();schedule=PassengerRequestSchedule.Create(session.jobs.current.candidateSeed,eligible);
  var behavior=session.jobs.Behavior;if(behavior!=null){var bound=Representative(behavior.RepresentativeType,behavior.RepresentativeVariant);if(bound.HasValue)schedule.BindRepresentative(bound.Value,eligible,behavior.RepresentativeType==PassengerType.Errand?(behavior.RepresentativeVariant==0?0:behavior.RepresentativeVariant==1?.25f:.5f):-1);}
  foreach(var slot in schedule.Capture().slots)observations.Add(null);
  SyncBehaviorContext();lostItem=ContentTuning.Flag("request.LostItem.enabled",true)&&DrunkRideState.Hash(session.jobs.current.candidateSeed+":postride-lost")%10==0;
 }
 // Explicit fixture/legacy path retains one selected request, without a random draw.
 public void BeginKind(RequestKind kind){Clear();legacy=true;boardingLawCount=session.law.ledger.Events.Length;var o=NewObservation();observations.Add(o);active=0;o.BeginKind(kind);}
 bool ClearSpace(){
  if((PvpNativeLan.IsReplica(session)?Mathf.Abs(session.taxi.Speed):session.taxi.ObservedVelocity.magnitude)>.1f||session.paused||Time.timeScale==0||!PvpNativeLan.IsReplica(session)&&session.taxi.body.angularVelocity.magnitude>.1f)return false;
  var p=session.taxi.transform.position;
  foreach(var j in session.traffic.junctions){
   var d=p-j.center;float x=Mathf.Abs(Vector3.Dot(d,session.traffic.AxisDirection(j,0))),z=Mathf.Abs(Vector3.Dot(d,session.traffic.AxisDirection(j,1)));
   // Vehicle half-length margin protects junction interior and the crossing strips,
   // but does not reject a normal stop behind the painted stop line.
   if(x<j.halfWidth[1]+2.5f&&z<j.halfWidth[0]+2.5f)return false;
   for(int axis=0;axis<2;axis++)for(int sign=-1;sign<=1;sign+=2){
    float along=Vector3.Dot(d,session.traffic.AxisDirection(j,axis))*sign;
    float across=Mathf.Abs(Vector3.Dot(d,session.traffic.AxisDirection(j,1-axis)));
    if(Mathf.Abs(along+j.CrossDistance(axis,sign))<TrafficJunction.CrosswalkDepth*.5f+2.2f&&across<j.halfWidth[axis]+1)return false;
   }
  }return true;
 }
 public bool PresentationAvailable {get{return !(session.accident&&session.accident.Locked)&&!PvpNativeLan.UiBlocked(session)&&(legacy||!ResponseBlockedThisFrame)&&!session.jobs.BehaviorAsleep&&!session.paused&&!session.FullMapOpen&&!session.ChoiceOpen&&string.IsNullOrEmpty(session.jobs.VisibleCareDialogue);}}
 // Dialogue input is independent of physical work safety.
 public bool CanTalk {get{return PresentationAvailable&&Time.timeScale>0&&session.jobs.Active&&!session.jobs.trip.ArrivalLocked;}}
 public bool SchedulePresentationAvailable {get{var j=session.jobs;return CanTalk&&!HasPendingChoice&&!j.BehaviorChoicesOpen&&!j.SituationPending&&!j.SituationWorkActive&&(j.Errand==null||!j.Errand.Waiting)&&(j.Behavior==null||j.Behavior.WorkRemaining<=0);}}
 public bool SafeStationarySpace {get{return ClearSpace();}}
 public float ProposalExtraSeconds {get{return Current?Current.ProposalExtraSeconds:float.PositiveInfinity;}}
 public bool SafeToRespond(){return ClearSpace()&&(legacy||stopSeconds>=2);}
 public bool Respond(bool accept){if(PvpNativeLan.Route(session,"response",accept?1:0))return true;if(!PresentationAvailable||!Current||!CanTalk)return false;bool ok=Current.Respond(accept);AfterResponse(ok);return ok;}
 public bool RespondEta(int option){if(PvpNativeLan.Route(session,"eta",option))return true;bool ok=PresentationAvailable&&Current&&Current.RespondEta(option);AfterResponse(ok);return ok;}
 public bool RespondAlternative(){if(PvpNativeLan.Route(session,"alternative"))return true;bool ok=PresentationAvailable&&Current&&Current.RespondAlternative();AfterResponse(ok);return ok;}
 public bool QuietPromiseActive {get{return !finished&&observations.Exists(o=>o&&o.request.Kind==RequestKind.Quiet&&o.request.Accepted);}}
 public string SmallTalkReply {get{return smallTalkVisible>0?smallTalkReply:"";}}
 IEnumerable<string> ChatCandidates(){
  if(QuietPromiseActive){yield return session.jobs.PassengerContent.Text("request.Quiet.reply.SmallTalkViolation","기사: 오늘은 어떤 하루를 보내셨나요?\n승객: 조용히 가 달라고 부탁드렸는데요. 쉬고 싶어요.");yield break;}
  yield return session.jobs.PassengerContent.Text("request.LocalChat.reply.SmallTalk","기사: 오늘은 어떤 하루를 보내셨나요?\n승객: 이런저런 일이 있었네요. 목적지까지 잘 부탁드려요.");
  foreach(string line in ExtraChatLines)yield return line;
 }
 public bool CanDriverSmallTalk {get{return session&&session.business.useV2&&!finished&&CanTalk&&session.jobs.trip.State==TripState.OnTrip&&!HasPendingChoice&&!session.jobs.BehaviorChoicesOpen&&!session.jobs.SituationPending&&!session.jobs.SituationWorkActive&&(session.jobs.Behavior==null||!session.jobs.Behavior.HasChoice&&session.jobs.Behavior.WorkRemaining<=0)&&smallTalkCooldown<=0&&chatHistory.Next(ChatCandidates())!=null;}}
 public bool TryDriverSmallTalk(){
  if(!CanDriverSmallTalk)return false;
  if(PvpNativeLan.Route(session,"smalltalk"))return true;
  if(PvpNativeLan.IsReplica(session))return false;
  smallTalkReply=chatHistory.Next(ChatCandidates());chatHistory.Remember(smallTalkReply);
  foreach(var o in observations)if(o)o.request.ObserveDriverSmallTalk();
  smallTalkCooldown=12;smallTalkVisible=6;session.business.SaveCheckpoint();return true;
 }
 public bool PrepareProposal(){return Current&&Current.PrepareProposal();}
 public bool CanChoose(PassengerReply reply){return CanTalk&&Current&&Current.CanChoose(reply);}
 public bool Choose(PassengerReply reply){if(PvpNativeLan.Route(session,"reply",(int)reply))return true;bool ok=CanChoose(reply)&&Current.Choose(reply);AfterResponse(ok);return ok;}
 void AfterResponse(bool ok){if(!ok)return;if(!legacy)BlockResponseThisFrame();if(schedule!=null)schedule.Responded();SyncBehaviorContext();session.business.SaveCheckpoint();}
 public string DialogueReply {get{return Current?Current.DialogueReply:"";}}
 public string AlternativeLabel {get{return Current?Current.AlternativeLabel:"";}}
 public PassengerRequestChoice[] ReplyChoices {get{return Current?Current.ReplyChoices:new PassengerRequestChoice[0];}}
 public string ChoiceText(PassengerRequestChoice choice){return Current?Current.ChoiceText(choice):"";}
 public static string UpcomingTitle(int completed){return PassengerRequestObservation.UpcomingTitle(completed);}
 public static Rect ReplyButtonRect(Rect area,int index){return PassengerRequestObservation.ReplyButtonRect(area,index);}
 public void AdvanceSafeStop(float seconds){stopSeconds=ClearSpace()?stopSeconds+Mathf.Max(0,seconds):0;}
 void Update(){
  if(!session||!session.business.useV2)return;if(!session.paused&&Time.timeScale>0){smallTalkCooldown=Mathf.Max(0,smallTalkCooldown-Time.deltaTime);smallTalkVisible=Mathf.Max(0,smallTalkVisible-Time.deltaTime);}AdvanceSafeStop(Time.deltaTime);
  if(session.jobs.trip.State!=TripState.OnTrip||session.jobs.trip.ArrivalLocked||session.paused)return;
  if(PvpNativeLan.IsReplica(session)){if(Current&&PresentationAvailable&&!session.IsSimulationBot)Current.ObserveInputOnly();return;}
  if(schedule!=null){
   if(Current&&Current.request.Resolved)schedule.Responded();
   float moving=session.taxi.ObservedVelocity.magnitude>.1f?Time.deltaTime:0;
   int slot=schedule.Tick(moving,1-session.jobs.nav.remaining/initialDistance,SchedulePresentationAvailable,false);
   if(slot>=0){var state=schedule.Capture().slots[slot];var o=NewObservation();observations[slot]=o;active=slot;o.BeginKind(state.kind);BlockResponseThisFrame();SyncBehaviorContext();session.business.SaveCheckpoint();}
  }
  foreach(var o in observations)if(o)o.Observe(!session.IsSimulationBot&&!PvpNativeLan.IsRemoteHuman(session)&&o==Current&&PresentationAvailable);
 }
 public void CancelForFatigue(){Clear();finished=true;session.manners.ComfortAccepted=false;}
 public void Finish(){if(finished)return;finished=true;if(schedule!=null)schedule.Tick(0,1,false,true);foreach(var o in observations)if(o)o.Finish();SyncBehaviorContext();session.manners.ComfortAccepted=false;}
 void OnGUI(){if(!session||session.jobs.trip.State!=TripState.OnTrip||session.jobs.trip.ArrivalLocked||session.IsSimulationBot||PvpNativeLan.IsRemoteHuman(session))return;var behavior=GetComponent<PassengerBehaviorUI>();if(!PresentationAvailable||behavior&&behavior.Showing)return;
  if(Current&&HasVisiblePanel){Current.Draw();return;}
 }
 public PassengerRequestUISnapshot CaptureLive(){
  var s=Current?JsonUtility.FromJson<PassengerRequestUISnapshot>(JsonUtility.ToJson(Current.CaptureLive())):new PassengerRequestUISnapshot{request=empty.Snapshot()};
  s.version=legacy||schedule==null?0:2;s.active=active;s.boardingLawCount=boardingLawCount;s.initialDistance=initialDistance;s.finished=finished;s.lostItem=lostItem;
  s.smallTalkCooldown=smallTalkCooldown;s.smallTalkVisible=smallTalkVisible;s.smallTalkReply=smallTalkReply;s.smallTalkSeen=chatHistory.Capture();
  s.preferLarge=session.jobs.nav.PreferLargeRoad;s.preferFast=session.jobs.nav.PreferFastest;
  if(!legacy){s.schedule=schedule==null?null:schedule.Capture();s.observations=observations.ConvertAll(o=>o?o.CaptureLive():new PassengerRequestObservationSnapshot()).ToArray();}return s;
 }
 public void RestoreLive(PassengerRequestUISnapshot s){
  if(s==null)throw new ArgumentException("Missing request snapshot");ValidateSmallTalk(s);float replicaStopSeconds=stopSeconds;Clear();if(PvpNativeLan.IsReplica(session))stopSeconds=replicaStopSeconds;boardingLawCount=s.version==2?s.boardingLawCount:s.lawBefore;initialDistance=s.initialDistance;finished=s.finished;lostItem=s.lostItem;smallTalkCooldown=s.smallTalkCooldown;smallTalkVisible=s.smallTalkVisible;smallTalkReply=s.smallTalkReply??"";chatHistory.Restore(s.smallTalkSeen,smallTalkReply);
  if(s.version==0){legacy=true;var o=NewObservation();observations.Add(o);active=0;o.RestoreLive(s);}
  else if(s.version==2){
   schedule=PassengerRequestSchedule.Restore(s.schedule);var slots=schedule.Capture().slots;
   if(s.observations==null||s.observations.Length!=slots.Length||s.active< -1||s.active>=slots.Length||(float.IsNaN(s.initialDistance)||float.IsInfinity(s.initialDistance))||s.initialDistance<=0)throw new ArgumentException("Invalid request observations");
   for(int i=0;i<slots.Length;i++){if(!slots[i].presented){observations.Add(null);continue;}var o=NewObservation();o.RestoreLive(s.observations[i]);observations.Add(o);}active=s.active;
  }else throw new ArgumentException("Unknown request version");
  SyncBehaviorContext();session.manners.ComfortAccepted=!finished&&observations.Exists(o=>o&&o.request.Kind==RequestKind.Comfort&&o.request.Accepted);
  if(session.jobs.trip.State==TripState.OnTrip&&!session.jobs.trip.ArrivalLocked&&(PvpNativeLan.IsReplica(session)?session.jobs.nav.PreferLargeRoad!=s.preferLarge||session.jobs.nav.PreferFastest!=s.preferFast:s.preferLarge||!s.preferFast))session.jobs.nav.SetPassengerPreference(s.preferLarge,s.preferFast);
 }
 static void ValidateSmallTalk(PassengerRequestUISnapshot s){if(float.IsNaN(s.smallTalkCooldown)||float.IsInfinity(s.smallTalkCooldown)||s.smallTalkCooldown<0||s.smallTalkCooldown>12||float.IsNaN(s.smallTalkVisible)||float.IsInfinity(s.smallTalkVisible)||s.smallTalkVisible<0||s.smallTalkVisible>6||(s.smallTalkReply??"").Length>4000||s.smallTalkVisible>s.smallTalkCooldown)throw new ArgumentException("Invalid passenger chat observation");new PassengerChatHistory().Restore(s.smallTalkSeen,s.smallTalkReply);}
 public static void ValidateSnapshot(PassengerRequestUISnapshot s){
  if(s==null)throw new ArgumentException("Missing request snapshot");ValidateSmallTalk(s);new PassengerRequest().Restore(s.request);
  if(s.version==0)return;if(s.version!=2)throw new ArgumentException("Unknown request version");
  var state=PassengerRequestSchedule.Restore(s.schedule).Capture();
  if(s.observations==null||s.observations.Length!=state.slots.Length||s.active!=state.active||float.IsNaN(s.initialDistance)||float.IsInfinity(s.initialDistance)||s.initialDistance<=0)throw new ArgumentException("Invalid request observations");
  for(int i=0;i<state.slots.Length;i++)if(state.slots[i].presented){var o=s.observations[i];if(o==null||o.request==null||o.request.kind!=state.slots[i].kind)throw new ArgumentException("Request kind changed on restore");new PassengerRequest().Restore(o.request);foreach(float v in new[]{o.travelled,o.broadTravel,o.etaAge,o.acceptedAge})if(float.IsNaN(v)||float.IsInfinity(v)||v<0)throw new ArgumentException("Invalid request observation time");}
 }
}
