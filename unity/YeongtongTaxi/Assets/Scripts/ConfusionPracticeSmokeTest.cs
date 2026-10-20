using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Actual map connectors and observers, controlled poses. Not a claim of manual driving validation.
public class ConfusionPracticeSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;StringBuilder report=new StringBuilder();ConfusionPractice fixture;PracticeCoach coach;PracticeSession picker;
 void Check(bool b,string n){ok&=b;report.AppendLine((b?"PASS ":"FAIL ")+n);File.WriteAllText("artifacts/confusion-practice-test.txt","success="+ok+"\n"+report);}
 void PoseFront(Vector3 front,Vector3 heading){var p=front-heading*2+Vector3.up*.65f;session.taxi.body.position=p;session.taxi.body.rotation=Quaternion.LookRotation(heading);session.taxi.transform.SetPositionAndRotation(p,session.taxi.body.rotation);Physics.SyncTransforms();}
 IEnumerator At(float at){PoseFront(fixture.Route.path.Position(at),fixture.Route.path.Tangent(at));yield return new WaitForFixedUpdate();yield return null;}
 IEnumerator Travel(float from,float to){for(float at=from;at<=to+.1f&&!fixture.State.Finished;at+=.5f)yield return At(at);}
 IEnumerator Capture(string n){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/"+n+".png"));yield return new WaitForSecondsRealtime(.3f);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1.5f);session.taxi.automated=true;session.taxi.enabled=false;session.taxi.body.isKinematic=false;session.taxi.body.useGravity=false;session.taxi.body.detectCollisions=false;fixture=GetComponent<ConfusionPractice>();coach=GetComponent<PracticeCoach>();picker=GetComponent<PracticeSession>();
  string account=JsonUtility.ToJson(session.business.ledger.Account.Snapshot());double clock=session.business.ledger.Elapsed;
  Check(session.world.map.mapId=="eungye","main build uses Eungye map");
  Check(picker.OpenPicker(),"picker opens in existing practice");picker.SelectedVariant=10;yield return Capture("confusion-picker");picker.CancelPicker();
  for(int v=8;v<=14;v++)for(int attempt=0;attempt<(v==9||v==11||v==14?1:2);attempt++){
   Check(picker.OpenPicker(),"select "+v);picker.SelectedVariant=v;bool started=picker.StartSelected();Check(started,"map connector prepared "+v);if(!started){Application.Quit(1);yield break;}
   Time.timeScale=3;yield return null;
   Check(fixture.Route.edges.Count==3&&session.jobs.nav.hdRoute!=null,"connected navigation route "+v);
   var j=fixture.Junction;Check(j.scenarioSignal&&j.scenarioForward==fixture.State.Signal,"displayed signal matches observer "+v);
   bool bad=attempt==1;int count=session.law.ledger.Events.Length;
   float error;var stop=j.center+fixture.Direction*fixture.StopAlong;float stopAt=fixture.Route.path.Project(stop,out error);
   // A pose 1m before the painted line records a local stop. A moving approach must not do so.
   if(!bad&&v!=9&&v!=11&&v!=14){yield return At(stopAt-1);session.taxi.body.velocity=Vector3.zero;yield return new WaitForFixedUpdate();yield return null;float until=Time.time+(v==10?6:v==12?5:v==13?13:0);while(Time.time<until){yield return At(stopAt-1);}}
   else {session.taxi.body.velocity=fixture.Direction*3;}
   if(v==8&&attempt==0)yield return Capture("confusion-red-flash");
   if(v==13&&attempt==0)yield return Capture("confusion-left-clear");
   // Fixed poses exercise entry and exit sampling, not wheel steering feel.
   yield return Travel(stopAt-0.5f,fixture.Route.path.length);
   Check(fixture.State.Finished&&fixture.State.CorrectRoute&&coach.ResultOpen,"automatic result after correct exit "+v+"/"+attempt);
   Check(session.law.ledger.Events.Length==count+1,"one independent event, no generic signal double count "+v+"/"+attempt);
   Check(fixture.AttemptEvent!=null&&(bad?fixture.AttemptEvent.Deduction>0:fixture.AttemptEvent.Deduction==0),"expected verdict "+v+"/"+attempt);
   if(coach.ResultOpen){Check(coach.Report.Details().Contains("잘한 점")&&coach.Report.Details().Contains("다음 시도"),"explained feedback "+v);if(v==10)yield return Capture(bad?"confusion-blocked-bad":"confusion-blocked-good");}
   Check(picker.OpenPicker(),"open then cancel alternative "+v);picker.SelectedVariant=v==8?1:8;picker.CancelPicker();
   Check(picker.RetryCurrent()&&fixture.State.Variant==v&&!fixture.State.Entered&&!coach.ResultOpen,"same-condition reset after cancelled selection "+v);
   Check(session.law.ledger.Events.Length==count+1,"retry retains history "+v);
  }
  Check(picker.OpenPicker(),"switch to original practice");picker.SelectedVariant=1;Check(picker.StartSelected()&&!fixture.Active&&GetComponent<RightTurnSituation>().Active,"original right-turn practice restored");
  Check(picker.OpenPicker(),"switch to exit practice");picker.SelectedVariant=7;Check(picker.StartSelected()&&coach.ExitMode&&!fixture.Active,"original exit practice restored");
  Check(account==JsonUtility.ToJson(session.business.ledger.Account.Snapshot())&&clock==session.business.ledger.Elapsed,"training leaves campaign account/time unchanged");
  File.WriteAllText("artifacts/confusion-practice-test.txt","success="+ok+"\n"+report+"Controlled poses on real graph; manual driving remains separate.\n");Application.Quit(ok?0:1);
 }
}
