using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class NativeLeftSignalRegressionTests {
 static void Check(bool value,string reason){if(!value)throw new Exception("NATIVE_LEFT_SIGNAL: "+reason);}
 public static void Run(){
  var map=ProjectMapStore.Load(ProjectMapBuild.Root).Compose();
  var host=new GameObject("native left signal regression");
  try {
   var system=host.AddComponent<TrafficSystem>();system.Build(map,false);
   var j=system.junctions.Single(x=>x.name=="J7973_3744");
   int inbound=Array.FindIndex(map.hdEdges,e=>e.id=="A2218G611886:0");
   int left=Array.FindIndex(map.hdEdges,e=>e.id=="A2218G611452:0");
   Check(inbound>=0&&left>=0,"reported native incoming and left connector exist");
   var incoming=map.hdEdges[inbound];var connector=map.hdEdges[left];
   Check(!incoming.disabled&&!connector.disabled&&incoming.next.Contains(left)&&connector.turn==-1,"reported incoming has enabled directed left connector");
   Check(connector.next.Any(n=>n>=0&&n<map.hdEdges.Length&&!map.hdEdges[n].disabled),"reported connector reaches enabled exit");
   var heading=(incoming.points.Last().V()-incoming.points[incoming.points.Length-2].V()).normalized;
   int axis=Mathf.Abs(Vector3.Dot(heading,system.AxisDirection(j,0)))>.7f?0:1;
   int sign=system.Sign(heading,axis,j);
   Check(axis==0&&sign==1,"reported eastbound approach is axis zero positive");
   var connectorPath=new AssistedRoadNetwork.Path(connector.points.Select(p=>p.V()).ToArray());
   var midpoint=connectorPath.Position(connectorPath.length*.5f);
   var nearest=system.junctions.OrderBy(x=>Vector3.Distance(x.center,midpoint)).First();
   Check(nearest==j&&Vector3.Distance(j.center,midpoint)<32,"native connector belongs to reported junction using traffic association");
   int stage=Array.FindIndex(j.gameProgram.stages,s=>s.groups.Any(id=>j.gameProgram.groups.Any(g=>g.id==id&&!g.pedestrian&&g.axis==axis&&g.sign==sign&&g.turn==-1)));
   Debug.Log("NATIVE_LEFT_SIGNAL_DIAGNOSTIC junction="+j.name+" axis="+axis+" sign="+sign+" incoming="+incoming.id+" connector="+connector.id+" hasLeft="+system.HasLeftMovement(j,axis,sign)+" stage="+stage);
   Check(system.HasLeftMovement(j,axis,sign),"enabled native eastbound left must retain protected left capability");
   Check(stage>=0,"enabled native eastbound left receives a game signal stage");
   bool arrow=false;for(int i=0;i<j.lampAxes.Count;i++)if(j.lampAxes[i]==axis&&j.lampSigns[i]==sign)arrow|=j.leftLamps[i].Length>0;
   Check(arrow,"reported protected approach has a visible left arrow");
   typeof(TrafficSystem).GetMethod("AddDemand",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(system,new object[]{j,axis,sign,-1});
   var pending=(Dictionary<TrafficJunction,int[]>)typeof(TrafficSystem).GetField("signalDemand",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(system);
   Check(pending[j][stage]==1,"Q-on eastbound movement registers a pending left request");
   var liveClock=(DemandSignalClock)typeof(TrafficSystem).GetMethod("DemandClock",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(system,new object[]{j});
   // Capture reads the clock's consumed demand, after the normal timing step.
   liveClock.Step(0,pending[j],false);
   var state=system.CaptureSignalRuntime().Single(s=>s.junction==j.name);
   Debug.Log("NATIVE_LEFT_SIGNAL_DEMAND stage="+stage+" pending="+pending[j][stage]+" captured="+state.demand[stage]);
   Check(state.demand[stage]==1,"normal clock step captures the registered left request");
   var clock=new DemandSignalClock(j.gameProgram);var counts=Enumerable.Repeat(1,j.gameProgram.stages.Length).ToArray();
   bool green=false;for(int tick=0;tick<1200;tick++){clock.Step(.1f,counts,false);if(j.gameProgram.Movement(axis,sign,-1,clock.ProgramTime)==SignalColor.Green){green=true;break;}}
   Check(green,"queued native left receives protected green under competing demand within120seconds");
   foreach(var approach in new[]{new[]{0,-1},new[]{1,-1},new[]{1,1}}){
    var signalJunction=system.junctions.Single(x=>x.name=="J5057_4449");
    Check(system.HasLeftMovement(signalJunction,approach[0],approach[1]),"native left retained at second affected junction "+approach[0]+"/"+approach[1]);
   }
   var closed=system.junctions.Single(x=>x.name=="J1927_5181");
   Check(!system.HasLeftMovement(closed,1,1),"intentionally closed westbound approach remains without left permission");
   var suinLeft=Array.FindIndex(map.hdEdges,e=>e.id=="A222BC004192:0");
   Check(suinLeft>=0&&map.hdEdges[suinLeft].turn==-1&&map.hdEdges.Any(e=>e.status=="provisional-portal"&&!e.disabled&&e.next.Contains(suinLeft)),"J1927 northeast approach retains an active portal into its left connector");
   Check(system.HasLeftMovement(closed,0,1),"J1927 southwestbound approach permits the mapped left");
   var suinStage=closed.gameProgram.stages.Single(s=>s.groups.Any(id=>closed.gameProgram.groups.Any(g=>g.id==id&&g.axis==0&&g.sign==1&&g.turn==-1)));
   Check(suinStage.green==8,"J1927 protected left has eight seconds of green");
   GuardFixtures(system);
   Debug.Log("NATIVE_LEFT_SIGNAL_REGRESSION_OK");
  }finally{UnityEngine.Object.DestroyImmediate(host);}
 }
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
 static void GuardFixtures(TrafficSystem system){
  var j=new TrafficJunction{name="native association fixture",direction=Vector3.forward,facility=new HybridJunctionRule{allowLeft=true}};
  var native=new HdEdge{id="native-in",status="ngii",ruleId="NGII:incoming",points=new[]{P(0,-30),P(0,-10)},next=new[]{5}};
  var left=new HdEdge{id="native-left",status="ngii",ruleId="NGII:left",turn=-1,points=new[]{P(0,-10),P(-10,0)},next=new[]{6}};
  var exit=new HdEdge{id="native-exit",status="ngii",points=new[]{P(-10,0),P(-30,0)},next=new int[0]};
  var edges=new HdEdge[7];Vector3[] headings={Vector3.forward,Vector3.back,Vector3.right,Vector3.left};
  for(int i=0;i<4;i++){var h=headings[i];edges[i]=new HdEdge{id="closed-local-"+i,ruleId=j.name,points=new[]{P(-h.x*30,-h.z*30),P(-h.x*10,-h.z*10)},next=new int[0]};}
  edges[4]=native;edges[5]=left;edges[6]=exit;
  typeof(TrafficSystem).GetField("roadMap",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(system,new MapData{hdEdges=edges});
  system.junctions.Clear();system.junctions.Add(j);
  Check(system.HasLeftMovement(j,0,1),"native directed left augments shared local approach evidence");
  Check(!system.HasLeftMovement(j,0,-1)&&!system.HasLeftMovement(j,1,1)&&!system.HasLeftMovement(j,1,-1),"native left cannot enable opposite or cross-axis approaches");
  native.disabled=true;Check(!system.HasLeftMovement(j,0,1),"disabled native incoming cannot add permission");native.disabled=false;
  left.disabled=true;Check(!system.HasLeftMovement(j,0,1),"disabled native connector cannot add permission");left.disabled=false;
  exit.disabled=true;Check(!system.HasLeftMovement(j,0,1),"disabled native exit cannot add permission");exit.disabled=false;
  native.next=new int[0];Check(!system.HasLeftMovement(j,0,1),"proximity alone without directed incoming link cannot add permission");native.next=new[]{5};
  var neighbour=new TrafficJunction{name="nearer junction",center=new Vector3(-5,0,-5),direction=Vector3.forward};system.junctions.Add(neighbour);
  Check(!system.HasLeftMovement(j,0,1),"native connector assigned to nearer junction cannot add permission");system.junctions.Remove(neighbour);
  j.center=Vector3.right*100;Check(!system.HasLeftMovement(j,0,1),"native connector beyond32metres cannot add permission");j.center=Vector3.zero;
  j.facility.allowLeft=false;Check(!system.HasLeftMovement(j,0,1),"explicit junction prohibition remains authoritative");
 }
}
