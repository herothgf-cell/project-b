"""North-south road from J3754 across 은행로149번길 to J3395, with a new junction (map r79 -> r80, 2026-11-27).
See tools/map_road_add.py (JunctionPatch).

J046-1 / J047-1: the aerial has a two-way road (one lane each way, yellow centre line) running north-north-west
from the dead end of 은행로144번길 (J3754_11003, x 377) across 은행로149번길 (about x 357, z 1145) to the dead end
of the road at J3395_12007 (x 338); the game has buildings' green there and both side roads end with no exit.
- a 9 m road strip along the centre line; one 3.3 m lane each way at +-1.9 m;
- the crossing: both 은행로149번길 lanes are split around the crossing into approach / through / exit pieces
  (fixed editor-segment ids), the new road's lanes likewise, and the four approaches get left and right turn
  connectors into the four exits. Approach, through and turn pieces share a new junction rule, from a
  "검토 교차로" marker (unsignalised, crosswalks and stop lines; ReviewedMidblockControl);
- the two ends are T junctions: the dead-end lanes turn into the new road and the new road turns into the
  side roads' start lanes (they keep the existing plain rules J3754_11003 / J3395_12007);
- sidewalk pieces across the new road are cut back.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r80_plan.py [--check <dir>]
Output: config/maps/review/r80-junction-road.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_road_add as R  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r80-junction-road.json'
# centre line read off the aerial (carriageway edges every 5-15 m): the south half runs about 4 m east of the north
# half, so the line bends at the crossing
CENTRE = [(372.5, 1098), (371.6, 1105), (369.5, 1112), (367.7, 1120), (365.6, 1135), (363.5, 1141), (361.0, 1147),
          (358.5, 1153), (356.5, 1158), (353.0, 1164), (350.3, 1170), (346.5, 1180), (344.0, 1190), (341.0, 1198),
          (340.0, 1201)]
CROSS = (361.0, 1147.0)   # midway between the 은행로149번길 lanes on the new centre line
NB_SPAN, SB_SPAN = (1109.0, 1192.0), (1189.0, 1106.0)   # lane ends (z), leaving room for the T-end turns
EB, WB = 'local:3118:11277:4044:11588', 'local:4029:11636:3101:11324'               # 은행로149번길
S_OUT, S_IN = 'local:3777:10978:4203:11029', 'local:4196:11084:3770:11033'           # 은행로144번길 at J3754_11003
N_OUT, N_IN = 'local:3369:12015:2904:11791', 'local:2920:11757:3386:11981'           # road at J3395_12007
STRIP, STYLE_ROAD, STYLE_LANE = 'road:review:r80:J3754-J3395', 'road:37612022:A0020000:1200:0', EB
RULE, SOUTH_RULE, NORTH_RULE = 'review:r80:J3575_11455', 'J3754_11003', 'J3395_12007'
HALF, WIDTH, LANE_W = 1.9, 10.5, 3.3
MAIN_HALF, NEW_HALF = 6.5, 6.5   # junction box half-lengths along the main road / the new road
NAME = '은행로 남북길'


def build():
    P = R.JunctionPatch([], 79, 80)
    P.strip(STRIP, CENTRE, WIDTH, STYLE_ROAD)
    line = E.resample(CENTRE, 2.0)
    def span(full, z0, z1):
        s = lambda z: R.station(full, min(full, key=lambda p: abs(p[1] - z)))
        return R.cut(full, s(z0), s(z1))
    nb_full = span(E.resample(R.offset_line(line, HALF), .5), *NB_SPAN)          # northbound: right of the north-going centre = east
    sb_full = span(E.resample(R.offset_line(line[::-1], HALF), .5), *SB_SPAN)    # southbound: right of the south-going centre = west
    # the crossing on 은행로149번길: approach / through / exit pieces of both main-road lanes
    eb_in, eb_mid, eb_out = P.cross_existing(EB, CROSS, MAIN_HALF, 'r80-cross', RULE)
    wb_in, wb_mid, wb_out = P.cross_existing(WB, CROSS, MAIN_HALF, 'r80-cross', RULE)
    for lane in (eb_in, wb_in):
        P.attr(lane, rule_id=RULE)
    # the new road's pieces either side of the crossing (approach ends NEW_HALF before the crossing centre)
    def pieces(full, tag):
        s = R.station(full, CROSS); L = E.length(full)
        return {k: R.cut(full, a, b) for k, (a, b) in dict(a=(0, s - NEW_HALF), m=(s - NEW_HALF, s + NEW_HALF), b=(s + NEW_HALF, L)).items()}
    nb, sb = pieces(nb_full, 'nb'), pieces(sb_full, 'sb')
    ids = {}
    def lane(key, pts, rule, prev=(), next=(), turn=0):
        item = P.lane(f'review:r80:{key}', pts, STYLE_LANE, STRIP, width=LANE_W, prev=prev, next=next, turn=turn, road_name=NAME)
        item['ruleId'] = rule; ids[key] = item['id']
        return item['id']
    nb_out = lane('nb:north', nb['b'], NORTH_RULE)
    nb_mid = lane('nb:through', nb['m'], RULE, next=[nb_out])
    nb_in = lane('nb:south', nb['a'], RULE, next=[nb_mid])
    sb_out = lane('sb:south', sb['b'], SOUTH_RULE)
    sb_mid = lane('sb:through', sb['m'], RULE, next=[sb_out])
    sb_in = lane('sb:north', sb['a'], RULE, next=[sb_mid])
    # turns at the crossing: (approach, exit, turn) -- right-hand traffic, left = -1
    for name, src, dst, t in (('nb-east', nb_in, eb_out, 1), ('nb-west', nb_in, wb_out, -1),
                              ('sb-west', sb_in, wb_out, 1), ('sb-east', sb_in, eb_out, -1),
                              ('eb-north', eb_in, nb_out, -1), ('eb-south', eb_in, sb_out, 1),
                              ('wb-south', wb_in, sb_out, -1), ('wb-north', wb_in, nb_out, 1)):
        P.turn(f'review:r80:turn:{name}', src, dst, EB, RULE, t, source=STRIP)
    # T ends: dead ends turn onto the new road, the new road turns into the side roads' start lanes
    for name, src, dst, t, rule in (('south-in', S_IN, nb_in, 1, SOUTH_RULE), ('south-out', sb_out, S_OUT, -1, SOUTH_RULE),
                                    ('north-in', N_IN, sb_in, 1, NORTH_RULE), ('north-out', nb_out, N_OUT, -1, NORTH_RULE)):
        P.turn(f'review:r80:turn:{name}', src, dst, EB, rule, t, source=STRIP)
    # junction marker: heading along the new road, width = new carriageway, length = main carriageway
    d = R.unit(*R.cut(line, R.station(line, CROSS) - 3, R.station(line, CROSS) + 3)[::len(R.cut(line, R.station(line, CROSS) - 3, R.station(line, CROSS) + 3)) - 1])
    main_w = min(math.dist(p, q) for p in P.pts(eb_mid) for q in P.pts(wb_mid)) + 3.5
    src = next(f['id'] for f in E.DOC['facilities'] if f['kind'] == 'midblockControl')
    P.addFacilities.append(dict(id='review:r80:J3575_11455:junction', copyOf=src, type='검토 교차로', junctionId=RULE, text='',
                                axis=0, x=CROSS[0], z=CROSS[1], yaw=round(math.degrees(math.atan2(d[0], d[1])) % 360, 3),
                                width=WIDTH, length=round(main_w, 2),
                                evidence=dict(state='observed', appearanceConfirmed=True, ruleConfirmed=False, checkedAt='2026-11-27',
                                              month='', url='', note='J046-1/J047-1: road and crossing from the aerial (Kakao skyview, checked '
                                              '2026-11-27); unsignalised crossing with crosswalks and stop lines is a game estimate.')))
    cut = []
    for p, q in zip(CENTRE, CENTRE[1:]):
        cut += P.clip_sidewalks(p, q, WIDTH / 2, f'r80:{len(cut)}')
    P.cut = sorted(set(cut))
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)}, 'cut', P.cut, 'off', P.disableObjects)
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'road.jpg', (356, 1150), 120, 'r80 J046-1 / J047-1')
        P.check(out / 'cross.jpg', CROSS, 40, 'r80 crossing')
        P.check(out / 'south.jpg', (373, 1103), 30, 'r80 south T')
        P.check(out / 'north.jpg', (341, 1195), 30, 'r80 north T')
