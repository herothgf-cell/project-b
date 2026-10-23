using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class TurnDriveTest : MonoBehaviour {
 public DriveSession session;
 IEnumerator Start(){
  yield return new WaitForSeconds(1);var t=session.taxi;t.automated=true;
  foreach(var car in session.traffic.cars){car.Recycle();car.enabled=false;}
  var pad=GameObject.CreatePrimitive(PrimitiveType.Cube);pad.name="Turn comparison pad";pad.transform.position=new Vector3(-500,-.5f,-500);pad.transform.localScale=new Vector3(400,1,400);
  var report=new StringBuilder();bool ok=true;float firstYaw=0;
  foreach(bool first in new[]{true,false})foreach(int side in new[]{-1,1}){
   t.autoThrottle=0;t.autoSteer=0;t.autoBrake=false;t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;
   t.body.position=new Vector3(-500,.65f,-500);t.body.rotation=Quaternion.identity;t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();session.drivingCamera.SetFirstPerson(first);session.manners.Suppress(30);
   yield return new WaitForSeconds(.8f);t.body.velocity=Vector3.forward*(25/3.6f);t.autoThrottle=1;t.autoSteer=side;
   float maxTilt=0,totalYaw=0;var previousForward=t.transform.forward;for(float elapsed=0;elapsed<4;elapsed+=Time.fixedDeltaTime){yield return new WaitForFixedUpdate();maxTilt=Mathf.Max(maxTilt,Vector3.Angle(Vector3.up,t.transform.up));totalYaw+=Vector3.SignedAngle(previousForward,t.transform.forward,Vector3.up);previousForward=t.transform.forward;}
   float yaw=Mathf.Abs(totalYaw);ok&=Mathf.Sign(totalYaw)==side;
   report.AppendLine("view="+(first?1:3)+" side="+side+" yaw="+yaw+" finalKph="+t.Speed*3.6f+" steer="+t.wheels[0].steerAngle+" tilt="+maxTilt);
   if(first&&side==1)firstYaw=yaw;if(!first&&side==1)ok&=Mathf.Abs(yaw-firstYaw)<3;ok&=yaw>85&&Mathf.Abs(t.Speed*3.6f)<22&&maxTilt<15;
  }
  t.autoSteer=0;t.autoThrottle=0;t.body.velocity=Vector3.zero;t.body.isKinematic=true;session.drivingCamera.SetFirstPerson(false);
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));
  foreach(int index in new[]{9,11}){var route=session.traffic.cars[index].route;float nearest=100;
   for(float distance=8;distance<route.length-15;distance+=.5f){t.transform.SetPositionAndRotation(route.Position(distance)+Vector3.up*.65f,Quaternion.LookRotation(route.Tangent(distance)));yield return null;float cameraDistance=Vector3.Distance(t.transform.position,session.drivingCamera.transform.position);nearest=Mathf.Min(nearest,cameraDistance);}
   report.AppendLine("junction="+index+" minimumCameraDistance="+nearest);ok&=nearest>3;
  }
  File.WriteAllText(Path.Combine(folder,"turn-drive-test.txt"),"success="+ok+"\n"+report);Debug.Log(report.ToString());Application.Quit(ok?0:1);
 }
}
