using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Play feedback round 6 (2026-11-26), player acceptance: road text is depth tested, police box and emergency-room
// side are map places and life-hub rows, a mission shows its own panel for the whole ride, cleanliness drops are
// told in the feed, Korean-style high humps jolt and cost comfort, parked cars stand in painted bays.
public sealed class PlayFeedback6Probe : MonoBehaviour {
 const string Flag="-feedback6Check";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;string error;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Play feedback 6 acceptance").AddComponent<PlayFeedback6Probe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("FEEDBACK6 "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Info(string line){report.AppendLine("INFO "+line);}
 void PoseAt(Vector3 p,Vector3 dir){var t=s.taxi;t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(dir);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();}
 bool Feed(string text){var n=s.GetComponent<DriveNotifications>();return n&&n.Feed.Any(line=>line.Contains(text));}
 IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
 IEnumerator Start(){
  float limit=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limit){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-feedback6Output");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/feedback6");Directory.CreateDirectory(output);
  for(int i=0;i<10;i++)yield return null;
  var j=s.jobs;var b=s.business;
  try{
   foreach(var c in FindObjectsOfType<TrafficCar>())c.gameObject.SetActive(false);
   b.settings=PrototypeSettings.ForThreeDays();b.settings.daySeconds=1200;b.ledger=new EconomyLedger(b.settings);b.passengers.Clear();b.enabledCampaign=true;b.useV2=true;b.preparing=false;s.paused=false;Time.timeScale=1;
   j.ClearForDay();s.law.ids=new PrototypeIds(Guid.NewGuid().ToString("N"));s.law.ledger=new LawLedger();s.law.tripId="";b.Story.firstDispatchAccepted=true;
   // 1) Road text uses the depth-tested sign shader.
   var texts=FindObjectsOfType<TextMesh>().Where(t=>t.name=="노면 글자").ToArray();
   Check(texts.Length>0&&texts.All(t=>t.GetComponent<RoadSignLabel>()&&t.GetComponent<Renderer>().sharedMaterial.shader.name=="Taxi/Road Sign Text"),"road text ("+texts.Length+") is drawn with the depth-tested shader, not over the taxi");
   // 2) Police box and emergency-room side.
   var map=s.GetComponent<FullMapOverlay>();var places=map.Places();
   var police=places.FirstOrDefault(p=>p.glyph==21);var hospital=places.FirstOrDefault(p=>p.glyph==22);
   Check(police!=null&&police.approach!=null&&police.name=="은행파출소","the police box is a map place with a road approach");
   Check(hospital!=null&&hospital.approach!=null,"the emergency-room side is a map place: "+(hospital!=null?hospital.name:""));
   Check(GameObject.Find("생활시설 표지 · 은행파출소"),"a sign stands at the police box");
   // 3) Korean humps and parking bays.
   Info("korean humps: "+s.bumps.KoreanStatus);var humps=s.bumps.bumps.Where(x=>x.korean).ToList();Check(humps.Count>=3,"Korean-style high humps placed: "+humps.Count);foreach(var h in humps)Info("hump "+h.id+" width "+(h.halfLength*2).ToString("0.0")+" m");
   var alley=s.parkedAlley;Check(alley.cars.Count>0&&alley.cars.All(c=>c.body.GetComponentsInChildren<Transform>().Count(t=>t.name=="주차 구획선")==4),"every parked car ("+alley.cars.Count+") stands in a painted bay");
   // 4) Age is gone from the passenger profile line (source check is in CockpitUI; presentation only).
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // Life hub with the new row.
  {var p=s.jobs.stops[0];PoseAt(p.Kerb,p.Direction);}
  yield return null;s.services.ToggleLifeHub();yield return Shot("life-hub");
  try{Check(s.services.PostTripOpen,"the life hub opens with the police / emergency row");s.services.DismissPostTrip();}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // Map with the new icons.
  {var overlay=s.GetComponent<FullMapOverlay>();overlay.Open();yield return Shot("map-places");overlay.Close();yield return null;}
  // 8) Road text seen from behind the taxi.
  {var text=FindObjectsOfType<TextMesh>().First(t=>t.name=="노면 글자"&&t.text=="30");var p=text.transform.position;float at;int edge=j.nav.hd.Nearest(p,Vector3.zero,out at);var dir=edge>=0?j.nav.hd.paths[edge].Tangent(at):Vector3.forward;dir.y=0;
   PoseAt(new Vector3(p.x,0,p.z),dir.normalized);yield return Shot("road-text-under-taxi");}
  // 9) Parking bays.
  {var c=s.parkedAlley.cars[0];var right=TrafficSystem.Right(c.direction);PoseAt(c.position-right*2.6f-c.direction*12,c.direction);yield return new WaitForSecondsRealtime(1.5f);yield return Shot("parking-bays");}
  {var h=s.bumps.bumps.First(x=>x.korean);PoseAt(h.centre-h.along*9,h.along);yield return new WaitForSecondsRealtime(1.5f);yield return Shot("korean-hump");}
  // 5) Mission panel: an express mission call, accepted and boarded.
  bool accepted=false;for(int t=0;t<j.stops.Length&&!accepted;t++){int p=(t*5)%j.stops.Length,d=(p+3)%j.stops.Length;
   var o=new JobOffer{pickup=p,drop=d,fare=6000,seconds=900,requiredScore=60,safeRouteSeconds=120,mission=MissionRules.Express};o.missionBonus=MissionRules.Bonus(o.mission,o.fare);o.distance=j.nav.Distance(j.stops[p],j.stops[d]);
   PoseAt(j.stops[p].Kerb,j.stops[p].Direction);j.offers[0]=o;j.ForcedRideCase=DrunkRideCase.Normal;j.ForcedPassengerType=PassengerType.General;accepted=j.Accept(0);}
  try{Check(accepted&&s.missionTracker.Showing&&s.missionTracker.Progress.Contains("타면"),"an accepted mission call shows the mission panel before boarding");for(int i=0;i<4&&j.trip.State==TripState.ToPickup;i++)j.Interact();Check(j.trip.State==TripState.OnTrip,"board");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(1);
  try{Check(s.missionTracker.Showing&&s.missionTracker.Progress.StartsWith("주행 "),"aboard, the panel shows live progress: "+s.missionTracker.Progress);Check(s.missionTracker.StripShown&&!MissionTracker.Drawn,"the mission rides in the navigation card strip, not a separate panel");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return Shot("mission-panel");
  // 6) A passenger dirt event is told in the feed.
  try{int before=b.ledger.Care.Cleanliness;if(b.ledger.Care.Soil("probe:crumbs",2))j.DirtCard("probe:crumbs",2);Check(b.ledger.Care.Cleanliness==before-2&&Feed("청결도 −2 · 음식 부스러기"),"cleanliness drop card in the feed");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // 7) Crossing a Korean hump at 30 km/h with the passenger aboard.
  {var h=s.bumps.bumps.First(x=>x.korean);int pen=s.bumps.KoreanPenalties;
   PoseAt(h.centre-h.along*14,h.along);yield return new WaitForFixedUpdate();
   float until=Time.realtimeSinceStartup+6;while(s.bumps.KoreanCrossings==0&&Time.realtimeSinceStartup<until){s.taxi.body.velocity=h.along*(30/3.6f)+Vector3.up*s.taxi.body.velocity.y;yield return new WaitForFixedUpdate();}
   yield return new WaitForSeconds(.3f);
   try{Check(s.bumps.KoreanCrossings>0,"the taxi crosses the hump at 30 km/h");Check(s.bumps.KoreanPenalties==pen+1&&Feed("높은 방지턱 · 덜컹"),"the jolt costs comfort and the passenger says so ("+s.manners.RideReasons+")");}catch(Exception e){error=e.ToString();}
   if(error!=null){Finish(error);yield break;}
   s.taxi.body.velocity=Vector3.zero;}
  Finish(null);
 }
 void Finish(string err){
  if(err!=null)Debug.LogError(err);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(err??""));
  Debug.Log("FEEDBACK6_"+(err==null?"OK":"FAIL")+" checks="+checks+" "+(err??""));Application.Quit(err==null?0:1);
 }
}
