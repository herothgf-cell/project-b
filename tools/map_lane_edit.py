"""Lane-count edits for the 2026-11-25 map review (add or retire lanes, re-pair exits, move lane ends).

Builds on tools/map_geometry_edit.py: a LanePatch starts from the cross-section deformation of a list of
Edit objects (lanes, road strips, curbs and paint move together), then applies lane operations on the
deformed geometry:
- add(id, copy_of, points, ...): a new lane that copies the source lane's attributes (source family,
  junction rule, road name, signal groups); `prev` lanes get it as an exit, `next` are its exits;
- offset(lane, d): the lane's (deformed) centre line shifted d metres to the right of its direction;
- blend_start / blend_end: move one end of a lane to a point, the displacement fading out along the lane
  over `length` metres (re-pairs a lane with a moved upstream or downstream lane);
- connector(from, to): a junction connector from the end of one lane to the start of another (cubic, as
  RoadMapAuthoringOperations.Connect draws it);
- exits(id, [...]): replaces a lane's exits; disable([...]): retires lanes (with `closure`, also every
  connector that starts or ends on them) -- the Unity side refuses a patch that leaves a lane without exits;
- copy_mark(src, new_id, d) / remove_mark: painted facilities (paving, dividers) copied sideways (an added
  item may also set `type`, e.g. a crosswalk of type "없음" that hides source paint).
A lane's `road:polish:<id>` strip follows the lane (moved lanes) or is copied (new lanes).
Output is the same patch file as map_geometry_edit (MapReviewGeometryEdit.ApplyFromCommandLine).
"""
import json
import math
from pathlib import Path

import map_geometry_edit as G

DOC = G.DOC
LANES = {l['id']: l for l in DOC['lanes']}
OBJECTS = {o['id']: o for o in DOC['scene']['objects']}
FACILITIES = {f['id']: f for f in DOC['facilities']}


def length(pts):
    return sum(math.dist(a, b) for a, b in zip(pts, pts[1:]))


def at(pts, s):
    """point and unit direction at distance s along a polyline"""
    s = max(0.0, min(length(pts), s))
    for a, b in zip(pts, pts[1:]):
        L = math.dist(a, b)
        if s <= L or b == pts[-1]:
            u = 0 if L == 0 else s / L
            return (a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u), ((b[0] - a[0]) / (L or 1), (b[1] - a[1]) / (L or 1))
        s -= L


def resample(pts, step=1.0):
    L = length(pts); n = max(1, round(L / step))
    return [at(pts, L * k / n)[0] for k in range(n + 1)]


def right(d):
    return (d[1], -d[0])  # TrafficSystem.Right for a heading (x, z)


