using System;
using System.Reflection;
using UnityEngine;
public static class DemandSignalChecks {
 static int checks;static void Check(bool v,string n){checks++;if(!v)throw new Exception("DEMAND_SIGNAL "+n);}
 public static void Run(){
  checks=0;var type=typeof(TrafficSystem).Assembly.GetType("DemandSignalClock");Check(type!=null,"demand clock exists without editing authored programs");
  var program=new RoadMapSignalProgram{groups=new[]{new RoadMapSignalGroup{id="a",axis=0,sign=1},new RoadMapSignalGroup{id="b",axis=1,sign=1},new RoadMapSignalGroup{id="c",axis=0,sign=1,turn=-1},new RoadMapSignalGroup{id="w",pedestrian=true,axis=0,sign=1}},stages=new[]{new RoadMapStage{groups=new[]{"a"},green=10,yellow=2,clearance=2},new RoadMapStage{groups=new[]{"c"},green=4,yellow=2,clearance=2},new RoadMapStage{groups=new[]{"b"},green=10,yellow=2,clearance=2},new RoadMapStage{groups=new[]{"w"},green=10,yellow=0,clearance=2}}};
  var clock=Activator.CreateInstance(type,new object[]{program});var step=type.GetMethod("Step");var time=type.GetProperty("ProgramTime");var stage=type.GetProperty("Stage");
  Action<float,int[],bool> tick=(dt,demand,occupied)=>step.Invoke(clock,new object[]{dt,demand,occupied});Func<float> now=()=>(float)time.GetValue(clock);Func<int> index=()=>(int)stage.GetValue(clock);
  int[] requests={0,0,3,0};tick(0,requests,false);for(int i=0;i<39;i++)tick(.1f,requests,false);Check(program.Movement(0,1,0,now())==SignalColor.Green,"minimum green cannot be cut off immediately");
  for(int i=0;i<41;i++)tick(.1f,requests,false);Check(index()==2,"empty left stage skipped after yellow and clearance");
  Check((float)type.GetProperty("GreenSeconds").GetValue(clock)>=10,"queued approach gets extended service");
  var wait=type.GetMethod("EstimateGreen");Check((float)wait.Invoke(clock,new object[]{1,1,0})==0,"current movement has zero wait");
  requests=new[]{1,1,1,1};bool pedestrian=false;int[] hits=new int[4];for(int i=0;i<1800;i++){tick(.1f,requests,false);hits[index()]++;float t=now();int vehicles=0;for(int a=0;a<2;a++)if(program.Movement(a,1,0,t)==SignalColor.Green)vehicles++;bool left=program.Movement(0,1,-1,t)==SignalColor.Green;bool walk=program.Walk(0,1,t);Check(vehicles<=1&&(!left||vehicles==0)&&(!walk||vehicles==0&&!left),"no conflicting green");pedestrian|=walk;}
  Check(pedestrian&&Array.TrueForAll(hits,x=>x>0),"continuous competing demand cannot starve any stage");
  while(index()!=3)tick(.1f,requests,false);for(int i=0;i<130;i++)tick(.1f,requests,true);Check(index()==3&&!program.Walk(0,1,now())&&program.Movement(0,1,0,now())==SignalColor.Red,"occupied crossing holds all-red after walk duration");tick(.1f,requests,false);Check(index()!=3,"finished pedestrian releases clearance");
  var saved=type.GetMethod("Capture").Invoke(clock,new object[]{"fixture"});var copy=Activator.CreateInstance(type,new object[]{program});type.GetMethod("Restore").Invoke(copy,new[]{saved});Check(Math.Abs((float)time.GetValue(copy)-now())<.001,"LAN runtime snapshot retains actual phase");
  Check(program.stages[0].green==10&&program.stages[1].green==4,"authored source timings unchanged");Debug.Log("DEMAND_SIGNAL_OK checks="+checks);
 }
}
