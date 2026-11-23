using UnityEngine;

// 08 chapter 2 "living meter" (2026-11-24, Eungye single): design switch only. The meter fare (base + driven
// distance + slow time) is always recorded on the trip, but settlement uses it only when the content switch
// meter.enabled is 1 (default 0 keeps the agreed-fare economy). With the switch on, the existing rules stay as
// they are: performance (driving score, satisfaction bonus) applies on top, late arrival still pays 0 and a
// meter left off still pays half. Other handling options are in the work record for the owner to decide.
public static class MeterRules {
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 public static bool Enabled {get{return ContentTuning.Int("meter.enabled",0)!=0;}}
 public static int BaseFare {get{return ContentTuning.Int("meter.baseFare",3000);}}
 public static float BaseMetres {get{return F("meter.baseMetres",0);}}
 public static float StepMetres {get{return Mathf.Max(1,F("meter.stepMetres",25));}}
 public static int StepFare {get{return ContentTuning.Int("meter.stepFare",100);}}
 public static float SlowKmh {get{return F("meter.slowKmh",15);}}
 public static float SlowSeconds {get{return Mathf.Max(1,F("meter.slowSeconds",30));}}
 public static int SlowFare {get{return ContentTuning.Int("meter.slowFare",100);}}
 public static int Fare(float metres,float slowSeconds){
  return Mathf.Max(0,BaseFare)+Mathf.CeilToInt(Mathf.Max(0,metres-BaseMetres)/StepMetres)*Mathf.Max(0,StepFare)+Mathf.FloorToInt(Mathf.Max(0,slowSeconds)/SlowSeconds)*Mathf.Max(0,SlowFare);
 }
}

public partial class PassengerJobs {
 Vector3 meterLast;bool meterHasLast;
 // Distance and slow time with the passenger aboard, kept on the saved trip.
 void TickMeter(float seconds){
  if(!CallBoardSingle||current==null||trip.State!=TripState.OnTrip||trip.ArrivalLocked||!session.taxi||PvpNativeLan.IsReplica(session)){meterHasLast=false;return;}
  var at=session.taxi.transform.position;at.y=0;
  if(meterHasLast){float step=Vector3.Distance(at,meterLast);if(step<20)current.meterMetres+=step;}
  meterLast=at;meterHasLast=true;
  if(session.taxi.body.velocity.magnitude*3.6f<=MeterRules.SlowKmh)current.meterSlowSeconds+=seconds;
 }
 public int MeterFare {get{return current==null?0:MeterRules.Fare(current.meterMetres,current.meterSlowSeconds);}}
 // The fare settlement starts from: the agreed fare, or the meter when the switch is on (single only).
 int SettlementFare(){return MeterRules.Enabled&&CallBoardSingle&&current!=null?MeterFare:trip.Fare;}
}
