using System;
// Fictional city districts; never used by the campaign demand table.
public static class PvpPassengerPolicy {
 public static int Phase(double elapsed,double duration){double ratio=duration>0?elapsed/duration:0;return ratio<.25?0:ratio<.75?1:2;}
 public static int Region(int pickup){int district=pickup/2;return district%3==0?0:district==4||district>=7?1:2;}
 public static string RegionLabel(int pickup){return new[]{"주거","업무·공공","상업·터미널"}[Region(pickup)];}
 public static int Weight(int pickup,double elapsed,double duration){return Region(pickup)==Phase(elapsed,duration)?3:1;}
 public static int Pickup(int serial,int count,double elapsed,double duration){int total=0;for(int i=0;i<count;i++)total+=Weight(i,elapsed,duration);if(total==0)return -1;uint hash=DrunkRideState.Hash("pvp-demand:"+serial);int roll=(int)(hash%(uint)total);for(int i=0;i<count;i++){roll-=Weight(i,elapsed,duration);if(roll<0)return i;}return count-1;}
 public static int BehaviorChoice(PassengerType type,bool followup,int cleanliness,double remaining){
  if(followup)return 1;
  if(type==PassengerType.Food)return cleanliness>=45?1:0;
  if(type==PassengerType.Luggage)return remaining>=30?0:1;
  if(type==PassengerType.MuddyLuggage)return remaining>=30?0:2;
  return type==PassengerType.Talkative?0:1;
 }
 public static double OfferSeconds(double pickup,JobOffer offer){
  if(offer==null||pickup<0||double.IsNaN(pickup)||double.IsInfinity(pickup))return double.PositiveInfinity;
  double ride=offer.timingPolicyVersion>=1?offer.safeRouteSeconds:offer.distance/8;
  double total=pickup/6+ride+Math.Max(0,offer.waitSeconds)+30;
  return total<0||double.IsNaN(total)||double.IsInfinity(total)?double.PositiveInfinity:total;
 }
 public static double OfferRank(double pickup,JobOffer offer,DriverCondition driver,double remaining){
  double total=OfferSeconds(pickup,offer);if(double.IsInfinity(total)||total>=remaining||driver==null)return double.PositiveInfinity;
  if(PvpConditionRules.Forecast(driver,pickup/6).fatigue>=100||PvpConditionRules.Forecast(driver,total).PassengerMustLeave)return double.PositiveInfinity;
  return total;
 }
 public static bool PursueRoadside(double distance){return distance>=0&&distance<=250;}
 public static bool AcceptRoute(double extraSeconds,double remaining,double raceRemaining,double baseEta=0,DriverCondition driver=null){return baseEta>=0&&baseEta+extraSeconds+30<raceRemaining&&extraSeconds>=0&&!double.IsInfinity(extraSeconds)&&!double.IsNaN(extraSeconds)&&extraSeconds+30<remaining&&extraSeconds+30<raceRemaining&&(driver==null||!PvpConditionRules.Forecast(driver,baseEta+extraSeconds).PassengerMustLeave);}
}
