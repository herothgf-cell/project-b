using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class EconomySmokeTest : MonoBehaviour {
 public DriveSession session;
 bool ok=true;int checks;readonly StringBuilder results=new StringBuilder();
 void Check(bool value,string name){checks++;ok&=value;results.AppendLine((value?"PASS ":"FAIL ")+name);Debug.Log((value?"PASS ":"FAIL ")+name);}
 void Move(int stop){var t=session.taxi;t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=session.jobs.stops[stop].Kerb+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(session.jobs.stops[stop].Direction);Physics.SyncTransforms();session.manners.Suppress(1);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);var b=session.business;var j=session.jobs;var t=session.taxi;
  string playerSave=Path.Combine(Application.persistentDataPath,"campaign-v1.json");string saved=File.Exists(playerSave)?File.ReadAllText(playerSave):null;
  t.automated=true;t.autoBrake=true;
  var settings=new EconomySettings{days=2,target=10000,daySeconds=60};b.ledger=new EconomyLedger(settings);
  var e=b.ledger;
  t.autoBrake=false;t.autoThrottle=1;yield return new WaitForSeconds(2);t.autoThrottle=0;t.autoBrake=true;yield return new WaitForSeconds(2);
  Check(e.Metres>1&&e.FuelCost>1,"physical driving consumes fuel");
  double distance=e.Metres;t.ResetTaxi();yield return new WaitForFixedUpdate();Check(e.Metres-distance<1,"reset does not charge teleport distance");
  Time.timeScale=0;double clock=e.Elapsed;int fuel=e.FuelCost;yield return new WaitForSecondsRealtime(.25f);Check(e.Elapsed==clock&&e.FuelCost==fuel,"pause freezes work clock and fuel");Time.timeScale=1;
  Check(j.Accept(0),"dispatch accepted during open business");Move(j.current.pickup);yield return new WaitForSeconds(.7f);Check(j.Interact(),"passenger boards");
  e.Advance(e.SecondsLeft,0);yield return null;Check(e.State==BusinessState.Closing&&j.trip.State==TripState.OnTrip&&!b.ShowingSummary,"deadline lets active passenger finish");
  Check(!j.Accept(1),"closing blocks more dispatches");Move(j.current.drop);yield return new WaitForSeconds(.7f);session.manners.ledger.Reset();int fare=j.current.fare;
  Check(j.Interact()&&e.Income==fare&&e.Rides==1,"real drop-off credits accounting");Check(!j.Interact()&&e.Income==fare,"duplicate drop-off never pays twice");
  yield return new WaitForSecondsRealtime(.15f);Check(b.ShowingSummary&&Time.timeScale==0&&e.Last.income==fare,"last passenger opens frozen settlement");
  Check(e.TotalNet==fare-e.Last.fuel-5000,"fuel insurance food deducted once");Check(!e.Settle(),"settlement is idempotent");
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
  string json=JsonUtility.ToJson(e.Snapshot(),true);File.WriteAllText(Path.Combine(folder,"economy-checkpoint-test.json"),json);
  var restored=EconomyLedger.Restore(JsonUtility.FromJson<EconomySnapshot>(File.ReadAllText(Path.Combine(folder,"economy-checkpoint-test.json"))));
  Check(restored.TotalNet==e.TotalNet&&restored.Day==1&&restored.State==e.State,"JSON checkpoint restores summary and cumulative net");
  session.drivingCamera.SetFirstPerson(true);yield return new WaitForSecondsRealtime(.1f);Check(!session.mirrors.Visible&&!session.assist.Visible,"summary hides mirror and assist overlays");
  yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"economy-summary.png"));yield return new WaitForSecondsRealtime(.5f);
  Check(b.NextDay()&&e.Day==2&&e.Income==0&&e.Rides==0&&e.FuelCost==0&&Time.timeScale==1,"next day resets daily counters and resumes driving");
  Move(1);yield return new WaitForSeconds(.7f);Check(j.Interact()&&j.trip.State==TripState.OnTrip,"roadside passenger on next day");
  Move(1);Check(b.RequestEndDay(),"manual stationary close accepted");j.trip.Tick(10000);yield return new WaitForSecondsRealtime(.15f);
  Check(e.State==BusinessState.Lost&&e.Last.failedTrips==1&&e.Last.income==0&&!b.NextDay(),"failed final passenger settles to deadline loss");
  yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"economy-failure.png"));yield return new WaitForSecondsRealtime(.4f);
  b.RestartCampaign();Check(b.ledger.Day==1&&b.ledger.TotalNet==0&&Time.timeScale==1,"new challenge starts after terminal result");
  e=b.ledger;e.RecordFare(110000);Move(1);Check(b.RequestEndDay(),"manual empty close");yield return new WaitForSecondsRealtime(.1f);Check(e.State==BusinessState.Won&&e.TotalNet>=e.Settings.target,"net goal victory after expenses");
  yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"economy-success.png"));yield return new WaitForSecondsRealtime(.4f);
  using(var locked=new FileStream(b.SavePath+".tmp",FileMode.Create,FileAccess.Write,FileShare.None)){
   Check(!b.SaveCheckpoint()&&b.SaveFailed,"failed terminal save reports retryable error");
   b.RestartCampaign();Check(b.ledger==e&&e.State==BusinessState.Won&&Time.timeScale==0,"failed restart save preserves terminal ledger");
  }
  Check(b.SaveCheckpoint()&&!b.SaveFailed,"terminal save can retry after storage recovers");
  var disk=EconomyLedger.Restore(JsonUtility.FromJson<EconomySnapshot>(File.ReadAllText(b.SavePath)));
  Check(disk.State==BusinessState.Won&&disk.TotalNet==e.TotalNet&&File.Exists(b.SavePath+".bak"),"atomic checkpoint and backup written to test-only storage");
  b.RestartCampaign();e=b.ledger;Check(j.Accept(1),"pickup call accepted");e.Advance(e.SecondsLeft,0);Move(j.current.pickup);Check(!j.Interact(),"expired shift cannot board pending pickup");
  yield return new WaitForSecondsRealtime(.1f);Check(b.ShowingSummary&&j.trip.State==TripState.Idle&&e.Rides==0&&e.FailedTrips==0,"unboarded call cancelled without passenger failure");
  b.NextDay();t.body.velocity=t.transform.forward*4;Check(!b.RequestEndDay()&&e.CanAccept,"manual close rejected while moving");t.body.velocity=Vector3.zero;
  yield return new WaitForSeconds(.7f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"economy-driving.png"));yield return new WaitForSeconds(.4f);
  Check((File.Exists(playerSave)?File.ReadAllText(playerSave):null)==saved,"test never changes player checkpoint");
  results.Insert(0,"success="+ok+" checks="+checks+"\n");File.WriteAllText(Path.Combine(folder,"economy-smoke.txt"),results.ToString());Debug.Log(results.ToString());Application.Quit(ok?0:1);
 }
}
