"""J7204 / J7917 approach paving and bicycle crossing (map r74 -> r75, 2026-11-26). See tools/map_lane_edit.py.

J015-2: the aerial shows red anti-skid paving (with white triangles) on the eastbound lanes just before the
J7204 west crosswalk (x 702-705.5, z 252.6-258.2) and a red bicycle crossing band on the outer (west) side of the
yellow J7917 west crosswalk (x 763-769). The paving is added like the r65 approach paving; the J7917 crosswalk
gets the bicycle band ("자전거횡단도:바깥쪽" in its text, as r56 does). J015-1 (yellow H3/H4 crosswalks) was
already done by r55 and is closed without a change.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r75_plan.py
Output: config/maps/review/r75-marks.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r75-marks.json'
J7917_WEST = 'review:r55:J7917_2565:a1:s-1:crosswalk'
BIKE = '자전거횡단도:바깥쪽'


def build():
    P = E.LanePatch([], 74, 75)
    f = E.FACILITIES[J7917_WEST]
    P.facilities[J7917_WEST] = dict(id=J7917_WEST, x=f['x'], z=f['z'], yaw=f['yaw'], width=f['width'], length=f['length'], text=BIKE)
    pave = P.copy_mark('review:r56:J019:line:051', 'review:r75:J015:paving:001')
    pave.update(x=703.75, z=255.4, yaw=90.0, width=5.6, length=3.5)
    return P


if __name__ == '__main__':
    r = build().write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)})
