using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Position fixtures test the production observers at actual rendered markings.
// These are deliberately labelled separately from Rigidbody driving tests.
public class ContentFlowSmokeTest : MonoBehaviour {
 public DriveSession session; bool ok=true; readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string message){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+message);Debug.Log(message+" = "+value);}
 void Pose(Vector3 front,Vector3 forward){var t=session.taxi;var p=front-forward*2+Vector3.up*.65f;t.transform.SetPositionAndRotation(p,Quaternion.LookRotation(forward));t.body.position=p;t.body.rotation=t.transform.rotation;Physics.SyncTransforms();}
 IEnumerator Move(Vector3 a,Vector3 b,Vector3 forward){int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.5f);for(int i=1;i<=count;i++){Pose(Vector3.Lerp(a,b,i/(float)count),forward);yield return new WaitForFixedUpdate();}}
 LawEvent Last(string family){var events=session.law.ledger.Events;for(int i=events.Length-1;i>=0;i--)if(events[i].family==family)return events[i];return null;}
 bool Finding(LawEvent e,string rule,LawVerdict verdict){return e!=null&&e.rules.Exists(r=>r.rule==rule&&r.verdict==verdict);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);Time.timeScale=5;session.business.enabledCampaign=false;session.taxi.automated=true;session.taxi.enabled=false;session.taxi.body.isKinematic=true;session.roadConditions.enabled=false;
  foreach(var c in session.traffic.cars){c.Recycle();c.enabled=false;}
  var right=GetComponent<RightTurnSituation>();var j=right.junction;var dir=session.traffic.AxisDirection(j,0);var side=TrafficSystem.Right(dir);
  Pose(j.center-dir*150+side*2,dir);Check(right.Select(5),"R5 can be prepared outside approach");yield return new WaitForSeconds(13);
  Check(j.rightTurnObservation.pedestrian==PedestrianState.Intending,"R5 pedestrian waits for driver approach instead of expiring remotely");Pose(j.center-dir*80+side*2,dir);yield return new WaitForSeconds(13);Check(j.rightTurnObservation.pedestrian==PedestrianState.None,"R5 releases after driver enters approach");right.Stop();right.enabled=false;
  for(int test=0;test<8;test++){
   session.law.ledger=new LawLedger();session.law.ResetObservation();
   var o=new RoadObservation{known=true,trafficClear=true,forward=test<2?SignalColor.Red:SignalColor.Green,dedicated=test==2||test==3,right=test==2?SignalColor.Red:SignalColor.Green,pedestrian=test==7?PedestrianState.Intending:test==4||test==5?PedestrianState.Crossing:PedestrianState.None};
   j.scenarioSignal=true;j.scenarioForward=o.forward;j.observationAxis=0;j.observationSign=1;j.rightTurnObservation=o;
   var start=j.center-dir*(j.StopDistance(0)+4)+side*2;Pose(start,dir);yield return new WaitForFixedUpdate();
   var stop=j.center-dir*(j.StopDistance(0)+1)+side*2;
   // All but test 1 stop before the marking. Test 1 crosses the three-metre zone in one sampled step.
   if(test!=1){yield return Move(start,stop,dir);yield return new WaitForFixedUpdate();}else{stop=start;}
   var entry=j.center-dir*(j.StopDistance(0)-.4f)+side*2;Pose(entry,dir);yield return new WaitForFixedUpdate();
   var corner=j.center-dir*2+side*2;yield return Move(entry,corner,dir);
   if(test==5){o.pedestrian=PedestrianState.None;yield return new WaitForFixedUpdate();}
   var before=j.center-dir*2+side*(j.halfWidth[0]+.4f);yield return Move(corner,before,side);
   Check(Last("right")==null,"no passage verdict before crosswalk leading edge case "+test);
   var across=j.center-dir*2+side*(j.halfWidth[0]+.8f);yield return Move(before,across,side);
   var e=Last("right");Check(e!=null,"passage observed at rendered crosswalk near edge case "+test);
   if(test==0)Check(Finding(e,"L02",LawVerdict.Compliant),"red approach stop preserved");
   if(test==1)Check(Finding(e,"L02",LawVerdict.Violation),"rolling red approach detected");
   if(test==2)Check(Finding(e,"L03",LawVerdict.Violation),"dedicated red detected");
   if(test==3)Check(Finding(e,"L03",LawVerdict.Compliant),"dedicated green accepted");
   if(test==4)Check(Finding(e,"L04",LawVerdict.Violation),"pedestrian at actual crossing detected before it disappears");
   if(test==5)Check(Finding(e,"L04",LawVerdict.Compliant),"wait for pedestrian then passage accepted");
   if(test==6)Check(e!=null&&e.Deduction==0,"R2 green vehicle signal and no pedestrian accepted");
   if(test==7)Check(Finding(e,"L04",LawVerdict.Violation),"R5 intending pedestrian protected at crossing");
   yield return Move(across,across+side*6,side);Check(session.law.ledger.Events.Length==1,"one passage one event case "+test);
  }
  for(int overshoot=0;overshoot<2;overshoot++){
   session.law.ledger=new LawLedger();session.law.ResetObservation();var o=new RoadObservation{known=true,trafficClear=true,forward=SignalColor.Red,pedestrian=PedestrianState.None};j.rightTurnObservation=o;j.scenarioForward=SignalColor.Red;
   var line=j.center-dir*j.StopDistance(0)+side*2;Pose(line-dir,dir);yield return new WaitForFixedUpdate();Pose(line+dir*(overshoot==0?.001f:.5f),dir);yield return new WaitForFixedUpdate();
   j.scenarioForward=SignalColor.Green;o.forward=SignalColor.Green;
   yield return Move(line+dir*(overshoot==0?.001f:.5f),j.center+dir*(j.StopDistance(0)+4)+side*2,dir);
   Check(Finding(Last("signal"),"L01",overshoot==0?LawVerdict.Compliant:LawVerdict.Violation),overshoot==0?"stationary stop-line jitter must not latch stale red":"real red entry remains violation after later green");
  }
  j.scenarioSignal=false;j.rightTurnObservation=null;
  File.WriteAllText(Path.GetFullPath("artifacts/content-flow-test.txt"),"success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
