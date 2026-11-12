using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Pose-controlled native controller acceptance. Does not simulate physical impact,
// human driving, a new-process resume, or LAN transport.
public sealed class AccidentSeverityUiProbe : MonoBehaviour {
 [Serializable] sealed class Result {public bool success;public string scope,error,savePath;public List<string> assertions=new List<string>();}
 Result result=new Result();DriveSession s;PvpSimulationMatch match;bool done,pvp;float deadline;string output,view;int serial;
 static bool Has(string flag){return Array.IndexOf(Environment.GetCommandLineArgs(),flag)>=0;}
 static string Arg(string flag,string fallback){var a=Environment.GetCommandLineArgs();int at=Array.IndexOf(a,flag);return at>=0&&at+1<a.Length?a[at+1]:fallback;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Has("-accidentSeverityUiCheck")||Has("-pvpAccidentSeverityUiCheck"))new GameObject("Accident severity UI acceptance").AddComponent<AccidentSeverityUiProbe>();}
 void Check(bool ok,string why){result.assertions.Add((ok?"PASS ":"FAIL ")+why);Debug.Log("ACCIDENT_SEVERITY_UI "+result.assertions.Last());if(!ok)throw new Exception(why);}
 void Finish(Exception error=null){if(done)return;done=true;result.success=error==null;result.error=error==null?"":error.ToString();Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));Debug.Log("ACCIDENT_SEVERITY_UI_"+(result.success?"OK":"FAIL")+" checks="+result.assertions.Count+" "+result.error);Application.Quit(result.success?0:1);}
 void Update(){if(!done&&deadline>0&&Time.realtimeSinceStartup>deadline)Finish(new TimeoutException("severity UI acceptance timeout"));}
 IEnumerator Start(){Application.runInBackground=true;output=Path.GetFullPath(Arg("-accidentSeverityUiOutput","artifacts/accident-severity-ui-player"));view=Arg("-accidentSeverityView","all").ToLowerInvariant();deadline=Time.realtimeSinceStartup+240;var stack=new Stack<IEnumerator>();stack.Push(Run());while(!done&&stack.Count>0){object next;try{var run=stack.Peek();if(!run.MoveNext()){stack.Pop();continue;}next=run.Current;var child=next as IEnumerator;if(child!=null){stack.Push(child);continue;}}catch(Exception e){Finish(e);yield break;}yield return next;}Finish();}
 void Pose(Vector3 position,Vector3 direction){s.taxi.body.constraints=RigidbodyConstraints.FreezeAll;s.taxi.body.velocity=Vector3.zero;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=position+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();if(s.drivingCamera)s.drivingCamera.SetFirstPerson(s.drivingCamera.FirstPerson);}
 void Fresh(){
  var map=s.GetComponent<FullMapOverlay>();if(map.IsOpen)map.Close();s.services.Close();s.business.preparing=false;s.paused=false;Time.timeScale=1;s.accident.Restore(new AccidentState());s.accident.Suppress(0);s.jobs.ClearForDay();s.business.passengers.Clear();var panel=s.GetComponent<RideCompletionPanel>();if(panel)panel.Close();
  s.business.settings=pvp?PrototypeSettings.ForOneDay():PrototypeSettings.ForThreeDays();if(pvp)s.business.settings.daySeconds=7200;s.business.ledger=new EconomyLedger(s.business.settings);if(pvp)s.business.ledger.EnablePvpProfile();s.law.ledger=new LawLedger();s.law.ids=new PrototypeIds("severity-ui-"+(serial++));s.business.ledger.Driver.satiety=80;s.business.ledger.Driver.fatigue=0;s.business.ledger.Driver.drowsiness.Recover();Pose(s.jobs.stops[0].Kerb,s.jobs.stops[0].Direction);
 }
 void Resolution(AccidentState state,AccidentKind kind,AccidentSeverity severity,int fee,double seconds,int penalty,string label){Check(state.Kind==kind&&state.Severity==severity&&state.Fee==fee&&state.ProcessingDuration==seconds&&state.ScorePenalty==penalty&&state.TowRequired==(severity==AccidentSeverity.Major),label+" preserves selected kind/severity/fee/time/score/tow");}
 IEnumerator Contract(AccidentKind kind,AccidentSeverity severity,int roll,int fee,double seconds,int penalty){
  Fresh();string label=kind+"/"+severity;var ledger=s.business.ledger;int cash=ledger.Account.Cash,score=s.law.ledger.TotalScore,durability=s.damage.Damage.Durability;
  Check(s.accident.ReportScenarioContact(kind,"scenario:"+label,roll),label+" starts native stopping event");Resolution(s.accident.State,kind,severity,fee,seconds,penalty,label);
  Check(s.law.ledger.TotalScore==score,label+" score awaits safe stopping settlement");string id=s.accident.State.eventId;
  Check(!s.accident.ReportScenarioContact(kind,"scenario:duplicate",999)&&s.accident.State.eventId==id&&s.law.ledger.TotalScore==score,label+" repeated contact cannot reroll or rededuct score");
  s.accident.Tick(.1f);Check(s.accident.Locked&&(pvp?s.accident.State.phase==AccidentPhase.Processing:s.accident.Waiting),label+" safe stop enters correct native processing phase");Check(s.law.ledger.TotalScore==score-penalty,label+" applies selected driving score exactly once at settlement");
  int selectedLoss=kind==AccidentKind.Person?AccidentSeverityPolicy.SamplePedestrianDamage(severity,(int)(DrunkRideState.Hash(id+":pedestrian-damage")%1000)):0;Check(s.accident.State.DurabilityLoss==selectedLoss&&s.damage.Damage.Durability==durability-selectedLoss,label+" selected pedestrian damage applies once; physical damage channel stays separate");
  Check(ledger.Account.Expense(ledger.Day,"accident")==fee&&ledger.Account.Cash==Mathf.Max(0,cash-fee),label+" charges selected fee exactly once");
  if(severity==AccidentSeverity.Major){Check(s.accident.State.workshopTransferred,label+" completes mandatory workshop transfer before waiting");Check(s.damage.Bays.Any(b=>!b.legacy&&Vector3.Distance(s.taxi.transform.position,b.position)<3),label+" physically occupies a dedicated workshop bay");Check(s.damage.Damage.Durability==durability-s.accident.State.appliedDamage&&ledger.Account.Expense(ledger.Day,"tow")==0,label+" transfer preserves physical durability and adds no separate tow fee");}
  var copy=s.accident.Capture();copy.targets[0]="mutated-copy";Check(s.accident.State.targets[0]!="mutated-copy",label+" capture owns its target array");
  AccidentState restored;
  if(pvp){Check(s.business.SavePath=="",label+" PVP has no campaign save");restored=JsonUtility.FromJson<AccidentState>(JsonUtility.ToJson(s.accident.Capture()));}
  else {Check(s.business.SaveCheckpoint()&&!s.business.SaveFailed,label+" writes isolated real campaign checkpoint");var saved=BusinessProgress.ParseCheckpoint(File.ReadAllText(s.business.SavePath));Check(saved.hasLive&&saved.live!=null&&saved.live.accident!=null,label+" checkpoint contains live accident");restored=saved.live.accident;}
  restored.Validate();Resolution(restored,kind,severity,fee,seconds,penalty,label+" serialized");s.accident.Restore(restored);Resolution(s.accident.State,kind,severity,fee,seconds,penalty,label+" restored");double before=ledger.Elapsed;
  if(!pvp){s.accident.Tick(30);Check(ledger.Elapsed==before&&ledger.Account.Expense(ledger.Day,"accident")==fee&&s.law.ledger.TotalScore==score-penalty,label+" restored confirmation wait adds no time/fee/score");}
  if(kind==AccidentKind.Person){bool enabled=s.accident.enabled;s.accident.enabled=true;yield return Capture("popup",label.ToLowerInvariant().Replace('/','-'));s.accident.enabled=enabled;}
  if(pvp){s.accident.Tick((float)seconds-1);Check(s.accident.Locked&&Math.Abs(s.accident.State.remaining-1)<.001,label+" explicit host tick keeps incomplete treatment locked");s.accident.Tick(2);Check(!s.accident.Locked,label+" explicit host tick completes selected treatment");s.accident.Tick(100);}
  else Check(s.accident.Confirm()&&!s.accident.Locked,label+" selected treatment completes");
  Check(Math.Abs(ledger.Elapsed-before-seconds)<.001,label+" selected treatment time advances once");Check(!s.accident.Confirm()&&Math.Abs(ledger.Elapsed-before-seconds)<.001&&ledger.Account.Expense(ledger.Day,"accident")==fee&&s.law.ledger.TotalScore==score-penalty,label+" repeated confirmation/tick is idempotent");
 }
 IEnumerator Capture(string mode,string name){
  if(!Has("-accidentSeverityCapture")||view!="all"&&view!=mode||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;
  while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;yield return new WaitForSecondsRealtime(.45f);yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();try{Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,mode+"-"+name+"-"+Screen.width+".png"),image.EncodeToPNG());}finally{Destroy(image);}Debug.Log("ACCIDENT_SEVERITY_UI_CAPTURE "+mode+" "+name);
 }
 IEnumerator Views(){
  Fresh();var overlay=s.GetComponent<FullMapOverlay>();overlay.ShowFacilities=true;
  // Use authored passenger stops nearest the real facilities. Only the offers
  // are fixtures; neither stop coordinates nor map projection are modified.
  var facilities=overlay.Places().Where(p=>p.glyph==8||p.glyph==12).ToArray();for(int i=0;i<Math.Min(3,s.jobs.offers.Length)&&facilities.Length>0;i++){
   int pickup=0;float nearest=float.PositiveInfinity;for(int j=0;j<s.jobs.stops.Length;j++){float distance=(s.jobs.stops[j].V()-facilities[i%facilities.Length].position).sqrMagnitude;if(distance<nearest){nearest=distance;pickup=j;}}
   s.jobs.offers[i]=new JobOffer{pickup=pickup,drop=(pickup+1)%s.jobs.stops.Length,fare=5000,seconds=1200,requiredScore=60,distance=1000,candidateSeed="map-facility-call-"+i};
  }
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080)}){
   if(Has("-accidentSeverityCapture")){Screen.SetResolution(size.x,size.y,false);for(int i=0;i<10;i++)yield return null;}
   Check(overlay.Open(),"native full map opens at "+size.x);overlay.ResetView();yield return Capture("map","facilities");overlay.Close();
  }
  Fresh();s.jobs.nav.SetDestination(s.jobs.stops[3]);var hud=s.GetComponent<DrivingReadability>();var route=s.jobs.nav.hdRoute;bool following=false;
  if(route!=null)for(float at=3;at<route.path.length-3;at+=5){Pose(route.path.Position(at),route.path.Tangent(at));hud.SelectSignal();if(hud.TurnCue.Valid&&hud.TurnCue.HasNext&&hud.SignalTarget!=null){following=true;break;}}
  Check(following,"real directed route exposes current/next maneuver and a signal for nav render");Time.timeScale=0;
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080)}){if(Has("-accidentSeverityCapture")){Screen.SetResolution(size.x,size.y,false);for(int i=0;i<10;i++)yield return null;}yield return Capture("navigation","following-turn");}
  if(!pvp){
   Fresh();s.damage.Damage.Advance(2,false);s.damage.ApplyImpact(15);s.damage.Damage.Advance(2,false);s.damage.ApplyImpact(15);
   Check(s.damage.Disabled&&!s.accident.Locked,"workshop display fixture has D0 without an accident");s.damage.enabled=true;
   if(Has("-accidentSeverityCapture")){Screen.SetResolution(1280,720,false);for(int i=0;i<10;i++)yield return null;}
   yield return Capture("workshop","disabled");Check(s.damage.Tow()&&s.damage.AtRepairBay,"native D0 tow reaches paid repair bay for UI");
   if(s.drivingCamera)s.drivingCamera.SetFirstPerson(s.drivingCamera.FirstPerson);yield return Capture("workshop","repair");
   Check(s.damage.Repair()&&!s.damage.Disabled,"native paid workshop repair action remains available");s.damage.enabled=false;
  }
 }
 IEnumerator HealthyBotDeparture(){
  var human=s;s=match.Bot;Check(s!=null,"native PVP bot exists for healthy major departure");
  s.jobs.ClearForDay();s.accident.Restore(new AccidentState());s.accident.Suppress(0);s.paused=false;Time.timeScale=1;Pose(s.jobs.stops[0].Kerb,s.jobs.stops[0].Direction);
  Check(s.damage.Damage.Durability==100,"physical exit fixture begins with healthy bot");int fee=s.business.ledger.Account.Expense(s.business.ledger.Day,"accident"),tow=s.business.ledger.Account.Expense(s.business.ledger.Day,"tow");
  Check(s.accident.ReportScenarioContact(AccidentKind.Property,"scenario:healthy-bot-departure",999),"native bot major event starts");s.accident.Tick(.1f);Check(s.accident.State.phase==AccidentPhase.Processing&&s.accident.State.workshopTransferred,"native bot waits in workshop during major processing");var bay=s.taxi.transform.position;
  s.accident.Tick(90);Check(!s.accident.Locked&&s.damage.Damage.Durability==100,"healthy bot completes selected major processing without repair");
  var services=s.GetComponent<PvpSimulationBotServices>();Check(services&&services.Active,"completed healthy bot arms native workshop departure");var pilot=s.GetComponent<PvpSimulationBot>();float driven=pilot.MetresDriven;
  s.taxi.body.useGravity=true;s.taxi.body.constraints=RigidbodyConstraints.None;s.taxi.enabled=true;s.assisted.enabled=true;s.damage.enabled=true;s.accident.enabled=true;s.business.enabled=true;services.enabled=true;pilot.enabled=true;float until=Time.realtimeSinceStartup+90;
  while(services.Active&&Time.realtimeSinceStartup<until&&!match.Finished)yield return null;
  Check(!services.Active&&Vector3.Distance(s.taxi.transform.position,bay)>10&&pilot.MetresDriven-driven>10,"healthy major bot physically exits workshop through native steering/pedals");
  Check(!s.accident.Locked&&s.damage.Damage.Durability==100&&s.business.ledger.Account.Expense(s.business.ledger.Day,"accident")==fee+6000&&s.business.ledger.Account.Expense(s.business.ledger.Day,"tow")==tow,"healthy major physical exit adds no accident reroll, repair or tow charge");
  pilot.enabled=false;services.enabled=false;s.taxi.enabled=false;s.damage.enabled=false;s.accident.enabled=false;s.business.enabled=false;s.assisted.SetActive(false);s.assisted.enabled=false;s.taxi.body.constraints=RigidbodyConstraints.FreezeAll;s.taxi.body.velocity=Vector3.zero;s=human;
 }
 void SolverStoppedSweepPromotion(){
  Fresh();s.jobs.RefreshWaitingPassengers();int index=-1;for(int i=0;i<s.jobs.people.Length;i++)if(s.jobs.people[i]&&s.jobs.people[i].activeInHierarchy&&s.jobs.HasWaitingPassenger(i)){index=i;break;}Check(index>=0,"solver-stop sweep fixture uses a real waiting passenger");var person=s.jobs.people[index];var point=person.transform.position;
  Pose(point-Vector3.forward*10,Vector3.forward);s.accident.ScanContacts();Check(!s.accident.Locked,"clear sweep origin does not report a stationary incident");
  // Deliberately no yield or fixed-step between this post-solver pose and Tick:
  // the incident must consult the previous sample before its first settlement.
  Pose(point+Vector3.forward*10,Vector3.forward);Check(s.taxi.ObservedVelocity.sqrMagnitude<.0001f,"post-solver body has zero observed speed past the waiting person");int score=s.law.ledger.TotalScore;
  Check(s.accident.ReportScenarioContact(AccidentKind.Property,"scenario:solver-stop-property",800),"property collision callback creates moderate pre-sweep incident");Check(s.accident.State.Kind==AccidentKind.Property&&s.business.ledger.Account.Expense(s.business.ledger.Day,"accident")==0,"pre-sweep property event remains uncharged");
  s.accident.Tick(.1f);
  Check(s.accident.State.Kind==AccidentKind.Person&&s.accident.State.Severity==AccidentSeverity.Moderate&&Array.IndexOf(s.accident.State.targets,"waiting:"+index)>=0,"controller Tick itself sweeps previous motion and promotes person without reroll");
  Check(s.accident.State.Fee==8000&&s.accident.State.ProcessingDuration==120&&s.business.ledger.Account.Expense(s.business.ledger.Day,"accident")==8000&&s.law.ledger.TotalScore==score-25,"late person sweep selects person fee/time/score before first charge");Check(s.accident.State.pedestrianDamageVersion==1&&s.accident.State.damageApplied&&s.damage.Damage.Durability==100-s.accident.State.appliedDamage,"new physical-to-person promotion applies selected damage");Check(s.accident.Confirm()&&!s.accident.Locked,"promoted event completes through native confirmation");
 }
 void AdvanceWorkshopFrames(){
  // Match the production BusinessProgress wrapper: each physics step is
  // observed before the next service step. A single 30s jump would make the
  // completion checkpoint misclassify that unobserved jump as a second lock.
  for(int n=0;n<10000&&s.damage.PendingServiceRemaining>0;n++){s.damage.AdvancePendingService(Mathf.Min(.02f,s.damage.PendingServiceRemaining));match.ObserveAccidentClock(s);}
  Check(s.damage.PendingServiceRemaining<=0,"explicit native workshop frames complete pending service");
 }
 IEnumerator DisabledPedestrianContracts(){
  foreach(var level in new[]{AccidentSeverity.Minor,AccidentSeverity.Major}){
   Fresh();s.damage.Damage.ApplyAccidentLoss("prior-wear",99);int maximum=AccidentSeverityPolicy.PedestrianDamageMaximum(level);string prefix="";
   for(int n=0;n<1000;n++){prefix="pedestrian-d0-"+level+"-"+n;string eventId=prefix+":pedestrian-accident:1";if(AccidentSeverityPolicy.SamplePedestrianDamage(level,(int)(DrunkRideState.Hash(eventId+":pedestrian-damage")%1000))==maximum)break;}
   s.law.ids=new PrototypeIds(prefix);Check(s.accident.ReportScenarioContact(AccidentKind.Person,"scenario:low-durability",level==AccidentSeverity.Minor?0:999),level+" low durability person event starts");s.accident.Tick(.1f);
   Check(s.damage.Disabled&&s.accident.State.selectedDamage==maximum&&s.accident.State.appliedDamage==1&&s.accident.State.damageApplied,level+" person loss clamps to actual last durability point");
   if(level==AccidentSeverity.Major)Check(s.accident.State.workshopTransferred&&s.damage.AtRepairBay,"major person D0 keeps workshop repair reservation");
   bool shown=s.accident.enabled;s.accident.enabled=true;yield return Capture("popup",level.ToString().ToLowerInvariant()+"-d0");s.accident.enabled=shown;
   if(pvp)s.accident.Tick((float)s.accident.State.ProcessingDuration);else Check(s.accident.Confirm(),level+" D0 event completes safely");
   Check(!s.accident.Locked&&s.damage.Disabled,level+" completed accident never repairs car for free");
   if(level==AccidentSeverity.Minor){Check(s.damage.Tow(),"minor person D0 uses normal tow");if(pvp)AdvanceWorkshopFrames();Check(s.damage.AtRepairBay,"minor person D0 reaches repair bay");}
   Debug.Log("PEDESTRIAN_REPAIR_DIAG level="+level+" can="+s.damage.CanRepair+" operating="+s.business.ledger.Operating+" restoring="+s.business.IsRestoring+" startup="+s.StartupReady+" phase="+s.accident.State.phase+" recovery="+s.accident.State.recoveryRequired+" pending="+s.damage.PendingServiceRemaining+" matchLock="+(pvp?match.ServiceRemaining(s):0)+" atBay="+s.damage.AtRepairBay+" speed="+s.taxi.body.velocity.magnitude+" trip="+s.jobs.trip.State+" durability="+s.damage.Damage.Durability);Check(s.damage.Repair(),level+" native repair accepted after person D0");if(pvp)AdvanceWorkshopFrames();
   Check(!s.damage.Disabled&&s.damage.Damage.Durability==100,level+" native paid repair completes");
   var replay=new AccidentState();replay.BeginResolved(s.accident.State.eventId,"scenario:replay",AccidentSeverityPolicy.Resolve(AccidentKind.Person,level));replay.SelectPedestrianDamage(999);
   Check(replay.ApplyVehicleDamage(s.damage.Damage)==0&&s.damage.Damage.Durability==100,level+" vehicle event ledger blocks damage replay after repair");
  }
 }
 void LegacyPendingPromotion(){
  Fresh();s.jobs.RefreshWaitingPassengers();int index=Array.FindIndex(s.jobs.people,p=>p&&p.activeInHierarchy);Check(index>=0,"legacy promotion uses actual waiting person");var point=s.jobs.people[index].transform.position;
  var legacy=new AccidentState();legacy.BeginResolved("legacy-physical","wall",AccidentSeverityPolicy.Resolve(AccidentKind.Property,AccidentSeverity.Moderate));legacy.pedestrianDamageEligible=false;
  s.accident.Restore(legacy);s.accident.Suppress(0);Pose(point-Vector3.forward*10,Vector3.forward);s.accident.ScanContacts();Pose(point+Vector3.forward*10,Vector3.forward);s.accident.Tick(.1f);
  Check(s.accident.State.Kind==AccidentKind.Person&&!s.accident.State.pedestrianDamageEligible&&s.accident.State.pedestrianDamageVersion==0&&s.damage.Damage.Durability==100,"restored old physical incident promotion adds no retroactive durability loss");Check(s.accident.Confirm(),"legacy promoted incident completes normally");
 }
 IEnumerator Run(){
  pvp=Has("-pvpAccidentSeverityUiCheck");result.scope=(pvp?"PVP explicit host-tick":"Single-player")+" deterministic native-controller pose fixtures; selected liability/score/time idempotency, JSON+state restore, workshop placement; optional native screenshots. Not physical impact, process reload, wall-clock timer, manual play or LAN.";
  Check(ContentTuningRuntime.TestMode,"-contentTest required before creating any campaign");
  if(pvp){if(!FindObjectOfType<Pvp.PvpBootstrap>()){var boot=FindObjectOfType<UnifiedModeBootstrap>();if(boot)boot.StartPvp();}Pvp.PvpBootstrap lobby;while(!(lobby=FindObjectOfType<Pvp.PvpBootstrap>()))yield return null;lobby.StartSimulation(true,600);while(!(match=FindObjectOfType<PvpSimulationMatch>())||!match.Human||!match.Human.StartupReady)yield return null;s=match.Human;}
  else {if(!FindObjectOfType<DriveSession>()){var boot=FindObjectOfType<UnifiedModeBootstrap>();if(boot)boot.StartSingle();}while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady)yield return null;}
  yield return null;while(s.business.IsRestoring)yield return null;result.savePath=s.business.SavePath;
  if(pvp)Check(result.savePath=="","PVP never writes a campaign");else Check(Path.GetFullPath(result.savePath).StartsWith(Path.GetFullPath(ContentTuningRuntime.TestDirectory)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"campaign uses private contentTest directory");Check(!s.business.ResumeBlocked,"private campaign startup is not blocked");
  foreach(var driver in pvp?(IEnumerable<DriveSession>)match.Drivers:new[]{s}){driver.business.enabled=false;driver.jobs.enabled=false;driver.requests.enabled=false;driver.law.enabled=false;driver.manners.enabled=false;driver.assisted.SetActive(false);driver.assisted.enabled=false;driver.taxi.enabled=false;driver.taxi.body.useGravity=false;driver.taxi.body.constraints=RigidbodyConstraints.FreezeAll;driver.damage.enabled=false;driver.accident.enabled=false;var brain=driver.GetComponent<PvpSimulationBot>();if(brain)brain.enabled=false;var service=driver.GetComponent<PvpSimulationBotServices>();if(service)service.enabled=false;}s.damage.BuildBays();
  if(!pvp){SolverStoppedSweepPromotion();LegacyPendingPromotion();}
  foreach(AccidentKind kind in new[]{AccidentKind.Person,AccidentKind.Vehicle,AccidentKind.Property})for(int severity=0;severity<3;severity++){
   int roll=severity==0?0:severity==1?800:999;int fee=kind==AccidentKind.Person?new[]{3000,8000,15000}[severity]:new[]{1000,3000,6000}[severity];double seconds=kind==AccidentKind.Person?new[]{45,120,240}[severity]:new[]{15,45,90}[severity];int penalty=kind==AccidentKind.Person?new[]{10,25,40}[severity]:new[]{3,8,15}[severity];
   yield return Contract(kind,(AccidentSeverity)severity,roll,fee,seconds,penalty);
  }
  yield return DisabledPedestrianContracts();if(pvp)yield return HealthyBotDeparture();if(Has("-accidentSeverityCapture"))yield return Views();
 }
}
