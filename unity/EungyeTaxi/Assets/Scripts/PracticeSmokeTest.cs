using System.Collections;
using System.IO;
using UnityEngine;
public class PracticeSmokeTest : MonoBehaviour {
 public DriveSession session;
 IEnumerator Start(){
  string playerPath=Path.Combine(Application.persistentDataPath,PrototypeSettings.SaveFile);string before=File.Exists(playerPath)?File.ReadAllText(playerPath):null;
  yield return new WaitForSeconds(2);var b=session.business;var a=b.ledger.Account;int cash=a.Cash;float fuel=a.Fuel;double time=b.ledger.Elapsed;
  session.taxi.automated=true;session.taxi.autoThrottle=.3f;yield return new WaitForSeconds(2);session.taxi.autoThrottle=0;session.taxi.autoBrake=true;
  var e=new LawEvent{id="practice-only",day=1,family="signal"};e.Add("L01",LawVerdict.Violation,20);session.law.ledger.Record(e);
  bool ok=PracticeSession.Enabled&&!b.enabledCampaign&&!b.CanAccept&&a.Cash==cash&&a.Fuel==fuel&&b.ledger.Elapsed==time&&b.SavePath!=playerPath&&(File.Exists(playerPath)?File.ReadAllText(playerPath):null)==before;
  File.WriteAllText(Path.GetFullPath("artifacts/practice-v2-test.txt"),"success="+ok+"\npractice input, account, clock and player checkpoint isolation\n");Application.Quit(ok?0:1);
 }
}
