using System;
using UnityEngine;
public static class PvpNativeLanTests {
 static int checks;static void Check(bool value,string name){checks++;if(!value)throw new Exception("Native LAN contract: "+name);}
 public static void Run(){
  PvpPassengerParityTests.Run();checks=0;
  var ledger=new EconomyLedger(PrototypeSettings.ForOneDay());Check(ledger.EnablePvpProfile(),"host PVP profile");
  var full=new PvpNativeFrame{full=true,own=new PvpNativeOwn{economy=ledger.Snapshot(),account=ledger.Account.Snapshot(),pvpMeals=2,pvpRests=1}};
  var restored=JsonUtility.FromJson<PvpNativeFrame>(JsonUtility.ToJson(full));
  Check(restored.full&&restored.own.account.settings.oneDay,"full presence survives Unity JSON");
  var view=EconomyLedger.RestoreCampaign(restored.own.economy,restored.own.account);
  Check(view.EnablePvpProfile(),"owner profile restores");view.ApplyNetworkPvpCounters(restored.own.pvpMeals,restored.own.pvpRests);
  Check(view.PvpMeals==2&&view.PvpRests==1,"runtime service counters restore");
  var sparse=JsonUtility.FromJson<PvpNativeFrame>(JsonUtility.ToJson(new PvpNativeFrame{full=false}));
  Check(!sparse.full,"sparse frame cannot apply inline default owner object");
  var frame=JsonUtility.FromJson<PvpNativeFrame>(JsonUtility.ToJson(new PvpNativeFrame{offers=new JobOffer[8],roadside=new JobOffer[3]}));
  var board=new PvpSimulationCallBoard(new PvpSimulationLedger(),i=>null,18,3);board.ApplyNetworkView(frame.offers,frame.roadside);
  foreach(var offer in board.Offers)Check(offer==null,"empty call slot remains empty");foreach(var offer in board.Roadside)Check(offer==null,"empty roadside slot remains empty");
  Check(!PvpNativeLan.IsReplica(null)&&!PvpNativeLan.UiBlocked(null),"offline null-safe hooks");
  Debug.Log("PVP_NATIVE_LAN_EDITOR_OK checks="+checks);
 }
 public static void BuildValidated(){Run();PrototypeBuild.BuildHybrid();}
}
