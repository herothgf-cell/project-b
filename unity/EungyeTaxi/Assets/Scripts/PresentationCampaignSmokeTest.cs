using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
// Position-controlled UI/interaction integration; not a manual or physical campaign run.
public sealed class PresentationCampaignSmokeTest:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-presentationHudTest")>=0)new GameObject("HUD and receipt checks").AddComponent<PresentationCampaignSmokeTest>();}
 DriveSession s;bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string name){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+name);Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/presentation-hud-test.txt",report.ToString());}
 void Pose(MapStop stop){s.taxi.body.velocity=s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=stop.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(stop.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();s.law.ResetObservation();s.manners.Suppress(2);}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);s=FindObjectOfType<DriveSession>();s.business.Depart();s.paused=false;Time.timeScale=1;
  Check(!s.assisted.Active,"startup is manual");Check(!s.jobs.nav.active&&!s.jobs.nav.HasDestination&&s.jobs.nav.path.Count==0,"startup has no automatic navigation route");Check(!s.GetComponent<DriveHUD>().HelpOpen,"help starts collapsed");
  s.taxi.automated=true;s.assisted.allowAutomated=true;foreach(var c in s.traffic.cars){c.Recycle();c.enabled=false;}
  s.signals.Clear();s.signals.ToggleHazards();Check(s.signals.Hazards&&s.signals.direction==0,"G hazard mode keeps no turn intent");s.signals.ToggleHazards();Check(!s.signals.Hazards,"manual hazard toggles off");
  var jobs=s.jobs;jobs.ClearForDay();var offer=new JobOffer{pickup=0,drop=3,fare=5000,seconds=2000,requiredScore=70,distance=jobs.nav.Distance(jobs.stops[0],jobs.stops[3])};jobs.offers[0]=offer;Pose(jobs.stops[0]);Check(jobs.Accept(0)&&jobs.Interact()&&jobs.trip.State==TripState.OnTrip,"controlled passenger boarding");Check(s.signals.Hazards,"boarding turns both hazard sides on");Check(jobs.nav.HasDestination,"passenger destination enables navigation");s.requests.request.DeferSafety();Pose(jobs.stops[jobs.current.drop]);Check(jobs.Interact()&&jobs.trip.State==TripState.Complete,"controlled destination drop-off");Check(s.signals.Hazards,"drop-off turns hazards on");
  var panel=s.GetComponent<RideCompletionPanel>();int income=s.business.ledger.Income,balance=jobs.balance;Check(panel.ShowLast()&&panel.Open&&s.RideResultOpen&&s.FullMapOpen&&Time.timeScale==0,"receipt pauses timing and blocks other modes");Check(panel.Result.paid==jobs.RequestReward.Total&&panel.Receipt.Contains("최초 예상 운임")&&panel.Receipt.Contains("감액")&&panel.Receipt.Contains("보너스")&&panel.Receipt.Contains("실수령"),"receipt separates fare change, deduction, bonus and actual income");Check(!jobs.Accept(0),"receipt blocks new call");panel.Close();Check(!panel.Open&&!s.paused&&Time.timeScale>0,"continue closes receipt and resumes");Check(s.business.ledger.Income==income&&jobs.balance==balance,"closing receipt never pays twice");
  s.taxi.body.velocity=s.taxi.transform.forward*2;s.signals.SendMessage("Update");s.taxi.body.velocity=Vector3.zero;Check(!s.signals.Hazards,"automatic stop hazards clear after driving away");
  jobs.nav.Clear();Check(s.assisted.SetActive(true),"H can explicitly assist without destination");Check(jobs.nav.freeGuidance&&!jobs.nav.HasDestination,"H internal lane guidance does not become destination");var line=jobs.GetComponentInChildren<LineRenderer>();Check(line==null||line.positionCount==0,"free H route is not drawn in world");s.assisted.SetActive(false);jobs.nav.Clear();s.jobs.nav.ObserveEdge(0);Check(!jobs.nav.HasDestination&&jobs.nav.RequiredVia<0,"law observation never becomes navigation goal or waypoint");
  Debug.Log(report.ToString());Application.Quit(ok?0:1);
 }
}
