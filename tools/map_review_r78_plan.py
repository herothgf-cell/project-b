"""Suin-ro north service road west of J866 (map r77 -> r78, 2026-11-26). See tools/map_road_add.py.

J022-2 / J077-3: the aerial has a two-way service road (about 6.5 m) north of Suin-ro from the west map edge to
the J866 north arm (centre z 565.2 at x 1, 564.4 at x 60, widening into the arm near x 75); the game has only the
surveyed right turn from the north arm (A2218G611394), which ends at x 75.4 with no exit.
- a road strip along the centre line (6.5 m);
- westbound lane: continues that right turn, bends onto the north half of the strip by x 60 and runs to the map
  edge (x 1.5, where it ends like the other edge lanes);
- eastbound lane: from the map edge along the south half to x 66, then a left turn across the north arm into its
  northbound lane (local:1093:5743:2303:7782), as a T junction;
- the row of small roadside sidewalk pieces across the mouth is cut back to the strip (pieces the strip covers
  entirely are switched off).
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r78_plan.py [--check <dir>]
Output: config/maps/review/r78-service-road.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_road_add as R  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r78-service-road.json'
STRIP = 'road:review:r78:suinro-service'
TURN_IN, NORTHBOUND = 'A2218G611394:0', 'local:1093:5743:2303:7782'
STYLE_ROAD, STYLE_LANE = 'road:A2218G611394', 'local:1093:5743:2303:7782'
CENTRE = [(1.5, 565.2), (30.0, 564.9), (60.0, 564.4), (70.0, 563.8)]
HALF = 1.6  # lane centres either side of the centre line
WIDTH = 6.5


def build():
    P = R.RoadPatch([], 77, 78)
    P.strip(STRIP, CENTRE + [(76.0, 562.8)], WIDTH, STYLE_ROAD)
    # westbound: from the end of the surveyed right turn onto the north half, then west to the edge
    turn = P.pts(TURN_IN)
    head = R.unit(turn[-2], turn[-1])
    west = R.offset_line(CENTRE[::-1], HALF)  # runs west: right of it = north
    joint = [p for p in west if p[0] <= 60.0]
    wb = R.curve(turn[-1], head, joint[0], R.unit(joint[0], joint[1]), steps=10)[:-1] + E.resample(joint, 3.0)
    P.lane('review:r78:service:westbound', wb, STYLE_LANE, STRIP, width=3.0, prev=[TURN_IN], road_name='수인로 측도')['ruleId'] = ''
    # eastbound: from the edge along the south half, then the left turn into the north arm's northbound lane
    east = [p for p in R.offset_line(CENTRE, HALF) if p[0] <= 66.0]
    eb = E.resample(east, 3.0)
    P.lane('review:r78:service:eastbound', eb, STYLE_LANE, STRIP, width=3.0, road_name='수인로 측도')['ruleId'] = ''
    join = P.connector('review:r78:service:left-out', 'review:r78:service:eastbound', NORTHBOUND, STYLE_LANE, turn=-1)
    join.update(sourceId=STRIP, roadName='수인로 측도', ruleId='')
    # the row of small roadside sidewalk pieces across the mouth (x 74-88) is where the road meets the north arm
    P.cut = P.clip_sidewalks((66.0, 563.9), (90.0, 563.0), WIDTH / 2, 'r78')
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)}, 'cut', P.cut, 'off', P.disableObjects)
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'service.jpg', (50, 562), 100, 'r78 Suin-ro service road')
        P.check(out / 'mouth.jpg', (82, 565), 40, 'r78 service road mouth')
