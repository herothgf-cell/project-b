using System;

// Game incident score, separate from passenger manners and real-world penalties.
public static class AccidentScore {
 public static bool Apply(LawLedger ledger,AccidentState state,int day){
  if(ledger==null||state==null||state.ScorePenalty<=0||state.scoreApplied)return false;
  var incident=new LawEvent{id=state.eventId+":driving-score",family="accident",scenario="game-accident",day=day,reason="사고 발생 · 운전 점수 −"+state.ScorePenalty,action="사고 처리",recoveryPolicyVersion=1};
  incident.Add("GAME-ACCIDENT",LawVerdict.Violation,state.ScorePenalty);
  bool recorded=ledger.Record(incident);state.scoreApplied=true;return recorded;
 }
}
