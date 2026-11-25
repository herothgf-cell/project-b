"""Pedestrian-street conversion (2026-11-26 map review, roadview-confirmed Eungye pedestrian streets).

Seeds are the through lanes of the streets the roadview showed as vehicle-free; the closure disables every
connector that starts or ends on a disabled lane (repeat until stable) and reports active lanes that would be
left without an exit (dead ends) -- those block the change.
Usage: python tools/map_review_plaza_plan.py   -> config/maps/review/r60-plaza-lanes.json
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'config/maps/review/r60-plaza-lanes.json'
# scene road surfaces of those streets: disabled and replaced by a pedestrian-space polygon of the same strip
# (1084/3830/1141/4129 are the J2468 crossing pieces. The alley-end pieces 3374/1144 and the 1115/1129 links lie
# inside the kept junctions and carriageways of Daeeun-ro, J2640, J2111 and J2830, so they stay)
SCENE_ROADS = {'south': ['road:37612022:A0020000:1107:0', 'road:37612022:A0020000:1132:0', 'road:37612022:A0020000:2501:0', 'road:37612022:A0020000:3693:0',
                         'road:37612022:A0020000:1084:0', 'road:37612022:A0020000:3830:0', 'road:37612022:A0020000:1141:0', 'road:37612022:A0020000:4129:0']}
SEEDS = {
    # south: Daeeun-ro (J2225) - J2468 - J2640, and J2468's two short alleys (J001-3, J038-2/3, J043-1/2, J045-1, J046-2, J047-2)
    'south': ['local:2279:11842:2400:11623', 'local:2453:11651:2331:11872', 'local:2481:11466:2583:11215', 'local:2640:11234:2536:11490',
              'local:2204:11415:2394:11503', 'local:2382:11530:2191:11442', 'local:2743:11710:2540:11614', 'local:2554:11587:2755:11683'],
}


def main(groups):
    d = json.loads((ROOT / 'config/maps/project/eungye.project.json').read_text(encoding='utf-8-sig'))['document']
    lanes = {l['id']: l for l in d['lanes']}
    off = {s for g in groups for s in SEEDS[g]}
    missing = [s for s in off if s not in lanes]
    assert not missing, missing
    changed = True
    while changed:
        changed = False
        for l in d['lanes']:
            if l['id'] in off or l.get('disabled') or not str(l['id']).startswith('join:'):
                continue
            starts_off = any(l['id'] in lanes[o].get('next', []) for o in off)
            ends_off = any(n in off for n in l.get('next', []))
            if starts_off or ends_off:
                off.add(l['id']); changed = True
    dead = []
    for l in d['lanes']:
        if l['id'] in off or l.get('disabled'):
            continue
        nxt = [n for n in l.get('next', []) if n in lanes and not lanes[n].get('disabled')]
        if nxt and all(n in off for n in nxt):
            dead.append(l['id'])
    scene = d['scene']['objects']; byid = {o['id']: o for o in scene}
    keep_roads = [o for o in scene if o['kind'] == 'road' and o.get('enabled', True) and o['id'] not in {r for g in groups for r in SCENE_ROADS[g]}]
    import math

    def segd(p, a, b):
        dx, dz = b[0] - a[0], b[1] - a[1]; L = dx * dx + dz * dz
        t = 0 if L == 0 else max(0, min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / L))
        return math.dist(p, (a[0] + t * dx, a[1] + t * dz))

    def clear(p):  # metres outside the nearest kept road's carriageway
        best = 99.0
        for o in keep_roads:
            pts = [(q['x'], q['z']) for q in o['points']]
            if min(abs(p[0] - q[0]) + abs(p[1] - q[1]) for q in pts) > 80:
                continue
            best = min(best, min(segd(p, a, b) for a, b in zip(pts, pts[1:])) - o['width'] / 2)
        return best

    plazas = []
    for g in groups:
        for rid in SCENE_ROADS[g]:
            o = byid[rid]; pts = [(q['x'], q['z']) for q in o['points']]
            dense = []  # resample every 0.5 m, then trim both ends until clear of kept roads by 0.3 m
            for a, b in zip(pts, pts[1:]):
                n = max(1, int(math.dist(a, b) / .5))
                dense += [(a[0] + (b[0] - a[0]) * k / n, a[1] + (b[1] - a[1]) * k / n) for k in range(n)]
            dense.append(pts[-1])
            w = o['width'] / 2
            ok = [clear(p) > w * 0 + .3 for p in dense]
            if not any(ok):
                print('plaza', rid, 'inside a kept road, no polygon'); continue
            i0 = next(i for i, v in enumerate(ok) if v); i1 = len(ok) - 1 - next(i for i, v in enumerate(reversed(ok)) if v)
            if i1 - i0 < 2:
                print('plaza', rid, 'too short after trimming'); continue
            line = dense[i0:i1 + 1:4] + ([dense[i1]] if (i1 - i0) % 4 else [])
            left, right = [], []
            for k, p in enumerate(line):
                a = line[max(0, k - 1)]; b = line[min(len(line) - 1, k + 1)]
                L = math.dist(a, b) or 1; f = ((b[0] - a[0]) / L, (b[1] - a[1]) / L); r = (f[1], -f[0])
                left.append((p[0] - r[0] * w, p[1] - r[1] * w)); right.append((p[0] + r[0] * w, p[1] + r[1] * w))
            ring = left + right[::-1]
            plazas.append(dict(id='sidewalk:review:r60:' + rid.split(':', 1)[1], source=rid, trimmed=[round(dense[0][0], 1), round(dense[0][1], 1), round(dense[i0][0], 1), round(dense[i0][1], 1), round(dense[-1][0], 1), round(dense[-1][1], 1), round(dense[i1][0], 1), round(dense[i1][1], 1)],
                               points=[dict(x=round(x, 3), z=round(z, 3)) for x, z in ring]))
            print('plaza', rid, 'kept', i1 - i0 + 1, 'of', len(dense), 'samples')
    junctions = sorted({lanes[i].get('ruleId') for i in off if lanes[i].get('ruleId')})
    print('disable', len(off), 'dead ends', dead, 'junctions', junctions)
    OUT.write_text(json.dumps(dict(fromRevision=59, toRevision=60, groups=groups, disable=sorted(off), deadEnds=dead, junctions=junctions, disableScene=[r for g in groups for r in SCENE_ROADS[g]], plazas=plazas), ensure_ascii=False, indent=1), encoding='utf-8')
    return off, dead


if __name__ == '__main__':
    main(sys.argv[1:] or ['south'])
