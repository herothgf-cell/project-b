using UnityEngine;
public partial class PassengerJobs {
 readonly System.Collections.Generic.Dictionary<int,int[]> shortDrops=new System.Collections.Generic.Dictionary<int,int[]>();
 int ShortSharedDrop(int pickup,int serial,int fallback){int[] options;if(!shortDrops.TryGetValue(pickup,out options)){var candidates=new System.Collections.Generic.List<int>();var distances=new System.Collections.Generic.Dictionary<int,float>();for(int d=0;d<stops.Length;d++){if(d==pickup)continue;float distance=nav.Distance(stops[pickup],stops[d]);if(!float.IsNaN(distance)&&!float.IsInfinity(distance)&&distance>=80){candidates.Add(d);distances[d]=distance;}}candidates.Sort((a,b)=>distances[a].CompareTo(distances[b]));int count=Mathf.Min(3,candidates.Count);options=candidates.GetRange(0,count).ToArray();shortDrops[pickup]=options;}return options.Length==0?fallback:options[(serial&int.MaxValue)%options.Length];}
 int generationDemandPhase=3;string[] nativeDemandRegions;
 string DemandRegion(int index){
  string region=index>=0&&index<runtimeStopIds.Count?PassengerDemandPolicy.Region(runtimeStopIds[index]):"neutral";if(region!="neutral")return region;
  if(nativeDemandRegions==null&&session.world&&session.world.map!=null)nativeDemandRegions=SimulationDemandClock.NativeRegions(session.world.map);
  return nativeDemandRegions!=null&&index>=0&&index<nativeDemandRegions.Length?nativeDemandRegions[index]:"neutral";
 }
 double DemandRank(JobOffer offer,int phase){return PassengerDemandPolicy.OfferRegionPriority(offer.candidateSeed,DemandRegion(offer.pickup),DemandRegion(offer.drop),offer.pickupStopId+">"+offer.dropStopId,phase);}
 // Rank a bounded set of new endpoint pairs before native Make performs the route
 // query. Existing accepted/shared offer objects are never re-ranked or rewritten.
 void SharedDemandEndpoints(int serial,out int pickup,out int drop){
  int stride=stops.Length/2+1;while(Gcd(stride,stops.Length)!=1)stride++;
  int phase=SimulationDemandClock.Phase(session);double best=double.PositiveInfinity;pickup=-1;drop=-1;
  string seed=WaitingSeed+":shared:"+serial;
  for(int k=0;k<6;k++){int candidate=serial+k*(stops.Length+1),p=session.simulationMatch?PvpPassengerPolicy.Pickup(candidate,stops.Length,session.simulationMatch.Elapsed,PvpSimulationMode.DurationSeconds):(candidate*stride)%stops.Length;
   if(StopOccupied(p)||session.simulationMatch&&session.simulationMatch.SharedCalls!=null&&!session.simulationMatch.SharedCalls.CanGenerateAt(p))continue;
   int offset=candidate%2==0?1+(candidate/stops.Length)%Mathf.Max(1,stops.Length/3):Mathf.Max(1,stops.Length/2)+(candidate/stops.Length)%Mathf.Max(1,stops.Length/3);int d=(p+offset)%stops.Length;if(d==p)d=(d+1)%stops.Length;
   double rank=PassengerDemandPolicy.OfferRegionPriority(seed,DemandRegion(p),DemandRegion(d),runtimeStopIds[p]+">"+runtimeStopIds[d],phase);if(rank>=best)continue;best=rank;pickup=p;drop=d;
  }
 }
}
