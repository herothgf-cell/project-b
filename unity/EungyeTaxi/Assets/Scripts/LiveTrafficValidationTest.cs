using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Physical taxi inputs, original ambient traffic. Only test starts are positioned.
public class LiveTrafficValidationTest : MonoBehaviour {
 public bool externallyDriven;public float legTimeout=420;
 public float cruise=6;public DriveSession session;bool ok=true,arrived;readonly StringBuilder report=new StringBuilder();
 readonly HashSet<string> stopped=new HashSet<string>();
 void Check(bool value,string name){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+name);Flush();}
 void Flush(){File.WriteAllText("artifacts/live-traffic-validation.txt","success="+ok+"\n"+report);}
 void Place(Vector3 p,Vector3 dir){var t=session.taxi;session.assisted.SetActive(false);t.autoThrottle=0;t.autoBrake=true;t.body.velocity=t.body.angularVelocity=Vector3.zero;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(dir);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();session.law.ResetObservation();session.manners.Suppress(1);stopped.Clear();}
 float Travel(){float sum=0;foreach(var car in session.traffic.cars)sum+=car.totalTravel;return sum;}
 float Limit(AssistedRoadNetwork.Path path,float at,float cruise){
  var taxi=session.taxi;float limit=cruise;float detection=Mathf.Max(20,taxi.Speed*taxi.Speed/4+8);
  for(float step=1;step<detection;step+=.7f){float ahead=Mathf.Min(path.length,at+step);bool hit=false;foreach(var c in Physics.OverlapBox(path.Position(ahead)+Vector3.up*.85f,new Vector3(.94f,.45f,2.1f),Quaternion.LookRotation(path.Tangent(ahead)),(1<<8)|(1<<9)|1,QueryTriggerInteraction.Ignore))if(c.attachedRigidbody!=taxi.body){hit=true;break;}if(hit){limit=Mathf.Min(limit,Mathf.Sqrt(3*Mathf.Max(0,step-3)));break;}}
  foreach(var j in session.traffic.junctions){if(!j.signalized&&!j.scenarioSignal)continue;for(int axis=0;axis<2;axis++)foreach(int sign in new[]{-1,1}){
   var key=j.name+"/"+axis+"/"+sign;var dir=session.traffic.AxisDirection(j,axis)*sign;if(Vector3.Dot(taxi.transform.forward,dir)<.85f){stopped.Remove(key);continue;}
   var front=taxi.transform.position+taxi.transform.forward*2;float gap=Vector3.Dot(session.traffic.StopPosition(j,axis,sign)-front,session.traffic.StopNormal(j,axis,sign)),side=Vector3.Dot(front-j.center,TrafficSystem.Right(dir));
   bool validLane=side>.15f&&side<j.halfWidth[axis]||session.traffic.DirectedApproach(j,axis,sign,taxi.transform.position,taxi.transform.forward);
   if(gap<-.4f||gap>45||!validLane){stopped.Remove(key);continue;}
   if(gap<=3&&taxi.body.velocity.magnitude<.05f)stopped.Add(key);
   var o=session.traffic.Observe(j,axis,sign,dir,taxi.gameObject);int turn=0;var nav=session.jobs.nav;
   if(nav.hdRoute!=null){float begin=0;for(int k=0;k<nav.hdRoute.edges.Count;k++){var edge=nav.hd.edges[nav.hdRoute.edges[k]];if(edge.ruleId==j.name&&begin-at<55&&nav.hdRoute.ends[k]>at){turn=edge.turn;break;}begin=nav.hdRoute.ends[k];}}
   string laneId=session.traffic.EditorLaneId(taxi.transform.position,taxi.transform.forward);
   bool permitted=turn==UturnMovement.Turn?session.traffic.UturnPermitted(j,axis,sign,laneId)&&o.PedestrianClear:turn>0?o.MayTurnRight(stopped.Contains(key)):turn<0?(j.editorProgram!=null?j.editorProgram.Movement(axis,sign,turn,session.traffic.Clock,laneId):session.traffic.LeftSignal(j,axis,sign))==SignalColor.Green&&o.PedestrianClear&&session.traffic.YieldClear(j,dir,turn,taxi.gameObject):o.forward==SignalColor.Green;
   var zones=session.GetComponent<ServiceLawZones>();if(zones&&zones.Owns(j)){var state=zones.Live.State;permitted=state.Flashing?(state.Variant==9||stopped.Contains(key)):state.Signal==SignalColor.Green;permitted&=!state.Blocked&&!state.Oncoming;}
   if(!permitted)limit=Mathf.Min(limit,Mathf.Sqrt(3*Mathf.Max(0,gap-1)));
  }}return limit;
 }
 public bool Arrived {get{return arrived;}}
 public bool Passed {get{return ok;}}
 public IEnumerator Drive(string name,float distanceLimit=0){
  stopped.Clear();var taxi=session.taxi;var nav=session.jobs.nav;arrived=false;float start=Time.time,initial=0,maxError=0,minProgress=0,lastProgressTime=Time.time,maxStopped=0;int collisions=session.manners.collisionEvents,reds=session.manners.redEvents,brakes=session.manners.brakeEvents,lawStart=session.law.ledger.Events.Length;
  Check(nav.hdRoute!=null,name+" connected navigation");if(nav.hdRoute==null)yield break;
  HdRoadGraph.RoutePath traced=null;float probe=0;
  float initialError;initial=nav.ProjectVehicle(out initialError);minProgress=initial;
  Check(session.assisted.SetActive(true),name+" steering armed");
  while(Time.time-start<legTimeout){
   if(session.business.ShowingSummary){report.AppendLine("DAY_ENDED "+name);break;}
   var route=nav.hdRoute;if(route==null)break;if(route!=traced){float err;float pp=route.path.Project(taxi.transform.position,out err);report.AppendLine("ROUTE t="+(Time.time-start)+" pos="+taxi.transform.position+" tangent="+route.path.Tangent(pp)+" forward="+taxi.transform.forward+" length="+route.path.length+" edge="+route.EdgeAt(pp)+" via="+nav.RequiredVia);traced=route;minProgress=pp;lastProgressTime=Time.time;Flush();}float error;float at=nav.ProjectVehicle(out error);maxError=Mathf.Max(maxError,error);if(Time.time>=probe){probe=Time.time+1;report.AppendLine("TRACE t="+(Time.time-start).ToString("F1")+" pos="+taxi.transform.position+" at="+at.ToString("F1")+" error="+error.ToString("F2")+" tangent="+route.path.Tangent(at)+" forward="+taxi.transform.forward+" target="+route.path.Position(at+5));Flush();}
   if(session.manners.redEvents>reds)report.AppendLine("SIGNAL_EVENT "+session.manners.lastEvent+" pos="+taxi.transform.position+" heading="+taxi.transform.forward);
   float remain=route.path.length-at;if(distanceLimit>0)remain=Mathf.Min(remain,distanceLimit-(at-initial));
   float bend=Vector3.Angle(route.path.Tangent(at),route.path.Tangent(Mathf.Min(route.path.length,at+12)));float speed=remain<12?Mathf.Sqrt(2*Mathf.Max(0,remain-1)):bend>12?3.2f:cruise;
   speed=Limit(route.path,at,Mathf.Min(cruise,speed));taxi.autoThrottle=taxi.Speed<speed-.1f?.23f:0;taxi.autoBrake=speed<.15f&&taxi.Speed<.3f;taxi.autoServiceBrake=Mathf.Clamp((taxi.Speed-speed)*.06f,0,.12f);
   if(at>minProgress+.6f){minProgress=at;lastProgressTime=Time.time;}maxStopped=Mathf.Max(maxStopped,Time.time-lastProgressTime);
   if(remain<1.9f&&taxi.body.velocity.magnitude<.5f){arrived=true;break;}
   if(!session.assisted.Active&&remain>2.5f){report.AppendLine("DISARM pos="+taxi.transform.position+" speed="+taxi.Speed+" remain="+remain+" status="+session.assisted.Status+" trip="+session.jobs.trip.State+" nav="+nav.instruction);break;}
   if(Time.time-lastProgressTime>100){report.AppendLine("BLOCK "+name+" pos="+taxi.transform.position+" remaining="+remain+" target="+speed);foreach(var c in session.traffic.cars)if(Vector3.Distance(c.transform.position,taxi.transform.position)<35)report.AppendLine("CAR "+c.name+" v="+c.speed+" signal="+c.waitingForSignal+" junction="+c.waitingForJunction+" obstacle="+c.blockedName);break;}
   yield return new WaitForFixedUpdate();
  }
  taxi.autoThrottle=0;taxi.autoServiceBrake=0;taxi.autoBrake=true;session.assisted.SetActive(false);
  Check(arrived&&maxError<2.5f,name+" physical arrival seconds="+(Time.time-start).ToString("F1")+" maxError="+maxError.ToString("F2")+" maxStopped="+maxStopped.ToString("F1"));
  Check(session.manners.collisionEvents==collisions,name+" no taxi collision");
  Check(session.manners.redEvents==reds,name+" no signal penalty");Check(session.manners.brakeEvents==brakes,name+" no harsh braking penalty");
  var events=session.law.ledger.Events;int deduction=0;for(int i=lawStart;i<events.Length;i++){deduction+=events[i].Deduction;report.AppendLine("LAW "+name+" "+events[i].junction+" "+events[i].family+" "+events[i].action+" deduction="+events[i].Deduction+" signal="+(events[i].observation==null?"none":events[i].observation.forward.ToString()));}
  Check(deduction==0,name+" no independent law deduction");Flush();
 }
 IEnumerator Start(){
  if(externallyDriven)yield break;
  yield return new WaitForSeconds(1);var taxi=session.taxi;taxi.automated=true;taxi.autoBrake=true;session.assisted.allowAutomated=true;session.roadConditions.enabled=false;session.business.enabledCampaign=false;Time.timeScale=4;
  Check(session.world.map.mapId=="eungye"&&session.traffic.cars.Count>=24,"Eungye and 24 ambient vehicles");float traveled=Travel();
  for(int kind=0;kind<3;kind++){
   var goal=session.jobs.stops[session.services.indices[kind]];var points=session.jobs.nav.Find(session.jobs.stops[(session.services.indices[kind]+1)%session.jobs.stops.Length],goal);Check(points.Count>1,"approach path "+kind);if(points.Count<2)continue;
   var path=new AssistedRoadNetwork.Path(points.ToArray());float at=Mathf.Max(0,path.length-110);Place(path.Position(at),path.Tangent(at));yield return new WaitForSeconds(.8f);
   session.jobs.nav.SetDestination(goal);yield return Drive("service approach "+kind);
   Check(arrived&&session.jobs.At(session.services.indices[kind]),"right-side same-direction service stop "+kind);
   if(arrived){session.services.Toggle();Check(session.services.open,"service menu "+kind);int cash=session.business.ledger.Account.Cash;double clock=session.business.ledger.Elapsed;Check(session.services.RequestAction(kind),"quote "+kind);Check(!session.services.ConfirmAction(false)&&cash==session.business.ledger.Account.Cash&&clock==session.business.ledger.Elapsed,"cancel does not charge "+kind);
    if(kind<2){session.services.RequestAction(kind);Check(session.services.ConfirmAction(true),"actual purchase "+kind);}if(session.services.open)session.services.Toggle();Time.timeScale=4;
    session.jobs.nav.Clear();session.jobs.nav.StartFreeGuidance();yield return Drive("service egress "+kind,45);
   }
  }
  var fixture=GetComponent<ExitQueueSituation>();var source=fixture.PreparedTemplate();Check(source!=null,"live exit template available");
  if(source!=null){var districts=new List<DistrictTraffic>();foreach(var c in session.traffic.cars)districts.Add(c.district);
   var travelBefore=Travel();Check(fixture.Replay(source),"eight-car queue with remaining ambient traffic");Time.timeScale=4;yield return new WaitForSeconds(.8f);
   session.jobs.nav.SetDestination(new Vector3(source.points[source.points.Length-2],0,source.points[source.points.Length-1]));yield return Drive("exit wait and turn");
   int count=0;foreach(var e in session.law.ledger.Events)if(e.family=="exit"){count++;report.AppendLine("EXIT action="+e.action+" deduction="+e.Deduction);Check(e.Deduction==0,"waiting exit has no insertion penalty");}Check(count==1,"one physical exit event");
   fixture.Stop();for(int i=0;i<districts.Count;i++)session.traffic.cars[i].district=districts[i];Check(Travel()>travelBefore,"traffic moved during queue test");
  }
  Check(Travel()>traveled+150,"ambient traffic remained active across tests");report.AppendLine("DONE physical throttle/brake + H steering, 24 NPCs; start placement only, not manual play.");Flush();Application.Quit(ok?0:1);
 }
}
