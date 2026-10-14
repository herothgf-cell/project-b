using System;
using UnityEngine;

public static class KoreanRoadTests {
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    public static void Run(){
        var roads=new MapRoad[4];var ends=new[]{new Vector3(0,0,-100),new Vector3(0,0,100),new Vector3(-100,0,0),new Vector3(100,0,0)};
        for(int i=0;i<4;i++)roads[i]=new MapRoad{id="test"+i,width=7,sourceLanes=2,points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=ends[i].x,z=ends[i].z}}};
        var net=new AssistedRoadNetwork();net.Build(new MapData{roads=roads});float at;
        var lane=net.FindLane(new Vector3(1.75f,0,-40),Vector3.forward,out at);
        Check(lane!=null,"7m two-way road must support assisted driving");
        Check(net.Adjacent(lane,1)==null&&net.Adjacent(lane,-1)==null,"one lane each way cannot change lanes");
        Check(net.Connection(lane,-1)!=null&&net.Connection(lane,1)!=null,"shared lane supports both turns");
        Debug.Log("KOREAN_ROAD_TEST_OK");
    }
}
