using UnityEngine;
// Decisions use the same native safe-stop and proposal contracts as the human.
public sealed class PvpBotPassengerResponses:MonoBehaviour {
 public int Accepted {get;private set;}public int Deferred {get;private set;}public int BehaviorReplies {get;private set;}
 public bool Tick(DriveSession s){
  var j=s.jobs;var r=s.requests;if(!j.Active||!r)return false;
  bool work=j.Behavior!=null&&j.Behavior.WorkRemaining>0||j.SituationWorkActive;
  if(!r.HasPendingChoice&&!j.BehaviorChoicesOpen&&!j.SituationPending&&!work)return false;
  if(!r.SafeStationarySpace)return false;
  if(!r.SafeToRespond()||r.ResponseBlockedThisFrame)return true;
  if(work)return true;
  if(r.HasPendingChoice){
   var kind=r.request.Kind;bool applied=false,defer=false;
   if(kind==RequestKind.SafeStop||kind==RequestKind.DestinationChange||kind==RequestKind.ViaStop){
    bool fits=r.PrepareProposal()&&PvpPassengerPolicy.AcceptRoute(r.ProposalExtraSeconds,j.trip.Remaining,s.simulationMatch.Remaining,j.nav.estimatedSeconds,s.business.ledger.Driver);
    applied=r.Respond(fits);defer=!fits;
   }else if(kind==RequestKind.Eta)applied=r.RespondEta(j.trip.Remaining>j.nav.estimatedSeconds+30?1:0);
   else if(kind==RequestKind.RouteAdvice)applied=j.trip.Remaining>j.nav.estimatedSeconds+60?r.RespondAlternative():r.Respond(true);
   else if(kind==RequestKind.LargeRoad&&j.trip.Remaining<j.nav.estimatedSeconds+30){applied=r.Respond(false);defer=true;}
   else applied=r.Respond(true);
   if(!applied&&r.PresentationAvailable){applied=r.Respond(false);defer=true;}
   if(applied){if(defer)Deferred++;else Accepted++;}return true;
  }
  if(j.CanRespondSituation){j.RespondSituation(0);return true;}
  if(j.BehaviorChoicesOpen&&j.Behavior!=null){var b=j.Behavior;int choice=PvpPassengerPolicy.BehaviorChoice(b.Type,b.FollowupPending,j.Cleanliness,s.simulationMatch.Remaining);if(j.RespondBehavior(choice))BehaviorReplies++;return true;}
  return false;
 }
}
