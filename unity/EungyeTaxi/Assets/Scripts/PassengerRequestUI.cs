using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class PassengerRequestUISnapshot : PassengerRequestObservationSnapshot {
 public int version,active=-1,boardingLawCount;public float initialDistance;public bool finished,lostItem;
 public PassengerRequestScheduleSnapshot schedule;
 public PassengerRequestObservationSnapshot[] observations;
}
public class PassengerRequestUI : MonoBehaviour {
 public DriveSession session;
 readonly List<PassengerRequestObservation> observations=new List<PassengerRequestObservation>();
 readonly PassengerRequest empty=new PassengerRequest();
 int responseBlockedFrame=-1;
 public bool ResponseBlockedThisFrame {get{return responseBlockedFrame==Time.frameCount;}}
 public void BlockResponseThisFrame(){responseBlockedFrame=Time.frameCount;}
 PassengerRequestSchedule schedule;int active=-1,boardingLawCount;float initialDistance,stopSeconds;bool finished,lostItem,legacy;
 PassengerRequestObservation Current {get{return active>=0&&active<observations.Count?observations[active]:null;}}
 public PassengerRequest request {get{return Current?Current.request:empty;}}
 public bool SuppressFollowupDialogue {get{return observations.Exists(o=>o&&o.SuppressFollowupDialogue);}}
 public int TotalSatisfactionDelta {get{int value=0;foreach(var o in observations)if(o)value+=o.request.SatisfactionDelta;return Mathf.Clamp(value,-10,10);}}
 public string Summary {get{var text=new List<string>();for(int i=0;i<observations.Count;i++)if(observations[i])text.Add((i+1)+". "+observations[i].request.ResponseText()+" ["+observations[i].request.SatisfactionDelta.ToString("+0;-0;0")+"]");if(schedule!=null)foreach(var slot in schedule.Capture().slots)if(!slot.presented)text.Add(slot.kind+" · 미발생 중립");if(text.Count==0)text.Add("요청 없음");if(lostItem&&finished)text.Add("하차 후 사건 "+session.law.tripId+":lost-item · 지갑 발견 → 보관·신고 절차 안내 (텍스트 사건)");return string.Join("\n",text.ToArray())+"\n요청 합계 "+TotalSatisfactionDelta.ToString("+0;-0;0");}}
 public bool LawClear {get{var events=session.law.ledger.Events;for(int i=boardingLawCount;i<events.Length;i++)if(events[i].Deduction>0)return false;return true;}}
 public void ExemptSystemRoute(string reason){foreach(var o in observations)if(o)o.SystemRouteReason=reason;}
 void Clear(){foreach(var o in observations)if(o){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}observations.Clear();active=-1;schedule=null;stopSeconds=0;finished=lostItem=legacy=false;empty.Restore(new PassengerRequestSnapshot{resolved=true,response="요청 없음"});}
 PassengerRequestObservation NewObservation(){var o=gameObject.AddComponent<PassengerRequestObservation>();o.enabled=false;o.session=session;return o;}
 public static RequestKind? Representative(PassengerType type,int variant){switch(type){case PassengerType.Comfort:return RequestKind.Comfort;case PassengerType.Hurried:return variant==0?RequestKind.Eta:RequestKind.OnTime;case PassengerType.RouteChecking:return variant==0?RequestKind.LargeRoad:variant==1?RequestKind.ShortRoad:RequestKind.RouteAdvice;case PassengerType.Errand:return RequestKind.ViaStop;case PassengerType.PrecisionStop:return RequestKind.SafeStop;default:return null;}}
 public RequestKind[] EligibleRequests(){
  var eligible=new List<RequestKind>((RequestKind[])Enum.GetValues(typeof(RequestKind)));eligible.Remove(RequestKind.LostItem);
  if(!(session.jobs.trip.State==TripState.ToPickup&&session.jobs.current!=null?session.jobs.nav.HasLargeRoadAlternativeTo(session.jobs.stops[session.jobs.current.drop]):session.jobs.nav.HasLargeRoadAlternative()))eligible.Remove(RequestKind.LargeRoad);
  if(session.jobs.nav.hd==null){eligible.Remove(RequestKind.Eta);eligible.RemoveAll(PassengerRequestSchedule.Route);}
  var zones=session.GetComponent<ServiceLawZones>();if(zones&&zones.Live&&zones.Live.Active||!HasSafeAlternative()){eligible.Remove(RequestKind.SafeStop);eligible.Remove(RequestKind.DestinationChange);eligible.Remove(RequestKind.ViaStop);}
  return eligible.ToArray();
 }
 bool HasSafeAlternative(){var j=session.jobs;if(j.current==null||j.nav.hd==null)return false;var from=j.stops[j.current.pickup];var to=j.stops[j.current.drop];for(int i=0;i<j.stops.Length;i++){if(i==j.current.pickup||i==j.current.drop||j.StopOccupied(i))continue;var stop=j.stops[i];if(j.nav.hd.Route(from.laneEdge,from.laneAt,stop.laneEdge,stop.laneAt)!=null&&j.nav.hd.Route(stop.laneEdge,stop.laneAt,to.laneEdge,to.laneAt)!=null)return true;}return false;}
 public PassengerType[] EligiblePassengerTypes(){var kinds=EligibleRequests();int count=PassengerRequestSchedule.Create(session.jobs.current.candidateSeed,kinds).Capture().slots.Length;var result=new List<PassengerType>();foreach(PassengerType type in Enum.GetValues(typeof(PassengerType))){if(type==PassengerType.General||type==PassengerType.Drunk)continue;var representative=Representative(type,PassengerBehaviorState.Create(session.jobs.current.candidateSeed,type).Variant);if(!representative.HasValue||count>0&&Array.IndexOf(kinds,representative.Value)>=0)result.Add(type);}return result.ToArray();}
 public int ScheduledChoiceCount {get{return schedule==null?observations.Count:schedule.Capture().slots.Length;}}
 public bool HasPendingChoice {get{return Current&&!Current.request.Resolved;}}
 public bool HasVisiblePanel {get{return Current&&Current.PanelVisible&&PresentationAvailable;}}
 public void SyncBehaviorContext(){var b=session.jobs.Behavior;if(b==null)return;var kind=Representative(b.Type,b.Variant);if(!kind.HasValue){b.SetRepresentativeContext(PassengerRepresentativeStatus.None,"");return;}PassengerRequestObservation found=null;foreach(var o in observations)if(o&&o.request.Kind==kind.Value){found=o;break;}if(!found){b.SetRepresentativeContext(PassengerRepresentativeStatus.NotPresented,"");return;}var state=found.request.Snapshot();var status=state.safety?PassengerRepresentativeStatus.Deferred:!state.accepted?(state.finished?PassengerRepresentativeStatus.Neutral:PassengerRepresentativeStatus.Pending):state.goalRecorded?(state.satisfactionDelta>0?PassengerRepresentativeStatus.Succeeded:state.satisfactionDelta<0?PassengerRepresentativeStatus.Failed:PassengerRepresentativeStatus.Neutral):PassengerRepresentativeStatus.Accepted;b.SetRepresentativeContext(status,found.request.ResponseText());}
 public void SuspendUnansweredForSleep(){bool changed=false;foreach(var o in observations)if(o&&!o.request.Resolved&&!o.request.Accepted){o.request.DeferSafety();changed=true;}if(changed){if(schedule!=null)schedule.Responded();SyncBehaviorContext();session.business.SaveCheckpoint();}}
 public void Begin(){
  if(!session.business.useV2)return;Clear();boardingLawCount=session.law.ledger.Events.Length;initialDistance=Mathf.Max(1,session.jobs.nav.remaining);
  var eligible=EligibleRequests();schedule=PassengerRequestSchedule.Create(session.jobs.current.candidateSeed,eligible);
  var behavior=session.jobs.Behavior;if(behavior!=null){var bound=Representative(behavior.Type,behavior.Variant);if(bound.HasValue)schedule.BindRepresentative(bound.Value,eligible,behavior.Type==PassengerType.Errand?(behavior.Variant==0?0:behavior.Variant==1?.25f:.5f):-1);}
  foreach(var slot in schedule.Capture().slots)observations.Add(null);
  SyncBehaviorContext();lostItem=DrunkRideState.Hash(session.jobs.current.candidateSeed+":postride-lost")%10==0;
 }
 // Explicit fixture/legacy path retains one selected request, without a random draw.
 public void BeginKind(RequestKind kind){Clear();legacy=true;boardingLawCount=session.law.ledger.Events.Length;var o=NewObservation();observations.Add(o);active=0;o.BeginKind(kind);}
 bool ClearSpace(){
  if(session.taxi.body.velocity.magnitude>.1f||session.paused||Time.timeScale==0||session.taxi.body.angularVelocity.magnitude>.1f)return false;
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
 public bool PresentationAvailable {get{return (legacy||!ResponseBlockedThisFrame)&&!session.jobs.BehaviorAsleep&&!session.paused&&!session.FullMapOpen&&!session.ChoiceOpen&&string.IsNullOrEmpty(session.jobs.VisibleCareDialogue);}}
 public bool SafeToRespond(){return ClearSpace()&&(legacy||stopSeconds>=2);}
 public bool Respond(bool accept){if(!PresentationAvailable||!Current||!SafeToRespond()&&!legacy)return false;bool ok=Current.Respond(accept);AfterResponse(ok);return ok;}
 public bool RespondEta(int option){bool ok=PresentationAvailable&&Current&&Current.RespondEta(option);AfterResponse(ok);return ok;}
 public bool RespondAlternative(){bool ok=PresentationAvailable&&Current&&Current.RespondAlternative();AfterResponse(ok);return ok;}
 public bool PrepareProposal(){return Current&&Current.PrepareProposal();}
 public bool CanChoose(PassengerReply reply){return PresentationAvailable&&Current&&SafeToRespond()&&Current.CanChoose(reply);}
 public bool Choose(PassengerReply reply){bool ok=CanChoose(reply)&&Current.Choose(reply);AfterResponse(ok);return ok;}
 void AfterResponse(bool ok){if(!ok)return;if(!legacy)BlockResponseThisFrame();if(schedule!=null)schedule.Responded();SyncBehaviorContext();session.business.SaveCheckpoint();}
 public string DialogueReply {get{return Current?Current.DialogueReply:"";}}
 public string AlternativeLabel {get{return Current?Current.AlternativeLabel:"";}}
 public PassengerRequestChoice[] ReplyChoices {get{return Current?Current.ReplyChoices:new PassengerRequestChoice[0];}}
 public string ChoiceText(PassengerRequestChoice choice){return Current?Current.ChoiceText(choice):"";}
 public static string UpcomingTitle(int completed){return PassengerRequestObservation.UpcomingTitle(completed);}
 public static Rect ReplyButtonRect(Rect area,int index){return PassengerRequestObservation.ReplyButtonRect(area,index);}
 public void AdvanceSafeStop(float seconds){stopSeconds=ClearSpace()?stopSeconds+Mathf.Max(0,seconds):0;}
 void Update(){
  if(!session||!session.business.useV2)return;AdvanceSafeStop(Time.deltaTime);
  if(session.jobs.trip.State!=TripState.OnTrip||session.jobs.trip.ArrivalLocked||session.paused)return;
  if(schedule!=null){
   if(Current&&Current.request.Resolved)schedule.Responded();
   float moving=session.taxi.body.velocity.magnitude>.1f?Time.deltaTime:0;
   bool dialogue=PresentationAvailable;
   int slot=schedule.Tick(moving,1-session.jobs.nav.remaining/initialDistance,SafeToRespond()&&dialogue,false);
   if(slot>=0){var state=schedule.Capture().slots[slot];var o=NewObservation();observations[slot]=o;active=slot;o.BeginKind(state.kind);BlockResponseThisFrame();SyncBehaviorContext();session.business.SaveCheckpoint();}
  }
  foreach(var o in observations)if(o)o.Observe(o==Current&&PresentationAvailable);
 }
 public void CancelForFatigue(){Clear();finished=true;session.manners.ComfortAccepted=false;}
 public void Finish(){if(finished)return;finished=true;if(schedule!=null)schedule.Tick(0,1,false,true);foreach(var o in observations)if(o)o.Finish();SyncBehaviorContext();session.manners.ComfortAccepted=false;}
 void OnGUI(){var behavior=GetComponent<PassengerBehaviorUI>();if(Current&&PresentationAvailable&&!(behavior&&behavior.Showing))Current.Draw();}
 public PassengerRequestUISnapshot CaptureLive(){
  var s=Current?JsonUtility.FromJson<PassengerRequestUISnapshot>(JsonUtility.ToJson(Current.CaptureLive())):new PassengerRequestUISnapshot{request=empty.Snapshot()};
  s.version=legacy||schedule==null?0:2;s.active=active;s.boardingLawCount=boardingLawCount;s.initialDistance=initialDistance;s.finished=finished;s.lostItem=lostItem;
  s.preferLarge=session.jobs.nav.PreferLargeRoad;s.preferFast=session.jobs.nav.PreferFastest;
  if(!legacy){s.schedule=schedule==null?null:schedule.Capture();s.observations=observations.ConvertAll(o=>o?o.CaptureLive():new PassengerRequestObservationSnapshot()).ToArray();}return s;
 }
 public void RestoreLive(PassengerRequestUISnapshot s){
  if(s==null)throw new ArgumentException("Missing request snapshot");Clear();boardingLawCount=s.version==2?s.boardingLawCount:s.lawBefore;initialDistance=s.initialDistance;finished=s.finished;lostItem=s.lostItem;
  if(s.version==0){legacy=true;var o=NewObservation();observations.Add(o);active=0;o.RestoreLive(s);}
  else if(s.version==2){
   schedule=PassengerRequestSchedule.Restore(s.schedule);var slots=schedule.Capture().slots;
   if(s.observations==null||s.observations.Length!=slots.Length||s.active< -1||s.active>=slots.Length||(float.IsNaN(s.initialDistance)||float.IsInfinity(s.initialDistance))||s.initialDistance<=0)throw new ArgumentException("Invalid request observations");
   for(int i=0;i<slots.Length;i++){if(!slots[i].presented){observations.Add(null);continue;}var o=NewObservation();o.RestoreLive(s.observations[i]);observations.Add(o);}active=s.active;
  }else throw new ArgumentException("Unknown request version");
  SyncBehaviorContext();session.manners.ComfortAccepted=!finished&&observations.Exists(o=>o&&o.request.Kind==RequestKind.Comfort&&o.request.Accepted);
  if(session.jobs.trip.State==TripState.OnTrip&&!session.jobs.trip.ArrivalLocked&&(s.preferLarge||!s.preferFast))session.jobs.nav.SetPassengerPreference(s.preferLarge,s.preferFast);
 }
 public static void ValidateSnapshot(PassengerRequestUISnapshot s){
  if(s==null)throw new ArgumentException("Missing request snapshot");new PassengerRequest().Restore(s.request);
  if(s.version==0)return;if(s.version!=2)throw new ArgumentException("Unknown request version");
  var state=PassengerRequestSchedule.Restore(s.schedule).Capture();
  if(s.observations==null||s.observations.Length!=state.slots.Length||s.active!=state.active||float.IsNaN(s.initialDistance)||float.IsInfinity(s.initialDistance)||s.initialDistance<=0)throw new ArgumentException("Invalid request observations");
  for(int i=0;i<state.slots.Length;i++)if(state.slots[i].presented){var o=s.observations[i];if(o==null||o.request==null||o.request.kind!=state.slots[i].kind)throw new ArgumentException("Request kind changed on restore");new PassengerRequest().Restore(o.request);foreach(float v in new[]{o.travelled,o.broadTravel,o.etaAge,o.acceptedAge})if(float.IsNaN(v)||float.IsInfinity(v)||v<0)throw new ArgumentException("Invalid request observation time");}
 }
}
