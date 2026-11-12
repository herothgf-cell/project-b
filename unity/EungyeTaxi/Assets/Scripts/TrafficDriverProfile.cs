using System;
public enum TrafficTemperament { Calm, Normal, Impatient }
// All profiles share the same physical collision, crossing and signal gates.
// The impatient profile uses the existing maximum cruise, never a higher cap.
public sealed class TrafficDriverProfile {
 public TrafficTemperament Kind;public float CruiseScale,Acceleration,Headway,LaneChangeCooldown,ExtraGap;
 public string Label {get{return Kind==TrafficTemperament.Calm?"여유":Kind==TrafficTemperament.Impatient?"성급":"보통";}}
 public static TrafficDriverProfile ForIndex(int index,bool eungyeSingle=false){
  int kind=(index%3+3)%3;
  if(eungyeSingle)return new TrafficDriverProfile{Kind=(TrafficTemperament)kind,CruiseScale=kind==0?.78f:kind==1?.93f:1,Acceleration=kind==0?1.8f:kind==1?2.6f:3.2f,Headway=kind==0?1.55f:kind==1?1.15f:.95f,LaneChangeCooldown=kind==0?12:kind==1?8:5,ExtraGap=kind==0?1.8f:kind==1?.8f:.4f};
  return new TrafficDriverProfile{Kind=(TrafficTemperament)kind,CruiseScale=kind==0?.8f:kind==1?.94f:1,Acceleration=kind==0?1.5f:kind==1?2.2f:2.6f,Headway=kind==0?1.7f:kind==1?1.3f:1.05f,LaneChangeCooldown=kind==0?12:kind==1?8:5,ExtraGap=kind==0?1.8f:kind==1?.8f:.4f};
 }
 public float LimitSpeed(float gap,float cruise,bool followingBody=false){
  float available=Math.Max(0,gap-(followingBody?ExtraGap:0));
  return Math.Min(TrafficRules.SafeSpeed(available,Math.Max(0,cruise)*CruiseScale),available/Headway);
 }
 public bool LaneChangeReady(double now,double completedAt){return now-completedAt>=LaneChangeCooldown;}
}
