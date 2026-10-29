using System;
using UnityEngine;
using Pvp.Map;
namespace Pvp.Tests.EditMode {
public static class PvpMapTests {
 static void Check(bool value,string message) { if(!value) throw new Exception("PVP_MAP: "+message); }
 public static void Run() {
  var d=PvpMapDefinition.CreateDefault(); var m=new PvpMapRuntime(d);
  foreach(int s in m.Spawns) foreach(var a in m.Stops) foreach(var b in m.Stops) {
   Check(PvpRoutePlanner.FindRoute(m,s,a.Node).Found,"spawn access");
   Check(PvpRoutePlanner.FindRoute(m,a.Node,b.Node).Found,"stop exit and access");
  }
  Check(PvpRoutePlanner.FindRoute(m,4,6,PvpRouteMode.Fastest).Distance>PvpRoutePlanner.FindRoute(m,4,6).Distance,"trial fastest differs from shortest");
  Check(!m.IsLegalTransition(new PvpRoadPosition(0,0.5f),new PvpRoadPosition(16,0.5f)),"mid-road lane jump rejected");
  Check(m.IsLegalTransition(new PvpRoadPosition(0,1),new PvpRoadPosition(1,0)),"connected junction transition");
  PvpRoadPosition branch;
  Check(m.TryLocate(new Vector3(0,0,-60),new PvpRoadPosition(0,1),out branch)&&branch.Edge==0,"junction tie retains prior lane");
  Check(m.TryLocate(new Vector3(2,0,-60),new PvpRoadPosition(0,1),out branch)&&branch.Edge==1,"east motion selects east branch");
  Check(m.TryLocate(new Vector3(0,0,-58),new PvpRoadPosition(0,1),out branch)&&branch.Edge==16,"north motion selects internal branch");
  Check(!m.TryLocate(new Vector3(0,0,0),new PvpRoadPosition(0,0.5f),out branch),"tracker cannot jump to remote connector");
  Check(!m.HasConnection(1,0),"opposite direction forbidden");
  Check(!m.HasConnection(0,6),"visual proximity never implies adjacency");
  Check(!PvpRoutePlanner.FindRoute(m,-1,0).Found,"unreachable invalid endpoint");
  var isolated=UnityEngine.Object.Instantiate(d); isolated.Connections=new PvpConnection[0];
  Check(!PvpRoutePlanner.FindRoute(new PvpMapRuntime(isolated),0,1).Found,"unreachable graph");
  Check(m.ContentHash==new PvpMapRuntime(d).ContentHash,"stable hash");
  d.Nodes[0]+=Vector3.right; Check(m.ContentHash!=new PvpMapRuntime(d).ContentHash,"geometry hashed");
  d=PvpMapDefinition.CreateDefault();d.RoadWidth+=1;Check(m.ContentHash!=new PvpMapRuntime(d).ContentHash,"settings hashed");
  PvpRoadPosition prior,recovered; Check(m.TryLocate(new Vector3(-30,0,-60),out prior),"locate");
  Check(m.TryRecover(prior,8,out recovered),"recover");
  Check(recovered.Edge==prior.Edge && recovered.Fraction<=prior.Fraction,"nonadvancing prior edge recovery");
  Check(Vector3.Distance(m.Position(prior),m.Position(recovered))>7.9f,"rearward distance applied");
  var backwards=PvpRoutePlanner.FindRoute(m,prior,m.Connections[prior.Edge].From);Check(backwards.Found && backwards.Distance>100,"no backwards route shortcut");
  Check(!m.TryRecover(new PvpRoadPosition(-1,0),8,out recovered),"invalid recovery rejected");
  Check(!m.TryLocate(new Vector3(1000,0,1000),out recovered),"off road rejected");
  d=PvpMapDefinition.CreateDefault();d.Connections[0]=new PvpConnection(0,999);bool rejected=false;
  try {new PvpMapRuntime(d);}catch(ArgumentException){rejected=true;} Check(rejected,"invalid index rejected");
  d=PvpMapDefinition.CreateDefault();d.Nodes[0]=new Vector3(float.NaN,0,0); rejected=false;
  try {new PvpMapRuntime(d);}catch(ArgumentException){rejected=true;} Check(rejected,"nonfinite geometry rejected");
  Debug.Log("PVP_MAP: all map contract checks passed");
 }
}}
