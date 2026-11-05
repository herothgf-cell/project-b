using System;
using UnityEngine;
public static class WorkshopLayoutTests {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception("WORKSHOP: "+message);}
 public static void Run(){
  checks=0;PvpSimulationMode.Clear();CheckMap(MapCatalog.Load());CheckMap(PvpCityMap.Create());
  var reservations=new WorkshopReservations();for(int i=0;i<4;i++)Check(reservations.TryReserve("tow:"+i,i+1,true),"four D0 reservations");
  Check(!reservations.TryReserve("tow:0",5,true),"no double allocation");Check(!reservations.TryReserve("blocked",6,false),"physical occupancy checked before charge");
  Check(reservations.TryReserve("tow:0",1,true),"same reservation restoration");reservations.Release(1);Check(reservations.TryReserve("tow:0",5,true),"repair releases reservation");
  var wire=JsonUtility.FromJson<PvpNativeOwn>(JsonUtility.ToJson(new PvpNativeOwn{workshopKind=2,workshopRemaining=17.5f,workshopNotice="예약칸 확인 중"}));
  Check(wire.workshopKind==2&&Mathf.Abs(wire.workshopRemaining-17.5f)<.001f&&wire.workshopNotice=="예약칸 확인 중","replica workshop progress round trip");
  var obj=new GameObject("Workshop view test");obj.SetActive(false);var damage=obj.AddComponent<VehicleDamageController>();
  try{
   damage.ApplyWorkshopNetworkView(wire.workshopKind,wire.workshopRemaining,wire.workshopNotice);
   Check(damage.PendingServiceKind==0&&damage.PendingServiceRemaining==0&&damage.Bays.Count==0,"network view cannot start authoritative service or reservation");
   var field=typeof(VehicleDamageController).GetField("networkWorkshopRemaining",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   Check(Mathf.Abs((float)field.GetValue(damage)-17.5f)<.001f,"network presentation retains remaining time");
   damage.ApplyWorkshopNetworkView(1,float.NaN,null);Check((float)field.GetValue(damage)==0,"malformed network time cannot poison UI");
   damage.ApplyWorkshopNetworkView(0,100,"완료");Check((float)field.GetValue(damage)==0&&damage.PendingServiceRemaining==0,"completed snapshot clears progress without starting service");
  }finally{UnityEngine.Object.DestroyImmediate(obj);}
  Debug.Log("WORKSHOP_LAYOUT_TESTS_OK checks="+checks);
 }
 static void CheckMap(MapData map){
  FuelStationLayout.Apply(map);var sites=WorkshopLayout.For(map);Check(sites.Count==2,map.mapId+" exactly two validated workshops");
  Check(ReferenceEquals(sites,WorkshopLayout.For(map)),"stable cached layout");
  for(int i=0;i<sites.Count;i++){
   var s=sites[i];WorkshopLayout.Validate(map,s);Check(s.tow.Length==2,"two dedicated tow bays");
   Check(s.entry.Length>20&&s.departure.Length>20,"smooth independent entry and exit");Check(s.waitingEntry.Length>20&&s.waitingToRepair.Length>20,"physical off-lane waiting entry and merge");
   Check(s.approach.laneEdge>=0,"directed road approach");Check(s.CanRepair(s.repair,s.direction,0),"ordinary repair alignment");
   Check(!s.CanRepair(s.repair,-s.direction,0)&&!s.CanRepair(s.repair,s.direction,1),"heading and movement reject purchase");
   Check(!s.CanRepair(s.waiting,s.direction,0)&&!s.CanRepair(s.tow[0],s.direction,0),"waiting and tow cannot become ordinary bay");
   Check(Vector3.Distance(s.tow[0],s.tow[1])>=4,"independent physical tow space");
   Debug.Log("WORKSHOP_VERIFIED "+s.id+" repair="+s.repair+" waiting="+s.waiting+" tow0="+s.tow[0]+" tow1="+s.tow[1]+" edge="+s.approach.laneEdge);
  }
  Check(Vector3.Distance(sites[0].repair,sites[1].repair)>600,"separate neighbourhoods");
 }
}
