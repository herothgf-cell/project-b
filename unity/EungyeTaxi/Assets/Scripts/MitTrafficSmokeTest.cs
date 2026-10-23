using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
public sealed class MitTrafficSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string message){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+message);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);
  var traffic=session.traffic;var options=traffic.mitOptions;
  int saved=PlayerPrefs.GetInt(MitTrafficOptions.PreferenceKey,-1);
  Check(options.TestRun,"test isolates user preference");
  foreach(var car in traffic.cars){car.Recycle();car.enabled=false;}
  var route=new TrafficRoute();route.SetPoints(new[]{new Vector3(5000,0,5000),new Vector3(5000,0,5300)});
  // Deliberately outside map bounds: initialize test actors directly; production
  // Spawn correctly rejects this route because it is not a mapped road.
  var leader=new GameObject("MIT fixture leader").AddComponent<TrafficCar>();leader.Initialize(traffic,route,10,400);
  var follower=new GameObject("MIT fixture follower").AddComponent<TrafficCar>();follower.Initialize(traffic,route,0,401);
  Check(leader&&follower,"isolated straight-road pair spawned");
  if(leader&&follower){
   leader.enabled=false;leader.speed=2;
   options.SetEnabled(true);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
   Check(follower.obstacleCar==leader&&follower.followingTarget<follower.baselineTarget,"ON executes actual leader adaptation");
   float at=follower.distance;var originalRoute=follower.route;
   options.SetEnabled(false);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
   Check(follower.followingTarget==follower.baselineTarget,"OFF returns actual vehicle to baseline target");
   Check(follower.route==originalRoute&&follower.distance>=at,"switch preserves route and position progression");
   options.SetEnabled(true);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
   Check(follower.followingTarget<follower.baselineTarget,"re-enable reapplies without respawn");
   leader.speed=0;follower.Place(leader.distance-5.5f);yield return new WaitForSeconds(1);
   Check(follower.speed==0,"follower fully stopped before leader release");
   Check(follower.distance<leader.distance-4.2f,"stationary leader no body overlap");
   float start=follower.distance;leader.enabled=true;yield return new WaitForSeconds(8);
   Check(follower.distance>start+5,"queue restarts after leader departs");
   leader.Recycle();follower.Recycle();leader.enabled=follower.enabled=false;
  }
  Check(PlayerPrefs.GetInt(MitTrafficOptions.PreferenceKey,-1)==saved,"automated switches never overwrite preference");
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
  session.paused=true;Time.timeScale=0;
  foreach(int width in new[]{1280,1600,1920}){
   Screen.SetResolution(width,width*9/16,false);yield return new WaitForSecondsRealtime(.5f);
   options.SetEnabled(width!=1600);yield return new WaitForEndOfFrame();
   ScreenCapture.CaptureScreenshot(Path.Combine(folder,"mit-traffic-menu-"+width+".png"));yield return new WaitForSecondsRealtime(.3f);
  }
  File.WriteAllText(Path.Combine(folder,"mit-traffic-test.txt"),"success="+ok+"\n"+report);
  Debug.Log("MIT_TRAFFIC_DONE success="+ok);Application.Quit(ok?0:1);
 }
}
