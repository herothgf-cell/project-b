using System;

// Game balance only. No dependency on driving physics, law scores or manners.
[Serializable] public class DriverCondition {
 public double satiety=80,fatigue=10,mealSeconds,sleepSeconds;
 public int meals,sleeps;
 public bool CanAccept {get{return fatigue<100;}}
 public DriverCondition Copy(){return (DriverCondition)MemberwiseClone();}
 public void Advance(double seconds,bool sleeping=false){
  if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)return;
  // Integrate exactly across the hunger threshold, independent of frame size.
  double normal=Math.Min(seconds,Math.Max(0,(satiety-25)*12));
  if(!sleeping)fatigue=Math.Min(100,fatigue+normal/15+(seconds-normal)/10);
  satiety=Math.Max(0,satiety-seconds/12);
 }
 public void Meal(){Advance(50);satiety=Math.Min(100,satiety+70);meals++;mealSeconds+=50;}
 public void Sleep(){Advance(100,true);fatigue=Math.Max(0,fatigue-40);sleeps++;sleepSeconds+=100;}
 public void Validate(){if(double.IsNaN(satiety)||double.IsNaN(fatigue)||satiety<0||satiety>100||fatigue<0||fatigue>100||meals<0||sleeps<0||mealSeconds!=meals*50||sleepSeconds!=sleeps*100)throw new ArgumentException("Invalid driver checkpoint");}
}
