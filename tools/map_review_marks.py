"""Plan paint-only road marks for the 2026-11-25 map review (markings batch 2, map r56).

Positions come from the review sheet (pixel boxes in the 1600x900 overhead capture of each card, or
grid cells) and are snapped to the document lanes: a crossing takes the direction of the nearest lane
and spans every aligned lane it cuts, school-zone paving follows the lanes inside a box, text and
diamonds sit on the nearest lane. Every plan is drawn over the Kakao aerial and the game capture
(Git-ignored check images) before it is applied.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_marks.py <out.json> [--check <dir>]
"""
import json
import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
W_M, H_M = 2 * 75 * math.tan(math.radians(30)) * 16 / 9, 2 * 75 * math.tan(math.radians(30))
DOC = json.loads((ROOT / 'config/maps/project/eungye.project.json').read_text(encoding='utf-8-sig'))['document']
ITEMS = {i['id']: i for i in json.loads((ROOT / 'artifacts/map-review-20261124/sheet/items.json').read_text(encoding='utf-8'))}
LANES = [l for l in DOC['lanes'] if not l.get('disabled') and l.get('change', 0) == 0 and len(l['points']) > 1]
SEGS = [(l, (a['x'], a['z']), (b['x'], b['z'])) for l in LANES for a, b in zip(l['points'], l['points'][1:]) if math.dist((a['x'], a['z']), (b['x'], b['z'])) > .05]
YELLOW, WHITE = '황색', '안내'
PAVING, TEXT, AHEAD, REMOVED = '보호구역 적색 포장', '노면 글자', '횡단보도 예고', '없음'


def px(item, u, v):
    it = ITEMS[item]
    return it['x'] + (u - 800) / 1600 * W_M, it['z'] - (v - 450) / 900 * H_M


def cells(item, names):
    it = ITEMS[item]; xs, ys = [], []
    for c in names:
        i, j = 'ABCDEFGH'.index(c[0]), int(c[1]) - 1
        xs += [i / 8, (i + 1) / 8]; ys += [j / 5, (j + 1) / 5]
    return (it['x'] + (min(xs) - .5) * W_M, it['z'] - (max(ys) - .5) * H_M, it['x'] + (max(xs) - .5) * W_M, it['z'] - (min(ys) - .5) * H_M)


def unit(a, b):
    d = math.dist(a, b); return ((b[0] - a[0]) / d, (b[1] - a[1]) / d)


def nearest(p, heading=None):
    best = None
    for l, a, b in SEGS:
        d = unit(a, b)
        if heading and d[0] * heading[0] + d[1] * heading[1] < .7:
            continue
        t = max(0, min(1, ((p[0] - a[0]) * (b[0] - a[0]) + (p[1] - a[1]) * (b[1] - a[1])) / math.dist(a, b) ** 2))
        q = (a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1])); dist = math.dist(p, q)
        if best is None or dist < best[0]:
            best = (dist, l, q, d)
    return best


def yaw_of(d):
    return math.degrees(math.atan2(d[0], d[1])) % 360


def crossing(p, depth=4.0, span=None, direction=None, pad=.8):
    """A crosswalk through p across the road: direction of the nearest lane, spanning the aligned lanes it cuts."""
    _, lane, q, d = nearest(p)
    if direction:
        d = direction
    r = (d[1], -d[0]); hits = []
    for l, a, b in SEGS:
        e = unit(a, b)
        if abs(e[0] * d[0] + e[1] * d[1]) < .8:
            continue
        ua = (a[0] - p[0]) * d[0] + (a[1] - p[1]) * d[1]; ub = (b[0] - p[0]) * d[0] + (b[1] - p[1]) * d[1]
        if ua * ub > 0 or ua == ub:
            continue
        s = ua / (ua - ub); x = (a[0] + s * (b[0] - a[0]), a[1] + s * (b[1] - a[1]))
        t = (x[0] - p[0]) * r[0] + (x[1] - p[1]) * r[1]
        if abs(t) < 30:
            hits.append((t, l.get('width', 3.5)))
    hits.sort()
    # keep the contiguous group of lanes around p (no gap wider than 8 m: some two-way roads have a wide middle)
    group = [h for h in hits if abs(h[0]) <= min(abs(x[0]) for x in hits) + 1e-6] if hits else []
    if group:
        lo = hi = group[0][0]
        grown = True
        while grown:
            grown = False
            for t, w in hits:
                if lo - w - 8 <= t <= hi + w + 8 and not lo <= t <= hi:  # edge-to-edge gap under 8 m
                    lo, hi = min(lo, t), max(hi, t); grown = True
        wl = max(w for t, w in hits if lo <= t <= hi)
        a, b = lo - wl / 2 - pad, hi + wl / 2 + pad
    else:
        a, b = -4, 4
    if span:
        mid = (a + b) / 2; a, b = mid - span / 2, mid + span / 2
    c = (p[0] + r[0] * (a + b) / 2, p[1] + r[1] * (a + b) / 2)
    return dict(x=c[0], z=c[1], yaw=yaw_of(d), width=b - a, length=depth)


