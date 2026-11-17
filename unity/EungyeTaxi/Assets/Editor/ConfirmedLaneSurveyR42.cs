using System;
using System.Linq;
using UnityEngine;

internal static partial class ConfirmedLaneSurveyR42
{
    private const string PreviousHash = "57ecdb77d3b473ac1e86e83c02a45219babce1118c3f36ea4d68919a771f12fe";

    public static void Apply()
    {
        string root = ProjectMapBuild.Root;
        string token = ProjectMapStore.Fingerprint(root);
        var previous = ProjectMapStore.Load(root);
        Check(previous.document.revision == 41 && previous.contentHash == PreviousHash, "Expected the reviewed r41 map");

        var document = previous.document.Copy();
        EditYellow(document);
        EditGreen(document);
        EditPink(document);
        document.revision = 42;
        document.projectRevision = "";
        document.savedAt = DateTime.UtcNow.ToString("o");

        var errors = RoadMapValidator.Validate(previous.baseline, document).Where(issue => issue.error).ToArray();
        Check(errors.Length == 0, string.Join(" / ", errors.Take(15).Select(issue => issue + " at " + issue.position.ToString("F2"))));
        var package = ProjectMapPackage.Create(previous.baseline, document);
        VerifyPackage(package);
        ProjectMapStore.Save(root, package, token);

        var saved = ProjectMapStore.Load(root);
        Check(saved.contentHash == package.contentHash && saved.baselineHash == previous.baselineHash, "Saved map identity changed");
        VerifyPackage(saved);
        ProjectMapBuild.Prepare();
        Debug.Log("CONFIRMED_LANE_SURVEY_APPLIED revision=42 contentHash=" + saved.contentHash);
    }

    public static void Verify()
    {
        VerifyPackage(ProjectMapStore.Load(ProjectMapBuild.Root));
    }

    public static void BuildReviewed()
    {
        Verify();
        Y05CorridorMapEdit.BuildReviewed();
    }

    internal static void VerifyPackage(ProjectMapPackage package)
    {
        VerifyYellow(package.document);
        VerifyGreen(package.document);
        VerifyPink(package.document);

        var map = package.Compose();
        EungyeConnectivityRepair.Apply(map, package.baseline);
        VerifyYellowRuntime(map);
        VerifyGreenRuntime(map);
        VerifyPinkRuntime(map);

        var graph = new HdRoadGraph(map);
        var stops = PassengerStopCatalog.For(map).Stops;
        Check(stops.Length == 39, "Expected 39 passenger stops");
        foreach (var start in stops)
        foreach (var destination in stops)
        {
            if (start != destination)
                Check(graph.Route(start.laneEdge, start.laneAt, destination.laneEdge, destination.laneAt) != null,
                    "Lost route " + start.name + " → " + destination.name);
        }

        Debug.Log("CONFIRMED_LANE_SURVEY_OK markers=46,47,49,50,51,52,53,54 stops=39 routes=1482");
    }

    private static void Check(bool result, string message)
    {
        if (!result) throw new InvalidOperationException("CONFIRMED_LANE_SURVEY: " + message);
    }
}
