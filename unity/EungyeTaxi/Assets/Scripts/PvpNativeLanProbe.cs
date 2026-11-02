using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Explicit acceptance fixture: two real player processes, native WheelColliders,
// input messages and authoritative snapshots. Phase files coordinate assertions only.
public sealed class PvpNativeLanProbe : MonoBehaviour {
 [Serializable] public sealed class Metrics {
  public string role,map,error;public bool passed;public int slot,checks,failed,driverCount,wheels,score,cash,durability,claim=-1;
  public float elapsed,metres,maxSpeed;public string trip,offer;public List<string> evidence=new List<string>();
 }
 [Serializable] public sealed class Model {public string offer,trip;public int pickup,score,cash,durability,npcs,manner;public float clock;}
 Metrics metrics=new Metrics();string directory,address;ushort port;bool host,reconnect,completed;float end;PvpNativeLan lan;
 static string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);return at>=0&&at+1<args.Length?args[at+1]:fallback;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeLanCheck")>=0&&!FindObjectOfType<PvpNativeLanProbe>()){var root=new GameObject("Native LAN acceptance probe");DontDestroyOnLoad(root);root.AddComponent<PvpNativeLanProbe>();}}
 void Check(bool value,string text){metrics.checks++;if(!value)metrics.failed++;metrics.evidence.Add((value?"PASS ":"FAIL ")+text);Debug.Log("PVP_NATIVE_PROBE "+metrics.role+" "+(value?"PASS ":"FAIL ")+text);if(!value)throw new InvalidOperationException(text);}
 string FileFor(string phase){return Path.Combine(directory,phase+".json");}
 void Mark(string phase,object value=null){File.WriteAllText(FileFor(phase),value==null?"{}":JsonUtility.ToJson(value,true));}
 bool Has(string phase){return File.Exists(FileFor(phase));}
 IEnumerator Until(Func<bool> condition,string label,float seconds=90){float limit=Mathf.Min(end,Time.realtimeSinceStartup+seconds);while(!condition()&&Time.realtimeSinceStartup<limit)yield return null;Check(condition(),label);}
 IEnumerator Bootstrap(){
  if(!FindObjectOfType<Pvp.PvpBootstrap>()){var unified=FindObjectOfType<UnifiedModeBootstrap>();if(unified)unified.StartPvp();}
  yield return Until(()=>FindObjectOfType<Pvp.PvpBootstrap>()!=null,"PVP bootstrap available");
  FindObjectOfType<Pvp.PvpBootstrap>().StartNativeLan(host,address,port,300);
  yield return Until(()=>PvpNativeLan.Instance!=null,"native transport created");lan=PvpNativeLan.Instance;
 }
 IEnumerator Start(){
  host=Arg("-pvpNativeLanRole","client")=="host";metrics.role=host?"host":"client";
  directory=Path.GetFullPath(Arg("-pvpNativeLanOutput",Path.Combine(Application.persistentDataPath,"native-lan-check")));Directory.CreateDirectory(directory);
  address=Arg("-pvpNativeLanAddress","127.0.0.1");port=ushort.Parse(Arg("-pvpNativeLanPort","17971"));reconnect=Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpNativeLanReconnect")>=0;
  end=Time.realtimeSinceStartup+240;Application.runInBackground=true;
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
  var match=lan.Match;var own=match.Human;metrics.slot=lan.LocalSlot;metrics.map=own.world.map.mapId;metrics.driverCount=match.Drivers.Count;metrics.wheels=own.taxi.wheels.Length;
  Check(metrics.map=="pvp-city-a"&&metrics.driverCount==4&&metrics.wheels==4,"native city and four original physical taxis");
  foreach(var d in match.Drivers)Check(d.business.SimulationSession&&d.business.SavePath==""&&d.taxi.wheels.Length==4,"native sessions retain campaign save isolation");
  if(host)yield return Host();else yield return Client();
  yield return Cleanup();
 }
 IEnumerator Host(){
  var match=lan.Match;var guest=match.Drivers[1];var car=guest.taxi;int carId=car.GetInstanceID();
  Check(guest.GetComponent<PvpSimulationBot>()&&!guest.GetComponent<PvpSimulationBot>().enabled,"guest seat disables native bot while connected");
  Vector3 before=car.transform.position;float startTime=Time.realtimeSinceStartup,maxSpeed=0;
  while(!Has("drive-done")&&Time.realtimeSinceStartup<end){maxSpeed=Mathf.Max(maxSpeed,car.body.velocity.magnitude);yield return null;}
  metrics.metres=Vector3.Distance(before,car.transform.position);metrics.maxSpeed=maxSpeed;Check(Has("drive-done")&&metrics.metres>2&&maxSpeed>.5f,"guest pedal messages move host WheelCollider taxi; metres="+metrics.metres+" speed="+maxSpeed);
  yield return Until(()=>Has("accepted"),"client native acceptance completes",35);
  Check(guest.jobs.Active&&guest.jobs.current!=null,"accepted guest native trip exists on host");
  Check(lan.ProbeRejectedTripCommands>=1&&lan.ProbeRejectedClaimCommands>=2,"host rejects stale trip and invalid/duplicate claim at validation gates");
  int pickup=guest.jobs.current.pickup;Check(!match.Ledger.Available(pickup,0)&&match.Ledger.Available(pickup,1),"host passenger claim belongs only to guest");
  var model=new Model{offer=guest.jobs.current.candidateSeed,trip=guest.law.tripId,pickup=pickup,score=match.Score(1),cash=guest.business.ledger.Account.Cash,durability=guest.damage.Damage.Durability,npcs=match.Human.traffic.cars.Count,clock=match.Human.traffic.Clock,manner=guest.manners.ledger.Score};
  Mark("host-model",model);yield return Until(()=>Has("replica-checked"),"client compares authoritative native model");
  yield return Until(()=>lan.ConnectedGuests==0,"guest disconnect observed",40);
  Check(match.Drivers[1]==guest&&guest.taxi==car&&car.GetInstanceID()==carId&&guest.GetComponent<PvpSimulationBot>().enabled,"disconnect hands same original taxi to native bot");
  Check(guest.jobs.current!=null&&guest.jobs.current.candidateSeed==model.offer&&match.Ledger.Available(pickup,1),"disconnect preserves accepted passenger and exclusive claim");Mark("takeover");
  if(reconnect){yield return Until(()=>lan.ConnectedGuests==1&&Has("reconnected"),"same-process ticket reconnect completes",80);Check(match.Drivers[1]==guest&&guest.taxi==car&&!guest.GetComponent<PvpSimulationBot>().enabled,"ticket reconnect reclaims same host taxi");Mark("reconnect-checked");yield return Until(()=>lan.ConnectedGuests==0,"reconnected guest exits cleanly",40);}
  yield return Until(()=>Has("client-clean"),"client returns to clean mode menu",40);metrics.elapsed=match.Elapsed;
 }
 IEnumerator Client(){
  yield return Until(()=>lan.HasOwnFrame,"first own native snapshot applied");
  var own=lan.Match.Human;Check(own.taxi.body.isKinematic,"client own taxi is a server pose replica");
  Vector3 before=own.taxi.transform.position;float driveUntil=Time.realtimeSinceStartup+6;
  lan.ProbeInput=()=>new PvpNativeDrive{throttle=Time.realtimeSinceStartup<driveUntil?.35f:0,steer=0,brake=Time.realtimeSinceStartup>=driveUntil,blocked=false};
  while(Time.realtimeSinceStartup<driveUntil+2)yield return null;
  metrics.metres=Vector3.Distance(before,own.taxi.transform.position);Check(metrics.metres>2,"client receives moving native taxi pose");Mark("drive-done");
  Check(!own.jobs.Active,"guest starts without fabricated accepted contract");
  lan.ProbeSendCommand(new PvpNativeCommand{action="accept",token="invalid-probe-claim",trip=own.law.tripId,request=own.requests.request.Id});
  yield return new WaitForSecondsRealtime(1);Check(!own.jobs.Active,"invalid claim token cannot accept a native passenger");
  float limit=Time.realtimeSinceStartup+25;
  while(!own.jobs.Active&&Time.realtimeSinceStartup<limit){for(int i=0;i<own.jobs.offers.Length;i++){var offer=own.jobs.offers[i];if(offer==null)continue;own.jobs.Accept(i);yield return new WaitForSecondsRealtime(.7f);if(own.jobs.Active)break;}}
  Check(own.jobs.Active,"valid candidate command accepts original passenger");string accepted=own.jobs.current.candidateSeed;string trip=own.law.tripId;var state=own.jobs.trip.State;
  lan.ProbeSendCommand(new PvpNativeCommand{action="interact",trip="stale-probe-trip",request=own.requests.request.Id});
  lan.ProbeSendCommand(new PvpNativeCommand{action="accept",token=accepted,trip=trip,request=own.requests.request.Id});
  yield return new WaitForSecondsRealtime(1);Check(own.jobs.Active&&own.jobs.current.candidateSeed==accepted&&own.jobs.trip.State==state,"stale trip and duplicate claim commands preserve active contract");Mark("accepted");
  yield return Until(()=>Has("host-model"),"host publishes native model");var model=JsonUtility.FromJson<Model>(File.ReadAllText(FileFor("host-model")));
  yield return Until(()=>own.jobs.current!=null&&own.jobs.current.candidateSeed==model.offer&&own.business.ledger.Account.Cash==model.cash&&own.damage.Damage.Durability==model.durability,"economy damage and passenger model replicated",10);
  Check(lan.Match.Score(0)==model.score&&lan.Match.Ledger.Available(model.pickup,0)&&!lan.Match.Ledger.Available(model.pickup,1),"score and exclusive claim mapped into client local seat");
  Check(own.traffic.cars.Count==model.npcs&&model.npcs==72&&Mathf.Abs(own.traffic.Clock-model.clock)<10,"shared 72 NPC population and signal clock mirrored");
  Check(own.manners.ledger.Score==model.manner,"native driving manner ledger mirrored");
  metrics.trip=own.law.tripId;metrics.offer=accepted;metrics.score=model.score;metrics.cash=model.cash;metrics.durability=model.durability;metrics.claim=model.pickup;Mark("replica-checked");
  int slot=lan.LocalSlot;lan.ProbeInput=null;lan.LeaveToMenu();yield return Until(()=>Has("takeover"),"host confirms same-car bot takeover",40);
  if(reconnect){yield return Bootstrap();yield return Until(()=>lan.Racing&&lan.Match&&lan.HasOwnFrame,"reconnect world ready",100);Check(lan.LocalSlot==slot,"same-process reconnect retains ticket seat");Mark("reconnected");yield return Until(()=>Has("reconnect-checked"),"host confirms reconnect identity");lan.LeaveToMenu();}
 }
 IEnumerator Cleanup(){
  if(PvpNativeLan.Instance)PvpNativeLan.Instance.LeaveToMenu();yield return null;yield return null;
  yield return Until(()=>!PvpNativeLan.Instance&&FindObjectsOfType<DriveSession>().Length==0,"native transport and all taxi sessions removed",20);
  var unified=FindObjectOfType<UnifiedModeBootstrap>();Check(!unified||UnifiedModeBootstrap.Selected==UnifiedModeBootstrap.Mode.Menu,"unified mode selection restored");
  if(!host)Mark("client-clean");
 }
 void Finish(){if(completed)return;completed=true;metrics.passed=metrics.failed==0;File.WriteAllText(Path.Combine(directory,metrics.role+"-metrics.json"),JsonUtility.ToJson(metrics,true));Debug.Log("PVP_NATIVE_LAN_"+(metrics.passed?"OK ":"FAIL ")+metrics.role);Application.Quit(metrics.passed?0:1);}
}
