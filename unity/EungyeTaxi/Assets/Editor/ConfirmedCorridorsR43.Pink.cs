using System;
using System.Linq;
using System.Text;
using UnityEngine;

internal static partial class ConfirmedCorridorsR43
{
    private const string PinkBank = "J1927_5181";
    private const string PinkBankHead = "local:1957:2414:1950:4938";
    private const string PinkBankLeft = "join:reviewed:P02:NB:left";
    private const string PinkBankStraight = "join:reviewed:P02:NB:straight";
    private const string PinkBankRight = "join:local:1957:2414:1950:4938:1";
    private const string PinkBankOutside = "reviewed:P02:NB:right-approach";
    private const string PinkBankWestExit = "A2218G611780:0";
    private const string PinkBankNorthExit = "portal:A222BC004230:0:out";
    private const string PinkBankEastExit = "local:2123:4953:4780:4331";
    private const float PinkBankTaperStart = 430;
    private const float PinkBankTaperEnd = 443;
    private static readonly string PinkBankInside = "editor:segment:" +
        Convert.ToBase64String(Encoding.UTF8.GetBytes(PinkBankHead)).TrimEnd('=') + ":p02-nb-through-left";
    private const string PinkGateWestbound = "local:7660:918:5205:927";
    private const string PinkGateInside = "reviewed:P15:WB:left-approach";
    private const string PinkGateLeft = "join:reviewed:P15:WB:hanyang";
    private const string PinkGateRight = "join:reviewed:P15:WB:hoban";
    private const string PinkGateThrough = "join:reviewed:P15:WB:through";
    private const string Pink55East = "local:2050:827:4943:827";
    private const string Pink55Added = "reviewed:P03:55:east-inner";
    private const string Pink56Junction = "reviewed:P22:56";
    private const string Pink56South = "local:5033:2461:5031:1007";
    private const string Pink56North = "editor:segment:bG9jYWw6NTExOToxMDA3OjUxMjE6MjQ2Mg:pink-roundabout";
    private const string PinkGateHanyang = "service:reviewed:r42:P51:hanyang-gate";
    private const string PinkGateHoban = "service:reviewed:r42:P51:hoban-gate";
    private static readonly string PinkGateOuter = PinkGateSegmentId("approach");
    private static readonly string PinkGateDeparture = PinkGateSegmentId("departure");

    private static void EditPink(RoadMapDocument document)
    {
        PatchPinkBank(document);
        PatchPinkGateWestbound(document);
        PatchPink55Eastbound(document);
        PatchPink56Midblock(document);
    }

