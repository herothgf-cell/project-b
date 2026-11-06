using System;

// Shared by human services, bot planning and HUD forecasts. No engine or save dependency.
public static class PvpConditionRules {
 static PvpConditionSettings current=new PvpConditionSettings();
 public static PvpConditionSettings Settings {get{return current.Copy();}}
 public static void Configure(PvpConditionSettings settings){if(settings==null)throw new ArgumentNullException("settings");settings.Validate();current=settings.Copy();}
 public static void Reset(){current=new PvpConditionSettings();}
 public static double FatigueMultiplier {get{return current.fatigueMultiplier;}}
 public static double MealSeconds {get{return current.mealSeconds;}}
 public static double RestSeconds {get{return current.restSeconds;}}
 public static int MealCost {get{return current.mealCost;}}
 public static int RestCost {get{return current.restCost;}}
 public const double MealSatiety=60,RestFatigue=40;
 public static double FatiguePerMinute(double satiety){return (satiety>25?4:satiety>=10?6:8)*FatigueMultiplier;}
 public static DriverCondition PreviewMeal(DriverCondition driver){
  if(driver==null)return null;var next=driver.Copy();
  AdvanceForecast(next,MealSeconds,false);next.satiety=Math.Min(100,next.satiety+MealSatiety);return next;
 }
 public static DriverCondition PreviewRest(DriverCondition driver){
  if(driver==null)return null;var next=driver.Copy();
  AdvanceForecast(next,RestSeconds,true);next.fatigue=Math.Max(0,next.fatigue-RestFatigue);
  if(next.drowsiness!=null)next.drowsiness.Recover();return next;
 }
 public static PvpConditionForecast Forecast(DriverCondition driver,double seconds,bool resting=false){
  if(driver==null)return null;
  if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)seconds=0;
  var next=driver.Copy();double threshold=resting?double.PositiveInfinity:SecondsToFatigue100(driver);
  AdvanceForecast(next,seconds,resting);
  return new PvpConditionForecast{satiety=next.satiety,fatigue=next.fatigue,
   drowsySeconds=next.drowsiness==null?0:next.drowsiness.visionSeconds,secondsToFatigue100=threshold};
 }
 static void AdvanceForecast(DriverCondition next,double seconds,bool resting){
  double threshold=SecondsToFatigue100(next);
  next.Advance(seconds,resting,FatigueMultiplier);
  if(next.drowsiness==null)next.drowsiness=new Drowsiness();
  if(next.fatigue<100){next.drowsiness.Recover();return;}
  if(!resting)next.drowsiness.Advance(Math.Max(0,seconds-threshold),true,100);
 }
 // Exact piecewise integration, matching DriverCondition.Advance at both hunger boundaries.
 public static double SecondsToFatigue100(DriverCondition driver){return SecondsToFatigue100(driver,FatigueMultiplier);}
 public static double SecondsToFatigue100(DriverCondition driver,double fatigueMultiplier){
  if(driver==null)return double.PositiveInfinity;
  double needed=Math.Max(0,100-driver.fatigue),elapsed=0;
  if(needed==0)return 0;
  double normal=Math.Max(0,(driver.satiety-25)*12),hungry=Math.Max(0,(Math.Min(driver.satiety,25)-10)*12);
  double rate=fatigueMultiplier*(1+Math.Max(-100,Math.Min(1000,driver.choiceFatigueGrowthPercent))/100);
  if(rate<=0)return double.PositiveInfinity;
  double normalGain=normal*4*rate/60;
  if(needed<=normalGain)return needed*60/(4*rate);
  needed-=normalGain;elapsed+=normal;
  double hungryGain=hungry*6*rate/60;
  if(needed<=hungryGain)return elapsed+needed*60/(6*rate);
  return elapsed+hungry+(needed-hungryGain)*60/(8*rate);
 }
}

public sealed class PvpConditionForecast {
 public double satiety,fatigue,drowsySeconds,secondsToFatigue100;
 public bool RestRecommended {get{return fatigue>=70;}}
 public bool EyesClosing {get{return drowsySeconds>=30;}}
 public bool PassengerMustLeave {get{return drowsySeconds>=120;}}
 public bool Blackout {get{return drowsySeconds>=180;}}
}

[Serializable] public sealed class PvpConditionSettings {
 public double fatigueMultiplier=1.25,mealSeconds=15,restSeconds=45;
 public int mealCost=1000,restCost=0;
 public PvpConditionSettings Copy(){return (PvpConditionSettings)MemberwiseClone();}
 public bool Same(PvpConditionSettings other){return other!=null&&fatigueMultiplier==other.fatigueMultiplier&&mealSeconds==other.mealSeconds&&restSeconds==other.restSeconds&&mealCost==other.mealCost&&restCost==other.restCost;}
 public void Validate(){if(!Range(fatigueMultiplier,.25,3)||!Range(mealSeconds,1,300)||!Range(restSeconds,1,300)||mealCost<0||mealCost>100000||restCost<0||restCost>100000)throw new ArgumentException("Invalid PVP condition settings");}
 static bool Range(double value,double min,double max){return !double.IsNaN(value)&&!double.IsInfinity(value)&&value>=min&&value<=max;}
}
