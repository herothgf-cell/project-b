using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// 08 chapter 5 player acceptance (Eungye single, 2026-11-24): the after-work free drive and per-car feel values.
// The day is settled with the developer shortcut, the drive is started from the settlement state, the taxi is
// posed on a road and pushed with the automated throttle, and the account, clock and law ledger are compared
// before and after (the one-second collision grace timer that every taxi reset restarts is left out). This checks the rules only; how the drive feels is a hands-on item.
public sealed class EveningDriveProbe : MonoBehaviour {
 const string Flag="-eveningDriveCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Evening drive acceptance").AddComponent<EveningDriveProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("EVENING_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Pose(MapStop p){s.taxi.body.velocity=Vector3.zero;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=p.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(p.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();}
 string Money(){var b=s.business;return JsonUtility.ToJson(b.ledger.Account.Snapshot())+"|"+b.ledger.Elapsed.ToString("R")+"|"+b.ledger.State+"|"+s.law.ledger.Count+"|"+s.law.ledger.TotalScore+"|"+b.passengers.Count+"|"+System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(b.ledger.Snapshot().care),"\"graceSeconds\":[0-9.eE+-]+","");}
 IEnumerator Start(){
  float until=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>until){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-eveningOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/evening");Directory.CreateDirectory(output);
  yield return null;string error=null;var b=s.business;var taxi=s.taxi;
  // Per-car feel values (standard, starter, owned cars).
  try{
   Check(taxi.VehicleSteerResponse==1&&taxi.VehicleBrakeRamp==0&&Mathf.Approximately(taxi.VehicleLean,1.25f)&&Mathf.Approximately(taxi.VehicleDip,1.2f),"father's taxi: standard steering and brake, looser body ("+taxi.VehicleSteerResponse+"/"+taxi.VehicleLean+"/"+taxi.VehicleDip+"/"+taxi.VehicleBrakeRamp+")");
   VehicleFeel.Apply(taxi,VehicleCatalog.Hybrid);Check(Mathf.Approximately(taxi.VehicleSteerResponse,1.25f)&&Mathf.Approximately(taxi.VehicleLean,.8f)&&taxi.VehicleBrakeRamp==0,"hybrid: quicker wheel, firmer body");
   VehicleFeel.Apply(taxi,VehicleCatalog.WideLpg);Check(Mathf.Approximately(taxi.VehicleSteerResponse,.85f)&&Mathf.Approximately(taxi.VehicleBrakeRamp,.2f)&&Mathf.Approximately(taxi.VehicleLean,1.1f),"LPG: heavier wheel, soft pedal");
   VehicleFeel.Apply(taxi,null);Check(taxi.VehicleSteerResponse==1&&taxi.VehicleLean==1&&taxi.VehicleDip==1&&taxi.VehicleBrakeRamp==0,"standard taxi (PVP) keeps 1/1/1/0");
   VehicleFeel.Apply(taxi,b.ActiveVehicle.id);
   Check(VehicleFeel.Describe(VehicleCatalog.Hybrid).Contains("핸들 반응 빠름")&&VehicleFeel.Describe(VehicleCatalog.WideLpg).Contains("브레이크가 부드럽게"),"garage card describes the feel");
   // Not available while the shift is open.
   b.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();
   Check(!b.CanStartEveningDrive&&!b.StartEveningDrive(),"no free drive while the shift is open");
   Check(b.DevEndDayNow(),"day closed with the developer shortcut: "+b.notice);
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  float wait=Time.realtimeSinceStartup+20;while(!b.ShowingSummary&&Time.realtimeSinceStartup<wait)yield return null;
  string before=null;Vector3 start=Vector3.zero;
  try{
   Check(b.ShowingSummary&&Time.timeScale==0,"settlement is showing");before=Money();
   Check(b.CanStartEveningDrive,"free drive offered on the settlement screen");
   Check(b.StartEveningDrive(),"free drive starts");
   Check(b.EveningDrive&&!b.ShowingSummary&&Time.timeScale==1&&!s.paused&&!b.ledger.Operating,"driving again while the ledger stays settled");
   Check(s.services.AtEndDayHub(),"starts in the home bay");
   Check(!b.NextDay()&&b.EveningDrive,"next day cannot start during the drive");
   Check(!s.jobs.Accept(0)&&s.jobs.message.Contains("퇴근 후 드라이브"),"calls are refused");
   Check(s.jobs.offers.All(o=>o==null),"call board is empty");
   var stop=s.jobs.stops[3];Pose(stop);start=taxi.transform.position;taxi.automated=true;taxi.autoBrake=false;taxi.autoSteer=0;taxi.autoThrottle=1;
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(5);
  Screen.SetResolution(1600,900,FullScreenMode.Windowed);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"evening-drive.png"));yield return new WaitForSecondsRealtime(.5f);
  try{
   float moved=Vector3.Distance(start,taxi.transform.position);report.AppendLine("moved "+moved.ToString("0.0")+" m in 5 s");
   Check(moved>15,"the taxi drives ("+moved.ToString("0.0")+" m)");
   taxi.autoThrottle=0;taxi.autoBrake=true;
   Check(s.jobs.offers.All(o=>o==null)&&!s.jobs.Active,"no call appeared while driving");
   var light=FindObjectOfType<DayLightController>();Check(light&&Math.Abs(light.ClockHour-EveningDriveRules.Hour)<.01,"night lighting hour "+(light?light.ClockHour:-1));
   Check(EveningDriveRules.RadioLine(6).Length>0&&EveningDriveRules.RadioLine(20).Length==0&&EveningDriveRules.RadioLine(EveningDriveRules.RadioSeconds+6)!=EveningDriveRules.RadioLine(6),"radio lines rotate with gaps");
   var radio=FindObjectOfType<RadioTicker>();Check(radio&&radio.VisibleText==EveningDriveRules.RadioLine(b.EveningDriveSeconds),"radio ticker follows the drive ("+b.EveningDriveSeconds.ToString("0.0")+" s)");
   Check(b.EveningDriveSeconds>4,"drive time advances");
   {string now=Money();if(now!=before){int at=0;while(at<now.Length&&at<before.Length&&now[at]==before[at])at++;report.AppendLine("DIFF at "+at+" | before: "+before.Substring(System.Math.Max(0,at-120),System.Math.Min(260,before.Length-System.Math.Max(0,at-120)))+" | after:  "+now.Substring(System.Math.Max(0,at-120),System.Math.Min(260,now.Length-System.Math.Max(0,at-120))));}}Check(Money()==before,"account, shift clock, law ledger, receipts and vehicle care are unchanged after driving");
   Check(!b.EndEveningDrive()&&b.EveningDrive&&b.notice.Contains("숙소 주차 칸"),"cannot finish away from the home bay");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // Finish in the home bay.
  taxi.automated=false;var bay=s.services.ParkingStop;taxi.body.velocity=Vector3.zero;taxi.body.angularVelocity=Vector3.zero;
  taxi.SetSpawn(bay.Kerb+Vector3.up*(HomeGarage.FloorTop+taxi.wheels[0].radius),Quaternion.LookRotation(bay.Direction));taxi.ResetTaxi();
  yield return new WaitForSeconds(1.5f);
  try{
   Check(s.services.AtEndDayHub(),"parked in the home bay");
   Check(b.EndEveningDrive(),"F in the home bay ends the drive");
   Check(!b.EveningDrive&&b.ShowingSummary&&Time.timeScale==0,"settlement is back");
   Check(Money()==before,"nothing changed across the whole drive");
   Check(b.CanStartEveningDrive,"the drive can be chosen again");
   Check(b.SaveCheckpoint(),"checkpoint saved");
   Check(b.NextDay(),"next day starts normally afterwards");
   Check(b.ledger.Day==2&&!b.EveningDrive,"day 2");
  }catch(Exception e){error=e.ToString();}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("EVENING_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
