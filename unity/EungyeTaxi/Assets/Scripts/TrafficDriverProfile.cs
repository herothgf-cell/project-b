using System;
public enum TrafficTemperament { Calm, Normal, Impatient }
// All profiles share the same physical collision, crossing and signal gates.
// The impatient profile uses the existing maximum cruise, never a higher cap.
public sealed class TrafficDriverProfile {
 public TrafficTemperament Kind;public float CruiseScale,Acceleration,Headway,LaneChangeCooldown,ExtraGap;
 public string Label {get{return Kind==TrafficTemperament.Calm?"여유":Kind==TrafficTemperament.Impatient?"성급":"보통";}}
 public static TrafficDriverProfile ForIndex(int index,bool eungyeSingle=false){
  int kind=(index%3+3)%3;
  return new TrafficDriverProfile{
   Kind=(TrafficTemperament)kind,
   CruiseScale=kind==0?.86f:kind==1?.96f:1,
   Acceleration=eungyeSingle?(kind==0?2.4f:kind==1?3.1f:3.7f):(kind==0?2f:kind==1?2.7f:3.2f),
   Headway=kind==0?1.4f:kind==1?1.05f:.85f,
   LaneChangeCooldown=kind==0?12:kind==1?8:5,
   ExtraGap=kind==0?1.4f:kind==1?.7f:.35f
  };
 }
 public float LimitSpeed(float gap,float cruise,bool followingBody=false,float leaderSpeed=0){
  float available=Math.Max(0,gap-(followingBody?ExtraGap:0));
  float following=available/Headway+(followingBody?.6f*Math.Max(0,leaderSpeed):0);
  return Math.Min(TrafficRules.SafeSpeed(available,Math.Max(0,cruise)*CruiseScale),following);
 }
 // Centre-to-centre rear clearance at the planned merge point. Drivers accept
 // different closing gaps, but a car alongside always has priority.
 public bool YieldsToRear(float distanceBehind,float rearSpeed,float ownSpeed){
  float closing=Math.Max(0,rearSpeed-ownSpeed);
  float seconds=Kind==TrafficTemperament.Calm?3f:Kind==TrafficTemperament.Normal?2.2f:1.5f;
  return distanceBehind<=6f+closing*seconds;
 }
 public bool LaneChangeReady(double now,double completedAt){return now-completedAt>=LaneChangeCooldown;}
}
