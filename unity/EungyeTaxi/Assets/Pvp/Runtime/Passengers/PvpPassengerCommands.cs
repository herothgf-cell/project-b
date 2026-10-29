using System;
using UnityEngine;
namespace Pvp.Competition {
public enum PvpPassengerStatus {Waiting,Onboard,Completed,Expired,Cancelled}
public enum PvpRoundPhase {Lobby,Countdown,Running,Closing,Settling,Results,Aborted}
[Serializable] public struct PvpPassengerCommand {public int Epoch,Kind,Revision;public string MatchId,RequestId,PassengerId;}
[Serializable] public sealed class PvpCommandResult {public string MatchId,RequestId,PassengerId,TripId,Reason;public bool Accepted;public int Player,Tick;}
[Serializable] public sealed class PvpPassengerView {public string Id,TripId,Reason;public int Revision,Pickup,Destination,Owner=-1,ContentIndex;public long QuotedFare;public PvpPassengerStatus Status;}
[Serializable] public sealed class PvpScoreRow {public int Player,Completed,Rank;public long Score,Fares,FarePoints,Bonus,Penalty,ServiceExpenses;public bool Eligible;public string Exclusion;}
[Serializable] public sealed class PvpLedgerEntry {public int Sequence,Player,Tick,Segment;public string Key,Kind;public long Amount;}
[Serializable] public sealed class PvpCompetitionSnapshot {
 public uint SupplySeed;public int Tick,StartTick,MainEndTick,FinalEndTick,LedgerSequence,ResultVersion;public string MatchId,SettingsHash,Reason;public PvpRoundPhase Phase;
 public PvpPassengerView[] Passengers;public PvpScoreRow[] Scores;public PvpCommandResult[] Responses;public PvpLedgerEntry[] RecentEntries;
 public PvpServiceView[] Services;
}
}
