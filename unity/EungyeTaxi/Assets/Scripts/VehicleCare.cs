using System;
using System.Collections.Generic;

[Serializable] public class VehicleCareSnapshot {
 public int cleanliness=100,day=1,drunksBoarded,refusals;
 public bool acceptanceLocked,vomitUnwashed;
 public double dirtSeconds;
 public string[] settled=new string[0],refused=new string[0];
}
// Vehicle condition is independent of manners, law and fare. Identities make effects replay safe.
public sealed class VehicleCare {
 public int Cleanliness {get;private set;}
 public int Day {get;private set;}
 public int DrunksBoarded {get;private set;}
 public int Refusals {get;private set;}
 public bool AcceptanceLocked {get;private set;}
 public bool VomitUnwashed {get;private set;}
 public bool NeedsWash {get{return Cleanliness<30||VomitUnwashed;}}
 public int BoardingBonus {get{return Cleanliness>=80?3:0;}}
 readonly HashSet<string> settled=new HashSet<string>(),refused=new HashSet<string>();
 double dirtSeconds;
 public VehicleCare(){Cleanliness=100;Day=1;}
 public void BeginDay(int day){if(day!=Day){Day=day;DrunksBoarded=0;}}
 public bool Refuse(string id){if(!NeedsWash||string.IsNullOrEmpty(id)||!refused.Add(id))return false;Refusals++;AcceptanceLocked=true;return true;}
 public bool BoardDrunk(){if(NeedsWash||DrunksBoarded>=1)return false;DrunksBoarded++;return true;}
 public void MarkVomit(){VomitUnwashed=true;AcceptanceLocked=true;}
 public bool Finish(string trip,bool vomited){if(string.IsNullOrEmpty(trip)||!settled.Add(trip))return false;if(vomited)MarkVomit();Cleanliness=vomited?Math.Min(Cleanliness,10):Math.Max(0,Cleanliness-2);return true;}
 public void Advance(double seconds){if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;double total=dirtSeconds+seconds;double points=Math.Floor(total/120);dirtSeconds=total%120;Cleanliness=(int)Math.Max(0,Cleanliness-Math.Min(100,points));}
 public void Wash(){Cleanliness=100;dirtSeconds=0;AcceptanceLocked=false;VomitUnwashed=false;}
 public VehicleCareSnapshot Snapshot(){return new VehicleCareSnapshot{dirtSeconds=dirtSeconds,cleanliness=Cleanliness,day=Day,drunksBoarded=DrunksBoarded,refusals=Refusals,acceptanceLocked=AcceptanceLocked,vomitUnwashed=VomitUnwashed,settled=new List<string>(settled).ToArray(),refused=new List<string>(refused).ToArray()};}
 public static VehicleCare Restore(VehicleCareSnapshot s){if(s==null)return new VehicleCare();if(double.IsNaN(s.dirtSeconds)||double.IsInfinity(s.dirtSeconds)||s.dirtSeconds<0||s.dirtSeconds>=120||s.cleanliness<0||s.cleanliness>100||s.day<1||s.drunksBoarded<0||s.drunksBoarded>1||s.refusals<0||s.acceptanceLocked&&s.cleanliness>=30&&!s.vomitUnwashed)throw new ArgumentException("Invalid vehicle care");var c=new VehicleCare{dirtSeconds=s.dirtSeconds,Cleanliness=s.cleanliness,Day=s.day,DrunksBoarded=s.drunksBoarded,Refusals=s.refusals,VomitUnwashed=s.vomitUnwashed,AcceptanceLocked=s.acceptanceLocked||s.vomitUnwashed||s.cleanliness<30};foreach(var id in s.settled??new string[0])if(string.IsNullOrEmpty(id)||!c.settled.Add(id))throw new ArgumentException("Invalid care trip");foreach(var id in s.refused??new string[0])if(string.IsNullOrEmpty(id)||!c.refused.Add(id))throw new ArgumentException("Invalid refusal");return c;}
}

