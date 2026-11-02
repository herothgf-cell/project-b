using System;
public enum PassengerSituationKind { None, SignalWait, Jolt, NecessaryBrake }
[Serializable] public sealed class PassengerSituationState {
 public PassengerType Type;public bool LuggageLoaded,PhysicalCaptured,FoodConsumed,FoodSpilled,SleepWoken;public int PhysicalBrake,PhysicalAccel,PhysicalNecessary;
 public float ReSecureRemaining;
 public bool OffersReSecure {get{return LuggageLoaded&&(Pending==PassengerSituationKind.Jolt||Pending==PassengerSituationKind.NecessaryBrake);}}
 public bool ApplyPhysicalObservation(PassengerBehaviorState behavior,int brake,int accel,int necessary){
  bool observed=PhysicalCaptured&&(brake>PhysicalBrake||accel>PhysicalAccel||necessary>PhysicalNecessary);PhysicalCaptured=true;PhysicalBrake=brake;PhysicalAccel=accel;PhysicalNecessary=necessary;
  if(behavior==null)return false;Type=behavior.Type;LuggageLoaded=behavior.Boarded&&(Type==PassengerType.Luggage&&behavior.Choice==0||Type==PassengerType.MuddyLuggage&&(behavior.Choice==0||behavior.Choice==1));bool changed=observed&&behavior.ObserveJolt();FoodConsumed=Type==PassengerType.Food&&behavior.ConsumptionOccurred;FoodSpilled=Type==PassengerType.Food&&behavior.SpillOccurred;SleepWoken=Type==PassengerType.Sleeping&&behavior.JoltWoken;return changed;
 }
 public bool AdvanceReSecure(float seconds,bool safe,bool paused){if(paused||!safe||!Finite(seconds)||seconds<=0||ReSecureRemaining<=0)return false;ReSecureRemaining=Math.Max(0,ReSecureRemaining-seconds);return ReSecureRemaining<=0;}
 public int Version=1,Reactions,LastBrake,LastAccel,LastNecessary,Answer=-1;
 public string TripId;
 public float Elapsed,SignalSeconds,LastReactionAt=-20;
 public bool Quiet,SignalReacted;
 public PassengerSituationKind Pending;
 // These observations describe the existing driving event. They never own scores or dirt.
 public string Dialogue {get{if(ReSecureRemaining>0)return "짐을 안전하게 확인·재고정하고 있어요. 안전 정차 "+Math.Ceiling(ReSecureRemaining)+"초 · 영업시간·연료 소모";string safety=Pending==PassengerSituationKind.NecessaryBrake?"안전을 위한 제동은 이해해요. ":"";if(Pending!=PassengerSituationKind.None&&Pending!=PassengerSituationKind.SignalWait){if(Type==PassengerType.Sleeping)return safety+(SleepWoken?"방금 흔들림에 잠에서 깼어요. 안전할 때 안심할 수 있게 안내해 주세요.":"쉬던 중 흔들림을 느꼈어요. 안전하게 부탁해요.");if(Type==PassengerType.Food)return safety+(FoodSpilled?"음식·음료를 흘렸어요. 안전한 곳에서 상태를 확인할게요.":FoodConsumed?"먹던 음식·음료가 흔들렸네요. 쏟지 않게 잡고 갈게요.":"보관한 음식·음료가 흔들렸네요. 포장을 확인하고 그대로 보관할게요.");if(LuggageLoaded)return safety+"짐이 흔들렸네요. 안전 정차한 동안5초 확인·재고정할까요? 계속 운행도 가능해요.";}return Pending==PassengerSituationKind.SignalWait?"신호를 오래 기다리고 있네요. 안전할 때 남은 안내를 부탁해요.":Pending==PassengerSituationKind.NecessaryBrake?"안전을 위한 제동이었군요. 필요한 조작은 이해해요.":Pending==PassengerSituationKind.Jolt?"방금 차가 흔들렸네요. 안전한 곳에서 짧게 안내해 주세요.":"안내를 확인했어요. 안전하게 부탁해요.";}}
 public static PassengerSituationState Create(string trip,int brake,int accel,int necessary){var s=new PassengerSituationState{TripId=trip,LastBrake=brake,LastAccel=accel,LastNecessary=necessary,PhysicalCaptured=true,PhysicalBrake=brake,PhysicalAccel=accel,PhysicalNecessary=necessary};s.Validate();return s;}
 public bool Observe(float seconds,bool signalWait,bool moving,bool available,bool quiet,int reserved,int brake,int accel,int necessary,bool paused){
  if(paused||!Finite(seconds)||seconds<=0)return false;
  Elapsed+=seconds;bool jolt=brake>LastBrake||accel>LastAccel,required=necessary>LastNecessary;
  LastBrake=Math.Max(LastBrake,brake);LastAccel=Math.Max(LastAccel,accel);LastNecessary=Math.Max(LastNecessary,necessary);
  if(signalWait&&!moving)SignalSeconds+=seconds;else{SignalSeconds=0;SignalReacted=false;}
  if(Pending!=PassengerSituationKind.None||Quiet||quiet||!available||ReSecureRemaining>0||Reactions>=2||reserved+Reactions>=3||Elapsed-LastReactionAt<20)return false;
  var kind=required?PassengerSituationKind.NecessaryBrake:jolt?PassengerSituationKind.Jolt:SignalSeconds>=18&&!SignalReacted?PassengerSituationKind.SignalWait:PassengerSituationKind.None;
  if(kind==PassengerSituationKind.None)return false;
  Pending=kind;Answer=-1;Reactions++;LastReactionAt=Elapsed;if(kind==PassengerSituationKind.SignalWait)SignalReacted=true;return true;
 }
 public bool Respond(int answer,bool safe){if(!safe||Pending==PassengerSituationKind.None||answer<0||answer>1)return false;if(answer==0&&OffersReSecure)ReSecureRemaining=5;Answer=answer;Quiet|=answer==1;Pending=PassengerSituationKind.None;LastReactionAt=Elapsed;return true;}
 public PassengerSituationState Copy(){return (PassengerSituationState)MemberwiseClone();}
 static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
 public void Validate(){if(!Enum.IsDefined(typeof(PassengerType),Type)||FoodConsumed&&Type!=PassengerType.Food||FoodSpilled&&(!FoodConsumed||Type!=PassengerType.Food)||SleepWoken&&Type!=PassengerType.Sleeping||PhysicalBrake<0||PhysicalAccel<0||PhysicalNecessary<0||!Finite(ReSecureRemaining)||ReSecureRemaining<0||ReSecureRemaining>5||ReSecureRemaining>0&&(!LuggageLoaded||Pending!=PassengerSituationKind.None)||LuggageLoaded&&Type!=PassengerType.Luggage&&Type!=PassengerType.MuddyLuggage)throw new ArgumentException("Invalid typed passenger situation");if(Version!=1||string.IsNullOrEmpty(TripId)||!Enum.IsDefined(typeof(PassengerSituationKind),Pending)||Reactions<0||Reactions>2||Answer< -1||Answer>1||LastBrake<0||LastAccel<0||LastNecessary<0||!Finite(Elapsed)||Elapsed<0||!Finite(SignalSeconds)||SignalSeconds<0||!Finite(LastReactionAt)||LastReactionAt< -20||LastReactionAt>Elapsed||Pending!=PassengerSituationKind.None&&Reactions==0)throw new ArgumentException("Invalid passenger situation state");}
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
