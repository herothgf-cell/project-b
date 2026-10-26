using System;
public enum TripState { Idle, ToPickup, OnTrip, Complete, Failed }
[Serializable] public sealed class FareTripSnapshot {public TripState state;public int fare;public float remaining,pickupRemaining;public bool pickupExpired,arrivalLocked,arrivedOnTime;}
public class FareTrip {
 public bool ExtendForSystemDelay(float seconds){if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0||seconds>90||ArrivalLocked)return false;if(State==TripState.ToPickup&&!PickupExpired){PickupRemaining+=seconds;return true;}if(State==TripState.OnTrip){Remaining+=seconds;return true;}return false;}
 public TripState State {get;private set;} public float Remaining {get;private set;} public int Fare {get;private set;}
 public float PickupRemaining {get;private set;} public bool PickupExpired {get;private set;}
 public bool ArrivalLocked {get;private set;}public bool ArrivedOnTime {get;private set;}
 public bool LockArrival(){if(State!=TripState.OnTrip||ArrivalLocked)return false;ArrivalLocked=true;ArrivedOnTime=Remaining>0;return true;}
 public FareTripSnapshot Snapshot(){return new FareTripSnapshot{state=State,fare=Fare,remaining=Remaining,pickupRemaining=float.IsPositiveInfinity(PickupRemaining)?float.MaxValue:PickupRemaining,pickupExpired=PickupExpired,arrivalLocked=ArrivalLocked,arrivedOnTime=ArrivedOnTime};}
 public void Restore(FareTripSnapshot s){if(s==null||!Enum.IsDefined(typeof(TripState),s.state)||s.fare<0||s.remaining<0||float.IsNaN(s.remaining)||float.IsInfinity(s.remaining)||s.pickupRemaining<0||float.IsNaN(s.pickupRemaining)||s.arrivalLocked&&s.state!=TripState.OnTrip&&s.state!=TripState.Complete&&s.state!=TripState.Failed)throw new ArgumentException("Invalid trip snapshot");State=s.state;Fare=s.fare;Remaining=s.remaining;PickupRemaining=s.pickupRemaining;PickupExpired=s.pickupExpired;ArrivalLocked=s.arrivalLocked;ArrivedOnTime=s.arrivedOnTime;}
 public bool Accept(int fare,float seconds,float pickupSeconds=float.PositiveInfinity){if(State!=TripState.Idle)return false;Fare=fare;Remaining=seconds;PickupRemaining=Math.Max(0,pickupSeconds);PickupExpired=false;State=TripState.ToPickup;return true;}
 public bool Board(){if(State!=TripState.ToPickup)return false;State=TripState.OnTrip;return true;}
 // A deadline changes the payout; the passenger leaves only at the destination.
 public void Tick(float seconds){if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)return;if(State==TripState.ToPickup){PickupRemaining=Math.Max(0,PickupRemaining-seconds);if(PickupRemaining<=0){PickupExpired=true;State=TripState.Failed;}return;}if(State!=TripState.OnTrip||ArrivalLocked)return;Remaining=Math.Max(0,Remaining-seconds);}
 public bool Revise(int fare,float remaining){if(State!=TripState.OnTrip||ArrivalLocked||Remaining<=0||fare<Fare||remaining<Remaining||float.IsNaN(remaining)||float.IsInfinity(remaining))return false;Fare=fare;Remaining=remaining;return true;}
 public int Complete(){if(State!=TripState.OnTrip)return 0;if(Remaining<=0){State=TripState.Failed;return 0;}State=TripState.Complete;return Fare;}
 public void Clear(){State=TripState.Idle;Fare=0;Remaining=PickupRemaining=0;PickupExpired=ArrivalLocked=ArrivedOnTime=false;}
}
