using System;

public enum AccidentPhase { None, Stopping, Waiting, Processing, Completed }
// Owned by the runtime's save/network snapshot, separate from campaign accounting.
[Serializable] public sealed class AccidentState {
 public const double ProcessingSeconds=45, RearmSeconds=.8;
 public string eventId="",targetId="";
 public double stopSeconds; public bool recoveryRequired; public string outcome=""; public string[] targets=new string[0];
 public AccidentPhase phase;
 public bool chargeApplied,outcomeApplied,contactLatched;
 // Version 0 is a legacy 45-second/3000-won event. Never infer or roll a new
 // severity when an old save lacks the selected-result fields.
 public int resolutionVersion,selectedFee,selectedScorePenalty;
 public double selectedSeconds;
 public AccidentKind accidentKind;
 public AccidentSeverity accidentSeverity;
 public bool selectedTowRequired,scoreApplied,workshopTransferred;
 public double elapsed,remaining,contactClearSeconds;
 public double ProcessingDuration {get{return resolutionVersion==0?ProcessingSeconds:selectedSeconds;}}
 public int Fee {get{return resolutionVersion==0?3000:selectedFee;}}
 public int ScorePenalty {get{return resolutionVersion==0?0:selectedScorePenalty;}}
 public bool TowRequired {get{return resolutionVersion!=0&&selectedTowRequired;}}
 public AccidentKind Kind {get{return accidentKind;}}
 public AccidentSeverity Severity {get{return accidentSeverity;}}
 public bool Locked {get{return phase==AccidentPhase.Stopping||phase==AccidentPhase.Waiting||phase==AccidentPhase.Processing;}}
 public AccidentState Copy(){var copy=(AccidentState)MemberwiseClone();copy.targets=targets==null?new string[0]:(string[])targets.Clone();return copy;}
 public bool Begin(string id,string target){
  return BeginCore(id,target,null);
 }
 public bool BeginResolved(string id,string target,AccidentResolution resolution){
  return resolution!=null&&BeginCore(id,target,resolution);
 }
 bool BeginCore(string id,string target,AccidentResolution resolution){
  if(Locked||contactLatched||string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(target)||id==eventId)return false;
  resolutionVersion=resolution==null?0:1;accidentKind=resolution==null?AccidentKind.Person:resolution.Kind;accidentSeverity=resolution==null?AccidentSeverity.Minor:resolution.Severity;
  selectedFee=resolution==null?0:resolution.Fee;selectedSeconds=resolution==null?0:resolution.Seconds;selectedScorePenalty=resolution==null?0:resolution.ScorePenalty;selectedTowRequired=resolution!=null&&resolution.TowRequired;scoreApplied=workshopTransferred=false;
  eventId=id;targetId=target;stopSeconds=0;recoveryRequired=false;outcome="";targets=new[]{target};phase=AccidentPhase.Stopping;chargeApplied=outcomeApplied=false;elapsed=0;remaining=ProcessingDuration;contactLatched=true;contactClearSeconds=0;return true;
 }
 public bool MarkStopped(bool pvp){if(phase!=AccidentPhase.Stopping)return false;phase=pvp?AccidentPhase.Processing:AccidentPhase.Waiting;return true;}
 public double AdvanceProcessing(double seconds){
  if(phase!=AccidentPhase.Processing||!Finite(seconds)||seconds<=0)return 0;
  double actual=Math.Min(remaining,seconds);elapsed=Math.Min(ProcessingDuration,elapsed+actual);remaining=ProcessingDuration-elapsed;
  if(remaining<1e-9){remaining=0;elapsed=ProcessingDuration;}return actual;
 }
 public bool Complete(){if(recoveryRequired||TowRequired&&!workshopTransferred)return false;if(phase!=AccidentPhase.Waiting&&(phase!=AccidentPhase.Processing||remaining>0)||!chargeApplied||!outcomeApplied)return false;elapsed=ProcessingDuration;remaining=0;phase=AccidentPhase.Completed;contactClearSeconds=0;return true;}
 public void ObserveContact(bool touching,double seconds){
  if(!Finite(seconds)||seconds<0)return;
  if(touching){contactClearSeconds=0;return;}
  // Rearm only after return to driving, never while a processing lock is active.
  if(!contactLatched||Locked)return;
  contactClearSeconds=Math.Min(RearmSeconds,contactClearSeconds+seconds);
  if(contactClearSeconds>=RearmSeconds-1e-12){contactLatched=false;contactClearSeconds=0;}
 }
 static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
 public void Validate(){
  if(resolutionVersion!=0&&resolutionVersion!=1||!Enum.IsDefined(typeof(AccidentKind),accidentKind)||!Enum.IsDefined(typeof(AccidentSeverity),accidentSeverity))throw new ArgumentException("Invalid accident resolution");
  if(resolutionVersion==0){if(selectedFee!=0||selectedSeconds!=0||selectedScorePenalty!=0||selectedTowRequired||accidentKind!=AccidentKind.Person||accidentSeverity!=AccidentSeverity.Minor)throw new ArgumentException("Invalid legacy accident resolution");}
  else {var chosen=AccidentSeverityPolicy.Resolve(accidentKind,accidentSeverity);if(selectedFee!=chosen.Fee||selectedSeconds!=chosen.Seconds||selectedScorePenalty!=chosen.ScorePenalty||selectedTowRequired!=chosen.TowRequired)throw new ArgumentException("Inconsistent accident resolution");}
  if(targets==null)targets=new string[0];if(!Finite(stopSeconds)||stopSeconds<0)throw new ArgumentException("Invalid stopping time");
  var seen=new System.Collections.Generic.HashSet<string>();foreach(var target in targets)if(string.IsNullOrWhiteSpace(target)||!seen.Add(target))throw new ArgumentException("Invalid accident targets");
  if(!Enum.IsDefined(typeof(AccidentPhase),phase)||!Finite(elapsed)||!Finite(remaining)||!Finite(contactClearSeconds)||elapsed<0||elapsed>ProcessingDuration||remaining<0||remaining>ProcessingDuration||contactClearSeconds<0||contactClearSeconds>=RearmSeconds||!contactLatched&&contactClearSeconds!=0)throw new ArgumentException("Invalid accident state");
  if(phase==AccidentPhase.None){if(!string.IsNullOrEmpty(eventId)||!string.IsNullOrEmpty(targetId)||chargeApplied||outcomeApplied||elapsed!=0||remaining!=0||contactLatched||resolutionVersion!=0||scoreApplied||workshopTransferred)throw new ArgumentException("Invalid empty accident");return;}
  if(string.IsNullOrWhiteSpace(eventId)||string.IsNullOrWhiteSpace(targetId)||Math.Abs(elapsed+remaining-ProcessingDuration)>1e-8||Locked&&!contactLatched||(phase==AccidentPhase.Stopping||phase==AccidentPhase.Waiting)&&elapsed!=0||phase==AccidentPhase.Completed&&(remaining!=0||!chargeApplied||!outcomeApplied||TowRequired&&!workshopTransferred))throw new ArgumentException("Inconsistent accident state");
 }
}
