using System;

public enum AccidentKind { Person, Vehicle, Property }
public enum AccidentSeverity { Minor, Moderate, Major }

// An event receives this value once. Random generation and physical recovery belong
// to the authoritative controller; this policy has no engine or clock dependency.
public sealed class AccidentResolution {
 public AccidentKind Kind {get;private set;}
 public AccidentSeverity Severity {get;private set;}
 public int Fee {get;private set;}
 public double Seconds {get;private set;}
 public int ScorePenalty {get;private set;}
 public bool TowRequired {get;private set;}
 internal AccidentResolution(AccidentKind kind,AccidentSeverity severity,int fee,double seconds,int penalty){Kind=kind;Severity=severity;Fee=fee;Seconds=seconds;ScorePenalty=penalty;TowRequired=severity==AccidentSeverity.Major;}
}

public static class AccidentSeverityPolicy {
 public static int PedestrianDamageMaximum(AccidentSeverity severity){
  if(!Enum.IsDefined(typeof(AccidentSeverity),severity))throw new ArgumentException("Invalid accident severity");
  return new[]{2,5,10}[(int)severity];
 }
 public static int SamplePedestrianDamage(AccidentSeverity severity,int roll){
  if(roll<0||roll>=1000)throw new ArgumentException("Invalid pedestrian damage roll");
  return roll*(PedestrianDamageMaximum(severity)+1)/1000;
 }
 public static AccidentResolution Resolve(AccidentKind kind,AccidentSeverity severity){
  if(!Enum.IsDefined(typeof(AccidentKind),kind)||!Enum.IsDefined(typeof(AccidentSeverity),severity))throw new ArgumentException("Invalid accident classification");
  int index=(int)severity;
  if(kind==AccidentKind.Person)return new AccidentResolution(kind,severity,new[]{3000,8000,15000}[index],new[]{45,120,240}[index],new[]{10,25,40}[index]);
  return new AccidentResolution(kind,severity,new[]{1000,3000,6000}[index],new[]{15,45,90}[index],new[]{3,8,15}[index]);
 }
 public static AccidentResolution Sample(AccidentKind kind,int roll){
  if(roll<0||roll>=1000)throw new ArgumentException("Accident roll must be between 0 and 999");
  int minor=kind==AccidentKind.Person?600:700,moderate=kind==AccidentKind.Person?900:950;
  return Resolve(kind,roll<minor?AccidentSeverity.Minor:roll<moderate?AccidentSeverity.Moderate:AccidentSeverity.Major);
 }
}
