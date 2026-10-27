using System;

// Game balance only. No dependency on driving physics, law scores or manners.
[Serializable] public class DriverCondition {
 public double satiety=80,fatigue=10,mealSeconds,sleepSeconds;
 public int meals,sleeps;
 public Drowsiness drowsiness=new Drowsiness();
 public bool CanAccept {get{return drowsiness==null||!drowsiness.PassengerMustLeave;}}
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
 public void EmergencySleep(){Advance(100,true);fatigue=Math.Max(0,fatigue-20);if(drowsiness!=null)drowsiness.Recover();sleeps++;sleepSeconds+=100;}
 public void Validate(){if(drowsiness==null)drowsiness=new Drowsiness();drowsiness.Validate();if(double.IsNaN(satiety)||double.IsNaN(fatigue)||satiety<0||satiety>100||fatigue<0||fatigue>100||meals<0||sleeps<0||mealSeconds!=meals*50||sleepSeconds!=sleeps*100)throw new ArgumentException("Invalid driver checkpoint");}
}


// Persistent active-gameplay fatigue exposure. Legacy episodic fields stay wire-compatible.
[Serializable] public class Drowsiness {
 public double checkSeconds,remaining;
 public double visionSeconds;
 public int phase,effect,randomState=173;
 public bool Warning {get{return visionSeconds>0&&visionSeconds<60;}}
 public bool Tunnel {get{return visionSeconds>=60;}}
 public bool PassengerMustLeave {get{return visionSeconds>=120;}}
 public bool Blackout {get{return visionSeconds>=180;}}
 public double VisionLoss {get{return visionSeconds<60?0:Math.Min(1,(visionSeconds-30)/150);}}
 public bool ControlsLost {get{return Blackout;}}
 public Drowsiness Copy(){return (Drowsiness)MemberwiseClone();}
 public void Recover(){visionSeconds=checkSeconds=remaining=0;phase=effect=0;}

 public void Advance(double seconds,bool eligible,double fatigue,bool moving=true){
  if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
  if(fatigue<100){Recover();return;}if(!eligible)return;
  // Legacy episodic fields remain readable; only active gameplay exposure now advances.
  visionSeconds=Math.Min(180,visionSeconds+seconds);
 }
 public void Validate(){if(double.IsNaN(visionSeconds)||double.IsInfinity(visionSeconds)||visionSeconds<0||visionSeconds>180||double.IsNaN(checkSeconds)||double.IsInfinity(checkSeconds)||checkSeconds<0||checkSeconds>=20||double.IsNaN(remaining)||double.IsInfinity(remaining)||remaining<0||remaining>3||phase<0||phase>2||effect<0||effect>1||randomState<=0||randomState>=2147483647||phase==0&&remaining!=0||phase!=0&&remaining<=0)throw new ArgumentException("Invalid drowsiness checkpoint");}
}
