"""Read-only audit of the project's imported marks and compiled lane paint sources."""

import argparse
from collections import Counter, defaultdict
import json
import math
from pathlib import Path


SAFETY_PAIR_MIN = 0.3
SAFETY_PAIR_MAX = 6.0


def _point(point):
    return (float(point["x"]), float(point["z"]))


def _distance(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1])


def _signature(mark):
    points = tuple(_point(point) for point in mark.get("points", []))
    return (mark.get("type"), mark.get("kind"), min(points, points[::-1]))


def _parallel_gap(a, b):
    """Return separation for overlapping, approximately parallel chords."""
    pa, pb = a.get("points", []), b.get("points", [])
    if len(pa) < 2 or len(pb) < 2:
        return None
    a0, a1, b0, b1 = _point(pa[0]), _point(pa[-1]), _point(pb[0]), _point(pb[-1])
    da, db = (a1[0] - a0[0], a1[1] - a0[1]), (b1[0] - b0[0], b1[1] - b0[1])
    la, lb = math.hypot(*da), math.hypot(*db)
    if min(la, lb) < 4:
        return None
    dot = (da[0] * db[0] + da[1] * db[1]) / (la * lb)
    if abs(dot) < .97:
        return None
    ux, uz = da[0] / la, da[1] / la
    start, end = sorted(((b0[0] - a0[0]) * ux + (b0[1] - a0[1]) * uz,
                         (b1[0] - a0[0]) * ux + (b1[1] - a0[1]) * uz))
    if min(la, end) - max(0, start) < 4:
        return None
    gap0 = abs((b0[0] - a0[0]) * uz - (b0[1] - a0[1]) * ux)
    gap1 = abs((b1[0] - a0[0]) * uz - (b1[1] - a0[1]) * ux)
    gap = (gap0 + gap1) / 2
    return round(gap, 3) if SAFETY_PAIR_MIN <= gap <= SAFETY_PAIR_MAX else None


def _point_to_mark(point, mark):
    best = float("inf")
    for a, b in zip(mark.get("points", []), mark.get("points", [])[1:]):
        a, b = _point(a), _point(b)
        dx, dz = b[0] - a[0], b[1] - a[1]
        length2 = dx * dx + dz * dz
        fraction = max(0, min(1, ((point[0] - a[0]) * dx + (point[1] - a[1]) * dz) / length2)) if length2 else 0
        best = min(best, _distance(point, (a[0] + fraction * dx, a[1] + fraction * dz)))
    return best


