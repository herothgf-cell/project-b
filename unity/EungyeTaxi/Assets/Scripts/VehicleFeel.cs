using UnityEngine;

// 08 chapter 5 "차마다 다른 손맛" (2026-11-24, Eungye single). Each car has four feel numbers:
//  steerResponse  how fast the wheel follows the input of the driver (1 = standard)
//  lean, dip      visible body roll/pitch and braking camera dip (1 = standard, visual only)
//  brakeRamp      seconds for service braking to build (0 = instant, the standard)
// The starter keeps the standard physics (1 and 0), so the first days and every recorded baseline are
// unchanged; PVP never calls Apply with an owned car and always drives the standard taxi.
public static class VehicleFeel {
 static float F(string model,string key,double fallback){return (float)ContentTuning.Number("feel.vehicle."+model+"."+key,fallback);}
 static double Default(string model,string key){
  bool hybrid=model==VehicleCatalog.Hybrid,lpg=model==VehicleCatalog.WideLpg;
  if(key=="steerResponse")return hybrid?1.25:lpg?.85:1;
  if(key=="lean")return hybrid?.8:lpg?1.1:1.25;
  if(key=="dip")return hybrid?.8:lpg?1.1:1.2;
  return lpg?.2:0;
 }
 public static float SteerResponse(string model){return Mathf.Clamp(F(model,"steerResponse",Default(model,"steerResponse")),.5f,2f);}
 public static float Lean(string model){return Mathf.Clamp(F(model,"lean",Default(model,"lean")),0,2f);}
 public static float Dip(string model){return Mathf.Clamp(F(model,"dip",Default(model,"dip")),0,2f);}
 public static float BrakeRamp(string model){return Mathf.Clamp(F(model,"brakeRamp",Default(model,"brakeRamp")),0,1f);}
 // model null = the standard taxi (PVP, practice).
 public static void Apply(TaxiController taxi,string model){
  if(!taxi)return;bool standard=string.IsNullOrEmpty(model)||VehicleCatalog.Get(model)==null;
  taxi.VehicleSteerResponse=standard?1:SteerResponse(model);taxi.VehicleLean=standard?1:Lean(model);
  taxi.VehicleDip=standard?1:Dip(model);taxi.VehicleBrakeRamp=standard?0:BrakeRamp(model);
 }
 public static string Describe(string model){
  float s=SteerResponse(model),l=Lean(model),b=BrakeRamp(model);
  return "손맛: "+(s>1.05f?"핸들 반응 빠름":s<.95f?"핸들 반응 묵직함":"핸들 반응 보통")+" · "+(l>1.05f?"차체가 많이 기움":l<.95f?"차체가 단단함":"차체 기울기 보통")+" · "+(b>.05f?"브레이크가 부드럽게 듦":"브레이크가 바로 듦");
 }
}
