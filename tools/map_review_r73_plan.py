"""Crosswalks read off the aerial (map r72 -> r73, 2026-11-26). See tools/map_lane_edit.py.

Positions from gridded Kakao aerials with the game crosswalks drawn on top (forward = along the road, width =
across it, as the facilities store them):
- J007-4 (J4396): the north-west crosswalk lay about 3.6 m outward and ran 38.6 m long, its north end about 7 m
  past the stripes; it moves onto the aerial stripes (centre x 430.4 z 1356.6, 23 m long). The other three
  J4396 crosswalks sit outward in the same way but were not on the sheet and stay.
- J040-1 (J2225, Daeeun-ro at the south end of the r60 pedestrian street): the aerial has one wide crossing
  over the whole carriageway (22.5 m across, 11.5 m deep along the road, centre x 220.7 z 1201.85, road heading
  50 deg); the game paints none there since the r60 plaza change. Added like the r64 mid-block crossings
  (no junction binding, white). The NGII source zebra just west of it (where the aerial shows the bicycle
  crossing band) is hidden by a "없음" crossing over that band.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r73_plan.py
Output: config/maps/review/r73-crosswalks.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r73-crosswalks.json'
NW = 'review:r55:J4396_13458:a0:s1:crosswalk'


def build():
    P = E.LanePatch([], 72, 73)
    f = E.FACILITIES[NW]
    P.facilities[NW] = dict(id=NW, x=430.4, z=1356.6, yaw=f['yaw'], width=23.0, length=f['length'])
    item = P.copy_mark('review:r64:J046:crosswalk:005', 'review:r73:J040:crosswalk:001')
    item.update(x=220.7, z=1201.85, yaw=50.0, width=22.5, length=11.5)
    # the NGII source paints a narrow zebra just west of it, where the aerial has only the bicycle crossing band:
    # an authored crossing of type "없음" (RoadSurfaceMarks.Removed) over that band hides the source stripes
    hide = P.copy_mark('review:r64:J046:crosswalk:005', 'review:r73:J040:crosswalk:002')
    hide.update(x=213.9, z=1196.1, yaw=50.0, width=24.0, length=6.5, type='없음')
    return P


if __name__ == '__main__':
    r = build().write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)})
