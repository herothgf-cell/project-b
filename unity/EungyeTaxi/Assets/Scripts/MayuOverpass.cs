using System;
using System.Collections.Generic;
using UnityEngine;

// The aerial at J10580 shows Mayu-ro passing over the diagonal Suin-ro
// intersection. The captured NGII turning lanes serve the intersection below.
public static class MayuOverpass {
    public const string RoadId = "37612023:A0020000:232:0";
    const string Junction = "J10580_1415";
    const float DeckHeight = 6.2f;

    // Survey data supplies the horizontal centreline. Clearance and ramp
    // lengths are game dimensions. Both ends meet the original ground road.
    static float Height(float z) {
        float t = Mathf.Min(Mathf.InverseLerp(8, 105, z), Mathf.InverseLerp(290, 190, z));
        return DeckHeight * Mathf.SmoothStep(0, 1, t);
    }

    public static void Apply(MapData map) {
        if (map.mapId != "eungye" || map.hdEdges == null || map.roads == null) return;
        var road = Array.Find(map.roads, r => r.id == RoadId);
        if (road == null) return;
        foreach (var p in road.points) p.elevation += Height(p.z);
        road.suppressPaint = true;
        var roads = new List<MapRoad>(map.roads);
        roads.Add(Frontage(road, -1));
        roads.Add(Frontage(road, 1));
        map.roads = roads.ToArray();
        var removed = new HashSet<int>();
        for (int i = 0; i < map.hdEdges.Length; i++) {
            var edge = map.hdEdges[i];
            if (edge.sourceId == null || Array.IndexOf(edge.sourceId.Split(','), RoadId) < 0) continue;
            if (edge.ruleId == Junction && edge.id.StartsWith("join:") && edge.turn != 0) {
                edge.disabled = true;
                edge.next = Array.Empty<int>();
                removed.Add(i);
                continue;
            }
            foreach (var point in edge.points) point.elevation += Height(point.z);
        }
        if (removed.Count > 0) foreach (var edge in map.hdEdges)
            if (Array.Exists(edge.next, removed.Contains)) edge.next = Array.FindAll(edge.next, n => !removed.Contains(n));
    }

    // The NGII captures of the surface ramps stop independently at lateral
    // offsets of 12..20 metres. Keep the ground carriageways continuous past
    // those capture ends, narrowing them into the road at the ramp feet.
    static MapRoad Frontage(MapRoad deck, int side) {
        var centerline = new List<MapPoint>();
        var points = deck.points;
        for (int i = 0; i < points.Length; i++) {
            var p = points[i];
            if (p.z > 301) continue;
            var north = points[Mathf.Max(0,i-1)].V() - points[Mathf.Min(i+1,points.Length-1)].V();
            var right = TrafficSystem.Right(north).normalized;
            float offset = Mathf.Lerp(3f,15.6f,Height(p.z)/DeckHeight);
            var center = p.V() + right * (side*offset);
            centerline.Add(new MapPoint{x=center.x,z=center.z});
        }
        return new MapRoad{id="mayu:frontage:"+side,name="마유로 고가 옆 지상도로",
            lanes=3,oneWay=1,width=12f,suppressPaint=true,points=centerline.ToArray()};
    }

    public static void Draw(WorldBuilder world, MapRoad road, bool collisions, Material asphalt) {
        var deck = new Dictionary<Vector2Int, WorldBuilder.GeometryBatch>();
        var parapets = new Dictionary<Vector2Int, WorldBuilder.GeometryBatch>();
        var stripes = new Dictionary<Vector2Int, WorldBuilder.GeometryBatch>();
        var median = new Dictionary<Vector2Int, WorldBuilder.GeometryBatch>();
        var points = road.points;
        var sides = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++) {
            var tangent = points[Mathf.Min(i + 1, points.Length - 1)].V() - points[Mathf.Max(i - 1, 0)].V();
            sides[i] = TrafficSystem.Right(tangent).normalized;
        }
        float half = road.width * .5f;
        float distance = 0;
        for (int i = 1; i < points.Length; i++) {
            var a = points[i-1].V(.045f);
            var b = points[i].V(.045f);
            var sideA = sides[i-1];
            var sideB = sides[i];
            var slab = WorldBuilder.Batch(deck, a);
            var concrete = WorldBuilder.Batch(parapets, a);
            var white = WorldBuilder.Batch(stripes, a);
            var yellow = WorldBuilder.Batch(median, a);
            Strip(slab, a, b, sideA, sideB, -half, half, 0);
            if (Mathf.Max(a.y,b.y) > .1f) {
                var down = Vector3.down * .55f;
                Quad(concrete, a + sideA*half + down, b + sideB*half + down,
                     b - sideB*half + down, a - sideA*half + down);
                foreach (int sign in new[]{-1,1}) {
                    var outerA = a + sign * half * sideA;
                    var outerB = b + sign * half * sideB;
                    var innerA = outerA - sign * .32f * sideA;
                    var innerB = outerB - sign * .32f * sideB;
                    var up = Vector3.up * .72f;
                    Quad(concrete, outerA + down, outerB + down, outerB + up, outerA + up, sign > 0);
                    Quad(concrete, innerA, innerB, innerB + up, innerA + up, sign < 0);
                    Strip(concrete, a, b, sideA, sideB, sign < 0 ? -half : half-.32f,
                          sign < 0 ? -half+.32f : half, .72f);
                }
            }
            Strip(yellow, a, b, sideA, sideB, -.20f, -.08f, .012f);
            Strip(yellow, a, b, sideA, sideB, .08f, .20f, .012f);
            world.CentreLines?.Add(a,b);
            float length = Vector3.Distance(a,b);
            foreach (int sign in new[]{-1,1}) {
                float edge = sign*(half-.54f);
                Strip(white, a, b, sideA, sideB, edge-.08f, edge+.08f, .013f);
                float divider = sign*half*.5f;
                for (float start = Mathf.Floor(distance/8)*8; start < distance+length; start += 8) {
                    float from = Mathf.Clamp01((start-distance)/length);
                    float to = Mathf.Clamp01((start+3-distance)/length);
                    if (to <= from) continue;
                    Strip(white, Vector3.Lerp(a,b,from), Vector3.Lerp(a,b,to),
                          Vector3.Lerp(sideA,sideB,from), Vector3.Lerp(sideA,sideB,to),
                          divider-.075f, divider+.075f, .013f);
                }
            }
            distance += length;
        }
        world.RenderBatches(deck, "마유로 고가 상판", asphalt, collisions);
        world.RenderBatches(parapets, "마유로 고가 난간·하부", WorldBuilder.Mat("고가 콘크리트", new Color(.48f,.48f,.44f)), collisions);
        world.RenderBatches(stripes, "마유로 고가 차선", WorldBuilder.Mat("고가 백색선", new Color(.92f,.91f,.84f)));
        world.RenderBatches(median, "마유로 고가 중앙선", WorldBuilder.Mat("고가 황색선", new Color(1,.74f,.12f)));
    }

    static void Strip(WorldBuilder.GeometryBatch batch, Vector3 a, Vector3 b, Vector3 sideA,
                      Vector3 sideB, float left, float right, float height) {
        var up = Vector3.up*height;
        Quad(batch, a+sideA*left+up, b+sideB*left+up, b+sideB*right+up, a+sideA*right+up);
    }
    static void Quad(WorldBuilder.GeometryBatch batch, Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool flip=false) {
        batch.Add(new[]{a,b,c,d}, flip ? new[]{0,2,1,0,3,2} : new[]{0,1,2,0,2,3});
    }
}
