using System;

// A view of the next head change, never permission to enter the junction.
public sealed class NextSignalPreview {
 public static readonly NextSignalPreview Unknown=new NextSignalPreview();
 readonly bool known;readonly float seconds;readonly SignalColor straight,left,right;
 public bool Known {get{return known;}}public float Seconds {get{return seconds;}}
 public SignalColor Straight {get{return straight;}}public SignalColor Left {get{return left;}}public SignalColor Right {get{return right;}}
 public string CountdownText {get{return known?UnityEngine.Mathf.CeilToInt(seconds)+"초":"";}}
 NextSignalPreview(){}
 internal NextSignalPreview(float wait,RoadMapSignalProgram program,int axis,int sign,float time){known=true;seconds=(float)Math.Round(Math.Max(0,wait),3);straight=program.Movement(axis,sign,0,time);left=program.Movement(axis,sign,-1,time);right=program.Movement(axis,sign,1,time);}
}

public static class SignalPhasePreview {
 static bool Valid(RoadMapSignalProgram p){return p!=null&&p.stages!=null&&p.stages.Length>0&&p.groups!=null&&p.Cycle>0;}
 static bool Changed(RoadMapSignalProgram p,int axis,int sign,float from,float to){return p.Movement(axis,sign,0,from)!=p.Movement(axis,sign,0,to)||p.Movement(axis,sign,-1,from)!=p.Movement(axis,sign,-1,to)||p.Movement(axis,sign,1,from)!=p.Movement(axis,sign,1,to);}
 public static NextSignalPreview Program(RoadMapSignalProgram p,int axis,int sign,float time){
  if(!Valid(p)||float.IsNaN(time)||float.IsInfinity(time))return NextSignalPreview.Unknown;
  float cycle=p.Cycle,phase=(time%cycle+cycle)%cycle,edge=0,best=float.PositiveInfinity;
  foreach(var stage in p.stages){foreach(float boundary in new[]{edge,edge+stage.green,edge+stage.green+stage.yellow}){float delta=(boundary-phase+cycle)%cycle;if(delta<.0001f)delta=cycle;if(delta<best&&Changed(p,axis,sign,time,time+delta+.001f))best=delta;}edge+=stage.green+stage.yellow+stage.clearance;}
  return float.IsInfinity(best)?NextSignalPreview.Unknown:new NextSignalPreview(best,p,axis,sign,time+best+.001f);
 }
 static bool Pedestrian(RoadMapSignalProgram p,int index){foreach(string id in p.stages[index].groups)foreach(var group in p.groups)if(group.id==id&&group.pedestrian)return true;return false;}
 public static NextSignalPreview Adaptive(RoadMapSignalProgram p,DemandSignalState state,int axis,int sign,bool crossingOccupied){
  if(!Valid(p)||state==null)return NextSignalPreview.Unknown;
  // Restore clones counts. All forecast steps operate on this private clock.
  var clock=new DemandSignalClock(p);clock.Restore(state);var counts=(int[])state.demand.Clone();
  float initial=clock.ProgramTime,total=0;
  for(int boundary=0;boundary<512&&total<=3600;boundary++){
   // Zero time applies demand shortening and already-completed clearances.
   clock.Step(0,counts,crossingOccupied);
   if(Changed(p,axis,sign,initial,clock.ProgramTime))return new NextSignalPreview(total,p,axis,sign,clock.ProgramTime);
   int index=clock.Stage;var stage=p.stages[index];float elapsed=clock.Elapsed,green=clock.GreenSeconds,delta;
   if(elapsed<green){
    bool other=false;for(int i=0;i<counts.Length;i++)if(i!=index&&counts[i]>0)other=true;
    if(!Pedestrian(p,index)&&!other)return NextSignalPreview.Unknown;
    delta=green-elapsed;
    // Match the runtime extension law just before its green boundary. Jumping
    // straight from an old elapsed value would underestimate a queued green.
    if(!Pedestrian(p,index)&&counts[index]>0&&green<stage.green+4){float before=Math.Max(0,delta-.001f);clock.Step(before,counts,crossingOccupied);total+=before;delta-=before;}
   }else if(elapsed<green+stage.yellow)delta=green+stage.yellow-elapsed;
   else {if(crossingOccupied)return NextSignalPreview.Unknown;delta=Math.Max(0,green+stage.yellow+stage.clearance-elapsed);}
   // Mono may compare Elapsed+delta at extended precision, then round the
   // stored Elapsed onto green. Probe just past the boundary so Step applies
   // its extension before the view can mistake that rounding for yellow.
   float step=delta+.0001f;clock.Step(step,counts,crossingOccupied);total+=step;
   if(Changed(p,axis,sign,initial,clock.ProgramTime))return new NextSignalPreview(total,p,axis,sign,clock.ProgramTime);
  }
  return NextSignalPreview.Unknown;
 }
}
