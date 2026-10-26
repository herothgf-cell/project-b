using System;

// Fictional operating districts, not real-world demand or official land use.
public static class PassengerDemandPolicy {
 public static string Region(string stopId){switch(stopId){
  case "EG-GAME-03":case "EG-GAME-04":case "EG-GAME-06":case "EG-GAME-08":case "EG-GAME-10":return "residential";
  case "EG-GAME-01":case "EG-GAME-05":case "EG-GAME-09":case "EG-GAME-11":return "office";
  case "EG-GAME-02":case "EG-GAME-07":case "EG-GAME-12":return "commercial";
  default:return "neutral";
 }}
 public static int Phase(double elapsed){return elapsed<300?0:elapsed<900?1:2;}
 public static int Weight(string region,double elapsed){return region==(Phase(elapsed)==0?"residential":Phase(elapsed)==1?"office":"commercial")?3:1;}
 public static string Label(string region){return region=="residential"?"주거":region=="office"?"업무":region=="commercial"?"상업":"중립";}
 public static string Description(double elapsed){string region=Phase(elapsed)==0?"residential":Phase(elapsed)==1?"office":"commercial";return "게임 상대 수요 · "+(Phase(elapsed)==0?"초반":Phase(elapsed)==1?"중반":"후반")+" "+Label(region)+" 3배 비중 / 나머지 1배 · 수입 보장·할증 없음";}
 public static double Priority(string seed,string stopId,double elapsed){
  uint h=2166136261;foreach(char c in (seed??"")+":demand:"+stopId){h^=c;h*=16777619;}h^=h>>16;h*=2246822519;h^=h>>13;
  double u=(h+1.0)/(uint.MaxValue+2.0);return -Math.Log(u)/Weight(Region(stopId),elapsed);
 }
}
