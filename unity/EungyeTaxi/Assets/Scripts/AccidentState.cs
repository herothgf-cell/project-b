using System;

public enum AccidentPhase { None, Stopping, Waiting, Processing, Completed }
// Owned by the runtime's save/network snapshot, separate from campaign accounting.
[Serializable] public sealed class AccidentState {
 public const double ProcessingSeconds=45, RearmSeconds=.8;
 public string eventId="",targetId="";
 public double stopSeconds; public bool recoveryRequired; public string outcome=""; public string[] targets=new string[0];
 public AccidentPhase phase;
 public bool chargeApplied,outcomeApplied,contactLatched;
 public double elapsed,remaining,contactClearSeconds;
 public bool Locked {get{return phase==AccidentPhase.Stopping||phase==AccidentPhase.Waiting||phase==AccidentPhase.Processing;}}
 public AccidentState Copy(){var copy=(AccidentState)MemberwiseClone();copy.targets=targets==null?new string[0]:(string[])targets.Clone();return copy;}
 public bool Begin(string id,string target){
  if(Locked||contactLatched||string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(target)||id==eventId)return false;
  eventId=id;targetId=target;stopSeconds=0;recoveryRequired=false;outcome="";targets=new[]{target};phase=AccidentPhase.Stopping;chargeApplied=outcomeApplied=false;elapsed=0;remaining=ProcessingSeconds;contactLatched=true;contactClearSeconds=0;return true;
 }
 public bool MarkStopped(bool pvp){if(phase!=AccidentPhase.Stopping)return false;phase=pvp?AccidentPhase.Processing:AccidentPhase.Waiting;return true;}
 public double AdvanceProcessing(double seconds){
  if(phase!=AccidentPhase.Processing||!Finite(seconds)||seconds<=0)return 0;
  double actual=Math.Min(remaining,seconds);elapsed=Math.Min(ProcessingSeconds,elapsed+actual);remaining=ProcessingSeconds-elapsed;
  if(remaining<1e-9){remaining=0;elapsed=ProcessingSeconds;}return actual;
 }
 public bool Complete(){if(recoveryRequired)return false;if(phase!=AccidentPhase.Waiting&&(phase!=AccidentPhase.Processing||remaining>0)||!chargeApplied||!outcomeApplied)return false;elapsed=ProcessingSeconds;remaining=0;phase=AccidentPhase.Completed;contactClearSeconds=0;return true;}
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
  if(targets==null)targets=new string[0];if(!Finite(stopSeconds)||stopSeconds<0)throw new ArgumentException("Invalid stopping time");
  var seen=new System.Collections.Generic.HashSet<string>();foreach(var target in targets)if(string.IsNullOrWhiteSpace(target)||!seen.Add(target))throw new ArgumentException("Invalid accident targets");
  if(!Enum.IsDefined(typeof(AccidentPhase),phase)||!Finite(elapsed)||!Finite(remaining)||!Finite(contactClearSeconds)||elapsed<0||elapsed>ProcessingSeconds||remaining<0||remaining>ProcessingSeconds||contactClearSeconds<0||contactClearSeconds>=RearmSeconds||!contactLatched&&contactClearSeconds!=0)throw new ArgumentException("Invalid accident state");
  if(phase==AccidentPhase.None){if(!string.IsNullOrEmpty(eventId)||!string.IsNullOrEmpty(targetId)||chargeApplied||outcomeApplied||elapsed!=0||remaining!=0||contactLatched)throw new ArgumentException("Invalid empty accident");return;}
  if(string.IsNullOrWhiteSpace(eventId)||string.IsNullOrWhiteSpace(targetId)||Math.Abs(elapsed+remaining-ProcessingSeconds)>1e-8||Locked&&!contactLatched||(phase==AccidentPhase.Stopping||phase==AccidentPhase.Waiting)&&elapsed!=0||phase==AccidentPhase.Completed&&(remaining!=0||!chargeApplied||!outcomeApplied))throw new ArgumentException("Inconsistent accident state");
 }
}
