using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

// Opt-in isolated render fixture: position-controlled captures at arbitrary map poses,
// not physical driving. Usage: -brightUiCheck -poseReview "x,z,yaw;x,z,yaw" -brightUiOutput <dir>
public sealed partial class BrightUIProbe {
 static string PoseReviewArgument(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-poseReview");return at>=0&&at+1<args.Length?args[at+1]:null;}
 IEnumerator ReviewPoses(string spec){
  var taxi=s.taxi;var camera=s.drivingCamera;var lens=camera.GetComponent<Camera>();var hud=s.GetComponent<DriveHUD>();var readability=s.GetComponent<DrivingReadability>();
  string save=s.business.SavePath;int cash=s.business.ledger.Account.Cash;double elapsed=s.business.ledger.Elapsed;
  Time.timeScale=0;s.traffic.manualClock=true;taxi.enabled=s.business.enabled=s.jobs.enabled=false;taxi.body.velocity=taxi.body.angularVelocity=Vector3.zero;
  foreach(var car in s.traffic.cars)if(car){car.enabled=false;car.gameObject.SetActive(false);}
  Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);
  var args=Environment.GetCommandLineArgs();int inspect=Array.IndexOf(args,"-poseInspect"),hide=Array.IndexOf(args,"-poseHide");
  if(inspect>=0&&inspect+1<args.Length){var q=args[inspect+1].Split(',');var c=new Vector3(float.Parse(q[0],CultureInfo.InvariantCulture),0,float.Parse(q[1],CultureInfo.InvariantCulture));float r=float.Parse(q[2],CultureInfo.InvariantCulture);
   foreach(var mf in FindObjectsOfType<MeshFilter>()){var mesh=mf.sharedMesh;var rend=mf.GetComponent<Renderer>();if(!mesh||!rend||!rend.enabled)continue;if(rend.bounds.SqrDistance(c+Vector3.up*rend.bounds.center.y)>r*r)continue;
    int near=0;if(mesh.isReadable){var v=mesh.vertices;var m=mf.transform.localToWorldMatrix;for(int i=0;i<v.Length;i++){var w=m.MultiplyPoint3x4(v[i]);if((new Vector2(w.x-c.x,w.z-c.z)).sqrMagnitude<r*r)near++;}}
    Debug.Log("POSE_INSPECT obj=\""+mf.gameObject.name+"\" parent=\""+(mf.transform.parent?mf.transform.parent.name:"")+"\" mat="+(rend.sharedMaterial?rend.sharedMaterial.name:"")+" verts="+near+" y="+rend.bounds.center.y.ToString("F2"));}}
  if(hide>=0&&hide+1<args.Length)foreach(var token in args[hide+1].Split('|'))foreach(var rend in FindObjectsOfType<Renderer>())if(rend.gameObject.name.Contains(token)||rend.transform.parent&&rend.transform.parent.name.Contains(token))rend.enabled=false;
  var poses=spec.Split(';');
  for(int n=0;n<poses.Length;n++){
   var v=poses[n].Split(',');if(v.Length<3)continue;
   float x=float.Parse(v[0],CultureInfo.InvariantCulture),z=float.Parse(v[1],CultureInfo.InvariantCulture),yaw=float.Parse(v[2],CultureInfo.InvariantCulture);
   var point=new Vector3(x,.65f,z);var heading=Quaternion.Euler(0,yaw,0);
   taxi.body.position=point;taxi.body.rotation=heading;taxi.transform.SetPositionAndRotation(point,heading);Physics.SyncTransforms();s.law.ResetObservation();s.manners.Suppress(2);
   camera.enabled=hud.enabled=readability.enabled=true;camera.SetFirstPerson(false);yield return null;
   yield return Capture("pose-0"+(n+1)+"-chase");
   camera.enabled=hud.enabled=readability.enabled=false;
   var forward=heading*Vector3.forward;
   foreach(var view in new[]{new{name="low",height=18f,back=-14f,look=22f},new{name="ahead",height=12f,back=4f,look=34f}}){
    camera.transform.position=point+Vector3.up*view.height-forward*view.back;camera.transform.LookAt(point+forward*view.look);lens.fieldOfView=60;
    yield return Capture("pose-0"+(n+1)+"-"+view.name);
   }
   foreach(float height in new[]{40f,90f}){camera.transform.position=point+Vector3.up*height;camera.transform.rotation=Quaternion.Euler(90,yaw,0);lens.fieldOfView=60;yield return Capture("pose-0"+(n+1)+"-top"+(int)height);}
  }
  Check(s.business.SavePath==save&&s.business.ledger.Account.Cash==cash&&s.business.ledger.Elapsed==elapsed,"pose rendering leaves campaign payment/time/save route unchanged");
 }
}
