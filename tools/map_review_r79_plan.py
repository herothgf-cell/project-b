"""J2608 north-west branch joined to the road west of it (map r78 -> r79, 2026-11-26). See tools/map_road_add.py.

J044-1: the J2608 north-west branch ends at x 236 (its north-west lane local:2570:13991:2360:14173 has no exit,
its south-east lane local:2324:14131:2532:13951 no entry), and 35 m west the road along the school ground ends
the same way at x 203-205 (local:1783:13935:2051:14331 / local:2026:14348:1758:13952). The aerial shows one road
(about 7 m, one lane each way) bending between them. Two lanes join the open ends with smooth curves along the
lanes' own headings, a 7 m road strip runs midway between them, and sidewalk pieces across it are cut back.
Usage: PYTHONPATH=artifacts/py314-deps python tools/map_review_r79_plan.py [--check <dir>]
Output: config/maps/review/r79-join-road.json (MapReviewGeometryEdit.ApplyFromCommandLine)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import map_geometry_edit as G  # noqa: E402
import map_lane_edit as E  # noqa: E402
import map_road_add as R  # noqa: E402

OUT = G.ROOT / 'config/maps/review/r79-join-road.json'
NW_OUT, SW_IN = 'local:2570:13991:2360:14173', 'local:2026:14348:1758:13952'   # north-west lane ends -> west road's south-west lane
NE_OUT, SE_IN = 'local:1783:13935:2051:14331', 'local:2324:14131:2532:13951'   # west road's north-east lane ends -> branch's south-east lane
STRIP, STYLE_ROAD = 'road:review:r79:J2608-west', 'road:37612022:A0020000:1200:0'


def build():
    P = R.RoadPatch([], 78, 79)
    def join(new_id, src, dst):
        # nearly straight along the aerial road, bending only near the west road (short 6 m handles)
        sp, dp = P.pts(src), P.pts(dst)
        pts = R.curve(sp[-1], R.unit(sp[-2], sp[-1]), dp[0], R.unit(dp[0], dp[1]), steps=24, handle=6.0)
        return P.lane(new_id, pts, src, STRIP, width=3.2, prev=[src], next=[dst])
    a = join('review:r79:J2608:west-bound', NW_OUT, SW_IN)
    b = join('review:r79:J2608:east-bound', NE_OUT, SE_IN)
    pa = [(q['x'], q['z']) for q in a['points']]; pb = [(q['x'], q['z']) for q in b['points']][::-1]
    centre = [((p[0] + q[0]) / 2, (p[1] + q[1]) / 2) for p, q in zip(pa, pb)]
    line = centre[::4]
    if line[-1] != centre[-1]:
        line.append(centre[-1])
    P.strip(STRIP, line, 7.0, STYLE_ROAD)
    for item in (a, b):
        item['ruleId'] = ''
    cut = []
    for p, q in zip(centre[::8], centre[8::8] + [centre[-1]]):
        if p != q:
            cut += P.clip_sidewalks(p, q, 3.5, f'r79:{len(cut)}')
    P.cut = sorted(set(cut))
    # the west road's dead-end cap (it already overlapped that road's north-east lane end) goes with the dead end
    cap = 'sidewalk:generated:1214'
    P.objects.pop(cap, None); P.addObjects = [o for o in P.addObjects if o.get('copyOf') != cap]
    if cap not in P.disableObjects:
        P.disableObjects.append(cap)
    return P


if __name__ == '__main__':
    P = build()
    r = P.write(OUT)
    print(OUT, {k: len(v) for k, v in r.items() if isinstance(v, list)}, 'cut', P.cut, 'off', P.disableObjects)
    if '--check' in sys.argv:
        out = Path(sys.argv[sys.argv.index('--check') + 1]); out.mkdir(parents=True, exist_ok=True)
        P.check(out / 'join.jpg', (220, 1425), 60, 'r79 J044-1')
