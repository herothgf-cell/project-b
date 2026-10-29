using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
public class DriveFeedbackSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;StringBuilder report=new StringBuilder();
 void Check(bool value,string message){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+message);}
 void CheckRepeatedIncidents(DriveNotifications notices){
  var original=session.law.ledger;var repeated=new LawLedger();session.law.ledger=repeated;
  for(int i=0;i<4;i++){var e=new LawEvent{id="repeat-"+i,day=1,tripId="same",family="signal",action="신호 위반"};e.Add("L02",LawVerdict.Violation,4);repeated.Record(e);}notices.RefreshScores();
  var next=new LawEvent{id="repeat-4",day=1,tripId="same",family="signal",action="신호 위반"};next.Add("L02",LawVerdict.Violation,4);repeated.Record(next);notices.RefreshScores();
  Check(repeated.TotalScore==80&&notices.VisibleText.Contains("-4"),"score84 immediately changes for independent violation in same family and trip");
  repeated.RecordIncident(next);notices.RefreshScores();Check(repeated.TotalScore==80&&repeated.Count==5,"same physical incident does not deduct each frame");session.law.ledger=original;notices.RefreshScores();
  var ids=new PrototypeIds("journal");var source=new LawLedger();var bad=new LawEvent{id="journal:event:499",day=1};bad.Add("red",LawVerdict.Violation,20);source.Record(bad);var good=new LawEvent{id="journal:event:500",day=1,recoveryPolicyVersion=1};good.Add("zero",LawVerdict.Compliant,0);source.Record(good);
  var upgrade=good.Copy();upgrade.rules[0].verdict=LawVerdict.Violation;
  var snapshot=new DrivingScoreSnapshot{campaign="journal",sequence=1,events=new[]{upgrade}};DrivingScoreJournal.Restore(snapshot,ids,source);
  Check(source.TotalScore==80&&ids.sequence==500&&ids.Next("event")=="journal:event:501","journal merges equal-penalty verdict upgrades and recovers actual event identity sequence");int revision=source.Revision;DrivingScoreJournal.Restore(snapshot,ids,source);Check(source.TotalScore==80&&source.Revision==revision,"journal replay is idempotent");
 }
 IEnumerator Start(){
  while(!session.StartupReady)yield return null;yield return null;session.business.enabledCampaign=true;session.business.Depart();session.taxi.automated=true;session.taxi.autoBrake=true;
  var notices=session.GetComponent<DriveNotifications>();Check(Application.productName=="은계 택시 시뮬레이터","player title");
  Check(session.traffic.cars.Count==DistrictTraffic.PoolCap,"production general car pool");int expectedSigns=0,signalApproaches=0;foreach(var junction in session.traffic.junctions)for(int i=0;i<junction.lamps.Count;i++){signalApproaches++;if(junction.leftTurnMode==LeftTurnMode.Permissive&&(!junction.scenarioSignal||junction.lampAxes[i]==junction.observationAxis&&junction.lampSigns[i]==junction.observationSign))expectedSigns++;}Check(signalApproaches>0&&session.GetComponent<RoadSignage>().Count==expectedSigns,"only applicable movement condition signs are created");
  session.business.ledger.Account.Consume(0,11500);yield return null;Check(notices.FuelLow,"top low fuel warning at 14 percent");
  var e=new LawEvent{id="feedback-bad",day=1,action="신호 위반"};e.Add("L01",LawVerdict.Violation,20);session.law.ledger.Record(e);yield return null;yield return null;
  Check(session.law.ledger.TotalScore==80&&notices.VisibleText.Contains("-20"),"law deduction toast uses actual score delta");
  CheckRepeatedIncidents(notices);yield return new WaitForSecondsRealtime(4.4f);Check(notices.VisibleText=="","toast automatically dismisses");
  var good=new LawEvent{id="feedback-good",day=2};good.Add("L02",LawVerdict.Compliant,0);session.law.ledger.Record(good);yield return null;yield return null;
  Check(session.law.ledger.TotalScore==81&&notices.VisibleText.Contains("+1"),"next day retains score and observed compliance recovers one");
  Check(session.business.SaveCheckpoint(),"checkpoint saves cumulative observations");var saved=JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(session.business.SavePath));var restored=new LawLedger();foreach(var entry in saved.law)restored.Record(entry);Check(restored.TotalScore==81,"serialized score restores across days");
  var journal=new DrivingScoreSnapshot{campaign="same",sequence=123,events=saved.law};var ids=new PrototypeIds("same");var fromJournal=new LawLedger();Check(DrivingScoreJournal.Restore(journal,ids,fromJournal)&&fromJournal.TotalScore==81&&ids.sequence==123,"journal restores score and avoids reused IDs");Check(DrivingScoreJournal.Restore(journal,ids,fromJournal)&&fromJournal.Count==2,"journal retry does not double score");Check(!DrivingScoreJournal.Restore(journal,new PrototypeIds("new"),new LawLedger()),"new campaign rejects old journal");
  yield return new WaitForSecondsRealtime(3.4f);var j=session.jobs;var taxi=session.taxi;taxi.enabled=false;taxi.body.isKinematic=true;taxi.body.position=j.stops[0].Kerb+Vector3.up*.65f;taxi.body.rotation=Quaternion.LookRotation(j.stops[0].Direction);taxi.transform.SetPositionAndRotation(taxi.body.position,taxi.body.rotation);Physics.SyncTransforms();j.offers[0]=new JobOffer{pickup=0,drop=3,fare=5000,seconds=600,requiredScore=85,distance=j.nav.Distance(j.stops[0],j.stops[3])};Check(j.Accept(0)&&j.Interact(),"actual passenger boarded for satisfaction alert");notices.RefreshScores();session.manners.Deduct("feedback",3,"승객 불편",0);Check(notices.VisibleText=="승객 불편 · -3"&&session.law.ledger.TotalScore==81,"immediate compact passenger alert separate from law score");
  session.paused=true;Time.timeScale=0;Check(session.business.RequestRestart()&&session.business.ConfirmRestart(true),"confirmed new campaign restarts");Check(session.law.ledger.TotalScore==100&&session.law.ledger.Count==0,"new campaign resets cumulative score");
  File.WriteAllText("artifacts/drive-feedback-test.txt","success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
