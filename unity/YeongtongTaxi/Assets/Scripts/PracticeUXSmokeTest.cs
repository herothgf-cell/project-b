using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class PracticeUXSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
 string ReportPath {get{return "artifacts/practice-ux-"+(PracticeSession.Enabled?"child":"parent")+".txt";}}
 void Check(bool value,string name){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+name);File.WriteAllText(ReportPath,"success="+ok+"\n"+report);}
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/"+name+".png"));yield return new WaitForSecondsRealtime(.3f);}
 IEnumerator Start(){
  if(PracticeSession.Enabled)Check(!session.GetComponent<PracticeSession>().OpenPicker(),"choice waits for practice initialization");
  yield return new WaitForSeconds(1.5f);var b=session.business;var p=session.GetComponent<PracticeSession>();var right=session.GetComponent<RightTurnSituation>();session.taxi.automated=true;session.taxi.autoBrake=true;
  if(PracticeSession.Enabled){
   float fuel=b.ledger.Account.Fuel;int cash=b.ledger.Account.Cash;double clock=b.ledger.Elapsed;
   var original=new LawEvent{id="prior-attempt",day=1};original.Add("L02",LawVerdict.Violation,15);session.law.ledger.Record(original);
   Check(p.OpenPicker()&&Time.timeScale==0,"child opens picker and freezes");p.SelectedVariant=6;yield return Capture("practice-picker-crossing");
   p.CancelPicker();Check(Time.timeScale==1&&!session.paused&&right.variant==1,"cancel keeps original scenario");
   Check(p.OpenPicker()&&p.StartSelected()&&right.variant==6&&Time.timeScale==1,"mid-practice condition switch");
   yield return null;Check(right.junction.rightTurnObservation.pedestrian==PedestrianState.Crossing,"new crossing scenario starts with visible pedestrian");
   Check(session.law.ledger.Events.Length==1,"switch retains previous attempt verdict");
   for(int v=1;v<=6;v++){Check(p.OpenPicker(),"reopen choice "+v);p.SelectedVariant=v;Check(p.StartSelected()&&right.variant==v,"start selected condition "+v);}
   Check(b.ledger.Account.Fuel==fuel&&b.ledger.Account.Cash==cash&&b.ledger.Elapsed==clock,"switch does not alter campaign economy");
  }else{
   b.preparing=true;Time.timeScale=0;b.RequestPayment(0);Check(b.ConfirmPayment(true)&&b.notice=="결제 완료","preparation payment feedback");yield return new WaitForSecondsRealtime(4.3f);Check(b.notice=="","preparation notice expires while paused");
   yield return Capture("practice-preparation-details");
   b.preparing=false;session.paused=true;var services=session.services;services.open=true;Check(services.RequestAction(4)&&services.ConfirmAction(true)&&services.notice=="결제 완료","service payment feedback");yield return new WaitForSecondsRealtime(4.3f);Check(services.notice=="","service notice expires while paused");services.open=false;
   var account=JsonUtility.ToJson(b.ledger.Account.Snapshot());double time=b.ledger.Elapsed;
   Check(p.OpenPicker(),"mid-business pause opens choice");yield return Capture("practice-picker-red");p.CancelPicker();Check(session.paused&&Time.timeScale==0,"cancel restores previous pause");
   Check(p.OpenPicker()&&p.StartSelected()&&p.Busy,"mid-business isolated practice launches");Check(!p.OpenPicker(),"cannot launch duplicate practice");
   float until=Time.realtimeSinceStartup+40;while(p.Busy&&Time.realtimeSinceStartup<until)yield return null;
   Check(!p.Busy&&account==JsonUtility.ToJson(b.ledger.Account.Snapshot())&&time==b.ledger.Elapsed&&Time.timeScale==0,"returns to unchanged paused business");
  }
  Application.Quit(ok?0:1);
 }
}
