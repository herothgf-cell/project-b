using System;
using UnityEngine;
public static class PvpSimulationLedgerTests {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 public static void Run(){
  var type=typeof(DriveSession).Assembly.GetType("PvpSimulationLedger");Check(type!=null,"missing simulation shared ledger");var ledger=Activator.CreateInstance(type);
  Func<string,object[],object> call=(name,args)=>type.GetMethod(name).Invoke(ledger,args);
  Check((bool)call("TryClaim",new object[]{4,0}),"first claim");Check(!(bool)call("TryClaim",new object[]{4,1}),"second claim denied");
  call("Release",new object[]{4,1});Check(!(bool)call("TryClaim",new object[]{4,1}),"wrong owner cannot release");call("Release",new object[]{4,0});Check((bool)call("TryClaim",new object[]{4,1}),"released stop available");
  Check((bool)call("Settle",new object[]{"one",0,5000}),"settle first");Check(!(bool)call("Settle",new object[]{"one",0,5000}),"idempotent fare");
  Check(Convert.ToInt32(call("Score",new object[]{0}))==5000,"exact score");call("SyncExpenses",new object[]{0,1200});Check(Convert.ToInt32(call("Score",new object[]{0}))==3800,"service costs reduce score");call("SyncExpenses",new object[]{0,1200});Check(Convert.ToInt32(call("Score",new object[]{0}))==3800,"expenses not double charged");call("Finish",new object[0]);call("SyncExpenses",new object[]{0,2400});Check(Convert.ToInt32(call("Score",new object[]{0}))==3800,"final expenses frozen");Check(!(bool)call("Settle",new object[]{"late",0,5000}),"final result frozen");
  Debug.Log("PVP_SIMULATION_LEDGER_TESTS_OK");
 }
}
