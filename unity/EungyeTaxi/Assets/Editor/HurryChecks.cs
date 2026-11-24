using System;
using System.IO;
using UnityEngine;

// Temptation "좀 빨리 가 주세요" (2026-11-24, Editor, no scene): defaults, when the passenger asks, targets and rewards,
// and that a speed-limit record forfeits the reward even on time.
public static class HurryChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("HURRY "+name);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  Check(HurryRules.Enabled&&HurryRules.PerShiftMax==1&&HurryRules.RushChance==60&&HurryRules.OtherChance==15&&HurryRules.MinDistance==600,"defaults: on, once a shift, 60% rush / 15% other, 600 m");
  Check(Mathf.Approximately(HurryRules.FastFactor,.9f)&&HurryRules.FastPercent==20&&Mathf.Approximately(HurryRules.SafeFactor,.95f)&&HurryRules.SafePercent==10&&HurryRules.ForfeitOnSpeeding,"defaults: 90%/20%, 95%/10%, speeding forfeits");
  Check(HurryRules.Blocked(true,0,0,false,900)=="","an ordinary 900 m ride may ask");
  Check(HurryRules.Blocked(true,1,0,false,900)!="","once a shift");
  Check(HurryRules.Blocked(true,0,MissionRules.Express,false,900)!=""&&HurryRules.Blocked(true,0,MissionRules.Green,false,900)!="","never on a mission call");
  Check(HurryRules.Blocked(true,0,0,true,900)!="","never for a drunk passenger");
  Check(HurryRules.Blocked(true,0,0,false,599)!="","not on a short ride");
  Check(HurryRules.Blocked(false,0,0,false,900)!="","Eungye single only");
  Check(HurryRules.Chance(true)==60&&HurryRules.Chance(false)==15,"rush calls ask more often");
  Check(HurryRules.RideSeconds(120,900)==120&&HurryRules.RideSeconds(0,800)==100,"estimate: safe route time, else distance at 8 m/s");
  Check(HurryRules.Target(HurryRules.Fast,120)==108&&HurryRules.Target(HurryRules.Safe,120)==114&&HurryRules.Target(HurryRules.Legal,120)==0,"targets 90% and 95% (rounded up)");
  Check(HurryRules.Reward(HurryRules.Fast,6000)==1200&&HurryRules.Reward(HurryRules.Safe,6000)==600&&HurryRules.Reward(HurryRules.Fast,5500)==1100&&HurryRules.Reward(HurryRules.Fast,8400)==1700&&HurryRules.Reward(HurryRules.Legal,6000)==0,"rewards 20% / 10% of the fare, up to 100 won");
  bool paid;
  Check(HurryRules.Outcome(HurryRules.Fast,100,108,false,out paid)=="목표 안 도착"&&paid,"on time, no speeding: paid");
  Check(HurryRules.Outcome(HurryRules.Fast,108,108,false,out paid)!=""&&paid,"exactly on the target counts");
  Check(HurryRules.Outcome(HurryRules.Fast,109,108,false,out paid)=="목표 시간 넘김"&&!paid,"over the target: not paid");
  Check(HurryRules.Outcome(HurryRules.Fast,80,108,true,out paid)=="과속으로 사례금 없음"&&!paid,"on time but a speeding record: not paid");
  Check(HurryRules.Outcome(HurryRules.Safe,80,114,true,out paid)!=""&&!paid,"the safe answer is forfeited by speeding too");
  Check(HurryRules.Outcome(HurryRules.Legal,10,0,false,out paid)==""&&!paid,"keeping to the limit has no settlement");
  Debug.Log("HURRY_CHECKS_OK checks="+checks);
 }
}
