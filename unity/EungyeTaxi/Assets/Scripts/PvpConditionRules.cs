using System;

// Shared by human services, bot planning and HUD forecasts. No engine or save dependency.
public static class PvpConditionRules {
 public const double FatigueMultiplier=1.25,MealSeconds=15,RestSeconds=45,MealSatiety=60,RestFatigue=40;
 public const int MealCost=1000;
 public static double FatiguePerMinute(double satiety){return satiety>25?5:satiety>=10?7.5:10;}
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
 public static double SecondsToFatigue100(DriverCondition driver){
  if(driver==null)return double.PositiveInfinity;
  double needed=Math.Max(0,100-driver.fatigue),elapsed=0;
  if(needed==0)return 0;
  double normal=Math.Max(0,(driver.satiety-25)*12),hungry=Math.Max(0,(Math.Min(driver.satiety,25)-10)*12);
  double normalGain=normal*5/60;
  if(needed<=normalGain)return needed*60/5;
  needed-=normalGain;elapsed+=normal;
  double hungryGain=hungry*7.5/60;
  if(needed<=hungryGain)return elapsed+needed*60/7.5;
  return elapsed+hungry+(needed-hungryGain)*60/10;
 }
}

public sealed class PvpConditionForecast {
 public double satiety,fatigue,drowsySeconds,secondsToFatigue100;
 public bool RestRecommended {get{return fatigue>=70;}}
 public bool EyesClosing {get{return drowsySeconds>=30;}}
 public bool PassengerMustLeave {get{return drowsySeconds>=120;}}
 public bool Blackout {get{return drowsySeconds>=180;}}
}
