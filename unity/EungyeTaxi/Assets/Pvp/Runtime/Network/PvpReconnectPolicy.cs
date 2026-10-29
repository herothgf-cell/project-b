namespace Pvp.Network {
// Recover before the 128-command prediction history fills, including a five-tick frame burst.
public static class PvpReconnectPolicy {
 public static bool ShouldRecover(int pending,float snapshotAge){return pending>=120||snapshotAge>3;}
 public static bool IsTerminalRejection(string reason){return reason.Contains("ticket")||reason.Contains("expired")||reason.Contains("left session")||reason.Contains("mismatch");}
}
}
