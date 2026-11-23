using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// 08 chapter 4 stage 2 player acceptance (Eungye single): the stopped-with-passenger summary and the last call
// board. Stops are posed, not driven; the shift clock is advanced directly.
public sealed class IdleTimeProbe : MonoBehaviour {
 const string Flag="-idleTimeCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Idle time acceptance").AddComponent<IdleTimeProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("IDLE_TIME_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Pose(MapStop p){s.taxi.body.velocity=Vector3.zero;s.taxi.body.angularVelocity=Vector3.zero;s.taxi.body.position=p.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(p.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();}
 IEnumerator Start(){
  float until=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>until){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-idleTimeOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/idle-time");Directory.CreateDirectory(output);
  yield return null;string error=null;var j=s.jobs;
  // Stopped with a passenger aboard.
  try{
   s.business.preparing=false;s.paused=false;Time.timeScale=1;j.ClearForDay();Array.Clear(j.cooldown,0,j.cooldown.Length);j.ResetOffers();j.ForcedPassengerType=PassengerType.General;j.ForcedRideCase=DrunkRideCase.Normal;
   int index=Array.FindIndex(j.offers,o=>o!=null&&o.variety!=CallBoardRules.Hidden);Check(index>=0,"a call with a visible destination");
   Pose(j.stops[j.offers[index].pickup]);Check(j.Accept(index),"accept"+" ("+j.message+" / "+s.business.ledger.State+" / left "+Mathf.RoundToInt((float)s.business.ledger.SecondsLeft)+" / canAccept "+s.business.CanAccept+")");for(int i=0;i<4&&j.trip.State==TripState.ToPickup;i++)j.Interact();
   Check(j.trip.State==TripState.OnTrip,"passenger aboard");
   Check(j.StopBreakSummary()==null,"nothing before the stop delay");
  }catch(Exception e){error=e.ToString();}
  if(error==null){yield return new WaitForSecondsRealtime(IdleTimeRules.StopBreakDelay+.4f);
   try{string summary=j.StopBreakSummary();report.AppendLine("summary: "+summary);
    Check(summary!=null&&summary.StartsWith("정차 중 · ")&&summary.Contains("하차 후 가까운 승차 지점"),"stopped summary gathers the next-call preview");
    s.taxi.body.velocity=s.taxi.transform.forward*5;Check(j.StopBreakSummary()==null,"moving hides the summary at once");s.taxi.body.velocity=Vector3.zero;
    j.ClearForDay();
   }catch(Exception e){error=e.ToString();}}
  // Last call near the end of the shift.
  if(error==null)try{
   Check(s.services.HasHomeBase,"single career has the Deoksung Plaza home base");
   var ledger=s.business.ledger;double left=ledger.SecondsLeft;Check(left>IdleTimeRules.LastCallWindow,"shift starts outside the window");
   ledger.Advance(left-IdleTimeRules.LastCallWindow+30,0);Check(ledger.SecondsLeft<=IdleTimeRules.LastCallWindow&&ledger.SecondsLeft>0,"clock moved into the last-call window");
   Array.Clear(j.cooldown,0,j.cooldown.Length);j.Generate();
   var present=j.offers.Where(o=>o!=null).ToArray();report.AppendLine("last call: "+string.Join(" | ",present.Select(o=>o.variety+" "+o.fare+" back "+Mathf.RoundToInt(j.HomeReturnMetres(o)))));
   Check(j.LastCallBoard&&present.Length>=1&&present.Length<=2&&present.All(o=>o.variety==IdleTimeRules.LastHome||o.variety==IdleTimeRules.LastFar),"board becomes the last call (1-2 calls)");
   var home=present.FirstOrDefault(o=>o.variety==IdleTimeRules.LastHome);var far=present.FirstOrDefault(o=>o.variety==IdleTimeRules.LastFar);
   if(home!=null&&far!=null)Check(j.HomeReturnMetres(home)<=j.HomeReturnMetres(far),"home-direction call ends closer to home than the far call");
   foreach(var o in present){string c=j.CallCardCaption(Array.IndexOf(j.offers,o),s.law.ledger.TotalScore);Check(c.Contains("마지막 한 콜")&&c.Contains("하차 후 덕성프라자까지 약 "),"last-call card shows the return distance");}
   Check(j.GoHomeNow()&&!string.IsNullOrEmpty(s.services.destination),"go home now starts guidance to the home garage ("+s.services.destination+")");
  }catch(Exception e){error=e.ToString();}
  if(error==null){var map=s.GetComponent<FullMapOverlay>();try{Array.Clear(j.cooldown,0,j.cooldown.Length);j.Generate();if(map){map.Open();map.ShowCalls();}}catch(Exception e){Debug.LogException(e);}
   Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.9f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"last-call.png"));yield return new WaitForSecondsRealtime(.6f);}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("IDLE_TIME_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
