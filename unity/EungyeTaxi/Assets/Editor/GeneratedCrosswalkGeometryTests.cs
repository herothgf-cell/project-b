using System;
using UnityEngine;

public static class GeneratedCrosswalkGeometryTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception("CROSSWALK_ENDPOINT: "+message);}
 public static void Run(){
  var type=typeof(TrafficSystem).Assembly.GetType("GeneratedCrosswalkGeometry");
  Check(type!=null,"generated crossings fit rendered pavement endpoints");
  var fit=type.GetMethod("Fit");
  var road=new MapRoad{id="wide",width=42,points=new[]{new MapPoint{x=0,z=-20},new MapPoint{x=0,z=20}}};
  var map=new MapData{hybrid=true,roads=new[]{road},areas=new MapArea[0]};
  object[] args={map,Vector3.zero,Vector3.forward,15f};
  Check((bool)fit.Invoke(null,args),"30m provisional crossing resolves on 42m pavement");
  Check(Mathf.Abs((float)args[3]-21)<.02f,"both endpoints reach pavement edges");
  road.points=new[]{new MapPoint{x=4,z=-20},new MapPoint{x=4,z=20}};
  args=new object[]{map,Vector3.zero,Vector3.forward,15f};fit.Invoke(null,args);
  Check(Mathf.Abs(((Vector3)args[1]).x-4)<.02f&&Mathf.Abs((float)args[3]-21)<.02f,"asymmetric footprint shifts crossing center");
  road.width=24;road.points=new[]{new MapPoint{x=0,z=-20},new MapPoint{x=0,z=20}};args=new object[]{map,Vector3.zero,Vector3.forward,21f};fit.Invoke(null,args);
  Check(Mathf.Abs((float)args[3]-12)<.02f,"oversized crossing shrinks to pavement");
  map.roads=new[]{new MapRoad{id="west",width=30,points=new[]{new MapPoint{x=-30,z=0},new MapPoint{x=0,z=0}}},new MapRoad{id="east",width=42.6f,points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=30,z=0}}}};
  args=new object[]{map,new Vector3(-6.8f,0,0),Vector3.right,15f};fit.Invoke(null,args);
  Check((float)args[3]>18,"wide adjacent approach cap expands narrow approach crossing at junction");
  map.roads=new[]{road};map.areas=new[]{new MapArea{kind="sidewalk",height=.14f,points=new[]{new MapPoint{x=10,z=-5},new MapPoint{x=15,z=-5},new MapPoint{x=15,z=5},new MapPoint{x=10,z=5}}}};
  args=new object[]{map,Vector3.zero,Vector3.forward,12f};fit.Invoke(null,args);
  Check(Mathf.Abs(((Vector3)args[1]).x+1)<.02f&&Mathf.Abs((float)args[3]-11)<.02f,"raised sidewalk clips actual endpoint, not only painted width");
  map.hybrid=false;args=new object[]{map,Vector3.zero,Vector3.forward,15f};
  Check(!(bool)fit.Invoke(null,args)&&(float)args[3]==15,"nonhybrid tracks unchanged");
  Debug.Log("CROSSWALK_ENDPOINT_TEST_OK extension/asymmetry/shrinking/adjacent-cap/sidewalk/nonhybrid");
 }
}