public enum DrunkRideCase { Random, Normal, Vomit, Sleepy, Both }
[Serializable] public sealed class DrunkRideState {
 public string identity;
 public bool IsDrunk,VomitPlanned,SleepPlanned,Vomited,ArrivalLocked,Waiting,OnTime,Settled;
 public float VomitProgress,Elapsed,ArrivalElapsed,DisembarkElapsed,WaitElapsed;
 public bool Warned,DialogueCritical;
 public float WarningElapsed,NextDialogueElapsed,DialogueUntil;
 public int DialogueCount;
 public string Dialogue;
 static readonly string[] chatter={"기사님, 오늘은 말이 자꾸 나오네요. 집까지 부탁해요.","아까도 이 얘기 했나요? 오늘 모임이 참 길었어요.","노래 한 곡 생각나는데… 가사가 하나도 기억이 안 나네요.","저 안 졸아요. 잠깐 눈만 감고 있는 거예요.","오늘은 제가 말을 너무 많이 하죠? 고맙습니다, 기사님.","창밖 불빛이 참 많네요. 천천히 편하게 가 주세요."};
 public string VisibleDialogue(bool quiet){return !Settled&&!ArrivalLocked&&Elapsed<DialogueUntil&&(!quiet||DialogueCritical)?Dialogue??"":"";}
 public bool UpdateDialogue(bool quiet){if(!IsDrunk||Settled||ArrivalLocked||Warned&&!Vomited||Elapsed<DialogueUntil)return false;if(NextDialogueElapsed<=0)NextDialogueElapsed=12+8*Roll(identity,"talk:0");if(Elapsed<NextDialogueElapsed)return false;int index=(int)(Hash(identity+":voice")%(uint)chatter.Length)+DialogueCount;DialogueCount++;NextDialogueElapsed=Elapsed+12+8*Roll(identity,"talk:"+DialogueCount);if(quiet)return true;DialogueCritical=false;Dialogue="손님  “"+chatter[index%chatter.Length]+"”";DialogueUntil=Elapsed+6;return true;}
 public float WaitUntilWake {get{return Math.Max(0,15-WaitElapsed);}}
 public static uint Hash(string value){uint hash=2166136261;foreach(char c in value??""){hash^=c;hash*=16777619;}return hash;}
 static float Roll(string id,string tag){return (Hash(id+":"+tag)%100000)/100000f;}
 public static DrunkRideState Create(string id,bool slotAvailable,DrunkRideCase fixedCase=DrunkRideCase.Random){bool forced=fixedCase!=DrunkRideCase.Random;bool drunk=slotAvailable&&(forced?fixedCase!=DrunkRideCase.Normal:Roll(id,"drunk")<.2f);return new DrunkRideState{identity=id,IsDrunk=drunk,Dialogue=drunk?"손님  “조금 취했네요. 목적지까지 부탁해요.”":null,DialogueUntil=drunk?6:0,VomitPlanned=drunk&&(forced?fixedCase==DrunkRideCase.Vomit||fixedCase==DrunkRideCase.Both:Roll(id,"vomit")<.25f),SleepPlanned=drunk&&(forced?fixedCase==DrunkRideCase.Sleepy||fixedCase==DrunkRideCase.Both:Roll(id,"sleep")<.3f),VomitProgress=.3f+.4f*Roll(id,"progress")};}
 public bool Advance(float seconds,float progress){if(Settled||float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)return false;Elapsed+=seconds;if(ArrivalLocked)WaitElapsed+=seconds;if(IsDrunk&&VomitPlanned&&!Vomited&&!ArrivalLocked){if(!Warned&&progress>=VomitProgress){Warned=true;DialogueCritical=true;WarningElapsed=Elapsed;Dialogue="손님  “속이 울렁거려요… 곧 토할 것 같아요.”";DialogueUntil=Elapsed+3;return false;}if(Warned&&Elapsed-WarningElapsed>=3){Vomited=true;DialogueCritical=true;Dialogue="손님  “죄송해요… 참지 못했어요.” · 구토 발생 / 하차 후 세차 필요";DialogueUntil=Elapsed+8;return true;}}return false;}
 public bool Arrive(bool onTime){if(ArrivalLocked)return false;ArrivalLocked=true;OnTime=onTime;ArrivalElapsed=Elapsed;Waiting=IsDrunk&&SleepPlanned;return true;}
 public bool CanDisembark(bool safe,bool wake){return !Settled&&ArrivalLocked&&safe&&(!Waiting||WaitElapsed>=30||wake&&WaitElapsed>=15);}
 public bool Disembark(bool safe,bool wake){if(!CanDisembark(safe,wake))return false;Settled=true;Waiting=false;DisembarkElapsed=Elapsed;return true;}
 public DrunkRideState Copy(){return (DrunkRideState)MemberwiseClone();}
 public void Validate(){foreach(float value in new[]{VomitProgress,Elapsed,ArrivalElapsed,DisembarkElapsed,WaitElapsed,WarningElapsed,NextDialogueElapsed,DialogueUntil})if(float.IsNaN(value)||float.IsInfinity(value)||value<0)throw new ArgumentException("Invalid drunk ride timing");if(DialogueCount<0||WarningElapsed>Elapsed||Warned&&(!IsDrunk||!VomitPlanned)||string.IsNullOrEmpty(identity)||VomitProgress<.3f||VomitProgress>.7f||Waiting&&(!ArrivalLocked||!IsDrunk||!SleepPlanned)||Vomited&&(!IsDrunk||!VomitPlanned)||ArrivalElapsed>Elapsed||DisembarkElapsed>Elapsed)throw new ArgumentException("Invalid drunk ride state");}
}
