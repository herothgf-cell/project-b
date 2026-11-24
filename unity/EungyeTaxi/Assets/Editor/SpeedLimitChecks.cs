using System;
using System.IO;
using UnityEngine;

// Eungye road speed limits (2026-11-24, Editor, no scene): table lookup, school zones, penalty tiers
// and the two copies of the data file.
public static class SpeedLimitChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("SPEEDLIMIT "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  string source=File.ReadAllText(Path.GetFullPath("../../config/maps/eungye-speed-limits.json")),resource=File.ReadAllText("Assets/Resources/eungye-speed-limits.json");
  Check(source==resource,"config and Resources copies are identical");
  RoadSpeedLimits.Configure(source);var data=RoadSpeedLimits.Data;
  Check(data!=null&&data.mapId=="eungye"&&data.roads.Length==2&&data.zones.Length==13,"file holds two confirmed roads and thirteen zone points");
  foreach(var r in data.roads)Check(r.evidence.StartsWith("confirmed"),"road value carries its evidence: "+r.name);
  foreach(var z in data.zones)Check(z.kmh==30&&z.evidence.StartsWith("confirmed-point")&&z.x>0&&z.x<2100&&z.z>0&&z.z<1750,"zone is a confirmed 30 point inside the map: "+z.id);
  Check(RoadSpeedLimits.Enabled&&RoadSpeedLimits.DefaultKmh==50&&RoadSpeedLimits.AlleyKmh==30&&RoadSpeedLimits.ZoneRadius==60,"estimate defaults");
  Check(RoadSpeedLimits.RoadLimit("수인로")==60&&RoadSpeedLimits.RoadLimit("대은로")==50,"confirmed roads");
  Check(RoadSpeedLimits.RoadLimit("계수로")==50&&RoadSpeedLimits.RoadLimit("")==50&&RoadSpeedLimits.RoadLimit(null)==50,"other roads and unnamed lanes take the estimate");
  Check(RoadSpeedLimits.RoadLimit("은행로144번길")==30&&RoadSpeedLimits.RoadLimit("대은로104번길")==30&&RoadSpeedLimits.RoadLimit("은계번영길")==50,"alley rule matches the suffix only");
  // School zone: 은계로 at 은빛초 (camera H0653) is 30 inside the radius and 50 outside it.
  Check(RoadSpeedLimits.Limit("은계로",new Vector3(801,0,104))==30&&RoadSpeedLimits.Limit("은계로",new Vector3(801,0,104+59))==30,"zone applies inside its radius");
  Check(RoadSpeedLimits.Limit("은계로",new Vector3(801,0,400))==50,"zone ends outside its radius");
  Check(RoadSpeedLimits.Limit("수인로",new Vector3(450,0,450))==60&&RoadSpeedLimits.Limit("수인로",new Vector3(782,0,384))==60,"no zone reaches the two 수인로 camera points");
  Check(RoadSpeedLimits.ZoneAt(329,1323).id=="RV-103"&&RoadSpeedLimits.ZoneAt(1500,900)==null,"nearest zone lookup");
  // Judgement numbers.
  Check(RoadSpeedLimits.Tolerance==10&&RoadSpeedLimits.Seconds==3&&RoadSpeedLimits.RepeatSeconds==20,"judgement defaults");
  Check(!RoadSpeedLimits.Over(60,50)&&RoadSpeedLimits.Over(60.5f,50)&&!RoadSpeedLimits.Over(200,0),"over means past limit plus tolerance; no limit means no judgement");
  Check(RoadSpeedLimits.PenaltyFor(65,50)==5&&RoadSpeedLimits.PenaltyFor(70,50)==5&&RoadSpeedLimits.PenaltyFor(70.5f,50)==10,"penalty tiers at +20 km/h");
  Check(Mathf.Approximately(RoadSpeedLimits.TrafficShare,.9f)&&Mathf.Approximately(60/3.6f*RoadSpeedLimits.TrafficShare,15f)&&Mathf.Approximately(30/3.6f*RoadSpeedLimits.TrafficShare,7.5f),"ordinary cars cruise at 90% of the limit");
  RoadSpeedLimits.Configure(null);Check(RoadSpeedLimits.RoadLimit("수인로")==50&&RoadSpeedLimits.ZoneAt(801,104)==null,"a missing file falls back to the estimate without failing");
  RoadSpeedLimits.Configure(source);
  Debug.Log("SPEEDLIMIT_OK checks="+checks);
 }
}
