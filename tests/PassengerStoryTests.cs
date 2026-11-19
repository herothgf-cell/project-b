using System;
using System.Collections.Generic;

public static class PassengerStoryTests {
    public static int Run() {
        int count = 0;
        Action<bool, string> check = (ok, label) => {
            if (!ok) throw new Exception(label);
            count++;
        };
        string original = ContentTuning.CaptureJson();
        try {
            ContentTuning.UseDefaults();
            check(PassengerStories.All.Count == 10, "exactly ten configured stories");
            check(PassengerStories.DailyMinimum == 2 && PassengerStories.DailyMaximum == 3, "daily story quota defaults");
            var roster = new HashSet<string>();
            foreach (var preset in PassengerPresetCatalog.Parse(ContentTuning.TextOrCatalog("passenger.presets"))) {
                if (preset.enabled) roster.Add(preset.id);
            }
            foreach (var story in PassengerStories.All) {
                check(roster.Contains(story.presetId), story.id + " preset is enabled");
                check(!string.IsNullOrWhiteSpace(story.title) && !string.IsNullOrWhiteSpace(story.intro), story.id + " has display text");
                var lines = new HashSet<string>();
                foreach (PassengerStoryEnding ending in Enum.GetValues(typeof(PassengerStoryEnding))) {
                    string line = story.Ending(ending);
                    check(!string.IsNullOrWhiteSpace(line) && lines.Add(line), story.id + " has distinct ending lines");
                    check(story.EndingFor(criterion => ending) == ending, story.id + " combines " + ending);
                }
                foreach (var failedCriterion in story.criteria) {
                    check(story.EndingFor(criterion => criterion == failedCriterion ? PassengerStoryEnding.Disappointing : PassengerStoryEnding.Good)
                        == PassengerStoryEnding.Disappointing, story.id + " fails when " + failedCriterion + " fails");
                }
                if (story.criteria.Count > 1) {
                    check(story.EndingFor(criterion => criterion == story.criteria[0] ? PassengerStoryEnding.Ordinary : PassengerStoryEnding.Good)
                        == PassengerStoryEnding.Ordinary, story.id + " partial evidence is ordinary");
                }
            }
            check(PassengerStories.Find("unknown") == null, "unknown story is not assigned");
            var student = PassengerStories.Find("last-train");
            check(student.request == RequestKind.FareHelp && student.secondaryRequest == RequestKind.Eta, "student has fare and ETA requests");
            var dinner = PassengerStories.Find("after-dinner");
            check(dinner.Weight(DailyConditionKind.Friday) > dinner.Weight(DailyConditionKind.Weekday), "Friday weights dinner");
            var moving = PassengerStories.Find("moving-day");
            check(moving.Weight(DailyConditionKind.Market) > moving.Weight(DailyConditionKind.Friday), "market weights luggage");

            string old = ContentTuning.CapturePvpJson();
            check(!old.Contains("\"story."), "v19 wire profile excludes all story settings");
            ContentTuning.UseJson(old);
            check(ContentTuning.CaptureJson() == old, "loading an old profile preserves its hash");
            check(PassengerStories.All.Count == 10 && PassengerStories.Find("night-shift").request == RequestKind.Quiet,
                "legacy profiles preserve shipped story defaults");
            string migrated = ContentTuning.UpgradeJson(old);
            check(migrated.Contains("\"story.logbook.cleanDays\""), "profile migration fills achievement defaults");
            ContentTuning.UseJson(migrated);

            ContentTuning.UseJson(Change("story.first-interview.title", "내가 쓴 첫 면접"));
            check(PassengerStories.Find("first-interview").title == "내가 쓴 첫 면접", "editing content refreshes definitions");
            foreach (var invalid in new[]{
                new KeyValuePair<string,string>("story.first-interview.request","LostItem"),
                new KeyValuePair<string,string>("story.first-interview.secondaryRequest","OnTime"),
                new KeyValuePair<string,string>("story.first-interview.criteria","Comfort,Comfort"),
                new KeyValuePair<string,string>("story.daily.minimum","4")
            }) {
                string before = ContentTuning.CaptureJson();
                bool refused = false;
                try { ContentTuning.UseJson(Change(invalid.Key, invalid.Value)); }
                catch (ArgumentException) { refused = true; }
                check(refused && ContentTuning.CaptureJson() == before, "invalid value is rejected atomically " + invalid.Key);
            }
        } finally {
            ContentTuning.UseJson(original);
        }
        return count;
    }

    static string Change(string key, string value) {
        var profile = ContentJson.Object(ContentJson.Parse(ContentTuning.CaptureJson()));
        foreach (var item in ContentJson.Array(profile["values"])) {
            var row = ContentJson.Object(item);
            if ((string)row["id"] == key) row["value"] = value;
        }
        profile["contentHash"] = ContentTuning.ComputeHash(profile);
        return ContentJson.Stringify(profile);
    }
}
