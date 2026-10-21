using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
public class DriveFeedbackSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;StringBuilder report=new StringBuilder();
 void Check(bool value,string message){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+message);}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);session.business.enabledCampaign=true;session.business.Depart();session.taxi.automated=true;session.taxi.autoBrake=true;
  var notices=session.GetComponent<DriveNotifications>();Check(Application.productName=="택시 시뮬레이터","player title");
  Check(session.traffic.cars.Count==48,"48 general cars");Check(session.GetComponent<RoadSignage>().Count>0,"game road signs created");
  session.business.ledger.Account.Consume(0,11500);yield return null;Check(notices.FuelLow,"top low fuel warning at 14 percent");
  var e=new LawEvent{id="feedback-bad",day=1,action="신호 위반"};e.Add("L01",LawVerdict.Violation,20);session.law.ledger.Record(e);yield return null;yield return null;
  Check(session.law.ledger.TotalScore==80&&notices.VisibleText.Contains("-20"),"law deduction toast uses actual score delta");
  yield return new WaitForSecondsRealtime(4.4f);Check(notices.VisibleText=="","toast automatically dismisses");
  var good=new LawEvent{id="feedback-good",day=2};good.Add("L02",LawVerdict.Compliant,0);session.law.ledger.Record(good);yield return null;yield return null;
  Check(session.law.ledger.TotalScore==81&&notices.VisibleText.Contains("+1"),"next day retains score and observed compliance recovers one");
  Check(session.business.SaveCheckpoint(),"checkpoint saves cumulative observations");var saved=JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(session.business.SavePath));var restored=new LawLedger();foreach(var entry in saved.law)restored.Record(entry);Check(restored.TotalScore==81,"serialized score restores across days");
  var journal=new DrivingScoreSnapshot{campaign="same",sequence=123,events=saved.law};var ids=new PrototypeIds("same");var fromJournal=new LawLedger();Check(DrivingScoreJournal.Restore(journal,ids,fromJournal)&&fromJournal.TotalScore==81&&ids.sequence==123,"journal restores score and avoids reused IDs");Check(DrivingScoreJournal.Restore(journal,ids,fromJournal)&&fromJournal.Count==2,"journal retry does not double score");Check(!DrivingScoreJournal.Restore(journal,new PrototypeIds("new"),new LawLedger()),"new campaign rejects old journal");
  yield return new WaitForSecondsRealtime(4.4f);session.manners.Deduct("feedback",3,"승객 불편",0);yield return null;yield return null;Check(notices.VisibleText.Contains("승객 매너 -3"),"manner notification separate from driving score");
  session.paused=true;Time.timeScale=0;Check(session.business.RequestRestart()&&session.business.ConfirmRestart(true),"confirmed new campaign restarts");Check(session.law.ledger.TotalScore==100&&session.law.ledger.Count==0,"new campaign resets cumulative score");
  File.WriteAllText("artifacts/drive-feedback-test.txt","success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
