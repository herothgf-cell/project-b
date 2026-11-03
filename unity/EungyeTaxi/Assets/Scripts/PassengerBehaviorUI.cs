using UnityEngine;
// Shared request stop gate and priority; one response surface handles the keyboard each frame.
public sealed class PassengerBehaviorUI : MonoBehaviour {
 public DriveSession session;
 float noticeUntil,responseUntil;
 string responseNotice="";PassengerBehaviorState responseOwner;
 string situationReply="",situationTrip="";float situationReplyUntil;
 public void NotifySituationReply(string line,string effect){situationReply=line+"\n효과 · "+effect;situationTrip=session.law.tripId;situationReplyUntil=Time.time+7;}
 public void NotifyResponse(int before,int after,string line){responseOwner=session.jobs.Behavior;responseNotice="기사 · "+line+"\n효과 · "+responseOwner.SelectedEffect;responseUntil=noticeUntil=Time.time+8;}
 public void NotifyStateChanged(){var b=session.jobs.Behavior;if(b==null)return;if(b.FollowupPending)session.requests.BlockResponseThisFrame();if(!b.Quiet||b.Sleeping||b.Type==PassengerType.Food)noticeUntil=Time.time+6;}

 public bool Showing {get{if(!session||!session.jobs||session.requests.ResponseBlockedThisFrame||session.jobs.Behavior==null||!session.jobs.Active||session.paused||session.FullMapOpen||session.ChoiceOpen||!string.IsNullOrEmpty(session.jobs.VisibleCareDialogue))return false;var b=session.jobs.Behavior;return b.Type!=PassengerType.General&&!session.requests.HasPendingChoice&&(Time.time<noticeUntil||!session.jobs.BehaviorAsleep&&(session.jobs.BehaviorChoicesOpen||b.WorkRemaining>0||!b.Quiet&&(b.Elapsed<8||b.MovingSeconds>=30&&b.MovingSeconds<38)));}}
 public bool CanRespond {get{return Showing&&session.jobs.BehaviorChoicesOpen&&session.requests.CanTalk&&(session.jobs.trip.State!=TripState.ToPickup||session.jobs.At(session.jobs.current.pickup));}}
 void Update(){if(session.IsSimulationBot)return;if(session.jobs.CanRespondSituation){if(Input.GetKeyDown(KeyCode.Y))session.jobs.RespondSituation(0);else if(Input.GetKeyDown(KeyCode.I))session.jobs.RespondSituation(1);return;}if(!CanRespond)return;if(Input.GetKeyDown(KeyCode.Y))session.jobs.RespondBehavior(0);else if(Input.GetKeyDown(KeyCode.I))session.jobs.RespondBehavior(1);else if(Input.GetKeyDown(KeyCode.O))session.jobs.RespondBehavior(2);}
 void OnGUI(){if(session.IsSimulationBot)return;if(session.jobs.SituationVisible&&session.jobs.SituationPresentationAvailable){DrawSituation();return;}if(!Showing){if(session.requests.CanTalk&&!session.requests.HasVisiblePanel&&session.jobs.Active&&session.law.tripId==situationTrip&&Time.time<situationReplyUntil)PassengerDialogueUI.Draw("기사 답변",situationReply,new PassengerDialogueUI.Option[0]);return;}var b=session.jobs.Behavior;
  var labels=session.jobs.BehaviorChoicesOpen?b.ChoiceLabels:new string[0];var effects=b.ChoiceEffects;var options=new PassengerDialogueUI.Option[labels.Length];for(int i=0;i<labels.Length;i++)options[i]=new PassengerDialogueUI.Option(new[]{"Y","I","O"}[i],labels[i],effects[i],CanRespond);
  string note=responseOwner==b&&Time.time<responseUntil?responseNotice:b.WorkRemaining>0?"안전 정차 중 작업 "+Mathf.CeilToInt(b.WorkRemaining)+"초":"";
  int selected=PassengerDialogueUI.Draw("승객 요청 · "+b.TypeLabel,b.Dialogue,options,note);if(selected>=0)session.jobs.RespondBehavior(selected);
 }
 void DrawSituation(){var j=session.jobs;var situation=j.Situation;var options=j.SituationWorkActive?new PassengerDialogueUI.Option[0]:new[]{new PassengerDialogueUI.Option("Y",situation.ReplyLine(0),situation.ReplyEffect(0),j.CanRespondSituation),new PassengerDialogueUI.Option("I",situation.ReplyLine(1),situation.ReplyEffect(1),j.CanRespondSituation)};
  int selected=PassengerDialogueUI.Draw("승객 이야기",situation.Dialogue,options,j.SituationWorkActive?"작업은 안전 정차 중에만 진행됩니다":"");if(selected>=0)j.RespondSituation(selected);
 }
}
