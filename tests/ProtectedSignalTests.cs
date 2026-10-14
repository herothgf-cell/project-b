using System;
class ProtectedSignalTests {
 static void Check(bool c,string m){if(!c)throw new Exception(m);}
 static void Main(){
  for(float t=0;t<TrafficRules.Cycle*2;t+=.1f){int moving=0;for(int axis=0;axis<2;axis++){if(TrafficRules.Signal(t,axis)!=SignalColor.Red)moving++;if(TrafficRules.LeftSignal(t,axis)!=SignalColor.Red)moving++;}Check(moving<=1,"conflicting protected movements");}
  Check(TrafficRules.LeftSignal(1,0)==SignalColor.Red,"straight phase is not protected left");
  Check(TrafficRules.LeftSignal(33,0)==SignalColor.Green,"main protected left");Check(TrafficRules.LeftSignal(45,1)==SignalColor.Green,"cross protected left");
  Check(TrafficRules.Signal(33,0)==SignalColor.Red,"left arrow does not permit straight");
  Console.WriteLine("PASS: protected left signal phases and no conflicting permissions");
 }
}
