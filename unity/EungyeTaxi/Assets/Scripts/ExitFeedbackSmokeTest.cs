using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
public class ExitFeedbackSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;PracticeCoach coach;readonly StringBuilder report=new StringBuilder();
 void Check(bool b,string name){ok&=b;report.AppendLine((b?"PASS ":"FAIL ")+name);File.WriteAllText("artifacts/exit-feedback-test.txt","success="+ok+"\n"+report);}
 void Pose(Vector3 p,Vector3 d){var t=session.taxi;t.transform.SetPositionAndRotation(p+Vector3.up*.65f,Quaternion.LookRotation(d));t.body.position=t.transform.position;t.body.rotation=t.transform.rotation;Physics.SyncTransforms();}
 IEnumerator Move(Vector3 a,Vector3 b,Vector3 d){int n=Mathf.CeilToInt(Vector3.Distance(a,b)/.4f);for(int i=1;i<=n&&!coach.ResultOpen;i++){Pose(Vector3.Lerp(a,b,i/(float)n),d);yield return new WaitForFixedUpdate();yield return null;}}
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/"+name+".png"));yield return new WaitForSecondsRealtime(.3f);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1.5f);coach=GetComponent<PracticeCoach>();var practice=GetComponent<PracticeSession>();var fixture=GetComponent<ExitQueueSituation>();GetComponent<RightTurnSituation>().Stop();
  var taxi=session.taxi;taxi.automated=true;taxi.enabled=false;taxi.body.isKinematic=true;Pose(new Vector3(-200,0,-200),Vector3.forward);
  Check(fixture.Begin(),"find exit route");if(!fixture.Active){Application.Quit(1);yield break;}var template=fixture.Template.Copy();fixture.Stop();
  int before=0;
  for(int choice=0;choice<4;choice++){
   Check(practice.StartExit(template),"start exit attempt "+choice);session.paused=false;Time.timeScale=6;before=session.law.ledger.Events.Length;fixture.enabled=false;
   var route=session.traffic.cars[0].route;var dir=fixture.approach;var side=TrafficSystem.Right(dir);var head=route.Position(template.stop-.25f);var start=head-dir*70-side*3.5f;if(choice==3){yield return new WaitForSeconds(1);var spawn=taxi.transform.position;spawn.y=0;yield return Move(spawn,start,dir);}else Pose(start,dir);yield return new WaitForSeconds(1);
   Check(coach.ExitMode&&coach.AttemptEvent==null,"clean exit mode "+choice);
   Check(session.traffic.cars[0].waitingForSignal,"queue waiting "+choice);
   if(choice==0){yield return Capture("exit-feedback-approach");Check(coach.ShowResult()&&coach.Report.outcome.Contains("미완료"),"incomplete not success");coach.CloseResult();yield return Move(start,head-dir*63,dir);yield return new WaitForSeconds(1);fixture.junction.rightTurnObservation.right=SignalColor.Green;yield return new WaitForSeconds(2);for(float at=2;at<route.length&&!coach.ResultOpen;at+=.4f){Pose(route.Position(at),route.Tangent(at));yield return new WaitForFixedUpdate();yield return null;}}
   if(choice==1||choice==3)yield return Move(start,head+dir*(30-Vector3.Dot(head-fixture.junction.center,dir))-side*3.5f,dir);
   if(choice==2){var beside=head+dir*5-side*3.5f;yield return Move(start,beside,dir);yield return Move(beside,head+dir*5,dir);yield return new WaitForSeconds(1);fixture.junction.rightTurnObservation.right=SignalColor.Green;yield return new WaitForSeconds(2);for(float at=template.stop+5;at<route.length&&!coach.ResultOpen;at+=.4f){Pose(route.Position(at),route.Tangent(at));yield return new WaitForFixedUpdate();yield return null;}}
   yield return null;Check(coach.ResultOpen&&coach.Finished&&coach.AttemptEvent!=null&&coach.AttemptEvent.family=="exit","automatic exit result "+choice);
   if(coach.Report!=null){Check(choice==2?coach.Report.issues.Contains("앞부분"):(choice==1||choice==3)?coach.Report.good.Contains("우회"):coach.Report.good.Contains("뒤"),"correct action explanation "+choice);yield return Capture("exit-feedback-result-"+choice);}
   int recorded=session.law.ledger.Events.Length;Check(recorded>before,"recorded attempt "+choice);
   Check(practice.RetryCurrent()&&!coach.ResultOpen&&coach.AttemptEvent==null&&fixture.Template.junction==template.junction,"same template retry "+choice);Check(session.law.ledger.Events.Length==recorded,"history retained "+choice);
  }
  var readability=GetComponent<DrivingReadability>();readability.SetBounds();Check(Vector2.Distance(readability.Project(taxi.transform.position),new Vector2(140,140))<.01f,"local map player centered");
  var j=fixture.junction;var heading=session.traffic.AxisDirection(j,fixture.axis)*fixture.sign;Pose(j.center-heading*30,heading);readability.SelectSignal();Check(readability.SignalTarget==j,"ahead signal selected");
  Pose(j.center+heading*30,heading);readability.SelectSignal();Check(readability.SignalTarget!=j,"behind signal excluded");
  // Exercise passenger markers and a long route without charging or saving a campaign.
  fixture.Stop();coach.enabled=false;GetComponent<PracticeSession>().enabled=false;session.jobs.ClearForDay();session.jobs.current=session.jobs.offers[0];session.jobs.trip.Accept(session.jobs.current.fare,session.jobs.current.seconds);session.jobs.nav.SetDestination(session.jobs.stops[session.jobs.current.pickup]);
  session.paused=false;Time.timeScale=1;var origin=session.jobs.stops[0];var lane=session.jobs.nav.hd.paths[origin.laneEdge];Pose(lane.Position(origin.laneAt),lane.Tangent(origin.laneAt));session.jobs.nav.SetDestination(session.jobs.stops[session.jobs.current.pickup]);yield return new WaitForSeconds(.6f);Check(session.jobs.nav.path.Count>1,"real passenger navigation path visible");readability.Overview=false;yield return Capture("navigation-local");readability.Overview=true;yield return Capture("navigation-overview");
  readability.Overview=false;var visible=session.traffic.junctions.Find(candidate=>candidate.signalized);var visibleDirection=session.traffic.AxisDirection(visible,0);Pose(visible.center-visibleDirection*30+TrafficSystem.Right(visibleDirection)*2,visibleDirection);readability.SelectSignal();Check(readability.SignalTarget==visible,"real signal shown in cockpit");session.drivingCamera.SetFirstPerson(true);yield return new WaitForSeconds(.5f);yield return Capture("signal-cockpit");
  Check(session.mirrors.Frame(2).xMax<Screen.width-296,"right mirror does not cover minimap or signal panel");
  Application.Quit(ok?0:1);
 }
}
