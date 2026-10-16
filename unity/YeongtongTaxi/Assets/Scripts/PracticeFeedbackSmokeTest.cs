using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Kinematic fixtures exercise the actual law observer and feedback at rendered markings.
public class PracticeFeedbackSmokeTest : MonoBehaviour {
 public DriveSession session;PracticeCoach coach;bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string name){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+name);File.WriteAllText("artifacts/practice-feedback-test.txt","success="+ok+"\n"+report);}
 void Pose(Vector3 front,Vector3 forward){var p=front-forward*2+Vector3.up*.65f;session.taxi.transform.SetPositionAndRotation(p,Quaternion.LookRotation(forward));session.taxi.body.position=p;session.taxi.body.rotation=session.taxi.transform.rotation;Physics.SyncTransforms();}
 IEnumerator Move(Vector3 a,Vector3 b,Vector3 forward){int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.5f);for(int i=1;i<=count&&!coach.ResultOpen;i++){Pose(Vector3.Lerp(a,b,i/(float)count),forward);yield return new WaitForFixedUpdate();yield return null;}}
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/"+name+".png"));yield return new WaitForSecondsRealtime(.3f);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1.3f);coach=GetComponent<PracticeCoach>();var p=GetComponent<PracticeSession>();var situation=GetComponent<RightTurnSituation>();var j=situation.junction;var dir=session.traffic.AxisDirection(j,0);var side=TrafficSystem.Right(dir);
  session.taxi.automated=true;session.taxi.enabled=false;session.taxi.body.isKinematic=true;
  Check(coach.ShowResult()&&coach.Report.outcome.Contains("미완료"),"no passage is incomplete, not 100-point success");yield return Capture("practice-feedback-incomplete");coach.CloseResult();
  int previousEvents=session.law.ledger.Events.Length;
  for(int test=0;test<8;test++){
   Check(p.OpenPicker(),"picker after attempt "+test);p.SelectedVariant=test<2?1:test==2?3:test==3?4:test<6?6:2;Check(p.StartSelected(),"start clean feedback attempt "+test);situation.enabled=false;Time.timeScale=4;
   var o=new RoadObservation{known=true,trafficClear=true,forward=test<2?SignalColor.Red:SignalColor.Green,dedicated=test==2||test==3,right=test==2?SignalColor.Red:SignalColor.Green,pedestrian=test==4||test==5?PedestrianState.Crossing:PedestrianState.None};
   j.scenarioSignal=true;j.scenarioForward=o.forward;j.observationAxis=0;j.observationSign=1;j.rightTurnObservation=o;
   var start=j.center-dir*(j.StopDistance(0)+4)+side*2;Pose(start,dir);yield return new WaitForFixedUpdate();yield return null;
   var stop=j.center-dir*(j.StopDistance(0)+1)+side*2;
   if(test!=1){yield return Move(start,stop,dir);yield return new WaitForFixedUpdate();yield return null;if(test==0){Check(coach.ProgressText.Contains("정지 인식: 확인됨"),"live stop feedback uses recorded observation");yield return Capture("practice-feedback-progress");}}
   var entry=j.center-dir*(j.StopDistance(0)-.4f)+side*2;Pose(entry,dir);yield return new WaitForFixedUpdate();yield return null;
   if(test==7){yield return Move(entry,j.center+dir*(j.StopDistance(0)+15)+side*2,dir);}
   else{
    var corner=j.center-dir*2+side*2;yield return Move(entry,corner,dir);
    if(test==5)o.pedestrian=PedestrianState.None;
    var across=j.center-dir*2+side*(j.CrossingNearEdge(0)+1);yield return Move(corner,across,side);
    Check(!coach.ResultOpen,"does not freeze on crossing "+test);
    yield return Move(across,across+side*14,side);
   }
   yield return null;Check(coach.ResultOpen&&coach.Finished&&Time.timeScale==0,"automatic result after clearing junction "+test);
   if(coach.Report==null){Application.Quit(1);yield break;}
   if(test==0)Check(coach.Report.good.Contains("정지")&&coach.Report.outcome.Contains("완료"),"R1 stopped success explained");
   if(test==1){Check(coach.Report.issues.Contains("정지선")&&coach.Report.outcome.Contains("보완"),"missed stop names exact location");yield return Capture("practice-feedback-missed-stop");}
   if(test==2)Check(coach.Report.issues.Contains("전용 적색"),"dedicated red explanation");
   if(test==3)Check(coach.Report.good.Contains("전용 녹색"),"green arrow explanation");
   if(test==4){Check(coach.Report.issues.Contains("횡단 중")&&coach.Report.issues.Contains("횡단보도"),"pedestrian violation names crossing location");yield return Capture("practice-feedback-pedestrian");}
   if(test==5)Check(coach.Report.good.Contains("기다린 뒤"),"pedestrian wait success explained");
   if(test==6){Check(coach.Report.outcome.Contains("위반 없음")&&!coach.Report.outcome.Contains("100"),"current successful attempt independent of earlier violations");yield return Capture("practice-feedback-success");}
   if(test==7)Check(coach.Report.outcome.Contains("경로"),"wrong direction is not a successful right turn");
   Check(session.law.ledger.Events.Length==previousEvents+1,"preserves earlier attempts with one new verdict "+test);previousEvents=session.law.ledger.Events.Length;
   coach.CloseResult();yield return null;yield return null;Check(!coach.ResultOpen,"result does not reopen continuously "+test);
   session.taxi.RequestReset();yield return null;Check(coach.AttemptEvent==null&&!coach.Finished&&!coach.ResultOpen&&!session.law.StopRecorded(j)&&session.law.ledger.Events.Length==previousEvents,"R/gamepad reset clears current verdict and stop credit, preserves history "+test);
  }
  Check(p.RetryRight()&&coach.AttemptEvent==null&&!coach.ResultOpen&&session.law.ledger.Events.Length==previousEvents,"same-condition retry clears feedback but retains history");
  Application.Quit(ok?0:1);
 }
}
