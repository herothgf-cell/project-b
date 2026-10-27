using System;
using UnityEngine;
public static class ProgressiveFatiguePersistenceTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("PROGRESSIVE_FATIGUE: "+why);}
 public static void Run(){
  checks=0;var ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());ledger.Driver.fatigue=100;ledger.Driver.drowsiness.Advance(119.5,true,100);ledger.Care.Advance(119.5);
  var saved=JsonUtility.FromJson<EconomySnapshot>(JsonUtility.ToJson(ledger.Snapshot()));var account=JsonUtility.FromJson<AccountSnapshot>(JsonUtility.ToJson(ledger.Account.Snapshot()));var restored=EconomyLedger.RestoreCampaign(saved,account);
  Check(restored.Driver.drowsiness.Tunnel&&!restored.Driver.drowsiness.PassengerMustLeave,"mid-progress JSON preserves vision and pre-cancellation threshold");
  restored.Driver.drowsiness.Advance(.5,true,100);restored.Care.Advance(.5);Check(restored.Driver.drowsiness.PassengerMustLeave&&!restored.CanAccept,"restored threshold blocks new boarding");Check(restored.Care.Cleanliness==99,"fractional dirt survives shipping JSON");
  restored.Driver.drowsiness.Advance(60,true,100);Check(restored.Driver.drowsiness.Blackout,"remaining exposure reaches full loss");Check(!restored.UseSleep("bypass"),"blackout cannot bypass emergency recovery via normal sleep");Check(restored.UseEmergencySleep("home"),"blackout permits explicit home rest");Check(restored.Driver.fatigue==80&&restored.Elapsed==100&&restored.Day==1&&restored.Operating,"home rest keeps day and applies weaker recovery");
  var after=EconomyLedger.RestoreCampaign(JsonUtility.FromJson<EconomySnapshot>(JsonUtility.ToJson(restored.Snapshot())),restored.Account.Snapshot());Check(!after.UseEmergencySleep("home")&&after.Driver.drowsiness.visionSeconds==0,"saved home recovery cannot replay");
  var legacy=JsonUtility.FromJson<DriverCondition>("{\"satiety\":80,\"fatigue\":100,\"drowsiness\":{\"checkSeconds\":0,\"remaining\":3,\"phase\":2,\"effect\":0,\"randomState\":173}}");legacy.Validate();Check(legacy.drowsiness.visionSeconds==0&&!legacy.drowsiness.Blackout,"legacy episodic fields remain valid and start new progressive exposure at zero");legacy.drowsiness.Advance(60,true,100);Check(legacy.drowsiness.Tunnel,"legacy save adopts progressive policy");
  var result=JsonUtility.FromJson<PassengerResult>(JsonUtility.ToJson(new PassengerResult{cancelledForFatigue=true,paid=0,farePolicyVersion=4}));Check(result.cancelledForFatigue&&RideReceiptSummary.PaymentStatus(result).Contains("운송 취소")&&!RideReceiptSummary.Rule(result).Contains("시간 초과"),"cancellation receipt survives JSON without timeout mislabel");
  var oldCare=VehicleCare.Restore(JsonUtility.FromJson<VehicleCareSnapshot>("{\"cleanliness\":85,\"day\":1}"));oldCare.Advance(120);Check(oldCare.Cleanliness==84,"legacy care missing remainder defaults to zero");
  Debug.Log("PROGRESSIVE_FATIGUE_PERSISTENCE_TESTS_OK checks="+checks);
 }
}
