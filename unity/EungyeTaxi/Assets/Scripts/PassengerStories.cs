using System;
using System.Collections.Generic;

public enum PassengerStoryEnding { Good=0, Ordinary=1, Disappointing=2 }

public enum PassengerStoryCriterion {
    OnTime, Comfort, LargeRoad, Quiet, FareHelp, ViaStop, SafeStop,
    Luggage, Protection, Conversation, Safety, Empathy
}

public sealed class PassengerStoryDefinition {
    public readonly string id, title, presetId, intro;
    public readonly RequestKind request;
    public readonly RequestKind? secondaryRequest;
    public readonly IReadOnlyList<PassengerStoryCriterion> criteria;
    readonly string good, ordinary, disappointing;
    readonly double fridayWeight, marketWeight;

    internal PassengerStoryDefinition(string id, string title) {
        this.id = id;
        this.title = title;
        string key = "story." + id + ".";
        presetId = ContentTuning.TextOrCatalog(key + "presetId");
        intro = ContentTuning.TextOrCatalog(key + "intro");
        request = (RequestKind)Enum.Parse(typeof(RequestKind), ContentTuning.TextOrCatalog(key + "request"));
        string secondary = ContentTuning.TextOrCatalog(key + "secondaryRequest");
        if (!string.IsNullOrEmpty(secondary) && secondary != "None")
            secondaryRequest = (RequestKind)Enum.Parse(typeof(RequestKind), secondary);

        var evidence = ContentTuning.TextOrCatalog(key + "criteria").Split(',');
        criteria = Array.AsReadOnly(Array.ConvertAll(evidence, name =>
            (PassengerStoryCriterion)Enum.Parse(typeof(PassengerStoryCriterion), name.Trim())));
        good = ContentTuning.TextOrCatalog(key + "ending.good");
        ordinary = ContentTuning.TextOrCatalog(key + "ending.ordinary");
        disappointing = ContentTuning.TextOrCatalog(key + "ending.disappointing");
        fridayWeight = ContentTuning.Number(key + "fridayWeight", 1);
        marketWeight = ContentTuning.Number(key + "marketWeight", 1);
    }

    public string Ending(PassengerStoryEnding ending) {
        if (ending == PassengerStoryEnding.Good) return good;
        return ending == PassengerStoryEnding.Disappointing ? disappointing : ordinary;
    }

    public PassengerStoryEnding EndingFor(Func<PassengerStoryCriterion, PassengerStoryEnding> result) {
        var ending = PassengerStoryEnding.Good;
        foreach (var criterion in criteria) {
            var observed = result(criterion);
            if (observed == PassengerStoryEnding.Disappointing) return observed;
            if (observed != PassengerStoryEnding.Good) ending = PassengerStoryEnding.Ordinary;
        }
        return ending;
    }

    public double Weight(DailyConditionKind condition) {
        if (condition == DailyConditionKind.Friday) return fridayWeight;
        return condition == DailyConditionKind.Market ? marketWeight : 1;
    }
}

public static class PassengerStories {
    // Published story order and stable IDs for the saved logbook.
    static readonly string[] Ids = {
        "first-interview", "feverish-child", "moving-day", "night-shift", "last-train",
        "wedding-guest", "plant-gift", "after-dinner", "trip-departure", "rough-day"
    };
    static string previousProfile;
    static IReadOnlyList<PassengerStoryDefinition> definitions;

    public static int DailyMinimum { get { return ContentTuning.Int("story.daily.minimum", 2); } }
    public static int DailyMaximum { get { return ContentTuning.Int("story.daily.maximum", 3); } }
    public static string StudentFareChoice {
        get { return ContentTuning.Text("story.last-train.fareChoice", "도착하면 보호자나 친구에게 연락해 볼까요?"); }
    }

    public static IReadOnlyList<PassengerStoryDefinition> All {
        get {
            string profile = ContentTuning.CaptureJson();
            if (ReferenceEquals(profile, previousProfile)) return definitions;
            var result = new List<PassengerStoryDefinition>(Ids.Length);
            foreach (string id in Ids) {
                string title = ContentTuning.TextOrCatalog("story." + id + ".title");
                if (title.Length > 0) result.Add(new PassengerStoryDefinition(id, title));
            }
            definitions = result.AsReadOnly();
            previousProfile = profile;
            return definitions;
        }
    }

    public static PassengerStoryDefinition Find(string id) {
        foreach (var definition in All) {
            if (definition.id == id) return definition;
        }
        return null;
    }
}
