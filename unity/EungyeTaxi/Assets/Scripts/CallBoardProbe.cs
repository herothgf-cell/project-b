using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// 08 chapter 3 stage 1 player acceptance (Eungye single): several real call boards, their characters,
// card text and a big-luggage boarding. Uses the real board generator; stops are posed, not driven.
public sealed class CallBoardProbe : MonoBehaviour {
 const string Flag="-callBoardCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Call board acceptance").AddComponent<CallBoardProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("CALL_BOARD_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 IEnumerator Start(){
  float until=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>until){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-callBoardOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/call-board");Directory.CreateDirectory(output);
  yield return null;yield return null;string error=null;
  try{Run();}catch(Exception e){error=e.ToString();Debug.LogException(e);}
  if(error==null){
   var map=s.GetComponent<FullMapOverlay>();bool shown=false;
   try{if(map){map.Open();map.ShowCalls();for(int i=0;i<s.jobs.offers.Length&&!shown;i++)if(s.jobs.offers[i]!=null&&s.jobs.offers[i].variety!=0)shown=map.SelectOffer(i);}}catch(Exception e){Debug.LogException(e);}
   Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.9f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"call-board.png"));yield return new WaitForSecondsRealtime(.6f);
  }
  Finish(error);
 }
 void Run(){
  var j=s.jobs;var b=s.business;
  Check(!s.simulationMatch&&b.ledger.OneDay,"Eungye single one-day ledger");
  b.preparing=false;s.paused=false;Time.timeScale=1;j.ClearForDay();
  var seen=new HashSet<int>();int boards=0;
  j.ResetOffers();
  // ResetOffers returns to generation 0; later boards advance like real play.
  for(int g=0;g<12;g++){
   Array.Clear(j.cooldown,0,j.cooldown.Length);if(g>0)j.Generate();boards++;
   var present=j.offers.Where(o=>o!=null).ToArray();Check(present.Length>=1,"board "+g+" has calls");
   var kinds=present.Where(o=>o.variety!=0).Select(o=>o.variety).ToArray();report.AppendLine("board "+g+": "+string.Join(",",present.Select(o=>o.variety+"/z"+o.zoneIndex+(string.IsNullOrEmpty(o.scenarioId)?"":"/s")+"/p"+Mathf.RoundToInt(j.CallPickupMetres(o))+"/d"+Mathf.RoundToInt(o.distance))));
   Check(kinds.Distinct().Count()==kinds.Length,"board "+g+" never repeats a character");
   foreach(var o in present){
    string caption=j.CallCardCaption(Array.IndexOf(j.offers,o),s.law.ledger.TotalScore);
    Check(caption.Contains("픽업 ")&&caption.Contains("m · 약 ")&&caption.Contains("기한 "),"card shows pickup distance, time and deadline");
    if(o.variety==CallBoardRules.Hidden){Check(caption.Contains("목적지 비공개")&&!caption.Contains(KoreanText.Stop(j.stops[o.drop].name))&&!caption.Contains("하차지 수요"),"hidden destination stays hidden on the card");}
    else Check(caption.Contains("하차지 수요 높음")||caption.Contains("하차지 수요 보통")||caption.Contains("하차지 수요 낮음"),"card shows drop-off demand");
    if(o.variety==CallBoardRules.Rush)Check(o.urgency==2&&o.fare%100==0,"rush call: urgent, fare in 100 won");
    if(o.variety==CallBoardRules.Near)Check(j.CallPickupMetres(o)<=CallBoardRules.NearMaxPickup&&o.distance<=CallBoardRules.NearMaxRide,"near call within limits");
    seen.Add(o.variety);
   }
  }
  report.AppendLine("kinds seen: "+string.Join(",",seen.OrderBy(x=>x)));
  Check(seen.Contains(CallBoardRules.Rush)&&seen.Contains(CallBoardRules.Hidden),"rush and hidden calls appear within 12 boards");
  bool trunk=b.ledger.Account.VehicleSpec.carriesLuggage;
  Check(!trunk||seen.Contains(CallBoardRules.Luggage),"luggage calls appear when the taxi has a trunk");
  // A saved board keeps the characters.
  var snapshot=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(j.CaptureLive()));
  Check(snapshot.offers.Where((o,i)=>snapshot.offerPresent[i]).Select(o=>o.variety).SequenceEqual(j.offers.Where(o=>o!=null).Select(o=>o.variety)),"board snapshot keeps characters");
  if(trunk){
   JobOffer luggage=null;for(int g=0;g<20&&luggage==null;g++){Array.Clear(j.cooldown,0,j.cooldown.Length);j.Generate();luggage=j.offers.FirstOrDefault(o=>o!=null&&o.variety==CallBoardRules.Luggage);}
   Check(luggage!=null,"a luggage call is available");int index=Array.IndexOf(j.offers,luggage);
   var p=j.stops[luggage.pickup];s.taxi.body.velocity=Vector3.zero;s.taxi.body.position=p.Kerb+Vector3.up*.65f;s.taxi.body.rotation=Quaternion.LookRotation(p.Direction);s.taxi.transform.SetPositionAndRotation(s.taxi.body.position,s.taxi.body.rotation);Physics.SyncTransforms();
   Check(j.Accept(index),"accept luggage call"+" ("+j.message+" / "+s.business.ledger.State+" / left "+Mathf.RoundToInt((float)s.business.ledger.SecondsLeft)+" / canAccept "+s.business.CanAccept+")");
   for(int i=0;i<4&&j.trip.State==TripState.ToPickup;i++)j.Interact();
   Check(j.Behavior!=null&&(j.Behavior.Type==PassengerType.Luggage||j.Behavior.Type==PassengerType.MuddyLuggage),"luggage call boards a luggage passenger (got "+(j.Behavior==null?"none":j.Behavior.Type.ToString())+")");
   j.ClearForDay();
  }
 }
 void Finish(string error){
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("CALL_BOARD_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
