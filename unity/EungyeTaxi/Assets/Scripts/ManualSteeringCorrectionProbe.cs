using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// Narrow real-Rigidbody check for the manual-demand corner-assist regression.
// Automated inputs enter TaxiController.FixedUpdate; no motion is imposed after setup.
public class ManualSteeringCorrectionProbe : MonoBehaviour {
 readonly Vector3 center=new Vector3(-5000,0,-5000);
 readonly StringBuilder report=new StringBuilder();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-manualSteeringCorrectionsTest")>=0)new GameObject("Manual steering correction check").AddComponent<ManualSteeringCorrectionProbe>();}
 static string F(float value){return value.ToString("F4",CultureInfo.InvariantCulture);}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(1);
  var session=FindObjectOfType<DriveSession>();
  if(!session||!session.taxi){Debug.LogError("MANUAL_CORRECTIONS missing taxi");Application.Quit(1);yield break;}
  var t=session.taxi;t.automated=true;
  session.assisted.SetActive(false);session.assisted.allowAutomated=false;
  session.business.enabledCampaign=false;session.business.enabled=false;session.paused=false;Time.timeScale=1;
  foreach(var car in session.traffic.cars){car.Recycle();car.enabled=false;}
  var pad=GameObject.CreatePrimitive(PrimitiveType.Cube);pad.name="Manual correction isolated pad";pad.transform.position=center+Vector3.down*.5f;pad.transform.localScale=new Vector3(800,1,800);
  bool ok=true;
  foreach(float kph in new[]{30f,50f,70f})foreach(int side in new[]{-1,1}){
   t.autoThrottle=t.autoSteer=t.autoServiceBrake=0;t.autoBrake=false;
   t.SetSpawn(center+Vector3.up*.65f,Quaternion.identity);t.ResetTaxi();
   yield return new WaitForSeconds(.6f);
   session.manners.Suppress(10);
   // Initial velocity is a fixture; subsequent samples are driven only by WheelColliders.
   t.body.velocity=Vector3.forward*(kph/3.6f);
   var start=t.transform.position;float minSpeed=t.Speed,maxTilt=0,maxSlip=0,minMotor=float.PositiveInfinity,maxBrake=0;
   bool corrected=false;t.autoThrottle=1;t.autoSteer=side*.25f;
   for(float elapsed=0;elapsed<1.2f;elapsed+=Time.fixedDeltaTime){
    yield return new WaitForFixedUpdate();
    minSpeed=Mathf.Min(minSpeed,t.Speed);maxTilt=Mathf.Max(maxTilt,Vector3.Angle(t.transform.up,Vector3.up));
    foreach(var wheel in t.wheels){WheelHit hit;if(wheel.GetGroundHit(out hit))maxSlip=Mathf.Max(maxSlip,Mathf.Abs(hit.sidewaysSlip));}
    if(elapsed>.1f){corrected|=t.CornerAssisting;foreach(var wheel in t.wheels)maxBrake=Mathf.Max(maxBrake,wheel.brakeTorque);}
    if(elapsed>.65f)minMotor=Mathf.Min(minMotor,t.wheels[2].motorTorque);
   }
   float endKph=t.Speed*3.6f,lateral=(t.transform.position.x-start.x)*side;
   t.autoSteer=0;
   yield return new WaitForSeconds(.6f);
   float yaw=Mathf.Abs(Vector3.Dot(t.body.angularVelocity,Vector3.up));
   bool passed=!corrected&&maxBrake==0&&minMotor>500&&minSpeed>=kph/3.6f*.95f&&endKph>=kph&&lateral>.15f&&maxTilt<15&&maxSlip<.5f&&yaw<.1f&&Mathf.Abs(t.wheels[0].steerAngle)<.001f;
   ok&=passed;
   string line=(passed?"PASS":"FAIL")+" kph="+kph+" side="+side+" endKph="+F(endKph)+" minKph="+F(minSpeed*3.6f)+" lateralM="+F(lateral)+" maxTiltDeg="+F(maxTilt)+" maxSideSlip="+F(maxSlip)+" minRearMotorNm="+F(minMotor)+" maxBrakeNm="+F(maxBrake)+" cornerAssisted="+corrected+" recoveredYawRadS="+F(yaw);
   report.AppendLine(line);Debug.Log("MANUAL_CORRECTIONS "+line);
  }
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
  File.WriteAllText(Path.Combine(folder,"manual-steering-corrections.txt"),"success="+ok+"\n"+report);
  Application.Quit(ok?0:1);
 }
}
