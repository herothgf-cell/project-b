using System;
public enum SlowRightTurnVerdict { NoOpportunity, Compliant, Violation, Deferred }
// A configured GAME observation threshold, not a universal legal definition of slow driving.
[Serializable] public sealed class SlowRightTurnState {
 public const float GameLimitKmh=10,ToleranceKmh=2,RequiredSeconds=.75f;
 public int version=1;public float observedSeconds,continuousExcess;public bool violated,necessary;
 public void Observe(float seconds,float kmh,bool rightTurning,bool known,bool safetyAvoidance){
  if(!rightTurning||!known||float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0||float.IsNaN(kmh)||float.IsInfinity(kmh)||kmh<0){continuousExcess=0;return;}
  observedSeconds+=seconds;necessary|=safetyAvoidance;
  if(kmh>GameLimitKmh+ToleranceKmh)continuousExcess+=seconds;else continuousExcess=0;
  if(continuousExcess>=RequiredSeconds)violated=true;
 }
 public SlowRightTurnVerdict Verdict {get{return necessary?SlowRightTurnVerdict.Deferred:observedSeconds<RequiredSeconds?SlowRightTurnVerdict.NoOpportunity:violated?SlowRightTurnVerdict.Violation:SlowRightTurnVerdict.Compliant;}}
 public SlowRightTurnState Copy(){return (SlowRightTurnState)MemberwiseClone();}
 public void Validate(){if(version!=1||float.IsNaN(observedSeconds)||float.IsInfinity(observedSeconds)||float.IsNaN(continuousExcess)||float.IsInfinity(continuousExcess)||observedSeconds<0||continuousExcess<0||continuousExcess>observedSeconds||violated&&observedSeconds<RequiredSeconds)throw new ArgumentException("Invalid slow-turn checkpoint");}
}
