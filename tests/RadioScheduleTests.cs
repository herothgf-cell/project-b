using System;
using System.Collections.Generic;

public static class RadioScheduleTests {
    public static int Run() {
        int count = 0;
        Action<bool, string> check = (ok, label) => {
            if (!ok) throw new Exception(label);
            count++;
        };
        const double duration = 1200;
        var radio = new RadioSchedule("market-fixture", 2, DailyConditionKind.Market, 0, duration, 100);
        check(radio.NeedsPresentation(0, duration, false), "opening needs a HUD visibility check");
        radio.Tick(0, duration, false, true, true, 0);
        check(radio.VisibleKey == "radio.morning.market", "opening reflects market card");
        check(radio.Remaining == 6, "opening starts with six displayed seconds");
        radio.Tick(2, duration, false, true, true, 2);
        check(radio.Remaining == 4, "visible seconds spend lifetime");
        radio.Tick(2, duration, false, false, true, 100);
        check(radio.VisibleKey == "" && radio.Remaining == 4, "modal or dialogue hides and preserves active cue");
        var saved = radio.Capture();
        saved.Validate(2);

        radio = new RadioSchedule("market-fixture", 2, DailyConditionKind.Market, 2, duration, 100, saved);
        radio.Tick(2, duration, false, true, true, 100);
        check(radio.VisibleKey == "radio.morning.market" && radio.Remaining == 4, "resume keeps remaining lifetime");
        radio.Tick(6, duration, false, true, true, 4);
        check(radio.VisibleKey == "", "opening disappears after six displayed seconds");
        check(!radio.NeedsPresentation(6, duration, false), "silent periods need no HUD visibility check");
        radio = new RadioSchedule("market-fixture", 2, DailyConditionKind.Market, 6, duration, 100, radio.Capture());
        radio.Tick(6, duration, false, true, true, 0);
        check(radio.VisibleKey == "", "finished greeting never repeats when resuming");

        radio.Tick(1000, duration, false, true, true, 0);
        var scheduled = new List<string>();
        while (radio.VisibleKey.Length > 0 && scheduled.Count < 7) {
            scheduled.Add(radio.VisibleKey);
            radio.Tick(1000, duration, false, true, true, 6);
        }
        check(scheduled.Count == 4, "full day has two traffic cues, news and a letter");
        check(scheduled[0] == "radio.traffic.regular" && scheduled[1] == "radio.news.market"
              && scheduled[2].StartsWith("radio.letter.") && scheduled[3] == "radio.traffic.evening",
            "market timeline uses ordered context and seeded letter");
        radio.Tick(1000, duration, true, true, true, 0);
        check(radio.VisibleKey == "radio.closing", "settlement transition shows closing");
        radio.Tick(1000, duration, true, true, true, 6);
        check(radio.VisibleKey == "", "closing expires without advancing the game clock");

        var legacy = new RadioSchedule("previous", 1, DailyConditionKind.Construction, 500, duration, 0);
        legacy.Tick(500, duration, false, true, true, 0);
        check(legacy.VisibleKey == "", "legacy resume suppresses elapsed broadcasts");
        var turnedOff = new RadioSchedule("previous", 1, DailyConditionKind.Rain, 0, duration, 0);
        turnedOff.Tick(300, duration, false, true, false, 0);
        turnedOff.Tick(300, duration, false, true, true, 0);
        check(turnedOff.VisibleKey == "", "disabled subtitles discard due broadcasts");
        turnedOff.Tick(480, duration, false, true, true, 0);
        check(turnedOff.VisibleKey == "radio.news.regular", "future cues respect the changed preference");
        turnedOff.Tick(480, duration, true, true, true, 0);
        check(turnedOff.VisibleKey == "radio.closing", "closing supersedes an in-progress news caption");

        var sight = new RadioSchedule("construction", 1, DailyConditionKind.Construction, 0, duration, 0);
        sight.Tick(0, duration, false, true, true, 0);
        check(sight.VisibleKey == "radio.morning.construction", "construction greeting");
        sight.Tick(180, duration, false, true, true, 6);
        check(sight.VisibleKey == "radio.traffic.construction", "construction names a real affected route");
        sight.Tick(1100, duration, false, true, true, 6);
        check(sight.VisibleKey == "radio.news.regular", "construction retains regular local news");
        sight.Tick(1100, duration, false, true, true, 6);
        check(sight.VisibleKey.StartsWith("radio.letter."), "construction retains a seeded letter");
        sight.Tick(1100, duration, false, true, true, 6);
        check(sight.VisibleKey == "", "zero chance removes the second traffic cue");

        bool rejected = false;
        try { saved.Validate(3); }
        catch (ArgumentException) { rejected = true; }
        check(rejected, "snapshot rejects a different operating day");
        check(!ContentTuning.CapturePvpJson().Contains("\"radio."), "PVP wire profile omits radio keys");
        check(ContentTuning.TextOrCatalog("radio.morning.rain").Contains("비 소식"), "rain line is loaded from the profile");
        return count;
    }
}
