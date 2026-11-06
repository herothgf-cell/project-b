using System;
// Immutable session tuning is saved by the existing campaign/LAN content snapshot.
public static class RainPolicy {
 public static bool Enabled {get{return ContentTuning.Flag("weather.rain.enabled",false);}}
 public static float Intensity {get{return Enabled?(float)Bound(ContentTuning.Number("weather.rain.intensity",.65),0,1):0;}}
 public static float WiperSeconds {get{return (float)Bound(ContentTuning.Number("weather.rain.wiperSeconds",1.2),.5,3);}}
 public static int BoardingDirt {get{return Enabled?Math.Max(0,Math.Min(10,ContentTuning.Int("weather.rain.boardingDirt",2))):0;}}
 public static float BrakeMultiplier(float speed){return !Enabled||Math.Abs(speed)<.5?1:(float)Bound(ContentTuning.Number("weather.rain.brakeMultiplier",.9),.75,1);}
 public static float TrafficSpeedMultiplier {get{return Enabled?(float)Bound(ContentTuning.Number("weather.rain.trafficSpeedMultiplier",.85),.6,1):1;}}
 public static float ExtraFollowingGap(float speed){return Enabled?Math.Max(0,speed)*.5f:0;}
 public static bool ProbabilisticSlipEnabled {get{return false;}} // Gate: physical acceptance + explicit activation, never a random accident.
 static double Bound(double value,double min,double max){return double.IsNaN(value)||double.IsInfinity(value)?max:Math.Max(min,Math.Min(max,value));}
}
