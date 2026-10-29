using System;
using UnityEngine;
using Pvp.Map;
using Pvp.Network;
namespace Pvp.Competition {
[Serializable] public sealed class PvpServiceView {public int Player,BusyUntilTick;public float Fuel=100;public string FacilityId,Status;public bool Busy;}
// Local and network matches use the same authority state; no campaign cash or checkpoint.
public sealed class PvpServiceAuthority {
 public const float EmptyFuel=.001f;
 sealed class State {public float Fuel=100;public int Until,Cost;public string Facility,Request,Status="연료 준비";public Vector3 Anchor;public bool Seen,Present;public int Tick;public Vector3 Position;}
 readonly State[] states={new State(),new State()};readonly PvpMapRuntime map;readonly PvpRoundController round;readonly PvpScoreLedger ledger;readonly Func<int,int,bool> atStop;readonly Func<int,int> stopped;
 public PvpServiceAuthority(PvpMapRuntime arena,PvpRoundController game,PvpScoreLedger scores,Func<int,int,bool> at,Func<int,int> held){map=arena;round=game;ledger=scores;atStop=at;stopped=held;}
 public bool CanDrive(int p){return map.Services.Count==0||states[p].Until==0&&states[p].Fuel>EmptyFuel;}
 public void Observe(int p,PvpDriveSnapshot pose,bool present){var state=states[p];if(pose.Tick<=state.Tick&&state.Seen)return;float distance=Vector3.Distance(state.Position,pose.State.Position);if(map.Services.Count>0&&state.Seen&&present&&state.Present&&pose.Tick==state.Tick+1&&distance<=2.1f&&round.CanComplete)state.Fuel=Mathf.Max(0,state.Fuel-distance*.02f);state.Seen=true;state.Tick=pose.Tick;state.Position=pose.State.Position;state.Present=present;if(!present&&state.Until>0)Cancel(p,"접속 중단 · 이용 취소/청구 없음");}
 public bool Begin(int p,string id,string request,int tick,out string reason){var state=states[p];reason="";
  if(!round.CanComplete||!state.Present||round.Withdrawn(p)){reason="경기/참가 상태를 확인하세요";return false;}
  if(state.Until>0){reason="이미 서비스를 이용 중입니다";return false;}
  int duration,cost;if(id=="roadside"){
   if(map.Services.Count==0||state.Fuel>EmptyFuel||stopped(p)<10){reason="연료 고갈 후 정차를 유지하면 지원을 요청할 수 있습니다";return false;}duration=400;cost=6;
  }else{bool found=false;PvpServicePoint facility=default(PvpServicePoint);foreach(var point in map.Services)if(point.Id==id){facility=point;found=true;break;}
   if(!found){reason="등록된 시설이 아닙니다";return false;}if(!atStop(p,facility.StopIndex)){reason="시설의 주유 정차점에서 방향/정차를 확인하세요";return false;}if(state.Fuel>=95){reason="연료가 충분합니다";return false;}
   for(int other=0;other<2;other++)if(other!=p&&states[other].Until>0&&states[other].Facility==id){reason="시설 이용 중 · 잠시 기다리세요";return false;}duration=facility.DurationTicks;cost=facility.CostPoints;
  }
  if((long)tick+duration>=round.FinalEndTick){reason="마감 전에 서비스를 끝낼 시간이 부족합니다";return false;}
  state.Facility=id;state.Request=request;state.Until=checked(tick+duration);state.Cost=cost;state.Anchor=state.Position;state.Status=id=="roadside"?"연료 지원 대기 · 경기 시간 진행":"주유 중 · 완료 시 "+cost+"점";reason=state.Status;return true;
 }
 public void Cancel(int p,string reason="이용 취소 · 청구 없음"){var state=states[p];state.Until=0;state.Status=reason;state.Facility=null;state.Request=null;}
 public void Advance(int tick){for(int p=0;p<2;p++){var state=states[p];if(state.Until==0)continue;if(!round.CanComplete||!state.Present||round.Withdrawn(p)||Vector3.Distance(state.Anchor,state.Position)>.05f){Cancel(p,"이용 중단 · 청구 없음");continue;}if(tick<state.Until)continue;
   ledger.DebitService(p,state.Request,state.Cost,tick);state.Fuel=100;state.Until=0;state.Status="주유 완료 · "+state.Cost+"점 차감";state.Facility=null;state.Request=null;
  }}
 public PvpServiceView[] Snapshot(){var result=new PvpServiceView[2];for(int p=0;p<2;p++){var state=states[p];result[p]=new PvpServiceView{Player=p,Fuel=state.Fuel,Busy=state.Until>0,BusyUntilTick=state.Until,FacilityId=state.Facility,Status=state.Status};}return result;}
}
}
