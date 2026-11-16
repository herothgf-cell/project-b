using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

internal static partial class ConfirmedLaneSurveyR42
{
    private const string DaeunInner = "local:3361:12816:2684:12355";
    private const string DaeunOuter = "local:3323:12871:2647:12412";
    private const string CentralInner = "local:6021:16841:7200:14988";
    private const string CentralOuter = "local:5992:16789:7141:14981";

    private static void VerifyYellow(RoadMapDocument document)
    {
        var lanes = document.lanes.ToDictionary(lane => lane.id);
        VerifyYellowApproach(lanes, 46, DaeunInner, DaeunOuter, new[] { -1 }, new[] { 0 }, new[] { 0, 1 });
        VerifyYellowApproach(lanes, 47, CentralInner, CentralOuter, new[] { -1 }, new[] { 0 }, new[] { 1 });
        var fromOuter = lanes[YellowPrefix(47) + "change-to-straight"];
        var straight = lanes[YellowPrefix(47) + "straight"];
        YellowCheck(!fromOuter.disabled && fromOuter.change == -1 && fromOuter.next.SequenceEqual(new[] { straight.id })
            && lanes[CentralOuter + ":part:2"].next.Contains(fromOuter.id)
            && Vector3.Distance(YellowPosition(fromOuter, fromOuter.points.Length - 1), YellowPosition(straight, 0)) < .02f,
            "47 upstream west-side lane must retain access to straight through an approach lane change");
    }

    private static void VerifyYellowApproach(Dictionary<string, RoadMapLane> lanes, int marker, string innerRoot,
        string outerRoot, params int[][] allowed)
    {
        var terminals = YellowTerminals(marker, innerRoot, outerRoot).Select(id => lanes[id]).ToArray();
        var right = TrafficSystem.Right(RoadMapEasySelection.Heading(terminals[0]));
        for (int i = 0; i < terminals.Length; i++)
        {
            var lane = terminals[i];
            var joins = lane.next.Select(id => lanes[id]).ToArray();
            YellowCheck(!lane.disabled && lane.number == i + 1 && joins.Length > 0
                && joins.All(join => !join.disabled && join.ruleId == lane.ruleId && join.change == 0
                    && join.id.StartsWith("join:", StringComparison.Ordinal) && join.next.Length > 0),
                $"{marker} lane {i + 1} is an active movement approach");
            YellowCheck(joins.Select(join => join.turn).Distinct().OrderBy(turn => turn).SequenceEqual(allowed[i]),
                $"{marker} lane {i + 1} has only its observed turns");
            if (i > 0)
            {
                var neighbour = terminals[i - 1];
                float spacing = Vector3.Dot(YellowPosition(lane, lane.points.Length - 1)
                    - YellowPosition(neighbour, neighbour.points.Length - 1), right);
                YellowCheck(spacing >= 2.6f && spacing < 3.7f, $"{marker} lane {i + 1} ordered physical spacing");
            }
        }
    }

    private static void VerifyYellowRuntime(MapData map)
    {
        var graph = new HdRoadGraph(map);
        var arrows = LaneDirectionPaint.Layout(map).ToLookup(arrow => graph.edges[arrow.edgeIndex].id);
        var terminalIds = YellowTerminals(46, DaeunInner, DaeunOuter)
            .Concat(YellowTerminals(47, CentralInner, CentralOuter)).ToArray();
        int[] expected = { LaneDirectionPaint.Left, LaneDirectionPaint.Straight, LaneDirectionPaint.Straight | LaneDirectionPaint.Right,
            LaneDirectionPaint.Left, LaneDirectionPaint.Straight, LaneDirectionPaint.Right };
        for (int i = 0; i < terminalIds.Length; i++)
        {
            var edge = graph.edges.Single(lane => lane.id == terminalIds[i]);
            var turns = edge.next.Select(index => graph.edges[index]).Where(join => !join.disabled).ToArray();
            int mask = turns.Aggregate(0, (value, join) => value | UturnMovement.PaintMask(join.turn));
            YellowCheck(mask == expected[i] && arrows[edge.id].Any(arrow => arrow.turns == mask),
                $"{edge.id} has matching navigation and painted permissions");
            Y07MapTests.Footprint(map, edge);
            foreach (var turn in turns)
                Y07MapTests.Footprint(map, turn);
        }

        foreach (var edge in graph.edges.Where(lane => lane.id == YellowPrefix(46) + "transition-to-straight"
            || lane.id == YellowPrefix(47) + "transition-to-straight" || lane.id == YellowPrefix(47) + "change-to-straight"))
            Y07MapTests.Footprint(map, edge);
        Debug.Log("R42_YELLOW_VERIFIED 46=L/S/SR 47=L/S/R painted=true footprint=true");
    }

