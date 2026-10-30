using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Regression: a protected junction must not invent a left lamp for a closed/no-left approach.
public static class ApproachSignalHeadTests {
 static void Check(bool ok,string reason){if(!ok)throw new Exception("APPROACH_SIGNAL_HEAD: "+reason);}
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
 static TrafficJunction Fixture(){return new TrafficJunction{name="fixture",direction=Vector3.forward,leftTurnMode=LeftTurnMode.ProtectedShared,facility=new HybridJunctionRule{allowLeft=true}};}
 static void Head(TrafficSystem system,TrafficJunction junction,Vector3 heading,bool arrow,string reason){
  typeof(TrafficSystem).GetMethod("AddApproach",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(system,new object[]{junction,heading,5f,5f});
  int at=junction.lamps.Count-1;
  Check(junction.lamps[at].Length==3,reason+" retains red/yellow/green");
  Check((junction.leftLamps[at].Length>0)==arrow,reason+" left arrow visibility");
  var housing=junction.lamps[at][0].transform.parent.Find("Signal housing");
  Check(Mathf.Abs(housing.localScale.x-(arrow?2.9f:2.25f))<.001f,reason+" housing matches lamp count");
 }
 public static void Run(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();var host=new GameObject("approach signal head regression");
  try{
   var system=host.AddComponent<TrafficSystem>();system.Build(map,false);
   var j=system.junctions.Single(x=>x.name=="J1927_5181");
   var west=map.hdEdges.Single(e=>e.id=="local:4861:4677:2204:5298");
   Check(!west.disabled&&west.next.Length==0,"reported westbound authored lane is closed");
   var heading=(west.points.Last().V()-west.points[west.points.Length-2].V()).normalized;
   int axis=Mathf.Abs(Vector3.Dot(heading,system.AxisDirection(j,0)))>.7f?0:1,sign=system.Sign(heading,axis,j);
   int at=-1;for(int i=0;i<j.lampAxes.Count;i++)if(j.lampAxes[i]==axis&&j.lampSigns[i]==sign)at=i;
   Check(at>=0,"reported westbound signal approach exists");
   Check(j.leftTurnMode==LeftTurnMode.ProtectedShared&&j.facility.allowLeft,"junction-wide protected configuration retained");
   Check(j.leftLamps[at].Length==0,"closed westbound approach displays no left arrow");
   Check(Mathf.Abs(j.lamps[at][0].transform.parent.Find("Signal housing").localScale.x-2.25f)<.001f,"closed westbound uses three-lamp housing");
   Check(!system.HasLeftMovement(j,axis,sign),"closed westbound capability available independently of lamps");
   system.manualClock=true;system.manualTime=axis*34+(sign>0?0:17)+2-j.offset;system.SendMessage("Update");
   Check(j.lamps[at][2].sharedMaterial.name=="Signal lit 2","closed westbound retains existing forward green phase");

   // Explicit directed fixture: only northbound can reach an enabled left connector.
   var left=new HdEdge{id="left",ruleId="fixture",turn=-1,points=new[]{P(0,-10),P(-10,0)},next=new[]{3}};
   var north=new HdEdge{id="north",ruleId="fixture",points=new[]{P(0,-30),P(0,-10)},next=new[]{2}};
   var south=new HdEdge{id="south",ruleId="fixture",points=new[]{P(0,30),P(0,10)},next=new int[0]};
   var exit=new HdEdge{id="exit",points=new[]{P(-10,0),P(-30,0)},next=new int[0]};
   var fixtureMap=new MapData{hdEdges=new[]{north,south,left,exit}};
   typeof(TrafficSystem).GetField("roadMap",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(system,fixtureMap);
   var fixture=Fixture();
   Head(system,fixture,Vector3.forward,true,"legal northbound left");
   Head(system,fixture,Vector3.back,false,"closed southbound");
   Check(system.HasLeftMovement(fixture,0,1)&&!system.HasLeftMovement(fixture,0,-1),"cached capability stays approach-specific");
   left.disabled=true;Head(system,Fixture(),Vector3.forward,false,"disabled left connector");left.disabled=false;
   exit.disabled=true;Head(system,Fixture(),Vector3.forward,false,"disabled left exit");exit.disabled=false;
   north.disabled=true;Head(system,Fixture(),Vector3.forward,false,"closed authored incoming lane");north.disabled=false;
   Head(system,Fixture(),Vector3.right,true,"unknown direction preserves configured fallback");
   fixture=Fixture();fixture.leftTurnMode=LeftTurnMode.Permissive;Head(system,fixture,Vector3.forward,false,"existing permissive three-lamp head");
   Check(system.HasLeftMovement(fixture,0,1),"permissive three lamps do not imply left prohibition");
   Debug.Log("APPROACH_SIGNAL_HEAD_OK");
  }finally{UnityEngine.Object.DestroyImmediate(host);}
 }
}
