using System;
class ManualHandlingTests {
 static int count;
 static void Check(bool ok,string message){count++;if(!ok){Console.WriteLine("FAIL: "+message);Environment.Exit(1);}}
 static void Main(){
  // Returning to the old immediate 2x1100Nm demand must fail: city starts need time to judge a corner.
  float acceleration=DriveMath.Resolve(1,0,false).motor*2200/(.34f*1400);
  Check(acceleration>=1.8f&&acceleration<=2.6f,"full city throttle must request 1.8-2.6m/s2, actual="+acceleration);
  float braking=DriveMath.Resolve(-1,8,false).brake*14000/(.34f*1400);
  Check(braking>=3.5f&&braking<=6,"ordinary S braking is effective and comfortable, actual="+braking);
  Check(DriveMath.Resolve(1,8,true).brake==1,"Space retains the emergency brake");
  Check(DriveMath.Resolve(-1,0,false).motor<0,"reverse remains available at rest");
  Check(DriveMath.Resolve(-1,8,false).motor==0,"S cannot reverse before slowing");
  Check(2.6/Math.Tan(DriveMath.SteerAngle(8)*Math.PI/180)<=3.1,"parking turn center radius at 8kph is at most 3.1m");
  foreach(float kph in new[]{30f,50f,90f}){
   double speed=kph/3.6;
   Check(speed*speed*Math.Tan(DriveMath.SteerAngle(kph)*Math.PI/180)/2.6<=5.01,"high-speed lateral stability at "+kph);
  }
  Check(DriveMath.SteerAngle(-8)==DriveMath.SteerAngle(8),"reverse uses the same parking steering geometry");
  Check(2.6/Math.Tan(DriveMath.ManualSteerAngle(8)*Math.PI/180)<2.4,"manual low-speed center radius below 2.4m");
  Check(DriveMath.ManualSteerAngle(-8)==DriveMath.ManualSteerAngle(8),"manual reverse symmetry");
  Check(2.6/Math.Tan(DriveMath.ManualSteerAngle(15)*Math.PI/180)<2.25,"manual15kph full lock center radius below2.25m");
  Check(DriveMath.ManualSteerAngle(20)>28,"manual20kph turn has noticeably more authority");
  foreach(float speed in new[]{30f,50f,90f})Check(DriveMath.ManualSteerAngle(speed)==DriveMath.SteerAngle(speed),"manual high-speed limit preserved "+speed);
  float outer=DriveMath.WheelSteerAngle(42,true),inner=DriveMath.WheelSteerAngle(42,false);
  Check(inner>42&&outer<42,"inside front wheel turns tighter to prevent low-speed tyre scrub");
  double leftRadius=2.6/Math.Tan(outer*Math.PI/180)-.85;
  double rightRadius=2.6/Math.Tan(inner*Math.PI/180)+.85;
  Check(Math.Abs(leftRadius-rightRadius)<.001,"both front wheels follow the same turning center");
  Check(DriveMath.WheelSteerAngle(-42,true)==-inner&&DriveMath.WheelSteerAngle(-42,false)==-outer,"Ackermann left/right symmetry");
  Check(DriveMath.WheelSteerAngle(0,true)==0&&DriveMath.WheelSteerAngle(0,false)==0,"straight wheels remain straight");
  Check(!DriveMath.HarshPassengerMotion(-5.8f,8,false),"measured ordinary S braking remains comfortable");
  Check(DriveMath.HarshPassengerMotion(-6.1f,8,false),"measured unnecessary emergency braking remains detectable on actual tyres");
  Check(!DriveMath.HarshPassengerMotion(-6.1f,8,true),"necessary emergency braking remains exempt");
  Check(!DriveMath.HarshPassengerMotion(-10,1,false),"low-speed parking impulses do not count as passenger harsh braking");
  Check(!DriveMath.HarshPassengerMotion(4.4f,8,false)&&DriveMath.HarshPassengerMotion(4.6f,8,false),"sustained excessive acceleration remains distinct from normal acceleration");
  Check(DriveMath.HarshPassengerMotion(-6.1f,-8,false),"reverse comfort detection uses travel acceleration and absolute speed");
  Console.WriteLine("PASS: "+count+" manual city handling checks");
 }
}
