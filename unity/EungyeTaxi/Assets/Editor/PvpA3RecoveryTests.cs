using System;using UnityEngine;
public static class PvpA3RecoveryTests {
 static void Check(bool value,string name){if(!value)throw new Exception("PVP recovery: "+name);}
 public static void Run(){
  Check(!PvpSimulationBotServices.AllowConditionDetour(3,3,false),"paid rest does not create another noncritical loop before the same fare");
  Check(!PvpSimulationBotServices.AllowConditionDetour(1,1,false),"paid meal does not repeat before the same fare");
  Check(PvpSimulationBotServices.AllowConditionDetour(3,1,false)&&PvpSimulationBotServices.AllowConditionDetour(1,3,false),"complementary native recovery remains available");
  Check(PvpSimulationBotServices.AllowConditionDetour(3,3,true),"critical fatigue still permits necessary repeated recovery");

  Check(PvpSimulationBotServices.RecoveryAtBay(new DriverCondition{fatigue=90,satiety=20},true,true)==3,"stay at real bay for useful additional native rest");
  Check(PvpSimulationBotServices.RecoveryAtBay(new DriverCondition{fatigue=10,satiety=20},true,true)==1,"recover hunger at same available restaurant bay");
  Check(PvpSimulationBotServices.RecoveryAtBay(new DriverCondition{fatigue=10,satiety=90},true,true)==-1,"prepared driver physically departs for fare");
  Check(PvpSimulationBotServices.RecoveryAtBay(new DriverCondition{fatigue=90,satiety=20},false,false)==-1,"no bay teleport or unsupported recovery");
  var hungry=new DriverCondition{fatigue=12,satiety=0};
  Check(PvpSimulationBotServices.ReconsiderCondition(3,hungry),"rested hungry driver may plan complementary meal");
  Check(PvpSimulationBotServices.NeededConditionService(hungry,900,6000,300,double.PositiveInfinity)==1,"meal safe-time benefit survives fatigue100 forecast clamp");
  Check(PvpSimulationBotServices.ReconsiderCondition(3,new DriverCondition{fatigue=12,satiety=40}),"rested driver anticipates complementary meal before starvation");
  var tired=new DriverCondition{fatigue=60,satiety=80};
  Check(PvpSimulationBotServices.ReconsiderCondition(1,tired),"fed tired driver may plan complementary rest");
  Check(PvpSimulationBotServices.NeededConditionService(tired,900,6000,double.PositiveInfinity,0)==3,"fed driver takes native rest before long trip");
  Check(!PvpSimulationBotServices.ReconsiderCondition(3,new DriverCondition{fatigue=20,satiety=80}),"prepared driver does not repeat service before a fare");
  Debug.Log("PVP_A3_RECOVERY_OK checks=14");
 }
}
