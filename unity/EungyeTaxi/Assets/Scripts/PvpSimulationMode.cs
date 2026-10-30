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

// Match-local ownership and native income. Each participant has its own ledger.
public sealed class PvpSimulationLedger {
    public const int DriverCount=4;
    readonly Dictionary<int,int> owners=new Dictionary<int,int>();
    readonly HashSet<string> settlements=new HashSet<string>();
    readonly int[] scores=new int[DriverCount];
    readonly int[] expenses=new int[DriverCount];
    public bool Finished { get; private set; }
    static bool Valid(int driver){return driver>=0&&driver<DriverCount;}
    public bool IsClaimed(int pickup){return owners.ContainsKey(pickup);}
    public bool Available(int pickup,int driver) { int owner;return !Finished&&Valid(driver)&&(!owners.TryGetValue(pickup,out owner)||owner==driver); }
    public bool TryClaim(int pickup,int driver) { if(pickup<0||!Available(pickup,driver))return false;owners[pickup]=driver;return true; }
    public void Release(int pickup,int driver) { int owner;if(owners.TryGetValue(pickup,out owner)&&owner==driver)owners.Remove(pickup); }
    public bool Settle(string tripId,int driver,int fare) { if(Finished||!Valid(driver)||fare<0||string.IsNullOrEmpty(tripId)||!settlements.Add(driver+":"+tripId))return false;scores[driver]+=fare;return true; }
    public bool SyncExpenses(int driver,int cumulativeExpenses) {if(Finished||!Valid(driver)||cumulativeExpenses<expenses[driver])return false;expenses[driver]=cumulativeExpenses;return true;}
    public int Score(int driver) { return Valid(driver)?scores[driver]-expenses[driver]:0; }
    public void Finish() { Finished=true;owners.Clear(); }
}
