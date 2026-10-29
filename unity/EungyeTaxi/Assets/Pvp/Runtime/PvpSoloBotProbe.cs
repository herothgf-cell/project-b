using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Pvp.Network;
using Pvp.Competition;
using Pvp.Driving;
namespace Pvp {
// Explicit command-line probe only; never installs an automated human in normal play.
public sealed class PvpSoloBotProbe:MonoBehaviour {
 bool Check(bool ok,string reason){if(ok)return true;Debug.LogError("PVP_SOLO_BOT_PLAYER_FAIL "+reason);Application.Quit(1);return false;}
 IEnumerator Start(){
  yield return null;var bootstrap=GetComponent<PvpBootstrap>();
  bool violation=Array.IndexOf(Environment.GetCommandLineArgs(),"-pvpSoloViolationCheck")>=0;
  bootstrap.StartSolo(0,0);yield return null;yield return null;
  if(!Check(!string.IsNullOrEmpty(bootstrap.StartupError)&&GetComponent<PvpNetworkSession>()==null&&GetComponent<PvpPracticeSession>()==null&&NetworkManager.Singleton==null,"invalid duration leaves menu and transport untouched"))yield break;
  bootstrap.StartSolo(120,10);var session=GetComponent<PvpNetworkSession>();var bridge=GetComponent<PvpCompetitionBridge>();var practice=GetComponent<PvpPracticeSession>();
  if(!Check(session!=null&&session.IsOfflineBot&&session.InputReady&&bridge!=null&&practice!=null&&NetworkManager.Singleton==null&&string.IsNullOrEmpty(bootstrap.StartupError),"single-window startup"))yield break;
  var testDriver=new PvpTaxiBot(practice.Map,0);var unsafeDriver=new PvpDrivingPilot(practice.Map){ObeyTraffic=false,CruiseSpeed=16};
  session.Probe=true;session.ProbeDriving=true;
  session.OfflineTestInput=pose=>{var d=testDriver.Step(bridge.CurrentMatch,bridge.CurrentTraffic,pose);if(d.Command.HasValue)session.IssueCompetitionCommand(d.Command.Value);bool provoke=violation&&bridge.CurrentMatch.Scores[0].Penalty==0;return provoke?unsafeDriver.Input(pose,d.TargetStop,pose.Tick):d.Input;};
  float timeout=Time.realtimeSinceStartup+180;
  while(bridge.CurrentMatch.Phase!=PvpRoundPhase.Results&&Time.realtimeSinceStartup<timeout){if(NetworkManager.Singleton!=null){Check(false,"local match opened transport");yield break;}yield return null;}
  var match=bridge.CurrentMatch;
  Debug.Log("PVP_SOLO_BOT_RESULT_OBSERVED "+bridge.ResultSignature);
  if(!Check(match.Phase==PvpRoundPhase.Results&&match.ResultVersion==1&&match.Scores[0].Completed>=1&&match.Scores[1].Completed>=1,"both participants transport and settle"))yield break;
  if(!Check(match.Scores[1].Penalty==0&&(violation?match.Scores[0].Penalty>=3:match.Scores[0].Penalty==0),"identical rule enforcement for human and bot"))yield break;
  foreach(var audit in session.DrivingAudits()){
   if(!Check(audit.FixtureWrites==0&&audit.DistanceMm>120000&&audit.BoundaryStops==0&&audit.Throttle>100&&audit.Steering>20&&audit.Brake>20,"input-only accepted motion player="+audit.Player))yield break;
   Debug.Log("PVP_SOLO_BOT_DRIVING_PROOF player="+audit.Player+" distanceMm="+audit.DistanceMm+" fixture="+audit.FixtureWrites+" boundaries="+audit.BoundaryStops);
  }
  if(!Check(bridge.CurrentTraffic.Npcs.Length==2&&bridge.CurrentTraffic.Pedestrians.Length==1,"shared NPC world"))yield break;
  Debug.Log("PVP_SOLO_BOT_RESULT "+bridge.ResultSignature+" violation="+violation);
  session.Stop();yield return null;yield return null;
  if(!Check(GetComponent<PvpNetworkSession>()==null&&GetComponent<PvpCompetitionBridge>()==null&&GetComponent<PvpPracticeSession>()==null&&GetComponent<PvpPrototypePanel>().enabled&&NetworkManager.Singleton==null,"exit cleans world and returns to menu"))yield break;
  bootstrap.StartSolo(3,0);session=GetComponent<PvpNetworkSession>();bridge=GetComponent<PvpCompetitionBridge>();
  if(!Check(session!=null&&bridge!=null&&session.IsOfflineBot,"restart from same menu"))yield break;
  session.Probe=true;session.OfflineTestInput=pose=>new PvpVehicleInput(0,1,0);timeout=Time.realtimeSinceStartup+15;
  while(bridge.CurrentMatch.Phase!=PvpRoundPhase.Results&&Time.realtimeSinceStartup<timeout)yield return null;
  if(!Check(bridge.CurrentMatch.Phase==PvpRoundPhase.Results&&bridge.CurrentMatch.ResultVersion==1&&bridge.CurrentMatch.Scores[0].Completed==0&&bridge.CurrentMatch.Scores[1].Completed==0,"fresh short round, no carried score or instant bot completion"))yield break;
  session.Stop();yield return null;yield return null;
  if(!Check(NetworkManager.Singleton==null&&GetComponent<PvpPrototypePanel>().enabled,"final menu and no transport"))yield break;
  Debug.Log("PVP_SOLO_BOT_PLAYER_OK transport=none single-process input-driving/settlement/rules/invalid/exit/restart");Application.Quit(0);
 }
}}
