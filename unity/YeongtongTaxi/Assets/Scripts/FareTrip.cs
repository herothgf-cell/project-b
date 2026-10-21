using System;
public enum TripState { Idle, ToPickup, OnTrip, Complete, Failed }
public class FareTrip {
 public TripState State {get;private set;} public float Remaining {get;private set;} public int Fare {get;private set;}
 public float PickupRemaining {get;private set;} public bool PickupExpired {get;private set;}
 public bool Accept(int fare,float seconds,float pickupSeconds=float.PositiveInfinity){if(State!=TripState.Idle)return false;Fare=fare;Remaining=seconds;PickupRemaining=Math.Max(0,pickupSeconds);PickupExpired=false;State=TripState.ToPickup;return true;}
 public bool Board(){if(State!=TripState.ToPickup)return false;State=TripState.OnTrip;return true;}
 // A deadline changes the payout; the passenger leaves only at the destination.
 public void Tick(float seconds){if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)return;if(State==TripState.ToPickup){PickupRemaining=Math.Max(0,PickupRemaining-seconds);if(PickupRemaining<=0){PickupExpired=true;State=TripState.Failed;}return;}if(State!=TripState.OnTrip)return;Remaining=Math.Max(0,Remaining-seconds);}
 public bool Revise(int fare,float remaining){if(State!=TripState.OnTrip||Remaining<=0||fare<Fare||remaining<Remaining||float.IsNaN(remaining)||float.IsInfinity(remaining))return false;Fare=fare;Remaining=remaining;return true;}
 public int Complete(){if(State!=TripState.OnTrip)return 0;if(Remaining<=0){State=TripState.Failed;return 0;}State=TripState.Complete;return Fare;}
 public void Clear(){State=TripState.Idle;Fare=0;Remaining=PickupRemaining=0;PickupExpired=false;}
}
