using System.Collections;
using System.Text;
using UnityEngine;

public class EungyeSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string label){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+label);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);
  var map=session.world.map;var taxi=session.taxi;
  Check(map.mapId=="eungye"&&map.coordinateStatus=="verified","Eungye source loaded with verified coordinates");
  Check(map.width>1000&&map.depth>1000,"district-scale map extent "+map.width+" x "+map.depth+"m");
  Check(map.roads.Length>100&&map.buildings.Length>100,"source roads/buildings "+map.roads.Length+" / "+map.buildings.Length);
  Check(map.landmarks!=null&&map.landmarks.Length>0,"source landmarks present");
  Check(session.roadEnvironment.OnRoad(taxi.transform.position,1),"taxi starts on source road");
  bool connected=true;int unreachable=0;float longest=0;
  foreach(var a in session.jobs.stops)foreach(var b in session.jobs.stops){if(a==b)continue;var route=session.jobs.nav.Find(a,b);if(route.Count<2){connected=false;unreachable++;continue;}longest=Mathf.Max(longest,session.jobs.nav.Distance(a,b));}
  Check(connected,"directed curb routes for all stops; unreachable="+unreachable+" longest="+longest);
  Check(session.assisted.network.lanes.Count>20,"assisted driving network "+session.assisted.network.lanes.Count+" lanes");
  bool finite=true,onRoad=true,staticClear=true;int routeSamples=0;foreach(var car in session.traffic.cars){
   for(float at=0;at<car.route.length+1;at+=1){float distance=Mathf.Min(at,car.route.length);var p=car.route.Position(distance);finite&=!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.z);onRoad&=session.roadEnvironment.OnRoad(p,-1.2f);routeSamples++;
    staticClear&=Physics.OverlapBox(p+Vector3.up*.85f,new Vector3(1.05f,.55f,2.3f),Quaternion.LookRotation(car.route.Tangent(distance)),1,QueryTriggerInteraction.Ignore).Length==0;
   }
   car.gameObject.SetActive(false);
  }
  Check(finite&&session.traffic.cars.Count>=2,"nonempty finite traffic fleet; cars="+session.traffic.cars.Count);
  Check(onRoad&&routeSamples>100,"traffic routes stay within mapped roads with vehicle margin; samples="+routeSamples);
  Check(staticClear,"traffic routes clear actual static building and roadside colliders");
  taxi.automated=true;taxi.autoThrottle=.6f;var start=taxi.transform.position;
  yield return new WaitForSeconds(3);taxi.autoThrottle=0;taxi.autoBrake=true;
  Check(Vector3.Distance(start,taxi.transform.position)>2&&session.roadEnvironment.OnRoad(taxi.transform.position,1),"taxi can drive on Eungye road geometry");
  Check(session.jobs.Accept(0)&&session.jobs.nav.path.Count>1,"Eungye call and pickup navigation");
  string dir=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../..","artifacts"));
  yield return new WaitForSeconds(.5f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir,"eungye-drive.png"));yield return new WaitForSeconds(.5f);
  // Give every real-map stream two signal cycles without the test taxi blocking it.
  taxi.enabled=false;taxi.body.velocity=Vector3.zero;taxi.body.isKinematic=true;taxi.transform.position=new Vector3(-100,20,-100);taxi.body.position=taxi.transform.position;
  var moved=new float[session.traffic.cars.Count];for(int i=0;i<moved.Length;i++){var car=session.traffic.cars[i];moved[i]=car.totalTravel;car.gameObject.SetActive(true);}
  Time.timeScale=4;yield return new WaitForSeconds(136);Time.timeScale=1;
  bool flowing=true;for(int i=0;i<moved.Length;i++){var car=session.traffic.cars[i];float travel=car.totalTravel-moved[i];flowing&=travel>5;report.AppendLine("traffic "+i+" moved="+travel.ToString("0.0")+" blocked="+car.blockedName);}
  Check(flowing,"every Eungye traffic stream advances over two signal cycles");
  System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"eungye-smoke.txt"),"success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
