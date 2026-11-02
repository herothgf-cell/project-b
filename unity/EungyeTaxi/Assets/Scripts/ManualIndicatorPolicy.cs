using System;
[Serializable] public sealed class ManualIndicatorState {
 public int direction;public double heading,startHeading,startX,startZ,signedTurn,straightSeconds,maxExcursion;public bool turnObserved;
 public ManualIndicatorState Copy(){return (ManualIndicatorState)MemberwiseClone();}
}
// Samples actual forward motion; time at red, reverse motion and menu time are
// never completion evidence. Assisted steering remains the indicator owner.
public sealed class ManualIndicatorPolicy {
 ManualIndicatorState state=new ManualIndicatorState();
 static bool Finite(double value){return !double.IsNaN(value)&&!double.IsInfinity(value);}
 static double Delta(double from,double to){double n=(to-from)%360;return n>180?n-360:n< -180?n+360:n;}
 public void Begin(int direction,double heading,double x,double z){
  Reset();if((direction!=1&&direction!=-1)||!Finite(heading)||!Finite(x)||!Finite(z))return;
  state=new ManualIndicatorState{direction=direction,heading=heading,startHeading=heading,startX=x,startZ=z};
 }
 public void Reset(){state=new ManualIndicatorState();}
 public ManualIndicatorState Capture(){return state.Copy();}
 public void Restore(ManualIndicatorState saved){
  if(saved==null){Reset();return;}
  if(saved.direction< -1||saved.direction>1||!Finite(saved.heading)||!Finite(saved.startHeading)||!Finite(saved.startX)||!Finite(saved.startZ)||!Finite(saved.signedTurn)||!Finite(saved.straightSeconds)||!Finite(saved.maxExcursion)||saved.straightSeconds<0||saved.straightSeconds>1||saved.maxExcursion<0)throw new ArgumentException("Invalid manual indicator state");
  state=saved.Copy();
 }
 public bool Observe(double speed,double heading,double steering,double x,double z,double seconds,bool hazards,bool assisted){
  if(hazards||assisted){Reset();return false;}
  if(state.direction==0)return false;
  if(!Finite(speed)||!Finite(heading)||!Finite(steering)||!Finite(x)||!Finite(z)||!Finite(seconds)||seconds<=0){state.straightSeconds=0;return false;}
  double delta=Delta(state.heading,heading);state.heading=heading;
  if(speed<=.5){state.straightSeconds=0;return false;}
  // Large one-sample jumps represent placement/reset, not observed driving.
  if(Math.Abs(delta)>80){Begin(state.direction,heading,x,z);return false;}
  state.signedTurn+=delta*state.direction;
  state.maxExcursion=Math.Max(state.maxExcursion,state.signedTurn);
  if(state.signedTurn>=35)state.turnObserved=true;
  double radians=state.startHeading*Math.PI/180;
  double lateral=((x-state.startX)*Math.Cos(radians)-(z-state.startZ)*Math.Sin(radians))*state.direction;
  bool laneChange=state.maxExcursion>=6&&lateral>=2.2&&Math.Abs(Delta(state.startHeading,heading))<=8;
  if((state.turnObserved||laneChange)&&Math.Abs(steering)<=.12)state.straightSeconds=Math.Min(1,state.straightSeconds+seconds);
  else state.straightSeconds=0;
  if(state.straightSeconds<1-1e-8)return false;
  Reset();return true;
 }
}
