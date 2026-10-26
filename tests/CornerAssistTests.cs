using System;
class CornerAssistTests {
 static int count;
 static void Check(bool value,string name){count++;if(!value)throw new Exception(name);}
 static void Main(){
  var full=new DriveMath.Demand{motor=1};
  var right=DriveMath.CornerAssist(full,25/3.6f,1);
  Check(right.motor==0&&right.brake>0,"corner at 25kph must slow even while W is held");
  var left=DriveMath.CornerAssist(full,25/3.6f,-1);
  Check(left.motor==right.motor&&left.brake==right.brake,"left and right behave identically");
  Check(DriveMath.CornerAssist(full,10/3.6f,1).motor==1,"low-speed corner retains acceleration");
  Check(DriveMath.CornerAssist(full,15,0).motor==1,"straight acceleration unchanged");
  Check(DriveMath.CornerAssist(full,15,.1f).motor==1,"small lane corrections do not brake");
  Check(DriveMath.CornerAssist(new DriveMath.Demand{brake=1},15,1).brake==1,"manual brake wins");
  var reverse=DriveMath.CornerAssist(new DriveMath.Demand{motor=-1},-4,1);
  Check(reverse.motor==-1&&reverse.brake==0,"reverse controls unchanged");
  Check(DriveMath.CornerAssist(full,25,1).brake<=.085f,"gentle assistance has bounded brake torque");
  Check(DriveMath.CornerSpeed(1)<DriveMath.CornerSpeed(.5f),"tighter turn requests lower speed");
  float speed=50/3.6f,smallWheelInput=2/DriveMath.SteerAngle(50);
  var correction=DriveMath.CornerAssist(full,speed,smallWheelInput,true);
  Check(correction.motor==1&&correction.brake==0,"assisted two-degree correction at 50kph keeps acceleration");
  var otherSide=DriveMath.CornerAssist(full,speed,-smallWheelInput,true);
  Check(otherSide.motor==correction.motor&&otherSide.brake==correction.brake,"assisted corrections symmetric");
  Check(DriveMath.CornerAssist(new DriveMath.Demand{brake=1},speed,smallWheelInput,true).brake==1,"assisted correction preserves manual brake");
  Check(DriveMath.CornerAssist(full,25/3.6f,1,true).motor==0,"assisted tight low-speed turn still slows");
  bool retainsDemand=true;
  foreach(float kph in new[]{30f,50f,70f}) foreach(float side in new[]{-1f,1f}) {
   var small=DriveMath.CornerAssist(new DriveMath.Demand{motor=.5f},kph/3.6f,side*.25f);
   Console.WriteLine("manual correction kph="+kph+" input="+(side*.25f)+" motor="+small.motor+" brake="+small.brake);
   retainsDemand &= small.motor==.5f&&small.brake==0;
  }
  Check(retainsDemand,"small manual corrections at 30/50/70kph retain throttle without automatic braking");
  Console.WriteLine("PASS: "+count+" corner-assistance checks");
 }
}
