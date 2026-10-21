using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
public class CampaignSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;StringBuilder report=new StringBuilder();
 void Check(bool pass,string text){ok&=pass;report.AppendLine((pass?"PASS ":"FAIL ")+text);}
 void Place(int index){var t=session.taxi;var s=session.jobs.stops[index];t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=s.Kerb+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(s.Direction);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();session.law.ResetObservation();}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);var b=session.business;var a=b.ledger.Account;var jobs=session.jobs;
  bool capture=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-captureCampaign")>=0;
  if(capture){Screen.SetResolution(1600,900,false);b.preparing=true;Time.timeScale=0;yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/campaign-preparation.png"));yield return new WaitForSecondsRealtime(.4f);b.preparing=false;Time.timeScale=1;}
  session.taxi.automated=true;session.taxi.autoBrake=true;foreach(var c in session.traffic.cars){c.Recycle();c.enabled=false;}
  Check(a!=null&&b.ledger.Settings.days==3&&b.ledger.Settings.daySeconds==480,"v2 active schedule");
  for(int i=0;i<3;i++){int index=session.services.indices[i];Place(index);yield return new WaitForSeconds(.4f);Check(jobs.At(index),"service stop alignment "+i);session.services.Toggle();Check(session.services.open&&Time.timeScale==0,"station menu freezes "+i);
   if(capture&&i==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/campaign-service.png"));yield return new WaitForSecondsRealtime(.3f);}
   if(i<2)Check(session.services.Buy(i),"station purchase "+i);else session.services.Toggle();}
  Place(0);yield return new WaitForSeconds(.4f);Check(jobs.Interact(),"roadside pickup v2");
  Check(!string.IsNullOrEmpty(session.law.tripId),"trip identity assigned");session.requests.request.DeferSafety();
  Place(jobs.current.drop);yield return new WaitForSeconds(.4f);int before=a.Cash;
  // This accounting fixture teleports between services/stops; it does not complete a law route.
  // Check the real anti-skip gate first, then detach that encounter for the isolated fare check.
  var zones=GetComponent<ServiceLawZones>();if(zones&&zones.Assigned!=null){Check(!jobs.Interact()&&a.Cash==before,"unvisited law zone blocks payment");zones.EndTrip();jobs.nav.RequireVia(-1);}
  Check(jobs.Interact()&&a.Cash>before,"fare credited to cash in isolated accounting fixture");
  int after=a.Cash;Check(!jobs.Interact()&&a.Cash==after,"duplicate completion does not pay");
  var e=new LawEvent{id="integration-event",day=1,family="exit"};e.Add("L06",LawVerdict.Violation,15);e.Add("L07",LawVerdict.Violation,15);session.law.ledger.Record(e);
  int cash=a.Cash,manners=session.manners.ledger.Score;Check(session.law.ledger.Score(1)==85&&cash==a.Cash&&manners==session.manners.ledger.Score,"independent law record");
  jobs.ClearForDay();Check(b.RequestEndDay(),"end day");yield return new WaitForSecondsRealtime(.2f);
  Check(b.ShowingSummary&&Time.timeScale==0,"daily settlement frozen");Check(b.SaveCheckpoint(),"v2 checkpoint write");
  if(capture){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/campaign-settlement.png"));yield return new WaitForSecondsRealtime(.3f);}
  var snapshot=JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(b.SavePath));
  Check(snapshot.version==2&&snapshot.law.Length>=1&&snapshot.account.entries.Length>0,"law account and identity in one checkpoint");
  var restored=EconomyLedger.RestoreCampaign(snapshot.days,snapshot.account);Check(restored.TotalNet==b.ledger.TotalNet,"daily restore net");
  using(var locked=new FileStream(b.SavePath+".tmp",FileMode.Create,FileAccess.Write,FileShare.None)){Check(!b.SaveCheckpoint()&&b.SaveFailed,"save failure surfaced");}
  Check(b.SaveCheckpoint()&&!b.SaveFailed,"save retry works");
  Check(b.NextDay()&&session.law.ledger.Score(2)==100&&session.law.ledger.Events.Length>=1,"new day resets score retains history");
  b.ledger=new EconomyLedger(new PrototypeSettings());b.ledger.RequestClose();b.ledger.Settle();b.ledger.NextDay();b.ledger.RequestClose();b.ledger.Settle();
  Check(b.NextDay()&&b.ledger.DepartureAllowed&&b.ledger.Account.Cash==3000,"third day departure needs no insurance payment");
  b.ledger=new EconomyLedger(new PrototypeSettings());
  Place(0);yield return new WaitForSeconds(.4f);Check(jobs.Interact(),"occupied fuel recovery setup");a=b.ledger.Account;int recoveryCash=a.Cash;a.Consume(0,1000000);yield return new WaitForSeconds(.2f);
  Check(jobs.trip.State==TripState.Idle&&b.ledger.FailedTrips==1&&b.ledger.Income==0,"fuel depletion ends occupied ride for zero fare");Check(a.Cash==recoveryCash-1800&&a.Fuel>19&&a.Fuel<=20&&b.ledger.Elapsed>=30,"recovery fee supply and time once");
  b.ledger.RecordFare("final-flow-fixture",35000);
  for(int day=1;day<=2;day++){Place(0);yield return new WaitForSeconds(.3f);Check(b.RequestEndDay(),"close day without excluded costs "+day);yield return new WaitForSecondsRealtime(.2f);Check(b.NextDay(),"next day without payment gate "+day);}
  b.ledger.RecordFare("final-day-fixture",7300);int finalCash=a.Cash;
  Place(0);yield return new WaitForSeconds(.3f);b.RequestEndDay();yield return new WaitForSecondsRealtime(.2f);
  Check(b.ledger.State==BusinessState.Won&&a.Net==30000,"final target needs no payment prerequisites");Check(a.Debt==0&&a.Cash==finalCash-3500,"final settlement charges only meal and parking");Check(!b.taxPrompt&&b.ShowingSummary&&Time.timeScale==0,"final summary needs no tax modal");
  string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts","campaign-v2-test.txt"));File.WriteAllText(path,"success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
