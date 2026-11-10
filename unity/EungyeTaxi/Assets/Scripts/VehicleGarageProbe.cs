using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Opt-in plan 22 integration fixture. Day closes and positions are controlled (not physical driving);
// the steering check reads the real WheelCollider angle. -vehicleGarageReloadCheck runs in a fresh process.
public sealed class VehicleGarageProbe : MonoBehaviour {
 int checks;float began;
 public static bool Enabled {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-vehicleGarageCheck")>=0||Reload;}}
 static bool Reload {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-vehicleGarageReloadCheck")>=0;}}
 const string Dir="artifacts/vehicle-garage-player/";
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){if(Enabled){var go=new GameObject("Vehicle garage acceptance");DontDestroyOnLoad(go);go.AddComponent<VehicleGarageProbe>();}}
 void Check(bool valid,string label){checks++;Debug.Log("VEHICLE_GARAGE_PLAYER "+(valid?"PASS ":"FAIL ")+label);if(!valid)throw new Exception(label);}
 void Update(){if(Time.realtimeSinceStartup-began>180){Debug.LogError("VEHICLE_GARAGE_PLAYER timeout");Application.Quit(2);}}
 IEnumerator Start(){began=Time.realtimeSinceStartup;var run=Run();while(true){object value;try{if(!run.MoveNext())yield break;value=run.Current;}catch(Exception ex){Debug.LogError("VEHICLE_GARAGE_PLAYER_EXCEPTION "+ex);Application.Quit(2);yield break;}yield return value;}}
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath(Dir+name+".png"));yield return new WaitForSecondsRealtime(.4f);}
 void CloseDay(DriveSession s){var b=s.business;b.preparing=false;s.paused=false;Time.timeScale=1;var p=s.services.ParkingStop;s.taxi.body.position=p.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(p.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();s.jobs.ClearForDay();Check(b.RequestEndDay(),"normal parking close accepted");if(b.ledger.Operating)Check(b.ledger.Settle(),"settlement completed");Check(b.ShowingSummary&&b.SaveCheckpoint(),"summary checkpoint saved");Time.timeScale=0;}
 static VehicleUnlocks Unlocks(string save){var path=Path.Combine(Path.GetDirectoryName(save),VehicleUnlockStore.FileName);if(!File.Exists(path))return null;var u=JsonUtility.FromJson<VehicleUnlocks>(File.ReadAllText(path));u.Validate();return u;}
 // Real WheelCollider front-left angle for full right input at standstill.
 IEnumerator Steer(DriveSession s,float factor,float[] result){var t=s.taxi;t.VehicleSteerFactor=factor;t.automated=true;t.AutomatedHumanInput=false;t.autoThrottle=0;t.autoBrake=true;t.autoSteer=1;t.enabled=true;Time.timeScale=1;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();result[0]=Mathf.Abs(t.wheels[0].steerAngle)+Mathf.Abs(t.wheels[1].steerAngle);t.autoSteer=0;t.enabled=false;Time.timeScale=0;}
 IEnumerator Run(){
  Check(ContentTuningRuntime.TestMode,"requires isolated content test mode");Directory.CreateDirectory(Dir);
  yield return null;var shell=FindObjectOfType<UnifiedModeBootstrap>();if(shell)shell.StartSingle();DriveSession s=null;
  while(!(s=FindObjectOfType<DriveSession>())||!s.StartupReady||s.business.IsRestoring)yield return null;
  yield return null;var b=s.business;Check(!b.ResumeBlocked,"valid isolated checkpoint");Check(b.SavePath.Replace('\\','/').Contains("vehicle-garage-player"),"save path isolated from player campaign");
  s.taxi.enabled=false;s.taxi.body.isKinematic=true;s.taxi.body.detectCollisions=false;s.jobs.enabled=false;s.requests.enabled=false;
  if(Reload){
   b.enabled=false;b.SyncVehicle();var v=b.ledger.Account.Vehicle;Check(b.ledger.Day==3&&b.ShowingSummary,"fresh process restores final summary day="+b.ledger.Day+" state="+b.ledger.State+" vehicle="+b.ledger.Account.Vehicle.modelId+" finalized="+b.ledger.Account.Room.finalized);
   Check(v.modelId=="lpg"&&v.altColorOwned&&v.color==1&&v.ornamentOwned&&v.ornamentPlaced,"fresh process restores vehicle, colour and ornament");
   Check(b.ledger.Account.Room.finalized&&!b.ChangeVehicle("color","",0)&&!b.ChangeVehicle("buy","hybrid"),"finished campaign garage read-only");
   var look=s.taxi.GetComponent<VehicleAppearance>();Check(look&&look.Model=="lpg"&&look.Color==1&&look.Ornament&&Mathf.Abs(s.taxi.VehicleSteerFactor-.86f)<.001f,"restored car look and turning applied");
   var u=Unlocks(b.SavePath);Check(u!=null&&u.HasModel("lpg")&&u.HasColor("lpg",1)&&u.ornament,"PVP unlocks persisted beside save");
   b.RoomOpen=true;b.GarageTab=true;yield return Capture("05-reloaded-final-garage");b.RoomOpen=false;b.GarageTab=false;
   // New 3-day campaign: vehicle resets to the starter, PVP unlocks remain.
   Check(b.RequestRestart()&&b.ConfirmRestart(true),"new campaign started");yield return null;b.SyncVehicle();
   Check(b.ledger.Day==1&&b.ledger.Account.Vehicle.modelId=="starter"&&!b.ledger.Account.Vehicle.ornamentOwned&&Mathf.Abs(s.taxi.VehicleSteerFactor-1)<.0001f&&s.taxi.GetComponent<VehicleAppearance>().Model=="starter","new campaign returns to the starter taxi");
   u=Unlocks(b.SavePath);Check(u.HasModel("lpg")&&u.ornament,"new campaign keeps PVP unlocks");
   Debug.Log("VEHICLE_GARAGE_RELOAD_OK checks="+checks);Application.Quit(0);yield break;
  }
  // Old save without a vehicle member migrates to the starter taxi at the JSON boundary.
  var legacyJson=System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(new CampaignCheckpoint{days=b.ledger.Snapshot(),account=b.ledger.Account.Snapshot()}),@",\s*""Vehicle""\s*:\s*\{[^}]*\}","");
  Check(!legacyJson.Contains("\"Vehicle\""),"legacy fixture has no vehicle member");var legacy=BusinessProgress.ParseCheckpoint(legacyJson);Check(EconomyLedger.RestoreCampaign(legacy.days,legacy.account).Account.Vehicle.modelId=="starter","legacy checkpoint migrates to starter");
  Check(b.ledger.Day==1&&b.ledger.Account.Vehicle.modelId=="starter","fresh three-day fixture starts with the starter taxi");
  float[] angle=new float[1];yield return Steer(s,1,angle);float standard=angle[0];b.SyncVehicle();Check(Mathf.Abs(s.taxi.VehicleSteerFactor-1)<.0001f,"starter keeps standard steering");
  // Day 1: not enough cash for the hybrid.
  b.Depart();Check(b.ledger.RecordFare("garage-fixture-1",4000),"day 1 fare");CloseDay(s);
  b.RoomOpen=true;b.GarageTab=true;yield return Capture("01-garage-day1");
  int cash=b.ledger.Account.Cash;var hq=b.ledger.QuoteVehicle("hybrid");Check(!hq.allowed&&hq.spend==16000&&cash<16000,"day 1 hybrid quote is short ("+cash+")");Check(!b.ChangeVehicle("buy","hybrid")&&b.ledger.Account.Cash==cash&&b.ledger.Account.Vehicle.modelId=="starter","insufficient cash rejected without mutation");
  b.RoomOpen=false;b.GarageTab=false;Check(b.NextDay(),"day 2 transition");yield return null;yield return null;
  // Day 2: buy the wide LPG taxi after settlement, plus colour and ornament.
  Check(b.ledger.RecordFare("garage-fixture-2",15000),"day 2 fare");Check(!b.ChangeVehicle("buy","lpg"),"no purchase while operating");CloseDay(s);
  b.RoomOpen=true;b.GarageTab=true;cash=b.ledger.Account.Cash;int net=b.ledger.TotalNet,dayNet=b.ledger.Last.net;var q=b.ledger.QuoteVehicle("lpg");
  Check(q.allowed&&q.price==22000&&q.tradeIn==10000&&q.spend==12000,"day 2 lpg quote");yield return Capture("02-garage-quote-day2");
  Check(b.ChangeVehicle("buy","lpg"),"lpg purchased through saved mutation");Check(!b.ChangeVehicle("buy","lpg")&&!b.ChangeVehicle("buy","hybrid"),"duplicate click / second replacement rejected");
  Check(b.ledger.Account.Cash==cash-12000&&b.ledger.TotalNet==net&&b.ledger.Last.net==dayNet&&b.ledger.Account.PersonalSpent==12000,"cash debited once, operating net unchanged");
  Check(b.ChangeVehicle("color-buy")&&b.ChangeVehicle("ornament-buy")&&b.ledger.Account.Cash==cash-12000-1500-2000,"colour and ornament purchased");
  var parsed=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));var restored=EconomyLedger.RestoreCampaign(parsed.days,parsed.account);
  Check(restored.Account.Vehicle.modelId=="lpg"&&restored.Account.Vehicle.altColorOwned&&restored.Account.Vehicle.ornamentOwned&&restored.Account.Cash==b.ledger.Account.Cash&&restored.Care.Damage.Durability==100,"serialized vehicle trade restored");
  var u2=Unlocks(b.SavePath);Check(u2!=null&&u2.HasModel("lpg")&&u2.HasColor("lpg",1)&&u2.ornament,"PVP unlock file written");
  string original=File.ReadAllText(b.SavePath);int before=b.ledger.Account.Cash;
  using(var held=new FileStream(b.SavePath,FileMode.Open,FileAccess.Read,FileShare.Read)){Check(!b.ChangeVehicle("color","",0),"save failure rejects garage change");Check(b.ledger.Account.Vehicle.color==1&&b.ledger.Account.Cash==before,"save failure rolls back");}
  Check(File.ReadAllText(b.SavePath)==original,"failed write preserves checkpoint");Check(b.SaveCheckpoint(),"save retry recovers");
  yield return Capture("03-garage-after-purchase");b.RoomOpen=false;b.GarageTab=false;
  // Day 3: the new car drives with its traits.
  Check(b.NextDay(),"day 3 transition");yield return null;yield return null;b.SyncVehicle();
  var app=s.taxi.GetComponent<VehicleAppearance>();Check(app&&app.Model=="lpg"&&app.Color==1&&app.Ornament,"day 3 appearance is the bought car");
  Check(b.ActiveVehicle.id=="lpg"&&Mathf.Abs(s.taxi.VehicleSteerFactor-.86f)<.001f&&b.ActiveVehicle.carriesLuggage&&b.ActiveVehicle.comfortFactor>1,"day 3 traits active");
  yield return Steer(s,.86f,angle);Check(angle[0]<standard*.9f&&angle[0]>standard*.8f,"real wheel angle reflects larger turning radius "+angle[0]+"/"+standard);
  float fuel=b.ledger.Account.Fuel;b.ledger.Account.Consume(0,1000);float used=fuel-b.ledger.Account.Fuel;float perKm=b.ledger.Account.Snapshot().settings.fuelPerKm;Check(Mathf.Abs(used-perKm*.95f)<.001f,"day 3 fuel factor "+used+"/"+perKm);
  b.Depart();Time.timeScale=1;yield return new WaitForSecondsRealtime(.6f);yield return Capture("04-day3-driving-new-car");
  CloseDay(s);b.RoomOpen=true;b.GarageTab=true;Check(!b.ChangeVehicle("buy","hybrid"),"one replacement per campaign");
  Check(b.ChangeRoom("finish"),"campaign finished");Check(b.SaveCheckpoint(),"final checkpoint saved");yield return Capture("05-final-garage");
  Debug.Log("VEHICLE_GARAGE_PLAYER_OK checks="+checks);Application.Quit(0);
 }
}
