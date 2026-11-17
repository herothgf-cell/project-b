using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

internal static partial class ConfirmedLaneSurveyR42
{
    const string Green49Junction = "J8921_10835";
    const string Green49Head = "editor:segment:bG9jYWw6ODI2NTo2MzA1OjkwNTY6MTA4NTE:c1b0d0a926574cd3992859201dc10032";
    const string Green49Left = "reviewed:survey49:left";
    const string Green50Head = "local:6037:12153:4590:13330:part:2";
    const string Green50Gate = "service:reviewed:survey50:apartment-gate";
    const string Green50Turn = "join:reviewed:survey50:apartment-left";
    const string Green54Junction = "J8066_6364";
    static readonly string[] Green54Roots =
    {
        "local:7847:9404:8047:6565",
        "local:7792:9389:7990:6566",
        "local:7737:9374:7933:6566"
    };
    static readonly string[] Green54Heads =
    {
        "editor:segment:bG9jYWw6Nzg0Nzo5NDA0OjgwNDc6NjU2NTpwYXJ0OjI:33dc43f0d9854fd0bb82c3a797899e51",
        "editor:segment:bG9jYWw6Nzc5Mjo5Mzg5Ojc5OTA6NjU2NjpwYXJ0OjM:b17e613c6a5d4f10abfc131badd5b3c8",
        "editor:segment:bG9jYWw6NzczNzo5Mzc0Ojc5MzM6NjU2NjpwYXJ0OjI:0e70970b167e48898435c7a6b0e29dc7"
    };
    static readonly string[] Green54Labels = { "left", "straight-1", "straight-2", "straight-3", "right" };

    static void GreenCheck(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException("R42_GREEN: " + message);
    }