    private static void PatchPink56Midblock(RoadMapDocument document)
    {
        var evidence = new RoadMapEvidence
        {
            state = "observed", month = "2026-05", checkedAt = "2026-11-17", appearanceConfirmed = true, ruleConfirmed = true,
            url = "https://map.naver.com/p?c=19,0,0,0,adh&p=U0NZrMdOqq88OdknXFp0bg,180,-20,85,Float",
            note = "56: a midblock signal and the southbound left pocket from the bus stop, confirmed by the user and the street view. " +
                "Signal timing, curb and gate-mouth positions are game estimates."
        };
        RoadMapLane Lane(string id) => document.lanes.Single(lane => lane.id == id);
        var south = Lane(Pink56South);
        var north = Lane(Pink56North);
        var added = new System.Collections.Generic.List<RoadMapLane>();

        RoadMapLane[] Approach(RoadMapLane origin, int sign, float startZ, float stopZ, float exitZ)
        {
            var path = new AssistedRoadNetwork.Path(origin.points.Select(point => point.V()).ToArray());
            float At(float z) => path.Project(new Vector3(path.points[0].x, 0, z), out _);
            float start = At(startZ), stop = At(stopZ), exit = At(exitZ);
            string label = Pink56Junction + (sign < 0 ? ":south" : ":north");
            string lineage = "editor:segment:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(RoadMapAuthoringOperations.Origin(origin)))
                .TrimEnd('=') + ":r43-56-" + (sign < 0 ? "south" : "north");

            var terminal = PinkGatePortion(origin, lineage + "-approach", start,
                Y07MapEdit.Slice(path, start, stop).Select(point => point.V()), evidence);
            var departure = PinkGatePortion(origin, lineage + "-departure", exit,
                Y07MapEdit.Slice(path, exit, path.length).Select(point => point.V()), evidence);
            terminal.fromNode = label + ":split";
            terminal.toNode = label + ":stop";
            terminal.ruleId = Pink56Junction;
            terminal.number = sign < 0 ? 2 : 1;
            terminal.width = 3.2f;
            departure.fromNode = label + ":departure";
            var through = PinkR43Join(terminal, departure, "join:" + label + ":through", 0, evidence);
            terminal.next = new[] { through.id };
            Y06EastApproachEdit.Assign(origin, G05NorthWestMapEdit.OriginalHead(path, start), evidence);
            origin.toNode = terminal.fromNode;
            origin.next = new[] { terminal.id };
            foreach (var facility in document.facilities.Where(f => f.laneId == origin.id && f.junctionId == departure.ruleId))
                facility.laneId = departure.id;
            added.AddRange(new[] { terminal, through, departure });
            if (sign > 0) return new[] { terminal };

            var left = PinkGatePortion(terminal, label + ":left", -terminal.sourceOffset,
                Enumerable.Range(0, 33).Select(step =>
                {
                    float at = Mathf.Lerp(start, stop, step / 32f);
                    return path.Position(at) - TrafficSystem.Right(path.Tangent(at)) * (3f * Mathf.SmoothStep(0, 1,
                        Mathf.Clamp01((at - start) / 17f)));
                }), evidence);
            left.toNode = label + ":left-stop";
            left.number = 1;
            origin.next = origin.next.Append(left.id).ToArray();
            var gate = new RoadMapLane { id = label + ":gate", sourceId = label + ":gate", roadName = "은계중앙로 상가 진입로",
                fromNode = label + ":gate-mouth", toNode = label + ":gate-end", width = 3.2f, number = 1, status = "provisional" };
            Y06EastApproachEdit.Assign(gate, new[] { new Vector3(516.5f, 0, 172.1f), new Vector3(519.3f, 0, 172.1f) }, evidence);
            var turn = PinkR43Join(left, gate, "join:" + label + ":left", -1, evidence);
            left.next = new[] { turn.id };
            added.AddRange(new[] { left, turn, gate });
            PinkBankPavement(document, turn, 4.2f);
            PinkBankPavement(document, gate, 4.2f);
            var gatePath = new AssistedRoadNetwork.Path(turn.points.Concat(gate.points.Skip(1)).Select(p => p.V()).ToArray());
            foreach (var item in document.scene.objects.Where(o => o.enabled && o.id.StartsWith("sidewalk:generated:") &&
                o.points.Any(p => p.x > 512 && p.x < 522 && p.z > 168 && p.z < 177)))
            {
                if (item.points.Any(p => { gatePath.Project(p.V(), out float distance); return distance < 2.5f; })) item.enabled = false;
            }
            return new[] { left, terminal };
        }

        var southLanes = Approach(south, -1, 211, 184, 151);
        var northLanes = Approach(north, 1, 130, 153, 189);
        document.lanes = document.lanes.Concat(added).ToArray();
        var facilities = new System.Collections.Generic.List<RoadMapFacility>
        {
            new RoadMapFacility { id = Pink56Junction + ":control", kind = ReviewedMidblockControl.Kind, type = ReviewedMidblockControl.Type,
                junctionId = Pink56Junction, x = 507.8f, z = 172, width = 12.6f, length = 8, evidence = evidence }
        };
        var groups = new System.Collections.Generic.List<RoadMapSignalGroup>();
        foreach (int sign in new[] { -1, 1 })
        {
            var lanes = sign < 0 ? southLanes : northLanes;
            var end = lanes.Last().points.Last().V();
            string label = Pink56Junction + (sign < 0 ? ":south" : ":north");
            RoadMapFacility Mark(string kind, float x, float z, float width, float length) => new RoadMapFacility
            {
                id = label + ":" + kind, kind = kind, junctionId = Pink56Junction, x = x, z = z, sign = sign,
                yaw = sign < 0 ? 180 : 0, width = width, length = length, evidence = evidence
            };
            var stop = Mark("stopLine", sign < 0 ? end.x + 1.5f : end.x, end.z, sign < 0 ? 6.5f : 3.3f, .4f);
            stop.laneId = lanes.Last().id;
            var crossing = Mark("crosswalk", 507.8f, sign < 0 ? 179 : 158, 12.6f, 3.2f);
            var lamp = Mark("vehicleSignal", sign < 0 ? 500.4f : 515.2f, sign < 0 ? 166 : 180, 2.8f, .3f);
            lamp.height = 5.2f; lamp.targetId = stop.id; lamp.laneId = stop.laneId; lamp.groupId = label + ":straight";
            facilities.AddRange(new[] { stop, crossing, lamp });
            groups.Add(new RoadMapSignalGroup { id = label + ":straight", name = "직진", sign = sign });
            groups.Add(new RoadMapSignalGroup { id = label + ":walk", name = "보행", sign = sign, pedestrian = true, crosswalkId = crossing.id });
        }
        var protectedLeft = new RoadMapSignalGroup { id = Pink56Junction + ":left", name = "남행 좌회전", sign = -1, turn = -1 };
        groups.Add(protectedLeft);
        document.facilities = document.facilities.Concat(facilities).ToArray();
        document.signals = document.signals.Append(new RoadMapSignalProgram { junctionId = Pink56Junction, groups = groups.ToArray(),
            stages = new[] {
                new RoadMapStage { name = "양방향 직진", green = 22, groups = groups.Where(g => !g.pedestrian && g.turn == 0).Select(g => g.id).ToArray() },
                new RoadMapStage { name = "남행 좌회전", green = 9, groups = new[] { protectedLeft.id } },
                new RoadMapStage { name = "보행", green = 20, yellow = 0, groups = groups.Where(g => g.pedestrian).Select(g => g.id).ToArray() }
            }
        }).ToArray();
    }

