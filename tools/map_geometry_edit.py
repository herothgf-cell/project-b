"""Road cross-section edits for the 2026-11-25 map review (curbs, carriageway width, road shifts).

One edit is a smooth deformation of the plane around a stretch of road:
- a reference line (world points along the road, in driving order of the stretch) gives a station s and
  a signed lateral offset t (positive = left of the line's direction) for every point near it;
- `controls` map old lateral offsets to new ones, e.g. [(-8.5, -8.5), (8.5, 4.7)] keeps the right curb
  and pulls the left curb 3.8 m in; offsets between controls are interpolated linearly, and beyond the
  outermost controls the move fades to nothing over `fade` metres (so a curb vertex moves with its curb
  and the far edge of the sidewalk stays where it is: the sidewalk widens or narrows);
- the full move applies between stations s0 and s1 and tapers to nothing over `taper` metres beyond
  each end (smoothstep; `taper` may be a (start, end) pair).
The same deformation moves every lane point (lane widths scale with the local stretch), every scene road
strip (centre and width; a strip whose width would vary is split per segment), every sidewalk polygon
vertex (edges crossing the stretch are first divided every 2 m) and every painted mark or crosswalk
(centre, width and length from its transformed corners; only thin lines also turn with the lanes). Buildings never move; the map
validator in Unity rejects a road or curb pushed into one.

Usage (from a plan module):  E = Edit(...); patch(edits, from_rev, to_rev, out_json); check(edits, out_jpg)
Unity: -executeMethod MapReviewGeometryEdit.ApplyFromCommandLine -geometryPatch config/maps/review/<file>
"""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOC = json.loads((ROOT / 'config/maps/project/eungye.project.json').read_text(encoding='utf-8-sig'))['document']


def smooth(u):
    u = max(0.0, min(1.0, u)); return u * u * (3 - 2 * u)


class Edit:
    def __init__(self, ref, s0, s1, controls, taper=12.0, fade=1.5, reach=None, name='', min_lane=3.0):
        self.ref = [tuple(p) for p in ref]
        self.cum = [0.0]
        for a, b in zip(self.ref, self.ref[1:]):
            self.cum.append(self.cum[-1] + math.dist(a, b))
        self.s0, self.s1, self.fade, self.name, self.min_lane = s0, s1, fade, name, min_lane
        self.t0, self.t1 = taper if isinstance(taper, tuple) else (taper, taper)
        self.taper = max(self.t0, self.t1)
        self.controls = sorted(controls)
        lo, hi = self.controls[0][0], self.controls[-1][0]
        self.reach = reach if reach is not None else max(abs(lo), abs(hi)) + fade + 1

    def project(self, p):
        best = None
        for i, (a, b) in enumerate(zip(self.ref, self.ref[1:])):
            dx, dz = b[0] - a[0], b[1] - a[1]; L2 = dx * dx + dz * dz
            u = ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / L2
            first, last = i == 0, i == len(self.ref) - 2
            uc = u if (first and u < 0) or (last and u > 1) else max(0.0, min(1.0, u))
            q = (a[0] + uc * dx, a[1] + uc * dz); L = math.sqrt(L2)
            n = (-dz / L, dx / L)
            t = (p[0] - q[0]) * n[0] + (p[1] - q[1]) * n[1]
            d = math.dist(p, q) if not (first and u < 0 or last and u > 1) else abs(t)
            if best is None or d < best[0]:
                best = (d, self.cum[i] + uc * L, t, n)
        return best[1], best[2], best[3]

    def lateral(self, t):
        """new offset for old offset t (no station weighting)"""
        c = self.controls
        if t <= c[0][0]:
            k = smooth((c[0][0] - t) / self.fade) if self.fade else 1
            return t + (c[0][1] - c[0][0]) * (1 - k)
        if t >= c[-1][0]:
            k = smooth((t - c[-1][0]) / self.fade) if self.fade else 1
            return t + (c[-1][1] - c[-1][0]) * (1 - k)
        for (a, na), (b, nb) in zip(c, c[1:]):
            if a <= t <= b:
                return na + (t - a) * (nb - na) / (b - a)

    def weight(self, s):
        if s < self.s0:
            return smooth(1 - (self.s0 - s) / self.t0) if self.t0 else 0.0
        if s > self.s1:
            return smooth(1 - (s - self.s1) / self.t1) if self.t1 else 0.0
        return 1.0

    def __call__(self, p):
        s, t, n = self.project(p)
        if abs(t) > self.reach:
            return p
        w = self.weight(s)
        if w <= 0:
            return p
        dt = (self.lateral(t) - t) * w
        return (p[0] + n[0] * dt, p[1] + n[1] * dt)

    def scale(self, p):
        """local lateral stretch factor at p"""
        a, b = self.__call__(p), None
        s, t, n = self.project(p)
        q = (p[0] + n[0] * .25, p[1] + n[1] * .25); r = (p[0] - n[0] * .25, p[1] - n[1] * .25)
        return math.dist(self.__call__(q), self.__call__(r)) / .5

    def near(self, pts):
        x0 = min(p[0] for p in self.ref) - self.reach - self.taper; x1 = max(p[0] for p in self.ref) + self.reach + self.taper
        z0 = min(p[1] for p in self.ref) - self.reach - self.taper; z1 = max(p[1] for p in self.ref) + self.reach + self.taper
        return any(x0 <= p[0] <= x1 and z0 <= p[1] <= z1 for p in pts)


