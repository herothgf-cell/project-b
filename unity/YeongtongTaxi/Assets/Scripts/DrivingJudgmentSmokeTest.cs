using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Controlled observations isolate judgment boundaries; live physical journeys run separately.
public class DrivingJudgmentSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string name){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+name);File.WriteAllText("artifacts/driving-judgment-test.txt","success="+ok+"\n"+report);}
 void Pose(Vector3 front,Vector3 heading){var p=front-heading*2+Vector3.up*.65f;var t=session.taxi;t.body.velocity=t.body.angularVelocity=Vector3.zero;t.body.position=p;t.body.rotation=Quaternion.LookRotation(heading);t.transform.SetPositionAndRotation(p,t.body.rotation);Physics.SyncTransforms();}
 IEnumerator Cross(TrafficJunction j,Vector3 heading){
  session.law.ResetObservation();session.law.ledger=new LawLedger();session.manners.Suppress(0);
  var line=j.center-Vector3.forward*j.StopDistance(0)+Vector3.right*2;
  Pose(line-Vector3.forward*.5f,heading);yield return new WaitForFixedUpdate();yield return null;
  var before=session.taxi.transform.position+heading*2;
  Pose(line+Vector3.forward*.7f,heading);session.manners.CheckSignal(before,line+Vector3.forward*.7f);yield return new WaitForFixedUpdate();yield return null;
  for(float z=-j.StopDistance(0)+1;z<=j.StopDistance(0)+4;z+=.5f){Pose(j.center+Vector3.forward*z+Vector3.right*2,heading);yield return new WaitForFixedUpdate();yield return null;}
 }
 IEnumerator BrakeAtCrossing(TrafficJunction j,bool hazard){
  Pose(j.center+Vector3.right*2-Vector3.forward*2,Vector3.right);session.taxi.body.velocity=Vector3.right*12;session.manners.Suppress(0);
  j.rightTurnObservation=new RoadObservation{known=true,pedestrian=hazard?PedestrianState.Crossing:PedestrianState.None};int before=session.manners.brakeEvents;
  for(int i=0;i<28;i++){session.taxi.body.velocity=Vector3.right*Mathf.Max(3,12-(i+1)*.45f);session.manners.SendMessage("FixedUpdate");yield return new WaitForFixedUpdate();}
  Check(hazard?session.manners.brakeEvents==before:session.manners.brakeEvents>before,hazard?"pedestrian braking does not deduct passenger manner":"unnecessary harsh braking still deducts passenger manner");
 }
 IEnumerator Start(){
  yield return new WaitForSeconds(1);session.taxi.automated=true;session.taxi.enabled=false;session.manners.enabled=false;
  foreach(var car in session.traffic.cars){car.Recycle();car.enabled=false;}
  var traffic=session.traffic;var j=new TrafficJunction{name="judgment-fixture",center=new Vector3(5000,0,5000),direction=Vector3.forward,scenarioSignal=true,scenarioForward=SignalColor.Red,observationAxis=0,observationSign=1};traffic.junctions.Add(j);
  int red=session.manners.redEvents;yield return Cross(j,Vector3.right);
  Check(session.manners.redEvents==red,"perpendicular heading does not receive another approach red penalty");
  Check(session.law.ledger.Events.Length==0,"perpendicular drift does not create another approach law event");
  yield return Cross(j,Vector3.forward);Check(session.law.ledger.Events.Length==1&&session.law.ledger.Events[0].Deduction>0,"real forward red entry still recorded");
  j.scenarioForward=SignalColor.Green;yield return Cross(j,Vector3.forward);Check(session.law.ledger.Events.Length==1&&session.law.ledger.Events[0].Deduction==0,"forward green remains compliant");
  int yellowRed=session.manners.redEvents;j.scenarioForward=SignalColor.Yellow;yield return Cross(j,Vector3.forward);
  Check(session.manners.redEvents==yellowRed,"yellow entry is not mislabeled as red passenger penalty");
  Check(session.law.ledger.Events.Length==1&&session.law.ledger.Events[0].rules[0].verdict==LawVerdict.Deferred,"yellow timing judgment remains deferred, not compliant");j.scenarioForward=SignalColor.Green;
  var ai=traffic.cars[0];var route=new TrafficRoute{axis=0,turn=0};route.SetPoints(new[]{j.center-Vector3.forward*60+Vector3.right*2,j.center+Vector3.forward*60+Vector3.right*2});ai.Continue(route);
  var stop=new TrafficStop{junction=j,axis=0,turn=0,distance=45,exitDistance=80};route.stops.Add(stop);traffic.manualClock=true;
  for(int t=0;t<150;t++)if(KoreanSignalRules.Signal(t,j.leftTurnMode,0,1)==SignalColor.Red){traffic.manualTime=t;break;}
  j.scenarioForward=SignalColor.Green;Check(traffic.CanEnter(ai,stop),"AI follows displayed scenario green even when underlying clock is red");
  for(int t=0;t<150;t++)if(KoreanSignalRules.Signal(t,j.leftTurnMode,0,1)==SignalColor.Green){traffic.manualTime=t;break;}
  j.scenarioForward=SignalColor.Red;Check(!traffic.CanEnter(ai,stop),"AI waits at displayed scenario red even when underlying clock is green");
  j.scenarioForward=SignalColor.Green;j.rightTurnObservation=new RoadObservation{known=true,pedestrian=PedestrianState.Crossing};stop.turn=-1;
  Check(!traffic.CanEnter(ai,stop),"scenario left turn still yields to crossing pedestrians");stop.turn=0;
  // Required braking near a marked pedestrian crossing must remain recognized after the entry line.
  Pose(j.center-Vector3.forward*j.StopDistance(0)+Vector3.right*2,Vector3.forward);j.scenarioForward=SignalColor.Green;
  j.rightTurnObservation=new RoadObservation{known=true,dedicated=false,pedestrian=PedestrianState.Crossing};
  Check(session.manners.NecessaryBraking(),"pedestrian required braking before line");
  Pose(j.center-Vector3.forward*2+Vector3.right*2,Vector3.right);Check(session.manners.NecessaryBraking(),"pedestrian required braking at outgoing crossing, after entry line");
  yield return BrakeAtCrossing(j,true);yield return BrakeAtCrossing(j,false);
  j.rightTurnObservation=null;traffic.junctions.Remove(j);Pose(new Vector3(5100,0,5100),Vector3.forward);
  Check(!session.manners.NecessaryBraking(),"unobstructed road has no required braking exception");
  report.AppendLine("DONE controlled judgment boundaries; not manual driving.");File.WriteAllText("artifacts/driving-judgment-test.txt","success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
