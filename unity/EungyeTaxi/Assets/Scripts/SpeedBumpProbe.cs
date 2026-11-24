using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Speed bumps, player acceptance (Eungye single, 2026-11-24). The five bumps of the road facility input are drawn;
// with a passenger aboard, 40 km/h over a bump costs comfort points under the "bump" key and a passenger line,
// 18 km/h does not; braking for a bump ahead is required braking; without a passenger nothing is deducted;
// no accident or damage. The taxi is moved along the real lane through each bump.
public sealed class SpeedBumpProbe : MonoBehaviour {
 const string Flag="-bumpCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Speed bump acceptance").AddComponent<SpeedBumpProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("SPEED_BUMP_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Info(string line){report.AppendLine("INFO "+line);}
 void PoseStop(MapStop p){s.taxi.body.velocity=Vector3.zero;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=p.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(p.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();}
 string Money(){var l=s.business.ledger;return l.Account.Expense(l.Day,"accident")+"|"+l.Account.Expense(l.Day,"repair")+"|"+l.Account.Cash+"|"+l.Care.Damage.Durability;}
 bool Feed(string text){var n=s.GetComponent<DriveNotifications>();return n&&n.Feed.Any(line=>line.Contains(text));}
 // Moves the taxi along the lane nearest the bump, from 40 m before to 10 m past it, at the given speed.
 IEnumerator Cross(SpeedBumps.Bump bump,float kmh,Action<bool> braking){
  var g=s.jobs.nav.hd;float at;int edge=g.Nearest(bump.centre,bump.along,out at);bool flip=false;if(edge<0){edge=g.Nearest(bump.centre,-bump.along,out at);flip=true;}
  var path=g.paths[edge];var dir=path.Tangent(at);float start=Mathf.Max(0,at-40),end=Mathf.Min(path.length,at+10);Info(bump.id+" lane "+g.edges[edge].id+" at "+at.ToString("0")+"/"+path.length.ToString("0")+(flip?" (reverse)":""));
  var t=s.taxi;var wait=new WaitForFixedUpdate();bool sawBrake=false;
  t.body.velocity=Vector3.zero;t.body.position=path.Position(start)+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(path.Tangent(start));t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();
  yield return new WaitForSeconds(.4f);float v=kmh/3.6f;
  for(float d=start;d<end;d+=v*Time.fixedDeltaTime){var tan=path.Tangent(d);tan.y=0;tan.Normalize();var p=path.Position(d)+Vector3.up*.65f;p.y=Mathf.Max(p.y,t.body.position.y-.2f);t.body.MovePosition(p);t.body.MoveRotation(Quaternion.LookRotation(tan));var vel=tan*v;vel.y=t.body.velocity.y;t.body.velocity=vel;
   if(at-d>3&&at-d<10&&s.manners.NecessaryBraking())sawBrake=true;yield return wait;}
  t.body.velocity=Vector3.zero;yield return new WaitForSeconds(.4f);braking(sawBrake);
 }
 IEnumerator Start(){
  float limit=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limit){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-bumpOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/bump");Directory.CreateDirectory(output);
  yield return null;string error=null;var j=s.jobs;var sb=s.bumps;
  try{s.business.preparing=false;s.paused=false;Time.timeScale=1;j.ClearForDay();foreach(var c in FindObjectsOfType<TrafficCar>())c.gameObject.SetActive(false);
   Check(sb&&sb.bumps.Count==2,"two bumps from the road facility input ("+(sb?sb.bumps.Count:0)+")");
   Check(sb.bumps.All(b=>GameObject.Find("과속방지턱 "+b.id)),"each bump is drawn");
   Check(sb.bumps.All(b=>{var x=GameObject.Find("과속방지턱 "+b.id);return x.GetComponentsInChildren<Collider>().Length==0;}),"bumps have no collider (never a property accident)");
   // Board a passenger.
   bool accepted=false;for(int p=0;p<j.stops.Length&&!accepted;p++){var o=new JobOffer{pickup=p,drop=(p+3)%j.stops.Length,fare=6000,seconds=900,requiredScore=60,safeRouteSeconds=120};o.distance=j.nav.Distance(j.stops[p],j.stops[o.drop]);j.offers[0]=o;j.ForcedRideCase=DrunkRideCase.Normal;j.ForcedPassengerType=PassengerType.General;PoseStop(j.stops[p]);accepted=j.Accept(0);}
   Check(accepted,"a call is accepted");for(int i=0;i<4&&j.trip.State==TripState.ToPickup;i++)j.Interact();Check(j.trip.State==TripState.OnTrip,"passenger aboard");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  var p12=sb.bumps.First(b=>b.id=="speed-bump-p12");var p24=sb.bumps.First(b=>b.id=="speed-bump-p24");var ngii=p12;// NGII side-road bumps were turned off; the no-passenger crossing reuses P12.
  string before=Money();int pen=sb.Penalties,cross=sb.Crossings;bool brake=false;
  yield return Cross(p12,40,x=>brake=x);
  try{Check(sb.Crossings==cross+1&&sb.LastBump==p12.id,"40 km/h over P12 is detected ("+sb.LastKmh.ToString("0")+" km/h)");Check(sb.Penalties==pen+1&&s.manners.lastEvent.Contains("방지턱"),"with a passenger: comfort penalty under its own key ("+s.manners.lastEvent+")");Check(Feed("방지턱 · "),"a passenger line");Check(brake,"a bump ahead counts as required braking");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"bump.png"));yield return null;
  pen=sb.Penalties;yield return Cross(p24,18,x=>{});
  try{Check(sb.LastBump==p24.id&&sb.Penalties==pen,"18 km/h over P24: no penalty");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  j.ClearForDay();yield return null;pen=sb.Penalties;
  yield return Cross(ngii,45,x=>{});
  try{Check(sb.LastBump==ngii.id&&sb.Penalties==pen,"no passenger: no penalty (P12 at 45 km/h)");Check(Money()==before&&!s.accident.Locked,"no accident, repair or durability change ("+before+" -> "+Money()+")");}catch(Exception e){error=e.ToString();}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("SPEED_BUMP_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
