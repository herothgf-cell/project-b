"""은행로 one-way northbound road from the J2338 road to the r80 south end (map r83 -> r84, 2026-11-28).
See tools/map_road_add.py (JunctionPatch).

J051-3: the aerial has a wide road west of J4410 (x 380-396 at z 1000) that the game lacks. Kakao roadview
(2026-06, 은행로) shows it is one-way northbound: white dashed lanes only, double yellow at the west curb, parked
cars along the east curb. Followed south, it curves west-south-west to about (290, 900), where the J2338 road
(game lanes end at J2627_8762, z 870) bends north-east into it and a road from the west (not in the game) joins.
Followed north, it runs into the r80 road (은행로 남북길) at J3754_11003, where 은행로144번길 starts.
- a 14.5 m road strip along the centre line; four 3.5 m lanes, all northbound;
- the J2338 road's two northbound lanes (dead ends at J2627_8762) turn into the two right-hand lanes;
- J3888_10040 (the J4410 west arm's dead end): a T junction. The 은행로 lanes are cut into approach / through /
  exit pieces; westbound turns right into the right-hand lane, the right-hand lane turns right into the
  eastbound lane. The west arm lanes are cut back to the 은행로 east edge;
- J3754_11003: the two left lanes go straight on into the r80 northbound lane, the two right lanes turn right
  into 은행로144번길 (eastbound). The 144번길 lanes are cut back to the 은행로 east edge, and the r80 T-end turns are
  redrawn to the new lane ends (southbound r80 traffic still has to turn into 144번길: 은행로 is one-way);
- sidewalk pieces across the new road are cut back.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r84_plan.py [--check <dir>]
Output: config/maps/review/r84-eungye-ro-one-way.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_road_add as R  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r84-eungye-ro-one-way.json'
# centre line read off the aerial (curbs every 10-15 m; the parking strip along the east curb is inside the road)
CENTRE = [(292, 896), (305, 897), (318, 900.5), (330.5, 907), (343, 917.5), (353, 929), (363, 938), (371, 947),
          (378, 957), (383, 968), (386, 980), (388, 992), (388, 1004), (387.5, 1016), (386.5, 1030), (385, 1043),
          (382.5, 1057), (380.5, 1070), (378.5, 1082), (376.5, 1092)]
WIDTH, LANE_W = 14.5, 3.5
OFFSETS = (-5.25, -1.75, 1.75, 5.25)          # lane centres right of the northbound centre line (1 = left)
STRIP, STYLE_ROAD, STYLE_LANE = 'road:review:r84:eungye-ro', 'road:37612022:A0020000:1200:0', 'local:3908:10026:4330:10037'
J2338_IN, J2338_OUT = 'local:2421:8073:2644:8729', 'local:2481:8056:2698:8699'      # northbound, ends at J2627_8762
WEST_EB, WEST_WB = 'local:3908:10026:4330:10037', 'local:4330:10067:3907:10056'      # J4410 west arm
S_OUT, S_IN = 'local:3777:10978:4203:11029', 'local:4196:11084:3770:11033'          # 은행로144번길 at J3754_11003
NB_IN, SB_OUT = 'review:r80:nb:south', 'review:r80:sb:south'
TURN_IN, TURN_OUT = 'review:r80:turn:south-in', 'review:r80:turn:south-out'
START_RULE, T_RULE, END_RULE = 'J2627_8762', 'J3888_10040', 'J3754_11003'
T_Z, T_HALF, END_Z = 1004.0, 6.0, 1087.0     # T junction centre (z) and half-length; lane ends before J3754
EAST_EDGE = 397.0                            # west arm lanes start / end here (은행로 east curb 395.3)
NAME = '은행로'


def trim(P, lane_id, x_min, at_end, width=None):
    """cut a lane back so that its end (or start) lies at x = x_min; the lane keeps its own vertices (one
    exact cut point added)"""
    pts = P.pts(lane_id)
    if at_end:
        pts = pts[::-1]
    k = next(i for i, p in enumerate(pts) if p[0] >= x_min)
    a, b = pts[k - 1], pts[k]; t = (x_min - a[0]) / (b[0] - a[0])
    out = [(x_min, a[1] + (b[1] - a[1]) * t)] + pts[k + (1 if math.dist((x_min, 0), (b[0], 0)) < .3 else 0):]
    P.set_points(lane_id, out[::-1] if at_end else out, width)
    return out[0]


def build():
    assert E.DOC['revision'] == 83, E.DOC['revision']
    P = R.JunctionPatch([], 83, 84)
    P.strip(STRIP, CENTRE, WIDTH, STYLE_ROAD)
    line = E.resample(CENTRE, 1.0)
    source = STRIP[len('road:'):]
    ids = {}

    def lane(key, pts, rule, prev=(), next=(), turn=0):
        item = P.lane(f'review:r84:{key}', pts, STYLE_LANE, source, width=LANE_W, prev=prev, next=next, turn=turn, road_name=NAME)
        item['ruleId'] = rule; ids[key] = item['id']
        return item['id']

    # the west arm and 144번길 cut back to the 은행로 east edge (before the new lanes are joined to them)
    # the west arm is about 6 m between its sidewalks: its two lanes narrowed to 2.8 m (the validator samples the
    # edges of an edited lane against the sidewalks)
    trim(P, WEST_EB, EAST_EDGE + 2, False, 2.8); trim(P, WEST_WB, EAST_EDGE + 2, True, 2.8)
    trim(P, S_OUT, 386.0, False); trim(P, S_IN, 386.0, True)
    # four lanes, each cut into: south part (to the T), T through piece, north part (to J3754)
    zs = lambda pts, z: R.station(pts, min(pts, key=lambda p: abs(p[1] - z)))
    for i, d in enumerate(OFFSETS, 1):
        full = E.resample(R.offset_line(line, d), .5)
        s_end = zs(full, END_Z)
        full = R.cut(full, 0, s_end)
        s0, s1 = zs(full, T_Z - T_HALF), zs(full, T_Z + T_HALF)
        north = lane(f'{i}:north', R.cut(full, s1, E.length(full)), END_RULE)
        mid = lane(f'{i}:through', R.cut(full, s0, s1), T_RULE, next=[north])
        lane(f'{i}:south', R.cut(full, 0, s0), T_RULE, next=[mid])
    # start: the J2338 road's northbound lanes bend into the two right-hand lanes
    P.turn('review:r84:turn:j2338-3', J2338_IN, ids['3:south'], STYLE_LANE, START_RULE, 1, source=source)
    P.turn('review:r84:turn:j2338-4', J2338_OUT, ids['4:south'], STYLE_LANE, START_RULE, 1, source=source)
    # T junction at J3888: right turns between the west arm and the right-hand lane
    P.turn('review:r84:turn:t-east', ids['4:south'], WEST_EB, STYLE_LANE, T_RULE, 1, source=source)
    P.turn('review:r84:turn:t-north', WEST_WB, ids['4:north'], STYLE_LANE, T_RULE, 1, source=source)
    # J3754: left lanes straight on into the r80 northbound lane, right lanes right into 144번길
    for i in (1, 2):
        P.turn(f'review:r84:turn:end-{i}', ids[f'{i}:north'], NB_IN, STYLE_LANE, END_RULE, 0, source=source)
    for i in (3, 4):
        P.turn(f'review:r84:turn:end-{i}', ids[f'{i}:north'], S_OUT, STYLE_LANE, END_RULE, 1, source=source)
    # the r80 T-end turns redrawn to the trimmed 144번길 lanes
    for t, a, b in ((TURN_IN, S_IN, NB_IN), (TURN_OUT, SB_OUT, S_OUT)):
        sp, dp = P.pts(a), P.pts(b)
        P.set_points(t, R.curve(sp[-1], R.unit(sp[-2], sp[-1]), dp[0], R.unit(dp[0], dp[1]), steps=16))
    cut = []
    for p, q in zip(CENTRE, CENTRE[1:]):
        cut += P.clip_sidewalks(p, q, WIDTH / 2, f'r84:{len(cut)}')
    # small sidewalk caps left inside the T and the J3754 junction (the turns run over them)
    for o in ('sidewalk:generated:285', 'sidewalk:generated:286', 'sidewalk:generated:2019', 'sidewalk:generated:2014', 'sidewalk:generated:2016',
              'sidewalk:generated:283', 'sidewalk:generated:284', 'sidewalk:generated:2018'):
        if o not in P.disableObjects:
            P.disableObjects.append(o); P.objects.pop(o, None)
    P.cut = sorted(set(cut))
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)}, 'cut', len(P.cut), 'off', len(P.disableObjects))
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'road.jpg', (340, 995), 230, 'r84 은행로')
        P.check(out / 'start.jpg', (285, 895), 50, 'r84 start (J2627)')
        P.check(out / 't.jpg', (390, 1004), 40, 'r84 T (J3888)')
        P.check(out / 'end.jpg', (377, 1095), 40, 'r84 end (J3754)')
