using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Accident fault, player acceptance (Eungye single, 2026-11-24). Real physics contacts:
// 1) the taxi stands still and an ordinary car is pushed into it from behind -> the other car's fault:
//    no durability loss, no accident handling, no ledger change, one notice;
// 2) the taxi is driven into a standing ordinary car -> the driver's own fault, the normal accident opens.
// The ordinary car is moved by the probe (traffic never drives into a standing taxi by itself).
public sealed class AccidentFaultProbe : MonoBehaviour {
 const string Flag="-accidentFaultCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Accident fault acceptance").AddComponent<AccidentFaultProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("ACCIDENTFAULT_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 Vector3 LaneHeading(float x,float z){
  var p=new Vector3(x,0,z);bool t;int first=-1,last=-1;
  for(int i=0;i<360;i++){bool ok=s.traffic.RoadSpeedLimit(p,Quaternion.Euler(0,i,0)*Vector3.forward,out t)>0;if(ok&&first<0)first=i;if(ok&&first>=0&&(last<0||i==last+1))last=i;}
  return Quaternion.Euler(0,first<0?0:(first+last)*.5f,0)*Vector3.forward;
 }
 void Pose(Vector3 p,Vector3 heading){var t=s.taxi;t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(heading);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();}
 // Accident fee, repair/tow spending, cash, law records, driving score and durability. Fuel and the shift clock move on their own while the taxi rolls, so the whole account is not compared.
 string Money(){var b=s.business;var l=b.ledger;return l.Account.Expense(l.Day,"accident")+"|"+l.Account.Expense(l.Day,"repair")+"|"+l.Account.Expense(l.Day,"tow")+"|"+l.Account.Cash+"|"+s.law.ledger.Count+"|"+s.law.ledger.TotalScore+"|"+l.Care.Damage.Durability;}
 IEnumerator Start(){
  float limitTime=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limitTime){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-accidentFaultOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/accident-fault");Directory.CreateDirectory(output);
  yield return null;string error=null;var b=s.business;TrafficCar car=null;Rigidbody body=null;Vector3 heading=Vector3.forward;var spot=new Vector3(450.2f,0,450.3f);string before=null;
  try{
   b.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();
   Check(s.damage&&s.damage.FaultRules,"fault rules are on in the Eungye single game");
   car=FindObjectsOfType<TrafficCar>().FirstOrDefault(c=>c.isActiveAndEnabled&&c.Visible);Check(car,"an ordinary car is available");
   foreach(var other in FindObjectsOfType<TrafficCar>())if(other!=car)other.gameObject.SetActive(false);
   car.enabled=false;body=car.GetComponent<Rigidbody>();Check(body&&body.isKinematic,"the ordinary car is a kinematic body");
   heading=LaneHeading(spot.x,spot.z);
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // 1) Standing taxi, struck from behind at 4 m/s.
  Pose(spot,heading);var from=spot-heading*9+Vector3.up*.02f;body.position=from;body.rotation=Quaternion.LookRotation(heading);car.transform.SetPositionAndRotation(from,body.rotation);car.speed=4;Physics.SyncTransforms();
  yield return new WaitForSeconds(1.2f);before=Money();int notices=s.damage.OtherFaultCount;
  {float until=Time.time+4;var wait=new WaitForFixedUpdate();float travelled=0;
   while(Time.time<until&&travelled<7.5f&&!(s.accident&&s.accident.Locked)){travelled+=4*Time.fixedDeltaTime;body.MovePosition(from+heading*travelled);yield return wait;}
   car.speed=0;report.AppendLine("INFO pushed "+travelled.ToString("0.0")+" m, taxi moved to speed "+s.taxi.body.velocity.magnitude.ToString("0.0")+" m/s");}
  yield return new WaitForSeconds(1f);
  try{
   Check(s.damage.OtherFaultCount==notices+1,"the contact was judged the other car's fault ("+s.damage.LastFaultReason+")");
   Check(!s.accident.Locked,"no accident handling opened");
   Check(Money()==before,"accident fee, repair spending, cash, law records, driving score and durability are unchanged ("+before+" -> "+Money()+")");
   Check(s.damage.Notice.Contains("상대 차량 과실")&&s.damage.Notice.Contains("처리비와 감점 없음"),"notice: "+s.damage.Notice);
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"other-fault.png"));yield return null;
  // 2) Taxi driven into the standing car at 6 m/s.
  body.position=spot+heading*12+Vector3.up*.02f;car.transform.position=body.position;car.speed=0;Pose(spot,heading);Physics.SyncTransforms();
  yield return new WaitForSeconds(1.2f);
  {float give=Time.realtimeSinceStartup+8;while(!s.accident.Locked&&Time.realtimeSinceStartup<give){var v=heading*6;v.y=s.taxi.body.velocity.y;s.taxi.body.velocity=v;yield return null;}}
  float settle=Time.realtimeSinceStartup+8;while(s.accident.State.phase==AccidentPhase.Stopping&&Time.realtimeSinceStartup<settle)yield return null;
  try{
   Check(s.accident.Locked,"driving into a standing car opens the accident");
   Check(s.accident.State.Kind==AccidentKind.Vehicle&&!s.accident.State.Shared&&s.accident.State.fault==0,"it is a vehicle accident at the driver's own fault");
   Check(s.accident.State.Fee==s.accident.State.selectedFee&&s.accident.State.Fee>=1000,"the full fee applies ("+s.accident.State.Fee+" won)");
   Check(b.ledger.Care.Damage.Durability<100,"durability was lost ("+b.ledger.Care.Damage.Durability+")");
  }catch(Exception e){error=e.ToString();}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("ACCIDENTFAULT_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
