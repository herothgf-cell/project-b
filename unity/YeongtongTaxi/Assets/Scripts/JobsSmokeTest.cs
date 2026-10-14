using System.Collections;
using System.IO;
using UnityEngine;
public class JobsSmokeTest : MonoBehaviour {
 public DriveSession session;
 void Move(int stop){var t=session.taxi;t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=session.jobs.stops[stop].Kerb+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(session.jobs.stops[stop].Direction);Physics.SyncTransforms();session.manners.Suppress(1);}
 IEnumerator Start(){yield return new WaitForSeconds(2);var j=session.jobs;var t=session.taxi;t.automated=true;
  bool connected=true;float longest=0;foreach(var a in j.stops)foreach(var b in j.stops){float d=j.nav.Distance(a.V(),b.V());connected&=!float.IsInfinity(d);longest=Mathf.Max(longest,d);}
  bool accept=j.Accept(0);bool reject=!j.Accept(1);Move(j.current.pickup);yield return new WaitForSeconds(.6f);bool board=j.Interact()&&j.trip.State==TripState.OnTrip;
  var original=j.nav.path.Count;Move((j.current.drop+2)%j.stops.Length);j.nav.Refresh();bool reroute=j.nav.path.Count>1;Move(j.current.drop);yield return new WaitForSeconds(.6f);int fare=j.current.fare;bool paid=j.Interact()&&j.balance==fare;bool once=!j.Interact()&&j.balance==fare;
  yield return new WaitForSeconds(4.1f);Move(1);yield return new WaitForSeconds(.6f);bool street=j.Interact()&&j.trip.State==TripState.OnTrip;
  int earned=j.balance;j.trip.Tick(10000);yield return null;bool failure=j.trip.State==TripState.Failed&&j.balance==earned;
  yield return new WaitForSeconds(4.1f);j.Accept(1);Move(j.current.pickup);yield return new WaitForSeconds(.6f);j.Interact();
  Time.timeScale=0;float remaining=j.trip.Remaining;yield return new WaitForSecondsRealtime(.3f);bool pause=j.trip.Remaining==remaining;Time.timeScale=1;
  session.signals.Clear();session.signals.Toggle(-1);yield return null;yield return null;bool left=session.assist.Mode=="LEFT CAMERA";
  session.signals.Toggle(1);yield return null;yield return null;bool right=session.assist.Mode=="RIGHT CAMERA";
  t.body.velocity=-t.transform.forward;yield return null;yield return null;bool rear=session.assist.Mode=="REAR CAMERA";
  t.body.velocity=Vector3.zero;session.signals.Clear();yield return null;yield return null;bool off=session.assist.Mode=="";
  session.drivingCamera.SetFirstPerson(false);session.signals.Toggle(-1);yield return new WaitForSeconds(.5f);
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
  yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"jobs-navigation.png"));yield return new WaitForSeconds(.6f);
  session.drivingCamera.SetFirstPerson(true);session.signals.Toggle(1);yield return new WaitForSeconds(.5f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"jobs-side-camera.png"));yield return new WaitForSeconds(.6f);
  bool ok=connected&&longest>450&&accept&&reject&&board&&reroute&&paid&&once&&street&&failure&&pause&&left&&right&&rear&&off;
  string result="success="+ok+" connected="+connected+" longestRoute="+longest+" accept="+accept+" singleJob="+reject+" board="+board+" reroute="+reroute+" paid="+paid+" noDuplicate="+once+" street="+street+" timeout="+failure+" pause="+pause+" autoCameras="+(left&&right&&rear&&off)+" cameras="+left+","+right+","+rear+","+off;
  File.WriteAllText(Path.Combine(folder,"jobs-smoke.txt"),result);Debug.Log(result);Application.Quit(ok?0:1);
 }
}
