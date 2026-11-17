using System;
using System.Linq;
using System.Text;
using UnityEngine;

internal static partial class ConfirmedCorridorsR43
{
    private const string OrangeApproach = "local:5126:4691:7863:6307";
    private const string OrangeLeft = "reviewed:r43:O01:left";
    private static readonly string OrangeThrough = "editor:segment:" +
        Convert.ToBase64String(Encoding.UTF8.GetBytes(OrangeApproach)).TrimEnd('=') + ":r43-O01-SR";
    private static readonly string[] GreenSouthFeeders =
    {
        "local:7871:2385:7846:1840",
        "reviewed:J7917:middle:local:7795:2388:7771:1843",
        "local:7795:2388:7771:1843"
    };
    private static readonly string[] GreenSouthTerminals =
    {
        "editor:segment:bG9jYWw6NzgzMDoxNDc2Ojc4MTE6MTA1MQ:g04-sb-left",
        "reviewed:G04:SB:straight-1", "reviewed:G04:SB:straight-2", "reviewed:G04:SB:right"
    };

    private static void GreenCyanCheck(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException("R43_GREEN_CYAN: " + message);
    }

    private static RoadMapEvidence GreenCyanEvidence(string panorama, string note) => new RoadMapEvidence
    {
        state = "observed", month = "2026-05", checkedAt = "2026-11-17",
        appearanceConfirmed = true, ruleConfirmed = true,
        url = "https://map.naver.com/p?c=19,0,0,0,adh&p=" + panorama + ",Float", note = note
    };

    private static AssistedRoadNetwork.Path GreenCyanPath(RoadMapLane lane) =>
        new AssistedRoadNetwork.Path(lane.points.Select(p => p.V()).ToArray());

