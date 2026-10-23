// Fictional roadside demand, independent of the catalog of safe stopping places.
public static class PassengerWaitingPolicy {
 public const int RefreshSeconds=120;
 public static bool Present(string seed,int generation,string stopId){return DrunkRideState.Hash(seed+":"+generation+":"+stopId+":waiting")%100<35;}
}
