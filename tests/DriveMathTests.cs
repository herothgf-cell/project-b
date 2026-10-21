using System;
class DriveMathTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){
  Check(2.6/Math.Tan(DriveMath.SteerAngle(0)*Math.PI/180)<=3.1,"parking center turning radius fits a narrow corner");
  Check(DriveMath.SteerAngle(100)<DriveMath.SteerAngle(20),"high speed steering must be reduced");
  Check(DriveMath.SteerAngle(-40)==DriveMath.SteerAngle(40),"reverse steering stability");
  var b=DriveMath.Resolve(-1,10,false);
  Check(b.brake>0 && b.motor==0,"S at speed must brake before reversing");
  var r=DriveMath.Resolve(-1,0,false);
  Check(r.motor<0 && r.brake==0,"S at rest reverses");
  Check(DriveMath.Resolve(1,25,false).motor==0,"forward speed cap");
  Check(DriveMath.Resolve(-1,-8,false).motor==0,"reverse speed cap");
  Check(DriveMath.Resolve(1,0,true).motor==0,"handbrake overrides throttle");
  Check(DriveMath.Resolve(0,0,false).brake>0,"idle hold");
  Console.WriteLine("PASS: 9 driving policy checks");
 }
}
