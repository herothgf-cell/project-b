using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
// Controlled pose/route integration checks, not a full physical passenger campaign.
public sealed class PassengerExpansionTest : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-passengerExpansionTest")>=0)new GameObject("Passenger expansion checks").AddComponent<PassengerExpansionTest>();}
 DriveSession s;bool ok=true;readonly StringBuilder log=new StringBuilder();
 void Check(bool value,string reason){ok&=value;log.AppendLine((value?"PASS ":"FAIL ")+reason);}
 void Pose(Vector3 p,Vector3 dir){var b=s.taxi.body;b.velocity=b.angularVelocity=Vector3.zero;b.position=p+Vector3.up*.65f;b.rotation=Quaternion.LookRotation(dir);s.taxi.transform.SetPositionAndRotation(b.position,b.rotation);Physics.SyncTransforms();s.law.ResetObservation();}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);s=FindObjectOfType<DriveSession>();Time.timeScale=1;s.paused=false;s.business.preparing=false;s.taxi.automated=true;s.taxi.autoBrake=true;foreach(var c in s.traffic.cars){c.Recycle();c.enabled=false;}
  var jobs=s.jobs;var zones=s.GetComponent<ServiceLawZones>();
  foreach(RequestKind kind in System.Enum.GetValues(typeof(RequestKind))){
   jobs.ClearForDay();if(zones)zones.EndTrip();var pickup=jobs.stops[0];Pose(pickup.Kerb,pickup.Direction);Check(jobs.Interact(),"board "+kind);if(zones)zones.EndTrip();jobs.nav.RequireVia(-1);
   // Fresh identity isolates one permitted request of each type without changing production frequency.
   s.law.tripId=s.law.ids.Next("request-fixture");s.requests.BeginKind(kind);
   string id=s.law.tripId;int fare=jobs.current.fare;Check(!s.requests.request.Begin(id,RequestKind.Quiet),"one interactive request per trip "+kind);
   Check(s.requests.SafeToRespond(),"safe pickup response position "+kind);
   bool response=s.requests.Choose(s.requests.ReplyChoices[0].reply);Check(response,"accept appropriate response "+kind);
   Check(s.law.tripId==id,"same transport identity "+kind);
   Check(!s.requests.Choose(s.requests.ReplyChoices[0].reply),"duplicate response rejected "+kind);
   if(kind==RequestKind.ViaStop){Check(jobs.PassengerViaPending,"via phase keeps passenger");int via=jobs.PassengerViaStop;if(via>=0){var v=jobs.stops[via];Pose(v.Kerb,v.Direction);float before=jobs.trip.Remaining;int quoted=jobs.trip.Fare;Check(jobs.Interact()&&!jobs.PassengerViaPending&&jobs.trip.State==TripState.OnTrip,"actual via stop F retains occupied ride");Check(s.law.tripId==id&&jobs.trip.Fare==quoted&&jobs.trip.Remaining==before,"via stop no duplicate fare time or identity");}}
   if(kind==RequestKind.FareHelp||kind==RequestKind.RejectIllegal)Check(jobs.current.fare==fare,"no discount or illegal reward "+kind);
   if(kind==RequestKind.LargeRoad)Check(jobs.nav.PreferLargeRoad&&jobs.nav.hdRoute!=null,"large road route shares directed graph");
   if(kind==RequestKind.ShortRoad)Check(!jobs.nav.PreferFastest&&jobs.nav.hdRoute!=null,"shortest route applied");
   if(kind==RequestKind.Comfort){
    Check(s.manners.ComfortAccepted,"accepted comfort enables conditional motion penalty");int score=s.manners.ledger.Score;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var type=typeof(DrivingManners);s.taxi.body.velocity=s.taxi.transform.forward*10;
    type.GetField("previousVelocity",flags).SetValue(s.manners,s.taxi.body.velocity);type.GetField("previousFront",flags).SetValue(s.manners,s.taxi.transform.position+s.taxi.transform.forward*2);type.GetField("suppressed",flags).SetValue(s.manners,0f);type.GetField("filtered",flags).SetValue(s.manners,8f);type.GetField("harshTime",flags).SetValue(s.manners,.6f);type.GetField("harshLatched",flags).SetValue(s.manners,false);s.manners.SendMessage("FixedUpdate");s.taxi.body.velocity=Vector3.zero;Check(s.manners.ledger.Score==score-4,"runtime comfort motion deducts four once rather than base plus request");
   }
   if(kind==RequestKind.SafeStop||kind==RequestKind.DestinationChange){var d=jobs.stops[jobs.current.drop];Pose(d.Kerb,d.Direction);}
   if(kind==RequestKind.RejectIllegal)Check(s.requests.DialogueReply.Contains("신호는 지켜야"),"illegal request dialogue clearly refuses");Check(!s.requests.DialogueReply.StartsWith(s.requests.request.Id+" · "),"dialogue hides request code");s.requests.Finish();Check(s.requests.request.ResponseText().Length>8,"record result "+kind);
   if(kind==RequestKind.SafeStop||kind==RequestKind.DestinationChange||kind==RequestKind.ViaStop)Check(s.requests.request.ResponseText().Contains("달성"),"changed destination or via goal reflects actual stopped position "+kind);
   if(kind==RequestKind.Comfort)Check(s.requests.request.ResponseText().Contains("미달"),"comfort records actual harsh event");
   if(kind==RequestKind.LostItem)Check(s.requests.request.ResponseText().Contains(id+":lost-item"),"lost item followup linked once without payout");
   var end=jobs.stops[jobs.current.drop];Pose(end.Kerb,end.Direction);int income=s.business.ledger.Income,balance=jobs.balance;Check(jobs.Interact(),"settle actual requested delivery "+kind);var reward=jobs.RequestReward;Check(reward!=null&&jobs.balance-balance==reward.Total&&s.business.ledger.Income-income==reward.Total,"fare plus bonus ledger agrees "+kind);int settled=jobs.balance;Check(!jobs.Interact()&&jobs.balance==settled,"delivery cannot pay twice "+kind);
   var saved=s.business.passengers[s.business.passengers.Count-1];var restored=JsonUtility.FromJson<PassengerResult>(JsonUtility.ToJson(saved));Check(saved.baseFare==reward.baseFare&&saved.bonus==reward.bonus&&saved.satisfaction==reward.satisfaction&&restored.bonus==reward.bonus&&restored.satisfactionDelta==reward.satisfactionDelta,"settlement fields serialize and restore "+kind);
   s.services.ObserveCompletedTrip();Check(s.services.PostTripVisible,"post-trip destinations appear "+kind);Check(s.services.GuideAfterTrip(1)&&jobs.nav.active&&!s.services.PostTripVisible,"chosen meal destination sets navigation "+kind);
  }
  foreach(RequestKind kind in System.Enum.GetValues(typeof(RequestKind)))foreach(var choice in PassengerRequestChoices.For(kind)){
   jobs.ClearForDay();if(zones)zones.EndTrip();Pose(jobs.stops[0].Kerb,jobs.stops[0].Direction);Check(jobs.Interact(),"choice fixture boards "+kind+"/"+choice.reply);if(zones)zones.EndTrip();s.law.tripId=s.law.ids.Next("button-fixture");s.requests.BeginKind(kind);
   Check(s.requests.request.Kind==kind,"requested kind available "+kind);int law=s.law.ledger.TotalScore,oldFare=jobs.current.fare;
   s.paused=true;Check(!s.requests.Choose(choice.reply),"paused button blocked "+kind+"/"+choice.reply);s.paused=false;
   if(choice.reply!=PassengerReply.Defer){s.taxi.body.velocity=Vector3.forward*2;Check(!s.requests.Choose(choice.reply),"moving response blocked "+kind+"/"+choice.reply);s.taxi.body.velocity=Vector3.zero;}
   Check(s.requests.CanChoose(choice.reply)&&s.requests.Choose(choice.reply),"visible choice applies "+kind+"/"+choice.reply);
   Check(!s.requests.Choose(choice.reply)&&s.law.ledger.TotalScore==law,"choice cannot repeat or award law points "+kind+"/"+choice.reply);
   if(choice.reply==PassengerReply.Defer){s.requests.Finish();Check(s.requests.request.SatisfactionDelta==0&&jobs.current.fare==oldFare,"defer keeps fare and neutral result "+kind);}
   if(kind==RequestKind.Empathy||kind==RequestKind.Interview)Check(s.requests.SuppressFollowupDialogue==(choice.reply==PassengerReply.Alternative),"quiet alternative actual effect "+kind+"/"+choice.reply);
   if(kind==RequestKind.RouteAdvice&&choice.reply!=PassengerReply.Defer)Check(jobs.nav.PreferFastest==(choice.reply==PassengerReply.Accept),"route button applies chosen route preference");
  }
  for(int i=0;i<4;i++){var area=new Rect(24,400,474,98);var r=PassengerRequestUI.ReplyButtonRect(area,i);Check(area.Contains(r.min)&&area.Contains(r.max-Vector2.one*.01f),"reply button contained "+i);for(int j=0;j<i;j++)Check(!r.Overlaps(PassengerRequestUI.ReplyButtonRect(area,j)),"reply buttons separate "+i+"/"+j);}
  jobs.ClearForDay();Pose(jobs.stops[0].Kerb,jobs.stops[0].Direction);jobs.Interact();s.law.tripId=s.law.ids.Next("safety-fixture");s.requests.BeginKind(RequestKind.Interview);
  if(s.traffic.junctions.Count>0){Pose(s.traffic.junctions[0].center,Vector3.forward);Check(!s.requests.SafeToRespond()&&!s.requests.Choose(s.requests.ReplyChoices[0].reply),"junction auto defers interaction");Check(s.requests.Respond(false),"safety defer always available");}
  File.WriteAllText("artifacts/passenger-expansion-test.txt",log.ToString());Debug.Log(log.ToString());Application.Quit(ok?0:1);
 }
}
