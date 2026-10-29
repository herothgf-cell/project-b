using System;
using Pvp.Map;
using Pvp.Network;
using Pvp.Driving;
using Pvp.Traffic;
namespace Pvp.Competition {
public struct PvpBotDecision {
 public PvpVehicleInput Input;
 public PvpPassengerCommand? Command;
 public int TargetStop;
}
// A local opponent reads the same public snapshot as a human. It only emits inputs
// and ordinary requests: no motor restore, fare payment or authority mutation.
public sealed class PvpTaxiBot {
 readonly PvpMapRuntime map;
 readonly int player;
 readonly PvpDrivingPilot pilot;
 string selected;
 int sequence;
 public string Status {get;private set;}
 public PvpTaxiBot(PvpMapRuntime arena,int slot){
  if(slot<0||slot>1)throw new ArgumentOutOfRangeException(nameof(slot));
  map=arena??throw new ArgumentNullException(nameof(arena));player=slot;pilot=new PvpDrivingPilot(map);
 }
 PvpPassengerView Target(PvpCompetitionSnapshot match,PvpDriveSnapshot pose){
  foreach(var passenger in match.Passengers)
   if(passenger.Status==PvpPassengerStatus.Onboard&&passenger.Owner==player)return passenger;
  if(match.Phase!=PvpRoundPhase.Running)return null;
  foreach(var passenger in match.Passengers)
   if(passenger.Id==selected&&passenger.Status==PvpPassengerStatus.Waiting)return passenger;
  PvpPassengerView nearest=null;float distance=float.PositiveInfinity;
  foreach(var passenger in match.Passengers){
   if(passenger.Status!=PvpPassengerStatus.Waiting)continue;
   var route=PvpRoutePlanner.FindRoute(map,new PvpRoadPosition(pose.Edge,pose.Fraction),map.Stops[passenger.Pickup].Node);
   if(!route.Found)continue;
   if(route.Distance<distance||route.Distance==distance&&(nearest==null||string.CompareOrdinal(passenger.Id,nearest.Id)<0)){nearest=passenger;distance=route.Distance;}
  }
  selected=nearest==null?null:nearest.Id;return nearest;
 }
 public PvpBotDecision Step(PvpCompetitionSnapshot match,PvpTrafficSnapshot traffic,PvpDriveSnapshot pose){
  var decision=new PvpBotDecision{Input=new PvpVehicleInput(0,1,0),TargetStop=-1};
  if(match==null||match.Passengers==null||(match.Phase!=PvpRoundPhase.Running&&match.Phase!=PvpRoundPhase.Closing)){Status="경기 대기";return decision;}
  var target=Target(match,pose);if(target==null){Status="승객 대기";return decision;}
  bool onboard=target.Status==PvpPassengerStatus.Onboard;
  decision.TargetStop=onboard?target.Destination:target.Pickup;
  pilot.ObserveTraffic(traffic);decision.Input=pilot.Input(pose,decision.TargetStop,pose.Tick);
  Status=(onboard?"운송 중: ":"승차 지점으로: ")+map.Stops[decision.TargetStop].Name;
  if(pose.Tick%15==0&&pilot.Arrived(pose,decision.TargetStop)){
   decision.Command=new PvpPassengerCommand{MatchId=match.MatchId,RequestId="bot-"+player+"-"+(++sequence),PassengerId=target.Id,Revision=target.Revision,Kind=onboard?2:1};
  }
  return decision;
 }
}}