    static Vector3 GreenPoint(float x, float z) => new Vector3(x, 0, z);
    static AssistedRoadNetwork.Path GreenPath(RoadMapLane lane) =>
        new AssistedRoadNetwork.Path(lane.points.Select(p => p.V()).ToArray());
    static RoadMapLane GreenClone(RoadMapLane lane) =>
        JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));

    static string GreenSegment(string origin, string label) =>
        "editor:segment:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(origin))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_') + ":" + label;

    static string Green54LaneId(int index) => index == 0
        ? GreenSegment(Green54Roots[0] + ":part:2", "survey54-left")
        : "reviewed:survey54:" + Green54Labels[index];

    static RoadMapEvidence GreenEvidence(string camera, string note) => new RoadMapEvidence
    {
        state = "observed", month = "2026-05", checkedAt = "2026-11-16",
        appearanceConfirmed = true, ruleConfirmed = true,
        url = "https://map.naver.com/p?c=19,0,0,0,adh&p=" + camera + ",Float", note = note
    };

    static RoadMapLane GreenTail(RoadMapLane head, string id, float cut, RoadMapEvidence evidence)
    {
        var path = GreenPath(head);
        GreenCheck(cut >= .5f && cut < path.length - .5f, "split outside " + head.id);
        var tail = GreenClone(head);
        tail.id = id;
        tail.sourceOffset += cut;
        tail.fromNode = id + ":start";
        Y06EastApproachEdit.Assign(tail, Y07MapEdit.Slice(path, cut, path.length).Select(p => p.V()), evidence);
        Y06EastApproachEdit.Assign(head, G05NorthWestMapEdit.OriginalHead(path, cut), evidence);
        head.toNode = tail.fromNode;
        head.next = new[] { tail.id };
        return tail;
    }

    static void GreenOffset(RoadMapLane lane, AssistedRoadNetwork.Path path, float offset, float taper, RoadMapEvidence evidence)
    {
        int steps = Mathf.CeilToInt(path.length);
        Y06EastApproachEdit.Assign(lane, Enumerable.Range(0, steps + 1).Select(i =>
        {
            float at = path.length * i / steps;
            float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01(at / taper));
            return path.Position(at) + TrafficSystem.Right(path.Tangent(at)) * (offset * blend);
        }), evidence);
    }

    static void GreenPavement(RoadMapDocument doc, RoadMapLane lane, float width)
    {
        string id = "road:polish:" + lane.id;
        var existing = doc.scene.objects.SingleOrDefault(o => o.id == id);
        if (existing != null)
        {
            existing.points = lane.points;
            return;
        }
        doc.scene.objects = doc.scene.objects.Append(new ProjectMapObject
        {
            id = id, kind = "road", name = lane.roadName, width = width,
            lanes = 1, oneWay = true, suppressPaint = true, points = lane.points
        }).ToArray();
    }

    static void GreenReconnect(RoadMapDocument doc, RoadMapLane join, RoadMapLane from,
        Dictionary<string, RoadMapLane> lanes, RoadMapEvidence evidence)
    {
        if (Vector3.Distance(join.points[0].V(), from.points.Last().V()) < .005f)
        {
            join.fromNode = from.toNode;
            return;
        }
        Y06EastApproachEdit.Reconnect(join, from, lanes[join.next.Single()], evidence);
        GreenPavement(doc, join, 4.2f);
    }

    static void GreenPatch49(RoadMapDocument doc, Dictionary<string, RoadMapLane> lanes)
    {
        var evidence = GreenEvidence("FZRydbv8G5LI-0nmgdm0Zw,277,-23,56",
            "49 westbound: left-only inside and through-only outside, confirmed by the user and 2022 paint. The short expansion fits the existing curb; dimensions are approximate.");
        var head = lanes[Green49Head];
        const float length = 38;
        string straightId = GreenSegment(RoadMapAuthoringOperations.Origin(head), "survey49-straight");
        var outer = GreenTail(head, straightId, GreenPath(head).length - length, evidence);
        var original = GreenPath(outer);
        var inner = GreenClone(outer);
        inner.id = Green49Left;
        inner.toNode = inner.id + ":end";
        inner.sourceOffset = 0;
        inner.number = 1;
        outer.number = 2;
        inner.width = outer.width = 3.2f;
        // The existing outside alignment clears the north curb. Add the pocket on the median side.
        GreenOffset(inner, original, -3.2f, 22, evidence);
        inner.next = outer.next.Where(id => lanes[id].turn == -1).ToArray();
        outer.next = outer.next.Where(id => lanes[id].turn == 0).ToArray();
        GreenCheck(inner.next.Length == 1 && outer.next.Length == 2, "49 existing south turn and both receiving lanes");
        head.next = new[] { inner.id, outer.id };
        foreach (var lane in new[] { inner, outer })
            foreach (string id in lane.next) GreenReconnect(doc, lanes[id], lane, lanes, evidence);
        doc.lanes = doc.lanes.Concat(new[] { inner, outer }).ToArray();
    }

    static void GreenPatch50(RoadMapDocument doc, Dictionary<string, RoadMapLane> lanes)
    {
        var evidence = GreenEvidence("ZqtLsB_lP7FfZkZLDOtWmA,212,0,75",
            "50 northwest approach: the user confirms left access to the apartment gate. The divider opening and curb are visible; the driveway geometry is approximate and only reaches the gate.");
        var head = lanes[Green50Head];
        var original = GreenPath(head);
        float cut = original.Project(GreenPoint(517, 1283.5f), out _);
        var through = GreenTail(head, GreenSegment(head.id, "survey50-through"), cut, evidence);
        var gate = new RoadMapLane
        {
            id = Green50Gate, sourceId = Green50Gate, fromNode = Green50Gate + ":mouth",
            toNode = Green50Gate + ":end", ruleId = "", roadName = "시흥은행우남아파트 정문",
            status = "provisional", number = 1, width = 3.5f
        };
        Y06EastApproachEdit.Assign(gate, new[] { GreenPoint(501.0f, 1272.5f), GreenPoint(497.3f, 1266.6f), GreenPoint(492.9f, 1259.6f) }, evidence);
        var left = GreenClone(gate);
        left.id = Green50Turn;
        left.turn = -1;
        Y06EastApproachEdit.Reconnect(left, head, gate, evidence);
        head.next = head.next.Append(left.id).ToArray();
        doc.lanes = doc.lanes.Concat(new[] { through, left, gate }).ToArray();
        GreenPavement(doc, left, 4.6f);
        GreenPavement(doc, gate, 4.6f);

        // Clear only the generated curb panels that occupy the actual mouth.
        var gatePath = GreenPath(gate);
        bool BlocksGate(MapPoint point)
        {
            gatePath.Project(point.V(), out float error);
            return error < 3.25f;
        }
        foreach (var panel in doc.scene.objects.Where(o => o.enabled && o.id.StartsWith("sidewalk:generated:") &&
            o.points.Any(p => p.x > 485 && p.x < 510 && p.z > 1255 && p.z < 1281)))
        {
            if (panel.points.Any(BlocksGate)) panel.enabled = false;
        }

        float opening = original.Project(GreenPoint(509f, 1286f), out _);
        doc.facilities = doc.facilities.Append(new RoadMapFacility
        {
            id = Green50Gate + ":median-opening", kind = "lanePresentation",
            type = ReviewedUnmarkedRoadPaint.FeatureType, laneId = head.id,
            width = 12, x = 509, z = 1286, startAt = head.sourceOffset + opening - 6,
            endAt = head.sourceOffset + opening + 6, evidence = evidence
        }).ToArray();
    }

    static void GreenPatch54(RoadMapDocument doc, Dictionary<string, RoadMapLane> lanes)
    {
        var evidence = GreenEvidence("ySRbCVrCcqISCQRndYcTyg,180,-9,50",
            "54 southbound: the user confirmed left only, three straight lanes, then right only. No southbound U-turn is added. The existing receiving lanes and opposite signal-controlled U-turn are kept.");
        var heads = Green54Heads.Select(id => lanes[id]).ToArray();
        var paths = heads.Select(GreenPath).ToArray();
        int[] source = { 0, 0, 1, 2, 2 };
        float[] lengths = { 39, 39, 23 };
        var tails = new RoadMapLane[5];
        var originals = new AssistedRoadNetwork.Path[3];
        for (int i = 0; i < heads.Length; i++)
        {
            int laneNumber = i + 2;
            var tail = GreenTail(heads[i], Green54LaneId(laneNumber - 1),
                paths[i].length - lengths[i], evidence);
            originals[i] = GreenPath(tail);
            tails[laneNumber - 1] = tail;
        }
        tails[0] = GreenClone(tails[1]);
        tails[4] = GreenClone(tails[3]);
        for (int i = 0; i < tails.Length; i++)
        {
            int origin = source[i];
            var tail = tails[i];
            tail.id = Green54LaneId(i);
            tail.fromNode = heads[origin].toNode;
            tail.toNode = tail.id + ":end";
            tail.number = i + 1;
            tail.width = 2.85f;
            float expected = 2.85f * i;
            var right = TrafficSystem.Right(paths[0].Tangent(paths[0].length));
            float current = Vector3.Dot(paths[origin].points.Last() - paths[0].points.Last(), right);
            // Reuse the extreme left and right channels and distribute three through lanes between them.
            if (i > 0 && i < tails.Length - 1)
                GreenOffset(tail, originals[origin], expected - current, origin == 2 ? 12 : 23, evidence);
        }
        heads[0].next = new[] { tails[0].id, tails[1].id };
        heads[1].next = new[] { tails[2].id };
        heads[2].next = new[] { tails[3].id, tails[4].id };
        string[] connectors =
        {
            "join:" + Green54Roots[0] + ":-1", "join:" + Green54Roots[0] + ":0",
            "join:" + Green54Roots[1] + ":0", "join:" + Green54Roots[2] + ":0",
            "join:" + Green54Roots[2] + ":1"
        };
        for (int i = 0; i < tails.Length; i++)
        {
            tails[i].next = new[] { connectors[i] };
            GreenReconnect(doc, lanes[connectors[i]], tails[i], lanes, evidence);
        }
        doc.lanes = doc.lanes.Concat(tails).ToArray();
    }

    private static void EditGreen(RoadMapDocument document)
    {
        var lanes = document.lanes.ToDictionary(lane => lane.id);
        GreenPatch49(document, lanes);
        GreenPatch50(document, lanes);
        GreenPatch54(document, lanes);
    }

    private static void VerifyGreen(RoadMapDocument document)
    {
        var lanes = document.lanes.ToDictionary(l => l.id);
        var lastWestbound = lanes.Values.Single(lane => !lane.disabled && lane.next.Contains(Green49Left));
        var lane49 = lastWestbound.next.Select(id => lanes[id]).OrderBy(l => l.number).ToArray();
        GreenCheck(lane49.Length == 2 && lane49.All(l => l.ruleId == Green49Junction), "49 missing two westbound lanes");
        GreenCheck(lane49[0].next.Select(id => lanes[id].turn).SequenceEqual(new[] { -1 }) &&
            lane49[1].next.All(id => lanes[id].turn == 0), "49 wrong westbound movements");
        GreenCheck(lanes[Green50Head].next.Contains(Green50Turn) && lanes[Green50Turn].turn == -1 &&
            lanes[Green50Turn].next.SequenceEqual(new[] { Green50Gate }), "50 left does not lead to the private gate");
        GreenCheck(lanes[Green50Head].next.Any(id => lanes[id].turn == 0 && lanes[id].ruleId == lanes[Green50Head].ruleId),
            "50 changed through continuity or downstream control");
        int[] turns = { -1, 0, 0, 0, 1 };
        var lane54 = Enumerable.Range(0, Green54Labels.Length).Select(i => lanes[Green54LaneId(i)]).ToArray();
        for (int i = 0; i < lane54.Length; i++)
            GreenCheck(lane54[i].ruleId == Green54Junction && lane54[i].number == i + 1 &&
                lanes[lane54[i].next.Single()].turn == turns[i], "54 wrong southbound movement " + lane54[i].id);
        GreenCheck(lane54.Skip(1).Take(3).Select(l => lanes[l.next.Single()].next.Single()).Distinct().Count() == 3,
            "54 through lanes do not have distinct exits");
        GreenCheck(!lanes["join:reviewed:J8066-S:uturn"].disabled &&
            document.facilities.Any(f => f.junctionId == Green54Junction && UturnMovement.IsCondition(f.type)),
            "54 changed the opposite northbound U-turn");
    }

    private static void VerifyGreenRuntime(MapData map)
    {
        var graph = new HdRoadGraph(map);
        var byId = graph.edges.Select((lane, index) => (lane, index)).ToDictionary(v => v.lane.id, v => v.index);
        var arrows = LaneDirectionPaint.Layout(map).ToLookup(a => a.edgeIndex);
        int left49 = byId[Green49Left];
        var lanes49 = graph.edges.Single(lane => !lane.disabled && lane.next.Contains(left49)).next
            .Select(i => graph.edges[i]).OrderBy(l => l.number).ToArray();
        var lanes54 = Enumerable.Range(0, Green54Labels.Length).Select(i => graph.edges[byId[Green54LaneId(i)]]).ToArray();
        int[] expected = { LaneDirectionPaint.Left, LaneDirectionPaint.Straight, LaneDirectionPaint.Left,
            LaneDirectionPaint.Straight, LaneDirectionPaint.Straight, LaneDirectionPaint.Straight, LaneDirectionPaint.Right };
        var approaches = lanes49.Concat(lanes54).ToArray();
        for (int i = 0; i < approaches.Length; i++)
        {
            var lane = approaches[i];
            int movements = lane.next.Aggregate(0, (mask, n) =>
            {
                var connector = graph.edges[n];
                GreenCheck(!connector.disabled && connector.change == 0 &&
                    connector.next.Any(target => !graph.edges[target].disabled),
                    "inactive movement from " + lane.id);
                return mask | UturnMovement.PaintMask(connector.turn);
            });
            GreenCheck(movements == expected[i], "incorrect runtime movements " + lane.id);
            GreenCheck(arrows[byId[lane.id]].Any(a => a.turns == expected[i]), "missing direction arrow " + lane.id);
            Y07MapTests.Footprint(map, lane);
            foreach (int n in lane.next) Y07MapTests.Footprint(map, graph.edges[n]);
        }
        var gate = graph.edges[byId[Green50Gate]];
        var turn = graph.edges[byId[Green50Turn]];
        Y07MapTests.Footprint(map, gate);
        Y07MapTests.Footprint(map, turn);
        int start = byId[Green50Head];
        GreenCheck(graph.Route(start, 4, byId[Green50Gate], 5) != null, "50 cannot route into the confirmed apartment entrance");
        var crossing = new CentreLineCrossing();
        RoadPaintGeometry.Paint(map, new List<TrafficJunction>(), (name, centre, forward, width, length, yellow) =>
        {
            if (name == "Centre line" && yellow && (centre - GreenPoint(509, 1286)).sqrMagnitude < 45 * 45)
                crossing.Add(centre - forward * (length / 2), centre + forward * (length / 2));
        });
        for (int i = 1; i < turn.points.Length; i++)
            GreenCheck(!crossing.Crossed(turn.points[i - 1].V(), turn.points[i].V()), "50 turn crosses a painted solid centre line");
        Debug.Log("R42_GREEN_OK 49 L/S, 50 gate entry, 54 L/S/S/S/R and opposite U-turn");
    }
}
