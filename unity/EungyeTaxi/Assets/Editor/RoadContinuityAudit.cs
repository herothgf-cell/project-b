using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class RoadContinuityAudit
{
    public static void BuildVerified()
    {
        Run();
        PrototypeBuild.BuildHybrid();
    }
    public static void Run()
    {
        RoadPaintCurveTests.Run();
        RoadPaintStrokeContinuityTests.Run();
        CrosswalkEndpointAudit.Run();
        ReviewedUnmarkedRoadPaintTests.Run();
        ReviewedNativeLanePresentationTests.Run();
        var package = ProjectMapStore.Load(ProjectMapBuild.Root);
        var map = package.Compose();
        EungyeConnectivityRepair.Apply(map, package.baseline);
        var paint = new List<Vector3>();
        RoadPaintGeometry.Paint(map, null, (name, point, direction, width, length, yellow) =>
        {
            if (name == "Centre line" || name == "Lane divider") paint.Add(point);
        });
        var layout = new RoadPaintGeometry(map);
        var clearances = RoadJunctionGeometry.PaintClearances(map);
        var openings = RoadJunctionGeometry.Build(map);
        openings.RemoveAll(o => clearances.Any(c => c.kind == "core" && (c.center-o.center).sqrMagnitude < 4));
        var crossings = ReviewedCrosswalkGeometry.Build(map);
        var unmarked = new ReviewedUnmarkedRoadPaint(map);
        var native = new ReviewedNativeLanePresentation(map);
        var uturns = UturnRoadMarkings.Build(map);
        var report = new List<string> { "kind,lane,x,z" };
        int samples = 0;
        foreach (var edge in map.hdEdges.Where(e => !e.disabled && e.status == "provisional" && e.turn == 0 && e.change == 0 && !e.id.StartsWith("join:")))
        {
            var path = new AssistedRoadNetwork.Path(edge.points.Select(p => p.V()).ToArray());
            for (float at = 5; at < path.length - 5; at += 5)
            {
                var p = path.Position(at);
                var direction = path.Tangent(at);
                foreach (var boundary in RoadGeometry.Boundaries(layout.Lanes(edge, p, direction)))
                {
                    var q = p + TrafficSystem.Right(direction) * boundary.offset;
                    if (boundary.yellow) q = ReviewedLaneLayout.Centreline(map, edge.sourceId, q);
                    string name = boundary.yellow ? "Centre line" : "Lane divider";
                    if (boundary.yellow && UturnRoadMarkings.Covers(uturns, q, .5f)) continue;
                    if (RoadPaintGeometry.InOpening(q, clearances, openings, crossings, 1) ||
                        unmarked.SuppressesSynthetic(name, q, direction, .14f, 1) || native.SuppressesSynthetic(name, q, direction)) continue;
                    samples++;
                    if (!paint.Any(mark => (mark-q).sqrMagnitude < 16))
                        report.Add($"missing,{edge.id.Replace(',', ';')},{q.x:F2},{q.z:F2}");
                }
            }
        }
        var output = Path.Combine(ProjectMapBuild.Root, "artifacts/color-road-survey/road-continuity.csv");
        File.WriteAllLines(output, report);
        Debug.Log($"ROAD_CONTINUITY_AUDIT samples={samples} missing={report.Count-1} paint={paint.Count} report={output}");
        if (samples == 0 || report.Count != 1)
            throw new InvalidOperationException("Road paint continuity audit has uncovered samples. Inspect " + output);
    }
}
