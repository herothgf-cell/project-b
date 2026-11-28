"""J5852 west arm: restore the median-band masks (map r88 -> r89, 2026-11-28). See tools/map_review_r88_plan.py.

r88 widened the two r87 paint-replacement masks to the whole carriageway to hide a dark line left in the
eastbound carriageway. The trial showed the dark line is not paint those masks control (it stayed), while the
wide masks hid every lane divider on the arm. This puts the masks back on the 3 m hatched median band exactly
as r87 wrote them. The r88 junction-piece width (19 m) stays.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r89_plan.py
Output: config/maps/review/r89-j5852-median-masks.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r89-j5852-median-masks.json'
R87 = json.loads((G.ROOT / 'config/maps/review/r87-j5852-west-arm.json').read_text(encoding='utf-8'))


def build():
    assert E.DOC['revision'] == 88, E.DOC['revision']
    P = E.LanePatch([], 88, 89)
    for f in R87['addFacilities']:
        if ':median:mask:' in f['id']:
            P.facilities[f['id']] = dict(E.FACILITIES[f['id']], x=f['x'], z=f['z'], yaw=f['yaw'], width=f['width'], length=f['length'])
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, [(f['id'], f['width']) for f in r['facilities']])