def _generated_sources(edges):
    """Identify source groups from which RoadPaintGeometry generates yellow borders."""
    groups = defaultdict(list)
    for edge in edges:
        if (edge.get("disabled") or edge.get("status") != "provisional"
                or edge.get("turn") or edge.get("change") or not edge.get("points")
                or str(edge.get("id", "")).startswith(("join:", "fuel:"))
                or str(edge.get("sourceId", "")).startswith("fuel:")):
            continue
        groups[edge.get("sourceId", "")].append(edge)
    candidates = []
    for source, lanes in groups.items():
        if not any(lane.get("number") == 1 for lane in lanes):
            continue
        vectors = []
        for lane in lanes:
            points = lane["points"]
            a, b = _point(points[0]), _point(points[-1])
            dx, dz = b[0] - a[0], b[1] - a[1]
            if math.hypot(dx, dz) > 4:
                vectors.append((lane, (dx, dz)))
        opposing = next(((a, b) for a in vectors for b in vectors
                         if a[1][0] * b[1][0] + a[1][1] * b[1][1] < 0), None)
        if opposing:
            a, b = opposing[0][0], opposing[1][0]
            pa, pb = a["points"][len(a["points"]) // 2], b["points"][len(b["points"]) // 2]
            sample = ((_point(pa)[0] + _point(pb)[0]) / 2, (_point(pa)[1] + _point(pb)[1]) / 2)
            candidates.append({"sourceId": source, "compiledLanes": len(lanes),
                               "approximateMidpoint": {"x": round(sample[0], 3), "z": round(sample[1], 3)}})
    return sorted(candidates, key=lambda item: item["sourceId"])


def audit(map_data, catalog=None):
    marks = map_data.get("hdMarks", [])
    seen, unique, duplicates = {}, [], []
    for mark in marks:
        signature = _signature(mark)
        if signature in seen:
            duplicates.append(sorted([seen[signature], mark["id"]]))
        else:
            seen[signature] = mark["id"]
            unique.append(mark)
    safety_pairs = []
    safety_ids = set()
    confirmed_ids = set()
    for facility in (catalog or {}).get("facilities", []):
        if facility.get("kind") == "paired-median" and facility.get("sourceUrl"):
            confirmed_ids.update(facility.get("northMarkIds", []))
            confirmed_ids.update(facility.get("southMarkIds", []))
    centre = [mark for mark in unique if mark.get("kind") == "501"]
    for i, a in enumerate(centre):
        for b in centre[i + 1:]:
            gap = _parallel_gap(a, b)
            if gap is not None:
                safety_pairs.append({"marks": [a["id"], b["id"]], "meanChordGapM": gap})
                safety_ids.update((a["id"], b["id"]))
    roles = Counter()
    for mark in unique:
        kind = mark.get("kind")
        role = ("central_safety_boundary" if mark["id"] in confirmed_ids else
                "parallel_501_candidate" if mark["id"] in safety_ids else
                "centerline" if kind == "501" else
                "safety_area" if kind == "531" else
                "road_edge" if kind == "505" else "other_imported")
        roles[role] += 1
    generated = _generated_sources(map_data.get("hdEdges", []))
    imported_yellow = [mark for mark in unique if str(mark.get("type", "")).startswith("1")]
    comparisons = []
    for candidate in generated:
        midpoint = candidate["approximateMidpoint"]
        sample = (midpoint["x"], midpoint["z"])
        distances = ((_point_to_mark(sample, mark), mark["id"]) for mark in imported_yellow)
        gap, nearest = min(distances, default=(float("inf"), None))
        comparisons.append({"sourceId": candidate["sourceId"], "nearestImportedMark": nearest,
                            "gapM": round(gap, 3) if math.isfinite(gap) else None,
                            "relationship": "coincident_sample" if gap <= .25 else "near_sample" if gap <= 6 else "no_nearby_imported_sample"})
    relationship_counts = Counter(item["relationship"] for item in comparisons)
    return {
        "counts": {"roads": len(map_data.get("roads", [])), "lanes": len(map_data.get("hdEdges", [])),
                   "importedMarks": len(marks), "uniqueImportedMarks": len(unique)},
        "markTypeKindCounts": {f"{typ}/{kind}": count for (typ, kind), count in sorted(
            Counter((mark.get("type"), mark.get("kind")) for mark in marks).items())},
        "exactDuplicatePairs": sorted(duplicates), "markRoles": dict(sorted(roles.items())),
        "parallelCenterlineCandidates": safety_pairs,
        "generatedYellowSourceCandidates": generated,
        "generatedVsImported": {"coincidentSamples": relationship_counts["coincident_sample"],
                                "nearSamples": relationship_counts["near_sample"],
                                "noNearbySamples": relationship_counts["no_nearby_imported_sample"],
                                "samples": comparisons},
        "limitations": [
            "Parallel pairs are geometric review candidates, not field-confirmed facilities.",
            "Generated yellow source midpoints approximate lane-group centers; proximity is a review cue, not a mesh overlap measurement. This script does not sample Unity's final paint meshes.",
            "Only the catalog's Suin-ro section has confirmed street-view barrier evidence; other sites remain unvisited.",
        ],
    }


def _markdown(report):
    counts = report["counts"]
    lines = ["# Whole-map road-marking audit", "",
             f"Roads: {counts['roads']}; compiled lanes: {counts['lanes']}; imported marks: {counts['importedMarks']} "
             f"({counts['uniqueImportedMarks']} after exact-geometry deduplication).", "",
             "## Imported type/kind counts", ""]
    lines += [f"- {key}: {count}" for key, count in report["markTypeKindCounts"].items()]
    lines += ["", "## Roles after deduplication", ""]
    lines += [f"- {key}: {count}" for key, count in report["markRoles"].items()]
    lines += ["", "Kind 501 denotes a centerline in the source. Parallel 501 pairs are candidates until a facility catalog names the reviewed strip; "
              "kind 531 denotes a safety area; kind 505 is a road edge.", "",
              f"Exact duplicate pairs: {len(report['exactDuplicatePairs'])}; parallel 501 review candidates: "
              f"{len(report['parallelCenterlineCandidates'])}; generated-yellow source candidates: "
              f"{len(report['generatedYellowSourceCandidates'])}.", "",
              "Generated midpoint samples versus imported yellow marks: "
              f"{report['generatedVsImported']['coincidentSamples']} coincident (<=0.25 m), "
              f"{report['generatedVsImported']['nearSamples']} near (<=6 m), "
              f"{report['generatedVsImported']['noNearbySamples']} without a nearby imported sample.", "",
              "## Exact duplicate pairs", ""]
    lines += [f"- {', '.join(pair)}" for pair in report["exactDuplicatePairs"]]
    lines += ["", "## Parallel centerline review candidates", ""]
    lines += [f"- {', '.join(pair['marks'])}: mean chord gap {pair['meanChordGapM']} m"
              for pair in report["parallelCenterlineCandidates"]]
    lines += ["", "## Scope", ""]
    lines += [f"- {item}" for item in report["limitations"]]
    return "\n".join(lines) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--package", default="config/maps/project/eungye.project.json")
    parser.add_argument("--catalog", default="config/maps/eungye-road-facilities.json")
    parser.add_argument("--output", default="artifacts/road-markings-audit")
    args = parser.parse_args()
    package = json.loads(Path(args.package).read_text(encoding="utf-8"))
    catalog = json.loads(Path(args.catalog).read_text(encoding="utf-8")) if Path(args.catalog).exists() else None
    report = audit(package["baseline"] if "baseline" in package else package, catalog)
    stem = Path(args.output)
    stem.parent.mkdir(parents=True, exist_ok=True)
    stem.with_suffix(".json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    stem.with_suffix(".md").write_text(_markdown(report), encoding="utf-8")
    print(json.dumps({"counts": report["counts"], "exactDuplicatePairs": len(report["exactDuplicatePairs"]),
                      "parallelCenterlineCandidates": len(report["parallelCenterlineCandidates"]),
                      "generatedYellowSourceCandidates": len(report["generatedYellowSourceCandidates"])}))


if __name__ == "__main__":
    main()
