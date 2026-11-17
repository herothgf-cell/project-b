using System;
using System.IO;
using System.Linq;
using UnityEngine;

internal static partial class ConfirmedCorridorsR43
{
    private const string PreviousHash = "0d7c0353d3bf477d7ab1d3ea1c6c8dc35a7281e30c10ccafd6f3c3b343f5633b";

    public static void Apply()
    {
        string root = ProjectMapBuild.Root;
        string token = ProjectMapStore.Fingerprint(root);
        var previous = ProjectMapStore.Load(root);
        var candidate = CreateCandidate(previous);
        Verify(candidate);
        ProjectMapStore.Save(root, candidate, token);
        var saved = ProjectMapStore.Load(root);
        RequireR43(saved.contentHash == candidate.contentHash && saved.baselineHash == previous.baselineHash,
            "Map identity changed while saving");
        Verify(saved);
        ProjectMapBuild.Prepare();
        Debug.Log("R43_CORRIDORS_APPLIED revision=43 contentHash=" + saved.contentHash);
    }

    public static void Preview()
    {
        ReviewedRoadFacilitiesTests.RunPedestrianRail();
        var candidate = CreateCandidate(ProjectMapStore.Load(ProjectMapBuild.Root));
        Verify(candidate);
        ConfirmedLaneSurveyR42.VerifyPackage(candidate);
        Y05CorridorMapEdit.Validate(candidate);
    }

    public static void Verify()
    {
        Verify(ProjectMapStore.Load(ProjectMapBuild.Root));
    }

    public static void BuildReviewed()
    {
        ReviewedRoadFacilitiesTests.RunTemporaryBarrier();
        ReviewedRoadFacilitiesTests.RunPedestrianRail();
        Verify();
        ConfirmedLaneSurveyR42.BuildReviewed();
    }

    private static ProjectMapPackage CreateCandidate(ProjectMapPackage previous)
    {
        RequireR43(previous.document.revision == 42 && previous.contentHash == PreviousHash, "Expected the reviewed r42 input map");
        var document = previous.document.Copy();
        EditGreenCyan(document);
        EditPink(document);
        EditYellow(document);
        document.revision = 43;
        document.projectRevision = "";

        var errors = RoadMapValidator.Validate(previous.baseline, document).Where(issue => issue.error).ToArray();
        RequireR43(errors.Length == 0, string.Join(" / ", errors.Take(15).Select(issue => issue + " at " + issue.position.ToString("F2"))));
        return ProjectMapPackage.Create(previous.baseline, document);
    }

    private static void Verify(ProjectMapPackage package)
    {
        var map = package.Compose();
        EungyeConnectivityRepair.Apply(map, package.baseline);
        VerifyGreenCyan(map);
        VerifyPink(map);
        VerifyYellow(map);
        var graph = new HdRoadGraph(map);
        VerifyFacilityClearance(map, graph);

        var catalog = PassengerStopCatalog.For(map);
        var stops = catalog.Stops;
        RequireR43(stops.Length == 39, "Expected 39 passenger stops, found " + stops.Length + ": " + string.Join(" / ", catalog.Rejected));
        foreach (var start in stops)
        foreach (var destination in stops)
        {
            if (start != destination)
                RequireR43(graph.Route(start.laneEdge, start.laneAt, destination.laneEdge, destination.laneAt) != null,
                    "Lost route " + start.name + " → " + destination.name);
        }
        Debug.Log("R43_CORRIDORS_OK stops=39 routes=1482");
    }

    private static void VerifyFacilityClearance(MapData map, HdRoadGraph graph)
    {
        string path = Path.Combine(ProjectMapBuild.Root, "config/maps/eungye-road-facilities.json");
        var catalog = JsonUtility.FromJson<ReviewedRoadFacilities.Catalog>(File.ReadAllText(path));
        var surveyed = catalog.facilities.Where(facility => facility.checkedAt == "2026-11-17").ToArray();
        RequireR43(surveyed.Length > 0 && surveyed.All(facility => !string.IsNullOrEmpty(facility.sourceUrl) &&
            !string.IsNullOrEmpty(facility.captureMonth) && facility.segments != null && facility.segments.Length > 0),
            "New facilities need a source and explicit endpoints");
        RequireR43(catalog.facilities.Select(facility => facility.id).Distinct().Count() == catalog.facilities.Length,
            "Duplicate reviewed facility identifier");
        var layout = ReviewedRoadFacilities.Plan(map,
            new ReviewedRoadFacilities.Catalog { mapId = catalog.mapId, facilities = surveyed });
        var lines = layout.barriers.Concat(layout.bollards).ToArray();
        RequireR43(surveyed.All(facility => lines.Any(line => line.facilityId == facility.id)), "Surveyed facility is missing from runtime layout");
        var crossings = ReviewedCrosswalkGeometry.Build(map);
        foreach (var line in lines)
        {
            int stations = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(line.a, line.b) * 4));
            for (int index = 0; index <= stations; index++)
            {
                Vector3 point = Vector3.Lerp(line.a, line.b, (float)index / stations);
                RequireR43(point.x >= 0 && point.x <= map.width && point.z >= 0 && point.z <= map.depth &&
                    line.width > 0 && line.height > 0, "Invalid facility dimensions or position " + line.facilityId);
                var blocking = crossings.FirstOrDefault(zone => zone.InBand(point, line.width * .5f + .15f));
                RequireR43(blocking == null, "Facility overlaps pedestrian crossing " + line.facilityId + " at " + point.ToString("F2"));
            }
        }

        var root = new GameObject("r43 surveyed facilities collision test");
        try
        {
            ReviewedRoadFacilities.Draw(layout, root.transform, true);
            Physics.SyncTransforms();
            var hits = new Collider[64];
            int tested = 0;
            for (int edge = 0; edge < graph.edges.Length; edge++)
            {
                if (graph.edges[edge].disabled) continue;
                var lane = graph.paths[edge];
                for (float at = 0; at <= lane.length; at += .5f)
                {
                    Vector3 point = lane.Position(at);
                    var closest = lines.FirstOrDefault(line => ReviewedRoadFacilitiesTests.PointDistance(point, line.a, line.b) < 5);
                    if (closest == null) continue;
                    var heading = Quaternion.LookRotation(lane.Tangent(at));
                    int count = Physics.OverlapBoxNonAlloc(point + Vector3.up * .85f, new Vector3(1.05f, .55f, 2.3f),
                        hits, heading, ~0, QueryTriggerInteraction.Ignore);
                    for (int index = 0; index < count; index++)
                    {
                        var hit = hits[index];
                        RequireR43(!hit.transform.IsChildOf(root.transform), "New facility touches taxi swept area " + hit.name +
                            " / " + graph.edges[edge].id + " at " + point.ToString("F2"));
                    }
                    tested++;
                }
            }
            RequireR43(tested > 100, "No nearby vehicle samples found");
            Debug.Log($"R43_FACILITIES_OK sites={surveyed.Length} rails={layout.barriers.Count} bollards={layout.bollards.Count} taxiSamples={tested}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void RequireR43(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("R43_CORRIDORS: " + message);
    }
}