def paving(box, heading=None, step=6.0):
    """Red paving along every lane inside box (x0, z0, x1, z1); heading keeps lanes moving that way."""
    out = []
    x0, z0, x1, z1 = box
    for l in LANES:
        pts = [(p['x'], p['z']) for p in l['points']]
        for a, b in zip(pts, pts[1:]):
            L = math.dist(a, b)
            if L < .5:
                continue
            d = unit(a, b)
            if heading and d[0] * heading[0] + d[1] * heading[1] < .7:
                continue
            n = max(1, math.ceil(L / step))
            for k in range(n):
                s0, s1 = k / n, (k + 1) / n
                m = (a[0] + (s0 + s1) / 2 * (b[0] - a[0]), a[1] + (s0 + s1) / 2 * (b[1] - a[1]))
                if x0 <= m[0] <= x1 and z0 <= m[1] <= z1:
                    out.append(dict(x=m[0], z=m[1], yaw=yaw_of(d), width=l.get('width', 3.5) - .3, length=L / n + .05, lane=l['id']))
    return out


def on_lane(p, heading=None, width=None, length=None):
    _, l, q, d = nearest(p, heading)
    return dict(x=q[0], z=q[1], yaw=yaw_of(d), width=width or l.get('width', 3.5) * .8, length=length or 4.0)


COMPASS = dict(N=(0, 1), S=(0, -1), E=(1, 0), W=(-1, 0))


def arm_lanes(centre, bearing, d0, d1, half=16.0, inbound=None):
    """Lane pieces on the arm leaving `centre` toward compass `bearing` (deg) between d0 and d1 m.
    inbound True keeps lanes moving toward the junction, False away from it, None both."""
    b = math.radians(bearing); ax = (math.sin(b), math.cos(b)); rx = (ax[1], -ax[0])
    def inside(m):
        dx, dz = m[0] - centre[0], m[1] - centre[1]
        along = dx * ax[0] + dz * ax[1]; side = dx * rx[0] + dz * rx[1]
        return d0 <= along <= d1 and abs(side) <= half
    def keep(d):
        if inbound is None:
            return abs(d[0] * ax[0] + d[1] * ax[1]) > .7
        s = -(d[0] * ax[0] + d[1] * ax[1])
        return s > .7 if inbound else s < -.7
    return inside, keep


def paving_arm(centre, bearing, d0, d1, inbound=None, half=16.0, step=6.0):
    """Red paving pieces (about `step` m long) along the lanes on one arm between d0 and d1 m."""
    inside, keep = arm_lanes(centre, bearing, d0, d1, half, inbound)
    out = []
    for l in LANES:
        pts = [(p['x'], p['z']) for p in l['points']]
        run = []  # consecutive points of this lane inside the arm box, moving the kept way

        def flush():
            if len(run) < 2:
                return
            total = sum(math.dist(a, b) for a, b in zip(run, run[1:]))
            n = max(1, round(total / step)); piece = total / n
            at, acc, k, last = 0, 0.0, 0, run[0]
            marks = [run[0]]; target = piece
            for a, b in zip(run, run[1:]):
                L = math.dist(a, b)
                while acc + L >= target - 1e-6 and len(marks) < n:
                    t = (target - acc) / L; marks.append((a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1]))); target += piece
                acc += L
            marks.append(run[-1])
            for a, b in zip(marks, marks[1:]):
                if math.dist(a, b) < .5:
                    continue
                m = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
                out.append(dict(x=m[0], z=m[1], yaw=yaw_of(unit(a, b)), width=l.get('width', 3.5) - .3, length=math.dist(a, b) + .1, lane=l['id']))

        for a, b in zip(pts, pts[1:]):
            ok = math.dist(a, b) > .05 and keep(unit(a, b)) and inside(((a[0] + b[0]) / 2, (a[1] + b[1]) / 2))
            if ok:
                if not run:
                    run.append(a)
                run.append(b)
            else:
                flush(); run = []
        flush()
    return out


