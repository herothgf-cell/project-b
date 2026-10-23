using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Controlled approach positions exercise A/B/C observers; the onward trip uses real physics.
public class ExitTransportSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string message){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+message);Debug.Log(message+" = "+value);Flush();}
 void Flush(){File.WriteAllText(Path.GetFullPath(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-exitAbortTest")>=0?"artifacts/exit-abort-transport-test.txt":"artifacts/exit-transport-test.txt"),"success="+ok+"\n"+report);}
 void Pose(Vector3 p,Vector3 dir){var t=session.taxi;t.transform.SetPositionAndRotation(p+Vector3.up*.65f,Quaternion.LookRotation(dir));t.body.position=t.transform.position;t.body.rotation=t.transform.rotation;Physics.SyncTransforms();}
 IEnumerator Move(Vector3 a,Vector3 b,Vector3 dir){int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.4f);for(int i=1;i<=count;i++){Pose(Vector3.Lerp(a,b,i/(float)count),dir);yield return new WaitForFixedUpdate();}}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);Time.timeScale=6;session.taxi.automated=true;session.assisted.allowAutomated=true;session.roadConditions.enabled=false;session.business.enabledCampaign=false;
  var fixture=GetComponent<ExitQueueSituation>();Pose(new Vector3(-200,0,-200),Vector3.forward);Check(fixture.Begin(),"prepared exit on shared graph");if(!fixture.Active){Application.Quit(1);yield break;}var template=fixture.Template.Copy();fixture.Stop();
  bool abortOnly=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-exitAbortTest")>=0;
  for(int choice=abortOnly?3:0;choice<(abortOnly?5:3);choice++){
   var jobs=session.jobs;var taxi=session.taxi;jobs.ClearForDay();taxi.body.isKinematic=true;taxi.enabled=false;session.law.ledger=new LawLedger();
   Pose(jobs.stops[0].Kerb,jobs.stops[0].Direction);yield return new WaitForFixedUpdate();Check(jobs.Interact(),"board before scenario "+choice);
   Check(fixture.Replay(template),"same queue replay "+choice);jobs.nav.SetDestination(jobs.stops[jobs.current.drop]);
   var route=session.traffic.cars[0].route;var dir=fixture.approach;var side=TrafficSystem.Right(dir);var head=route.Position(template.stop-.25f);
   var start=head-dir*70-side*3.5f;Pose(start,dir);yield return new WaitForSeconds(1);
   var observer=GetComponent<ExitQueueObserver>();var tracked=typeof(ExitQueueObserver).GetField("junction",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(observer);var leader=session.traffic.cars[0];report.AppendLine("DIAG choice="+choice+" v2="+session.business.useV2+" tracked="+(tracked!=null)+" signalized="+fixture.junction.signalized+" dedicated="+session.traffic.Observe(fixture.junction,fixture.axis,session.traffic.Sign(route.direction,fixture.axis,fixture.junction),route.direction,taxi.gameObject).dedicated+" headSignal="+leader.waitingForSignal+" headSpeed="+leader.speed+" stopped="+leader.distance+" stop="+template.stop+" taxiHead="+Vector3.Distance(taxi.transform.position,leader.transform.position));Flush();
   if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-exitProbe")>=0){Application.Quit(0);yield break;}
   Check(leader.waitingForSignal&&leader.speed<.4f,"fixture head obeys provisional red "+choice);
   if(choice==0){var merge=head-dir*63;yield return Move(start,merge,dir);yield return new WaitForSeconds(31);for(float at=2;at<=route.length;at+=.4f){Pose(route.Position(at),route.Tangent(at));yield return new WaitForFixedUpdate();}}
   if(choice==1){yield return Move(start,head+dir*(30-Vector3.Dot(head-fixture.junction.center,dir))-side*3.5f,dir);}
   if(choice==2){var beside=head+dir*5-side*3.5f;yield return Move(start,beside,dir);yield return Move(beside,head+dir*5,dir);yield return new WaitForSeconds(1);yield return new WaitForSeconds(30);for(float at=template.stop+5;at<=route.length;at+=.4f){Pose(route.Position(at),route.Tangent(at));yield return new WaitForFixedUpdate();}}
   if(choice>=3){var beside=head+dir*5-side*3.5f;yield return Move(start,beside,dir);if(choice==4){yield return Move(beside,head+dir*5,dir);yield return new WaitForSeconds(1);yield return Move(head+dir*5,beside,dir);}yield return Move(beside,head+dir*(30-Vector3.Dot(head-fixture.junction.center,dir))-side*3.5f,dir);}
   yield return new WaitForFixedUpdate();LawEvent result=null;int count=0;foreach(var e in session.law.ledger.Events)if(e.family=="exit"){result=e;count++;}
   Check(count==1,"one exit event choice="+choice+" actual="+count);
   if(result!=null){Check(result.rules.Exists(r=>r.rule=="L06"&&r.verdict==(choice==0?LawVerdict.Compliant:(choice==1||choice==3)?LawVerdict.NoOpportunity:LawVerdict.Violation)),"choice L06 "+choice);Check(result.Deduction==((choice==2||choice==4)?15:0),"event max deduction "+choice);Check(result.rules.Exists(r=>r.rule=="L07"&&r.verdict==(choice==2?LawVerdict.Violation:(choice==1||choice==3)?LawVerdict.NoOpportunity:LawVerdict.Compliant)),"observed obstruction verdict "+choice);report.AppendLine("choice="+choice+" action="+result.action+" L07="+result.rules.Find(r=>r.rule=="L07").verdict);}
   fixture.Stop();foreach(var c in session.traffic.cars){c.Recycle();c.enabled=false;}
   jobs.nav.Refresh();Check(jobs.nav.hdRoute!=null,"onward navigation after choice "+choice);if(jobs.nav.hdRoute==null)continue;
   // Keep the actual passenger and deadline; never credit test fares directly.
   taxi.enabled=true;taxi.body.isKinematic=false;taxi.autoBrake=true;yield return new WaitForSeconds(.4f);Check(session.assisted.SetActive(true),"onward assistance "+choice);
   bool arrived=false;float maxError=0,began=Time.time;
   while(Time.time-began<400&&jobs.trip.State==TripState.OnTrip){var path=jobs.nav.hdRoute;if(path==null)break;float error;float at=path.path.Project(taxi.transform.position,out error);maxError=Mathf.Max(error,maxError);float remain=path.path.length-at;var edge=jobs.nav.hd.edges[path.EdgeAt(at)];float bend=Vector3.Angle(path.path.Tangent(at),path.path.Tangent(Mathf.Min(path.path.length,at+12)));float speed=remain<8?1.2f:bend>12||edge.turn!=0||edge.status=="provisional-portal"?3.5f:8;
    taxi.autoThrottle=taxi.Speed<speed?.4f:0;taxi.autoBrake=taxi.Speed>speed+.4f;if(remain<1.8f){taxi.autoThrottle=0;taxi.autoBrake=true;if(taxi.body.velocity.magnitude<.56f){arrived=true;break;}}if(!session.assisted.Active&&remain>2.5f)break;yield return new WaitForSeconds(.1f);
   }
   taxi.autoThrottle=0;taxi.autoBrake=true;session.assisted.SetActive(false);Check(arrived,"physical destination after choice="+choice+" seconds="+(Time.time-began)+" error="+maxError+" fareTime="+jobs.trip.Remaining);int paid=jobs.balance;Check(arrived&&jobs.Interact()&&jobs.balance>paid,"transport pays after choice "+choice);
  }
  report.AppendLine("DONE: kinematic A/B/C approach + Rigidbody onward transport. No manual driving claim.");Flush();Application.Quit(ok?0:1);
 }
}
