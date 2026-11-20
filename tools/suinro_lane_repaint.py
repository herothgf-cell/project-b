"""Build the 2026-11-20 Suin-ro lane repaint as map facilities (masks + strokes).

The NGII source markings on Suin-ro (2020 survey) no longer match the current
road: centre and kerb lines run across today's lanes (checked against Kakao
aerial photos and road view, 2026-06). This script derives lane dividers,
a double yellow centre and white edge lines from the driven NGII lanes, which
are where cars actually drive, and writes masks that hide the old source lines
in the same footprint. Read-only on the map; the output JSON edit file is
appended to the project map by the editor method SuinroLaneRepaint.Apply.

    python tools/suinro_lane_repaint.py config/maps/edits/2026-11-20-suinro-lane-repaint.json
"""
import json
import math
import sys

MAP = 'config/maps/project/eungye.project.json'
d = json.load(open(MAP, encoding='utf-8-sig'))
lanes = [l for l in d['document']['lanes'] if not l['disabled'] and l['turn'] == 0 and l['change'] == 0
         and l['status'] == 'ngii' and not l['id'].startswith('join') and len(l['points']) >= 2]
uturn_starts = [(l['points'][0]['x'], l['points'][0]['z']) for l in d['document']['lanes']
                if not l['disabled'] and l['turn'] == -2]
rules = {r['id']: r for r in d['baseline']['junctionRules']}
suin = [(p['x'], p['z']) for l in lanes if l['roadName'] == '수인로' for p in l['points']]
x0, x1 = min(p[0] for p in suin), max(p[0] for p in suin)

