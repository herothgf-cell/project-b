using System;
class AssistedDrivingPolicyTests {
 static int count;
 static void Check(bool value,string name){count++;if(!value)throw new Exception(name);}
 static void Main(){
  var input=new AssistedDrivingPolicy();
  Check(input.Pulse(1)==1,"first direction fires");
  Check(input.Pulse(1)==0,"held input does not change another lane");
  Check(input.Pulse(-1)==0,"reversal must pass through neutral");
  Check(input.Pulse(0)==0&&input.Pulse(-1)==-1,"neutral rearms");
  Check(!AssistedDrivingPolicy.CanChange(0,2,-1,false,5),"cannot cross centre line");
  Check(!AssistedDrivingPolicy.CanChange(1,2,1,false,5),"cannot leave kerb");
  Check(AssistedDrivingPolicy.CanChange(0,2,1,false,5),"adjacent lane allowed");
  Check(!AssistedDrivingPolicy.CanChange(0,2,1,true,5),"no queue during change");
  Check(!AssistedDrivingPolicy.CanChange(0,2,1,false,0),"no sideways motion at rest");
  Check(!AssistedDrivingPolicy.CanChange(0,2,1,false,-2),"no change in reverse");
  Check(AssistedDrivingPolicy.RearRisk(12,10,5),"fast approaching rear car warns");
  Check(!AssistedDrivingPolicy.RearRisk(30,5,10),"receding rear car does not warn");
  Check(AssistedDrivingPolicy.CruiseLimit(false,0,0,1)>10,"straight crossing keeps approach speed");
  Check(AssistedDrivingPolicy.CruiseLimit(false,1,0,30)<6,"turning connection limits speed");
  Check(AssistedDrivingPolicy.CruiseLimit(false,0,-1,10)<6,"upcoming turn slows before entry");
  Console.WriteLine("PASS: "+count+" assisted input and gap checks");
 }
}
