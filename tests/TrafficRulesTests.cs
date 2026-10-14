using System;
class TrafficRulesTests {
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);}
    static void Main(){
        for(int i=0;i<640;i++){
            var a=TrafficRules.Signal(i*.1f,0);var b=TrafficRules.Signal(i*.1f,1);
            Check(!(a!=SignalColor.Red&&b!=SignalColor.Red),"conflicting approaches permitted");
        }
        Check(TrafficRules.Signal(0,0)==SignalColor.Green,"initial main green");
        Check(TrafficRules.Signal(12,0)==SignalColor.Yellow,"yellow boundary");
        Check(TrafficRules.Signal(15,0)==SignalColor.Red&&TrafficRules.Signal(15,1)==SignalColor.Red,"clearance");
        Check(TrafficRules.Signal(17,1)==SignalColor.Green,"cross green");
        Check(TrafficRules.Signal(TrafficRules.Cycle,0)==SignalColor.Green,"cycle repeat");
        Check(TrafficRules.SafeSpeed(0,10)==0,"stop at line");
        Check(TrafficRules.SafeSpeed(2,10)<TrafficRules.SafeSpeed(10,10),"braking distance");
        Check(TrafficRules.SafeSpeed(100,10)==10,"cruise cap");
        Check(TrafficRules.ToggleIndicator(0,-1)==-1,"left on");
        Check(TrafficRules.ToggleIndicator(-1,1)==1,"switch right");
        Check(TrafficRules.ToggleIndicator(1,1)==0,"right off");
        Console.WriteLine("PASS: conflict-free cycle and 11 traffic/input policy assertions");
    }
}
