using System;
public enum SignalColor { Red, Yellow, Green }
public static class TrafficRules {
    public const float Cycle = 56f;
    public static SignalColor Signal(float time, int axis) {
        float t = (time % Cycle + Cycle) % Cycle;
        if (axis == 0) return t < 12 ? SignalColor.Green : t < 15 ? SignalColor.Yellow : SignalColor.Red;
        return t >= 17 && t < 27 ? SignalColor.Green : t >= 27 && t < 30 ? SignalColor.Yellow : SignalColor.Red;
    }
    public static SignalColor LeftSignal(float time,int axis){float t=(time%Cycle+Cycle)%Cycle;float start=axis==0?32:44;return t>=start&&t<start+8?SignalColor.Green:t>=start+8&&t<start+10?SignalColor.Yellow:SignalColor.Red;}
    public static float SafeSpeed(float gap, float cruise) { return Math.Min(cruise, (float)Math.Sqrt(2 * 3f * Math.Max(0, gap))); }
    public static int ToggleIndicator(int current, int requested) { return current == requested ? 0 : requested; }
}
