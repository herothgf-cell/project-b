using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Opt-in executable observation of the ordinary NPC/pedestrian population.
public sealed class JunctionGridlockProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-junctionGridlockTest")>=0)new GameObject("Junction gridlock observation").AddComponent<JunctionGridlockProbe>();}
 sealed class Progress {public float travel,distance,lastMove,cycleSince=-1;public TrafficRoute route;}
 readonly Dictionary<TrafficCar,Progress> progress=new Dictionary<TrafficCar,Progress>();
 readonly StringBuilder report=new StringBuilder();
 readonly Vector3 center=new Vector3(192.719f,0,518.075f);
 DriveSession session;DistrictTraffic district;float started,realStarted;bool finished;
 bool Near(TrafficCar c){return c&&c.enabled&&c.Visible&&(c.transform.position-center).sqrMagnitude<65*65;}
 IEnumerator Start(){
  realStarted=Time.realtimeSinceStartup;
  while(!(session=FindObjectOfType<DriveSession>())||!session.StartupReady){if(Time.realtimeSinceStartup-realStarted>120){Finish(false,"startup deadline");yield break;}yield return null;}
  yield return null;yield return null;
  district=session.traffic.GetComponent<DistrictTraffic>();
  if(!district){Finish(false,"district missing");yield break;}
  // The Test suffix selects the existing isolated generic-test save path at startup.
  report.AppendLine("savePath="+session.business.SavePath+" population="+session.traffic.cars.Count+" fixedDeltaTime="+Time.fixedDeltaTime+" naturalPopulation=true ambientPedestriansPreserved=true");
  session.business.preparing=false;session.paused=false;
  session.taxi.automated=true;session.taxi.autoBrake=true;
  session.taxi.SetSpawn(new Vector3(211.2f,.65f,519.08f),Quaternion.LookRotation(Vector3.left));session.taxi.ResetTaxi();
  district.ClearPlayerOverlap();Physics.SyncTransforms();
  Time.timeScale=8;started=Time.time;realStarted=Time.realtimeSinceStartup;
  Dump("initial");float nextDump=started+10;
  while(Time.time-started<200){
   session.taxi.autoBrake=true;
   if(Time.realtimeSinceStartup-realStarted>120){Finish(false,"real deadline before 200 simulation seconds");yield break;}
   foreach(var car in session.traffic.cars){
    if(!Near(car)){progress.Remove(car);continue;}
    Progress p;if(!progress.TryGetValue(car,out p)){p=new Progress{travel=car.totalTravel,distance=car.distance,lastMove=Time.time,route=car.route};progress.Add(car,p);}
    if(car.route!=p.route||Mathf.Abs(car.totalTravel-p.travel)>.15f||Mathf.Abs(car.distance-p.distance)>.15f){p.travel=car.totalTravel;p.distance=car.distance;p.route=car.route;p.lastMove=Time.time;}
    bool cycle=ObstacleCycle(car);
    if(!cycle)p.cycleSince=-1;else if(p.cycleSince<0)p.cycleSince=Time.time;
    if(cycle&&Time.time-p.cycleSince>5){Finish(false,"persistent mutual obstacle cycle: "+car.name+" id="+car.GetInstanceID());yield break;}
    if(Time.time-p.lastMove>90){Finish(false,"immobility beyond full signal cycle: "+car.name+" id="+car.GetInstanceID());yield break;}
   }
   if(Time.time>=nextDump){Dump("periodic");nextDump=Time.time+10;}
   yield return null;
  }
  Finish(true,"200 simulation seconds without persistent cycle or 90-second immobility");
 }
 bool ObstacleCycle(TrafficCar origin){
  var seen=new HashSet<TrafficCar>();var car=origin;
  while(car&&car.enabled&&car.Visible&&car.waitingForObstacle&&car.speed<.1f){if(!seen.Add(car))return car==origin;car=car.obstacleCar;}
  return false;
 }
 void Dump(string label){
  report.AppendLine("SNAPSHOT "+label+" sim="+(Time.time-started).ToString("F2")+" real="+(Time.realtimeSinceStartup-realStarted).ToString("F2")+" clock="+(session?session.traffic.Clock:0));
  if(!session||!district)return;
  foreach(var car in session.traffic.cars){if(!Near(car))continue;Progress p;progress.TryGetValue(car,out p);
   report.AppendLine("CAR "+car.name+" id="+car.GetInstanceID()+" pos="+car.transform.position.ToString("F3")+" distance="+car.distance.ToString("F3")+" totalTravel="+car.totalTravel.ToString("F3")+" speed="+car.speed.ToString("F3")+" immobile="+(p==null?0:Time.time-p.lastMove).ToString("F2")+" baselineTarget="+car.baselineTarget+" followingTarget="+car.followingTarget+" waitingSignal="+car.waitingForSignal+" waitingJunction="+car.waitingForJunction+" waitingObstacle="+car.waitingForObstacle+" entryBlocked="+car.entryBlocked+" blocker="+car.blockedName+" obstacleCar="+(car.obstacleCar?car.obstacleCar.GetInstanceID().ToString():"none"));
   var path=car.route.graphPath;if(path!=null){report.Append("ROUTE ");foreach(int edge in path.edges)report.Append(district.graph.edges[edge].id+" -> ");report.AppendLine(" current="+district.graph.edges[path.EdgeAt(Mathf.Clamp(car.distance,0,car.route.length))].id);}
   foreach(var stop in car.route.stops){if(stop.exitDistance<car.distance-5||stop.distance>car.distance+100)continue;
    report.AppendLine("STOP "+stop.junction.name+" distance="+stop.distance+" exit="+stop.exitDistance+" gap="+(stop.distance-car.distance)+" axis="+stop.axis+" turn="+stop.Movement(car.route)+" signalized="+stop.junction.signalized+" canEnter="+session.traffic.CanEnter(car,stop)+" exitClear="+session.traffic.ExitClear(car,stop)+" reserved="+stop.junction.reservations.Contains(car)+" requested="+stop.junction.requests.ContainsKey(car)+" enterPos="+car.route.Position(stop.distance).ToString("F3")+" exitPos="+car.route.Position(stop.exitDistance).ToString("F3"));
   }
  }
 }
 void Finish(bool success,string reason){
  if(finished)return;finished=true;report.AppendLine("RESULT success="+success+" reason="+reason);Dump("final");
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"junction-gridlock-test.txt");File.WriteAllText(path,"success="+success+"\n"+report);
  Debug.Log("JUNCTION_GRIDLOCK_DONE success="+success+" reason="+reason+" report="+path);Application.Quit(success?0:1);
 }
}
