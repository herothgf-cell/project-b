"""은행로 start: road strip along the J2338 bend (map r84 -> r85, 2026-11-28).

r84 joined the J2338 road's two northbound lanes to 은행로 with turn connectors, but the r84 strip starts at
x 285 and the J2338 road strip ends at z 875, so the connectors ran over grass. This adds a 9 m strip along the
line midway between the two connectors (the slip road in the aerial), and cuts sidewalks it crosses.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r85_plan.py [--check <dir>]
Output: config/maps/review/r85-eungye-ro-start.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_road_add as R  # noqa: E402
import map_review_r84_plan as P84  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r85-eungye-ro-start.json'
STRIP, WIDTH = 'road:review:r85:eungye-ro-start', 9.0
TURNS = ('review:r84:turn:j2338-3', 'review:r84:turn:j2338-4')


def build():
    assert E.DOC['revision'] == 84, E.DOC['revision']
    P = R.JunctionPatch([], 84, 85)
    a, b = (E.resample(P.pts(t), 1.0) for t in TURNS)
    n = min(len(a), len(b))
    mid = [((a[int(i * (len(a) - 1) / (n - 1))][0] + b[int(i * (len(b) - 1) / (n - 1))][0]) / 2,
            (a[int(i * (len(a) - 1) / (n - 1))][1] + b[int(i * (len(b) - 1) / (n - 1))][1]) / 2) for i in range(n)]
    # run 3 m back into the J2338 road and 3 m on into 은행로 so the strips overlap
    d0 = R.unit(mid[1], mid[0]); d1 = R.unit(mid[-2], mid[-1])
    line = [(mid[0][0] + d0[0] * 3, mid[0][1] + d0[1] * 3)] + mid[::3] + [mid[-1], (mid[-1][0] + d1[0] * 3, mid[-1][1] + d1[1] * 3)]
    P.strip(STRIP, line, WIDTH, P84.STYLE_ROAD)
    cut = []
    for p, q in zip(line, line[1:]):
        cut += P.clip_sidewalks(p, q, WIDTH / 2, f'r85:{len(cut)}')
    P.cut = sorted(set(cut))
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list) and v}, 'cut', P.cut, 'off', P.disableObjects)
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'start85.jpg', (280, 888), 50, 'r85 은행로 start strip')
