using System;

// Game balance only. No dependency on driving physics, law scores or manners.
[Serializable] public class DriverCondition {
 public double satiety=80,fatigue=10,mealSeconds,sleepSeconds;
 public int meals,sleeps;
 public Drowsiness drowsiness=new Drowsiness();
 public bool CanAccept {get{return true;}}
 public DriverCondition Copy(){var c=(DriverCondition)MemberwiseClone();c.drowsiness=drowsiness==null?new Drowsiness():drowsiness.Copy();return c;}
 public void Advance(double seconds,bool sleeping=false){
  if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)return;
  // Integrate exactly across the hunger threshold, independent of frame size.
  double normal=Math.Min(seconds,Math.Max(0,(satiety-25)*12));
  if(!sleeping)fatigue=Math.Min(100,fatigue+normal/15+(seconds-normal)/10);
  satiety=Math.Max(0,satiety-seconds/12);
 }
 public void Meal(){Advance(50);satiety=Math.Min(100,satiety+70);meals++;mealSeconds+=50;}
 public void Sleep(){Advance(100,true);fatigue=Math.Max(0,fatigue-40);if(drowsiness!=null)drowsiness.Recover();sleeps++;sleepSeconds+=100;}
 public void Validate(){if(drowsiness==null)drowsiness=new Drowsiness();drowsiness.Validate();if(double.IsNaN(satiety)||double.IsNaN(fatigue)||satiety<0||satiety>100||fatigue<0||fatigue>100||meals<0||sleeps<0||mealSeconds!=meals*50||sleepSeconds!=sleeps*100)throw new ArgumentException("Invalid driver checkpoint");}
}


// Persisted deterministic sequence: stopping/menu suspends progress without rerolling.
[Serializable] public class Drowsiness {
 public double checkSeconds,remaining;
 public int phase,effect,randomState=173;
 public bool Warning {get{return phase==1;}}
 public bool Tunnel {get{return phase==2&&effect==0;}}
 public bool ControlsLost {get{return phase==2&&effect==1;}}
 public Drowsiness Copy(){return (Drowsiness)MemberwiseClone();}
 public void Recover(){checkSeconds=remaining=0;phase=effect=0;}
 int Draw(){randomState=(int)(((long)randomState*48271)%2147483647);return randomState;}
 public void Advance(double seconds,bool eligible,double fatigue,bool moving=true){
  if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
  if(fatigue<100){Recover();return;}if(!eligible)return;
  while(seconds>0){if(phase==0&&!moving)return;
   double until=phase==0?20-checkSeconds:remaining,step=Math.Min(seconds,until);seconds-=step;
   if(phase==0){checkSeconds+=step;if(checkSeconds>=20){checkSeconds=0;if(Draw()%100<25){effect=Draw()%2;phase=1;remaining=2;}}}
   else {remaining-=step;if(remaining<=0){if(phase==1){phase=2;remaining=effect==0?3:1;}else{phase=0;remaining=0;}}}
  }
 }
 public void Validate(){if(double.IsNaN(checkSeconds)||double.IsInfinity(checkSeconds)||checkSeconds<0||checkSeconds>=20||double.IsNaN(remaining)||double.IsInfinity(remaining)||remaining<0||remaining>3||phase<0||phase>2||effect<0||effect>1||randomState<=0||randomState>=2147483647||phase==0&&remaining!=0||phase!=0&&remaining<=0)throw new ArgumentException("Invalid drowsiness checkpoint");}
}
