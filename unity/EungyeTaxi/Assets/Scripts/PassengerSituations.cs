using System;
public enum PassengerSituationKind { None, SignalWait, Jolt, NecessaryBrake }
[Serializable] public sealed class PassengerSituationState {
 public int Version=1,Reactions,LastBrake,LastAccel,LastNecessary,Answer=-1;
 public string TripId;
 public float Elapsed,SignalSeconds,LastReactionAt=-20;
 public bool Quiet,SignalReacted;
 public PassengerSituationKind Pending;
 // These observations describe the existing driving event. They never own scores or dirt.
 public string Dialogue {get{return Pending==PassengerSituationKind.SignalWait?"신호를 오래 기다리고 있네요. 안전할 때 남은 안내를 부탁해요.":Pending==PassengerSituationKind.NecessaryBrake?"안전을 위한 제동이었군요. 필요한 조작은 이해해요.":Pending==PassengerSituationKind.Jolt?"방금 차가 흔들렸네요. 안전한 곳에서 짧게 안내해 주세요.":"안내를 확인했어요. 안전하게 부탁해요.";}}
 public static PassengerSituationState Create(string trip,int brake,int accel,int necessary){var s=new PassengerSituationState{TripId=trip,LastBrake=brake,LastAccel=accel,LastNecessary=necessary};s.Validate();return s;}
 public bool Observe(float seconds,bool signalWait,bool moving,bool available,bool quiet,int reserved,int brake,int accel,int necessary,bool paused){
  if(paused||!Finite(seconds)||seconds<=0)return false;
  Elapsed+=seconds;bool jolt=brake>LastBrake||accel>LastAccel,required=necessary>LastNecessary;
  LastBrake=Math.Max(LastBrake,brake);LastAccel=Math.Max(LastAccel,accel);LastNecessary=Math.Max(LastNecessary,necessary);
  if(signalWait&&!moving)SignalSeconds+=seconds;else{SignalSeconds=0;SignalReacted=false;}
  if(Pending!=PassengerSituationKind.None||Quiet||quiet||!available||Reactions>=2||reserved+Reactions>=3||Elapsed-LastReactionAt<20)return false;
  var kind=required?PassengerSituationKind.NecessaryBrake:jolt?PassengerSituationKind.Jolt:SignalSeconds>=18&&!SignalReacted?PassengerSituationKind.SignalWait:PassengerSituationKind.None;
  if(kind==PassengerSituationKind.None)return false;
  Pending=kind;Answer=-1;Reactions++;LastReactionAt=Elapsed;if(kind==PassengerSituationKind.SignalWait)SignalReacted=true;return true;
 }
 public bool Respond(int answer,bool safe){if(!safe||Pending==PassengerSituationKind.None||answer<0||answer>1)return false;Answer=answer;Quiet|=answer==1;Pending=PassengerSituationKind.None;LastReactionAt=Elapsed;return true;}
 public PassengerSituationState Copy(){return (PassengerSituationState)MemberwiseClone();}
 static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
 public void Validate(){if(Version!=1||string.IsNullOrEmpty(TripId)||!Enum.IsDefined(typeof(PassengerSituationKind),Pending)||Reactions<0||Reactions>2||Answer< -1||Answer>1||LastBrake<0||LastAccel<0||LastNecessary<0||!Finite(Elapsed)||Elapsed<0||!Finite(SignalSeconds)||SignalSeconds<0||!Finite(LastReactionAt)||LastReactionAt< -20||LastReactionAt>Elapsed||Pending!=PassengerSituationKind.None&&Reactions==0)throw new ArgumentException("Invalid passenger situation state");}
}
[Serializable] public sealed class PassengerErrandState {
 public const float AgreedSeconds=20;
 public int Version=1;public string TripId,ViaStopId,OriginalDestinationId;public float Remaining=AgreedSeconds;public bool Waiting,Completed;
 public static PassengerErrandState Agree(string trip,string via,string destination){var s=new PassengerErrandState{TripId=trip,ViaStopId=via,OriginalDestinationId=destination};s.Validate();return s;}
 public bool Start(bool safe){if(!safe||Waiting||Completed)return false;Waiting=true;return true;}
 public bool Advance(float seconds,bool safe,bool paused){if(!Waiting||Completed||paused||!safe||float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return false;Remaining=Math.Max(0,Remaining-seconds);if(Remaining>0)return false;Waiting=false;Completed=true;return true;}
 public PassengerErrandState Copy(){return (PassengerErrandState)MemberwiseClone();}
 public void Validate(){if(Version!=1||string.IsNullOrEmpty(TripId)||string.IsNullOrEmpty(ViaStopId)||string.IsNullOrEmpty(OriginalDestinationId)||ViaStopId==OriginalDestinationId||float.IsNaN(Remaining)||float.IsInfinity(Remaining)||Remaining<0||Remaining>AgreedSeconds||Completed&&(Waiting||Remaining!=0)||!Completed&&Remaining<=0)throw new ArgumentException("Invalid passenger errand state");}
}
