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
  bool grace=Time.time<suppressedUntil;bool fast=session.taxi.ObservedVelocity.magnitude>=1.5f;
  var contacts=new List<string>();var world=session.simulationMatch?session.simulationMatch.Human:session;
  if(world.ambientPedestrians)foreach(var a in world.ambientPedestrians.actors)if(a&&a.VisualActive&&Touches(a.transform.position+Vector3.up*.85f,fast&&!grace))contacts.Add("ambient:"+a.route.id);
  var jobs=world.jobs;
  for(int i=0;i<jobs.people.Length;i++){
   bool waiting=jobs.HasWaitingPassenger(i);
   if(session.simulationMatch)foreach(var driver in session.simulationMatch.Drivers)waiting|=driver.jobs.current!=null&&driver.jobs.trip.State==TripState.ToPickup&&driver.jobs.current.pickup==i;
   if(waiting&&jobs.people[i]&&Touches(jobs.people[i].transform.position+Vector3.up*.85f,fast&&!grace))contacts.Add("waiting:"+i);
  }
  foreach(var id in contacts)if(!grace&&(fast||Locked))ReportContact(id);
  State.ObserveContact(contacts.Count>0,Time.fixedDeltaTime);
  var body=session.taxi.GetComponent<BoxCollider>();previous=body?body.transform.TransformPoint(body.center):transform.position;previousRotation=transform.rotation;sampled=true;
 }
 void FixedUpdate(){ScanContacts();}
 public bool ReportContact(string targetId){
  if(!Ready||PvpNativeLan.IsReplica(session)||PvpNativeLan.WaitingForWorld||session.paused||Time.timeScale<=0||Time.time<suppressedUntil||string.IsNullOrEmpty(targetId))return false;
  if(Locked){var list=new List<string>(State.targets??new string[0]);if(!list.Contains(targetId)){list.Add(targetId);State.targets=list.ToArray();session.business.SaveCheckpoint();}return false;}
  if(!State.Begin(session.law.ids.Next("pedestrian-accident"),targetId))return false;
  session.assisted.SetActive(false);session.services.Close();session.taxi.ClearPredictedPedals();
  if(session.damage)session.damage.CancelPendingService();
  if(session.simulationMatch)session.simulationMatch.CancelConditionService(session);
  session.manners.Deduct("collision",10,"보행자 사고 · 안전 정차",0);
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
   if(session.taxi.body.velocity.magnitude>=.56f){SyncClock();return;}
   session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;
   if(!State.chargeApplied){ledger.ChargeAccident(State.eventId);State.chargeApplied=true;}
   if(!State.outcomeApplied){Atomic=true;try{State.outcome=session.jobs.ResolveAccident();State.outcomeApplied=true;ResolveVictims();}finally{Atomic=false;}}
   State.MarkStopped(session.simulationMatch!=null);SyncClock();session.business.SaveCheckpoint();
   if(!session.simulationMatch){Time.timeScale=0;return;}
   return;
  }
  if(State.phase==AccidentPhase.Processing){
   double step=State.AdvanceProcessing(Math.Min(seconds,session.simulationMatch?session.simulationMatch.Remaining:seconds));
   ledger.AdvanceAccidentProcessing(step);SyncClock();
   if(State.remaining<=0){ledger.CompleteAccident(State.eventId);FinishProcessing();}
  }
 }
 void SyncClock(){if(session.simulationMatch)session.simulationMatch.ObserveAccidentClock(session);}
 public bool Confirm(){
  if(PvpNativeLan.IsReplica(session)||!Ready||session.simulationMatch||!Waiting)return false;
  if(!session.business.ledger.CompleteAccident(State.eventId))return false;
  FinishProcessing();Time.timeScale=1;session.paused=false;session.business.SaveCheckpoint();return true;
 }
 void FinishProcessing(){ResolveVictims();State.Complete();session.taxi.ClearPredictedPedals();fadeUntil=Time.unscaledTime+.3f;session.business.SaveCheckpoint();}
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
  float w=Mathf.Min(580,Screen.width-24),h=310,x=(Screen.width-w)/2,y=(Screen.height-h)/2;GUI.depth=-600;
  CasualUI.Panel(new Rect(x,y,w,h));var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};style.normal.textColor=CasualUI.Ink;
  string text=State.phase==AccidentPhase.Stopping?"보행자 접촉 · 안전 정차 중":"보행자 사고 처리";
  text+="\n"+(State.outcome??"")+"\n처리비 3,000원 · 부족분 미납\n"+(session.simulationMatch?"남은 처리 "+Math.Ceiling(State.remaining)+"초 · 다른 차량 경기 계속":"처리 시간 45초")+"\n추가 피로 없음 · 시간에 따른 피로/포만감 변화\n내구도 "+session.business.ledger.Care.Damage.Durability+" · "+(session.damage.Disabled?"처리 후 견인 필요":"처리 후 운행 재개 / 공업사 이용 가능");
  if(State.recoveryRequired)text+="\n안전 복구 공간 대기 · 추가 청구 없음";
  GUI.Label(new Rect(x+20,y+18,w-40,220),text,style);
  if(Waiting&&!session.simulationMatch&&GUI.Button(new Rect(x+20,y+254,w-40,40),"사고 처리 확인 · 45초 경과",CasualUI.Action(18)))Confirm();
  DrawFade();
 }
 void DrawFade(){if(Time.unscaledTime<fadeUntil){GUI.depth=-2000;GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;}}
}
