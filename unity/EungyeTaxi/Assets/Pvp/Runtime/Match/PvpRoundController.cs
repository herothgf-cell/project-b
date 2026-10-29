using System;
namespace Pvp.Competition {
public sealed class PvpRoundController {
 public readonly PvpMatchSettings Settings;public readonly int StartTick,MainEndTick,FinalEndTick;
 public int Tick {get;private set;}public PvpRoundPhase Phase {get;private set;}public string Reason {get;private set;}
 readonly bool[] withdrawn=new bool[2],connected={true,true},eligible=new bool[2];readonly int[] expiry={int.MaxValue,int.MaxValue};bool frozen;
 public PvpRoundController(PvpMatchSettings settings,int startTick){if(settings==null||settings.PlayerCount!=2||startTick<0)throw new ArgumentException("Invalid trial round");Settings=settings;StartTick=startTick;MainEndTick=checked(startTick+settings.MatchDurationSeconds*50);FinalEndTick=checked(MainEndTick+settings.FinishGraceSeconds*50);Phase=PvpRoundPhase.Countdown;}
 public bool Withdrawn(int player){return withdrawn[player];}public bool Eligible(int player){return frozen?eligible[player]:!withdrawn[player];}
 public void Disconnect(int player,int now,int graceTicks){if(graceTicks<0)throw new ArgumentOutOfRangeException(nameof(graceTicks));connected[player]=false;expiry[player]=checked(now+graceTicks);if(now<StartTick){Phase=PvpRoundPhase.Lobby;Reason="준비 중 연결 해제 · 준비를 다시 맞추세요";}}
 public void Reconnect(int player){if(!withdrawn[player]){connected[player]=true;expiry[player]=int.MaxValue;}}
 public void Leave(int player){if(!frozen)withdrawn[player]=true;connected[player]=false;}
 public void Abort(string reason){if(Phase!=PvpRoundPhase.Results){Reason=reason;Phase=PvpRoundPhase.Aborted;}}
 public void Advance(int tick){if(tick<Tick)throw new ArgumentException("Authority tick cannot rewind");Tick=tick;if(Phase==PvpRoundPhase.Aborted||Phase==PvpRoundPhase.Results||Phase==PvpRoundPhase.Lobby)return;
  if(!frozen){for(int p=0;p<2;p++)if(!connected[p]&&expiry[p]<=tick)withdrawn[p]=true;}
  if(tick>=FinalEndTick){if(!frozen){for(int p=0;p<2;p++)eligible[p]=!withdrawn[p];frozen=true;}Phase=PvpRoundPhase.Settling;}
  else Phase=tick>=MainEndTick?PvpRoundPhase.Closing:tick>=StartTick?PvpRoundPhase.Running:PvpRoundPhase.Countdown;
  if(!frozen&&withdrawn[0]&&withdrawn[1])Abort("모든 참가자가 이탈했습니다");
 }
 public bool CanPickup {get{return Phase==PvpRoundPhase.Running;}}public bool CanComplete {get{return Phase==PvpRoundPhase.Running||Phase==PvpRoundPhase.Closing;}}
 public void Finish(){if(Phase!=PvpRoundPhase.Settling)throw new InvalidOperationException("Round not settled");Phase=PvpRoundPhase.Results;}
}
}