# Corridor centreline: median z of Suin-ro lane points per 20 m x bin, lightly smoothed.
V = []
x = x0
while x <= x1:
    zs = sorted(p[1] for p in suin if abs(p[0] - x) < 12)
    if zs:
        V.append((x, zs[len(zs) // 2]))
    x += 20
V = [(V[i][0], sum(v[1] for v in V[max(0, i - 1):i + 2]) / len(V[max(0, i - 1):i + 2])) for i in range(len(V))]
cum = [0.0]
for a, b in zip(V, V[1:]):
    cum.append(cum[-1] + math.dist(a, b))


def frame(s):
    for i in range(len(V) - 1):
        if cum[i] <= s <= cum[i + 1]:
            n = cum[i + 1] - cum[i]
            t = (s - cum[i]) / n
            a, b = V[i], V[i + 1]
            u = ((b[0] - a[0]) / n, (b[1] - a[1]) / n)
            return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t), u, (u[1], -u[0])
    return None


# Junction boxes keep their own markings (stop lines, guides, arrows).
JUNCTIONS = [(rules[j]['x'], rules[j]['z'], max([a['width'] for a in rules[j]['approaches']] + [20]) / 2 + 4)
             for j in ('J866_5285', 'J1927_5181', 'J5057_4449', 'J7973_3744')]
JUNCTIONS.append((rules['J6478_4105']['x'], rules['J6478_4105']['z'], 5))
# J5057 approaches were reviewed against road view on 2026-11-09 (lane roles, observed
# yellow kerb); keep their source paint out to 36 m from the junction centre.
JUNCTIONS = [(x, z, 36 if (x, z) == (rules['J5057_4449']['x'], rules['J5057_4449']['z']) else c) for x, z, c in JUNCTIONS]


def offsets(C, u, r):
    out = []
    for l in lanes:
        pp = l['points']
        for p, q in zip(pp, pp[1:]):
            sp = (p['x'] - C[0]) * u[0] + (p['z'] - C[1]) * u[1]
            sq = (q['x'] - C[0]) * u[0] + (q['z'] - C[1]) * u[1]
            if sp * sq > 0 or sp == sq:
                continue
            t = -sp / (sq - sp)
            x = p['x'] + (q['x'] - p['x']) * t
            z = p['z'] + (q['z'] - p['z']) * t
            dx, dz = q['x'] - p['x'], q['z'] - p['z']
            dot = (dx * u[0] + dz * u[1]) / math.hypot(dx, dz)
            off = (x - C[0]) * r[0] + (z - C[1]) * r[1]
            if abs(off) < 16 and abs(dot) > .93:
                out.append((off, 1 if dot > 0 else -1))
            break
    return out


def dedupe(values):
    out = []
    for v in sorted(values):
        if not out or v - out[-1] > 1.2:
            out.append(v)
    return out


tracks = {'div': [], 'ctr': [], 'edge': []}
frames = {}
s = 0.0
while s <= cum[-1]:
    f = frame(s)
    if f:
        C, u, r = f
        frames[round(s)] = f
        if not any(math.dist(C, (jx, jz)) < c for jx, jz, c in JUNCTIONS):
            o = offsets(C, u, r)
            neg = dedupe([a for a, g in o if g < 0])
            pos = dedupe([a for a, g in o if g > 0])
            if neg and pos and 1.5 < min(pos) - max(neg) < 14:
                tracks['ctr'].append((s, (max(neg) + min(pos)) / 2))
            for side, outer in ((neg, -1), (pos, 1)):
                for a, b in zip(side, side[1:]):
                    if 2.4 < b - a < 4.4:
                        tracks['div'].append((s, (a + b) / 2))
                if side:
                    tracks['edge'].append((s, side[0] - 1.7 if outer < 0 else side[-1] + 1.7))
    s += 1.0


def world(s, o):
    C, u, r = frames[round(s)]
    return (C[0] + r[0] * o, C[1] + r[1] * o)


def chain(points, tol=.6):
    polys = []
    for s, o in sorted(points):
        best = None
        for pl in polys:
            ls, lo = pl[-1]
            if 0 < s - ls <= 1.01 and abs(o - lo) < tol and (best is None or abs(o - lo) < abs(o - best[-1][1])):
                best = pl
        if best is not None:
            best.append((s, o))
        else:
            polys.append([(s, o)])
    return [p for p in polys if len(p) >= 4]


def pieces(poly, on, off):
    """Dashes along a polyline: `on` metres painted then `off` skipped, one straight piece per dash."""
    pts = [world(s, o) for s, o in poly]
    acc, draw, start = 0.0, True, pts[0]
    for a, b in zip(pts, pts[1:]):
        L = math.dist(a, b)
        t = 0.0
        while t < L - 1e-6:
            target = on if draw else off
            step = min(target - acc, L - t)
            t += step
            acc += step
            here = (a[0] + (b[0] - a[0]) * t / L, a[1] + (b[1] - a[1]) * t / L)
            if acc >= target - 1e-6:
                if draw:
                    yield start, here
                acc, draw = 0.0, not draw
                start = here
    if draw and acc > .5:
        yield start, pts[-1]


EVIDENCE = {'state': 'game', 'url': 'https://map.kakao.com/', 'month': '2026-06', 'checkedAt': '2026-11-20',
            'note': '수인로 원본 노면선(2020 측량)이 현재 차로와 어긋남. 카카오 항공사진·로드뷰 대조 후 주행 차로 기준으로 다시 그림. 위치는 게임 추정치.',
            'appearanceConfirmed': False, 'ruleConfirmed': False}


def simplify(points, tol=.05):
    if len(points) < 3:
        return points
    a, b = points[0], points[-1]
    L = math.dist(a, b) or 1e-9
    far, idx = -1, 0
    for i, p in enumerate(points[1:-1], 1):
        d = abs((b[0] - a[0]) * (a[1] - p[1]) - (a[0] - p[0]) * (b[1] - a[1])) / L
        if d > far:
            far, idx = d, i
    if far <= tol:
        return [a, b]
    return simplify(points[:idx + 1], tol)[:-1] + simplify(points[idx:], tol)


def solid(poly, longest=12.0):
    """Solid line as straight pieces between simplified vertices, each at most `longest` metres."""
    pts = simplify([world(s, o) for s, o in poly])
    for a, b in zip(pts, pts[1:]):
        k = max(1, math.ceil(math.dist(a, b) / longest))
        for i in range(k):
            yield ((a[0] + (b[0] - a[0]) * i / k, a[1] + (b[1] - a[1]) * i / k),
                   (a[0] + (b[0] - a[0]) * (i + 1) / k, a[1] + (b[1] - a[1]) * (i + 1) / k))


def facility(fid, kind, typ, a, b, width):
    mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
    dx, dz = b[0] - a[0], b[1] - a[1]
    yaw = (math.degrees(math.atan2(dx, dz)) + 360) % 360
    return {'id': fid, 'kind': kind, 'type': typ, 'junctionId': '', 'laneId': '', 'groupId': '', 'targetId': '', 'text': '',
            'rule': False, 'x': mid[0], 'z': mid[1], 'height': 0.0, 'yaw': yaw, 'width': width,
            'length': math.hypot(dx, dz) + .01, 'scale': 1.0, 'startAt': 0.0, 'endAt': 0.0, 'speed': 30.0,
            'axis': 0, 'sign': 1, 'evidence': EVIDENCE}


def near_uturn(p):
    return any(math.dist(p, u) < 12 for u in uturn_starts)


out = []
n = 0
for i, poly in enumerate(chain(tracks['div'])):
    for a, b in pieces(poly, 3, 3):
        out.append(facility(f'r48:suinro:div:{i}:{n}', 'line', '검토 차선', a, b, .13)); n += 1
for i, poly in enumerate(chain(tracks['edge'])):
    for a, b in solid(poly):
        out.append(facility(f'r48:suinro:edge:{i}:{n}', 'line', '검토 차선', a, b, .13)); n += 1
for i, poly in enumerate(chain(tracks['ctr'])):
    for side in (-.17, .17):
        for a, b in solid([(s, o + side) for s, o in poly], 4.0):
            if near_uturn(((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)):
                continue
            out.append(facility(f'r48:suinro:centre:{i}:{n}', 'line', '검토 중앙선', a, b, .15)); n += 1

# Masks: rectangles along the corridor over every painted offset, cut to the stations
# that actually carry new paint (so preserved junction zones stay untouched).
allp = [(s, o) for v in tracks.values() for s, o in v]
s, m = 0.0, 0
while s < cum[-1]:
    seg = [(t, o) for t, o in allp if s <= t < s + 30]
    if seg:
        offs = [o for _, o in seg]
        t0, t1 = min(t for t, _ in seg), max(t for t, _ in seg)
        lo, hi = min(offs) - 3.5, max(offs) + 3.5
        if hi - lo > 40:  # project map validator limit for facility width
            mid = (lo + hi) / 2
            lo, hi = mid - 20, mid + 20
        C, u, r = frame((t0 + t1) / 2)
        mid = (lo + hi) / 2
        half = (t1 - t0) / 2 + .5
        c = (C[0] + r[0] * mid, C[1] + r[1] * mid)
        f = facility(f'r48:suinro:mask:{m}', 'lanePresentation', '검토 도색 교체 영역',
                     (c[0] - u[0] * half, c[1] - u[1] * half), (c[0] + u[0] * half, c[1] + u[1] * half), hi - lo)
        f['length'] = 2 * half + .04
        out.append(f)
        m += 1
    s += 30
json.dump({'revision': 48, 'facilities': out}, open(sys.argv[1], 'w', encoding='utf-8'), ensure_ascii=False)
print('facilities', len(out), 'masks', m)
