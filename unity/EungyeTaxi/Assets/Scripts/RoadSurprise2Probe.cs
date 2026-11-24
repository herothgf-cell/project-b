using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Road surprises, second batch, player acceptance (Eungye single, 2026-11-24). The probe drives the taxi by
// setting its velocity and places the scenes.
// A) kick scooter on a 번길 street: visible and ringing before it rolls out; the taxi stops -> "먼저 보냄", no accident.
// B) kick scooter touching a braking taxi right after rolling out -> person accident with relief (half, no major).
// C) ambulance across a junction the taxi approaches on green: traffic holds; the taxi stops -> "양보", no record.
// D) ambulance again, the taxi drives into the junction -> "긴급차 양보 위반" law record.
public sealed class RoadSurprise2Probe : MonoBehaviour {
 const string Flag="-surprise2Check";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Road surprise 2 acceptance").AddComponent<RoadSurprise2Probe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("ROADSURPRISE2_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Info(string line){report.AppendLine("INFO "+line);Debug.Log("ROADSURPRISE2_PLAYER INFO "+line);}
 void Pose(Vector3 p,Vector3 heading){var t=s.taxi;t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(heading);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();}
 string Money(){var l=s.business.ledger;return l.Account.Expense(l.Day,"accident")+"|"+l.Account.Cash+"|"+s.law.ledger.Count+"|"+s.law.ledger.TotalScore+"|"+l.Care.Damage.Durability;}
 void Drive(Vector3 dir,float speed){var v=dir*speed;v.y=s.taxi.body.velocity.y;s.taxi.body.velocity=v;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.rotation=Quaternion.LookRotation(dir);}
 bool Feed(string text){var n=s.GetComponent<DriveNotifications>();return n&&n.Feed.Any(line=>line.Contains(text));}
 static bool Ordinary(HdEdge e){return e.change==0&&e.turn==0&&!e.TraversalDisabled&&!e.id.StartsWith("join:");}
 bool ScooterScene(int skip,out Vector3 start,out Vector3 dir,out Vector3 from,out Vector3 to,out Vector3 crossing){
  var g=s.jobs.nav.hd;start=dir=from=to=crossing=Vector3.zero;int found=0;
  for(int i=0;i<g.edges.Length;i++){var e=g.edges[i];if(!Ordinary(e)||g.paths[i].length<90||string.IsNullOrEmpty(e.roadName)||!e.roadName.EndsWith("번길"))continue;
   var p=g.paths[i].Position(8);var d=g.paths[i].Tangent(8);d.y=0;d.Normalize();Pose(p,d);
   if(!s.surprises.FindScooterSpot(35,70,out from,out to,out crossing))continue;
   var toward=crossing-p;toward.y=0;if(Vector3.Angle(toward,d)>3)continue;
   bool limited;if(s.traffic.RoadSpeedLimit(crossing,d,out limited)>0)continue; // outside school zones: no limit to exceed
   if(found++<skip)continue;start=p;dir=toward.normalized;Pose(start,dir);Info("scooter scene on "+e.roadName+" crossing "+crossing.ToString("F1"));return true;}
  return false;
 }
 IEnumerator Start(){
  float limit=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limit){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-surprise2Output");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/surprise2");Directory.CreateDirectory(output);
  yield return null;string error=null;var sp=s.surprises;var wait=new WaitForFixedUpdate();Vector3 start=Vector3.zero,dir=Vector3.forward,from=Vector3.zero,to=Vector3.zero,crossing=Vector3.zero;string before;
  try{s.business.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();s.jobs.nav.Clear();
   foreach(var c in FindObjectsOfType<TrafficCar>())c.gameObject.SetActive(false);
   Check(ScooterScene(0,out start,out dir,out from,out to,out crossing),"a 번길 crossing for the scooter is found");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // A) scooter, the taxi stops.
  sp.SpawnScooter(from,to,crossing);yield return new WaitForSeconds(.3f);before=Money();int avoided=sp.Avoided;float visibleAlong=-1,startAlong=-1;
  {float until=Time.time+15;while(Time.time<until&&!sp.ScooterMoving){Drive(dir,8);if(visibleAlong<0&&sp.ScooterRider&&sp.ScooterRider.activeSelf)visibleAlong=Vector3.Dot(crossing-s.taxi.transform.position,dir);yield return wait;}
   startAlong=Vector3.Dot(crossing-s.taxi.transform.position,dir);}
  {float v=8;while(v>0){v=Mathf.Max(0,v-5*Time.fixedDeltaTime);Drive(dir,v);yield return wait;}}
  yield return new WaitForSeconds(.2f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"scooter.png"));
  {float until=Time.time+10;while(Time.time<until&&sp.Avoided==avoided&&sp.Touched==0)yield return null;}
  try{
   Check(visibleAlong>startAlong+4,"the rider is visible (and rings) before rolling out (visible at "+visibleAlong.ToString("0.0")+" m, rolls out at "+startAlong.ToString("0.0")+" m)");
   Check(startAlong>=18&&startAlong<=26,"it rolls out about 22 m ahead");
   Check(sp.Avoided==avoided+1&&sp.LastOutcome=="먼저 보냄","stopping lets it cross ("+sp.LastOutcome+")");
   Check(Feed("킥보드를 먼저 보냈어요"),"a card tells the driver");
   Check(!s.accident.Locked&&Money()==before,"no accident or ledger change");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return new WaitForSeconds(2.5f);
  // B) scooter rolls out right in front of a braking taxi: person accident, relieved.
  try{Check(ScooterScene(0,out start,out dir,out from,out to,out crossing),"second scooter scene");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  {var right=TrafficSystem.Right(dir);var lanePoint=crossing;var p=lanePoint-dir*3.2f;Pose(p,dir);}
  sp.SpawnScooter(from,to,crossing);int touched=sp.Touched;
  {float v=4;float until=Time.time+5;while(Time.time<until&&sp.Touched==touched&&!s.accident.Locked){v=Mathf.Max(0,v-4*Time.fixedDeltaTime);Drive(dir,v);yield return wait;}}
  {float settle=Time.realtimeSinceStartup+8;while(s.accident.State!=null&&s.accident.State.phase==AccidentPhase.Stopping&&Time.realtimeSinceStartup<settle)yield return null;}
  yield return new WaitForSecondsRealtime(.5f);
  try{
   Check(sp.Touched==touched+1,"the rider touches the taxi ("+sp.LastOutcome+")");
   Check(s.accident.Locked&&s.accident.State.Kind==AccidentKind.Person,"a person accident opens");
   Check(sp.LastScooterRelief&&s.accident.State.Shared,"relieved: rolled out within the reaction time, not over the limit, braking");
   Check(s.accident.State.Severity!=AccidentSeverity.Major,"no major grade under relief ("+s.accident.State.Severity+")");
   Info("person accident fee "+s.accident.State.Fee+" (selected "+s.accident.State.selectedFee+"), penalty "+s.accident.State.ScorePenalty);
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // Clear the accident for the ambulance scenes: a fresh state, as the accident probes do between scenes.
  s.accident.Restore(null);s.paused=false;Time.timeScale=1;sp.ResetForDay();yield return new WaitForSeconds(1.2f);
  // C) ambulance: find a junction and a green approach.
  TrafficJunction j=null;int axis=0,sign=0;Vector3 jdir=Vector3.zero,jstart=Vector3.zero;s.traffic.manualClock=true;
  var g=s.jobs.nav.hd;var tried=new System.Collections.Generic.Dictionary<string,int>();var rejects=new System.Collections.Generic.List<string>();
  foreach(var cand in s.traffic.junctions){if(!cand.signalized)continue;
   for(int ax=0;ax<2&&j==null;ax++)foreach(int sg in new[]{1,-1}){var d=s.traffic.AxisDirection(cand,ax)*sg;d.y=0;d.Normalize();var p=cand.center-d*70+TrafficSystem.Right(d)*1.75f;float at;int edge=g.Nearest(p,d,out at);if(edge<0||!Ordinary(g.edges[edge]))continue;
    var lane=g.paths[edge].Position(at);var ld=g.paths[edge].Tangent(at);ld.y=0;ld.Normalize();if(Vector3.Angle(ld,d)>6)continue;
    Pose(lane,d);int a2,s2;float ahead;var found=sp.JunctionAhead(out a2,out s2,out ahead);if(found!=cand){tried["ahead"]=tried.TryGetValue("ahead",out var n1)?n1+1:1;continue;}
    bool green=false;for(float t=0;t<240&&!green;t+=1){s.traffic.manualTime=t;green=s.traffic.Signal(cand,a2,s2)==SignalColor.Green&&s.traffic.Signal(cand,a2,s2)==SignalColor.Green;}
    if(!green){tried["green"]=tried.TryGetValue("green",out var n2)?n2+1:1;continue;}s.traffic.manualTime+=2;if(!sp.TryAmbulance(true)){tried["refused"]=tried.TryGetValue("refused",out var n3)?n3+1:1;if(rejects.Count<8)rejects.Add(cand.name+" "+sp.AmbulanceReject);continue;}
    j=cand;axis=a2;sign=s2;jdir=d;jstart=lane;break;}
   if(j!=null)break;}
  Info("ambulance search: "+string.Join(", ",tried.Select(kv=>kv.Key+" "+kv.Value))+" | "+string.Join(" ; ",rejects));
  try{Check(j!=null,"an ambulance crosses a junction the taxi approaches on green");Info("junction "+j.name+" side "+sp.AmbulanceSide);}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  before=Money();avoided=sp.Avoided;
  try{Check(s.traffic.emergencyHold==j&&sp.AmbulanceBody&&sp.AmbulanceBody.GetComponent<AudioSource>().isPlaying,"ordinary cars hold at the junction, the siren sounds");Check(Feed("구급차 접근"),"a card warns which side it comes from");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  {float half=Mathf.Max(j.halfWidth[0],j.halfWidth[1]);float v=7;float stopAt=half+10;float until=Time.time+20;
   while(Time.time<until&&sp.AmbulanceBody){float dist=Vector3.Dot(j.center-s.taxi.transform.position,jdir);if(dist<stopAt+6)v=Mathf.Max(0,v-6*Time.fixedDeltaTime);Drive(jdir,v);yield return wait;if(sp.LastOutcome=="양보"||sp.LastOutcome=="양보 위반")break;}}
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"ambulance.png"));yield return null;
  try{Check(sp.LastOutcome=="양보"&&!sp.AmbulanceViolation,"waiting before the junction is a yield ("+sp.LastOutcome+")");Check(Feed("구급차에 길을 비켜 줬어요"),"a card tells the driver");Check(Money()==before,"no law record or ledger change");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  {float until=Time.time+10;while(sp.Busy&&Time.time<until)yield return null;}
  try{Check(s.traffic.emergencyHold==null||Time.time>=s.traffic.emergencyHoldUntil,"the hold ends after the ambulance");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // D) again, the taxi goes in.
  Pose(jstart,jdir);yield return new WaitForSeconds(.5f);int lawBefore=s.law.ledger.Count;int scoreBefore=s.law.ledger.TotalScore;
  bool spawned=sp.TryAmbulance(true);
  try{Check(spawned,"a second ambulance");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  {float until=Time.time+12;while(Time.time<until&&!sp.AmbulanceViolation&&!s.accident.Locked){Drive(jdir,9);yield return wait;}Drive(jdir,0);}
  yield return new WaitForSecondsRealtime(.5f);
  try{
   Check(sp.AmbulanceViolation,"entering the junction before the ambulance is a violation");
   var events=s.law.ledger.Events;Check(s.law.ledger.Count>lawBefore&&events.Any(e=>e.action=="긴급차 양보 위반"),"law record 긴급차 양보 위반");
   Check(s.law.ledger.TotalScore<=scoreBefore-SurpriseRules.YieldScorePenalty+0,"driving score -"+SurpriseRules.YieldScorePenalty+" ("+scoreBefore+" -> "+s.law.ledger.TotalScore+")");
  }catch(Exception e){error=e.ToString();}
  s.traffic.manualClock=false;
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("ROADSURPRISE2_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
