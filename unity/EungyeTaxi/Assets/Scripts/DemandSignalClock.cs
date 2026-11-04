using System;
[Serializable] public sealed class DemandSignalState {public string junction;public int stage;public float elapsed,green;public int[] demand;}
// Runtime timing only. Authored stages, groups and safety transitions stay immutable.
public sealed class DemandSignalClock {
 readonly RoadMapSignalProgram program;readonly int[] demand;
 public int Stage {get;private set;}public float Elapsed {get;private set;}public float GreenSeconds {get;private set;}
 public DemandSignalClock(RoadMapSignalProgram source){program=source;demand=new int[source.stages.Length];GreenSeconds=Minimum(0);}
 bool Pedestrian(int index){foreach(string id in program.stages[index].groups)foreach(var g in program.groups)if(g.id==id&&g.pedestrian)return true;return false;}
 float Minimum(int index){return Pedestrian(index)?program.stages[index].green:Math.Min(4,program.stages[index].green);}
 float Duration(int index){var s=program.stages[index];return Pedestrian(index)?s.green:demand[index]>=3?s.green+4:Math.Min(6,s.green);}
 bool OtherDemand(){for(int i=0;i<demand.Length;i++)if(i!=Stage&&demand[i]>0)return true;return false;}
 public float ProgramTime {get{float t=0;for(int i=0;i<Stage;i++){var s=program.stages[i];t+=s.green+s.yellow+s.clearance;}var current=program.stages[Stage];return t+(Elapsed<GreenSeconds?Math.Min(Elapsed,current.green-.001f):current.green+Math.Min(Elapsed-GreenSeconds,current.yellow+current.clearance-.001f));}}
 public void Step(float seconds,int[] counts,bool crossingOccupied){
  if(counts==null||counts.Length!=demand.Length||seconds<0||float.IsNaN(seconds)||float.IsInfinity(seconds))throw new ArgumentException("Invalid signal demand");
  for(int i=0;i<demand.Length;i++)demand[i]=Math.Max(0,Math.Min(100,counts[i]));
  var current=program.stages[Stage];bool pedestrian=Pedestrian(Stage);
  if(Elapsed<GreenSeconds&&!pedestrian){
   if(demand[Stage]>=3)GreenSeconds=Math.Max(GreenSeconds,current.green+4);
   if(demand[Stage]==0&&OtherDemand())GreenSeconds=Math.Max(Minimum(Stage),Math.Min(GreenSeconds,Elapsed));
   if(Elapsed+seconds>=GreenSeconds&&demand[Stage]>0)GreenSeconds=Math.Min(current.green+4,Math.Max(GreenSeconds,Elapsed+2));
   if(Elapsed+seconds>=GreenSeconds&&!OtherDemand()){Elapsed=Math.Max(0,GreenSeconds-.001f);return;}
  }
  Elapsed+=seconds;
  if(Elapsed+.0001f<GreenSeconds+current.yellow+current.clearance||crossingOccupied)return;
  int next=-1;for(int offset=1;offset<=demand.Length;offset++){int index=(Stage+offset)%demand.Length;if(demand[index]>0){next=index;break;}}
  Stage=next<0?0:next;Elapsed=0;GreenSeconds=Math.Max(Minimum(Stage),Duration(Stage));
 }
 public float SecondsToChange(int axis,int sign){
  var s=program.stages[Stage];bool own=false;foreach(string id in s.groups)foreach(var g in program.groups)if(g.id==id&&!g.pedestrian&&g.axis==axis&&g.sign==sign)own=true;
  if(own&&Elapsed<GreenSeconds)return !OtherDemand()&&!Pedestrian(Stage)?-1:Math.Max(.001f,GreenSeconds-Elapsed);
  if(own&&Elapsed<GreenSeconds+s.yellow)return GreenSeconds+s.yellow-Elapsed;
  float best=float.PositiveInfinity;foreach(int turn in new[]{0,-1,1}){float value=EstimateGreen(axis,sign,turn);if(value>0)best=Math.Min(best,value);}return float.IsInfinity(best)?-1:best;
 }
 public float WalkRemaining {get{return Pedestrian(Stage)&&Elapsed<GreenSeconds?GreenSeconds-Elapsed:0;}}
 public float EstimateGreen(int axis,int sign,int turn){
  float t=ProgramTime;if(program.Movement(axis,sign,turn,t)==SignalColor.Green)return 0;
  float wait=Math.Max(0,GreenSeconds+program.stages[Stage].yellow+program.stages[Stage].clearance-Elapsed);
  for(int offset=1;offset<=demand.Length;offset++){
   int index=(Stage+offset)%demand.Length;bool target=false;foreach(string id in program.stages[index].groups)foreach(var g in program.groups)if(g.id==id&&!g.pedestrian&&g.axis==axis&&g.sign==sign&&g.turn==turn)target=true;
   if(target)return Math.Max(.1f,wait);if(demand[index]>0)wait+=Duration(index)+program.stages[index].yellow+program.stages[index].clearance;
  }return -1;
 }
 public DemandSignalState Capture(string id){return new DemandSignalState{junction=id,stage=Stage,elapsed=Elapsed,green=GreenSeconds,demand=(int[])demand.Clone()};}
 public void Restore(DemandSignalState saved){
  if(saved==null||saved.stage<0||saved.stage>=demand.Length||saved.demand==null||saved.demand.Length!=demand.Length||float.IsNaN(saved.elapsed)||float.IsInfinity(saved.elapsed)||saved.elapsed<0||saved.elapsed>3600||float.IsNaN(saved.green)||float.IsInfinity(saved.green)||saved.green<Minimum(saved.stage)||saved.green>program.stages[saved.stage].green+4)throw new ArgumentException("Invalid signal runtime snapshot");
  Stage=saved.stage;Elapsed=saved.elapsed;GreenSeconds=saved.green;for(int i=0;i<demand.Length;i++)demand[i]=Math.Max(0,Math.Min(100,saved.demand[i]));
 }
}
