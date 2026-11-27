"""J3516 (Y04) south-west left pocket kept out of the oncoming lane (map r81 -> r82, 2026-11-27).

The district traffic test stalled 6 cars at J3516_12881 from r80 on (the new 은행로 north-south road sends cars
through this left turn). The south-west-bound left pocket `reviewed:Y04:SW:left` narrows towards the oncoming
north-east-bound straight (`join:local:2721:12299:3399:12760:0`): 5.7 m between centres 15 m before its end,
2.8 m at the end, and its connector `join:local:4187:13357:3637:12998:-1` goes on to 1.8 m. A car waiting there
for a gap in oncoming traffic stands in the oncoming car's corridor, so that car may not enter, and the two wait
for each other through every green. The north-east pocket on the other side ends 3.6 m from its oncoming lane.
This patch moves the pocket end and the connector start 0.8 m away from the oncoming lane (3.6 m, as on the
other side), fading out over 15 m along both.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r82_plan.py [--check <dir>]
Output: config/maps/review/r82-y04-left-pocket.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_road_add as R  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r82-y04-left-pocket.json'
POCKET, LEFT, ONCOMING = 'reviewed:Y04:SW:left', 'join:local:4187:13357:3637:12998:-1', 'join:local:2721:12299:3399:12760:0'
SHIFT, FADE = 0.8, 15.0


def gaps(P, lane, n):
    o = P.pts(ONCOMING); pts = P.pts(lane)
    pts = pts[-n:] if lane == POCKET else pts[:n]
    return [round(math.dist(p, E.at(o, R.station(o, p))[0]), 2) for p in pts]


def build():
    assert E.DOC['revision'] == 81, E.DOC['revision']
    P = R.JunctionPatch([], 81, 82)
    end = P.pts(POCKET)[-1]; o = P.pts(ONCOMING); q = E.at(o, R.station(o, end))[0]
    away = R.unit(q, end)
    target = (end[0] + away[0] * SHIFT, end[1] + away[1] * SHIFT)
    P.before = (gaps(P, POCKET, 6), gaps(P, LEFT, 16))
    P.blend(POCKET, target, FADE, end=True)
    P.blend(LEFT, target, FADE)
    P.after = (gaps(P, POCKET, 6), gaps(P, LEFT, 16))
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list) and v})
    print('pocket end / connector start, metres to the oncoming lane before', P.before, 'after', P.after)
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'y04.jpg', (360, 1292), 40, 'r82 Y04 SW left pocket')
