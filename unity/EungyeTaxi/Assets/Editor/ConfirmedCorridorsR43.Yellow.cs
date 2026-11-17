using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal static partial class ConfirmedCorridorsR43
{
    private const string YellowJunction = "J5852_16963";
    private const string Yellow10Left = "local:4974:17454:5645:17011";
    private const string Yellow10Straight = "local:4914:17388:5607:16931";
    private const string Yellow11Straight = "local:7260:14995:6050:16894";
    private const string Yellow11Right = "local:7319:15001:6080:16946";
    private const string Yellow06Median = "local:5917:14217:7083:14624";
    private const string Yellow06Curb = "local:5937:14159:7103:14567";
    private const string Yellow06Third = "reviewed:Y05:east:receiving-3";
    private const string Yellow06Straight = "reviewed:Y06:east:straight-3";

    private static void EditYellow(RoadMapDocument document)
    {
        var lanes = document.lanes.ToDictionary(lane => lane.id);
        YellowRestoreBusLaneContinuity(document, lanes);
        YellowSeparateThrough(document, lanes, Yellow10Left, Yellow10Straight,
            "UwmEqN8sjDoY-y0Kpft__A,301,-20,80", "Y10 south-eastbound: inside left-only and outside straight-only.");
        YellowSeparateThrough(document, lanes, Yellow11Right, Yellow11Straight,
            "Y-cOGW5-YAbNW8QHdKF0_w,145,-20,80", "Y11 north-westbound: inside straight-only and outside right-only.");
    }

    private static void YellowRestoreBusLaneContinuity(RoadMapDocument document, Dictionary<string, RoadMapLane> lanes)
    {
        var third = lanes[Yellow06Third];
        var curb = lanes[Yellow06Curb];
        var curbWindow = lanes[Yellow06Curb + ":part:1"];
        var curbTail = lanes[Yellow06Curb + ":part:2"];
        var median = lanes[Yellow06Median];
        var straight = lanes[Yellow06Straight];
        var right = lanes[curbTail.next.Single(id => id != straight.id)];
        YellowRequire(third.next.SequenceEqual(new[] { straight.id }) && third.toNode == curbTail.toNode &&
            curbTail.next.Contains(straight.id) && right.number == 5, "expected r42 east receiving-lane merge");

        var evidence = new RoadMapEvidence
        {
            state = "observed", month = "2026-05", checkedAt = "2026-11-17",
            appearanceConfirmed = true, ruleConfirmed = true,
            url = "https://map.naver.com/p?c=19,0,0,0,adh&p=1zkMkPr5YlH9MdvGMjx1Ow,70,-20,80,Float",
            note = "Four exit lanes at KhhluSFI-JhOiJTWhDc8Ew. The marked curb bus stop at DTzK3etFyazCcMDmR0GL7w continues as the outer right-turn lane, with three independent through lanes before the left pocket opens. Game lane alignment follows the current curb; original upstream 10–42 m lane-change window is reused."
        };
        Vector3 inward = third.points[0].V() - curb.points[0].V();
        YellowRequire(inward.magnitude >= 2.85f && inward.magnitude <= 3.2f, "expected four-channel eastward geometry");
        var thirdWindow = JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(third));
        var thirdTail = JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(third));
        thirdWindow.id = third.id + ":lane-change-window";
        thirdTail.id = third.id + ":after-lane-change";

        void ParallelPiece(RoadMapLane lane, RoadMapLane source, string exit, string next)
        {
            lane.sourceOffset = source.sourceOffset;
            lane.toNode = exit;
            lane.next = new[] { next };
            Y06EastApproachEdit.Assign(lane, source.points.Select(point => point.V() + inward), evidence);
            YellowPavement(document, lane);
        }

        ParallelPiece(third, curb, third.id + ":after-ten", thirdWindow.id);
        thirdWindow.fromNode = third.toNode;
        ParallelPiece(thirdWindow, curbWindow, third.id + ":after-forty-two", thirdTail.id);
        thirdTail.fromNode = thirdWindow.toNode;
        ParallelPiece(thirdTail, curbTail, third.id + ":end", straight.id);
        curbTail.next = new[] { right.id };
        straight.fromNode = thirdTail.toNode;

        var existing = new AssistedRoadNetwork.Path(straight.points.Select(point => point.V()).ToArray());
        int steps = Mathf.CeilToInt(existing.length);
        Y06EastApproachEdit.Assign(straight, Enumerable.Range(0, steps + 1).Select(step =>
        {
            float station = existing.length * step / steps;
            float retainedOffset = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(station / 26));
            return existing.Position(station) + inward * retainedOffset;
        }), evidence);
        YellowPavement(document, straight);

        var fromMedian = lanes["change:" + median.id + ":" + curb.id];
        var fromCurb = lanes["change:" + curb.id + ":" + median.id];
        var toMedian = JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(fromCurb));
        var toCurb = JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(fromMedian));
        toMedian.id = "change:r43:Y06:third-to-median";
        toCurb.id = "change:r43:Y06:third-to-curb";

        void AdjacentChange(RoadMapLane move, RoadMapLane from, RoadMapLane to)
        {
            move.change = to.number - from.number;
            YellowRequire(Math.Abs(move.change) == 1, "non-adjacent change " + move.id);
            Y06EastApproachEdit.Reconnect(move, from, to, evidence);
            YellowPavement(document, move);
        }

        AdjacentChange(fromMedian, median, thirdTail);
        AdjacentChange(fromCurb, curb, thirdTail);
        AdjacentChange(toMedian, third, lanes[Yellow06Median + ":part:2"]);
        AdjacentChange(toCurb, third, curbTail);
        third.next = third.next.Concat(new[] { toMedian.id, toCurb.id }).ToArray();
        document.lanes = document.lanes.Concat(new[] { thirdWindow, thirdTail, toMedian, toCurb }).ToArray();
    }

    private static void YellowPavement(RoadMapDocument document, RoadMapLane lane)
    {
        string id = "road:polish:" + lane.id;
        var surface = document.scene.objects.SingleOrDefault(item => item.id == id);
        if (surface == null)
        {
            surface = new ProjectMapObject { id = id, kind = "road", name = lane.roadName, lanes = 1, oneWay = true, suppressPaint = true };
            document.scene.objects = document.scene.objects.Append(surface).ToArray();
        }
        surface.width = 3.8f;
        surface.points = lane.points;
    }

    private static void YellowSeparateThrough(RoadMapDocument document, Dictionary<string, RoadMapLane> lanes,
        string dedicatedRoot, string throughRoot, string panorama, string observation)
    {
        var dedicated = lanes[dedicatedRoot + ":part:2"];
        var through = lanes[throughRoot + ":part:2"];
        var shifted = lanes["join:" + dedicatedRoot + ":0"];
        var target = lanes[shifted.next.Single()];
        YellowRequire(!dedicated.disabled && !through.disabled && dedicated.ruleId == YellowJunction && through.ruleId == YellowJunction &&
            dedicated.next.Contains(shifted.id) && !shifted.disabled && shifted.turn == 0, "expected r42 shared straight connector");

        var evidence = new RoadMapEvidence
        {
            state = "observed",
            month = "2026-05",
            checkedAt = "2026-11-17",
            appearanceConfirmed = true,
            ruleConfirmed = true,
            url = "https://map.naver.com/p?c=18,0,0,0,adh&p=" + panorama + ",Float",
            note = observation + " The alternate connection to the other receiving lane is a game path."
        };
        dedicated.next = dedicated.next.Where(id => id != shifted.id).ToArray();
        through.next = through.next.Append(shifted.id).ToArray();
        dedicated.evidence = through.evidence = evidence;
        Y06EastApproachEdit.Reconnect(shifted, through, target, evidence);

        var pavement = document.scene.objects.Single(o => o.id == "road:polish:" + shifted.id);
        pavement.points = shifted.points;
    }

    private static void VerifyYellow(MapData map)
    {
        var graph = new HdRoadGraph(map);
        var byId = graph.edges.ToDictionary(edge => edge.id);
        var arrows = LaneDirectionPaint.Layout(map).ToLookup(arrow => map.hdEdges[arrow.edgeIndex].id);

        void VerifyLane(string root, int mask)
        {
            var approach = byId[root + ":part:2"];
            var moves = approach.next.Select(index => graph.edges[index]).Where(edge => !edge.disabled).ToArray();
            YellowRequire(!approach.disabled && approach.ruleId == YellowJunction &&
                moves.Aggregate(0, (directions, move) => directions | UturnMovement.PaintMask(move.turn)) == mask,
                "wrong terminal movements on " + approach.id);
            YellowRequire(arrows[approach.id].Any(arrow => arrow.turns == mask), "missing terminal direction marking on " + approach.id);
        }

        VerifyLane(Yellow10Left, LaneDirectionPaint.Left);
        VerifyLane(Yellow10Straight, LaneDirectionPaint.Straight);
        VerifyLane(Yellow11Straight, LaneDirectionPaint.Straight);
        VerifyLane(Yellow11Right, LaneDirectionPaint.Right);
        foreach (var root in new[] { Yellow10Left, Yellow11Right })
            Y07MapTests.Footprint(map, byId["join:" + root + ":0"]);

        HdEdge Follow(HdEdge edge) => graph.edges[edge.next.Single()];
        var third = byId[Yellow06Third];
        var thirdTail = Follow(graph.edges[third.next.Single(index => graph.edges[index].change == 0)]);
        var curbTail = byId[Yellow06Curb + ":part:2"];
        YellowRequire(Follow(thirdTail).id == Yellow06Straight && Follow(curbTail).number == 5 &&
            Vector3.Distance(thirdTail.points.Last().V(), curbTail.points.Last().V()) > 2.85f,
            "third through lane must stay independent of the curb lane up to the right-turn split");
        YellowRequire(Follow(Follow(thirdTail)).turn == 0 && Follow(Follow(curbTail)).turn == 1,
            "different approaches must reach the through and curb-right movements");
        foreach (var id in new[] { Yellow06Third, Yellow06Third + ":lane-change-window", thirdTail.id, Yellow06Straight,
            "change:" + Yellow06Median + ":" + Yellow06Curb, "change:" + Yellow06Curb + ":" + Yellow06Median,
            "change:r43:Y06:third-to-median", "change:r43:Y06:third-to-curb" })
            Y07MapTests.Footprint(map, byId[id]);

        Debug.Log("R43_YELLOW_OK Y06 third through and curb-right independent; J5852 south-east L/S and north-west S/R");
    }

    private static void YellowRequire(bool condition, string detail)
    {
        if (!condition) throw new InvalidOperationException("R43_YELLOW: " + detail);
    }
}
