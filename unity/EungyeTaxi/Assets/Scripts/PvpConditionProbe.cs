using System;
using System.Collections;
using System.IO;
using UnityEngine;
public sealed class PvpConditionProbe:MonoBehaviour {
 bool ok=true,done;PvpSimulationMatch match;DriveSession human;float deadline;string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpConditionCheck")<0)return;var o=new GameObject("PVP condition acceptance");DontDestroyOnLoad(o);o.AddComponent<PvpConditionProbe>();}
 void Check(bool value,string why){ok&=value;Debug.Log("PVP_CONDITION "+(value?"PASS ":"FAIL ")+why);}
 void Update(){if(!done&&Time.realtimeSinceStartup>deadline&&deadline>0){Debug.LogError("PVP_CONDITION timeout");done=true;Application.Quit(2);}}
 IEnumerator Start(){deadline=Time.realtimeSinceStartup+300;var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-pvpConditionOutput");output=index>=0&&index+1<args.Length?args[index+1]:"artifacts";Directory.CreateDirectory(output);var steps=Run();while(!done){object current;try{if(!steps.MoveNext())yield break;current=steps.Current;}catch(Exception e){Debug.LogError("PVP_CONDITION_EXCEPTION "+e);done=true;Application.Quit(2);yield break;}yield return current;}}
 IEnumerator Capture(string name){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-pvpConditionCaptureOutput");if(i<0||i+1>=args.Length||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;Directory.CreateDirectory(args[i+1]);yield return new WaitForEndOfFrame();var pixels=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(args[i+1],name+".png"),pixels.EncodeToPNG());}finally{Destroy(pixels);}}
 IEnumerator Wait(float seconds){float until=match.Elapsed+seconds;while(match.Elapsed<until&&!match.Finished)yield return null;}
 IEnumerator Site(int kind){
  human.services.open=false;human.paused=false;human.jobs.ClearForDay();human.jobs.nav.Clear();human.assisted.SetActive(false);human.taxi.automated=true;human.taxi.autoThrottle=0;human.taxi.autoBrake=true;
  ProjectMapObject selected=null;foreach(var o in ProjectMapScene.Services(human.world.map,kind==1?"restaurant":"parking")){if(!Physics.CheckBox(o.Position+Vector3.up*.8f,new Vector3(1.1f,.5f,2.3f),Quaternion.LookRotation(o.Direction),(1<<8)|(1<<9),QueryTriggerInteraction.Ignore)){selected=o;break;}}
  if(selected==null)throw new Exception("no clear service fixture site");human.taxi.SetSpawn(selected.Position+Vector3.up*.65f,Quaternion.LookRotation(selected.Direction));human.taxi.ResetTaxi();yield return Wait(2);human.services.open=true;
  Check(human.services.CanUse(kind),"safe authored site kind="+kind+" initial placement fixture; no driving assertion");
 }
 static float Traffic(DriveSession s){float x=0;foreach(var car in s.traffic.cars)x+=car.totalTravel;return x;}
 IEnumerator Run(){
  Pvp.PvpBootstrap bootstrap;while(!(bootstrap=FindObjectOfType<Pvp.PvpBootstrap>()))yield return null;bootstrap.StartSimulation(true,7200);
  while(!(match=FindObjectOfType<PvpSimulationMatch>())||!match.Human||!match.Human.StartupReady)yield return null;human=match.Human;Time.timeScale=3;
  foreach(var driver in match.Drivers)Check(driver.business.SimulationSession&&driver.business.ledger.PvpProfile&&driver.business.SavePath=="","isolated PVP condition profile driver="+driver.SimulationDriver);
  yield return Site(1);var ledger=human.business.ledger;ledger.Driver.satiety=20;ledger.Driver.fatigue=60;ledger.Driver.drowsiness.Recover();
  int cash=ledger.Account.Cash;float start=match.Elapsed,npcs=Traffic(human);double elapsed=ledger.Elapsed,operation=match.Bot.business.ledger.Elapsed;
  Check(human.services.RequestAction(1),"native meal confirmation opens");yield return Capture("meal-quote");Check(human.services.ConfirmAction(true)&&match.ConditionServiceActive(human),"meal starts through native service API");Check(ledger.Account.Cash==cash&&ledger.Driver.satiety<=20.01,"no instant meal recovery/payment");
  yield return Wait(3);Check(ledger.Account.Cash==cash&&ledger.Driver.satiety<20&&ledger.Driver.fatigue>60,"partial meal only elapsed condition");yield return Capture("meal-pending");human.services.Close();Check(!match.ConditionServiceActive(human)&&ledger.Account.Cash==cash,"cancel no fee/recovery");
  yield return Site(1);ledger.Driver.satiety=20;ledger.Driver.fatigue=60;cash=ledger.Account.Cash;start=match.Elapsed;elapsed=ledger.Elapsed;npcs=Traffic(human);operation=match.Bot.business.ledger.Elapsed;
  Check(human.services.Buy(1),"restart meal");while(match.ConditionServiceActive(human))yield return null;
  Check(match.Elapsed-start>=14.95f&&ledger.Elapsed-elapsed>=14.95&&ledger.Elapsed-elapsed<15.3,"meal uses real shared15 seconds without doubletime");
  Check(ledger.Account.Cash==cash-1000&&ledger.Driver.satiety>78.5&&ledger.PvpMeals==1,"one meal expense and completed satiety +60");Check(Traffic(human)>npcs&&match.Bot.business.ledger.Elapsed>operation,"NPC and other driver continue during meal");cash=ledger.Account.Cash;yield return Wait(1);Check(ledger.Account.Cash==cash,"no duplicate completion payment");
  yield return Site(3);ledger.Driver.satiety=20;ledger.Driver.fatigue=80;ledger.Driver.drowsiness.visionSeconds=20;start=match.Elapsed;cash=ledger.Account.Cash;operation=match.Bot.business.ledger.Elapsed;
  Check(human.services.Buy(3),"rest starts");yield return Wait(10);Check(Math.Abs(ledger.Driver.fatigue-80)<.01&&ledger.Driver.drowsiness.visionSeconds==20,"partialrest holds fatigue/exposure without early recovery");yield return Capture("rest-pending");while(match.ConditionServiceActive(human))yield return null;
  Check(match.Elapsed-start>=44.95f&&ledger.Driver.fatigue<40.1&&ledger.Driver.fatigue>=40&&ledger.Driver.satiety<16.4&&ledger.Driver.drowsiness.visionSeconds==0&&ledger.PvpRests==1,"real45 second rest -40/reset with satiety consumption");Check(ledger.Account.Cash==cash&&match.Bot.business.ledger.Elapsed>operation,"free rest only payer waits");
  yield return Site(1);cash=ledger.Account.Cash;double satiety=ledger.Driver.satiety;Check(human.services.Buy(1),"unfinished end fixture begins");match.Finish();Check(!match.ConditionServiceActive(human)&&ledger.Account.Cash==cash&&ledger.Driver.satiety==satiety,"match end grants no unfinished fee/recovery");
  done=true;File.WriteAllText(Path.Combine(output,"pvp-condition-result.txt"),"success="+ok+" source=runtime pending-service acceptance; initial site placement fixtures\n");Debug.Log(ok?"PVP_CONDITION_PLAYER_OK":"PVP_CONDITION_PLAYER_FAIL");Time.timeScale=1;Application.Quit(ok?0:1);
 }
}
