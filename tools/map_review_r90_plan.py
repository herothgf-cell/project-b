"""J6366 crosswalks and crosswalk-ahead diamond (map r89 -> r90, 2026-11-28). See tools/map_review_marks.py.

J058-1: the aerial and the Kakao roadview (2026-06) show crosswalks across the north-west arm (about 629, 1350)
and the south-east arm (about 642, 1335) of J6366_13416; the game has none. Both are placed with the r56
junction-crosswalk rule (map_review_marks.arm_crossing: half the widest other road + 3 m, spanning the lanes
they cut), which lands within 1-2 m of the aerial stripes. The stripes along the south-west edge cross an
underground car-park ramp that is not a game road and are left out; the north-east arm has no crosswalk near
the junction (the one at G1 belongs to J7292).
J058-2 (part): one crosswalk-ahead diamond on the south-east-bound approach of the north-west arm, 32 m out
(the aerial has two, one per real approach lane; the game has one approach lane).
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r90_plan.py
Output: config/maps/review/r90-j6366-crosswalks.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_review_marks as M  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r90-j6366-crosswalks.json'
CROSSWALK, DIAMOND = 'review:r56:J055:crosswalk:020', 'review:r56:J055:line:332'
CENTRE = (M.ITEMS['J058']['x'], M.ITEMS['J058']['z'])
NW, SE = 322.8, 141.4


def build():
    assert E.DOC['revision'] == 89, E.DOC['revision']
    P = E.LanePatch([], 89, 90)
    arms = M.arms(CENTRE)
    for name, bearing in (('nw', NW), ('se', SE)):
        g = M.arm_crossing(CENTRE, min(arms, key=lambda a: abs(a - bearing)), arms)
        P.copy_mark(CROSSWALK, f'review:r90:J058:crosswalk:{name}').update(
            x=round(g['x'], 3), z=round(g['z'], 3), yaw=round(g['yaw'], 3), width=round(min(g['width'], 40.0), 3), length=round(g['length'], 3))
    for k, g in enumerate(M.lane_points_on_arm(CENTRE, NW, 32, True)):
        P.copy_mark(DIAMOND, f'review:r90:J058:ahead:{k}').update(x=round(g['x'], 3), z=round(g['z'], 3), yaw=round(g['yaw'], 3))
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, [(f['id'], f['x'], f['z'], f['width']) for f in r['addFacilities']])
