using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Opt-in plan 22 PVP fixture: "내 차" lobby capture, then an offline bot match. Appearance only.
public sealed class PvpVehicleLookProbe : MonoBehaviour {
 int checks;float began;const string Dir="artifacts/vehicle-garage-player/";
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpVehicleLookCheck")>=0){var go=new GameObject("PVP vehicle look acceptance");DontDestroyOnLoad(go);go.AddComponent<PvpVehicleLookProbe>();}}
 void Check(bool valid,string label){checks++;Debug.Log("PVP_VEHICLE_LOOK "+(valid?"PASS ":"FAIL ")+label);if(!valid)throw new Exception(label);}
 void Update(){if(Time.realtimeSinceStartup-began>150){Debug.LogError("PVP_VEHICLE_LOOK timeout");Application.Quit(2);}}
 IEnumerator Start(){began=Time.realtimeSinceStartup;var run=Run();while(true){object value;try{if(!run.MoveNext())yield break;value=run.Current;}catch(Exception ex){Debug.LogError("PVP_VEHICLE_LOOK_EXCEPTION "+ex);Application.Quit(2);yield break;}yield return value;}}
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath(Dir+name+".png"));yield return new WaitForSecondsRealtime(.4f);}
 IEnumerator Run(){
  Directory.CreateDirectory(Dir);yield return null;
  var unlocks=VehicleUnlockStore.Load();Check(unlocks.HasModel("lpg")&&unlocks.HasColor("lpg",1)&&unlocks.ornament,"unlock file from single-player purchase readable");
  var selected=VehicleUnlockStore.Selected;Check(selected.model=="lpg"&&selected.color==1&&selected.ornament,"pinned selection owned");
  Check(PvpVehicleLook.Owned(new PvpVehicleLook{model="hybrid"},unlocks).model=="starter","unowned model falls back to default taxi");
  var shell=FindObjectOfType<UnifiedModeBootstrap>();if(shell)shell.StartPvp();Pvp.PvpBootstrap boot=null;
  while(!(boot=FindObjectOfType<Pvp.PvpBootstrap>()))yield return null;yield return new WaitForSecondsRealtime(.6f);
  yield return Capture("06-pvp-my-car-lobby");
  PvpConditionRules.Configure(new PvpConditionSettings());boot.StartSimulation(true,120);
  PvpSimulationMatch match=null;while(!(match=FindObjectOfType<PvpSimulationMatch>())||match.Human==null||match.Drivers.Count<4)yield return null;yield return new WaitForSecondsRealtime(1.5f);
  var mine=match.Human.taxi.GetComponent<VehicleAppearance>();Check(mine&&mine.Model=="lpg"&&mine.Color==1&&mine.Ornament,"my PVP taxi uses the selected look");
  for(int i=0;i<match.Drivers.Count;i++){var d=match.Drivers[i];var a=d.taxi.GetComponent<VehicleAppearance>();Check(a&&VehicleCatalog.Get(a.Model)!=null,"driver "+i+" has a catalog look");Check(d.taxi.VehicleSteerFactor==1&&d.business.ActiveVehicle.id=="starter"&&d.business.ledger.Account.Vehicle.modelId=="starter","driver "+i+" keeps PVP standard performance and economy");}
  yield return Capture("07-pvp-match-my-car");
  Debug.Log("PVP_VEHICLE_LOOK_OK checks="+checks);Application.Quit(0);
 }
}
