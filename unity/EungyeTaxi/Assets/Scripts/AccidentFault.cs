using System;
using UnityEngine;

// Who is at fault in a vehicle contact (design 08 ch.5.6 item 1.1, Eungye single, 2026-11-24).
// A game rule in three steps, not a model of real liability shares. Rows are tried in order.
public enum FaultSide { Own=0, Shared=1, Other=2 }

public struct FaultInput {
 public bool otherVehicle;      // the other body is a car (ordinary traffic or a taxi)
 public bool violating;         // wrong way now, or a law violation recorded moments ago
 public bool speeding;          // over the limit of the road plus the tolerance
 public bool aboveLimit;        // over the limit at all, tolerance included (2026-11-24 round 2)
 public bool surprise;          // the other body is a scripted surprise (cut-in, opening door)
 public float sinceSurprise;    // seconds since that surprise began
 public float ownSpeed;         // m/s at the contact point
 public float otherSpeed;       // m/s at the contact point
 public float contactAlong;     // contact position along the taxi, metres from its centre (+front)
 public float halfLength;       // half the taxi length
 public float ownAlong;         // own speed along the taxi's forward axis
 public float otherAlong;       // other speed along the taxi's forward axis
}

public static class AccidentFault {
 public static bool Enabled {get{return ContentTuning.Int("accident.fault.enabled",1)>0;}}
 public static float StoppedKmh {get{return (float)ContentTuning.Number("accident.fault.stoppedKmh",2);}}
 public static float ReactionSeconds {get{return (float)ContentTuning.Number("accident.fault.reactionSeconds",1.2);}}
 public static int SharedPercent {get{return Mathf.Clamp(ContentTuning.Int("accident.fault.sharedPercent",50),0,100);}}
 public static float ViolationSeconds {get{return (float)ContentTuning.Number("accident.fault.violationSeconds",3);}}
 public static FaultSide Decide(FaultInput i,out string reason){
  if(!i.otherVehicle){reason="시설 접촉";return FaultSide.Own;}
  if(i.violating){reason="위반 중 접촉";return FaultSide.Own;}
  bool otherMoving=i.otherSpeed>.5f;
  if(i.ownSpeed*3.6f<StoppedKmh&&otherMoving){reason="정차 중 상대 차량이 접촉";return FaultSide.Other;}
  bool fast=i.speeding||i.aboveLimit;
  // Over the limit, even inside the tolerance, the other side's fault becomes shared.
  if(i.contactAlong<-i.halfLength/3f&&i.otherAlong>i.ownAlong+.5f&&otherMoving){if(fast){reason="뒤에서 받힘 · 제한속도 초과 중";return FaultSide.Shared;}reason="뒤에서 받힘";return FaultSide.Other;}
  if(i.surprise){
   if(!fast&&i.sinceSurprise<=ReactionSeconds){reason="돌발 차량 · 피할 시간 없음";return FaultSide.Other;}
   reason=fast?"돌발 차량 · 제한속도 초과 중":"돌발 차량 · 피할 시간 있었음";return FaultSide.Shared;
  }
  reason="주행 중 접촉";return FaultSide.Own;
 }
 // Shared fault: the fee is halved up to the next 100 won, the score penalty is halved up.
 public static int SharedFee(int fee,int percent){if(fee<=0)return 0;return Math.Max(100,(int)Math.Ceiling(fee*Mathf.Clamp(percent,0,100)/100.0/100.0)*100);}
 public static int SharedPenalty(int penalty,int percent){return penalty<=0?0:(int)Math.Ceiling(penalty*Mathf.Clamp(percent,0,100)/100.0);}
 public static string Label(FaultSide side){return side==FaultSide.Other?"상대 과실":side==FaultSide.Shared?"쌍방 과실":"내 과실";}
}

// Marks a scripted surprise (cut-in car, opening door). None is spawned yet; the fault rule already reads it.
public sealed class FaultActor : MonoBehaviour {
 public float startedAt;
 public void Begin(){startedAt=Time.time;}
}
