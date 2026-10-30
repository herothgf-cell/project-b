using System;
using System.Reflection;
using UnityEngine;

// Exercise the actual obstacle/signal decision. Flashing red uses the same
// full-stop evidence as legacy right-on-red, without requiring a route fixture.
public static class PvpSimulationStopEvidenceTests {
    static void Check(bool value,string detail){if(!value)throw new Exception(detail);}
    static void Pose(PvpSimulationBot pilot,int axis,float distance,bool stopped){
        var direction=axis==0?Vector3.forward:Vector3.right;
        var taxi=pilot.Session.taxi;taxi.transform.SetPositionAndRotation(-direction*(11.4f+distance),Quaternion.LookRotation(direction));
        typeof(PvpSimulationBot).GetProperty("StationarySeconds").GetSetMethod(true).Invoke(pilot,new object[]{stopped?1.1f:0f});
    }
    static void Case(int revisit){
        var root=new GameObject("stop evidence fixture");root.SetActive(false);
        try{
            var session=root.AddComponent<DriveSession>();session.IsSimulationBot=true;
            session.taxi=root.AddComponent<TaxiController>();session.taxi.body=root.AddComponent<Rigidbody>();session.taxi.body.useGravity=false;
            session.traffic=root.AddComponent<TrafficSystem>();session.traffic.mainDirection=Vector3.forward;session.traffic.manualClock=true;
            session.traffic.junctions.Add(new TrafficJunction{name="stop fixture",direction=Vector3.forward,halfWidth=new[]{5f,5f},scenarioSignal=true,scenarioFlashing=true,scenarioForward=SignalColor.Red,rightTurnObservation=new RoadObservation{known=true,pedestrian=PedestrianState.None,trafficClear=true}});
            session.jobs=root.AddComponent<PassengerJobs>();session.jobs.nav=root.AddComponent<RoadNavigation>();
            var pilot=root.AddComponent<PvpSimulationBot>();pilot.Initialize(session);
            Pose(pilot,0,.5f,false);Check(pilot.ObstacleDistance()<10,"fresh red approach needs full stop");
            Pose(pilot,0,.5f,true);Check(pilot.ObstacleDistance()>35,"actual full stop permits departure");
            Pose(pilot,0,.2f,false);Check(pilot.ObstacleDistance()>35,"permission survives starting forward on original approach");
            if(revisit==0){Pose(pilot,0,-.1f,false);Check(pilot.ObstacleDistance()>35,"committed departure retains clearance through crossing tolerance");Pose(pilot,0,.5f,false);}
            else if(revisit==1)Pose(pilot,1,.5f,false);
            else{Pose(pilot,0,40,false);pilot.ObstacleDistance(false);Pose(pilot,0,.5f,false);}
            Check(pilot.ObstacleDistance()<10,revisit==0?"crossing then same-arm reentry must stop again":revisit==1?"changed-arm reentry cannot inherit another approach's stop":"leaving approach then returning must stop again");
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    public static void Run(){
        string failures="";
        for(int i=0;i<3;i++)try{Case(i);}catch(Exception e){failures+=e.Message+"\n";}
        Check(failures.Length==0,failures);Debug.Log("PVP_SIMULATION_STOP_EVIDENCE_OK crossing, approach change, departure, forward start");
    }
}