def lane_points_on_arm(centre, bearing, dist, inbound=True, half=16.0):
    """One point per lane (moving toward/away from the junction) at `dist` m along the arm."""
    b = math.radians(bearing); ax = (math.sin(b), math.cos(b)); rx = (ax[1], -ax[0])
    p = (centre[0] + ax[0] * dist, centre[1] + ax[1] * dist); out = []
    for l in LANES:
        pts = [(q['x'], q['z']) for q in l['points']]
        for a, c in zip(pts, pts[1:]):
            if math.dist(a, c) < .5:
                continue
            d = unit(a, c); s = -(d[0] * ax[0] + d[1] * ax[1])
            if (s < .7) if inbound else (s > -.7):
                continue
            ua = (a[0] - p[0]) * ax[0] + (a[1] - p[1]) * ax[1]; uc = (c[0] - p[0]) * ax[0] + (c[1] - p[1]) * ax[1]
            if ua * uc > 0 or ua == uc:
                continue
            t = ua / (ua - uc); x = (a[0] + t * (c[0] - a[0]), a[1] + t * (c[1] - a[1]))
            if abs((x[0] - p[0]) * rx[0] + (x[1] - p[1]) * rx[1]) <= half:
                out.append(dict(x=x[0], z=x[1], yaw=yaw_of(d), lanewidth=l.get('width', 3.5), lane=l['id']))
    return out


def arms(centre, r0=12.0, r1=35.0):
    """Compass bearings (deg) of the road arms around a junction, from lane segments in a ring."""
    hist = {}
    for l, a, b in SEGS:
        m = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2); r = math.dist(m, centre)
        if not r0 <= r <= r1:
            continue
        d = unit(a, b); radial = ((m[0] - centre[0]) / r, (m[1] - centre[1]) / r)
        if abs(d[0] * radial[0] + d[1] * radial[1]) < .8:
            continue
        br = math.degrees(math.atan2(radial[0], radial[1])) % 360
        k = int(br // 10); hist[k] = hist.get(k, 0) + math.dist(a, b)
    peaks = []
    for k in sorted(hist, key=lambda k: -hist[k]):
        if hist[k] < 8:
            break
        if all(min(abs(k - p), 36 - abs(k - p)) > 3 for p in peaks):
            peaks.append(k)
    out = []
    for p in peaks:
        bins = [((p + i) % 36, hist.get((p + i) % 36, 0)) for i in (-1, 0, 1)]
        sx = sum(w * math.sin(math.radians(b * 10 + 5)) for b, w in bins); sz = sum(w * math.cos(math.radians(b * 10 + 5)) for b, w in bins)
        out.append(round(math.degrees(math.atan2(sx, sz)) % 360, 1))
    return sorted(out)


def arm_crossing(centre, bearing, all_arms, depth=4.0, offset=3.0, dist=None):
    """A crosswalk across the arm at the generated distance: half the widest other road plus offset."""
    b = math.radians(bearing); ax = (math.sin(b), math.cos(b))
    if dist is None:
        widths = []
        for o in all_arms:
            if min(abs(o - bearing), 360 - abs(o - bearing)) < 45:
                continue
            ob = math.radians(o); p = (centre[0] + math.sin(ob) * 15, centre[1] + math.cos(ob) * 15)
            widths.append(crossing(p, direction=(math.sin(ob), math.cos(ob)))['width'])
        dist = min(22.0, max(8.0, (max(widths) / 2 if widths else 6.0) + offset + depth / 2))
    best = None
    for extra in (0, 2, 4, 6, 8):  # approach lanes can end short of the junction: measure a little further out
        q = (centre[0] + ax[0] * (dist + extra), centre[1] + ax[1] * (dist + extra))
        g = crossing(q, depth=depth, direction=ax)
        if best is None or g['width'] > best['width'] + .5:
            best = g
    along = (best['x'] - centre[0]) * ax[0] + (best['z'] - centre[1]) * ax[1]
    best['x'] -= ax[0] * (along - dist); best['z'] -= ax[1] * (along - dist)
    best['dist'] = round(dist, 1)
    return best
