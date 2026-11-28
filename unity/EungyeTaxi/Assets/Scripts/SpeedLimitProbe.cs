using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Eungye road speed limits, player acceptance (2026-11-24). The taxi is posed on real lanes and the limit the
// game reports is compared with the surveyed values; then the taxi is held at a fixed speed on 수인로 to see
// that staying inside the tolerance records nothing and going past it records once per repeat window.
// The speed is forced by the probe, so this checks the rule, not how driving under a limit feels.
public sealed class SpeedLimitProbe : MonoBehaviour {
 const string Flag="-speedLimitCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Speed limit acceptance").AddComponent<SpeedLimitProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("SPEEDLIMIT_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 // Limit at a map point, trying headings until a lane accepts one. Returns the heading that matched.
 float LimitAt(float x,float z,out Vector3 heading,out bool table){
  var p=new Vector3(x,0,z);table=false;heading=Vector3.forward;
  for(int i=0;i<24;i++){var h=Quaternion.Euler(0,i*15,0)*Vector3.forward;bool t;float limit=s.traffic.RoadSpeedLimit(p,h,out t);if(limit>0){heading=h;table=t;return limit;}}
  return 0;
 }
 // Direction of the lane under a point: the middle of the run of headings the lane lookup accepts.
 Vector3 LaneHeading(float x,float z,Vector3 near){
  var p=new Vector3(x,0,z);float start=Mathf.Atan2(near.x,near.z)*Mathf.Rad2Deg;int low=0,high=0;bool t;
  while(low>-90&&s.traffic.RoadSpeedLimit(p,Quaternion.Euler(0,start+low-1,0)*Vector3.forward,out t)>0)low--;
  while(high<90&&s.traffic.RoadSpeedLimit(p,Quaternion.Euler(0,start+high+1,0)*Vector3.forward,out t)>0)high++;
  return Quaternion.Euler(0,start+(low+high)*.5f,0)*Vector3.forward;
 }
 // Only speed-limit records count here: the forced run also crosses a signalled junction, which the law ledger records on its own.
 int Speeding(){return s.law.ledger.Events.Count(e=>e.family=="제한속도");}
 string Others(){return string.Join(", ",s.law.ledger.Events.Where(e=>e.family!="제한속도").Select(e=>e.family+"/"+e.action).ToArray());}
 void Pose(Vector3 p,Vector3 heading){var t=s.taxi;t.body.velocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;t.body.position=p+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(heading);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();}
 IEnumerator Hold(Vector3 heading,float kmh,float seconds){
  float until=Time.time+seconds,give=Time.realtimeSinceStartup+seconds*4+5;object wait=null;
  while(Time.time<until){if(Time.realtimeSinceStartup>give){report.AppendLine("INFO hold stalled: timeScale="+Time.timeScale+" paused="+s.paused+" summary="+s.business.ShowingSummary+" accident="+(s.accident?s.accident.State.phase.ToString():"-")+" notice="+s.business.notice);yield break;}var v=heading*(kmh/3.6f);v.y=s.taxi.body.velocity.y;s.taxi.body.velocity=v;s.taxi.body.angularVelocity=Vector3.zero;yield return wait;}
 }
 IEnumerator Start(){
  float limitTime=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limitTime){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-speedLimitOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/speedlimit");Directory.CreateDirectory(output);
  yield return null;string error=null;var b=s.business;Vector3 suin=Vector3.forward;bool table;Vector3 h;
  try{
   b.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();
   Check(s.traffic.EungyeSingle&&RoadSpeedLimits.Enabled,"Eungye single with road limits on");
   Check(RoadSpeedLimits.Data!=null&&RoadSpeedLimits.Data.zones.Length==13,"limit file loaded in the player");
   Check(LimitAt(450,450,out suin,out table)==60&&table,"수인로 at the camera point (450,450) reports 60");
   Check(LimitAt(782,384,out h,out table)==60,"수인로 at the second camera point reports 60");
   Check(LimitAt(442,1342,out h,out table)==0,"대은로 (inactive since round 4) has no limit");
   Check(LimitAt(872,1541,out h,out table)==0,"계수로 (unconfirmed) has no limit by default");
   Check(LimitAt(1235,726,out h,out table)==0,"마유로 (unconfirmed) has no limit by default");
   Check(LimitAt(487,1113,out h,out table)==0,"은행로144번길 (alley estimate) has no limit by default");
   Check(LimitAt(796.3f,104.3f,out h,out table)==30,"은계로 at 은빛초 (school zone) reports 30");
   Check(LimitAt(821,823,out h,out table)==0,"은계로 away from schools has no limit by default");
   Check(LimitAt(1097,685,out h,out table)==30,"은계중앙로 at 은계유치원 (school zone) reports 30");
   Check(LimitAt(534,435,out h,out table)==60,"수인로 turn pocket without a road name (534,435) still reports 60");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // Ordinary cars: let them run, then read their speed against the limit of the lane they are on.
  yield return new WaitForSeconds(25);
  try{
   var cars=FindObjectsOfType<TrafficCar>().Where(c=>c.isActiveAndEnabled&&c.Visible).ToArray();int known=0,fast=0;float worst=0,top=0;string worstCar="";
   foreach(var c in cars){float limit=s.traffic.RoadSpeedLimit(c);if(limit<=0)continue;known++;float kmh=c.speed*3.6f;top=Mathf.Max(top,kmh);if(kmh-limit>worst)worstCar=c.name+" at "+c.transform.position.x.ToString("0")+","+c.transform.position.z.ToString("0")+" "+kmh.ToString("0.0")+"km/h on a "+limit.ToString("0")+" lane";worst=Mathf.Max(worst,kmh-limit);if(limit>=60&&kmh>13f*3.6f+1)fast++;}
   report.AppendLine("INFO traffic cars="+cars.Length+" with a limit="+known+" fastest="+top.ToString("0.0")+"km/h largest (speed-limit)="+worst.ToString("0.0")+"km/h cars above the old 47km/h cap on a 60 road="+fast+" worst="+worstCar);
   Check(cars.Length>0&&known>0,"ordinary cars have a limit on their lane ("+known+"/"+cars.Length+")");
   Check(worst<=.5f,"no ordinary car is above the limit of its lane (largest excess "+worst.ToString("0.0")+" km/h)");
   foreach(var c in FindObjectsOfType<TrafficCar>())c.gameObject.SetActive(false);
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // Judgement on 수인로 (limit 60, tolerance 10, 3 s, repeat 20 s).
  suin=LaneHeading(450.2f,450.3f,suin);var start=new Vector3(450.2f,0,450.3f);int before=Speeding();
  // The forced runs are short and re-posed so the taxi stays on the straight before the next junction.
  if(s.accident)s.accident.Suppress(120);
  Pose(start,suin);yield return new WaitForSeconds(.6f);
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"speed-limit.png"));yield return null;
  yield return Hold(suin,68,4.5f);
  try{Check(s.law.CurrentLimit==60,"the taxi reads 60 on 수인로 (read "+s.law.CurrentLimit+" at "+s.taxi.transform.position.x.ToString("0")+","+s.taxi.transform.position.z.ToString("0")+")");Check(Speeding()==before,"68 km/h for 4.5 s is inside the 10 km/h tolerance: nothing recorded");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  Pose(start,suin);yield return Hold(suin,75,2f);yield return Hold(suin,40,1f);
  try{Check(Speeding()==before,"75 km/h for 2 s is shorter than 3 s: nothing recorded");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  Pose(start,suin);yield return Hold(suin,75,2f);Pose(start,suin);yield return Hold(suin,75,1.6f);
  try{
   Check(Speeding()==before+1,"75 km/h held past 3 s records one violation");
   var last=s.law.ledger.Events.Last(e=>e.family=="제한속도");Check(last.Deduction==RoadSpeedLimits.Penalty&&last.reason.Contains("60"),"the record takes 5 points and names the 60 limit ("+last.reason+")");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // Ease off, then go over again inside the repeat window.
  report.AppendLine("INFO before the repeat run: timeScale="+Time.timeScale+" paused="+s.paused+" summary="+s.business.ShowingSummary+" accident="+(s.accident?s.accident.State.phase.ToString():"-"));
  Pose(start,suin);yield return Hold(suin,50,1f);yield return Hold(suin,78,2.2f);Pose(start,suin);yield return Hold(suin,78,2.3f);
  try{Check(Speeding()==before+1,"a second excess inside 20 s is not recorded again");}catch(Exception e){error=e.ToString();}
  report.AppendLine("INFO other law records during the forced run: "+Others());
  // Tolerance band (2026-11-24 round 2): a crash at 65 on a 60 road adds the crash record.
  if(error==null)try{Pose(start,suin);int crashes=s.law.ledger.Events.Count(e=>e.action=="제한속도 초과 중 사고");
   Check(s.law.CurrentLimit==60&&s.law.RecordSpeedingCrash(65/3.6f),"a crash over the limit is recorded");
   var crash=s.law.ledger.Events.Last(e=>e.action=="제한속도 초과 중 사고");Check(s.law.ledger.Events.Count(e=>e.action=="제한속도 초과 중 사고")==crashes+1&&crash.Deduction==RoadSpeedLimits.CrashPenalty,"the crash record takes "+RoadSpeedLimits.CrashPenalty+" points");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // Round 3 (2026-11-24): limited sections are marked on the road and the map, and entering one shows a notice.
  SpeedLimitSections sec=null;SpeedLimitSigns signs=null;SpeedLimitSections.Mark entry=default(SpeedLimitSections.Mark);
  try{
   sec=SpeedLimitSections.For(s.traffic);signs=s.GetComponent<SpeedLimitSigns>();
   Check(sec!=null&&sec.segments.Any(x=>x.school&&x.kmh==30)&&sec.segments.Any(x=>!x.school&&x.kmh==60)&&!sec.segments.Any(x=>!x.school&&x.kmh==50),"map sections: school 30 and 수인로 60 stretches, no 대은로 50 ("+(sec==null?0:sec.segments.Count)+" segments)");
   Check(sec.signs.Count>0&&sec.signs.All(m=>m.from!=m.to)&&sec.signs.Any(m=>m.to==0)&&sec.signs.Any(m=>m.school&&m.to==30),"section signs: starts and ends, each a real change ("+sec.signs.Count+" signs, "+sec.marks.Count+" lane marks)");
   Check(signs&&signs.SignCount+signs.Skipped==sec.signs.Count&&signs.SignCount>=sec.signs.Count*.7f,"road-side signs built ("+(signs?signs.SignCount:0)+" of "+sec.signs.Count+", skipped without sidewalk "+(signs?signs.Skipped:0)+")");
   // Round 5: every sign stands on the sidewalk, off the carriageway and clear of crosswalks; no red paint.
   int offWalk=-1;if(signs){offWalk=0;for(int i=0;i<signs.Placed.Count;i++)if(!SpeedLimitSigns.Sidewalk(s.world.map,signs.Placed[i])||!SpeedLimitSigns.ClearOfCrossings(s.world.map,s.traffic,signs.Placed[i],signs.PlacedForward[i]))offWalk++;}
   Check(offWalk==0,"signs on the sidewalk, clear of crosswalks ("+offWalk+" off)");
   Check(GameObject.Find("어린이보호구역 적색 포장")==null,"no red school-zone paint");
   var zone=RoadSpeedLimits.Data.zones.OrderBy(z=>(z.x-796.3f)*(z.x-796.3f)+(z.z-104.3f)*(z.z-104.3f)).First();
   entry=sec.signs.Where(m=>m.school&&m.to==30&&m.from==0).OrderBy(m=>(new Vector2(m.position.x-zone.x,m.position.z-zone.z)).sqrMagnitude).First();
   report.AppendLine("INFO 은빛초 entry sign at "+entry.position.x.ToString("0.0")+","+entry.position.z.ToString("0.0")+" heading "+entry.forward.x.ToString("0.00")+","+entry.forward.z.ToString("0.00"));
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  Pose(entry.position-entry.forward*22,entry.forward);yield return new WaitForSeconds(1.2f);
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"school-zone-entry.png"));yield return null;
  // Round 5 captures: a section end (해제표지) and a road limit start, seen from the lane 20 m before each.
  foreach(var pick in new[]{"end","road"}){
   var list=signs.PlacedMarks.Where(m=>pick=="end"?m.to<=0&&m.school:m.to>30).ToList();if(list.Count==0){report.AppendLine("INFO no "+pick+" sign placed");continue;}
   var at=list.OrderBy(m=>(new Vector2(m.position.x-entry.position.x,m.position.z-entry.position.z)).sqrMagnitude).First();
   Pose(at.position-at.forward*20,at.forward);yield return new WaitForSeconds(1.2f);
   ScreenCapture.CaptureScreenshot(Path.Combine(output,"sign-"+pick+".png"));yield return null;yield return null;}
  // School zones reach 40 m since round 4, so the lane inside can be short: step the taxi across the sign instead of driving it.
  Pose(entry.position-entry.forward*12,entry.forward);yield return new WaitForSeconds(1f);Pose(entry.position+entry.forward*4,entry.forward);yield return new WaitForSeconds(1.5f);
  try{Check(s.law.CurrentLimit==30,"driving past the entry sign reads 30 (read "+s.law.CurrentLimit+")");Check(signs.Notice.Contains("어린이보호구역"),"entering shows the school-zone notice ("+signs.Notice+")");}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"school-zone-notice.png"));yield return null;
  var overlay=s.GetComponent<FullMapOverlay>();bool limitsAtStart=overlay&&overlay.ShowLimits;if(overlay)overlay.ShowLimits=true;bool opened=overlay&&overlay.Open();report.AppendLine("INFO full map opened="+opened+" (choice="+s.ChoiceOpen+" result="+s.RideResultOpen+" payment="+s.business.pendingPayment+")");if(opened){yield return new WaitForSecondsRealtime(.6f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"map-sections.png"));yield return null;yield return null;
   // 2026-11-26: speed cameras on the map (CCTV badge) and the two map filters.
   try{Check(SpeedCameraMap.Available(s)&&SpeedCameraMap.Icon!=null,"speed camera map icon loaded");var groups=SpeedCameraMap.Groups(p=>new Vector2(p.x,-p.z),28);Check(groups.Count>=10&&groups.Count<SpeedCameraRules.Cameras.Length,"cameras grouped for the map ("+groups.Count+" of "+SpeedCameraRules.Cameras.Length+", pairs merged)");Check(!limitsAtStart&&overlay.ShowCameras,"round 8: limit filter starts off, camera filter on");}catch(Exception e){error=e.ToString();}
   overlay.ShowCameras=false;yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"map-cameras-off.png"));yield return null;yield return null;
   overlay.ShowCameras=true;overlay.ShowLimits=false;yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"map-limits-off.png"));yield return null;yield return null;
   overlay.ShowLimits=true;overlay.Close();}
  // Vehicle state alert: the fatigue meter crossing 75 pops a card.
  var alerts=s.GetComponent<CriticalAlerts>();double fatigue=s.business.ledger.Driver.fatigue;
  try{Check(alerts!=null,"critical alerts present");alerts.Dismiss();s.business.ledger.Driver.fatigue=Math.Min(fatigue,60);}catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  yield return null;yield return null;s.business.ledger.Driver.fatigue=80;float wait=Time.realtimeSinceStartup+3;
  while(Time.realtimeSinceStartup<wait&&(alerts.Current==null||!alerts.Current.title.StartsWith("피로")))yield return null;
  try{Check(alerts.Current!=null&&alerts.Current.title.StartsWith("피로"),"fatigue past 75 shows an alert card ("+(alerts.Current==null?"none":alerts.Current.title)+")");}catch(Exception e){error=e.ToString();}
  if(error==null){yield return new WaitForSeconds(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"alert-card.png"));yield return null;yield return null;}
  s.business.ledger.Driver.fatigue=fatigue;
  // A promise for later today shows as a chip in the right column (student reunion, round 3).
  if(error==null){var book=s.business.Logbook;int sd=book.studentDay,sm=book.studentMinute;bool sp=book.studentPromised,sr=book.studentReturned;string place=book.studentPlace;
   book.studentDay=Math.Max(1,s.business.ledger.Day-2);book.studentMinute=Math.Min(1200,s.jobs.StudentMinute+90);book.studentPromised=true;book.studentReturned=false;book.studentPlace="은빛초 앞";
   alerts.Dismiss();yield return new WaitForSeconds(.4f);bool today=book.studentDay+2==s.business.ledger.Day;
   try{var items=AppointmentChip.Items(s);Check(items.Exists(i=>i.text.StartsWith("학생과 약속")&&i.text.Contains("은빛초 앞")&&i.text.Contains(today?"뒤":"일차")),"an open promise shows as a chip ("+(items.Count>0?items[0].text:"none")+")");}catch(Exception e){error=e.ToString();}
   if(error==null){ScreenCapture.CaptureScreenshot(Path.Combine(output,"appointment-chip.png"));yield return null;yield return null;}
   book.studentDay=sd;book.studentMinute=sm;book.studentPromised=sp;book.studentReturned=sr;book.studentPlace=place;}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("SPEEDLIMIT_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
