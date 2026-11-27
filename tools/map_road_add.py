"""New-road edits for the 2026-11-25 map review (a road the game lacks, joined to the existing network).

Builds on tools/map_lane_edit.py (LanePatch). A RoadPatch adds:
- strip(id, centre, width, copy_of): a scene road strip along a centre line (copies a style object such as a
  provisional road strip; width and points are set);
- lane(id, points, copy_of, ...): a lane of the new road. Its source is the new strip's id, so the synthetic
  paint groups the road's two directions and draws the centre line between them;
- offset_line(centre, d): the centre line shifted d metres to the right of its direction (right-hand traffic:
  a two-way road has its lanes at +w/2 for the direction of the centre line, -w/2 for the other);
- curve(a, da, b, db): a cubic between two points with headings (lane ends that bend into existing lanes);
- clip_sidewalks(band polygon): sidewalk polygons crossed by the new road are cut back with Sutherland-Hodgman
  half-plane clipping (a crossing band splits a polygon into the parts on either side).
Connections reuse LanePatch.connector / exits / blend; the Unity side (MapReviewGeometryEdit) adds the lanes and
objects and the map validator checks lanes against sidewalks, buildings and the map bounds.
"""
import math

import map_geometry_edit as G
import map_lane_edit as E


def offset_line(pts, d):
    out = []
    for i, p in enumerate(pts):
        a = pts[max(0, i - 1)]; b = pts[min(len(pts) - 1, i + 1)]
        L = math.dist(a, b) or 1; f = ((b[0] - a[0]) / L, (b[1] - a[1]) / L); r = E.right(f)
        out.append((p[0] + r[0] * d, p[1] + r[1] * d))
    return out


def curve(a, da, b, db, steps=16, handle=None):
    h = handle if handle is not None else math.dist(a, b) * .4
    c1 = (a[0] + da[0] * h, a[1] + da[1] * h); c2 = (b[0] - db[0] * h, b[1] - db[1] * h)
    out = []
    for i in range(steps + 1):
        t = i / steps; u = 1 - t
        out.append(tuple(u ** 3 * a[k] + 3 * u * u * t * c1[k] + 3 * u * t * t * c2[k] + t ** 3 * b[k] for k in (0, 1)))
    return out


def unit(a, b):
    L = math.dist(a, b) or 1
    return ((b[0] - a[0]) / L, (b[1] - a[1]) / L)


def clip(poly, f):
    """part of poly where the linear function f(p) >= 0 (Sutherland-Hodgman, one half-plane)"""
    out = []
    for i, p in enumerate(poly):
        q = poly[(i + 1) % len(poly)]; sp, sq = f(p), f(q)
        if sp >= 0:
            out.append(p)
        if (sp >= 0) != (sq >= 0):
            t = sp / (sp - sq); out.append((p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t))
    return out


def area(poly):
    return abs(sum(p[0] * q[1] - q[0] * p[1] for p, q in zip(poly, poly[1:] + poly[:1]))) / 2


class RoadPatch(E.LanePatch):
    def strip(self, obj_id, centre, width, copy_of):
        self.addObjects.append(dict(id=obj_id, copyOf=copy_of, width=round(width, 3), points=G._mp(centre)))
        return obj_id

    def lane(self, lane_id, pts, copy_of, source, width=3.0, number=1, prev=(), next=(), turn=0, road_name=''):
        item = self.add(lane_id, copy_of, pts, width, number, prev=prev, next=next, turn=turn, polish=False)
        item.update(sourceId=source, roadName=road_name)
        return item

    def clip_sidewalks(self, a, b, half, tag):
        """cut every sidewalk crossed by the band of half-width `half` around the segment a->b; the pieces on
        either side replace the polygon (the original is shrunk to the first piece, the second is added)"""
        d = unit(a, b); n = (-d[1], d[0]); L = math.dist(a, b)
        across = lambda p: (p[0] - a[0]) * n[0] + (p[1] - a[1]) * n[1]
        along = lambda p: (p[0] - a[0]) * d[0] + (p[1] - a[1]) * d[1]
        cut = []
        for o in E.DOC['scene']['objects']:
            if o['kind'] != 'sidewalk' or not o.get('enabled', True) or o['id'] in self.disableObjects:
                continue
            poly = [(q['x'], q['z']) for q in (self.objects.get(o['id']) or o)['points']]
            inside = poly
            for f in (lambda p: half - across(p), lambda p: half + across(p), along, lambda p: L - along(p)):
                inside = clip(inside, f) if len(inside) >= 3 else inside
            if area(inside) < .05:
                continue
            pieces = [p for p in (clip(poly, lambda p: across(p) - half), clip(poly, lambda p: -across(p) - half)) if len(p) >= 3 and area(p) > .2]
            cut.append(o['id'])
            if not pieces:  # wholly inside the new road: switched off
                self.disableObjects.append(o['id']); self.objects.pop(o['id'], None)
                continue
            self.objects[o['id']] = dict(id=o['id'], width=o.get('width', 4), points=G._mp(pieces[0]))
            for k, p in enumerate(pieces[1:]):
                self.addObjects.append(dict(id=f"{o['id']}:{tag}:{k}", copyOf=o['id'], width=o.get('width', 4), points=G._mp(p)))
        return cut


