using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
public class DistrictTrafficSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;StringBuilder report=new StringBuilder();
 void Check(bool v,string message){ok&=v;report.AppendLine((v?"PASS ":"FAIL ")+message);Debug.Log((v?"PASS ":"FAIL ")+message);}
 IEnumerator Start(){
  yield return new WaitForSeconds(1);Time.timeScale=4;var traffic=session.traffic;var district=traffic.GetComponent<DistrictTraffic>();
  session.taxi.body.isKinematic=true;session.taxi.body.position=new Vector3(-200,1,-200);session.taxi.transform.position=session.taxi.body.position;session.taxi.automated=true;session.taxi.autoBrake=true;session.taxi.gameObject.SetActive(false);Physics.SyncTransforms();session.roadConditions.enabled=false;
  Check(!session.taxi.gameObject.activeInHierarchy,"player taxi excluded from NPC soak");
  Check(district!=null&&traffic.cars.Count==36,"district population 36");
  var starts=new HashSet<int>();int left=0,right=0,overlaps=0,offpath=0;bool hd=false,local=false;var late=new Dictionary<TrafficCar,float>();
  if(district!=null){
   foreach(var car in traffic.cars){Check(car.route.graphPath!=null,"NPC shares graph");starts.Add(car.route.graphPath.edges[0]);}
   Check(starts.Count>=8,"distributed starting lanes");
   for(int t=0;t<180;t++){
    foreach(var car in traffic.cars){if(t==120)late[car]=car.totalTravel;if(!car.Visible)continue;
     float error;car.route.graphPath.path.Project(car.transform.position,out error);if(error>.2f)offpath++;
     int edge=car.route.graphPath.EdgeAt(car.distance);hd|=district.graph.edges[edge].status=="ngii";local|=district.graph.edges[edge].status=="provisional";
     if(car.speed>.5f){if(car.CurrentTurn<0)left++;if(car.CurrentTurn>0)right++;}
     foreach(var hit in Physics.OverlapBox(car.transform.position+Vector3.up*.85f,new Vector3(.7f,.4f,1.6f),car.transform.rotation,1<<9,QueryTriggerInteraction.Ignore)){var other=hit.GetComponentInParent<TrafficCar>();if(other&&other!=car&&other.Visible)overlaps++;}
    }
    yield return new WaitForSeconds(1);
   }
   float total=0;int moved=0,stalled=0;foreach(var car in traffic.cars){total+=car.totalTravel;if(car.totalTravel>50)moved++;if(late.ContainsKey(car)&&car.totalTravel-late[car]<5){stalled++;report.AppendLine("STALLED "+car.name+" at="+car.distance+" blocked="+car.blockedName+" signal="+car.waitingForSignal+" junction="+car.waitingForJunction);}}
   Check(moved==36&&total>5000,"district movement moved="+moved+" metres="+total);
   Check(left>0&&right>0&&hd&&local,"both turns and both source areas");Check(offpath==0&&overlaps==0,"geometry offpath="+offpath+" overlaps="+overlaps);
   Check(stalled==0,"no prolonged stalled cars count="+stalled);Check(district.continued>0,"continuous journeys="+district.continued);
   // Exercise a real mapped signal with a three-car queue, separately from the soak.
   foreach(var c in traffic.cars){c.Recycle();c.enabled=false;}TrafficRoute fixture=null;TrafficStop stop=null;
   foreach(var a in session.world.map.stops){foreach(var b in session.world.map.stops){if(a==b)continue;var r=district.CreateRoute(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);if(r==null)continue;foreach(var s in r.stops)if(s.junction.signalized&&s.turn==0&&s.distance>40&&s.exitDistance<r.length-30){fixture=r;stop=s;break;}if(fixture!=null)break;}if(fixture!=null)break;}
   Check(fixture!=null,"signal fixture available");
   if(fixture!=null){var queue=new List<TrafficCar>();traffic.manualClock=true;int sign=traffic.Sign(fixture.Tangent(stop.distance),stop.axis,stop.junction);int phase=stop.axis*2+(sign>0?0:1);traffic.manualTime=phase*17+16-stop.junction.offset;
    for(int i=0;i<3;i++){var c=traffic.Spawn(fixture,stop.distance-12-i*9,100+i);Check(c!=null,"signal queue spawned");if(c){c.district=district;queue.Add(c);}}
    yield return new WaitForSeconds(14);foreach(var c in queue)Check(c.distance<=stop.distance+.05f,"red stop "+c.name);
    traffic.manualTime=phase*17+2-stop.junction.offset;yield return new WaitForSeconds(22);
    foreach(var c in queue)Check(c.distance>stop.exitDistance+5,"green queue clears "+c.name+" distance="+c.distance+" block="+c.blockedName);
   }
  }
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"district-traffic-test.txt"),"success="+ok+"\n"+report);Debug.Log("DISTRICT_TRAFFIC_DONE success="+ok);Application.Quit(ok?0:1);
 }
}
