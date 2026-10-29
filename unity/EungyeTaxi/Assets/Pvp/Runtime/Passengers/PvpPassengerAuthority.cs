using System;
using System.Collections.Generic;
using UnityEngine;
using Pvp.Map;
using Pvp.Network;
namespace Pvp.Competition {
// This object is called only from the authority tick, never from prediction replay.
public sealed class PvpPassengerAuthority {
 sealed class Request {public int Player;public PvpPassengerCommand Command;}
 readonly PvpMapRuntime map;readonly uint seed;readonly Dictionary<string,PvpPassengerView> passengers=new Dictionary<string,PvpPassengerView>();
 readonly Dictionary<string,PvpCommandResult> responses=new Dictionary<string,PvpCommandResult>();readonly List<Request> queue=new List<Request>();readonly PvpCommandResult[] latest=new PvpCommandResult[2];
 readonly PvpDriveSnapshot[] poses=new PvpDriveSnapshot[2];readonly bool[] present=new bool[2];readonly int[] stopped=new int[2],observedTick={-1,-1};
 public readonly PvpRoundController Round;public readonly PvpScoreLedger Ledger=new PvpScoreLedger();public readonly PvpServiceAuthority Services;PvpScoreRow[] finalRows;int supplyNumber,supplyCursor;bool supplied;
 public PvpPassengerAuthority(PvpMapRuntime map,PvpMatchSettings settings,int startTick,uint supplySeed=1){if(map==null||map.Stops.Count<3)throw new ArgumentException("Three valid stops required");this.map=map;seed=supplySeed;Round=new PvpRoundController(settings,startTick);Services=new PvpServiceAuthority(map,Round,Ledger,(p,s)=>At(p,s),p=>stopped[p]);}
 public static uint StableOrder(uint seed,string passenger,int player){uint h=2166136261;foreach(char c in seed+"|"+passenger+"|"+player){h^=c;h=unchecked(h*16777619);}return h;}
 public int IncomingEdge(int stop){if(stop<0||stop>=map.Stops.Count)throw new ArgumentOutOfRangeException(nameof(stop));int node=map.Stops[stop].Node;for(int e=0;e<map.Connections.Count;e++)if(map.Connections[e].To==node)return e;throw new ArgumentException("No legal stop approach");}
 public PvpDriveSnapshot AtStop(int stop){int edge=IncomingEdge(stop);var road=new PvpRoadPosition(edge,.99f);var f=map.Forward(road);return new PvpDriveSnapshot{Edge=edge,Fraction=.99f,State=new Driving.PvpVehicleState{Position=map.Position(road),YawDegrees=Mathf.Atan2(f.x,f.z)*Mathf.Rad2Deg}};}
 public void ObserveVehicle(int player,PvpDriveSnapshot pose,bool connected,bool recovering=false){if(player<0||player>1)throw new ArgumentOutOfRangeException(nameof(player));if(pose.Tick<=observedTick[player])return;bool steady=Vector3.Distance(poses[player].State.Position,pose.State.Position)<=.02f;poses[player]=pose;PvpRoadPosition located;present[player]=connected&&!recovering&&map.TryLocate(pose.State.Position,new PvpRoadPosition(pose.Edge,pose.Fraction),out located);stopped[player]=present[player]&&Mathf.Abs(pose.State.Speed)<=.35f?(pose.Tick==observedTick[player]+1&&steady?Math.Min(10,stopped[player]+1):1):0;observedTick[player]=pose.Tick;Services.Observe(player,pose,present[player]);}
 bool At(int player,int stop){if(!present[player]||stopped[player]<10)return false;var s=poses[player];if(s.Edge!=IncomingEdge(stop)||Vector3.Distance(s.State.Position,map.Stops[stop].Position)>3)return false;var forward=Quaternion.Euler(0,s.State.YawDegrees,0)*Vector3.forward;return Vector3.Angle(forward,map.Forward(new PvpRoadPosition(s.Edge,s.Fraction)))<=25;}
 public bool IsStoppedAt(int player,int stop){return At(player,stop);}
 public PvpPassengerView[] Active(){var result=new List<PvpPassengerView>();foreach(var p in passengers.Values)if(p.Status==PvpPassengerStatus.Waiting||p.Status==PvpPassengerStatus.Onboard)result.Add(Copy(p));result.Sort((a,b)=>string.CompareOrdinal(a.Id,b.Id));return result.ToArray();}
 public PvpPassengerView Passenger(string id){PvpPassengerView p;return passengers.TryGetValue(id,out p)?Copy(p):null;}
 public PvpPassengerView TripFor(int player){foreach(var p in passengers.Values)if(p.Owner==player&&p.Status==PvpPassengerStatus.Onboard)return Copy(p);return null;}
 public PvpCommandResult LastResponse(int player){return latest[player];}
 public int StoppedTicks(int player){return stopped[player];}
 public static long QuoteFare(long lengthCentimeters){if(lengthCentimeters<0)throw new ArgumentOutOfRangeException(nameof(lengthCentimeters));long steps=lengthCentimeters/10000+(lengthCentimeters%10000==0?0:1);return checked(1000+steps*1000);}
 void Supply(){int waiting=0;foreach(var p in passengers.Values)if(p.Status==PvpPassengerStatus.Waiting)waiting++;
  for(int scan=0;scan<map.Stops.Count&&waiting<3&&supplyNumber<128;scan++){int stop=map.IsExpanded?map.SupplyStop(supplyCursor++):scan;
   bool occupied=false;foreach(var p in passengers.Values)if(p.Status==PvpPassengerStatus.Waiting&&p.Pickup==stop)occupied=true;
   if(supplied)for(int player=0;player<2;player++)if(present[player]&&Vector3.Distance(poses[player].State.Position,map.Stops[stop].Position)<6)occupied=true;if(occupied)continue;
   int destination=map.Destination(stop);var route=PvpRoutePlanner.FindRoute(map,map.Stops[stop].Node,map.Stops[destination].Node);if(!route.Found||route.Distance<=0)continue;
   int number=++supplyNumber;var value=new PvpPassengerView{Id="p-"+number,Pickup=stop,Destination=destination,Revision=1,QuotedFare=QuoteFare((long)Math.Ceiling(route.Distance*100.0)),ContentIndex=(int)(StableOrder(seed,"p-"+number,0)%Passengers.PvpPassengerCatalog.Count)};passengers.Add(value.Id,value);waiting++;
  }supplied=true;
 }
 static PvpPassengerView Copy(PvpPassengerView p){return new PvpPassengerView{Id=p.Id,TripId=p.TripId,Reason=p.Reason,Revision=p.Revision,Pickup=p.Pickup,Destination=p.Destination,Owner=p.Owner,ContentIndex=p.ContentIndex,QuotedFare=p.QuotedFare,Status=p.Status};}
 public void Enqueue(int player,PvpPassengerCommand command){if(player<0||player>1)return;if(string.IsNullOrEmpty(command.RequestId)||command.RequestId.Length>96)return;string key=command.MatchId+":"+player+":"+command.RequestId;if(command.MatchId!=Round.Settings.MatchId){Store(player,command,false,"이전 경기 또는 잘못된 경기 요청",null);return;}PvpCommandResult prior;if(responses.TryGetValue(key,out prior)){latest[player]=prior;return;}
  if(responses.Count>=4096||queue.Count>=64){Store(player,command,false,"요청 처리 한도 초과",null);return;}if(command.Kind<1||command.Kind>5||string.IsNullOrEmpty(command.PassengerId)||command.PassengerId.Length>96){Store(player,command,false,"잘못된 요청",null);return;}queue.Add(new Request{Player=player,Command=command});
 }
 void Store(int player,PvpPassengerCommand c,bool accepted,string reason,string trip){string key=c.MatchId+":"+player+":"+c.RequestId;PvpCommandResult result;if(!responses.TryGetValue(key,out result)){result=new PvpCommandResult{MatchId=c.MatchId,Player=player,RequestId=c.RequestId,PassengerId=c.PassengerId,TripId=trip,Accepted=accepted,Reason=reason,Tick=Round.Tick};if(responses.Count<4096)responses.Add(key,result);}latest[player]=result;}
 public void Advance(int tick){Round.Advance(tick);
  foreach(var p in passengers.Values){if(p.Status==PvpPassengerStatus.Waiting&&!Round.CanPickup){if(Round.Phase==PvpRoundPhase.Closing||Round.Phase==PvpRoundPhase.Settling||Round.Phase==PvpRoundPhase.Aborted)Terminal(p,PvpPassengerStatus.Expired,"승차 마감");}
   else if(p.Status==PvpPassengerStatus.Onboard){if(Round.Withdrawn(p.Owner))Terminal(p,PvpPassengerStatus.Cancelled,"참가자 이탈");else if(Round.Phase==PvpRoundPhase.Settling||Round.Phase==PvpRoundPhase.Aborted)Terminal(p,PvpPassengerStatus.Expired,"운송 마감");}}
  if(Round.CanPickup&&(!supplied||tick%250==0))Supply();
  queue.Sort((a,b)=>{int c=string.CompareOrdinal(a.Command.PassengerId,b.Command.PassengerId);if(c!=0)return c;c=StableOrder(seed,a.Command.PassengerId,a.Player).CompareTo(StableOrder(seed,b.Command.PassengerId,b.Player));return c!=0?c:string.CompareOrdinal(a.Command.RequestId,b.Command.RequestId);});
  foreach(var request in queue)Process(request.Player,request.Command);queue.Clear();Services.Advance(tick);
  if(Round.Phase==PvpRoundPhase.Settling){Ledger.Close();finalRows=Ledger.BuildResult(Round.Eligible(0),Round.Eligible(1));Round.Finish();}
 }
 static void Terminal(PvpPassengerView p,PvpPassengerStatus state,string reason){p.Status=state;p.Revision++;p.Reason=reason;}
 void Process(int player,PvpPassengerCommand c){string key=c.MatchId+":"+player+":"+c.RequestId;PvpCommandResult previous;if(responses.TryGetValue(key,out previous)){latest[player]=previous;return;}
  if(c.Kind>=3){if(c.Kind==5){Services.Cancel(player);Store(player,c,true,"이용 취소 · 청구 없음",null);}else{string reason;bool ok=Services.Begin(player,c.Kind==4?"roadside":c.PassengerId,c.RequestId,Round.Tick,out reason);Store(player,c,ok,reason,null);}return;}
  PvpPassengerView p;if(!passengers.TryGetValue(c.PassengerId,out p)){Store(player,c,false,"대상 승객이 없습니다",null);return;}
  if(c.Kind==2&&p.Owner==player&&p.Status==PvpPassengerStatus.Completed&&Ledger.HasTrip(p.TripId)){Store(player,c,true,"이미 정산된 운송",p.TripId);return;}
  string error=null;
  if(Round.Withdrawn(player)||!present[player])error="참가 상태를 확인하세요";
  else if(c.Revision!=p.Revision)error="승객 상태가 바뀌었습니다";
  else if(c.Kind==1){if(!Round.CanPickup)error="신규 승차가 마감됐습니다";else if(p.Status!=PvpPassengerStatus.Waiting)error="다른 선수가 승차했거나 종료된 승객입니다";else if(TripFor(player)!=null)error="이미 운송 중입니다";else if(!At(player,p.Pickup))error="승차 위치·방향·정차 유지 확인";}
  else {if(!Round.CanComplete)error="하차 마감";else if(p.Status!=PvpPassengerStatus.Onboard||p.Owner!=player)error="본인의 운송이 아닙니다";else if(!At(player,p.Destination))error="하차 위치·방향·정차 유지 확인";}
  if(error!=null){Store(player,c,false,error,p.TripId);return;}
  if(c.Kind==1){p.Owner=player;p.TripId=Round.Settings.MatchId+"-"+p.Id;p.Status=PvpPassengerStatus.Onboard;p.Revision++;Store(player,c,true,"승차 확정",p.TripId);}
  else {try{PvpSettlementService.TryComplete(p,Ledger,Round.Tick);Store(player,c,true,"운임·정산 확정",p.TripId);}catch(Exception e){Store(player,c,false,"정산 보류: "+e.GetType().Name,p.TripId);}}
 }
 public void ApplyIncident(int player,string id,int revision,long amount,int tick){if(Round.CanComplete&&tick==Round.Tick)Ledger.ApplyIncident(player,id,revision,amount,tick);}
 public PvpCompetitionSnapshot Snapshot(){var scores=finalRows??Ledger.BuildResult(Round.Eligible(0),Round.Eligible(1));var all=Ledger.Entries;int count=Math.Min(16,all.Length);var recent=new PvpLedgerEntry[count];Array.Copy(all,all.Length-count,recent,0,count);return new PvpCompetitionSnapshot{SupplySeed=seed,Tick=Round.Tick,StartTick=Round.StartTick,MainEndTick=Round.MainEndTick,FinalEndTick=Round.FinalEndTick,Phase=Round.Phase,MatchId=Round.Settings.MatchId,SettingsHash=Round.Settings.SettingsHash,Reason=Round.Reason,LedgerSequence=Ledger.Sequence,ResultVersion=finalRows==null?0:1,Passengers=Active(),Scores=scores,Services=Services.Snapshot(),Responses=(PvpCommandResult[])latest.Clone(),RecentEntries=recent};}
}
}
