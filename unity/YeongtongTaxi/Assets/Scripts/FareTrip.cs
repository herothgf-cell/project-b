using System;
public enum TripState { Idle, ToPickup, OnTrip, Complete, Failed }
public class FareTrip {
 public TripState State {get;private set;} public float Remaining {get;private set;} public int Fare {get;private set;}
 public bool Accept(int fare,float seconds){if(State!=TripState.Idle)return false;Fare=fare;Remaining=seconds;State=TripState.ToPickup;return true;}
 public bool Board(){if(State!=TripState.ToPickup)return false;State=TripState.OnTrip;return true;}
 // A deadline changes the payout; the passenger leaves only at the destination.
 public void Tick(float seconds){if(State!=TripState.OnTrip)return;Remaining=Math.Max(0,Remaining-Math.Max(0,seconds));}
 public int Complete(){if(State!=TripState.OnTrip)return 0;if(Remaining<=0){State=TripState.Failed;return 0;}State=TripState.Complete;return Fare;}
 public void Clear(){State=TripState.Idle;Fare=0;Remaining=0;}
}
