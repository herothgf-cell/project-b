using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Position-controlled contract checks. This intentionally does NOT claim a physical or manual completion.
public sealed class PrototypeScenarioProbe : MonoBehaviour {
 DriveSession session;readonly StringBuilder report=new StringBuilder();bool ok=true;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-prototypeScenarioTest")>=0)new GameObject("Prototype scenario probe").AddComponent<PrototypeScenarioProbe>();}
 void Check(bool value,string text){ok&=value;string line=(value?"PASS ":"FAIL ")+text;report.AppendLine(line);Debug.Log(line);Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/prototype-scenario-test.txt",report.ToString());}
 void Pose(Vector3 p,Vector3 direction){var t=session.taxi;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(direction);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();}
 void AtStop(int index){var s=session.jobs.stops[index];Pose(s.Kerb,s.Direction);}
 void PassVia(){var nav=session.jobs.nav;var r=nav.hdRoute;int via=nav.ObservedEdge,k=r==null?-1:r.edges.IndexOf(via);Check(k>=0,"observed connector exists on ordinary navigation");if(k<0)return;float end=r.ends[k];for(float at=end-2;at<end+2;at+=.2f){Pose(r.path.Position(at),r.path.Tangent(at));nav.SendMessage("FixedUpdate");}Check(nav.PassedObservedEdge==via,"continuous position samples pass connector "+via);}
 IEnumerator Start(){var run=Run();float deadline=Time.realtimeSinceStartup+90;while(true){object next=null;bool more=false,failed=false;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){Check(false,e.ToString());Application.Quit(1);failed=true;}if(failed||!more)yield break;if(Time.realtimeSinceStartup>deadline){Check(false,"scenario probe timeout");Application.Quit(1);yield break;}yield return next;}}
 IEnumerator Run(){yield return new WaitForSeconds(1);session=FindObjectOfType<DriveSession>();var jobs=session.jobs;var zones=session.GetComponent<ServiceLawZones>();session.taxi.enabled=false;session.taxi.body.isKinematic=true;session.taxi.body.interpolation=RigidbodyInterpolation.None;session.business.enabledCampaign=false;jobs.enabled=false;jobs.nav.enabled=false;zones.enabled=false;session.law.enabled=false;foreach(var c in session.traffic.cars){c.Recycle();c.enabled=false;}
  Check(zones.scenarios.Count==9,"nine normal Eungye courses registered");
  foreach(var scenario in zones.scenarios){int attempts=scenario.id=="PS08"?2:1;for(int attempt=0;attempt<attempts;attempt++){
   jobs.ClearForDay();session.law.ledger=new LawLedger();var start=scenario.route.path;Pose(start.Position(0),start.Tangent(0));var offer=new JobOffer{fare=5000,seconds=2000,requiredScore=60};bool configured=zones.ConfigureScenario(offer,scenario);Check(configured,scenario.id+" reachable offer at authored approach");if(!configured){Application.Quit(1);yield break;}jobs.offers[0]=offer;Check(jobs.Accept(0),scenario.id+" accept");string accepted=jobs.OfferId;Check(jobs.trip.PickupRemaining>=180,"pickup deadline separate "+scenario.id);
   if(scenario.beforePickup){AtStop(offer.pickup);Check(!jobs.Interact(),scenario.id+" pickup cannot skip initial situation");Pose(start.Position(0),start.Tangent(0));jobs.nav.Refresh();PassVia();zones.SendMessage("Update");Check(zones.Completed,"pre-pickup mandatory route completed");bool empty=false;foreach(var e in session.law.ledger.Events)if(e.tripId==accepted&&!e.occupied)empty=true;Check(empty,"pre-pickup observation remains empty-cab event");}
   Check(jobs.nav.RequiredVia==-1,scenario.id+" pickup has no situation waypoint");AtStop(offer.pickup);Check(jobs.Interact()&&jobs.trip.State==TripState.OnTrip,scenario.id+" board");Check(jobs.nav.RequiredVia==-1,scenario.id+" delivery has no situation waypoint");var choice=session.GetComponent<ScenarioChoiceSituation>();if(choice&&choice.Course!=null)choice.enabled=false;
   if(!scenario.beforePickup){Pose(start.Position(0),start.Tangent(0));jobs.nav.Refresh();
    if(scenario.id=="PS08"||scenario.id=="PS09"){
     var r=jobs.nav.hdRoute;Check(r!=null,scenario.id+" live navigation after reset pose");if(r==null){Application.Quit(1);yield break;}int k=r.edges.IndexOf(scenario.connector);Check(k>0,scenario.id+" natural situation connector in navigation");if(k<1){Application.Quit(1);yield break;}float approach=scenario.id=="PS08"?0:Mathf.Max(0,r.ends[k-1]-180);Pose(r.path.Position(approach),r.path.Tangent(approach));choice.SendMessage("Update");
     if(choice.Open){float remaining=jobs.trip.Remaining,pickup=jobs.trip.PickupRemaining;Check(Time.timeScale==0&&session.paused,"plan pauses simulation");yield return new WaitForSecondsRealtime(.05f);Check(jobs.trip.Remaining==remaining&&jobs.trip.PickupRemaining==pickup,"plan leaves timers unchanged");if(scenario.id=="PS08"&&attempt==1){bool can=choice.CanDetour();Check(can,"detour preflight "+choice.DetourDiagnostic);}Check(choice.Select(scenario.id=="PS08"?attempt:0),scenario.id+" plan selection "+attempt+" "+choice.DetourDiagnostic);Check(Time.timeScale>0&&!session.paused,"plan resumes same session");}
     else if(scenario.id=="PS08"&&attempt==1){Check(false,"PS08 bypass plan exposed before branch");}
     if(scenario.id=="PS08"&&attempt==1){Check(jobs.nav.AvoidEdge==scenario.connector&&jobs.nav.hdRoute!=null&&!jobs.nav.hdRoute.edges.Contains(scenario.connector),"detour excludes blocked connector");jobs.nav.TogglePreference();Check(jobs.nav.hdRoute!=null&&!jobs.nav.hdRoute.edges.Contains(scenario.connector),"Tab preference preserves excluded connector");}
    }
    if(scenario.id=="PS09"){
     var nav=jobs.nav;var r=nav.hdRoute;int k=r.edges.IndexOf(scenario.connector);Pose(r.path.Position(r.ends[k-1]-80),r.path.Tangent(r.ends[k-1]-80));choice.SendMessage("Update");
     var field=typeof(ScenarioChoiceSituation).GetField("pedestrianState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);var state=(RightPracticeState)field.GetValue(choice);Check(state!=null,"PS09 approaching creates pedestrian condition");
     if(state!=null){
      bool allowed=false;for(int target=0;target<jobs.stops.Length;target++){float extra,remaining;int fare;if(jobs.PreviewPassengerChange(target,false,out extra,out fare,out remaining))allowed=true;}Check(!allowed,"active PS09 pedestrian fixture defers destination changes");
      var j=session.traffic.junctions.Find(x=>x.name==nav.hd.edges[scenario.connector].ruleId);var incoming=nav.hd.paths[r.edges[k-1]];var direction=incoming.Tangent(incoming.length-2);int axis=Mathf.Abs(Vector3.Dot(direction,session.traffic.AxisDirection(j,0)))>.7f?0:1;int sign=session.traffic.Sign(direction,axis,j);direction=session.traffic.AxisDirection(j,axis)*sign;
      Pose(j.center-direction*(j.StopDistance(axis)+2.5f),direction);choice.SendMessage("Update");Check(state.Started,"PS09 real decision-point stop starts crossing");
      Pose(nav.hd.paths[scenario.connector].Position(2),nav.hd.paths[scenario.connector].Tangent(2));nav.TogglePreference();Check(nav.hdRoute!=null&&nav.hdRoute.edges[0]==scenario.connector,"Tab reroutes with current right connector first");float elapsed=state.Elapsed;
      for(int tick=0;tick<8;tick++){yield return new WaitForSecondsRealtime(.03f);choice.SendMessage("Update");}
      Check(state.Elapsed>elapsed,"pedestrian continues crossing after connector-first reroute");
     }
    }
    PassVia();zones.SendMessage("Update");if(choice&&choice.Course!=null)choice.SendMessage("Update");
   }
   AtStop(offer.drop);Check(jobs.Interact()&&jobs.trip.State==TripState.Complete,scenario.id+" actual delivery interaction and payment after course "+attempt);
  }}
  jobs.ClearForDay();jobs.offers[0]=new JobOffer{pickup=0,drop=1,fare=5000,seconds=90,pickupSeconds=1};AtStop(0);Check(jobs.Accept(0),"pickup expiry fixture accept");jobs.trip.Tick(1);Check(jobs.trip.PickupExpired&&!jobs.trip.Board(),"expired empty pickup never boards");jobs.SendMessage("Update");Check(!jobs.nav.active,"expired pickup removes navigation");
  Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/prototype-scenario-test.txt",report.ToString());Debug.Log(report.ToString());Application.Quit(ok?0:1);
 }
}
