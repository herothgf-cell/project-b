using System;
using System.Collections.Generic;
using UnityEngine;

// The host owns contacts, charging and transport outcomes. Replicas only render State.
[DefaultExecutionOrder(100)]
public sealed class AccidentController : MonoBehaviour {
 public DriveSession session;
 public AccidentState State=new AccidentState();
 public bool Locked {get{return State!=null&&(State.Locked||State.recoveryRequired);}}
 public bool Waiting {get{return State.phase==AccidentPhase.Waiting;}}
 public bool Atomic {get;private set;}
 public static bool PersonHeld(DriveSession world,string id){if(!world)return false;if(world.simulationMatch){foreach(var d in world.simulationMatch.Drivers)if(d.accident&&d.accident.HoldsPerson(id))return true;return false;}return world.accident&&world.accident.HoldsPerson(id);}
 float suppressedUntil,fadeUntil;Vector3 previous;Quaternion previousRotation;bool sampled;
 bool Ready {get{return session&&session.StartupReady&&session.business&&session.business.enabledCampaign&&session.business.ledger.Account!=null&&session.business.ledger.Operating&&!session.business.preparing&&!session.business.IsRestoring&&!session.PracticeBusy&&!PracticeSession.Enabled&&!RoadMapRuntime.Trial;}}
 public AccidentState Capture(){return State.Copy();}
 public void Restore(AccidentState value){State=value==null?new AccidentState():value.Copy();State.Validate();Suppress(1);}
 public void Suppress(float seconds){suppressedUntil=Time.time+Mathf.Max(0,seconds);sampled=false;}
 public static bool SweptContact(Vector3 from,Vector3 to,Quaternion rotation,Vector3 half,Vector3 point,float margin){
  var inv=Quaternion.Inverse(rotation);var a=inv*(from-point);var d=inv*(to-from);half+=Vector3.one*margin;
  float enter=0,exit=1;
  for(int axis=0;axis<3;axis++){float origin=a[axis],delta=d[axis],h=half[axis];if(Mathf.Abs(delta)<.00001f){if(Mathf.Abs(origin)>h)return false;continue;}float x=(-h-origin)/delta,y=(h-origin)/delta;if(x>y){float temp=x;x=y;y=temp;}enter=Mathf.Max(enter,x);exit=Mathf.Min(exit,y);if(enter>exit)return false;}
  return true;
 }
 bool Touches(Vector3 chest,bool swept){var box=session.taxi.GetComponent<BoxCollider>();if(!box)return false;var current=box.transform.TransformPoint(box.center);var half=Vector3.Scale(box.size*.5f,box.transform.lossyScale);
  if(Vector3.Distance(chest,box.ClosestPoint(chest))<=.32f)return true;
  return swept&&sampled&&SweptContact(previous,current,box.transform.rotation,half,chest,.32f);
 }
 public void ScanContacts(){
  if(!Ready||PvpNativeLan.IsReplica(session)||PvpNativeLan.WaitingForWorld||session.paused||Time.timeScale<=0)return;
  bool grace=Time.time<suppressedUntil;bool fast=session.taxi.ObservedVelocity.magnitude>=1.5f;bool sweep=fast||State.phase==AccidentPhase.Stopping;
  var contacts=new List<string>();var world=session.simulationMatch?session.simulationMatch.Human:session;
  if(world.ambientPedestrians)foreach(var a in world.ambientPedestrians.actors)if(a&&a.VisualActive&&Touches(a.transform.position+Vector3.up*.85f,sweep&&!grace))contacts.Add("ambient:"+a.route.id);
  var jobs=world.jobs;
  for(int i=0;i<jobs.people.Length;i++){
   bool waiting=jobs.HasWaitingPassenger(i);
   if(session.simulationMatch)foreach(var driver in session.simulationMatch.Drivers)waiting|=driver.jobs.current!=null&&driver.jobs.trip.State==TripState.ToPickup&&driver.jobs.current.pickup==i;
   if(waiting&&jobs.people[i]&&Touches(jobs.people[i].transform.position+Vector3.up*.85f,sweep&&!grace))contacts.Add("waiting:"+i);
  }
  foreach(var id in contacts)if(!grace&&(fast||Locked))ReportContact(id);
  State.ObserveContact(contacts.Count>0||session.damage&&session.damage.ContactCount>0,Time.fixedDeltaTime);
  var body=session.taxi.GetComponent<BoxCollider>();previous=body?body.transform.TransformPoint(body.center):transform.position;previousRotation=transform.rotation;sampled=true;
 }
 void FixedUpdate(){ScanContacts();}
 public bool ReportContact(string targetId){
  return ReportIncident(AccidentKind.Person,targetId,null);
 }
 public bool ReportCollision(AccidentKind kind,string targetId){return ReportIncident(kind,targetId,null);}
 // Authored scenarios and runtime probes still execute the authoritative incident
 // path, with the scenario's bounded roll instead of a newly sampled value.
 public bool ReportScenarioContact(AccidentKind kind,string targetId,int roll){
  if(roll<0||roll>=1000)throw new ArgumentException("Accident roll must be between 0 and 999");
  return ReportIncident(kind,targetId,roll);
 }
 bool ReportIncident(AccidentKind kind,string targetId,int? roll){
  if(!Ready||PvpNativeLan.IsReplica(session)||PvpNativeLan.WaitingForWorld||session.paused||Time.timeScale<=0||Time.time<suppressedUntil||string.IsNullOrWhiteSpace(targetId)||!Enum.IsDefined(typeof(AccidentKind),kind))return false;
  if(Locked){
   bool changed=false;var list=new List<string>(State.targets??new string[0]);if(!list.Contains(targetId)){list.Add(targetId);State.targets=list.ToArray();changed=true;}
   // PhysX may report a body before the pedestrian sweep in the same stopping
   // event. Keep its chosen severity and account for the person before charging.
   if(kind==AccidentKind.Person&&State.Kind!=AccidentKind.Person&&State.resolutionVersion==1&&State.phase==AccidentPhase.Stopping&&!State.chargeApplied&&!State.outcomeApplied){
    var person=AccidentSeverityPolicy.Resolve(AccidentKind.Person,State.Severity);State.accidentKind=person.Kind;State.selectedFee=person.Fee;State.selectedSeconds=person.Seconds;State.selectedScorePenalty=person.ScorePenalty;State.selectedTowRequired=person.TowRequired;State.remaining=person.Seconds;
    SelectPedestrianDamage();
    session.manners.Deduct("collision",10,"보행자 사고 · 안전 정차",0);changed=true;
   }
   if(changed)session.business.SaveCheckpoint();return false;
  }
  if(State.contactLatched)return false;
  string id=session.law.ids.Next(kind==AccidentKind.Person?"pedestrian-accident":"collision-accident");
  var resolution=AccidentSeverityPolicy.Sample(kind,roll??(int)(DrunkRideState.Hash(id)%1000));
  if(!State.BeginResolved(id,targetId,resolution))return false;
  if(kind==AccidentKind.Person)SelectPedestrianDamage();
  session.assisted.SetActive(false);session.services.Close();session.taxi.ClearPredictedPedals();
  if(session.damage)session.damage.CancelPendingService();
  if(session.simulationMatch)session.simulationMatch.CancelConditionService(session);
  if(kind==AccidentKind.Person)session.manners.Deduct("collision",10,"보행자 사고 · 안전 정차",0);
  session.business.SaveCheckpoint();return true;
 }
 // Called instead of normal operation once per BusinessProgress physics tick.
 public void Tick(float seconds){
  if(!Ready||PvpNativeLan.IsReplica(session)||!Locked||session.paused||Time.timeScale<=0||seconds<=0)return;
  var ledger=session.business.ledger;
  if(State.phase==AccidentPhase.Stopping){
   State.stopSeconds+=seconds;var speed=session.taxi.body.velocity.magnitude;
   ledger.AdvanceOperation(seconds,speed*seconds,session.jobs.trip.State==TripState.OnTrip);
   if(ledger.Driver!=null)ledger.Driver.drowsiness.Advance(seconds,true,ledger.Driver.fatigue,speed>=.56f);
   session.taxi.body.velocity=Vector3.MoveTowards(session.taxi.body.velocity,Vector3.zero,6*seconds);
   bool safelyStopped=session.taxi.body.velocity.magnitude<.56f&&Vector3.Dot(transform.up,Vector3.up)>.5f;
   if(safelyStopped)State.recoveryRequired=false;
   if(State.recoveryRequired||State.stopSeconds>=5&&!safelyStopped){State.recoveryRequired=true;if(!TrySafetyRecovery()){SyncClock();return;}}
   if(session.taxi.body.velocity.magnitude>=.56f||Vector3.Dot(transform.up,Vector3.up)<=.5f){SyncClock();return;}
   session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;
   // BusinessProgress ticks before the late contact scanner. A wall can stop a
   // taxi in the same PhysX step as a pedestrian sweep; classify it before fees.
   if(!State.outcomeApplied)ScanContacts();
   bool settled=State.chargeApplied&&State.outcomeApplied&&(State.scoreApplied||State.ScorePenalty<=0);SettleOutcome();
   if(State.TowRequired&&!State.workshopTransferred){
    if(!session.damage||!session.damage.TryAccidentTransfer()){SyncClock();if(!settled)session.business.SaveCheckpoint();return;}
    State.workshopTransferred=true;
   }
   State.MarkStopped(session.simulationMatch!=null);SyncClock();session.business.SaveCheckpoint();
   if(!session.simulationMatch){Time.timeScale=0;return;}
   return;
  }
  if(State.phase==AccidentPhase.Processing){
   double step=State.AdvanceProcessing(Math.Min(seconds,session.simulationMatch?session.simulationMatch.Remaining:seconds));
   ledger.AdvanceAccidentProcessing(step);SyncClock();
   if(State.remaining<=0){ledger.CompleteAccident(State.eventId,State.ProcessingDuration);FinishProcessing();}
  }
 }
 void SelectPedestrianDamage(){State.SelectPedestrianDamage((int)(DrunkRideState.Hash(State.eventId+":pedestrian-damage")%1000));}
 void SettleOutcome(){
  State.ApplyVehicleDamage(session.business.ledger.Care.Damage);
  if(!State.chargeApplied){session.business.ledger.ChargeAccident(State.eventId,State.Fee);State.chargeApplied=true;}
  AccidentScore.Apply(session.law.ledger,State,session.business.ledger.Day);
  string cause=State.Kind==AccidentKind.Person?"보행자 사고":State.Kind==AccidentKind.Vehicle?"차량 사고":"시설 사고";
  if(!State.outcomeApplied){Atomic=true;try{State.outcome=session.jobs.ResolveAccident(cause);State.outcomeApplied=true;ResolveVictims();}finally{Atomic=false;}}
 }
 // Mandatory liability survives match expiry; optional services are cancelled.
 // Freeze before transport resolution, without advancing either clock.
 public void SettleForMatchEnd(){
  if(!session||!session.simulationMatch||PvpNativeLan.IsReplica(session)||!Locked)return;
  session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;
  SettleOutcome();
 }
 void SyncClock(){if(session.simulationMatch)session.simulationMatch.ObserveAccidentClock(session);}
 public bool Confirm(){
  if(PvpNativeLan.IsReplica(session)||!Ready||session.simulationMatch||!Waiting||State.recoveryRequired||State.TowRequired&&!State.workshopTransferred||!State.chargeApplied||!State.outcomeApplied)return false;
  if(!session.business.ledger.CompleteAccident(State.eventId,State.ProcessingDuration))return false;
  FinishProcessing();Time.timeScale=1;session.paused=false;session.business.SaveCheckpoint();return true;
 }
 void FinishProcessing(){if(!State.Complete())return;ResolveVictims();session.taxi.ClearPredictedPedals();if(session.damage)session.damage.ResumeAfterAccident();fadeUntil=Time.unscaledTime+.3f;session.business.SaveCheckpoint();}
 void ResolveVictims(){
  var world=session.simulationMatch?session.simulationMatch.Human:session;
  foreach(string id in State.targets??new[]{State.targetId}){
   if(id.StartsWith("ambient:")){var actor=world.ambientPedestrians?world.ambientPedestrians.At(id.Substring(8)):null;if(actor)actor.ResetSafe();}
   else if(id.StartsWith("waiting:")){int index;if(!int.TryParse(id.Substring(8),out index))continue;
    if(session.simulationMatch){session.simulationMatch.SharedCalls.SuspendPickup(index,60);foreach(var driver in session.simulationMatch.Drivers)driver.jobs.SuspendAccidentPickup(index);}
    else session.jobs.SuspendAccidentPickup(index);
   }
  }
 }
 public bool HoldsPerson(string id){return Locked&&Array.IndexOf(State.targets??new string[0],id)>=0;}
 bool TrySafetyRecovery(){
  // Only authored stop bays close to the incident; never a distant facility shortcut.
  MapStop best=null;float nearest=80*80;
  foreach(var stop in session.jobs.stops){float distance=(stop.Kerb-transform.position).sqrMagnitude;if(distance>=nearest)continue;bool clear=true;foreach(var c in Physics.OverlapBox(stop.Kerb+Vector3.up*.85f,new Vector3(1.2f,.65f,2.6f),Quaternion.LookRotation(stop.Direction),~0,QueryTriggerInteraction.Ignore)){if(c.attachedRigidbody==session.taxi.body||c.bounds.max.y<=stop.Kerb.y+.2f)continue;clear=false;break;}if(clear){best=stop;nearest=distance;}}
  if(best==null)return false;
  session.taxi.SetSpawn(best.Kerb+Vector3.up*.65f,Quaternion.LookRotation(best.Direction));session.taxi.ResetTaxi();State.recoveryRequired=false;fadeUntil=Time.unscaledTime+.4f;return true;
 }
 void OnGUI(){
  if(!session||session.IsSimulationBot||PvpNativeLan.IsRemoteHuman(session)||session.simulationMatch&&session.simulationMatch.Finished)return;
  if(!Locked){DrawFade();return;}
  // The shared presentation restores its caller's depth; retain a modal depth
  // on this component so cockpit panels and score notices stay behind it.
  GUI.depth=-600;
  CasualUI.Backdrop();
  if(AccidentPresentation.Draw(State,session.damage&&session.damage.Disabled,session.simulationMatch!=null))Confirm();
  DrawFade();
 }
 void DrawFade(){if(Time.unscaledTime<fadeUntil){GUI.depth=-2000;GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;}}
}
