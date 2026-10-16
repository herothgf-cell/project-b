using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

public class CampaignRestartSmokeTest : MonoBehaviour {
 bool ok=true; readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string name){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+name);File.WriteAllText("artifacts/campaign-restart-test.txt","success="+ok+"\n"+report);}
 bool Call(BusinessProgress b,string name,params object[] args){return (bool)typeof(BusinessProgress).GetMethod(name).Invoke(b,args);}
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/"+name+".png"));yield return new WaitForSecondsRealtime(.3f);}
 public IEnumerator Run(DriveSession s){
  yield return new WaitForSeconds(1);var b=s.business;
  Check(typeof(BusinessProgress).GetMethod("RequestRestart")!=null&&typeof(BusinessProgress).GetMethod("ConfirmRestart")!=null,"restart confirmation API exists");
  if(!ok){Application.Quit(1);yield break;}
  s.taxi.automated=true;s.taxi.autoBrake=true;
  Check(!Call(b,"RequestRestart"),"moving business cannot open reset directly");
  b.ledger.RecordFare("restart-old-trip",8000);b.ledger.Advance(480,0);b.ledger.Settle();b.ledger.NextDay();
  var stop=s.jobs.stops[0];s.taxi.body.velocity=s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=stop.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(stop.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();yield return new WaitForSeconds(.3f);
  report.AppendLine("BOARD FIXTURE speed="+s.taxi.body.velocity.magnitude+" at="+s.jobs.At(0)+" canAccept="+b.CanAccept+" time="+Time.timeScale+" state="+s.jobs.trip.State);
  s.taxi.body.velocity=s.taxi.body.angularVelocity=Vector3.zero;
  Check(s.jobs.Interact()&&s.jobs.trip.State==TripState.OnTrip,"occupied trip before restart");
  b.passengers.Add(new PassengerResult{tripId="old"});b.practices.Add(new PracticeResult{sourceId="old"});
  var e=new LawEvent{id="old",day=1};e.Add("L01",LawVerdict.Violation,20);s.law.ledger.Record(e);
  s.paused=true;Time.timeScale=0;Check(b.SaveCheckpoint(),"test checkpoint established");
  var old=b.ledger;string bytes=File.ReadAllText(b.SavePath);string campaign=s.law.ids.campaign;
  Check(Call(b,"RequestRestart"),"pause can request restart");yield return Capture("restart-confirmation");
  Check(!Call(b,"ConfirmRestart",false)&&b.ledger==old&&File.ReadAllText(b.SavePath)==bytes&&s.paused&&Time.timeScale==0&&s.jobs.trip.State==TripState.OnTrip,"cancel preserves state save and pause");
  Check(!Call(b,"ConfirmRestart",true),"confirmation required");
  Call(b,"RequestRestart");Directory.CreateDirectory(b.SavePath+".tmp");
  try{Check(!Call(b,"ConfirmRestart",true)&&b.ledger==old&&File.ReadAllText(b.SavePath)==bytes&&b.SaveFailed&&b.restartPrompt,"failed save preserves current campaign and visible error");}finally{Directory.Delete(b.SavePath+".tmp");}
  Check(Call(b,"ConfirmRestart",true),"restart after failure retry");yield return null;yield return null;
  Check(b.ledger.Day==1&&b.ledger.Elapsed==0&&b.ledger.Account.Cash==10000&&b.ledger.Account.Debt==0&&b.ledger.Account.Fuel==35&&!b.ledger.Account.Renewed&&!b.ledger.Account.TaxPaid,"fresh first day finances and fuel");
  Check(b.passengers.Count==0&&b.practices.Count==0&&s.law.ledger.Events.Length==0&&s.law.ids.campaign!=campaign&&s.jobs.trip.State==TripState.Idle,"old history and trip cleared");
  Check(b.preparing&&Time.timeScale==0&&s.jobs.At(s.services.indices[2]),"returns to first day parking preparation");
  var saved=JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(b.SavePath));var restored=EconomyLedger.RestoreCampaign(saved.days,saved.account);
  Check(restored.Day==1&&restored.Account.Fuel==35&&restored.Account.Cash==10000&&saved.ids.campaign==s.law.ids.campaign&&saved.law.Length==0&&saved.passengers.Length==0,"new checkpoint reloads with matching identity");
  Check(!Call(b,"ConfirmRestart",true),"duplicate confirm blocked");
  Check(Call(b,"RequestRestart")&&!Call(b,"ConfirmRestart",false),"preparation supports cancel");yield return Capture("restart-first-day");
  b.Depart();b.ledger.RequestClose();b.ledger.Settle();Time.timeScale=0;
  Check(Call(b,"RequestRestart")&&!Call(b,"ConfirmRestart",false),"ordinary settlement supports restart");
  b.pendingPayment=0;Check(!Call(b,"RequestRestart"),"payment confirmation cannot be interrupted");b.pendingPayment=-1;
  Application.Quit(ok?0:1);
 }
}