class LanePatch:
    def __init__(self, edits, from_rev, to_rev):
        self.edits, self.from_rev, self.to_rev = edits, from_rev, to_rev
        p = G.plan(edits)
        self.lanes = {l['id']: l for l in p['lanes']}
        self.objects = {o['id']: o for o in p['objects']}
        self.addObjects = list(p['addObjects'])
        self.facilities = {f['id']: f for f in p['facilities']}
        self.added, self.nexts, self.disabled, self.addFacilities, self.removeFacilities = [], {}, [], [], []
        self.new = {}

    # current (deformed / edited / added) geometry
    def pts(self, lane_id):
        if lane_id in self.new:
            return [(q['x'], q['z']) for q in self.new[lane_id]['points']]
        l = self.lanes.get(lane_id) or LANES[lane_id]
        return [(q['x'], q['z']) for q in l['points']]

    def width(self, lane_id):
        if lane_id in self.new:
            return self.new[lane_id]['width']
        return (self.lanes.get(lane_id) or LANES[lane_id])['width']

    def set_points(self, lane_id, pts, width=None):
        w = width if width is not None else self.width(lane_id)
        if lane_id in self.new:
            self.new[lane_id].update(points=G._mp(pts), width=w)
        else:
            self.lanes[lane_id] = dict(id=lane_id, width=round(w, 3), points=G._mp(pts))
        polish = 'road:polish:' + lane_id
        if polish in OBJECTS:
            self.objects[polish] = dict(id=polish, width=OBJECTS[polish]['width'], points=G._mp(pts))

    def offset(self, lane_id, d, s0=None, s1=None):
        pts = resample(self.pts(lane_id), 2.0); L = length(pts); out = []
        for k in range(len(pts)):
            s = L * k / (len(pts) - 1)
            if (s0 is not None and s < s0 - 1e-6) or (s1 is not None and s > s1 + 1e-6):
                continue
            p, f = at(pts, s); r = right(f); out.append((p[0] + r[0] * d, p[1] + r[1] * d))
        return out

    def blend(self, lane_id, target, length_, end=False):
        """move the start (or end) of a lane to target; the move fades out over length_ metres"""
        pts = self.pts(lane_id)
        if end:
            pts = pts[::-1]
        pts = resample(pts, 1.0) if length(pts) / max(1, len(pts) - 1) > 1.05 else pts
        delta = (target[0] - pts[0][0], target[1] - pts[0][1]); s = 0.0; out = [target]
        for a, b in zip(pts, pts[1:]):
            s += math.dist(a, b); w = 1 - G.smooth(s / length_)
            out.append((b[0] + delta[0] * w, b[1] + delta[1] * w))
        self.set_points(lane_id, out[::-1] if end else out)

    def add(self, lane_id, copy_of, pts, width, number, prev=(), next=(), turn=0, change=0, polish=True):
        assert lane_id not in LANES and lane_id not in self.new, lane_id
        item = dict(id=lane_id, copyOf=copy_of, width=round(width, 3), number=number, turn=turn, change=change,
                    points=G._mp(pts), next=list(next), prev=list(prev))
        self.new[lane_id] = item; self.added.append(item)
        src = 'road:polish:' + copy_of
        if polish and src in OBJECTS:
            self.addObjects.append(dict(id='road:polish:' + lane_id, copyOf=src, width=OBJECTS[src]['width'], points=G._mp(pts)))
        return item

    def connector(self, lane_id, from_id, to_id, copy_of, turn=0, number=None, width=None, prev=True):
        a_pts, b_pts = self.pts(from_id), self.pts(to_id)
        a, b = a_pts[-1], b_pts[0]
        fa = at(a_pts, length(a_pts) - .01)[1]; fb = at(b_pts, .01)[1]
        h = math.dist(a, b) * .4; pts = []
        for i in range(33):
            t = i / 32; u = 1 - t
            c1 = (a[0] + fa[0] * h, a[1] + fa[1] * h); c2 = (b[0] - fb[0] * h, b[1] - fb[1] * h)
            pts.append(tuple(u ** 3 * a[k] + 3 * u * u * t * c1[k] + 3 * u * t * t * c2[k] + t ** 3 * b[k] for k in (0, 1)))
        src = LANES[copy_of]
        return self.add(lane_id, copy_of, pts, width or src['width'], number if number is not None else src['number'],
                        prev=[from_id] if prev else [], next=[to_id], turn=turn, polish=False)

    def exits(self, lane_id, nxt):
        if lane_id in self.new:
            self.new[lane_id]['next'] = list(nxt)
        else:
            self.nexts[lane_id] = list(nxt)

    def disable(self, ids, closure=True):
        off = set(ids)
        while closure:
            closure = False
            for l in DOC['lanes']:
                if l['id'] in off or l.get('disabled') or not (l['id'].startswith('join:') or l['id'].startswith('change:')):
                    continue
                if any(n in off for n in l['next']) or any(l['id'] in LANES[o]['next'] for o in off):
                    off.add(l['id']); closure = True
        self.disabled += sorted(off - set(self.disabled))
        return off

    def copy_mark(self, src, new_id, d=0.0, along=0.0, lane_id=None):
        f = self.facilities.get(src)
        base = dict(FACILITIES[src], **(f or {}))
        y = math.radians(base['yaw']); fw = (math.sin(y), math.cos(y)); r = right(fw)
        item = dict(id=new_id, copyOf=src, x=round(base['x'] + r[0] * d + fw[0] * along, 4), z=round(base['z'] + r[1] * d + fw[1] * along, 4),
                    yaw=base['yaw'], width=base['width'], length=base['length'])
        if lane_id:
            item['laneId'] = lane_id
        self.addFacilities.append(item)
        return item

    def remove_mark(self, ids):
        self.removeFacilities += [i for i in ids if i not in self.removeFacilities]

    def result(self):
        return dict(fromRevision=self.from_rev, toRevision=self.to_rev, lanes=list(self.lanes.values()), objects=list(self.objects.values()),
                    addObjects=self.addObjects, facilities=list(self.facilities.values()), disableLanes=self.disabled, addLanes=self.added,
                    nexts=[dict(id=k, next=v) for k, v in self.nexts.items()], addFacilities=self.addFacilities, removeFacilities=self.removeFacilities)

    def write(self, out):
        r = self.result()
        Path(out).write_text(json.dumps(r, ensure_ascii=False, indent=1), encoding='utf-8')
        return r

    def check(self, out, centre, size=60.0, title=''):
        """before (thin) / after (bold) lanes, new lanes cyan, retired lanes red, moved/added marks yellow"""
        r = self.result()
        G.check(self.edits, r, out, centre, size, title)
        from PIL import Image, ImageDraw
        im = Image.open(out); d = ImageDraw.Draw(im, 'RGBA'); cx, cz = centre; S, PX = size, 1000
        Q = lambda x, z: ((x - cx) / S * PX + PX / 2, PX / 2 - (z - cz) / S * PX)
        for a in self.added:
            pts = [Q(q['x'], q['z']) for q in a['points']]
            d.line(pts, fill=(0, 255, 255, 255), width=3)
            hw = a['width'] / 2; P = [(q['x'], q['z']) for q in a['points']]
            for s in (-1, 1):
                e = []
                for i, q in enumerate(P):
                    f = at(P, length(P[:i + 1]))[1]; rr = right(f); e.append(Q(q[0] + rr[0] * hw * s, q[1] + rr[1] * hw * s))
                d.line(e, fill=(0, 255, 255, 110), width=1)
        for i in self.disabled:
            d.line([Q(q['x'], q['z']) for q in LANES[i]['points']], fill=(255, 40, 40, 220), width=2)
        for f in list(self.facilities.values()) + self.addFacilities:
            if 'x' not in f:
                continue
            y = math.radians(f['yaw']); fw = (math.sin(y), math.cos(y)); rt = right(fw); hw, hl = f['width'] / 2, f['length'] / 2
            c = [(f['x'] + rt[0] * u + fw[0] * v, f['z'] + rt[1] * u + fw[1] * v) for u, v in ((-hw, -hl), (hw, -hl), (hw, hl), (-hw, hl))]
            d.polygon([Q(*p) for p in c], outline=(255, 230, 0, 230))
        im.save(out, quality=85)
