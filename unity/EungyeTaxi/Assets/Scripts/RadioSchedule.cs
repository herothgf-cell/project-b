using System;

public enum RadioCueKind { Morning=0, Traffic=1, LocalNews=2, Letter=3, LateTraffic=4, Closing=5 }

[Serializable] public sealed class RadioScheduleSnapshot {
    public const int CurrentVersion = 1;
    public int version = CurrentVersion;
    public int day, completedMask;
    public int activeCue = -1;
    public double remaining;

    public void Validate(int expectedDay) {
        int lastCue = (int)RadioCueKind.Closing;
        int maximumMask = (1 << (lastCue + 1)) - 1;
        if (version != CurrentVersion || day < 1 || day != expectedDay ||
            completedMask < 0 || completedMask > maximumMask || activeCue < -1 || activeCue > lastCue ||
            double.IsNaN(remaining) || double.IsInfinity(remaining) || remaining < 0 || remaining > RadioSchedule.CaptionSeconds ||
            (activeCue < 0 ? remaining != 0 : remaining <= 0 || (completedMask & (1 << activeCue)) == 0))
            throw new ArgumentException("Invalid radio schedule");
    }
}

public sealed class RadioSchedule {
    public const double CaptionSeconds = 6;
    const int ClosingCue = (int)RadioCueKind.Closing;
    const int DayCuesMask = (1 << ClosingCue) - 1;

    readonly int day;
    readonly string[] keys;
    readonly double[] times;
    int completed;
    int active = -1;
    bool visible;

    public double Remaining { get; private set; }
    public RadioCueKind? VisibleKind { get { return visible ? (RadioCueKind?)active : null; } }
    public string VisibleKey { get { return visible ? keys[active] : ""; } }

    public bool NeedsPresentation(double elapsed, double shiftSeconds, bool closing) {
        return closing ? active == ClosingCue || !Completed(ClosingCue) : active >= 0 || NextCue(elapsed, shiftSeconds) >= 0;
    }

    public RadioSchedule(string campaign, int day, DailyConditionKind condition, double elapsed,
        double shiftSeconds, int secondTrafficPercent, RadioScheduleSnapshot saved = null) {
        this.day = day;
        uint seed = Seed(campaign, day);
        bool construction = condition == DailyConditionKind.Construction;
        bool secondTraffic = seed % 100 < Math.Max(0, Math.Min(100, secondTrafficPercent));
        string letter = (1 + (seed / 100) % 3).ToString(System.Globalization.CultureInfo.InvariantCulture);
        keys = new[] {
            "radio.morning." + ConditionKey(condition),
            construction ? "radio.traffic.construction" : "radio.traffic.regular",
            condition == DailyConditionKind.Market ? "radio.news.market" : condition == DailyConditionKind.Friday ? "radio.news.friday" : "radio.news.regular",
            "radio.letter." + letter,
            secondTraffic ? construction ? "radio.traffic.constructionEvening" : "radio.traffic.evening" : null,
            "radio.closing"
        };
        times = new[] { 0, .14, .40, .54 + ((seed / 300) % 11) / 100.0, .78 };
        if (saved == null) {
            // Legacy checkpoints can resume near a cue. Leave one subtitle interval for the first frame of a new shift.
            ConsumeDue(elapsed - CaptionSeconds, shiftSeconds);
        } else {
            saved.Validate(day);
            completed = saved.completedMask;
            if (saved.activeCue >= 0 && keys[saved.activeCue] != null) {
                active = saved.activeCue;
                Remaining = saved.remaining;
            }
        }
    }

    public void Tick(double elapsed, double shiftSeconds, bool closing, bool mayShow, bool enabled, double visibleDelta) {
        bool wasVisible = visible;
        visible = false;
        if (closing) {
            completed |= DayCuesMask;
            if (active != ClosingCue) ClearActive();
        }
        if (!enabled) {
            ClearActive();
            if (closing) completed |= 1 << ClosingCue;
            else ConsumeDue(elapsed, shiftSeconds);
            return;
        }
        if (!mayShow) return;
        if (active >= 0 && wasVisible) {
            Remaining = Math.Max(0, Remaining - Math.Max(0, visibleDelta));
            if (Remaining == 0) ClearActive();
        }
        if (active < 0) {
            int next = closing ? (Completed(ClosingCue) ? -1 : ClosingCue) : NextCue(elapsed, shiftSeconds);
            if (next >= 0) Begin(next);
        }
        visible = active >= 0;
    }

    public RadioScheduleSnapshot Capture() {
        return new RadioScheduleSnapshot { day=day, completedMask=completed, activeCue=active, remaining=Remaining };
    }

    void ClearActive() { active = -1; Remaining = 0; }
    bool Completed(int cue) { return (completed & (1 << cue)) != 0; }
    bool IsDue(int cue, double elapsed, double duration) { return keys[cue] != null && elapsed >= times[cue] * duration; }
    void Begin(int cue) { active = cue; completed |= 1 << cue; Remaining = CaptionSeconds; }
    int NextCue(double elapsed, double duration) {
        for (int i = 0; i < times.Length; i++) {
            if (!Completed(i) && IsDue(i, elapsed, duration)) return i;
        }
        return -1;
    }
    void ConsumeDue(double elapsed, double duration) {
        for (int i = 0; i < times.Length; i++) {
            if (IsDue(i, elapsed, duration)) completed |= 1 << i;
        }
    }

    static uint Seed(string campaign, int day) {
        uint hash = 2166136261u;
        foreach (char c in campaign ?? "") hash = unchecked((hash ^ c) * 16777619u);
        return unchecked((hash ^ (uint)day) * 16777619u);
    }

    static string ConditionKey(DailyConditionKind condition) {
        switch (condition) {
            case DailyConditionKind.Rain: return "rain";
            case DailyConditionKind.Market: return "market";
            case DailyConditionKind.Friday: return "friday";
            case DailyConditionKind.Construction: return "construction";
            default: return "weekday";
        }
    }
}
