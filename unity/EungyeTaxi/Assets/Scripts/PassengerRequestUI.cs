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
 public void Begin(){
  if(!session.business.useV2)return;Clear();boardingLawCount=session.law.ledger.Events.Length;initialDistance=Mathf.Max(1,session.jobs.nav.remaining);
  var eligible=new List<RequestKind>((RequestKind[])Enum.GetValues(typeof(RequestKind)));eligible.Remove(RequestKind.LostItem);
  if(!session.jobs.nav.HasLargeRoadAlternative())eligible.Remove(RequestKind.LargeRoad);
  if(session.jobs.nav.path.Count<2){eligible.Remove(RequestKind.Eta);eligible.RemoveAll(PassengerRequestSchedule.Route);}
  var zones=session.GetComponent<ServiceLawZones>();if(zones&&zones.Live&&zones.Live.Active)eligible.RemoveAll(k=>k==RequestKind.SafeStop||k==RequestKind.DestinationChange||k==RequestKind.ViaStop);
  schedule=PassengerRequestSchedule.Create(session.jobs.current.candidateSeed,eligible.ToArray());foreach(var slot in schedule.Capture().slots)observations.Add(null);
  lostItem=DrunkRideState.Hash(session.jobs.current.candidateSeed+":postride-lost")%10==0;
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
 public bool PresentationAvailable {get{return !session.paused&&!session.FullMapOpen&&!session.ChoiceOpen&&string.IsNullOrEmpty(session.jobs.VisibleCareDialogue);}}
 public bool SafeToRespond(){return ClearSpace()&&(legacy||stopSeconds>=2);}
 public bool Respond(bool accept){if(!PresentationAvailable||!Current||!SafeToRespond()&&!legacy)return false;bool ok=Current.Respond(accept);AfterResponse(ok);return ok;}
 public bool RespondEta(int option){bool ok=PresentationAvailable&&Current&&Current.RespondEta(option);AfterResponse(ok);return ok;}
 public bool RespondAlternative(){bool ok=PresentationAvailable&&Current&&Current.RespondAlternative();AfterResponse(ok);return ok;}
 public bool PrepareProposal(){return Current&&Current.PrepareProposal();}
 public bool CanChoose(PassengerReply reply){return PresentationAvailable&&Current&&SafeToRespond()&&Current.CanChoose(reply);}
 public bool Choose(PassengerReply reply){bool ok=CanChoose(reply)&&Current.Choose(reply);AfterResponse(ok);return ok;}
 void AfterResponse(bool ok){if(!ok)return;if(schedule!=null)schedule.Responded();session.business.SaveCheckpoint();}
 public string DialogueReply {get{return Current?Current.DialogueReply:"";}}
 public string AlternativeLabel {get{return Current?Current.AlternativeLabel:"";}}
 public PassengerRequestChoice[] ReplyChoices {get{return Current?Current.ReplyChoices:new PassengerRequestChoice[0];}}
 public string ChoiceText(PassengerRequestChoice choice){return Current?Current.ChoiceText(choice):"";}
 public static string UpcomingTitle(int completed){return PassengerRequestObservation.UpcomingTitle(completed);}
 public static Rect ReplyButtonRect(Rect area,int index){return PassengerRequestObservation.ReplyButtonRect(area,index);}
 void Update(){
  if(!session||!session.business.useV2||session.jobs.trip.State!=TripState.OnTrip||session.jobs.trip.ArrivalLocked||session.paused)return;
  stopSeconds=ClearSpace()?stopSeconds+Time.deltaTime:0;
  if(schedule!=null){
   if(Current&&Current.request.Resolved)schedule.Responded();
   float moving=session.taxi.body.velocity.magnitude>.1f?Time.deltaTime:0;
   bool dialogue=PresentationAvailable;
   int slot=schedule.Tick(moving,1-session.jobs.nav.remaining/initialDistance,SafeToRespond()&&dialogue,false);
   if(slot>=0){var state=schedule.Capture().slots[slot];var o=NewObservation();observations[slot]=o;active=slot;o.BeginKind(state.kind);session.business.SaveCheckpoint();}
  }
  foreach(var o in observations)if(o)o.Observe(o==Current&&PresentationAvailable);
 }
 public void Finish(){if(finished)return;finished=true;if(schedule!=null)schedule.Tick(0,1,false,true);foreach(var o in observations)if(o)o.Finish();session.manners.ComfortAccepted=false;}
 void OnGUI(){if(Current&&PresentationAvailable)Current.Draw();}
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
  session.manners.ComfortAccepted=!finished&&observations.Exists(o=>o&&o.request.Kind==RequestKind.Comfort&&o.request.Accepted);
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
