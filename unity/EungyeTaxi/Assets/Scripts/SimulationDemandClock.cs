using UnityEngine;
public static class SimulationDemandClock {
 public static int Phase(DriveSession session){
  if(!session)return 3;
  if(session.simulationMatch)return PassengerDemandPolicy.Phase(session.simulationMatch.Elapsed,PvpSimulationMode.DurationSeconds,true);
  if(!session.business||session.business.ledger==null)return 3;
  return PassengerDemandPolicy.Phase(session.business.ledger.Elapsed,session.business.ledger.Settings.daySeconds,false);
 }
 // Native NPC endpoints borrow the nearest already-authored fictional demand
 // district. The own-city map uses explicitly fictional cyclic endpoint roles.
 public static string[] NativeRegions(MapData map){
  var result=new string[map.stops.Length];var catalog=PassengerStopCatalog.For(map);
  for(int i=0;i<result.Length;i++){
   result[i]=map.mapId=="eungye"?"neutral":i%3==0?"residential":i%3==1?"office":"commercial";
   float best=float.PositiveInfinity;
   if(map.mapId=="eungye")foreach(var entry in catalog.Entries){string region=PassengerDemandPolicy.Region(entry.StopId);if(region=="neutral")continue;float d=(entry.Stop.V()-map.stops[i].V()).sqrMagnitude;if(d<best){best=d;result[i]=region;}}
  }
  return result;
 }
}