    private static void PatchPink55Eastbound(RoadMapDocument document)
    {
        var head = document.lanes.Single(lane => lane.id == Pink55East);
        var path = new AssistedRoadNetwork.Path(head.points.Select(point => point.V()).ToArray());
        float start = path.Project(new Vector3(244, 0, 83), out _);
        float end = path.Project(new Vector3(380, 0, 83), out _);
        const float taper = 14;
        string lineage = "editor:segment:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(Pink55East)).TrimEnd('=') + ":r43-55-";
        var evidence = new RoadMapEvidence
        {
            state = "observed", month = "2026-05", checkedAt = "2026-11-17",
            appearanceConfirmed = true, ruleConfirmed = false,
            url = "https://map.naver.com/p?c=19,0,0,0,adh&p=m4ZRcviRgVlyQDMGXM2Q9Q,90,-19,85,Float",
            note = "55: eastbound opens to two after the school crossing visible between the x230 and x249 cameras. " +
                "The verified double lane is included from x258 to x366, then tapered back into the existing approach. " +
                "Gate movements beyond the observed cross-section are not inferred."
        };
        var outer = PinkGatePortion(head, lineage + "outer", start,
            Y07MapEdit.Slice(path, start, end).Select(point => point.V()), evidence);
        var departure = PinkGatePortion(head, lineage + "departure", end,
            Y07MapEdit.Slice(path, end, path.length).Select(point => point.V()), evidence);
        outer.fromNode = Pink55Added + ":split";
        outer.toNode = Pink55Added + ":merge";
        outer.number = 2;
        outer.width = 3.2f;
        outer.next = new[] { departure.id };
        departure.fromNode = outer.toNode;
        var inner = PinkGatePortion(outer, Pink55Added, -outer.sourceOffset,
            Enumerable.Range(0, Mathf.CeilToInt(end - start) + 1).Select(step =>
            {
                float station = Mathf.Lerp(start, end, step / (float)Mathf.CeilToInt(end - start));
                float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Mathf.Min(station - start, end - station) / taper));
                return path.Position(station) - TrafficSystem.Right(path.Tangent(station)) * (3.2f * blend);
            }), evidence);
        inner.number = 1;
        Y06EastApproachEdit.Assign(head, G05NorthWestMapEdit.OriginalHead(path, start), evidence);
        head.toNode = outer.fromNode;
        head.next = new[] { outer.id, inner.id };
        foreach (var facility in document.facilities.Where(item => item.laneId == head.id && item.kind == "stopLine"))
            facility.laneId = departure.id;
        document.lanes = document.lanes.Concat(new[] { outer, inner, departure }).ToArray();
    }

    private static void PatchPinkBank(RoadMapDocument document)
    {
        var lanes = document.lanes.ToDictionary(lane => lane.id);
        var head = lanes[PinkBankHead];
        var path = new AssistedRoadNetwork.Path(head.points.Select(point => point.V()).ToArray());
        var originalEnd = path.Position(path.length);
        var cut = path.Project(new Vector3(originalEnd.x, 0, PinkBankTaperStart), out _);
        var evidence = new RoadMapEvidence
        {
            state = "observed", month = "2026-05", checkedAt = "2026-11-17",
            appearanceConfirmed = true, ruleConfirmed = true,
            url = "https://map.naver.com/p?c=19,0,0,0,adh&p=lr59CsRJck5r9tnIqSak7g,180,-19,85,Float",
            note = "Bank intersection northbound: one lane expands at Hun Jang Gol (Kxis5QAv6CMiAoziz9vCmA; S-mjtoK65hlGuQAHK-hAWQ) to inside straight/left and outside right. Through traffic reaches the north public road. Stations, lane centres and stop-line width are game estimates."
        };
        var inside = PinkBankBranch(head, path, cut, PinkBankInside, 1, -1.95f, evidence);
        var outside = PinkBankBranch(head, path, cut, PinkBankOutside, 2, 0.8f, evidence);

        // Reuse the original public landings from the disconnected imported
        // approaches. The old short orphan approach strips no longer duplicate traffic paths.
        var orphanApproaches = new[] { "A222BC004227:0", "A222BC004228:0" };
        var orphanIds = orphanApproaches.Concat(orphanApproaches.SelectMany(id => lanes[id].next)).ToArray();
        PinkR43Check(!document.lanes.Any(lane => !lane.disabled && !orphanIds.Contains(lane.id) && lane.next.Any(orphanIds.Contains)),
            "P02 orphan roads now have an incoming path");
        PinkR43Check(!document.facilities.Any(facility => orphanIds.Contains(facility.laneId)) &&
            !document.scene.objects.Any(item => item.IsService && orphanIds.Contains(item.laneId)),
            "P02 orphan roads have a facility or stop anchor");
        foreach (var id in orphanIds)
        {
            lanes[id].disabled = true;
            lanes[id].next = Array.Empty<string>();
        }

        var left = PinkR43Join(inside, lanes[PinkBankWestExit], PinkBankLeft, -1, evidence);
        var straight = PinkR43Join(inside, lanes[PinkBankNorthExit], PinkBankStraight, 0, evidence);
        var right = lanes[PinkBankRight];
        Y06EastApproachEdit.Reconnect(right, outside, lanes[PinkBankEastExit], evidence);
        inside.next = new[] { left.id, straight.id };
        outside.next = new[] { right.id };
        Y06EastApproachEdit.Assign(head, G05NorthWestMapEdit.OriginalHead(path, cut), evidence);
        head.toNode = inside.fromNode;
        head.next = new[] { inside.id, outside.id };
        document.lanes = document.lanes.Concat(new[] { inside, outside, left, straight }).ToArray();

        foreach (var lane in new[] { outside, left, straight, right })
            PinkBankPavement(document, lane, lane == outside ? 2.9f : 4.2f);

        var stop = document.facilities.Single(facility => facility.kind == "stopLine" &&
            facility.junctionId == PinkBank && facility.axis == 0 && facility.sign == -1);
        var insideStop = PinkBankAtZ(inside, stop.z);
        var outsideStop = PinkBankAtZ(outside, stop.z);
        var centre = (insideStop + outsideStop) * .5f;
        stop.x = centre.x;
        stop.width = Vector3.Distance(insideStop, outsideStop) + inside.width;
        stop.evidence = evidence;
    }

    private static RoadMapLane PinkBankBranch(RoadMapLane head, AssistedRoadNetwork.Path path, float cut,
        string id, int number, float offset, RoadMapEvidence evidence)
    {
        var lane = JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(head));
        lane.id = id;
        lane.number = number;
        lane.width = 2.7f;
        lane.sourceOffset = number == 1 ? head.sourceOffset + cut : 0;
        lane.fromNode = PinkBankHead + ":r43-split";
        lane.toNode = id + ":end";
        float length = path.length - cut;
        int steps = Mathf.CeilToInt(length);
        Y06EastApproachEdit.Assign(lane, Enumerable.Range(0, steps + 1).Select(step =>
        {
            float station = cut + length * step / steps;
            var point = path.Position(station);
            float widening = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(PinkBankTaperStart, PinkBankTaperEnd, point.z));
            // The 2020 native asphalt strips near the signal bend east as
            // the opposite straight enters. Fit both approaches across the same apron.
            float apron = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(477.8f, 491.5f, point.z));
            return point + TrafficSystem.Right(path.Tangent(station)) * (offset * widening + 2.9f * apron);
        }), evidence);
        return lane;
    }

    private static RoadMapLane PinkR43Join(RoadMapLane from, RoadMapLane to, string id, int turn, RoadMapEvidence evidence)
    {
        var lane = new RoadMapLane
        {
            id = id, sourceId = from.sourceId, roadName = from.roadName,
            width = from.width, status = "provisional", ruleId = from.ruleId, turn = turn
        };
        Y06EastApproachEdit.Reconnect(lane, from, to, evidence);
        return lane;
    }

    private static string PinkGateSegmentId(string role)
    {
        return "editor:segment:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(PinkGateWestbound)).TrimEnd('=') +
            ":p15-wb-" + role;
    }

    private static void PatchPinkGateWestbound(RoadMapDocument document)
    {
        var lanes = document.lanes.ToDictionary(lane => lane.id);
        var head = lanes[PinkGateWestbound];
        var path = new AssistedRoadNetwork.Path(head.points.Select(point => point.V()).ToArray());
        float cut = path.Project(new Vector3(737, 0, 92), out _);
        float splitCrossing = path.Project(new Vector3(687.5f, 0, 92), out _);
        float resume = path.Project(new Vector3(675.6f, 0, 92), out _);
        var evidence = new RoadMapEvidence
        {
            state = "observed", month = "2026-05", checkedAt = "2026-11-17",
            appearanceConfirmed = true, ruleConfirmed = true,
            url = "https://map.naver.com/p?c=19,0,0,0,adh&p=UfWX9J1ywndOBx-dLbIgqQ,270,-19,85,Float",
            note = "Latest and October 2024 corroborate westbound inside left to Hanyang and outside straight/right to Hoban. Lane widening is present at x721.8 and not yet separated at x741.3. Taper, zebra, gate geometry and station lengths are game estimates."
        };

        var outside = PinkGatePortion(head, PinkGateOuter, cut, Y07MapEdit.Slice(path, cut, splitCrossing)
            .Select(point => point.V()), evidence);
        outside.fromNode = PinkGateWestbound + ":r43-split";
        outside.toNode = outside.id + ":end";
        outside.ruleId = "";
        outside.width = 2.8f;
        outside.number = 2;
        int steps = Mathf.CeilToInt(splitCrossing - cut);
        var inside = PinkGatePortion(outside, PinkGateInside, -outside.sourceOffset, Enumerable.Range(0, steps + 1).Select(i =>
        {
            float at = Mathf.Lerp(cut, splitCrossing, (float)i / steps);
            return path.Position(at) - TrafficSystem.Right(path.Tangent(at)) *
                (3.05f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(cut, cut + 15, at)));
        }), evidence);
        inside.number = 1;
        inside.toNode = inside.id + ":end";
        var departure = PinkGatePortion(head, PinkGateDeparture, resume, Y07MapEdit.Slice(path, resume, path.length)
            .Select(point => point.V()), evidence);
        departure.fromNode = departure.id + ":start";

        var left = PinkGateTurn(inside, lanes[PinkGateHanyang], PinkGateLeft, -1, evidence);
        var right = PinkGateTurn(outside, lanes[PinkGateHoban], PinkGateRight, 1, evidence);
        var through = PinkR43Join(outside, departure, PinkGateThrough, 0, evidence);
        inside.next = new[] { left.id };
        outside.next = new[] { through.id, right.id };
        Y06EastApproachEdit.Assign(head, G05NorthWestMapEdit.OriginalHead(path, cut), evidence);
        head.next = new[] { outside.id, inside.id };
        head.toNode = outside.fromNode;
        head.ruleId = "";
        document.lanes = document.lanes.Concat(new[] { inside, outside, left, right, through, departure }).ToArray();
        foreach (var lane in new[] { left, right, through })
            PinkBankPavement(document, lane, 4.6f);

        // Duplicate placeholder strips ran across the east half of the existing
        // Hoban entrance. Keep the other areas of the sidewalk untouched.
        foreach (string id in new[] { "sidewalk:generated:8175", "sidewalk:generated:14099" })
            document.scene.objects.Single(item => item.id == id).enabled = false;
        foreach (var facility in document.facilities.Where(item => item.laneId == head.id && item.junctionId == departure.ruleId))
            facility.laneId = departure.id;

        // Connectors cover x687.5–675.6; the original paint suppression starts
        // beyond that interval on the original through lineage at x674.
        PinkR43Check(document.facilities.Single(item => item.id == "reviewed:r42:P51:yellow-gap").startAt >
            departure.sourceOffset, "P15 existing junction paint gap does not start on the departure lane");
        var centre = (inside.points.Last().V() + outside.points.Last().V()) * .5f;
        document.facilities = document.facilities.Concat(new[]
        {
            new RoadMapFacility { id = "reviewed:P15:WB:stop", kind = "stopLine", x = centre.x + 1, z = centre.z,
                yaw = 270, width = 5.85f, length = .38f, laneId = outside.id, evidence = evidence },
            new RoadMapFacility { id = "reviewed:P15:WB:crosswalk", kind = "crosswalk", x = 683.5f, z = 87.2f,
                yaw = 270, width = 13.6f, length = 3f, evidence = evidence }
        }).ToArray();
    }

    private static RoadMapLane PinkGatePortion(RoadMapLane source, string id, float extraOffset,
        System.Collections.Generic.IEnumerable<Vector3> points, RoadMapEvidence evidence)
    {
        var result = JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(source));
        result.id = id;
        result.sourceOffset += extraOffset;
        Y06EastApproachEdit.Assign(result, points, evidence);
        return result;
    }

    private static RoadMapLane PinkGateTurn(RoadMapLane from, RoadMapLane gate, string id, int turn, RoadMapEvidence evidence)
    {
        var pivot = JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(from));
        var destination = new Vector3(666, 0, from.points.Last().z);
        // Follow the approach until the vehicle is at the access opening.
        // Begin turning only there to avoid crossing the adjacent sidewalk.
        Y06EastApproachEdit.Assign(pivot, new[] { destination + Vector3.right, destination }, evidence);
        var join = PinkR43Join(pivot, gate, id, turn, evidence);
        Y06EastApproachEdit.Assign(join, join.points.Select(point => point.V()).Prepend(from.points.Last().V()), evidence);
        return join;
    }

    private static Vector3 PinkBankAtZ(RoadMapLane lane, float z)
    {
        var path = new AssistedRoadNetwork.Path(lane.points.Select(point => point.V()).ToArray());
        return path.Position(path.Project(new Vector3(path.points.Last().x, 0, z), out _));
    }

    private static void PinkBankPavement(RoadMapDocument document, RoadMapLane lane, float width)
    {
        string id = "road:polish:" + lane.id;
        var surface = document.scene.objects.SingleOrDefault(item => item.id == id);
        if (surface == null)
        {
            surface = new ProjectMapObject { id = id, kind = "road", name = lane.roadName, lanes = 1,
                oneWay = true, suppressPaint = true };
            document.scene.objects = document.scene.objects.Append(surface).ToArray();
        }
        surface.width = width;
        surface.points = lane.points;
    }

    private static void VerifyPink(MapData map)
    {
        var graph = new HdRoadGraph(map);
        var edges = graph.edges.ToDictionary(lane => lane.id);
        var head = edges[PinkBankHead];
        var inside = edges[PinkBankInside];
        var outside = edges[PinkBankOutside];
        PinkR43Check(Mathf.Abs(head.points.Last().z - PinkBankTaperStart) < .2f &&
            head.next.Select(index => graph.edges[index].id).SequenceEqual(new[] { inside.id, outside.id }),
            "P02 1→2 northbound split and map station");
        var paint = LaneDirectionPaint.Layout(map).ToLookup(arrow => graph.edges[arrow.edgeIndex].id);
        foreach (var (lane, mask) in new[] { (inside, LaneDirectionPaint.Left | LaneDirectionPaint.Straight),
                                            (outside, LaneDirectionPaint.Right) })
        {
            PinkR43Check(lane.ruleId == PinkBank && !lane.disabled &&
                lane.next.Aggregate(0, (result, index) => result | UturnMovement.PaintMask(graph.edges[index].turn)) == mask,
                "P02 rendered northbound lane must have its recorded junction and movements " + lane.id);
            PinkR43Check(paint[lane.id].Any(arrow => arrow.turns == mask), "P02 no matching lane arrow " + lane.id);
            Y07MapTests.Footprint(map, lane);
        }
        foreach (var (join, target) in new[] { (PinkBankLeft, PinkBankWestExit), (PinkBankStraight, PinkBankNorthExit),
                                             (PinkBankRight, PinkBankEastExit) })
        {
            PinkR43Check(edges[join].next.Select(index => graph.edges[index].id).SequenceEqual(new[] { target }),
                "P02 missing receiving lane " + target);
            Y07MapTests.Footprint(map, edges[join]);
        }
        var innerEnd = inside.points.Last().V();
        var outerEnd = outside.points.Last().V();
        PinkR43Check(outerEnd.x - innerEnd.x >= 2.65f && outside.number == 2 && inside.number == 1,
            "P02 northbound lanes must remain distinct before turning");
        VerifyPinkGateWestbound(map, graph, edges, paint);
        var east55 = edges[Pink55East];
        var inner55 = edges[Pink55Added];
        PinkR43Check(east55.points.Last().x > 243 && east55.points.Last().x < 245 &&
            inner55.points.Any(p => p.x > 330 && p.x < 360 && p.z > 85.8f), "55 extra eastbound lane starts east of the school crossing");
        Y07MapTests.Footprint(map, inner55);
        VerifyPink56Midblock(map, graph, edges, paint);
        Debug.Log("R43_PINK_OK P02=1-to-2-at-z430 P15=1-to-2-at-x737 P03=55-crossing-taper P22=56-protected-left");
    }

    private static void VerifyPink56Midblock(MapData map, HdRoadGraph graph,
        System.Collections.Generic.Dictionary<string, HdEdge> edges, ILookup<string, LaneDirectionPaint.Arrow> paint)
    {
        var left = edges[Pink56Junction + ":south:left"];
        var turn = graph.edges[left.next.Single()];
        PinkR43Check(Mathf.Abs(left.points[0].z - 211) < .3f && turn.turn == -1 &&
            turn.next.Select(index => graph.edges[index].id).SequenceEqual(new[] { Pink56Junction + ":south:gate" }) &&
            paint[left.id].Any(arrow => arrow.turns == LaneDirectionPaint.Left), "56 left lane starts at the bus stop and serves the east entrance");
        Y07MapTests.Footprint(map, left);
        Y07MapTests.Footprint(map, turn);
        Y07MapTests.Footprint(map, graph.edges[turn.next.Single()]);
        PinkR43Check(map.junctionRules.Single(j => j.id == Pink56Junction).signalMode == 2, "56 missing midblock signal");
        var signal = map.editorDocument.signals.Single(p => p.junctionId == Pink56Junction);
        int green = 0;
        for (float at = 0; at < signal.Cycle; at += .5f)
        {
            if (signal.Movement(0, -1, -1, at) != SignalColor.Green) continue;
            green++;
            PinkR43Check(signal.Movement(0, 1, 0, at) != SignalColor.Green && !signal.Walk(0, -1, at) && !signal.Walk(0, 1, at),
                "56 protected left is clear of opposing through traffic and pedestrians");
        }
        PinkR43Check(green >= 16, "56 protected left green lasts at least eight seconds");
    }

    private static void VerifyPinkGateWestbound(MapData map, HdRoadGraph graph,
        System.Collections.Generic.Dictionary<string, HdEdge> edges,
        ILookup<string, LaneDirectionPaint.Arrow> paint)
    {
        var inside = edges[PinkGateInside];
        var outside = edges[PinkGateOuter];
        PinkR43Check(Mathf.Abs(edges[PinkGateWestbound].points.Last().x - 737) < .2f &&
            outside.number == 2 && inside.number == 1 &&
            Vector3.Distance(outside.points.Last().V(), inside.points.Last().V()) > 2.9f,
            "P15 westbound branches need separate approach lane centres and the recorded taper station");
        foreach (var (lane, mask) in new[] { (inside, LaneDirectionPaint.Left),
                                            (outside, LaneDirectionPaint.Straight | LaneDirectionPaint.Right) })
        {
            PinkR43Check(lane.next.Aggregate(0, (result, index) => result | UturnMovement.PaintMask(graph.edges[index].turn)) == mask &&
                paint[lane.id].Any(arrow => arrow.turns == mask), "P15 wrong westbound movement or glyph " + lane.id);
            Y07MapTests.Footprint(map, lane);
        }
        foreach (var (entry, movement, exit) in new[] { (PinkGateInside, PinkGateLeft, PinkGateHanyang),
            (PinkGateOuter, PinkGateRight, PinkGateHoban), (PinkGateOuter, PinkGateThrough, PinkGateDeparture) })
        {
            int start = Array.FindIndex(graph.edges, lane => lane.id == entry);
            int target = Array.FindIndex(graph.edges, lane => lane.id == exit);
            var route = graph.Route(start, graph.paths[start].length - 4, target, 3);
            PinkR43Check(route != null && route.edges.Select(index => graph.edges[index].id)
                .SequenceEqual(new[] { entry, movement, exit }), "P15 route does not use the observed westbound gate movement");
            Y07MapTests.Footprint(map, edges[movement]);
        }
    }

    private static void PinkR43Check(bool condition, string reason)
    {
        if (!condition)
            throw new InvalidOperationException("R43_PINK: " + reason);
    }
}
