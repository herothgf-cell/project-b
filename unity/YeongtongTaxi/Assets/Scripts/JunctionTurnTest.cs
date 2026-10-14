using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class JunctionTurnTest : MonoBehaviour {
 public DriveSession session;string contact="";
 public void Contact(Collision collision){if(collision.collider.name!="Ground")contact=collision.collider.name;}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);var t=session.taxi;t.automated=true;session.drivingCamera.SetFirstPerson(false);var observer=t.gameObject.AddComponent<TurnContactProbe>();observer.test=this;
  foreach(var car in session.traffic.cars){car.Recycle();car.enabled=false;}
  var result=new StringBuilder();bool ok=true;
  foreach(var junction in session.traffic.junctions)foreach(int sign in new[]{1,-1})foreach(float kph in new[]{10f,25f}){
   var dir=session.traffic.mainDirection*sign;var right=TrafficSystem.Right(dir);t.autoThrottle=0;t.autoSteer=0;t.autoBrake=true;
   t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;var pos=junction.center-dir*12+right*2.5f+Vector3.up*.65f;t.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(dir));t.body.position=pos;t.body.rotation=t.transform.rotation;Physics.SyncTransforms();session.manners.Suppress(30);yield return new WaitForSeconds(.8f);
   t.autoBrake=false;t.autoThrottle=1;t.autoSteer=1;t.body.velocity=dir*(kph/3.6f);contact="";float turn=0,elapsed=0;
   while(elapsed<8&&turn<88){yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;turn=Vector3.SignedAngle(dir,t.transform.forward,Vector3.up);}
   t.autoSteer=0;yield return new WaitForSeconds(1.5f);float exit=Vector3.Dot(t.transform.position-junction.center,right);
   bool passed=turn>=88&&exit>6;ok&=passed;result.AppendLine("junction="+junction.name+" sign="+sign+" startKph="+kph+" turn="+turn+" seconds="+elapsed+" exit="+exit+" speed="+t.Speed*3.6f+" contact="+contact+" passed="+passed);
  }
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));File.WriteAllText(Path.Combine(folder,"junction-turn-test.txt"),"success="+ok+"\n"+result);Debug.Log(result.ToString());Application.Quit(ok?0:1);
 }
}
public class TurnContactProbe : MonoBehaviour {public JunctionTurnTest test;void OnCollisionStay(Collision collision){test.Contact(collision);}}
