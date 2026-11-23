using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// 2026-11-24 chained dispatch player acceptance (Eungye single, run with -contentTest): reserve near the drop-off,
// the current trip settles unchanged, the reserved call starts after the result with a pickup budget measured
// from the drop-off point, and a saved reservation survives a restore. Stops are posed, not driven.
public sealed class ChainProbe : MonoBehaviour {
 const string Flag="-chainCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Chain acceptance").AddComponent<ChainProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("CHAIN_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Pose(Vector3 at,Vector3 dir){s.taxi.body.velocity=Vector3.zero;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=at+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(dir);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();}
 IEnumerator Start(){
  float until=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>until){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-chainOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/chain");Directory.CreateDirectory(output);
  yield return null;string error=null;var j=s.jobs;JobOffer reserved=null;PassengerResult settled=null;int dropIndex=-1;
  try{
   s.business.preparing=false;s.paused=false;Time.timeScale=1;s.business.Story.firstDispatchAccepted=true;j.ClearForDay();Array.Clear(j.cooldown,0,j.cooldown.Length);j.ResetOffers();j.ForcedPassengerType=PassengerType.General;j.ForcedRideCase=DrunkRideCase.Normal;
   int index=Array.FindIndex(j.offers,o=>o!=null);Check(index>=0,"a call on the board");var offer=j.offers[index];dropIndex=offer.drop;
   var p=j.stops[offer.pickup];Pose(p.Kerb,p.Direction);Check(j.Accept(index),"accept");for(int i=0;i<4&&j.trip.State==TripState.ToPickup;i++)j.Interact();Check(j.trip.State==TripState.OnTrip,"passenger aboard");
   var far=j.stops[offer.drop];Pose(far.Kerb-far.Direction.normalized*(ChainRules.PreviewMetres+300),far.Direction);Check(!j.ChainWindow,"closed far from the drop-off");
   Pose(far.Kerb-far.Direction.normalized*60,far.Direction);Check(j.ChainWindow,"open within "+ChainRules.PreviewMetres+" m of the drop-off");
   var list=j.ChainCandidates();report.AppendLine("candidates "+string.Join(" | ",list.Select(o=>o.pickup+"->"+o.drop+" "+Mathf.RoundToInt(j.ChainPickupMetres(o))+"m")));
   Check(list.Length>=1&&list.Length<=ChainRules.MaxOffers&&list.All(o=>j.ChainPickupMetres(o)<=ChainRules.RadiusMetres&&o.pickup!=offer.drop),"nearby candidates within the radius");
   int fareBefore=j.trip.Fare;float remainingBefore=j.trip.Remaining;reserved=list[0];Check(j.ReserveChain(reserved)&&j.ChainedOffer==reserved,"reserve");
   Check(j.trip.Fare==fareBefore&&j.trip.State==TripState.OnTrip&&Mathf.Abs(j.trip.Remaining-remainingBefore)<.01f,"current trip untouched by the reservation");
   var snap=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(j.CaptureLive()));Check(snap.hasChained&&snap.chained.pickup==reserved.pickup,"reservation is saved");
   j.CancelChain();j.RestoreLive(snap);Check(j.ChainedOffer!=null&&j.ChainedOffer.pickup==reserved.pickup,"reservation survives a restore");reserved=j.ChainedOffer;
   var legacy=JsonUtility.FromJson<PassengerJobsSnapshot>("{\"generation\":1}");Check(!legacy.hasChained,"older saves have no reservation");
   if(j.DrunkRide!=null)j.DrunkRide.Advance(30,.8f);j.trip.Tick(20);Pose(far.Kerb,far.Direction);int before=s.business.passengers.Count;
   for(int i=0;i<4&&j.trip.State==TripState.OnTrip;i++)j.Interact();Check(s.business.passengers.Count==before+1,"current trip settled");settled=s.business.passengers.Last();
   Check(settled.paid>0&&settled.paid==settled.baseFare+settled.bonus-settled.meterDeduction,"fare settled by the usual rule");
  }catch(Exception e){error=e.ToString();}
  if(error==null){var panel=s.GetComponent<RideCompletionPanel>();if(panel&&panel.Open)panel.Close();s.paused=false;Time.timeScale=1;float wait=Time.realtimeSinceStartup+10;
   while(j.trip.State!=TripState.ToPickup&&Time.realtimeSinceStartup<wait)yield return null;
   try{
    Check(j.trip.State==TripState.ToPickup&&j.current!=null&&j.current.pickup==reserved.pickup&&j.ChainedOffer==null,"reserved call started after the drop-off result");
    float expected=OneDayOfferPolicy.PickupSeconds(j.nav.DistanceFromVehicle(j.stops[reserved.pickup]));report.AppendLine("pickup budget "+j.trip.PickupRemaining+" expected "+expected);
    Check(Mathf.Abs(j.trip.PickupRemaining-expected)<=6,"pickup deadline counted from the drop-off point");
    Check(s.business.passengers.Count(x=>x.tripId==settled.tripId)==1,"the finished trip was recorded once");
   }catch(Exception e){error=e.ToString();}}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("CHAIN_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
