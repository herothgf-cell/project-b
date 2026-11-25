"""Build the r55 crosswalk edit list (2026-11-25 map review, markings batch 1).

Answers taken from the review JSON + confirmation answers (docs/work-history/2026-11-25-map-review-markings-1.md).
Generated crosswalk geometry comes from a player dump whose crosswalk stripes carry "a<axis> s<sign>"
(tools/map_crosswalk_approaches.py). Every edit becomes a document crosswalk facility with the same
junction/axis/sign, which replaces the generated one. Kakao aerials (Git-ignored) showed every bicycle
crossing on the outer side of its crosswalk, and the stop line sits ~0.9 m outside the stripes, so a
crosswalk that gains a bicycle crossing moves SHIFT m toward the junction and the strip takes its old place.
Usage: python tools/map_review_r55_crosswalks.py <map-audit.txt with tagged crosswalks>
Output: config/maps/review/r55-crosswalk-edits.json (read by MapReviewCrosswalkEdits.Apply)
"""
import json
import math
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from map_crosswalk_approaches import approaches  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'config/maps/review/r55-crosswalk-edits.json'
SHIFT, BIKE_DEPTH, GAP, CORNER_GAP = 2.3, 2.0, .15, .3
YELLOW, WHITE = '황색', '안내'
ALL = None
# junction: (review refs, colour, bicycle crossing, approaches[(axis, sign)] or ALL)
PLAN = [
    ('J2145_15970', 'J004-1', YELLOW, True, ALL),
    ('J4396_13458', 'J007-2', WHITE, True, ALL),
    ('J5057_4449', 'J008-3', WHITE, True, ALL),
    ('J5692_14220', 'J011-2', WHITE, True, [(0, 1), (0, -1)]),
    ('J6199_11987', 'J013-1', WHITE, True, [(0, 1), (0, -1), (1, 1)]),
    ('J7254_14780', 'J016-1', WHITE, True, ALL),
    ('J7801_9629', 'J017-2', WHITE, True, ALL),
    ('J7973_3744', 'J020-3', WHITE, True, ALL),
    ('J7841_867', 'J018-4, J065-1', YELLOW, False, ALL),
    ('J7917_2565', 'J019-3, J067-2', YELLOW, False, [(1, -1), (1, 1), (0, -1)]),
    ('J8066_6364', 'J021-1', YELLOW, False, ALL),
]
# J7841_867 (J018-1, J065-1): yellow X diagonal crossings between the corner pairs of the four crosswalks.
DIAGONAL = ('J7841_867', 'J018-1, J065-1', 4.0, .8)


def footprint(e, bike):
    y = math.radians(e['yaw']); f = (math.sin(y), math.cos(y)); r = (f[1], -f[0])
    back = e['length'] / 2 + (GAP + BIKE_DEPTH if bike else 0)
    return dict(c=(e['x'], e['z']), f=f, r=r, w=e['width'], d0=-back, d1=e['length'] / 2)


def inside(p, fp, pad):
    dx, dz = p[0] - fp['c'][0], p[1] - fp['c'][1]
    t = dx * fp['r'][0] + dz * fp['r'][1]; d = dx * fp['f'][0] + dz * fp['f'][1]
    return abs(t) <= fp['w'] / 2 + pad and fp['d0'] - pad <= d <= fp['d1'] + pad


def trim(e, others, pad=CORNER_GAP):
    """Shorten a crossing (and its strip) so neither end runs into another crossing at the same junction."""
    a = footprint(e, True); ends = []
    for side in (1, -1):
        t = 0.0
        while t < a['w'] / 2:
            nt = t + .1
            pts = [(a['c'][0] + a['r'][0] * side * nt + a['f'][0] * d, a['c'][1] + a['r'][1] * side * nt + a['f'][1] * d)
                   for d in [a['d0'] + k * (a['d1'] - a['d0']) / 8 for k in range(9)]]
            if any(inside(p, o, pad) for o in others for p in pts):
                break
            t = nt
        ends.append(min(t, a['w'] / 2))
    right, left = ends; shift = (right - left) / 2
    e['x'] = round(e['x'] + a['r'][0] * shift, 3); e['z'] = round(e['z'] + a['r'][1] * shift, 3); e['width'] = round(right + left, 3)
    return (a['w'] - e['width'])


