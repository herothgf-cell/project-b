using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
public class CampaignSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;StringBuilder report=new StringBuilder();
 void Check(bool pass,string text){ok&=pass;report.AppendLine((pass?"PASS ":"FAIL ")+text);}
 void Place(int index){var t=session.taxi;var s=session.jobs.stops[index];t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=s.Kerb+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(s.Direction);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();session.law.ResetObservation();}
 void PlaceService(int kind){if(kind!=0||session.services.Stations.Count==0){Place(session.services.indices[kind]);return;}var station=session.services.Stations[0];var t=session.taxi;t.body.velocity=t.body.angularVelocity=Vector3.zero;t.body.position=station.pump+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(station.direction);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();session.law.ResetObservation();}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);var b=session.business;var a=b.ledger.Account;var jobs=session.jobs;
  bool capture=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-captureCampaign")>=0;
  if(capture){Screen.SetResolution(1600,900,false);b.preparing=true;Time.timeScale=0;yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/campaign-preparation.png"));yield return new WaitForSecondsRealtime(.4f);b.preparing=false;Time.timeScale=1;}
  session.taxi.automated=true;session.taxi.autoBrake=true;foreach(var c in session.traffic.cars){c.Recycle();c.enabled=false;}
  Check(a!=null&&b.ledger.Settings.days==3&&b.ledger.Settings.daySeconds==720&&a.Fuel>59,"v2 active twelve-minute schedule and starting fuel");
  foreach(var offer in jobs.offers)Check(offer.seconds>=120&&offer.seconds>=offer.distance/4+90+offer.waitSeconds,"generous offer deadline without fare change");
  for(int i=0;i<3;i++){PlaceService(i);yield return new WaitForSeconds(.4f);Check(session.services.CanUse(i),"service stop alignment "+i);session.services.Toggle();Check(session.services.open&&Time.timeScale==0,"station menu freezes "+i);
   if(capture&&i==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/campaign-service.png"));yield return new WaitForSecondsRealtime(.3f);}
   if(i<2)Check(session.services.Buy(i),"station purchase "+i);if(session.services.open)session.services.Toggle();}
  Place(0);yield return new WaitForSeconds(.4f);Check(jobs.Interact(),"roadside pickup v2");
  Check(!string.IsNullOrEmpty(session.law.tripId),"trip identity assigned");session.requests.request.DeferSafety();
  Check(!b.RequestEndDay()&&b.ledger.State==BusinessState.Open&&jobs.trip.State==TripState.OnTrip,"N cannot close day while occupied");
  Place(jobs.current.drop);yield return new WaitForSeconds(.4f);int before=a.Cash;
  // This accounting fixture teleports between services/stops; it does not complete a law route.
  // Check the real anti-skip gate first, then detach that encounter for the isolated fare check.
  var zones=GetComponent<ServiceLawZones>();
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
  Place(0);yield return new WaitForSeconds(.4f);Check(jobs.Interact(),"occupied fuel recovery setup");a=b.ledger.Account;int recoveryCash=a.Cash;string occupiedId=session.law.tripId;var occupiedOffer=jobs.current;var occupiedRoute=jobs.nav.hdRoute;var occupiedPosition=session.taxi.transform.position;a.Consume(0,1000000);yield return new WaitForSeconds(.2f);
  Check(jobs.trip.State==TripState.OnTrip&&jobs.current==occupiedOffer&&session.law.tripId==occupiedId&&jobs.nav.hdRoute==occupiedRoute&&Vector3.Distance(occupiedPosition,session.taxi.transform.position)<.8f&&b.ledger.FailedTrips==0,"fuel recovery retains occupied ride identity route and location");Check(a.Cash==recoveryCash-1800&&a.Fuel>19&&a.Fuel<=20&&b.ledger.Elapsed>=30,"recovery fee supply and time once");
  int passengersBefore=b.passengers.Count;jobs.trip.Tick(100000);jobs.RecordExpiredTrip();jobs.RecordExpiredTrip();b.ledger.Advance(720,0);yield return new WaitForSecondsRealtime(.2f);
  Check(!b.ShowingSummary&&b.ledger.State==BusinessState.Closing&&jobs.trip.State==TripState.OnTrip&&jobs.trip.Remaining==0&&jobs.nav.hdRoute==occupiedRoute&&b.passengers.Count==passengersBefore&&b.ledger.FailedTrips==0,"automatic closing waits for late passenger without early record or route removal");
  Check(!jobs.Accept(0)&&!b.RequestEndDay()&&!jobs.Interact(),"closing refuses new call premature N and remote dropoff");
  Place(jobs.current.drop);yield return new WaitForSeconds(.3f);zones=GetComponent<ServiceLawZones>();
  Check(jobs.Interact()&&b.ledger.FailedTrips==1&&b.ledger.Income==0&&b.passengers.Count==passengersBefore+1&&b.passengers[b.passengers.Count-1].paid==0,"late passenger leaves at destination for zero fare and one failure");
  Check(!jobs.Interact()&&b.ledger.FailedTrips==1&&b.passengers.Count==passengersBefore+1,"late dropoff result records once");yield return new WaitForSecondsRealtime(.2f);Check(b.ShowingSummary,"late destination dropoff allows settlement");
  b.ledger=new EconomyLedger(new PrototypeSettings());a=b.ledger.Account;Time.timeScale=1;
  b.ledger.RecordFare("final-flow-fixture",33000);
  for(int day=1;day<=2;day++){Place(0);yield return new WaitForSeconds(.3f);Check(b.RequestEndDay(),"close day without excluded costs "+day);yield return new WaitForSecondsRealtime(.2f);Check(b.NextDay(),"next day without payment gate "+day);}
  b.ledger.RecordFare("final-day-fixture",7500);int finalCash=a.Cash;
  Place(0);yield return new WaitForSeconds(.3f);b.RequestEndDay();yield return new WaitForSecondsRealtime(.2f);
  Check(b.ledger.State==BusinessState.Won&&a.Net==30000,"final target needs no payment prerequisites");Check(a.Debt==0&&a.Cash==finalCash-3500,"final settlement charges only meal and parking");Check(!b.taxPrompt&&b.ShowingSummary&&Time.timeScale==0,"final summary needs no tax modal");
  string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts","campaign-v2-test.txt"));File.WriteAllText(path,"success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
