using System;
using UnityEngine;
public static class PedestrianDamageChecks {
 static int count;static void Check(bool ok,string reason){count++;if(!ok)throw new Exception("PEDESTRIAN_DAMAGE "+reason);}
 public static void Run(){
  count=0;var state=new AccidentState();state.BeginResolved("editor-person","waiting:0",AccidentSeverityPolicy.Resolve(AccidentKind.Person,AccidentSeverity.Major));Check(state.SelectPedestrianDamage(999),"select max");
  var car=new VehicleDamage();Check(state.ApplyVehicleDamage(car)==10&&car.Durability==90,"apply once");
  var restored=JsonUtility.FromJson<AccidentState>(JsonUtility.ToJson(state));restored.Validate();var vehicle=VehicleDamage.Restore(JsonUtility.FromJson<VehicleDamageSnapshot>(JsonUtility.ToJson(car.Snapshot())));
  Check(restored.DurabilityLoss==10&&restored.appliedDamage==10&&restored.damageApplied,"JSON retains selected and applied amounts");Check(restored.ApplyVehicleDamage(vehicle)==0&&vehicle.Durability==90,"JSON restore cannot repeat loss");
  var replay=new AccidentState();replay.BeginResolved(state.eventId,"waiting:0",AccidentSeverityPolicy.Resolve(AccidentKind.Person,AccidentSeverity.Major));replay.SelectPedestrianDamage(999);Check(replay.ApplyVehicleDamage(vehicle)==0&&vehicle.Durability==90,"vehicle JSON event IDs independently deduplicate");
  var old=JsonUtility.FromJson<AccidentState>("{\"eventId\":\"old\",\"targetId\":\"waiting:0\",\"phase\":2,\"contactLatched\":true,\"remaining\":45,\"chargeApplied\":true,\"outcomeApplied\":true}");old.Validate();Check(old.ApplyVehicleDamage(vehicle)==0&&vehicle.Durability==90,"pre-severity save no retroactive loss");
  var oldVehicle=VehicleDamage.Restore(JsonUtility.FromJson<VehicleDamageSnapshot>("{\"version\":1,\"durability\":100}"));Check(oldVehicle.Durability==100,"legacy vehicle without accident IDs restores");
  Check(AccidentPresentation.DurabilityText(state)=="−10","popup actual loss");state.appliedDamage=0;Check(AccidentPresentation.DurabilityText(state)=="변동 없음","zero actual loss");
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080)}){var box=AccidentPresentation.DamageBounds(size.x,size.y);Check(box.x>=0&&box.y>=0&&box.xMax<=size.x&&box.yMax<=size.y,"five row card fits");}
  Check(PvpNativeLan.Protocol=="native-city-lan-v8","network contract version");Debug.Log("PEDESTRIAN_DAMAGE_EDITOR_OK checks="+count);
 }
 public static void Build(){Run();AccidentSeverityChecks.Run();AccidentUiRevisionChecks.Run();ContentTuningBuild.Build();}
}
