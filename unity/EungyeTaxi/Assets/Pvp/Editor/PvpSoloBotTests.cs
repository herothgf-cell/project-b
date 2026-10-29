using System;
using System.Reflection;
using UnityEngine;
using Pvp.Map;
using Pvp.Network;
using Pvp.Driving;
using Pvp.Competition;
using Pvp.Traffic;
namespace Pvp.Tests.EditMode {
public static class PvpSoloBotTests {
 static void Check(bool ok,string reason){if(!ok)throw new Exception("PVP_SOLO_BOT: "+reason);}
 public static void RunAll(){PvpDrivingTrafficTests.RunAll();Run();}
 sealed class Bot {
  readonly object value;readonly MethodInfo step;FieldInfo input,command;
  public Bot(Type type,PvpMapRuntime map,int player){value=Activator.CreateInstance(type,map,player);step=type.GetMethod("Step");Check(step!=null,"bot decision contract");}
  public PvpVehicleInput Step(PvpCompetitionSnapshot match,PvpTrafficSnapshot traffic,PvpDriveSnapshot pose,out PvpPassengerCommand? action){object d=step.Invoke(value,new object[]{match,traffic,pose});if(input==null){input=d.GetType().GetField("Input");command=d.GetType().GetField("Command");}object c=command.GetValue(d);action=c==null?(PvpPassengerCommand?)null:(PvpPassengerCommand)c;return (PvpVehicleInput)input.GetValue(d);}
 }
 public static void Run(){
  var type=typeof(PvpNetworkSession).Assembly.GetType("Pvp.Competition.PvpTaxiBot");Check(type!=null,"one-window bot controller is missing");
  var definition=PvpMapDefinition.CreateDefault();try{var map=new PvpMapRuntime(definition);var settings=new PvpSessionConfig{PlayerCount=2,MatchDurationSeconds=360,FinishGraceSeconds=10,RulesVersion="solo-bot-test"}.Freeze("solo-bot-model",map.MapHash);var authority=new PvpPassengerAuthority(map,settings,150,2);
   var motors=new[]{new PvpNetworkMotor(map,0,true),new PvpNetworkMotor(map,1,true)};var bots=new[]{new Bot(type,map,0),new Bot(type,map,1)};var traffic=new PvpNpcAuthority(map);var law=new Pvp.Rules.PvpRuleAuthority(map,"solo-bot-model",(p,id,rev,amount,t)=>authority.ApplyIncident(p,id,rev,amount,t));int commands=0;
   for(int tick=1;tick<=authority.Round.FinalEndTick;tick++){
    authority.Round.Advance(tick);var before=new PvpDriveSnapshot[2];for(int p=0;p<2;p++){before[p]=motors[p].Capture();before[p].Owner=(ulong)p;before[p].Connected=true;before[p].Tick=tick;}traffic.Advance(tick,before);var shared=traffic.Snapshot();var snapshot=authority.Snapshot();
    for(int p=0;p<2;p++){PvpPassengerCommand? action;var input=bots[p].Step(snapshot,shared,before[p],out action);motors[p].ObserveTraffic(shared);motors[p].Step(input,tick);var after=motors[p].Capture();after.Owner=(ulong)p;after.Connected=true;after.Tick=tick;law.Observe(p,before[p],after,motors[p].LastMotion,shared,authority.Round.CanComplete);authority.ObserveVehicle(p,after,true);if(action.HasValue){Check(authority.Round.CanComplete,"no commands before countdown or after results");authority.Enqueue(p,action.Value);commands++;}}
    authority.Advance(tick);
   }
   var final=authority.Snapshot();Check(final.Phase==PvpRoundPhase.Results&&final.ResultVersion==1,"final settlement");
   for(int p=0;p<2;p++){Check(final.Scores[p].Completed>=2,"continuous deliveries for player="+p+" completed="+final.Scores[p].Completed);Check(motors[p].TravelDistance>500&&motors[p].BoundaryStops==0,"bounded actual driving player="+p+" distance="+motors[p].TravelDistance+" boundaries="+motors[p].BoundaryStops);Check(final.Scores[p].Penalty==0,"same lawful driving rules player="+p+" penalty="+final.Scores[p].Penalty);Debug.Log("PVP_SOLO_BOT_MODEL_DRIVER player="+p+" distance="+motors[p].TravelDistance+" completed="+final.Scores[p].Completed+" score="+final.Scores[p].Score);}
   var idle=new Bot(type,map,1);PvpPassengerCommand? late;idle.Step(final,traffic.Snapshot(),motors[1].Capture(),out late);Check(!late.HasValue,"closed match produces no command");
   long sum=0;foreach(var e in authority.Ledger.Entries)sum+=e.Amount;Check(sum==final.Scores[0].Score+final.Scores[1].Score,"one ledger without extra bot credits");Debug.Log("PVP_SOLO_BOT_MODEL_OK continuous/no position fixture/shared rules/commands="+commands);
  }finally{UnityEngine.Object.DestroyImmediate(definition);}
 }
}}
