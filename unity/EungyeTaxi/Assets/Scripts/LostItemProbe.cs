using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Lost items and the emergency passenger, player acceptance (Eungye single, 2026-11-25). Real boarding and delivery
// (stops are posed, not driven). Lost item: returned to the owner at the drop stop (thank-you line), handed in at
// 은행파출소 (small thank-you), or left (the next passenger minds it). Emergency: a call becomes "병원으로 빨리요"
// to the stop in front of the in-map emergency room (은계 응급의료센터, game-only since 2026-11-26); the ride ends there.
public sealed class LostItemProbe : MonoBehaviour {
 const string Flag="-lostCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;int pickupSeed;string error;JobOffer offer;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Lost item acceptance").AddComponent<LostItemProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("LOST_ITEM_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Info(string line){report.AppendLine("INFO "+line);}
 void PoseAt(Vector3 p,Vector3 dir){var t=s.taxi;t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(dir);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();}
 void Pose(MapStop p){PoseAt(p.Kerb,p.Direction);}
 bool Feed(string text){var n=s.GetComponent<DriveNotifications>();return n&&n.Feed.Any(line=>line.Contains(text));}
 int Tips {get{var l=s.business.ledger;return l.Account.Tips(l.Day);}}
 IEnumerator Board(Func<JobOffer,bool> shape){
  var j=s.jobs;int n=j.stops.Length;bool accepted=false;j.ClearForDay();Array.Clear(j.cooldown,0,j.cooldown.Length);
  for(int tries=0;tries<n&&!accepted;tries++){int p=(pickupSeed++*5)%n,d=(p+3)%n;
   offer=new JobOffer{pickup=p,drop=d,fare=6000,seconds=900,requiredScore=60,safeRouteSeconds=120};offer.distance=j.nav.Distance(j.stops[p],j.stops[d]);Pose(j.stops[p]);
   if(shape!=null&&!shape(offer))continue;j.offers[0]=offer;j.ForcedRideCase=DrunkRideCase.Normal;j.ForcedPassengerType=PassengerType.General;accepted=j.Accept(0);}
  try{Check(accepted,"accept a call");for(int i=0;i<4&&j.trip.State==TripState.ToPickup;i++)j.Interact();Check(j.trip.State==TripState.OnTrip,"board");}catch(Exception e){error=e.ToString();}
  yield return null;yield return null;
 }
 IEnumerator Deliver(){
  var j=s.jobs;j.trip.Tick(30);
  {float until=Time.realtimeSinceStartup+25;while(j.trip.State==TripState.OnTrip&&Time.realtimeSinceStartup<until){if(s.requests.HasPendingChoice)s.requests.request.DeferSafety();int target=j.PassengerViaPending?j.PassengerViaStop:j.current.drop;Pose(j.EffectiveStop(target));j.Interact();yield return null;}
   if(j.trip.State==TripState.OnTrip)Info("still aboard after 25 s: "+j.message);}
  yield return null;yield return null;
  if(j.trip.State!=TripState.Idle&&j.trip.State!=TripState.OnTrip)for(int i=0;i<2;i++)j.Interact();
  var panel=s.GetComponent<RideCompletionPanel>();if(panel&&panel.Open)panel.Close();s.paused=false;Time.timeScale=1;yield return null;
 }
 IEnumerator Start(){
  float limit=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limit){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-lostOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/lost");Directory.CreateDirectory(output);
  yield return null;var lost=s.lostItems;var j=s.jobs;var b=s.business;
  try{foreach(var c in FindObjectsOfType<TrafficCar>())c.gameObject.SetActive(false);
   b.settings=PrototypeSettings.ForThreeDays();b.settings.daySeconds=1200;b.ledger=new EconomyLedger(b.settings);b.passengers.Clear();b.enabledCampaign=true;b.useV2=true;b.preparing=false;s.paused=false;Time.timeScale=1;
   j.ClearForDay();s.law.ids=new PrototypeIds(Guid.NewGuid().ToString("N"));s.law.ledger=new LawLedger();s.law.tripId="";b.Story.firstDispatchAccepted=true;
   Check(lost,"the lost item component is attached");Check(LostItemRules.Police!=null&&LostItemRules.Police.name=="은행파출소"&&LostItemRules.Hospital!=null,"real places loaded: "+LostItemRules.Police.name+", "+LostItemRules.Hospital.name);
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // 1) Returned to the owner.
  yield return Board(null);if(error!=null){Finish(error);yield break;}
  yield return Deliver();yield return null;
  var r=b.passengers.LastOrDefault();
  try{Check(r!=null&&r.paid>0,"first ride delivered and paid");if(lost.State==LostItems.None)lost.Begin(r.tripId,r.dropStopId,2);Check(lost.State==LostItems.Pending&&Feed("분실물 · 뒷좌석에"),"an item is left on the back seat ("+LostItemRules.Names[lost.Item]+")");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"lost-item.png"));yield return null;
  int tips=Tips;int item=lost.Item;lost.Choose(1);var owner=j.stops.Select((x,i)=>new{x,i}).First(p=>j.Catalog.Entry(p.i)!=null&&j.Catalog.Entry(p.i).StopId==r.dropStopId).x;
  try{Check(lost.State==LostItems.Returning,"returning to the owner");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  PoseAt(owner.Kerb-owner.Direction*30,owner.Direction);yield return new WaitForSeconds(.3f);Pose(owner);yield return new WaitForSeconds(.5f);
  try{Check(lost.Returned==1&&Tips==tips+LostItemRules.Reward(item),"stopping at the drop stop returns it: +"+LostItemRules.Reward(item)+" ("+tips+" -> "+Tips+")");Check(Feed("돌려드렸어요"),"a card thanks the driver");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // 2) Handed in at the police box.
  lost.Begin("probe-police",r.dropStopId,0);lost.Choose(2);tips=Tips;
  try{Check(lost.State==LostItems.ToPolice,"on the way to "+LostItemRules.Police.name);}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  {var road=lost.PoliceRoad;Info("police road point "+road.ToString("F0")+" ("+Vector3.Distance(new Vector3(road.x,0,road.z),lost.PolicePoint).ToString("0")+" m from the building)");float at;int edge=j.nav.hd.Nearest(road,Vector3.zero,out at);var dir=edge>=0?j.nav.hd.paths[edge].Tangent(at):Vector3.forward;PoseAt(road,dir);}
  yield return new WaitForSeconds(.5f);
  try{Check(lost.Handed==1&&Tips==tips+LostItemRules.PoliceReward,"stopping at the police box hands it in: +"+LostItemRules.PoliceReward);}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // 3) Left: the next passenger minds it.
  lost.Begin("probe-leave",r.dropStopId,1);lost.Choose(3);
  yield return Board(null);if(error!=null){Finish(error);yield break;}
  yield return null;
  try{Check(lost.State==LostItems.Left&&s.manners.RideReasons.Contains("뒷좌석 물건"),"the next passenger minds the item left on the seat");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return Deliver();
  // 4) Emergency passenger.
  j.ClearForDay();yield return null;int hospital=j.HospitalStop;
  try{Check(hospital>=0&&j.EmergencyBlocked()=="","the stop nearest the hospital: "+KoreanText.Stop(j.stops[hospital].name)+", "+Mathf.RoundToInt(j.HospitalLegMetres)+" m from it");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return Board(o=>j.MakeEmergency(o));if(error!=null){Finish(error);yield break;}
  try{Check(j.current.variety==CallBoardRules.Emergency&&j.current.drop==hospital,"an emergency call to the hospital side, fare "+j.current.fare+", deadline "+j.current.seconds);var parts=j.CallCardParts(0,100);}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(4);
  try{Check(!s.hurry.Showing,"no hurry request on the emergency ride");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  double before=b.ledger.Elapsed;yield return Deliver();
  {float until=Time.realtimeSinceStartup+10;while(s.outOfTown.EmergencyLegs==0&&Time.realtimeSinceStartup<until)yield return null;}
  {float until=Time.realtimeSinceStartup+8;while(s.outOfTown.Blackout&&Time.realtimeSinceStartup<until)yield return null;}
  try{Check(s.outOfTown.EmergencyLegs==1&&!s.outOfTown.Blackout&&s.outOfTown.LastOutcome=="응급실 도착","the ride ends at the in-map emergency room, no blackout");Check(b.ledger.Elapsed-before<120,"no out-of-map leg passes on the shift clock ("+(b.ledger.Elapsed-before).ToString("0")+" s)");Check(j.HospitalLegMetres<60,"the drop stop is in front of the emergency room ("+Mathf.RoundToInt(j.HospitalLegMetres)+" m)");Check(Feed("응급실에 모셔다") ||Feed(LostItemRules.Hospital.name),"a card says the passenger reached the hospital");Check(j.EmergencyDoneToday==1&&j.EmergencyBlocked()=="하루 한도","once a day");Info("shift "+before.ToString("0")+" -> "+b.ledger.Elapsed.ToString("0")+" s");}catch(Exception e){error=e.ToString();}
  Finish(error);
 }
 void Finish(string err){
  if(err!=null)Debug.LogError(err);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(err??""));
  Debug.Log("LOST_ITEM_PLAYER_"+(err==null?"OK":"FAIL")+" checks="+checks+" "+(err??""));Application.Quit(err==null?0:1);
 }
}
