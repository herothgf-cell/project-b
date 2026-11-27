"""J495 east alley to the north-south road at x 190 (map r85 -> r86, 2026-11-28).
See tools/map_road_add.py (JunctionPatch).

J054-2: the aerial has a narrow two-way alley from J495_4339 (an unconfigured T) going east-south-east between
the buildings, then east to the north-south road at x 190 (z about 388). Kakao roadview (2026-06) shows a
two-way residential alley with cars parked along it; the game has no road there.
- a 6 m road strip along the centre line read off the aerial; one 2.8 m lane each way at +-1.5 m;
- west end: J495 becomes a four-way plain junction (rule J495_4339): left / straight / right turns between the
  alley and the three existing arms;
- east end: both lanes of the x 190 road are cut into approach / through / exit pieces around the alley mouth
  (new plain rule review:r86:J1880_3950), with left and right turns in and out of the alley;
- sidewalk pieces across the new road are cut back; a 2 x 4 m game shed on the alley is switched off.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r86_plan.py [--check <dir>]
Output: config/maps/review/r86-j495-east-alley.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_road_add as R  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r86-j495-east-alley.json'
# centre line read off the aerial, then fitted between the game's building footprints (they sit up to 4 m south of
# the aerial buildings between x 90 and 120, and a kiosk at the east mouth reaches z 392)
CENTRE = [(54, 433), (59, 430.4), (66, 427.5), (75, 425.5), (90, 420.5), (100, 417), (110, 413.5), (120, 409.5), (130, 407),
          (140, 405.5), (150, 404.5), (160, 402.5), (168, 399.5), (176, 394), (183, 389), (189, 387.5)]
WIDTH, LANE_W, HALF = 6.0, 2.8, 1.5
WEST_BOX, EAST_BOX = 6.0, 5.5          # lane ends kept back from the two junction centres
WEST_C = (48.5, 434.0)
EAST_C = (192.8, 387.5)                # on the x 190 road between its two lanes
STRIP, STYLE_ROAD, STYLE_LANE = 'road:review:r86:J495-east', 'road:37612022:A0020000:1200:0', 'local:770:5101:499:4442'
WEST_RULE, EAST_RULE = 'J495_4339', 'review:r86:J1880_3950'
SB, NB = 'local:1903:4938:1912:2429', 'local:1957:2414:1950:4938'      # x 190 road (south / north bound)
# J495 arms: approaches (end at the junction) and exits (start there)
S_IN, N_IN, W_IN = 'local:34:3346:482:4238', 'local:770:5101:499:4442', 'local:16:4501:398:4360'
N_OUT, S_OUT, W_OUT = 'local:562:4417:834:5080', 'local:431:4260:-16:3370', 'local:410:4390:23:4532'
NAME = ''


def build():
    assert E.DOC['revision'] == 85, E.DOC['revision']
    P = R.JunctionPatch([], 85, 86)
    P.strip(STRIP, CENTRE, WIDTH, STYLE_ROAD)
    line = E.resample(CENTRE, .5)
    source = STRIP[len('road:'):]

    def span(pts, a, b):
        s0 = R.station(pts, min(pts, key=lambda p: math.dist(p, a)))
        s1 = R.station(pts, min(pts, key=lambda p: math.dist(p, b)))
        return R.cut(pts, s0, s1) if s0 < s1 else R.cut(pts, s1, s0)
    # the alley lanes stop WEST_BOX / EAST_BOX short of the two junction centres
    L = E.length(line)
    s_w = R.station(line, WEST_C) + WEST_BOX; s_e = R.station(line, EAST_C) - EAST_BOX
    core = R.cut(line, s_w, s_e)
    eb_pts = E.resample(R.offset_line(core, HALF), .5)
    wb_pts = E.resample(R.offset_line(core[::-1], HALF), .5)

    def lane(key, pts, rule):
        item = P.lane(f'review:r86:{key}', pts, STYLE_LANE, source, width=LANE_W, road_name=NAME)
        item['ruleId'] = rule
        return item['id']
    eb = lane('eb', eb_pts, EAST_RULE)      # eastbound: enters the east T
    wb = lane('wb', wb_pts, WEST_RULE)      # westbound: enters J495
    # east T: the x 190 road cut around the alley mouth
    sb_in, sb_mid, sb_out = P.cross_existing(SB, EAST_C, EAST_BOX, 'r86-east', EAST_RULE)
    nb_in, nb_mid, nb_out = P.cross_existing(NB, EAST_C, EAST_BOX, 'r86-east', EAST_RULE)
    for l in (sb_in, nb_in):
        P.attr(l, rule_id=EAST_RULE)
    for name, src, dst, t, rule in (
            ('east-right-out', eb, sb_out, 1, EAST_RULE), ('east-left-out', eb, nb_out, -1, EAST_RULE),
            ('east-right-in', sb_in, wb, 1, EAST_RULE), ('east-left-in', nb_in, wb, -1, EAST_RULE),
            ('west-right-out', wb, N_OUT, 1, WEST_RULE), ('west-left-out', wb, S_OUT, -1, WEST_RULE),
            ('west-straight-out', wb, W_OUT, 0, WEST_RULE),
            ('west-right-in', S_IN, eb, 1, WEST_RULE), ('west-left-in', N_IN, eb, -1, WEST_RULE),
            ('west-straight-in', W_IN, eb, 0, WEST_RULE)):
        P.turn(f'review:r86:turn:{name}', src, dst, STYLE_LANE, rule, t, source=source)
    # the right turn from the south arm into the alley swings wide of the J495 corner building (6971)
    sp, dp = P.pts(S_IN), P.pts(eb)
    a, b = sp[-1], dp[0]
    P.set_points('review:r86:turn:west-right-in', R.curve(a, R.unit(sp[-2], a), b, R.unit(b, dp[1]), steps=16, handle=math.dist(a, b) * .9), width=LANE_W)
    cut = []
    for p, q in zip(CENTRE, CENTRE[1:]):
        cut += P.clip_sidewalks(p, q, WIDTH / 2, f'r86:{len(cut)}')
    # a 2 x 4 m game shed standing on the alley (the aerial shows road there) and the sidewalk tile at the
    # east mouth under the turns (four tiles, two of them inside the x 190 carriageway)
    for o in ('building:37612022:B0010000:6206', 'sidewalk:generated:1275', 'sidewalk:generated:1756',
              'sidewalk:generated:799', 'sidewalk:generated:2173'):
        if o not in P.disableObjects:
            P.disableObjects.append(o); P.objects.pop(o, None)
    P.cut = sorted(set(cut))
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list) and v}, 'cut', P.cut, 'off', P.disableObjects)
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'alley.jpg', (120, 415), 150, 'r86 J495 east alley')
        P.check(out / 'west.jpg', (52, 432), 35, 'r86 J495')
        P.check(out / 'east.jpg', (188, 396), 35, 'r86 east T')
