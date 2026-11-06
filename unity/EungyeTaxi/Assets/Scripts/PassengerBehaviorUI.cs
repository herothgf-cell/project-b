using UnityEngine;
// Shared dialogue priority; physical work keeps its separate stop gate; one response surface handles the keyboard each frame.
public sealed class PassengerBehaviorUI : MonoBehaviour {
 public DriveSession session;
 float noticeUntil,responseUntil;
 string responseNotice="";PassengerBehaviorState responseOwner;
 string situationReply="",situationEffect="",situationTrip="";float situationReplyUntil;
 public void NotifySituationReply(string line,string effect){situationReply=line;situationEffect=PassengerDialogueUI.EffectText(effect);situationTrip=session.law.tripId;situationReplyUntil=Time.time+7;}
 public void NotifyResponse(int before,int after,string line){responseOwner=session.jobs.Behavior;responseNotice="기사 · "+line+"\n효과 · "+responseOwner.SelectedEffect;responseUntil=noticeUntil=Time.time+8;}
 public void NotifyStateChanged(){var b=session.jobs.Behavior;if(b==null)return;if(b.FollowupPending)session.requests.BlockResponseThisFrame();if(!b.Quiet||b.Sleeping||b.Type==PassengerType.Food)noticeUntil=Time.time+6;}

 public bool Showing {get{if(!session||!session.jobs||session.requests.ResponseBlockedThisFrame||session.jobs.Behavior==null||!session.jobs.Active||session.paused||session.FullMapOpen||session.ChoiceOpen||!string.IsNullOrEmpty(session.jobs.VisibleCareDialogue))return false;var b=session.jobs.Behavior;return (b.Type!=PassengerType.General||b.TraitAction.Length>0||b.TraitWorkRemaining>0||!string.IsNullOrEmpty(b.PresetIntro))&&!session.requests.HasPendingChoice&&(Time.time<noticeUntil||!session.jobs.BehaviorAsleep&&(session.jobs.BehaviorChoicesOpen||b.WorkRemaining>0||!string.IsNullOrEmpty(b.PresetIntro)&&b.Elapsed<8||!b.Quiet&&(b.Elapsed<8||b.MovingSeconds>=30&&b.MovingSeconds<38)));}}
 public bool CanRespond {get{return Showing&&session.jobs.BehaviorChoicesOpen&&session.requests.CanTalk&&(session.jobs.Behavior.TraitAction.Length==0||session.requests.SafeToRespond())&&(session.jobs.trip.State!=TripState.ToPickup||session.jobs.At(session.jobs.current.pickup));}}
 void Update(){
  if(session.IsSimulationBot||PvpNativeLan.IsRemoteHuman(session)||session.requests.ResponseBlockedThisFrame)return;
  if(session.jobs.CanRespondSituation){int selected=NumericChoiceInput.Read(2,i=>session.jobs.CanRespondSituation);if(selected>=0)session.jobs.RespondSituation(selected);return;}
  if(!CanRespond)return;
  int answer=NumericChoiceInput.Read(session.jobs.Behavior.ChoiceLabels.Length,i=>CanRespond);if(answer>=0)session.jobs.RespondBehavior(answer);
 }
 void OnGUI(){if(session.IsSimulationBot||PvpNativeLan.IsRemoteHuman(session))return;if(session.jobs.SituationVisible&&session.jobs.SituationPresentationAvailable){DrawSituation();return;}if(session.jobs.Active&&session.law.tripId==situationTrip&&Time.time<situationReplyUntil&&!session.paused&&Time.timeScale>0&&!session.FullMapOpen&&!session.ChoiceOpen&&!session.requests.HasVisiblePanel&&!session.requests.HasPendingChoice&&!session.requests.ResponseBlockedThisFrame&&string.IsNullOrEmpty(session.jobs.VisibleCareDialogue)){PassengerDialogueUI.Draw("기사 답변",situationReply,new PassengerDialogueUI.Option[0],situationEffect,PassengerDialogueUI.PortraitIdentity(session));return;}if(!Showing)return;var b=session.jobs.Behavior;
  var labels=session.jobs.BehaviorChoicesOpen?b.ChoiceLabels:new string[0];var effects=b.ChoiceEffects;var options=new PassengerDialogueUI.Option[labels.Length];for(int i=0;i<labels.Length;i++)options[i]=new PassengerDialogueUI.Option(NumericChoiceInput.Label(i),labels[i],effects[i],CanRespond);
  string note=responseOwner==b&&Time.time<responseUntil?responseNotice:b.WorkRemaining>0?"안전 정차 중 작업 "+Mathf.CeilToInt(b.WorkRemaining)+"초":"";
  if(b.TraitStatus.Length>0)note+=(note.Length>0?"\n":"")+b.TraitStatus;
  if(b.Type==PassengerType.Food&&b.ConsumptionOccurred)note+=(note.Length>0?"\n":"")+"발생 효과 · 섭취 청결도 -"+b.Content.Int("effect.food.crumbs",2)+""+(b.SpillOccurred?" · 흘림 청결도 -"+b.Content.Int("effect.food.spill",6)+"":"");
  int selected=PassengerDialogueUI.Draw("승객 요청 · "+b.TypeLabel,b.Dialogue,options,note,PassengerDialogueUI.PortraitIdentity(session));if(selected>=0)session.jobs.RespondBehavior(selected);
 }
 void DrawSituation(){var j=session.jobs;var situation=j.Situation;var options=j.SituationWorkActive?new PassengerDialogueUI.Option[0]:new[]{new PassengerDialogueUI.Option("1",situation.ReplyLine(0),situation.ReplyEffect(0),j.CanRespondSituation),new PassengerDialogueUI.Option("2",situation.ReplyLine(1),situation.ReplyEffect(1),j.CanRespondSituation)};
  int selected=PassengerDialogueUI.Draw("승객 이야기",situation.Dialogue,options,j.SituationWorkActive?"작업은 안전 정차 중에만 진행됩니다":"",PassengerDialogueUI.PortraitIdentity(session));if(selected>=0)j.RespondSituation(selected);
 }
}
