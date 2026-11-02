using System;
using System.Reflection;
using UnityEngine;

public static class P1P2CheckpointLawTests {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception("P1P2_CHECKPOINT_LAW: "+message);}
 static void Reject(Action action,string message){bool failed=false;try{action();}catch(ArgumentException){failed=true;}Check(failed,message);}
 public static void Run(){
  checks=0;
  Check(DrivingLaw.SlowTurnZoneEnabled("eungye","J7841_867"),"authored zone");
  Check(!DrivingLaw.SlowTurnZoneEnabled("eungye","unknown")&&!DrivingLaw.SlowTurnZoneEnabled("other","J7841_867"),"no invented zone");
  var objects=new GameObject("checkpoint law fixture");
  try{
   var session=objects.AddComponent<DriveSession>();session.enabled=false;
   session.traffic=objects.AddComponent<TrafficSystem>();session.traffic.enabled=false;
   session.taxi=objects.AddComponent<TaxiController>();session.taxi.enabled=false;
   var law=objects.AddComponent<DrivingLaw>();law.enabled=false;law.session=session;
   session.traffic.junctions.Add(new TrafficJunction{name="J7841_867"});
   var state=new SlowRightTurnState();state.Observe(.4f,18,true,true,false);
   law.RestoreObservation(new DrivingLawSnapshot{entry=new LawEntrySnapshot{junction="J7841_867",id="one-event",axis=0,sign=1,direction=Vector3.forward,observed=new RoadObservation{known=true},slowPresent=true,slow=state,necessaryBefore=2}});
   var saved=JsonUtility.FromJson<DrivingLawSnapshot>(JsonUtility.ToJson(law.CaptureObservation()));
   Check(saved.entry.slowPresent&&saved.entry.slow.continuousExcess==.4f&&saved.entry.necessaryBefore==2,"actual law Json checkpoint retains partial turn");
   saved.entry.slow.Observe(.4f,18,true,true,false);law.RestoreObservation(saved);
   var incident=new LawEvent{id="one-event",day=1,reason="fixture"};incident.Add("signal",LawVerdict.Violation,12);
   typeof(DrivingLaw).GetMethod("AddSlowTurnFinding",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(law,new object[]{incident});
   Check(incident.rules.Count==2&&incident.rules[1].verdict==LawVerdict.Violation,"runtime incident includes sustained turn evidence");
   var ledger=new LawLedger();ledger.RecordIncident(incident);int score=ledger.TotalScore;ledger.RecordIncident(incident);
   Check(ledger.TotalScore==score&&score==88&&ledger.Count==1,"same incident max penalty and duplicate neutral");
   saved.entry.slow.necessary=true;law.RestoreObservation(saved);var safe=new LawEvent{id="necessary",reason="fixture"};
   typeof(DrivingLaw).GetMethod("AddSlowTurnFinding",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(law,new object[]{safe});
   Check(safe.rules[0].verdict==LawVerdict.Deferred,"necessary avoidance neutral");
   saved.entry.slowPresent=false;law.RestoreObservation(saved);Check(law.CaptureObservation().entry.slow==null,"legacy absent slow state ignored");
   var signals=objects.AddComponent<TaxiSignals>();signals.Toggle(1);signals.ObserveManualMotion(4,40,.5f,Vector3.forward,.1f,false);
   var copy=JsonUtility.FromJson<TaxiSignalsSnapshot>(JsonUtility.ToJson(signals.CaptureLive()));signals.Clear();signals.RestoreLive(copy);
   Check(signals.direction==1,"actual signal Json restore");
   copy.direction=2;Reject(()=>TaxiSignals.ValidateSnapshot(copy),"bad signal rejected");
   Reject(()=>TaxiSignals.ValidateSnapshot(new TaxiSignalsSnapshot{direction=1,manualHazards=true}),"hazard and direction conflict rejected");
   saved.entry.slowPresent=true;saved.entry.slow=null;Reject(()=>law.RestoreObservation(saved),"missing explicit turn state rejected");
   var legacy=new LiveCampaignCheckpoint{operationVersion=0,signals=new TaxiSignalsSnapshot{direction=2},lawObservation=saved};BusinessProgress.ValidateOperationState(legacy);
   Check(legacy.signals==null&&legacy.lawObservation==null,"legacy Json phantom fields ignored");
   Reject(()=>BusinessProgress.ValidateOperationState(new LiveCampaignCheckpoint{operationVersion=1,signalsPresent=true}),"explicit missing signal state rejected");
   Reject(()=>BusinessProgress.ValidateOperationState(new LiveCampaignCheckpoint{operationVersion=1,lawObservationPresent=true,lawObservation=new DrivingLawSnapshot{wrongSeconds=float.NaN}}),"nonfinite live evidence rejected");
   var absent=new LiveCampaignCheckpoint{operationVersion=1,signals=new TaxiSignalsSnapshot{direction=2}};BusinessProgress.ValidateOperationState(absent);Check(absent.signals==null,"explicit absent signal remains neutral");
   Debug.Log("P1P2_CHECKPOINT_LAW_OK checks="+checks);
  }finally{UnityEngine.Object.DestroyImmediate(objects);}
 }
}
