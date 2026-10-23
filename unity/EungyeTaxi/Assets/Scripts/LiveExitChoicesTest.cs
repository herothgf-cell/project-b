using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Controlled branch boundaries with all original NPCs running; physical A is tested separately.
public class LiveExitChoicesTest : MonoBehaviour {
 public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool b,string text){ok&=b;report.AppendLine((b?"PASS ":"FAIL ")+text);File.WriteAllText("artifacts/live-exit-choices.txt","success="+ok+"\n"+report);}
 void Pose(Vector3 p,Vector3 d){var t=session.taxi;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(d);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();}
 IEnumerator Move(Vector3 a,Vector3 b,Vector3 d){int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.4f);for(int i=1;i<=count;i++){Pose(Vector3.Lerp(a,b,i/(float)count),d);yield return new WaitForFixedUpdate();}}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);session.taxi.enabled=false;session.taxi.body.isKinematic=true;session.roadConditions.enabled=false;Time.timeScale=5;
  var fixture=GetComponent<ExitQueueSituation>();var source=fixture.PreparedTemplate();Check(source!=null,"prepared ambient exit");if(source==null){Application.Quit(1);yield break;}
  var districts=new DistrictTraffic[session.traffic.cars.Count];var originalRoutes=new TrafficRoute[districts.Length];float beforeTravel=0;
  for(int i=0;i<districts.Length;i++){districts[i]=session.traffic.cars[i].district;originalRoutes[i]=session.traffic.cars[i].route;beforeTravel+=session.traffic.cars[i].totalTravel;}
  for(int choice=0;choice<3;choice++){
   Check(fixture.Replay(source),"queue fixture "+choice);fixture.enabled=false;GetComponent<ExitQueueObserver>().ResetAttempt();session.law.ledger=new LawLedger();
   var route=session.traffic.cars[0].route;var dir=fixture.approach;var side=TrafficSystem.Right(dir);var head=route.Position(source.stop-.25f);var start=head-dir*70-side*3.5f;Pose(start,dir);yield return new WaitForSeconds(1);
   Check(session.traffic.cars[0].waitingForSignal,"actual queue red stop "+choice);
   int running=0;for(int i=8;i<session.traffic.cars.Count;i++)if(session.traffic.cars[i].enabled)running++;Check(running==session.traffic.cars.Count-8,"other ambient cars active "+choice);
   if(choice==0){yield return Move(start,head-dir*63,dir);yield return new WaitForSeconds(.5f);fixture.junction.rightTurnObservation.right=SignalColor.Green;yield return new WaitForSeconds(2);for(float at=2;at<=route.length;at+=.4f){Pose(route.Position(at),route.Tangent(at));yield return new WaitForFixedUpdate();}}
   if(choice==1)yield return Move(start,head+dir*(30-Vector3.Dot(head-fixture.junction.center,dir))-side*3.5f,dir);
   if(choice==2){var beside=head+dir*5-side*3.5f;yield return Move(start,beside,dir);yield return Move(beside,head+dir*5,dir);yield return new WaitForSeconds(1);fixture.junction.rightTurnObservation.right=SignalColor.Green;yield return new WaitForSeconds(2);for(float at=source.stop+5;at<=route.length;at+=.4f){Pose(route.Position(at),route.Tangent(at));yield return new WaitForFixedUpdate();}}
   yield return new WaitForFixedUpdate();LawEvent found=null;int count=0;foreach(var e in session.law.ledger.Events)if(e.family=="exit"){found=e;count++;}
   Check(count==1,"single branch event "+choice);if(found!=null){Check(found.Deduction==(choice==2?15:0),"A/B lawful, C max deduction "+choice);Check(found.rules.Exists(r=>r.rule=="L06"&&r.verdict==(choice==0?LawVerdict.Compliant:choice==1?LawVerdict.NoOpportunity:LawVerdict.Violation)),"L06 branch "+choice);report.AppendLine("CHOICE "+choice+" action="+found.action+" deduction="+found.Deduction);}
   fixture.Stop();for(int i=0;i<8;i++){var car=session.traffic.cars[i];car.Continue(originalRoutes[i]);car.district=districts[i];car.Recycle();}
  }
  float afterTravel=0;foreach(var car in session.traffic.cars)afterTravel+=car.totalTravel;Check(afterTravel>beforeTravel+100,"ambient traffic actually moved");report.AppendLine("DONE branch boundaries use controlled positions, 24 NPCs enabled; no manual/full-physics B/C claim.");File.WriteAllText("artifacts/live-exit-choices.txt","success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
