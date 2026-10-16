using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class HybridDriveSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string message){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+message);Debug.Log((value?"PASS ":"FAIL ")+message);}
 void Place(MapStop stop){var taxi=session.taxi;session.assisted.SetActive(false);taxi.autoThrottle=0;taxi.autoBrake=true;taxi.body.velocity=Vector3.zero;taxi.body.angularVelocity=Vector3.zero;taxi.body.position=stop.Kerb+Vector3.up*.65f;taxi.body.rotation=Quaternion.LookRotation(stop.Direction);taxi.transform.SetPositionAndRotation(taxi.body.position,taxi.body.rotation);Physics.SyncTransforms();}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);Time.timeScale=4;
  var jobs=session.jobs;var nav=jobs.nav;var taxi=session.taxi;var assist=session.assisted;var map=session.world.map;
  taxi.automated=true;assist.allowAutomated=true;session.roadConditions.enabled=false;
  foreach(var car in session.traffic.cars){car.Recycle();car.enabled=false;}
  Check(map.hybrid&&jobs.enabled&&session.business.enabledCampaign,"hybrid jobs and campaign enabled");
  Check(assist.hd.nav==nav&&map.stops.Length>=8,"shared district graph and distributed stops");
  int pairs=0;foreach(var a in map.stops)foreach(var b in map.stops)if(a!=b){var p=nav.Find(a,b);Check(p.Count>1,"stop route "+a.name+" > "+b.name);pairs++;}
  // Choose real mixed-source journeys, rather than testing two independent maps.
  int from=-1,to=-1;float best=float.MaxValue;
  for(int a=0;a<map.stops.Length;a++)for(int b=0;b<map.stops.Length;b++){
   if(a==b||nav.hd.edges[map.stops[a].laneEdge].status!="ngii"||nav.hd.edges[map.stops[b].laneEdge].status!="provisional")continue;
   var p=nav.hd.Route(map.stops[a].laneEdge,map.stops[a].laneAt,map.stops[b].laneEdge,map.stops[b].laneAt);bool portal=false;foreach(int e in p.edges)portal|=nav.hd.edges[e].status=="provisional-portal";
   if(portal&&p.path.length<best){best=p.path.length;from=a;to=b;}
  }
  Check(from>=0,"mixed source trip fixture");
  bool quick=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-hybridQuickTest")>=0;
  if(from>=0&&!quick)for(int direction=0;direction<2;direction++){
   int a=direction==0?from:to,b=direction==0?to:from;var start=map.stops[a];var end=map.stops[b];
   jobs.ClearForDay();Place(start);yield return new WaitForSeconds(.6f);
   float distance=nav.Distance(start,end);jobs.offers[0]=new JobOffer{pickup=a,drop=b,distance=distance,fare=5000,seconds=Mathf.Ceil(distance/5+90),requiredScore=60};
   Check(jobs.Accept(0)&&jobs.trip.State==TripState.ToPickup,"accept call "+direction);
   Check(jobs.Interact()&&jobs.trip.State==TripState.OnTrip,"pickup on right-side stop "+direction);
   Check(nav.hdRoute!=null&&assist.SetActive(true),"route and assistance after boarding "+direction);
   var route=nav.hdRoute;float maxError=0,maxTilt=0;bool arrived=false,sawPortal=false,sawHd=false,sawLocal=false;int contacts=0;int paidBefore=jobs.balance;
   for(float elapsed=0;elapsed<400;elapsed+=.1f){
    if(route==null)break;float error;float at=route.path.Project(taxi.transform.position,out error);maxError=Mathf.Max(maxError,error);maxTilt=Mathf.Max(maxTilt,Vector3.Angle(taxi.transform.up,Vector3.up));
    var edge=nav.hd.edges[route.EdgeAt(at)];sawPortal|=edge.status=="provisional-portal";sawHd|=edge.status=="ngii";sawLocal|=edge.status=="provisional";
    float remain=route.path.length-at;float bend=Vector3.Angle(route.path.Tangent(at),route.path.Tangent(Mathf.Min(route.path.length,at+12)));float speed=remain<8?1.2f:(bend>12||edge.turn!=0||edge.status=="provisional-portal"?3.5f:6f);
    taxi.autoThrottle=taxi.Speed<speed?.45f:0;taxi.autoBrake=taxi.Speed>speed+.4f;
    if(remain<1.8f){taxi.autoThrottle=0;taxi.autoBrake=true;if(taxi.body.velocity.magnitude<.56f){arrived=true;break;}}
    if(!assist.Active&&remain>2.5f)break;
    var demand=new DriveMath.Demand{motor=.7f,brake=.2f};float steer=0;assist.Apply(ref steer,ref demand);if(demand.motor!=.7f||demand.brake!=.2f){Check(false,"driver demand mutated");break;}
    if(Physics.OverlapBox(taxi.transform.position+Vector3.up*.42f,new Vector3(.75f,.25f,1.7f),taxi.transform.rotation,1,QueryTriggerInteraction.Ignore).Length>0)contacts++;
    yield return new WaitForSeconds(.1f);
   }
   Check(arrived,"physical mixed arrival "+direction+" status="+assist.Status);
   Check(sawPortal&&sawHd&&sawLocal,"crossed local/portal/HD "+direction);
   Check(maxError<1.6f&&maxTilt<12&&contacts==0,"stable "+direction+" error="+maxError+" tilt="+maxTilt+" contacts="+contacts);
   Check(jobs.Interact()&&jobs.balance>paidBefore&&jobs.trip.State==TripState.Complete,"dropoff and fare "+direction);
  }
  // Roadside pickup also creates a route on the combined graph.
  jobs.ClearForDay();Place(map.stops[2]);yield return new WaitForSeconds(.4f);
  Check(assist.SetActive(true)&&nav.freeGuidance,"free driving H without a call");
  Place(map.stops[3]);yield return new WaitForSeconds(.4f);nav.Refresh();
  float freeError;if(nav.hdRoute!=null){nav.hdRoute.path.Project(taxi.transform.position,out freeError);Check(freeError<.5f,"free route rebuilt after manual deviation");}else Check(false,"free route rebuilt after manual deviation");
  Check(assist.SetActive(true)&&nav.freeGuidance,"H rearms on another valid lane");
  assist.SetActive(false);
  Check(jobs.Interact()&&jobs.trip.State==TripState.OnTrip,"roadside passenger pickup");
  Check(nav.hdRoute!=null&&!nav.freeGuidance,"roadside pickup creates whole-map route");
  Time.timeScale=1;taxi.autoThrottle=0;taxi.autoBrake=true;
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,quick?"hybrid-quick-test.txt":"hybrid-drive-test.txt"),"success="+ok+"\n"+report);Debug.Log("HYBRID_DRIVE_TEST_DONE success="+ok+" quick="+quick);Application.Quit(ok?0:1);
 }
}