    private static void EditYellow(RoadMapDocument document)
    {
        EditYellowApproach(document, 46, DaeunInner, DaeunOuter, 34f, 15f, false,
            "zyQLy0iNXk5WBzbE4WYJJA,235,-20,80", "Daeun-ro southwestbound: left / straight / straight-right.");
        EditYellowApproach(document, 47, CentralInner, CentralOuter, 55f, 25f, true,
            "N7PjR0zf8DCG420DRWF3Qg,260,-20,80", "Eungyejungang-ro southbound: left / straight / right.");
    }

    private static void EditYellowApproach(RoadMapDocument document, int marker, string innerRoot, string outerRoot,
        float approach, float taper, bool outerRightOnly, string panorama, string observation)
    {
        var lanes = document.lanes.ToDictionary(lane => lane.id);
        var inner = lanes[innerRoot + ":part:2"];
        var outer = lanes[outerRoot + ":part:2"];
        var innerPath = YellowPath(inner);
        var outerPath = YellowPath(outer);
        var prefix = YellowPrefix(marker);
        float innerCut = innerPath.length - approach;
        float outerCut = outerPath.length - approach;
        float spacing = Vector3.Dot(outerPath.Position(outerPath.length) - innerPath.Position(innerPath.length),
            TrafficSystem.Right(innerPath.Tangent(innerPath.length))) * .5f;
        YellowCheck(innerCut > 1f && outerCut > 1f && spacing >= 2.6f && spacing < 3.7f,
            $"{marker} calibrated approach station or width changed");
        var evidence = new RoadMapEvidence
        {
            state = "observed", month = "2026-05", checkedAt = "2026-11-16", appearanceConfirmed = true, ruleConfirmed = true,
            url = "https://map.naver.com/p?c=18,0,0,0,adh&p=" + panorama + ",Float",
            note = $"Marker {marker}, user-confirmed {observation} Approximate {approach:0}m approach and {taper:0}m taper on existing road surface."
        };
        var ids = YellowTerminals(marker, innerRoot, outerRoot);
        var left = YellowTail(inner, innerPath, innerCut, ids[0], prefix + "split-inner", 1, spacing, evidence);
        var right = YellowTail(outer, outerPath, outerCut, ids[2], prefix + "split-outer", 3, spacing, evidence);
        var straight = YellowCopy(inner);
        straight.id = ids[1];
        straight.fromNode = prefix + "straight-start";
        straight.toNode = prefix + "straight-end";
        straight.number = 2;
        straight.width = spacing;
        straight.sourceOffset = 0;

        Vector3 Inner(float station) => innerPath.Position(innerCut + station);
        Vector3 Outer(float station) => outerPath.Position(outerCut + station);
        Vector3 Centre(float station) => Vector3.Lerp(Inner(station), Outer(station), .5f);

        var innerTransition = YellowCopy(straight);
        innerTransition.id = prefix + "transition-to-straight";
        innerTransition.fromNode = left.fromNode;
        innerTransition.toNode = straight.fromNode;
        innerTransition.next = new[] { straight.id };
        Y06EastApproachEdit.Assign(innerTransition, YellowSample(0, taper,
            station => Vector3.Lerp(Inner(station), Centre(station), Mathf.SmoothStep(0, 1, station / taper))), evidence);
        Y06EastApproachEdit.Assign(straight, YellowSample(taper, approach, Centre), evidence);

        var straightJoin = lanes[$"join:{innerRoot}:0"];
        Y06EastApproachEdit.Reconnect(straightJoin, straight, lanes[straightJoin.next.Single()], evidence);
        straight.next = new[] { straightJoin.id };
        left.next = left.next.Where(id => lanes[id].turn == -1).ToArray();
        right.next = right.next.Where(id => !outerRightOnly || lanes[id].turn == 1).ToArray();
        inner.next = new[] { left.id, innerTransition.id };
        outer.next = new[] { right.id };
        foreach (var turn in left.next.Concat(right.next).Select(id => lanes[id]))
            turn.evidence = evidence;

        var added = new List<RoadMapLane> { left, innerTransition, straight, right };
        if (outerRightOnly)
        {
            var removed = lanes[$"join:{outerRoot}:0"];
            YellowCheck(!document.lanes.Any(lane => lane != outer && lane != removed && lane.next.Contains(removed.id)),
                $"{marker} previous outer straight connector has another approach");
            removed.disabled = true;
            removed.next = Array.Empty<string>();

            var laneChange = YellowCopy(innerTransition);
            laneChange.id = prefix + "change-to-straight";
            laneChange.fromNode = right.fromNode;
            laneChange.change = -1;
            Y06EastApproachEdit.Assign(laneChange, YellowSample(0, taper,
                station => Vector3.Lerp(Outer(station), Centre(station), Mathf.SmoothStep(0, 1, station / taper))), evidence);
            outer.next = outer.next.Append(laneChange.id).ToArray();
            added.Add(laneChange);
        }
        document.lanes = document.lanes.Concat(added).ToArray();

        foreach (var lane in new[] { straight, straightJoin, innerTransition })
        {
            string surfaceId = "road:polish:" + lane.id;
            var surface = document.scene.objects.SingleOrDefault(item => item.id == surfaceId);
            if (surface == null)
            {
                surface = new ProjectMapObject { id = surfaceId, kind = "road", name = lane.roadName, lanes = 1, oneWay = true, suppressPaint = true };
                document.scene.objects = document.scene.objects.Append(surface).ToArray();
            }
            surface.width = 3.8f;
            surface.points = lane.points;
        }
    }

