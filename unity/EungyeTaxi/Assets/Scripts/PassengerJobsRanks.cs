// Taxi ranks and NPC taxis (design 08 ch.5.1 / 5.5 item 7, decided 2026-11-25, Eungye single): hooks into the
// roadside waiting passengers. A rank decides whether its stop has a waiting passenger (only for the taxi at the
// head of the queue) and may name a far destination for that passenger; an NPC taxi can take a roadside passenger.
public partial class PassengerJobs {
 public System.Func<int,bool?> roadsideOverride;
 bool? RoadsideOverride(int index){return roadsideOverride==null?null:roadsideOverride(index);}
 readonly System.Collections.Generic.Dictionary<int,int> forcedRoadsideDrop=new System.Collections.Generic.Dictionary<int,int>();
 public void SetForcedRoadsideDrop(int pickup,int drop){if(drop<0)forcedRoadsideDrop.Remove(pickup);else forcedRoadsideDrop[pickup]=drop;}
 int TakeForcedRoadsideDrop(int pickup){int drop;if(!forcedRoadsideDrop.TryGetValue(pickup,out drop))return -1;forcedRoadsideDrop.Remove(pickup);return drop>=0&&drop<stops.Length&&drop!=pickup?drop:-1;}
 public bool RoadsideWaitingAt(int index){return roadsideWaiting!=null&&index>=0&&index<roadsideWaiting.Length&&roadsideWaiting[index];}
 public void SetRoadsideWaiting(int index,bool value){if(roadsideWaiting==null||roadsideWaiting.Length!=stops.Length)System.Array.Resize(ref roadsideWaiting,stops.Length);if(index<0||index>=roadsideWaiting.Length)return;roadsideWaiting[index]=value;if(people!=null&&index<people.Length&&people[index])people[index].SetActive(!session.IsSimulationBot&&HasWaitingPassenger(index));}
 // An app call or the player's own pickup there is never taken.
 public bool RoadsideTakeable(int index){if(!RoadsideWaitingAt(index))return false;if(current!=null&&current.pickup==index&&trip.State!=TripState.Idle)return false;foreach(var o in offers)if(o!=null&&o.pickup==index)return false;return true;}
 // Another taxi picked the roadside passenger up first.
 public bool TakenByOtherTaxi(int index){if(!RoadsideTakeable(index))return false;SetRoadsideWaiting(index,false);cooldown[index]=UnityEngine.Mathf.Max(cooldown[index],UnityEngine.Time.time+45);return true;}
}
