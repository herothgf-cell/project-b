using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Explicit acceptance fixture: two real player processes, native WheelColliders,
// input messages and authoritative snapshots. Phase files coordinate assertions only.
[DefaultExecutionOrder(20000)]
public sealed class PvpNativeLanProbe : MonoBehaviour {
 [Serializable] public sealed class Metrics {
  public int movingFrames,frozenFrames;public float movingMaxSpeed;public double movingSeconds,movingMetres,maxFreezeMs;
  public string product;public string graphics;public int width,height;public PvpNativePerfView[] performance;public string role,map,error;public bool passed;public int slot,checks,failed,driverCount,wheels,score,cash,durability,claim=-1;
  public float elapsed,metres,maxSpeed;public string trip,offer;public List<string> evidence=new List<string>();
 }
 [Serializable] public sealed class Model {public string offer,trip;public int pickup,score,cash,durability,npcs,manner;public float clock;}
 float motionSeconds;
 void LateUpdate(){if(lan)PvpNativeReplica.TraceMotion(lan);}
 Metrics metrics=new Metrics();string directory,address;ushort port;bool host,reconnect,completed,motionOnly;float end;PvpNativeLan lan;
 static string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);return at>=0&&at+1<args.Length?args[at+1]:fallback;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeLanCheck")>=0&&!FindObjectOfType<PvpNativeLanProbe>()){var root=new GameObject("Native LAN acceptance probe");DontDestroyOnLoad(root);root.AddComponent<PvpNativeLanProbe>();}}
 void Check(bool value,string text){metrics.checks++;if(!value)metrics.failed++;metrics.evidence.Add((value?"PASS ":"FAIL ")+text);Debug.Log("PVP_NATIVE_PROBE "+metrics.role+" "+(value?"PASS ":"FAIL ")+text);if(!value)throw new InvalidOperationException(text);}
 string FileFor(string phase){return Path.Combine(directory,phase+".json");}
 void Mark(string phase,object value=null){string path=FileFor(phase),temp=path+".tmp";File.WriteAllText(temp,value==null?"{}":JsonUtility.ToJson(value,true));File.Move(temp,path);}
 bool Has(string phase){return File.Exists(FileFor(phase));}
 IEnumerator Until(Func<bool> condition,string label,float seconds=90){float limit=Mathf.Min(end,Time.realtimeSinceStartup+seconds);while(!condition()&&Time.realtimeSinceStartup<limit)yield return null;Check(condition(),label);}
 IEnumerator Bootstrap(){
  if(!FindObjectOfType<Pvp.PvpBootstrap>()){var unified=FindObjectOfType<UnifiedModeBootstrap>();if(unified)unified.StartPvp();}
  yield return Until(()=>FindObjectOfType<Pvp.PvpBootstrap>()!=null,"PVP bootstrap available");
  FindObjectOfType<Pvp.PvpBootstrap>().StartNativeLan(host,address,port,motionOnly?1200:300);
  yield return Until(()=>PvpNativeLan.Instance!=null,"native transport created");lan=PvpNativeLan.Instance;
 }
 IEnumerator Start(){
  host=Arg("-pvpNativeLanRole","client")=="host";metrics.role=host?"host":"client";
  directory=Path.GetFullPath(Arg("-pvpNativeLanOutput",Path.Combine(Application.persistentDataPath,"native-lan-check")));Directory.CreateDirectory(directory);
  address=Arg("-pvpNativeLanAddress","127.0.0.1");port=ushort.Parse(Arg("-pvpNativeLanPort","17971"));reconnect=Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeLanReconnect")>=0;
  motionOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeMotionOnly")>=0;
  motionSeconds=Mathf.Clamp(float.Parse(Arg("-pvpNativeMotionSeconds","0"),System.Globalization.CultureInfo.InvariantCulture),0,60);
  end=Time.realtimeSinceStartup+300;Application.runInBackground=true;
  // Drive a nested coroutine explicitly so an exception always produces JSON and a nonzero exit.
  var stack=new Stack<IEnumerator>();stack.Push(Run());
  while(stack.Count>0){object current=null;Exception failure=null;bool advanced=false;try{advanced=stack.Peek().MoveNext();if(advanced)current=stack.Peek().Current;}catch(Exception e){failure=e;}
   if(failure!=null){metrics.failed++;metrics.error=failure.ToString();Debug.LogException(failure);break;}
   if(!advanced){stack.Pop();continue;}var nested=current as IEnumerator;if(nested!=null){stack.Push(nested);continue;}yield return current;
  }
  Finish();
 }
 IEnumerator Run(){
  yield return Bootstrap();
  if(host){yield return Until(()=>lan.ConnectedGuests==1,"one LAN guest connected");lan.StartMatch();}
  yield return Until(()=>lan&&lan.Match&&lan.Racing,"all participants pass world-ready barrier",120);
  var match=lan.Match;Check(lan.Label(0).Contains(lan.IsHost?"검증_host":"검증_client")&&lan.Label(1).Contains(lan.IsHost?"검증_client":"검증_host"),"host retains and broadcasts both nicknames");var own=match.Human;metrics.slot=lan.LocalSlot;metrics.map=own.world.map.mapId;metrics.driverCount=match.Drivers.Count;metrics.wheels=own.taxi.wheels.Length;
  Check(metrics.map=="pvp-city-a"&&metrics.driverCount==4&&metrics.wheels==4,"native city and four original physical taxis");
  foreach(var d in match.Drivers)Check(d.business.SimulationSession&&d.business.SavePath==""&&d.taxi.wheels.Length==4,"native sessions retain campaign save isolation");
  metrics.product=Application.productName;Check(metrics.product=="택시 시뮬레이션","latest main product name retained");metrics.graphics=SystemInfo.graphicsDeviceType.ToString();metrics.width=Screen.width;metrics.height=Screen.height;
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeRenderCheck")>=0){Check(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null,"graphics device is active");bool captured=false;StartCoroutine(CaptureFrame(()=>captured=true));yield return Until(()=>captured,"actual rendered frame captured",8);if(!host){var map=own.GetComponent<FullMapOverlay>();Check(map&&map.Open(),"native A2 full map opens");yield return null;yield return null;captured=false;StartCoroutine(CaptureFrame(()=>captured=true,"client-a3-map.png"));yield return Until(()=>captured,"actual A2 map render captured",8);map.Close();}}
  if(host)yield return Host();else yield return Client();
  yield return Cleanup();
 }
 IEnumerator Host(){
  var match=lan.Match;
  int listed=0;float shortest=0,longest=0;foreach(var offer in match.SharedCalls.Offers){if(offer==null)continue;listed++;int nearer=0;for(int d=0;d<match.Human.jobs.stops.Length;d++){if(d==offer.pickup)continue;float distance=match.Human.jobs.nav.Distance(match.Human.jobs.stops[offer.pickup],match.Human.jobs.stops[d]);if(distance>=80&&distance<offer.distance-.1f)nearer++;}Check(nearer<3,"short-match offer uses one of three nearest reachable drops");shortest=shortest==0?offer.distance:Mathf.Min(shortest,offer.distance);longest=Mathf.Max(longest,offer.distance);}Check(listed>=6,"short match keeps a populated shared call list");Debug.Log("PVP_SHORT_CALLS count="+listed+" shortest="+shortest+" longest="+longest);
  var guest=match.Drivers[1];var car=guest.taxi;int carId=car.GetInstanceID();
  Check(guest.GetComponent<PvpSimulationBot>()&&!guest.GetComponent<PvpSimulationBot>().enabled,"guest seat disables native bot while connected");
  float menuElapsed=match.Elapsed;lan.ToggleMenu();yield return new WaitForSecondsRealtime(.5f);Check(!match.Human.paused&&Time.timeScale==1&&match.Elapsed>menuElapsed&&PvpNativeLan.UiBlocked(match.Human),"shared clock continues while own keyboard/pad menu brakes only local taxi");lan.ToggleMenu();
  Vector3 before=car.transform.position;float startTime=Time.realtimeSinceStartup,maxSpeed=0;
  while(!Has("drive-done")&&Time.realtimeSinceStartup<end){maxSpeed=Mathf.Max(maxSpeed,car.body.velocity.magnitude);yield return null;}
  metrics.metres=Vector3.Distance(before,car.transform.position);metrics.maxSpeed=maxSpeed;Check(Has("drive-done")&&metrics.metres>2&&maxSpeed>.5f,"guest pedal messages move host WheelCollider taxi; metres="+metrics.metres+" speed="+maxSpeed);
  if(motionSeconds>0){
   float stopping=Time.realtimeSinceStartup+5;while(car.body.velocity.magnitude>.4f&&Time.realtimeSinceStartup<stopping)yield return null;
   guest.assisted.allowAutomated=true;Check(guest.assisted.SetActive(true),"motion run arms original lane assistance without pose control; "+guest.assisted.Status+" speed="+car.Speed+" pos="+car.body.position);PvpNativePerf.Moving=true;Mark("motion-ready");
   bool captureChecked=false;float maxOffset=0;
   while(!Has("motion-done")&&Time.realtimeSinceStartup<end){
    if(car.body.velocity.magnitude>2){maxOffset=Mathf.Max(maxOffset,Vector3.Distance(car.transform.position,car.body.position));if(!captureChecked||Time.frameCount%60==0){var captured=PvpNativeReplica.Capture(lan,1,false).actors[1];if(Vector3.Distance(captured.position,car.body.position)>.0001f||Quaternion.Angle(captured.rotation,car.body.rotation)>.01f)throw new Exception("Physics pose and snapshot timestamp source diverged");captureChecked=true;}}
    yield return null;
   }
   var npcFrame=PvpNativeReplica.Capture(lan,1,false);bool npcPhysics=true;foreach(var npc in npcFrame.npcs)npcPhysics&=Vector3.Distance(npc.position,match.Human.traffic.cars[npc.index].PhysicsPosition)<.0001f;Check(npcPhysics,"shared NPC snapshots use the same physics pose clock");
   PvpNativePerf.Moving=false;Check(captureChecked,"moving capture uses physics pose consistently; maximum render/body offset="+maxOffset);guest.assisted.SetActive(false);guest.assisted.allowAutomated=false;Mark("motion-host-done");
  }
  if(motionOnly){yield return Until(()=>lan.ConnectedGuests==0&&Has("client-clean"),"motion-only guest exits to menu",20);yield break;}
  yield return Until(()=>Has("accepted"),"client native acceptance completes",35);
  Check(guest.jobs.Active&&guest.jobs.current!=null,"accepted guest native trip exists on host");
  Check(lan.ProbeRejectedTripCommands>=2&&lan.ProbeRejectedClaimCommands>=2,"host rejects stale trip/situation and invalid/duplicate claim at validation gates");
  int pickup=guest.jobs.current.pickup;Check(!match.Ledger.Available(pickup,0)&&match.Ledger.Available(pickup,1),"host passenger claim belongs only to guest");
  var model=new Model{offer=guest.jobs.current.candidateSeed,trip=guest.law.tripId,pickup=pickup,score=match.Score(1),cash=guest.business.ledger.Account.Cash,durability=guest.damage.Damage.Durability,npcs=match.Human.traffic.cars.Count,clock=match.Human.traffic.Clock,manner=guest.manners.ledger.Score};
  Mark("host-model",model);yield return Until(()=>Has("replica-checked"),"client compares authoritative native model");
  // Explicit model fixture, not a claim of a physically driven red-light crossing.
  Check(guest.jobs.trip.Board(),"notification fixture boards accepted model without moving taxi");var onboard=guest.jobs.CaptureLive();onboard.hasDrunk=true;onboard.drunk=DrunkRideState.Create("notice-fixture",false,DrunkRideCase.Normal);onboard.startSatisfaction=70;guest.jobs.RestoreLive(onboard);guest.law.tripId=guest.law.ids.Next("notice-fixture");guest.manners.BeginRide();Mark("notice-baseline");
  yield return Until(()=>Has("notice-ready"),"guest establishes onboard notification baseline",10);
  var violation=LawEvaluator.Signal("native-notice-fixture",1,new RoadObservation{known=true,forward=SignalColor.Red},false,SignalColor.Red);guest.law.ledger.RecordIncident(violation);guest.manners.Deduct("native-notice-fixture",12,"신호 위반",0);Mark("notice-event");
  yield return Until(()=>Has("notice-checked"),"guest sees authoritative score and satisfaction notices",12);
  yield return Until(()=>lan.ConnectedGuests==0,"guest disconnect observed",40);
  Check(match.Drivers[1]==guest&&guest.taxi==car&&car.GetInstanceID()==carId&&guest.GetComponent<PvpSimulationBot>().enabled,"disconnect hands same original taxi to native bot");
  Check(guest.jobs.current!=null&&guest.jobs.current.candidateSeed==model.offer&&match.Ledger.Available(pickup,1),"disconnect preserves accepted passenger and exclusive claim");Mark("takeover");
  if(reconnect){yield return Until(()=>lan.ConnectedGuests==1&&Has("reconnected"),"same-process ticket reconnect completes",80);Check(match.Drivers[1]==guest&&guest.taxi==car&&!guest.GetComponent<PvpSimulationBot>().enabled,"ticket reconnect reclaims same host taxi");Mark("reconnect-checked");yield return Until(()=>lan.ConnectedGuests==0,"reconnected guest exits cleanly",40);}
  yield return Until(()=>Has("client-clean"),"client returns to clean mode menu",40);metrics.elapsed=match.Elapsed;
 }
 IEnumerator Client(){
  yield return Until(()=>lan.HasOwnFrame,"first own native snapshot applied");
  var own=lan.Match.Human;Check(lan.Label(0).Contains("검증_client")&&lan.Label(1).Contains("검증_host"),"guest receives host-authoritative nicknames in local slot order");Check(!own.taxi.body.isKinematic&&own.taxi.NetworkPredicted&&lan.Prediction!=null,"only local owner runs native predicted WheelCollider physics");
  for(int i=1;i<lan.Match.Drivers.Count;i++)Check(lan.Match.Drivers[i].taxi.body.isKinematic&&!lan.Match.Drivers[i].taxi.NetworkPredicted,"other taxis remain server interpolated replicas "+i);
  yield return Until(()=>lan.Racing,"owner prediction starts on shared race clock",10);
  yield return Until(()=>lan.Prediction.LastAck>0,"numbered control stream acknowledged before loss probes",10);
  lan.ProbeHoldInputs=true;int sentBefore=lan.ProbeInputCount;Vector3 predictionStart=own.taxi.body.position;
  lan.ProbeInput=()=>new PvpNativeDrive{throttle=1,steer=.25f,blocked=false};yield return new WaitForSecondsRealtime(.35f);
  var localTravel=own.taxi.body.position-predictionStart;localTravel.y=0;
  Check(lan.ProbeInputCount==sentBefore&&lan.Prediction.Steps>=5&&localTravel.magnitude>.002f,"native owner moves before throttle reaches host; distance="+localTravel.magnitude);
  Check(own.taxi.autoSteer>.1f&&Mathf.Abs(own.taxi.wheels[0].steerAngle)>.1f,"steering responds locally while input transmission withheld; auto="+own.taxi.autoSteer+" wheel="+own.taxi.wheels[0].steerAngle+" "+lan.Prediction.Diagnostics);
  lan.ProbeHoldInputs=false;lan.ProbeInput=()=>new PvpNativeDrive{throttle=.35f,steer=0,blocked=false};
  yield return Until(()=>lan.Prediction.LastAck>0,"input acknowledgement resumes before snapshot-gap probe",10);
  yield return new WaitForSecondsRealtime(.15f);
  int beforeGapResets=lan.Prediction.HardResets;lan.ProbeHoldFrames=true;Vector3 gapStart=own.taxi.body.position;
  yield return new WaitForSecondsRealtime(.3f);Check(Vector3.Distance(gapStart,own.taxi.body.position)>.005f,"native owner keeps simulating through brief snapshot delivery gap");
  lan.ProbeHoldFrames=false;yield return new WaitForSecondsRealtime(.8f);
  Check(lan.Prediction.HardResets==beforeGapResets,"snapshot recovery reconciles without snapping owner to old server pose");
  var facilities=own.services.FacilityOptions(0);Check(facilities.Count>0&&facilities.TrueForAll(o=>o.QueueKnown),"guest facility comparison receives host queue and physical occupancy facts");
  Vector3 before=own.taxi.transform.position;float driveUntil=Time.realtimeSinceStartup+6;
  lan.ProbeInput=()=>new PvpNativeDrive{throttle=Time.realtimeSinceStartup<driveUntil?.35f:0,steer=0,brake=Time.realtimeSinceStartup>=driveUntil,blocked=false};
  while(Time.realtimeSinceStartup<driveUntil+2)yield return null;
  metrics.metres=Vector3.Distance(before,own.taxi.transform.position);Check(metrics.metres>2,"predicted native taxi continues under acknowledged server controls");
  Check(lan.Prediction.LastAck>0&&lan.Prediction.Corrections>0,"host input acknowledgements reconcile historical native owner states");
  Check(lan.Prediction.HardResets==1,"normal acknowledged driving does not repeatedly snap the owner");
  lan.ProbeInput=()=>new PvpNativeDrive{throttle=-.35f,blocked=false};yield return new WaitForSecondsRealtime(1.3f);
  Check(own.taxi.Speed<-.05f,"native predicted reverse accepts local pedal and host authority");
  lan.ProbeInput=()=>new PvpNativeDrive{brake=true,blocked=false};yield return new WaitForSecondsRealtime(1);
  Check(own.taxi.body.velocity.magnitude<.6f,"native predicted brake stops physical owner");Mark("drive-done");
  if(motionSeconds>0){
   yield return Until(()=>Has("motion-ready")&&own.assisted.Active&&own.jobs.nav.freeGuidance&&own.jobs.nav.hdRoute!=null,"host authoritative free guide ready for native predicted steering",10);
   float motionUntil=Time.realtimeSinceStartup+motionSeconds;PvpNativePerf.Moving=true;
   lan.ProbeInput=()=>new PvpNativeDrive{throttle=Time.realtimeSinceStartup<motionUntil?1:0,steer=0,brake=Time.realtimeSinceStartup>=motionUntil,blocked=false};
   while(Time.realtimeSinceStartup<motionUntil)yield return null;
   PvpNativePerf.Moving=false;metrics.movingMaxSpeed=PvpNativePerf.MovingMaxSpeed;metrics.movingFrames=PvpNativePerf.MovingFrames;metrics.frozenFrames=PvpNativePerf.FrozenFrames;metrics.movingSeconds=PvpNativePerf.MovingSeconds;metrics.movingMetres=PvpNativePerf.MovingMetres;metrics.maxFreezeMs=PvpNativePerf.MaxFreezeMs;
   Check(metrics.movingSeconds>=8&&metrics.movingMetres>=50,"sustained moving render trace rather than parked FPS; seconds="+metrics.movingSeconds+" metres="+metrics.movingMetres+" maxFreezeMs="+metrics.maxFreezeMs);
   Mark("motion-done");yield return Until(()=>Has("motion-host-done"),"host confirms physical motion capture consistency",10);yield return new WaitForSecondsRealtime(2);
  }
  if(motionOnly){lan.ProbeInput=null;lan.LeaveToMenu();yield break;}
  own.signals.Toggle(1);yield return Until(()=>own.signals.direction==1&&!own.signals.Hazards,"manual indicator command returns authoritative host state",10);
  own.signals.ToggleHazards();yield return Until(()=>own.signals.Hazards&&own.signals.direction==0,"host hazards override replica cancellation",10);own.signals.ToggleHazards();
  Check(!own.jobs.Active,"guest starts without fabricated accepted contract");
  lan.ProbeSendCommand(new PvpNativeCommand{action="accept",token="invalid-probe-claim",trip=own.law.tripId,request=own.requests.request.Id});
  yield return new WaitForSecondsRealtime(1);Check(!own.jobs.Active,"invalid claim token cannot accept a native passenger");
  float limit=Time.realtimeSinceStartup+25;
  while(!own.jobs.Active&&Time.realtimeSinceStartup<limit){for(int i=0;i<own.jobs.offers.Length;i++){var offer=own.jobs.offers[i];if(offer==null)continue;own.jobs.Accept(i);yield return new WaitForSecondsRealtime(.7f);if(own.jobs.Active)break;}}
  Check(own.jobs.Active,"valid candidate command accepts original passenger");string accepted=own.jobs.current.candidateSeed;string trip=own.law.tripId;var state=own.jobs.trip.State;
  lan.ProbeSendCommand(new PvpNativeCommand{action="interact",trip="stale-probe-trip",request=own.requests.request.Id});
  lan.ProbeSendCommand(new PvpNativeCommand{action="situation",value=0,token="stale-situation",trip="stale-probe-trip",request=own.requests.request.Id});
  lan.ProbeSendCommand(new PvpNativeCommand{action="accept",token=accepted,trip=trip,request=own.requests.request.Id});
  yield return new WaitForSecondsRealtime(1);Check(own.jobs.Active&&own.jobs.current.candidateSeed==accepted&&own.jobs.trip.State==state,"stale trip and duplicate claim commands preserve active contract");var savedTrip=own.jobs.trip.Snapshot();var savedRequest=own.requests.CaptureLive();bool savedLarge=own.jobs.nav.PreferLargeRoad,savedFast=own.jobs.nav.PreferFastest,wasApplying=PvpNativeLan.ApplyingReplica;
  try{PvpNativeLan.ApplyingReplica=true;Check(own.jobs.trip.Board(),"transient client request-restore fixture enters onboard model");own.jobs.nav.SetPassengerPreference(false,false);var repeated=own.requests.CaptureLive();var route=own.jobs.nav.hdRoute;own.requests.RestoreLive(repeated);Check(route!=null&&ReferenceEquals(route,own.jobs.nav.hdRoute),"unchanged onboard short-road preference keeps existing route");}
  finally{own.jobs.trip.Restore(savedTrip);own.requests.RestoreLive(savedRequest);own.jobs.nav.SetPassengerPreference(savedLarge,savedFast);PvpNativeLan.ApplyingReplica=wasApplying;}
  PvpNativePerf.Steady=true;yield return new WaitForSecondsRealtime(10);PvpNativePerf.Steady=false;Mark("accepted");
  yield return Until(()=>Has("host-model"),"host publishes native model");var model=JsonUtility.FromJson<Model>(File.ReadAllText(FileFor("host-model")));
  yield return Until(()=>own.jobs.current!=null&&own.jobs.current.candidateSeed==model.offer&&own.business.ledger.Account.Cash==model.cash&&own.damage.Damage.Durability==model.durability,"economy damage and passenger model replicated",10);
  Check(lan.Match.Score(0)==model.score&&lan.Match.Ledger.Available(model.pickup,0)&&!lan.Match.Ledger.Available(model.pickup,1),"score and exclusive claim mapped into client local seat");
  Check(own.traffic.cars.Count==model.npcs&&model.npcs==72&&Mathf.Abs(own.traffic.Clock-model.clock)<10,"shared 72 NPC population and signal clock mirrored");
  Check(own.manners.ledger.Score==model.manner,"native driving manner ledger mirrored");
  metrics.trip=own.law.tripId;metrics.offer=accepted;metrics.score=model.score;metrics.cash=model.cash;metrics.durability=model.durability;metrics.claim=model.pickup;Mark("replica-checked");
  yield return Until(()=>Has("notice-baseline")&&own.jobs.HasPassengerScore,"onboard model notification baseline arrives",10);
  var notices=own.GetComponent<DriveNotifications>();Check(notices!=null,"native guest has original notification UI");notices.RefreshScores();Mark("notice-ready");
  yield return Until(()=>Has("notice-event")&&Array.Exists(notices.VisibleTexts,t=>t.StartsWith("운전 |")&&t.Contains("신호"))&&Array.Exists(notices.VisibleTexts,t=>t.StartsWith("만족도 |")&&t.Contains("신호")),"host signal incident produces guest driving and passenger satisfaction notices",10);
  int noticeCount=notices.VisibleTexts.Length;yield return new WaitForSecondsRealtime(1);Check(notices.VisibleTexts.Length==noticeCount,"unchanged full snapshots retain visible notices without duplicates");
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeRenderCheck")>=0){bool captured=false;StartCoroutine(CaptureFrame(()=>captured=true,"client-notices.png"));yield return Until(()=>captured,"notification card render captured",8);}
  Mark("notice-checked");
  int slot=lan.LocalSlot;lan.ProbeInput=null;lan.LeaveToMenu();yield return Until(()=>Has("takeover"),"host confirms same-car bot takeover",40);
  if(reconnect){yield return Bootstrap();yield return Until(()=>lan.Racing&&lan.Match&&lan.HasOwnFrame,"reconnect world ready",100);Check(lan.LocalSlot==slot,"same-process reconnect retains ticket seat");Mark("reconnected");yield return Until(()=>Has("reconnect-checked"),"host confirms reconnect identity");lan.LeaveToMenu();}
 }
 IEnumerator CaptureFrame(Action done,string name=null){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,name??metrics.role+"-render.png"),texture.EncodeToPNG());Destroy(texture);done();}
 IEnumerator Cleanup(){
  if(PvpNativeLan.Instance)PvpNativeLan.Instance.LeaveToMenu();yield return null;yield return null;
  yield return Until(()=>!PvpNativeLan.Instance&&FindObjectsOfType<DriveSession>().Length==0,"native transport and all taxi sessions removed",20);
  var unified=FindObjectOfType<UnifiedModeBootstrap>();Check(!unified||UnifiedModeBootstrap.Selected==UnifiedModeBootstrap.Mode.Menu,"unified mode selection restored");
  if(!host)Mark("client-clean");
 }
 void Finish(){if(completed)return;completed=true;metrics.passed=metrics.failed==0;metrics.performance=PvpNativePerf.Capture();PvpNativePerf.WriteMotion(Path.Combine(directory,metrics.role+"-motion.csv"));File.WriteAllText(Path.Combine(directory,metrics.role+"-metrics.json"),JsonUtility.ToJson(metrics,true));Debug.Log("PVP_NATIVE_LAN_"+(metrics.passed?"OK ":"FAIL ")+metrics.role);Application.Quit(metrics.passed?0:1);}
}
