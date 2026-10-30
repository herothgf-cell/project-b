using System;
using System.Collections.Generic;

public static class PvpSimulationMode {
    public static bool Active { get; private set; }
    public static bool Expanded { get; private set; }
    public static int DurationSeconds { get; private set; } = 600;
    public static void Configure(bool expanded, int seconds) {
        if(seconds < 30 || seconds > 7200) throw new ArgumentOutOfRangeException("seconds");
        Expanded=expanded; DurationSeconds=seconds; Active=true;
    }
    public static void Clear() { Active=false; Expanded=false; }
    public static void Reset() { Clear(); }
}

// One match owns this ledger. No static claims, preferences or campaign saves.
public sealed class PvpSimulationLedger {
    readonly Dictionary<int,int> owners=new Dictionary<int,int>();
    readonly HashSet<string> settlements=new HashSet<string>();
    readonly int[] scores=new int[2];
    readonly int[] expenses=new int[2];
    public bool Finished { get; private set; }
    public bool Available(int pickup,int driver) { int owner;return !Finished&&(!owners.TryGetValue(pickup,out owner)||owner==driver); }
    public bool TryClaim(int pickup,int driver) { if(pickup<0||driver<0||driver>1||!Available(pickup,driver))return false;owners[pickup]=driver;return true; }
    public void Release(int pickup,int driver) { int owner;if(owners.TryGetValue(pickup,out owner)&&owner==driver)owners.Remove(pickup); }
    public bool Settle(string tripId,int driver,int fare) { if(Finished||driver<0||driver>1||fare<0||string.IsNullOrEmpty(tripId)||!settlements.Add(tripId))return false;scores[driver]+=fare;return true; }
    public bool SyncExpenses(int driver,int cumulativeExpenses) {if(Finished||driver<0||driver>1||cumulativeExpenses<expenses[driver])return false;expenses[driver]=cumulativeExpenses;return true;}
    public int Score(int driver) { return scores[driver]-expenses[driver]; }
    public void Finish() { Finished=true;owners.Clear(); }
}
