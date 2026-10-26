using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

// Explicit position/clock controlled integration. It is not manual driving.
public sealed class ProtoExpansionProbe : MonoBehaviour {
 DriveSession s;int checks;bool reload;
 const string Expected="artifacts/proto-expansion-expected.json";
 [Serializable] sealed class ExpectedState {public string trip;public float remaining;public int fare;public double elapsed;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){var a=Environment.GetCommandLineArgs();if(Array.IndexOf(a,"-protoExpansionTest")>=0||Array.IndexOf(a,"-protoExpansionReloadTest")>=0)new GameObject("Proto expansion integration").AddComponent<ProtoExpansionProbe>();}
 void Check(bool value,string message){checks++;Debug.Log((value?"PROTO_PASS ":"PROTO_FAIL ")+message);if(!value)throw new Exception(message);}
 void Pose(int index){var stop=s.jobs.stops[index];var p=stop.Kerb+Vector3.up*.65f;var r=Quaternion.LookRotation(stop.Direction);s.taxi.body.position=p;s.taxi.body.rotation=r;s.taxi.transform.SetPositionAndRotation(p,r);Physics.SyncTransforms();}
 IEnumerator Start(){
  while(!(s=FindObjectOfType<DriveSession>())||!s.StartupReady)yield return null;yield return null;yield return null;
  reload=Array.IndexOf(Environment.GetCommandLineArgs(),"-protoExpansionReloadTest")>=0;bool ok=false;
  try{Disable();if(reload)Reload();else Save();ok=true;}catch(Exception e){Debug.LogException(e);}
  if(ok&&Array.IndexOf(Environment.GetCommandLineArgs(),"-protoCapture")>=0){Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.8f);s.GetComponent<FullMapOverlay>().Open();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/proto-expansion-map.png"));yield return new WaitForSecondsRealtime(.3f);s.GetComponent<FullMapOverlay>().Close();}
  Debug.Log("PROTO_EXPANSION_"+(reload?"RELOAD_":"SAVE_")+(ok?"OK":"FAILED")+" checks="+checks);Application.Quit(ok?0:1);
 }
 void Disable(){s.business.enabled=s.jobs.enabled=s.jobs.nav.enabled=s.requests.enabled=s.law.enabled=s.manners.enabled=s.taxi.enabled=s.assisted.enabled=false;s.taxi.body.isKinematic=true;s.taxi.body.interpolation=RigidbodyInterpolation.None;s.GetComponent<ServiceLawZones>().enabled=false;s.GetComponent<StopOccupancySystem>().enabled=false;}
 void Save(){
  var b=s.business;var j=s.jobs;b.settings=PrototypeSettings.ForThreeDays();b.ledger=new EconomyLedger(b.settings);b.passengers.Clear();b.enabledCampaign=b.useV2=true;b.preparing=false;s.paused=false;Time.timeScale=1;j.ClearForDay();s.law.ids=new PrototypeIds("proto-"+Guid.NewGuid().ToString("N"));s.law.ledger=new LawLedger();
  int p=j.Catalog.LegacyCount,d=p+1;var o=new JobOffer{pickup=p,drop=d,fare=5000,seconds=600,requiredScore=70,distance=j.nav.Distance(j.stops[p],j.stops[d])};j.offers[0]=o;Pose(p);Check(j.Accept(0),"ordinary candidate accepted");b.ledger.AdvanceOperation(12,120,false);j.trip.Tick(12);j.ForcedRideCase=DrunkRideCase.Normal;Check(j.Interact(),"ordinary passenger boards");
  var comfort=new PassengerRequest();comfort.Begin(s.law.tripId,RequestKind.Comfort);comfort.Accept("부드러운 운행 약속");var eta=new PassengerRequest();eta.Begin(s.law.tripId,RequestKind.Eta);
  var v=new PassengerRequestUISnapshot{version=2,active=1,initialDistance=j.nav.remaining,preferFast=true,boardingLawCount=s.law.ledger.Count,schedule=new PassengerRequestScheduleSnapshot{active=1,awaitingResponse=true,slots=new[]{new PassengerRequestSlot{kind=RequestKind.Comfort,presented=true},new PassengerRequestSlot{kind=RequestKind.Eta,presented=true,threshold=.4f}}},observations=new[]{new PassengerRequestObservationSnapshot{request=comfort.Snapshot(),observedTrip=s.law.tripId,preferFast=true},new PassengerRequestObservationSnapshot{request=eta.Snapshot(),observedTrip=s.law.tripId,preferFast=true}}};s.requests.RestoreLive(v);
  b.ledger.AdvanceOperation(20,200,true);j.trip.Tick(20);var occupancy=s.GetComponent<StopOccupancySystem>();occupancy.Restore(new StopOccupancySnapshot{active=true,eventId="fixture-block",stopId=j.Catalog.Entry(d).StopId,remaining=26});typeof(StopOccupancySystem).GetMethod("Protect",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(occupancy,null);
  Check(occupancy.AffectsActive&&occupancy.Capture().compensated,"occupied destination protected once");float remaining=j.trip.Remaining;typeof(StopOccupancySystem).GetMethod("Protect",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(occupancy,null);Check(j.trip.Remaining==remaining&&j.trip.Fare==5000,"no duplicate extension or repricing");
  Check(s.manners.ComfortAccepted&&s.requests.CaptureLive().observations.Length==2,"first promise still observed with second pending");Check(b.SaveCheckpoint()&&!b.SaveFailed,"real checkpoint write");
  File.WriteAllText(Expected,JsonUtility.ToJson(new ExpectedState{trip=s.law.tripId,remaining=remaining,fare=j.trip.Fare,elapsed=b.ledger.Elapsed}));
 }
 void Reload(){
  var e=JsonUtility.FromJson<ExpectedState>(File.ReadAllText(Expected));var b=s.business;var j=s.jobs;var occupancy=s.GetComponent<StopOccupancySystem>();
  Check(!b.ResumeBlocked&&j.trip.State==TripState.OnTrip&&s.paused,"new process restored paused active transport");Check(s.law.tripId==e.trip&&j.trip.Remaining==e.remaining&&j.trip.Fare==e.fare,"same trip, protected deadline and original fare");
  var requests=s.requests.CaptureLive();Check(requests.version==2&&requests.observations.Length==2&&s.manners.ComfortAccepted&&s.requests.request.Kind==RequestKind.Eta&&!s.requests.request.Resolved,"independent request promises and pending response preserved");
  var metrics=b.ledger.Metrics.Capture();Check(metrics.emptyMetres==120&&metrics.occupiedMetres==200&&b.ledger.Elapsed==e.elapsed,"metrics restored without replay");Check(occupancy.Capture().remaining==26&&occupancy.Capture().compensated,"occupancy timer and one-shot protection restored");
  MapStop alternative;Check(!occupancy.TryGetAlternative(j.current.dropStopId,out alternative),"no unverified alternative");s.paused=false;Time.timeScale=1;Pose(j.current.drop);Check(!j.Interact(),"occupied destination cannot complete");var bay=j.stops[j.current.drop];var waiting=bay.Kerb-bay.Direction*5.5f+Vector3.up*.65f;s.taxi.body.position=waiting;s.taxi.transform.position=waiting;Physics.SyncTransforms();b.ledger.AdvanceOperation(31,0,true);j.trip.Tick(31);typeof(StopOccupancySystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(occupancy,null);Check(occupancy.Capture().remaining==0&&!occupancy.IsOccupied(j.current.drop),"restored occupied bay actually counts down and vehicle leaves with taxi waiting behind");Pose(j.current.drop);Check(j.Interact()&&b.passengers.Count==1,"cleared space completes exactly one transport");j.Interact();Check(b.passengers.Count==1&&b.ledger.Rides==1,"no duplicate fare");Check(b.passengers[0].request.Contains("부드러운 운행")&&b.passengers[0].request.Contains("요청 합계"),"all request records reach settlement");
 }
}
