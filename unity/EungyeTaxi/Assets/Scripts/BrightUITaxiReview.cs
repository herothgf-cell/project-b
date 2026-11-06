using System.Collections;
using UnityEngine;
public sealed partial class BrightUIProbe {
 IEnumerator ReviewTaxiArt(){
  var taxi=s.taxi;var camera=s.drivingCamera;var view=camera.GetComponent<Camera>();var hud=s.GetComponent<DriveHUD>();var readability=s.GetComponent<DrivingReadability>();
  taxi.enabled=false;taxi.body.isKinematic=true;s.business.enabled=false;s.jobs.enabled=false;foreach(var c in s.traffic.cars){c.Recycle();c.enabled=false;}
  yield return null;Check(taxi.body.mass==1400&&taxi.GetComponent<BoxCollider>().size==new Vector3(1.8f,.7f,4),"vehicle physical mass and hull unchanged");Check(taxi.GetComponentsInChildren<Collider>().Length==5,"no extra visual colliders");foreach(var wheel in taxi.wheels)Check(wheel.radius==.34f&&wheel.mass==25&&wheel.suspensionDistance==.25f,"native wheel physics unchanged");
  var savedPosition=taxi.transform.position;var savedRotation=taxi.transform.rotation;camera.enabled=false;hud.enabled=false;readability.enabled=false;Vector3 stage=new Vector3(savedPosition.x,100,savedPosition.z);taxi.transform.SetPositionAndRotation(stage,Quaternion.identity);taxi.body.position=stage;taxi.body.rotation=Quaternion.identity;
  // Set the wheel visuals from their real physical mounting points for the static design review.
  int index=0;foreach(Transform child in taxi.transform)if(child.name=="Tyre visual"){child.localPosition=taxi.wheels[index++].transform.localPosition;child.localRotation=Quaternion.Euler(0,0,90);}
  var floor=WorldBuilder.Box("Review platform",stage+Vector3.down*.39f,new Vector3(30,.1f,30),WorldBuilder.Mat("Review ivory",new Color(.84f,.9f,.97f)),null,false);
  Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);view.fieldOfView=38;
  foreach(var item in new[]{new Vector3(4,2,5.7f),new Vector3(6,1.7f,0),new Vector3(-4,2,-5.7f)}){camera.transform.position=stage+item;camera.transform.LookAt(stage+Vector3.up*.7f);yield return Capture(item.z>1?"taxi-front":item.z< -1?"taxi-rear":"taxi-side");}
  camera.transform.position=stage+new Vector3(-2.1f,.4f,-2.0f);camera.transform.LookAt(stage+new Vector3(-.85f,.05f,-1.3f));yield return Capture("taxi-wheel-closeup");
  Destroy(floor);taxi.transform.SetPositionAndRotation(savedPosition,savedRotation);taxi.body.position=savedPosition;taxi.body.rotation=savedRotation;hud.enabled=readability.enabled=camera.enabled=true;camera.SetFirstPerson(false);yield return null;yield return BeginAction(PassengerType.General);s.requests.enabled=false; // Freeze request presentation during controlled cockpit screenshots.
  foreach(int width in new[]{1280,1920}){
   Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.4f);yield return Capture("taxi-chase-"+width);
   camera.SetFirstPerson(true);yield return new WaitForSecondsRealtime(.3f);Check((view.cullingMask&(1<<8))==0,"first person hides outer shell");Check(taxi.transform.Find("Taxi cockpit").gameObject.activeSelf,"first person shows dashboard");yield return Capture("taxi-cockpit-"+width);
   camera.testLook=-1;yield return new WaitForSecondsRealtime(.6f);Check(camera.LookYaw< -30,"look left preserved");yield return Capture("taxi-look-left-"+width);camera.testLook=1;yield return new WaitForSecondsRealtime(.6f);Check(camera.LookYaw>30,"look right preserved");yield return Capture("taxi-look-right-"+width);camera.testLook=0;
   camera.testRearView=true;yield return null;Check(!taxi.transform.Find("Taxi cockpit").gameObject.activeSelf,"rear view hides cockpit");yield return Capture("taxi-rear-view-"+width);camera.testRearView=false;camera.SetFirstPerson(false);yield return null;Check(!taxi.transform.Find("Taxi cockpit").gameObject.activeSelf,"third person hides cockpit");
  }
 }
}
