using System;
using System.IO;
using UnityEngine;

// Accident fault rule (2026-11-24, Editor, no scene): the decision rows, the shared-fault arithmetic,
// and that saves written before the fault fields load as the driver's own fault.
public static class AccidentFaultChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("ACCIDENTFAULT "+name);}
 static FaultSide Side(FaultInput i){string why;return AccidentFault.Decide(i,out why);}
 public static void Run(){
  checks=0;
  ContentTuning.ConfigureCatalog(File.ReadAllText(Path.GetFullPath("../../config/content/catalog.json")));
  ContentTuning.ValidateJson(File.ReadAllText(Path.GetFullPath("../../config/content/default-profile.json")));
  Check(AccidentFault.Enabled&&AccidentFault.StoppedKmh==2&&Mathf.Approximately(AccidentFault.ReactionSeconds,1.2f)&&AccidentFault.SharedPercent==50&&AccidentFault.ViolationSeconds==3,"defaults");
  var hit=new FaultInput{otherVehicle=true,ownSpeed=8,otherSpeed=0,contactAlong=1.9f,halfLength=2,ownAlong=8,otherAlong=0};
  Check(Side(hit)==FaultSide.Own,"driving into a standing car is the driver's own fault");
  var wall=hit;wall.otherVehicle=false;wall.ownSpeed=0;wall.otherSpeed=3;Check(Side(wall)==FaultSide.Own,"a non-vehicle contact is always own fault");
  var parked=new FaultInput{otherVehicle=true,ownSpeed=.3f,otherSpeed=4,contactAlong=0,halfLength=2,otherAlong=0};
  Check(Side(parked)==FaultSide.Other,"standing still and struck by a moving car: the other car's fault");
  var slow=parked;slow.ownSpeed=.6f;Check(Side(slow)==FaultSide.Own,"2.16 km/h is no longer standing (limit 2 km/h)");
  var both=parked;both.otherSpeed=.2f;Check(Side(both)==FaultSide.Own,"two near-stationary cars touching is not the other car's fault");
  var wrong=parked;wrong.violating=true;Check(Side(wrong)==FaultSide.Own,"standing on the wrong side or just after a violation stays own fault");
  var rear=new FaultInput{otherVehicle=true,ownSpeed=6,otherSpeed=10,contactAlong=-1.9f,halfLength=2,ownAlong=6,otherAlong=10};
  Check(Side(rear)==FaultSide.Other,"struck from behind by a faster car: the other car's fault");
  var side=rear;side.contactAlong=-.5f;Check(Side(side)==FaultSide.Own,"a contact ahead of the rear third is not a rear-end hit");
  var reversing=rear;reversing.ownAlong=-3;reversing.otherAlong=-3.2f;Check(Side(reversing)==FaultSide.Own,"backing into a car is own fault");
  var cut=new FaultInput{otherVehicle=true,surprise=true,sinceSurprise=.8f,ownSpeed=11,otherSpeed=9,contactAlong=1.9f,halfLength=2,ownAlong=11,otherAlong=9};
  Check(Side(cut)==FaultSide.Other,"a surprise inside the reaction time: the other car's fault");
  var late=cut;late.sinceSurprise=1.3f;Check(Side(late)==FaultSide.Shared,"a surprise after the reaction time: shared");
  var fast=cut;fast.speeding=true;Check(Side(fast)==FaultSide.Shared,"a surprise while speeding: shared");
  var breach=cut;breach.violating=true;Check(Side(breach)==FaultSide.Own,"a violation outranks the surprise rows");
  // Shared arithmetic.
  Check(AccidentFault.SharedFee(1000,50)==500&&AccidentFault.SharedFee(3000,50)==1500&&AccidentFault.SharedFee(6000,50)==3000&&AccidentFault.SharedFee(1000,25)==300&&AccidentFault.SharedFee(1000,0)==100,"shared fee is halved, rounded up to 100 won, never below 100");
  Check(AccidentFault.SharedPenalty(3,50)==2&&AccidentFault.SharedPenalty(8,50)==4&&AccidentFault.SharedPenalty(15,50)==8&&AccidentFault.SharedPenalty(0,50)==0,"shared score penalty is halved up");
  // State: fee and penalty follow the fault; the stored selection stays the policy value.
  var state=new AccidentState();var moderate=AccidentSeverityPolicy.Resolve(AccidentKind.Vehicle,AccidentSeverity.Moderate);
  Check(state.BeginResolved("e1","collision:1",moderate)&&state.Fee==3000&&state.ScorePenalty==moderate.ScorePenalty&&!state.Shared,"own-fault event keeps the full fee");
  Check(state.SetShared(50)&&state.Shared&&state.Fee==1500&&state.ScorePenalty==AccidentFault.SharedPenalty(moderate.ScorePenalty,50)&&state.selectedFee==3000,"shared event halves fee and penalty");
  state.Validate();var copy=JsonUtility.FromJson<AccidentState>(JsonUtility.ToJson(state));copy.Validate();Check(copy.Shared&&copy.Fee==1500&&copy.faultSharePercent==50,"shared fault round-trips in saves");
  state.chargeApplied=true;Check(!state.SetShared(10)&&state.faultSharePercent==50,"the share cannot change after the fee is charged");
  // A save from before the fields existed.
  var plain=new AccidentState();plain.BeginResolved("e2","collision:2",moderate);string json=System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(plain),"\"fault[A-Za-z]*\":[0-9]+,?","").Replace(",}","}");
  Check(!json.Contains("fault"),"test save has no fault fields: "+json);var old=JsonUtility.FromJson<AccidentState>(json);old.Validate();Check(!old.Shared&&old.fault==0&&old.Fee==3000,"older saves load as own fault with the full fee");
  var bad=JsonUtility.FromJson<AccidentState>(JsonUtility.ToJson(plain));bad.fault=2;bool threw=false;try{bad.Validate();}catch(ArgumentException){threw=true;}Check(threw,"an unknown fault value is rejected on load");
  var empty=new AccidentState();empty.Validate();Check(!empty.Shared,"empty state is valid");
  Check(AccidentFault.Label(FaultSide.Other)=="상대 과실"&&AccidentFault.Label(FaultSide.Shared)=="쌍방 과실"&&AccidentFault.Label(FaultSide.Own)=="내 과실","labels");
  Debug.Log("ACCIDENTFAULT_OK checks="+checks);
 }
}
