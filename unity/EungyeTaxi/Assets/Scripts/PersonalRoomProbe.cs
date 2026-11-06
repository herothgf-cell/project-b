using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Opt-in integration fixture. Positions/state transitions are controlled; this is not physical driving.
public sealed class PersonalRoomProbe : MonoBehaviour {
 int checks;float began;
 static bool Reload {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-personalRoomReloadCheck")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-personalRoomCheck")>=0||Reload){var go=new GameObject("Personal room acceptance");DontDestroyOnLoad(go);go.AddComponent<PersonalRoomProbe>();}}
 void Check(bool valid,string label){checks++;Debug.Log("PERSONAL_ROOM_PLAYER "+(valid?"PASS ":"FAIL ")+label);if(!valid)throw new Exception(label);}
 void Update(){if(Time.realtimeSinceStartup-began>150){Debug.LogError("PERSONAL_ROOM_PLAYER timeout");Application.Quit(2);}}
 IEnumerator Start(){began=Time.realtimeSinceStartup;var run=Run();while(true){object value;try{if(!run.MoveNext())yield break;value=run.Current;}catch(Exception ex){Debug.LogError("PERSONAL_ROOM_PLAYER_EXCEPTION "+ex);Application.Quit(2);yield break;}yield return value;}}
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/personal-room-player/"+name+".png"));yield return new WaitForSecondsRealtime(.4f);}
 void CloseDay(DriveSession s){var b=s.business;b.preparing=false;s.paused=false;Time.timeScale=1;var p=s.services.ParkingStop;s.taxi.body.position=p.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(p.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();s.jobs.ClearForDay();Check(b.RequestEndDay(),"normal parking close accepted");if(b.ledger.Operating)Check(b.ledger.Settle(),"settlement completed");Check(b.ShowingSummary&&b.SaveCheckpoint(),"summary checkpoint saved");Time.timeScale=0;}
 IEnumerator Run(){
  Check(ContentTuningRuntime.TestMode,"requires isolated content test mode");Directory.CreateDirectory("artifacts/personal-room-player");
  yield return null;var shell=FindObjectOfType<UnifiedModeBootstrap>();if(shell)shell.StartSingle();DriveSession s=null;
  while(!(s=FindObjectOfType<DriveSession>())||!s.StartupReady||s.business.IsRestoring)yield return null;
  yield return null;var b=s.business;Check(!b.ResumeBlocked,"valid isolated checkpoint");Check(b.SavePath.Replace('\\','/').Contains("personal-room-player"),"save path isolated from player campaign");
  s.taxi.enabled=false;s.taxi.body.isKinematic=true;s.taxi.body.detectCollisions=false;s.jobs.enabled=false;s.requests.enabled=false;b.enabled=false;
  if(Reload){Check(b.ledger.Day==3&&b.ShowingSummary,"fresh process restores final summary");Check(b.ledger.Account.Room.lampOwned&&b.ledger.Account.Room.rugOwned,"fresh process restores both purchases");Check(b.ledger.Account.Room.finalized,"fresh process restores final archive lock");b.RoomOpen=true;Check(!b.ChangeRoom("light","lamp"),"final room read-only");yield return Capture("04-reloaded-final-room");Debug.Log("PERSONAL_ROOM_RELOAD_OK checks="+checks);Application.Quit(0);yield break;}
  var legacyJson=System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(new CampaignCheckpoint{account=b.ledger.Account.Snapshot()}),@"""Room""\s*:\s*\{[^}]*\}","\"legacyRemoved\":null");Check(BusinessProgress.ParseCheckpoint(legacyJson).account.Room==null,"legacy absent Room normalized at JSON boundary");
  Check(b.ledger.Day==1&&!b.ledger.Account.Room.lampOwned,"fresh three-day fixture starts empty");b.Depart();Check(b.ledger.RecordFare("room-fixture-fare",15000),"fixture earning posted");CloseDay(s);
  int cash=b.ledger.Account.Cash,net=b.ledger.TotalNet,dayNet=b.ledger.Last.net;b.RoomOpen=true;yield return Capture("01-before-purchase");
  Check(b.ChangeRoom("buy","lamp",1),"lamp purchased through saved room mutation");Check(b.ledger.Account.Cash==cash-5000&&b.ledger.Account.Room.lampOwned&&b.ledger.Account.Room.lampPlaced,"cash and owned placement committed");
  Check(b.ledger.TotalNet==net&&b.ledger.Last.net==dayNet,"personal spending preserves daily and cumulative operating net");Check(!b.ChangeRoom("buy","lamp",0),"duplicate purchase rejected");
  Check(b.ChangeRoom("light","lamp"),"owned lamp toggles");bool lampOn=b.ledger.Account.Room.lampOn;Check(b.ChangeRoom("wish","rug"),"wishlist stored");yield return Capture("02-after-lamp-purchase");
  var parsed=BusinessProgress.ParseCheckpoint(File.ReadAllText(b.SavePath));var restored=EconomyLedger.RestoreCampaign(parsed.days,parsed.account);Check(restored.Account.Room.lampOwned&&restored.Account.Room.lampStyle==1&&restored.Account.Room.lampOn==lampOn,"serialized ownership style and light restored");Check(restored.TotalNet==net&&restored.Last.net==dayNet,"serialized operating results invariant");
  string original=File.ReadAllText(b.SavePath);int before=b.ledger.Account.Cash;
  using(var held=new FileStream(b.SavePath,FileMode.Open,FileAccess.Read,FileShare.Read)){Check(!b.ChangeRoom("buy","rug",0),"save failure rejects transaction");Check(!b.ledger.Account.Room.rugOwned&&b.ledger.Account.Cash==before,"save failure rolls back cash and ownership");}
  Check(File.ReadAllText(b.SavePath)==original,"failed write preserves previous checkpoint");Check(b.SaveCheckpoint(),"save retry recovers");b.RoomOpen=false;
  Check(b.NextDay(),"next day transition");yield return null;yield return null;Check(b.ledger.Day==2&&b.ledger.Account.Room.lampOwned&&b.ledger.Account.Room.wished=="rug","ownership and wish persist next day");
  CloseDay(s);b.RoomOpen=true;Check(b.ChangeRoom("buy","rug",0),"second day rug purchase");yield return Capture("03-second-day-room");b.RoomOpen=false;Check(b.NextDay(),"third day transition");yield return null;yield return null;CloseDay(s);b.RoomOpen=true;
  Check(b.ChangeRoom("finish"),"final room committed");Check(b.ledger.Account.Room.finalized&&!b.ChangeRoom("light","lamp"),"finalized room immutable");Check(b.SaveCheckpoint(),"final checkpoint saved");yield return Capture("04-final-room");
  var low=new CampaignAccount(PrototypeSettings.ForThreeDays());Check(low.BuyRoomItem("rug",0,1)&&!low.BuyRoomItem("lamp",0,1),"insufficient cash rejected");var debt=new CampaignAccount(PrototypeSettings.ForThreeDays());debt.Repair("room-debt",1,100);Check(debt.Debt>0&&!debt.BuyRoomItem("lamp",0,1),"unpaid operating debt blocks discretionary purchase");
  Debug.Log("PERSONAL_ROOM_PLAYER_OK checks="+checks);Application.Quit(0);
 }
}
