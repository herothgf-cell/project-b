"""J7801 north arm, northbound side (map r71 -> r72, 2026-11-26). See tools/map_geometry_edit.py.

J017-1 (north arm) and J017-4: between the J7801 north mouth (z 985) and the start of the reviewed J6199
approach lanes (G05:NW, z 1136) the three northbound lanes sit about 6.7 m apart and the east kerb 15.3 m from
the inner lane; the aerial shows three lanes of about 3.4 m and the kerb about 9 m from the inner lane (x 778.5
at z 1000), the arm about 6 m narrower, as the sheet reads. The five southbound lanes (G05 reviewed) and the west
side road match and stay.
- offsets about the inner northbound lane (+ = toward the median): kerb -15.3 -> -9.3, lanes -13.3 -> -6.8 and
  -6.6 -> -3.4, the inner lane and everything west of it stay; back to the old line inside J7801 over 8 m and
  15 m before the G05:NW lanes;
- the north crosswalk (inside the junction, 14 m south of the lane mouth) is shortened at its east end by the
  same 6 m instead (J017-4), so the junction box is not squeezed;
- the G05 southbound U-turn keeps its curve and now ends in the outer northbound lane (the middle one moves 3 m
  in, which would bring its radius under 4.5 m).
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r72_plan.py [--check <dir>]
Output: config/maps/review/r72-north-arm.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r72-north-arm.json'
CHAIN = ['local:7747:9855:6400:11866', 'local:7747:9855:6400:11866:part:1', 'local:7747:9855:6400:11866:part:2']
REF = []
for i in CHAIN:
    pts = [(q['x'], q['z']) for q in E.LANES[i]['points']]
    REF += pts if not REF else pts[1:]
REF = E.resample(REF, 4.0)  # runs north-west from the J7801 mouth: + = left = toward the median
L = E.length(REF)
UTURN, NB3 = 'join:reviewed:G05:SE:uturn', 'local:7870:9907:6489:11965'
EDIT = G.Edit(REF, s0=0.0, s1=L - 20.0, controls=[(-15.3, -9.3), (-13.3, -6.8), (-6.6, -3.4), (0.0, 0.0)], taper=(8, 15), skip=[UTURN])
CROSSWALK = 'review:r55:J7801_9629:a1:s-1:crosswalk'
SHORTEN = 6.0


def build():
    P = E.LanePatch([EDIT], 71, 72)
    # the G05 U-turn ran into the middle northbound lane, which moves 3 m in (radius under 4.5 m); the outer lane
    # moves to about where the middle one was, so the U-turn keeps its curve and turns into the outer lane
    P.exits(UTURN, [NB3])
    P.blend(UTURN, P.pts(NB3)[0], 10.0, end=True)
    f = E.FACILITIES[CROSSWALK]
    y = math.radians(f['yaw']); fw = (math.sin(y), math.cos(y)); rt = E.right(fw)
    # keep the west end; the east end is the one on the +right side (east): check and move it in
    east = 1 if f['x'] + rt[0] * f['width'] / 2 > f['x'] - rt[0] * f['width'] / 2 else -1
    P.facilities[CROSSWALK] = dict(id=CROSSWALK, x=round(f['x'] - east * rt[0] * SHORTEN / 2, 4), z=round(f['z'] - east * rt[1] * SHORTEN / 2, 4),
                                   yaw=f['yaw'], width=round(f['width'] - SHORTEN, 4), length=f['length'])
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)}, 'ref length', round(L, 1))
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'mouth.jpg', (772, 995), 50, 'r72 J7801 north mouth')
        P.check(out / 'arm.jpg', (730, 1060), 140, 'r72 J7801 north arm')
