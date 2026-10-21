using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
public class RemainingPrototypeTest:MonoBehaviour {
 DriveSession s;bool ok=true;StringBuilder report=new StringBuilder();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-remainingPrototypeTest")>=0)new GameObject("Remaining prototype test").AddComponent<RemainingPrototypeTest>();}
 void Check(bool v,string text){ok&=v;report.AppendLine((v?"PASS ":"FAIL ")+text);Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/remaining-prototype-test.txt",report.ToString());}
 void Pose(Vector3 p,Vector3 dir){s.taxi.body.position=p+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(dir);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();}
 void At(int i){var p=s.jobs.stops[i];Pose(p.Kerb,p.Direction);}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);s=FindObjectOfType<DriveSession>();s.business.enabledCampaign=true;s.business.Depart();s.taxi.enabled=false;s.taxi.body.isKinematic=true;s.taxi.body.interpolation=RigidbodyInterpolation.None;s.taxi.body.detectCollisions=false;
  var zones=s.GetComponent<ServiceLawZones>();foreach(var course in zones.scenarios){if(course.beforePickup)continue;s.jobs.ClearForDay();At(course.pickup);var o=new JobOffer{fare=5000,seconds=2000,requiredScore=60};Check(zones.ConfigureScenario(o,course),course.id+" offer");s.jobs.offers[0]=o;Check(s.jobs.Accept(0)&&s.jobs.Interact(),course.id+" board");Check(s.jobs.nav.RequiredVia==-1,course.id+" no forced pickup/delivery situation waypoint");bool proposal=false;for(int target=0;target<s.jobs.stops.Length;target++){float extra,remaining;int fare;if(s.jobs.PreviewPassengerChange(target,false,out extra,out fare,out remaining)){proposal=true;break;}}Check(proposal,course.id+" unvisited situation does not block passenger destination request");int before=s.law.ledger.TotalScore;At(course.drop);Check(s.jobs.Interact()&&s.jobs.trip.State==TripState.Complete,course.id+" skip scenario and deliver");Check(s.law.ledger.TotalScore==before,course.id+" unvisited gives no reward or penalty");Check(!s.jobs.Interact(),"no duplicate payment");}
  s.jobs.ClearForDay();At(0);var offer=new JobOffer{pickup=0,drop=1,distance=1000,seconds=300};float fuel=s.jobs.EstimatedFuel(offer);Check(Mathf.Abs(fuel-4.5f)<.01f,"pickup at same stop: 1km plus five minute idle estimate");
  var services=s.services;var station=services.Stations[0];Pose(station.pump,station.direction);s.business.ledger.Account.Consume(0,10000);services.open=true;float beforeFuel=s.business.ledger.Account.Fuel;int cash=s.business.ledger.Account.Cash;
  Check(services.FuelAmount==10,"single ten percentage point refill");Check(services.Buy(0),"refuel directly at pump");Check(Mathf.Abs(s.business.ledger.Account.Fuel-(beforeFuel+10-s.business.settings.idlePerMinute*10/60))<.01f,"ten points minus service idle consumption");Check(s.business.ledger.Account.Cash==cash-400&&services.open,"400 won and menu remains open for another ten points");Check(services.Buy(0),"repeat purchase without reopening");
  s.business.ledger.Account.Refuel(s.law.ids.Next("fixture"),100,1);Check(services.FuelAmount==0&&!services.Buy(0),"full tank cannot charge again");services.open=false;Time.timeScale=1;s.paused=false;
  var choice=s.GetComponent<ScenarioChoiceSituation>();choice.Begin(zones.scenarios.Find(c=>c.id=="PS08"));Check(choice.CanOfferSaved(),"first exposure allowed");choice.SendMessage("RecordExposure");Check(!choice.CanOfferSaved(),"repeat exposure suppressed");var ids=s.law.ids;var snapshot=JsonUtility.FromJson<DrivingScoreSnapshot>(JsonUtility.ToJson(new DrivingScoreSnapshot{campaign=ids.campaign,sequence=ids.sequence,events=s.law.ledger.Events}));s.law.ledger=new LawLedger();Check(DrivingScoreJournal.Restore(snapshot,ids,s.law.ledger)&&!choice.CanOfferSaved(),"exposure restored through score journal");s.law.ledger=new LawLedger();Check(choice.CanOfferSaved(),"new campaign ledger resets exposure");choice.Stop();
  Application.Quit(ok?0:1);
 }
}
