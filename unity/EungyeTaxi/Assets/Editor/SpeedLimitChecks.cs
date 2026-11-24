using System;
using System.IO;
using UnityEngine;

// Eungye road speed limits (2026-11-24, Editor, no scene): table lookup, school zones, penalty tiers
// and the two copies of the data file.
public static class SpeedLimitChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("SPEEDLIMIT "+name);}
 static void UseEstimated(string profileText,bool on){
  var p=ContentJson.Object(ContentJson.Parse(profileText));
  foreach(object o in ContentJson.Array(p["values"])){var row=ContentJson.Object(o);if((string)row["id"]=="law.speed.estimatedRoads")row["value"]=on?"1":"0";}
  p["contentHash"]=ContentTuning.ComputeHash(p);ContentTuning.UseJson(ContentJson.Stringify(p));
 }
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
  string profile=File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json"));
  Check(RoadSpeedLimits.Enabled&&!RoadSpeedLimits.EstimatedRoads&&RoadSpeedLimits.DefaultKmh==50&&RoadSpeedLimits.AlleyKmh==30&&RoadSpeedLimits.ZoneRadius==60,"defaults: confirmed sections only, estimates kept for the switch");
  Check(RoadSpeedLimits.RoadLimit("수인로")==60&&RoadSpeedLimits.RoadLimit("대은로")==0,"confirmed roads: 수인로 60; 대은로 kept in the file but inactive (round 4)");
  // 2026-11-24 round 2 default: roads without a confirmed value carry no limit.
  Check(RoadSpeedLimits.RoadLimit("계수로")==0&&RoadSpeedLimits.RoadLimit("")==0&&RoadSpeedLimits.RoadLimit(null)==0&&RoadSpeedLimits.RoadLimit("은행로144번길")==0,"unconfirmed roads and alleys carry no limit by default");
  Check(RoadSpeedLimits.Limit("은계로",new Vector3(801,0,104))==30&&RoadSpeedLimits.Limit("은계로",new Vector3(801,0,400))==0,"a school zone still applies on an unlimited road, and ends at its radius");
  Check(RoadSpeedLimits.Limit("수인로",new Vector3(1500,0,900))==60,"a confirmed road keeps its value outside zones");
  UseEstimated(profile,true);
  Check(RoadSpeedLimits.EstimatedRoads,"switch on");
  Check(RoadSpeedLimits.RoadLimit("계수로")==50&&RoadSpeedLimits.RoadLimit("")==50&&RoadSpeedLimits.RoadLimit(null)==50,"with the switch, other roads and unnamed lanes take the estimate");
  Check(RoadSpeedLimits.RoadLimit("은행로144번길")==30&&RoadSpeedLimits.RoadLimit("대은로104번길")==30&&RoadSpeedLimits.RoadLimit("은계번영길")==50,"alley rule matches the suffix only");
  // School zone: 은계로 at 은빛초 (camera H0653) is 30 inside the radius and 50 outside it.
  Check(RoadSpeedLimits.Limit("은계로",new Vector3(801,0,104))==30&&RoadSpeedLimits.Limit("은계로",new Vector3(801,0,104+39))==30&&RoadSpeedLimits.Limit("은계로",new Vector3(801,0,104+45))==50,"zone applies inside its own 40 m reach and not beyond");
  Check(RoadSpeedLimits.Limit("은계로",new Vector3(801,0,400))==50,"zone ends outside its radius");
  RoadSpeedLimits.Configure(null);Check(RoadSpeedLimits.RoadLimit("수인로")==50&&RoadSpeedLimits.ZoneAt(801,104)==null,"a missing file falls back to the estimate without failing");
  RoadSpeedLimits.Configure(source);UseEstimated(profile,false);
  Check(RoadSpeedLimits.Limit("수인로",new Vector3(450,0,450))==60&&RoadSpeedLimits.Limit("수인로",new Vector3(782,0,384))==60,"no zone reaches the two 수인로 camera points");
  Check(RoadSpeedLimits.ZoneAt(329,1323).id=="RV-103"&&RoadSpeedLimits.ZoneAt(1500,900)==null,"nearest zone lookup");
  // Judgement numbers.
  Check(RoadSpeedLimits.Tolerance==10&&RoadSpeedLimits.Seconds==3&&RoadSpeedLimits.RepeatSeconds==20,"judgement defaults");
  Check(!RoadSpeedLimits.Over(60,50)&&RoadSpeedLimits.Over(60.5f,50)&&!RoadSpeedLimits.Over(200,0),"over means past limit plus tolerance; no limit means no judgement");
  Check(RoadSpeedLimits.PenaltyFor(65,50)==5&&RoadSpeedLimits.PenaltyFor(70,50)==5&&RoadSpeedLimits.PenaltyFor(70.5f,50)==10,"penalty tiers at +20 km/h");
  Check(Mathf.Approximately(RoadSpeedLimits.TrafficShare,.9f)&&Mathf.Approximately(60/3.6f*RoadSpeedLimits.TrafficShare,15f)&&Mathf.Approximately(30/3.6f*RoadSpeedLimits.TrafficShare,7.5f),"ordinary cars cruise at 90% of the limit");
  // Tolerance band (2026-11-24 round 2): no record inside +10, but a crash there costs more.
  Check(RoadSpeedLimits.CrashPenalty==5&&!RoadSpeedLimits.AboveLimit(50.9f,50)&&RoadSpeedLimits.AboveLimit(51,50)&&RoadSpeedLimits.AboveLimit(60,50)&&!RoadSpeedLimits.Over(60,50)&&!RoadSpeedLimits.AboveLimit(90,0),"above-limit band starts at +1 and stays below the record line");
  string why;var rear=new FaultInput{otherVehicle=true,ownSpeed=14,otherSpeed=17,contactAlong=-2,halfLength=2,ownAlong=14,otherAlong=17};
  Check(AccidentFault.Decide(rear,out why)==FaultSide.Other,"rear-ended at the limit stays the other side's fault");
  rear.aboveLimit=true;Check(AccidentFault.Decide(rear,out why)==FaultSide.Shared&&why.Contains("제한속도"),"rear-ended inside the tolerance band becomes shared");
  var cut=new FaultInput{otherVehicle=true,surprise=true,sinceSurprise=.5f,ownSpeed=14,otherSpeed=8,contactAlong=2,halfLength=2};
  Check(AccidentFault.Decide(cut,out why)==FaultSide.Other,"surprise with no time to react, at the limit: other side");
  cut.aboveLimit=true;Check(AccidentFault.Decide(cut,out why)==FaultSide.Shared,"same surprise inside the tolerance band: shared");
  var own=new FaultInput{otherVehicle=true,ownSpeed=14,otherSpeed=0,contactAlong=2,halfLength=2,aboveLimit=true};
  Check(AccidentFault.Decide(own,out why)==FaultSide.Own,"own fault stays own (the extra points come from the law record)");
  Debug.Log("SPEEDLIMIT_OK checks="+checks);
 }
}