def compose(edits, p):
    for e in edits:
        p = e(p)
    return p


def moved(a, b):
    return math.dist(a, b) > 1e-3


def _pts(o):
    return [(q['x'], q['z']) for q in o['points']]


def _mp(pts):
    return [dict(x=round(x, 4), z=round(z, 4)) for x, z in pts]


def plan(edits):
    lanes, objects, added, facilities = [], [], [], []
    taken = {o['id'] for o in DOC['scene']['objects']}
    near = lambda pts: any(e.near(pts) for e in edits)
    for l in DOC['lanes']:
        pts = _pts(l)
        if len(pts) < 2 or not near(pts):
            continue
        new = [compose(edits, p) for p in pts]
        if not any(moved(a, b) for a, b in zip(pts, new)):
            continue
        # one width per lane: the narrowest stretch over the moved points (lanes must still fit there)
        k = 1.0; floor = 2.0
        for e in edits:
            hit = [q for q in pts if moved(q, e(q))]
            if hit:
                k *= min(e.scale(q) for q in hit); floor = max(floor, e.min_lane)
        w0 = l.get('width', 3.5)
        lanes.append(dict(id=l['id'], width=round(max(min(w0, floor), min(6.0, w0 * k)), 3), points=_mp(new)))
    for o in DOC['scene']['objects']:
        if not o.get('enabled', True) or o['kind'] not in ('road', 'sidewalk'):
            continue
        pts = _pts(o)
        if not near(pts):
            continue
        if o['kind'] == 'sidewalk':
            dense = []
            for i, a in enumerate(pts):
                b = pts[(i + 1) % len(pts)]; n = max(1, int(math.dist(a, b) / 2.0))
                for k in range(n):
                    dense.append((a[0] + (b[0] - a[0]) * k / n, a[1] + (b[1] - a[1]) * k / n))
            new = [compose(edits, p) for p in dense]
            if not any(moved(a, b) for a, b in zip(dense, new)):
                continue
            # drop the inserted points that stayed on a straight original edge
            keep = [q for q, p0 in zip(new, dense) if p0 in pts or any(moved(p0, q2) for q2 in [q])]
            objects.append(dict(id=o['id'], width=o.get('width', 4), points=_mp(keep)))
            continue
        w = o['width'] / 2; cs, ws = [], []
        if True:
            dense = [pts[0]]  # divide every 3 m so a taper gives a stepped, not a single wide, strip
            for a, b in zip(pts, pts[1:]):
                n = max(1, int(math.dist(a, b) / 3.0))
                dense += [(a[0] + (b[0] - a[0]) * k / n, a[1] + (b[1] - a[1]) * k / n) for k in range(1, n + 1)]
            pts = dense
        for i, p in enumerate(pts):
            a = pts[max(0, i - 1)]; b = pts[min(len(pts) - 1, i + 1)]
            dx, dz = b[0] - a[0], b[1] - a[1]; L = math.hypot(dx, dz) or 1; n = (-dz / L, dx / L)
            l_ = compose(edits, (p[0] + n[0] * w, p[1] + n[1] * w)); r_ = compose(edits, (p[0] - n[0] * w, p[1] - n[1] * w))
            cs.append(((l_[0] + r_[0]) / 2, (l_[1] + r_[1]) / 2)); ws.append(math.dist(l_, r_))
        if not any(moved(a, b) for a, b in zip(pts, cs)) and max(abs(x - o['width']) for x in ws) < 1e-3:
            continue
        if max(ws) - min(ws) <= .3:
            objects.append(dict(id=o['id'], width=round(max(ws), 3), points=_mp(cs)))
            continue
        # split into runs of segments whose widths stay within 0.3 m (first run keeps the id)
        runs, cur = [], [0]
        for i in range(1, len(cs)):
            seg = cur + [i]
            if max(ws[j] for j in seg) - min(ws[j] for j in seg) > .3 and len(cur) > 1:
                runs.append(cur); cur = [i - 1, i]
            else:
                cur = seg
        runs.append(cur)
        for k, run in enumerate(runs):
            item = dict(width=round(max(ws[j] for j in run), 3), points=_mp([cs[j] for j in run]))
            if k == 0:
                objects.append(dict(id=o['id'], **item))
            else:
                n = k
                while f"{o['id']}:geo{n}" in taken:  # pieces from an earlier revision keep their ids
                    n += 1
                taken.add(f"{o['id']}:geo{n}")
                added.append(dict(id=f"{o['id']}:geo{n}", copyOf=o['id'], **item))
    newlen = {l['id']: sum(math.dist((a['x'], a['z']), (b['x'], b['z'])) for a, b in zip(l['points'], l['points'][1:])) for l in lanes}
    oldlen = {l['id']: sum(math.dist(a, b) for a, b in zip(_pts(l), _pts(l)[1:])) for l in DOC['lanes'] if l['id'] in newlen}
    for f in DOC['facilities']:
        if f['kind'] == 'lanePresentation':  # lane-bound range: rescale to the new lane length
            if f.get('laneId') in newlen and f.get('endAt', 0) > 0:
                k = newlen[f['laneId']] / oldlen[f['laneId']]
                facilities.append(dict(id=f['id'], range=True, startAt=round(f['startAt'] * k, 4), endAt=round(min(f['endAt'] * k, newlen[f['laneId']]), 4)))
            continue
        if f['kind'] not in ('line', 'crosswalk', 'stopLine'):
            continue
        c = (f['x'], f['z'])
        if not near([c]):
            continue
        y = math.radians(f['yaw']); fw = (math.sin(y), math.cos(y)); rt = (fw[1], -fw[0])
        hw, hl = f['width'] / 2, f['length'] / 2
        P = lambda u, v: compose(edits, (c[0] + rt[0] * u + fw[0] * v, c[1] + rt[1] * u + fw[1] * v))
        nc = compose(edits, c)
        if not moved(c, nc) and not moved(P(hw, 0), (c[0] + rt[0] * hw, c[1] + rt[1] * hw)):
            continue
        fa, fb, ra, rb = P(0, -hl), P(0, hl), P(-hw, 0), P(hw, 0)
        # thin painted lines follow the lanes; area marks (crosswalks, paving, text) keep their heading
        yaw = math.degrees(math.atan2(fb[0] - fa[0], fb[1] - fa[1])) % 360 if hl > 1e-3 and f['width'] < .5 else f['yaw']
        facilities.append(dict(id=f['id'], x=round(nc[0], 4), z=round(nc[1], 4), yaw=round(yaw, 3),
                               width=round(math.dist(ra, rb), 4), length=round(math.dist(fa, fb), 4)))
    return dict(lanes=lanes, objects=objects, addObjects=added, facilities=facilities)


