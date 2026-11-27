"""r80 north-south road: centre-line paint and a smooth road strip (map r80 -> r81, 2026-11-27).

The r80 lanes took the strip's object id ('road:review:r80:...') as their source. The synthetic paint groups lanes
by the road id, which is the strip id without 'road:', so the new road got no centre line. The strip also followed
the coarse aerial polyline, and its mesh showed each kink as a wave. This patch, on top of r80:
- sets the source of every r80 lane to the strip's road id (yellow centre line between the two directions);
- moves the strip onto a smoothed line through the same aerial points (lanes stay where they are; the smoothed
  line is within 0.3 m of the coarse one).
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r81_plan.py [--check <dir>]
Output: config/maps/review/r81-junction-road-paint.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_road_add as R  # noqa: E402
import map_review_r80_plan as P80  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r81-junction-road-paint.json'
SOURCE = P80.STRIP[len('road:'):]


def smooth(pts, k):
    out = []
    for i in range(len(pts)):
        if i == 0 or i == len(pts) - 1:
            out.append(pts[i]); continue
        a, b = max(0, i - k), min(len(pts), i + k + 1)
        out.append((sum(p[0] for p in pts[a:b]) / (b - a), sum(p[1] for p in pts[a:b]) / (b - a)))
    return out


def build():
    assert E.DOC['revision'] == 80, E.DOC['revision']
    P = R.JunctionPatch([], 80, 81)
    line = smooth(E.resample(P80.CENTRE, 1.0), 6)
    strip = line[::3] + ([line[-1]] if (len(line) - 1) % 3 else [])
    P.objects[P80.STRIP] = dict(id=P80.STRIP, width=P80.WIDTH, points=G._mp(strip))
    for l in E.DOC['lanes']:
        if l['id'].startswith('review:r80:'):
            P.attrs = getattr(P, 'attrs', []); P.attrs.append(dict(id=l['id'], sourceId=SOURCE, ruleId=l['ruleId']))  # ruleId kept as is
    P.drift = max(min(((p[0] - q[0]) ** 2 + (p[1] - q[1]) ** 2) ** .5 for q in E.resample(P80.CENTRE, .25)) for p in strip)
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)}, 'max drift from the aerial line %.2f m' % P.drift)
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'road81.jpg', (356, 1150), 120, 'r81 strip')
