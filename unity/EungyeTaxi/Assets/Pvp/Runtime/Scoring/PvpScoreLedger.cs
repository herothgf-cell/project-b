using System;
using System.Collections.Generic;
namespace Pvp.Competition {
public sealed class PvpScoreLedger {
 sealed class Account {public long Score,Fares,FarePoints,Bonus,Penalty;public int Completed,Segment;public bool Safe=true;}
 sealed class Incident {public long Penalty;public int Revision,Segment;}
 readonly Account[] accounts={new Account(),new Account()};readonly List<PvpLedgerEntry> entries=new List<PvpLedgerEntry>();readonly HashSet<string> trips=new HashSet<string>();readonly Dictionary<string,Incident> incidents=new Dictionary<string,Incident>();
 public int Sequence {get{return entries.Count;}}public Action BeforeCompletionCommit;public bool Closed {get;private set;}
 public PvpLedgerEntry[] Entries {get{return entries.ToArray();}}
 public void Close(){Closed=true;}
 public bool HasTrip(string trip){return trips.Contains(trip);}
 public long ApplyIncident(int player,string id,int revision,long penalty,int tick){if(Closed)throw new InvalidOperationException("Ledger closed");if(player<0||player>1||string.IsNullOrEmpty(id)||id.Length>128||revision<1||penalty<0)throw new ArgumentException("Invalid incident");string key=player+":"+id;Incident previous;incidents.TryGetValue(key,out previous);if(previous!=null&&revision<=previous.Revision)return 0;
  var a=accounts[player];long old=previous==null?0:previous.Penalty;long delta=Math.Max(old,penalty)-old;long score=checked(a.Score-delta),total=checked(a.Penalty+delta);int segment=previous==null||previous.Penalty==0&&delta>0?a.Segment:previous.Segment;
  if(delta>0)entries.Add(new PvpLedgerEntry{Sequence=checked(entries.Count+1),Player=player,Tick=tick,Segment=segment,Key=key+":"+revision,Kind="Penalty",Amount=-delta});
  incidents[key]=new Incident{Penalty=Math.Max(old,penalty),Revision=revision,Segment=segment};a.Score=score;a.Penalty=total;if(delta>0&&segment==a.Segment)a.Safe=false;return delta;
 }
 public bool Complete(int player,string trip,long fare,int tick,long wonPerPoint=100,long bonusNumerator=1,long bonusDenominator=10){if(trips.Contains(trip))return false;if(Closed)throw new InvalidOperationException("Ledger closed");if(player<0||player>1||string.IsNullOrEmpty(trip)||fare<0||wonPerPoint<=0||bonusNumerator<0||bonusDenominator<=0)throw new ArgumentException("Invalid settlement");var a=accounts[player];long points=fare/wonPerPoint,bonus=a.Safe?checked(points*bonusNumerator)/bonusDenominator:0;long score=checked(checked(a.Score+points)+bonus),fares=checked(a.Fares+fare),farePoints=checked(a.FarePoints+points),bonuses=checked(a.Bonus+bonus);int completed=checked(a.Completed+1),segment=checked(a.Segment+1);int first=checked(entries.Count+1);int last=checked(first+1);
  if(BeforeCompletionCommit!=null)BeforeCompletionCommit();
  entries.Add(new PvpLedgerEntry{Sequence=first,Player=player,Tick=tick,Segment=a.Segment,Key=trip+":fare",Kind="Fare",Amount=points});entries.Add(new PvpLedgerEntry{Sequence=last,Player=player,Tick=tick,Segment=a.Segment,Key=trip+":bonus",Kind="SafeBonus",Amount=bonus});trips.Add(trip);
  a.Score=score;a.Fares=fares;a.FarePoints=farePoints;a.Bonus=bonuses;a.Completed=completed;a.Segment=segment;a.Safe=true;return true;
 }
 public PvpScoreRow Read(int p){var a=accounts[p];return new PvpScoreRow{Player=p,Score=a.Score,Fares=a.Fares,FarePoints=a.FarePoints,Bonus=a.Bonus,Penalty=a.Penalty,Completed=a.Completed};}
 public PvpScoreRow[] BuildResult(bool firstEligible,bool secondEligible){var rows=new[]{Read(0),Read(1)};rows[0].Eligible=firstEligible&&rows[0].Completed>0;rows[1].Eligible=secondEligible&&rows[1].Completed>0;
  rows[0].Exclusion=firstEligible?"완료 운송 없음":"중도 이탈";rows[1].Exclusion=secondEligible?"완료 운송 없음":"중도 이탈";
  for(int i=0;i<2;i++)if(rows[i].Eligible){rows[i].Rank=1;for(int j=0;j<2;j++)if(j!=i&&rows[j].Eligible&&Compare(rows[j],rows[i])<0)rows[i].Rank++;}return rows;
 }
 static int Compare(PvpScoreRow a,PvpScoreRow b){int c=b.Score.CompareTo(a.Score);if(c!=0)return c;c=b.Fares.CompareTo(a.Fares);return c!=0?c:a.Penalty.CompareTo(b.Penalty);}
}
}
