using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Unity.Netcode;
using Pvp.Competition;
using Pvp.Driving;
using Pvp.Network;
namespace Pvp {
public sealed class PvpExpandedProbe:MonoBehaviour {
 int request;bool cancelSent,serviceDone;int cancelTick;
 bool Check(bool ok,string message){if(ok)return true;Debug.LogError("PVP_EXPANDED_PLAYER_FAIL "+message);Application.Quit(1);return false;}
 IEnumerator Start(){
  yield return null;var root=GetComponent<PvpBootstrap>();root.StartSolo(240,15);var session=GetComponent<PvpNetworkSession>();var bridge=GetComponent<PvpCompetitionBridge>();var practice=GetComponent<PvpPracticeSession>();
  if(!Check(session!=null&&session.IsOfflineBot&&session.Arena.IsExpanded&&session.Arena.Stops.Count==8&&session.Arena.Services.Count==2&&GetComponent<Pvp.UI.PvpRaceHud>()!=null&&NetworkManager.Singleton==null,"expanded one-window entry/HUD/data"))yield break;
  var bot=new PvpTaxiBot(session.Arena,0);var pilot=new PvpDrivingPilot(session.Arena);session.Probe=true;session.ProbeDriving=true;
  session.OfflineTestInput=pose=>{
   var match=bridge.CurrentMatch;var state=match.Services[0];
   if(state.Status!=null&&state.Status.StartsWith("주유 완료"))serviceDone=true;
   if(match.Scores[0].Completed==0||serviceDone){var decision=bot.StepWithServices(match,bridge.CurrentTraffic,pose);if(decision.Command.HasValue)session.IssueCompetitionCommand(decision.Command.Value);return decision.Input;}
   if(state.Busy){if(!cancelSent&&pose.Tick>=state.BusyUntilTick-150){cancelSent=true;cancelTick=pose.Tick;session.IssueCompetitionCommand(Command(match,"cancel",5));}return new PvpVehicleInput(0,1,0);}
   if(cancelSent&&pose.Tick-cancelTick<15)return new PvpVehicleInput(0,1,0);
   var facility=session.Arena.Services[0];pilot.ObserveTraffic(bridge.CurrentTraffic);var input=pilot.Input(pose,facility.StopIndex,pose.Tick);
   if(pose.Tick%15==0&&pilot.Arrived(pose,facility.StopIndex))session.IssueCompetitionCommand(Command(match,facility.Id,3));return input;
  };
  var args=Environment.GetCommandLineArgs();int capture=Array.IndexOf(args,"-pvpCapturePath");
  if(capture>=0&&capture+1<args.Length){
   float wait=Time.realtimeSinceStartup+20;while(!UnityEngine.Rendering.SplashScreen.isFinished&&Time.realtimeSinceStartup<wait)yield return null;yield return new WaitForSecondsRealtime(4);
   string path=args[capture+1];ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(3);practice.ToggleNetworkOverview();yield return new WaitForSecondsRealtime(2);ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+"-map.png"));yield return new WaitForSecondsRealtime(3);practice.ToggleNetworkOverview();practice.ToggleCamera();yield return new WaitForSecondsRealtime(2);ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+"-driver.png"));yield return new WaitForSecondsRealtime(3);practice.ToggleCamera();
   if(Array.IndexOf(args,"-pvpCaptureOnly")>=0){if(!Check(ValidCapture(path),"nonblank rendered capture"))yield break;Debug.Log("PVP_EXPANDED_RENDER_OK "+path);Application.Quit(0);yield break;}
  }
  float deadline=Time.realtimeSinceStartup+290;while(bridge.CurrentMatch.Phase!=PvpRoundPhase.Results&&Time.realtimeSinceStartup<deadline)yield return null;
  var result=bridge.CurrentMatch;Debug.Log("PVP_EXPANDED_RESULT "+bridge.ResultSignature);
  Debug.Log("PVP_EXPANDED_SERVICE observed="+JsonUtility.ToJson(result.Services[0])+" cancelled="+cancelSent+" done="+serviceDone+" pose="+JsonUtility.ToJson(session.DriverPose)+" response="+JsonUtility.ToJson(result.Responses[0]));
  foreach(var audit in session.DrivingAudits())Debug.Log("PVP_EXPANDED_DRIVING player="+audit.Player+" distanceMm="+audit.DistanceMm+" fixture="+audit.FixtureWrites+" boundary="+audit.BoundaryStops);
  if(!Check(result.Phase==PvpRoundPhase.Results&&result.Scores[0].Completed>=1&&result.Scores[1].Completed>=1,"actual transport and final result"))yield break;
  if(!Check(cancelSent&&serviceDone&&result.Scores[0].ServiceExpenses==2&&result.Scores[0].Penalty==0,"drive to facility/cancel/retry/one fee/no law exemption"))yield break;
  foreach(var audit in session.DrivingAudits())if(!Check(audit.DistanceMm>120000&&audit.FixtureWrites==0&&audit.BoundaryStops==0,"accepted input driving"))yield break;
  if(!Check(bridge.CurrentTraffic.Npcs.Length==6&&bridge.CurrentTraffic.Pedestrians.Length==4&&NetworkManager.Singleton==null,"expanded shared traffic/no transport"))yield break;
  session.Stop();yield return null;yield return null;
  if(!Check(GetComponent<Pvp.UI.PvpRaceHud>()==null&&GetComponent<PvpNetworkSession>()==null&&GetComponent<PvpPracticeSession>()==null&&GetComponent<PvpPrototypePanel>().enabled,"expanded HUD/world cleanup"))yield break;
  root.StartSolo(2,0);session=GetComponent<PvpNetworkSession>();bridge=GetComponent<PvpCompetitionBridge>();session.Probe=true;session.OfflineTestInput=pose=>new PvpVehicleInput(0,1,0);deadline=Time.realtimeSinceStartup+12;
  while(bridge.CurrentMatch.Phase!=PvpRoundPhase.Results&&Time.realtimeSinceStartup<deadline)yield return null;
  if(!Check(bridge.CurrentMatch.Phase==PvpRoundPhase.Results&&bridge.CurrentMatch.Scores[0].Score==0&&bridge.CurrentMatch.Services[0].Fuel==100,"fresh expanded restart"))yield break;
  session.Stop();yield return null;yield return null;Debug.Log("PVP_EXPANDED_PLAYER_OK actual driving/passengers/NPC/rules/service/cancel/one expense/cleanup/restart");Application.Quit(0);
 }
 PvpPassengerCommand Command(PvpCompetitionSnapshot match,string id,int kind){return new PvpPassengerCommand{MatchId=match.MatchId,RequestId="expanded-"+(++request),PassengerId=id,Kind=kind};}
 bool ValidCapture(string path){if(!File.Exists(path))return false;var texture=new Texture2D(2,2);try{if(!texture.LoadImage(File.ReadAllBytes(path)))return false;var pixels=texture.GetPixels32();var colors=new System.Collections.Generic.HashSet<int>();for(int i=0;i<pixels.Length;i+=31){var c=pixels[i];colors.Add(c.r<<16|c.g<<8|c.b);}return colors.Count>32;}finally{Destroy(texture);}}
}}
