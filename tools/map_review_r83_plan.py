"""J3516 (Y04) Daeeun-ro stop lines where the approach lanes end (map r82 -> r83, 2026-11-27).

Neither Daeeun-ro approach of J3516_12881 had an authored stop line, so the game placed the stop 11 m from the
junction centre while both approaches' lanes (left pockets and straights) end about 16 m out. A left turner
then waited 5 m inside the box, on its connector, with its body in the oncoming straight's corridor; the
oncoming car may not enter an occupied corridor and the left turner waits for that oncoming car, so both sat
through every green (district traffic test, 6 stalled cars from r80 on). r82 moved the south-west pocket out
of the oncoming lane; this patch adds the two stop lines at 16 m (just before the lanes end), so cars wait in
their lanes and left turners creep into the box only when they go.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r83_plan.py
Output: config/maps/review/r83-y04-stop-lines.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_road_add as R  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r83-y04-stop-lines.json'
J, C = 'J3516_12881', (351.584, 1288.13)
A0 = (0.590029, -0.807382)              # the rule's heading (axis 0)
A1 = (-A0[1], A0[0])                    # north-east along Daeeun-ro; TrafficSystem axis 1 points the other way
STOP, WIDTH = 16.0, 7.0
# (name, sign, metres along Daeeun-ro from the centre, lateral centre of the approach's lanes along axis 0, lane the line belongs to)
APPROACHES = (('SW', 1, STOP, -0.7, 'reviewed:Y04:SW:left'), ('NE', -1, -STOP, 1.0, 'reviewed:Y04:NE:left'))


def build():
    assert E.DOC['revision'] == 82, E.DOC['revision']
    P = R.JunctionPatch([], 82, 83)
    src = 'reviewed:J5057-N:stop'
    for name, sign, along, lateral, lane in APPROACHES:
        x = C[0] + A1[0] * along + A0[0] * lateral; z = C[1] + A1[1] * along + A0[1] * lateral
        heading = (-A1[0] * sign, -A1[1] * sign)  # travel direction of that approach
        P.addFacilities.append(dict(id=f'review:r83:{J}:{name}:stop', copyOf=src, type='안내', junctionId=J, text='게임 설정', laneId=lane,
                                    axis=1, sign=sign, x=round(x, 3), z=round(z, 3),
                                    yaw=round(math.degrees(math.atan2(heading[0], heading[1])) % 360, 2), width=WIDTH, length=0.4))
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, P.addFacilities)
