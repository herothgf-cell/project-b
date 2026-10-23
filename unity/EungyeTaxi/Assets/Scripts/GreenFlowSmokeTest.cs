using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Isolated, deterministic physics probe for uninterrupted green approaches and safety gates.
public class GreenFlowSmokeTest : MonoBehaviour {
    public DriveSession session;
    TrafficSystem traffic; TrafficCar car, leader, front; TrafficStop stop;
    readonly StringBuilder samples=new StringBuilder("case,time,remaining,speed,signalWait,junctionWait,obstacleWait,blockedBy\n");
    readonly StringBuilder results=new StringBuilder();
    bool success=true;

    IEnumerator Start() {
        yield return new WaitForSeconds(1);
        traffic=session.traffic;session.taxi.automated=true;
        session.taxi.body.position=new Vector3(-200,.65f,-200);session.taxi.body.velocity=Vector3.zero;
        foreach(var other in traffic.cars){other.Recycle();other.enabled=false;}
        traffic.manualClock=true;traffic.manualTime=1;
        var j=new TrafficJunction{center=new Vector3(1000,0,1000),name="green flow probe",leftTurnMode=LeftTurnMode.ProtectedShared};
        traffic.junctions.Add(j);
        var direction=traffic.mainDirection;
        var route=new TrafficRoute{axis=0,turn=0,direction=direction,length=150,start=j.center-direction*60+TrafficSystem.Right(direction)*2.5f};
        route.end=route.start+direction*route.length;
        stop=new TrafficStop{junction=j,axis=0,distance=60-j.StopDistance(0)-2.1f,exitDistance=60+j.halfWidth[1]+5};route.stops.Add(stop);
        car=Spawn(route,"Green flow probe car",0);leader=Spawn(route,"Green flow probe leader",1);leader.Recycle();leader.enabled=false;
        front=Spawn(route,"Green flow probe front",2);front.Recycle();front.enabled=false;
        yield return Run("clearGreen",1,false,null,true);
        yield return Run("followingGreen",1,true,null,true);
        yield return Run("convoyGreen",1,true,null,true,true);
        yield return Run("red",18,false,null,false);
        yield return Run("yellow",13,false,null,false);
        var blocker=new GameObject("Green flow exit blocker");blocker.layer=9;
        var box=blocker.AddComponent<BoxCollider>();box.size=new Vector3(1.8f,1.2f,4.2f);
        blocker.transform.SetPositionAndRotation(route.Position(stop.exitDistance)+Vector3.up*.8f,Quaternion.LookRotation(direction));
        yield return Run("blockedExit",1,false,blocker,false);
        blocker.transform.position=j.center+TrafficSystem.Right(direction)*2.5f+Vector3.up*.8f;
        yield return Run("occupiedConflict",1,false,blocker,false);
        Destroy(blocker);
        CheckReviewedRoads();
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"green-flow-samples.csv"),samples.ToString());
        string result="success="+success+"\n"+results;
        File.WriteAllText(Path.Combine(folder,"green-flow-smoke.txt"),result);Debug.Log(result);Application.Quit(success?0:1);
    }
    TrafficCar Spawn(TrafficRoute route,string name,int index){var value=new GameObject(name).AddComponent<TrafficCar>();value.Initialize(traffic,route,0,index);traffic.cars.Add(value);return value;}
    void CheckReviewedRoads(){
        var road=new MapRoad{id="reviewed single lane",width=6,lanes=1,oneWay=1,points=new[]{new MapPoint{x=1500,z=1400},new MapPoint{x=1500,z=1700}}};
        var map=new MapData{width=2000,depth=2000,roads=new[]{road},targets=new[]{new MapTarget{name="A",x=1500,z=1500},new MapTarget{name="B",x=1500,z=1600}}};
        var fixture=new GameObject("Reviewed road traffic fixture").AddComponent<TrafficSystem>();fixture.Build(map,true);
        bool centered=true,finite=true,oneWay=true;int mainCars=0;
        foreach(var c in fixture.cars){c.enabled=false;for(float at=0;at<c.route.length;at+=3){var p=c.route.Position(at);finite&=!float.IsNaN(p.x)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.z);}
            if(c.route.axis==0&&c.route.turn==0){mainCars++;centered&=Mathf.Abs(c.route.start.x-1500)<.01f;oneWay&=c.route.direction.z>.99f;}}
        foreach(var j in fixture.junctions)centered&=j.laneCount[0]==1&&j.centeredLane[0];
        var forward=new TrafficRoute{start=new Vector3(1500,0,1400),direction=Vector3.forward,length=300};
        var reverse=new TrafficRoute{start=new Vector3(1500,0,1700),direction=Vector3.back,length=300};
        oneWay&=fixture.AllowsRoute(forward,map)&&!fixture.AllowsRoute(reverse,map)&&mainCars==2;
        road.oneWay=-1;oneWay&=!fixture.AllowsRoute(forward,map)&&fixture.AllowsRoute(reverse,map);
        road.oneWay=0;oneWay&=fixture.AllowsRoute(forward,map)&&fixture.AllowsRoute(reverse,map);
        bool passed=centered&&finite&&oneWay;success&=passed;results.AppendLine("reviewedRoads="+passed+" singleLaneCentered="+centered+" finiteRoutes="+finite+" oneWay="+oneWay+" mainCars="+mainCars);
        Destroy(fixture.gameObject);
    }
    IEnumerator Run(string name,float clock,bool following,GameObject blocker,bool shouldFlow,bool convoy=false){
        traffic.manualTime=clock;leader.Recycle();leader.enabled=false;front.Recycle();front.enabled=false;car.Place(stop.distance-25);car.speed=9;
        if(following){leader.enabled=true;leader.Place(car.distance+20);leader.speed=9;}
        if(convoy){front.enabled=true;front.Place(car.distance+40);front.speed=9;}
        Physics.SyncTransforms();float minSpeed=car.speed;int junctionWait=0,signalWait=0,obstacleWait=0;bool crossed=false,overlap=false;
        for(int frame=0;frame<600;frame++){
            yield return new WaitForFixedUpdate();
            float remaining=stop.distance-car.distance;
            if(remaining>=0){minSpeed=Mathf.Min(minSpeed,car.speed);if(car.waitingForJunction)junctionWait++;if(car.waitingForSignal)signalWait++;if(car.waitingForObstacle)obstacleWait++;}
            if(following&&Vector3.Distance(car.transform.position,leader.transform.position)<4.2f)overlap=true;
            if(convoy&&Vector3.Distance(leader.transform.position,front.transform.position)<4.2f)overlap=true;
            if(frame%5==0)samples.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1:F2},{2:F3},{3:F3},{4},{5},{6},{7}\n",name,frame*Time.fixedDeltaTime,remaining,car.speed,car.waitingForSignal,car.waitingForJunction,car.waitingForObstacle,car.blockedName);
            if(remaining<-.1f){crossed=true;if(shouldFlow)break;}
        }
        bool passed=shouldFlow?crossed&&minSpeed>=8.5f&&junctionWait==0&&!overlap:!crossed&&car.speed<.15f&&car.distance<=stop.distance+.05f;
        success&=passed;results.AppendLine(name+"="+passed+" crossed="+crossed+" minSpeed="+minSpeed.ToString("F3")+" finalSpeed="+car.speed.ToString("F3")+" remaining="+(stop.distance-car.distance).ToString("F3")+" junctionWaitFrames="+junctionWait+" signalWaitFrames="+signalWait+" obstacleWaitFrames="+obstacleWait+" overlap="+overlap);
    }
}
