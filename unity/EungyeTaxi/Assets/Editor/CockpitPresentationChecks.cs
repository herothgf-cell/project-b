using System;
using UnityEngine;
public static class CockpitPresentationChecks {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception("COCKPIT_PRESENTATION "+message);}
 public static void Run(){
  checks=0;var type=typeof(FareTrip).Assembly.GetType("CockpitPresentation");Check(type!=null,"trip status and deadline presentation exists");
  var status=type.GetMethod("TaxiStatus");Check((string)status.Invoke(null,new object[]{TripState.Idle})=="빈차","idle taxi is vacant");Check((string)status.Invoke(null,new object[]{TripState.ToPickup})=="예약","pickup taxi is reserved");Check((string)status.Invoke(null,new object[]{TripState.OnTrip})=="","occupied taxi has no status label");Check((string)status.Invoke(null,new object[]{TripState.Complete})=="빈차","completed trip is vacant");
  var timer=type.GetMethod("Deadline");Check((string)timer.Invoke(null,new object[]{446f})=="7:26","deadline is remaining minutes and seconds");Check((string)timer.Invoke(null,new object[]{0f})=="0:00","expired countdown stays zero");Check((string)timer.Invoke(null,new object[]{float.PositiveInfinity})=="—","unbounded time is not an invented deadline");
  Check((string)type.GetMethod("Destination").Invoke(null,new object[]{"은계 승하차 10"})=="목적지 (은계 승하차 10)","destination removes pickup/dropoff prefix");
  Debug.Log("COCKPIT_PRESENTATION_OK checks="+checks);
 }
}
