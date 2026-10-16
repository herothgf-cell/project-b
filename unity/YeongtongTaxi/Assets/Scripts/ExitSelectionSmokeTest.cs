using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
public class ExitSelectionSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool b,string name){ok&=b;report.AppendLine((b?"PASS ":"FAIL ")+name);File.WriteAllText("artifacts/exit-selection-"+(PracticeSession.Enabled?"child":"parent")+".txt","success="+ok+"\n"+report);}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(1.5f);var p=GetComponent<PracticeSession>();var coach=GetComponent<PracticeCoach>();
  var cash=session.business.ledger.Account.Cash;var fuel=session.business.ledger.Account.Fuel;var clock=session.business.ledger.Elapsed;
  Check(session.law.ledger.Events.Length==0,"no business history required");
  if(PracticeSession.Enabled){
   if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-practiceExit")>=0)Check(coach.Active&&coach.ExitMode&&p.SelectedVariant==7,"direct exit startup before opening picker");
   Check(p.OpenPicker(),"open practice picker");p.SelectedVariant=7;Check(p.SelectedTitle.Contains("출구")&&p.SelectedConditions.Contains("8대")&&p.SelectedGuidance.Contains("우회"),"exit guidance shared with preparation");
   yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/exit-selection.png"));
   Check(p.StartSelected()&&coach.ExitMode,"select exit directly");
   if(!coach.ExitMode){Application.Quit(1);yield break;}
   var source=GetComponent<ExitQueueSituation>().Template.Copy();Check(source!=null&&session.traffic.cars[0].enabled,"queue prepared");
   Check(p.RetryCurrent()&&GetComponent<ExitQueueSituation>().Template.junction==source.junction,"retry same exit");
   Check(p.OpenPicker(),"change to right");p.SelectedVariant=1;Check(p.StartSelected()&&!coach.ExitMode,"right after exit");
   Check(p.OpenPicker(),"change back to exit");p.SelectedVariant=7;Check(p.StartSelected()&&coach.ExitMode,"exit after right");
   Check(GetComponent<ExitQueueSituation>().Template.junction==source.junction,"stable prepared route");
  }else{
   session.business.enabledCampaign=true;session.business.preparing=true;session.paused=true;Time.timeScale=0;
   Check(p.OpenPicker(),"preparation opens choice");p.SelectedVariant=7;Check(p.StartSelected()&&p.Busy,"launch separate exit without history");
   float end=Time.realtimeSinceStartup+60;while(p.Busy&&Time.realtimeSinceStartup<end)yield return null;
   Check(!p.Busy&&session.business.practices.Count>0,"child returns exit result");
  }
  Check(cash==session.business.ledger.Account.Cash&&fuel==session.business.ledger.Account.Fuel&&clock==session.business.ledger.Elapsed,"economy and time unchanged");
  Application.Quit(ok?0:1);
 }
}
