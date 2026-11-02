using System;

// Fictional operating districts, not real-world demand, land use or fare multipliers.
public static class PassengerDemandPolicy {
 public static string Region(string stopId){switch(stopId){
  case "EG-GAME-03":case "EG-GAME-04":case "EG-GAME-06":case "EG-GAME-08":case "EG-GAME-10":return "residential";
  case "EG-GAME-01":case "EG-GAME-05":case "EG-GAME-09":case "EG-GAME-11":return "office";
  case "EG-GAME-02":case "EG-GAME-07":case "EG-GAME-12":return "commercial";
  default:return "neutral";
 }}
 // The campaign HUD maps one operating day to 08:00–20:00. Overtime is even,
 // rather than cycling morning demand when the display crosses midnight.
 public static int Phase(double elapsed,double duration,bool pvp){
  if(double.IsNaN(elapsed)||double.IsInfinity(elapsed)||double.IsNaN(duration)||double.IsInfinity(duration)||duration<=0)return 3;
  double ratio=Math.Max(0,elapsed)/duration;
  if(ratio>=1)return 3;
  return pvp?(ratio<1.0/3?0:ratio<2.0/3?1:2):(ratio<.25?0:ratio<.75?1:2);
 }
 public static int Phase(double elapsed){return Phase(elapsed,1200,false);}
 public static int Weight(string region,int phase,bool destination){
  if(region=="neutral"||phase<0||phase>2)return 1;
  string preferred=destination?(phase==0?"office":phase==1?"commercial":"residential"):(phase==0?"residential":phase==1?"office":"commercial");
  return region==preferred?3:1;
 }
 public static int Weight(string region,double elapsed){return Weight(region,Phase(elapsed),false);}
 public static string Label(string region){return region=="residential"?"주거":region=="office"?"업무":region=="commercial"?"상업":"중립";}
 public static string Description(int phase,bool pvp){
  if(phase<0||phase>2)return "초과 영업 · 전 지역 고른 공급 · 운임 할증 없음";
  string from=phase==0?"residential":phase==1?"office":"commercial",to=phase==0?"office":phase==1?"commercial":"residential";
  return "게임 상대 수요 · "+(pvp?(phase==0?"초반":phase==1?"중반":"후반"):(phase==0?"아침":phase==1?"낮":"저녁"))+" "+Label(from)+" 출발 → "+Label(to)+" 도착 각 3배 비중 · 운임 할증 없음";
 }
 public static string Description(double elapsed){return Description(Phase(elapsed),false);}
 static double Draw(string seed,string key){
  uint h=2166136261;foreach(char c in (seed??"")+":demand:"+key){h^=c;h*=16777619;}h^=h>>16;h*=2246822519;h^=h>>13;
  double u=(h+1.0)/(uint.MaxValue+2.0);return -Math.Log(u);
 }
 public static double Priority(string seed,string stopId,double elapsed){return Draw(seed,stopId)/Weight(Region(stopId),elapsed);}
 public static double OfferPriority(string seed,string pickupStopId,string dropStopId,int phase){return OfferRegionPriority(seed,Region(pickupStopId),Region(dropStopId),pickupStopId+">"+dropStopId,phase);}
 public static double OfferRegionPriority(string seed,string pickupRegion,string dropRegion,string identity,int phase){return Draw(seed,identity)/(Weight(pickupRegion,phase,false)*Weight(dropRegion,phase,true));}
}
