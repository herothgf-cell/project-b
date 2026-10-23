using System.Collections.Generic;
using UnityEngine;

// Records actual signal queues; parked incident props are never classified as prohibited queues.
public class ExitQueueObserver : MonoBehaviour {
 public DriveSession session;
 TrafficJunction junction;Vector3 direction,lanePoint;float tailAlong;bool outside,changed,inserted,blocked,waited,knownAtChange; Vector3 previous;
 string id;readonly List<TrafficCar> queue=new List<TrafficCar>();
 PracticeRoute practice,completedTemplate;float changedAt;Vector3 mergePosition;
 public void ResetAttempt(){junction=null;completedTemplate=null;queue.Clear();previous=session.taxi.transform.position;}
 void FixedUpdate(){
  if(!session.business.useV2||session.paused||session.business.ShowingSummary)return;
  var taxi=session.taxi; if(Vector3.Distance(previous,taxi.transform.position)>15){junction=null;queue.Clear();} previous=taxi.transform.position;
  if(junction==null){
   foreach(var head in session.traffic.cars){
    if(!head.Visible||!head.waitingForSignal||head.speed>.4f)continue;
    TrafficJunction candidate=null;foreach(var stop in head.route.stops)if(stop.distance-head.distance>=-.1f&&stop.distance-head.distance<8&&stop.Movement(head.route)>0){candidate=stop.junction;break;}
    var fixture=session.GetComponent<ExitQueueSituation>();if(candidate==null||!fixture.Active||fixture.junction!=candidate||fixture.Template==completedTemplate||Vector3.Distance(taxi.transform.position,head.transform.position)>90||Vector3.Dot(taxi.transform.forward,head.transform.forward)<.8f)continue;
    queue.Clear();direction=head.transform.forward;lanePoint=head.transform.position;
    foreach(var c in session.traffic.cars){var q=c.transform.position-lanePoint;if(c.Visible&&c.speed<1&&Vector3.Dot(c.transform.forward,direction)>.85f&&Mathf.Abs(Vector3.Dot(q,TrafficSystem.Right(direction)))<1.4f&&Vector3.Dot(q,direction)<=1&&Vector3.Dot(q,direction)>-65)queue.Add(c);}
    if(queue.Count<2)continue;
    tailAlong=0;foreach(var c in queue)tailAlong=Mathf.Min(tailAlong,Vector3.Dot(c.transform.position-lanePoint,direction));
    junction=candidate;practice=fixture.Template.Copy();id=session.law.ids.Next("event");outside=Mathf.Abs(Vector3.Dot(taxi.transform.position-lanePoint,TrafficSystem.Right(direction)))>2;changed=inserted=blocked=waited=false;knownAtChange=true;break;
   }
   return;
  }
  var delta=taxi.transform.position-lanePoint;float along=Vector3.Dot(delta,direction),lateral=Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(direction)));
  if(lateral<1.2f&&along<=tailAlong-4&&along>=tailAlong-20&&taxi.body.velocity.magnitude<.5f)waited=true;
  if(outside&&lateral<1.2f&&!changed){knownAtChange=queue.Exists(c=>c&&c.waitingForSignal);changed=true;changedAt=Time.time;mergePosition=taxi.transform.position;if(along>tailAlong+4)inserted=true;}
  if(lateral>2)outside=true;
  if(changed&&(Time.time-changedAt<=2||inserted&&Vector3.Distance(taxi.transform.position,mergePosition)<8))foreach(var c in queue)if(c&&c.Visible&&c.waitingForObstacle&&c.blockedName.Contains("Taxi")&&Vector3.Dot(taxi.transform.position-c.transform.position,direction)>0)blocked=true;
  var outgoing=TrafficSystem.Right(direction);bool clearedRight=Vector3.Dot(taxi.transform.position-junction.center,outgoing)>junction.halfWidth[practice.axis]+5&&Vector3.Dot(taxi.transform.forward,outgoing)>.5f;
  if(clearedRight||Vector3.Distance(taxi.transform.position,junction.center)>110||Vector3.Dot(taxi.transform.position-junction.center,direction)>20){
   var e=LawEvaluator.Exit(id,session.law.Day,knownAtChange,changed,inserted,blocked,session.law.settings);e.practice=practice;e.scenario="exit";if(waited&&!inserted)e.action="뒤에서 대기";e.junction=junction.name;e.tripId=session.law.tripId;e.assisted=session.assisted.Active;e.occupied=session.jobs.trip.State==TripState.OnTrip;e.seconds=(float)session.business.ledger.Elapsed;session.law.ledger.Record(e);completedTemplate=session.GetComponent<ExitQueueSituation>().Template;junction=null;queue.Clear();
  }
 }
}
