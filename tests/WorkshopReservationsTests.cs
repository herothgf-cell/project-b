using System;
public static class WorkshopReservationsTests {
 public static void Run() {
  var type=Type.GetType("WorkshopReservations");
  if(type==null)throw new Exception("FAIL: paid tow reservations are missing");
  dynamic r=Activator.CreateInstance(type);
  for(int i=0;i<4;i++)if(!(bool)r.TryReserve("tow:"+i,i+1,true))throw new Exception("four D0 capacity");
  if((bool)r.TryReserve("tow:0",5,true))throw new Exception("double reservation");
  if((bool)r.TryReserve("blocked",6,false))throw new Exception("occupied bay reservation");
  if(!(bool)r.TryReserve("tow:0",1,true))throw new Exception("restore own paid reservation");
  r.Release(1);
  if(!(bool)r.TryReserve("tow:0",5,true))throw new Exception("release after repair");
  if((bool)r.TryReserve("other",2,true))throw new Exception("owner may not pay for second bay");
  Console.WriteLine("WORKSHOP_RESERVATIONS_TESTS_OK");
 }
}
