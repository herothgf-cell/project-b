using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
// Runtime fixtures isolate saves; pose checks are separate from PhysX corner tests.
public sealed class CasualDriveProbe:MonoBehaviour {
 DriveSession s;int checks;bool pvp;
 static bool Has(string flag){return Array.IndexOf(Environment.GetCommandLineArgs(),flag)>=0;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Has("-casualDriveTest")||Has("-pvpCasualDriveCheck"))new GameObject("Casual control acceptance").AddComponent<CasualDriveProbe>();}
 void Check(bool value,string text){checks++;Debug.Log("CASUAL_PLAYER_"+(value?"PASS ":"FAIL ")+text);if(!value)throw new Exception(text);}
 IEnumerator Start(){
  pvp=Has("-pvpCasualDriveCheck");float deadline=Time.realtimeSinceStartup+90;
  while(!s&&Time.realtimeSinceStartup<deadline){s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot);if(pvp&&!s){var shell=FindObjectOfType<Pvp.PvpBootstrap>();if(shell)shell.StartSimulation(true,600);}yield return null;}
  while(s&&!s.StartupReady&&Time.realtimeSinceStartup<deadline)yield return null;
  bool ok=false;try{Prepare();ok=true;}catch(Exception e){Debug.LogException(e);}
  if(ok){yield return new WaitForSeconds(.6f);try{DemandAndWire();}catch(Exception e){ok=false;Debug.LogException(e);}}
  if(ok){float until=Time.time+100;var j=chosen;while(s.traffic.MovementSignal(j,chosenAxis,-1,chosenSign)!=SignalColor.Green&&Time.time<until)yield return null;try{Check(s.traffic.MovementSignal(j,chosenAxis,-1,chosenSign)==SignalColor.Green,"unrouted left-turn taxi eventually gets protected green");}catch(Exception e){ok=false;Debug.LogException(e);}}
  if(ok&&Has("-casualCapture")){s.GetComponent<DrivingReadability>().SelectSignal();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/casual-"+(pvp?"pvp":"single")+".png"));yield return new WaitForSecondsRealtime(.4f);}
  Time.timeScale=1;Debug.Log("CASUAL_PLAYER_"+(ok?"OK":"FAILED")+" checks="+checks);Application.Quit(ok?0:1);
 }
 TrafficJunction chosen;int chosenAxis,chosenSign;
 void Prepare(){
  Check(s&&s.StartupReady,"native world initialized");Check(pvp==(s.simulationMatch!=null),"requested native mode");if(!pvp)Check(s.business.SavePath.Replace('\\','/').Contains("/artifacts/"),"single diagnostic save isolated");
  var t=s.traffic;Check(t.AdaptiveSignalCount>0,"basic game signals use live demand");
  foreach(var car in t.cars)car.enabled=false;if(pvp)foreach(var bot in s.simulationMatch.Bots){bot.GetComponent<PvpSimulationBot>().enabled=false;bot.taxi.body.isKinematic=true;bot.taxi.enabled=false;}
  s.business.enabled=false;s.requests.enabled=false;s.manners.enabled=false;s.law.enabled=false;s.jobs.enabled=false;s.assisted.SetActive(false);s.jobs.nav.Clear();s.taxi.enabled=false;s.taxi.body.isKinematic=true;
  chosen=t.junctions.First(j=>j.gameProgram!=null&&j.editorProgram==null&&!j.scenarioSignal&&j.gameProgram.groups.Any(g=>g.turn==-1));var g=chosen.gameProgram.groups.First(x=>x.turn==-1);chosenAxis=g.axis;chosenSign=g.sign;
  var direction=t.AxisDirection(chosen,chosenAxis)*chosenSign;var point=chosen.center-direction*(chosen.StopDistance(chosenAxis,chosenSign)+20)+TrafficSystem.Right(direction)*2;
  s.taxi.body.position=point;s.taxi.body.rotation=Quaternion.LookRotation(direction);s.taxi.transform.SetPositionAndRotation(point,s.taxi.body.rotation);s.taxi.GetComponent<TaxiSignals>().ApplyNetworkView(-1,false);Physics.SyncTransforms();Time.timeScale=5;
 }
 void DemandAndWire(){
  var t=s.traffic;Check(!s.jobs.nav.HasDestination,"no navigation required for demand");var states=t.CaptureSignalRuntime();var state=states.First(x=>x.junction==chosen.name);int wanted=Array.FindIndex(chosen.gameProgram.stages,stage=>stage.groups.Any(id=>chosen.gameProgram.groups.Any(g=>g.id==id&&g.axis==chosenAxis&&g.sign==chosenSign&&g.turn==-1)));
  Check(state.demand[wanted]>0,"physical taxi and indicator request left phase");
  float wait=t.EstimatedGreenWait(chosen,chosenAxis,chosenSign,-1);Check(wait>=0&&wait<100,"own-direction estimated wait is finite");
  Check(t.EstimatedGreenWait(chosen,chosenAxis,chosenSign,1)<0,"right turn never advertised as automatic permission");
  var root=new GameObject("authoritative signal wire fixture");try{var copy=root.AddComponent<TrafficSystem>();copy.manualClock=true;foreach(var j in t.junctions)copy.junctions.Add(new TrafficJunction{name=j.name,gameProgram=j.gameProgram,editorProgram=j.editorProgram});
   var wire=JsonUtility.FromJson<PvpNativeFrame>(JsonUtility.ToJson(new PvpNativeFrame{signals=states}));copy.RestoreSignalRuntime(wire.signals);Check(copy.AdaptiveSignalCount==states.Length,"all host signal runtime states survive JSON");
   foreach(var j in t.junctions.Where(x=>x.gameProgram!=null&&!x.scenarioSignal&&x.editorProgram==null)){var replica=copy.junctions.First(x=>x.name==j.name);foreach(int axis in new[]{0,1})foreach(int sign in new[]{-1,1})foreach(int turn in new[]{-1,0,1})Check(t.MovementSignal(j,axis,turn,sign)==copy.MovementSignal(replica,axis,turn,sign),"host/replica movement lamps agree");}
  }finally{Destroy(root);}
 }
}