    private static RoadMapLane GreenCyanCopy(RoadMapLane lane) =>
        JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));

    private static void EditGreenCyan(RoadMapDocument document)
    {
        EditOrangeWestApproach(document);
        EditGreenSouthTransition(document);
        EditNatureForetCrossSections(document);
    }

    private static void EditNatureForetCrossSections(RoadMapDocument document)
    {
        const string west = "local:9032:10757:8270:6402";
        const string east = "editor:segment:bG9jYWw6ODI2NTo2MzA1OjkwNTY6MTA4NTE:c1b0d0a926574cd3992859201dc10032";
        var evidence = GreenCyanEvidence("EaiG_iKIiABlGZ1_r_n4nA,255,-19,85",
            "58: Nature Foret main gate has E2/W2 on the west side, E1/W3 on the east side, then E2/W1 toward the school. " +
            "Station placement and tapers are approximations. Existing driveway movements remain assigned to their original connectors.");
        evidence.ruleConfirmed = false;

        RoadMapLane AddInside(RoadMapLane head, string key, Vector3 startPoint, Vector3 endPoint, float taper)
        {
            var path = GreenCyanPath(head);
            float start = path.Project(startPoint, out _), end = path.Project(endPoint, out _);
            GreenCyanCheck(start > 1 && end < path.length - 1 && end - start > taper * 2, "58 invalid segment " + key);
            string id = "reviewed:C03:58:" + key;
            string lineage = "editor:segment:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(RoadMapAuthoringOperations.Origin(head))).TrimEnd('=') + ":r43-58-" + key;
            var outer = GreenCyanCopy(head);
            outer.id = lineage + "-outer";
            outer.sourceOffset += start;
            outer.fromNode = id + ":split";
            outer.toNode = id + ":merge";
            outer.number = 2;
            outer.width = 3.1f;
            Y06EastApproachEdit.Assign(outer, Y07MapEdit.Slice(path, start, end).Select(p => p.V()), evidence);
            var departure = GreenCyanCopy(head);
            departure.id = lineage + "-departure";
            departure.sourceOffset += end;
            departure.fromNode = outer.toNode;
            Y06EastApproachEdit.Assign(departure, Y07MapEdit.Slice(path, end, path.length).Select(p => p.V()), evidence);
            outer.next = new[] { departure.id };
            var inner = GreenCyanCopy(outer);
            inner.id = id;
            inner.sourceOffset = 0;
            inner.number = 1;
            int steps = Mathf.CeilToInt(end - start);
            Y06EastApproachEdit.Assign(inner, Enumerable.Range(0, steps + 1).Select(index =>
            {
                float station = Mathf.Lerp(start, end, index / (float)steps);
                float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Mathf.Min(station - start, end - station) / taper));
                return path.Position(station) - TrafficSystem.Right(path.Tangent(station)) * (3.1f * blend);
            }), evidence);
            Y06EastApproachEdit.Assign(head, G05NorthWestMapEdit.OriginalHead(path, start), evidence);
            head.toNode = outer.fromNode;
            head.next = new[] { outer.id, inner.id };
            document.lanes = document.lanes.Concat(new[] { outer, departure, inner }).ToArray();
            return inner;
        }

        Vector3 Point(float x, float z) => new Vector3(x, 0, z);
        var westMain = document.lanes.Single(l => l.id == west);
        var eastMain = document.lanes.Single(l => l.id == east);
        var middle = AddInside(westMain, "west-middle", Point(1073, 664.6f), Point(973, 632.7f), 15);
        AddInside(middle, "west-inner", Point(1055, 650), Point(1019, 637.1f), 10);
        AddInside(eastMain, "east-school", Point(1057, 644), Point(1113, 687), 13);
        AddInside(eastMain, "east-gate", Point(987, 624.64f), Point(1016, 629.2f), 8);
        RoadMapAuthoringOperations.RemapServiceBindings(document);
    }

    private static void EditOrangeWestApproach(RoadMapDocument document)
    {
        var evidence = GreenCyanEvidence("7FicJjaaPboSh9OnwrYR5g,92,-15,63",
            "O01 eastbound west of J8066. 2026-05 and 2025-10 show left-only and straight/right lanes; westbound remains one lane. " +
            "The photographed split lies between x740 and x759; the game starts an inner pocket around x742 with a 17m taper.");
        var lanes = document.lanes.ToDictionary(lane => lane.id);
        var head = lanes[OrangeApproach];
        var path = GreenCyanPath(head);
        int splitIndex = Array.FindIndex(head.points, point => point.x >= 742f);
        GreenCyanCheck(splitIndex > 0 && splitIndex < head.points.Length - 1, "O01 split vertex missing");
        float split = Enumerable.Range(1, splitIndex).Sum(index =>
            Vector3.Distance(path.points[index - 1], path.points[index]));
        GreenCyanCheck(!document.facilities.Any(facility => facility.laneId == head.id) &&
            !document.scene.objects.Any(item => item.IsService && item.laneId == head.id && item.laneAt >= split),
            "O01 requires an upstream facility remap");

        var through = GreenCyanCopy(head);
        through.id = OrangeThrough;
        through.sourceOffset += split;
        through.fromNode = "reviewed:r43:O01:split";
        through.number = 2;
        through.width = 3.25f;
        Y06EastApproachEdit.Assign(through, path.points.Skip(splitIndex), evidence);
        through.next = head.next.Where(id => lanes[id].turn >= 0).ToArray();

        var left = GreenCyanCopy(through);
        left.id = OrangeLeft;
        left.number = 1;
        left.toNode = left.id + ":end";
        int steps = Mathf.CeilToInt(path.length - split);
        Y06EastApproachEdit.Assign(left, Enumerable.Range(0, steps + 1).Select(index =>
        {
            if (index == 0) return path.points[splitIndex];
            float at = Mathf.Lerp(split, path.length, index / (float)steps);
            float offset = 3.3f * Mathf.SmoothStep(0, 1, (at - split) / 17f);
            return path.Position(at) - TrafficSystem.Right(path.Tangent(at)) * offset;
        }), evidence);
        var turn = lanes[head.next.Single(id => lanes[id].turn == -1)];
        left.next = new[] { turn.id };
        Y06EastApproachEdit.Reconnect(turn, left, lanes[turn.next.Single()], evidence);
        var surface = document.scene.objects.SingleOrDefault(item => item.id == "road:polish:" + turn.id);
        if (surface != null) surface.points = turn.points;

        head.toNode = through.fromNode;
        head.next = new[] { left.id, through.id };
        Y06EastApproachEdit.Assign(head, path.points.Take(splitIndex + 1), evidence);
        document.lanes = document.lanes.Concat(new[] { left, through }).ToArray();

        var stop = document.facilities.Single(facility => facility.id == "reviewed:2026-10-30:J8066_6364:0:-1:stop");
        var tangent = path.Tangent(path.length);
        var center = stop.Position - TrafficSystem.Right(tangent) * 1.65f;
        stop.x = center.x;
        stop.z = center.z;
        stop.width = 6.55f;
        stop.evidence = evidence;
    }

    private static void EditGreenSouthTransition(RoadMapDocument document)
    {
        var evidence = GreenCyanEvidence("7ms6I-SeMdaSj2rgpyWWdg,183,-20,85",
            "G10–G11 southbound. Three lanes are visible around z233 and four by z179–164. " +
            "The lead-in continues the existing 37 southbound left/U, straight, straight and right-only approach. " +
            "The z184–164 transition and lane centres fit the game corridor and are estimates.");
        var lanes = document.lanes.ToDictionary(lane => lane.id);
        var feeders = GreenSouthFeeders.Select(id => lanes[id]).ToArray();
        var terminals = GreenSouthTerminals.Select(id => lanes[id]).ToArray();
        string[] replacedHeads = { "local:7830:1476:7811:1051", "local:7754:1479:7736:1054" };
        var oldConnectors = feeders.Select(feed => lanes[feed.next.Single()]).ToArray();
        GreenCyanCheck(oldConnectors.All(join => join.turn == 0 && replacedHeads.Contains(join.next.Single())),
            "G10–G11 feeders no longer have the expected exits");
        GreenCyanCheck(!document.facilities.Any(facility => replacedHeads.Contains(facility.laneId)) &&
            !document.scene.objects.Any(item => item.IsService && replacedHeads.Contains(item.laneId)),
            "G10–G11 replaced heads contain a facility");
        foreach (var lane in oldConnectors.Concat(replacedHeads.Select(id => lanes[id])))
        {
            lane.disabled = true;
            lane.next = Array.Empty<string>();
        }

        int[] origins = { 0, 0, 1, 2 };
        float[] xAtFullWidth = { 785.60f, 782.35f, 779.10f, 775.85f };
        for (int index = 0; index < terminals.Length; index++)
        {
            var lane = terminals[index];
            var feeder = feeders[origins[index]];
            var from = feeder.points.Last().V();
            var to = lane.points.Last().V();
            lane.fromNode = feeder.toNode;
            lane.sourceOffset = 0;
            int span = Mathf.CeilToInt(from.z - to.z);
            const float widened = 164f;
            float transition = (from.z - widened) / (from.z - to.z);
            float adjustment = xAtFullWidth[index] - Mathf.Lerp(from.x, to.x, transition);
            Y06EastApproachEdit.Assign(lane, Enumerable.Range(0, span + 1).Select(step =>
            {
                float t = step / (float)span;
                float blend = t < transition ? Mathf.SmoothStep(0, 1, t / transition) :
                    Mathf.SmoothStep(1, 0, (t - transition) / (1 - transition));
                var point = Vector3.Lerp(from, to, t);
                point.x += adjustment * blend;
                return point;
            }), evidence);
        }

        for (int index = 0; index < feeders.Length; index++)
        {
            int origin = index;
            feeders[index].next = terminals.Where((_, i) => origins[i] == origin).Select(lane => lane.id).ToArray();
        }

        // Use the full-length southbound median-side lane as the source-family paint reference.
        var ordered = document.lanes.ToList();
        var left = terminals[0];
        ordered.Remove(left);
        ordered.Insert(ordered.FindIndex(lane => lane.sourceId == left.sourceId), left);
        document.lanes = ordered.ToArray();
    }

    private static void VerifyGreenCyan(MapData map)
    {
        var graph = new HdRoadGraph(map);
        var arrows = LaneDirectionPaint.Layout(map).ToLookup(arrow => map.hdEdges[arrow.edgeIndex].id);
        var approachIds = new[] { OrangeLeft, OrangeThrough }.Concat(GreenSouthTerminals).ToArray();
        int[] expected = { LaneDirectionPaint.Left, LaneDirectionPaint.Straight | LaneDirectionPaint.Right,
            LaneDirectionPaint.Left | LaneDirectionPaint.Uturn, LaneDirectionPaint.Straight,
            LaneDirectionPaint.Straight, LaneDirectionPaint.Right };
        for (int index = 0; index < approachIds.Length; index++)
        {
            var lane = graph.edges.Single(edge => edge.id == approachIds[index]);
            int mask = lane.next.Aggregate(0, (turns, next) => turns | UturnMovement.PaintMask(graph.edges[next].turn));
            GreenCyanCheck(mask == expected[index] && arrows[lane.id].Any(arrow => arrow.turns == mask),
                "Movement or painted arrow at " + lane.id);
            Y07MapTests.Footprint(map, lane);
        }

        var widths = GreenSouthTerminals.Select(id =>
        {
            var points = graph.edges.Single(edge => edge.id == id).points;
            int end = Array.FindIndex(points, point => point.z <= 164f);
            var a = points[end - 1];
            var b = points[end];
            return Mathf.Lerp(a.x, b.x, (a.z - 164f) / (a.z - b.z));
        }).ToArray();
        GreenCyanCheck(widths.Zip(widths.Skip(1), (a, b) => a - b).All(gap => gap >= 3.1f),
            "G10–G11 should already be four distinct lanes by z164");
        GreenCyanCheck(UturnMovement.Condition(map.editorDocument, GreenSouthTerminals[0])?.type ==
            UturnMovement.LeftOrPedestrianGreen, "37 signed southbound U-turn retained");
        var cyanOuter = graph.edges.Single(edge => edge.id == "local:8824:10924:7438:12714");
        GreenCyanCheck(cyanOuter.points.Any(p => p.z > 1180) && cyanOuter.next.Any(i => graph.edges[i].turn == 0),
            "57 outer northwestern lane stays through after the crossing");
        var nature = graph.edges.Where(edge => !edge.disabled && edge.id.StartsWith("reviewed:C03:58:")).ToArray();
        GreenCyanCheck(nature.Any(edge => edge.id.EndsWith(":west-inner")) && nature.Any(edge => edge.id.EndsWith(":east-school")) &&
            nature.Any(edge => edge.id.EndsWith(":east-gate")), "58 missing the lane changes around Nature Foret");
        foreach (var lane in nature) Y07MapTests.Footprint(map, lane);
        foreach (var (x, minZ, maxZ, east, west) in new[] {(1002f, 620f, 642f, 2, 2), (1036f, 631f, 652f, 1, 3), (1090f, 660f, 685f, 2, 1)})
        {
            var directions = graph.edges.Where(edge => !edge.disabled && edge.turn == 0 && edge.change == 0)
                .Select(edge => (edge.id, sign: edge.points.Zip(edge.points.Skip(1), (a, b) =>
                {
                    if (Mathf.Abs(b.x - a.x) < .01f || x < Mathf.Min(a.x, b.x) || x > Mathf.Max(a.x, b.x)) return 0f;
                    float z = Mathf.Lerp(a.z, b.z, (x - a.x) / (b.x - a.x));
                    return z > minZ && z < maxZ ? Mathf.Sign(b.x - a.x) : 0f;
                }).FirstOrDefault(sign => sign != 0f)))
                .Where(lane => lane.sign != 0f).ToArray();
            GreenCyanCheck(directions.Count(lane => lane.sign > 0) == east && directions.Count(lane => lane.sign < 0) == west,
                $"58 unexpected lane allocation at x{x}: {string.Join(", ", directions.Select(lane => $"{lane.id}={lane.sign}"))}");
        }
        Debug.Log("R43_GREEN_CYAN_OK O01 east 1→2, G10–G11 south 3→4, C01 outer-through, C03 Nature Foret cross-sections");
    }

}
