using System;
using UnityEngine;

public static class AssistedNetworkTests {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static MapRoad Road(string id,float ax,float az,float bx,float bz){return new MapRoad{id=id,width=12,sourceLanes=4,points=new[]{new MapPoint{x=ax,z=az},new MapPoint{x=bx,z=bz}}};}
    public static void Run(){
        var map=new MapData{roads=new[]{Road("s",0,-80,0,0),Road("n",0,0,0,80),Road("w",-80,0,0,0),Road("e",0,0,80,0)}};
        var net=new AssistedRoadNetwork();net.Build(map);
        float progress;var lane=net.FindLane(new Vector3(1.5f,0,-40),Vector3.forward,out progress);
        Check(lane!=null&&lane.index==0,"find inner forward lane");
        Check(net.Connection(lane,-1)!=null,"inner lane can turn left");
        Check(net.Connection(lane,1)==null,"inner lane cannot turn right");
        var outer=net.Adjacent(lane,1);
        Check(outer!=null&&net.Adjacent(lane,-1)==null,"lane change cannot cross centre");
        Check(net.Connection(outer,1)!=null&&net.Connection(outer,-1)==null,"outer lane turns right only");
        Check(net.Connection(lane,0)!=null,"straight exists");
        Check(net.FindLane(new Vector3(200,0,200),Vector3.forward,out progress)==null,"off road does not attach");
        var join=net.Connection(outer,1);
        Check(Vector3.Distance(join.path.Position(0),outer.path.Position(outer.path.length))<.01f,"turn starts continuously");
        Check(Vector3.Distance(join.path.Position(join.path.length),join.target.path.Position(0))<.01f,"turn ends continuously");
        Check(Vector3.Dot(join.path.Tangent(0),outer.path.Tangent(outer.path.length))>.97f,"entry heading continuous");
        Check(Vector3.Dot(join.path.Tangent(join.path.length),join.target.path.Tangent(0))>.97f,"exit heading continuous");
        net.Build(new MapData{roads=new[]{Road("a",0,0,0,30),Road("b",0,30,0,60)}});
        Check(net.lanes.Count==4,"degree two geometry joins are merged");
        var real=JsonUtility.FromJson<MapData>(Resources.Load<TextAsset>("drive-map").text);net.Build(real);
        Check(net.lanes.Count>10,"real map network");
        var forward=(real.targets[1].V()-real.targets[0].V()).normalized;
        var start=net.FindLane(real.targets[0].V()-forward*23+TrafficSystem.Right(forward)*2.5f,forward,out progress);
        var exit=-TrafficSystem.Right(forward);
        var goal=net.FindLane(real.targets[1].V()+exit*40+TrafficSystem.Right(exit)*2.5f,exit,out progress);
        Check(start!=null&&goal!=null,"trial start and finish attach");
        var primary=net.FindRoute(start,goal,-1);Check(primary.Count>1,"primary route exists");
        var alternative=net.Alternative(primary);Check(alternative.Count>primary.Count,"longer alternative exists");
        Debug.Log("TRIAL_PATHS primary="+string.Join(" > ",primary.ConvertAll(l=>l.sourceIds).ToArray())+" alternative="+string.Join(" > ",alternative.ConvertAll(l=>l.sourceIds).ToArray()));
        Debug.Log("ASSISTED_NETWORK_TEST_OK lanes="+net.lanes.Count);
    }
}
