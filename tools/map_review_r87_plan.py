"""J5852 west arm: carriageway width, lane positions and the hatched median (map r86 -> r87, 2026-11-28).
See tools/map_geometry_edit.py (Edit) and tools/map_lane_edit.py (LanePatch).

J056-1 / J056-3: the arm north-west of J5852_16963 (x 499-585) is a 35.5 m road strip (and the 35.5 m junction
piece bulges south of the junction); the aerial shows about 18 m of carriageway (north kerb -8.7 m, south kerb
+9.3 m about the strip's end-to-end line, + = south-west) with a 3 m yellow-hatched median at about -0.3 m.
J056-2: the game's eastbound lanes run 6-10 m south of the real eastbound carriageway (the outer one on the
plaza beside the road) and the westbound inner lane sits on the real median.
- EDIT_ROAD moves the strip edges to the aerial kerbs and the sidewalks with them (kept 4 m wide); lanes, their
  dark polish strips, the north connector strip and the r56 connector crosswalk stay;
- EDIT_LANES moves only the four arm lanes and their polish strips (westbound -6.9 / -3.6, eastbound 3.5 / 7.0),
  tapering through the junction mouth so the turn connectors bend to meet them;
- a 3 m hatched median (two outline lines, a stroke every 2 m, mask over the synthetic centre line) from the
  junction mouth to the map edge, in the r77 form.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r87_plan.py [--check <dir>]
Output: config/maps/review/r87-j5852-west-arm.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r87-j5852-west-arm.json'
A, B = (576.0, 1701.0), (499.0, 1750.0)            # end-to-end line of road:...:2226 (runs north-west)
L = math.dist(A, B)
U = ((B[0] - A[0]) / L, (B[1] - A[1]) / L)
N = (-U[1], U[0])                                   # + = left of the north-west direction = south-west
REF = [(A[0] - U[0] * 30, A[1] - U[1] * 30), A, B, (B[0] + U[0] * 30, B[1] + U[1] * 30)]
S0 = 30.0                                           # station of A on REF


def W(s, t):
    """world point at station s (from A, + = north-west) and offset t (+ = south-west)"""
    return (A[0] + U[0] * s + N[0] * t, A[1] + U[1] * s + N[1] * t)


NORTH_KERB, SOUTH_KERB, MEDIAN_T, HALF = -8.7, 9.3, -0.3, 1.5
# the 2226 strip centre offset at its vertices (station, offset); edges are +-17.75 about it
STRIP_C = [(-0.5, .3), (13.5, 2.1), (27.5, 3.8), (41.6, 5.6), (54.3, 4.9), (67.1, 4.1), (79.2, 2.1), (91.4, .1)]
ARM_LANES = ['local:4974:17454:5645:17011', 'local:4914:17388:5607:16931', 'local:5683:17092:5033:17520', 'local:5721:17172:5092:17586',
             'local:4974:17454:5645:17011:part:2', 'local:4974:17454:5645:17011:part:1',
             'local:4914:17388:5607:16931:part:2', 'local:4914:17388:5607:16931:part:1',
             'local:5683:17092:5033:17520:part:1', 'local:5683:17092:5033:17520:part:2',
             'local:5721:17172:5092:17586:part:1', 'local:5721:17172:5092:17586:part:2']
# lane offsets now (station: westbound outer, westbound inner, eastbound inner, eastbound outer)
LANE_T = [(10, (-11.6, -2.8, 6.0, 14.8)), (20, (-10.3, -1.5, 7.3, 16.1)), (30, (-9.0, -.2, 8.6, 17.4)),
          (40, (-7.8, 1.0, 9.9, 18.7)), (50, (-8.2, .7, 9.7, 18.5)), (60, (-8.8, .1, 9.0, 17.9)),
          (70, (-9.8, -.8, 8.1, 16.8)), (81, (-11.8, -2.7, 6.4, 15.2)), (92, (-13.6, -4.4, 4.9, 13.6))]
LANE_NEW = (-6.9, -3.6, 3.5, 7.0)
HATCH, MEDIAN, MASK = 'r49:suinro:J6478:median:hatch:0', 'reviewed:r47:A:median:north', 'reviewed:r47:A:mask:1'
POLISH = 'road:polish:'
KEEP_ROAD = ['road:37612023:A0020000:2227:0', 'road:37612023:A0020000:2236:0', 'road:37612023:A0020000:2228:0',
             'road:37612023:A0020000:2233:0', 'review:r56:J056:crosswalk:037', 'road:37612023:A0020000:2235:0']
JUNCTION_PIECE, JUNCTION_W = 'road:37612023:A0020000:2235:0', 23.0   # was 35.5 m: the bulge south of J5852 (J056-3)
# turn connectors through J5852 that start or end on the arm lanes: redrawn between the moved lane ends
JOINS = ['join:local:4974:17454:5645:17011:-1', 'join:local:4974:17454:5645:17011:0', 'join:local:4914:17388:5607:16931:0',
         'join:local:10539:16004:5952:17147:1', 'join:local:7260:14995:6050:16894:0', 'join:local:7319:15001:6080:16946:0']

objects = [o['id'] for o in E.DOC['scene']['objects']]
lane_ids = list(E.LANES)


def _mix(old, new, w):
    return old + (new - old) * w


# the kerbs come in from the junction corners: the north one over stations 4-16 (the connector's right turn
# sweeps the corner), the south one over 0-10
STRIP_C = [(-0.5, .3), (4.0, .9), (10.0, 1.65)] + STRIP_C[1:]
road_sections = []
for s, c in STRIP_C:
    wn, ws = G.smooth((s - 4) / 12), G.smooth(s / 10)
    n0, s0_ = c - 17.75, c + 17.75
    road_sections.append((S0 + s, [(n0 - 2.3, _mix(n0 - 2.3, NORTH_KERB - 4.0, wn)), (n0, _mix(n0, NORTH_KERB, wn)),
                                   (s0_, _mix(s0_, SOUTH_KERB, ws)), (s0_ + 2.3, _mix(s0_ + 2.3, SOUTH_KERB + 4.0, ws))]))
EDIT_ROAD = G.Edit(REF, s0=S0 - 4, s1=S0 + 100, controls=road_sections, taper=(6, 6), fade=1.5,
                   skip=lane_ids + [o for o in objects if o.startswith(POLISH)] + KEEP_ROAD)
CHANGES = [i for i in E.LANES if i.startswith('change:') and sum(k in i for k in ('4974:17454', '4914:17388', '5683:17092', '5721:17172')) == 2]
moving = set(ARM_LANES + CHANGES) | {POLISH + l for l in ARM_LANES + CHANGES}
lane_sections = [(S0 + s, list(zip(t, LANE_NEW))) for s, t in LANE_T]
EDIT_LANES = G.Edit(REF, s0=S0 + 8, s1=S0 + 100, controls=lane_sections, taper=(.5, 6), fade=4.0, min_lane=3.0,
                    skip=[i for i in lane_ids + objects if i not in moving])


def stroke(P, src, new_id, a, b, width):
    item = P.copy_mark(src, new_id)
    item.update(x=round((a[0] + b[0]) / 2, 4), z=round((a[1] + b[1]) / 2, 4),
                yaw=round(math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360, 3), width=width, length=round(math.dist(a, b), 4))
    return item


def median(P, s0, s1):
    """outline lines and mask in pieces of at most 40 m (the facility length limit)"""
    n = math.ceil((s1 - s0) / 38.0); cuts = [s0 + (s1 - s0) * i / n for i in range(n + 1)]
    for i, (a_s, b_s) in enumerate(zip(cuts, cuts[1:])):
        for side, sg in (('north', -1), ('south', 1)):
            stroke(P, MEDIAN, f'review:r87:J056:median:edge:{side}:{i}', W(a_s, MEDIAN_T + sg * HALF), W(b_s, MEDIAN_T + sg * HALF), .15)
        m = P.copy_mark(MASK, f'review:r87:J056:median:mask:{i}')
        a, b = W(a_s, MEDIAN_T), W(b_s, MEDIAN_T)
        m.update(x=round((a[0] + b[0]) / 2, 4), z=round((a[1] + b[1]) / 2, 4),
                 yaw=round(math.degrees(math.atan2(b[0] - a[0], b[1] - a[1])) % 360, 3), width=2 * HALF + .4,
                 length=round(math.dist(a, b), 4))
    k, s = 0, s0 + .5
    while s + 2 * HALF <= s1:
        stroke(P, HATCH, f'review:r87:J056:median:hatch:{k:03d}', W(s, MEDIAN_T + HALF), W(s + 2 * HALF, MEDIAN_T - HALF), .22)
        k += 1; s += 2.0


def build():
    assert E.DOC['revision'] == 86, E.DOC['revision']
    P = E.LanePatch([EDIT_ROAD, EDIT_LANES], 86, 87)
    median(P, 16.0, 90.0)
    # the strip's far end sits on the map's north boundary (z 1750): keep its moved pieces inside it
    z_max = max(q['z'] for o in E.DOC['scene']['objects'] if o['id'] == 'road:37612023:A0020000:2226:0' for q in o['points'])
    for o in P.addObjects + list(P.objects.values()):
        if o['id'].startswith('road:37612023:A0020000:2226:0'):
            for q in o['points']:
                q['z'] = min(q['z'], z_max)
    # moved westbound lanes that now run past the north boundary end on it
    for lid in ARM_LANES + CHANGES:
        pts = P.pts(lid)
        if max(q[1] for q in pts) > z_max and lid in P.lanes:
            out = [pts[0]]
            for a, b in zip(pts, pts[1:]):
                if b[1] <= z_max:
                    out.append(b); continue
                if a[1] < z_max:
                    u = (z_max - a[1]) / (b[1] - a[1]); out.append((a[0] + (b[0] - a[0]) * u, z_max))
                break
            P.set_points(lid, out)
    o = next(o for o in E.DOC['scene']['objects'] if o['id'] == JUNCTION_PIECE)
    P.objects[JUNCTION_PIECE] = dict(id=JUNCTION_PIECE, width=JUNCTION_W, points=o['points'])
    for j in JOINS:
        src = next(i for i, x in E.LANES.items() if j in x['next'] and not x.get('disabled')); dst = E.LANES[j]['next'][0]
        a_pts, b_pts = P.pts(src), P.pts(dst)
        a, b = a_pts[-1], b_pts[0]
        fa = E.at(a_pts, E.length(a_pts) - .01)[1]; fb = E.at(b_pts, .01)[1]
        h = math.dist(a, b) * .4; c1 = (a[0] + fa[0] * h, a[1] + fa[1] * h); c2 = (b[0] - fb[0] * h, b[1] - fb[1] * h)
        pts = [tuple((1 - t) ** 3 * a[k] + 3 * (1 - t) ** 2 * t * c1[k] + 3 * (1 - t) * t * t * c2[k] + t ** 3 * b[k] for k in (0, 1))
               for t in (i / 32 for i in range(33))]
        P.set_points(j, pts)
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list) and v})
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'arm.jpg', (545, 1722), 100, 'r87 J5852 west arm')
        P.check(out / 'junction.jpg', (580, 1700), 50, 'r87 J5852')