def patch(edits, from_rev, to_rev, out):
    p = plan(edits)
    Path(out).write_text(json.dumps(dict(fromRevision=from_rev, toRevision=to_rev, **p), ensure_ascii=False, indent=1), encoding='utf-8')
    return p


def check(edits, p, out, centre, size=100.0, title=''):
    """before (thin) / after (bold) over the Kakao aerial: road strip edges orange, curbs blue, lanes magenta"""
    import sys
    sys.path.insert(0, str(ROOT / 'tools'))
    from kakao_aerial_compare import aerial
    from PIL import ImageDraw, ImageFont
    F = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 14)
    cx, cz = centre; S, PX = size, 1000
    im, _ = aerial(cx, cz, S, S, (PX, PX)); d = ImageDraw.Draw(im, 'RGBA')
    Q = lambda x, z: ((x - cx) / S * PX + PX / 2, PX / 2 - (z - cz) / S * PX)
    inside = lambda pts: any(abs(x - cx) < S / 2 + 10 and abs(z - cz) < S / 2 + 10 for x, z in pts)
    step = 10 if S <= 150 else 20
    for k in range(int(S / step) + 2):
        x = math.floor((cx - S / 2) / step) * step + k * step; u = Q(x, cz)[0]
        d.line([(u, 0), (u, PX)], fill=(0, 255, 255, 70)); d.text((u + 2, 2), str(x), font=F, fill=(0, 255, 255))
        z = math.floor((cz - S / 2) / step) * step + k * step; v = Q(cx, z)[1]
        d.line([(0, v), (PX, v)], fill=(0, 255, 255, 70)); d.text((2, v + 2), str(z), font=F, fill=(0, 255, 255))
    lanes = {l['id']: l for l in p['lanes']}; objs = {o['id']: o for o in p['objects']}

    def edges(pts, w):
        out = []
        for s in (-1, 1):
            e = []
            for i, q in enumerate(pts):
                a = pts[max(0, i - 1)]; b = pts[min(len(pts) - 1, i + 1)]
                dx, dz = b[0] - a[0], b[1] - a[1]; L = math.hypot(dx, dz) or 1
                e.append(Q(q[0] - s * dz / L * w / 2, q[1] + s * dx / L * w / 2))
            out.append(e)
        return out
    for o in DOC['scene']['objects']:
        if not o.get('enabled', True) or o['kind'] not in ('road', 'sidewalk') or not inside(_pts(o)):
            continue
        new = objs.get(o['id'])
        if o['kind'] == 'sidewalk':
            d.polygon([Q(*q) for q in _pts(o)], outline=(120, 170, 255, 110))
            if new:
                d.polygon([Q(q['x'], q['z']) for q in new['points']], outline=(60, 120, 255, 255), fill=(60, 120, 255, 50))
        else:
            for e in edges(_pts(o), o['width']):
                d.line(e, fill=(255, 160, 0, 110), width=1)
    for o in list(p['objects']) + list(p['addObjects']):
        if 'copyOf' in o or next((x for x in DOC['scene']['objects'] if x['id'] == o['id'] and x['kind'] == 'road'), None):
            for e in edges([(q['x'], q['z']) for q in o['points']], o['width']):
                d.line(e, fill=(255, 140, 0, 255), width=3)
    for l in DOC['lanes']:
        if l.get('disabled') or not inside(_pts(l)):
            continue
        d.line([Q(*q) for q in _pts(l)], fill=(255, 0, 255, 90), width=1)
        if l['id'] in lanes:
            d.line([Q(q['x'], q['z']) for q in lanes[l['id']]['points']], fill=(255, 0, 255, 255), width=2)
    for e in edits:
        d.line([Q(*q) for q in e.ref], fill=(0, 255, 0, 200), width=1)
    d.text((10, 980), title, font=F, fill='white')
    im.save(out, quality=85)
