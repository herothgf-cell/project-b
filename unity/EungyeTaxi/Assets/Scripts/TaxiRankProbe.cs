using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Taxi ranks and NPC taxis, player acceptance (Eungye single, 2026-11-25). Positions are set by the probe.
// Three ranks with queued taxis; the empty taxi stopped behind the queue keeps its place, moves up as taxis leave
// with passengers, and at the head takes a passenger who goes to one of the farthest stops. NPC taxis are painted
// and take a roadside passenger first, never an app call.
public sealed class TaxiRankProbe : MonoBehaviour {
 const string Flag="-rankCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Taxi rank acceptance").AddComponent<TaxiRankProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("TAXI_RANK_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Info(string line){report.AppendLine("INFO "+line);}
 void Pose(Vector3 p,Vector3 dir){var t=s.taxi;t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(dir);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();}
 bool Feed(string text){var n=s.GetComponent<DriveNotifications>();return n&&n.Feed.Any(line=>line.Contains(text));}
 IEnumerator Start(){
  float limit=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limit){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-rankOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/rank");Directory.CreateDirectory(output);
  yield return null;string error=null;var j=s.jobs;var tr=s.ranks;var npc=s.npcTaxis;TaxiRanks.Rank r=null;
  try{
   s.business.preparing=false;s.paused=false;Time.timeScale=1;j.ClearForDay();
   Check(tr&&tr.ranks.Count==3,"three game-only ranks ("+(tr?tr.ranks.Count:0)+")");
   foreach(var x in tr.ranks)Info(x.data.name+" stop "+x.stop+" "+KoreanText.Stop(j.stops[x.stop].name)+" queue "+x.queue.Count);
   Check(tr.ranks.All(x=>x.queue.Count==2&&x.queue.All(q=>q&&q.GetComponent<BoxCollider>()&&q.GetComponent<StagedVehicle>())),"each rank starts with two queued taxis (solid, staged vehicles)");
   Check(npc&&npc.TaxiCount==8,"eight ordinary cars are NPC taxis ("+(npc?npc.TaxiCount:0)+")");
   r=tr.ranks.First(x=>x.data.id=="rank-centre");
   // Queue: stop behind the last taxi.
   Pose(r.kerb-r.dir*TaxiRankRules.Slot*r.queue.Count,r.dir);
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(.5f);
  try{Check(r.playerAhead==2,"stopped behind two taxis: two ahead ("+r.playerAhead+")");Check(tr.Status.Contains("앞 택시 2대"),"status shows the place ("+tr.Status+")");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"rank-queue.png"));yield return null;
  // Two passengers come: the taxis ahead leave; the player moves up each time.
  for(int n=0;n<2;n++){r.nextPassenger=Time.time;yield return null;yield return null;Pose(r.kerb-r.dir*TaxiRankRules.Slot*r.queue.Count,r.dir);yield return new WaitForSeconds(.3f);}
  try{Check(r.queue.Count==0&&r.playerAhead==0,"after two departures the player is at the head ("+r.playerAhead+")");Check(r.departures>=2,"queued taxis left with passengers");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  r.nextPassenger=Time.time;yield return null;yield return null;
  try{Check(r.passengerWaiting&&j.RoadsideWaitingAt(r.stop)&&j.HasWaitingPassenger(r.stop),"a passenger waits for the player at the head");Check(Feed("승강장 · 손님이 왔어요"),"a card says so");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  Pose(j.stops[r.stop].Kerb,r.dir);yield return new WaitForSeconds(.3f);
  {float until=Time.realtimeSinceStartup+6;while(j.trip.State!=TripState.OnTrip&&Time.realtimeSinceStartup<until){j.Interact();yield return null;}}
  try{
   Check(j.trip.State==TripState.OnTrip&&j.current!=null&&j.current.pickup==r.stop,"the rank passenger boards with F");
   var far=Enumerable.Range(0,j.stops.Length).Where(i=>i!=r.stop&&i!=j.Catalog.OppositeIndex(r.stop)).OrderByDescending(i=>j.nav.Distance(j.stops[r.stop],j.stops[i])).Take(TaxiRankRules.FarChoices).ToArray();
   Info("drop "+KoreanText.Stop(j.stops[j.current.drop].name)+" "+Mathf.RoundToInt(j.current.distance)+" m; far set "+string.Join(",",far));
   Check(far.Contains(j.current.drop),"the rank passenger goes to one of the three farthest stops");
   Check(!r.passengerWaiting,"the rank is free again");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // NPC taxis: a roadside passenger at a non-rank stop is taken; an app call is not.
  j.ClearForDay();yield return null;
  try{
   int stop=Enumerable.Range(0,j.stops.Length).First(i=>!tr.IsRankStop(i));for(int k=0;k<j.offers.Length;k++)j.offers[k]=null;
   j.SetRoadsideWaiting(stop,true);Check(j.RoadsideTakeable(stop),"a roadside passenger can be taken");
   int taken=npc.Taken;Check(npc.TryTakeAt(stop)&&npc.Taken==taken+1&&!j.RoadsideWaitingAt(stop),"an NPC taxi takes the roadside passenger first");
   int other=Enumerable.Range(0,j.stops.Length).First(i=>!tr.IsRankStop(i)&&i!=stop);j.SetRoadsideWaiting(other,true);j.offers[0]=new JobOffer{pickup=other,drop=(other+3)%j.stops.Length,fare=6000,seconds=600};
   Check(!j.RoadsideTakeable(other)&&!npc.TryTakeAt(other),"an app call's passenger is never taken");
   Check(!j.RoadsideTakeable(r.stop)||tr.IsRankStop(r.stop),"rank stops are handled by the rank, not by passing taxis");
  }catch(Exception e){error=e.ToString();}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("TAXI_RANK_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
