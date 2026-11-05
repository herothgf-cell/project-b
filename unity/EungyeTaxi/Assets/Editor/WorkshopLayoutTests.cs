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