# ---- junctions (2026-11-27): a new road crossing or meeting existing lanes ----
import base64  # noqa: E402

SEGMENT = 'editor:segment:'


def origin(lane_id):
    """RoadMapAuthoringOperations.Origin: the source lane an editor segment was cut from"""
    if not lane_id.startswith(SEGMENT):
        return lane_id
    token = lane_id[len(SEGMENT):].split(':', 1)[0].replace('-', '+').replace('_', '/')
    try:
        return base64.b64decode(token + '=' * (-len(token) % 4)).decode('utf-8')
    except Exception:
        return lane_id


def segment_id(lane_id, suffix):
    token = base64.b64encode(origin(lane_id).encode('utf-8')).decode().rstrip('=').replace('+', '-').replace('/', '_')
    return f'{SEGMENT}{token}:{suffix}'


def station(pts, p):
    """distance along a polyline to the point nearest p"""
    best, run = None, 0.0
    for a, b in zip(pts, pts[1:]):
        dx, dz = b[0] - a[0], b[1] - a[1]; L2 = dx * dx + dz * dz; L = math.sqrt(L2)
        t = 0.0 if L2 == 0 else max(0.0, min(1.0, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / L2))
        d = math.dist(p, (a[0] + dx * t, a[1] + dz * t))
        if best is None or d < best[0]:
            best = (d, run + L * t)
        run += L
    return best[1]


def cut(pts, s0, s1):
    """the part of a polyline between distances s0 and s1"""
    out, run = [], 0.0
    L = E.length(pts)
    s0, s1 = max(0.0, s0), min(L, s1)
    out.append(E.at(pts, s0)[0])
    for a, b in zip(pts, pts[1:]):
        run += math.dist(a, b)
        if s0 + .05 < run < s1 - .05:
            out.append(b)
    out.append(E.at(pts, s1)[0])
    return out


class JunctionPatch(RoadPatch):
    def split(self, lane_id, at, suffix):
        """cut an existing lane at distance `at`: the head keeps the id, the tail gets a fixed editor-segment id"""
        self.splits = getattr(self, 'splits', []); self.virtual = getattr(self, 'virtual', {})
        pts = self.pts(lane_id); tail = segment_id(lane_id, suffix)
        self.splits.append(dict(id=lane_id, at=round(at, 4), tailId=tail))
        self.virtual[lane_id] = cut(pts, 0, at); self.virtual[tail] = cut(pts, at, E.length(pts))
        return tail

    def attr(self, lane_id, rule_id=None, turn=None):
        self.attrs = getattr(self, 'attrs', [])
        item = dict(id=lane_id)
        if rule_id is not None:
            item['ruleId'] = rule_id
        if turn is not None:
            item.update(turn=turn, setTurn=True)
        self.attrs.append(item)

    def cross_existing(self, lane_id, centre, half, tag, rule):
        """split an existing lane into approach / through piece / exit around a junction centre; the through
        piece joins the junction rule. Returns (approach, through, exit) ids."""
        s = station(self.pts(lane_id), centre)
        mid = self.split(lane_id, s - half, f'{tag}:through')
        out = self.split(mid, half * 2, f'{tag}:exit')
        self.attr(mid, rule_id=rule, turn=0)
        return lane_id, mid, out

    def turn(self, new_id, src, dst, copy_of, rule, turn, source=''):
        sp, dp = self.pts(src), self.pts(dst)
        pts = curve(sp[-1], unit(sp[-2], sp[-1]), dp[0], unit(dp[0], dp[1]), steps=16)
        item = self.lane(new_id, pts, copy_of, source or E.LANES[origin(copy_of)]['sourceId'] if origin(copy_of) in E.LANES else source,
                         width=3.0, prev=[src], next=[dst], turn=turn)
        item['ruleId'] = rule
        return item
