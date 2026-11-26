"""Daeeun-ro north side between the J075 side street and J5057 (map r73 -> r74, 2026-11-26). See map_lane_edit.py.

J008-5 (J5057 west arm north boundary about 8 m too far north): east of the side street (x 427-482) an earlier
review's kerb-side planting strip (sidewalk:reviewed:A4218G610178) already marks the real kerb, but behind it the
game keeps an asphalt band with the provisional westbound lane local:4861:4677:2204:5298 and the north sidewalk
about 8 m further north; the aerial shows sidewalk with tree pits there.
- the provisional lane no longer starts at J5057: its J5057 entry (join:local:6283:4335:5335:4564:0, one of two
  exits of its east-arm lane) is retired and the lane keeps only its part west of x 427. It stays the closed
  westbound approach that sets the J1927 signal head (r71);
- the north sidewalk comes south to just behind the planting strip (offset +3.7 -> -2.6 about the old lane
  line) between x 427 and 482, tapering over 5 m into the side street corner and 4 m before the J5057 corner;
  the planting strip and every lane stay as they are.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r74_plan.py [--check <dir>]
Output: config/maps/review/r74-north-side.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r74-north-side.json'
WEST, ENTRY = 'local:4861:4677:2204:5298', 'join:local:6283:4335:5335:4564:0'
CUT_X = 427.0
REF = [(p['x'], p['z']) for p in E.LANES[WEST]['points']][::-1]  # the old lane line, running east: + = north


def station_x(x):
    for i, (a, b) in enumerate(zip(REF, REF[1:])):
        if (a[0] - x) * (b[0] - x) <= 0:
            return E.length(REF[:i + 1]) + G.math.dist(a, b) * abs(x - a[0]) / max(1e-9, abs(b[0] - a[0]))


SKIP = list(E.LANES) + [o['id'] for o in E.DOC['scene']['objects']
                        if o['id'].startswith(('road:polish:', 'sidewalk:reviewed:A4218G610178'))]
KERB = G.Edit(REF, s0=station_x(CUT_X + 5.0), s1=station_x(478.0), controls=[(-2.65, -2.65), (3.7, -2.6)], taper=(5, 4), skip=SKIP)


def build():
    P = E.LanePatch([KERB], 73, 74)
    P.disable([ENTRY], closure=False)
    pts = P.pts(WEST)  # runs west from J5057
    keep = [p for p in pts if p[0] <= CUT_X]
    first = next(i for i, p in enumerate(pts) if p[0] <= CUT_X)
    a, b = pts[first - 1], pts[first]
    u = (a[0] - CUT_X) / (a[0] - b[0])
    start = (a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u)
    P.set_points(WEST, [start] + keep)
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)}, r['disableLanes'])
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'east.jpg', (458, 472), 70, 'r74 Daeeun-ro north side')
