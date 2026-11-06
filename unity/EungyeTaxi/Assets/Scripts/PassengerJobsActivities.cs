using System;
using UnityEngine;

// The live checkpoint owns these two states. No new fare/soil/score ledger is introduced.
public partial class PassengerJobs {
 public PassengerSituationState Situation {get;private set;}
 public PassengerErrandState Errand {get;private set;}
 TrafficJunction reactionSignal;int reactionAxis,reactionSign;float nextReactionSignalSearch;
 public bool SituationWorkActive {get{return Situation!=null&&Situation.ReSecureRemaining>0;}}
 public bool SituationVisible {get{return SituationPending||SituationWorkActive;}}
 public bool SituationPending {get{return Situation!=null&&Situation.Pending!=PassengerSituationKind.None;}}
 public bool SituationPresentationAvailable {get{return Active&&trip.State==TripState.OnTrip&&!trip.ArrivalLocked&&!session.paused&&Time.timeScale>0&&!session.FullMapOpen&&!session.ChoiceOpen&&session.requests&&!session.requests.ResponseBlockedThisFrame&&!session.requests.HasVisiblePanel&&!session.requests.HasPendingChoice&&!BehaviorChoicesOpen&&!BehaviorAsleep&&string.IsNullOrEmpty(VisibleCareDialogue)&&!(Errand!=null&&Errand.Waiting);}}
 public string ErrandStatus {get{return Errand==null||!Errand.Waiting?"":"승객 용무 · 무료 대기 "+Mathf.CeilToInt(Errand.Remaining)+"초 · 영업시간·연료 소모 · 안전 정차 유지";}}
 int ReservedPassengerChoices {get{return (session.requests?session.requests.ScheduledChoiceCount:0)+(Behavior!=null&&Behavior.Type!=PassengerType.General?1:0)+(Behavior!=null&&Behavior.SleepInterrupted?1:0);}}
 public bool CanRespondSituation {get{return SituationPending&&SituationPresentationAvailable&&session.requests.CanTalk;}}
 public bool RespondSituation(int answer){if(PvpNativeLan.Route(session,"situation",answer,SituationPending?Situation.Pending.ToString():""))return true;if(!CanRespondSituation)return false;string reply=Situation.ReplyLine(answer),effect=Situation.ReplyEffect(answer);var kind=Situation.Pending;int reaction=Situation.Reactions;if(!Situation.Respond(answer,true))return false;RecordSituationChoice(kind,reaction,answer);var ui=GetComponent<PassengerBehaviorUI>();if(ui)ui.NotifySituationReply(reply,effect);if(Behavior!=null)Behavior.ReassureJolt(answer==1);session.requests.BlockResponseThisFrame();session.business.SaveCheckpoint();return true;}
 void BeginPassengerActivities(){Situation=PassengerSituationState.Create(session.law.tripId,session.manners.brakeEvents,session.manners.accelEvents,session.manners.necessaryBrakeEvents);Errand=null;reactionSignal=null;nextReactionSignalSearch=0;}
 void ClearPassengerActivities(){CancelChoiceEffects();Situation=null;Errand=null;reactionSignal=null;nextReactionSignalSearch=0;}
 public static void ValidateSituationSnapshot(PassengerJobsSnapshot s){
  if(s==null)throw new ArgumentException("Missing passenger activity snapshot");
  if(s.activityVersion==0){s.situation=null;s.errand=null;return;}
  if(s.activityVersion!=1)throw new ArgumentException("Unknown passenger activity version");
  if(!s.situationPresent)s.situation=null;else if(s.situation==null)throw new ArgumentException("Missing passenger situation");
  if(!s.errandPresent)s.errand=null;else if(s.errand==null)throw new ArgumentException("Missing passenger errand");
  if(s.situation!=null)s.situation.Validate();
  if(s.errand!=null){s.errand.Validate();if(s.current==null||s.trip==null||s.trip.state!=TripState.OnTrip&&s.trip.state!=TripState.Complete&&s.trip.state!=TripState.Failed||!s.changeApplied||s.errand.OriginalDestinationId!=s.current.dropStopId||!s.errand.Completed&&(s.passengerVia<0||s.stopIds==null||s.passengerVia>=s.stopIds.Length||s.stopIds[s.passengerVia]!=s.errand.ViaStopId)||s.situation!=null&&s.errand.TripId!=s.situation.TripId)throw new ArgumentException("Inconsistent passenger errand");}
 }
 void RestorePassengerActivities(PassengerJobsSnapshot s){Situation=s.situation==null?null:s.situation.Copy();Errand=s.errand==null?null:s.errand.Copy();reactionSignal=null;nextReactionSignalSearch=0;}
 // Search authored junction data at1Hz, then observe only the chosen approach each frame.
 bool ObservedSignalWait(){
  if(session.taxi.body.velocity.magnitude>.1f||session.taxi.body.angularVelocity.magnitude>.1f)return false;
  if(Time.time>=nextReactionSignalSearch){nextReactionSignalSearch=Time.time+1;reactionSignal=null;float nearest=45;var front=session.taxi.transform.position+session.taxi.transform.forward*2;
   foreach(var j in session.traffic.junctions){if(!j.signalized&&!j.scenarioSignal)continue;for(int axis=0;axis<2;axis++)for(int sign=-1;sign<=1;sign+=2){var dir=session.traffic.AxisDirection(j,axis)*sign;if(Vector3.Dot(dir,session.taxi.transform.forward)<.85f)continue;var offset=j.center-dir*j.StopDistance(axis)-front;float ahead=Vector3.Dot(offset,dir),side=Mathf.Abs(Vector3.Dot(offset,TrafficSystem.Right(dir)));if(ahead< -1||ahead>nearest||side>j.halfWidth[axis]||Vector3.Dot(front-j.center,TrafficSystem.Right(dir))<.15f)continue;nearest=ahead;reactionSignal=j;reactionAxis=axis;reactionSign=sign;}}
  }
  if(reactionSignal==null)return false;var heading=session.traffic.AxisDirection(reactionSignal,reactionAxis)*reactionSign;var o=session.traffic.Observe(reactionSignal,reactionAxis,reactionSign,heading,session.taxi.gameObject);
  return o.known&&o.forward==SignalColor.Red&&session.traffic.LeftSignal(reactionSignal,reactionAxis,reactionSign)!=SignalColor.Green&&!(o.dedicated&&o.right==SignalColor.Green);
 }
 public void AdvancePassengerActivities(float seconds){
  if(!Active||trip.State!=TripState.OnTrip||trip.ArrivalLocked||session.paused||Time.timeScale==0||float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return;
  bool changed=false;
  if(Errand!=null&&Errand.Waiting){bool safe=PassengerViaPending&&At(passengerVia)&&session.requests.SafeToRespond();if(Errand.Advance(seconds,safe,false)){passengerVia=-1;SetGoal(current.drop);message="20초 무료 용무 대기 완료 · 같은 승객의 원래 목적지로 출발하세요";changed=true;}else message=safe?ErrandStatus:"승객 용무 대기 중단 · 약속한 경유 장소로 돌아와 안전 정차하세요 · 남은 "+Mathf.CeilToInt(Errand.Remaining)+"초";}
  if(Situation!=null){bool physical=Situation.ApplyPhysicalObservation(Behavior,session.manners.brakeEvents,session.manners.accelEvents,session.manners.necessaryBrakeEvents);changed|=physical;if(physical){var ui=GetComponent<PassengerBehaviorUI>();if(ui)ui.NotifyStateChanged();}changed|=Situation.AdvanceReSecure(seconds,session.requests.SafeToRespond(),false);if(SituationWorkActive)message=Situation.Dialogue;ApplyBehaviorDirt();bool quiet=Behavior!=null&&Behavior.Quiet||session.requests.SuppressFollowupDialogue;bool available=SituationPresentationAvailable;changed|=Situation.Observe(seconds,ObservedSignalWait(),session.taxi.body.velocity.magnitude>.1f,available,quiet,ReservedPassengerChoices,session.manners.brakeEvents,session.manners.accelEvents,session.manners.necessaryBrakeEvents,false);}
  if(changed)session.business.SaveCheckpoint();
 }
 bool InteractPassengerVia(){
  if(!At(passengerVia)||!session.requests.SafeToRespond()){message="부탁한 경유 장소의 오른쪽 연석에 안전 정차 후 F · 무료 용무 대기20초";return false;}
  session.signals.BeginPassengerStop();
  // Legacy accepted trips have no new errand contract: do not impose a retroactive wait.
  if(Errand==null){passengerVia=-1;SetGoal(current.drop);message="경유 완료 · 최종 목적지로 출발하세요";session.business.SaveCheckpoint();return true;}
  if(Errand.Start(true)){message=ErrandStatus;session.business.SaveCheckpoint();return true;}
  message=ErrandStatus;return Errand.Waiting;
 }
}
