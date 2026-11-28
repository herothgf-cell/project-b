"""J5852 west arm follow-up (map r87 -> r88, 2026-11-28). See tools/map_review_r87_plan.py.

The r87 trial showed two leftovers:
- a dark fragment of the old (HD) centre line still painted in the eastbound carriageway (the "black stain" of
  J056-2), outside the r87 mask that only covered the hatched median band: the two paint-replacement masks are
  widened to the whole carriageway (kerb to kerb), so inside them only the reviewed lanes' markings are drawn;
- the junction piece south of J5852 (r87: 23 m) still reached about 2 m past the moved south kerb at the arm
  mouth: narrowed to 19 m, the arm's width.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r88_plan.py [--check <dir>]
Output: config/maps/review/r88-j5852-west-arm-masks.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_review_r87_plan as P87  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r88-j5852-west-arm-masks.json'
MASKS = ['review:r87:J056:median:mask:0', 'review:r87:J056:median:mask:1']
WIDTH = P87.SOUTH_KERB - P87.NORTH_KERB         # 18 m
CENTRE_T = (P87.SOUTH_KERB + P87.NORTH_KERB) / 2


def build():
    assert E.DOC['revision'] == 87, E.DOC['revision']
    P = E.LanePatch([], 87, 88)
    for m in MASKS:
        f = dict(E.FACILITIES[m])
        # the r87 masks sit on the median line (t = MEDIAN_T); shift their centre to the carriageway centre
        d = CENTRE_T - P87.MEDIAN_T
        f.update(x=round(f['x'] + P87.N[0] * d, 4), z=round(f['z'] + P87.N[1] * d, 4), width=round(WIDTH, 3))
        P.facilities[m] = f
    o = next(o for o in E.DOC['scene']['objects'] if o['id'] == P87.JUNCTION_PIECE)
    P.objects[P87.JUNCTION_PIECE] = dict(id=P87.JUNCTION_PIECE, width=19.0, points=o['points'])
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list) and v})
