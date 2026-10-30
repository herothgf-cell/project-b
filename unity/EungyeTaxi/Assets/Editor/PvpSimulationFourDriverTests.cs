using System;
using System.Reflection;
using UnityEngine;
public static class PvpSimulationFourDriverTests {
 static void Check(bool value,string why){if(!value)throw new Exception("Four-driver: "+why);}
 public static void Ledger(){
  var ledger=new PvpSimulationLedger();
  for(int driver=0;driver<4;driver++){
   Check(ledger.TryClaim(driver,driver),"driver "+driver+" can claim its own physical passenger");
   Check(ledger.Settle("native-trip-1",driver,5000+driver*100),"native per-session trip ids may repeat across drivers");
   Check(ledger.SyncExpenses(driver,driver*100),"independent native expenses");
   Check(ledger.Score(driver)==5000,"independent exact net score");
  }
  Check(!ledger.TryClaim(9,4)&&!ledger.TryClaim(9,-1),"invalid participant rejected");
  Check(!ledger.Settle("native-trip-1",3,9000),"per-driver fare remains idempotent");
  ledger.Finish();Check(!ledger.Settle("late",2,5),"all four results frozen");
  Debug.Log("PVP_FOUR_DRIVER_LEDGER_TESTS_OK");
 }
 public static void Calls(){
  var type=typeof(PvpSimulationLedger).Assembly.GetType("PvpSimulationCallBoard");Check(type!=null,"missing common eight-call board");
  var ledger=new PvpSimulationLedger();
  Func<int,JobOffer> factory=serial=>new JobOffer{pickup=serial%18,drop=(serial%18+1)%18,fare=3000,distance=100,seconds=90};
  var board=Activator.CreateInstance(type,new object[]{ledger,factory});
  var offers=(JobOffer[])type.GetProperty("Offers").GetValue(board,null);
  Check(offers.Length==8,"exactly eight shared slots");
  var seen=new System.Collections.Generic.HashSet<int>();
  foreach(var offer in offers)Check(offer!=null&&seen.Add(offer.pickup),"eight different waiting passengers");
  var accepted=offers[0];
  Check((bool)type.GetMethod("TryClaim").Invoke(board,new object[]{accepted,1}),"shared identity accepted");
  Check(!(bool)type.GetMethod("TryClaim").Invoke(board,new object[]{accepted,2}),"stale identical call cannot be claimed twice");
  foreach(var offer in offers)Check(offer!=null&&offer.pickup!=accepted.pickup,"claimed passenger removed and replenished for every viewer");
  Check(ReferenceEquals(offers,type.GetProperty("Offers").GetValue(board,null)),"same visible pool remains bound to all drivers");
  Check(!ledger.Available(accepted.pickup,3),"claim held during real transport");
  int attempts=0;bool available=false;
  Func<int,JobOffer> recovering=serial=>{attempts++;return available?new JobOffer{pickup=serial%18,drop=(serial%18+1)%18,distance=100}:null;};
  var retry=Activator.CreateInstance(type,new object[]{new PvpSimulationLedger(),recovering});
  Check(attempts<=64,"unavailable calls have bounded route-generation work");
  var pending=(JobOffer[])type.GetProperty("Offers").GetValue(retry,null);
  foreach(var offer in pending)Check(offer==null,"unavailable routes do not publish phantom calls");
  available=true;type.GetMethod("Replenish").Invoke(retry,new object[0]);
  Check(ReferenceEquals(pending,type.GetProperty("Offers").GetValue(retry,null)),"recovery preserves every viewer binding");
  foreach(var offer in pending)Check(offer!=null,"available native calls replenish after a bounded failed attempt");
  Debug.Log("PVP_SHARED_EIGHT_CALLS_TESTS_OK");
 }
 public static void Nearby(){
  var map=new MapData{hdEdges=new[]{new HdEdge{id="test-lane",fromNode="a",toNode="b",next=new int[0],points=new[]{new MapPoint{x=0,z=0},new MapPoint{x=0,z=100}}}}};
  var graph=new HdRoadGraph(map);
  var stop=new MapStop{x=0,z=50,dx=0,dz=1,width=3,graphStop=true,laneEdge=0,laneAt=50};
  Check(graph.Route(0,50,0,50)==null,"reproduces native zero-length route absence");
  var method=typeof(PvpSimulationBot).GetMethod("PickupDistance",BindingFlags.Public|BindingFlags.Static);
  Check(method!=null,"bot must consider a physically reachable near pickup without a nonzero graph route");
  var distance=(float)method.Invoke(null,new object[]{graph,new Vector3(0,0,49.9f),Vector3.forward,stop});
  Check(!float.IsInfinity(distance)&&distance<10,"same lane near pickup is selected");
  var backwards=(float)method.Invoke(null,new object[]{graph,new Vector3(0,0,60),Vector3.forward,stop});
  Check(float.IsInfinity(backwards),"does not reverse illegally to passed pickup");
  var service=typeof(PvpSimulationBotServices).GetMethod("AtApproach",BindingFlags.Public|BindingFlags.Static);
  Check(service!=null,"physical service entry must handle a zero-length native approach route");
  Check((bool)service.Invoke(null,new object[]{stop,new Vector3(0,0,50),Vector3.forward}),"service at authored approach may enter driveway even when road route is null");
  Check(!(bool)service.Invoke(null,new object[]{stop,new Vector3(0,0,60),Vector3.forward}),"passed service approach cannot enter backwards");
  Debug.Log("PVP_BOT_NEAR_PICKUP_TESTS_OK");
 }
 public static void Run(){Ledger();Calls();Nearby();}
}
