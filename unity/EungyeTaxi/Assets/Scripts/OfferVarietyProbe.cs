using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Isolated generation/route acceptance. Does not drive, board or settle rides.
public sealed class OfferVarietyProbe:MonoBehaviour {
 [Serializable] public sealed class PhaseResult {public int phase,generations,coverage,repeatedPickups,sameBoards;public int[] slotCoverage,slotRepeats,pickupCounts;}
 [Serializable] public sealed class Result {public bool success;public int checks,stops;public string map,savePath,error;public PhaseResult[] phases;}
 Result result=new Result();DriveSession session;string output;bool finished;
 static bool Has(string flag){return Array.IndexOf(Environment.GetCommandLineArgs(),flag)>=0;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Has("-offerVarietyTest"))new GameObject("Offer variety acceptance").AddComponent<OfferVarietyProbe>();}
 void Check(bool value,string message){result.checks++;if(!value)throw new Exception(message);}
 IEnumerator Start(){
  output=ContentTuningRuntime.Argument("-offerVarietyOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/pickup-variety/native");Directory.CreateDirectory(output);Application.runInBackground=true;
  var run=Run();while(true){object next;try{if(!run.MoveNext())break;next=run.Current;}catch(Exception e){Finish(e);yield break;}yield return next;}Finish(null);
 }
 void Finish(Exception error){if(finished)return;finished=true;result.success=error==null;result.error=error==null?"":error.ToString();File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));Debug.Log("OFFER_VARIETY_PLAYER_"+(result.success?"OK":"FAIL")+" checks="+result.checks+" "+result.error);Application.Quit(result.success?0:1);}
 IEnumerator Run(){
  Check(ContentTuningRuntime.TestMode,"-contentTest required for isolated campaign");
  float deadline=Time.realtimeSinceStartup+180;
  while(!(session=FindObjectsOfType<DriveSession>().FirstOrDefault(s=>!s.IsSimulationBot))||!session.StartupReady){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(!session&&mode)mode.StartSingle();if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("single world startup");yield return null;}
  Check(!session.simulationMatch,"single campaign is required; shared host call board is unchanged");
  result.savePath=session.business.SavePath;Check(Path.GetFullPath(result.savePath).StartsWith(Path.GetFullPath(ContentTuningRuntime.TestDirectory)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"campaign path is private contentTest directory");
  session.business.enabled=false;session.business.preparing=false;session.business.enabledCampaign=true;session.business.useV2=true;
  session.taxi.enabled=false;session.taxi.body.isKinematic=true;session.jobs.enabled=false;session.law.enabled=false;session.requests.enabled=false;
  Time.timeScale=0;session.paused=true;
  var jobs=session.jobs;result.map=session.world.map.mapId;result.stops=jobs.stops.Length;
  var zones=jobs.GetComponent<ServiceLawZones>();Check(jobs.Catalog!=null&&jobs.nav.hd!=null&&zones&&zones.Ready,"real stop catalog, directed graph and representative courses ready");
  var phases=new List<PhaseResult>();result.phases=new PhaseResult[0];
  foreach(double elapsed in new[]{0.0,400.0,1000.0,1300.0}){
   session.business.settings=PrototypeSettings.ForThreeDays();session.business.ledger=new EconomyLedger(session.business.settings);session.business.ledger.Advance(elapsed,0);
   jobs.ClearForDay();jobs.ResetOffers();var stats=new PhaseResult{phase=SimulationDemandClock.Phase(session),pickupCounts=new int[jobs.stops.Length],slotCoverage=new int[3],slotRepeats=new int[3]};
   var seen=new HashSet<int>();var slotSeen=new[]{new HashSet<int>(),new HashSet<int>(),new HashSet<int>()};int[] previous=jobs.offers.Where(o=>o!=null).Select(o=>o.pickup).ToArray();
   for(int generation=0;generation<24;generation++){
    int sourceGeneration=jobs.OfferGeneration;jobs.Generate();var present=jobs.offers.Where(o=>o!=null).ToArray();Check(present.Select(o=>o.pickup).Distinct().Count()==present.Length,"board has distinct pickups");
    var now=present.Select(o=>o.pickup).ToArray();bool same=now.Length==previous.Length&&now.All(i=>Array.IndexOf(previous,i)>=0);if(same)stats.sameBoards++;
    for(int slot=0;slot<jobs.offers.Length;slot++){
     var offer=jobs.offers[slot];if(offer==null)continue;
     Check(offer.pickup>=0&&offer.pickup<jobs.stops.Length&&offer.drop>=0&&offer.drop<jobs.stops.Length&&offer.pickup!=offer.drop,"native endpoint indices are legal");
     var from=jobs.stops[offer.pickup];var to=jobs.stops[offer.drop];Check(jobs.nav.hd.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"native directed ride route exists");
     float pickup=PassengerJobs.DirectedDistance(jobs.nav.hd,session.taxi.transform.position,session.taxi.transform.forward,from);Check(!float.IsNaN(pickup)&&!float.IsInfinity(pickup)&&!jobs.StopOccupied(offer.pickup),"pickup is currently reachable and available");
     seen.Add(offer.pickup);slotSeen[slot].Add(offer.pickup);stats.pickupCounts[offer.pickup]++;
     if(Array.IndexOf(previous,offer.pickup)>=0){stats.repeatedPickups++;stats.slotRepeats[slot]++;
      // Report representative repeats too; a merely fresh course need not
      // satisfy the stronger spatial pass that production intentionally keeps.
     }
    }
    // A small constrained pool can validly repeat. Recreate this generation's
    // ordinary pool (not the next one's), and require progress only if a fresh
    // pickup satisfies the strict spatial pass against every displayed stop.
    if(same){
     var field=typeof(PassengerJobs).GetField("generation",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);int savedGeneration=jobs.OfferGeneration;JobOffer[] pool;
     try{field.SetValue(jobs,sourceGeneration);pool=jobs.OrdinaryCandidates();}finally{field.SetValue(jobs,savedGeneration);}
     bool alternative=pool.Any(o=>Array.IndexOf(previous,o.pickup)<0&&present.All(prior=>OneDayOfferPolicy.PickupCell(jobs.stops[prior.pickup].x,jobs.stops[prior.pickup].z,session.world.map.width,session.world.map.depth)!=OneDayOfferPolicy.PickupCell(jobs.stops[o.pickup].x,jobs.stops[o.pickup].z,session.world.map.width,session.world.map.depth)&&(jobs.stops[prior.pickup].Kerb-jobs.stops[o.pickup].Kerb).sqrMagnitude>=OneDayOfferPolicy.PickupSeparation*OneDayOfferPolicy.PickupSeparation));
     Check(!alternative,"a full stale board must not persist with an equally spread fresh reachable alternative");
    }
    stats.generations++;previous=now;yield return null;
   }
   stats.coverage=seen.Count;for(int slot=0;slot<3;slot++)stats.slotCoverage[slot]=slotSeen[slot].Count;
   Check(stats.generations==24&&stats.coverage>=Math.Min(3,jobs.stops.Length),"native generator maintains a usable distribution");phases.Add(stats);result.phases=phases.ToArray();Debug.Log("OFFER_VARIETY_NATIVE phase="+stats.phase+" generations="+stats.generations+" coverage="+stats.coverage+" repeats="+stats.repeatedPickups+" sameBoards="+stats.sameBoards+" slotCoverage="+string.Join(",",stats.slotCoverage.Select(x=>x.ToString()).ToArray())+" slotRepeats="+string.Join(",",stats.slotRepeats.Select(x=>x.ToString()).ToArray()));
  }
 }
}
