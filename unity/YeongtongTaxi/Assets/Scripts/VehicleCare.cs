using System;
using System.Collections.Generic;

[Serializable] public class VehicleCareSnapshot {
 public int cleanliness=100,day=1,drunksBoarded,refusals;
 public bool acceptanceLocked;
 public string[] settled=new string[0],refused=new string[0];
}
// Vehicle condition is independent of manners, law and fare. Identities make effects replay safe.
public sealed class VehicleCare {
 public int Cleanliness {get;private set;}
 public int Day {get;private set;}
 public int DrunksBoarded {get;private set;}
 public int Refusals {get;private set;}
 public bool AcceptanceLocked {get;private set;}
 public bool NeedsWash {get{return Cleanliness<30;}}
 public int BoardingBonus {get{return Cleanliness>=80?3:0;}}
 readonly HashSet<string> settled=new HashSet<string>(),refused=new HashSet<string>();
 public VehicleCare(){Cleanliness=100;Day=1;}
 public void BeginDay(int day){if(day!=Day){Day=day;DrunksBoarded=0;}}
 public bool Refuse(string id){if(!NeedsWash||string.IsNullOrEmpty(id)||!refused.Add(id))return false;Refusals++;AcceptanceLocked=true;return true;}
 public bool BoardDrunk(){if(NeedsWash||DrunksBoarded>=1)return false;DrunksBoarded++;return true;}
 public bool Finish(string trip,bool vomited){if(string.IsNullOrEmpty(trip)||!settled.Add(trip))return false;Cleanliness=vomited?Math.Min(Cleanliness,10):Math.Max(0,Cleanliness-2);return true;}
 public void Wash(){Cleanliness=100;AcceptanceLocked=false;}
 public VehicleCareSnapshot Snapshot(){return new VehicleCareSnapshot{cleanliness=Cleanliness,day=Day,drunksBoarded=DrunksBoarded,refusals=Refusals,acceptanceLocked=AcceptanceLocked,settled=new List<string>(settled).ToArray(),refused=new List<string>(refused).ToArray()};}
 public static VehicleCare Restore(VehicleCareSnapshot s){if(s==null)return new VehicleCare();if(s.cleanliness<0||s.cleanliness>100||s.day<1||s.drunksBoarded<0||s.drunksBoarded>1||s.refusals<0||s.acceptanceLocked&&s.cleanliness>=30)throw new ArgumentException("Invalid vehicle care");var c=new VehicleCare{Cleanliness=s.cleanliness,Day=s.day,DrunksBoarded=s.drunksBoarded,Refusals=s.refusals,AcceptanceLocked=s.acceptanceLocked};foreach(var id in s.settled??new string[0])if(string.IsNullOrEmpty(id)||!c.settled.Add(id))throw new ArgumentException("Invalid care trip");foreach(var id in s.refused??new string[0])if(string.IsNullOrEmpty(id)||!c.refused.Add(id))throw new ArgumentException("Invalid refusal");return c;}
}

public enum DrunkRideCase { Random, Normal, Vomit, Sleepy, Both }
[Serializable] public sealed class DrunkRideState {
 public string identity;
 public bool IsDrunk,VomitPlanned,SleepPlanned,Vomited,ArrivalLocked,Waiting,OnTime,Settled;
 public float VomitProgress,Elapsed,ArrivalElapsed,DisembarkElapsed,WaitElapsed;
 public float WaitUntilWake {get{return Math.Max(0,15-WaitElapsed);}}
 public static uint Hash(string value){uint hash=2166136261;foreach(char c in value??""){hash^=c;hash*=16777619;}return hash;}
 static float Roll(string id,string tag){return (Hash(id+":"+tag)%100000)/100000f;}
 public static DrunkRideState Create(string id,bool slotAvailable,DrunkRideCase fixedCase=DrunkRideCase.Random){bool forced=fixedCase!=DrunkRideCase.Random;bool drunk=slotAvailable&&(forced?fixedCase!=DrunkRideCase.Normal:Roll(id,"drunk")<.2f);return new DrunkRideState{identity=id,IsDrunk=drunk,VomitPlanned=drunk&&(forced?fixedCase==DrunkRideCase.Vomit||fixedCase==DrunkRideCase.Both:Roll(id,"vomit")<.25f),SleepPlanned=drunk&&(forced?fixedCase==DrunkRideCase.Sleepy||fixedCase==DrunkRideCase.Both:Roll(id,"sleep")<.3f),VomitProgress=.3f+.4f*Roll(id,"progress")};}
 public bool Advance(float seconds,float progress){if(Settled||float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)return false;Elapsed+=seconds;if(ArrivalLocked)WaitElapsed+=seconds;if(IsDrunk&&VomitPlanned&&!Vomited&&!ArrivalLocked&&progress>=VomitProgress){Vomited=true;return true;}return false;}
 public bool Arrive(bool onTime){if(ArrivalLocked)return false;ArrivalLocked=true;OnTime=onTime;ArrivalElapsed=Elapsed;Waiting=IsDrunk&&SleepPlanned;return true;}
 public bool CanDisembark(bool safe,bool wake){return !Settled&&ArrivalLocked&&safe&&(!Waiting||WaitElapsed>=30||wake&&WaitElapsed>=15);}
 public bool Disembark(bool safe,bool wake){if(!CanDisembark(safe,wake))return false;Settled=true;Waiting=false;DisembarkElapsed=Elapsed;return true;}
 public DrunkRideState Copy(){return (DrunkRideState)MemberwiseClone();}
 public void Validate(){foreach(float value in new[]{VomitProgress,Elapsed,ArrivalElapsed,DisembarkElapsed,WaitElapsed})if(float.IsNaN(value)||float.IsInfinity(value)||value<0)throw new ArgumentException("Invalid drunk ride timing");if(string.IsNullOrEmpty(identity)||VomitProgress<.3f||VomitProgress>.7f||Waiting&&(!ArrivalLocked||!IsDrunk||!SleepPlanned)||Vomited&&(!IsDrunk||!VomitPlanned)||ArrivalElapsed>Elapsed||DisembarkElapsed>Elapsed)throw new ArgumentException("Invalid drunk ride state");}
}