def main(audit):
    gen = approaches(audit)
    doc = json.loads((ROOT / 'config/maps/project/eungye.project.json').read_text(encoding='utf-8-sig'))['document']
    sheet = {i['junction']: i for i in json.loads((ROOT / 'artifacts/map-review-20261124/sheet/items.json').read_text(encoding='utf-8')) if i['kind'] == 'junction'}
    stops = [f for f in doc['facilities'] if f['kind'] == 'stopLine']
    paint_stops = []
    for line in Path(audit).read_text(encoding='utf-8').splitlines():
        m = re.match(r'PAINT Stop line (\S+) at=\(([-\d.]+), [-\d.]+, ([-\d.]+)\)', line)
        if m:
            paint_stops.append((m[1], float(m[2]), float(m[3])))
    edits = []
    for jid, refs, colour, bike, which in PLAN:
        centre = (sheet[jid]['x'], sheet[jid]['z'])
        docs = {(f['axis'], f['sign']): f for f in doc['facilities'] if f['kind'] == 'crosswalk' and f['junctionId'] == jid}
        keys = sorted(set(k[1:] for k in gen if k[0] == jid) | set(docs)) if which is ALL else which
        for axis, sign in keys:
            src = docs.get((axis, sign))
            g = gen.get((jid, axis, sign))
            base = dict(x=src['x'], z=src['z'], yaw=src['yaw'], width=src['width'], length=src['length']) if src else dict(x=g['x'], z=g['z'], yaw=g['yaw'], width=g['width'], length=2.4)
            y = math.radians(base['yaw']); fx, fz = math.sin(y), math.cos(y)
            if (centre[0] - base['x']) * fx + (centre[1] - base['z']) * fz < 0:  # +z must face the junction
                base['yaw'] = (base['yaw'] + 180) % 360; fx, fz = -fx, -fz
            x, z = base['x'], base['z']
            if bike:
                x += fx * SHIFT; z += fz * SHIFT
                strip = (x - fx * (base['length'] / 2 + GAP + BIKE_DEPTH / 2), z - fz * (base['length'] / 2 + GAP + BIKE_DEPTH / 2))
                for sx, sz, name in [(f['x'], f['z'], f['id']) for f in stops if f['junctionId'] == jid] + [(p[1], p[2], 'generated') for p in paint_stops if p[0] == jid]:
                    along = (sx - strip[0]) * fx + (sz - strip[1]) * fz; lat = abs((sx - strip[0]) * fz - (sz - strip[1]) * fx)
                    if abs(along) < BIKE_DEPTH / 2 + .2 and lat < base['width'] / 2:
                        raise SystemExit(f'{jid} a{axis} s{sign}: bicycle crossing would cover stop line {name} (along {along:.2f})')
            edits.append(dict(junctionId=jid, axis=axis, sign=sign, existing=src['id'] if src else '', refs=refs, type=colour,
                              text='자전거횡단도:바깥쪽' if bike else '게임 설정', x=round(x, 3), z=round(z, 3), yaw=round(base['yaw'], 3),
                              width=round(min(base['width'], 40.0), 3), length=base['length']))  # validator limit; Effective() refits two-way crossings
    for jid, refs, colour, bike, which in PLAN:
        if not bike:
            continue
        mine = [e for e in edits if e['junctionId'] == jid]
        keys = {(e['axis'], e['sign']) for e in mine}
        fixed = [footprint(dict(x=g['x'], z=g['z'], yaw=g['yaw'], width=g['width'], length=2.4), False) for k, g in gen.items() if k[0] == jid and k[1:] not in keys]
        fixed += [footprint(f, False) for f in doc['facilities'] if f['kind'] == 'crosswalk' and f['junctionId'] == jid and (f['axis'], f['sign']) not in keys]
        for e in sorted(mine, key=lambda e: e['width']):
            others = fixed + [footprint(o, o['text'] != '게임 설정') for o in mine if o is not e]
            cut = trim(e, others)
            if cut > .05:
                print(f"  trimmed {jid} a{e['axis']} s{e['sign']} by {cut:.1f} m -> {e['width']:.1f} m")
    jid, refs, depth, keep = DIAGONAL
    cw = {(e['axis'], e['sign']): e for e in edits if e['junctionId'] == jid}
    xs = sorted(cw.values(), key=lambda e: e['x']); zs = sorted(cw.values(), key=lambda e: e['z'])
    west, east, south, north = xs[0]['x'] + 1.2, xs[-1]['x'] - 1.2, zs[0]['z'] + 1.2, zs[-1]['z'] - 1.2
    for k, (a, b) in enumerate([((west, north), (east, south)), ((east, north), (west, south))]):
        mx, mz = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2; L = math.dist(a, b)
        along = math.degrees(math.atan2(b[0] - a[0], b[1] - a[1]))
        edits.append(dict(junctionId='', axis=0, sign=1, existing='', id=f'review:r55:{jid}:diagonal:{k + 1}', refs=refs, type=YELLOW, text='대각선 횡단',
                          x=round(mx, 3), z=round(mz, 3), yaw=round((along + 90) % 360, 3), width=round(L * keep, 3), length=depth))
    OUT.write_text(json.dumps(dict(fromRevision=54, toRevision=55, edits=edits), ensure_ascii=False, indent=1), encoding='utf-8')
    print(OUT, len(edits))


if __name__ == '__main__':
    main(sys.argv[1])
