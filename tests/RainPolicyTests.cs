using System;
public static class ContentTuning {
 public static bool rain; public static double coefficient=.9;
 public static bool Flag(string id,bool fallback){return rain;}
 public static double Number(string id,double fallback){return id=="weather.rain.brakeMultiplier"?coefficient:fallback;}
 public static int Int(string id,int fallback){return fallback;}
}
class RainPolicyTests {
 static int n;static void Check(bool b,string why){if(!b)throw new Exception(why);n++;}
 static void Main(){
  ContentTuning.rain=false;Check(RainPolicy.BrakeMultiplier(12)==1,"dry brakes unchanged");Check(RainPolicy.BoardingDirt==0,"dry boarding clean");
  ContentTuning.rain=true;Check(Math.Abs(RainPolicy.BrakeMultiplier(12)-.9)<.0001,"wet moving brakes");Check(RainPolicy.BrakeMultiplier(0)==1,"park holding unchanged");Check(RainPolicy.BrakeMultiplier(-12)==RainPolicy.BrakeMultiplier(12),"reverse parity");Check(RainPolicy.BoardingDirt==2,"wet boarding dirt");
  Check(RainPolicy.TrafficSpeedMultiplier<1,"traffic slows");Check(RainPolicy.ExtraFollowingGap(10)>RainPolicy.ExtraFollowingGap(0),"speed dependent safety margin");Check(!RainPolicy.ProbabilisticSlipEnabled,"unverified slip locked");
  ContentTuning.coefficient=0;Check(RainPolicy.BrakeMultiplier(12)>=.75,"bounded brake floor");ContentTuning.coefficient=3;Check(RainPolicy.BrakeMultiplier(12)==1,"no rain braking boost");Console.WriteLine("RainPolicy checks="+n);
 }
}