    private static RoadMapLane YellowTail(RoadMapLane head, AssistedRoadNetwork.Path path, float cut,
        string id, string node, int number, float width, RoadMapEvidence evidence)
    {
        var tail = YellowCopy(head);
        tail.id = id;
        tail.fromNode = node;
        tail.number = number;
        tail.width = width;
        tail.sourceOffset += cut;
        Y06EastApproachEdit.Assign(tail, YellowSample(cut, path.length, path.Position), evidence);
        Y06EastApproachEdit.Assign(head, G05NorthWestMapEdit.OriginalHead(path, cut), evidence);
        head.toNode = node;
        return tail;
    }

    private static IEnumerable<Vector3> YellowSample(float from, float to, Func<float, Vector3> position)
    {
        int count = Mathf.CeilToInt(to - from);
        for (int i = 0; i <= count; i++)
            yield return position(Mathf.Lerp(from, to, i / (float)count));
    }

    private static string[] YellowTerminals(int marker, string innerRoot, string outerRoot)
    {
        string SegmentId(string root) => "editor:segment:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(root + ":part:2"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_') + ":r42-" + marker;
        return new[] { SegmentId(innerRoot), YellowPrefix(marker) + "straight", SegmentId(outerRoot) };
    }

    private static string YellowPrefix(int marker) => $"reviewed:survey:{marker}:";
    private static RoadMapLane YellowCopy(RoadMapLane lane) => JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));
    private static AssistedRoadNetwork.Path YellowPath(RoadMapLane lane) => new AssistedRoadNetwork.Path(
        lane.points.Select((_, i) => YellowPosition(lane, i)).ToArray());
    private static Vector3 YellowPosition(RoadMapLane lane, int i) => lane.points[i].V(
        lane.heights != null && i < lane.heights.Length ? lane.heights[i] : 0);

    private static void YellowCheck(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("R42_YELLOW: " + message);
    }
}
