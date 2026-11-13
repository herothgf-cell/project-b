using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// A single setup pose per case, followed by native Rigidbody/PhysX movement.
// No accident-report API, selected grade, timer tick or collision callback is injected.
public sealed class AccidentPhysicalCollisionProbe : MonoBehaviour {
 [Serializable] sealed class Result {public bool success;public string scope,error,savePath;public List<string> assertions=new List<string>();}
 Result result=new Result();DriveSession session;string output;bool done;float deadline;Vector3 origin;GameObject floor,target;int continuousCases;
 static bool Has(string flag){return Array.IndexOf(Environment.GetCommandLineArgs(),flag)>=0;}
 static string Arg(string flag,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,flag);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Has("-accidentPhysicalCollisionCheck"))new GameObject("Real PhysX accident checks").AddComponent<AccidentPhysicalCollisionProbe>();}
 void Check(bool value,string message){result.assertions.Add((value?"PASS ":"FAIL ")+message);Debug.Log("ACCIDENT_PHYSX "+result.assertions[result.assertions.Count-1]);if(!value)throw new Exception(message);}
 void Finish(Exception error=null){if(done)return;done=true;result.success=error==null;result.error=error==null?"":error.ToString();Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));Debug.Log("ACCIDENT_PHYSX_"+(result.success?"OK":"FAIL")+" checks="+result.assertions.Count+" "+result.error);Application.Quit(result.success?0:1);}
 void Update(){if(!done&&deadline>0&&Time.realtimeSinceStartup>deadline)Finish(new TimeoutException("real collision acceptance timeout"));}
 IEnumerator Start(){
  Application.runInBackground=true;output=Path.GetFullPath(Arg("-accidentPhysicalCollisionOutput","artifacts/accident-physical-collision-player"));deadline=Time.realtimeSinceStartup+180;
  var stack=new Stack<IEnumerator>();stack.Push(Run());while(!done&&stack.Count>0){object next;try{var run=stack.Peek();if(!run.MoveNext()){stack.Pop();continue;}next=run.Current;var child=next as IEnumerator;if(child!=null){stack.Push(child);continue;}}catch(Exception error){Finish(error);yield break;}yield return next;}Finish();
 }
 IEnumerator PhysicsSeconds(float seconds){float end=Time.fixedTime+seconds;while(Time.fixedTime<end)yield return new WaitForFixedUpdate();}
 void Setup(AccidentKind kind){
  if(target)Destroy(target);session.services.Close();session.jobs.ClearForDay();session.business.passengers.Clear();var panel=session.GetComponent<RideCompletionPanel>();if(panel)panel.Close();
  session.business.settings=PrototypeSettings.ForThreeDays();session.business.ledger=new EconomyLedger(session.business.settings);session.law.ledger=new LawLedger();session.law.ids=new PrototypeIds("accident-physx-"+kind);
  session.accident.Restore(new AccidentState());session.business.preparing=false;session.paused=false;Time.timeScale=1;
  var body=session.taxi.body;body.isKinematic=false;body.useGravity=true;body.constraints=RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionX;body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;
  // Elevated isolated floor avoids moving traffic and pedestrians. The taxi's
  // Update is disabled so its ordinary out-of-world reset does not run here.
  body.position=origin+Vector3.up*2;body.rotation=Quaternion.identity;session.taxi.transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();session.damage.TechnicalRecovery();session.accident.Suppress(0);
 }
 void Obstacle(AccidentKind kind){
  if(kind==AccidentKind.Property){target=new GameObject("PhysX static wall");target.transform.position=origin+Vector3.forward*14+Vector3.up*2;target.AddComponent<BoxCollider>().size=new Vector3(8,4,1);}
  else {
   target=new GameObject("PhysX stationary taxi");var taxi=target.AddComponent<TaxiController>();taxi.Create(origin+Vector3.forward*14,Quaternion.identity);taxi.enabled=false;foreach(var wheel in taxi.wheels)wheel.enabled=false;taxi.body.isKinematic=true;taxi.body.useGravity=false;
   // Production impact motion exists on native traffic and other taxis too.
   target.AddComponent<VehicleImpactMotion>().Sample();
  }
  Physics.SyncTransforms();
 }
 IEnumerator CollisionCase(AccidentKind kind){
  Setup(kind);yield return PhysicsSeconds(2);
  Check(!session.accident.Locked&&session.damage.Damage.Durability==100&&session.damage.ContactCount==0,kind+" real floor landing is supporting contact, not an accident");
  var body=session.taxi.body;Check(Mathf.Abs(body.position.y-origin.y)<.3f,kind+" floor physically supports taxi body");Obstacle(kind);yield return PhysicsSeconds(.1f);
  Vector3 start=body.position;body.velocity=Vector3.forward*7;
  float until=Time.realtimeSinceStartup+10;while(!session.accident.Locked&&Time.realtimeSinceStartup<until)yield return null;
  Debug.Log("ACCIDENT_PHYSX_DIAGNOSTIC "+kind+" travelled="+Vector3.Distance(start,body.position)+" velocity="+body.velocity+" contacts="+session.damage.ContactCount+" position="+body.position+" target="+target.transform.position);
  Check(session.accident.Locked&&Vector3.Distance(start,body.position)>6,kind+" moving Rigidbody physically reaches obstacle and creates native incident");
  var state=session.accident.State;Check(state.Kind==kind&&state.targetId.StartsWith("collision:"),kind+" production VDC collision classifies actual collider");
  var selected=AccidentSeverityPolicy.Sample(kind,(int)(DrunkRideState.Hash(state.eventId)%1000));Check(state.Severity==selected.Severity&&state.Fee==selected.Fee&&state.ProcessingDuration==selected.Seconds&&state.ScorePenalty==selected.ScorePenalty,kind+" normal host sampler persists selected policy without grade injection");
  until=Time.realtimeSinceStartup+12;while(!session.accident.Waiting&&Time.realtimeSinceStartup<until)yield return null;
  Check(session.accident.Waiting&&state.chargeApplied&&state.outcomeApplied,kind+" native fixed updates reach safe confirmation phase");
  var ledger=session.business.ledger;int fee=state.Fee,penalty=state.ScorePenalty;string id=state.eventId;Check(ledger.Account.Expense(ledger.Day,"accident")==fee&&session.law.ledger.TotalScore==100-penalty,kind+" selected fee and global driving deduction apply once");
  Check(session.damage.Damage.Durability<100&&ledger.Account.Expense(ledger.Day,"repair")==0&&ledger.Account.Expense(ledger.Day,"tow")==0,kind+" impact damage remains and no repair or extra tow is charged");
  if(state.TowRequired){Check(state.workshopTransferred,kind+" sampled major completes mandatory workshop transfer");bool atBay=false;foreach(var bay in session.damage.Bays)if(!bay.legacy&&Vector3.Distance(body.position,bay.position)<3)atBay=true;Check(atBay,kind+" sampled major occupies real dedicated workshop bay");}
  else {
   // Re-enable physics behind the confirmation card without moving the taxi:
   // contact stays continuous while native incident handling remains locked.
   Time.timeScale=1;yield return PhysicsSeconds(1);
   Check(session.damage.ContactCount>0&&state.contactLatched&&state.eventId==id&&ledger.Account.Expense(ledger.Day,"accident")==fee&&session.law.ledger.TotalScore==100-penalty,kind+" continuous physical contact cannot reroll or charge during confirmation");continuousCases++;
  }
  double before=ledger.Elapsed;Check(session.accident.Confirm()&&!session.accident.Locked,kind+" native confirmation safely completes");Check(Math.Abs(ledger.Elapsed-before-state.ProcessingDuration)<.001,kind+" chosen treatment duration advances exactly once");
  Check(!session.accident.Confirm()&&ledger.Account.Expense(ledger.Day,"accident")==fee&&session.law.ledger.TotalScore==100-penalty,kind+" repeated confirmation cannot repeat money or score");
  if(!state.TowRequired){yield return PhysicsSeconds(1.2f);Check(session.damage.ContactCount>0&&state.contactLatched&&state.eventId==id&&ledger.Account.Expense(ledger.Day,"accident")==fee,kind+" retained contact after completion cannot rearm a second incident");}
  Check(session.business.SaveCheckpoint()&&!session.business.SaveFailed,kind+" writes only isolated contentTest checkpoint");
 }
 IEnumerator TrafficSideImpact(){
  Setup(AccidentKind.Vehicle);
  yield return PhysicsSeconds(2);
  var taxi=session.taxi.body;
  var route=new TrafficRoute();
  route.SetPoints(new[]{origin+new Vector3(3.6f,0,-8),origin+new Vector3(3.6f,0,-3),origin+new Vector3(1.3f,0,2),origin+new Vector3(0,0,8)});
  target=new GameObject("PhysX lane-changing traffic");
  var traffic=target.AddComponent<TrafficCar>();
  traffic.Initialize(session.traffic,route,0,0);
  traffic.respawn=false;
  traffic.speed=9;
  target.AddComponent<VehicleImpactMotion>();
  // The probe disables avoidance queries on the test taxi only. Its collider
  // still participates in PhysX, so traffic approaches and strikes its side.
  var constraints=taxi.constraints;
  taxi.constraints=constraints|RigidbodyConstraints.FreezePositionZ;
  session.taxi.gameObject.layer=11;
  Physics.SyncTransforms();
  float until=Time.realtimeSinceStartup+5;
  while(!session.accident.Locked&&Time.realtimeSinceStartup<until)yield return null;
  session.taxi.gameObject.layer=8;
  taxi.constraints=constraints;
  Check(traffic.distance>3&&session.damage.Damage.Durability<100&&session.accident.Locked&&session.accident.State.Kind==AccidentKind.Vehicle,
   "kinematic traffic crossing laterally into the stationary taxi causes vehicle damage and an accident");
 }
 IEnumerator Run(){
  result.scope="Real PhysX Rigidbody collisions on isolated elevated support floor; setup pose once per case, actual travel into static wall and kinematic full taxi; automatic VDC/controller, normal sampled severity, fee/score/time and continuous contact. Not human driving, wheel driving, full campaign or LAN.";
  Check(ContentTuningRuntime.TestMode,"contentTest isolation required before campaign startup");
  if(!FindObjectOfType<DriveSession>()){var boot=FindObjectOfType<UnifiedModeBootstrap>();if(boot)boot.StartSingle();}
  while(!(session=FindObjectOfType<DriveSession>())||!session.StartupReady)yield return null;yield return null;while(session.business.IsRestoring)yield return null;
  result.savePath=session.business.SavePath;Check(!session.business.ResumeBlocked&&Path.GetFullPath(result.savePath).StartsWith(Path.GetFullPath(ContentTuningRuntime.TestDirectory)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"native campaign uses private contentTest directory");
  session.jobs.enabled=false;session.requests.enabled=false;session.law.enabled=false;session.manners.enabled=false;session.assisted.SetActive(false);session.assisted.enabled=false;session.taxi.enabled=false;foreach(var wheel in session.taxi.wheels)wheel.enabled=false;
  session.business.enabled=true;session.damage.enabled=true;session.accident.enabled=true;origin=session.jobs.stops[0].Kerb;origin.y=300;
  floor=new GameObject("PhysX isolated support floor");floor.transform.position=origin-Vector3.up*.5f+Vector3.forward*8;var support=floor.AddComponent<BoxCollider>();support.size=new Vector3(30,1,50);
  support.material=new PhysicMaterial("Collision probe frictionless support"){dynamicFriction=0,staticFriction=0,frictionCombine=PhysicMaterialCombine.Minimum};Physics.SyncTransforms();
  yield return CollisionCase(AccidentKind.Property);yield return CollisionCase(AccidentKind.Vehicle);Check(continuousCases>0,"at least one normally sampled case exercises retained real contact");
  yield return TrafficSideImpact();
 }
}
