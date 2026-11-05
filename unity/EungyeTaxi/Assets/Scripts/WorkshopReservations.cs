using System.Collections.Generic;
// A physical bay can stay empty while its paid owner is temporarily displaced.
public sealed class WorkshopReservations {
 readonly Dictionary<string,int> bays=new Dictionary<string,int>();
 public bool Available(string bay,int owner){int value;return !bays.TryGetValue(bay,out value)||value==owner;}
 public bool TryReserve(string bay,int owner,bool physicallyClear){
  if(string.IsNullOrEmpty(bay)||!physicallyClear||!Available(bay,owner))return false;
  foreach(var item in bays)if(item.Value==owner&&item.Key!=bay)return false;
  bays[bay]=owner;return true;
 }
 public void Release(int owner){string found=null;foreach(var item in bays)if(item.Value==owner){found=item.Key;break;}if(found!=null)bays.Remove(found);}
}
